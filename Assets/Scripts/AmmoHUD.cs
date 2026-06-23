using UnityEngine;
using UnityEngine.UI;

public class AmmoHUD : MonoBehaviour
{
    [Header("Sprites de gota (en orden)")]
    [Tooltip("0=llena, 1=3/4, 2=media, 3=1/4, 4=vacia")]
    public Sprite[] dropSprites = new Sprite[5];

    [Header("Images de las gotas")]
    public Image drop1; // se vacia primero
    public Image drop2; // se vacia segunda

    private PaintColor lastColor;
    private int lastAmmo = -1;

    void Update()
    {
        if (AmmoManager.instance == null || WeaponManager.instance == null)
        {
            Debug.Log("AmmoManager: " + AmmoManager.instance + " | WeaponManager: " + WeaponManager.instance);
            return;
        }

        PaintColor color = WeaponManager.instance.currentColor;
        int ammo = AmmoManager.instance.GetAmmo(color);

        // Solo actualizar si cambio algo
        if (color == lastColor && ammo == lastAmmo) return;
        lastColor = color;
        lastAmmo = ammo;

        // Cantidad en cada gota
        // Drop1 representa los primeros 10 que se gastan (11-20)
        // Drop2 representa los ultimos 10 (1-10)
        int drop1Ammo = Mathf.Max(0, ammo - 10);
        int drop2Ammo = Mathf.Min(10, ammo);

        Color tint = PaintColorUtils.ToUnityColor(color);
        tint.a = 1f;

        UpdateDrop(drop1, drop1Ammo, tint);
        UpdateDrop(drop2, drop2Ammo, tint);
    }

    void UpdateDrop(Image img, int ammoInDrop, Color tint)
    {
        if (img == null) { Debug.Log("IMG ES NULL"); return; }
        Debug.Log("Aplicando color: " + tint + " | sprite ammo: " + ammoInDrop + " | img: " + img.name);
        img.color = tint;
        img.sprite = GetSprite(ammoInDrop);
    }

    Sprite GetSprite(int amount)
    {
        if (dropSprites == null || dropSprites.Length < 5) return null;

        if (amount >= 10) return dropSprites[0]; // llena
        if (amount >= 7)  return dropSprites[1]; // 3/4
        if (amount >= 3)  return dropSprites[2]; // media
        if (amount >= 1)  return dropSprites[3]; // 1/4
        return dropSprites[4];                   // vacia
    }
}
