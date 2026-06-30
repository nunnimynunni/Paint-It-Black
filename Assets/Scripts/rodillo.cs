using UnityEngine;

public class rodillo : MonoBehaviour
{
    public float lifetime = 0.4f;

    [Header("Daño")]
    // Daño rebalanceado: 8 por golpe → 3 golpes = 24 daño, NPC queda vivo con 76 HP para recibir el efecto de color.
    // Sigue siendo el arma más fuerte por golpe pero requiere acercarse (melee).
    public int damage = 8;
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
        // El rodillo no se destruye al golpear un enemigo — sigue activo por su lifetime
        // FUTURO: podría acumular hits para efectos especiales
    }
}
