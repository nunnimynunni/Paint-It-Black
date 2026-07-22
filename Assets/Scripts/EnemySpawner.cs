using System.Collections.Generic;
using UnityEngine;

// Spawnea NPCs en 3 oleadas con drip-feed: nunca más de maxSimultaneo
// enemigos vivos a la vez. A medida que mueren se spawnean más hasta
// completar el total de la oleada. La siguiente oleada no empieza
// hasta que todos los enemigos de la anterior estén muertos.
//
// Oleadas: 5 / 10 / 15 enemigos (tamaños fijos, ver totalPorOleada).
//
// Puzzles: al terminar la oleada 0 y la oleada 1 se activa una casa
// aleatoria (sin pintar) como minijuego de pintado. Las dos casas
// elegidas son distintas. La victoria final requiere que ambas estén
// completadas además de que no queden enemigos de la oleada 3.
//
// No arranca solo: hay que llamar StartWaves() (lo hace GomezInteraction).
public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyTypeConfig
    {
        public string nombre = "Tipo";
        public GameObject prefab;
        [Tooltip("A partir de qué oleada (0 = primera) empieza a aparecer este tipo")]
        public int unlockWave = 0;
        [Tooltip("Peso relativo de spawn. 1 = normal, 0.3 = poco frecuente. Se usa para el Encerador.")]
        public float spawnWeight = 1f;
        // aliveCount y everSpawned se movieron a arrays privados en EnemySpawner
        // para evitar el error de layout de serialización en builds (Unity 6).
    }

    [Tooltip("Pistola, Porra y Anti Disturbios. Configurar unlockWave para desbloquear gradualmente.")]
    public List<EnemyTypeConfig> tipos = new List<EnemyTypeConfig>();

    [Header("Oleadas")]
    [Tooltip("Cantidad total de oleadas")]
    public int totalWaves = 3;
    [Tooltip("Segundos de pausa entre oleadas, después de eliminar todos los enemigos")]
    public float timeBetweenWaves = 5f;

    [Header("Límite simultáneo")]
    [Tooltip("Máximo de enemigos vivos en pantalla al mismo tiempo. A medida que mueren se spawnean más.")]
    public int maxSimultaneo = 7;

    [Header("Spawn Points")]
    [Tooltip("Puertas de las casas. Se elige uno al azar por cada spawn.")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Spawn sin puntos fijos (fallback)")]
    [Tooltip("Qué tan lejos del borde de cámara aparecen los NPCs cuando no hay spawn points")]
    public float spawnMargin = 4.5f;

    public int CurrentWave { get; private set; } = 0;
    public bool Started    { get; private set; } = false;

    // VERTICAL SLICE: el minijuego pausa el spawner mientras está abierto.
    public bool Paused { get; private set; } = false;
    public void SetPaused(bool valor) => Paused = valor;

    // Un puzzle por cada oleada con minijuego (oleada 0 y oleada 1).
    private PuzzleStructure puzzleOleada0;
    private PuzzleStructure puzzleOleada1;

    private Camera cam;
    private float waveTimer = 0f;

    [Header("Spawn progresivo")]
    [Tooltip("Segundos mínimos entre cada spawn individual dentro de la oleada. " +
             "Evita que todos los NPCs salgan de golpe.")]
    public float tiempoEntreSpawns = 0.7f;
    private float spawnTimer = 0f;

    // Estado del drip-feed de la oleada activa
    private bool waveActive  = false;
    private int  waveTotal   = 0; // total de enemigos a spawnear esta oleada
    private int  waveSpawned = 0; // cuántos ya se spawnearon

    // Si queda 1 solo NPC y el jugador no lo elimina en TIMEOUT_ULTIMO_NPC segundos,
    // se le aplica la muerte automáticamente y la oleada avanza.
    // EXCEPCIÓN: la última oleada no tiene timeout (el jugador debe limpiarla).
    private float timerUltimoNPC = 0f;
    private const float TIMEOUT_ULTIMO_NPC = 30f;

    // Control de Encerador por oleada: exactamente 2 por oleada.
    // Se spawnean en los slots 1 y 2 (después del primer soldado de cubierta)
    // y siempre aparecen junto a un NPC vivo para no quedar solos en el mapa.
    private int enceradorSpawnadosEstaOleada = 0;
    private const int enceradorePorOleada    = 2;
    private int  indiceEncerador = -1; // cacheado al iniciar

    [Tooltip("Porción de la barra de progreso de pintado que corresponde a limpiar este combate")]
    [Range(0f, 1f)] public float combatProgressShare = 0.6f;
    private int totalEnemiesPlanned = 0;
    private int enemiesKilledSoFar  = 0;

    void Start()
    {
        cam = Camera.main;

        // WebGL: algunos campos del componente pueden no deserializarse correctamente
        // desde la escena binaria. Si spawnPoints está vacío o maxSimultaneo es 0,
        // los recuperamos en runtime para no depender de la serialización.
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] spawnPoints vacíos en WebGL — buscando SpawnPoints por nombre...");
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.gameObject.name.StartsWith("SpawnPoint"))
                    spawnPoints.Add(t);
            Debug.Log($"[EnemySpawner] SpawnPoints recuperados: {spawnPoints.Count}");
        }

        if (maxSimultaneo <= 0)
        {
            Debug.LogWarning("[EnemySpawner] maxSimultaneo era 0 en WebGL — usando 7 por defecto.");
            maxSimultaneo = 7;
        }
    }

    // Llamado desde GomezInteraction.StartExit() al terminar la interacción con Gomez.
    public void StartWaves()
    {
        if (Started) return;
        Started = true;
        Debug.Log($"[EnemySpawner] StartWaves: tipos={tipos.Count}, spawnPoints={spawnPoints.Count}");
        for (int i = 0; i < tipos.Count; i++)
            Debug.Log($"[EnemySpawner] tipo[{i}] '{tipos[i].nombre}' prefab={(tipos[i].prefab != null ? tipos[i].prefab.name : "NULL")} unlockWave={tipos[i].unlockWave}");
        waveTimer = 0f;
        totalEnemiesPlanned = ComputeTotalPlanned();
        tipoAlive   = new int[tipos.Count];
        tipoSpawned = new bool[tipos.Count];

        // Cachear el índice del Encerador una sola vez
        indiceEncerador = -1;
        for (int i = 0; i < tipos.Count; i++)
            if (tipos[i].nombre.Contains("Encerador")) { indiceEncerador = i; break; }
    }

    void Update()
    {
        if (!Started || Paused) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        if (waveActive)
        {
            // Drip-feed progresivo: un NPC cada tiempoEntreSpawns segundos,
            // siempre que haya cupo y queden por spawnear.
            spawnTimer -= Time.deltaTime;
            if (TotalAlive() < maxSimultaneo && waveSpawned < waveTotal && spawnTimer <= 0f)
            {
                int tipoIndex = ElegirTipoIndex(CurrentWave);
                if (tipoIndex >= 0)
                {
                    // Los Enceradores aparecen junto a un NPC vivo para no quedar
                    // solos entre edificios. El resto usa spawn points normales.
                    Vector3 pos;
                    if (tipoIndex == indiceEncerador)
                    {
                        pos = ObtenerPosicionCercaDeNPC();
                    }
                    else
                    {
                        Transform sp = spawnPoints.Count > 0 ? spawnPoints[Random.Range(0, spawnPoints.Count)] : null;
                        pos = (sp != null) ? sp.position : GetOffscreenPosition();
                    }
                    SpawnOneAt(tipoIndex, pos);
                    waveSpawned++;
                    spawnTimer = tiempoEntreSpawns;
                }
                else
                {
                    Debug.LogWarning($"EnemySpawner: sin tipos disponibles para oleada {CurrentWave + 1}. Revisá unlockWave.");
                }
            }

            // Auto-eliminar el último NPC si lleva demasiado tiempo sin morir.
            // Solo en oleadas no-finales: la última siempre la debe limpiar el jugador.
            bool esUltimaOleada = (CurrentWave >= totalWaves - 1);
            if (!esUltimaOleada && waveSpawned >= waveTotal && TotalAlive() == 1)
            {
                timerUltimoNPC += Time.deltaTime;
                if (timerUltimoNPC >= TIMEOUT_ULTIMO_NPC)
                {
                    Debug.Log("[EnemySpawner] Tiempo agotado: eliminando último NPC de la oleada.");
                    EliminarUltimoNPCVivo();
                    timerUltimoNPC = 0f;
                }
            }
            else
            {
                timerUltimoNPC = 0f;
            }

            // Oleada terminada: se spawnearon todos Y no queda ninguno vivo
            if (waveSpawned >= waveTotal && TotalAlive() == 0)
            {
                waveActive = false;
                int oleadaTerminada = CurrentWave;
                CurrentWave++;

                // Activar minijuego al final de la oleada 0 y de la oleada 1.
                // Cada una elige una casa distinta al azar.
                if (oleadaTerminada == 0)
                    puzzleOleada0 = ActivarEstructuraDelPuzzle(null);
                else if (oleadaTerminada == 1)
                    puzzleOleada1 = ActivarEstructuraDelPuzzle(puzzleOleada0 != null ? puzzleOleada0.gameObject : null);

                if (CurrentWave >= totalWaves)
                {
                    CheckVictory();
                    return;
                }
                waveTimer = timeBetweenWaves; // pausa antes de la siguiente oleada
            }
            return;
        }

        waveTimer -= Time.deltaTime;
        if (waveTimer > 0f) return;

        if (CurrentWave < totalWaves)
        {
            IniciarOleada();
        }
        else
        {
            waveTimer = 1f;
            CheckVictory();
        }
    }

    // Total de enemigos por oleada (índice 0, 1, 2 → oleadas 1, 2, 3)
    private static readonly int[] totalPorOleada = { 5, 10, 15 };

    void IniciarOleada()
    {
        waveTotal = CurrentWave < totalPorOleada.Length
            ? totalPorOleada[CurrentWave]
            : (CurrentWave + 1) * 5; // fallback si hay más de 3 oleadas
        waveSpawned = 0;
        waveActive  = true;
        timerUltimoNPC = 0f;
        enceradorSpawnadosEstaOleada = 0;
        Debug.Log($"Oleada {CurrentWave + 1}: {waveTotal} enemigos en total, máx {maxSimultaneo} simultáneos.");
    }

    // Aplica daño letal al único NPC vivo que queda (búsqueda por EnemyHealth).
    // Usa TakeDamage para que se ejecute la secuencia de muerte completa
    // (drop de munición, curación al jugador, animación Defeated, etc.).
    void EliminarUltimoNPCVivo()
    {
        EnemyHealth[] todos = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth e in todos)
        {
            if (e == null || !e.IsAlive()) continue;
            e.TakeDamage(99999, PaintColor.Gray);
            break;
        }
    }

    // Devuelve el índice en 'tipos' del tipo elegido por peso relativo, o -1 si no hay disponibles.
    // Los Enceradores se reservan en los slots 1 y 2 (después del primer soldado de cubierta)
    // y se excluyen del resto de la selección aleatoria para que sean exactamente 2 por oleada.
    int ElegirTipoIndex(int waveIndex)
    {
        bool enceradorDisponible = indiceEncerador >= 0
            && tipos[indiceEncerador].prefab != null
            && waveIndex >= tipos[indiceEncerador].unlockWave;

        // Slots reservados para Encerador: posiciones 1 y 2 de la oleada
        // (así el primer soldado normal ya está en el mapa cuando entran).
        if (enceradorDisponible
            && enceradorSpawnadosEstaOleada < enceradorePorOleada
            && waveSpawned >= 1
            && waveSpawned <= enceradorePorOleada)
        {
            enceradorSpawnadosEstaOleada++;
            return indiceEncerador;
        }

        // Resto de slots: selección por peso, sin incluir al Encerador.
        float pesoTotal = 0f;
        for (int i = 0; i < tipos.Count; i++)
        {
            if (tipos[i].prefab == null || waveIndex < tipos[i].unlockWave) continue;
            if (enceradorDisponible && i == indiceEncerador) continue;
            pesoTotal += Mathf.Max(0.01f, tipos[i].spawnWeight);
        }

        if (pesoTotal <= 0f) return -1;

        float r = Random.Range(0f, pesoTotal);
        float acum = 0f;
        for (int i = 0; i < tipos.Count; i++)
        {
            if (tipos[i].prefab == null || waveIndex < tipos[i].unlockWave) continue;
            if (enceradorDisponible && i == indiceEncerador) continue;
            acum += Mathf.Max(0.01f, tipos[i].spawnWeight);
            if (r <= acum) return i;
        }
        return -1;
    }

    // Devuelve la posición de un NPC vivo al azar + un pequeño offset,
    // para que el Encerador aparezca pegado a él y ya tenga "escolta" de entrada.
    Vector3 ObtenerPosicionCercaDeNPC()
    {
        EnemyHealth[] todos = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        var candidatos = new System.Collections.Generic.List<EnemyHealth>();
        foreach (var e in todos)
        {
            if (e == null || !e.IsAlive()) continue;
            if (e.GetComponent<EnceradorAI>() != null) continue; // no spawnear junto a otro Encerador
            candidatos.Add(e);
        }

        if (candidatos.Count > 0)
        {
            var elegido = candidatos[Random.Range(0, candidatos.Count)];
            // Pequeño offset aleatorio para no quedar exactamente encima
            Vector2 offset = Random.insideUnitCircle * 0.6f;
            return elegido.transform.position + (Vector3)offset;
        }

        // Fallback: posición fuera de pantalla normal
        return GetOffscreenPosition();
    }

    void SpawnOneAt(int tipoIndex, Vector3 pos)
    {
        EnemyTypeConfig tipo = tipos[tipoIndex];
        GameObject obj = Instantiate(tipo.prefab, pos, Quaternion.identity);
        tipoAlive[tipoIndex]++;
        tipoSpawned[tipoIndex] = true;

        EnemyHealth health = obj.GetComponent<EnemyHealth>();
        if (health == null) return;

        health.OnDeath += () =>
        {
            tipoAlive[tipoIndex]--;
            enemiesKilledSoFar++;

            if (GameManager.Instance != null && totalEnemiesPlanned > 0)
                GameManager.Instance.SetPaintProgress(
                    (float)enemiesKilledSoFar / totalEnemiesPlanned * combatProgressShare);

            CheckVictory();
        };
    }

    int TotalAlive()
    {
        int total = 0;
        if (tipoAlive != null)
            foreach (int c in tipoAlive) total += c;
        return total;
    }

    int ComputeTotalPlanned()
    {
        // Usa los tamaños fijos de oleada (no la fórmula vieja por spawn points).
        int total = 0;
        for (int w = 0; w < totalWaves; w++)
            total += w < totalPorOleada.Length ? totalPorOleada[w] : (w + 1) * 5;
        return total; // 5+10+15 = 30 con 3 oleadas
    }

    // Elige una casa activa sin pintar al azar y le agrega el componente PuzzleStructure.
    // excluir: casa ya usada en la oleada anterior (para que no repita).
    PuzzleStructure ActivarEstructuraDelPuzzle(GameObject excluir)
    {
        // Busca por Transform (no por SpriteRenderer) para que funcione aunque
        // el SpriteRenderer esté en un hijo y no en el root del GameObject.
        // Usa Include para no saltear casas dentro de padres temporalmente inactivos.
        var candidatas = new List<GameObject>();
        var todos = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in todos)
        {
            string nombre = t.gameObject.name;
            if (!nombre.StartsWith("Casa", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (nombre.IndexOf("Pintada", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (!t.gameObject.activeInHierarchy) continue;
            // GetComponentInParent para cubrir el caso en que PuzzleStructure
            // esté en el padre del SpriteRenderer (evita elegir la misma casa dos veces).
            if (t.GetComponentInParent<PuzzleStructure>() != null) continue;
            if (t.GetComponent<PuzzleStructure>() != null) continue;
            if (excluir != null && t.gameObject == excluir) continue;
            // Necesita un SpriteRenderer en sí mismo o en sus hijos para poder mostrar el outline.
            if (t.GetComponent<SpriteRenderer>() == null && t.GetComponentInChildren<SpriteRenderer>() == null) continue;
            candidatas.Add(t.gameObject);
        }

        if (candidatas.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: no hay casas disponibles para activar el puzzle.");
            return null;
        }

        GameObject estructura = candidatas[Random.Range(0, candidatas.Count)];
        PuzzleStructure ps = estructura.AddComponent<PuzzleStructure>();
        ps.spawner = this;
        Debug.Log($"EnemySpawner: puzzle activado en '{estructura.name}'.");
        return ps;
    }

    public void RecheckVictory() => CheckVictory();

    // Estado de runtime por tipo (no serializado — arrays privados indexados igual que 'tipos')
    private int[]  tipoAlive;    // cuántos enemigos de cada tipo están vivos
    private bool[] tipoSpawned;  // si alguna vez se spawneó cada tipo

    private bool combateYaTermino = false;
    // Expuesto para que RainManager sepa cuándo dejar de generar lluvia.
    public bool CombateTerminado => combateYaTermino;

    void CheckVictory()
    {
        if (!Started || combateYaTermino) return;
        if (CurrentWave < totalWaves) return;
        if (TotalAlive() > 0) return;
        // Ambos puzzles deben estar completados (si fueron activados).
        if (puzzleOleada0 != null && !puzzleOleada0.Completed) return;
        if (puzzleOleada1 != null && !puzzleOleada1.Completed) return;

        combateYaTermino = true;

        if (AmmoManager.instance != null)
            AmmoManager.instance.RellenarTodoCompletamente();
        if (MusicManager.Instance != null)
            MusicManager.Instance.CrossfadeAExploracion();

        if (CombatEndTrigger.instance != null)
            CombatEndTrigger.instance.OnCombatEnd();
        else
            Debug.LogWarning("EnemySpawner: no hay CombatEndTrigger en la escena para hacer entrar a Cromagustín.");
    }

    Vector3 GetOffscreenPosition()
    {
        if (cam == null) cam = Camera.main;
        float height = cam.orthographicSize * 2f;
        float width  = height * cam.aspect;
        Vector3 center = cam.transform.position;
        float top    = center.y + height / 2f + spawnMargin;
        float bottom = center.y - height / 2f - spawnMargin;
        float right  = center.x + width  / 2f + spawnMargin;
        float left   = center.x - width  / 2f - spawnMargin;
        switch (Random.Range(0, 4))
        {
            case 0:  return new Vector3(Random.Range(left, right), top,    0f);
            case 1:  return new Vector3(Random.Range(left, right), bottom, 0f);
            case 2:  return new Vector3(left,  Random.Range(bottom, top),  0f);
            default: return new Vector3(right, Random.Range(bottom, top),  0f);
        }
    }
}
