using UnityEngine;

/// <summary>
/// Punto de extracción o evacuación de la misión.
/// Permite finalizar la misión y terminar la partida, pero ÚNICAMENTE si el jugador
/// tiene el ítem USB equipado en mano (o en el inventario según configuración).
/// </summary>
public class ExtractionPoint : Interactable
{
    [Header("Requisitos de Ítem")]
    [Tooltip("Si es true, la extracción exige robar y tener el USB. Si es false (misión de asesinato), valida que los objetivos obligatorios estén cumplidos.")]
    [SerializeField] private bool requireUsb = true;

    [Tooltip("Pickup canónico del USB requerido. Si no se asigna, valida por Tipo de Ítem.")]
    [SerializeField] private ItemInteractable requiredUsbItem;

    [Tooltip("Tipo de ítem requerido si no se especifica un pickup concreto (por defecto MissionItem).")]
    [SerializeField] private ItemType requiredItemType = ItemType.MissionItem;

    [Tooltip("Si es true, el jugador DEBE tener el USB seleccionado activamente en su mano. Si es false, basta con tenerlo en cualquier ranura.")]
    [SerializeField] private bool requireEquippedInHand = true;

    [Tooltip("Si es true, retira el pendrive del inventario al momento de la entrega/extracción exitosa.")]
    [SerializeField] private bool consumeUsbOnDelivery = true;

    [Header("Validaciones Adicionales")]
    [Tooltip("Si es true, comprueba que todos los demás objetivos obligatorios de MissionManager estén listos.")]
    [SerializeField] private bool requireAllObjectivesComplete = true;

    /// <summary>
    /// Configura si esta extracción exige portar el USB o si es una extracción basada en objetivos (ej. asesinato).
    /// </summary>
    public void SetRequireUsb(bool required)
    {
        requireUsb = required;
        if (!required)
        {
            requireAllObjectivesComplete = true;
        }
        UpdatePromptState(null);
    }

    [Header("Modo Trigger Opcional")]
    [Tooltip("Si es true, la extracción se activará también al entrar físicamente al collider trigger.")]
    [SerializeField] private bool allowTriggerExtraction = false;

    [Header("Audio y Efectos")]
    [SerializeField] private AudioClip extractionSuccessSound;
    [SerializeField] private AudioClip extractionDeniedSound;

    private bool isExtracted = false;

    private void Awake()
    {
        interactionPrompt = "Extraer (Requiere USB en mano)";
    }

    private void Start()
    {
        UpdatePromptState(null);
    }

    public override void OnFocus(GameObject interactor)
    {
        base.OnFocus(interactor);
        UpdatePromptState(interactor);
    }

    private void UpdatePromptState(GameObject interactor)
    {
        if (interactor == null)
        {
            interactionPrompt = "Punto de Extracción";
            return;
        }

        if (!requireUsb)
        {
            bool objectivesReady = MissionManager.Instance == null || MissionManager.Instance.AreMandatoryObjectivesComplete();
            if (objectivesReady)
            {
                interactionPrompt = "Extraer <color=#22C55E>[Objetivo Neutralizado]</color>";
            }
            else
            {
                interactionPrompt = "Bloqueado <color=#EF4444>[Elimina al Ejecutivo]</color>";
            }
            return;
        }

        PlayerHotbar hotbar = interactor.GetComponent<PlayerHotbar>();
        if (hotbar == null) hotbar = interactor.GetComponentInParent<PlayerHotbar>();

        if (hotbar != null)
        {
            bool isEquipped = IsUsbEquipped(hotbar);
            bool hasInInventory = HasUsbInInventory(hotbar);

            if (isEquipped)
            {
                interactionPrompt = "Extraer <color=#22C55E>[USB listo]</color>";
            }
            else if (hasInInventory)
            {
                interactionPrompt = "Extraer <color=#F59E0B>[Equipa el USB]</color>";
            }
            else
            {
                interactionPrompt = "Bloqueado <color=#EF4444>[Falta USB]</color>";
            }
        }
    }

    public override void Interact(GameObject interactor)
    {
        if (isExtracted) return;
        TryExtract(interactor);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!allowTriggerExtraction || isExtracted) return;

        PlayerHotbar hotbar = other.GetComponent<PlayerHotbar>();
        if (hotbar == null) hotbar = other.GetComponentInParent<PlayerHotbar>();

        if (hotbar != null)
        {
            TryExtract(other.gameObject);
        }
    }

    /// <summary>
    /// Intenta procesar la extracción del jugador validando los requisitos según la misión activa.
    /// </summary>
    public bool TryExtract(GameObject playerObject)
    {
        if (isExtracted || playerObject == null) return false;

        PlayerHotbar hotbar = playerObject.GetComponent<PlayerHotbar>();
        if (hotbar == null)
        {
            hotbar = playerObject.GetComponentInParent<PlayerHotbar>();
        }

        if (hotbar == null)
        {
            Debug.LogWarning("[ExtractionPoint] El objeto que intenta extraer no posee PlayerHotbar.");
            return false;
        }

        // Si la misión requiere USB (misión de extracción):
        if (requireUsb)
        {
            // 1. Verificar si el jugador tiene el USB en su inventario
            bool hasUsb = HasUsbInInventory(hotbar);
            if (!hasUsb)
            {
                PlayDeniedFeedback("¡Misión incompleta! Necesitas encontrar y robar el USB antes de extraer.");
                return false;
            }

            // 2. Verificar si el USB está EQUIPADO en la mano activa
            if (requireEquippedInHand)
            {
                bool isEquipped = IsUsbEquipped(hotbar);
                if (!isEquipped)
                {
                    PlayDeniedFeedback("¡Debes tener el USB equipado en tu mano para confirmar la extracción!");
                    return false;
                }
            }
        }

        // 3. Verificar objetivos de misión adicionales si corresponde
        if (requireAllObjectivesComplete && MissionManager.Instance != null)
        {
            if (!MissionManager.Instance.AreMandatoryObjectivesComplete())
            {
                string msg = requireUsb
                    ? "No puedes extraer todavía: quedan objetivos prioritarios por completar."
                    : "¡Misión incompleta! Debes neutralizar al objetivo antes de extraer.";
                PlayDeniedFeedback(msg);
                return false;
            }
        }

        // 4. ¡Extracción Exitosa!
        ExecuteExtraction(hotbar);
        return true;
    }

    private void ExecuteExtraction(PlayerHotbar hotbar)
    {
        isExtracted = true;
        canInteract = false;

        Debug.Log("🚀 <color=#4CAF50><b>[ExtractionPoint] ¡Extracción autorizada! Operación cumplida con éxito.</b></color>");

        // Retirar el pendrive entregado de la mano/inventario si correspondía a misión de USB
        if (requireUsb && consumeUsbOnDelivery && hotbar != null)
        {
            ItemInteractable equipped = hotbar.GetSelectedItem();
            if (equipped != null && IsMatchingUsb(equipped))
            {
                hotbar.RemoveItem(hotbar.SelectedSlotIndex, 1);
            }
            else
            {
                // Si no estaba en el slot seleccionado, buscarlo en el inventario y removerlo
                for (int i = 0; i < hotbar.SlotCount; i++)
                {
                    HotbarSlot slot = hotbar.GetSlot(i);
                    if (slot != null && !slot.IsEmpty && IsMatchingUsb(slot.Item))
                    {
                        hotbar.RemoveItem(i, 1);
                        break;
                    }
                }
            }
        }

        if (extractionSuccessSound != null)
        {
            AudioSource.PlayClipAtPoint(extractionSuccessSound, transform.position);
        }

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.CompleteMission();
        }
        else
        {
            // Respaldo por si no hubiera MissionManager en la escena
            FPSPlayerController controller = FindFirstObjectByType<FPSPlayerController>();
            if (controller != null) controller.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void PlayDeniedFeedback(string reason)
    {
        Debug.LogWarning($"⛔ [ExtractionPoint] Extracción rechazada: {reason}");

        if (extractionDeniedSound != null)
        {
            AudioSource.PlayClipAtPoint(extractionDeniedSound, transform.position);
        }

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.NotifyMissionMessage(reason);
        }
    }

    /// <summary>
    /// Comprueba si el ítem actualmente seleccionado en la mano del jugador es el USB.
    /// </summary>
    public bool IsUsbEquipped(PlayerHotbar hotbar)
    {
        if (hotbar == null) return false;

        ItemInteractable selectedItem = hotbar.GetSelectedItem();
        return IsMatchingUsb(selectedItem);
    }

    /// <summary>
    /// Comprueba si el USB está en cualquiera de los slots del inventario del jugador.
    /// </summary>
    public bool HasUsbInInventory(PlayerHotbar hotbar)
    {
        if (hotbar == null) return false;

        for (int i = 0; i < hotbar.SlotCount; i++)
        {
            HotbarSlot slot = hotbar.GetSlot(i);
            if (slot != null && !slot.IsEmpty && IsMatchingUsb(slot.Item))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Valida si un ítem corresponde al USB requerido.
    /// </summary>
    private bool IsMatchingUsb(ItemInteractable item)
    {
        if (item == null) return false;

        if (requiredUsbItem != null)
        {
            return item.HasSameIdentity(requiredUsbItem);
        }

        if (requiredItemType != ItemType.None && item.ItemType == requiredItemType)
        {
            return true;
        }

        return false;
    }
}
