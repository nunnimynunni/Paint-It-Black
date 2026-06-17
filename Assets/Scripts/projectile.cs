using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 3f;

    [Header("Daño")]
    public int damage = 1;
    public PaintColor colorType = PaintColor.Red; // seteado por playerataque al instanciar

    private Vector2 direction;

    void Awake()
    {
        // Forzar isTrigger para que detecte enemigos sin bloquear físicamente
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void Init(Vector2 dir)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        Destroy(gameObject, lifetime);

        // Aplicar tinte de color al sprite
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = PaintColorUtils.ToUnityColor(colorType);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy == null) return;

        enemy.TakeDamage(damage, colorType);
        Destroy(gameObject);
    }
}
