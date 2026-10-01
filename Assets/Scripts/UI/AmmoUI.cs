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
        if (playerCombat == null)
        {
            playerCombat = FindFirstObjectByType<PlayerCombat>();
        }

        if (playerHotbar == null)
        {
            playerHotbar = FindFirstObjectByType<PlayerHotbar>();
        }

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

    private void OnEnable()
    {
        if (playerCombat != null)
        {
            playerCombat.OnAmmoChanged += HandleAmmoChanged;
        }

        if (playerHotbar != null)
        {
            playerHotbar.OnSlotSelected += HandleSlotSelected;
            playerHotbar.OnSlotUpdated += HandleSlotUpdated;
        }
    }

    private void OnDisable()
    {
        if (playerCombat != null)
        {
            playerCombat.OnAmmoChanged -= HandleAmmoChanged;
        }

        if (playerHotbar != null)
        {
            playerHotbar.OnSlotSelected -= HandleSlotSelected;
            playerHotbar.OnSlotUpdated -= HandleSlotUpdated;
        }
    }

    private void Start()
    {
        UpdateVisibility();

        if (playerCombat != null)
        {
            HandleAmmoChanged(playerCombat.GetCurrentAmmo(), playerCombat.GetMaxAmmo());
        }
    }

    private void Update()
    {
        // Doble chequeo en tiempo de juego por si cambió el ítem en mano
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
        if (currentAmmoText != null)
        {
            currentAmmoText.text = current.ToString();
            if (current <= 2)
            {
                currentAmmoText.color = lowAmmoColor;
            }
            else
            {
                currentAmmoText.color = normalAmmoColor;
            }
        }

        if (maxAmmoText != null)
        {
            maxAmmoText.text = "/ " + max.ToString();
        }

        if (reloadPromptText != null)
        {
            bool isPistol = IsPistolEquipped();
            if (isPistol && current <= 0)
            {
                reloadPromptText.gameObject.SetActive(true);
            }
            else
            {
                reloadPromptText.gameObject.SetActive(false);
            }
        }
    }

    private bool IsPistolEquipped()
    {
        if (MissionManager.Instance != null && (MissionManager.Instance.IsMissionCompleted || MissionManager.Instance.IsMissionFailed))
        {
            return false;
        }

        if (playerHotbar == null) return false;

        ItemInteractable selectedItem = playerHotbar.GetSelectedItem();
        if (selectedItem != null && selectedItem.ItemType == ItemType.SilencedPistol)
        {
            return true;
        }
        return false;
    }

    private void UpdateVisibility()
    {
        bool isPistol = IsPistolEquipped();

        if (isPistol && playerHotbar != null)
        {
            ItemInteractable selectedItem = playerHotbar.GetSelectedItem();
            if (weaponNameText != null)
            {
                weaponNameText.text = selectedItem.ItemName.ToUpper();
            }

            if (weaponIconImage != null && selectedItem.Icon != null)
            {
                weaponIconImage.sprite = selectedItem.Icon;
            }
        }

        // Controlar visibilidad con CanvasGroup (1 visible, 0 invisible)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isPistol ? 1f : 0f;
            canvasGroup.blocksRaycasts = isPistol;
            canvasGroup.interactable = isPistol;
        }

        if (reloadPromptText != null && !isPistol)
        {
            reloadPromptText.gameObject.SetActive(false);
        }
    }
}
