using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager instance;

    public enum WeaponType { Spray, Projectile, Melee }
    public WeaponType currentWeapon = WeaponType.Spray;

    // Color activo del disparo — se pasa a cada proyectil al instanciarlo
    // FUTURO: este valor lo va a setear el sistema de selección de color del jugador
    public PaintColor currentColor = PaintColor.Red; // empieza en rojo por defecto

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        // Cambio de arma
        if (Keyboard.current.digit1Key.wasPressedThisFrame) currentWeapon = WeaponType.Spray;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) currentWeapon = WeaponType.Projectile;
        if (Keyboard.current.digit3Key.wasPressedThisFrame) currentWeapon = WeaponType.Melee;

        // Bloq Mayús cicla entre los colores disponibles
        if (Keyboard.current.capsLockKey.wasPressedThisFrame)
            CycleColor();
    }

    void CycleColor()
    {
        int total = System.Enum.GetValues(typeof(PaintColor)).Length;
        currentColor = (PaintColor)(((int)currentColor + 1) % total);
        // Orden: None → Green → Red → Blue → Yellow → None → ...
    }
}
