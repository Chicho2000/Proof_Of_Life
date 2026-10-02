using UnityEngine;

[RequireComponent(typeof(DetectionSystem))]
public class SecurityCamera : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DetectionSystem detectionSystem;
    [SerializeField] private Transform player;
    [SerializeField] private Light cameraLight;

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

        if (cameraLight == null)
        {
            cameraLight = GetComponentInChildren<Light>();
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
            return;
        }

        FindPlayerIfNeeded();
        if (player == null || detectionSystem == null)
        {
            return;
        }

        if (detectionSystem.CanDetectTarget(player))
        {
            Debug.Log($"[SecurityCamera] {gameObject.name} detectó al jugador y activó la alarma.", this);
            alarmManager.TriggerAlarm();
        }
    }

    public void DeactivateCamera()
    {
        isDeactivated = true;

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
