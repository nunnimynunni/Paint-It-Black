using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// ============================================================
// SCRIPT: RainManager
// Feature: Lluvia — ocurre exactamente 2 veces durante el combate.
//
// AUDIO: colocar clips en Assets/Resources/Audio/RainManager/
//   - lluvia.wav  (o .mp3/.ogg) — loop de lluvia
//   - trueno_0.wav, trueno_1.wav, trueno_2.wav, ... — variantes de trueno
//   Si no se encuentran, el sistema funciona igual pero sin sonido.
//
// Efectos:
//   - Pantalla muy oscura pero visible durante la lluvia.
//   - Relámpagos que iluminan la escena (sin cegar).
//   - Trueno 0.3–1.5s después de cada relámpago (como en la realidad).
//   - Gotas blancas procedurales densas en Canvas UI.
//   - Pantalla desaturada (B&W) + postExposure oscuro (URP Volume).
//   - Al quedar B&W: casas pintadas revierten a sprite original.
//   - Pintura de color no tiene efecto mientras llueve.
//   - Al terminar la lluvia: NPCs pintados pierden su estado de color.
// ============================================================
public class RainManager : MonoBehaviour
{
    public static RainManager Instance;

    [Header("Timing")]
    [Tooltip("Espera (seg) antes de la primera lluvia desde que empieza el combate")]
    public float esperaPrimeraLluvia = 40f;
    [Tooltip("Espera (seg) dentro de la oleada 3 antes de que empiece la segunda lluvia")]
    public float esperaSegundaLluvia = 10f;
    [Tooltip("Duración de la lluvia (seg)")]
    public float rainDuration = 15f;
    [Tooltip("Duración del fade a B&W/oscuro y de vuelta al color (seg)")]
    public float fadeDuration = 2f;

    [Header("Gotas")]
    [Tooltip("Cantidad de gotas de lluvia en pantalla")]
    public int rainDropCount = 500;

    [Header("Oscuridad")]
    [Tooltip("Opacidad máxima del overlay oscuro (0=nada, 1=negro total)")]
    public float oscuridadMaxima = 0.65f;
    [Tooltip("postExposure del URP Volume durante la lluvia (negativo = más oscuro)")]
    public float postExposureTarget = -2.2f;

    [Header("Relámpagos")]
    [Tooltip("Tiempo mínimo entre relámpagos (seg)")]
    public float relampagosMinInterval = 2.5f;
    [Tooltip("Tiempo máximo entre relámpagos (seg)")]
    public float relampagosMaxInterval = 6f;
    [Tooltip("postExposure pico durante el relámpago (positivo = escena más brillante)")]
    public float relampagoPeakExposure = 1.5f;

    [Header("Audio")]
    [Tooltip("Volumen del loop de lluvia (0-1)")]
    [Range(0f, 1f)] public float volumenLluvia = 1f;
    [Tooltip("Volumen de los truenos — puede superar 1 para sonar por encima de la lluvia")]
    [Range(0f, 2f)] public float volumenTrueno = 1.4f;
    [Tooltip("Duración del fade de entrada/salida del audio de lluvia (seg)")]
    public float audioFadeDuration = 1.5f;

    public bool IsRaining { get; private set; } = false;

    // ── URP Volume ──────────────────────────────────────────
    private Volume rainVolume;
    private ColorAdjustments colorAdj;

    // ── Canvas ──────────────────────────────────────────────
    private Canvas rainCanvas;
    private Image overlayOscuro;
    private readonly List<RainDrop> drops = new List<RainDrop>();

    // ── Audio ────────────────────────────────────────────────
    private AudioSource audioLluvia;
    private AudioSource audioTrueno;
    private AudioClip clipLluvia;
    private AudioClip[] clipsTrueno;

    // ── Referencia a spawner ─────────────────────────────────
    private EnemySpawner spawner;
    private Coroutine relampagosRoutine;
    private Coroutine audioFadeRoutine;

    private class RainDrop
    {
        public RectTransform rect;
        public float speed;
    }

    // ============================================================
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("RainManager_Runtime").AddComponent<RainManager>();
    }

    void Start()
    {
        spawner = FindFirstObjectByType<EnemySpawner>();
        CrearVolumenBW();
        CrearCanvasLluvia();
        CargarAudio();
        StartCoroutine(BucleRain());
    }

    // ============================================================
    // AUDIO — carga desde Resources/Audio/RainManager/
    // ============================================================
    void CargarAudio()
    {
        // Loop de lluvia
        clipLluvia = Resources.Load<AudioClip>("Audio/RainManager/lluvia");
        if (clipLluvia == null)
            Debug.LogWarning("RainManager: no se encontró 'Resources/Audio/RainManager/lluvia'. Sonido de lluvia desactivado.");

        // Truenos (puede haber varios: trueno_0, trueno_1, trueno_2, ...)
        clipsTrueno = Resources.LoadAll<AudioClip>("Audio/RainManager/truenos");
        if (clipsTrueno == null || clipsTrueno.Length == 0)
        {
            // Fallback: intentar con nombre singular
            AudioClip single = Resources.Load<AudioClip>("Audio/RainManager/trueno");
            clipsTrueno = single != null ? new[] { single } : new AudioClip[0];
            if (clipsTrueno.Length == 0)
                Debug.LogWarning("RainManager: no se encontraron clips de trueno en 'Resources/Audio/RainManager/truenos/'. Truenos desactivados.");
        }

        // AudioSource para lluvia (loop)
        audioLluvia = gameObject.AddComponent<AudioSource>();
        audioLluvia.loop        = true;
        audioLluvia.playOnAwake = false;
        audioLluvia.volume      = 0f;
        audioLluvia.spatialBlend = 0f; // 2D — ambiente global
        if (clipLluvia != null) audioLluvia.clip = clipLluvia;

        // AudioSource para truenos (one-shot)
        // Nota: volume en 1f para que PlayOneShot(clip, volumenTrueno) use
        // volumenTrueno directamente como escala, permitiendo valores > 1.
        audioTrueno = gameObject.AddComponent<AudioSource>();
        audioTrueno.loop        = false;
        audioTrueno.playOnAwake = false;
        audioTrueno.volume      = 1f;
        audioTrueno.spatialBlend = 0f;
    }

    AudioClip TruenoCualquiera()
    {
        if (clipsTrueno == null || clipsTrueno.Length == 0) return null;
        return clipsTrueno[Random.Range(0, clipsTrueno.Length)];
    }

    // ============================================================
    // BUCLE PRINCIPAL — exactamente 2 lluvias durante el combate
    // ============================================================
    IEnumerator BucleRain()
    {
        yield return new WaitUntil(() => spawner != null && spawner.Started);

        // ── Primera lluvia ─────────────────────────────────────────
        yield return new WaitForSeconds(esperaPrimeraLluvia);
        if (spawner == null || spawner.CombateTerminado) yield break;
        yield return StartCoroutine(SecuenciaLluvia());

        // ── Segunda lluvia: esperar al inicio de la oleada 3 (índice 2) ──
        // CurrentWave es 0-based: 0=oleada1, 1=oleada2, 2=oleada3.
        // Esperamos a que la oleada 3 arranque antes de empezar la lluvia.
        yield return new WaitUntil(() => spawner == null
                                     || spawner.CombateTerminado
                                     || spawner.CurrentWave >= 2);
        if (spawner == null || spawner.CombateTerminado) yield break;

        // Pequeña pausa dentro de la oleada 3 para que el jugador lo sienta
        yield return new WaitForSeconds(esperaSegundaLluvia);
        if (spawner == null || spawner.CombateTerminado) yield break;
        yield return StartCoroutine(SecuenciaLluvia());
    }

    IEnumerator SecuenciaLluvia()
    {
        yield return StartCoroutine(IniciarLluvia());
        relampagosRoutine = StartCoroutine(BucleRelampagos());
        yield return new WaitForSeconds(rainDuration);
        if (relampagosRoutine != null) { StopCoroutine(relampagosRoutine); relampagosRoutine = null; }
        yield return StartCoroutine(TerminarLluvia());
    }

    // ============================================================
    // INICIAR LLUVIA
    // ============================================================
    IEnumerator IniciarLluvia()
    {
        IsRaining = true;
        MostrarGotas(true);
        IniciarAudioLluvia();
        MusicManager.Instance?.DuckMusica(audioFadeDuration);

        if (colorAdj != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float pct = Mathf.SmoothStep(0f, 1f, t / fadeDuration);
                colorAdj.saturation.value   = Mathf.Lerp(0f,            -100f,              pct);
                colorAdj.postExposure.value = Mathf.Lerp(0f,            postExposureTarget, pct);
                if (overlayOscuro != null)
                    overlayOscuro.color = new Color(0f, 0f, 0.04f, Mathf.Lerp(0f, oscuridadMaxima, pct));
                yield return null;
            }
            colorAdj.saturation.value   = -100f;
            colorAdj.postExposure.value = postExposureTarget;
            if (overlayOscuro != null)
                overlayOscuro.color = new Color(0f, 0f, 0.04f, oscuridadMaxima);
        }

        RevertirCasasPintadas();
    }

    // ============================================================
    // TERMINAR LLUVIA
    // ============================================================
    IEnumerator TerminarLluvia()
    {
        DetenerAudioLluvia();
        MusicManager.Instance?.RestaurarMusica(audioFadeDuration);

        if (colorAdj != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float pct = Mathf.SmoothStep(0f, 1f, t / fadeDuration);
                colorAdj.saturation.value   = Mathf.Lerp(-100f,            0f, pct);
                colorAdj.postExposure.value = Mathf.Lerp(postExposureTarget, 0f, pct);
                if (overlayOscuro != null)
                    overlayOscuro.color = new Color(0f, 0f, 0.04f, Mathf.Lerp(oscuridadMaxima, 0f, pct));
                yield return null;
            }
            colorAdj.saturation.value   = 0f;
            colorAdj.postExposure.value = 0f;
            if (overlayOscuro != null)
                overlayOscuro.color = new Color(0f, 0f, 0.04f, 0f);
        }

        DesbloquearCasas();
        LimpiarPinturaEnNPCs();
        MostrarGotas(false);
        IsRaining = false;
    }

    // ============================================================
    // AUDIO DE LLUVIA — fade in / fade out
    // ============================================================
    void IniciarAudioLluvia()
    {
        if (audioLluvia == null || clipLluvia == null) return;
        if (audioFadeRoutine != null) StopCoroutine(audioFadeRoutine);
        audioLluvia.Play();
        audioFadeRoutine = StartCoroutine(FadeAudio(audioLluvia, 0f, volumenLluvia, audioFadeDuration));
    }

    void DetenerAudioLluvia()
    {
        if (audioLluvia == null) return;
        if (audioFadeRoutine != null) StopCoroutine(audioFadeRoutine);
        audioFadeRoutine = StartCoroutine(FadeAudioYDetener(audioLluvia, audioLluvia.volume, 0f, audioFadeDuration));
    }

    IEnumerator FadeAudio(AudioSource src, float desde, float hasta, float dur)
    {
        float t = 0f;
        src.volume = desde;
        while (t < dur)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(desde, hasta, t / dur);
            yield return null;
        }
        src.volume = hasta;
        audioFadeRoutine = null;
    }

    IEnumerator FadeAudioYDetener(AudioSource src, float desde, float hasta, float dur)
    {
        yield return StartCoroutine(FadeAudio(src, desde, hasta, dur));
        src.Stop();
    }

    // ============================================================
    // RELÁMPAGOS — corren en paralelo mientras llueve
    // ============================================================
    IEnumerator BucleRelampagos()
    {
        yield return new WaitForSeconds(Random.Range(0.8f, 1.5f));

        while (true)
        {
            yield return StartCoroutine(FlashRelampago());
            yield return new WaitForSeconds(Random.Range(relampagosMinInterval, relampagosMaxInterval));
        }
    }

    IEnumerator FlashRelampago()
    {
        // El trueno suena 0.3–1.5s después del destello (la luz viaja más rápido que el sonido)
        float demora = Random.Range(0.3f, 1.5f);
        StartCoroutine(TruenoDemorado(demora));

        // ─ Primer destello ────────────────────────────────────────
        yield return StartCoroutine(FadeRelampago(oscuridadMaxima, 0f,
                                                  postExposureTarget, relampagoPeakExposure,
                                                  0.04f));
        yield return new WaitForSeconds(Random.Range(0.06f, 0.10f));
        yield return StartCoroutine(FadeRelampago(0f, oscuridadMaxima,
                                                  relampagoPeakExposure, postExposureTarget,
                                                  0.05f));

        // ─ Pausa brevísima entre destellos ───────────────────────
        yield return new WaitForSeconds(Random.Range(0.04f, 0.09f));

        // ─ Segundo destello (más tenue) ──────────────────────────
        float peakExposure2 = relampagoPeakExposure * Random.Range(0.35f, 0.6f);
        float peakOscuro2   = oscuridadMaxima * Random.Range(0.35f, 0.55f);
        yield return StartCoroutine(FadeRelampago(oscuridadMaxima, peakOscuro2,
                                                  postExposureTarget, peakExposure2,
                                                  0.03f));
        yield return new WaitForSeconds(Random.Range(0.04f, 0.07f));
        yield return StartCoroutine(FadeRelampago(peakOscuro2, oscuridadMaxima,
                                                  peakExposure2, postExposureTarget,
                                                  0.06f));
    }

    IEnumerator TruenoDemorado(float demora)
    {
        yield return new WaitForSeconds(demora);
        AudioClip clip = TruenoCualquiera();
        if (clip != null && audioTrueno != null)
            audioTrueno.PlayOneShot(clip, volumenTrueno);
    }

    IEnumerator FadeRelampago(float oscuroDesde, float oscuroHasta,
                               float expDesde,   float expHasta,
                               float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float pct = t / duracion;
            if (overlayOscuro != null)
                overlayOscuro.color = new Color(0f, 0f, 0.04f, Mathf.Lerp(oscuroDesde, oscuroHasta, pct));
            if (colorAdj != null)
                colorAdj.postExposure.value = Mathf.Lerp(expDesde, expHasta, pct);
            yield return null;
        }
        if (overlayOscuro != null)
            overlayOscuro.color = new Color(0f, 0f, 0.04f, oscuroHasta);
        if (colorAdj != null)
            colorAdj.postExposure.value = expHasta;
    }

    // ============================================================
    // ACCIONES SOBRE EL MUNDO
    // ============================================================
    void RevertirCasasPintadas()
    {
        var puzzles = FindObjectsByType<PuzzleStructure>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ps in puzzles)
            if (ps.Completed)
                ps.RevertirPorLluvia();
    }

    void DesbloquearCasas()
    {
        var puzzles = FindObjectsByType<PuzzleStructure>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ps in puzzles)
            ps.DesbloquearPorFinLluvia();
    }

    void LimpiarPinturaEnNPCs()
    {
        var efectos = FindObjectsByType<EnemyStatusEffects>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var e in efectos)
            e.LimpiarPorLluvia();
    }

    // ============================================================
    // URP VOLUME — B&W + oscurecimiento
    // ============================================================
    void CrearVolumenBW()
    {
        if (Camera.main != null)
        {
            var camData = Camera.main.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null) camData.renderPostProcessing = true;
        }

        GameObject volObj = new GameObject("RainManager_BWVolume");
        rainVolume = volObj.AddComponent<Volume>();
        rainVolume.isGlobal = true;
        rainVolume.priority = 100f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        rainVolume.profile = profile;

        colorAdj = profile.Add<ColorAdjustments>(false);
        colorAdj.active = true;
        colorAdj.saturation.overrideState   = true;
        colorAdj.saturation.value           = 0f;
        colorAdj.postExposure.overrideState  = true;
        colorAdj.postExposure.value          = 0f;
    }

    // ============================================================
    // CANVAS — overlay oscuro + gotas blancas
    // ============================================================
    void CrearCanvasLluvia()
    {
        GameObject canvasObj = new GameObject("RainManager_Canvas");
        rainCanvas = canvasObj.AddComponent<Canvas>();
        rainCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        rainCanvas.sortingOrder = 500;
        canvasObj.AddComponent<CanvasScaler>();

        overlayOscuro = CrearOverlay(canvasObj.transform, "Overlay_Oscuro");
        overlayOscuro.color = new Color(0f, 0f, 0.04f, 0f);

        GameObject contenedor = new GameObject("Gotas", typeof(RectTransform));
        contenedor.transform.SetParent(canvasObj.transform, false);
        var contRect = contenedor.GetComponent<RectTransform>();
        contRect.anchorMin = Vector2.zero;
        contRect.anchorMax = Vector2.one;
        contRect.offsetMin = Vector2.zero;
        contRect.offsetMax = Vector2.zero;
        contenedor.AddComponent<RectMask2D>();

        float screenH = Screen.height;
        float screenW = Screen.width;

        for (int i = 0; i < rainDropCount; i++)
        {
            GameObject dropObj = new GameObject("Drop_" + i, typeof(RectTransform));
            dropObj.transform.SetParent(contenedor.transform, false);

            var img = dropObj.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, Random.Range(0.45f, 0.75f));

            float altura = Random.Range(8f, 18f);
            float ancho  = Random.Range(0.8f, 1.4f);

            var rect = dropObj.GetComponent<RectTransform>();
            rect.sizeDelta        = new Vector2(ancho, altura);
            rect.anchorMin        = Vector2.zero;
            rect.anchorMax        = Vector2.zero;
            rect.pivot            = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(
                Random.Range(0f, screenW),
                Random.Range(0f, screenH));

            drops.Add(new RainDrop
            {
                rect  = rect,
                speed = Random.Range(500f, 800f),
            });
        }

        canvasObj.SetActive(false);
    }

    Image CrearOverlay(Transform parent, string nombre)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        return obj.AddComponent<Image>();
    }

    void MostrarGotas(bool visible)
    {
        if (rainCanvas != null)
            rainCanvas.gameObject.SetActive(visible);
    }

    // ============================================================
    // ANIMACIÓN DE GOTAS
    // ============================================================
    void Update()
    {
        if (!IsRaining) return;

        float dt      = Time.deltaTime;
        float screenH = Screen.height;
        float screenW = Screen.width;

        foreach (var drop in drops)
        {
            if (drop.rect == null) continue;

            Vector2 pos = drop.rect.anchoredPosition;
            pos.y -= drop.speed * dt;

            if (pos.y < -10f)
            {
                pos.y = screenH + Random.Range(0f, 80f);
                pos.x = Random.Range(0f, screenW);
            }

            drop.rect.anchoredPosition = pos;
        }
    }
}
