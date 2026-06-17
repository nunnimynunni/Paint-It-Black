using UnityEngine;

// Soldado Pistola: mantiene una banda de distancia (no se pega de más, no se aleja de más)
// para poder disparar. Si está Pacificado no dispara; si está Asustado huye; si está Frenzy
// le dispara a lo que tenga más cerca (jugador u otro NPC).
public class EnemyPistolero : EnemyAI
{
    [Header("Rango de combate")]
    public float minRange = 4f;
    public float maxRange = 6f;
    [Tooltip("Si el objetivo está fuera de este radio, deja de perseguir")]
    public float detectionRange = 12f;

    [Header("Disparo")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 1.2f;
    public int damage = 1;
    [Tooltip("Probabilidad (0-1) de que el disparo vaya certero. El resto de las veces sale desviado y no pega.")]
    [Range(0f, 1f)] public float accuracy = 0.6f;
    [Tooltip("Grados de desvío máximo cuando el disparo falla")]
    public float missSpreadDegrees = 18f;

    private float shootTimer = 0f;

    protected override void Tick(float dt)
    {
        if (shootTimer > 0f) shootTimer -= dt * (status.AttackSpeedMultiplier);

        // Pacificado: deambula sin atacar
        if (!status.CanAct)
        {
            moveDir = ComputeErraticMovement(dt);
            return;
        }

        Transform target = status.IsFrenzied ? FindNearestEntity() : PlayerTransform;
        if (target == null) { moveDir = Vector2.zero; return; }

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float dist = toTarget.magnitude;

        // Asustado (Violeta): mantiene la máxima distancia posible del jugador
        if (status.IsFearful && target == PlayerTransform)
        {
            moveDir = dist < detectionRange ? -toTarget.normalized : Vector2.zero;
            return;
        }

        if (dist > detectionRange)
        {
            moveDir = Vector2.zero;
            return;
        }

        // Mantener banda [minRange, maxRange]
        if (dist > maxRange)
            moveDir = toTarget.normalized; // acercarse
        else if (dist < minRange)
            moveDir = -toTarget.normalized; // alejarse
        else
        {
            moveDir = Vector2.zero; // en rango, quedarse y disparar
            FaceDirection(toTarget.normalized); // sigue mirando al objetivo aunque no se mueva
        }

        // Disparar si está dentro de rango y el cooldown lo permite
        if (dist <= maxRange && shootTimer <= 0f)
        {
            Shoot(toTarget.normalized, target.gameObject);
            shootTimer = shootCooldown;
        }
    }

    void Shoot(Vector2 dir, GameObject targetObj)
    {
        if (bulletPrefab == null) return;
        Transform spawnFrom = firePoint != null ? firePoint : transform;

        // Tirada de precisión: si falla, desvía la dirección del disparo unos grados
        // para que no le acierte al objetivo (sigue disparando, solo erra el tiro).
        Vector2 finalDir = dir;
        if (Random.value > accuracy)
        {
            float offset = Random.Range(-missSpreadDegrees, missSpreadDegrees);
            // Evitar desvíos muy chicos que por casualidad sigan acertando
            if (Mathf.Abs(offset) < 5f) offset = offset < 0f ? -5f : 5f;
            finalDir = Quaternion.Euler(0, 0, offset) * dir;
        }

        GameObject bulletObj = Instantiate(bulletPrefab, spawnFrom.position, Quaternion.identity);
        EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            bullet.owner = gameObject;
            bullet.damage = Mathf.RoundToInt(damage * status.DamageMultiplier);
            bullet.Init(finalDir);
        }

        if (animator != null) animator.SetTrigger("Attack");
    }
}
