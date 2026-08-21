using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaldeCharco : MonoBehaviour
{
    [Header("Duración (GDD: 6 segundos)")]
    public float duracion = 6f;

    [Header("Daño por tick (GDD: 10 cada 2s)")]
    public int danoPorTick = 10;
    public float intervaloTick = 2f;

    [Header("Expansión del collider")]
    public float radioInicial = 0.1f;
    public float radioFinal = 0.5f;
    public float tiempoDeExpansion = 1.5f;

    private PaintColor colorType;
    private CircleCollider2D circleCol;
    private float tiempoVivo = 0f;
    private float timerTick = 0f;
    private HashSet<EnemyHealth> enemigosAdentro = new HashSet<EnemyHealth>();

    public void Init(PaintColor color)
    {
        colorType = color;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = PaintColorUtils.ToUnityColor(color);
    }

    void Start()
    {
        circleCol = GetComponent<CircleCollider2D>();
        if (circleCol != null)
            circleCol.radius = radioInicial;

        Destroy(gameObject, duracion);
    }

    void Update()
    {
        tiempoVivo += Time.deltaTime;

        // Expandir el collider gradualmente
        if (circleCol != null && tiempoVivo < tiempoDeExpansion)
        {
            float t = tiempoVivo / tiempoDeExpansion;
            circleCol.radius = Mathf.Lerp(radioInicial, radioFinal, t);
        }

        // Daño periódico
        timerTick += Time.deltaTime;
        if (timerTick >= intervaloTick)
        {
            timerTick = 0f;
            AplicarDano();
        }
    }

    void AplicarDano()
    {
        float danoMult = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.ArmaDanoMultiplier : 1f;
        int danoFinal = Mathf.RoundToInt(danoPorTick * danoMult);
        enemigosAdentro.RemoveWhere(e => e == null || !e.IsAlive());
        foreach (EnemyHealth enemy in enemigosAdentro)
        {
            if (enemy != null && enemy.IsAlive())
                enemy.TakeDamage(danoFinal, colorType);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth eh = other.GetComponent<EnemyHealth>();
        if (eh != null) enemigosAdentro.Add(eh);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        EnemyHealth eh = other.GetComponent<EnemyHealth>();
        if (eh != null) enemigosAdentro.Remove(eh);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            Vector3 center = transform.TransformPoint(col.offset);
            float radio = col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            Gizmos.DrawWireSphere(center, radio);
        }
    }
}
