using UnityEngine;

public class spray : MonoBehaviour
{
    [Header("Daño")]
    // VERTICAL SLICE: este script es el "Aerosol" del GDD. Daño real = 20 (antes placeholder = 1).
    // Ojo: como hace tick cada damageCooldown mientras se mantiene encima del enemigo,
    // el daño POR SEGUNDO real es damage / damageCooldown ≈ 66.6 dps a este ritmo —
    // si se siente muy fuerte en la práctica, conviene subir damageCooldown antes que bajar damage.
    public int damage = 20;
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
        if (enemy == null)
        {
            // Feedback de playtest: detenerse/destruirse al chocar con un
            // objeto sólido del mapa (obstáculo) en vez de atravesarlo.
            if (ObstacleUtils.EsObstaculoSolido(other))
                Destroy(gameObject);
            return;
        }

        enemy.TakeDamage(damage, colorType);
        damageTimer = damageCooldown;
    }
}
