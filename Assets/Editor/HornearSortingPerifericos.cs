using UnityEngine;
using UnityEditor;

// Corre desde: Paint-It-Black → Hornear Sorting Periféricos
//
// La vegetación del borde no se mueve, así que no necesita YSort dinámico.
// Este script le saca YSort y le aplica un Order in Layer fijo calculado
// una sola vez desde su posición Y (misma fórmula que YSort: 300 - posY * 15).
// Resultado: se ven bien ordenados entre sí sin conflictos de capas.
public class HornearSortingPerifericos
{
    const int ORDEN_BASE = 300;
    const float ESCALA   = 15f;

    [MenuItem("Paint-It-Black/Hornear Sorting Periféricos")]
    static void Hornear()
    {
        int count = 0;
        var todos = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var go in todos)
        {
            string nombre = go.name.ToLower();

            bool esPeriférico = nombre.StartsWith("periferico") ||
                                EstaDentroDeGrupo(go.transform, "Perifericos") ||
                                EstaDentroDeGrupo(go.transform, "Vegetacion Periferia");

            if (!esPeriférico) continue;

            // Quitar YSort si lo tiene
            YSort ys = go.GetComponent<YSort>();
            if (ys != null) Object.DestroyImmediate(ys);

            // Aplicar sort order fijo basado en la posición Y actual
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) continue;

            float refY = sr.bounds.min.y;
            sr.sortingOrder = ORDEN_BASE + Mathf.RoundToInt(-refY * ESCALA);
            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"✅ Sorting horneado en {count} periféricos. Guardá con Ctrl+S.");
    }

    static bool EstaDentroDeGrupo(Transform t, string nombreGrupo)
    {
        Transform actual = t;
        while (actual != null)
        {
            if (actual.name == nombreGrupo) return true;
            actual = actual.parent;
        }
        return false;
    }
}
