using UnityEngine;

// Controlador central de partida: game over y victoria.
// Poner en un GameObject vacío llamado "GameManager" en cada escena de juego.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI opcional (arrastrar paneles si existen)")]
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    public bool IsGameOver { get; private set; } = false;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        Debug.Log("GAME OVER");
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void Victory()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        Debug.Log("VICTORIA - todos los enemigos eliminados");
        if (victoryPanel != null) victoryPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    // Útil para un botón de "Reintentar" en el panel de Game Over
    public void Restart()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
