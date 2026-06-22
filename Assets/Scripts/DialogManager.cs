using System.Collections;
using UnityEngine;
using TMPro;

public class DialogManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialogPanel;
    public TextMeshProUGUI dialogText;

    [Header("Contenido")]
    [TextArea(3, 6)]
    public string[] dialogLines = {
        "¡Hola! Soy Gomez. Bienvenido a este lugar.",
        "Nunca vas a llegar al presidente. Nadie te va a recordar."
    };

    [Header("Typewriter")]
    public float charDelay = 0.04f;

    private int currentLine = 0;
    private bool isTyping = false;
    private Coroutine typewriterCoroutine;

    void Start()
    {
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
    }

    // Devuelve true si todavía hay líneas, false si se acabaron
    public bool Advance()
    {
        if (isTyping)
        {
            // Si está escribiendo, mostrar el texto completo al instante
            StopCoroutine(typewriterCoroutine);
            dialogText.text = dialogLines[currentLine];
            isTyping = false;
            return true;
        }

        currentLine++;

        if (currentLine >= dialogLines.Length)
        {
            // Se acabaron las líneas
            currentLine = 0;
            dialogPanel.SetActive(false);
            return false;
        }

        // Mostrar siguiente línea
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypeText(dialogLines[currentLine]));
        return true;
    }

    public void OpenDialog()
    {
        if (dialogPanel == null) return;
        currentLine = 0;
        dialogPanel.SetActive(true);
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypeText(dialogLines[currentLine]));
    }

    public void CloseDialog()
    {
        if (dialogPanel == null) return;
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        currentLine = 0;
        isTyping = false;
        dialogPanel.SetActive(false);
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogText.text = "";
        foreach (char c in text)
        {
            dialogText.text += c;
            yield return new WaitForSeconds(charDelay);
        }
        isTyping = false;
    }
}
