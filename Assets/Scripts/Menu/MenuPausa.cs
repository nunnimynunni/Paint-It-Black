using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour
{
    public GameObject panelPausa;
    private bool pausado = false;

    // Guardamos si el bullseye de oleada estaba activo para restaurarlo al reanudar
    private bool bullseyeEstabaActivo = false;

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
        // Guardar si el bullseye de arma estaba activo para restaurarlo al salir
        bullseyeEstabaActivo = WeaponCursor.Instance != null && WeaponCursor.Instance.OleadaActiva;

        panelPausa.SetActive(true);

        // Ocultar el HUD de combate/exploración mientras el menú está abierto
        GameManager.SetHudVisible(false);

        // Mostrar el cursor del sistema para poder hacer clic en los botones del menú
        Cursor.visible = true;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        Time.timeScale = 0f;
        pausado = true;
    }

    public void Reanudar()
    {
        panelPausa.SetActive(false);

        // Restaurar el HUD de combate/exploración
        GameManager.SetHudVisible(true);

        // Si la oleada estaba activa, restaurar también el bullseye en vez del cursor del sistema
        if (bullseyeEstabaActivo && WeaponCursor.Instance != null)
            WeaponCursor.Instance.ActivarModoOleada();

        Time.timeScale = 1f;
        pausado = false;
    }

    public void SalirAlMenu(int indiceMenu)
    {
        Time.timeScale = 1f; // importante: descongela antes de cambiar de escena
        SceneManager.LoadScene(indiceMenu);
    }
}