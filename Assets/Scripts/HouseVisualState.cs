using UnityEngine;

// ============================================================
// SCRIPT: HouseVisualState
// Gestiona los 3 sprites de una casa: Nueva → Arruinada → Pintada.
//
// SETUP en el Editor (una vez por casa):
//   1. Seleccionar el GameObject de la casa en la escena.
//   2. Add Component → HouseVisualState.
//   3. Configurar 'numeroCasa' (1, 2, 3 o 4).
//
// SPRITES — colocar en Assets/Resources/Mapa/Casas/ con estos nombres exactos:
//   Casa 1 Nueva.png       Casa 2 Nueva.png       Casa 3 Nueva.png       Casa 4 Nueva.png
//   Casa 1 Arruinada.png   Casa 2 Arruinada.png   Casa 3 Arruinada.png   Casa 4 Arruinada.png
//   Casa 1 Pintada.png     Casa 2 Pintada.png     Casa 3 Pintada.png     Casa 4 Pintada.png
//
// FLUJO DE ESTADOS:
//   Nueva      → al iniciar el juego (sprite limpio).
//   Arruinada  → cuando un proyectil / spray / rodillo impacta la casa,
//                o automáticamente al activarse el puzzle en esa casa.
//   Pintada    → cuando el jugador completa el puzzle.
//   (La lluvia revierte Pintada/Arruinada → Nueva)
// ============================================================
public class HouseVisualState : MonoBehaviour
{
    public enum Estado { Nueva, Arruinada, Pintada }

    [Tooltip("Número de casa (1–4). Debe coincidir con los archivos de sprite.")]
    public int numeroCasa = 1;

    public Estado EstadoActual { get; private set; } = Estado.Nueva;

    private SpriteRenderer sr;
    private Sprite spriteNueva;
    private Sprite spriteArruinada;
    private Sprite spritePintada;

    // ── Collider propio para detectar pintura ──────────────────
    // Solo se crea si el GameObject no tiene ningún Collider2D.
    // Las casas con PuzzleStructure ya tienen uno (lo agrega PuzzleStructure.Start),
    // pero se ejecuta antes porque Awake corre antes que Start de otros scripts.
    // Para garantizarlo, usamos Awake.
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        CargarSprites();
        AplicarSprite(Estado.Nueva);

        // Si la casa no tiene ningún collider, agregar un trigger para detectar pintura.
        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.5f;
        }
    }

    // ============================================================
    // CARGA DE SPRITES
    // ============================================================
    void CargarSprites()
    {
        spriteNueva     = CargarSprite($"Mapa/Casas/Casa {numeroCasa} Nueva");
        spriteArruinada = CargarSprite($"Mapa/Casas/Casa {numeroCasa} Arruinada");
        spritePintada   = CargarSprite($"Mapa/Casas/Casa {numeroCasa} Pintada");

        if (spriteNueva     == null) Debug.LogWarning($"HouseVisualState ({name}): no encontró 'Mapa/Casas/Casa {numeroCasa} Nueva'");
        if (spriteArruinada == null) Debug.LogWarning($"HouseVisualState ({name}): no encontró 'Mapa/Casas/Casa {numeroCasa} Arruinada'");
        if (spritePintada   == null) Debug.LogWarning($"HouseVisualState ({name}): no encontró 'Mapa/Casas/Casa {numeroCasa} Pintada'");
    }

    static Sprite CargarSprite(string path)
    {
        // Intento directo (sprite individual)
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) return s;
        // Fallback: sprite sheet en modo Multiple.
        // Tomamos el sprite de mayor área para evitar artefactos de auto-slicing
        // (el auto-slicer puede generar sub-sprites diminutos de píxeles sueltos).
        Sprite[] all = Resources.LoadAll<Sprite>(path);
        if (all == null || all.Length == 0) return null;
        Sprite mayor = all[0];
        for (int i = 1; i < all.Length; i++)
        {
            if (all[i].rect.width * all[i].rect.height > mayor.rect.width * mayor.rect.height)
                mayor = all[i];
        }
        return mayor;
    }

    // ============================================================
    // CAMBIO DE ESTADO (llamado por PuzzleStructure y RainManager)
    // ============================================================
    public void SetEstado(Estado estado)
    {
        EstadoActual = estado;
        AplicarSprite(estado);
    }

    void AplicarSprite(Estado estado)
    {
        if (sr == null) return;
        Sprite target = estado switch
        {
            Estado.Nueva     => spriteNueva,
            Estado.Arruinada => spriteArruinada,
            Estado.Pintada   => spritePintada,
            _                => spriteNueva,
        };
        if (target != null) sr.sprite = target;
    }

    // ============================================================
    // DETECCIÓN DE PINTURA — cambia a Arruinada si la casa recibe
    // un impacto de cualquier arma de pintura del jugador.
    // No hace nada si ya está Arruinada o Pintada.
    // ============================================================
    void OnTriggerEnter2D(Collider2D other)
    {
        if (EstadoActual != Estado.Nueva) return;

        bool esPintura = other.GetComponent<Projectile>() != null
                      || other.GetComponent<spray>()      != null
                      || other.GetComponent<rodillo>()    != null;

        if (esPintura)
            SetEstado(Estado.Arruinada);
    }
}
