using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class InventorySlotUI
{
    [Tooltip("Transform raíz del slot (ej. Slot 1)")]
    public RectTransform slotRoot;

    [Tooltip("Fondo visual del slot (opcional)")]
    public Image slotBackground;

    [Tooltip("Componente Image donde se muestra el icono del ítem")]
    public Image itemIcon;

    [Header("Textos (Soporta TextMeshPro y UI.Text)")]
    public TMP_Text slotNumberText;
    public Text legacySlotNumberText;

    public TMP_Text countText;
    public Text legacyCountText;

    public TMP_Text itemNameText;
    public Text legacyItemNameText;

    [Header("Interacción y Selección")]
    [Tooltip("Objeto o borde que se enciende cuando este slot está seleccionado")]
    public GameObject selectionHighlight;

    [Tooltip("Botón interactuable para seleccionar el slot al hacer click")]
    public Button slotButton;

    public void UpdateSlot(HotbarSlot slot, bool isSelected)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(isSelected);
        }

        if (slot == null || slot.IsEmpty || slot.Item == null)
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
            }
            SetCountText(string.Empty);
            SetItemNameText(string.Empty);
        }
        else
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = slot.Item.Icon;
                itemIcon.enabled = slot.Item.Icon != null;
            }
            SetCountText(slot.Count > 1 ? $"x{slot.Count}" : string.Empty);
            SetItemNameText(slot.Item.ItemName);
        }
    }

    public void SetCountText(string text)
    {
        if (countText != null) countText.text = text;
        if (legacyCountText != null) legacyCountText.text = text;
    }

    public void SetItemNameText(string text)
    {
        if (itemNameText != null) itemNameText.text = text;
        if (legacyItemNameText != null) legacyItemNameText.text = text;
    }

    public void SetSlotNumberText(string text)
    {
        if (slotNumberText != null) slotNumberText.text = text;
        if (legacySlotNumberText != null) legacySlotNumberText.text = text;
    }
}

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Referencias Principales")]
    [SerializeField] private PlayerHotbar playerHotbar;

    [Tooltip("Panel o GameObject 'Inventory' que contiene los 6 slots y que se muestra/oculta")]
    [SerializeField] private GameObject inventoryPanel;

    [Tooltip("CanvasGroup para controlar visibilidad y bloquear raycasts (se añade automáticamente si falta)")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Configuración de Teclas")]
    [Tooltip("Tecla para abrir / cerrar el inventario")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Q;

    [Tooltip("Tecla para soltar el ítem seleccionado")]
    [SerializeField] private KeyCode dropKey = KeyCode.G;

    [Header("Comportamiento")]
    [Tooltip("Desbloquea el cursor del mouse al abrir el inventario para poder clickear ranuras")]
    [SerializeField] private bool unlockCursorWhenOpen = true;

    [Tooltip("Pausa el tiempo de juego mientras el inventario está abierto")]
    [SerializeField] private bool pauseGameWhenOpen = false;

    [Tooltip("Cierra el inventario al presionar Escape")]
    [SerializeField] private bool closeOnEscape = true;

    [Header("Ranuras del Inventario (6 Slots)")]
    [SerializeField] private List<InventorySlotUI> uiSlots = new List<InventorySlotUI>();

    [SerializeField] private Button dropButton;

    [Header("Audio (Opcional)")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip dropSound;

    // Eventos
    public event Action<bool> OnInventoryToggled;
    public event Action<int> OnSlotSelected;

    private bool isOpen = false;
    public bool IsInventoryOpen => isOpen;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ResolvePanelReference();
        ResolvePlayerHotbar();
        AutoFindSlots();
        SetupSlotButtons();

        if (dropButton != null)
        {
            dropButton.onClick.RemoveAllListeners();
            dropButton.onClick.AddListener(DropSelectedItem);
        }
    }

    private void Start()
    {
        if (playerHotbar == null)
        {
            ResolvePlayerHotbar();
        }

        if (playerHotbar != null)
        {
            playerHotbar.OnSlotUpdated += HandleSlotUpdated;
            playerHotbar.OnSlotSelected += HandleSlotSelected;
        }

        // El inventario inicia cerrado por defecto
        SetInventoryOpen(false);
    }

    private void OnDestroy()
    {
        if (playerHotbar != null)
        {
            playerHotbar.OnSlotUpdated -= HandleSlotUpdated;
            playerHotbar.OnSlotSelected -= HandleSlotSelected;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        // 1. Abrir / Cerrar con la tecla Q
        if (Input.GetKeyDown(toggleKey))
        {
            // Evitar alternar si estamos en pausa (Time.timeScale <= 0 y el inventario no fue el que pausó)
            if (Time.timeScale <= 0f && !isOpen)
            {
                return;
            }

            ToggleInventory();
            return;
        }

        if (!isOpen)
        {
            return;
        }

        // 2. Cerrar con Escape si está configurado
        if (closeOnEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
            return;
        }

        // 3. Soltar el ítem seleccionado con la tecla V
        if (Input.GetKeyDown(dropKey))
        {
            DropSelectedItem();
        }

        // 4. Teclas numéricas 1 al 6 para selección rápida dentro del inventario
        HandleNumericSlotSelection();
        HandleDirectionalSlotSelection();
        ConfirmSelectedSlot();
    }

    public void ToggleInventory()
    {
        SetInventoryOpen(!isOpen);
    }

    public void OpenInventory()
    {
        SetInventoryOpen(true);
    }

    public void CloseInventory()
    {
        SetInventoryOpen(false);
    }

    public void SetInventoryOpen(bool open)
    {
        isOpen = open;

        if (canvasGroup == null && inventoryPanel != null)
        {
            canvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
        }

        if (inventoryPanel != null)
        {
            if (inventoryPanel != gameObject)
            {
                // Si este script está en el Canvas y inventoryPanel es el GameObject hijo "Inventory"
                inventoryPanel.SetActive(open);
            }
            else
            {
                // Si el script está directamente en el GameObject "Inventory"
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }

                canvasGroup.alpha = open ? 1f : 0f;
                canvasGroup.interactable = open;
                canvasGroup.blocksRaycasts = open;

                // Ocultar/mostrar los hijos visuales para que no se rendericen cuando está cerrado
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(open);
                }
            }
        }

        if (open)
        {
            RefreshAll();
            UpdateItemDetails();
            FocusSlotButton(playerHotbar != null ? playerHotbar.SelectedSlotIndex : -1);

            if (unlockCursorWhenOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (pauseGameWhenOpen)
            {
                Time.timeScale = 0f;
            }

            PlaySound(openSound);
        }
        else
        {
            if (unlockCursorWhenOpen)
            {
                // Restaurar el cursor solo si el juego sigue corriendo
                if (Time.timeScale > 0f)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            if (pauseGameWhenOpen)
            {
                Time.timeScale = 1f;
            }

            PlaySound(closeSound);
        }

        OnInventoryToggled?.Invoke(open);
    }

    public void SelectSlot(int slotIndex)
    {
        if (playerHotbar != null)
        {
            playerHotbar.SelectSlot(slotIndex);
        }

        RefreshSelectionVisuals();
        UpdateItemDetails();
        if (isOpen)
        {
            FocusSlotButton(slotIndex);
        }
        PlaySound(selectSound);

        OnSlotSelected?.Invoke(slotIndex);
    }

    public void DropSelectedItem()
    {
        if (playerHotbar == null) return;

        bool dropped = playerHotbar.DropSelectedItem();
        if (dropped)
        {
            PlaySound(dropSound);
            RefreshAll();
            UpdateItemDetails();
        }
    }

    public void DropItemFromSlot(int slotIndex)
    {
        if (playerHotbar == null) return;

        playerHotbar.SelectSlot(slotIndex);
        DropSelectedItem();
    }

    public void RefreshAll()
    {
        if (playerHotbar == null)
        {
            ResolvePlayerHotbar();
            if (playerHotbar == null) return;
        }

        int activeCount = Mathf.Min(uiSlots.Count, playerHotbar.SlotCount);
        for (int i = 0; i < activeCount; i++)
        {
            HotbarSlot slot = playerHotbar.GetSlot(i);
            bool isSelected = (i == playerHotbar.SelectedSlotIndex);
            uiSlots[i].UpdateSlot(slot, isSelected);
        }

        RefreshSelectionVisuals();
    }

    private void RefreshSelectionVisuals()
    {
        int selectedIndex = playerHotbar != null ? playerHotbar.SelectedSlotIndex : -1;
        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (uiSlots[i].selectionHighlight != null)
            {
                uiSlots[i].selectionHighlight.SetActive(i == selectedIndex);
            }
        }
    }

    private void UpdateItemDetails()
    {
        if (playerHotbar == null) return;

        if (dropButton != null)
        {
            dropButton.interactable = playerHotbar.GetSelectedItem() != null;
        }
    }

    private void HandleSlotUpdated(int slotIndex, HotbarSlot slot)
    {
        // Como los slots se compactan (se desplazan), un cambio en cualquier slot
        // puede afectar a todos, así que refrescamos todo el inventario de una vez.
        RefreshAll();
        UpdateItemDetails();
    }

    private void HandleSlotSelected(int selectedIndex)
    {
        RefreshSelectionVisuals();
        UpdateItemDetails();
        if (isOpen)
        {
            FocusSlotButton(selectedIndex);
        }
    }

    private void HandleNumericSlotSelection()
    {
        for (int i = 0; i < uiSlots.Count && i < 6; i++)
        {
            KeyCode key = KeyCode.Alpha1 + i;
            if (Input.GetKeyDown(key))
            {
                SelectSlot(i);
                return;
            }
        }
    }

    private void HandleDirectionalSlotSelection()
    {
        if (playerHotbar == null) return;

        Vector2 direction = Vector2.zero;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) direction = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) direction = Vector2.right;
        else if (Input.GetKeyDown(KeyCode.UpArrow)) direction = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) direction = Vector2.down;

        if (direction == Vector2.zero) return;

        int activeCount = Mathf.Min(uiSlots.Count, playerHotbar.SlotCount);
        if (activeCount == 0) return;

        int currentIndex = Mathf.Clamp(playerHotbar.SelectedSlotIndex, 0, activeCount - 1);
        int nextIndex = FindAdjacentSlot(currentIndex, direction, activeCount);
        if (nextIndex >= 0)
        {
            SelectSlot(nextIndex);
        }
    }

    private int FindAdjacentSlot(int currentIndex, Vector2 direction, int activeCount)
    {
        RectTransform currentSlot = uiSlots[currentIndex].slotRoot;
        if (currentSlot == null) return -1;

        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        float bestScore = float.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < activeCount; i++)
        {
            if (i == currentIndex || uiSlots[i].slotRoot == null) continue;

            Vector2 delta = uiSlots[i].slotRoot.anchoredPosition - currentSlot.anchoredPosition;
            float distanceInDirection = Vector2.Dot(delta, direction);
            if (distanceInDirection <= 0.01f) continue;

            float lateralDistance = Mathf.Abs(Vector2.Dot(delta, perpendicular));
            float score = distanceInDirection + lateralDistance * 2f;
            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void ConfirmSelectedSlot()
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;
        if (playerHotbar == null) return;

        HotbarSlot selectedSlot = playerHotbar.GetSlot(playerHotbar.SelectedSlotIndex);
        if (selectedSlot != null && !selectedSlot.IsEmpty)
        {
            CloseInventory();
        }
    }

    private void SelectSlotFromInventory(int slotIndex)
    {
        SelectSlot(slotIndex);

        HotbarSlot selectedSlot = playerHotbar != null ? playerHotbar.GetSlot(slotIndex) : null;
        if (selectedSlot != null && !selectedSlot.IsEmpty)
        {
            CloseInventory();
        }
    }

    private void FocusSlotButton(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= uiSlots.Count) return;

        Button slotButton = uiSlots[slotIndex].slotButton;
        if (slotButton == null || !slotButton.isActiveAndEnabled) return;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(slotButton.gameObject);
        }
    }

    private void SetupSlotButtons()
    {
        for (int i = 0; i < uiSlots.Count; i++)
        {
            int index = i;
            InventorySlotUI slotUI = uiSlots[i];

            if (slotUI.slotButton == null && slotUI.slotRoot != null)
            {
                slotUI.slotButton = slotUI.slotRoot.GetComponent<Button>();
                if (slotUI.slotButton == null)
                {
                    slotUI.slotButton = slotUI.slotRoot.gameObject.AddComponent<Button>();
                }
            }

            if (slotUI.slotButton != null)
            {
                ConfigureSlotButton(slotUI);
                slotUI.slotButton.onClick.RemoveAllListeners();
                slotUI.slotButton.onClick.AddListener(() => SelectSlotFromInventory(index));
            }
        }
    }

    private void ConfigureSlotButton(InventorySlotUI slotUI)
    {
        Button slotButton = slotUI.slotButton;
        if (slotButton == null) return;

        slotButton.targetGraphic = slotUI.slotBackground;

        ColorBlock colors = slotButton.colors;
        colors.normalColor = slotUI.slotBackground != null ? slotUI.slotBackground.color : Color.white;
        colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        colors.selectedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        slotButton.colors = colors;

        Navigation navigation = slotButton.navigation;
        navigation.mode = Navigation.Mode.None;
        slotButton.navigation = navigation;
    }

    public void AutoFindSlots()
    {
        if (uiSlots != null && uiSlots.Count >= 6)
        {
            return;
        }

        if (inventoryPanel == null)
        {
            ResolvePanelReference();
        }

        if (inventoryPanel == null)
        {
            return;
        }

        if (uiSlots == null)
        {
            uiSlots = new List<InventorySlotUI>();
        }
        else
        {
            uiSlots.Clear();
        }

        Transform container = FindSlotsContainer(inventoryPanel.transform);
        List<Transform> candidateSlots = new List<Transform>();

        // 1. Buscar transforms que contengan la palabra "Slot"
        for (int i = 0; i < container.childCount; i++)
        {
            Transform child = container.GetChild(i);
            if (child.name.IndexOf("slot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                candidateSlots.Add(child);
            }
        }

        // 2. Si no tienen "Slot" en el nombre, tomar los primeros 6 hijos omitiendo fondos o logos
        if (candidateSlots.Count == 0)
        {
            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
                string lower = child.name.ToLower();
                if (lower.Contains("title") || lower.Contains("header") || lower.Contains("logo")
                    || lower.Contains("background") || lower.Contains("bg") || lower.Contains("close"))
                {
                    continue;
                }
                candidateSlots.Add(child);
                if (candidateSlots.Count >= 6) break;
            }
        }

        // Ordenar naturalmente para que Slot 1 vaya antes que Slot 2, etc.
        candidateSlots.Sort((a, b) => NaturalCompare(a.name, b.name));

        for (int i = 0; i < candidateSlots.Count && i < 6; i++)
        {
            uiSlots.Add(CreateSlotUIFromTransform(candidateSlots[i], i + 1));
        }

        // Inicializar todos los iconos desactivados al arrancar
        foreach (InventorySlotUI s in uiSlots)
        {
            if (s.itemIcon != null)
            {
                s.itemIcon.sprite  = null;
                s.itemIcon.enabled = false;
            }
        }
    }

    private Transform FindSlotsContainer(Transform root)
    {
        string[] containerNames = { "Slots", "SlotsContainer", "SlotContainer", "Grid", "Content", "SlotsPanel", "Panel_Slots" };
        foreach (string name in containerNames)
        {
            Transform found = root.Find(name);
            if (found != null) return found;
        }

        foreach (Transform child in root)
        {
            foreach (string name in containerNames)
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return child;
            }
        }

        return root;
    }

    private InventorySlotUI CreateSlotUIFromTransform(Transform t, int slotNumber)
    {
        InventorySlotUI slot = new InventorySlotUI();
        slot.slotRoot       = t as RectTransform;
        slot.slotBackground = t.GetComponent<Image>();
        slot.slotButton     = t.GetComponent<Button>();

        // Si el GameObject ya tiene un SlotUI, usar sus referencias directamente
        SlotUI slotUIComp = t.GetComponent<SlotUI>();
        if (slotUIComp != null)
        {
            slot.itemIcon          = slotUIComp.ItemIconImage;
            slot.selectionHighlight = slotUIComp.Highlight;

            if (slot.itemIcon != null)
            {
                return slot;
            }
        }

        // Buscar imagen de ícono en los hijos
        Image[] images = t.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.transform == t) continue;
            string lower = img.name.ToLower();
            if (lower.Contains("icon") || lower.Contains("item") || lower.Contains("sprite"))
            {
                slot.itemIcon = img;
                break;
            }
        }

        if (slot.itemIcon == null)
        {
            foreach (var img in images)
            {
                if (img.transform != t)
                {
                    slot.itemIcon = img;
                    break;
                }
            }
        }

        // Buscar textos TMP
        TMP_Text[] tmps = t.GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in tmps)
        {
            string lower = txt.name.ToLower();
            if (lower.Contains("num") || lower.Contains("index") || lower.Contains("slot"))
            {
                slot.slotNumberText = txt;
            }
            else if (lower.Contains("count") || lower.Contains("cant") || lower.Contains("amount") || lower.Contains("qty"))
            {
                slot.countText = txt;
            }
            else if (lower.Contains("name") || lower.Contains("nombre") || lower.Contains("title"))
            {
                slot.itemNameText = txt;
            }
        }

        // Buscar textos Legacy UI
        Text[] legacyTexts = t.GetComponentsInChildren<Text>(true);
        foreach (var txt in legacyTexts)
        {
            string lower = txt.name.ToLower();
            if (lower.Contains("num") || lower.Contains("index") || lower.Contains("slot"))
            {
                slot.legacySlotNumberText = txt;
            }
            else if (lower.Contains("count") || lower.Contains("cant") || lower.Contains("amount") || lower.Contains("qty"))
            {
                slot.legacyCountText = txt;
            }
            else if (lower.Contains("name") || lower.Contains("nombre") || lower.Contains("title"))
            {
                slot.legacyItemNameText = txt;
            }
        }

        // Asignar el número de slot (1 al 6) visualmente si el texto existe y está vacío
        if (slot.slotNumberText != null && string.IsNullOrEmpty(slot.slotNumberText.text))
        {
            slot.slotNumberText.text = slotNumber.ToString();
        }
        if (slot.legacySlotNumberText != null && string.IsNullOrEmpty(slot.legacySlotNumberText.text))
        {
            slot.legacySlotNumberText.text = slotNumber.ToString();
        }

        // Buscar highlight de selección
        Transform hl = t.Find("Highlight");
        if (hl == null) hl = t.Find("Selection");
        if (hl == null) hl = t.Find("Selected");
        if (hl == null) hl = t.Find("Border");
        if (hl == null) hl = t.Find("Frame");
        if (hl != null) slot.selectionHighlight = hl.gameObject;

        return slot;
    }

    private void ResolvePanelReference()
    {
        if (inventoryPanel != null) return;

        if (gameObject.name.IndexOf("Inventory", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            inventoryPanel = gameObject;
            return;
        }

        Transform child = transform.Find("Inventory");
        if (child != null)
        {
            inventoryPanel = child.gameObject;
            return;
        }

        GameObject found = GameObject.Find("Inventory");
        if (found != null)
        {
            inventoryPanel = found;
            return;
        }

        inventoryPanel = gameObject;
    }

    private void ResolvePlayerHotbar()
    {
        if (playerHotbar != null) return;

        playerHotbar = FindFirstObjectByType<PlayerHotbar>();
        if (playerHotbar == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerHotbar = player.GetComponent<PlayerHotbar>();
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        Camera cam = Camera.main;
        Vector3 pos = cam != null ? cam.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(clip, pos);
    }

    private int NaturalCompare(string a, string b)
    {
        int numA = ExtractNumber(a);
        int numB = ExtractNumber(b);
        if (numA != -1 && numB != -1)
        {
            return numA.CompareTo(numB);
        }
        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private int ExtractNumber(string s)
    {
        if (string.IsNullOrEmpty(s)) return -1;
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(s, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int result))
        {
            return result;
        }
        return -1;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (inventoryPanel == null)
        {
            ResolvePanelReference();
        }
    }
#endif
}
