using UnityEngine;
using UnityEditor;

// ============================================================
// Correr desde: Paint-It-Black → Calibrar YSort Árboles y Arbustos
//
// YSort usa (transform.position.y + yOffset) como referencia.
// Para personajes, casas y NPCs: yOffset = 0 (el pivot está en los
// pies, transform.position.y ya es correcto).
// Para árboles y arbustos: el pivot puede estar en el centro del
// sprite → transform.position.y queda en la mitad del árbol, lo que
// hace que el "cruce" de sorting sea demasiado alto y el personaje
// siempre aparezca por delante aunque esté visualmente detrás.
// Solución: yOffset = bounds.min.y - transform.position.y → la
// referencia final es bounds.min.y (la base del sprite = tronco).
//
// Solo afecta objetos dentro de los grupos:
//   "Arboles Vivos", "Arboles Muertos", "Arbustos"
// No toca personajes, NPCs, casas, ni Vegetacion Periferia.
// ============================================================
public class CalibrarArbolesYSort
{
    private static readonly string[] GRUPOS_OBJETIVO = {
        "Arboles Vivos",
        "Arboles Muertos",
        "Arbustos"
    };

    [MenuItem("Paint-It-Black/Calibrar YSort Árboles y Arbustos")]
    static void Calibrar()
    {
        int calibrados = 0;
        int sinYSort   = 0;
        int sinSprite  = 0;

        var renderers = Object.FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var sr in renderers)
        {
            if (sr == null) continue;
            GameObject go = sr.gameObject;

            // Solo objetos que estén dentro de uno de los grupos objetivo
            if (!EstaEnGrupo(go.transform)) continue;

            // Necesita YSort para funcionar
            YSort ys = go.GetComponent<YSort>();
            if (ys == null)
            {
                sinYSort++;
                Debug.LogWarning($"[CalibrarArbolesYSort] '{go.name}' está en el grupo pero no tiene YSort. " +
                                 "Corré primero Paint-It-Black → Restaurar Y-Sorting.", go);
                continue;
            }

            // YSort ahora usa sr.bounds.min.y + yOffset como referencia.
            // Para árboles/arbustos, yOffset = 0: se usa bounds.min.y directamente,
            // que es el comportamiento original que funcionaba correctamente.
            // Este script limpia cualquier yOffset incorrecto que haya quedado
            // de calibraciones anteriores.
            Bounds b = sr.bounds;
            if (b.size == Vector3.zero)
            {
                sinSprite++;
                Debug.LogWarning($"[CalibrarArbolesYSort] '{go.name}' tiene bounds vacíos, se saltea.", go);
                continue;
            }

            // YSort usa transform.position.y + yOffset como base.
            // El crossover correcto es el CENTRO del BoxCollider2D del árbol:
            //   - El collider marca el tronco (obstáculo físico real).
            //   - El personaje se detiene en el borde SUR del collider.
            //   - El centro del collider queda justo AL NORTE de donde para el personaje.
            //   - → el personaje aparece delante mientras toca el árbol ✓
            //   - → aparece detrás en cuanto pasa el centro del tronco ✓
            // Fallback si no hay collider sólido: 25 % desde la base del sprite
            // (mejor que el centro del sprite para pivots en el tope).
            Collider2D col = go.GetComponent<Collider2D>();
            float referenceY;
            if (col != null && !col.isTrigger)
            {
                referenceY = col.bounds.center.y;
            }
            else
            {
                referenceY = b.min.y + b.size.y * 0.25f;
            }
            float nuevoOffset = referenceY - go.transform.position.y;
            if (Mathf.Approximately(ys.yOffset, nuevoOffset)) continue;

            Undo.RecordObject(ys, "Calibrar yOffset YSort");
            ys.yOffset = nuevoOffset;
            EditorUtility.SetDirty(ys);
            calibrados++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        Debug.Log($"[CalibrarArbolesYSort] ✅ {calibrados} objetos calibrados. " +
                  $"Sin YSort: {sinYSort}. Sin sprite: {sinSprite}. Guardá con Ctrl+S.");
    }

    static bool EstaEnGrupo(Transform t)
    {
        Transform actual = t.parent;
        while (actual != null)
        {
            foreach (var grupo in GRUPOS_OBJETIVO)
            {
                if (actual.name == grupo) return true;
            }
            actual = actual.parent;
        }
        return false;
    }
}
