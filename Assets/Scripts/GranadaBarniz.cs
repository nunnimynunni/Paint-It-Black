using System.Collections;
using UnityEngine;

public class GranadaBarniz : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 8f;

    [Header("Rotación visual")]
    public float velocidadRotacion = 360f;

    [Header("Explosión")]
    public float timerExplosion = 1.5f;
    public float radioExplosion = 2f;
    public float duracionInmovilizacion = 5f;

    [Header("Visual (opcional)")]
    public GameObject explosionVFXPrefab;

    private Rigidbody2D rb;
    private bool exploto = false;

    public void Init(Vector2 dir)
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = dir.normalized * velocidad;

        StartCoroutine(TimerYExplotar());
    }

    void Update()
    {
        if (!exploto)
            transform.Rotate(0, 0, velocidadRotacion * Time.deltaTime);
    }

    IEnumerator TimerYExplotar()
    {
        yield return new WaitForSeconds(timerExplosion);
        Explotar();
    }

    void Explotar()
    {
        if (exploto) return;
        exploto = true;

        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radioExplosion);
        foreach (Collider2D col in hits)
        {
            EnemyStatusEffects status = col.GetComponent<EnemyStatusEffects>();
            if (status != null)
                status.ApplyImmobilize(duracionInmovilizacion);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radioExplosion);
    }
}
