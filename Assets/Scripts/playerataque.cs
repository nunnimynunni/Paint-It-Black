using UnityEngine;
using UnityEngine.InputSystem;

public class playerataque : MonoBehaviour
{
    [Header("Arma 1 - Spray")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Arma 2 - Proyectil")]
    public GameObject projectilePrefab;
    public Transform projectileFirePoint;

    [Header("Arma 3 - Melee")]
    public GameObject meleePrefab;
    public Transform meleeFirePoint;

    private GameObject currentSpray;

   void Update()
{
    if (WeaponManager.instance == null) return;
    
    var weapon = WeaponManager.instance.currentWeapon;

    // --- SPRAY ---
    if (weapon == WeaponManager.WeaponType.Spray)
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            SpawnSpray();

        if (Mouse.current.leftButton.isPressed && currentSpray != null)
            UpdateSprayDirection();

        if (Mouse.current.leftButton.wasReleasedThisFrame && currentSpray != null)
        {
            Destroy(currentSpray);
            currentSpray = null;
        }
    }

    if (weapon == WeaponManager.WeaponType.Projectile)
    {
        if (currentSpray != null) { Destroy(currentSpray); currentSpray = null; }

        if (Mouse.current.leftButton.wasPressedThisFrame)
            SpawnProjectile();
    }

    if (weapon == WeaponManager.WeaponType.Melee)
    {
        if (currentSpray != null) { Destroy(currentSpray); currentSpray = null; }

        if (Mouse.current.leftButton.wasPressedThisFrame)
            SpawnMelee();
    }
}

    void SpawnSpray()
    {
        Vector2 dir = GetMouseDirection(firePoint);
        currentSpray = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity, firePoint);
        currentSpray.GetComponent<spray>().Init(dir);
    }

    void UpdateSprayDirection()
    {
        Vector2 dir = GetMouseDirection(firePoint);
        currentSpray.GetComponent<spray>().Init(dir);
    }

    void SpawnProjectile()
    {
        Vector2 dir = GetMouseDirection(projectileFirePoint);
        GameObject bullet = Instantiate(projectilePrefab, projectileFirePoint.position, Quaternion.identity);
        bullet.GetComponent<Projectile>().Init(dir);
    }

   void SpawnMelee()
{
    Vector2 dir = GetMouseDirection(meleeFirePoint);
    
    // Cuánto se aleja del personaje — ajustá este número a tu gusto
    float offset = 1f;
    
    Vector3 spawnPos = meleeFirePoint.position + (Vector3)(dir * offset);
    
    GameObject melee = Instantiate(meleePrefab, spawnPos, Quaternion.identity);
    melee.GetComponent<rodillo>().Init(dir);
}

    Vector2 GetMouseDirection(Transform from)
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(
            Mouse.current.position.ReadValue()
        );
        mousePos.z = 0;
        return (mousePos - from.position).normalized;
    }
}