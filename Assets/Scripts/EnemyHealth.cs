using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHP = 4;
    private int currentHP;

    [Header("Sprites por color (asignar en Inspector)")]
    public Sprite spriteBase;
    public Sprite spriteAfectadoRojo;
    public Sprite spriteAfectadoAzul;
    public Sprite spriteAfectadoAmarillo;
    public Sprite spriteAfectadoVerde;
    public Sprite spriteAfectadoVioleta;
    public Sprite spriteAfectadoGris;

    private SpriteRenderer sr;
    private Animator anim;
    private Color colorActual;

    void Start()
    {
        currentHP = maxHP;
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        if (sr != null && spriteBase == null)
            spriteBase = sr.sprite;
    }

    public void TakeDamage(int amount, PaintColor color)
    {
        if (currentHP <= 0) return;

        currentHP -= amount;

        if (anim != null) anim.SetTrigger("OnHit");

        // Color del proyectil con transparencia
        Color c = PaintColorUtils.ToUnityColor(color);
        c.a = 0.6f;
        colorActual = c;
        if (sr != null) sr.color = colorActual;

        ApplySpriteForColor(color);

        if (currentHP <= 0)
            Die();
        else
            StartCoroutine(ParpadeaOculto());
    }

    IEnumerator ParpadeaOculto()
    {
        if (sr != null) sr.enabled = false;
        yield return new WaitForSeconds(0.5f);
        if (sr != null) sr.enabled = true;
    }

    void ApplySpriteForColor(PaintColor color)
    {
        if (sr == null) return;
        Sprite s = color switch
        {
            PaintColor.Red    => spriteAfectadoRojo,
            PaintColor.Blue   => spriteAfectadoAzul,
            PaintColor.Yellow => spriteAfectadoAmarillo,
            PaintColor.Green  => spriteAfectadoVerde,
            PaintColor.Purple => spriteAfectadoVioleta,
            PaintColor.Gray   => spriteAfectadoGris,
            _ => null
        };
        if (s != null) sr.sprite = s;
    }

    void Die()
    {
        Destroy(gameObject);
    }

    public int GetCurrentHP() => currentHP;
    public bool IsAlive() => currentHP > 0;
}
