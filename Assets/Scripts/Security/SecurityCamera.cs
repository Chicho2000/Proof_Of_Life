using UnityEngine;

/// <summary>
/// Controlador de Cámara de Seguridad Estática.
/// Utiliza DetectionSystem para vigilar su sector.
/// Aplica tiempo de reacción de 1.5s a 2.0s (estado de sospecha) antes de disparar la alarma global.
/// Controla un Spotlight como feedback visual (Verde -> Amarillo -> Rojo).
/// </summary>
public class SecurityCamera : MonoBehaviour
{
    public enum CameraState
    {
        Normal,     // Vigilando, sin intrusos a la vista (Verde)
        Suspicion,  // Intruso en el cono, acumulando tiempo de sospecha (Amarillo)
        Alarm       // Intruso confirmado, alarma global activada (Rojo)
    }

    [Header("Referencias")]
    [Tooltip("Sistema sensorial de detección de la cámara.")]
    [SerializeField] private DetectionSystem detectionSystem;

    [Tooltip("Foco de luz (Spotlight) que proyecta el cono visual en el suelo y paredes.")]
    [SerializeField] private Light spotLight;

    [Tooltip("Transform del jugador. Si no se asigna, se busca por Tag 'Player'.")]
    [SerializeField] private Transform targetPlayer;

    [Header("Tiempos de Reacción (ClickUp)")]
    [Tooltip("Tiempo continuo en segundos que el jugador debe permanecer en el cono para sonar la alarma (1.5s - 2.0s).")]
    [Range(0.5f, 5.0f)]
    [SerializeField] private float timeToDetect = 1.8f;

    [Tooltip("Velocidad con la que desciende el medidor de sospecha si el jugador se oculta a tiempo.")]
    [SerializeField] private float suspicionDecayRate = 1.2f;

    [Header("Feedback Visual (Semáforo)")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.85f, 0.2f, 1f);       // Verde táctico
    [SerializeField] private Color suspicionColor = new Color(1f, 0.85f, 0.1f, 1f);     // Amarillo advertencia
    [SerializeField] private Color alarmColor = new Color(1f, 0.15f, 0.15f, 1f);         // Rojo alarma

    [Header("Audio Opcional")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip suspicionClip;
    [SerializeField] private AudioClip alarmClip;

    [Header("Estado Actual (Solo Lectura)")]
    [SerializeField] private CameraState currentState = CameraState.Normal;
    [SerializeField] private float currentDetectionTime = 0f;
    [SerializeField] private bool isPlayerInWarningZone = false;

    public CameraState CurrentState => currentState;
    public float DetectionProgress => Mathf.Clamp01(currentDetectionTime / timeToDetect);
    public bool IsPlayerInWarningZone => isPlayerInWarningZone;

    private void Awake()
    {
        if (detectionSystem == null)
        {
            detectionSystem = GetComponent<DetectionSystem>();
            if (detectionSystem == null)
            {
                detectionSystem = gameObject.AddComponent<DetectionSystem>();
            }
        }

        if (spotLight == null)
        {
            spotLight = GetComponentInChildren<Light>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        FindPlayerTarget();
        SyncLightParameters();
        UpdateVisuals();
    }

    private void Update()
    {
        // Si no tenemos referencia al jugador, intentamos hallarlo
        if (targetPlayer == null)
        {
            FindPlayerTarget();
            if (targetPlayer == null) return;
        }

        // Si ya está en Alerta Total, mantenemos el estado y el semáforo en rojo
        if (currentState == CameraState.Alarm)
        {
            UpdateVisuals();
            return;
        }

        // Evaluar visión con el DetectionSystem
        bool canSee = detectionSystem != null && detectionSystem.CanSeeTarget(targetPlayer, out _);

        // Evaluar zona de pre-aviso periférica (solo si no está dentro del cono principal)
        isPlayerInWarningZone = !canSee && (currentState != CameraState.Alarm) && (detectionSystem != null && detectionSystem.IsInWarningZone(targetPlayer));

        if (canSee)
        {
            // El jugador está dentro del cono y visible
            currentDetectionTime += Time.deltaTime;

            if (currentDetectionTime >= timeToDetect)
            {
                TriggerCameraAlarm();
            }
            else
            {
                if (currentState != CameraState.Suspicion)
                {
                    SetState(CameraState.Suspicion);
                    Debug.Log("<color=yellow>[SecurityCamera]</color> ⚠️ Intruso en cono de visión. Nivel de sospecha aumentando...");
                }
            }
        }
        else
        {
            // El jugador se ocultó o salió del cono
            if (currentDetectionTime > 0f)
            {
                currentDetectionTime -= Time.deltaTime * suspicionDecayRate;

                if (currentDetectionTime <= 0f)
                {
                    currentDetectionTime = 0f;
                    if (currentState != CameraState.Normal)
                    {
                        SetState(CameraState.Normal);
                        Debug.Log("<color=green>[SecurityCamera]</color> Intruso perdido de vista. Estado normal restablecido.");
                    }
                }
            }
        }

        UpdateVisuals();
    }

    /// <summary>
    /// Cambia el estado de la cámara y ejecuta lógica de transición.
    /// </summary>
    private void SetState(CameraState newState)
    {
        currentState = newState;

        if (newState == CameraState.Suspicion && suspicionClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(suspicionClip);
        }
    }

    /// <summary>
    /// Dispara la alarma confirmada tras cumplirse los 1.5s - 2.0s de sospecha.
    /// </summary>
    private void TriggerCameraAlarm()
    {
        currentDetectionTime = timeToDetect;
        SetState(CameraState.Alarm);

        Debug.Log("<color=red><b>[SecurityCamera]</b></color> 🚨 ¡INTRUSO CONFIRMADO! Notificando a AlarmManager...");

        if (alarmClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(alarmClip);
        }

        // Disparar Alarma Global mediante el Singleton de la arquitectura
        if (AlarmManager.Instance != null)
        {
            AlarmManager.Instance.TriggerAlarm(targetPlayer);
        }
        else
        {
            Debug.LogWarning("[SecurityCamera] AlarmManager.Instance no encontrado en la escena. Asegúrate de añadir el GameObject de AlarmManager.");
        }
    }

    /// <summary>
    /// Sincroniza el rango y ángulo del Spotlight con la configuración del DetectionSystem.
    /// </summary>
    private void SyncLightParameters()
    {
        if (spotLight != null && detectionSystem != null)
        {
            spotLight.type = LightType.Spot;
            spotLight.range = detectionSystem.ViewDistance;
            spotLight.spotAngle = detectionSystem.ViewAngle;
        }
    }

    /// <summary>
    /// Actualiza el color e intensidad del Spotlight en base al progreso de sospecha.
    /// </summary>
    private void UpdateVisuals()
    {
        if (spotLight == null) return;

        switch (currentState)
        {
            case CameraState.Normal:
                spotLight.color = normalColor;
                break;

            case CameraState.Suspicion:
                // Interpola suavemente de amarillo a rojo a medida que se llena el timer
                float t = DetectionProgress;
                spotLight.color = Color.Lerp(suspicionColor, alarmColor, t);
                break;

            case CameraState.Alarm:
                spotLight.color = alarmColor;
                break;
        }
    }

    /// <summary>
    /// Localiza al jugador en la escena priorizando el Tag 'Player'.
    /// </summary>
    private void FindPlayerTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            targetPlayer = playerObj.transform;
            return;
        }

        // Fallback: buscar por componente FPSPlayerController
        FPSPlayerController controller = Object.FindFirstObjectByType<FPSPlayerController>();
        if (controller != null)
        {
            targetPlayer = controller.transform;
        }
    }

    /// <summary>
    /// Reinicia la cámara al estado normal (útil para checkpoints o terminales de seguridad hackeados).
    /// </summary>
    public void ResetCamera()
    {
        currentDetectionTime = 0f;
        SetState(CameraState.Normal);
        UpdateVisuals();
    }
}
