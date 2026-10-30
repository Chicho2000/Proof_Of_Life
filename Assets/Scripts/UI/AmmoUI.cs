using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    [Header("Referencias del Jugador")]
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerHotbar playerHotbar;

    [Header("Elementos de UI")]
    [SerializeField] private GameObject ammoContainer;
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text currentAmmoText;
    [SerializeField] private TMP_Text maxAmmoText;
    [SerializeField] private TMP_Text reloadPromptText;

    [Header("Colores")]
    [SerializeField] private Color normalAmmoColor = Color.white;
    [SerializeField] private Color lowAmmoColor = new Color(1f, 0.3f, 0.2f, 1f);

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        ResolveReferences();

        if (ammoContainer == null)
        {
            ammoContainer = gameObject;
        }

        // Usamos CanvasGroup para no deshabilitar el GameObject ni el script
        canvasGroup = ammoContainer.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = ammoContainer.AddComponent<CanvasGroup>();
        }

        if (reloadPromptText != null)
        {
            reloadPromptText.gameObject.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        if (playerCombat == null)
        {
            playerCombat = FindFirstObjectByType<PlayerCombat>();
        }

        if (playerHotbar == null)
        {
            playerHotbar = FindFirstObjectByType<PlayerHotbar>();
        }

        if (weaponIconImage == null)
        {
            Transform tr = transform.Find("Weapon_Icon");
            if (tr != null) weaponIconImage = tr.GetComponent<Image>();
        }

        if (weaponNameText == null)
        {
            Transform tr = transform.Find("Weapon_Name_Text");
            if (tr != null) weaponNameText = tr.GetComponent<TMP_Text>();
        }

        if (currentAmmoText == null)
        {
            Transform tr = transform.Find("Current_Ammo_Text");
            if (tr != null) currentAmmoText = tr.GetComponent<TMP_Text>();
        }

        if (maxAmmoText == null)
        {
            Transform tr = transform.Find("Max_Ammo_Text");
            if (tr != null) maxAmmoText = tr.GetComponent<TMP_Text>();
        }

        if (reloadPromptText == null)
        {
            Transform tr = transform.Find("Reload_Prompt_Text");
            if (tr != null) reloadPromptText = tr.GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (playerCombat != null)
        {
            playerCombat.OnAmmoChanged -= HandleAmmoChanged;
            playerCombat.OnAmmoChanged += HandleAmmoChanged;
            playerCombat.OnReloadStateChanged -= HandleReloadStateChanged;
            playerCombat.OnReloadStateChanged += HandleReloadStateChanged;
        }

        if (playerHotbar != null)
        {
            playerHotbar.OnSlotSelected -= HandleSlotSelected;
            playerHotbar.OnSlotSelected += HandleSlotSelected;
            playerHotbar.OnSlotUpdated -= HandleSlotUpdated;
            playerHotbar.OnSlotUpdated += HandleSlotUpdated;
        }
    }

    private void UnsubscribeEvents()
    {
        if (playerCombat != null)
        {
            playerCombat.OnAmmoChanged -= HandleAmmoChanged;
            playerCombat.OnReloadStateChanged -= HandleReloadStateChanged;
        }

        if (playerHotbar != null)
        {
            playerHotbar.OnSlotSelected -= HandleSlotSelected;
            playerHotbar.OnSlotUpdated -= HandleSlotUpdated;
        }
    }

    private void Start()
    {
        if (playerHotbar == null || playerCombat == null)
        {
            ResolveReferences();
            SubscribeEvents();
        }

        UpdateVisibility();

        if (playerCombat != null && IsPistolEquipped())
        {
            HandleAmmoChanged(playerCombat.GetCurrentAmmo(), playerCombat.GetMaxAmmo());
        }
    }

    private void Update()
    {
        if (playerHotbar == null || playerCombat == null)
        {
            ResolveReferences();
            SubscribeEvents();
        }

        // Chequeo en tiempo de juego por si cambió el ítem en mano
        UpdateVisibility();
    }

    private void HandleSlotSelected(int slotIndex)
    {
        UpdateVisibility();
    }

    private void HandleSlotUpdated(int slotIndex, HotbarSlot slot)
    {
        UpdateVisibility();
    }

    private void HandleAmmoChanged(int current, int max)
    {
        if (!IsPistolEquipped())
        {
            HideAmmoElements();
            return;
        }

        UpdateAmmoDisplay(current, max);
    }

    private void HandleReloadStateChanged(bool reloading)
    {
        if (IsPistolEquipped() && playerCombat != null)
        {
            HandleAmmoChanged(playerCombat.GetCurrentAmmo(), playerCombat.GetMaxAmmo());
        }
        else if (reloadPromptText != null)
        {
            reloadPromptText.gameObject.SetActive(false);
        }
    }

    private ItemInteractable GetEquippedItem()
    {
        if (MissionManager.Instance != null && (MissionManager.Instance.IsMissionCompleted || MissionManager.Instance.IsMissionFailed))
        {
            return null;
        }

        if (BodyInteractable.IsDraggingAnyBody)
        {
            return null;
        }

        if (playerHotbar == null) return null;

        return playerHotbar.GetSelectedItem();
    }

    private bool IsPistolEquipped()
    {
        ItemInteractable selectedItem = GetEquippedItem();
        return selectedItem != null && selectedItem.ItemType == ItemType.SilencedPistol;
    }

    private void UpdateVisibility()
    {
        ItemInteractable selectedItem = GetEquippedItem();
        bool hasItem = selectedItem != null;

        if (hasItem)
        {
            // 1. Mostrar nombre del ítem
            if (weaponNameText != null)
            {
                weaponNameText.text = !string.IsNullOrEmpty(selectedItem.ItemName)
                    ? selectedItem.ItemName.ToUpper()
                    : string.Empty;
            }

            // 2. Mostrar icono del ítem
            if (weaponIconImage != null)
            {
                weaponIconImage.sprite = selectedItem.Icon;
                weaponIconImage.enabled = selectedItem.Icon != null;
                weaponIconImage.preserveAspect = true;
            }

            // 3. El único objeto con munición es el arma (pistola). Para el resto, no mostrar munición.
            if (selectedItem.ItemType == ItemType.SilencedPistol)
            {
                if (playerCombat != null)
                {
                    UpdateAmmoDisplay(playerCombat.GetCurrentAmmo(), playerCombat.GetMaxAmmo());
                }
                else
                {
                    if (currentAmmoText != null) currentAmmoText.gameObject.SetActive(true);
                    if (maxAmmoText != null) maxAmmoText.gameObject.SetActive(true);
                }
            }
            else
            {
                HideAmmoElements();
            }
        }
        else
        {
            HideAmmoElements();
        }

        // Controlar visibilidad del panel con CanvasGroup (1 visible, 0 invisible)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = hasItem ? 1f : 0f;
            canvasGroup.blocksRaycasts = hasItem;
            canvasGroup.interactable = hasItem;
        }
    }

    private void UpdateAmmoDisplay(int current, int max)
    {
        if (currentAmmoText != null)
        {
            currentAmmoText.gameObject.SetActive(true);
            currentAmmoText.text = current.ToString();
            currentAmmoText.color = current <= 2 ? lowAmmoColor : normalAmmoColor;
        }

        if (maxAmmoText != null)
        {
            maxAmmoText.gameObject.SetActive(true);
            maxAmmoText.text = "/ " + max.ToString();
        }

        if (reloadPromptText != null)
        {
            bool isPistol = IsPistolEquipped();
            if (isPistol && (current <= 0 || (playerCombat != null && playerCombat.IsReloading)))
            {
                reloadPromptText.text = playerCombat != null && playerCombat.IsReloading
                    ? "RECARGANDO..."
                    : "R PARA RECARGAR";
                reloadPromptText.gameObject.SetActive(true);
            }
            else
            {
                reloadPromptText.gameObject.SetActive(false);
            }
        }
    }

    private void HideAmmoElements()
    {
        if (currentAmmoText != null)
        {
            currentAmmoText.text = string.Empty;
            currentAmmoText.gameObject.SetActive(false);
        }

        if (maxAmmoText != null)
        {
            maxAmmoText.text = string.Empty;
            maxAmmoText.gameObject.SetActive(false);
        }

        if (reloadPromptText != null)
        {
            reloadPromptText.gameObject.SetActive(false);
        }
    }
}
