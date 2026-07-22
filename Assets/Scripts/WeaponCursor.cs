using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// ============================================================
// SCRIPT: WeaponCursor
// Gestiona el cursor según el modo de juego:
//   - Exploración (antes/después de oleada): sin cursor.
//   - Oleada activa: punterobullseye de UI, tintado con el color
//     de munición activo (igual que las gotas de AmmoHUD).
//   - Minijuego: cursor del sistema en pincel (lo maneja
//     PaintCanvasPuzzle vía Suspender/Reanudar).
//
// Flujo esperado:
//   GomezInteraction → ActivarModoOleada() al empezar combate.
//   PaintCanvasPuzzle → Suspender() al abrir puzzle, Reanudar() al cerrar.
//   GameManager.SetHudVisible(false) → DesactivarModoOleada() al terminar.
// ============================================================
public class WeaponCursor : MonoBehaviour
{
    public static WeaponCursor Instance;

    [Tooltip("Sprite del bullseye (base blanca, se tintea con el color activo)")]
    public Sprite spriteBullseye;
    [Tooltip("Tamaño del bullseye en pantalla (px)")]
    public float tamanio = 79f;

    private Canvas canvasCursor;
    private Image imagenMira;
    private RectTransform rectMira;

    // Indica si la oleada está activa (para que Reanudar() sepa qué mostrar)
    private bool oleadaActiva = false;

    // Permite que MenuPausa sepa si debe restaurar el bullseye al reanudar
    public bool OleadaActiva => oleadaActiva;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Por defecto: sin cursor (exploración pre-oleada)
        Cursor.visible = false;
    }

    // ── Crea el canvas de UI con el bullseye (solo la primera vez) ───────────
    void AsegurarCursorUI()
    {
        if (canvasCursor != null) return;
        if (spriteBullseye == null) return;

        GameObject canvasObj = new GameObject("WeaponCursor_Canvas");
        canvasObj.transform.SetParent(transform);
        canvasCursor = canvasObj.AddComponent<Canvas>();
        canvasCursor.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasCursor.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();

        GameObject miraObj = new GameObject("Mira", typeof(RectTransform));
        miraObj.transform.SetParent(canvasObj.transform, false);

        imagenMira = miraObj.AddComponent<Image>();
        imagenMira.sprite = spriteBullseye;
        imagenMira.raycastTarget = false;

        rectMira = miraObj.GetComponent<RectTransform>();
        rectMira.sizeDelta = new Vector2(tamanio, tamanio);
        rectMira.anchorMin = Vector2.zero;
        rectMira.anchorMax = Vector2.zero;
        rectMira.pivot = new Vector2(0.5f, 0.5f);
    }

    void Update()
    {
        if (imagenMira == null || rectMira == null) return;
        if (canvasCursor == null || !canvasCursor.gameObject.activeSelf) return;

        // Seguir la posición del mouse
        rectMira.anchoredPosition = Mouse.current.position.ReadValue();

        // Sin tinte: el bullseye se ve siempre como es
        imagenMira.color = Color.white;
    }

    // ── API pública ──────────────────────────────────────────────────────────

    // Llamado por GomezInteraction al arrancar el combate.
    public void ActivarModoOleada()
    {
        oleadaActiva = true;
        Cursor.visible = false;
        AsegurarCursorUI();
        if (canvasCursor != null) canvasCursor.gameObject.SetActive(true);
    }

    // Llamado por GameManager.SetHudVisible(false) al terminar/perder la oleada.
    public void DesactivarModoOleada()
    {
        oleadaActiva = false;
        Cursor.visible = false;
        if (canvasCursor != null) canvasCursor.gameObject.SetActive(false);
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // Llamado por PaintCanvasPuzzle al ABRIR el minijuego:
    // oculta el bullseye y muestra el cursor del sistema (el puzzle pone el pincel).
    public void Suspender()
    {
        Cursor.visible = true;
        if (canvasCursor != null) canvasCursor.gameObject.SetActive(false);
    }

    // Llamado por PaintCanvasPuzzle al CERRAR el minijuego:
    // limpia el cursor del sistema y vuelve al bullseye.
    public void Reanudar()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.visible = false;
        // Asegurar que el canvas existe (puede haberse destruido por algún motivo)
        AsegurarCursorUI();
        // Volver al bullseye siempre que el canvas exista, sin depender de oleadaActiva
        // (el puzzle solo se abre durante la oleada, así que siempre queremos volver al bullseye)
        if (canvasCursor != null)
            canvasCursor.gameObject.SetActive(true);
    }
}
