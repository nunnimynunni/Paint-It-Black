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

        // Normalizar para que en diagonal no vaya más rápido
        if (movimiento.magnitude > 1)
            movimiento = movimiento.normalized;

        transform.Translate(movimiento * velocidad * Time.deltaTime);

        // Animaciones
        bool isMoving = keyboard.sKey.isPressed;
        bool yendoDerecha = keyboard.dKey.isPressed && !keyboard.sKey.isPressed;
        bool yendoArriba = keyboard.wKey.isPressed && !keyboard.dKey.isPressed;
        bool yendoIzq = keyboard.aKey.isPressed && !keyboard.wKey.isPressed
                                                     && !keyboard.sKey.isPressed;

        anim.SetBool("isMoving", isMoving);
        anim.SetBool("yendoDerecha", yendoDerecha);
        anim.SetBool("yendoArriba", yendoArriba);
        anim.SetBool("yendoIzq", yendoIzq);
    }
}