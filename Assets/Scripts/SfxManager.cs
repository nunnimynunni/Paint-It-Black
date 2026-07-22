using UnityEngine;

// ============================================================
// SCRIPT: SfxManager
// Centraliza los efectos de sonido pedidos por el usuario:
//   - Aerosol (arma Spray): usa el clip "Pulverizador" (pedido del usuario:
//     reemplaza al clip "Aerosol" original).
//   - Pincelazo: cada disparo del arma Pincel, y cada celda pintada en el
//     minijuego (en teoría usa un pincel ahí también).
//   - Rodillo (arma Melee): usa el clip "Baldazo" (pedido del usuario).
//   - Pisadas: loop mientras el Forastero camina, se detiene al frenar.
//   - Impacto a enemigo: cada vez que un ataque golpea a un NPC.
//
// Mismo criterio que MusicManager: clips cargados por path desde
// Assets/Resources/Audio/SFX (no por GUID), así no depende de wireado a
// mano en el Editor. Mismo patrón de auto-instanciación que WeaponCursor.
// ============================================================
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance;

    // Pedido del usuario: "que todos los sonidos y las musicas convivan
    // bien, ajustá los volúmenes". Antes había un solo volumenSfx para
    // los 3 one-shot (aerosol/pincelazo/impacto) y se solapaban mal entre
    // ellos y con la música. Ahora cada uno tiene su propio volumen:
    // - Pisadas: el más bajo, porque es un loop CONSTANTE mientras camina,
    //   así que aunque sea bajo siempre está presente (si fuera fuerte,
    //   cansa y tapa todo lo demás).
    // - Aerosol/Pincelazo: se disparan muy seguido (mantener click), un
    //   volumen medio para que no se vuelvan molestos a la larga.
    // - Impacto: el más fuerte de los SFX, da el "feedback" de golpe que
    //   tiene que sentirse por encima de la música y del resto.
    [Header("Volumen (ajustado para convivir con la música)")]
    [Range(0f, 1f)] public float volumenAerosol = 0.55f;
    [Range(0f, 1f)] public float volumenPincelazo = 0.55f;
    [Range(0f, 1f)] public float volumenRodillo = 0.6f;
    [Range(0f, 1f)] public float volumenImpacto = 0.7f;
    [Range(0f, 1f)] public float volumenPisadas = 0.3f;

    private AudioSource sourceOneShot; // golpes puntuales (PlayOneShot permite solaparse)
    private AudioSource sourcePisadas; // loop dedicado, on/off según si el jugador se mueve

    // Pedido del usuario: el sonido del Aerosol ahora usa el clip
    // "Pulverizador" (en vez del clip "Aerosol" original).
    private AudioClip clipAerosol;
    private AudioClip clipPincelazo;
    // Pedido del usuario: el Rodillo (arma Melee) ahora tiene sonido propio,
    // usando el clip "Baldazo".
    private AudioClip clipRodillo;
    private AudioClip clipPisadas;
    private AudioClip clipImpacto;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("SfxManager_Runtime");
        go.AddComponent<SfxManager>();
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

        clipAerosol = Resources.Load<AudioClip>("Audio/SFX/Pulverizador");
        clipPincelazo = Resources.Load<AudioClip>("Audio/SFX/Pincelazo");
        clipRodillo = Resources.Load<AudioClip>("Audio/SFX/Baldazo");
        clipPisadas = Resources.Load<AudioClip>("Audio/SFX/Pisadas");
        clipImpacto = Resources.Load<AudioClip>("Audio/SFX/ImpactoEnemigo");

        sourceOneShot = gameObject.AddComponent<AudioSource>();
        sourceOneShot.playOnAwake = false;

        sourcePisadas = gameObject.AddComponent<AudioSource>();
        sourcePisadas.playOnAwake = false;
        sourcePisadas.loop = true;
        sourcePisadas.clip = clipPisadas;
        sourcePisadas.volume = volumenPisadas;
    }

    public void PlayAerosol()
    {
        if (clipAerosol != null) sourceOneShot.PlayOneShot(clipAerosol, volumenAerosol);
    }

    public void PlayPincelazo()
    {
        if (clipPincelazo != null) sourceOneShot.PlayOneShot(clipPincelazo, volumenPincelazo);
    }

    public void PlayImpacto()
    {
        if (clipImpacto != null) sourceOneShot.PlayOneShot(clipImpacto, volumenImpacto);
    }

    // Pedido del usuario: sonido "Baldazo" para el Rodillo (arma Melee).
    public void PlayRodillo()
    {
        if (clipRodillo != null) sourceOneShot.PlayOneShot(clipRodillo, volumenRodillo);
    }

    public void IniciarPisadas()
    {
        if (sourcePisadas != null && clipPisadas != null && !sourcePisadas.isPlaying)
            sourcePisadas.Play();
    }

    public void DetenerPisadas()
    {
        if (sourcePisadas != null && sourcePisadas.isPlaying)
            sourcePisadas.Stop();
    }
}
