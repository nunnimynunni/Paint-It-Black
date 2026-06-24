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

    // Feedback de playtest: "asegurate que los npcs no traspasen los objetos
    // de estructuras [...] que siempre se mantengan lejos a un rango minimo
    // de estas asi evitan traspasarlas". La física (Rigidbody2D + Collider2D
    // sólido) ya impide que las atraviesen del todo, pero al perseguir en
    // línea recta al jugador terminan pegados/rozando el borde de casas,
    // cascada, árboles, etc., lo que se ve como que las traspasan un poco.
    [Header("Evasión de estructuras")]
    [Tooltip("Distancia mínima que se intenta mantener respecto a obstáculos sólidos del mapa (casas, cascada, árboles...).")]
    public float distanciaMinimaEstructuras = 0.5f;
    private static readonly Collider2D[] bufferEstructuras = new Collider2D[8];
    // Physics2D.OverlapCircleNonAlloc quedó obsoleto; el reemplazo no-obsoleto
    // pide un ContactFilter2D. NoFilter() reproduce el mismo comportamiento de
    // antes (sin filtrar por capa/profundidad, incluye triggers).
    private static readonly ContactFilter2D filtroEstructuras = CrearFiltroSinFiltrar();
    private static ContactFilter2D CrearFiltroSinFiltrar()
    {
        ContactFilter2D f = new ContactFilter2D();
        f.NoFilter();
        return f;
    }

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

        ultimaPosicionRevisada = transform.position;
    }

    // ============================================================
    // Feedback de playtest: "un enemigo quedo atrapado entre arboles, esto
    // no puede suceder ya que no puedo ganar" (CheckVictory exige que TODOS
    // los enemigos planeados estén muertos). Red de seguridad: si el NPC
    // lleva un buen rato queriendo moverse (moveDir != 0) pero apenas cambia
    // de posición real, se asume que quedó físicamente encajonado entre
    // colliders sólidos (ej: dos árboles muy juntos) y se lo destraba con un
    // pequeño desplazamiento directo del transform (no pasa por el
    // Rigidbody/colisiones, así ningún collider en el medio lo puede volver
    // a frenar).
    // ============================================================
    private Vector2 ultimaPosicionRevisada;
    private float tiempoAtascado = 0f;
    private float proximaRevisionAtasco = 0f;
    private const float intervaloRevisionAtasco = 1f;
    private const float distanciaMinimaParaNoEstarAtascado = 0.15f;
    private const float tiempoMaximoAtascado = 4f;

    void RevisarAtasco(float dt)
    {
        proximaRevisionAtasco -= dt;
        if (proximaRevisionAtasco > 0f) return;
        proximaRevisionAtasco = intervaloRevisionAtasco;

        float avance = Vector2.Distance(transform.position, ultimaPosicionRevisada);
        ultimaPosicionRevisada = transform.position;

        bool queriaMoverse = moveDir.sqrMagnitude > 0.0001f;

        if (queriaMoverse && avance < distanciaMinimaParaNoEstarAtascado)
            tiempoAtascado += intervaloRevisionAtasco;
        else
            tiempoAtascado = 0f;

        if (tiempoAtascado >= tiempoMaximoAtascado)
        {
            Destrabar();
            tiempoAtascado = 0f;
        }
    }

    void Destrabar()
    {
        Vector2 direccionLibre = Random.insideUnitCircle.normalized;
        transform.position += (Vector3)(direccionLibre * 1.5f);
        ultimaPosicionRevisada = transform.position;
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

        // Feedback de playtest: "los enemigos no deben poder moverse ni
        // atacar mientras tienen la animacion onhit". Tick() es quien decide
        // tanto el movimiento como disparar/atacar en cada subclase, así que
        // alcanza con no llamarlo mientras dure el aturdimiento del golpe.
        if (health != null && health.IsStunned)
        {
            moveDir = Vector2.zero;
            UpdateAnimator(moveDir);
            return;
        }

        Tick(Time.deltaTime);
        moveDir = AplicarEvasionDeEstructuras(moveDir);
        UpdateAnimator(moveDir);
        RevisarAtasco(Time.deltaTime);
    }

    // Suma un empuje "hacia afuera" al moveDir deseado por cada obstáculo
    // sólido del mapa (ObstacleUtils, ver su comentario: por exclusión, todo
    // collider sólido que no sea jugador/NPC/pickup) que esté a menos de
    // distanciaMinimaEstructuras. Así el NPC rodea la estructura en vez de
    // quedarse frotando/clavado contra su borde mientras persigue al jugador.
    protected Vector2 AplicarEvasionDeEstructuras(Vector2 deseado)
    {
        int n = Physics2D.OverlapCircle(transform.position, distanciaMinimaEstructuras, filtroEstructuras, bufferEstructuras);
        if (n <= 0) return deseado;

        Vector2 empuje = Vector2.zero;
        for (int i = 0; i < n; i++)
        {
            Collider2D col = bufferEstructuras[i];
            if (!ObstacleUtils.EsObstaculoSolido(col)) continue;

            Vector2 puntoCercano = col.ClosestPoint(transform.position);
            Vector2 fuera = (Vector2)transform.position - puntoCercano;
            float dist = fuera.magnitude;
            if (dist < 0.0001f) continue; // ya está encima del centro, evita dividir por cero

            float fuerza = 1f - Mathf.Clamp01(dist / distanciaMinimaEstructuras);
            empuje += fuera.normalized * fuerza;
        }

        if (empuje.sqrMagnitude < 0.0001f) return deseado;

        Vector2 resultado = deseado + empuje;
        float magnitudDeseada = Mathf.Max(deseado.magnitude, 0.01f);
        return resultado.sqrMagnitude > 0.0001f ? resultado.normalized * magnitudDeseada : deseado;
    }

    void FixedUpdate()
    {
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            float speed = baseMoveSpeed * (status != null ? status.MoveSpeedMultiplier : 1f);
            rb.MovePosition(rb.position + moveDir.normalized * speed * Time.fixedDeltaTime);
        }
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
