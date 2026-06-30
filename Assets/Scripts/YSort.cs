using UnityEngine;

// ============================================================
// SCRIPT: YSort
// ============================================================
// Implementa "Y-sorting": los personajes y objetos más abajo en
// la pantalla (Y menor) aparecen delante de los que están más
// arriba. Esto da ilusión de profundidad en un juego 2D top-down.
//
// Fórmula: sortingOrder = base + round(-posY * escala)
//
// EstructurasAlFrente usa offset 10000, así que los valores de
// Y-sort (~-500 a +500 para la zona de juego) nunca interfieren
// con casas/árboles, que siempre van por delante de todo.
// ============================================================
public class YSort : MonoBehaviour
{
    [Tooltip("Multiplicador de posición Y. Más alto = separación más fina entre objetos cercanos.")]
    public float escala = 100f;

    [Tooltip("Orden de base sobre el que se suma el offset de Y. Dejalo en 0 para personajes normales.")]
    public int ordenBase = 0;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (sr == null) return;

        // Un Y más bajo (más al frente en pantalla) → sortingOrder más alto → se dibuja encima
        sr.sortingOrder = ordenBase + Mathf.RoundToInt(-transform.position.y * escala);
    }
}
