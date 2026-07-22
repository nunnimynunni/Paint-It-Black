using System.Collections;
using UnityEngine;

// ============================================================
// SCRIPT: HouseVisualState
// Gestiona los 3 sprites de una casa: Nueva → Arruinada → Pintada.
//
// SETUP en el Editor (una vez por casa):
//   1. Seleccionar el GameObject de la casa en la escena.
//   2. Add Component → HouseVisualState.
//   3. Configurar 'numeroCasa' (1, 2, 3 o 4).
//
// SPRITES — colocar en Assets/Resources/Mapa/Casas/ con estos nombres exactos:
//   Casa 1 Nueva.png       Casa 2 Nueva.png       Casa 3 Nueva.png       Casa 4 Nueva.png
//   Casa 1 Arruinada.png   Casa 2 Arruinada.png   Casa 3 Arruinada.png   Casa 4 Arruinada.png
//   Casa 1 Pintada.png     Casa 2 Pintada.png     Casa 3 Pintada.png     Casa 4 Pintada.png
//
// FLUJO DE ESTADOS:
//   Nueva      → al iniciar el juego (sprite limpio).
//   Arruinada  → luego de 3-5 impactos de pintura (aleatoria por casa).
//   Pintada    → cuando el jugador completa el puzzle.
//   (La lluvia revierte Pintada/Arruinada → Nueva, reseteando el contador)
//
// EFECTOS EN CADA IMPACTO (independientemente del estado actual):
//   - Rattle: pequeña vibración del transform que decae en ~0.25s.
//   - Tinte: el sprite se tiñe por unos segundos con el color recibido;
//     un nuevo impacto de distinto color reemplaza el tinte inmediatamente.
// ============================================================
public class HouseVisualState : MonoBehaviour
{
    public enum Estado { Nueva, Arruinada, Pintada }

    [Tooltip("Número de casa (1–4). Debe coincidir con los archivos de sprite.")]
    public int numeroCasa = 1;

    public Estado EstadoActual { get; private set; } = Estado.Nueva;

    private SpriteRenderer sr;
    private Sprite spriteNueva;
    private Sprite spriteArruinada;
    private Sprite spritePintada;

    // ============================================================
    // RESISTENCIA AL COLOR
    // Pedido del usuario: "recién a los 3-5 disparos recibidos cambie
    // (que la resistencia sea aleatoria en cada casa)". La casa no
    // transiciona a Arruinada en el primer impacto; necesita acumular
    // entre 3 y 5 golpes (generado al Awake, distinto por instancia).
    // ============================================================
    private int resistencia;       // Umbral aleatorio 3–5, fijado en Awake
    private int hitsRecibidos = 0; // Golpes acumulados (solo cuenta de Nueva)

    // ============================================================
    // RATTLE
    // Pedido del usuario: "en esos 3 disparos recibidos en cada uno
    // haga un muy leve rattle como movimiento de vibración corta".
    // Oscilación con decaimiento exponencial, sin tocar el Rigidbody.
    // ============================================================
    [Header("Rattle")]
    [Tooltip("Amplitud máxima del shake (unidades de mundo)")]
    public float rattleAmplitud = 0.055f;
    [Tooltip("Duración total del efecto de shake en segundos")]
    public float rattleDuracion = 0.28f;

    // ============================================================
    // TINTE DE COLOR
    // Pedido del usuario: "dependiendo del color que le impacta, al
    // igual que los npcs, esta cambie su hue por unos segundos a dicho
    // color recibido (si recibe otro este otro lo reemplaza)".
    // Mismo esquema que EnemyStatusEffects: Color.Lerp entre el color
    // original del sprite y el color de la pintura, hold + fade-out.
    // ============================================================
    // El tinte es PERMANENTE: se aplica al recibir un impacto y persiste
    // hasta que la lluvia limpie la casa (SetEstado → Nueva).
    // Los disparos enemigos (EnemyBullet) se tratan como PaintColor.Gray.
    [Header("Tinte de color")]
    [Tooltip("Intensidad del tinte (0 = sin efecto, 1 = color puro)")]
    [Range(0f, 1f)] public float tintStrength = 0.55f;

    private Color colorOriginal;
    private Vector3 posicionLocalOriginal;
    private Coroutine rattleRoutine;
    private Coroutine tintRoutine;

    // ── Collider propio para detectar pintura ──────────────────
    // Solo se crea si el GameObject no tiene ningún Collider2D.
    // Las casas con PuzzleStructure ya tienen uno (lo agrega PuzzleStructure.Start),
    // pero Awake corre antes que Start de otros scripts, así que el check
    // puede dar false aunque PuzzleStructure lo vaya a agregar luego. El
    // collider extra (si se crea) es Trigger = true con radio amplio, lo
    // que no rompe nada: el de PuzzleStructure lo complementa.
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        CargarSprites();
        AplicarSprite(Estado.Nueva);

        colorOriginal        = sr != null ? sr.color : Color.white;
        posicionLocalOriginal = transform.localPosition;

        // Umbral de resistencia independiente por instancia de casa
        resistencia = Random.Range(3, 6); // 3, 4 o 5

        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.5f;
        }
    }

    // ============================================================
    // CARGA DE SPRITES
    // ============================================================
    void CargarSprites()
    {
        spriteNueva     = CargarSprite($"Mapa/Casas/Casa {numeroCasa} Nueva");
        spriteArruinada = CargarSprite($"Mapa/Casas/Casa {numeroCasa} Arruinada");
        spritePintada   = CargarSprite($"Mapa/Casas/Casa {numeroCasa} Pintada");

        if (spriteNueva     == null) Debug.LogWarning($"HouseVisualState ({name}): no encontró 'Mapa/Casas/Casa {numeroCasa} Nueva'");
        if (spriteArruinada == null) Debug.LogWarning($"HouseVisualState ({name}): no encontró 'Mapa/Casas/Casa {numeroCasa} Arruinada'");
        if (spritePintada   == null) Debug.LogWarning($"HouseVisualState ({name}): no encontró 'Mapa/Casas/Casa {numeroCasa} Pintada'");
    }

    static Sprite CargarSprite(string path)
    {
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) return s;
        // Fallback: sprite sheet en modo Multiple. Tomamos el sub-sprite
        // de mayor área para no confundir artefactos de auto-slicing.
        Sprite[] all = Resources.LoadAll<Sprite>(path);
        if (all == null || all.Length == 0) return null;
        Sprite mayor = all[0];
        for (int i = 1; i < all.Length; i++)
        {
            if (all[i].rect.width * all[i].rect.height > mayor.rect.width * mayor.rect.height)
                mayor = all[i];
        }
        return mayor;
    }

    // ============================================================
    // CAMBIO DE ESTADO (llamado por PuzzleStructure y RainManager)
    // ============================================================
    public void SetEstado(Estado estado)
    {
        EstadoActual = estado;
        AplicarSprite(estado);

        // Al volver a Nueva (lluvia): resetear contador y efectos visuales
        if (estado == Estado.Nueva)
        {
            hitsRecibidos = 0;
            resistencia   = Random.Range(3, 6); // nuevo umbral para esta "vida" de la casa

            // Cancelar rattle/tinte en curso y restaurar colores/posición
            if (rattleRoutine != null) { StopCoroutine(rattleRoutine); rattleRoutine = null; }
            if (tintRoutine   != null) { StopCoroutine(tintRoutine);   tintRoutine   = null; }
            if (sr != null) sr.color = colorOriginal;
            transform.localPosition = posicionLocalOriginal;
        }
    }

    void AplicarSprite(Estado estado)
    {
        if (sr == null) return;
        Sprite target = estado switch
        {
            Estado.Nueva     => spriteNueva,
            Estado.Arruinada => spriteArruinada,
            Estado.Pintada   => spritePintada,
            _                => spriteNueva,
        };
        if (target != null) sr.sprite = target;
    }

    // ============================================================
    // DETECCIÓN DE PINTURA
    // Antes: un solo impacto cambiaba el estado a Arruinada.
    // Ahora: cada impacto produce rattle + tinte independientemente del
    // estado actual; el cambio Nueva→Arruinada requiere 'resistencia'
    // golpes acumulados. Las casas ya Pintadas no reaccionan (el puzzle
    // ya las terminó; solo la lluvia puede revertirlas).
    // ============================================================
    void OnTriggerEnter2D(Collider2D other)
    {
        if (EstadoActual == Estado.Pintada) return;

        PaintColor? colorImpacto = ObtenerColorDeImpacto(other);
        if (colorImpacto == null) return;

        // Efectos visuales en todo impacto (rattle + tinte)
        IniciarRattle();
        AplicarTinte(colorImpacto.Value);

        // Cambio de estado: solo aplica de Nueva → Arruinada
        if (EstadoActual == Estado.Nueva)
        {
            hitsRecibidos++;
            if (hitsRecibidos >= resistencia)
                SetEstado(Estado.Arruinada);
        }
    }

    // Extrae el PaintColor del proyectil que produjo el impacto.
    // Los disparos del jugador llevan su color actual; los de enemigos
    // (EnemyBullet) se tratan como Gray para "ensuciar" la casa.
    PaintColor? ObtenerColorDeImpacto(Collider2D other)
    {
        var proj = other.GetComponent<Projectile>();
        if (proj != null) return proj.colorType;

        var sp = other.GetComponent<spray>();
        if (sp != null) return sp.colorType;

        var rod = other.GetComponent<rodillo>();
        if (rod != null) return rod.colorType;

        // Bala enemiga: no tiene colorType, siempre aplica Gray
        if (other.GetComponent<EnemyBullet>() != null) return PaintColor.Gray;

        return null;
    }

    // ============================================================
    // RATTLE — vibración breve con decaimiento en cada impacto
    // ============================================================
    void IniciarRattle()
    {
        if (rattleRoutine != null) StopCoroutine(rattleRoutine);
        rattleRoutine = StartCoroutine(RattleCoroutine());
    }

    IEnumerator RattleCoroutine()
    {
        float t = 0f;
        while (t < rattleDuracion)
        {
            // Amplitud decrece conforme pasa el tiempo (1 → 0)
            float amortiguacion = 1f - (t / rattleDuracion);
            float ox = Mathf.Sin(t * 65f)        * rattleAmplitud * amortiguacion;
            float oy = Mathf.Sin(t * 50f + 1.3f) * rattleAmplitud * 0.35f * amortiguacion;
            transform.localPosition = posicionLocalOriginal + new Vector3(ox, oy, 0f);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = posicionLocalOriginal;
        rattleRoutine = null;
    }

    // ============================================================
    // TINTE DE COLOR — permanente hasta que la lluvia limpie la casa.
    // Un impacto nuevo reemplaza el tinte anterior de inmediato.
    // Se cancela cualquier coroutine de tinte anterior (por si alguna
    // quedó pendiente de una versión anterior del script en caliente).
    // ============================================================
    void AplicarTinte(PaintColor color)
    {
        if (sr == null) return;
        if (tintRoutine != null) { StopCoroutine(tintRoutine); tintRoutine = null; }
        Color colorPintura = PaintColorUtils.ToUnityColor(color);
        sr.color = Color.Lerp(colorOriginal, colorPintura, tintStrength);
    }
}
