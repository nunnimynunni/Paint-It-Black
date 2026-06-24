using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================
// SCRIPT: PuzzleStructure
// FASE 5 del vertical slice (corregida 2da vez): la estructura marcada del
// GDD 3.7.
// ============================================================
//
// "Una estructura en el mapa se marca señalizada en medio de la oleada, el
//  jugador debe acercarse a ella para comenzar con el puzzle."
//
// EnemySpawner.StartWaves() le agrega este componente en runtime a una casa
// ya existente en la escena (AddComponent), así no hace falta crear nada a
// mano en el Editor. Este script:
//
//  - Agrega su propio Collider2D (trigger) para detectar al jugador.
//  - Se marca con un BORDE/STROKE AMARILLO (no un cuadrado flotante): se
//    reutiliza el mismo esquema que ya usan Gomez/Cromagustin para su
//    outline blanco de "interactuable" (sprite hijo más grande, mismo
//    shader Custom/SpriteWhiteSolid, detrás en sorting order), pero
//    tiñendo el SpriteRenderer de amarillo en vez de blanco. El shader se
//    corrigió para que SÍ respete el tinte (antes pintaba blanco siempre).
//  - Al presionar E en rango, abre PaintCanvasPuzzle y CONGELA TODO EL
//    JUEGO DE FONDO con Time.timeScale = 0 (además de pausar el spawner):
//    los enemigos ya spawneados no se mueven, no atacan, no hay físicas,
//    así que no pueden lastimar al jugador mientras se resuelve el puzzle.
//    El propio puzzle usa tiempo NO escalado (Time.unscaledDeltaTime) para
//    que su cuenta regresiva de 60s siga corriendo en tiempo real.
//  - Victoria (GDD 3.7): otorga el 40% de progreso de pintado reservado
//    al puzzle Y una mejora real al azar (no un punto abstracto), elegida
//    entre las 11 que lista el GDD: arma (daño/cooldown/velocidad de
//    disparo/munición/área), vida y defensa (curación/escudo temporal/
//    reducción de daño/vida extra) o pasivas (velocidad de movimiento/
//    rebote de proyectiles). La aplica UpgradeSystem.OtorgarMejoraAleatoria().
//  - Derrota/timeout (GDD 3.7): el GDD no define ningún castigo adicional
//    más allá de no recibir la recompensa. La UI simplemente se cierra y
//    el punto de interacción queda disponible para reintentar cuando
//    quiera (Completed sigue en false).
// ============================================================
[DisallowMultipleComponent]
public class PuzzleStructure : MonoBehaviour
{
    public EnemySpawner spawner;

    [Tooltip("Progreso de pintado (0-1) que otorga resolver el puzzle con éxito")]
    public float progresoPorCompletar = 0.4f;

    public bool Completed { get; private set; } = false;

    private bool playerInRange = false;
    private bool puzzleOpen = false;
    private GameObject outlineObject;

    void Start()
    {
        CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 2f;

        CrearOutlineAmarillo();
    }

    // Replica el esquema de outline que ya usan los NPCs interactuables
    // (sprite hijo, mismo tamaño que el original, mismo shader, apenas más
    // grande, detrás en sorting order) pero amarillo y visible TODO el
    // tiempo que dure el combate (no solo al acercarse), porque el GDD dice
    // que la estructura queda "señalizada" en cuanto aparece, no recién
    // cuando el jugador está cerca.
    void CrearOutlineAmarillo()
    {
        SpriteRenderer srOriginal = GetComponent<SpriteRenderer>();
        if (srOriginal == null) srOriginal = GetComponentInChildren<SpriteRenderer>();
        if (srOriginal == null)
        {
            Debug.LogWarning("PuzzleStructure: la estructura marcada no tiene SpriteRenderer, no se puede dibujar el borde amarillo.");
            return;
        }

        outlineObject = new GameObject("PuzzleStructure_OutlineAmarillo");
        outlineObject.transform.SetParent(srOriginal.transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localScale = new Vector3(1.06f, 1.06f, 1f);

        SpriteRenderer srOutline = outlineObject.AddComponent<SpriteRenderer>();
        srOutline.sprite = srOriginal.sprite;
        srOutline.sortingOrder = srOriginal.sortingOrder - 1;
        srOutline.color = new Color(1f, 0.85f, 0.05f, 1f); // amarillo

        Material mat = EncontrarMaterialOutlineExistente();
        if (mat != null) srOutline.material = mat;
        // Si no se encuentra el material (no había ningún outline de NPC
        // todavía instanciado en la escena), se usa el material default del
        // SpriteRenderer: no queda idéntico al outline de los NPCs, pero
        // sigue marcando la estructura en amarillo en vez de no marcar nada.
    }

    // No se puede cargar un asset por path en runtime sin que esté en una
    // carpeta Resources, así que se toma prestado el material que ya están
    // usando los outlines de los NPCs (Gomez/Cromagustin) en la escena.
    Material EncontrarMaterialOutlineExistente()
    {
        SpriteRenderer[] todos = FindObjectsOfType<SpriteRenderer>(true);
        foreach (var r in todos)
        {
            if (r == null || r.sharedMaterial == null || r.sharedMaterial.shader == null) continue;
            if (r.sharedMaterial.shader.name == "Custom/SpriteWhiteSolid")
                return r.sharedMaterial;
        }
        return null;
    }

    void Update()
    {
        if (Completed)
        {
            if (outlineObject != null) outlineObject.SetActive(false);
            return;
        }

        if (puzzleOpen) return;
        if (!playerInRange) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            AbrirPuzzle();
    }

    void AbrirPuzzle()
    {
        puzzleOpen = true;
        if (spawner != null) spawner.SetPaused(true);

        // Feedback de playtest: mientras el minijuego está abierto solo debe
        // verse su propia interfaz, no la HUD de combate/exploración de atrás.
        GameManager.SetHudVisible(false);

        // CONGELA TODO EL JUEGO DE FONDO: enemigos, físicas, animaciones.
        // El jugador NO puede recibir daño mientras el puzzle está abierto.
        Time.timeScale = 0f;

        GameObject puzzleObj = new GameObject("PaintCanvasPuzzle_Runtime");
        PaintCanvasPuzzle puzzle = puzzleObj.AddComponent<PaintCanvasPuzzle>();
        puzzle.OnFinished += ManejarResultado;
    }

    // Devuelve el mensaje de la mejora otorgada (para que PaintCanvasPuzzle
    // lo muestre en su cartel de resultado), o null si fue derrota.
    string ManejarResultado(bool victoria)
    {
        puzzleOpen = false;
        Time.timeScale = 1f; // descongela el juego de fondo
        if (spawner != null) spawner.SetPaused(false);

        // Vuelve a mostrar la HUD normal al cerrarse el minijuego (a menos
        // que justo en este instante se haya activado el game over, que la
        // vuelve a esconder por su cuenta).
        if (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
            GameManager.SetHudVisible(true);

        if (!victoria)
        {
            // Derrota/timeout: sin castigo adicional según el GDD. El punto
            // de interacción queda disponible para reintentar cuando quiera.
            return null;
        }

        Completed = true;

        // Feedback de playtest: "la casa del minijuego una vez que este se
        // termina por realizacion correcta debe cambiar del sprite casa a
        // casapinta [...] debe conservar el colider de la casa original
        // cambiando unicamente el sprite en la exacta misma posicion". Solo
        // se reemplaza el sprite del SpriteRenderer; no se toca el Collider2D
        // ni el transform, así que la posición y la forma de colisión quedan
        // exactamente iguales.
        PintarCasa();

        if (GameManager.Instance != null)
            GameManager.Instance.AddPaintProgress(progresoPorCompletar);

        // GDD 3.7: la recompensa real es una mejora de arma, vida/defensa o
        // pasiva, otorgada al azar y aplicada de inmediato (ver UpgradeSystem).
        string mensajeMejora = UpgradeSystem.EnsureInstance().OtorgarMejoraAleatoria();

        if (spawner != null)
            spawner.RecheckVictory();

        return mensajeMejora;
    }

    private static Sprite spriteCasaPintadaCache;

    // Feedback de playtest: cambia el sprite de la SpriteRenderer de la casa
    // (la misma que ya tenía antes de empezar el puzzle) al sprite
    // "casapinta", para mostrar visualmente que quedó pintada. No se toca el
    // Collider2D ni el transform: misma posición, mismo tamaño de colisión.
    void PintarCasa()
    {
        SpriteRenderer srCasa = GetComponent<SpriteRenderer>();
        if (srCasa == null) srCasa = GetComponentInChildren<SpriteRenderer>();
        if (srCasa == null) return;

        Sprite spritePintado = EncontrarSpriteCasaPintada();
        if (spritePintado == null)
        {
            Debug.LogWarning("PuzzleStructure: no se encontró el sprite 'casapinta' en Resources/Mapa, la casa no cambia de aspecto.");
            return;
        }

        srCasa.sprite = spritePintado;
    }

    // El sprite "casapinta" vive en Assets/Assets/Resources/Mapa/casapinta.png
    // (movido ahí especialmente para poder cargarlo en runtime sin asignarlo
    // a mano en el Editor). Se usa LoadAll en vez de Load porque el import
    // está en modo "Multiple" (un solo sub-sprite "casapinta_0" adentro).
    static Sprite EncontrarSpriteCasaPintada()
    {
        if (spriteCasaPintadaCache != null) return spriteCasaPintadaCache;
        Sprite[] sprites = Resources.LoadAll<Sprite>("Mapa/casapinta");
        if (sprites != null && sprites.Length > 0)
            spriteCasaPintadaCache = sprites[0];
        return spriteCasaPintadaCache;
    }

    bool IsPlayer(Collider2D other) => other.CompareTag("Player") || other.transform.root.CompareTag("Player");

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        playerInRange = false;
    }
}
