using UnityEngine;

// ============================================================
// SCRIPT: AmmoPickup
// GDD: "Reposición de Munición [...] También se puede lootear en puntos
// donde hayan rastros de pintura (en donde aparecerá la gota de color)."
//
// Es una gota de pintura que queda flotando en el piso (la dropea
// EnemyHealth.Die()) y que, al ser tocada por el jugador, rellena la
// munición de ese color y muestra feedback (texto "+N" flotante, ver
// MunicionFeedback.cs). El HUD de munición (AmmoHUD) ya lee
// AmmoManager.GetAmmo() todos los frames, así que se actualiza solo en
// cuanto se suma la munición: no hace falta tocarlo desde aquí.
//
// Reutiliza tamaños/sprites ya existentes: toma prestado en runtime el
// mismo sprite "GOTA MUNITION1" (llena) que ya usa cualquier AmmoHUD
// presente en la escena, igual truco que PuzzleStructure/PlayerHealth
// usan para encontrar el material de outline sin necesitar una carpeta
// Resources. Así no hace falta crear ni asignar ningún asset nuevo a mano.
// ============================================================
[DisallowMultipleComponent]
public class AmmoPickup : MonoBehaviour
{
    private const float RADIO_RECOLECCION = 0.45f;
    // Feedback de playtest: "las gotas de regeneración de munición deben ser
    // aun más grandes y deben tener una animación de hacia arriba y hacia
    // abajo para mostrar que son consumibles" — se sube bastante más la
    // amplitud del flote (antes 0.12) para que el movimiento se note a
    // simple vista incluso sin mirar fijo la gota.
    private const float ALTURA_BOB = 0.22f;
    private const float VELOCIDAD_BOB = 3f;

    public PaintColor color;
    public int cantidad = 8;

    private Vector3 posInicial;
    private float bobTimer;

    // Crea en runtime una gota de munición lootable en la posición dada.
    // No requiere ningún prefab armado a mano en el Editor.
    public static GameObject Crear(Vector3 posicion, PaintColor colorGota, int cantidadOtorgada = 8)
    {
        GameObject obj = new GameObject("AmmoPickup_" + colorGota);
        obj.transform.position = posicion;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = EncontrarSpriteGota();
        sr.color = PaintColorUtils.ToUnityColor(colorGota);
        // sortingOrder 200: siempre por encima de todos los sprites del mapa/NPCs (YSort usa ~0-100)
        sr.sortingOrder = 200;
        // Escala más grande para visibilidad
        obj.transform.localScale = new Vector3(2.0f, 2.0f, 1f);

        CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = RADIO_RECOLECCION;

        AmmoPickup pickup = obj.AddComponent<AmmoPickup>();
        pickup.color = colorGota;
        pickup.cantidad = cantidadOtorgada;

        return obj;
    }

    static Sprite EncontrarSpriteGota()
    {
        // Incluye inactivos por consistencia con PaintCanvasPuzzle (ver su
        // EncontrarSpriteGota): si en algún momento el hudCombate está
        // oculto, esto sigue encontrando el sprite igual.
        AmmoHUD hud = Object.FindFirstObjectByType<AmmoHUD>(FindObjectsInactive.Include);
        if (hud != null && hud.dropSprites != null && hud.dropSprites.Length > 0 && hud.dropSprites[0] != null)
            return hud.dropSprites[0]; // gota "llena", la misma que ya usa el HUD
        return null;
    }

    void Start()
    {
        posInicial = transform.position;
    }

    void Update()
    {
        // Flota suavemente en el lugar (el "rastro de pintura" del GDD),
        // para que se note a simple vista que es algo que se puede recoger.
        bobTimer += Time.deltaTime * VELOCIDAD_BOB;
        transform.position = posInicial + Vector3.up * Mathf.Sin(bobTimer) * ALTURA_BOB;
    }

    bool EsJugador(Collider2D other) => other.CompareTag("Player") || other.transform.root.CompareTag("Player");

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!EsJugador(other)) return;

        if (AmmoManager.instance != null)
            AmmoManager.instance.AddAmmo(color, cantidad);

        MunicionFeedback.Mostrar(transform.position, color, cantidad);

        Destroy(gameObject);
    }
}
