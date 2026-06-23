using System.Collections.Generic;
using UnityEngine;

public class AmmoManager : MonoBehaviour
{
    public static AmmoManager instance;

    public const int MaxAmmoPerColor = 20;

    private Dictionary<PaintColor, int> ammo = new Dictionary<PaintColor, int>();

    void Awake()
    {
        instance = this;

        // Inicializar 20 balas por color
        foreach (PaintColor color in System.Enum.GetValues(typeof(PaintColor)))
            ammo[color] = MaxAmmoPerColor;
    }

    // Devuelve true si habia municion y se pudo consumir
    public bool ConsumeAmmo(PaintColor color, int amount = 1)
    {
        if (!ammo.ContainsKey(color)) return false;
        if (ammo[color] <= 0) return false;

        ammo[color] = Mathf.Max(0, ammo[color] - amount);
        return true;
    }

    public int GetAmmo(PaintColor color)
    {
        return ammo.ContainsKey(color) ? ammo[color] : 0;
    }

    public bool HasAmmo(PaintColor color)
    {
        return GetAmmo(color) > 0;
    }
}
