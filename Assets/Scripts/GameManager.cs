using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Controlador central de partida: game over y victoria.
// Poner en un GameObject vacío llamado "GameManager" en cada escena de juego.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI opcional (arrastrar paneles si existen)")]
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    public bool IsGameOver { get; private set; } = false;

    // ============================================================
    // VERTICAL SLICE: progreso global de "repintar el pueblo" (Fase 3 del slice).
    // No se crea una barra de vida nueva ni se duplica HealthHUD: se reutiliza
    // el objeto "barraprogreso" que ya existe como hijo de hudExploracion en el
    // Canvas (con su sprite "barra de progreso_0" ya asignado a mano), solo se
    // lo configura por código como Image.Type=Filled la primera vez que se usa.
    // Fuentes de progreso (vertical slice):
    //   - 60% repartido proporcionalmente entre los enemigos eliminados en combate
    //     (lo incrementa EnemySpawner al matar cada NPC).
    //   - 40% restante lo otorga PuzzleStructure/PaintCanvasPuzzle al resolver
    //     con éxito el puzzle "Copia de Patrón bajo Presión" (GDD 3.7).
    // ============================================================
    [Header("Progreso de pintado (vertical slice)")]
    [Range(0f, 1f)] public float paintProgress = 0f;

    private UnityEngine.UI.Image barraProgresoImg;
    private bool barraProgresoBuscada = false;

    // ============================================================
    // GDD 3.7: la recompensa real del puzzle "Copia de Patrón bajo Presión"
    // NO es un contador de puntos abstracto: es una mejora concreta y
    // aplicada de inmediato (daño/cooldown/velocidad de disparo/munición/
    // área de un arma, vida/escudo/reducción de daño/vida extra, o
    // velocidad de movimiento/rebote de proyectiles). Ver UpgradeSystem.cs,
    // que es quien la otorga (PuzzleStructure lo llama al ganar el puzzle).
    // ============================================================

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        RefreshBarraProgreso();
    }


    public void AddPaintProgress(float amount) => SetPaintProgress(paintProgress + amount);

    public void SetPaintProgress(float value)
    {
        paintProgress = Mathf.Clamp01(value);
        RefreshBarraProgreso();
    }

    void RefreshBarraProgreso()
    {
        if (!barraProgresoBuscada)
        {
            GameObject barraObj = GameObject.Find("barraprogreso");
            if (barraObj != null)
            {
                barraProgresoImg = barraObj.GetComponent<UnityEngine.UI.Image>();
                if (barraProgresoImg != null && barraProgresoImg.type != UnityEngine.UI.Image.Type.Filled)
                {
                    barraProgresoImg.type = UnityEngine.UI.Image.Type.Filled;
                    barraProgresoImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                    barraProgresoImg.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
                }
            }
            else
            {
                Debug.LogWarning("GameManager: no se encontró el GameObject 'barraprogreso' en la escena; la barra de progreso de pintado no se va a actualizar visualmente.");
            }
            barraProgresoBuscada = true;
        }

        if (barraProgresoImg != null)
            barraProgresoImg.fillAmount = paintProgress;
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        Debug.Log("GAME OVER");
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        SetHudVisible(false);

        Time.timeScale = 0f;
    }

    public void Victory()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        Debug.Log("VICTORIA - todos los enemigos eliminados");
        if (victoryPanel != null) victoryPanel.SetActive(true);
        SetHudVisible(false);

        Time.timeScale = 0f;
    }

    // ============================================================
    // Feedback de playtest: "al estar en el minijuego se debe esconder la
    // hud de juego normal [...] esta también debe esconderse cuando se ve
    // la interfaz de perdiste". Reutiliza el patrón GameObject.Find ya
    // establecido en el proyecto (sin asignar referencias a mano en el
    // Editor). Estático para que PuzzleStructure también pueda llamarlo al
    // abrir/cerrar el minijuego, sin depender de que exista GameManager.Instance.
    //
    // Bug de playtest: "luego del minijuego la hud de oleada no reaparece".
    // GameObject.Find NO encuentra objetos inactivos: una vez que
    // SetHudVisible(false) los desactiva, el SetHudVisible(true) posterior
    // ya no podía volver a encontrarlos y se quedaban escondidos para
    // siempre. Se cachean las referencias una sola vez (mientras todavía
    // están activos) para no depender de Find después de la primera vez.
    // ============================================================
    private static GameObject hudCombateCache;
    private static GameObject hudExploracionCache;

    public static void SetHudVisible(bool visible)
    {
        if (hudCombateCache == null) hudCombateCache = GameObject.Find("hudCombate");
        if (hudCombateCache != null) hudCombateCache.SetActive(visible);

        if (hudExploracionCache == null) hudExploracionCache = GameObject.Find("hudExploracion");
        if (hudExploracionCache != null) hudExploracionCache.SetActive(visible);
    }

    // Útil para un botón de "Reintentar" en el panel de Game Over
    public void Restart()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // ============================================================
    // VERTICAL SLICE (Etapa 3 del GDD): cuando Cromagustín termina su
    // diálogo de felicitación, se dispara esto. Feedback de playtest: "saca
    // el fade con la pantalla final a negro, deja que siga después de hablar
    // con cromagustin" — antes esto fundía la pantalla a negro y dejaba un
    // cartel de créditos fijo, además de marcar IsGameOver=true (lo cual
    // congelaba enemigos/spawner/daño del jugador). Ahora NO se marca
    // IsGameOver ni se tapa la pantalla: solo se muestra un cartel chico y
    // no-bloqueante en una esquina que se desvanece solo, y el jugador sigue
    // jugando con total normalidad.
    // ============================================================
    public void ShowEndingCredits()
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        Canvas canvas = canvasObj != null ? canvasObj.GetComponent<Canvas>() : FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("GameManager: no se encontró ningún Canvas para mostrar el cartel de cierre del slice.");
            return;
        }

        GameObject textoObj = new GameObject("Ending_CartelNoBloqueante", typeof(RectTransform));
        textoObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = textoObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(700f, 60f);

        Text texto = textoObj.AddComponent<Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.alignment = TextAnchor.MiddleCenter;
        texto.fontSize = 28;
        texto.color = Color.white;
        texto.text = "Fin del Vertical Slice — ¡Gracias por jugar!";

        StartCoroutine(DesvanecerYDestruir(texto));
    }

    IEnumerator DesvanecerYDestruir(Text texto)
    {
        yield return new WaitForSeconds(3f);

        float t = 0f;
        const float duracionFade = 1.5f;
        Color c0 = texto.color;
        while (t < duracionFade)
        {
            t += Time.deltaTime;
            Color c = c0;
            c.a = Mathf.Lerp(c0.a, 0f, t / duracionFade);
            texto.color = c;
            yield return null;
        }

        Destroy(texto.gameObject);
    }
}
