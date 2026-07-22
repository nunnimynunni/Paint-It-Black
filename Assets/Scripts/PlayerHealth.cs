using System.Collections;
using UnityEngine;

// Vida del Forastero (jugador). Poner en el mismo GameObject que PlayerAnimator.
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    // VERTICAL SLICE: 100 HP fiel al GDD (antes 16, ajustado temporalmente
    // mientras el daño de las armas/enemigos eran placeholders de 1).
    // Ahora que esos daños ya están en sus valores reales del GDD, la
    // proporción de golpes-para-morir queda equivalente a la que ya
    // habíamos probado, solo escalada x6.25.
    public int maxHealth = 100;
    [Tooltip("Segundos que el sprite queda en rojo tras recibir un golpe")]
    public float hitFlashDuration = 1f;
    [Tooltip("Segundos de invulnerabilidad tras recibir un golpe (evita que varios enemigos lo maten en el mismo instante)")]
    public float invulnerabilityDuration = 0.6f;

    private int currentHealth;
    private SpriteRenderer sr;
    private Color colorOriginal;
    private float flashTimer = 0f;
    private float invulnTimer = 0f;
    // Feedback de playtest: vacante para la animación de golpeado del
    // Forastero (Animator "frottnguy_0", parámetro Trigger "OnHit", ver
    // estado placeholder "Golpeado"). Si no hay Animator en el GameObject
    // simplemente no se dispara nada (no rompe nada existente).
    private Animator animator;
    private PlayerAnimator playerAnimator;

    // ============================================================
    // GDD 3.7: "Mejoras de Vida y Defensa" otorgadas por el puzzle.
    // - escudo temporal: shieldTimer > 0 bloquea TODO el daño mientras dura.
    // - extraLives: al morir, si hay alguna disponible, revive con 50% de
    //   vida en vez de disparar Game Over.
    // - la reducción de daño permanente vive en UpgradeSystem
    //   (VidaReduccionDanoPercent) y se lee directo desde ahí.
    // ============================================================
    private float shieldTimer = 0f;
    private int extraLives = 0;

    // ============================================================
    // Outline VERDE de "beneficio activo" (pedido del usuario): mientras
    // dura el efecto de una mejora otorgada por el puzzle, el protagonista
    // muestra un borde verde, igual esquema que el outline amarillo de
    // PuzzleStructure (sprite hijo, mismo shader Custom/SpriteWhiteSolid,
    // un poco más grande, detrás en sorting order). Duración máxima 30s.
    // ============================================================
    private const float DURACION_MAXIMA_BUFF = 60f;
    private GameObject buffOutlineObj;
    private SpriteRenderer buffOutlineSr;
    private Coroutine buffOutlineRoutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        colorOriginal = sr.color;
        animator = GetComponent<Animator>();
        playerAnimator = GetComponent<PlayerAnimator>();
    }

    void LateUpdate()
    {
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;
        if (shieldTimer > 0f) shieldTimer -= Time.deltaTime;

        // Mientras el outline de beneficio esté visible, lo mantenemos
        // sincronizado con el sprite/flip actual del jugador (animación y
        // dirección cambian todo el tiempo).
        if (buffOutlineObj != null && buffOutlineObj.activeSelf && buffOutlineSr != null)
        {
            buffOutlineSr.sprite = sr.sprite;
            buffOutlineSr.flipX = sr.flipX;
            buffOutlineSr.sortingOrder = sr.sortingOrder - 1; // YSort cambia sortingOrder cada frame; el outline debe seguirlo
        }

        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;
        sr.color = flashTimer > 0f ? Color.red : colorOriginal;
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (invulnTimer > 0f) return; // todavía en ventana de invulnerabilidad del golpe anterior

        // Escudo temporal (mejora del puzzle): bloquea el golpe entero, no
        // gasta la invulnerabilidad normal del golpe.
        if (shieldTimer > 0f) return;

        // Reducción de daño permanente (mejora del puzzle).
        if (UpgradeSystem.Instance != null && UpgradeSystem.Instance.VidaReduccionDanoPercent > 0f)
            amount = Mathf.Max(1, Mathf.RoundToInt(amount * (1f - UpgradeSystem.Instance.VidaReduccionDanoPercent)));

        currentHealth -= amount;
        flashTimer = hitFlashDuration;
        invulnTimer = invulnerabilityDuration;
        sr.color = Color.red;
        if (animator != null) animator.SetTrigger("OnHit");
        playerAnimator?.TriggerFreezeOnHit();

        if (currentHealth <= 0)
        {
            if (extraLives > 0)
            {
                // Vida extra (mejora del puzzle): revive en vez de Game Over.
                extraLives--;
                currentHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * 0.5f));
                invulnTimer = Mathf.Max(invulnTimer, 1f); // un respiro extra al revivir
                return;
            }

            currentHealth = 0;
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }

    // --- Mejoras de Vida y Defensa (GDD 3.7), llamadas por UpgradeSystem ---

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void ActivarEscudoTemporal(float seconds)
    {
        shieldTimer = Mathf.Max(shieldTimer, seconds);
    }

    public void AddVidaExtra(int cantidad)
    {
        extraLives += cantidad;
    }

    // ============================================================
    // Outline verde de beneficio activo: lo llama UpgradeSystem cada vez
    // que otorga una mejora, indicando cuánto debe durar visible (la
    // duración real del beneficio si la tiene, p.ej. el escudo temporal;
    // si no, un tiempo corto a modo de "aviso" de que se obtuvo algo).
    // Nunca dura más de 30 segundos, sin importar lo que se le pida.
    // ============================================================
    public void MostrarBuffOutline(float duracionSegundos)
    {
        float duracion = Mathf.Clamp(duracionSegundos, 0.1f, DURACION_MAXIMA_BUFF);

        if (buffOutlineObj == null) CrearBuffOutline();
        buffOutlineObj.SetActive(true);

        if (buffOutlineRoutine != null) StopCoroutine(buffOutlineRoutine);
        buffOutlineRoutine = StartCoroutine(OcultarBuffOutlineLuegoDe(duracion));
    }

    void CrearBuffOutline()
    {
        buffOutlineObj = new GameObject("PlayerHealth_OutlineMulticolorBuff");
        buffOutlineObj.transform.SetParent(sr.transform, false);
        buffOutlineObj.transform.localPosition = Vector3.zero;
        buffOutlineObj.transform.localScale = new Vector3(1.06f, 1.06f, 1f);

        buffOutlineSr = buffOutlineObj.AddComponent<SpriteRenderer>();
        buffOutlineSr.sprite = sr.sprite;
        buffOutlineSr.sortingOrder = sr.sortingOrder - 1;
        // El color lo anima OcultarBuffOutlineLuegoDe frame a frame (arcoiris)

        Material mat = EncontrarMaterialOutlineExistente();
        if (mat != null) buffOutlineSr.material = mat;

        buffOutlineObj.SetActive(false);
    }

    // Mismo truco que usa PuzzleStructure: no se puede cargar el shader por
    // path sin carpeta Resources, así que se reutiliza el material que ya
    // esté usando cualquier otro outline (NPC o estructura) en la escena.
    Material EncontrarMaterialOutlineExistente()
    {
        SpriteRenderer[] todos = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var r in todos)
        {
            if (r == null || r.sharedMaterial == null || r.sharedMaterial.shader == null) continue;
            if (r.sharedMaterial.shader.name == "Custom/SpriteWhiteSolid")
                return r.sharedMaterial;
        }
        return null;
    }

    // Pedido del usuario: outline multicolor tipo estrella de Mario en vez de verde fijo.
    // El outline cicla por todo el espectro HSV a razón de 0.5 ciclos/segundo
    // (una vuelta completa de colores cada 2 segundos), igual que la estrella de
    // Super Mario Bros. Usa yield return null + deltaTime en vez de WaitForSeconds
    // para poder actualizar el color cada frame mientras dura el efecto.
    IEnumerator OcultarBuffOutlineLuegoDe(float segundos)
    {
        float tiempoRestante = segundos;
        float hue = 0f;
        const float velocidadArcoiris = 0.5f; // ciclos por segundo

        while (tiempoRestante > 0f)
        {
            hue = Mathf.Repeat(hue + Time.deltaTime * velocidadArcoiris, 1f);
            if (buffOutlineSr != null)
                buffOutlineSr.color = Color.HSVToRGB(hue, 1f, 1f);
            tiempoRestante -= Time.deltaTime;
            yield return null;
        }

        if (buffOutlineObj != null) buffOutlineObj.SetActive(false);
        buffOutlineRoutine = null;

        // El potenciador expiró: avisar a MusicManager para que vuelva a
        // "Peleas Genericas". Solo lo hace si todavía está en la pista del
        // boss (si la victoria llegó antes, ya está en Exploración Color y
        // TerminarPotenciador lo detecta y no hace nada).
        if (MusicManager.Instance != null) MusicManager.Instance.TerminarPotenciador();
    }

    // Pedido del usuario: "recorda cortarlo" — cortar el potenciador también
    // al hacer Game Over (de lo contrario el coroutine se destruye con la
    // escena sin llamar TerminarPotenciador y la música de boss sigue sonando
    // en el menú). Llamado desde GameManager.GameOver().
    public void TerminarBuffOutline()
    {
        if (buffOutlineRoutine != null)
        {
            StopCoroutine(buffOutlineRoutine);
            buffOutlineRoutine = null;
        }
        if (buffOutlineObj != null) buffOutlineObj.SetActive(false);
        if (MusicManager.Instance != null) MusicManager.Instance.TerminarPotenciador();
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsAlive() => currentHealth > 0;

    // ============================================================
    // VERTICAL SLICE: usado por el dodge/roll (PlayerAnimator.DoDodge).
    // Extiende la ventana de invulnerabilidad ya existente (la misma que
    // evita que varios enemigos peguen en el mismo instante) para que
    // esquivar también sirva para evitar golpes durante la maniobra.
    // No reemplaza ni duplica el sistema de invulnerabilidad: solo lo alarga.
    // ============================================================
    public void GrantInvulnerability(float seconds)
    {
        invulnTimer = Mathf.Max(invulnTimer, seconds);
    }
}
