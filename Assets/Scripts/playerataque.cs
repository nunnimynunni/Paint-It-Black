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
                timerSpray = cooldownSpray; // cooldown al soltar
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

    void SpawnSpray()
    {
        Vector2 dir = GetMouseDirection(firePoint);
        currentSpray = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity, firePoint);
        spray s = currentSpray.GetComponent<spray>();
        if (s != null)
        {
            s.colorType = WeaponManager.instance.currentColor;
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
        Vector2 dir = GetMouseDirection(projectileFirePoint);
        GameObject bullet = Instantiate(projectilePrefab, projectileFirePoint.position, Quaternion.identity);
        Projectile p = bullet.GetComponent<Projectile>();
        if (p != null)
        {
            p.colorType = WeaponManager.instance.currentColor;
            p.Init(dir);
        }
        timerPincel = cooldownPincel;
    }

    void SpawnMelee()
    {
        Vector2 dir = GetMouseDirection(meleeFirePoint);
        float offset = 1f;
        Vector3 spawnPos = meleeFirePoint.position + (Vector3)(dir * offset);
        GameObject melee = Instantiate(meleePrefab, spawnPos, Quaternion.identity);
        rodillo r = melee.GetComponent<rodillo>();
        if (r != null)
        {
            r.colorType = WeaponManager.instance.currentColor;
            r.Init(dir);
        }
        timerRodillo = cooldownRodillo;
    }

    Vector2 GetMouseDirection(Transform from)
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0;
        return (mousePos - from.position).normalized;
    }
}
