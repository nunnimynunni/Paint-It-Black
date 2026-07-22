using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    void Start()
    {
        // WeaponCursor.Start() setea Cursor.visible = false en la escena de juego
        // y ese estado persiste al volver al menú. Se restaura acá para que el
        // jugador pueda navegar el menú con su cursor normal.
        Cursor.visible = true;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void CargarEscena(int indice)
    {
        SceneManager.LoadScene(indice);
    }

    public void AbrirPanel(GameObject panel)
    {
        panel.SetActive(true);
    }

    public void CerrarPanel(GameObject panel)
    {
        panel.SetActive(false);
    }

    public void Salir()
    {
        Application.Quit();
        Debug.Log("Salir"); // para verlo en el editor
    }
}
