using UnityEngine;

// Soldado Porra: se acerca al objetivo hasta rango melee y ataca cuerpo a cuerpo.
public class EnemyPorra : EnemyAI
{
    [Header("Combate cuerpo a cuerpo")]
    public float meleeRange = 1.1f;
    public float detectionRange = 12f;
    public float attackCooldown = 1f;
    public int damage = 2;
    [Tooltip("Segundos entre que se dispara la animación de ataque y se aplica el daño")]
    public float attackHitDelay = 0.25f;

    private float attackTimer = 0f;
    private Transform pendingTarget;
    private float pendingHitTimer = -1f;

    protected override void Tick(float dt)
    {
        if (attackTimer > 0f) attackTimer -= dt * status.AttackSpeedMultiplier;

        // Resolver golpe pendiente (sincronizado con la animación de ataque)
        if (pendingHitTimer >= 0f)
        {
            pendingHitTimer -= dt;
            if (pendingHitTimer <= 0f)
            {
                if (pendingTarget != null &&
                    Vector2.Distance(transform.position, pendingTarget.position) <= meleeRange + 0.3f)
                {
                    DealDamageTo(pendingTarget, Mathf.RoundToInt(damage * status.DamageMultiplier));
                }
                pendingHitTimer = -1f;
            }
        }

        if (!status.CanAct)
        {
            moveDir = ComputeErraticMovement(dt);
            return;
        }

        Transform target = status.IsFrenzied ? FindNearestEntity() : PlayerTransform;
        if (target == null) { moveDir = Vector2.zero; return; }

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float dist = toTarget.magnitude;

        // Asustado: nunca entra en rango melee, si lo fuerzan evade
        if (status.IsFearful && target == PlayerTransform)
        {
            if (dist < meleeRange * 1.5f)
                moveDir = -toTarget.normalized;
            else
                moveDir = Vector2.zero;
            return;
        }

        if (dist > detectionRange) { moveDir = Vector2.zero; return; }

        if (dist > meleeRange)
        {
            moveDir = toTarget.normalized;
        }
        else
        {
            moveDir = Vector2.zero;
            FaceDirection(toTarget.normalized);
            if (attackTimer <= 0f)
            {
                Attack(target);
                attackTimer = attackCooldown;
            }
        }
    }

    void Attack(Transform target)
    {
        pendingTarget = target;
        pendingHitTimer = attackHitDelay;
        if (animator != null) animator.SetTrigger("Attack");
    }
}
