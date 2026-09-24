using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    [Header("Referencia al Jugador")]
    [SerializeField] private PlayerHotbar playerHotbar;

    [System.Serializable]
    public class HotbarSlotUI
    {
        public RectTransform slotRoot;
        public Image slotBackground;
        public Image itemIcon;
        public Text slotNumberText;
        public Text countText;
        public Text itemNameText;
        public GameObject selectionHighlight;

        public void UpdateSlot(HotbarSlot slot, bool isSelected)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(isSelected);
            }

            if (slot == null || slot.IsEmpty)
            {
                if (itemIcon != null)
                {
                    itemIcon.sprite = null;
                    itemIcon.enabled = false;
                }
                if (countText != null)
                {
                    countText.text = string.Empty;
                }
                if (itemNameText != null)
                {
                    itemNameText.text = string.Empty;
                }
            }
            else
            {
                if (itemIcon != null)
                {
                    itemIcon.sprite = slot.Item.Icon;
                    itemIcon.enabled = slot.Item.Icon != null;
                }
                if (countText != null)
                {
                    countText.text = slot.Count > 1 ? $"x{slot.Count}" : string.Empty;
                }
                if (itemNameText != null)
                {
                    itemNameText.text = slot.Item.ItemName;
                }
            }
        }
    }

    [Header("Marco Selector Flotante (Opcional)")]
    [SerializeField] private RectTransform movingSelectionFrame;

    [Header("Ranuras Visuales")]
    [SerializeField] private List<HotbarSlotUI> uiSlots = new List<HotbarSlotUI>();

    private void Start()
    {
        if (playerHotbar == null)
        {
            playerHotbar = FindFirstObjectByType<PlayerHotbar>();
        }

        if (playerHotbar != null)
        {
            playerHotbar.OnSlotUpdated += HandleSlotUpdated;
            playerHotbar.OnSlotSelected += HandleSlotSelected;

            RefreshAll();
        }
    }

    private void OnDestroy()
    {
        if (playerHotbar != null)
        {
            playerHotbar.OnSlotUpdated -= HandleSlotUpdated;
            playerHotbar.OnSlotSelected -= HandleSlotSelected;
        }
    }

    private void HandleSlotUpdated(int slotIndex, HotbarSlot slot)
    {
        if (slotIndex >= 0 && slotIndex < uiSlots.Count)
        {
            bool isSelected = (playerHotbar != null && slotIndex == playerHotbar.SelectedSlotIndex);
            uiSlots[slotIndex].UpdateSlot(slot, isSelected);
        }
    }

    private void HandleSlotSelected(int selectedIndex)
    {
        RefreshAll();

        if (movingSelectionFrame != null && selectedIndex >= 0 && selectedIndex < uiSlots.Count)
        {
            RectTransform targetSlot = uiSlots[selectedIndex].slotRoot;
            if (targetSlot != null)
            {
                movingSelectionFrame.position = targetSlot.position;
            }
        }
    }

    public void RefreshAll()
    {
        if (playerHotbar == null) return;

        for (int i = 0; i < uiSlots.Count && i < playerHotbar.SlotCount; i++)
        {
            HotbarSlot slot = playerHotbar.GetSlot(i);
            bool isSelected = (i == playerHotbar.SelectedSlotIndex);
            uiSlots[i].UpdateSlot(slot, isSelected);
        }

        if (movingSelectionFrame != null && playerHotbar.SelectedSlotIndex >= 0 && playerHotbar.SelectedSlotIndex < uiSlots.Count)
        {
            RectTransform targetSlot = uiSlots[playerHotbar.SelectedSlotIndex].slotRoot;
            if (targetSlot != null)
            {
                movingSelectionFrame.position = targetSlot.position;
            }
        }
    }
}
