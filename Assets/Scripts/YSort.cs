using UnityEngine;

// ============================================================
// SCRIPT: YSort
// ============================================================
// Implementa "Y-sorting": los personajes y objetos más abajo en
// la pantalla (Y menor) aparecen delante de los que están más
// arriba. Esto da ilusión de profundidad en un juego 2D top-down.
//
// Fórmula: sortingOrder = base + round(-(posY + yOffset) * escala)
//
// yOffset permite calibrar el punto de referencia por objeto:
//   - Personajes/casas (pivot en los pies): yOffset = 0
//   - Árboles/arbustos (pivot en el centro): yOffset negativo
//     para desplazar la referencia hacia la base del sprite.
//     Se calcula automáticamente con el script de editor
//     Paint-It-Black → Calibrar YSort Árboles y Arbustos.
// ============================================================
public class YSort : MonoBehaviour
{
    [Tooltip("Multiplicador de posición Y. Más alto = separación más fina entre objetos cercanos.")]
    public float escala = 10f;

    [Tooltip("Orden de base. Valor medio para que los objetos puedan quedar tanto delante como detrás según su Y.")]
    public int ordenBase = 300;

    [Tooltip("Desplazamiento vertical del punto de referencia. " +
             "0 = usa transform.position.y (correcto para personajes y objetos con pivot en los pies). " +
             "Valor negativo = baja la referencia hacia la base del sprite " +
             "(para árboles/arbustos con pivot en el centro). " +
             "Se calibra automáticamente con Paint-It-Black → Calibrar YSort Árboles y Arbustos.")]
    public float yOffset = 0f;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (sr == null) return;

        // Base: transform.position.y (correcto para NPCs, jugador y casas,
        // cuyo pivot coincide con los pies o con el centro de masa visual).
        // yOffset desplaza la referencia por objeto:
        //   - Personajes/casas: yOffset=0 (usan transform.position.y directamente)
        //   - Árboles/arbustos: yOffset = bounds.min.y - transform.position.y
        //     (baja la referencia hasta la base del sprite = tronco)
        //     Se calibra con Paint-It-Black → Calibrar YSort Árboles y Arbustos.
        float referenceY = transform.position.y + yOffset;
        sr.sortingOrder = ordenBase + Mathf.RoundToInt(-referenceY * escala);
    }
}
