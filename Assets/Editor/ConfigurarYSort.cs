using UnityEngine;
using UnityEditor;

// Corre desde: Paint-It-Black → Configurar Y-Sorting
//
// Agrega YSort a todos los objetos que necesitan profundidad dinámica:
//   - Personajes en escena (Forastero, Gomez, Cromagustin)
//   - Estructuras del mapa (árboles, casas, cascada, fuente)
//   - Prefabs de NPCs enemigos (SoldadoPorra, SoldadoPistola, SoldadoAntiDisturbios)
//
// NO toca:
//   - Objetos con sortingOrder >= 10000 (EstructurasAlFrente — siempre al frente)
//   - Objetos dentro del grupo Fondo (tiles de suelo — siempre atrás)
//   - UI, cámaras, managers
public class ConfigurarYSort
{
    [MenuItem("Paint-It-Black/Configurar Y-Sorting")]
    static void Configurar()
    {
        int count = 0;

        // ── 1. Objetos en escena ─────────────────────────────────────────────
        var todos = Object.FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var sr in todos)
        {
            GameObject go = sr.gameObject;

            // Saltar si ya tiene YSort
            if (go.GetComponent<YSort>() != null) continue;

            // Saltar UI (Canvas)
            if (go.GetComponentInParent<Canvas>() != null) continue;

            string nombre = go.name.ToLower();

            // Estructuras por nombre — tienen prioridad sobre el grupo Fondo
            // (los árboles/arbustos/casas pueden estar dentro de Piso/Fondo por como fue importado el tileset)
            bool esEstructuraPorNombre = nombre.StartsWith("arbol") ||
                                         nombre.StartsWith("arbbbb") ||
                                         nombre.StartsWith("arbusto") ||
                                         nombre.StartsWith("casa") ||
                                         nombre.StartsWith("cascada") ||
                                         nombre.StartsWith("fuente") ||
                                         nombre.StartsWith("fountain") ||
                                         nombre.StartsWith("periferico");

            // También capturar todo lo que esté dentro de una carpeta "Perifericos"
            if (!esEstructuraPorNombre)
                esEstructuraPorNombre = EstaDentroDeGrupo(go.transform, "Perifericos");

            if (esEstructuraPorNombre)
            {
                go.AddComponent<YSort>();
                sr.sortingOrder = 0;
                count++;
                continue;
            }

            // Para el resto: saltar si está en Fondo (tiles de suelo)
            if (EstaDentroDeGrupo(go.transform, "Fondo")) continue;

            bool esEstructura = EstaDentroDeGrupo(go.transform, "Estructuras");
            bool esPersonaje  = EstaDentroDeGrupo(go.transform, "--- PERSONAJES ---");

            if (!esEstructura && !esPersonaje) continue;

            go.AddComponent<YSort>();
            sr.sortingOrder = 0;
            count++;
        }

        // ── 2. Prefabs de NPCs ───────────────────────────────────────────────
        string[] prefabPaths = new string[]
        {
            "Assets/Assets/NPCs enemigos/Soldado Porra/SoldadoPorra.prefab",
            "Assets/Assets/NPCs enemigos/Soldado Pistola/SoldadoPistola.prefab",
            "Assets/Assets/NPCs enemigos/Soldado Anti Disturbios/SoldadoAntiDisturbios.prefab",
        };

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;
                if (root.GetComponent<YSort>() != null) continue;

                root.AddComponent<YSort>();
                SpriteRenderer prefabSR = root.GetComponent<SpriteRenderer>();
                if (prefabSR != null) prefabSR.sortingOrder = 0;
                count++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"✅ YSort configurado en {count} objetos. Guardá con Ctrl+S.");
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
