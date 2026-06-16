using UnityEngine;
using TMPro;

// DialogManager.cs
// Adjuntar este script a un GameObject vacío en la escena (ej: "DialogManager").
//
// Requiere en el Canvas UI:
//   - Un Panel/GameObject "DialogPanel" con el sprite del cuadro de diálogo
//   - Un TextMeshPro - Text (UI) para el texto del diálogo
//   - (Opcional) Un Image para el portrait de Gomez con un Mask encima

public class DialogManager : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("El panel raíz del diálogo. Se activa/desactiva al abrir/cerrar.")]
    public GameObject dialogPanel;

    [Tooltip("El componente de texto donde aparece el mensaje.")]
    public TextMeshProUGUI dialogText;

    [Header("Contenido")]
    [TextArea(3, 6)]
    [Tooltip("El texto que aparece en el diálogo de Gomez.")]
    public string dialogContent = "¡Hola! Soy Gomez. Bienvenido a este lugar.";

    void Start()
    {
        // El diálogo empieza cerrado
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
    }

    public void OpenDialog()
    {
        if (dialogPanel == null) return;

        dialogText.text = dialogContent;
        dialogPanel.SetActive(true);
    }

    public void CloseDialog()
    {
        if (dialogPanel == null) return;

        dialogPanel.SetActive(false);
    }
}
