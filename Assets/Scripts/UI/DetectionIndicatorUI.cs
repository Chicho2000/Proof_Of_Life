using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicador direccional de sospecha / amenaza en pantalla estilo Hitman / Far Cry.
/// Rota de forma continua y ultra suave (LerpAngle) sobre la mira (Crosshair)
/// señalando con precisión hacia la cámara o guardia que está detectando al jugador.
/// </summary>
public class DetectionIndicatorUI : MonoBehaviour
{
    [Header("Referencias Visuales")]
    [Tooltip("Nodo pivot que rota 360° alrededor del centro de la pantalla.")]
    [SerializeField] private RectTransform indicatorRoot;

    [Tooltip("Imagen que muestra el sprite del arco curvo.")]
    [SerializeField] private Image arcImage;

    [Tooltip("CanvasGroup para controlar la transparencia general y el fade in / out.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Comportamiento y Suavizado")]
    [Tooltip("Velocidad de interpolación angular para que la rotación sea ultra suave y continua.")]
    [Range(5f, 30f)]
    [SerializeField] private float rotationSmoothSpeed = 16f;

    [Tooltip("Velocidad de aparición y desvanecimiento (Fade In / Fade Out).")]
    [Range(2f, 15f)]
    [SerializeField] private float fadeSpeed = 8f;

    [Tooltip("Distancia radial en píxeles desde el centro de la pantalla (mira) al arco.")]
    [SerializeField] private float radius = 55f;

    [Header("Colores de Amenaza")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.7f);           // Blanco tenue
    [SerializeField] private Color suspicionColor = new Color(1f, 0.88f, 0.2f, 1f);     // Amarillo sospecha
    [SerializeField] private Color alarmColor = new Color(1f, 0.15f, 0.15f, 1f);         // Rojo alerta

    private Transform playerCamera;
    private float currentAngle = 0f;

    private void Awake()
    {
        if (indicatorRoot == null)
        {
            indicatorRoot = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (arcImage == null)
        {
            arcImage = GetComponentInChildren<Image>();
        }

        // Posicionar el arco en el radio deseado sobre el eje Y
        if (arcImage != null)
        {
            RectTransform arcRect = arcImage.rectTransform;
            arcRect.anchoredPosition = new Vector2(0f, radius);
        }

        // Iniciar oculto
        canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        FindPlayerCamera();
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            FindPlayerCamera();
            if (playerCamera == null) return;
        }

        // Buscar la amenaza activa más relevante
        Transform activeThreat = null;
        float highestProgress = 0f;
        SecurityCamera.CameraState highestState = SecurityCamera.CameraState.Normal;
        bool isPreWarning = false;

        SecurityCamera[] cameras = Object.FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            if (cam.CurrentState != SecurityCamera.CameraState.Normal || cam.DetectionProgress > 0f)
            {
                if (cam.DetectionProgress >= highestProgress)
                {
                    highestProgress = cam.DetectionProgress;
                    highestState = cam.CurrentState;
                    activeThreat = cam.transform;
                    isPreWarning = false;
                }
            }
            else if (cam.IsPlayerInWarningZone && activeThreat == null)
            {
                // Si no hay amenaza activa con sospecha acumulándose, evaluamos zona de pre-aviso
                activeThreat = cam.transform;
                isPreWarning = true;
            }
        }

        bool hasThreat = activeThreat != null;
        float targetAlpha = 0f;

        if (hasThreat)
        {
            targetAlpha = isPreWarning ? 0.75f : 1f;
        }

        // Fade in / out suave
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        if (canvasGroup.alpha > 0.001f && activeThreat != null)
        {
            // 1. Cálculo de dirección angular relativa a la cámara del jugador
            Vector3 toThreat = activeThreat.position - playerCamera.position;

            // Transformar la dirección al espacio local de la mirada del jugador
            Vector3 localDir = playerCamera.InverseTransformDirection(toThreat);
            localDir.y = 0f; // Proyectar en el plano horizontal de la vista

            if (localDir.sqrMagnitude > 0.001f)
            {
                // Atan2: 0° adelante (+Z), 90° derecha (+X), -90° izquierda (-X), 180° atrás (-Z)
                // En el Canvas, rotación Z negativa rota en sentido horario (hacia la derecha)
                float targetAngle = -Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

                // 2. Interpolación angular continua y orgánica grado a grado
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * rotationSmoothSpeed);
                indicatorRoot.localEulerAngles = new Vector3(0f, 0f, currentAngle);
            }

            // 3. Semáforo y feedback de color
            if (arcImage != null)
            {
                if (isPreWarning)
                {
                    // Pre-aviso antes de entrar al cono: Blanco tenue
                    arcImage.color = new Color(1f, 1f, 1f, 0.85f);
                }
                else if (highestState == SecurityCamera.CameraState.Alarm)
                {
                    // Alarma confirmada: Rojo
                    arcImage.color = alarmColor;
                }
                else
                {
                    // Sospecha activa: Transición de amarillo a rojo conforme sube el tiempo
                    arcImage.color = Color.Lerp(suspicionColor, alarmColor, highestProgress);
                }
            }
        }
    }

    private void FindPlayerCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            playerCamera = mainCam.transform;
            return;
        }

        FPSPlayerController player = Object.FindFirstObjectByType<FPSPlayerController>();
        if (player != null)
        {
            Camera camInPlayer = player.GetComponentInChildren<Camera>();
            if (camInPlayer != null)
            {
                playerCamera = camInPlayer.transform;
            }
        }
    }
}
