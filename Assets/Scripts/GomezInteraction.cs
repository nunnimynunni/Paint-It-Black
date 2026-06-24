using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================
// CONTEXTO PARA DESARROLLADORES
// ============================================================
// Este script maneja la interacción con Gomez (NPC inicial).
// El flujo del juego es:
//
// 1. EXPLORACIÓN
//    - El jugador camina por la escena con el HUD de exploración visible
//    - Al acercarse a Gomez aparece un outline blanco alrededor de él
//    - Al presionar E se abre el diálogo
//
// 2. DIÁLOGO
//    - El HUD de exploración se oculta
//    - El jugador avanza el diálogo con E
//    - Si el texto está escribiéndose, E lo completa al instante
//    - Al terminar el último texto, Gomez camina hacia arriba y desaparece
//
// 3. INICIO DEL COMBATE
//    - Al terminar el diálogo, hudCombate se activa (ver campo hudCombate)
//    - AQUÍ ES DONDE DEBE ARRANCAR EL SISTEMA DE OLEADAS
//    - Para enganchar el inicio del combate, buscar el método StartExit()
//      y agregar la llamada al sistema de oleadas ahí:
//      Ejemplo: EnemySpawner.instance.StartWaves();
//
// 4. FIN DEL COMBATE
//    - Cuando terminen todas las oleadas, llamar:
//      CombatEndTrigger.instance.OnCombatEnd()
//    - Esto activa a Cromagustin y lo hace entrar a la escena
// ============================================================

public class GomezInteraction : MonoBehaviour
{
    [Header("Outline")]
    public GameObject outlineObject;

    [Header("Diálogo")]
    public DialogManager dialogManager;

    [Header("HUD")]
    // hudExploracion: se oculta al abrir el diálogo y no vuelve más
    public GameObject hudExploracion;
    // hudCombate: se activa al terminar el diálogo (gotas de munición, barra de vida, arma)
    public GameObject hudCombate;

    [Header("Salida")]
    [Tooltip("Si está tildado, el NPC camina hacia arriba y desaparece al terminar el diálogo. Destildar para NPCs que se quedan en la escena.")]
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

        // hudCombate empieza oculto, se activa al terminar el diálogo
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
        // Agregar aquí la llamada para arrancar el combate. Ejemplo:
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
