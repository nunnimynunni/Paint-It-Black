using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 3f;

    [Header("Daño")]
    // Daño rebalanceado: 12 por impacto → NPC de 100 HP muere en ~8 disparos, efecto de color a los 3.
    public int damage = 12;
    public PaintColor colorType = PaintColor.Red; // seteado por playerataque al instanciar

    // GDD 3.7: "Mejoras de Habilidades Pasivas: rebote de proyectiles".
    // Si la mejora está activa, el primer impacto no destruye el proyectil:
    // lo redirige al enemigo más cercano (que no sea el que ya golpeó) y
    // sigue volando. Solo rebota una vez por proyectil.
    [Tooltip("Radio de búsqueda del próximo objetivo al rebotar")]
    public float radioRebote = 6f;
    private bool yaReboto = false;

    // Coraza Rebote: si este flag está activo el proyectil viaja de vuelta al
    // jugador y puede hacerle daño. El flag lo activa EnemyCoraza.IntentarRebote.
    private bool rebotadoAlJugador = false;

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
        Debug.Log("Projectile hit: " + other.name);

        // ── Proyectil rebotado: solo puede golpear al jugador ──────────────
        if (rebotadoAlJugador)
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                Destroy(gameObject);
            }
            else if (ObstacleUtils.EsObstaculoSolido(other))
            {
                Destroy(gameObject);
            }
            // Ignora colisiones con NPCs mientras viaja de vuelta al jugador
            return;
        }

        // ── Flujo normal: proyectil del jugador ────────────────────────────
        EnemyHealth enemy = other.GetComponent<EnemyHealth>() ?? other.GetComponentInParent<EnemyHealth>();
        if (enemy == null)
        {
            // Feedback de playtest: el proyectil debe detenerse/destruirse al
            // chocar con un objeto sólido del mapa (obstáculo) en vez de
            // atravesarlo sin efecto.
            if (ObstacleUtils.EsObstaculoSolido(other))
                Destroy(gameObject);
            return;
        }

        // ── Coraza Rebote: chequear si el NPC tiene coraza activa ──────────
        EnemyCoraza coraza = enemy.GetComponent<EnemyCoraza>() ?? enemy.GetComponentInParent<EnemyCoraza>();
        if (coraza != null && coraza.EstaActiva)
        {
            bool procesado = coraza.IntentarRebote(this);
            if (procesado)
            {
                // Si fue redirigido al jugador (MarcarComoRebotado lo setea),
                // el proyectil sigue volando; si fue absorbido por un NPC,
                // IntentarRebote llamó TakeDamage internamente → destruir.
                if (!rebotadoAlJugador)
                    Destroy(gameObject);
                return;
            }
            // Si no hubo rebote (25 % de chance): el proyectil pasa pero NO
            // aplica daño ni efectos de pintura mientras la coraza está activa.
            Destroy(gameObject);
            return;
        }

        enemy.TakeDamage(damage, colorType);

        bool puedeRebotar = !yaReboto && UpgradeSystem.Instance != null && UpgradeSystem.Instance.PasivaRebote;
        if (puedeRebotar)
        {
            EnemyHealth siguiente = BuscarSiguienteObjetivo(enemy);
            if (siguiente != null)
            {
                yaReboto = true;
                Vector2 nuevaDir = (Vector2)(siguiente.transform.position - transform.position);
                Init(nuevaDir);
                return; // no se destruye: sigue volando hacia el nuevo objetivo
            }
        }

        Destroy(gameObject);
    }

    EnemyHealth BuscarSiguienteObjetivo(EnemyHealth excluir)
    {
        EnemyHealth[] todos = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        EnemyHealth mejor = null;
        float mejorDist = radioRebote;

        foreach (var e in todos)
        {
            if (e == excluir || e == null) continue;
            if (!e.IsAlive()) continue;

            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist <= mejorDist)
            {
                mejorDist = dist;
                mejor = e;
            }
        }

        return mejor;
    }

    // ── API para EnemyCoraza ─────────────────────────────────────────────────

    // Cambia la dirección del proyectil (llamado por EnemyCoraza para redirigir al jugador)
    public void RebotrSetDireccion(Vector2 nuevaDir)
    {
        direction = nuevaDir.normalized;
        float angle = Mathf.Atan2(nuevaDir.y, nuevaDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    // Marca el proyectil como rebotado: a partir de aquí solo puede dañar al jugador
    public void MarcarComoRebotado()
    {
        rebotadoAlJugador = true;
    }
}
