using UnityEngine;
using UnityEditor;

// ============================================================
// Correr desde: Paint-It-Black → Calibrar YSort Casas
//
// YSort usa (transform.position.y + yOffset) como referencia.
// Para casas, el pivot del sprite YA está en los pies → basta con
// yOffset = 0, que es el default de YSort. Esto garantiza que la
// referencia sea transform.position.y directamente, igual que los
// personajes y NPCs. Este script limpia cualquier yOffset incorrecto
// que haya quedado de calibraciones anteriores (con la fórmula
// anterior basada en bounds.min.y).
// ============================================================
public class CalibrarCasasYSort
{
    private static readonly string[] PREFIJOS_CASAS = { "Casa" };

    [MenuItem("Paint-It-Black/Calibrar YSort Casas")]
    static void Calibrar()
    {
        int calibradas = 0;
        int sinYSort   = 0;

        var renderers = Object.FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var sr in renderers)
        {
            if (sr == null) continue;
            GameObject go = sr.gameObject;

            // Solo objetos cuyo nombre empiece con "Casa"
            if (!EsCasa(go.name)) continue;

            // Ignorar Vegetacion Periferia (tiene baked sorting propio)
            if (EstaDentroDeGrupo(go.transform, "Vegetacion Periferia")) continue;

            YSort ys = go.GetComponent<YSort>();
            if (ys == null)
            {
                sinYSort++;
                Debug.LogWarning($"[CalibrarCasasYSort] '{go.name}' no tiene YSort. " +
                                 "Corré primero Paint-It-Black → Restaurar Y-Sorting.", go);
                continue;
            }

            Bounds b = sr.bounds;
            if (b.size == Vector3.zero) continue;

            // YSort usa transform.position.y + yOffset como base.
            // Para casas (pivot en los pies): yOffset = 0
            // → referenceY = transform.position.y directamente.
            float nuevoOffset = 0f;
            if (Mathf.Approximately(ys.yOffset, nuevoOffset)) continue;

            Undo.RecordObject(ys, "Calibrar yOffset YSort Casa");
            ys.yOffset = nuevoOffset;
            EditorUtility.SetDirty(ys);
            calibradas++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[CalibrarCasasYSort] ✅ {calibradas} casas calibradas. " +
                  $"Sin YSort: {sinYSort}. Guardá con Ctrl+S.");
    }

    static bool EsCasa(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return false;
        foreach (var prefijo in PREFIJOS_CASAS)
            if (nombre.StartsWith(prefijo, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    static bool EstaDentroDeGrupo(Transform t, string grupo)
    {
        Transform actual = t.parent;
        while (actual != null)
        {
            if (actual.name == grupo) return true;
            actual = actual.parent;
        }
        return false;
    }
}
