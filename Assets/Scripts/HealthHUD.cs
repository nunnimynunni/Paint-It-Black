using UnityEngine;
using UnityEngine.UI;

public class HealthHUD : MonoBehaviour
{
    [Header("UI")]
    public Image rellenoVida;

    void Update()
    {
        if (PlayerHealth.Instance == null || rellenoVida == null) return;

        float fill = (float)PlayerHealth.Instance.GetCurrentHealth() / PlayerHealth.Instance.GetMaxHealth();
        rellenoVida.fillAmount = fill;
    }
}
