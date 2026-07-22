using System.Collections;
using UnityEngine;

// ============================================================
// SCRIPT: EnemyCoraza
// Mecánica exclusiva del Anti Disturbios: "Coraza Rebote".
//
// Ciclo de vida:
//   1. ESPERA  — el NPC funciona normalmente. Dura [minEspera, maxEspera] segs.
//   2. ALERTA  — "Pre-coraza": el sprite parpadea levemente (advertencia visual
//                de que la coraza está por activarse). Dura alertaDuracion segs.
//   3. CORAZA  — estado rígido activo: tinte gris, escala levemente aumentada,
//                inmune a efectos de pintura, rebota proyectiles del jugador
//                con un 75 % de probabilidad directamente de vuelta al jugador.
//                Si un NPC se interpone en el camino, recibe el impacto en su lugar.
//                Dura [minCoraza, maxCoraza] segs.
//   Vuelve a ESPERA y repite.
//
// Vuelta de tuerca: "Onda de Choque" post-coraza.
//   Al desactivarse la coraza, el Anti Disturbios libera una pequeña onda de
//   choque radial que empuja a los NPCs aliados cercanos hacia afuera (los
//   dispersa brevemente) y aplica un micro-stun visual (flash blanco) a cualquier
//   NPC de otro tipo que esté demasiado cerca. Esto recompensa al jugador por
//   aguantar la coraza sin hacer daño: los aliados quedan desordenados por un
//   instante, creando ventanas de ataque.
// ============================================================
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyCoraza : MonoBehaviour
{
    [Header("Tiempos (segundos)")]
    [Tooltip("Rango de duración del estado de espera (entre coraza y coraza)")]
    public float minEspera  = 4f;
    public float maxEspera  = 9f;

    [Tooltip("Duración del parpadeo de alerta antes de activarse")]
    public float alertaDuracion = 1.2f;

    [Tooltip("Rango de duración del estado de coraza activa")]
    public float minCoraza  = 2.5f;
    public float maxCoraza  = 5f;

    [Header("Rebote")]
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de rebotar el proyectil hacia el jugador (0-1)")]
    public float chanceRebote = 0.75f;

    [Tooltip("Radio de búsqueda de NPC interpositor para absorber el rebote")]
    public float radioInterposicion = 2.5f;

    [Header("Onda de Choque (post-coraza)")]
    [Tooltip("Radio del empuje radial al desactivarse")]
    public float radioOnda   = 3.5f;
    [Tooltip("Fuerza del empuje de la onda de choque")]
    public float fuerzaOnda  = 6f;

    [Header("Visual")]
    public Color colorCoraza   = new Color(0.72f, 0.72f, 0.72f, 1f); // gris metálico
    public float escalaCoraza  = 1.12f; // leve aumento de tamaño al endurecerse
    public float velParpadeo   = 8f;    // frecuencia del parpadeo de alerta

    // ── Estado público ──────────────────────────────────────────
    public bool  EstaActiva  { get; private set; } = false;
    // Color actual de la coraza (parpadea mientras está activa).
    // EnemyHealth.LateUpdate lo usa para pintar el sprite sin que el sistema
    // de efectos de color lo sobreescriba.
    public Color ColorActual { get; private set; } = Color.white;

    // ── Privados ────────────────────────────────────────────────
    private SpriteRenderer sr;
    private Color colorBase;
    private Vector3 escalaBase;
    private Coroutine cicloCoroutine;
    private EnemyHealth health;

    void Awake()
    {
        sr     = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();
    }

    void Start()
    {
        colorBase  = sr.color;
        escalaBase = transform.localScale;
        cicloCoroutine = StartCoroutine(CicloCoraza());
    }

    void OnDisable()
    {
        // Restaurar visual si el NPC es desactivado o muere durante coraza
        if (sr != null)
        {
            sr.color = colorBase;
            transform.localScale = escalaBase;
        }
        EstaActiva = false;
    }

    // ── Ciclo principal ─────────────────────────────────────────

    IEnumerator CicloCoraza()
    {
        while (true)
        {
            // 1. ESPERA
            float espera = Random.Range(minEspera, maxEspera);
            yield return new WaitForSeconds(espera);

            if (health != null && !health.IsAlive()) yield break;

            // 2. ALERTA — parpadeo de advertencia
            yield return StartCoroutine(FaseAlerta());

            if (health != null && !health.IsAlive()) yield break;

            // 3. CORAZA ACTIVA
            float duracionCoraza = Random.Range(minCoraza, maxCoraza);
            yield return StartCoroutine(FaseCoraza(duracionCoraza));

            if (health != null && !health.IsAlive()) yield break;

            // 4. ONDA DE CHOQUE al salir de coraza
            DispararOndaChoque();
        }
    }

    IEnumerator FaseAlerta()
    {
        float t = 0f;
        while (t < alertaDuracion)
        {
            // Parpadeo entre color base y gris usando seno
            float lerp = (Mathf.Sin(t * velParpadeo) + 1f) * 0.5f;
            sr.color = Color.Lerp(colorBase, colorCoraza, lerp * 0.6f);
            t += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator FaseCoraza(float duracion)
    {
        // Activar
        EstaActiva   = true;
        ColorActual  = colorCoraza;
        transform.localScale = escalaBase * escalaCoraza;

        // Parpadeo gris durante toda la fase de coraza activa.
        // Alterna entre el color coraza gris y un gris más claro para que el
        // jugador perciba que el estado está "vivo" (no es solo un tinte estático).
        float t        = 0f;
        Color corazaOscuro = colorCoraza;                                 // (0.72, 0.72, 0.72)
        Color corazaClaro  = new Color(0.92f, 0.92f, 0.92f, 1f);         // casi blanco
        float velTitilo    = 4f; // ciclos por segundo del parpadeo

        while (t < duracion)
        {
            float lerp   = (Mathf.Sin(t * velTitilo * Mathf.PI * 2f) + 1f) * 0.5f;
            ColorActual  = Color.Lerp(corazaOscuro, corazaClaro, lerp);
            t           += Time.deltaTime;
            yield return null;
        }

        // Desactivar
        EstaActiva  = false;
        ColorActual = colorCoraza;

        // Restaurar color base SOLO si EnemyStatusEffects no tiene un tinte activo
        EnemyStatusEffects eff = GetComponent<EnemyStatusEffects>();
        if (eff != null)
            sr.color = eff.CurrentBaseColor;
        else
            sr.color = colorBase;

        transform.localScale = escalaBase;
    }

    // ── Rebote de proyectil ─────────────────────────────────────

    // Llamado desde Projectile.OnTriggerEnter2D cuando EstaActiva == true.
    // Devuelve true si el proyectil fue procesado (rebotado o absorbido por NPC).
    // El projectile debe destruirse o redirigirse según el valor devuelto.
    public bool IntentarRebote(Projectile proj)
    {
        if (!EstaActiva) return false;

        // 75 % de chance de rebotar
        if (Random.value > chanceRebote) return false;

        // Verificar si hay un NPC interpositor que absorba el golpe
        EnemyHealth interpositor = BuscarInterpositor(proj);
        if (interpositor != null)
        {
            // El NPC interpositor absorbe el proyectil
            interpositor.TakeDamage(proj.damage, proj.colorType);
            return true; // el proyectil se consume
        }

        // Sin interpositor: rebotar hacia el jugador
        if (PlayerHealth.Instance == null) return false;

        Vector2 dirAlJugador = ((Vector2)PlayerHealth.Instance.transform.position
                                - (Vector2)proj.transform.position).normalized;
        proj.RebotrSetDireccion(dirAlJugador);
        proj.MarcarComoRebotado();
        return true; // el proyectil sigue vivo, ahora va al jugador
    }

    // Busca el NPC más cercano que esté en la trayectoria entre este AntiDisturbios
    // y el jugador (dentro del radioInterposicion). Excluye a este mismo NPC.
    EnemyHealth BuscarInterpositor(Projectile proj)
    {
        if (PlayerHealth.Instance == null) return null;

        Vector2 origen  = transform.position;
        Vector2 destino = PlayerHealth.Instance.transform.position;
        Vector2 dir     = (destino - origen).normalized;

        EnemyHealth[] todos = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        EnemyHealth mejor   = null;
        float mejorDot      = 0.3f; // umbral mínimo de alineación con la trayectoria

        foreach (EnemyHealth e in todos)
        {
            if (e == null || e.gameObject == gameObject) continue;
            if (!e.IsAlive()) continue;

            Vector2 haciaEl = ((Vector2)e.transform.position - origen);
            float dist      = haciaEl.magnitude;
            if (dist > radioInterposicion) continue;

            float dot = Vector2.Dot(haciaEl.normalized, dir);
            if (dot > mejorDot)
            {
                mejorDot = dot;
                mejor    = e;
            }
        }

        return mejor;
    }

    // ── Onda de Choque ──────────────────────────────────────────

    void DispararOndaChoque()
    {
        // Visual: anillo expansivo que crece y se desvanece
        StartCoroutine(AnilloExpansivo());

        // Empuje: usa EnemyAI.RecibirEmpuje en vez de rb.AddForce.
        // MovePosition tiene prioridad sobre forces en Rigidbody2D Interpolate,
        // así que AddForce era ignorado por FixedUpdate en el mismo frame.
        Collider2D[] cercanos = Physics2D.OverlapCircleAll(transform.position, radioOnda);
        foreach (Collider2D col in cercanos)
        {
            if (col.gameObject == gameObject) continue;

            EnemyAI ai = col.GetComponent<EnemyAI>() ?? col.GetComponentInParent<EnemyAI>();
            EnemyHealth e = col.GetComponent<EnemyHealth>() ?? col.GetComponentInParent<EnemyHealth>();
            if (e == null || !e.IsAlive()) continue;

            Vector2 dir     = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
            float distancia = Vector2.Distance(transform.position, col.transform.position);
            float falloff   = 1f - Mathf.Clamp01(distancia / radioOnda);
            float duracion  = Mathf.Lerp(0.15f, 0.45f, falloff); // más cerca = empuje más largo

            if (ai != null)
                ai.RecibirEmpuje(dir, duracion);

            // Flash blanco en los NPCs afectados (micro-stun visual)
            SpriteRenderer objetivo = col.GetComponent<SpriteRenderer>()
                                   ?? col.GetComponentInChildren<SpriteRenderer>();
            StartCoroutine(FlashBlanco(objetivo));
        }
    }

    // Crea un anillo con LineRenderer que se expande y desvanece.
    // No requiere ningún asset — se genera 100% por código.
    IEnumerator AnilloExpansivo()
    {
        GameObject anilloObj = new GameObject("OndaChoque_Ring");
        anilloObj.transform.position = transform.position;

        LineRenderer lr = anilloObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;

        // Usar el material estándar de sprites para que el color funcione sin shader custom
        lr.material = new Material(Shader.Find("Sprites/Default"));

        int segmentos = 32;
        lr.positionCount = segmentos;
        lr.startWidth = 0.12f;
        lr.endWidth   = 0.12f;

        float duracion   = 0.35f;
        float t          = 0f;
        Vector3 centro   = transform.position;

        while (t < duracion)
        {
            float progreso   = t / duracion;
            float radio      = Mathf.Lerp(0.3f, radioOnda, progreso);
            float alfa       = Mathf.Lerp(1f, 0f, progreso);
            Color colorAnillo = new Color(1f, 1f, 1f, alfa);
            lr.startColor = colorAnillo;
            lr.endColor   = colorAnillo;

            for (int i = 0; i < segmentos; i++)
            {
                float angulo = (float)i / segmentos * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector3(
                    Mathf.Cos(angulo) * radio,
                    Mathf.Sin(angulo) * radio,
                    0f));
            }

            t += Time.deltaTime;
            yield return null;
        }

        Destroy(anilloObj);
    }

    IEnumerator FlashBlanco(SpriteRenderer objetivo)
    {
        if (objetivo == null) yield break;
        Color original = objetivo.color;
        objetivo.color = Color.white;
        yield return new WaitForSeconds(0.15f);
        if (objetivo != null) objetivo.color = original;
    }
}
