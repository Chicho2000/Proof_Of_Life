using UnityEngine;

public class ItemInteractable : Interactable
{
    [Tooltip("Identidad estable usada para apilar y comparar pickups equivalentes.")]
    [SerializeField] private string itemId;

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

    public string ItemId => !string.IsNullOrWhiteSpace(itemId)
        ? itemId.Trim()
        : $"type:{(int)itemType}";
    public string ItemName => itemName;
    public ItemType ItemType => itemType;
    public string Description => description;
    public Sprite Icon => icon;
    public bool IsStackable => isStackable;
    public int MaxStack => Mathf.Max(1, maxStack);
    public int Amount => amount;
    public GameObject WorldPrefab => worldPrefab;
    public GameObject InHandPrefab => inHandPrefab != null ? inHandPrefab : WorldPrefab;
    public Vector3 InHandPositionOffset => inHandPositionOffset;
    public Vector3 InHandRotationOffset => inHandRotationOffset;
    public Vector3 InHandScale => inHandScale;
    public void Initialize(int newAmount = 1)
    {
        amount = Mathf.Max(1, newAmount);
        canInteract = true;
        UpdatePrompt();
    }

    private void Start()
    {
        UpdatePrompt();
    }

    private void OnValidate()
    {
        maxStack = Mathf.Max(1, maxStack);
        amount = Mathf.Max(1, amount);
        UpdatePrompt();
    }

    public bool HasSameIdentity(ItemInteractable other)
    {
        return other != null
            && string.Equals(ItemId, other.ItemId, System.StringComparison.Ordinal);
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
            bool added = hotbar.AddItem(this, amount);
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

                if (hotbar.IsStoredItemSource(this))
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.Log($"[ItemInteractable] No se pudo recoger {itemName}.");
            }
        }
        else
        {
            Debug.LogWarning($"[ItemInteractable] El interactor {interactor.name} no posee un componente PlayerHotbar.");
        }
    }
}
