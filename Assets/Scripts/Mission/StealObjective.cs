using UnityEngine;

/// <summary>
/// Objetivo de misión que requiere robar u obtener un ítem específico (por ejemplo, el USB confidencial).
/// Se sincroniza automáticamente con el sistema de PlayerHotbar.
/// </summary>
public class StealObjective : MissionObjective
{
    [Header("Configuración del Ítem Objetivo")]
    [Tooltip("El ItemData específico a robar (ej: USB_Data). Si se deja vacío, buscará por Tipo o Nombre.")]
    [SerializeField] private ItemData targetItem;

    [Tooltip("Tipo de ítem a buscar si no se especificó un ItemData concreto.")]
    [SerializeField] private ItemType targetItemType = ItemType.MissionItem;

    [Tooltip("Palabra clave a buscar en el nombre del ítem si no se asigna ItemData (ej: 'usb')")]
    [SerializeField] private string itemNameKeyword = "usb";

    [Tooltip("Si es verdadero, el objetivo vuelve a estar incompleto si el jugador suelta o descarta el ítem.")]
    [SerializeField] private bool requireRetainedInInventory = true;

    [Header("Referencia Opcional al Pickup en el Mundo")]
    [Tooltip("Objeto pickup en la escena que contiene el ítem. Opcional.")]
    [SerializeField] private ItemInteractable worldPickupItem;

    private PlayerHotbar cachedPlayerHotbar;

    public ItemData TargetItem => targetItem;
    public ItemType TargetItemType => targetItemType;

    private void Awake()
    {
        if (string.IsNullOrEmpty(objectiveTitle) || objectiveTitle == "Objetivo de Misión")
        {
            objectiveTitle = targetItem != null ? $"Robar {targetItem.ItemName}" : "Robar USB confidencial";
        }

        if (string.IsNullOrEmpty(objectiveDescription) || objectiveDescription == "Descripción del objetivo a cumplir.")
        {
            objectiveDescription = "Encuentra y recoge el dispositivo USB con información confidencial.";
        }
    }

    private void Start()
    {
        FindAndSubscribePlayerHotbar();
    }

    private void OnDestroy()
    {
        UnsubscribePlayerHotbar();
    }

    private void FindAndSubscribePlayerHotbar()
    {
        if (cachedPlayerHotbar == null)
        {
            cachedPlayerHotbar = FindFirstObjectByType<PlayerHotbar>();
        }

        if (cachedPlayerHotbar != null)
        {
            cachedPlayerHotbar.OnSlotUpdated += HandleSlotUpdated;
            CheckHotbarForTargetItem(cachedPlayerHotbar);
        }
    }

    private void UnsubscribePlayerHotbar()
    {
        if (cachedPlayerHotbar != null)
        {
            cachedPlayerHotbar.OnSlotUpdated -= HandleSlotUpdated;
        }
    }

    private void HandleSlotUpdated(int slotIndex, HotbarSlot slot)
    {
        if (cachedPlayerHotbar != null)
        {
            CheckHotbarForTargetItem(cachedPlayerHotbar);
        }
    }

    /// <summary>
    /// Verifica si el inventario del jugador contiene el ítem objetivo.
    /// </summary>
    public void CheckHotbarForTargetItem(PlayerHotbar hotbar)
    {
        if (hotbar == null) return;

        bool hasItem = HasTargetItem(hotbar);

        if (hasItem && !IsComplete)
        {
            CompleteObjective();
        }
        else if (!hasItem && IsComplete && requireRetainedInInventory)
        {
            SetIncomplete();
        }
    }

    /// <summary>
    /// Comprueba si el Hotbar del jugador tiene el ítem configurado.
    /// </summary>
    public bool HasTargetItem(PlayerHotbar hotbar)
    {
        if (hotbar == null) return false;

        for (int i = 0; i < hotbar.SlotCount; i++)
        {
            HotbarSlot slot = hotbar.GetSlot(i);
            if (slot != null && !slot.IsEmpty && IsMatchingItem(slot.Item))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Valida si un ItemData coincide con el ítem requerido para este objetivo.
    /// </summary>
    public bool IsMatchingItem(ItemData item)
    {
        if (item == null) return false;

        // 1. Si hay un ItemData asignado, comparar por referencia directa
        if (targetItem != null)
        {
            return item == targetItem;
        }

        // 2. Si no, validar por tipo de ítem
        if (targetItemType != ItemType.None && item.ItemType == targetItemType)
        {
            return true;
        }

        // 3. Validar por palabra clave en el nombre (ej: "USB")
        if (!string.IsNullOrEmpty(itemNameKeyword) && 
            item.ItemName.IndexOf(itemNameKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return false;
    }
}
