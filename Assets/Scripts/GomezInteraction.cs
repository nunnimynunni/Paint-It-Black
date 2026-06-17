using UnityEngine;
using UnityEngine.InputSystem;

// GomezInteraction.cs
// Adjuntar este script al GameObject de Gomez.
//
// Requiere en el GameObject de Gomez:
//   - CircleCollider2D (Is Trigger = true) para detectar al jugador
//   - Un child GameObject "Outline" con SpriteRenderer blanco levemente más grande
//   - Una referencia al DialogManager de la escena

public class GomezInteraction : MonoBehaviour
{
    [Header("Outline")]
    [Tooltip("Child GameObject de Gomez con el sprite blanco (el outline). Asignar en Inspector.")]
    public GameObject outlineObject;

    [Header("Diálogo")]
    [Tooltip("El DialogManager de la escena. Asignar en Inspector.")]
    public DialogManager dialogManager;

    private bool playerInRange = false;
    private bool dialogOpen = false;

    void Update()
    {
        // Solo escuchar E si el jugador está cerca
        if (!playerInRange) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (!dialogOpen)
                OpenDialog();
            else
                CloseDialog();
        }
    }

    bool IsPlayer(Collider2D other)
    {
        // Busca el tag "Player" tanto en el collider como en su raíz
        // (por si el collider está en un hijo del jugador)
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        playerInRange = true;

        if (outlineObject != null)
        {
            outlineObject.SetActive(true);
            Debug.Log("Outline activado");
        }
        else
        {
            Debug.LogWarning("outlineObject es NULL — asignarlo en el Inspector de GomezInteraction");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        playerInRange = false;

        if (outlineObject != null)
            outlineObject.SetActive(false);

        // Si el jugador se alejó con el diálogo abierto, cerrarlo
        if (dialogOpen)
            CloseDialog();
    }

    void OpenDialog()
    {
        dialogOpen = true;
        if (dialogManager != null)
            dialogManager.OpenDialog();
    }

    void CloseDialog()
    {
        dialogOpen = false;
        if (dialogManager != null)
            dialogManager.CloseDialog();
    }
}
