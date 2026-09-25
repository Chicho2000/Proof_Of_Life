using UnityEngine;

[RequireComponent(typeof(DetectionSystem))]
public class SecurityCamera : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DetectionSystem detectionSystem;
    [SerializeField] private Transform player;

    private void Awake()
    {
        if (detectionSystem == null)
        {
            detectionSystem = GetComponent<DetectionSystem>();
        }
    }

    private void Start()
    {
        FindPlayerIfNeeded();
    }

    private void Update()
    {
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
