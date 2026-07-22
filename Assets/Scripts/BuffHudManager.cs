using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
// SCRIPT: BuffHudManager
// Al otorgarse una mejora del puzzle, muestra durante 8 segundos:
//   1. Un recuadro con tipografía pixelada al lado de la vida
//      indicando qué buff se obtuvo.
//   2. Un outline arcoiris en el área del HUD correspondiente:
//      mejoras de arma  → outline en hudCombate/HudCombateVisual
//      mejoras de vida/pasiva → outline en hudExploracion
//
// Se auto-instancia la primera vez que UpgradeSystem lo necesita;
// no hace falta arrastrar nada al Editor.
// ============================================================
public class BuffHudManager : MonoBehaviour
{
    // Auto-instanciado si no existe en la escena
    private static BuffHudManager _instance;
    public static BuffHudManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("BuffHudManager_Runtime");
                _instance = go.AddComponent<BuffHudManager>();
            }
            return _instance;
        }
    }

    [Tooltip("Fuente pixelada para el texto del buff. " +
             "Asignar en el Inspector si se quiere tipografía pixel art. " +
             "Si queda vacío, se usa la fuente del sistema.")]
    public Font fuentePixel;

    // Elementos de UI creados en runtime
    private GameObject panelTexto;
    private Text textoLabel;
    private Image fondoPanel;
    private Image outlineArmas;
    private Image outlineVida;

    private Coroutine rutinaActual;

    // Velocidad del ciclo arcoiris (ciclos por segundo, igual que el outline del jugador)
    const float VEL_ARCOIRIS = 0.5f;
    // Borde en píxeles que sobresale del panel del HUD
    const float GROSOR_OUTLINE = 5f;

    void Awake() => _instance = this;

    // Llamado por UpgradeSystem.
    // esArma: true → outline en el HUD de armas; false → en el de vida
    public void MostrarBuff(string nombreBuff, bool esArma)
    {
        if (rutinaActual != null) StopCoroutine(rutinaActual);
        rutinaActual = StartCoroutine(RutinaMostrar(nombreBuff, esArma));
    }

    IEnumerator RutinaMostrar(string nombre, bool esArma)
    {
        // Construir la UI la primera vez (lazy).
        // Texto: canvas propio con sortingOrder alto.
        // Outlines: búsqueda directa por nombre de GameObject.
        AsegurarTexto();
        AsegurarOutlines();

        // Mostrar recuadro de texto
        if (panelTexto != null)
        {
            panelTexto.SetActive(true);
            if (textoLabel != null) textoLabel.text = nombre;
        }

        // Activar outline del área correspondiente
        Image outline = esArma ? outlineArmas : outlineVida;
        if (outline != null) outline.gameObject.SetActive(true);

        // Animar arcoiris indefinidamente: el banner NO se auto-oculta.
        // PlayerHealth llama OcultarBanner() cuando el outline del jugador
        // termina (por expiración natural o por GameOver), asegurando que
        // el cartel desaparezca exactamente al mismo tiempo que el buff.
        float tiempo = 0f;
        while (true)
        {
            float hue = Mathf.Repeat(tiempo * VEL_ARCOIRIS, 1f);
            Color c = Color.HSVToRGB(hue, 1f, 1f);

            if (outline != null) outline.color = c;

            // Fondo del panel: versión oscura del color activo
            if (fondoPanel != null)
                fondoPanel.color = new Color(c.r * 0.18f, c.g * 0.18f, c.b * 0.18f, 0.90f);

            tiempo += Time.deltaTime;
            yield return null;
        }
    }

    // Oculta el banner y detiene la animación arcoiris.
    // Llamado por PlayerHealth cuando el outline del jugador termina.
    public void OcultarBanner()
    {
        if (rutinaActual != null)
        {
            StopCoroutine(rutinaActual);
            rutinaActual = null;
        }
        if (panelTexto  != null) panelTexto.SetActive(false);
        if (outlineArmas != null) outlineArmas.gameObject.SetActive(false);
        if (outlineVida  != null) outlineVida.gameObject.SetActive(false);
    }

    // ── Construcción del recuadro de texto ───────────────────────────────────

    void AsegurarTexto()
    {
        if (panelTexto != null) return;

        // Canvas exclusivo con sortingOrder alto: garantiza que el panel se
        // vea siempre por encima del HUD, del minijuego y de cualquier otro
        // canvas de la escena, sin depender de buscar el canvas correcto.
        GameObject canvasObj = new GameObject("BuffHUD_OverlayCanvas");
        Canvas buffCanvas = canvasObj.AddComponent<Canvas>();
        buffCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        buffCanvas.sortingOrder = 500;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Intentar alinear con hudExploracion (barra de vida): mismo X, mismo ancho,
        // posicionado justo debajo con un gap de 6px, usando las esquinas del mundo.
        float panelX = 10f;
        float panelY = -100f;  // fallback: claramente debajo de la barra de vida
        float panelW = 240f;
        const float panelH = 52f;
        const float gap    = 10f;

        GameObject vidaObj = GameObject.Find("hudExploracion") ?? GameObject.Find("barraVida");
        if (vidaObj != null)
        {
            RectTransform vrt = vidaObj.GetComponent<RectTransform>();
            if (vrt != null)
            {
                // GetWorldCorners: para un canvas ScreenSpaceOverlay, world = screen pixels
                Vector3[] corners = new Vector3[4];
                vrt.GetWorldCorners(corners);
                // corners[0]=BottomLeft, [1]=TopLeft, [2]=TopRight, [3]=BottomRight
                float leftPx   = corners[0].x;
                float bottomPx = corners[0].y;
                float widthPx  = corners[2].x - corners[0].x;

                // Convertir de screen-pixel a anchoredPosition (anchor top-left = (0,1))
                float screenH = Screen.height;
                panelX = leftPx;
                panelY = (bottomPx - screenH) - gap; // Y negativo desde top
                panelW = Mathf.Max(widthPx, 180f);
            }
        }

        // Panel contenedor: ancla arriba-izquierda, debajo de la barra de vida
        panelTexto = new GameObject("BuffHUD_Panel");
        panelTexto.transform.SetParent(canvasObj.transform, false);

        RectTransform rt = panelTexto.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(panelX, panelY);
        rt.sizeDelta = new Vector2(panelW, panelH);

        fondoPanel = panelTexto.AddComponent<Image>();
        fondoPanel.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

        // Etiqueta de texto
        GameObject textObj = new GameObject("Etiqueta");
        textObj.transform.SetParent(panelTexto.transform, false);

        RectTransform rtT = textObj.AddComponent<RectTransform>();
        rtT.anchorMin = Vector2.zero;
        rtT.anchorMax = Vector2.one;
        rtT.offsetMin = new Vector2(10f, 4f);
        rtT.offsetMax = new Vector2(-10f, -4f);

        textoLabel = textObj.AddComponent<Text>();
        textoLabel.alignment = TextAnchor.MiddleCenter;
        textoLabel.color     = Color.white;

        // Carga la misma fuente pixelada que usa PaintCanvasPuzzle
        Font px = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        textoLabel.font     = px != null ? px
                            : (fuentePixel != null ? fuentePixel
                            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        textoLabel.fontSize = px != null ? 12 : 18; // más grande que antes (era 10/15)

        panelTexto.SetActive(false);
    }

    // ── Construcción de los outlines de HUD ──────────────────────────────────

    void AsegurarOutlines()
    {
        if (outlineArmas == null)
        {
            // HUD de armas: buscar por nombre directo (igual que GameManager/PaintCanvasPuzzle)
            GameObject obj = GameObject.Find("HudCombateVisual") ?? GameObject.Find("hudCombate");
            if (obj != null)
                outlineArmas = CrearOutlineUI(obj.GetComponent<RectTransform>(), "BuffOutline_Armas");
        }

        if (outlineVida == null)
        {
            // HUD de vida: buscar hudExploracion (contiene barra de vida)
            GameObject obj = GameObject.Find("hudExploracion") ?? GameObject.Find("barraVida");
            if (obj != null)
                outlineVida = CrearOutlineUI(obj.GetComponent<RectTransform>(), "BuffOutline_Vida");
        }
    }

    // Crea un Image panel que sobresale GROSOR_OUTLINE px de todos los lados
    // del objetivo, insertado justo antes de él en la jerarquía (queda detrás).
    Image CrearOutlineUI(RectTransform objetivo, string nombre)
    {
        if (objetivo == null) return null;

        GameObject obj = new GameObject(nombre);
        obj.transform.SetParent(objetivo.parent, false);
        // Insertar antes del objetivo → queda detrás de él en el canvas
        obj.transform.SetSiblingIndex(objetivo.GetSiblingIndex());

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = objetivo.anchorMin;
        rt.anchorMax = objetivo.anchorMax;
        rt.pivot     = objetivo.pivot;
        // offsetMin/Max funcionan con cualquier esquema de anclaje (fijo o estirado)
        rt.offsetMin = objetivo.offsetMin - new Vector2(GROSOR_OUTLINE, GROSOR_OUTLINE);
        rt.offsetMax = objetivo.offsetMax + new Vector2(GROSOR_OUTLINE, GROSOR_OUTLINE);

        Image img = obj.AddComponent<Image>();
        img.color         = Color.white;
        img.raycastTarget = false;
        obj.SetActive(false);
        return img;
    }

    // Búsqueda recursiva por nombre en la jerarquía de UI
    static GameObject BuscarEnHierarquia(Transform raiz, string nombre)
    {
        foreach (Transform hijo in raiz)
        {
            if (hijo.name == nombre) return hijo.gameObject;
            GameObject r = BuscarEnHierarquia(hijo, nombre);
            if (r != null) return r;
        }
        return null;
    }
}
