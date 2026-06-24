using System.Collections;
using UnityEngine;

// ============================================================
// Soldado Anti Disturbios (GDD): unidad defensiva con 150 PV. Combina tres
// comportamientos:
//   1) Protección: se ubica entre el jugador y el aliado más cercano para
//      que su propio collider "tape" disparos antes de que lleguen al
//      aliado de atrás (comportamiento ya existente, sin tocar).
//   2) Ataque cuerpo a cuerpo: GDD dice explícitamente "su ataque es cuerpo
//      a cuerpo". Si el jugador se acerca a rango melee, golpea (mismo
//      patrón que EnemyPorra: animación -> delay -> daño).
//   3) Escudo frontal: "un escudo que bloquea ataques frontales. Este
//      bloqueo, si bien efectivo, lo inmoviliza por 3 segundos, creando una
//      ventana de oportunidad para flanquear o usar estrategias de color."
//      EnemyHealth.TakeDamage() llama a TryBloquear() ANTES de aplicar el
//      golpe; si el golpe llega de frente (según hacia dónde está mirando
//      este NPC) lo bloquea por completo y arranca el stun de 3s.
//
// Nota de balance (pedido del usuario de bajar levemente la dificultad):
// cooldown de ataque algo más lento y daño algo menor que el Porra, ya que
// esta unidad acumula varios roles (proteger + bloquear + atacar) y 150 PV.
// ============================================================
public class EnemyAntiDisturbios : EnemyAI
{
    [Header("Protección")]
    public float allyDetectionRange = 10f;
    [Tooltip("Qué tan cerca del jugador se para a interponerse (0 = encima del aliado, 1 = encima del jugador)")]
    [Range(0.1f, 0.9f)] public float blockPositionRatio = 0.4f;
    public float followDistance = 0.3f; // margen para no vibrar al llegar al punto

    [Header("Combate cuerpo a cuerpo")]
    public float meleeRange = 1.2f;
    public float detectionRange = 12f;
    public float attackCooldown = 1.3f;
    public int damage = 10;
    [Tooltip("Segundos entre que se dispara la animación de ataque y se aplica el daño")]
    public float attackHitDelay = 0.3f;

    private float attackTimer = 0f;
    private Transform pendingTarget;
    private float pendingHitTimer = -1f;

    [Header("Escudo frontal (GDD)")]
    [Tooltip("Qué tan de frente tiene que llegar el golpe para que el escudo lo bloquee (1 = exactamente de frente, valores menores amplían el ángulo)")]
    [Range(0f, 1f)] public float umbralFrontal = 0.35f;
    [Tooltip("Segundos inmovilizado después de bloquear con éxito (GDD: ventana para flanquear)")]
    public float duracionStunPorBloqueo = 3f;

    private float stunTimer = 0f;
    public bool EstaInmovilizadoPorEscudo => stunTimer > 0f;

    protected override void Tick(float dt)
    {
        // Resolver golpe melee pendiente (sincronizado con la animación de ataque)
        if (attackTimer > 0f) attackTimer -= dt * status.AttackSpeedMultiplier;
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

        // GDD: tras bloquear con el escudo queda inmovilizado: ni se mueve ni ataca.
        if (stunTimer > 0f)
        {
            stunTimer -= dt;
            moveDir = Vector2.zero;
            return;
        }

        if (!status.CanAct)
        {
            moveDir = ComputeErraticMovement(dt);
            return;
        }

        Transform playerT = PlayerTransform;
        if (playerT == null) { moveDir = Vector2.zero; return; }

        if (status.IsFearful)
        {
            float distP = DistanceToPlayer();
            moveDir = distP < allyDetectionRange
                ? ((Vector2)transform.position - (Vector2)playerT.position).normalized
                : Vector2.zero;
            return;
        }

        float dist = DistanceToPlayer();

        // Si el jugador está en rango melee, prioriza atacar por sobre interponerse.
        if (dist <= meleeRange && dist <= detectionRange)
        {
            moveDir = Vector2.zero;
            FaceDirection(((Vector2)playerT.position - (Vector2)transform.position).normalized);
            if (attackTimer <= 0f)
            {
                Attack(playerT);
                attackTimer = attackCooldown;
            }
            return;
        }

        Transform ally = FindNearestAlly();

        Vector3 targetPoint;
        if (ally != null)
        {
            targetPoint = Vector3.Lerp(ally.position, playerT.position, blockPositionRatio);
        }
        else
        {
            Vector2 awayFromPlayer = ((Vector2)transform.position - (Vector2)playerT.position);
            if (awayFromPlayer.sqrMagnitude < 0.01f) awayFromPlayer = Vector2.up;
            targetPoint = playerT.position + (Vector3)(awayFromPlayer.normalized * 3f);
        }

        Vector2 toPoint = (Vector2)targetPoint - (Vector2)transform.position;
        moveDir = toPoint.magnitude > followDistance ? toPoint.normalized : Vector2.zero;

        // Cuando ya llegó al punto de bloqueo y queda quieto, encara al jugador:
        // así el escudo efectivamente "mira" hacia de dónde van a venir los golpes.
        if (moveDir == Vector2.zero)
            FaceDirection(((Vector2)playerT.position - (Vector2)transform.position).normalized);
    }

    void Attack(Transform target)
    {
        pendingTarget = target;
        pendingHitTimer = attackHitDelay;
        if (animator != null) animator.SetTrigger("Attack");
    }

    Transform FindNearestAlly()
    {
        Transform nearest = null;
        float best = allyDetectionRange;
        foreach (var other in All)
        {
            if (other == this || other == null) continue;
            if (other is EnemyAntiDisturbios) continue;
            float d = Vector2.Distance(transform.position, other.transform.position);
            if (d < best) { best = d; nearest = other.transform; }
        }
        return nearest;
    }

    // ============================================================
    // Escudo frontal (GDD): EnemyHealth.TakeDamage() llama a esto ANTES de
    // aplicar cualquier golpe. Si el ataque llega desde el lado al que está
    // mirando este NPC, lo bloquea por completo y arranca la inmovilización
    // de `duracionStunPorBloqueo` segundos. Si llega de costado/atrás
    // (flanqueo), no bloquea y el golpe pasa normalmente.
    // ============================================================
    public bool TryBloquear()
    {
        if (stunTimer > 0f) return false; // ya está en la ventana de stun: no puede volver a bloquear

        Transform playerT = PlayerTransform;
        if (playerT == null) return false;

        Vector2 dirHaciaJugador = ((Vector2)playerT.position - (Vector2)transform.position).normalized;
        float dot = Vector2.Dot(facing, dirHaciaJugador);

        if (dot < umbralFrontal) return false; // no está de frente: flanqueo exitoso, el golpe pasa

        stunTimer = duracionStunPorBloqueo;
        StartCoroutine(FlashBloqueoYStun());
        return true;
    }

    // Feedback visual del bloqueo: un flash blanco breve ("golpe absorbido") y
    // después un tinte gris apagado durante toda la inmovilización, para que
    // se note a simple vista que está "abierto" y es el momento de flanquearlo.
    IEnumerator FlashBloqueoYStun()
    {
        if (sr == null) yield break;

        Color colorAnterior = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.1f);

        sr.color = new Color(0.6f, 0.6f, 0.6f, colorAnterior.a);

        float restante = duracionStunPorBloqueo - 0.1f;
        if (restante > 0f) yield return new WaitForSeconds(restante);

        sr.color = colorAnterior;
    }
}
