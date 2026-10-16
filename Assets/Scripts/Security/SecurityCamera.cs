using UnityEngine;

[RequireComponent(typeof(DetectionSystem))]
public class SecurityCamera : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DetectionSystem detectionSystem;
    [SerializeField] private NPCSuspicionSystem suspicionSystem;
    [SerializeField] private Transform player;
    [SerializeField] private Light cameraLight;

    [Header("Feedback Visual")]
    [SerializeField] private Color normalLightColor = Color.white;
    [SerializeField] private Color suspiciousLightColor = new Color(1f, 0.7f, 0.1f);
    [SerializeField] private Color alertLightColor = Color.red;

    [Header("Estado")]
    [SerializeField] private bool isDeactivated = false;

    public bool IsDeactivated
    {
        get { return isDeactivated; }
    }

    private void Awake()
    {
        if (detectionSystem == null)
        {
            detectionSystem = GetComponent<DetectionSystem>();
        }

        if (suspicionSystem == null)
        {
            suspicionSystem = GetComponent<NPCSuspicionSystem>();
            if (suspicionSystem == null)
            {
                suspicionSystem = gameObject.AddComponent<NPCSuspicionSystem>();
            }
        }

        if (suspicionSystem != null)
        {
            suspicionSystem.CanRotateTowardsTarget = false;
            suspicionSystem.CanInvestigateMovement = false;
        }

        if (cameraLight == null)
        {
            cameraLight = GetComponentInChildren<Light>();
        }

        if (cameraLight != null)
        {
            normalLightColor = cameraLight.color;
        }
    }

    private void OnEnable()
    {
        if (suspicionSystem != null)
        {
            suspicionSystem.OnSuspicionLevel2Reached += HandleAlertReached;
        }
    }

    private void OnDisable()
    {
        if (suspicionSystem != null)
        {
            suspicionSystem.OnSuspicionLevel2Reached -= HandleAlertReached;
        }
    }

    private void Start()
    {
        FindPlayerIfNeeded();
    }

    private void Update()
    {
        if (isDeactivated)
        {
            return;
        }

        AlarmManager alarmManager = AlarmManager.Instance;
        if (alarmManager == null || alarmManager.IsAlarmActive)
        {
            UpdateCameraLightFeedback();
            return;
        }

        FindPlayerIfNeeded();

        // Fallback directo si no cuenta con suspicionSystem
        if (suspicionSystem == null && player != null && detectionSystem != null)
        {
            if (detectionSystem.CanDetectTarget(player))
            {
                Debug.Log($"[SecurityCamera] {gameObject.name} detectó al jugador y activó la alarma.", this);
                alarmManager.TriggerAlarm();
            }
        }

        UpdateCameraLightFeedback();
    }

    private void HandleAlertReached()
    {
        if (isDeactivated)
        {
            return;
        }

        AlarmManager alarmManager = AlarmManager.Instance;
        if (alarmManager != null && !alarmManager.IsAlarmActive)
        {
            Debug.Log($"[SecurityCamera] {gameObject.name} colmó su nivel de sospecha (Nivel 2) y activó la alarma.", this);
            alarmManager.TriggerAlarm();
        }
    }

    private void UpdateCameraLightFeedback()
    {
        if (cameraLight == null)
        {
            return;
        }

        AlarmManager alarmManager = AlarmManager.Instance;
        if (alarmManager != null && alarmManager.IsAlarmActive)
        {
            cameraLight.color = alertLightColor;
            return;
        }

        if (suspicionSystem != null)
        {
            if (suspicionSystem.CurrentLevel == 2)
            {
                cameraLight.color = alertLightColor;
            }
            else if (suspicionSystem.CurrentLevel == 1)
            {
                cameraLight.color = suspiciousLightColor;
            }
            else
            {
                cameraLight.color = normalLightColor;
            }
        }
    }

    public void DeactivateCamera()
    {
        isDeactivated = true;

        if (suspicionSystem != null)
        {
            suspicionSystem.ResetSuspicion();
            suspicionSystem.enabled = false;
        }

        if (detectionSystem != null)
        {
            detectionSystem.enabled = false;
        }

        if (cameraLight != null)
        {
            cameraLight.enabled = false;
        }

        Debug.Log($"[SecurityCamera] {gameObject.name} ha sido desactivada.");
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
        {
            return;
        }

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            player = playerHealth.transform;
            return;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            player = taggedPlayer.transform;
        }
    }
}
