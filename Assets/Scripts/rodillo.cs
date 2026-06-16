using UnityEngine;

public class rodillo : MonoBehaviour
{
    public float lifetime = 0.4f;

    [Header("Daño")]
    public int damage = 2; // el melee hace más daño que los proyectiles
    public PaintColor colorType = PaintColor.Red; // seteado por playerataque al instanciar

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void Init(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        Destroy(gameObject, lifetime);

        // Aplicar tinte de color al sprite
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = PaintColorUtils.ToUnityColor(colorType);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy == null) return;

        enemy.TakeDamage(damage, colorType);
        // El rodillo no se destruye al golpear — sigue activo por su lifetime
        // FUTURO: podría acumular hits para efectos especiales
    }
}
