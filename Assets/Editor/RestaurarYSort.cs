using UnityEngine;
using UnityEditor;

// Corre desde: Paint-It-Black → Restaurar Y-Sorting
// Vuelve al estado que funcionaba: YSort dinámico en todos los objetos estáticos
// de la escena (casas, árboles, arbustos, cascada, etc.), excepto fondo y UI.
// Resetea el sortingOrder a 0 para que YSort lo maneje dinámicamente.
public class RestaurarYSort
{
    [MenuItem("Paint-It-Black/Restaurar Y-Sorting")]
    static void Restaurar()
    {
        int count = 0;

        var todos = Object.FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var sr in todos)
        {
            GameObject go = sr.gameObject;

            // Ignorar UI
            if (go.GetComponentInParent<Canvas>() != null) continue;

            // Ignorar jugador y NPCs (ya tienen YSort, no tocar)
            if (go.GetComponent<PlayerHealth>() != null) continue;
            if (go.GetComponentInParent<PlayerHealth>() != null) continue;
            if (go.GetComponent<EnemyHealth>() != null) continue;
            if (go.GetComponentInParent<EnemyHealth>() != null) continue;

            // Ignorar fondo/suelo (siempre atrás)
            if (sr.sortingOrder <= -50) continue;

            // Ignorar Vegetacion Periferia (tiene baked sorting que funciona bien)
            if (EstaDentroDeGrupo(go.transform, "Vegetacion Periferia")) continue;

            // Agregar YSort si no lo tiene
            if (go.GetComponent<YSort>() == null)
                go.AddComponent<YSort>();

            // Resetear sortingOrder para que YSort lo controle
            sr.sortingOrder = 0;
            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"✅ YSort restaurado en {count} objetos. Guardá con Ctrl+S.");
    }

    static bool EstaDentroDeGrupo(Transform t, string grupo)
    {
        Transform actual = t;
        while (actual != null)
        {
            if (actual.name == grupo) return true;
            actual = actual.parent;
        }
        return false;
    }
}
