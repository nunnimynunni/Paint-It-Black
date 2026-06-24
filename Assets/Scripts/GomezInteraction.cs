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

    void Start()
    {
        if (outlineObject != null)
            outlineObject.SetActive(false);

        if (hudCombate != null)
            hudCombate.SetActive(false);

        animator = GetComponent<Animator>();

        AsegurarColliderSolido();
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
            }
            else
            {
                bool stillOpen = dialogManager != null && dialogManager.Advance();
                if (!stillOpen)
                {
                    dialogOpen = false;
                    if (hudCombate != null) hudCombate.SetActive(true);
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
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.StartWaves();
        else Debug.LogWarning("GomezInteraction: no se encontró ningún EnemySpawner en la escena para arrancar las oleadas.");

        StartCoroutine(MoveOut());
    }

    IEnumerator MoveOut()
    {
        while (true)
        {
            transform.position += Vector3.up * exitSpeed * Time.deltaTime;
            if (transform.position.y > Camera.main.transform.position.y + 15f)
            {
                Destroy(gameObject);
                yield break;
            }
            yield return null;
        }
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
