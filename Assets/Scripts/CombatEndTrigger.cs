using System.Collections;
using UnityEngine;

// ============================================================
// CONTEXTO PARA DESARROLLADORES
// ============================================================
// Este script maneja lo que pasa cuando termina el combate.
//
// CÓMO USARLO:
// Cuando el sistema de oleadas detecte que no quedan más enemigos,
// llamar este método desde cualquier script:
//
//     CombatEndTrigger.instance.OnCombatEnd();
//
// Esto va a:
// 1. Activar a Cromagustin (que estaba desactivado en la escena)
// 2. Hacerlo entrar caminando desde entryStart hasta entryEnd
// 3. Detener la animación cuando llega a destino
// 4. Cromagustin queda interactuable para el jugador (via GomezInteraction)
//
// SETUP EN UNITY:
// - Crear dos GameObjects vacíos: EntryStart (fuera de pantalla abajo)
//   y EntryEnd (donde Cromagustin se va a detener)
// - Asignarlos en el Inspector junto con el GameObject de Cromagustin
// ============================================================

public class CombatEndTrigger : MonoBehaviour
{
    public static CombatEndTrigger instance;

    [Header("Cromagustin")]
    public GameObject cromagustin;

    [Tooltip("Posicion desde donde entra Cromagustin (abajo de la pantalla, fuera de vista)")]
    public Transform entryStart;
    [Tooltip("Posicion final donde Cromagustin se detiene")]
    public Transform entryEnd;

    [Tooltip("Velocidad de entrada")]
    public float entrySpeed = 2f;

    [Tooltip("Sprite de Cromagustin mirando de frente (cuando se detiene)")]
    public Sprite idleSprite;

    private Animator animator;

    void Awake()
    {
        instance = this;
    }

    // ============================================================
    // LLAMAR ESTE MÉTODO AL TERMINAR LA ÚLTIMA OLEADA:
    // CombatEndTrigger.instance.OnCombatEnd();
    // ============================================================
    public void OnCombatEnd()
    {
        if (cromagustin == null) return;

        cromagustin.SetActive(true);
        cromagustin.transform.position = entryStart.position;
        animator = cromagustin.GetComponent<Animator>();
        if (animator != null) animator.SetTrigger("WalkNorth");

        StartCoroutine(MoveIn());
    }

    IEnumerator MoveIn()
    {
        while (Vector2.Distance(cromagustin.transform.position, entryEnd.position) > 0.05f)
        {
            cromagustin.transform.position = Vector2.MoveTowards(
                cromagustin.transform.position,
                entryEnd.position,
                entrySpeed * Time.deltaTime
            );
            yield return null;
        }

        cromagustin.transform.position = entryEnd.position;

        // Detener animación y mostrar sprite de frente
        if (animator != null) animator.enabled = false;
        if (idleSprite != null)
        {
            SpriteRenderer sr = cromagustin.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = idleSprite;
        }
    }
}
