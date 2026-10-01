using UnityEngine;

public class ItemInteractable : Interactable
{
    [Header("Configuración del Ítem (En Objeto Padre)")]
    [SerializeField] private string itemName = "Nuevo Ítem";
    [SerializeField] private ItemType itemType = ItemType.None;
    [TextArea(2, 3)]
    [SerializeField] private string description = "";
    [SerializeField] private Sprite icon;
    [SerializeField] private bool isStackable = false;
    [SerializeField] private int maxStack = 1;
    [SerializeField] private int amount = 1;

    [Header("Visuales en Mano y Mundo")]
    [Tooltip("Prefab que se genera al soltar el ítem al suelo (tecla G).")]
    [SerializeField] private GameObject worldPrefab;
    [Tooltip("Modelo o prefab que se visualiza en la mano del jugador.")]
    [SerializeField] private GameObject inHandPrefab;
    [SerializeField] private Vector3 inHandPositionOffset = new Vector3(0.25f, -0.2f, 0.45f);
    [SerializeField] private Vector3 inHandRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 inHandScale = Vector3.one;

    [Header("Feedback Audio/Visual")]
    [SerializeField] private AudioClip pickupSound;

    private ItemData cachedRuntimeData;

    public ItemData ItemData
    {
        get
        {
            if (cachedRuntimeData == null)
            {
                cachedRuntimeData = ScriptableObject.CreateInstance<ItemData>();
                cachedRuntimeData.Initialize(
                    itemName,
                    itemType,
                    description,
                    icon,
                    worldPrefab != null ? worldPrefab : gameObject,
                    inHandPrefab,
                    inHandPositionOffset,
                    inHandRotationOffset,
                    inHandScale,
                    isStackable,
                    maxStack
                );
            }
            return cachedRuntimeData;
        }
    }

    public int Amount => amount;

    public void Initialize(ItemData newItemData, int newAmount = 1)
    {
        if (newItemData != null)
        {
            itemName = newItemData.ItemName;
            itemType = newItemData.ItemType;
            description = newItemData.Description;
            icon = newItemData.Icon;
            worldPrefab = newItemData.WorldPrefab;
            inHandPrefab = newItemData.InHandPrefab;
            inHandPositionOffset = newItemData.InHandPositionOffset;
            inHandRotationOffset = newItemData.InHandRotationOffset;
            inHandScale = newItemData.InHandScale;
            isStackable = newItemData.IsStackable;
            maxStack = newItemData.MaxStack;
        }
        amount = Mathf.Max(1, newAmount);
        canInteract = true;
        cachedRuntimeData = null;
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
        string name = !string.IsNullOrEmpty(itemName) ? itemName : gameObject.name;
        interactionPrompt = amount > 1 
            ? $"Recoger {name} x{amount}" 
            : $"Recoger {name}";
        canInteract = true;
    }

    public override void Interact(GameObject interactor)
    {
        if (interactor == null) return;

        PlayerHotbar hotbar = interactor.GetComponent<PlayerHotbar>();
        if (hotbar == null)
        {
            hotbar = interactor.GetComponentInParent<PlayerHotbar>();
        }

        if (hotbar != null)
        {
            ItemData data = ItemData;
            if (data == null) return;

            bool added = hotbar.AddItem(data, amount);
            if (added)
            {
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                PlayerInteraction playerInteraction = interactor.GetComponent<PlayerInteraction>();
                if (playerInteraction != null && playerInteraction.currentInteractable == this)
                {
                    playerInteraction.ClearCurrentInteractable();
                }

                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"[ItemInteractable] No hay espacio en la Hotbar para recoger {data.ItemName}.");
            }
        }
        else
        {
            Debug.LogWarning($"[ItemInteractable] El interactor {interactor.name} no posee un componente PlayerHotbar.");
        }
    }
}
