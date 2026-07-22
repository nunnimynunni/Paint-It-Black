using UnityEngine;
using System.Collections.Generic;

// ============================================================
// SCRIPT: EnceradorAI
// NPC de apoyo. Merodea por el mapa libremente. En cuanto detecta
// un soldado vivo sin encerarlos, se dirige a él, lo encerarlos (2.5s)
// y vuelve a merodear. El soldado queda marcado ~20s antes de poder
// ser re-encerado. Si no quedan soldados vivos, ataca al jugador.
// ============================================================
public class EnceradorAI : EnemyAI
{
    [Header("Merodeo")]
    [Tooltip("Segundos entre cambios de dirección al merodear")]
    public float tiempoEntreGiros = 2.8f;
    [Tooltip("Factor de velocidad durante el merodeo (0-1 sobre la speed base)")]
    public float factorVelocidadMerodeo = 0.55f;

    [Header("Encerado")]
    [Tooltip("Distancia al soldado para aplicar el encerado (contacto)")]
    public float rangoAplicar = 1.0f;
    [Tooltip("Segundos que un soldado queda marcado como 'ya encerando' (sin ser re-targetizado)")]
    public float duracionMarcado = 20f;
    [Tooltip("Segundos de merodeo libre obligatorio después de un encerado")]
    public float tiempoMerodeoPostEncerado = 4f;

    [Header("Modo melee (sin soldados vivos)")]
    [Tooltip("Daño por golpe cuando ataca directamente al jugador")]
    public int meleeDamage = 8;
    [Tooltip("Radio en el que aplica el golpe")]
    public float meleeRange = 1.1f;
    [Tooltip("Segundos entre golpes")]
    public float meleeCooldown = 1.2f;

    // ──────────────────────────────────────────────
    // Estado interno
    // ──────────────────────────────────────────────
    private enum Estado { Merodeando, YendoASoldado }
    private Estado estado = Estado.Merodeando;

    private EnemyHealth objetivoSoldado = null;
    private Vector2     dirMerodeo     = Vector2.right;
    private float       timerMerodeo   = 0f;
    private float timerBusqueda         = 0f;
    private float timerCooldownBusqueda = 0f;
    private float meleeTimer            = 0f;

    private const float INTERVALO_BUSQUEDA = 0.5f;

    // Tinte celeste suave: indica que el soldado fue encerado
    private static readonly Color COLOR_ENCERANDO_FIN = new Color(0.78f, 0.95f, 1f, 1f);

    // Set global: soldados actualmente siendo dirigidos por algún Encerador
    private static readonly HashSet<EnemyHealth> soldadosOcupados = new HashSet<EnemyHealth>();

    // Diccionario global: soldado → tiempo (Time.time) en que expira el marcado post-encerado.
    // Mientras esté marcado no puede ser re-targetizado por ningún Encerador.
    private static readonly Dictionary<EnemyHealth, float> soldadosEnceranos =
        new Dictionary<EnemyHealth, float>();

    // ──────────────────────────────────────────────
    // Inicialización
    // ──────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        dirMerodeo        = Random.insideUnitCircle.normalized;
        timerMerodeo      = Random.Range(0.5f, tiempoEntreGiros);
        // Primera búsqueda no inmediata: esperar un poco tras el spawn
        timerBusqueda     = Random.Range(1f, 2.5f);
    }

    // ──────────────────────────────────────────────
    // Tick principal
    // ──────────────────────────────────────────────
    protected override void Tick(float dt)
    {
        // Limpiar referencias muertas de los sets globales
        soldadosOcupados.RemoveWhere(e => e == null || !e.IsAlive());
        LimpiarEnceranosCaducados();

        if (timerCooldownBusqueda > 0f) timerCooldownBusqueda -= dt;

        switch (estado)
        {
            case Estado.Merodeando:    TickMerodeando(dt);    break;
            case Estado.YendoASoldado: TickYendoASoldado(dt); break;
        }
    }

    // ──────────────────────────────────────────────
    // MERODEO
    // ──────────────────────────────────────────────
    void TickMerodeando(float dt)
    {
        // Cambiar dirección periódicamente
        timerMerodeo -= dt;
        if (timerMerodeo <= 0f)
        {
            dirMerodeo   = Random.insideUnitCircle.normalized;
            timerMerodeo = tiempoEntreGiros + Random.Range(-0.6f, 0.6f);
        }

        // Buscar candidato (con throttle + cooldown post-encerado)
        timerBusqueda -= dt;
        if (timerBusqueda <= 0f && timerCooldownBusqueda <= 0f)
        {
            timerBusqueda = INTERVALO_BUSQUEDA;

            EnemyHealth candidato = BuscarSoldadoLibre();
            if (candidato != null)
            {
                objetivoSoldado = candidato;
                soldadosOcupados.Add(candidato);
                estado      = Estado.YendoASoldado;
                moveDir     = Vector2.zero;
                return;
            }

            if (!HaySoldadosVivos())
            {
                TickMelee(dt);
                return;
            }
        }

        // Merodear a velocidad reducida
        moveDir = dirMerodeo * factorVelocidadMerodeo;
    }

    // ──────────────────────────────────────────────
    // YENDO AL SOLDADO
    // ──────────────────────────────────────────────
    void TickYendoASoldado(float dt)
    {
        if (ObjetivoInvalido()) { AbandonarObjetivo(); return; }

        Vector2 hacia = (Vector2)objetivoSoldado.transform.position - (Vector2)transform.position;
        if (hacia.magnitude <= rangoAplicar)
        {
            // ── ENCERADO INSTANTÁNEO ──
            // Aplica el tinte y se va de inmediato, sin detenerse junto al soldado.
            soldadosEnceranos[objetivoSoldado] = Time.time + duracionMarcado;
            SetTinteSoldado(COLOR_ENCERANDO_FIN);
            soldadosOcupados.Remove(objetivoSoldado);
            objetivoSoldado       = null;
            estado                = Estado.Merodeando;
            timerCooldownBusqueda = tiempoMerodeoPostEncerado;
            timerMerodeo          = 0f; // elige nueva dirección enseguida
            return;
        }

        moveDir = hacia.normalized;
    }

    // ──────────────────────────────────────────────
    // MELEE: sin soldados, atacar al jugador
    // ──────────────────────────────────────────────
    void TickMelee(float dt)
    {
        if (meleeTimer > 0f) meleeTimer -= dt;
        if (PlayerTransform == null) { moveDir = Vector2.zero; return; }

        Vector2 posEste = transform.position;
        float dist      = Vector2.Distance(posEste, PlayerTransform.position);

        if (dist > meleeRange)
        {
            moveDir = ((Vector2)PlayerTransform.position - posEste).normalized;
        }
        else
        {
            moveDir = Vector2.zero;
            if (meleeTimer <= 0f)
            {
                DealDamageTo(PlayerTransform, meleeDamage);
                meleeTimer = meleeCooldown;
            }
        }
    }

    // ──────────────────────────────────────────────
    // UTILIDADES
    // ──────────────────────────────────────────────

    bool ObjetivoInvalido() =>
        objetivoSoldado == null || !objetivoSoldado.IsAlive();

    void AbandonarObjetivo()
    {
        if (objetivoSoldado != null)
        {
            soldadosOcupados.Remove(objetivoSoldado);
            objetivoSoldado = null;
        }
        estado       = Estado.Merodeando;
        timerMerodeo = 0f;
        moveDir      = Vector2.zero;
    }

    // Soldado libre = vivo + no ocupado por otro Encerador + no marcado como recién encerado
    EnemyHealth BuscarSoldadoLibre()
    {
        EnemyHealth[] todos = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        EnemyHealth mejor   = null;
        float mejorDist     = float.MaxValue;

        foreach (EnemyHealth e in todos)
        {
            if (e == null || !e.IsAlive() || e.gameObject == gameObject) continue;
            if (e.GetComponent<EnceradorAI>() != null) continue;
            if (soldadosOcupados.Contains(e)) continue;
            // Saltar soldados marcados como "ya encerados" (marcado no expirado)
            if (soldadosEnceranos.TryGetValue(e, out float expiry) && Time.time < expiry) continue;

            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < mejorDist) { mejorDist = d; mejor = e; }
        }

        return mejor;
    }

    bool HaySoldadosVivos()
    {
        EnemyHealth[] todos = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth e in todos)
        {
            if (e == null || !e.IsAlive() || e.gameObject == gameObject) continue;
            if (e.GetComponent<EnceradorAI>() != null) continue;
            return true;
        }
        return false;
    }

    void SetTinteSoldado(Color color)
    {
        if (objetivoSoldado == null) return;
        SpriteRenderer sr = objetivoSoldado.GetComponent<SpriteRenderer>();
        if (sr == null) sr = objetivoSoldado.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = color;
    }

    // Eliminar del diccionario global los soldados cuyo marcado ya expiró o que murieron
    static void LimpiarEnceranosCaducados()
    {
        var aEliminar = new System.Collections.Generic.List<EnemyHealth>();
        foreach (var kv in soldadosEnceranos)
        {
            if (kv.Key == null || !kv.Key.IsAlive() || Time.time >= kv.Value)
                aEliminar.Add(kv.Key);
        }
        foreach (var k in aEliminar) soldadosEnceranos.Remove(k);
    }

    protected override void OnDisable()
    {
        if (objetivoSoldado != null)
        {
            soldadosOcupados.Remove(objetivoSoldado);
            objetivoSoldado = null;
        }
    }
}
