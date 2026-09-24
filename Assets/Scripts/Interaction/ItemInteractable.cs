using UnityEngine;

public class ItemInteractable : Interactable
{
    [Header("Datos del Ítem")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;

    [Header("Feedback Audio/Visual")]
    [SerializeField] private AudioClip pickupSound;

    public ItemData ItemData => itemData;
    public int Amount => amount;

    public void Initialize(ItemData newItemData, int newAmount = 1)
    {
        itemData = newItemData;
        amount = Mathf.Max(1, newAmount);
        canInteract = itemData != null;
        UpdatePrompt();
    }

    private void Start()
    {
        UpdatePrompt();
    }

    private void OnValidate()
    {
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (itemData != null)
        {
            interactionPrompt = amount > 1 
                ? $"Recoger {itemData.ItemName} x{amount}" 
                : $"Recoger {itemData.ItemName}";
        }
        else
        {
            interactionPrompt = "Recoger Objeto";
        }
    }

    public override void Interact(GameObject interactor)
    {
        if (interactor == null || itemData == null) return;

        PlayerHotbar hotbar = interactor.GetComponent<PlayerHotbar>();
        if (hotbar == null)
        {
            hotbar = interactor.GetComponentInParent<PlayerHotbar>();
        }

        if (hotbar != null)
        {
            bool added = hotbar.AddItem(itemData, amount);
            if (added)
            {
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"[ItemInteractable] No hay espacio en la Hotbar para recoger {itemData.ItemName}.");
            }
        }
        else
        {
            Debug.LogWarning($"[ItemInteractable] El interactor {interactor.name} no posee un componente PlayerHotbar.");
        }
    }
}
