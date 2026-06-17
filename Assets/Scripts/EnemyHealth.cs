using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHP = 100;
    private int currentHP;

    public GameObject damagePopupPrefab;

    private SpriteRenderer sr;
    private Animator anim;
    private Color colorOriginal;

    private Color hitColor;
    private float hitTimer = 0f;

    void Start()
    {
        currentHP = maxHP;
        sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (sr != null) colorOriginal = sr.color;
    }

    void LateUpdate()
    {
        if (sr == null || hitTimer <= 0f) return;
        hitTimer -= Time.deltaTime;
        sr.color = hitTimer > 0f ? hitColor : colorOriginal;
    }

    public void TakeDamage(int amount, PaintColor color)
    {
        if (currentHP <= 0) return;

        currentHP -= amount;

        if (anim != null) anim.SetTrigger("OnHit");

        hitColor = PaintColorUtils.ToUnityColor(color);
        hitColor.a = 0.6f;
        hitTimer = 0.2f;

        DamagePopup.Create(damagePopupPrefab, transform.position, amount, PaintColorUtils.ToUnityColor(color));

        if (currentHP <= 0)
            Destroy(gameObject);
    }

    public int GetCurrentHP() => currentHP;
    public bool IsAlive() => currentHP > 0;
}
