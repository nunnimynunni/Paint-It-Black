using UnityEngine;
using UnityEditor;

// Corre desde: Paint-It-Black → Hornear Sorting Escena
// Aplica Order in Layer fijo basado en Y a todos los objetos dentro de "Vegetacion Periferia".
public class HornearSortingEscena
{
    const int   ORDEN_BASE = 300;
    const float ESCALA     = 15f;

    [MenuItem("Paint-It-Black/Hornear Sorting Escena")]
    static void Hornear()
    {
        int count = 0;

        var todos = Object.FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var sr in todos)
        {
            GameObject go = sr.gameObject;

            // Ignorar periferia (tiene su propio sorting horneado)
            if (EstaDentroDeGrupo(sr.transform, "Vegetacion Periferia")) continue;

            // Ignorar UI
            if (sr.GetComponentInParent<Canvas>() != null) continue;

            // Ignorar jugador y NPCs (necesitan YSort dinámico porque se mueven)
            if (go.GetComponent<PlayerHealth>() != null) continue;
            if (go.GetComponentInParent<PlayerHealth>() != null) continue;
            if (go.GetComponent<EnemyHealth>() != null) continue;
            if (go.GetComponentInParent<EnemyHealth>() != null) continue;

            // Ignorar fondo/suelo (sortingOrder muy bajo, siempre atrás)
            if (sr.sortingOrder <= -50) continue;

            // Ignorar EstructurasAlFrente
            if (sr.sortingOrder >= 10000) continue;

            // Si tiene YSort (objeto estático que no necesita sorting dinámico), quitarlo
            YSort ys = go.GetComponent<YSort>();
            if (ys != null) Object.DestroyImmediate(ys);

            float refY = sr.transform.position.y;
            sr.sortingOrder = ORDEN_BASE + Mathf.RoundToInt(-refY * ESCALA);
            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"✅ Sorting horneado en {count} objetos. Guardá con Ctrl+S.");
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
