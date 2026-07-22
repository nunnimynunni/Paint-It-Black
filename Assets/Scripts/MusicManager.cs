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

    // Pedido del usuario: "toda debe estar un poco más bajo de volumen".
    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenMusica = 0.28f;
    [Tooltip("Volumen de la música mientras llueve (la lluvia pasa al frente)")]
    [Range(0f, 1f)] public float volumenMusicaEnLluvia = 0.07f;

    [Header("Tiempos de transición")]
    public float duracionFadeInInicial = 2.5f;
    public float duracionCrossfade = 1.5f;

    // Pedido del usuario: "Peleas genéricas" ligeramente ralentizada para
    // que no sea tan intensa. Unity no tiene control de tempo independiente
    // del pitch, así que se baja el pitch mínimamente (0.93 ≈ un semitono
    // más grave, apenas perceptible pero reduce la energía del track).
    [Header("Pitch de pelea")]
    [Range(0.7f, 1f)] public float pitchPeleasGenericas = 0.93f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private bool activaEsA = true;

    private AudioClip clipIntro;
    private AudioClip clipExploracionByN;
    private AudioClip clipExploracionColor;
    private AudioClip clipBoss;
    private AudioClip clipPeleasGenericas;

    private Coroutine rutinaActual;
    private Coroutine duckRoutine;

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

        clipIntro            = Resources.Load<AudioClip>("Audio/Music/Intro");
        clipExploracionByN   = Resources.Load<AudioClip>("Audio/Music/ExploracionByN");
        clipExploracionColor = Resources.Load<AudioClip>("Audio/Music/ExploracionColor");
        clipBoss             = Resources.Load<AudioClip>("Audio/Music/FinalBoss");
        clipPeleasGenericas  = Resources.Load<AudioClip>("Audio/Music/PeleasGenericas");

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

    // ============================================================
    // API PÚBLICA — llamados desde el resto del juego
    // ============================================================

    // Llamado desde GomezInteraction.StartExit() cuando Gomez sale y
    // arrancan las oleadas. Suena "Peleas genéricas" ligeramente más lenta.
    public void CrossfadeAPeleasGenericas()
    {
        Crossfade(clipPeleasGenericas);
    }

    // Llamado desde UpgradeSystem.OtorgarMejoraAleatoria() cuando el
    // jugador gana el puzzle y queda potenciado (verde). Sube la tensión
    // con "Final Boss" mientras dure el potenciador.
    public void CrossfadeABoss()
    {
        Crossfade(clipBoss);
    }

    // Llamado desde PlayerHealth.OcultarBuffOutlineLuegoDe() cuando el
    // potenciador expira. Vuelve a Peleas Genericas, pero solo si todavía
    // estamos en la pista del boss (si la victoria llegó antes, ya habrá
    // hecho crossfade a Exploración Color y no queremos pisarlo).
    public void TerminarPotenciador()
    {
        if (clipObjetivoActual == clipBoss)
            Crossfade(clipPeleasGenericas);
    }

    // Llamado desde EnemySpawner.CheckVictory() al ganar la oleada.
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

        entrante.clip  = nuevoClip;
        entrante.pitch = nuevoClip == clipPeleasGenericas ? pitchPeleasGenericas : 1f;
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

    // ============================================================
    // DUCKING DE LLUVIA — llamado por RainManager
    // ============================================================

    /// <summary>Baja la música gradualmente para que la lluvia pase al frente.</summary>
    public void DuckMusica(float duracion = 2f)
    {
        if (duckRoutine != null) StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(CoroutineDuck(volumenMusicaEnLluvia, duracion));
    }

    /// <summary>Restaura la música a su volumen normal cuando termina la lluvia.</summary>
    public void RestaurarMusica(float duracion = 2f)
    {
        if (duckRoutine != null) StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(CoroutineDuck(volumenMusica, duracion));
    }

    IEnumerator CoroutineDuck(float hasta, float duracion)
    {
        float desdeA = sourceA.volume;
        float desdeB = sourceB.volume;
        // Solo afectar las fuentes que están sonando
        float hastaA = sourceA.isPlaying ? hasta : 0f;
        float hastaB = sourceB.isPlaying ? hasta : 0f;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float pct = Mathf.SmoothStep(0f, 1f, t / duracion);
            sourceA.volume = Mathf.Lerp(desdeA, hastaA, pct);
            sourceB.volume = Mathf.Lerp(desdeB, hastaB, pct);
            yield return null;
        }
        sourceA.volume = hastaA;
        sourceB.volume = hastaB;
        duckRoutine = null;
    }

    void Update()
    {
        // Garantiza loop instantáneo: si la fuente activa dejó de sonar
        // (los .mp3 pueden tener silencio al final que produce un corte
        // breve aunque loop = true esté activo), se reinicia desde el
        // principio en el mismo frame sin esperar al sistema de Unity.
        AudioSource activa = activaEsA ? sourceA : sourceB;
        if (activa.clip != null && activa.clip == clipObjetivoActual
            && !activa.isPlaying && activa.volume > 0.01f)
        {
            activa.timeSamples = 0;
            activa.Play();
        }
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
