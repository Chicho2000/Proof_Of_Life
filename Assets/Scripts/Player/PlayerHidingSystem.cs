using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHidingSystem : MonoBehaviour
{
    [Header("Configuración de Cámara en Escondite")]
    [Tooltip("Ángulo horizontal máximo (yaw) para mirar desde el escondite")]
    [SerializeField] private float maxLookYaw = 75f;

    [Tooltip("Ángulo vertical máximo (pitch) para mirar desde el escondite")]
    [SerializeField] private float maxLookPitch = 35f;

    [Tooltip("Sensibilidad del mouse al mirar dentro del escondite")]
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip exitSound;

    [Header("Efectos Visuales de Interior")]
    [Tooltip("Opacidad de la viñeta de penumbra interior (0 a 1)")]
    [Range(0f, 1f)]
    [SerializeField] private float interiorDarknessAlpha = 0.35f;

    private bool isHiding = false;
    private HideSpot currentHideSpot;
    private Camera playerCamera;
    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;
    private float originalNearClipPlane = 0.3f;
    private float originalFov = 60f;
    private float lookYaw = 0f;
    private float lookPitch = 0f;

    // Posición y rotación previa del jugador antes de entrar al escondite (evita salir al vacío / espacio)
    private Vector3 enterPlayerPosition;
    private Quaternion enterPlayerRotation;

    // Componentes del jugador a desactivar temporalmente
    private CharacterController characterController;
    private FPSPlayerController fpsController;
    private PlayerCombat playerCombat;
    private PlayerInteraction playerInteraction;
    private PlayerHotbar playerHotbar;
    private Transform handSocket;
    private GameObject firstPersonHandsObj;

    // Lista de renderers del propio personaje que se ocultan mientras está escondido
    private readonly System.Collections.Generic.List<Renderer> hiddenPlayerRenderers = new System.Collections.Generic.List<Renderer>();

    // Textura procedural de viñeta para la vista desde el interior
    private static Texture2D vignetteTexture;

    public bool IsHiding => isHiding;
    public HideSpot CurrentHideSpot => currentHideSpot;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        fpsController = GetComponent<FPSPlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
        playerInteraction = GetComponent<PlayerInteraction>();
        playerHotbar = GetComponent<PlayerHotbar>();

        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (playerCamera != null)
        {
            originalCameraLocalPos = playerCamera.transform.localPosition;
            originalCameraLocalRot = playerCamera.transform.localRotation;
            originalNearClipPlane = playerCamera.nearClipPlane;
            originalFov = playerCamera.fieldOfView;

            Transform foundSocket = playerCamera.transform.Find("HandSocket");
            if (foundSocket != null)
            {
                handSocket = foundSocket;
            }

            Transform foundHands = playerCamera.transform.Find("FirstPersonHands");
            if (foundHands != null)
            {
                firstPersonHandsObj = foundHands.gameObject;
            }
        }

        CreateVignetteTextureIfNeeded();
    }

    private void Update()
    {
        if (!isHiding)
        {
            return;
        }

        // Salir del escondite al presionar [E] o [Espacio]
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            ExitHideSpot();
        }
    }

    private void LateUpdate()
    {
        if (!isHiding || playerCamera == null || currentHideSpot == null)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f)
        {
            return;
        }

        // Control de vista natural en primera persona desde el interior del escondite
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        lookYaw = Mathf.Clamp(lookYaw + mouseX, -maxLookYaw, maxLookYaw);
        lookPitch = Mathf.Clamp(lookPitch - mouseY, -maxLookPitch, maxLookPitch);

        Transform camRef = currentHideSpot.CameraPoint;
        playerCamera.transform.position = camRef.position;
        playerCamera.transform.rotation = camRef.rotation * Quaternion.Euler(lookPitch, lookYaw, 0f);
    }

    public bool EnterHideSpot(HideSpot spot)
    {
        if (isHiding || spot == null || !spot.CanPlayerHide)
        {
            return false;
        }

        isHiding = true;
        currentHideSpot = spot;

        // Guardar la posición y rotación exacta del jugador para restaurarlo de forma 100% segura al salir
        enterPlayerPosition = transform.position;
        enterPlayerRotation = transform.rotation;

        // Desactivar controles y colisiones del jugador
        if (characterController != null) characterController.enabled = false;
        if (fpsController != null) fpsController.enabled = false;
        if (playerCombat != null) playerCombat.enabled = false;
        if (playerInteraction != null) playerInteraction.enabled = false;
        if (playerHotbar != null) playerHotbar.enabled = false;

        // Buscar socket de arma y manos si no estaban cacheados
        if (playerCamera != null)
        {
            if (handSocket == null)
            {
                handSocket = playerCamera.transform.Find("HandSocket");
            }
            if (firstPersonHandsObj == null)
            {
                Transform foundHands = playerCamera.transform.Find("FirstPersonHands");
                if (foundHands != null)
                {
                    firstPersonHandsObj = foundHands.gameObject;
                }
            }
        }

        // 1. Ocultar arma en mano
        if (handSocket != null)
        {
            handSocket.gameObject.SetActive(false);
        }

        // 2. Ocultar manos de primera persona
        if (firstPersonHandsObj != null)
        {
            firstPersonHandsObj.SetActive(false);
        }

        // 3. Ocultar completamente todos los renderers del propio personaje (cuerpo, ropa, cabeza)
        hiddenPlayerRenderers.Clear();
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in allRenderers)
        {
            if (r != null && r.enabled)
            {
                r.enabled = false;
                hiddenPlayerRenderers.Add(r);
            }
        }

        // Posicionar al jugador en el punto del escondite
        Transform hidePoint = spot.HidePoint;
        transform.position = hidePoint.position;
        transform.rotation = hidePoint.rotation;

        // Resetear ángulos de vista
        lookYaw = 0f;
        lookPitch = 0f;

        // Ajustar la cámara: reducir nearClipPlane a 1.5cm para evitar cortes con el marco o tapa
        if (playerCamera != null)
        {
            originalNearClipPlane = playerCamera.nearClipPlane;
            playerCamera.nearClipPlane = 0.015f;
            originalFov = playerCamera.fieldOfView;

            playerCamera.transform.position = spot.CameraPoint.position;
            playerCamera.transform.rotation = spot.CameraPoint.rotation;
        }

        // Asegurar renderizado de dos caras (Double-Sided) en los materiales del escondite
        EnsureDoubleSidedMaterials(spot);

        // Notificar al escondite
        spot.OnPlayerEntered(this);

        // Notificar al HUD
        InteractionPromptUI promptUI = FindFirstObjectByType<InteractionPromptUI>();
        if (promptUI != null)
        {
            promptUI.ShowPrompt("[E] Salir del escondite");
        }

        if (enterSound != null)
        {
            AudioSource.PlayClipAtPoint(enterSound, transform.position);
        }

        Debug.Log($"🚪 [PlayerHidingSystem] Jugador escondido dentro de '{spot.SpotName}'.");
        return true;
    }

    public void ExitHideSpot()
    {
        if (!isHiding)
        {
            return;
        }

        HideSpot spot = currentHideSpot;
        isHiding = false;
        currentHideSpot = null;

        // Restaurar al jugador en la posición de entrada segura (evita caer al vacío o salir en el espacio)
        Vector3 exitPos = (spot != null && spot.HasCustomExitPoint)
            ? spot.CustomExitPoint.position
            : enterPlayerPosition;

        Quaternion exitRot = (spot != null && spot.HasCustomExitPoint)
            ? spot.CustomExitPoint.rotation
            : enterPlayerRotation;

        transform.position = exitPos;
        transform.rotation = exitRot;

        if (spot != null)
        {
            spot.OnPlayerExited(this);
        }

        // Restaurar posición, rotación local, nearClipPlane y FOV de la cámara
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = originalCameraLocalPos;
            playerCamera.transform.localRotation = originalCameraLocalRot;
            playerCamera.nearClipPlane = originalNearClipPlane;
            playerCamera.fieldOfView = originalFov;
        }

        // Restaurar todos los renderers del propio personaje
        foreach (Renderer r in hiddenPlayerRenderers)
        {
            if (r != null)
            {
                r.enabled = true;
            }
        }
        hiddenPlayerRenderers.Clear();

        // Restaurar manos en primera persona
        if (firstPersonHandsObj != null)
        {
            firstPersonHandsObj.SetActive(true);
        }

        // Restaurar mano / arma
        if (handSocket != null)
        {
            handSocket.gameObject.SetActive(true);
        }

        // Reactivar componentes
        if (characterController != null) characterController.enabled = true;
        if (fpsController != null) fpsController.enabled = true;
        if (playerCombat != null) playerCombat.enabled = true;
        if (playerHotbar != null) playerHotbar.enabled = true;
        if (playerInteraction != null)
        {
            playerInteraction.enabled = true;
            playerInteraction.ClearCurrentInteractable();
        }

        // Limpiar aviso del HUD
        InteractionPromptUI promptUI = FindFirstObjectByType<InteractionPromptUI>();
        if (promptUI != null)
        {
            promptUI.ShowTemporaryWarning($"<color=#38BDF8>Saliste de {spot?.SpotName ?? "escondite"}</color>", 1.5f);
        }

        if (exitSound != null)
        {
            AudioSource.PlayClipAtPoint(exitSound, transform.position);
        }

        Debug.Log("🚪 [PlayerHidingSystem] Jugador salió del escondite.");
    }

    private void EnsureDoubleSidedMaterials(HideSpot spot)
    {
        if (spot == null) return;

        Renderer[] renderers = spot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            foreach (Material m in r.materials)
            {
                if (m != null && m.HasProperty("_Cull"))
                {
                    m.SetFloat("_Cull", 0f); // 0 = Cull Off (Double-Sided)
                }
            }
        }
    }

    private static void CreateVignetteTextureIfNeeded()
    {
        if (vignetteTexture != null) return;

        int size = 128;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        vignetteTexture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDist = Mathf.Clamp01(dist / maxDist);
                float alpha = Mathf.SmoothStep(0.1f, 0.85f, normalizedDist);
                vignetteTexture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }

        vignetteTexture.Apply();
    }

    private void OnGUI()
    {
        if (!isHiding)
        {
            return;
        }

        // Viñeta oscura sutil del interior de escondite
        if (vignetteTexture != null && interiorDarknessAlpha > 0f)
        {
            Color prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, interiorDarknessAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture, ScaleMode.StretchToFill);
            GUI.color = prevColor;
        }
    }
}
