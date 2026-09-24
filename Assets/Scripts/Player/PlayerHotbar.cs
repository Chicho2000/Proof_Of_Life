using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HotbarSlot
{
    [SerializeField] private ItemData item;
    [SerializeField] private int count;

    public ItemData Item => item;
    public int Count => count;
    public bool IsEmpty => item == null || count <= 0;

    public HotbarSlot()
    {
        item = null;
        count = 0;
    }

    public HotbarSlot(ItemData item, int count)
    {
        this.item = item;
        this.count = count;
    }

    public void Set(ItemData newItem, int newCount)
    {
        item = newItem;
        count = newCount;
    }

    public void Add(int amount)
    {
        count += amount;
    }

    public void Remove(int amount)
    {
        count -= amount;
        if (count <= 0)
        {
            Clear();
        }
    }

    public void Clear()
    {
        item = null;
        count = 0;
    }
}

public class PlayerHotbar : MonoBehaviour
{
    [Header("Configuración de Ranuras")]
    [Range(1, 10)]
    [SerializeField] private int slotCount = 5;

    [Header("Slots (Visualización en Inspector)")]
    [SerializeField] private List<HotbarSlot> slots = new List<HotbarSlot>();

    [Header("Selección Activa")]
    [SerializeField] private int selectedSlotIndex = 0;

    [Header("Visualización en Mano (Primera Persona)")]
    [SerializeField] private Transform handSocket;
    private GameObject currentInHandObject;

    [Header("Soltar Ítems")]
    [SerializeField] private KeyCode dropKey = KeyCode.G;
    [SerializeField] private float dropDistance = 1.5f;
    [SerializeField] private float dropHeight = 0.5f;
    [SerializeField] private float dropClearanceRadius = 0.2f;
    [SerializeField] private LayerMask dropCollisionMask = ~0;

    // Eventos desacoplados para UI / Sistemas
    public event Action<int, HotbarSlot> OnSlotUpdated;
    public event Action<int> OnSlotSelected;

    public int SlotCount => slotCount;
    public int SelectedSlotIndex => selectedSlotIndex;
    public Transform HandSocket => handSocket;

    private void Awake()
    {
        InitializeSlots();
        InitializeHandSocket();
    }

    private void Start()
    {
        SelectSlot(selectedSlotIndex);
    }

    private void Update()
    {
        HandleNumericInput();
        HandleScrollInput();
        HandleDropInput();
    }

    private void InitializeSlots()
    {
        if (slots.Count != slotCount)
        {
            slots.Clear();
            for (int i = 0; i < slotCount; i++)
            {
                slots.Add(new HotbarSlot());
            }
        }
    }

    private void InitializeHandSocket()
    {
        if (handSocket == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = GetComponentInChildren<Camera>();
            }

            if (mainCam != null)
            {
                Transform existingSocket = mainCam.transform.Find("HandSocket");
                if (existingSocket != null)
                {
                    handSocket = existingSocket;
                }
                else
                {
                    GameObject socketObj = new GameObject("HandSocket");
                    socketObj.transform.SetParent(mainCam.transform, false);
                    socketObj.transform.localPosition = Vector3.zero;
                    socketObj.transform.localRotation = Quaternion.identity;
                    handSocket = socketObj.transform;
                }
            }
        }
    }

    private void HandleNumericInput()
    {
        for (int i = 0; i < slotCount; i++)
        {
            KeyCode key = KeyCode.Alpha1 + i;
            if (Input.GetKeyDown(key))
            {
                SelectSlot(i);
                return;
            }
        }
    }

    private void HandleScrollInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (scroll < 0f)
            {
                // Scroll abajo: siguiente ranura
                int nextIndex = (selectedSlotIndex + 1) % slotCount;
                SelectSlot(nextIndex);
            }
            else if (scroll > 0f)
            {
                // Scroll arriba: ranura anterior
                int prevIndex = (selectedSlotIndex - 1 + slotCount) % slotCount;
                SelectSlot(prevIndex);
            }
        }
    }

    private void HandleDropInput()
    {
        if (Input.GetKeyDown(dropKey))
        {
            DropSelectedItem();
        }
    }

    public bool DropSelectedItem()
    {
        HotbarSlot selectedSlot = GetSlot(selectedSlotIndex);
        if (selectedSlot == null || selectedSlot.IsEmpty)
        {
            return false;
        }

        ItemData selectedItem = selectedSlot.Item;
        GameObject worldPrefab = selectedItem.WorldPrefab;

        if (worldPrefab == null)
        {
            Debug.LogWarning($"[Hotbar] No se puede soltar '{selectedItem.ItemName}': su ItemData no tiene WorldPrefab asignado.");
            return false;
        }

        if (worldPrefab.GetComponent<ItemInteractable>() == null)
        {
            Debug.LogWarning($"[Hotbar] No se puede soltar '{selectedItem.ItemName}': el WorldPrefab debe tener ItemInteractable en el objeto raíz.");
            return false;
        }

        if (worldPrefab.GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"[Hotbar] No se puede soltar '{selectedItem.ItemName}': el WorldPrefab debe tener un Collider en el objeto raíz.");
            return false;
        }

        if (!TryGetDropPosition(out Vector3 spawnPosition))
        {
            Debug.LogWarning($"[Hotbar] No hay espacio suficiente delante del jugador para soltar '{selectedItem.ItemName}'.");
            return false;
        }

        GameObject droppedObject;

        try
        {
            droppedObject = Instantiate(worldPrefab, spawnPosition, worldPrefab.transform.rotation);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Hotbar] Falló el spawn de '{selectedItem.ItemName}'. El ítem permanece en la hotbar. {exception.Message}");
            return false;
        }

        if (droppedObject == null)
        {
            Debug.LogWarning($"[Hotbar] Falló el spawn de '{selectedItem.ItemName}'. El ítem permanece en la hotbar.");
            return false;
        }

        ItemInteractable droppedInteractable = droppedObject.GetComponent<ItemInteractable>();
        Collider droppedCollider = droppedObject.GetComponent<Collider>();

        if (droppedInteractable == null || droppedCollider == null)
        {
            droppedObject.SetActive(false);
            Destroy(droppedObject);
            Debug.LogWarning($"[Hotbar] El objeto instanciado para '{selectedItem.ItemName}' no es un pickup válido. El ítem permanece en la hotbar.");
            return false;
        }

        droppedInteractable.Initialize(selectedItem, 1);

        if (!RemoveItem(selectedSlotIndex, 1))
        {
            droppedObject.SetActive(false);
            Destroy(droppedObject);
            Debug.LogWarning($"[Hotbar] No se pudo descontar '{selectedItem.ItemName}' de la hotbar. Se canceló el drop para evitar duplicados.");
            return false;
        }

        Debug.Log($"[Hotbar] Se soltó '{selectedItem.ItemName}' desde el Slot {selectedSlotIndex + 1}.");
        return true;
    }

    private bool TryGetDropPosition(out Vector3 spawnPosition)
    {
        Transform directionSource = transform;
        Camera playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera != null)
        {
            directionSource = playerCamera.transform;
        }

        Vector3 up = transform.up;
        Vector3 forward = Vector3.ProjectOnPlane(directionSource.forward, up).normalized;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = transform.forward;
        }

        float clearanceRadius = Mathf.Max(0.01f, dropClearanceRadius);
        float desiredDistance = Mathf.Max(0.1f, dropDistance);
        Vector3 castOrigin = transform.position + up * Mathf.Max(clearanceRadius, dropHeight);

        float availableDistance = desiredDistance;
        RaycastHit[] hits = Physics.SphereCastAll(
            castOrigin,
            clearanceRadius,
            forward,
            desiredDistance,
            dropCollisionMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            if (IsPlayerCollider(hit.collider))
            {
                continue;
            }

            availableDistance = Mathf.Min(availableDistance, hit.distance - clearanceRadius);
        }

        CharacterController characterController = GetComponent<CharacterController>();
        float playerRadius = characterController != null ? characterController.radius : 0.5f;
        float minimumDistance = playerRadius + clearanceRadius + 0.1f;

        if (availableDistance < minimumDistance)
        {
            spawnPosition = default;
            return false;
        }

        spawnPosition = castOrigin + forward * availableDistance;
        SnapDropPositionToGround(ref spawnPosition, up);
        return true;
    }

    private void SnapDropPositionToGround(ref Vector3 spawnPosition, Vector3 up)
    {
        Vector3 rayOrigin = spawnPosition + up;
        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            -up,
            2f,
            dropCollisionMask,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.PositiveInfinity;

        foreach (RaycastHit hit in hits)
        {
            if (IsPlayerCollider(hit.collider) || hit.distance >= closestDistance)
            {
                continue;
            }

            closestDistance = hit.distance;
            spawnPosition = hit.point + up * Mathf.Max(0.01f, dropClearanceRadius);
        }
    }

    private bool IsPlayerCollider(Collider candidate)
    {
        return candidate != null
            && (candidate.transform == transform || candidate.transform.IsChildOf(transform));
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slotCount) return;

        selectedSlotIndex = index;
        UpdateInHandVisual();
        OnSlotSelected?.Invoke(selectedSlotIndex);

        string itemName = (selectedSlotIndex < slots.Count && !slots[selectedSlotIndex].IsEmpty) 
            ? slots[selectedSlotIndex].Item.ItemName 
            : "Vacío";
        Debug.Log($"🔄 [Hotbar] Ranura activa cambiada a <b>Slot {selectedSlotIndex + 1}</b> (Ítem: {itemName}).");
    }

    public void UpdateInHandVisual()
    {
        if (currentInHandObject != null)
        {
            Destroy(currentInHandObject);
            currentInHandObject = null;
        }

        if (handSocket == null)
        {
            InitializeHandSocket();
            if (handSocket == null) return;
        }

        ItemData selectedItem = GetSelectedItem();
        if (selectedItem != null && selectedItem.InHandPrefab != null)
        {
            currentInHandObject = Instantiate(selectedItem.InHandPrefab, handSocket);
            currentInHandObject.transform.localPosition = selectedItem.InHandPositionOffset;
            currentInHandObject.transform.localRotation = Quaternion.Euler(selectedItem.InHandRotationOffset);
            currentInHandObject.transform.localScale = selectedItem.InHandScale;

            // Desactivar colliders e interactables en el ítem sostenido para evitar interferencias
            foreach (var col in currentInHandObject.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
            foreach (var interactable in currentInHandObject.GetComponentsInChildren<Interactable>())
            {
                interactable.enabled = false;
            }
        }
    }

    public ItemData GetSelectedItem()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            return slots[selectedSlotIndex].Item;
        }
        return null;
    }

    public HotbarSlot GetSlot(int index)
    {
        if (index >= 0 && index < slots.Count)
        {
            return slots[index];
        }
        return null;
    }

    public bool AddItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0) return false;

        bool addedSuccessfully = false;

        // 1. Si es apilable, intentar sumar a una ranura existente con el mismo ítem
        if (itemData.IsStackable)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty && slots[i].Item == itemData && slots[i].Count < itemData.MaxStack)
                {
                    int spaceAvailable = itemData.MaxStack - slots[i].Count;
                    int toAdd = Mathf.Min(spaceAvailable, amount);
                    slots[i].Add(toAdd);
                    amount -= toAdd;

                    OnSlotUpdated?.Invoke(i, slots[i]);
                    addedSuccessfully = true;

                    Debug.Log($"📦 <color=#4CAF50><b>[Hotbar]</b> ¡Ítem Apilado!</color> Se sumó '<b>{itemData.ItemName}</b>' en el Slot {i + 1}. Cantidad actual: <b>{slots[i].Count} / {itemData.MaxStack}</b>.");

                    if (i == selectedSlotIndex)
                    {
                        UpdateInHandVisual();
                    }

                    if (amount <= 0) return true;
                }
            }
        }

        // 2. Si el slot actualmente seleccionado está vacío, priorizar colocarlo en la mano
        if (slots[selectedSlotIndex].IsEmpty)
        {
            int toAdd = itemData.IsStackable ? Mathf.Min(itemData.MaxStack, amount) : 1;
            slots[selectedSlotIndex].Set(itemData, toAdd);
            amount -= toAdd;

            OnSlotUpdated?.Invoke(selectedSlotIndex, slots[selectedSlotIndex]);
            UpdateInHandVisual();
            addedSuccessfully = true;

            Debug.Log($"✋ <color=#2196F3><b>[Hotbar]</b> Ítem equipado en mano:</color> '<b>{itemData.ItemName}</b>' en el Slot {selectedSlotIndex + 1}. Cantidad: <b>{toAdd}</b>.");

            if (amount <= 0) return true;
        }

        // 3. Buscar cualquier otra ranura vacía
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty)
            {
                int toAdd = itemData.IsStackable ? Mathf.Min(itemData.MaxStack, amount) : 1;
                slots[i].Set(itemData, toAdd);
                amount -= toAdd;

                OnSlotUpdated?.Invoke(i, slots[i]);
                addedSuccessfully = true;

                Debug.Log($"📥 <color=#00BCD4><b>[Hotbar]</b> Ítem guardado:</color> '<b>{itemData.ItemName}</b>' en el Slot {i + 1} (Ranura libre). Cantidad: <b>{toAdd}</b>.");

                if (i == selectedSlotIndex)
                {
                    UpdateInHandVisual();
                }

                if (amount <= 0) return true;
            }
        }

        if (amount > 0)
        {
            Debug.LogWarning($"⚠️ <color=#FF9800><b>[Hotbar]</b> ¡Inventario lleno!</color> No hay espacio disponible para guardar '{itemData.ItemName}'.");
        }

        return addedSuccessfully && amount <= 0;
    }

    public bool RemoveItem(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count || slots[slotIndex].IsEmpty || amount <= 0)
        {
            return false;
        }

        slots[slotIndex].Remove(amount);
        OnSlotUpdated?.Invoke(slotIndex, slots[slotIndex]);

        if (slotIndex == selectedSlotIndex)
        {
            UpdateInHandVisual();
        }

        return true;
    }

    public bool RemoveItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty && slots[i].Item == itemData)
            {
                int toRemove = Mathf.Min(slots[i].Count, amount);
                slots[i].Remove(toRemove);
                amount -= toRemove;

                OnSlotUpdated?.Invoke(i, slots[i]);

                if (i == selectedSlotIndex)
                {
                    UpdateInHandVisual();
                }

                if (amount <= 0) return true;
            }
        }

        return amount <= 0;
    }

    public bool HasItem(ItemType type)
    {
        if (type == ItemType.None) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty && slots[i].Item.ItemType == type)
            {
                return true;
            }
        }
        return false;
    }

    public bool HasItem(ItemData itemData)
    {
        if (itemData == null) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty && slots[i].Item == itemData)
            {
                return true;
            }
        }
        return false;
    }
}
