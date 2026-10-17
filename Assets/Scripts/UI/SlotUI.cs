using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente que vive en la raíz de cada slot del inventario.
/// Se encarga de mostrar el ícono del ítem, el nombre y la cantidad,
/// y de activar / desactivar el highlight de selección.
///
/// Solo tienes que ponerlo en cada uno de los 6 GameObjects "Slot X"
/// (o dejarlo vacío y se auto-detectará en Awake).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SlotUI : MonoBehaviour
{
    [Header("Icono del Ítem")]
    [Tooltip("Image donde se dibujará el ícono. Se detecta automáticamente si se deja vacío.")]
    [SerializeField] private Image itemIconImage;

    [Header("Textos (opcionales)")]
    [SerializeField] private TMP_Text countTextTMP;
    [SerializeField] private Text countTextLegacy;

    [SerializeField] private TMP_Text itemNameTextTMP;
    [SerializeField] private Text itemNameTextLegacy;

    [Header("Highlight de Selección")]
    [Tooltip("GameObject que se activa cuando este slot está seleccionado. Se detecta automáticamente si se deja vacío.")]
    [SerializeField] private GameObject selectionHighlight;

    // ── Accesores públicos para que InventoryUI pueda leer las referencias ──
    public Image ItemIconImage   => itemIconImage;
    public GameObject Highlight  => selectionHighlight;

    // ───────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        AutoFindReferences();
    }

    // ── Búsqueda automática de referencias ──────────────────────────────────
    private void AutoFindReferences()
    {
        if (itemIconImage == null)
        {
            // 1. Buscar Image con "icon" / "item" / "sprite" en el nombre
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img.transform == transform) continue;
                string lower = img.name.ToLower();
                if (lower.Contains("icon") || lower.Contains("item") || lower.Contains("sprite"))
                {
                    itemIconImage = img;
                    break;
                }
            }

            // 2. Fallback: primer hijo con Image (que no sea el fondo del slot)
            if (itemIconImage == null)
            {
                foreach (Image img in images)
                {
                    if (img.transform != transform)
                    {
                        itemIconImage = img;
                        break;
                    }
                }
            }
        }

        if (selectionHighlight == null)
        {
            string[] names = { "Highlight", "Selection", "Selected", "Border", "Frame", "Outline" };
            foreach (string n in names)
            {
                Transform t = transform.Find(n);
                if (t != null)
                {
                    selectionHighlight = t.gameObject;
                    break;
                }
            }
        }

        // Textos TMP
        if (countTextTMP == null || itemNameTextTMP == null)
        {
            TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text txt in tmps)
            {
                string lower = txt.name.ToLower();
                if (countTextTMP == null && (lower.Contains("count") || lower.Contains("cant") || lower.Contains("amount") || lower.Contains("qty")))
                    countTextTMP = txt;
                else if (itemNameTextTMP == null && (lower.Contains("name") || lower.Contains("nombre") || lower.Contains("title")))
                    itemNameTextTMP = txt;
            }
        }

        // Textos legacy
        if (countTextLegacy == null || itemNameTextLegacy == null)
        {
            Text[] legacyTexts = GetComponentsInChildren<Text>(true);
            foreach (Text txt in legacyTexts)
            {
                string lower = txt.name.ToLower();
                if (countTextLegacy == null && (lower.Contains("count") || lower.Contains("cant") || lower.Contains("amount") || lower.Contains("qty")))
                    countTextLegacy = txt;
                else if (itemNameTextLegacy == null && (lower.Contains("name") || lower.Contains("nombre") || lower.Contains("title")))
                    itemNameTextLegacy = txt;
            }
        }
    }

    // ── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Actualiza la visualización del slot con los datos de <paramref name="slot"/>.
    /// Llama a este método desde InventoryUI cada vez que el slot cambia.
    /// </summary>
    public void UpdateDisplay(HotbarSlot slot, bool isSelected = false)
    {
        SetSelected(isSelected);

        bool isEmpty = slot == null || slot.IsEmpty || slot.Item == null;

        // ── Ícono ──────────────────────────────────────────────────────────
        if (itemIconImage != null)
        {
            if (isEmpty || slot.Item.Icon == null)
            {
                itemIconImage.sprite  = null;
                itemIconImage.enabled = false;
            }
            else
            {
                itemIconImage.sprite  = slot.Item.Icon;
                itemIconImage.enabled = true;
            }
        }

        // ── Cantidad ────────────────────────────────────────────────────────
        string countStr = (!isEmpty && slot.Count > 1) ? $"x{slot.Count}" : string.Empty;
        if (countTextTMP    != null) countTextTMP.text    = countStr;
        if (countTextLegacy != null) countTextLegacy.text = countStr;

        // ── Nombre ──────────────────────────────────────────────────────────
        string nameStr = isEmpty ? string.Empty : slot.Item.ItemName;
        if (itemNameTextTMP    != null) itemNameTextTMP.text    = nameStr;
        if (itemNameTextLegacy != null) itemNameTextLegacy.text = nameStr;
    }

    /// <summary>
    /// Limpia el slot: sin ícono, sin texto.
    /// </summary>
    public void ClearDisplay()
    {
        UpdateDisplay(null, false);
    }

    /// <summary>
    /// Activa o desactiva el highlight de selección.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(selected);
        }
    }
}