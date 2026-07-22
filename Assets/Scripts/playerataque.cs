using UnityEngine;
using UnityEngine.InputSystem;

public class playerataque : MonoBehaviour
{
    [Header("Arma 1 - Spray")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    [Tooltip("Segundos antes de poder usar el spray de nuevo tras soltarlo")]
    public float cooldownSpray = 0.5f;

    [Header("Arma 2 - Pincel (Proyectil)")]
    public GameObject projectilePrefab;
    public Transform projectileFirePoint;
    [Tooltip("Segundos entre disparos del pincel (mantener clic = auto-disparo)")]
    public float cooldownPincel = 0.25f;

    [Header("Arma 3 - Rodillo (Melee)")]
    public GameObject meleePrefab;
    public Transform meleeFirePoint;
    [Tooltip("Segundos entre golpes del rodillo")]
    public float cooldownRodillo = 0.5f;

    private GameObject currentSpray;
    private float timerSpray   = 0f;
    private float timerPincel  = 0f;
    private float timerRodillo = 0f;

    // Feedback de playtest: vacantes de animación de disparo del Forastero
    // (Animator "frottnguy_0", parámetros Trigger "Disparar" + Int
    // "ArmaActual" 0=Pincel/1=Spray/2=Rodillo, ver estados placeholder
    // "Disparo ..."). Si no hay Animator en el GameObject no se rompe nada.
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (WeaponManager.instance == null) return;

        // Descontar timers
        if (timerSpray   > 0f) timerSpray   -= Time.deltaTime;
        if (timerPincel  > 0f) timerPincel  -= Time.deltaTime;
        if (timerRodillo > 0f) timerRodillo -= Time.deltaTime;

        var weapon = WeaponManager.instance.currentWeapon;

        // --- SPRAY ---
        if (weapon == WeaponManager.WeaponType.Spray)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && timerSpray <= 0f
                && AmmoManager.instance != null && AmmoManager.instance.ConsumeAmmo(WeaponManager.instance.currentColor))
                SpawnSpray();

            if (Mouse.current.leftButton.isPressed && currentSpray != null)
                UpdateSprayDirection();

            if (Mouse.current.leftButton.wasReleasedThisFrame && currentSpray != null)
            {
                Destroy(currentSpray);
                currentSpray = null;
                timerSpray = cooldownSpray * CooldownMult; // cooldown al soltar (reducido por mejoras)
            }
        }
        else
        {
            // Si cambió de arma con el spray activo, destruirlo
            if (currentSpray != null)
            {
                Destroy(currentSpray);
                currentSpray = null;
            }
        }

        // --- PINCEL (auto-disparo al mantener clic) ---
        if (weapon == WeaponManager.WeaponType.Projectile)
        {
            if (Mouse.current.leftButton.isPressed && timerPincel <= 0f
                && AmmoManager.instance != null && AmmoManager.instance.ConsumeAmmo(WeaponManager.instance.currentColor))
                SpawnProjectile();
        }

        // --- RODILLO ---
        if (weapon == WeaponManager.WeaponType.Melee)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && timerRodillo <= 0f
                && AmmoManager.instance != null && AmmoManager.instance.ConsumeAmmo(WeaponManager.instance.currentColor))
                SpawnMelee();
        }
    }

    // Multiplicadores de UpgradeSystem (GDD 3.7: Mejoras de Arma). Si todavía
    // no se ganó ninguna mejora, UpgradeSystem.Instance puede ser null: en
    // ese caso se usan los valores neutros (1f) y el arma se comporta
    // exactamente igual que antes de este sistema.
    float DanoMult => UpgradeSystem.Instance != null ? UpgradeSystem.Instance.ArmaDanoMultiplier : 1f;
    float CooldownMult => UpgradeSystem.Instance != null ? UpgradeSystem.Instance.ArmaCooldownMultiplier : 1f;
    float VelocidadDisparoMult => UpgradeSystem.Instance != null ? UpgradeSystem.Instance.ArmaVelocidadDisparoMultiplier : 1f;
    float AreaMult => UpgradeSystem.Instance != null ? UpgradeSystem.Instance.ArmaAreaEfectoMultiplier : 1f;

    void DispararAnimacion(int arma)
    {
        if (animator == null) return;
        animator.SetInteger("ArmaActual", arma);
        animator.SetTrigger("Disparar");
    }

    void SpawnSpray()
    {
        DispararAnimacion(1); // 1 = Spray
        if (SfxManager.Instance != null) SfxManager.Instance.PlayAerosol();
        Vector2 dir = GetMouseDirection(firePoint);
        currentSpray = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity, firePoint);
        currentSpray.transform.localScale *= AreaMult;
        spray s = currentSpray.GetComponent<spray>();
        if (s != null)
        {
            s.colorType = WeaponManager.instance.currentColor;
            s.damage = Mathf.RoundToInt(s.damage * DanoMult);
            s.Init(dir);
        }
    }

    void UpdateSprayDirection()
    {
        Vector2 dir = GetMouseDirection(firePoint);
        spray s = currentSpray.GetComponent<spray>();
        if (s != null) s.Init(dir);
    }

    void SpawnProjectile()
    {
        DispararAnimacion(0); // 0 = Pincel
        if (SfxManager.Instance != null) SfxManager.Instance.PlayPincelazo();
        Vector2 dir = GetMouseDirection(projectileFirePoint);
        GameObject bullet = Instantiate(projectilePrefab, projectileFirePoint.position, Quaternion.identity);
        bullet.transform.localScale *= AreaMult;
        Projectile p = bullet.GetComponent<Projectile>();
        if (p != null)
        {
            p.colorType = WeaponManager.instance.currentColor;
            p.damage = Mathf.RoundToInt(p.damage * DanoMult);
            p.Init(dir);
        }
        // "Mayor velocidad de disparo" = menos espera entre disparos del Pincel.
        timerPincel = cooldownPincel / Mathf.Max(0.1f, VelocidadDisparoMult);
    }

    void SpawnMelee()
    {
        DispararAnimacion(2); // 2 = Rodillo
        if (SfxManager.Instance != null) SfxManager.Instance.PlayRodillo();
        Vector2 dir = GetMouseDirection(meleeFirePoint);
        float offset = 1f;
        Vector3 spawnPos = meleeFirePoint.position + (Vector3)(dir * offset);
        GameObject melee = Instantiate(meleePrefab, spawnPos, Quaternion.identity);
        melee.transform.localScale *= AreaMult;
        rodillo r = melee.GetComponent<rodillo>();
        if (r != null)
        {
            r.colorType = WeaponManager.instance.currentColor;
            r.damage = Mathf.RoundToInt(r.damage * DanoMult);
            r.Init(dir);
        }
        timerRodillo = cooldownRodillo * CooldownMult;
    }

    Vector2 GetMouseDirection(Transform from)
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0;
        return (mousePos - from.position).normalized;
    }
}
