using UnityEngine;
using UnityEditor;

// Corre esto UNA sola vez desde el menú: Paint-It-Black → Organizar Jerarquía
// Crea carpetas (GameObjects vacíos) y mueve cada objeto al grupo correcto.
// Después de correrlo podés borrar este script si querés.
public class OrganizarJerarquia
{
    [MenuItem("Paint-It-Black/Organizar Jerarquía")]
    static void Organizar()
    {
        // --- MANAGERS ---
        var managers = Carpeta("--- MANAGERS ---");
        Mover("GameManager",            managers);
        Mover("EnemySpawner",           managers);
        Mover("ammoManager",            managers);
        Mover("EventSystem",            managers);
        Mover("CombatEndTrigger",       managers);
        Mover("DialogManagerCromagus",  managers);
        Mover("dialogmanagerGomez",     managers);
        Mover("Global Light 2D",        managers);

        // --- CAMARA ---
        var camara = Carpeta("--- CAMARA ---");
        Mover("Main Camera", camara);

        // --- PERSONAJES ---
        var personajes = Carpeta("--- PERSONAJES ---");
        Mover("frottnguy_0",  personajes);
        Mover("cromagustin",  personajes);
        Mover("gomez1",       personajes);

        // --- MAPA ---
        var mapa = Carpeta("--- MAPA ---");

        var fondo = Carpeta("Fondo", mapa);
        Mover("Fondo",   fondo);
        Mover("Fondo 2", fondo);
        Mover("pixellab-the-green-of-the-floor-is-a-li-1782327036156_0", fondo);
        Mover("eefsdfsdf_0", fondo);

        var estructuras = Carpeta("Estructuras", mapa);
        Mover("cascada (2)_0", estructuras);
        Mover("casa_0 (1)",    estructuras);
        Mover("casa (1)_0",    estructuras);
        Mover("casa (2)_0",    estructuras);
        // Árboles y arbustos — los movemos por prefijo
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.transform.parent != null) continue;
            string n = go.name;
            if (n.StartsWith("arbol1_0") || n.StartsWith("arbbbb_0"))
                go.transform.SetParent(estructuras.transform, true);
        }

        var collideres = Carpeta("Collideres", mapa);
        Mover("coso_0",        collideres);
        Mover("coso_0 (1)",    collideres);
        Mover("entryStart",    collideres);
        Mover("EntryEnd",      collideres);
        Mover("EntryEnd (1)",  collideres);
        Mover("untitled (4)_0", collideres);
        Mover("untitled (5)_0", collideres);
        Mover("untitled (6)_0", collideres);

        // --- UI ---
        var ui = Carpeta("--- UI ---");
        Mover("Canvas",     ui);
        Mover("MenuPausa",  ui);
        Mover("hudCombate",     ui);
        Mover("hudExploracion", ui);
        Mover("ammoHud",        ui);
        Mover("vidaHud",        ui);
        Mover("marcoVida",      ui);
        Mover("RellenoVida",    ui);
        Mover("weaponhud",      ui);
        Mover("marcoArmas",     ui);
        Mover("armaIcon",       ui);
        Mover("gota1",          ui);
        Mover("gota2",          ui);
        Mover("GameOverPanel",  ui);
        Mover("VictoryPanel",   ui);
        Mover("Logo",           ui);
        Mover("dialogpanelCromagus", ui);
        Mover("dialogpanelGomez",    ui);
        Mover("dialogtext",          ui);
        Mover("outline",             ui);
        Mover("Square",              ui);

        // Marcar la escena como modificada para que Unity pida guardar
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("✅ Jerarquía organizada. Guardá la escena con Ctrl+S.");
    }

    // Crea un GameObject vacío con ese nombre (opcionalmente hijo de un padre)
    static GameObject Carpeta(string nombre, GameObject padre = null)
    {
        // Si ya existe, reutilizarlo
        var existing = GameObject.Find(nombre);
        if (existing != null) return existing;

        var go = new GameObject(nombre);
        if (padre != null)
            go.transform.SetParent(padre.transform, false);
        return go;
    }

    // Mueve el primer GameObject con ese nombre exacto al padre indicado
    static void Mover(string nombre, GameObject padre)
    {
        var go = GameObject.Find(nombre);
        if (go == null) return;
        // Solo mover si está en la raíz (evita mover hijos que coincidan de nombre)
        if (go.transform.parent != null) return;
        go.transform.SetParent(padre.transform, true);
    }
}
