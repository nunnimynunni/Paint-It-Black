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

    [Header("Separación entre NPCs")]
    [Tooltip("Radio de repulsión respecto a otros NPCs aliados. Evita que se agrupen. " +
             "Poner 0 para deshabilitar (p.ej. Anti Disturbios que forman escudo).")]
    public float radioSeparacionNPCs = 2.5f;
    private static readonly Collider2D[] bufferNPCs = new Collider2D[16];
    // Physics2D.OverlapCircleNonAlloc quedó obsoleto; el reemplazo no-obsoleto
    // pide un ContactFilter2D. NoFilter() reproduce el mismo comportamiento de
    // antes (sin filtrar por capa/profundidad, incluye triggers).
    private static readonly ContactFilter2D filtroEstructuras = CrearFiltroSinFiltrar();
    private static ContactFilter2D CrearFiltroSinFiltrar()
    {
        return ContactFilter2D.noFilter;
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

    // Histeresis de flipX basada en TIEMPO: el NPC debe querer el mismo flip
    // CONSECUTIVAMENTE durante COOLDOWN_FLIP segundos antes de que se aplique.
    // Si en cualquier momento el deseo cambia de sentido, el contador se resetea.
    //
    // Guard de frame: EnceradorAI llama UpdateAnimator dos veces por frame
    // (una via base.Update, otra al final de su propio Update). Sin el guard,
    // Time.deltaTime se acumularía el doble de rápido para Enceradores.
    private bool  flipDeseado       = false;
    private float timerCambioFlip   = 0f;
    private int   ultimoFrameFlip   = -1;
    private const float COOLDOWN_FLIP = 1.5f; // segundos de deseo continuo antes de espejar

    // Debounce de dirección: la nueva dirección debe mantenerse estable al menos
    // FRAMES_PARA_CONFIRMAR frames antes de actualizar 'facing'.
    // Umbral de "cambio inmediato" subido de 90° a 135°: cambios de lado horizontales
    // (exactamente 90°) ahora pasan por debounce en lugar de aplicarse instantáneamente,
    // eliminando la principal fuente de oscilación ante fuerzas de separación.
    // Solo los giros de casi U-turn (≥135°) se siguen aplicando al instante.
    private Vector2 pendingFacing       = Vector2.zero;
    private int     pendingFacingFrames = 0;
    private const int   FRAMES_PARA_CONFIRMAR  = 6;    // ~0.1s a 60 fps, más estable
    private const float ANGULO_CAMBIO_INMEDIATO = 135f; // era 90°

    // Empuje externo temporal (ej: onda de choque del AntiDisturbios).
    // Tiene prioridad absoluta sobre moveDir durante timerEmpuje segundos.
    private Vector2 empujeExterno    = Vector2.zero;
    private float   timerEmpuje      = 0f;
    private float   empujeVelocidad  = 8f; // unidades/s del empuje (mucho más que el walk normal)

    // Referencia cacheada al componente de Coraza (solo AntiDisturbios la tiene).
    // Se usa en FixedUpdate para reducir la velocidad mientras la coraza está activa.
    protected EnemyCoraza coraza;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();
        status = GetComponent<EnemyStatusEffects>();
        coraza = GetComponent<EnemyCoraza>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        ultimaPosicionRevisada = transform.position;
    }

    // Llamado por EnemyCoraza.DispararOndaChoque() para que el NPC se
    // desplace en la dirección del empuje durante un instante breve.
    // Es más confiable que rb.AddForce porque MovePosition tiene prioridad
    // sobre las fuerzas aplicadas a Rigidbodies 2D con Interpolate.
    public void RecibirEmpuje(Vector2 dir, float duracion, float velocidad = 9f)
    {
        empujeExterno   = dir.normalized;
        timerEmpuje     = duracion;
        empujeVelocidad = velocidad;
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
    private const float tiempoMaximoAtascado = 1.5f;

    protected void RevisarAtasco(float dt)
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

    // Candidatas de dirección para el ray-scan (8 ejes cardinales/diagonales)
    private static readonly Vector2[] DIRS_DESTRABAR = {
        Vector2.right, Vector2.left, Vector2.up, Vector2.down,
        new Vector2( 1f,  1f) * 0.7071f, new Vector2(-1f,  1f) * 0.7071f,
        new Vector2( 1f, -1f) * 0.7071f, new Vector2(-1f, -1f) * 0.7071f
    };

    void Destrabar()
    {
        // Elige la dirección con más espacio libre (raycast de 2 unidades en cada eje).
        // Si ningún ray llega a 2 unidades libres, usa el que llega más lejos.
        // Teleporta directamente (transform.position) en vez de rb.MovePosition:
        // así ningún collider intermedio puede bloquear el escape de emergencia.
        Vector2 mejorDir = Random.insideUnitCircle.normalized;
        float mayorEspacio = -1f;

        for (int i = 0; i < DIRS_DESTRABAR.Length; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, DIRS_DESTRABAR[i], 2f);
            float espacio = (hit.collider != null) ? hit.distance : 2f;
            if (espacio > mayorEspacio)
            {
                mayorEspacio = espacio;
                mejorDir     = DIRS_DESTRABAR[i];
            }
        }

        // Empuje de 1.2 unidades en la dirección más despejada
        transform.position = (Vector2)transform.position + mejorDir * 1.2f;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        ultimaPosicionRevisada = transform.position;
        tiempoAtascado = 0f;
    }

    protected virtual void OnEnable() => All.Add(this);
    protected virtual void OnDisable() => All.Remove(this);

    // ── Lógica de movimiento sin UpdateAnimator ──────────────────────────────
    // Separada para que subclases (EnceradorAI) puedan llamar primero la lógica
    // de movimiento, aplicar sus propias correcciones, y luego llamar
    // UpdateAnimator UNA SOLA VEZ con el moveDir definitivo.
    // Devuelve false si el frame fue interceptado (GameOver, stun, empuje)
    // y UpdateAnimator ya fue llamado internamente.
    protected bool ActualizarMovimiento()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            moveDir = Vector2.zero;
            UpdateAnimator(moveDir);
            return false;
        }

        if (health != null && health.IsStunned)
        {
            moveDir = Vector2.zero;
            UpdateAnimator(moveDir);
            return false;
        }

        if (timerEmpuje > 0f)
        {
            timerEmpuje -= Time.deltaTime;
            moveDir = empujeExterno;
            UpdateAnimator(moveDir);
            return false;
        }

        Tick(Time.deltaTime);
        moveDir = AplicarEvasionDeEstructuras(moveDir);
        moveDir = AplicarSeparacionDeNPCs(moveDir);
        if (moveDir.sqrMagnitude < 0.04f) moveDir = Vector2.zero;
        return true; // el llamador debe invocar UpdateAnimator y RevisarAtasco
    }

    protected virtual void Update()
    {
        if (!ActualizarMovimiento()) return;
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
        // Radio de sondeo ligeramente mayor al mínimo para que el NPC detecte
        // el obstáculo con anticipación y empiece a deslizarse antes de chocar.
        float radioSondeo = distanciaMinimaEstructuras * 1.4f;
        int n = Physics2D.OverlapCircle(transform.position, radioSondeo, filtroEstructuras, bufferEstructuras);
        if (n <= 0) return deseado;

        Vector2 empuje = Vector2.zero;
        bool hayObstaculo = false;
        for (int i = 0; i < n; i++)
        {
            Collider2D col = bufferEstructuras[i];
            if (!ObstacleUtils.EsObstaculoSolido(col)) continue;

            Vector2 puntoCercano = col.ClosestPoint(transform.position);
            Vector2 fuera = (Vector2)transform.position - puntoCercano;
            float dist = fuera.magnitude;
            if (dist < 0.0001f) continue;

            float fuerza = 1f - Mathf.Clamp01(dist / radioSondeo);
            empuje += fuera.normalized * fuerza;
            hayObstaculo = true;
        }

        if (!hayObstaculo || empuje.sqrMagnitude < 0.0001f) return deseado;

        // ── Wall-sliding ────────────────────────────────────────────────────
        // En vez de simplemente sumar el empuje al vector deseado (lo que puede
        // dejar al NPC presionando la pared si el empuje no es suficientemente
        // fuerte), cancelamos la componente del movimiento que va HACIA la pared
        // y dejamos intacta la componente que desliza a lo LARGO de ella.
        // Resultado: el NPC "resbala" pegado al borde del edificio en vez de
        // quedarse clavado caminando contra él.
        Vector2 normal = empuje.normalized;
        float componenteEntrante = Vector2.Dot(deseado, normal);
        if (componenteEntrante < 0f)
        {
            // Hay movimiento hacia adentro → cancelar esa parte
            deseado -= normal * componenteEntrante;
        }

        // Empuje suave hacia afuera para cerrar la brecha si ya está rozando
        Vector2 resultado = deseado + normal * empuje.magnitude * 0.4f;

        float magnitudDeseada = Mathf.Max(moveDir.magnitude, 0.01f);
        return resultado.sqrMagnitude > 0.0001f
            ? resultado.normalized * magnitudDeseada
            : deseado;
    }

    void FixedUpdate()
    {
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            float speed;
            if (timerEmpuje > 0f)
            {
                // Velocidad fija del empuje externo (onda de choque, etc.)
                // — mucho más rápida que el movimiento normal para que sea visible.
                speed = empujeVelocidad;
            }
            else
            {
                speed = baseMoveSpeed * (status != null ? status.MoveSpeedMultiplier : 1f);
                // La coraza del AntiDisturbios lo ralentiza: mientras está activa se mueve
                // muy lento (se "endurece"), lo que refuerza el feedback visual del estado.
                if (coraza != null && coraza.EstaActiva) speed *= 0.2f;
            }
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
    // Es el ÚNICO camino de flip inmediato: cuando el NPC apunta deliberadamente
    // a su objetivo el sprite debe mirarlo ya, sin espera de tiempo.
    protected void FaceDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        facing = SnapTo8(dir);
        if (sr != null && facing.x != 0)
        {
            bool flip = facing.x < 0;
            // Sincronizar flipDeseado: si no lo hacemos, AplicarFlip seguiría
            // acumulando timer en el sentido contrario al que FaceDirection impone,
            // y eventualmente lo revertiría (bug de conflicto con EnemyPistolero).
            flipDeseado     = flip;
            timerCambioFlip = 0f;
            if (sr.flipX != flip)
                sr.flipX = flip;
        }
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
            // ── Debounce de dirección ────────────────────────────────────────────
            // SnapTo8 discretiza en 8 sectores de 45°. En las zonas límite entre
            // dos sectores adyacentes, pequeñas oscilaciones del vector (por fuerzas
            // de separación / evasión) hacen que el snap alterne entre dos valores
            // consecutivos frame a frame, generando flickering en el animator y el
            // sprite. El debounce exige que la nueva dirección sea estable durante
            // FRAMES_PARA_CONFIRMAR frames antes de actualizar 'facing'.
            // Excepción: giros ≥90° (cambios reales de dirección) se aplican ya.
            Vector2 newSnap = SnapTo8(dir);
            if (newSnap != facing)
            {
                float angleDelta = facing != Vector2.zero ? Vector2.Angle(newSnap, facing) : 180f;
                bool cambioGrande = angleDelta >= ANGULO_CAMBIO_INMEDIATO;

                if (cambioGrande)
                {
                    // Giro brusco → aplicar de inmediato y resetear candidato
                    facing             = newSnap;
                    pendingFacing      = Vector2.zero;
                    pendingFacingFrames = 0;
                }
                else if (newSnap == pendingFacing)
                {
                    // Mismo candidato de antes: acumular frames
                    pendingFacingFrames++;
                    if (pendingFacingFrames >= FRAMES_PARA_CONFIRMAR)
                    {
                        facing             = newSnap;
                        pendingFacing      = Vector2.zero;
                        pendingFacingFrames = 0;
                    }
                    // Todavía en espera: no actualizar facing
                }
                else
                {
                    // Nuevo candidato distinto al anterior: reiniciar cuenta
                    pendingFacing       = newSnap;
                    pendingFacingFrames = 1;
                }
            }
            else
            {
                // Misma dirección que facing actual: resetear candidato
                pendingFacing       = Vector2.zero;
                pendingFacingFrames = 0;
            }
            // ────────────────────────────────────────────────────────────────────

            if (facing == Vector2.down) { animator.SetBool("isWalkingDown", true); AplicarFlip(false); }
            else if (facing == Vector2.up) { animator.SetBool("isWalkingUp", true); AplicarFlip(false); }
            else if (facing.x > 0)
            {
                AplicarFlip(false);
                if (facing.y > 0) animator.SetBool("isWalkingDiagonalUp", true);
                else if (facing.y < 0) animator.SetBool("isWalkingDiagonalDown", true);
                else animator.SetBool("isWalkingRight", true);
            }
            else if (facing.x < 0)
            {
                AplicarFlip(true);
                if (facing.y > 0) animator.SetBool("isWalkingDiagonalUp", true);
                else if (facing.y < 0) animator.SetBool("isWalkingDiagonalDown", true);
                else animator.SetBool("isWalkingRight", true);
            }
        }
        else
        {
            // Quieto: no cambiar el flip (evita flickering al cruzar el deadzone).
            // FaceDirection() es el único que actualiza el flip cuando el NPC apunta.
            pendingFacing       = Vector2.zero;
            pendingFacingFrames = 0;
        }

        SetFacingParams(facing);
    }

    // Aplica el flip con histeresis de TIEMPO: el NPC debe querer el mismo flip
    // durante COOLDOWN_FLIP segundos seguidos antes de que sr.flipX cambie.
    // Si el deseo oscila (izq→der→izq...), el timer se resetea y nunca llega.
    //
    // IMPORTANTE: nunca hace flip inmediato (eso es responsabilidad de FaceDirection).
    // Guard de frame: evita que el doble UpdateAnimator del EnceradorAI
    // acumule Time.deltaTime dos veces en el mismo frame.
    private void AplicarFlip(bool quiereFlip)
    {
        // Guard: solo procesar una vez por frame
        if (Time.frameCount == ultimoFrameFlip) return;
        ultimoFrameFlip = Time.frameCount;

        // Sin cambio: resetear timer (el flip actual ya es el correcto)
        if (quiereFlip == sr.flipX) { timerCambioFlip = 0f; return; }

        // El deseo cambió de sentido: reiniciar timer
        if (quiereFlip != flipDeseado)
        {
            flipDeseado     = quiereFlip;
            timerCambioFlip = 0f;
            return;
        }

        // Mismo sentido que antes: acumular
        timerCambioFlip += Time.deltaTime;
        if (timerCambioFlip >= COOLDOWN_FLIP)
        {
            sr.flipX        = quiereFlip;
            timerCambioFlip = 0f;
        }
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

    // Empuja al NPC lejos de otros NPCs aliados demasiado cercanos.
    // El Anti Disturbios puede poner radioSeparacionNPCs=0 para desactivarlo
    // y mantener la formación de escudo.
    Vector2 AplicarSeparacionDeNPCs(Vector2 deseado)
    {
        if (radioSeparacionNPCs <= 0f) return deseado;

        int n = Physics2D.OverlapCircle(transform.position, radioSeparacionNPCs,
                                        filtroEstructuras, bufferNPCs);
        if (n <= 0) return deseado;

        Vector2 empuje = Vector2.zero;
        for (int i = 0; i < n; i++)
        {
            Collider2D col = bufferNPCs[i];
            if (col == null || col.gameObject == gameObject) continue;

            // Solo considerar otros NPCs, no al jugador ni estructuras
            EnemyAI otroNPC = col.GetComponent<EnemyAI>() ?? col.GetComponentInParent<EnemyAI>();
            if (otroNPC == null || otroNPC == this) continue;

            Vector2 fuera = (Vector2)transform.position - (Vector2)col.bounds.center;
            float dist = fuera.magnitude;
            if (dist < 0.0001f) { empuje += Random.insideUnitCircle.normalized * 0.5f; continue; }

            // Fuerza inversamente proporcional a la distancia (más cerca, más empuje)
            float intensidad = (1f - Mathf.Clamp01(dist / radioSeparacionNPCs)) * 1.5f;
            empuje += fuera.normalized * intensidad;
        }

        if (empuje.sqrMagnitude < 0.0001f) return deseado;

        // La separación solo debe DESVIAR lateralmente al NPC, nunca hacerlo
        // retroceder: si el empuje tiene componente en la dirección contraria al
        // deseado, se cancela esa parte y solo queda la desviación lateral.
        // Así el NPC siempre avanza hacia el jugador aunque esquive a sus aliados.
        if (deseado.sqrMagnitude > 0.0001f)
        {
            Vector2 dirDeseada = deseado.normalized;
            float retroceso = Vector2.Dot(empuje, -dirDeseada);
            if (retroceso > 0f)
                empuje += dirDeseada * retroceso; // neutralizar componente de retroceso
        }

        Vector2 resultado = deseado + empuje * 0.6f; // escalar el desvío lateral al 60%
        float magnitudDeseada = Mathf.Max(deseado.magnitude, 0.01f);
        return resultado.sqrMagnitude > 0.0001f ? resultado.normalized * magnitudDeseada : deseado;
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
