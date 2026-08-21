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

    [Header("Arma 4 - Balde (AoE + Charco)")]
    public GameObject baldePrefab;
    public Transform baldeFirePoint;
    [Tooltip("Segundos entre lanzamientos del balde")]
    public float cooldownBalde = 1.5f;
    [Tooltip("Unidades de munición que consume el balde (GDD: 5)")]
    public int baldeAmmoCost = 5;

    [Header("Granada - Frasco de Barniz")]
    public GameObject granadaPrefab;
    public float cooldownGranada = 2f;
    public int granadasMax = 5;
    private int granadasActuales = 3;
    private float timerGranada = 0f;

    private GameObject currentSpray;
    private GameObject currentBalde;
    private float timerSpray   = 0f;
    private float timerPincel  = 0f;
    private float timerRodillo = 0f;
    private float timerBalde   = 0f;

    // Feedback de playtest: vacantes de animación de disparo del Forastero
    // (Animator "frottnguy_0", parámetros Trigger "Disparar" + Int
    // "ArmaActual" 0=Pincel/1=Spray/2=Rodillo, ver estados placeholder
    // "Disparo ..."). Si no hay Animator en el GameObject no se rompe nada.
    private Animator animator;
    // Para setear el flipX de la animación de balde según dirección del mouse
    private PlayerAnimator playerAnim;

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerAnim = GetComponent<PlayerAnimator>();
    }

    void Update()
    {
        if (WeaponManager.instance == null) return;

        // Descontar timers
        if (timerSpray   > 0f) timerSpray   -= Time.deltaTime;
        if (timerPincel  > 0f) timerPincel  -= Time.deltaTime;
        if (timerRodillo > 0f) timerRodillo -= Time.deltaTime;
        if (timerBalde   > 0f) timerBalde   -= Time.deltaTime;
        if (timerGranada > 0f) timerGranada -= Time.deltaTime;

        // --- GRANADA (clic derecho, independiente del arma activa) ---
        if (Mouse.current.rightButton.wasPressedThisFrame && timerGranada <= 0f && granadasActuales > 0)
            LanzarGranada();

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

        // --- BALDE ---
        if (weapon == WeaponManager.WeaponType.Balde)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && timerBalde <= 0f
                && AmmoManager.instance != null && AmmoManager.instance.ConsumeAmmo(WeaponManager.instance.currentColor, baldeAmmoCost))
                SpawnBalde();

            // Actualizar rotación hacia el mouse mientras el balde existe
            if (currentBalde != null)
                UpdateBaldeDirection();
        }
        else
        {
            // Si cambió de arma con el balde activo, destruirlo
            if (currentBalde != null)
            {
                Destroy(currentBalde);
                currentBalde = null;
            }
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
        // Sin animación de disparo: la animación actual sigue sin interrupciones.
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

    // Igual que UpdateSprayDirection: llama a Init() cada frame
    void UpdateBaldeDirection()
    {
        Vector2 dir = GetMouseDirection(firePoint);
        BaldeProyectil bp = currentBalde.GetComponent<BaldeProyectil>();
        if (bp != null) bp.Init(dir);
    }

    void SpawnProjectile()
    {
        // Sin animación de disparo: la animación actual sigue sin interrupciones.
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
        // Sin animación de disparo: la animación actual sigue sin interrupciones.
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

    void SpawnBalde()
    {
        if (SfxManager.Instance != null) SfxManager.Instance.PlayImpacto();
        Vector2 dir = GetMouseDirection(firePoint);
        currentBalde = Instantiate(baldePrefab, firePoint.position, Quaternion.identity, firePoint);
        currentBalde.transform.localScale *= AreaMult;
        BaldeProyectil bp = currentBalde.GetComponent<BaldeProyectil>();
        if (bp != null)
        {
            bp.colorType = WeaponManager.instance.currentColor;
            bp.damage = Mathf.RoundToInt(bp.damage * DanoMult);
            bp.Init(dir);
        }
        timerBalde = cooldownBalde * CooldownMult;
    }

    void LanzarGranada()
    {
        if (granadaPrefab == null) return;
        granadasActuales--;
        timerGranada = cooldownGranada;

        Vector2 dir = GetMouseDirection(firePoint);
        GameObject granada = Instantiate(granadaPrefab, firePoint.position, Quaternion.identity);
        GranadaBarniz gb = granada.GetComponent<GranadaBarniz>();
        if (gb != null)
            gb.Init(dir);
    }

    // Llamado por GranadaPickup al recoger
    public void AgregarGranadas(int cantidad)
    {
        granadasActuales = Mathf.Min(granadasActuales + cantidad, granadasMax);
    }

    public int GetGranadas() => granadasActuales;

    Vector2 GetMouseDirection(Transform from)
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0;
        return (mousePos - from.position).normalized;
    }
}
