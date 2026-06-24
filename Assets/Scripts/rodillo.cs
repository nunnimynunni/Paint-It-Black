using UnityEngine;

public class rodillo : MonoBehaviour
{
    public float lifetime = 0.4f;

    [Header("Daño")]
    // VERTICAL SLICE: este script es el "Rodillo" del GDD. Daño real = 35 (antes placeholder = 2).
    public int damage = 35;
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
