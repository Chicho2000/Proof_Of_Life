using UnityEngine;
using TMPro;

public class AlarmUI : MonoBehaviour
{
    [Header("Elementos de UI")]
    [SerializeField] private GameObject alarmBanner;
    [SerializeField] private TMP_Text alarmText;

    [Header("Configuración")]
    [SerializeField] private string alarmMessage = "[ ! ] ALARMA ACTIVADA";
    [SerializeField] private float displayDuration = 3.5f;

    private CanvasGroup canvasGroup;
    private float hideTimer = 0f;

    private void Awake()
    {
        if (alarmBanner == null)
        {
            alarmBanner = gameObject;
        }

        canvasGroup = alarmBanner.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = alarmBanner.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;

        if (alarmText != null)
        {
            alarmText.text = alarmMessage;
        }
    }

    private void OnEnable()
    {
        AlarmManager.OnAlarmTriggered += HandleAlarm;
    }

    private void OnDisable()
    {
        AlarmManager.OnAlarmTriggered -= HandleAlarm;
    }

    private void Update()
    {
        // Temporizador simple con Time.deltaTime en vez de Corrutinas
        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
        }
    }

    private void HandleAlarm()
    {
        if (alarmText != null)
        {
            alarmText.text = alarmMessage;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        hideTimer = displayDuration;
    }
}
