using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GomezInteraction : MonoBehaviour
{
    [Header("Outline")]
    public GameObject outlineObject;

    [Header("Diálogo")]
    public DialogManager dialogManager;

    [Header("Salida")]
    [Tooltip("Velocidad a la que Gomez sube al salir")]
    public float exitSpeed = 2f;
    [Tooltip("Nombre del trigger en el Animator para caminar hacia el norte")]
    public string walkNorthTrigger = "WalkNorth";

    private bool playerInRange = false;
    private bool dialogOpen = false;
    private bool exiting = false;
    private Animator animator;

    void Start()
    {
        if (outlineObject != null)
            outlineObject.SetActive(false);

        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (exiting) return;

        if (!playerInRange) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (!dialogOpen)
            {
                dialogOpen = true;
                if (dialogManager != null) dialogManager.OpenDialog();
            }
            else
            {
                bool stillOpen = dialogManager != null && dialogManager.Advance();
                if (!stillOpen)
                {
                    dialogOpen = false;
                    StartExit();
                }
            }
        }
    }

    void StartExit()
    {
        exiting = true;

        // Ocultar outline
        if (outlineObject != null) outlineObject.SetActive(false);

        // Activar animación
        if (animator != null) animator.SetTrigger(walkNorthTrigger);

        // Mover hacia arriba hasta salir de pantalla
        StartCoroutine(MoveOut());
    }

    IEnumerator MoveOut()
    {
        // Gomez sube hasta que está bien lejos de la cámara
        while (true)
        {
            transform.position += Vector3.up * exitSpeed * Time.deltaTime;

            // Destruir cuando está suficientemente lejos
            if (transform.position.y > Camera.main.transform.position.y + 15f)
            {
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }

    bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (exiting) return;
        if (!IsPlayer(other)) return;
        playerInRange = true;
        if (outlineObject != null) outlineObject.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        playerInRange = false;
        if (outlineObject != null) outlineObject.SetActive(false);
        if (dialogOpen)
        {
            dialogOpen = false;
            if (dialogManager != null) dialogManager.CloseDialog();
        }
    }
}
