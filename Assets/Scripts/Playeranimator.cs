using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal"); // A y D
        float moveY = Input.GetAxisRaw("Vertical");   // W y S
        bool space = Input.GetKey(KeyCode.Space);

        bool isMoving = moveX != 0 || moveY != 0 || space;

        anim.SetBool("isMoving", isMoving);
    }
}