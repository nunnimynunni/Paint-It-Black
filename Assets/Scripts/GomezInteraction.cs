using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================
// SCRIPT: GomezInteraction
// USADO EN: Gomez (NPC inicial) y Cromagustin (NPC final)
// ESTE SCRIPT ES REUTILIZABLE PARA CUALQUIER NPC INTERACTUABLE
// ============================================================
//
// FLUJO DEL JUEGO (corregido según el GDD y la corrección del usuario):
//
// 1. EXPLORACIÓN
//    - HUD de exploración visible (barra de progreso con rodillo)
//    - Al acercarse al NPC aparece un outline blanco
//    - Presionar E abre el diálogo
//
// 2. DIÁLOGO
//    - HUD de exploración se oculta
//    - E avanza el texto (si está escribiéndose, lo completa al instante)
//    - Al terminar el último texto: depende de exitAfterDialog
//
// 3a. SI exitAfterDialog = true (ej: Gomez)
//    - El NPC camina hacia arriba y desaparece
//    - hudCombate se activa
//    - El combate arranca ACÁ MISMO: StartExit() llama directamente a
//      EnemySpawner.StartWaves() (ya no hay zona de piso de por medio, el
//      trigger del piso quedó eliminado/deprecado a pedido del usuario).
//    - En medio de esa oleada va a aparecer una estructura marcada
//      (PuzzleStructure, creada en runtime por el propio EnemySpawner) que
//      dispara el minijuego "Copia de Patrón bajo Presión" y pausa la
//      oleada mientras se resuelve.
//
// 3b. SI exitAfterDialog = false (ej: Cromagustin)
//    - El NPC se queda en la escena
//    - No activa ningún HUD
//    - Si triggersEndingAfterDialog = true, dispara el fundido a negro +
//      créditos de cierre del vertical slice (Etapa 3 del GDD).
//
// 4. FIN DEL COMBATE + PUZZLE
//    - EnemySpawner, cuando ya no quedan oleadas/enemigos Y el puzzle de la
//      estructura marcada fue resuelto, llama:
//      CombatEndTrigger.instance.OnCombatEnd()
//    - Esto activa a Cromagustin y lo hace entrar corriendo desde la
//      izquierda de la pantalla hacia el centro.
//
// SETUP EN UNITY POR NPC:
//    - CircleCollider2D (Is Trigger = true, Radius = 1.5)
//    - Rigidbody2D (Kinematic)
//    - Child "outline" con SpriteRenderer + material outlineblanco + Scale 1.08
//    - Animator con trigger "WalkNorth" y estado Idle por defecto
//    - DialogManager propio por NPC (cada uno tiene su panel y textos)
// ============================================================

public class GomezInteraction : MonoBehaviour
{
    [Header("Outline")]
    // Child del NPC con SpriteRenderer + material outlineblanco, Scale 1.08
    public GameObject outlineObject;

    [Header("Diálogo")]
    // Cada NPC tiene su propio DialogManager con su panel y textos
    public DialogManager dialogManager;

    [Header("HUD")]
    // Solo asignar en Gomez. Cromagustin no maneja HUDs.
    // hudExploracion: se oculta al abrir el diálogo y no vuelve más
    public GameObject hudExploracion;
    // hudCombate: se activa al terminar el diálogo (gotas, barra de vida, arma)
    public GameObject hudCombate;
    // Referencia a HudCombateVisual (los elementos visuales: MarcoArmas, barra vida, gotas).
    // Asignado en la escena como los demás campos de HUD.
    [SerializeField] private GameObject hudCombateVisual;

    [Header("Salida")]
    // true = NPC desaparece al terminar diálogo (Gomez)
    // false = NPC se queda en la escena (Cromagustin)
    public bool exitAfterDialog = true;
    public float exitSpeed = 2f;
    public string walkNorthTrigger = "WalkNorth";

    [Header("Final del slice (vertical slice)")]
    [Tooltip("Si exitAfterDialog = false y esto está activo, al cerrar el diálogo dispara el fundido a negro + créditos de cierre (ej: Cromagustin). Por defecto en true porque hoy es el único NPC con exitAfterDialog=false en el slice.")]
    public bool triggersEndingAfterDialog = true;
    private bool endingStarted = false;

    private bool playerInRange = false;
    private bool dialogOpen = false;
    private bool exiting = false;
    private Animator animator;
    private CircleCollider2D colliderSolido;

    // Para mantener el outline siempre 1 sortingOrder detrás de Gomez.
    private SpriteRenderer srSelf;
    private SpriteRenderer srOutline;

    // ============================================================
    // Pedido del usuario: "gomez debe evitar estas estructuras al irse"
    // (casas/árboles/arbustos). El collider sólido se apaga durante la
    // salida (ver StartExit) para no bloquear físicamente al jugador en el
    // camino, así que la evasión de estructuras se hace por steering, igual
    // esquema que ya usa EnemyAI.AplicarEvasionDeEstructuras: un empuje
    // hacia afuera de cualquier obstáculo sólido cercano (ObstacleUtils),
    // sumado a la dirección de salida deseada (hacia arriba).
    // ============================================================
    [Header("Evasión de estructuras al salir")]
    public float distanciaMinimaEstructurasSalida = 0.5f;
    private static readonly Collider2D[] bufferEstructurasSalida = new Collider2D[8];
    private static readonly ContactFilter2D filtroEstructurasSalida = CrearFiltroSinFiltrarSalida();
    private static ContactFilter2D CrearFiltroSinFiltrarSalida()
    {
        return ContactFilter2D.noFilter;
    }

    void Start()
    {
        srSelf = GetComponent<SpriteRenderer>();

        if (outlineObject != null)
        {
            srOutline = outlineObject.GetComponent<SpriteRenderer>();

            // Destruir YSort del outline para que no compita con el control manual
            // de sortingOrder que hacemos en LateUpdate (outline siempre = Gomez - 1).
            YSort ysOutline = outlineObject.GetComponent<YSort>();
            if (ysOutline != null) Destroy(ysOutline);

            outlineObject.SetActive(false);
        }

        if (hudCombate != null)
            hudCombate.SetActive(false);

        if (hudCombateVisual != null)
            hudCombateVisual.SetActive(false);

        animator = GetComponent<Animator>();

        AsegurarColliderSolido();
    }

    void LateUpdate()
    {
        // Mantiene el outline siempre 1 sortingOrder detrás de Gomez,
        // independientemente de qué valor le haya asignado YSort a Gomez este frame.
        if (srSelf != null && srOutline != null)
            srOutline.sortingOrder = srSelf.sortingOrder - 1;
    }

    // ============================================================
    // Feedback de playtest: "gomez y cromagustin deben tener colider no
    // debo poder traspasarlos". El collider que ya tienen (CircleCollider2D
    // con Is Trigger = true) es solo para detectar el rango de interacción,
    // así que el jugador los atraviesa sin chocar. Se agrega por código un
    // SEGUNDO collider, sólido (Is Trigger = false), más chico que el de
    // rango, para bloquear físicamente sin tocar el del Editor ni romper
    // OnTriggerEnter2D/Exit2D (que sigue funcionando con el trigger original).
    // ============================================================
    void AsegurarColliderSolido()
    {
        Collider2D[] existentes = GetComponents<Collider2D>();
        foreach (var c in existentes)
        {
            if (!c.isTrigger)
            {
                colliderSolido = c as CircleCollider2D;
                return; // ya hay uno sólido (asignado a mano), no duplicar
            }
        }

        colliderSolido = gameObject.AddComponent<CircleCollider2D>();
        colliderSolido.isTrigger = false;
        colliderSolido.radius = 0.4f; // bloqueo físico, más chico que el radio de interacción (1.5)
    }

    void Update()
    {
        if (exiting) return;
        if (!playerInRange) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (!dialogOpen)
            {
                dialogOpen = true;
                if (hudExploracion != null) hudExploracion.SetActive(false);
                if (dialogManager != null) dialogManager.OpenDialog();

                // La música de exploración BYN sigue sonando durante el diálogo
                // con Gomez (ya no hace crossfade a Boss acá). El crossfade a
                // "Peleas Genericas" ocurre en StartExit(), cuando Gomez sale y
                // arrancan las oleadas.
            }
            else
            {
                bool stillOpen = dialogManager != null && dialogManager.Advance();
                if (!stillOpen)
                {
                    dialogOpen = false;
                    if (hudCombate != null) hudCombate.SetActive(true);
                    if (hudCombateVisual != null) hudCombateVisual.SetActive(true);
                    WeaponCursor.Instance?.ActivarModoOleada();
                    if (exitAfterDialog) StartExit();
                    else if (triggersEndingAfterDialog) StartEndingPostDialog();
                }
            }
        }
    }

    // VERTICAL SLICE (Etapa 3 del GDD): dispara el cierre del slice -fundido a
    // negro + créditos- desde GameManager, que es quien ya controla el estado
    // global de partida (IsGameOver, etc.), en vez de armar la UI acá mismo.
    void StartEndingPostDialog()
    {
        if (endingStarted) return;
        endingStarted = true;

        if (GameManager.Instance != null)
            GameManager.Instance.ShowEndingCredits();
    }

    void StartExit()
    {
        exiting = true;
        if (outlineObject != null) outlineObject.SetActive(false);
        if (animator != null) animator.SetTrigger(walkNorthTrigger);

        // Al irse caminando hacia arriba y salir de cámara, se apaga el
        // collider sólido para que no empuje/bloquee al jugador en el camino.
        if (colliderSolido != null) colliderSolido.enabled = false;

        // ============================================================
        // CORRECCIÓN (vertical slice): el combate arranca directo acá, ya no
        // depende de pisar una zona del piso (WaveTriggerZone quedó deprecado).
        // ============================================================

        // Gomez sale → oleadas empiezan → crossfade a "Peleas Genericas".
        // La música de exploración BYN sonó durante todo el diálogo; recién
        // acá sube la tensión musical (sin llegar al boss todavía).
        if (MusicManager.Instance != null) MusicManager.Instance.CrossfadeAPeleasGenericas();

        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        Debug.Log($"[GomezInteraction] StartExit: EnemySpawner {(spawner != null ? "encontrado → arrancando oleadas" : "NO ENCONTRADO")}");
        if (spawner != null) spawner.StartWaves();
        else Debug.LogWarning("GomezInteraction: no se encontró ningún EnemySpawner en la escena para arrancar las oleadas.");

        StartCoroutine(MoveOut());
    }

    IEnumerator MoveOut()
    {
        while (true)
        {
            // Pedido del usuario: que evite las estructuras (casas/árboles/
            // arbustos) al irse, en vez de atravesarlas en línea recta.
            Vector2 direccion = AplicarEvasionDeEstructuras(Vector2.up);
            transform.position += (Vector3)(direccion * exitSpeed * Time.deltaTime);
            if (transform.position.y > Camera.main.transform.position.y + 15f)
            {
                Destroy(gameObject);
                yield break;
            }
            yield return null;
        }
    }

    // Mismo esquema que EnemyAI.AplicarEvasionDeEstructuras: busca obstáculos
    // sólidos cercanos (ObstacleUtils descarta jugador/enemigos/NPCs, así que
    // solo detecta casas/árboles/arbustos y similares) y suma un empuje hacia
    // afuera de ellos a la dirección deseada, para esquivarlos sin tener que
    // depender del collider sólido (que sigue apagado durante la salida para
    // no bloquear físicamente al jugador).
    Vector2 AplicarEvasionDeEstructuras(Vector2 deseado)
    {
        int n = Physics2D.OverlapCircle(transform.position, distanciaMinimaEstructurasSalida, filtroEstructurasSalida, bufferEstructurasSalida);
        if (n <= 0) return deseado;

        Vector2 empuje = Vector2.zero;
        for (int i = 0; i < n; i++)
        {
            Collider2D col = bufferEstructurasSalida[i];
            if (!ObstacleUtils.EsObstaculoSolido(col)) continue;

            Vector2 puntoCercano = col.ClosestPoint(transform.position);
            Vector2 fuera = (Vector2)transform.position - puntoCercano;
            float dist = fuera.magnitude;
            if (dist < 0.0001f) continue;

            float fuerza = 1f - Mathf.Clamp01(dist / distanciaMinimaEstructurasSalida);
            empuje += fuera.normalized * fuerza;
        }

        if (empuje.sqrMagnitude < 0.0001f) return deseado;

        Vector2 resultado = deseado + empuje;
        return resultado.sqrMagnitude > 0.0001f ? resultado.normalized : deseado;
    }

    bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (exiting) return;
        if (!IsPlayer(other)) return;
        playerInRange = true;
        if (outlineObject != null) outlineObject.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        playerInRange = false;
        if (outlineObject != null) outlineObject.SetActive(false);
        if (dialogOpen)
        {
            dialogOpen = false;
            if (dialogManager != null) dialogManager.CloseDialog();
        }
    }
}
