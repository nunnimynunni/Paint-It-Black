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

    // Se inicializa yOffset en el primer LateUpdate donde los bounds sean válidos
    // (no en Start/Awake) para evitar el caso de WebGL donde los bounds del
    // SpriteRenderer pueden devolver (0,0,0) hasta que el motor gráfico
    // haya renderizado al menos un frame. Si se inicializara en Start() con
    // bounds=(0,0,0), quedaría yOffset = -transform.position.y, y referenceY=0
    // para todos los objetos → sortingOrder=ordenBase constante → Y-sorting roto.
    private bool yOffsetInit = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (sr == null) return;

        // Inicializar yOffset en el primer frame donde los bounds ya sean válidos.
        // Misma fórmula que CalibrarArbolesYSort:
        //   - Personajes (pivot en los pies): bounds.min.y ≈ position.y → yOffset ≈ 0 ✓
        //   - Árboles/arbustos (pivot al centro): yOffset = -mitad de altura ✓
        if (!yOffsetInit)
        {
            Bounds b = sr.bounds;
            if (b.size.y > 0.001f)
            {
                yOffset = b.min.y - transform.position.y;
                yOffsetInit = true;
            }
        }

        float referenceY = transform.position.y + yOffset;
        sr.sortingOrder = ordenBase + Mathf.RoundToInt(-referenceY * escala);
    }
}
