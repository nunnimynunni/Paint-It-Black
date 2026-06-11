using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
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
        spriteRenderer = GetComponent<SpriteRenderer>();
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
            animator.SetBool("isWalkingRight", false);
            animator.SetBool("isWalkingDiagonalUp", false);
            animator.SetBool("isWalkingDiagonalDown", false);
            
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
        
        // Limitar en Y
        if (transform.position.y >= cameraTop - margin && moveDirection.y > 0)
        {
            moveDirection = new Vector3(moveDirection.x, -moveDirection.y, 0);
            ChangeAnimation();
        }
        else if (transform.position.y <= cameraBottom + margin && moveDirection.y < 0)
        {
            moveDirection = new Vector3(moveDirection.x, -moveDirection.y, 0);
            ChangeAnimation();
        }
        
        // Limitar en X
        if (transform.position.x >= cameraRight - margin && moveDirection.x > 0)
        {
            moveDirection = new Vector3(-moveDirection.x, moveDirection.y, 0);
            ChangeAnimation();
        }
        else if (transform.position.x <= cameraLeft + margin && moveDirection.x < 0)
        {
            moveDirection = new Vector3(-moveDirection.x, moveDirection.y, 0);
            ChangeAnimation();
        }
    }

    void ChangeAnimation()
    {
        animator.SetBool("isWalkingDown", false);
        animator.SetBool("isWalkingUp", false);
        animator.SetBool("isWalkingRight", false);
        animator.SetBool("isWalkingDiagonalUp", false);
        animator.SetBool("isWalkingDiagonalDown", false);
        
        // Arriba y abajo no flippean
        if (moveDirection == Vector3.down)
        {
            animator.SetBool("isWalkingDown", true);
            spriteRenderer.flipX = false;
        }
        else if (moveDirection == Vector3.up)
        {
            animator.SetBool("isWalkingUp", true);
            spriteRenderer.flipX = false;
        }
        // Solo derecha/diagonales flippean
        else if (moveDirection.x > 0)
        {
            spriteRenderer.flipX = false;
            
            if (moveDirection.y > 0)
            {
                animator.SetBool("isWalkingDiagonalUp", true);
            }
            else if (moveDirection.y < 0)
            {
                animator.SetBool("isWalkingDiagonalDown", true);
            }
            else
            {
                animator.SetBool("isWalkingRight", true);
            }
        }
        else if (moveDirection.x < 0)
        {
            spriteRenderer.flipX = true;
            
            if (moveDirection.y > 0)
            {
                animator.SetBool("isWalkingDiagonalUp", true);
            }
            else if (moveDirection.y < 0)
            {
                animator.SetBool("isWalkingDiagonalDown", true);
            }
            else
            {
                animator.SetBool("isWalkingRight", true);
            }
        }
    }

    void ChooseRandomDirection()
    {
        int randomDir = Random.Range(0, 8);
        
        switch(randomDir)
        {
            case 0: // Arriba
                moveDirection = Vector3.up;
                break;
            case 1: // Abajo
                moveDirection = Vector3.down;
                break;
            case 2: // Derecha
                moveDirection = Vector3.right;
                break;
            case 3: // Izquierda
                moveDirection = Vector3.left;
                break;
            case 4: // Noreste (arriba-derecha)
                moveDirection = new Vector3(1, 1, 0).normalized;
                break;
            case 5: // Noroeste (arriba-izquierda)
                moveDirection = new Vector3(-1, 1, 0).normalized;
                break;
            case 6: // Sureste (abajo-derecha)
                moveDirection = new Vector3(1, -1, 0).normalized;
                break;
            case 7: // Suroeste (abajo-izquierda)
                moveDirection = new Vector3(-1, -1, 0).normalized;
                break;
        }
        
        ChangeAnimation();
    }

    void RandomizeTimers()
    {
        randomWaitTime = Random.Range(1f, 3f);
        randomMoveTime = Random.Range(2f, 4f);
    }
}