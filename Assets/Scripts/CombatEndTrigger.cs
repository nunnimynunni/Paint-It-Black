using System.Collections;
using UnityEngine;

// ============================================================
// CONTEXTO PARA DESARROLLADORES
// ============================================================
// Este script maneja lo que pasa al terminar la etapa de combate + puzzle
// (Etapa 3 del GDD: el puzzle ya se resolvió y ya no quedan enemigos).
//
// CÓMO USARLO:
// EnemySpawner.CheckVictory() llama esto solo:
//
//     CombatEndTrigger.instance.OnCombatEnd();
//
// Esto va a:
// 1. Activar a Cromagustin (que estaba desactivado en la escena)
// 2. Hacerlo ENTRAR CORRIENDO DESDE LA IZQUIERDA hacia el centro (GDD: "se
//    acerca corriendo desde la izquierda de la pantalla")
// 3. Detener la animación cuando llega a destino
// 4. Cromagustin queda interactuable para el jugador (via GomezInteraction)
//
// SETUP EN UNITY (opcional):
// - Se puede crear un GameObject vacío "EntryStart" bien a la izquierda,
//   fuera de cámara, y asignarlo en el Inspector. Si NO se asigna nada,
//   este script calcula solo una posición a la izquierda de la cámara
//   actual (ComputePosicionIzquierdaFueraDeCamara), así que funciona igual
//   sin tocar el Editor.
// - entryEnd: si no se asigna, Cromagustin se queda donde ya estaba puesto
//   en la escena.
// ============================================================

public class CombatEndTrigger : MonoBehaviour
{
    public static CombatEndTrigger instance;

    [Header("Cromagustin")]
    public GameObject cromagustin;

    [Tooltip("Posición desde donde entra Cromagustin. Si se deja vacío, se calcula sola a la izquierda y fuera de cámara (GDD: entra corriendo desde la izquierda).")]
    public Transform entryStart;
    [Tooltip("Posición final donde Cromagustin se detiene. Si se deja vacío, usa la posición que ya tenía en la escena.")]
    public Transform entryEnd;

    [Tooltip("Velocidad de entrada")]
    public float entrySpeed = 3f;

    [Tooltip("Sprite de Cromagustin mirando de frente (cuando se detiene)")]
    public Sprite idleSprite;

    private Animator animator;

    // Bug de playtest: "cromagustin entra super rápido en loop". Pasaba
    // porque EnemySpawner podía llamar OnCombatEnd() más de una vez (la
    // condición de victoria se revisa en cada muerte de NPC, y una vez
    // cumplida sigue siendo cumplida en revisiones posteriores), así que
    // cada llamada reiniciaba la posición y largaba OTRA corutina MoveIn en
    // paralelo con la anterior: varias corutinas tirando de la posición a
    // la vez = el "loop" rapidísimo que describía el feedback. Con este
    // flag, OnCombatEnd() ya solo hace efecto la primera vez.
    private bool yaEntro = false;
    private Coroutine entradaEnCurso;

    void Awake()
    {
        instance = this;
    }

    // ============================================================
    // LLAMAR ESTE MÉTODO AL TERMINAR LA ÚLTIMA OLEADA + EL PUZZLE:
    // CombatEndTrigger.instance.OnCombatEnd();
    // ============================================================
    public void OnCombatEnd()
    {
        if (cromagustin == null) return;
        if (yaEntro) return; // ya entró antes: ignorar llamadas repetidas
        yaEntro = true;

        Vector3 posInicial = entryStart != null ? entryStart.position : ComputePosicionIzquierdaFueraDeCamara();
        Vector3 posFinal = entryEnd != null ? entryEnd.position : cromagustin.transform.position;

        cromagustin.SetActive(true);
        cromagustin.transform.position = posInicial;
        animator = cromagustin.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetTrigger("WalkNorth");
        }

        if (entradaEnCurso != null) StopCoroutine(entradaEnCurso);
        entradaEnCurso = StartCoroutine(MoveIn(posFinal));
    }

    // Calcula un punto bien a la izquierda, fuera del campo visible de la
    // cámara principal, para que Cromagustin entre corriendo desde ahí sin
    // necesitar un Transform asignado a mano en el Editor.
    Vector3 ComputePosicionIzquierdaFueraDeCamara()
    {
        Camera cam = Camera.main;
        if (cam == null) return cromagustin.transform.position;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Vector3 centro = cam.transform.position;

        float izquierda = centro.x - width / 2f - 2f;
        return new Vector3(izquierda, centro.y, 0f);
    }

    IEnumerator MoveIn(Vector3 destino)
    {
        while (Vector2.Distance(cromagustin.transform.position, destino) > 0.05f)
        {
            cromagustin.transform.position = Vector2.MoveTowards(
                cromagustin.transform.position,
                destino,
                entrySpeed * Time.deltaTime
            );
            yield return null;
        }

        cromagustin.transform.position = destino;

        // Detener animación y mostrar sprite de frente
        if (animator != null) animator.enabled = false;
        if (idleSprite != null)
        {
            SpriteRenderer sr = cromagustin.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = idleSprite;
        }
    }
}
