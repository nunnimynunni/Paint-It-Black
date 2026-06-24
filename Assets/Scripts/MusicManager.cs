using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioSource audioSource;

    // Nombres de las escenas del menú donde esta música debe seguir sonando
    [SerializeField] private string[] escenasDeMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si la escena que cargó NO es del menú, corto la música y me destruyo
        if (!EsEscenaDeMenu(scene.name))
        {
            audioSource.Stop();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Destroy(gameObject);
        }
    }

    private bool EsEscenaDeMenu(string nombre)
    {
        foreach (string s in escenasDeMenu)
            if (s == nombre) return true;
        return false;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}