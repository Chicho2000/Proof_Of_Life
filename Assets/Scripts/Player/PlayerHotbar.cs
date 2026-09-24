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
