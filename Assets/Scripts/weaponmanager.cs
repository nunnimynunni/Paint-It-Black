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

        // Control cicla entre los colores disponibles (antes era Bloq Mayús,
        // feedback de playtest pidió cambiarlo a Control)
        if (Keyboard.current.ctrlKey.wasPressedThisFrame)
            CycleColor();

        // Feedback de playtest: "al acabarse las gotas de un color se debe
        // cambiar automático al siguiente". Si el color activo se quedó sin
        // munición, salta solo al próximo color que sí tenga.
        if (AmmoManager.instance != null && !AmmoManager.instance.HasAmmo(currentColor))
            CambiarAlSiguienteColorConMunicion();
    }

    void CycleColor()
    {
        int total = System.Enum.GetValues(typeof(PaintColor)).Length;
        currentColor = (PaintColor)(((int)currentColor + 1) % total);
        // Orden: None → Green → Red → Blue → Yellow → None → ...
    }

    // Recorre los colores en el mismo orden que CycleColor() empezando
    // desde el actual, y se queda con el primero que todavía tenga
    // munición. Si ninguno tiene, no cambia nada (todos vacíos).
    void CambiarAlSiguienteColorConMunicion()
    {
        int total = System.Enum.GetValues(typeof(PaintColor)).Length;
        for (int i = 1; i <= total; i++)
        {
            PaintColor candidato = (PaintColor)(((int)currentColor + i) % total);
            if (AmmoManager.instance.HasAmmo(candidato))
            {
                currentColor = candidato;
                return;
            }
        }
    }
}
