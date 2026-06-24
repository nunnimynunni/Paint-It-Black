using UnityEngine;

// Proyectil disparado por el Soldado Pistola. A diferencia de Projectile.cs (del jugador),
// este puede dañar tanto al jugador como a otros NPCs (necesario para el Frenzy / Rojo,
// donde un enemizo enfurecido puede terminar disparándole a otro NPC).
public class EnemyBullet : MonoBehaviour
{
    public float speed = 8f;
    public float lifetime = 3f;
    public int damage = 1;

    [HideInInspector] public GameObject owner;

    private Vector2 dir;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void Init(Vector2 direction)
    {
        dir = direction.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(dir * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        EnemyHealth eh = other.GetComponent<EnemyHealth>() ?? other.GetComponentInParent<EnemyHealth>();
        if (eh != null && (owner == null || eh.gameObject != owner))
        {
            // Nota: usa Gray (Aguado) para no contar como impacto de color en fuego amigo
            eh.TakeDamage(damage, PaintColor.Gray);
            Destroy(gameObject);
            return;
        }

        // Feedback de playtest: detenerse/destruirse al chocar con un objeto
        // sólido del mapa (obstáculo) en vez de atravesarlo sin efecto.
        if (ObstacleUtils.EsObstaculoSolido(other))
            Destroy(gameObject);
    }
}
