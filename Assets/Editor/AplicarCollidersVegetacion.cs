using UnityEngine;
using UnityEditor;

// Corre desde: Paint-It-Black → Aplicar Colliders Vegetación
// Aplica los valores de BoxCollider2D configurados a mano al resto de
// arbustos y árboles de la escena.
public class AplicarCollidersVegetacion
{
    // ── Valores del Arbusto (configurados a mano por el usuario) ──
    static readonly Vector2 ARBUSTO_OFFSET = new Vector2(0.0033f, -0.1976f);
    static readonly Vector2 ARBUSTO_SIZE   = new Vector2(0.4812f, 0.2546f);

    // ── Arbol Vivo (sprite arbbbb_0) ──
    static readonly Vector2 ARBOL_VIVO_OFFSET = new Vector2(0.0347f, -0.6335f);
    static readonly Vector2 ARBOL_VIVO_SIZE   = new Vector2(0.6519f,  0.2934f);

    // ── Arbol Muerto (sprite arbol1_0) ──
    static readonly Vector2 ARBOL_MUERTO_OFFSET = new Vector2(0.0090f, -0.4762f);
    static readonly Vector2 ARBOL_MUERTO_SIZE   = new Vector2(0.6708f,  0.1874f);

    [MenuItem("Paint-It-Black/Aplicar Colliders Vegetación")]
    static void Aplicar()
    {
        int count = 0;
        var todos = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var go in todos)
        {
            string nombre = go.name.ToLower();

            bool esArbusto     = nombre.StartsWith("arbusto");
            bool esArbolVivo   = nombre.StartsWith("arbol vivo") || nombre.StartsWith("arbbbb");
            bool esArbolMuerto = nombre.StartsWith("arbol muerto") || nombre.StartsWith("arbol1_0") || nombre.StartsWith("arbol");

            if (!esArbusto && !esArbolVivo && !esArbolMuerto) continue;

            // Eliminar todos los colliders existentes antes de agregar el nuevo
            foreach (var c in go.GetComponents<Collider2D>())
                Object.DestroyImmediate(c);

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();

            if (esArbusto)
            {
                col.offset = ARBUSTO_OFFSET;
                col.size   = ARBUSTO_SIZE;
            }
            else if (esArbolVivo)
            {
                col.offset = ARBOL_VIVO_OFFSET;
                col.size   = ARBOL_VIVO_SIZE;
            }
            else
            {
                col.offset = ARBOL_MUERTO_OFFSET;
                col.size   = ARBOL_MUERTO_SIZE;
            }

            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"✅ Colliders aplicados a {count} objetos. Guardá con Ctrl+S.");
    }
}
