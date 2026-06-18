using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHP = 100;
    private int currentHP;

    public GameObject damagePopupPrefab;

    // Se dispara justo antes de destruir el objeto (lo usa el EnemySpawner para contar bajas)
    public event System.Action OnDeath;

    private SpriteRenderer sr;
    private Animator anim;
    private Color colorOriginal;
    private EnemyStatusEffects statusEffects;

    private Color hitColor;
    private float hitTimer = 0f;

    void Start()
    {
        currentHP = maxHP;
        sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        statusEffects = GetComponent<EnemyStatusEffects>();
        if (sr != null) colorOriginal = sr.color;
    }

    void LateUpdate()
    {
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

        currentHP -= amount;

        if (anim != null) anim.SetTrigger("OnHit");

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

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        currentHP = 0;
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    public int GetCurrentHP() => currentHP;
    public bool IsAlive() => currentHP > 0;
}
