using UnityEngine;

// Soldado Anti Disturbios: no ataca. Se ubica entre el jugador y el aliado más cercano
// para "tapar" los disparos con su propio cuerpo/escudo (el bloqueo ocurre solo, porque
// su collider físico intercepta el proyectil antes de que llegue al aliado de atrás).
public class EnemyAntiDisturbios : EnemyAI
{
    [Header("Protección")]
    public float allyDetectionRange = 10f;
    [Tooltip("Qué tan cerca del jugador se para a interponerse (0 = encima del aliado, 1 = encima del jugador)")]
    [Range(0.1f, 0.9f)] public float blockPositionRatio = 0.4f;
    public float followDistance = 0.3f; // margen para no vibrar al llegar al punto

    protected override void Tick(float dt)
    {
        if (!status.CanAct)
        {
            moveDir = ComputeErraticMovement(dt);
            return;
        }

        Transform playerT = PlayerTransform;
        if (playerT == null) { moveDir = Vector2.zero; return; }

        // Asustado: prioriza huir, igual que los demás
        if (status.IsFearful)
        {
            float distP = DistanceToPlayer();
            moveDir = distP < allyDetectionRange
                ? ((Vector2)transform.position - (Vector2)playerT.position).normalized
                : Vector2.zero;
            return;
        }

        Transform ally = FindNearestAlly();

        Vector3 targetPoint;
        if (ally != null)
        {
            // Punto entre el aliado y el jugador, más cerca del aliado para taparlo
            targetPoint = Vector3.Lerp(ally.position, playerT.position, blockPositionRatio);
        }
        else
        {
            // Sin aliados cerca: se mantiene a distancia media del jugador, escudo al frente
            Vector2 awayFromPlayer = ((Vector2)transform.position - (Vector2)playerT.position);
            if (awayFromPlayer.sqrMagnitude < 0.01f) awayFromPlayer = Vector2.up;
            targetPoint = playerT.position + (Vector3)(awayFromPlayer.normalized * 3f);
        }

        Vector2 toPoint = (Vector2)targetPoint - (Vector2)transform.position;
        moveDir = toPoint.magnitude > followDistance ? toPoint.normalized : Vector2.zero;
    }

    // Busca el enemigo (que no sea otro Anti Disturbios) más cercano para protegerlo
    Transform FindNearestAlly()
    {
        Transform nearest = null;
        float best = allyDetectionRange;

        foreach (var other in All)
        {
            if (other == this || other == null) continue;
            if (other is EnemyAntiDisturbios) continue;

            float d = Vector2.Distance(transform.position, other.transform.position);
            if (d < best)
            {
                best = d;
                nearest = other.transform;
            }
        }

        return nearest;
    }
}
