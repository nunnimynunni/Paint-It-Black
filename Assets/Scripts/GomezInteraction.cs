using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================
// SCRIPT: GomezInteraction
// USADO EN: Gomez (NPC inicial) y Cromagustin (NPC final)
// ESTE SCRIPT ES REUTILIZABLE PARA CUALQUIER NPC INTERACTUABLE
// ============================================================
//
// FLUJO DEL JUEGO:
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
//    - AQUÍ DEBE ARRANCAR EL SISTEMA DE OLEADAS:
//      Buscar StartExit() y agregar: EnemySpawner.instance.StartWaves();
//
// 3b. SI exitAfterDialog = false (ej: Cromagustin)
//    - El NPC se queda en la escena
//    - No activa ningún HUD
//
// 4. FIN DEL COMBATE
//    - El sistema de oleadas debe llamar:
//      CombatEndTrigger.instance.OnCombatEnd()
//    - Esto activa a Cromagustin y lo hace entrar desde la izquierda
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

    private bool playerInRange = false;
    private bool dialogOpen = false;
    private bool exiting = false;
    private Animator animator;

    void Start()
    {
        if (outlineObject != null)
            outlineObject.SetActive(false);

        if (hudCombate != null)
            hudCombate.SetActive(false);

        animator = GetComponent<Animator>();
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
                }
            }
        }
    }

    void StartExit()
    {
        exiting = true;
        if (outlineObject != null) outlineObject.SetActive(false);
        if (animator != null) animator.SetTrigger(walkNorthTrigger);

        // ============================================================
        // PUNTO DE ENGANCHE PARA EL SISTEMA DE OLEADAS
        // Cuando el sistema de oleadas esté implementado, agregar aquí:
        // EnemySpawner.instance.StartWaves();
        // ============================================================

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
