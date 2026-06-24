using System.Collections;
using UnityEngine;

// ============================================================
// SCRIPT: MusicManager
// Pedido del usuario: música de exploración con fade-in al arrancar el
// juego; al iniciar la interacción con Gomez, crossfade a "Final Boss -
// Paint It Black" (loop durante toda la oleada + minijuego); al ganar la
// oleada, crossfade de vuelta a la música de exploración.
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

    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenMusica = 0.6f;

    [Header("Tiempos de transición")]
    public float duracionFadeInInicial = 2.5f;
    public float duracionCrossfade = 1.5f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private bool activaEsA = true;

    private AudioClip clipExploracion;
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

        clipExploracion = Resources.Load<AudioClip>("Audio/Music/Exploracion");
        clipBoss = Resources.Load<AudioClip>("Audio/Music/FinalBoss");

        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        foreach (AudioSource s in new[] { sourceA, sourceB })
        {
            s.loop = true;
            s.playOnAwake = false;
            s.volume = 0f;
        }
    }

    void Start()
    {
        if (clipExploracion == null)
        {
            Debug.LogWarning("MusicManager: no se encontró el clip Assets/Resources/Audio/Music/Exploracion.mp3");
            return;
        }

        AudioSource activa = activaEsA ? sourceA : sourceB;
        activa.clip = clipExploracion;
        activa.volume = 0f;
        activa.Play();
        clipObjetivoActual = clipExploracion;

        rutinaActual = StartCoroutine(FadeVolumen(activa, 0f, volumenMusica, duracionFadeInInicial));
    }

    // Llamado desde GomezInteraction al iniciarse el diálogo (inicio de la interacción).
    public void CrossfadeABoss()
    {
        Crossfade(clipBoss);
    }

    // Llamado desde EnemySpawner.CheckVictory() al ganar la oleada.
    public void CrossfadeAExploracion()
    {
        Crossfade(clipExploracion);
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
