using UnityEngine;
using UnityEngine.UI;

public class ThrowChargeUI : MonoBehaviour
{
    [SerializeField] private PlayerThrowController throwController;
    [SerializeField] private GameObject chargePanel = null;
    [SerializeField] private Image chargeFill = null;

    private void Start()
    {
        if (throwController == null) throwController = FindFirstObjectByType<PlayerThrowController>();
        if (chargePanel != null) chargePanel.SetActive(false);
        if (chargeFill != null) chargeFill.type = Image.Type.Filled;
    }

    private void Update()
    {
        bool visible = throwController != null && throwController.IsCharging && Time.timeScale > 0f;
        if (chargePanel != null && chargePanel.activeSelf != visible) chargePanel.SetActive(visible);
        if (visible && chargeFill != null) chargeFill.fillAmount = throwController.ChargeNormalized;
    }
}
