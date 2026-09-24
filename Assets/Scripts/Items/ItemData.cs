using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "ProofOfLife/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identificación")]
    [SerializeField] private string itemName = "Nuevo Ítem";
    [SerializeField] private ItemType itemType = ItemType.None;
    [TextArea(2, 4)]
    [SerializeField] private string description;

    [Header("Visuales en el Mundo")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject worldPrefab;

    [Header("Visuales en Mano (Primera Persona)")]
    [SerializeField] private GameObject inHandPrefab;
    [SerializeField] private Vector3 inHandPositionOffset = new Vector3(0.25f, -0.2f, 0.45f);
    [SerializeField] private Vector3 inHandRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 inHandScale = Vector3.one;

    [Header("Comportamiento")]
    [SerializeField] private bool isStackable = false;
    [SerializeField] private int maxStack = 1;

    // Propiedades públicas de solo lectura
    public string ItemName => itemName;
    public ItemType ItemType => itemType;
    public string Description => description;
    public Sprite Icon => icon;
    public GameObject WorldPrefab => worldPrefab;

    public GameObject InHandPrefab => inHandPrefab != null ? inHandPrefab : worldPrefab;
    public Vector3 InHandPositionOffset => inHandPositionOffset;
    public Vector3 InHandRotationOffset => inHandRotationOffset;
    public Vector3 InHandScale => inHandScale;

    public bool IsStackable => isStackable;
    public int MaxStack => Mathf.Max(1, maxStack);
}
