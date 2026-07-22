using UnityEngine;
using UnityEngine.UI;

// HUD de armas: 4 marcos (MARCO HUD_0) con icono + número.
// El arma seleccionada se agranda para indicar que está activa.
// Arrastrá los 4 slots desde el Canvas en el Inspector.
//
// Estructura esperada en el Canvas (cada slot):
//   SlotArma1 (Image -> MARCO HUD_0)
//     └─ IconoArma (Image -> sprite del arma)
//     └─ Numero    (Text  -> "1")
//
// Asignar cada SlotArma como elemento del array "slots" en el Inspector.
public class WeaponHUDNew : MonoBehaviour
{
    [System.Serializable]
    public class WeaponSlot
    {
        [Tooltip("El GameObject raíz del slot (tiene el Image con MARCO HUD_0)")]
        public RectTransform root;
        [Tooltip("El Image hijo que muestra el icono del arma")]
        public Image iconoArma;
        [Tooltip("El tipo de arma que representa este slot")]
        public WeaponManager.WeaponType weaponType;
    }

    [Header("Slots (en orden 1-4)")]
    public WeaponSlot[] slots = new WeaponSlot[4];

    [Header("Escala")]
    [Tooltip("Escala normal de un slot no seleccionado")]
    public float normalScale = 1f;
    [Tooltip("Escala del slot seleccionado (un poco más grande)")]
    public float selectedScale = 1.25f;
    [Tooltip("Velocidad de la animación de escala")]
    public float scaleSpeed = 10f;

    [Header("Opacidad (opcional)")]
    [Tooltip("Opacidad del icono cuando el arma NO está seleccionada")]
    [Range(0f, 1f)] public float inactiveAlpha = 0.5f;
    [Tooltip("Opacidad del icono cuando el arma SÍ está seleccionada")]
    [Range(0f, 1f)] public float activeAlpha = 1f;

    private WeaponManager.WeaponType lastWeapon;

    void Start()
    {
        if (WeaponManager.instance != null)
            lastWeapon = WeaponManager.instance.currentWeapon;

        // Aplicar estado inicial sin animación
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].root == null) continue;
            bool selected = slots[i].weaponType == lastWeapon;
            slots[i].root.localScale = Vector3.one * (selected ? selectedScale : normalScale);
            SetIconAlpha(slots[i], selected ? activeAlpha : inactiveAlpha);
        }
    }

    void Update()
    {
        if (WeaponManager.instance == null) return;

        WeaponManager.WeaponType current = WeaponManager.instance.currentWeapon;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].root == null) continue;

            bool selected = slots[i].weaponType == current;
            float targetScale = selected ? selectedScale : normalScale;
            float targetAlpha = selected ? activeAlpha : inactiveAlpha;

            // Lerp suave hacia la escala objetivo
            Vector3 currentScale = slots[i].root.localScale;
            Vector3 target = Vector3.one * targetScale;
            slots[i].root.localScale = Vector3.Lerp(currentScale, target, Time.deltaTime * scaleSpeed);

            // Actualizar opacidad del icono
            SetIconAlpha(slots[i], targetAlpha);
        }

        lastWeapon = current;
    }

    void SetIconAlpha(WeaponSlot slot, float alpha)
    {
        if (slot.iconoArma == null) return;
        Color c = slot.iconoArma.color;
        c.a = alpha;
        slot.iconoArma.color = c;
    }
}
