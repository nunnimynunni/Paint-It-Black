using UnityEngine;

// EnemyHealth.cs
// Agregar este componente a cada GameObject enemigo en Unity.
// El enemigo también necesita un BoxCollider2D con "Is Trigger = true".
//
// Sistema de colores:
//   - Asignar sprites "afectados" en el Inspector (uno por color).
//   - Si no se asigna sprite para un color, el efecto visual no cambia (pero el daño sí aplica).
//   - En el futuro se pueden agregar efectos adicionales por color (slow, quema, etc.) dentro de ApplyColorEffect().

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHP = 3;
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

    void Start()
    {
        currentHP = maxHP;
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // Guardar sprite base si no se asignó manualmente
        if (sr != null && spriteBase == null)
            spriteBase = sr.sprite;
    }

    // Llamado por los scripts de proyectil cuando impactan al enemigo
    public void TakeDamage(int amount, PaintColor color)
    {
        if (currentHP <= 0) return; // ya muerto, ignorar

        currentHP -= amount;

        // Reproducir animación de golpe si existe
        if (anim != null)
            anim.SetTrigger("OnHit");

        // Aplicar efecto visual del color
        ApplyColorEffect(color);

        if (currentHP <= 0)
            Die();
    }

    void ApplyColorEffect(PaintColor color)
    {
        if (sr == null) return;

        switch (color)
        {
            case PaintColor.Red:
                if (spriteAfectadoRojo != null) sr.sprite = spriteAfectadoRojo;
                // FUTURO: ej: quema, daño extra
                break;

            case PaintColor.Blue:
                if (spriteAfectadoAzul != null) sr.sprite = spriteAfectadoAzul;
                // FUTURO: ej: congelamiento
                break;

            case PaintColor.Yellow:
                if (spriteAfectadoAmarillo != null) sr.sprite = spriteAfectadoAmarillo;
                // FUTURO: ej: aturdimiento
                break;

            case PaintColor.Green:
                if (spriteAfectadoVerde != null) sr.sprite = spriteAfectadoVerde;
                // FUTURO: ej: slow, veneno
                break;

            case PaintColor.Purple:
                if (spriteAfectadoVioleta != null) sr.sprite = spriteAfectadoVioleta;
                // FUTURO: ej: confusión, inversión de controles
                break;

            case PaintColor.Gray:
                if (spriteAfectadoGris != null) sr.sprite = spriteAfectadoGris;
                // FUTURO: ej: debuff, reducción de velocidad
                break;

            default:
                break;
        }
    }

    void Die()
    {
        // FUTURO: animación de muerte, drops de items, sonido, etc.
        Destroy(gameObject);
    }

    // Utilidades para otros sistemas
    public int GetCurrentHP() => currentHP;
    public bool IsAlive() => currentHP > 0;
}
