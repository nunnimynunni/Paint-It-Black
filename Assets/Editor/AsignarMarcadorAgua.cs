using UnityEngine;
using UnityEditor;

// Corre desde: Paint-It-Black → Asignar Marcador Agua
// Busca todos los GameObjects cuyo nombre contenga "Rio" o "Collider Rio"
// y les agrega el componente MarcadorAgua si no lo tienen ya.
public class AsignarMarcadorAgua
{
    [MenuItem("Paint-It-Black/Asignar Marcador Agua")]
    static void Asignar()
    {
        var todos = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        int count = 0;
        foreach (var go in todos)
        {
            string nombre = go.name.ToLower();
            if (nombre.Contains("rio") || nombre.Contains("collider rio") || nombre.Contains("agua"))
            {
                if (go.GetComponent<MarcadorAgua>() == null)
                {
                    go.AddComponent<MarcadorAgua>();
                    count++;
                }
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"✅ MarcadorAgua agregado a {count} objetos. Guardá con Ctrl+S.");
    }
}
