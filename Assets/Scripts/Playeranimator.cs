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

        if (keyboard.sKey.isPressed)
            movimiento = Vector3.down;

        bool isMoving = movimiento != Vector3.zero;

        transform.Translate(movimiento * velocidad * Time.deltaTime);
        anim.SetBool("isMoving", isMoving);
    }
}