using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// ============================================================
// SCRIPT: WeaponCursor
// Muestra una mira de UI que sigue al mouse y se tintea con el
// color de munición activo (igual que las gotas de AmmoHUD).
// El cursor del sistema se oculta mientras el juego está activo.
//
// SETUP: arrastrar el sprite de la mira al campo "spriteMira" en
// el Inspector del GameObject WeaponCursor_Runtime, o asignarlo
// en Project Settings → Player → Default Cursor como fallback.
//
// Suspender()/Reanudar() los sigue usando PaintCanvasPuzzle para
// mostrar/ocultar la mira durante el minijuego.
// ============================================================
public class WeaponCursor : MonoBehaviour
{
    public static WeaponCursor Instance;

    [Tooltip("Sprite de la mira (base blanca, se tintea con el color activo)")]
    public Sprite spriteMira;
    [Tooltip("Tamaño de la mira en pantalla (px)")]
    public float tamanio = 32f;

    private Canvas canvas;
    private Image imagenMira;
    private RectTransform rectMira;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (spriteMira == null) return;
        CrearCursorUI();
        Cursor.visible = false;
    }

    void CrearCursorUI()
    {
        GameObject canvasObj = new GameObject("WeaponCursor_Canvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // por encima de todo

        canvasObj.AddComponent<CanvasScaler>();

        GameObject miraObj = new GameObject("Mira", typeof(RectTransform));
        miraObj.transform.SetParent(canvasObj.transform, false);

        imagenMira = miraObj.AddComponent<Image>();
        imagenMira.sprite = spriteMira;
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

        // Seguir la posición del mouse
        rectMira.anchoredPosition = Mouse.current.position.ReadValue();

        // Tintear con el color de munición activo (igual que AmmoHUD)
        if (WeaponManager.instance != null)
        {
            Color colorMunicion = PaintColorUtils.ToUnityColor(WeaponManager.instance.currentColor);
            imagenMira.color = colorMunicion;
        }
    }

    // Llamado por PaintCanvasPuzzle al abrir el minijuego:
    // muestra el cursor del sistema y oculta la mira de UI.
    public void Suspender()
    {
        Cursor.visible = true;
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    // Llamado por PaintCanvasPuzzle al cerrar el minijuego:
    // oculta el cursor del sistema y vuelve a mostrar la mira.
    public void Reanudar()
    {
        Cursor.visible = false;
        if (canvas != null) canvas.gameObject.SetActive(true);
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
