using UnityEngine;
using UnityEditor;

// Corre desde: Paint-It-Black → Reparar Árboles Rotos
//
// Busca todos los SpriteRenderers con sortingOrder = -200 (los que quedaron
// rotos por el script anterior) y les restaura YSort con los valores normales.
public class RepararArbolesRotos
{
    [MenuItem("Paint-It-Black/Reparar Árboles Rotos")]
    static void Reparar()
    {
        int count = 0;

        var todos = Object.FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var sr in todos)
        {
            // Solo los que quedaron en -200 por el script roto
            if (sr.sortingOrder != -200) continue;

            GameObject go = sr.gameObject;

            // Quitar YSort viejo si quedó alguno
            YSort ys = go.GetComponent<YSort>();
            if (ys != null) Object.DestroyImmediate(ys);

            // Agregar YSort fresco con valores normales
            YSort nuevoYSort = go.AddComponent<YSort>();
            nuevoYSort.ordenBase = 300;
            nuevoYSort.escala = 10;

            sr.sortingOrder = 0;
            EditorUtility.SetDirty(go);
            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"✅ {count} árboles reparados. Guardá con Ctrl+S.");
    }
}
