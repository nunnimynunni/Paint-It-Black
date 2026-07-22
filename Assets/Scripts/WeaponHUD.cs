using UnityEngine;
using UnityEngine.UI;

// ============================================================
// SCRIPT: WeaponHUD
// Muestra los 3 slots de arma en una fila horizontal.
//   - El slot seleccionado crece levemente y tiene mayor opacidad.
//   - Cada slot muestra la tecla correspondiente (1, 2, 3) debajo.
//
// ARQUITECTURA:
//   Este script vive en HudArmas (hijo de HudCombate). Los slots
//   visuales se crean como hijos del CONTENEDOR VISUAL (el padre de
//   weaponImage = MarcoArmas), que es el que tiene posición en canvas
//   y cuyo RectTransform se movió al corner bottom-left.
//   Así no aparece una copia extra en el centro de pantalla.
// ============================================================
public class WeaponHUD : MonoBehaviour
{
    [Header("Sprites de armas")]
    public Sprite spriteSpray;
    public Sprite spritePincel;
    public Sprite spriteRodillo;

    [Header("Referencia al icono único heredado")]
    [Tooltip("Apunta al Image component de IconoArma (hijo de MarcoArmas). " +
             "Se usa para encontrar el contenedor visual y se destruye al iniciar.")]
    public Image weaponImage;

    [Header("Layout de slots")]
    [Tooltip("Tamaño del icono de arma en pixels")]
    public float iconSize = 64f;
    [Tooltip("Factor de escala del slot activo")]
    public float activeScale = 1.20f;
    [Tooltip("Separación horizontal entre slots")]
    public float spacing = 14f;

    // ── Colores (estetica graffiti: manchas de pintura, alto contraste) ────────
    // Activo: mancha amarillo-dorado como aerosol sobre pared, icono blanco puro
    static readonly Color COLOR_ICONO_ACTIVO   = new Color(1f,    1f,    1f,    1f);
    static readonly Color COLOR_ICONO_INACTIVO = new Color(0.55f, 0.55f, 0.55f, 0.50f);
    static readonly Color COLOR_FONDO_ACTIVO   = new Color(0.95f, 0.75f, 0.05f, 0.88f); // amarillo spray
    static readonly Color COLOR_FONDO_INACTIVO = new Color(0.04f, 0.04f, 0.04f, 0.65f); // negro pared
    static readonly Color COLOR_TECLA_ACTIVO   = new Color(1f,    1f,    1f,    1.00f); // blanco siempre
    static readonly Color COLOR_TECLA_INACTIVO = new Color(1f,    1f,    1f,    0.80f);

    // ── Internos ───────────────────────────────────────────────
    private RectTransform[] slotRT = new RectTransform[3];
    private Image[]          iconos  = new Image[3];
    private Image[]          fondos  = new Image[3];
    private Text[]           teclas  = new Text[3];

    private WeaponManager.WeaponType lastWeapon;

    // ── Fuente pixel (igual que en el minijuego) ───────────────
    static Font fuenteCache;
    static Font ObtenerFuente()
    {
        if (fuenteCache != null) return fuenteCache;
        fuenteCache = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        return fuenteCache != null ? fuenteCache : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    // ==========================================================
    void Start()
    {
        // El contenedor visual es el PADRE de weaponImage (= MarcoArmas / 1262612192).
        // Si weaponImage no está asignado, usar el transform propio como fallback.
        Transform contenedor = weaponImage != null
            ? weaponImage.transform.parent
            : transform;

        // Eliminar todos los hijos del contenedor (el IconoArma original)
        for (int i = contenedor.childCount - 1; i >= 0; i--)
            Destroy(contenedor.GetChild(i).gameObject);

        // El contenedor tiene su propio Image component (fondo del recuadro) que
        // queremos mantener, por eso solo destruimos hijos, no el componente propio.

        ConstruirSlots(contenedor);

        lastWeapon = WeaponManager.instance != null
            ? WeaponManager.instance.currentWeapon
            : WeaponManager.WeaponType.Spray;
        ActualizarSlots(lastWeapon);
    }

    void ConstruirSlots(Transform contenedor)
    {
        Sprite[] sprites = { spriteSpray, spritePincel, spriteRodillo };
        string[] labels  = { "1", "2", "3" };

        float slotAncho  = iconSize + 16f;   // marco alrededor del icono
        float slotAlto   = slotAncho + 20f;  // extra abajo para la tecla
        float totalAncho = slotAncho * 3 + spacing * 2;
        float xInicio    = -totalAncho / 2f + slotAncho / 2f;
        float yIcono     = 10f;  // sube levemente para dejar hueco a la tecla

        Font fuente = ObtenerFuente();

        for (int i = 0; i < 3; i++)
        {
            float xPos = xInicio + i * (slotAncho + spacing);

            // ── Contenedor del slot ────────────────────────────
            // IMPORTANTE: AddComponent<RectTransform> en modo stretch (0,0→1,1) por defecto.
            // Hay que fijar anchorMin/Max a (0.5,0.5) para que sizeDelta sea el tamaño real.
            var go = new GameObject($"Slot{i + 1}");
            go.transform.SetParent(contenedor, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(slotAncho, slotAlto);
            rt.anchoredPosition = new Vector2(xPos, 0f);
            slotRT[i] = rt;

            // ── Fondo (recuadro semitransparente) ──────────────
            var goF = new GameObject("Fondo");
            goF.transform.SetParent(go.transform, false);
            var rtF = goF.AddComponent<RectTransform>();
            rtF.anchorMin = new Vector2(0.5f, 0.5f);
            rtF.anchorMax = new Vector2(0.5f, 0.5f);
            rtF.pivot     = new Vector2(0.5f, 0.5f);
            rtF.sizeDelta = new Vector2(slotAncho, slotAncho);
            rtF.anchoredPosition = new Vector2(0f, yIcono);
            var imgF = goF.AddComponent<Image>();
            imgF.color = COLOR_FONDO_INACTIVO;
            fondos[i] = imgF;

            // ── Icono de arma ──────────────────────────────────
            var goI = new GameObject("Icono");
            goI.transform.SetParent(go.transform, false);
            var rtI = goI.AddComponent<RectTransform>();
            rtI.anchorMin = new Vector2(0.5f, 0.5f);
            rtI.anchorMax = new Vector2(0.5f, 0.5f);
            rtI.pivot     = new Vector2(0.5f, 0.5f);
            rtI.sizeDelta = new Vector2(iconSize, iconSize);
            rtI.anchoredPosition = new Vector2(0f, yIcono);
            var imgI = goI.AddComponent<Image>();
            imgI.sprite = sprites[i];
            imgI.preserveAspect = true;
            imgI.color = COLOR_ICONO_INACTIVO;
            iconos[i] = imgI;

            // ── Tecla numérica ─────────────────────────────────
            var goT = new GameObject("Tecla");
            goT.transform.SetParent(go.transform, false);
            var rtT = goT.AddComponent<RectTransform>();
            rtT.anchorMin = new Vector2(0.5f, 0.5f);
            rtT.anchorMax = new Vector2(0.5f, 0.5f);
            rtT.pivot     = new Vector2(0.5f, 0.5f);
            rtT.sizeDelta = new Vector2(slotAncho, 20f);
            rtT.anchoredPosition = new Vector2(0f, -slotAncho / 2f + yIcono - 4f);
            var txt = goT.AddComponent<Text>();
            txt.text = labels[i];
            txt.font = fuente;
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = COLOR_TECLA_INACTIVO;
            teclas[i] = txt;
        }
    }

    // ==========================================================
    void Update()
    {
        if (WeaponManager.instance == null) return;
        WeaponManager.WeaponType current = WeaponManager.instance.currentWeapon;
        if (current == lastWeapon) return;
        lastWeapon = current;
        ActualizarSlots(current);
    }

    void ActualizarSlots(WeaponManager.WeaponType seleccionada)
    {
        int idx = (int)seleccionada;
        for (int i = 0; i < 3; i++)
        {
            bool activo = i == idx;
            float escala = activo ? activeScale : 1f;
            slotRT[i].localScale = new Vector3(escala, escala, 1f);
            iconos[i].color = activo ? COLOR_ICONO_ACTIVO   : COLOR_ICONO_INACTIVO;
            fondos[i].color = activo ? COLOR_FONDO_ACTIVO   : COLOR_FONDO_INACTIVO;
            teclas[i].color = activo ? COLOR_TECLA_ACTIVO   : COLOR_TECLA_INACTIVO;
        }
    }
}
