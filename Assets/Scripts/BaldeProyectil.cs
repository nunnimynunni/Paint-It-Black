using System.Collections;
using UnityEngine;

public class BaldeProyectil : MonoBehaviour
{
    [Header("Daño de impacto (GDD: 60)")]
    public int damage = 60;
    public float aoeRadius = 1.5f;

    [Header("Charco")]
    public GameObject charcoPrefab;

    [HideInInspector] public PaintColor colorType;

    private bool yaPego = false;

    public void Init(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = PaintColorUtils.ToUnityColor(colorType);
    }

    void Start()
    {
        AplicarDano();
        StartCoroutine(EsperarAnimacionYDejarCharco());
    }

    void AplicarDano()
    {
        if (yaPego) return;
        yaPego = true;

        float danoMult = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.ArmaDanoMultiplier : 1f;
        float areaMult = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.ArmaAreaEfectoMultiplier : 1f;
        int danoFinal = Mathf.RoundToInt(damage * danoMult);
        float radioFinal = aoeRadius * areaMult;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radioFinal);
        foreach (Collider2D col in hits)
        {
            EnemyHealth eh = col.GetComponent<EnemyHealth>();
            if (eh != null)
                eh.TakeDamage(danoFinal, colorType);
        }

        if (SfxManager.Instance != null) SfxManager.Instance.PlayImpacto();
    }

    IEnumerator EsperarAnimacionYDejarCharco()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            yield return null;
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(info.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (charcoPrefab != null)
        {
            GameObject charco = Instantiate(charcoPrefab, transform.position, Quaternion.identity);
            BaldeCharco bc = charco.GetComponent<BaldeCharco>();
            if (bc != null)
                bc.Init(colorType);
        }

        transform.SetParent(null);
        Destroy(gameObject);
    }
}
