using UnityEngine;

public class SecurityPanelInteractable : Interactable
{
    [Header("Configuración")]
    [SerializeField] private float hackDuration = 3.5f;
    [SerializeField] private string initialPrompt = "Desactivar seguridad y cámaras";
    [SerializeField] private string resetAlarmPrompt = "Silenciar alarma activa";
    [SerializeField] private string systemDisabledPrompt = "Sistema de cámaras saboteado";

    [Header("Requisito de Ítem")]
    [SerializeField] private bool requireDecoder = true;
    [SerializeField] private ItemType requiredItemType = ItemType.Decoder;
    [Tooltip("Si es true, el jugador debe tener el decodificador seleccionado y sostenido en la mano.")]
    [SerializeField] private bool requireEquippedInHand = true;
    [Tooltip("Si es true, se consume el decodificador al piratear el panel.")]
    [SerializeField] private bool consumeDecoderOnUse = false;
    [SerializeField] private string missingDecoderWarning = "<color=#EF4444>[ ! ] SE REQUIERE DECODIFICADOR</color>";

    private bool areCamerasDisabled = false;
    private bool isHacking = false;
    private float currentHackProgress = 0f;
    private PlayerInteraction playerInteraction;
    private PlayerHotbar playerHotbar;

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

        PlayerHotbar hotbar = interactor.GetComponent<PlayerHotbar>();
        if (hotbar == null)
        {
            hotbar = interactor.GetComponentInParent<PlayerHotbar>();
        }

        if (!HasDecoder(hotbar))
        {
            InteractionPromptUI promptUI = FindFirstObjectByType<InteractionPromptUI>();
            if (promptUI != null)
            {
                promptUI.ShowTemporaryWarning(missingDecoderWarning, 2.0f);
            }
            Debug.Log("[SecurityPanel] Necesitás tener el decodificador equipado para interactuar con este panel.");
            return;
        }

        playerInteraction = interactor.GetComponent<PlayerInteraction>();
        playerHotbar = hotbar;
        isHacking = true;
        currentHackProgress = 0f;
    }

    private bool HasDecoder(PlayerHotbar hotbar)
    {
        if (!requireDecoder)
        {
            return true;
        }

        if (hotbar == null)
        {
            return false;
        }

        if (requireEquippedInHand)
        {
            ItemInteractable selectedItem = hotbar.GetSelectedItem();
            return selectedItem != null && selectedItem.ItemType == requiredItemType;
        }
        else
        {
            return hotbar.HasItem(requiredItemType);
        }
    }

    private void Update()
    {
        if (!isHacking)
        {
            return;
        }

        bool stillFocused = playerInteraction != null && playerInteraction.currentInteractable == this;
        bool keyHeld = playerInteraction != null && Input.GetKey(playerInteraction.interactKey);
        bool decoderStillEquipped = HasDecoder(playerHotbar);

        if (!stillFocused || !keyHeld || !decoderStillEquipped)
        {
            CancelHacking();
            return;
        }

        currentHackProgress += Time.deltaTime;

        if (currentHackProgress >= hackDuration)
        {
            FinishHacking();
        }
    }

    private void CancelHacking()
    {
        isHacking = false;
        currentHackProgress = 0f;
        playerInteraction = null;
        playerHotbar = null;
    }

    private void FinishHacking()
    {
        isHacking = false;
        currentHackProgress = 0f;
        areCamerasDisabled = true;

        if (consumeDecoderOnUse && playerHotbar != null)
        {
            ConsumeDecoder(playerHotbar);
        }

        playerInteraction = null;
        playerHotbar = null;

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

    private void ConsumeDecoder(PlayerHotbar hotbar)
    {
        if (hotbar == null)
        {
            return;
        }

        for (int i = 0; i < hotbar.SlotCount; i++)
        {
            HotbarSlot slot = hotbar.GetSlot(i);
            if (slot != null && !slot.IsEmpty && slot.Item != null && slot.Item.ItemType == requiredItemType)
            {
                hotbar.RemoveItem(i, 1);
                break;
            }
        }
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
