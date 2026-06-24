using UnityEngine;

// ============================================================
// SCRIPT: WeaponCursor
// Feedback de playtest: "mejor saca las imagenes del puntero en el modo
// oleada, dejalo en el minijuego nomas". Antes este script reemplazaba el
// cursor del sistema por el sprite del arma activa durante el modo
// oleada/exploración; ahora ya NO toca el cursor nunca (se deja el cursor
// normal del sistema durante el juego). Se conserva la clase y los métodos
// Suspender()/Reanudar() solo para no romper a PaintCanvasPuzzle, que sigue
// siendo el único lugar donde se ve un cursor custom (el pincel, dentro del
// minijuego): al cerrarse, en vez de devolver el control a este script,
// ahora restaura directamente el cursor por default del sistema.
// ============================================================
public class WeaponCursor : MonoBehaviour
{
    public static WeaponCursor Instance;

    void Awake()
    {
        Instance = this;
    }

    // Se auto-instancia al cargar la escena (mismo criterio que el resto del
    // slice: nada para arrastrar/crear a mano en el Editor).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("WeaponCursor_Runtime");
        go.AddComponent<WeaponCursor>();
    }

    // Ya no hace nada: se deja como no-op para que PaintCanvasPuzzle pueda
    // seguir llamándolo sin necesitar cambios.
    public void Suspender()
    {
    }

    // Restaura el cursor normal del sistema (en vez de reaplicar un sprite
    // de arma, como hacía antes).
    public void Reanudar()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
