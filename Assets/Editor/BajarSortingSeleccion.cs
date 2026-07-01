using UnityEngine;
using UnityEditor;

// Corre desde: Paint-It-Black → Bajar Sorting de Selección (Periféricos Norte)
//
// Para los árboles/arbustos del borde NORTE: les saca YSort y les pone un
// sortingOrder fijo de -200. Así SIEMPRE quedan detrás de todo el interior
// (casas con ~250, jugador con ~250) sin importar las posiciones Y.
//
// NO usar en periféricos del sur (esos necesitan YSort normal para tapar al jugador).
public class BajarSortingSeleccion
{
    const int SORT_FIJO_NORTE = 50; // por encima del fondo (~-50) pero debajo del interior (~250)

    [MenuItem("Paint-It-Black/Bajar Sorting de Selección (Periféricos Norte)")]
    static void Bajar()
    {
        int count = 0;

        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                // Quitar YSort si lo tiene (no lo necesitan, no se mueven)
                YSort ys = sr.GetComponent<YSort>();
                if (ys != null) Object.DestroyImmediate(ys);

                sr.sortingOrder = SORT_FIJO_NORTE;
                EditorUtility.SetDirty(sr);
                count++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"✅ {count} periféricos norte → sortingOrder fijo {SORT_FIJO_NORTE}. Guardá con Ctrl+S.");
    }

    [MenuItem("Paint-It-Black/Bajar Sorting de Selección (Periféricos)", true)]
    static bool ValidarSeleccion() => Selection.gameObjects.Length > 0;
}
