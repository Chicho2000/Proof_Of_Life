using UnityEngine;
using UnityEngine.UI;

public class ThrowChargeUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerThrowController throwController;
    [SerializeField] private GameObject chargePanel = null;
    [SerializeField] private Image chargeFill = null;

    [Header("Colores de Carga Dinámicos")]
    [SerializeField] private Color minChargeColor = new Color(0.2f, 0.9f, 0.3f, 1f); // Verde (tiro suave)
    [SerializeField] private Color midChargeColor = new Color(0.95f, 0.85f, 0.15f, 1f); // Amarillo (tiro medio)
    [SerializeField] private Color maxChargeColor = new Color(0.95f, 0.2f, 0.2f, 1f); // Rojo (fuerza máxima)

    private void Start()
    {
        if (throwController == null)
        {
            throwController = FindFirstObjectByType<PlayerThrowController>();
        }

        if (chargePanel != null)
        {
            chargePanel.SetActive(false);
        }

        if (chargeFill != null)
        {
            chargeFill.type = Image.Type.Filled;
        }
    }

    private void Update()
    {
        bool visible = throwController != null && throwController.IsCharging && Time.timeScale > 0f;

        if (chargePanel != null && chargePanel.activeSelf != visible)
        {
            chargePanel.SetActive(visible);
        }

        if (visible && chargeFill != null)
        {
            float progress = throwController.ChargeNormalized;
            chargeFill.fillAmount = progress;
            chargeFill.color = EvaluateChargeColor(progress);
        }
    }

    private Color EvaluateChargeColor(float progress)
    {
        if (progress < 0.5f)
        {
            float t = progress / 0.5f;
            return Color.Lerp(minChargeColor, midChargeColor, t);
        }
        else
        {
            float t = (progress - 0.5f) / 0.5f;
            return Color.Lerp(midChargeColor, maxChargeColor, t);
        }
    }
}
