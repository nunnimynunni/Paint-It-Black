using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour
{
    public GameObject panelPausa;
    private bool pausado = false;

    void Update()
    {
        // Abrir/cerrar la pausa con Escape
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (pausado) Reanudar();
            else Pausar();
        }
    }

    public void Pausar()
    {
        panelPausa.SetActive(true);
        Time.timeScale = 0f; // congela el juego
        pausado = true;
    }

    public void Reanudar()
    {
        panelPausa.SetActive(false);
        Time.timeScale = 1f; // vuelve a correr
        pausado = false;
    }

    public void SalirAlMenu(int indiceMenu)
    {
        Time.timeScale = 1f; // importante: descongela antes de cambiar de escena
        SceneManager.LoadScene(indiceMenu);
    }
}