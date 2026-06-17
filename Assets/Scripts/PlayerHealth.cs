using UnityEngine;

// Vida del Forastero (jugador). Poner en el mismo GameObject que PlayerAnimator.
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    public int maxHealth = 16;
    [Tooltip("Segundos que el sprite queda en rojo tras recibir un golpe")]
    public float hitFlashDuration = 1f;
    [Tooltip("Segundos de invulnerabilidad tras recibir un golpe (evita que varios enemigos lo maten en el mismo instante)")]
    public float invulnerabilityDuration = 0.6f;

    private int currentHealth;
    private SpriteRenderer sr;
    private Color colorOriginal;
    private float flashTimer = 0f;
    private float invulnTimer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        colorOriginal = sr.color;
    }

    void LateUpdate()
    {
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;

        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;
        sr.color = flashTimer > 0f ? Color.red : colorOriginal;
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (invulnTimer > 0f) return; // todavía en ventana de invulnerabilidad del golpe anterior

        currentHealth -= amount;
        flashTimer = hitFlashDuration;
        invulnTimer = invulnerabilityDuration;
        sr.color = Color.red;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsAlive() => currentHealth > 0;
}
