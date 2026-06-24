using System.Collections.Generic;
using UnityEngine;

// Base de IA contextual para los NPCs de combate (Pistola, Porra, Anti Disturbios).
// Se encarga de: moverse físicamente (choca con colliders, no los traspasa),
// animar la dirección de movimiento (reusa los mismos bools que NPCMovement),
// y exponer al jugador / a otros enemigos para que las subclases decidan qué hacer.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyStatusEffects))]
public abstract class EnemyAI : MonoBehaviour
{
    // Registro estático de todos los enemigos vivos -> lo usa el modo Frenzy (Rojo)
    // para elegir como objetivo a "la entidad más cercana" sin importar facción.
    public static readonly List<EnemyAI> All = new List<EnemyAI>();

    [Header("Movimiento")]
    public float baseMoveSpeed = 2f;

    [Header("Evitar obstáculos")]
    [Tooltip("Capa de casas/árboles/etc. El NPC se aleja de lo que esté en esta capa antes de chocar, para no quedar trabado contra el hitbox.")]
    public LayerMask obstacleLayer;
    [Tooltip("Radio desde el que empieza a 'sentir' un obstáculo cercano y desviarse")]
    public float obstacleAvoidRadius = 1.2f;
    [Tooltip("Qué tan fuerte pesa la evasión contra la dirección que quería tomar la IA")]
    public float obstacleAvoidStrength = 1.6f;

    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer sr;
    protected EnemyHealth health;
    protected EnemyStatusEffects status;

    // Dirección de movimiento deseada para este frame (la setean las subclases en Tick())
    protected Vector2 moveDir = Vector2.zero;

    // Última dirección "hacia donde mira" el NPC. Se actualiza al moverse, y también
    // la pueden forzar las subclases con FaceDirection() para encarar al objetivo
    // mientras están quietas disparando/atacando.
    protected Vector2 facing = Vector2.down;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();
        status = GetComponent<EnemyStatusEffects>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    protected virtual void OnEnable() => All.Add(this);
    protected virtual void OnDisable() => All.Remove(this);

    protected virtual void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            moveDir = Vector2.zero;
            UpdateAnimator(moveDir);
            return;
        }

        Tick(Time.deltaTime);
        UpdateAnimator(moveDir);
    }

    void FixedUpdate()
    {
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Vector2 desired = moveDir.normalized;
            Vector2 avoidance = ComputeObstacleAvoidance();

            Vector2 finalDir = desired + avoidance * obstacleAvoidStrength;
            if (finalDir.sqrMagnitude > 0.0001f) finalDir.Normalize();
            else finalDir = desired;

            float speed = baseMoveSpeed * (status != null ? status.MoveSpeedMultiplier : 1f);
            rb.MovePosition(rb.position + finalDir * speed * Time.fixedDeltaTime);
        }
    }

    // Steering simple: suma un vector "de escape" por cada obstáculo cercano (casa, árbol, etc.),
    // más fuerte cuanto más cerca está. Así el NPC empieza a desviarse antes de llegar a chocar
    // con el collider, en vez de quedar empujándolo/trabado contra el borde del hitbox.
    private Vector2 ComputeObstacleAvoidance()
    {
        if (obstacleLayer.value == 0) return Vector2.zero;

        Vector2 result = Vector2.zero;
        Collider2D[] hits = Physics2D.OverlapCircleAll(rb.position, obstacleAvoidRadius, obstacleLayer);

        foreach (var hit in hits)
        {
            Vector2 closest = hit.ClosestPoint(rb.position);
            Vector2 away = rb.position - closest;
            float dist = away.magnitude;
            if (dist < 0.001f) continue;

            float weight = Mathf.Clamp01(1f - dist / obstacleAvoidRadius);
            result += away.normalized * weight;
        }

        return result;
    }

    // Las subclases implementan acá su comportamiento (perseguir, mantener rango, proteger, etc.)
    // y deben setear `moveDir` con la dirección deseada (puede ser Vector2.zero para quedarse quieto).
    protected abstract void Tick(float dt);

    // --- Helpers comunes ---

    protected Transform PlayerTransform =>
        PlayerHealth.Instance != null ? PlayerHealth.Instance.transform : null;

    protected float DistanceToPlayer()
    {
        if (PlayerTransform == null) return float.MaxValue;
        return Vector2.Distance(transform.position, PlayerTransform.position);
    }

    // Busca la entidad (jugador u otro enemigo) más cercana a este NPC. Usado por Frenzy.
    protected Transform FindNearestEntity()
    {
        Transform nearest = null;
        float best = float.MaxValue;

        if (PlayerTransform != null)
        {
            best = Vector2.Distance(transform.position, PlayerTransform.position);
            nearest = PlayerTransform;
        }

        foreach (var other in All)
        {
            if (other == this || other == null) continue;
            float d = Vector2.Distance(transform.position, other.transform.position);
            if (d < best)
            {
                best = d;
                nearest = other.transform;
            }
        }

        return nearest;
    }

    // Intenta hacer daño a lo que sea (jugador o enemigo) que esté en `target`.
    protected void DealDamageTo(Transform target, int amount)
    {
        if (target == null) return;

        PlayerHealth ph = target.GetComponent<PlayerHealth>();
        if (ph != null) { ph.TakeDamage(amount); return; }

        EnemyHealth eh = target.GetComponent<EnemyHealth>();
        if (eh != null && eh != health) eh.TakeDamage(amount, PaintColor.Gray);
    }

    // Movimiento errático (para el estado Pacificado / Amarillo): deambula sin intención de combate
    private Vector2 erraticDir = Vector2.zero;
    private float erraticTimer = 0f;
    protected Vector2 ComputeErraticMovement(float dt)
    {
        erraticTimer -= dt;
        if (erraticTimer <= 0f)
        {
            erraticDir = Random.insideUnitCircle.normalized;
            erraticTimer = Random.Range(0.8f, 2f);
        }
        return erraticDir;
    }

    // Fuerza hacia dónde "mira" el NPC aunque no se esté moviendo (ej: apuntar al disparar).
    protected void FaceDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        facing = SnapTo8(dir);
    }

    // --- Animación: igual lógica que NPCMovement pero para una dirección cualquiera ---
    // Setea dos grupos de parámetros:
    //  - isWalkingX: true solo mientras se está trasladando (anima el ciclo de caminata)
    //  - facingX: siempre refleja hacia dónde mira (lo usan los ataques/disparos direccionales)
    protected void UpdateAnimator(Vector2 dir)
    {
        animator.SetBool("isWalkingDown", false);
        animator.SetBool("isWalkingUp", false);
        animator.SetBool("isWalkingRight", false);
        animator.SetBool("isWalkingDiagonalUp", false);
        animator.SetBool("isWalkingDiagonalDown", false);

        if (dir.sqrMagnitude > 0.0001f)
        {
            facing = SnapTo8(dir);

            if (facing == Vector2.down) { animator.SetBool("isWalkingDown", true); sr.flipX = false; }
            else if (facing == Vector2.up) { animator.SetBool("isWalkingUp", true); sr.flipX = false; }
            else if (facing.x > 0)
            {
                sr.flipX = false;
                if (facing.y > 0) animator.SetBool("isWalkingDiagonalUp", true);
                else if (facing.y < 0) animator.SetBool("isWalkingDiagonalDown", true);
                else animator.SetBool("isWalkingRight", true);
            }
            else if (facing.x < 0)
            {
                sr.flipX = true;
                if (facing.y > 0) animator.SetBool("isWalkingDiagonalUp", true);
                else if (facing.y < 0) animator.SetBool("isWalkingDiagonalDown", true);
                else animator.SetBool("isWalkingRight", true);
            }
        }
        else
        {
            // Quieto: mantiene el flip acorde a la última dirección conocida
            sr.flipX = facing.x < 0;
        }

        SetFacingParams(facing);
    }

    void SetFacingParams(Vector2 f)
    {
        bool down = f == Vector2.down;
        bool up = f == Vector2.up;
        bool right = !down && !up && f.y == 0;
        bool diagUp = !down && !up && !right && f.y > 0;
        bool diagDown = !down && !up && !right && !diagUp;

        // Estos 5 parámetros (bool) hay que agregarlos a mano en los controllers
        // que vayan a tener ataques/disparos direccionales (ver guía aparte).
        SafeSetBool("facingDown", down);
        SafeSetBool("facingUp", up);
        SafeSetBool("facingRight", right);
        SafeSetBool("facingDiagonalUp", diagUp);
        SafeSetBool("facingDiagonalDown", diagDown);
    }

    // Evita warnings en consola si el controller todavía no tiene el parámetro agregado.
    void SafeSetBool(string name, bool value)
    {
        foreach (var p in animator.parameters)
        {
            if (p.name == name && p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(name, value);
                return;
            }
        }
    }

    // Redondea cualquier dirección al octante más cercano (8 direcciones)
    protected static Vector2 SnapTo8(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        float rad = snappedAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Round(Mathf.Cos(rad)), Mathf.Round(Mathf.Sin(rad)));
    }
}
