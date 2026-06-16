using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimator : MonoBehaviour
{
    Animator anim;
    public float velocidad = 3f;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector3 movimiento = Vector3.zero;

        if (keyboard.wKey.isPressed) movimiento += Vector3.up;
        if (keyboard.sKey.isPressed) movimiento += Vector3.down;
        if (keyboard.aKey.isPressed) movimiento += Vector3.left;
        if (keyboard.dKey.isPressed) movimiento += Vector3.right;

        if (movimiento.magnitude > 1)
            movimiento = movimiento.normalized;

        transform.Translate(movimiento * velocidad * Time.deltaTime);

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

        // Espejo automático en X
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (diagAbajoI || diagArribaI)
            sr.flipX = true;
        else
            sr.flipX = false;
    }
}