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

    private Camera cam;
    private float waveTimer = 0f;
    private bool waitingForFirstWave = true;

    void Start()
    {
        cam = Camera.main;
    }

    // Llamado desde afuera (WaveTriggerZone) cuando el jugador pisa la zona que arranca el combate.
    public void StartWaves()
    {
        if (Started) return;
        Started = true;
        waveTimer = 0f; // la primera oleada sale apenas se llama esto
    }

    void Update()
    {
        if (!Started) return;
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
                CheckVictory();
            };
        }
    }

    void CheckVictory()
    {
        if (!Started) return;
        if (CurrentWave < totalWaves) return; // todavía quedan oleadas por salir

        foreach (var tipo in tipos)
        {
            if (tipo.aliveCount > 0) return; // todavía queda alguno vivo
        }

        if (GameManager.Instance != null)
            GameManager.Instance.Victory();
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
