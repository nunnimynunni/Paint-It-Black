using System.Collections;
using UnityEngine;

// ============================================================
// SCRIPT: NpcCeraEffect
// Componente que el Encerador agrega dinámicamente a un NPC
// aliado cuando lo encera. Mientras está activo:
//   - El NPC es inmune a efectos de pintura y al daño de
//     proyectiles no-aguados (EnemyHealth lo consulta).
//   - Muestra un outline (stroke/borde) turquesa: un sprite
//     hijo al mismo layer, escala 1.06x, material outlineblanco
//     (Custom/SpriteWhiteSolid) con color turquesa. El resultado
//     es un borde fino de ~3-4px alrededor del sprite, igual al
//     outline del jugador durante el buff.
//   - Se auto-destruye al expirar la duración.
// ============================================================
public class NpcCeraEffect : MonoBehaviour
{
    public static int EnceredosActivos { get; private set; } = 0;
    public static int MaxEnceredos = 2;

    [Tooltip("Segundos que dura la capa de cera")]
    public float duracion = 10f;

    public bool EstaEncerado { get; private set; } = false;

    private SpriteRenderer srPrincipal;
    private GameObject outlineObj;
    private SpriteRenderer srOutline;

    private static readonly Color ColorCera = new Color(0f, 0.85f, 0.78f, 1f);

    void Awake()
    {
        srPrincipal = GetComponent<SpriteRenderer>();
    }

    void OnDestroy()
    {
        if (EstaEncerado) EnceredosActivos--;
        if (outlineObj != null) Destroy(outlineObj);
    }

    public void Activar(float duracionCera)
    {
        if (EstaEncerado) return;
        duracion     = duracionCera;
        EstaEncerado = true;
        EnceredosActivos++;
        CrearOutline();
        StartCoroutine(TimerExpiracion());
    }

    void LateUpdate()
    {
        // Sincronizar el outline con el sprite principal cada frame.
        // El YSort del NPC cambia sortingOrder constantemente → el outline
        // debe actualizarse también para siempre quedar detrás.
        if (srOutline == null || srPrincipal == null) return;
        srOutline.sprite        = srPrincipal.sprite;
        srOutline.flipX         = srPrincipal.flipX;
        srOutline.sortingLayerID = srPrincipal.sortingLayerID;
        srOutline.sortingOrder  = srPrincipal.sortingOrder - 1;
    }

    void CrearOutline()
    {
        if (srPrincipal == null) return;

        // Mismo esquema que el buff del jugador (PlayerHealth.CrearBuffOutline):
        // - posición (0,0,0) relativa al sprite padre
        // - escala 1.06x → ~3-4px de borde visible alrededor del sprite
        // - material Custom/SpriteWhiteSolid buscado en la escena
        // - color turquesa en el SpriteRenderer (el shader usa ese color como fill)
        outlineObj = new GameObject("CeraOutline");
        outlineObj.transform.SetParent(srPrincipal.transform, false);
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localScale    = new Vector3(1.06f, 1.06f, 1f);

        srOutline = outlineObj.AddComponent<SpriteRenderer>();
        srOutline.sprite         = srPrincipal.sprite;
        srOutline.flipX          = srPrincipal.flipX;
        srOutline.color          = ColorCera;
        srOutline.sortingLayerID = srPrincipal.sortingLayerID;
        srOutline.sortingOrder   = srPrincipal.sortingOrder - 1;

        // Buscar el material outlineblanco que ya existe en la escena.
        // Instanciar una copia propia para que cambiar el color no afecte
        // el material compartido ni los outlines de otros objetos.
        Material matBase = BuscarMaterialOutline();
        if (matBase != null)
            srOutline.material = new Material(matBase);
    }

    Material BuscarMaterialOutline()
    {
        // Buscar cualquier SpriteRenderer que ya use el shader sólido
        // (Custom/SpriteWhiteSolid). El más probable: Gomez, PuzzleStructure,
        // o el outline del buff del jugador.
        foreach (var r in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r == null || r.sharedMaterial == null) continue;
            if (r.sharedMaterial.shader?.name == "Custom/SpriteWhiteSolid")
                return r.sharedMaterial;
        }
        // Fallback: crear material desde el shader directamente
        Shader s = Shader.Find("Custom/SpriteWhiteSolid");
        return s != null ? new Material(s) : null;
    }

    void SetOutlineVisible(bool v)
    {
        if (outlineObj != null) outlineObj.SetActive(v);
    }

    IEnumerator TimerExpiracion()
    {
        float tiempoParpadeo = 2f;
        yield return new WaitForSeconds(Mathf.Max(0f, duracion - tiempoParpadeo));

        float t = 0f;
        while (t < tiempoParpadeo)
        {
            SetOutlineVisible((int)(t * 8f) % 2 == 0);
            t += Time.deltaTime;
            yield return null;
        }

        EstaEncerado = false;
        EnceredosActivos--;
        if (outlineObj != null) Destroy(outlineObj);
        Destroy(this);
    }
}
