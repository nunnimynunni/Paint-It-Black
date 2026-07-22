using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHUD : MonoBehaviour
{
    [Header("Sprites de armas")]
    public Sprite spriteSpray;
    public Sprite spritePincel;
    public Sprite spriteRodillo;

    [Header("UI")]
    public Image weaponImage;

    [Header("Animación")]
    [Tooltip("Distancia en pixels que recorre el sprite al entrar/salir")]
    public float slideDistance = 100f;
    [Tooltip("Duración de la animación en segundos")]
    public float slideDuration = 0.15f;

    private WeaponManager.WeaponType lastWeapon;
    private Coroutine slideCoroutine;
    private RectTransform imgRect;

    void Start()
    {
        imgRect = weaponImage.GetComponent<RectTransform>();
        lastWeapon = WeaponManager.instance != null ? WeaponManager.instance.currentWeapon : WeaponManager.WeaponType.Spray;
        weaponImage.sprite = GetSprite(lastWeapon);
    }

    void Update()
    {
        if (WeaponManager.instance == null) return;

        WeaponManager.WeaponType current = WeaponManager.instance.currentWeapon;
        if (current == lastWeapon) return;

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideSwitch(lastWeapon, current));
        lastWeapon = current;
    }

    IEnumerator SlideSwitch(WeaponManager.WeaponType from, WeaponManager.WeaponType to)
    {
        // Determinar direccion: las armas estan en orden Spray=0, Projectile=1, Melee=2
        // Si el nuevo es mayor, sale por la izquierda y entra por la derecha, y viceversa
        int fromIndex = (int)from;
        int toIndex   = (int)to;
        float exitDir  = (toIndex > fromIndex) ? -1f : 1f;
        float enterDir = -exitDir;

        Vector2 centerPos = Vector2.zero;
        Vector2 exitPos   = new Vector2(slideDistance * exitDir, 0);
        Vector2 enterPos  = new Vector2(slideDistance * enterDir, 0);

        // Salida del arma actual
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            imgRect.anchoredPosition = Vector2.Lerp(centerPos, exitPos, t / slideDuration);
            yield return null;
        }

        // Cambiar sprite y entrar desde el lado opuesto
        weaponImage.sprite = GetSprite(to);
        imgRect.anchoredPosition = enterPos;

        t = 0f;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            imgRect.anchoredPosition = Vector2.Lerp(enterPos, centerPos, t / slideDuration);
            yield return null;
        }

        imgRect.anchoredPosition = centerPos;
    }

    Sprite GetSprite(WeaponManager.WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponManager.WeaponType.Spray:      return spriteSpray;
            case WeaponManager.WeaponType.Projectile: return spritePincel;
            case WeaponManager.WeaponType.Melee:      return spriteRodillo;
            default: return null;
        }
    }
}
