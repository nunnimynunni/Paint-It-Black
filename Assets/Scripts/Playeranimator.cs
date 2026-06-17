using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimator : MonoBehaviour
{
    Animator anim;
    Rigidbody2D rb;
    public float velocidad = 3f;

    Vector2 movimiento;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

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
    }

    void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = movimiento * velocidad;
    }
}