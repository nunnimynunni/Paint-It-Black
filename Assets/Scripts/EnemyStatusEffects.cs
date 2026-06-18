using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Maneja el sistema de "pintura": cuenta impactos por color y aplica
// el efecto correspondiente al llegar a la cantidad necesaria.
//
// Mapeo de color -> efecto (ver pedido original):
//   Rojo    (Red)    -> Frenzy   (cólera, ataca a cualquiera, más rápido)
//   Amarillo(Yellow)  -> Pacified (pacífico, no ataca)
//   Violeta (Purple)  -> Fear     (miedo, evita al jugador)
//   Verde   (Green)   -> Poison   (daño en el tiempo)
//   Azul    (Blue)    -> Slow     (lento y pega menos)
//   Aguado  (Gray)    -> Sin efecto (no cuenta, no tiñe)
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyStatusEffects : MonoBehaviour
{
    public enum StatusType { None, Frenzy, Pacified, Fear, Slow }

    [Header("Config")]
    [Tooltip("Cantidad de impactos del mismo color para activar el efecto")]
    public int hitsToTrigger = 5;
    [Tooltip("Cuánta vida máxima (%) pierde por veneno, repartida durante poisonDuration")]
    public float poisonPercentOfMaxHP = 30f;
    public float poisonDuration = 5f;
    [Tooltip("Qué tan fuerte se nota el tinte de color sobre el sprite original")]
    [Range(0f, 1f)] public float tintStrength = 0.7f;
    [Tooltip("Cuánto dura un efecto de comportamiento (Frenzy/Pacificado/Miedo/Lento) antes de volver a la normalidad. Se reinicia si vuelve a juntar 5 impactos del mismo color mientras está activo.")]
    public float effectDuration = 12f;

    public StatusType CurrentStatus { get; private set; } = StatusType.None;
    public bool IsPoisoned { get; private set; } = false;

    // Color "base" actual del sprite (el original si no hay efecto, o el tinte del efecto activo).
    // EnemyHealth lo usa para no perder el tinte al terminar el flash de un golpe.
    public Color CurrentBaseColor { get; private set; }

    private Dictionary<PaintColor, int> hits = new Dictionary<PaintColor, int>();
    private SpriteRenderer sr;
    private Color baseColor;
    private EnemyHealth health;
    private Coroutine poisonRoutine;
    private Coroutine effectRoutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();
    }

    void Start()
    {
        baseColor = sr.color;
        CurrentBaseColor = baseColor;
    }

    // --- Multiplicadores que leen los scripts de IA/ataque ---
    public float MoveSpeedMultiplier
    {
        get
        {
            if (CurrentStatus == StatusType.Frenzy) return 1.5f;
            if (CurrentStatus == StatusType.Slow) return 0.5f;
            return 1f;
        }
    }

    public float AttackSpeedMultiplier => CurrentStatus == StatusType.Frenzy ? 1.5f : 1f;

    public float DamageMultiplier
    {
        get
        {
            if (CurrentStatus == StatusType.Frenzy) return 1.5f;
            if (CurrentStatus == StatusType.Slow) return 0.6f;
            return 1f;
        }
    }

    // Pacificado: no ataca, se mueve errático sin intención de combate
    public bool CanAct => CurrentStatus != StatusType.Pacified;
    public bool IsFrenzied => CurrentStatus == StatusType.Frenzy;
    public bool IsFearful => CurrentStatus == StatusType.Fear;
    public bool IsPacified => CurrentStatus == StatusType.Pacified;

    // Llamado desde EnemyHealth.TakeDamage cada vez que el enemigo recibe un disparo
    public void RegisterHit(PaintColor color)
    {
        if (color == PaintColor.Gray) return; // Aguado: sin efectos, no cuenta

        if (!hits.ContainsKey(color)) hits[color] = 0;
        hits[color]++;

        if (hits[color] >= hitsToTrigger)
            ApplyEffect(color);
    }

    void ApplyEffect(PaintColor color)
    {
        // Reiniciar contadores: hace falta volver a juntar 5 impactos para cambiar de nuevo
        var keys = new List<PaintColor>(hits.Keys);
        foreach (var k in keys) hits[k] = 0;

        switch (color)
        {
            case PaintColor.Red:
                CurrentStatus = StatusType.Frenzy;
                RestartEffectTimer();
                break;
            case PaintColor.Yellow:
                CurrentStatus = StatusType.Pacified;
                RestartEffectTimer();
                break;
            case PaintColor.Purple:
                CurrentStatus = StatusType.Fear;
                RestartEffectTimer();
                break;
            case PaintColor.Blue:
                CurrentStatus = StatusType.Slow;
                RestartEffectTimer();
                break;
            case PaintColor.Green:
                // El veneno corre en paralelo, no pisa el estado de comportamiento actual
                if (poisonRoutine != null) StopCoroutine(poisonRoutine);
                poisonRoutine = StartCoroutine(PoisonRoutine());
                break;
        }

        ApplyTint(color);
    }

    void ApplyTint(PaintColor color)
    {
        Color target = PaintColorUtils.ToUnityColor(color);
        Color tinted = Color.Lerp(baseColor, target, tintStrength);
        sr.color = tinted;
        CurrentBaseColor = tinted; // queda como "base" hasta que el efecto expire o se reemplace
    }

    // Reinicia el cronómetro de duración del efecto de comportamiento actual (no aplica a Veneno,
    // que tiene su propia duración independiente). Si ya había un timer corriendo lo reemplaza,
    // así que volver a pintar al enemigo del mismo color "recarga" el efecto en vez de cortarlo.
    void RestartEffectTimer()
    {
        if (effectRoutine != null) StopCoroutine(effectRoutine);
        effectRoutine = StartCoroutine(EffectDurationRoutine());
    }

    IEnumerator EffectDurationRoutine()
    {
        yield return new WaitForSeconds(effectDuration);
        CurrentStatus = StatusType.None;
        CurrentBaseColor = baseColor;
        sr.color = baseColor;
        effectRoutine = null;
    }

    IEnumerator PoisonRoutine()
    {
        IsPoisoned = true;
        int ticks = Mathf.Max(1, Mathf.RoundToInt(poisonDuration));
        int totalDamage = Mathf.RoundToInt(health.maxHP * (poisonPercentOfMaxHP / 100f));
        int perTick = Mathf.Max(1, totalDamage / ticks);

        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(1f);
            if (health == null || !health.IsAlive()) yield break;
            health.TakeRawDamage(perTick);
        }

        IsPoisoned = false;

        // Si no hay otro efecto de comportamiento corriendo en paralelo, vuelve al color original
        if (effectRoutine == null && CurrentStatus == StatusType.None)
        {
            CurrentBaseColor = baseColor;
            sr.color = baseColor;
        }
    }
}
