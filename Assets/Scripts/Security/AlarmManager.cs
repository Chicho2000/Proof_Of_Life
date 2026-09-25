using System;
using UnityEngine;

public class AlarmManager : MonoBehaviour
{
    public static AlarmManager Instance { get; private set; }

    public static event Action OnAlarmTriggered;

    [Header("Estado")]
    [SerializeField] private bool isAlarmActive;

    [Header("Audio de Alarma")]
    [SerializeField] private AudioSource alarmAudioSource;
    [SerializeField] private AudioClip alarmClip;
    [SerializeField] private bool loopAlarmSound = true;
    [SerializeField] private bool playAlarmSound = true;

    public bool IsAlarmActive => isAlarmActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                $"[AlarmManager] Ya existe un AlarmManager activo en '{Instance.gameObject.name}'. " +
                $"Se eliminará el duplicado '{gameObject.name}'.",
                this
            );
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveAudioSource(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void TriggerAlarm()
    {
        if (isAlarmActive)
        {
            return;
        }

        isAlarmActive = true;
        Debug.Log("[AlarmManager] Alarma activada.");

        PlayAlarmAudio();
        OnAlarmTriggered?.Invoke();
    }

    [ContextMenu("Debug/Stop Alarm")]
    public void StopAlarm()
    {
        if (!isAlarmActive)
        {
            return;
        }

        isAlarmActive = false;

        if (alarmAudioSource != null && alarmAudioSource.isPlaying)
        {
            alarmAudioSource.Stop();
        }

        Debug.Log("[AlarmManager] Alarma detenida.");
    }

    // Compatibilidad temporal con código existente.
    public bool GetIsAlarmActive()
    {
        return IsAlarmActive;
    }

    private void PlayAlarmAudio()
    {
        if (!playAlarmSound)
        {
            return;
        }

        if (!ResolveAudioSource(false))
        {
            Debug.LogWarning("[AlarmManager] No se puede reproducir la alarma porque falta un AudioSource.", this);
            return;
        }

        if (alarmClip == null)
        {
            Debug.LogWarning("[AlarmManager] Play Alarm Sound está activo, pero Alarm Clip no está asignado.", this);
            return;
        }

        alarmAudioSource.clip = alarmClip;
        alarmAudioSource.loop = loopAlarmSound;
        alarmAudioSource.Play();
    }

    private bool ResolveAudioSource(bool showWarning)
    {
        if (alarmAudioSource == null)
        {
            alarmAudioSource = GetComponent<AudioSource>();
        }

        if (alarmAudioSource == null && showWarning)
        {
            Debug.LogWarning(
                "[AlarmManager] No hay un AudioSource asignado ni agregado al GameObject. " +
                "La alarma funcionará, pero no tendrá sonido.",
                this
            );
        }

        return alarmAudioSource != null;
    }

    [ContextMenu("Debug/Trigger Alarm")]
    private void DebugTriggerAlarm()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[AlarmManager] La alarma de debug sólo puede activarse en Play Mode.");
            return;
        }

        TriggerAlarm();
    }
}
