using UnityEngine;
using TMPro;

public class AlarmUI : MonoBehaviour
{
    [Header("Elementos de UI")]
    [SerializeField] private GameObject alarmBanner;
    [SerializeField] private TMP_Text alarmText;

    [Header("Configuración")]
    [SerializeField] private string alarmMessage = "[ ! ] ALARMA ACTIVADA";

    private CanvasGroup canvasGroup;

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
        AlarmManager.OnAlarmStopped += HandleAlarmStopped;
        MissionManager.OnMissionCompleted += HandleAlarmStopped;
        MissionManager.OnMissionFailed += HandleMissionEnd;

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnDeath += HandleAlarmStopped;
        }
    }

    private void OnDisable()
    {
        AlarmManager.OnAlarmTriggered -= HandleAlarm;
        AlarmManager.OnAlarmStopped -= HandleAlarmStopped;
        MissionManager.OnMissionCompleted -= HandleAlarmStopped;
        MissionManager.OnMissionFailed -= HandleMissionEnd;

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= HandleAlarmStopped;
        }
    }

    private void HandleMissionEnd(string reason)
    {
        HandleAlarmStopped();
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
    }

    private void HandleAlarmStopped()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }
}
