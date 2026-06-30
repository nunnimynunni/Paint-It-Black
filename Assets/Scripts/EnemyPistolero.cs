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
    [Tooltip("Ya no se usa para la posición de salida de la bala (ver distanciaCanon): se deja solo por compatibilidad si algún prefab lo tiene asignado.")]
    public Transform firePoint;
    [Tooltip("Distancia desde el centro del soldado hasta la punta del arma, en la dirección hacia la que está disparando. Feedback de playtest: los disparos deben salir de la punta del arma, no de 'cualquier lado'.")]
    public float distanciaCanon = 0.55f;
    // Feedback de playtest: "los enemigos deben tener menos cadencia de
    // disparo para que sea mas facil ganar". Subido de 1.2 a 1.7 (también
    // actualizado el override serializado en SoldadoPistola.prefab, que
    // tenía su propio valor 1.2 guardado y no toma el default del script).
    public float shootCooldown = 1.7f;
    // Daño rebalanceado: 10 por bala (×0.85 reducción = 8.5 ≈ 8 efectivo) → jugador con 100 HP aguanta ~12 impactos.
    public int damage = 10;
    [Tooltip("Probabilidad (0-1) de que el disparo vaya certero. El resto de las veces sale desviado y no pega.")]
    [Range(0f, 1f)] public float accuracy = 0.6f;
    [Tooltip("Grados de desvío máximo cuando el disparo falla")]
    public float missSpreadDegrees = 18f;
    [Tooltip("Segundos entre que se dispara el trigger 'Attack' del Animator y sale la bala de verdad, para que la bala no aparezca antes de que la animación llegue al frame de disparo.")]
    public float attackShotDelay = 0.15f;

    private float shootTimer = 0f;

    // Disparo pendiente: igual patrón que pendingHitTimer en EnemyPorra/
    // EnemyAntiDisturbios, para sincronizar el efecto real (acá, instanciar
    // la bala) con el momento justo de la animación en vez de spawnearla en
    // el mismo frame en que arranca el trigger "Attack".
    private Vector2 pendingDir;
    private GameObject pendingTargetObj;
    private float pendingShotTimer = -1f;

    protected override void Tick(float dt)
    {
        if (shootTimer > 0f) shootTimer -= dt * (status.AttackSpeedMultiplier);

        if (pendingShotTimer >= 0f)
        {
            pendingShotTimer -= dt;
            if (pendingShotTimer <= 0f)
            {
                DispararBala(pendingDir, pendingTargetObj);
                pendingShotTimer = -1f;
            }
        }

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

    // Feedback de playtest: "verifica que las animaciones de disparo de los
    // npcs estén bien timeados con el disparo". Antes la bala se instanciaba
    // en el mismo frame en que se disparaba el trigger "Attack" (antes
    // incluso de que la animación empezara a reproducirse). Ahora el trigger
    // sale al instante (para que la animación arranque ya mismo) pero la
    // bala de verdad recién aparece attackShotDelay segundos después,
    // sincronizada con el frame de disparo de la animación.
    void Shoot(Vector2 dir, GameObject targetObj)
    {
        if (bulletPrefab == null) return;

        pendingDir = dir;
        pendingTargetObj = targetObj;
        pendingShotTimer = attackShotDelay;

        if (animator != null) animator.SetTrigger("Attack");
    }

    // Feedback de playtest: "que los disparos salgan de la punta del arma
    // de los soldados, que no salgan de cualquier lado". Un firePoint hijo
    // fijo solo queda bien alineado para UNA orientación del sprite; como
    // este soldado encara a su objetivo en las 8 direcciones (FaceDirection
    // ya actualiza 'facing'), la posición de salida se calcula siempre en
    // runtime como un punto a distanciaCanon del centro, en la dirección
    // hacia la que está mirando/disparando — así la bala sale del lado
    // correcto sea cual sea la dirección.
    void DispararBala(Vector2 dir, GameObject targetObj)
    {
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

        Vector3 origenCanon = transform.position + (Vector3)(facing.normalized * distanciaCanon);

        GameObject bulletObj = Instantiate(bulletPrefab, origenCanon, Quaternion.identity);
        EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            bullet.owner = gameObject;
            // Feedback de playtest: "los disparos enemigos deben hacer un 15%
            // menos de daño". Se aplica como multiplicador acá (en vez de
            // bajar el valor base "damage") para que quede documentado y
            // siga escalando bien con status.DamageMultiplier y con cualquier
            // futuro ajuste del valor base en el Inspector.
            const float reduccionDanoFeedback = 0.85f;
            bullet.damage = Mathf.RoundToInt(damage * status.DamageMultiplier * reduccionDanoFeedback);
            bullet.Init(finalDir);
        }
    }
}
