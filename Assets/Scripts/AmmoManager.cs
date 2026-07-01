using System.Collections.Generic;
using UnityEngine;

public class AmmoManager : MonoBehaviour
{
    public static AmmoManager instance;

    // Antes era "const": pasa a campo de instancia para poder subirlo en
    // runtime con la mejora "Mejoras de Arma: capacidad de munición" del
    // puzzle (GDD 3.7), sin romper a nadie que ya usaba MaxAmmoPerColor
    // como valor inicial (sigue siendo 20 por defecto).
    public int MaxAmmoPerColor = 20;

    private Dictionary<PaintColor, int> ammo = new Dictionary<PaintColor, int>();

    void Awake()
    {
        instance = this;

        // Inicializar balas por color
        foreach (PaintColor color in System.Enum.GetValues(typeof(PaintColor)))
            ammo[color] = MaxAmmoPerColor;
    }

    // Llamado por UpgradeSystem al otorgar la mejora "capacidad de munición":
    // sube el máximo por color y rellena la munición actual en la misma
    // cantidad, para que la mejora se sienta de inmediato.
    public void AumentarCapacidadMunicion(int extra)
    {
        MaxAmmoPerColor += extra;
        var colores = new List<PaintColor>(ammo.Keys);
        foreach (var color in colores)
            ammo[color] = Mathf.Min(MaxAmmoPerColor, ammo[color] + extra);
    }

    // ============================================================
    // GDD: "Reposición de Munición": la munición de los colores limitados
    // se repone completamente al finalizar el encuentro/combate, y también
    // se puede lootear en gotas de pintura que dropean los enemigos.
    // ============================================================

    // Suma munición de un color puntual (lo usa AmmoPickup al agarrar una
    // gota lootable). Clampea al máximo actual, igual que el resto del
    // sistema.
    public void AddAmmo(PaintColor color, int amount)
    {
        if (!ammo.ContainsKey(color)) ammo[color] = 0;
        ammo[color] = Mathf.Min(MaxAmmoPerColor, ammo[color] + amount);
    }

    // Repone TODA la munición de TODOS los colores al máximo. Lo llama
    // EnemySpawner cuando el combate/encuentro termina de verdad (GDD).
    public void RellenarTodoCompletamente()
    {
        var colores = new List<PaintColor>(ammo.Keys);
        foreach (var color in colores)
            ammo[color] = MaxAmmoPerColor;
    }

    // El color gris (aguado) es infinito: nunca gasta munición ni se muestra vacío.
    public static bool EsInfinito(PaintColor color) => color == PaintColor.Gray;

    // Devuelve true si habia municion y se pudo consumir
    public bool ConsumeAmmo(PaintColor color, int amount = 1)
    {
        if (EsInfinito(color)) return true; // infinito: siempre permite disparar

        if (!ammo.ContainsKey(color)) return false;
        if (ammo[color] <= 0) return false;

        ammo[color] = Mathf.Max(0, ammo[color] - amount);
        return true;
    }

    public int GetAmmo(PaintColor color)
    {
        if (EsInfinito(color)) return MaxAmmoPerColor; // siempre lleno en el HUD
        return ammo.ContainsKey(color) ? ammo[color] : 0;
    }

    public bool HasAmmo(PaintColor color)
    {
        return GetAmmo(color) > 0;
    }
}
