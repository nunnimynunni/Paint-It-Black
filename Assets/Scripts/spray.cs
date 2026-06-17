using UnityEngine;

public class spray : MonoBehaviour
{
    [Header("Daño")]
    public int damage = 1;
    public float damageCooldown = 0.3f; // segundos entre ticks de daño (evita sacar HP cada frame)
    public PaintColor colorType = PaintColor.Red; // seteado por playerataque al instanciar

    private float damageTimer = 0f;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void Init(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        // Aplicar tinte de color al sprite
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = PaintColorUtils.ToUnityColor(colorType);
    }

    void Update()
    {
        if (damageTimer > 0f)
            damageTimer -= Time.deltaTime;
    }

    // OnTriggerStay2D: se llama cada frame mientras el spray superpone un collider
    void OnTriggerStay2D(Collider2D other)
    {
        if (damageTimer > 0f) return;

        EnemyHealth enemy = other.GetComponent<EnemyHealth>() ?? other.GetComponentInParent<EnemyHealth>();
        if (enemy == null) return;

        enemy.TakeDamage(damage, colorType);
        damageTimer = damageCooldown;
    }
}
