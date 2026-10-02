using UnityEngine;

public class SecurityPanelInteractable : Interactable
{
    [Header("Configuración")]
    [SerializeField] private float hackDuration = 3.5f;
    [SerializeField] private string initialPrompt = "Desactivar seguridad y cámaras";
    [SerializeField] private string resetAlarmPrompt = "Silenciar alarma activa";
    [SerializeField] private string systemDisabledPrompt = "Sistema de cámaras saboteado";

    private bool areCamerasDisabled = false;
    private bool isHacking = false;
    private float currentHackProgress = 0f;
    private PlayerInteraction playerInteraction;

    private void Start()
    {
        UpdatePromptState();
    }

    private void OnEnable()
    {
        AlarmManager.OnAlarmTriggered += UpdatePromptState;
        AlarmManager.OnAlarmStopped += UpdatePromptState;
    }

    private void OnDisable()
    {
        AlarmManager.OnAlarmTriggered -= UpdatePromptState;
        AlarmManager.OnAlarmStopped -= UpdatePromptState;
    }

    public override void Interact(GameObject interactor)
    {
        if (isHacking)
        {
            return;
        }

        bool isAlarmActive = AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive;
        if (areCamerasDisabled && !isAlarmActive)
        {
            return;
        }

        playerInteraction = interactor.GetComponent<PlayerInteraction>();
        isHacking = true;
        currentHackProgress = 0f;
    }

    private void Update()
    {
        if (!isHacking)
        {
            return;
        }

        bool stillFocused = playerInteraction != null && playerInteraction.currentInteractable == this;
        bool keyHeld = playerInteraction != null && Input.GetKey(playerInteraction.interactKey);

        if (!stillFocused || !keyHeld)
        {
            isHacking = false;
            currentHackProgress = 0f;
            return;
        }

        currentHackProgress += Time.deltaTime;

        if (currentHackProgress >= hackDuration)
        {
            FinishHacking();
        }
    }

    private void FinishHacking()
    {
        isHacking = false;
        currentHackProgress = 0f;
        areCamerasDisabled = true;

        // 1. Apagar todas las cámaras de seguridad
        SecurityCamera[] cameras = FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
            {
                cameras[i].DeactivateCamera();
            }
        }

        // 2. Detener la alarma
        if (AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive)
        {
            AlarmManager.Instance.StopAlarm();
        }

        // 3. Aviso táctico en pantalla
        InteractionPromptUI promptUI = FindFirstObjectByType<InteractionPromptUI>();
        if (promptUI != null)
        {
            promptUI.ShowTemporaryWarning("<color=#22C55E>[✓] SISTEMA Y CÁMARAS DESACTIVADOS</color>", 3.0f);
        }

        UpdatePromptState();
    }

    public bool IsHacking()
    {
        return isHacking;
    }

    public float GetHackProgressNormalized()
    {
        if (hackDuration <= 0f)
        {
            return 0f;
        }
        return Mathf.Clamp01(currentHackProgress / hackDuration);
    }

    private void UpdatePromptState()
    {
        bool isAlarmActive = AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive;

        if (isAlarmActive)
        {
            canInteract = true;
            interactionPrompt = resetAlarmPrompt;
        }
        else if (areCamerasDisabled)
        {
            canInteract = false;
            interactionPrompt = systemDisabledPrompt;
        }
        else
        {
            canInteract = true;
            interactionPrompt = initialPrompt;
        }
    }
}
