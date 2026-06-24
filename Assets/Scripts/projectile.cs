using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 3f;

    [Header("Daño")]
    public int damage = 1;
    public PaintColor colorType = PaintColor.Red; // seteado por playerataque al instanciar

    [Header("Obstáculos")]
    [Tooltip("Capa de casas/árboles/etc. Si el disparo choca contra algo de esta capa, se destruye ahí (no la atraviesa).")]
    public LayerMask obstacleLayer;

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
        // Choca contra un obstáculo (casa, árbol, etc.): se frena ahí, no sigue de largo.
        if (((1 << other.gameObject.layer) & obstacleLayer.value) != 0)
        {
            Destroy(gameObject);
            return;
        }

        Debug.Log("Projectile hit: " + other.name);
        EnemyHealth enemy = other.GetComponent<EnemyHealth>() ?? other.GetComponentInParent<EnemyHealth>();
        if (enemy == null) return;

        enemy.TakeDamage(damage, colorType);
        Destroy(gameObject);
    }
}
