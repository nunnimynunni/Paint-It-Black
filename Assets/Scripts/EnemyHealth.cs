using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHP = 100;
    private int currentHP;

    public GameObject damagePopupPrefab;

    // ============================================================
    // GDD: "Reposición de Munición [...] también se puede lootear en
    // puntos donde hayan rastros de pintura". Al morir, el enemigo puede
    // dropear una gota de munición lootable (AmmoPickup) del color con el
    // que fue golpeado por última vez (así el "rastro de pintura" tiene
    // sentido: es literalmente el color que lo mató).
    // ============================================================
    [Header("Drop de munición (GDD: rastros de pintura)")]
    [Range(0f, 1f)] public float probabilidadDropMunicion = 0.5f;
    public int cantidadMunicionPorDrop = 8;
    private PaintColor ultimoColorRecibido = PaintColor.Red;

    // ============================================================
    // GDD / feedback de playtest: "matar npcs debe regenerar un pequeño
    // porcentaje de la vida" del jugador. Se calcula sobre la vida MÁXIMA
    // (no la actual) para que el porcentaje sea consistente sin importar
    // cuánta vida le quede en ese momento.
    // ============================================================
    [Header("Curación al matar NPC (GDD)")]
    [Range(0f, 1f)] public float porcentajeCuracionAlMorir = 0.03f;

    // Se dispara justo antes de destruir el objeto (lo usa el EnemySpawner para contar bajas)
    public event System.Action OnDeath;

    private SpriteRenderer sr;
    private Animator anim;
    private Color colorOriginal;
    private EnemyStatusEffects statusEffects;

    private Color hitColor;
    private float hitTimer = 0f;

    // ============================================================
    // Feedback de playtest: "los enemigos no deben poder moverse ni atacar
    // mientras tienen la animacion onhit". EnemyAI consulta IsStunned para
    // saltearse Tick() (que es quien decide tanto el movimiento como
    // disparar/atacar en cada subclase) mientras dura este aturdimiento.
    // ============================================================
    [Header("Aturdimiento al ser golpeado")]
    [Tooltip("Mientras dura esto tras recibir un golpe, el NPC no se mueve ni ataca (se ve completa la animación OnHit).")]
    public float duracionAturdimientoPorGolpe = 0.3f;
    private float stunTimer = 0f;
    public bool IsStunned => stunTimer > 0f;

    // ============================================================
    // GDD: el Anti Disturbios tiene un escudo que bloquea ataques frontales
    // (inmovilizándolo 3s después). Si este componente existe en el mismo
    // GameObject, se le consulta ANTES de aplicar cualquier golpe.
    // ============================================================
    private EnemyAntiDisturbios escudo;

    // ============================================================
    // Feedback de playtest (énfasis del usuario): los enemigos caídos NO
    // deben moverse NADA mientras están en el estado Down/Defeated, deben
    // quedar fijos en el lugar hasta desaparecer. Apagar IA/Rigidbody ya
    // debería bastar, pero como red de seguridad ante cualquier otra causa
    // de movimiento (curvas de posición dentro del propio clip de animación,
    // interpolación residual del Rigidbody, etc.) se fija la posición exacta
    // del momento de morir y se la reimpone cada frame en LateUpdate mientras
    // dure la secuencia de derrota.
    // ============================================================
    private bool muerto = false;
    private Vector3 posicionAlMorir;
    // Feedback de playtest (nueva ronda): "no deben girar ni espejarse" en el
    // estado caído. Además de la posición, se fija también la rotación y el
    // flip del sprite del momento exacto de morir, y se reimponen los tres
    // cada frame en LateUpdate, sin importar la causa real del movimiento
    // (root motion del clip, Mecanim, física residual, etc.) — mismo criterio
    // "red de seguridad" que ya se usó para la posición.
    private Quaternion rotacionAlMorir;
    private bool flipXAlMorir;

    void Start()
    {
        currentHP = maxHP;
        sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        statusEffects = GetComponent<EnemyStatusEffects>();
        escudo = GetComponent<EnemyAntiDisturbios>();
        if (sr != null) colorOriginal = sr.color;
    }

    void LateUpdate()
    {
        // Red de seguridad: si está en el estado caído/derrotado, se fijan
        // posición, rotación y flip SIEMPRE, sin importar qué otra cosa
        // intente moverlos/girarlos/espejarlos (clip de animación, Mecanim, etc.).
        if (muerto)
        {
            transform.position = posicionAlMorir;
            transform.rotation = rotacionAlMorir;
            if (sr != null) sr.flipX = flipXAlMorir;
        }

        if (stunTimer > 0f) stunTimer -= Time.deltaTime;

        if (sr == null || hitTimer <= 0f) return;
        hitTimer -= Time.deltaTime;
        // OJO: al terminar el flash del golpe, NO volver al color original sin teñir.
        // Si hay un efecto de pintura activo, su tinte es la "base" actual del sprite;
        // si volviéramos a colorOriginal lo perderíamos cada vez que pega de nuevo
        // (esto era el bug por el cual el efecto "se desactivaba al instante").
        Color baseNow = (statusEffects != null) ? statusEffects.CurrentBaseColor : colorOriginal;
        sr.color = hitTimer > 0f ? hitColor : baseNow;
    }

    public void TakeDamage(int amount, PaintColor color)
    {
        if (currentHP <= 0) return;

        // GDD: escudo frontal del Anti Disturbios. Si bloquea, el golpe se
        // absorbe por completo (sin daño, sin tinte, sin efecto de color),
        // pero el NPC queda inmovilizado unos segundos como contrapartida.
        if (escudo != null && escudo.TryBloquear())
            return;

        currentHP -= amount;
        ultimoColorRecibido = color;

        if (anim != null) anim.SetTrigger("OnHit");
        stunTimer = duracionAturdimientoPorGolpe;

        hitColor = PaintColorUtils.ToUnityColor(color);
        hitColor.a = 0.6f;
        hitTimer = 0.2f;

        DamagePopup.Create(damagePopupPrefab, transform.position, amount, PaintColorUtils.ToUnityColor(color));

        // Sistema de pintura: cuenta el impacto de este color, puede disparar un efecto de estado
        if (statusEffects != null) statusEffects.RegisterHit(color);

        if (currentHP <= 0)
            Die();
    }

    // Daño "crudo" (ej: veneno) que no cuenta como impacto de color ni reinicia el tinte
    public void TakeRawDamage(int amount)
    {
        if (currentHP <= 0) return;

        currentHP -= amount;

        hitColor = Color.green;
        hitColor.a = 0.6f;
        hitTimer = 0.2f;

        // Feedback de playtest: "si es como el verde que saca vida debe
        // verse ese nro de vida sacada". Antes el veneno (TakeRawDamage)
        // no mostraba ningún número de daño, solo TakeDamage lo hacía.
        DamagePopup.Create(damagePopupPrefab, transform.position, amount, Color.green);

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        currentHP = 0;

        // Drop de munición lootable (GDD), con el color con el que murió.
        if (Random.value <= probabilidadDropMunicion)
            AmmoPickup.Crear(transform.position, ultimoColorRecibido, cantidadMunicionPorDrop);

        // Feedback de playtest: "matar npcs debe regenerar un pequeño
        // porcentaje de la vida" del jugador, calculado sobre su vida máxima.
        if (PlayerHealth.Instance != null && porcentajeCuracionAlMorir > 0f)
        {
            int curacion = Mathf.RoundToInt(PlayerHealth.Instance.GetMaxHealth() * porcentajeCuracionAlMorir);
            if (curacion > 0) PlayerHealth.Instance.Heal(curacion);
        }

        OnDeath?.Invoke();
        StartCoroutine(SecuenciaDerrota());
    }

    // ============================================================
    // Feedback de playtest: "el down o defeated [...] esa animación los
    // muestra en el suelo por ende deben quedar en la pose final de esa
    // animación cuando los derrotas y desvanecerse tras unos 10 segundos".
    //
    // 1) Apaga IA/física/colisión para que el cuerpo no siga moviéndose
    //    ni bloqueando/recibiendo más golpes.
    // 2) Dispara el trigger "Defeated" del Animator (ya cableado en los
    //    controllers a un estado terminal sin transición de salida).
    // 3) Tras un instante para que la animación llegue a su última pose,
    //    apaga el Animator (congela el sprite ahí) y, ya cerca de los 10s
    //    totales, desvanece el SpriteRenderer antes de destruir el objeto.
    // ============================================================
    IEnumerator SecuenciaDerrota()
    {
        // Fija la posición exacta donde murió: LateUpdate la reimpone cada
        // frame hasta que el objeto se destruye, así no hay forma de que
        // se mueva (ni por física residual ni por el propio clip de animación).
        muerto = true;
        posicionAlMorir = transform.position;
        rotacionAlMorir = transform.rotation;
        flipXAlMorir = (sr != null) && sr.flipX;

        EnemyAI ia = GetComponent<EnemyAI>();
        if (ia != null) ia.enabled = false;

        // Por si el clip de "Defeated" tiene curvas de posición/rotación
        // (root motion) horneadas: se desactiva para que Mecanim no mueva ni
        // gire el transform por su cuenta mientras dure la animación.
        if (anim != null) anim.applyRootMotion = false;

        Rigidbody2D rb2 = GetComponent<Rigidbody2D>();
        if (rb2 != null)
        {
            rb2.linearVelocity = Vector2.zero;
            rb2.angularVelocity = 0f;
            rb2.bodyType = RigidbodyType2D.Kinematic;
        }

        // Feedback de playtest: el jugador NO debe poder atravesar a los
        // enemigos caídos, así que el collider se deja activo (antes se
        // desactivaba acá). El Rigidbody2D ya se puso Kinematic arriba, así
        // que el cuerpo no se ve empujado por física pese a seguir colisionando.

        if (anim != null) anim.SetTrigger("Defeated");

        const float tiempoTotal = 10f;
        const float duracionFade = 1f;
        const float esperaAntesDeCongelar = 0.8f;

        yield return new WaitForSeconds(esperaAntesDeCongelar);
        if (anim != null) anim.enabled = false; // congela el sprite en la última pose

        float esperaAntesDeFade = tiempoTotal - esperaAntesDeCongelar - duracionFade;
        if (esperaAntesDeFade > 0f) yield return new WaitForSeconds(esperaAntesDeFade);

        if (sr != null)
        {
            float t = 0f;
            Color c0 = sr.color;
            while (t < duracionFade)
            {
                t += Time.deltaTime;
                Color c = c0;
                c.a = Mathf.Lerp(c0.a, 0f, t / duracionFade);
                sr.color = c;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(duracionFade);
        }

        Destroy(gameObject);
    }

    public int GetCurrentHP() => currentHP;
    public bool IsAlive() => currentHP > 0;
}
