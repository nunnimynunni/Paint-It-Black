using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimator : MonoBehaviour
{
    Animator anim;
    Rigidbody2D rb;
    public float velocidad = 3f;

    // ============================================================
    // VERTICAL SLICE: Dodge/Roll (Fase 4 del slice), corregido según GDD 3.3.
    // Pensado para no tocar nada del movimiento/animación normal que ya
    // funcionaba: mientras se está esquivando (o en la ventana de recuperación
    // posterior), Update() simplemente no recalcula "movimiento" (los inputs
    // de movimiento se "congelan") y FixedUpdate no pisa la velocidad que
    // pone la corutina DoDodge().
    //
    // GDD 3.3: "Shift + dirección" para esquivar, invulnerable DURANTE la
    // esquiva, y después de esquivar el jugador queda un instante inmóvil y
    // VULNERABLE (ventana de recuperación) antes de poder volver a esquivar.
    // Esto evita espamear la esquiva para volverse invencible todo el tiempo.
    // ============================================================
    [Header("Dodge / Roll")]
    [Tooltip("Multiplicador de velocidad durante la esquiva")]
    public float dodgeSpeedMultiplier = 2.5f;
    [Tooltip("Duración de la esquiva en segundos (invulnerable todo este tiempo)")]
    public float dodgeDuration = 0.25f;
    [Tooltip("Ventana inmóvil y VULNERABLE inmediatamente después de la esquiva, antes de recuperar el control normal (GDD 3.3)")]
    public float dodgeRecoveryDuration = 0.2f;
    [Tooltip("Tiempo de espera entre una esquiva y la siguiente (se cuenta desde que arranca la esquiva, incluye la ventana de recuperación)")]
    public float dodgeCooldown = 0.8f;
    [Tooltip("Nombre del Trigger en el Animator para la animación de esquiva (si no existe, no rompe nada, simplemente no anima)")]
    public string dodgeAnimTrigger = "Dodge";

    private bool isDodging = false;
    private float dodgeCooldownTimer = 0f;
    private Vector2 lastDirection = Vector2.down;

    // GDD 3.3: "el jugador queda momentáneamente inmovilizado [...] impidiendo
    // el uso consecutivo o en cadena del movimiento". Hasta ahora, intentar
    // esquivar durante el cooldown simplemente no hacía nada (el input se
    // descartaba en silencio). spriteRendererPropio se cachea para poder dar
    // un feedback visual breve (flash + achique) cuando eso pasa.
    private SpriteRenderer spriteRendererPropio;
    private bool mostrandoFeedbackCooldown = false;

    Vector2 movimiento;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRendererPropio = GetComponent<SpriteRenderer>();

    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (dodgeCooldownTimer > 0f) dodgeCooldownTimer -= Time.deltaTime;

        // Mientras se esquiva, no reprocesamos el input de movimiento normal:
        // la corutina DoDodge() ya está manejando la velocidad por su cuenta.
        if (isDodging) return;

        movimiento = Vector2.zero;

        if (keyboard.wKey.isPressed) movimiento += Vector2.up;
        if (keyboard.sKey.isPressed) movimiento += Vector2.down;
        if (keyboard.aKey.isPressed) movimiento += Vector2.left;
        if (keyboard.dKey.isPressed) movimiento += Vector2.right;

        if (movimiento.magnitude > 1)
            movimiento = movimiento.normalized;

        bool arriba = keyboard.wKey.isPressed;

        bool abajo = keyboard.sKey.isPressed;
        bool derecha = keyboard.dKey.isPressed;
        bool izquierda = keyboard.aKey.isPressed;

        bool diagAbajoD = abajo && derecha;
        bool diagAbajoI = abajo && izquierda;
        bool diagArribaD = arriba && derecha;
        bool diagArribaI = arriba && izquierda;

        // Movimiento puro (sin diagonal)
        bool isMoving = abajo && !derecha && !izquierda;
        bool yendoArriba = arriba && !derecha && !izquierda;
        bool yendoDerecha = derecha && !arriba && !abajo;
        bool yendoIzq = izquierda && !arriba && !abajo;

        anim.SetBool("isMoving", isMoving);
        anim.SetBool("yendoArriba", yendoArriba);
        anim.SetBool("yendoDerecha", yendoDerecha);
        anim.SetBool("yendoIzq", yendoIzq);
        anim.SetBool("diagonalAbajoD", diagAbajoD || diagAbajoI);
        anim.SetBool("diagonalArribaD", diagArribaD || diagArribaI);

        // Espejo autom�tico en X
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (diagAbajoI || diagArribaI)
            sr.flipX = true;
        else
            sr.flipX = false;

        if (movimiento.sqrMagnitude > 0.01f)
            lastDirection = movimiento;

        // Pedido del usuario: sonido de pisadas en loop mientras camina,
        // se corta apenas se suelta el movimiento.
        if (SfxManager.Instance != null)
        {
            if (movimiento.sqrMagnitude > 0.01f) SfxManager.Instance.IniciarPisadas();
            else SfxManager.Instance.DetenerPisadas();
        }

        bool shiftPresionado = keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame;
        if (shiftPresionado)
        {
            if (dodgeCooldownTimer <= 0f)
                StartCoroutine(DoDodge());
            else
                MostrarFeedbackCooldown(); // intentó esquivar de nuevo demasiado pronto
        }
    }

    // GDD 3.3: feedback visual de "todavía no podés esquivar". Distinto del
    // flash rojo de daño (PlayerHealth) y del outline verde de beneficio
    // activo: un flash gris-azulado breve + un achique sutil del sprite, para
    // que quede claro que el intento de encadenar esquivas fue rechazado.
    void MostrarFeedbackCooldown()
    {
        if (mostrandoFeedbackCooldown) return; // no acumular flashes si mantiene Shift apretado
        if (spriteRendererPropio == null) return;
        StartCoroutine(FlashCooldownRechazado());
    }

    IEnumerator FlashCooldownRechazado()
    {
        mostrandoFeedbackCooldown = true;

        Color colorAnterior = spriteRendererPropio.color;
        Vector3 escalaAnterior = transform.localScale;

        spriteRendererPropio.color = new Color(0.55f, 0.6f, 0.78f, colorAnterior.a);
        transform.localScale = escalaAnterior * 0.92f;

        yield return new WaitForSeconds(0.12f);

        spriteRendererPropio.color = colorAnterior;
        transform.localScale = escalaAnterior;
        mostrandoFeedbackCooldown = false;
    }

    void FixedUpdate()
    {
        // Mientras isDodging == true, DoDodge() es quien controla rb.linearVelocity.
        if (isDodging) return;

        if (rb != null)
            rb.linearVelocity = movimiento * velocidad * VelocidadMovimientoMult;
    }

    // ============================================================
    // Feedback de playtest: "a veces mi personaje queda congelado". Red de
    // seguridad (mismo criterio que ya se usa en EnemyHealth): si este
    // componente o el GameObject se desactivan mientras DoDodge() está a
    // mitad de camino, la corutina se corta sin llegar a la línea final que
    // pone isDodging de nuevo en false, dejando a Update()/FixedUpdate()
    // bloqueando el movimiento para siempre aunque el objeto se reactive.
    // Esto garantiza que, pase lo que pase, isDodging nunca quede pegado en
    // true más allá de la vida del propio componente.
    // ============================================================
    void OnDisable()
    {
        isDodging = false;
    }

    // GDD 3.7: "Mejoras de Habilidades Pasivas: aumento de velocidad de
    // movimiento", otorgada por el puzzle. Neutra (1f) si no se ganó ninguna.
    float VelocidadMovimientoMult => UpgradeSystem.Instance != null ? UpgradeSystem.Instance.PasivaVelocidadMovimientoMultiplier : 1f;

    IEnumerator DoDodge()
    {
        isDodging = true;
        dodgeCooldownTimer = dodgeCooldown;

        // Mientras esquiva, Update() ya no reprocesa "movimiento" (ver arriba),
        // así que el loop de pisadas se cortaría recién en el próximo frame
        // normal; se corta ahora mismo para no dejarlo sonando de fondo.
        if (SfxManager.Instance != null) SfxManager.Instance.DetenerPisadas();

        // Invulnerable SOLO durante la esquiva en sí (no durante la ventana de
        // recuperación posterior: ahí el GDD pide que quede vulnerable).
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.GrantInvulnerability(dodgeDuration + 0.05f);

        if (anim != null) anim.SetTrigger(dodgeAnimTrigger);

        Vector2 dir = lastDirection.sqrMagnitude > 0.01f ? lastDirection.normalized : Vector2.down;

        float t = 0f;
        while (t < dodgeDuration)
        {
            if (rb != null) rb.linearVelocity = dir * velocidad * dodgeSpeedMultiplier;
            t += Time.deltaTime;
            yield return null;
        }

        // ============================================================
        // GDD 3.3: ventana de recuperación. El jugador queda inmóvil (no
        // recibe input de movimiento) y VULNERABLE (ya no tiene la
        // invulnerabilidad de la esquiva) un instante antes de recuperar el
        // control normal. isDodging sigue en true para que Update()/
        // FixedUpdate() no le devuelvan el control de movimiento todavía.
        // ============================================================
        if (rb != null) rb.linearVelocity = Vector2.zero;

        float r = 0f;
        while (r < dodgeRecoveryDuration)
        {
            r += Time.deltaTime;
            yield return null;
        }

        isDodging = false;
    }
}