using System.Collections.Generic;
using UnityEngine;

// Spawnea NPCs en oleadas discretas separadas por tiempo, con dificultad creciente
// (empieza fácil con un solo tipo, después se van sumando y aumentando cantidades).
// No arranca solo: hay que llamar StartWaves() (lo hace WaveTriggerZone cuando el
// jugador pisa la zona del piso indicada). Mientras no arrancó, no spawnea nada.
public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyTypeConfig
    {
        public string nombre = "Tipo";
        public GameObject prefab;
        [Tooltip("Máximo de este tipo visibles/vivos en simultáneo")]
        public int maxOnScreen = 3;
        [Tooltip("A partir de qué oleada (0 = primera) empieza a aparecer este tipo")]
        public int unlockWave = 0;
        [Tooltip("Cuántos aparecen la primera vez que se desbloquea")]
        public int startCount = 1;
        [Tooltip("Cuántos más se suman por cada oleada siguiente (hasta llegar a maxOnScreen)")]
        public int increasePerWave = 1;

        [HideInInspector] public int aliveCount = 0;
        [HideInInspector] public bool everSpawned = false;
    }

    [Tooltip("Pistola, Porra y Anti Disturbios. Configurar unlockWave creciente para que no aparezcan todos juntos desde el principio.")]
    public List<EnemyTypeConfig> tipos = new List<EnemyTypeConfig>();

    [Header("Oleadas")]
    [Tooltip("Segundos de espera entre el final de una oleada y el inicio de la siguiente")]
    public float timeBetweenWaves = 15f;
    [Tooltip("Cantidad total de oleadas. Después de la última, ya no aparecen más NPCs nuevos.")]
    public int totalWaves = 8;

    [Header("Spawn")]
    [Tooltip("Qué tan lejos del borde de cámara aparecen los NPCs")]
    public float spawnMargin = 1.5f;

    public int CurrentWave { get; private set; } = 0;
    public bool Started { get; private set; } = false;

    // ============================================================
    // VERTICAL SLICE: el minijuego ("Copia de Patrón bajo Presión", GDD 3.7)
    // es INTERNO a la oleada y debe pausarla mientras se resuelve. Mientras
    // Paused = true, Update() no descuenta el timer de oleada ni spawnea.
    // PuzzleStructure es quien prende/apaga esto.
    // ============================================================
    public bool Paused { get; private set; } = false;
    public void SetPaused(bool valor) => Paused = valor;

    private PuzzleStructure puzzleStructure;

    private Camera cam;
    private float waveTimer = 0f;
    private bool waitingForFirstWave = true;

    // VERTICAL SLICE: usados para repartir el 60% "de combate" de la barra
    // de progreso de pintado (GameManager.paintProgress) proporcionalmente
    // a cuántos enemigos del total planeado ya se mataron.
    [Tooltip("Porción de la barra de progreso de pintado que corresponde a limpiar este combate (el resto lo da el minijuego final)")]
    [Range(0f, 1f)] public float combatProgressShare = 0.6f;
    private int totalEnemiesPlanned = 0;
    private int enemiesKilledSoFar = 0;

    // Feedback de playtest: "el minijuego debe aparecer un poco antes en la
    // oleada". Antes la casa se marcaba recién al matar la MITAD (0.5) de
    // los enemigos planeados; ahora se adelanta a un tercio (0.35) del total.
    [Tooltip("Fracción de enemigos planeados que hay que matar para que se marque la casa del minijuego (antes 0.5 = mitad de la oleada)")]
    [Range(0.1f, 0.9f)] public float fraccionOleadaParaPuzzle = 0.35f;

    void Start()
    {
        cam = Camera.main;
    }

    // Llamado directamente desde GomezInteraction.StartExit() al terminar la
    // interacción con Gomez (ya no depende de pisar una zona del piso).
    public void StartWaves()
    {
        if (Started) return;
        Started = true;
        waveTimer = 0f; // la primera oleada sale apenas se llama esto
        totalEnemiesPlanned = ComputeTotalPlanned();
        // GDD 3.7 / feedback de playtest: "la casa debe marcarse para el
        // minijuego en la mitad de la oleada, no en el comienzo". La
        // activación real ahora se dispara desde SpawnOne() cuando se llega
        // a la mitad de los enemigos totales planeados (ver
        // RevisarActivacionMitadDeOleada), no acá al arrancar el combate.
    }

    // Se llama cada vez que muere un enemigo. Apenas se llega (o se pasa) la
    // fracción configurada (fraccionOleadaParaPuzzle) de los enemigos totales
    // planeados para todo el combate, activa la estructura del puzzle por
    // primera y única vez (puzzleStructure se usa como flag de "ya activada").
    void RevisarActivacionMitadDeOleada()
    {
        if (puzzleStructure != null) return; // ya activada
        if (totalEnemiesPlanned <= 0) return;

        if (enemiesKilledSoFar >= Mathf.CeilToInt(totalEnemiesPlanned * fraccionOleadaParaPuzzle))
            ActivarEstructuraDelPuzzle();
    }

    // ============================================================
    // VERTICAL SLICE (GDD 3.7): "Una estructura en el mapa se marca señalizada
    // en medio de la oleada". Se busca una casa ya existente en la escena y se
    // le agrega PuzzleStructure en runtime (AddComponent), sin tocar nada a
    // mano en el Editor. Si en el futuro se quiere una casa específica, basta
    // con ponerle ese nombre exacto en la escena.
    // ============================================================
    void ActivarEstructuraDelPuzzle()
    {
        GameObject estructura = GameObject.Find("casa_0 (1)");
        if (estructura == null) estructura = GameObject.Find("casa_0");

        if (estructura == null)
        {
            Debug.LogWarning("EnemySpawner: no se encontró ninguna 'casa_0' en la escena para marcar como estructura del puzzle del GDD 3.7.");
            return;
        }

        puzzleStructure = estructura.GetComponent<PuzzleStructure>();
        if (puzzleStructure == null) puzzleStructure = estructura.AddComponent<PuzzleStructure>();
        puzzleStructure.spawner = this;
    }

    // Simula (sin instanciar nada) cuántos enemigos van a salir en total a lo
    // largo de todas las oleadas, en el peor caso de que nadie muera nunca
    // entre oleada y oleada. Es una estimación: sirve como denominador para
    // la barra de progreso, no necesita ser exacta al enemigo.
    int ComputeTotalPlanned()
    {
        int total = 0;
        foreach (var tipo in tipos)
        {
            if (tipo.prefab == null) continue;
            int aliveSim = 0;
            for (int waveIndex = 0; waveIndex < totalWaves; waveIndex++)
            {
                if (waveIndex < tipo.unlockWave) continue;
                int wavesSinceUnlock = waveIndex - tipo.unlockWave;
                int desiredCount = tipo.startCount + wavesSinceUnlock * tipo.increasePerWave;
                desiredCount = Mathf.Min(desiredCount, tipo.maxOnScreen);
                int toSpawn = Mathf.Max(0, desiredCount - aliveSim);
                total += toSpawn;
                aliveSim += toSpawn;
            }
        }
        return total;
    }

    void Update()
    {
        if (!Started) return;
        if (Paused) return; // minijuego de la estructura marcada en curso: la oleada queda congelada
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        waveTimer -= Time.deltaTime;
        if (waveTimer <= 0f)
        {
            if (CurrentWave < totalWaves)
            {
                SpawnWave();
                CurrentWave++;
                waveTimer = waitingForFirstWave ? 0f : timeBetweenWaves;
                waitingForFirstWave = false;
            }
            else
            {
                // ya no hay más oleadas: solo controlar si se puede declarar victoria
                waveTimer = 1f;
                CheckVictory();
            }
        }
    }

    void SpawnWave()
    {
        int waveIndex = CurrentWave; // 0-based

        foreach (var tipo in tipos)
        {
            if (tipo.prefab == null) continue;
            if (waveIndex < tipo.unlockWave) continue;

            int wavesSinceUnlock = waveIndex - tipo.unlockWave;
            int desiredCount = tipo.startCount + wavesSinceUnlock * tipo.increasePerWave;
            desiredCount = Mathf.Min(desiredCount, tipo.maxOnScreen);

            int toSpawn = Mathf.Max(0, desiredCount - tipo.aliveCount);
            for (int i = 0; i < toSpawn; i++)
                SpawnOne(tipo);
        }
    }

    void SpawnOne(EnemyTypeConfig tipo)
    {
        Vector3 pos = GetOffscreenPosition();
        GameObject obj = Instantiate(tipo.prefab, pos, Quaternion.identity);

        tipo.aliveCount++;
        tipo.everSpawned = true;

        EnemyHealth health = obj.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.OnDeath += () =>
            {
                tipo.aliveCount--;
                enemiesKilledSoFar++;

                if (GameManager.Instance != null && totalEnemiesPlanned > 0)
                {
                    float progresoCombate = (float)enemiesKilledSoFar / totalEnemiesPlanned * combatProgressShare;
                    GameManager.Instance.SetPaintProgress(progresoCombate);
                }

                RevisarActivacionMitadDeOleada();
                CheckVictory();
            };
        }
    }

    // Punto de reenganche público: PuzzleStructure lo llama al resolver el
    // puzzle con éxito, por si para ese momento ya no quedaban oleadas ni
    // enemigos vivos (si no, CheckVictory ya se habría llamado solo al matar
    // al último enemigo, pero quedaba bloqueada esperando el puzzle).
    public void RecheckVictory() => CheckVictory();

    // Bug de playtest: "Cromagustín entra super rápido en loop". CheckVictory()
    // se llama una vez por cada muerte de NPC, y una vez que la condición de
    // victoria queda cumplida sigue cumplida en cualquier revisión posterior
    // (ej: si dos enemigos mueren el mismo frame, o el puzzle se resuelve
    // después del último enemigo). Sin este flag, OnCombatEnd() se disparaba
    // de nuevo en cada revisión extra, reiniciando la entrada de Cromagustín.
    private bool combateYaTermino = false;

    void CheckVictory()
    {
        if (!Started) return;
        if (combateYaTermino) return; // ya se disparó el final del combate una vez
        if (CurrentWave < totalWaves) return; // todavía quedan oleadas por salir

        foreach (var tipo in tipos)
        {
            if (tipo.aliveCount > 0) return; // todavía queda alguno vivo
        }

        // ============================================================
        // VERTICAL SLICE (corrección del usuario): el puzzle es interno a la
        // oleada, así que el combate no se considera "terminado del todo"
        // hasta que también esté resuelto, aunque ya no queden enemigos.
        // ============================================================
        if (puzzleStructure != null && !puzzleStructure.Completed)
            return;

        // GDD: "la munición de los colores limitados se repondrá completamente
        // al finalizar el encuentro o combate". Este punto es exactamente eso:
        // ya no quedan oleadas ni enemigos vivos y el puzzle interno está resuelto.
        if (AmmoManager.instance != null)
            AmmoManager.instance.RellenarTodoCompletamente();

        // ============================================================
        // VERTICAL SLICE: limpiar todas las oleadas ya NO dispara el panel
        // de Victoria (eso queda reservado únicamente para una derrota real
        // del jugador vía GameManager.GameOver(); este combate es solo una
        // etapa del slice, no el final del juego).
        // En su lugar, entra Cromagustín y la partida sigue hacia el
        // minijuego. Asegurate de tener un CombatEndTrigger en la escena
        // con Cromagustín, EntryStart y EntryEnd asignados.
        // ============================================================
        combateYaTermino = true;

        if (CombatEndTrigger.instance != null)
            CombatEndTrigger.instance.OnCombatEnd();
        else
            Debug.LogWarning("EnemySpawner: se limpiaron todas las oleadas pero no hay un CombatEndTrigger en la escena para hacer entrar a Cromagustín.");
    }

    Vector3 GetOffscreenPosition()
    {
        if (cam == null) cam = Camera.main;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Vector3 center = cam.transform.position;

        float top = center.y + height / 2f + spawnMargin;
        float bottom = center.y - height / 2f - spawnMargin;
        float right = center.x + width / 2f + spawnMargin;
        float left = center.x - width / 2f - spawnMargin;

        int side = Random.Range(0, 4);
        switch (side)
        {
            case 0: return new Vector3(Random.Range(left, right), top, 0f);
            case 1: return new Vector3(Random.Range(left, right), bottom, 0f);
            case 2: return new Vector3(left, Random.Range(bottom, top), 0f);
            default: return new Vector3(right, Random.Range(bottom, top), 0f);
        }
    }
}
