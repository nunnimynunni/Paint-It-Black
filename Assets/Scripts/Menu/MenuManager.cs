using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
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
