using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
// SCRIPT: EstructurasAlFrente
// ============================================================
// Pedido del usuario: "todos los assets de casa arboles arbustos esten por
// delante de todo en la jerarquia asi ni los npcs ni el forastero los pasan
// por delante".
//
// La escena (SampleScene) tiene los sortingOrder de cada SpriteRenderer
// horneados a mano, sin usar Sorting Layers (van de aprox. -100 a 400 entre
// todos los objetos). En vez de editar a mano miles de líneas del .unity
// (lo cual además puede pisarse cada vez que se reordena algo en el
// Editor), este script fuerza en runtime un sortingOrder altísimo a todo
// SpriteRenderer cuyo GameObject sea una casa/árbol/arbusto, así siempre
// quedan dibujados por encima de cualquier NPC o del Forastero (jugador),
// sin importar el orden manual que tengan en la escena.
//
// Se ejecuta automáticamente al cargar cualquier escena (igual esquema que
// MusicManager con SceneManager.sceneLoaded), así sigue funcionando después
// de reiniciar la partida o volver a entrar a SampleScene desde el menú.
// ============================================================
public class EstructurasAlFrente : MonoBehaviour
{
    // Asentado bien por encima del sortingOrder más alto que usa cualquier
    // otro objeto de la escena (jugador/NPCs/proyectiles/UI de mundo, que
    // hoy no pasan de ~400), para garantizar que las estructuras queden
    // siempre por delante sin importar el valor original que tuvieran.
    private const int OFFSET_SORTING_ORDER = 10000;

    // Prefijos de nombre (case-insensitive) de los GameObjects de casas,
    // árboles y arbustos, según las convenciones ya usadas en SampleScene
    // ("casa (1)_0", "casa_0 (1)", "arbol1_0 (N)", "arbbbb_0 (N)").
    private static readonly string[] PREFIJOS_ESTRUCTURAS = { "casa", "arbol", "arbbbb" };

    private static EstructurasAlFrente instancia;

    // DESACTIVADO: Este script forzaba sortingOrder +10000 en casas/árboles,
    // lo que impedía el Y-sorting correcto. Las casas ahora usan YSort igual
    // que los personajes (correr Paint-It-Black → Restaurar Y-Sorting en el
    // Editor para agregar YSort a las casas de la escena).
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        // no-op
    }

    void OnSceneLoaded(Scene escena, LoadSceneMode modo)
    {
        AplicarATodaLaEscena();
    }

    void AplicarATodaLaEscena()
    {
        SpriteRenderer[] todos = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sr in todos)
        {
            if (sr == null) continue;
            if (!EsEstructura(sr.gameObject.name)) continue;

            // Si el objeto tiene YSort, se encarga solo del sorting — no interferir.
            if (sr.GetComponent<YSort>() != null || sr.GetComponentInParent<YSort>() != null) continue;

            // Evita ir sumando el offset de nuevo si esta función se vuelve a
            // llamar sobre un objeto que ya lo tenía aplicado.
            if (sr.sortingOrder >= OFFSET_SORTING_ORDER) continue;

            sr.sortingOrder += OFFSET_SORTING_ORDER;
        }
    }

    static bool EsEstructura(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return false;
        foreach (var prefijo in PREFIJOS_ESTRUCTURAS)
        {
            if (nombre.StartsWith(prefijo, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
