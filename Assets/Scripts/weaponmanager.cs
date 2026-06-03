using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager instance;

    public enum WeaponType { Spray, Projectile, Melee }
    public WeaponType currentWeapon = WeaponType.Spray;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            currentWeapon = WeaponType.Spray;

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            currentWeapon = WeaponType.Projectile;

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            currentWeapon = WeaponType.Melee;
    }
}