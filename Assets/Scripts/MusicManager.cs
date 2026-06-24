using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
// SCRIPT: MusicManager
// Pedido del usuario: respetar el flujo completo de las instrumentales
// según la escena/momento:
//   - "Intro" en el menú (1Titulo, 2Menu y las escenas de Opciones).
//   - Crossfade a "Exploración mundo BYN" apenas aparece el juego
//     (se carga SampleScene).
//   - Crossfade a "Final Boss" al hablar con Gomez (CrossfadeABoss,
//     llamado desde GomezInteraction).
//   - Crossfade a "Exploración color" al ganar la oleada
//     (CrossfadeAExploracion, llamado desde EnemySpawner.CheckVictory()).
//
// Como el objeto es DontDestroyOnLoad (persiste entre escenas), la
// transición menú→juego y juego→menú (al perder, ver GameManager.GameOver)
// se manejan solas escuchando SceneManager.sceneLoaded: cualquier escena
// que NO sea "SampleScene" reproduce el Intro; "SampleScene" reproduce la
// Exploración BYN. Así no hace falta tocar cada escena de menú a mano.
//
// Los clips se cargan desde Assets/Resources/Audio/Music (Resources.Load
// por path, no por GUID), así no depende de que el archivo ya esté
// importado/wireado a mano en el Editor: apenas el .mp3 esté en esa
// carpeta, este script lo encuentra solo.
//
// Mismo criterio de auto-instanciación que WeaponCursor: no hay nada para
// arrastrar/crear a mano en el Editor.
// ============================================================
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    // Nombre exacto de la escena de juego (ver ProjectSettings/EditorBuildSettings).
    // Cualquier otra escena (1Titulo, 2Menu, Opciones, Display, Sonido,
    // Controles, Idioma, etc.) se trata como "menú" y suena el Intro.
    const string ESCENA_JUEGO = "SampleScene";

    // Pedido del usuario: "que todos los sonidos y las musicas convivan
    // bien". Se bajó un poco la música base (de 0.6 a 0.45) para que quede
    // de fondo sin tapar los SFX puntuales (aerosol, pincelazo, impacto),
    // que ahora se manejan con volúmenes propios en SfxManager.
    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenMusica = 0.45f;

    [Header("Tiempos de transición")]
    public float duracionFadeInInicial = 2.5f;
    public float duracionCrossfade = 1.5f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private bool activaEsA = true;

    private AudioClip clipIntro;
    private AudioClip clipExploracionByN;
    private AudioClip clipExploracionColor;
    private AudioClip clipBoss;

    private Coroutine rutinaActual;

    // Para no reiniciar un crossfade hacia la pista que ya está sonando
    // (ej: si el jugador reabre el diálogo de Gomez sin haber salido del
    // combate, o CheckVictory se revisa varias veces tras la victoria).
    private AudioClip clipObjetivoActual;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("MusicManager_Runtime");
        go.AddComponent<MusicManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        clipIntro = Resources.Load<AudioClip>("Audio/Music/Intro");
        clipExploracionByN = Resources.Load<AudioClip>("Audio/Music/ExploracionByN");
        clipExploracionColor = Resources.Load<AudioClip>("Audio/Music/ExploracionColor");
        clipBoss = Resources.Load<AudioClip>("Audio/Music/FinalBoss");

        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        foreach (AudioSource s in new[] { sourceA, sourceB })
        {
            s.loop = true;
            s.playOnAwake = false;
            s.volume = 0f;
        }

        // Escucha cada cambio de escena para decidir solo si toca Intro
        // (menú) o Exploración BYN (SampleScene), sin tener que cablear
        // nada a mano en cada escena de menú.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        AudioClip inicial = ClipParaEscena(SceneManager.GetActiveScene().name);
        if (inicial == null)
        {
            Debug.LogWarning("MusicManager: no se encontró el clip inicial para la escena '" + SceneManager.GetActiveScene().name + "'.");
            return;
        }

        AudioSource activa = activaEsA ? sourceA : sourceB;
        activa.clip = inicial;
        activa.volume = 0f;
        activa.Play();
        clipObjetivoActual = inicial;

        rutinaActual = StartCoroutine(FadeVolumen(activa, 0f, volumenMusica, duracionFadeInInicial));
    }

    // Pedido del usuario: Intro en el menú, Exploración BYN apenas aparece
    // el juego. SampleScene = la única escena de juego; todo lo demás
    // (Título, Menú, Opciones, Pausa, etc.) cuenta como "menú".
    AudioClip ClipParaEscena(string nombreEscena)
    {
        return nombreEscena == ESCENA_JUEGO ? clipExploracionByN : clipIntro;
    }

    void OnSceneLoaded(Scene escena, LoadSceneMode modo)
    {
        Crossfade(ClipParaEscena(escena.name));
    }

    // Llamado desde GomezInteraction al iniciarse el diálogo (inicio de la interacción).
    public void CrossfadeABoss()
    {
        Crossfade(clipBoss);
    }

    // Llamado desde EnemySpawner.CheckVictory() al ganar la oleada: el pueblo
    // ya recuperó color, así que vuelve a la exploración en color (no a la BYN).
    public void CrossfadeAExploracion()
    {
        Crossfade(clipExploracionColor);
    }

    void Crossfade(AudioClip nuevoClip)
    {
        if (nuevoClip == null || nuevoClip == clipObjetivoActual) return;
        clipObjetivoActual = nuevoClip;

        if (rutinaActual != null) StopCoroutine(rutinaActual);

        AudioSource saliente = activaEsA ? sourceA : sourceB;
        AudioSource entrante = activaEsA ? sourceB : sourceA;
        activaEsA = !activaEsA;

        entrante.clip = nuevoClip;
        entrante.volume = 0f;
        entrante.Play();

        rutinaActual = StartCoroutine(RutinaCrossfade(saliente, entrante));
    }

    IEnumerator RutinaCrossfade(AudioSource saliente, AudioSource entrante)
    {
        float t = 0f;
        float volInicialSaliente = saliente.volume;
        while (t < duracionCrossfade)
        {
            t += Time.deltaTime;
            float p = t / duracionCrossfade;
            saliente.volume = Mathf.Lerp(volInicialSaliente, 0f, p);
            entrante.volume = Mathf.Lerp(0f, volumenMusica, p);
            yield return null;
        }
        saliente.volume = 0f;
        saliente.Stop();
        entrante.volume = volumenMusica;
    }

    IEnumerator FadeVolumen(AudioSource source, float desde, float hasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        source.volume = hasta;
    }
}
