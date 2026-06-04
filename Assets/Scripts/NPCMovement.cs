using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    private Animator animator;
    private Camera mainCamera;
    public float moveSpeed = 2f;
    
    private float waitTimer = 0f;
    private float moveTimer = 0f;
    private float randomWaitTime = 0f;
    private float randomMoveTime = 0f;
    
    private bool isWaiting = true;
    private Vector3 moveDirection = Vector3.down;
    
    // Límites de la cámara
    private float cameraTop;
    private float cameraBottom;
    private float cameraRight;
    private float cameraLeft;

    void Start()
    {
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        CalculateCameraBounds();
        RandomizeTimers();
    }

    void Update()
    {
        // Verificar si llegó a los límites
        CheckBounds();
        
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            animator.SetBool("isWalkingDown", false);
            animator.SetBool("isWalkingUp", false);
            
            if (waitTimer >= randomWaitTime)
            {
                isWaiting = false;
                moveTimer = 0f;
                ChooseRandomDirection();
                RandomizeTimers();
            }
        }
        else
        {
            moveTimer += Time.deltaTime;
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
            
            if (moveTimer >= randomMoveTime)
            {
                isWaiting = true;
                waitTimer = 0f;
                RandomizeTimers();
            }
        }
    }

    // Función pública para cuando es golpeado
    public void TakeDamage()
    {
        animator.SetTrigger("OnHit");
    }

    void CalculateCameraBounds()
    {
        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        
        Vector3 cameraPos = mainCamera.transform.position;
        
        cameraTop = cameraPos.y + mainCamera.orthographicSize;
        cameraBottom = cameraPos.y - mainCamera.orthographicSize;
        cameraRight = cameraPos.x + cameraWidth / 2f;
        cameraLeft = cameraPos.x - cameraWidth / 2f;
    }

    void CheckBounds()
    {
        float margin = 0.5f;
        
        if (transform.position.y >= cameraTop - margin && moveDirection == Vector3.up)
        {
            moveDirection = Vector3.down;
            ChangeAnimation();
        }
        else if (transform.position.y <= cameraBottom + margin && moveDirection == Vector3.down)
        {
            moveDirection = Vector3.up;
            ChangeAnimation();
        }
        
        if (transform.position.x >= cameraRight - margin && moveDirection == Vector3.right)
        {
            moveDirection = Vector3.left;
            ChangeAnimation();
        }
        else if (transform.position.x <= cameraLeft + margin && moveDirection == Vector3.left)
        {
            moveDirection = Vector3.right;
            ChangeAnimation();
        }
    }

    void ChangeAnimation()
    {
        if (moveDirection == Vector3.down)
        {
            animator.SetBool("isWalkingDown", true);
            animator.SetBool("isWalkingUp", false);
        }
        else if (moveDirection == Vector3.up)
        {
            animator.SetBool("isWalkingUp", true);
            animator.SetBool("isWalkingDown", false);
        }
    }

    void ChooseRandomDirection()
    {
        int randomDir = Random.Range(0, 2);
        
        if (randomDir == 0)
        {
            moveDirection = Vector3.down;
            animator.SetBool("isWalkingDown", true);
            animator.SetBool("isWalkingUp", false);
        }
        else
        {
            moveDirection = Vector3.up;
            animator.SetBool("isWalkingUp", true);
            animator.SetBool("isWalkingDown", false);
        }
    }

    void RandomizeTimers()
    {
        randomWaitTime = Random.Range(1f, 3f);
        randomMoveTime = Random.Range(2f, 4f);
    }
}