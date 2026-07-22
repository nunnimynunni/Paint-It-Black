using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ============================================================
// SCRIPT: PaintCanvasPuzzle
// FASE 5 del vertical slice (REDISEÑADO): "Copia de Patrón bajo Presión"
// del GDD 3.7, fiel a la descripción real:
//
//   "Aparece un patrón de referencia (una imagen ya pintada con la paleta
//    de colores) y un lienzo en blanco con el mismo tamaño/forma. El
//    jugador debe repintar el lienzo usando la paleta de colores para que
//    quede igual al patrón de referencia, contra un límite de tiempo de
//    60 segundos. UX simplificada estilo MS Paint: elegís un color de la
//    paleta y pintás celdas del lienzo."
//
// Esto reemplaza el diseño anterior (un juego de memoria con teclas 1-6),
// que no coincidía con el GDD. Esta versión:
//   - Usa una grilla de celdas (lienzo) en vez de una secuencia temporal.
//   - Tiene una paleta de colores clickeable (igual a los colores de
//     pintura que ya usan las armas: Red/Blue/Yellow/Green/Purple/Gray).
//   - Tiene un único intento por apertura con un timer visible de 60s.
//   - Victoria = el lienzo coincide exactamente con la referencia (se
//     valida solo, automáticamente, en cuanto coincide TODO).
//   - Derrota = se acaba el tiempo sin lograr la coincidencia exacta.
//
// QUIÉN LO ABRE Y CÓMO SE INTEGRA CON LA OLEADA:
// Lo abre PuzzleStructure cuando el jugador interactúa con la estructura
// marcada en medio de la oleada. PuzzleStructure ya se encarga de pausar
// el EnemySpawner mientras este objeto existe, y de escuchar OnFinished
// para reanudarlo y otorgar progreso si corresponde. Este script NO toca
// el EnemySpawner directamente: solo construye su UI y al terminar invoca
// OnFinished(bool victoria).
//
// CÓMO SE ARMA LA UI: igual que el resto del slice, 100% por código dentro
// del Canvas existente, sin tocar nada del Editor. Las celdas son
// placeholders (Image de color sólido) ya que no hay arte específico para
// este minijuego en el GDD.
// ============================================================
public class PaintCanvasPuzzle : MonoBehaviour
{
    private const int GRID_SIZE = 4; // 4x4 = 16 celdas: suficiente para sentir presión sin ser tedioso
    private const float TIME_LIMIT = 35f;

    private static readonly PaintColor[] paleta =
    {
        PaintColor.Red, PaintColor.Blue, PaintColor.Yellow, PaintColor.Green, PaintColor.Purple, PaintColor.Gray
    };

    // true = victoria (lienzo == referencia), false = derrota/timeout.
    // Devuelve un mensaje en español (la mejora otorgada) para mostrarlo en
    // pantalla antes de cerrar, o null/"" si no hay nada que mostrar.
    public System.Func<bool, string> OnFinished;

    // Se invoca recién cuando esta interfaz termina de cerrarse del todo
    // (justo antes de Destroy), no apenas se resuelve el patrón. Sirve para
    // que quien escuche pueda despausar el juego de fondo exactamente en el
    // instante en que el jugador recupera la vista del mapa, sin la ventana
    // de ~1.4s de vulnerabilidad que había antes.
    public System.Action OnClosed;

    private Canvas canvas;
    private GameObject backdrop;
    private Text titulo;
    private Text timerTexto;
    private Image[] celdasLienzo;
    private PaintColor[] patronReferencia;
    private PaintColor[] patronLienzo;
    private PaintColor colorSeleccionado = PaintColor.Red;
    private Image[] botonesSwatch;
    private GameObject seleccionMarco;

    private float tiempoRestante;
    private bool terminado = false;

    // ============================================================
    // Feedback de playtest: "permitir asignar sprites [...] reemplazar los
    // bloques de selección de color del minijuego (que son de color plano)
    // por el sprite de la gota de pintura tintado por color". Reusa el mismo
    // truco que AmmoPickup.EncontrarSpriteGota(): toma prestado en runtime el
    // sprite "GOTA MUNITION1" (llena) que ya usa cualquier AmmoHUD presente
    // en la escena, sin necesitar asignar ningún asset nuevo a mano. Si no
    // hay ningún AmmoHUD en la escena (o no tiene sprites configurados), cae
    // de nuevo al placeholder de color plano de siempre.
    // ============================================================
    private static Sprite spriteGotaCache;
    static Sprite EncontrarSpriteGota()
    {
        if (spriteGotaCache != null) return spriteGotaCache;
        // OJO: hay que buscar incluyendo objetos INACTIVOS. PuzzleStructure
        // esconde hudCombate/hudExploracion (GameManager.SetHudVisible(false))
        // justo ANTES de crear este puzzle, así que el AmmoHUD (que vive
        // dentro de hudCombate) ya está inactivo para cuando este método se
        // llama. FindObjectOfType sin este parámetro ignora objetos
        // inactivos y por eso el minijuego dejó de mostrar las gotas de
        // pintura y volvió al placeholder de color plano.
        AmmoHUD hud = FindFirstObjectByType<AmmoHUD>(FindObjectsInactive.Include);
        if (hud != null && hud.dropSprites != null && hud.dropSprites.Length > 0 && hud.dropSprites[0] != null)
            spriteGotaCache = hud.dropSprites[0];
        return spriteGotaCache;
    }

    // ============================================================
    // Feedback de playtest: "la hud del minijuego debe tener los textos un
    // poco más grandes y deben tener un estilo más pixelado que vaya acorde
    // al juego". Se reusa la fuente "Press Start 2P" que ya está importada
    // en el proyecto (Assets/TextMesh Pro/Fonts), copiada a una carpeta
    // Resources (Assets/Assets/Resources/Fonts) para poder cargarla en
    // runtime con Resources.Load sin tocar nada del Editor. Si por algún
    // motivo no se encuentra, cae de nuevo a la fuente legacy de siempre.
    // ============================================================
    private static Font fuentePixelCache;
    private static bool fuentePixelBuscada = false;
    static Font EncontrarFuentePixel()
    {
        if (!fuentePixelBuscada)
        {
            fuentePixelBuscada = true;
            fuentePixelCache = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        }
        return fuentePixelCache != null ? fuentePixelCache : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void Start()
    {
        EncontrarOCrearCanvas();
        GenerarPatronReferencia();
        ConstruirUI();
        AplicarCursorPincel();
        tiempoRestante = TIME_LIMIT;
        StartCoroutine(FlujoTimer());
    }

    // ============================================================
    // Feedback de playtest: "el cursor debe ser el png del sprite del
    // pincel" (mientras el minijuego está abierto). Le pide a WeaponCursor
    // que se quede quieto (Suspender) y toma el control del cursor del
    // sistema usando el mismo sprite "pincel" que ya tiene asignado
    // WeaponHUD para el arma Projectile, sin necesitar un sprite nuevo
    // asignado a mano.
    // ============================================================
    void AplicarCursorPincel()
    {
        if (WeaponCursor.Instance != null) WeaponCursor.Instance.Suspender();

        WeaponHUD hud = FindFirstObjectByType<WeaponHUD>(FindObjectsInactive.Include);
        Sprite pincel = hud != null ? hud.spritePincel : null;
        if (pincel == null) return;

        // Feedback de playtest: "el cursor debe ser mucho mas pequeño y
        // estira los pngs horizontalmente un poco ya que estan muy
        // aplanados". Mismo criterio de tamaño/estiramiento que WeaponCursor.
        const int altoCursorPincelPx = 24;
        const float estiramientoHorizontalPincel = 1.35f;
        int anchoCursorPincelPx = Mathf.RoundToInt(altoCursorPincelPx * estiramientoHorizontalPincel);

        Texture2D tex = CursorTextureUtils.SpriteATexturaEscalada(pincel, anchoCursorPincelPx, altoCursorPincelPx);
        if (tex == null) return;

        Cursor.SetCursor(tex, new Vector2(tex.width / 2f, tex.height / 2f), CursorMode.Auto);
    }

    // Red de seguridad: si este objeto se destruye por cualquier vía que no
    // sea el cierre normal (MostrarResultadoYCerrar ya llama a esto también),
    // el cursor del arma vuelve a tomar control en vez de quedarse pegado
    // en el pincel para siempre.
    void OnDestroy()
    {
        if (WeaponCursor.Instance != null) WeaponCursor.Instance.Reanudar();
    }

    void EncontrarOCrearCanvas()
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        canvas = canvasObj != null ? canvasObj.GetComponent<Canvas>() : null;
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject nuevo = new GameObject("Canvas_PaintPuzzle_Fallback");
            canvas = nuevo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            nuevo.AddComponent<CanvasScaler>();
            nuevo.AddComponent<GraphicRaycaster>();
        }
    }

    void GenerarPatronReferencia()
    {
        int n = GRID_SIZE * GRID_SIZE;
        patronReferencia = new PaintColor[n];
        patronLienzo = new PaintColor[n];
        for (int i = 0; i < n; i++)
        {
            patronReferencia[i] = paleta[Random.Range(0, paleta.Length)];
            patronLienzo[i] = PaintColor.Gray; // el lienzo arranca "en blanco" (sin pintar = Aguado)
        }
    }

    void ConstruirUI()
    {
        backdrop = new GameObject("PaintPuzzle_Backdrop", typeof(RectTransform));
        backdrop.transform.SetParent(canvas.transform, false);
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        Image backdropImg = backdrop.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.88f);

        // Feedback de playtest: "el titulo del minijuego se pega al timer
        // ahora". Se sube el título más arriba (antes y=300) para dejar más
        // aire respecto del timer, que se queda donde estaba.
        titulo = CrearTexto("PaintPuzzle_Titulo", backdrop.transform, new Vector2(0f, 370f), 34,
            "Copia el Patrón bajo Presión");
        titulo.lineSpacing = 1.5f;
        titulo.rectTransform.sizeDelta = new Vector2(560f, 80f);
        timerTexto = CrearTexto("PaintPuzzle_Timer", backdrop.transform, new Vector2(0f, 278f), 28,
            "Tiempo: 35s");

        // --- Referencia (izquierda) ---
        CrearTexto("PaintPuzzle_LabelRef", backdrop.transform, new Vector2(-295f, 218f), 20, "Referencia");
        RectTransform gridRef = CrearGrillaContenedor("PaintPuzzle_GridRef", backdrop.transform, new Vector2(-295f, 0f));
        Image[] celdasRef = CrearCeldas(gridRef, false);
        for (int i = 0; i < celdasRef.Length; i++) celdasRef[i].color = PaintColorUtils.ToUnityColor(patronReferencia[i]);

        // --- Lienzo (derecha) ---
        CrearTexto("PaintPuzzle_LabelLienzo", backdrop.transform, new Vector2(295f, 218f), 20, "Tu lienzo");
        RectTransform gridLienzo = CrearGrillaContenedor("PaintPuzzle_GridLienzo", backdrop.transform, new Vector2(295f, 0f));
        celdasLienzo = CrearCeldas(gridLienzo, true);
        for (int i = 0; i < celdasLienzo.Length; i++) celdasLienzo[i].color = PaintColorUtils.ToUnityColor(PaintColor.Gray);

        // --- Paleta de colores (abajo de todo) ---
        ConstruirPaleta();
    }

    Text CrearTexto(string nombre, Transform padre, Vector2 pos, int tamano, string contenido)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        obj.transform.SetParent(padre, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(500f, 50f);

        Text txt = obj.AddComponent<Text>();
        txt.font = EncontrarFuentePixel();
        txt.fontSize = tamano;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = contenido;

        // La fuente pixelada (Press Start 2P) es bastante más ancha por
        // caracter que la legacy de antes; con Best Fit el texto usa el
        // tamaño pedido como máximo pero se achica solo si no entra en el
        // ancho de la caja, así nunca se corta ni se sale del cartel.
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 10;
        txt.resizeTextMaxSize = tamano;
        return txt;
    }

    RectTransform CrearGrillaContenedor(string nombre, Transform padre, Vector2 pos)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        obj.transform.SetParent(padre, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        float lado = GRID_SIZE * 72f;
        rect.sizeDelta = new Vector2(lado, lado);
        return rect;
    }

    // Crea las GRID_SIZE x GRID_SIZE celdas dentro del contenedor dado.
    // Si interactiva = true, cada celda tiene Button y al clickearla se
    // pinta con colorSeleccionado.
    Image[] CrearCeldas(RectTransform contenedor, bool interactiva)
    {
        int n = GRID_SIZE * GRID_SIZE;
        Image[] celdas = new Image[n];
        float ancho = 62f;
        float espacio = 8f;
        float total = GRID_SIZE * (ancho + espacio) - espacio;
        float inicio = -total / 2f + ancho / 2f;

        for (int i = 0; i < n; i++)
        {
            int fila = i / GRID_SIZE;
            int col = i % GRID_SIZE;

            GameObject celdaObj = new GameObject("Celda_" + i, typeof(RectTransform));
            celdaObj.transform.SetParent(contenedor, false);
            RectTransform rect = celdaObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(ancho, ancho);
            rect.anchoredPosition = new Vector2(inicio + col * (ancho + espacio), -(inicio + fila * (ancho + espacio)));

            Image img = celdaObj.AddComponent<Image>();
            img.color = Color.gray;
            Sprite gota = EncontrarSpriteGota();
            if (gota != null)
            {
                img.sprite = gota;
                img.preserveAspect = true;
            }
            celdas[i] = img;

            if (interactiva)
            {
                Button btn = celdaObj.AddComponent<Button>();
                int indiceCapturado = i;
                btn.onClick.AddListener(() => PintarCelda(indiceCapturado));

                // Feedback de playtest: "permitir pintar varios cuadros a
                // imitar con arrastre de click en el minijuego". Button.onClick
                // de arriba solo pinta UNA celda por click suelto. Se agrega
                // un EventTrigger para que también pinte: (a) apenas se
                // presiona el botón sobre la celda (PointerDown, así la
                // primera celda del arrastre no depende de que el click se
                // "suelte" justo ahí), y (b) cada celda nueva que el mouse
                // vaya tocando MIENTRAS el botón izquierdo sigue presionado
                // (PointerEnter + chequeo de Mouse.current, sino pintaría
                // solo con pasar el mouse por encima sin clickear).
                EventTrigger trigger = celdaObj.AddComponent<EventTrigger>();

                EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entryDown.callback.AddListener((data) => PintarCelda(indiceCapturado));
                trigger.triggers.Add(entryDown);

                EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entryEnter.callback.AddListener((data) =>
                {
                    if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                        PintarCelda(indiceCapturado);
                });
                trigger.triggers.Add(entryEnter);
            }
        }
        return celdas;
    }

    void ConstruirPaleta()
    {
        GameObject paletaObj = new GameObject("PaintPuzzle_Paleta", typeof(RectTransform));
        paletaObj.transform.SetParent(backdrop.transform, false);
        RectTransform rect = paletaObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, -300f);
        rect.sizeDelta = new Vector2(600f, 70f);

        botonesSwatch = new Image[paleta.Length];
        float ancho = 62f;
        float espacio = 16f;
        float total = paleta.Length * (ancho + espacio) - espacio;
        float inicio = -total / 2f + ancho / 2f;

        for (int i = 0; i < paleta.Length; i++)
        {
            PaintColor colorActual = paleta[i];

            GameObject swatchObj = new GameObject("Swatch_" + colorActual, typeof(RectTransform));
            swatchObj.transform.SetParent(paletaObj.transform, false);
            RectTransform srect = swatchObj.GetComponent<RectTransform>();
            srect.sizeDelta = new Vector2(ancho, ancho);
            srect.anchoredPosition = new Vector2(inicio + i * (ancho + espacio), 0f);

            Image img = swatchObj.AddComponent<Image>();
            img.color = PaintColorUtils.ToUnityColor(colorActual);
            Sprite gotaSwatch = EncontrarSpriteGota();
            if (gotaSwatch != null)
            {
                img.sprite = gotaSwatch;
                img.preserveAspect = true;
            }
            botonesSwatch[i] = img;

            Button btn = swatchObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SeleccionarColor(colorActual));
        }

        // Marco blanco simple que rodea el color seleccionado (placeholder de selección).
        seleccionMarco = new GameObject("PaintPuzzle_SeleccionMarco", typeof(RectTransform));
        seleccionMarco.transform.SetParent(paletaObj.transform, false);
        RectTransform mrect = seleccionMarco.GetComponent<RectTransform>();
        mrect.sizeDelta = new Vector2(ancho + 12f, ancho + 12f);
        Image marcoImg = seleccionMarco.AddComponent<Image>();
        marcoImg.color = new Color(0f, 0f, 0f, 0f); // transparente, solo deja ver el outline del Image de UI por defecto
        Outline outline = seleccionMarco.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(3f, 3f);
        seleccionMarco.transform.SetSiblingIndex(0);

        SeleccionarColor(paleta[0]);
    }

    void SeleccionarColor(PaintColor color)
    {
        colorSeleccionado = color;
        int idx = System.Array.IndexOf(paleta, color);
        if (idx >= 0 && botonesSwatch != null && seleccionMarco != null)
            seleccionMarco.transform.position = botonesSwatch[idx].transform.position;
    }

    void PintarCelda(int indice)
    {
        if (terminado) return;

        // OJO: cada celda tiene Button.onClick Y un EventTrigger de
        // PointerDown apuntando a este mismo método (para soportar arrastre),
        // así que un click suelto normal dispara esto DOS veces. Si la celda
        // ya tiene el color seleccionado, no hay nada que repintar ni que
        // sonar de nuevo (evita el "pincelazo" doble en cada click).
        if (patronLienzo[indice] == colorSeleccionado) return;

        patronLienzo[indice] = colorSeleccionado;
        celdasLienzo[indice].color = PaintColorUtils.ToUnityColor(colorSeleccionado);

        // Pedido del usuario: "pincelazo" también en cada click/arrastre del
        // minijuego, porque en teoría se pinta con un pincel.
        if (SfxManager.Instance != null) SfxManager.Instance.PlayPincelazo();

        if (CoincideConReferencia())
            Finalizar(true);
    }

    bool CoincideConReferencia()
    {
        for (int i = 0; i < patronReferencia.Length; i++)
            if (patronLienzo[i] != patronReferencia[i]) return false;
        return true;
    }

    // Feedback de playtest: "el timer del minijuego debe titilar en rojo
    // cuando quedan 10 segundos para terminarlo".
    private const float UMBRAL_TITILEO = 10f;
    private const float VELOCIDAD_TITILEO = 6f; // parpadeos por segundo aprox.

    IEnumerator FlujoTimer()
    {
        // Usa tiempo NO escalado: PuzzleStructure pone Time.timeScale = 0 para
        // congelar el combate de fondo mientras el puzzle está abierto, pero
        // la cuenta regresiva de 60s de ESTE puzzle tiene que seguir corriendo
        // en tiempo real, sino nunca avanzaría.
        while (tiempoRestante > 0f && !terminado)
        {
            tiempoRestante -= Time.unscaledDeltaTime;
            timerTexto.text = "Tiempo: " + Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante)) + "s";

            if (tiempoRestante <= UMBRAL_TITILEO)
            {
                // Titileo blanco<->rojo: usa Time.unscaledTime para que siga
                // andando aunque Time.timeScale esté en 0 de fondo.
                float fase = (Mathf.Sin(Time.unscaledTime * VELOCIDAD_TITILEO) + 1f) * 0.5f;
                timerTexto.color = Color.Lerp(Color.red, Color.white, fase);
            }
            else
            {
                timerTexto.color = Color.white;
            }

            yield return null;
        }

        if (!terminado) Finalizar(false);
    }

    void Finalizar(bool victoria)
    {
        if (terminado) return;
        terminado = true;

        StopAllCoroutines();
        StartCoroutine(MostrarResultadoYCerrar(victoria));
    }

    IEnumerator MostrarResultadoYCerrar(bool victoria)
    {
        // Se invoca ACÁ (no al final) para poder mostrar en el cartel de
        // resultado qué mejora concreta otorgó PuzzleStructure/UpgradeSystem
        // (GDD 3.7: la recompensa es una mejora real, no un punto abstracto).
        string mensajeMejora = OnFinished?.Invoke(victoria);

        titulo.text = victoria ? "¡Patrón completado!" : "¡Se acabó el tiempo!";
        if (victoria && !string.IsNullOrEmpty(mensajeMejora))
            timerTexto.text = mensajeMejora;
        else
            timerTexto.text = victoria ? "El pueblo recupera un poco más de color." : "Podés volver a intentarlo cuando quieras.";

        yield return new WaitForSecondsRealtime(1.4f);

        // Recién acá, justo antes de que la interfaz desaparezca de verdad,
        // se avisa para despausar el juego de fondo: así no queda ninguna
        // ventana en la que los enemigos ya se mueven detrás del cartel.
        OnClosed?.Invoke();

        Destroy(backdrop);
        Destroy(gameObject);
    }
}
