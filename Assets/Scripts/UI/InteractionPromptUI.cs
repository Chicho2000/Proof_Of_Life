using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("Referencias UI - Mensaje")]
    [SerializeField] private GameObject promptContainer;
    [SerializeField] private TMP_Text promptText;

    [Header("Barra de Progreso - Ganzúa (Horizontal)")]
    [Tooltip("Contenedor raíz de la barra de ganzuado")]
    [FormerlySerializedAs("progressContainer")]
    [SerializeField] private GameObject lockpickProgressContainer;

    [Tooltip("Imagen de relleno horizontal (ej. barra o sprite de GanzuaBarra)")]
    [FormerlySerializedAs("progressBarFill")]
    [SerializeField] private Image lockpickProgressBarFill;

    // Campos de compatibilidad serializada con versiones previas
    [HideInInspector]
    [SerializeField] private GameObject progressContainer;
    [HideInInspector]
    [SerializeField] private Image progressBarFill;

    [Header("Barra de Progreso - Decodificador (Vertical)")]
    [Tooltip("Contenedor raíz de la barra de la pantalla del decodificador")]
    [SerializeField] private GameObject decoderProgressContainer;

    [Tooltip("Imagen de relleno vertical que llena la pantalla del decodificador")]
    [SerializeField] private Image decoderProgressBarFill;

    [Tooltip("Texto opcional de estado en pantalla del decodificador (ej. DECODIFICANDO...)")]
    [SerializeField] private TMP_Text decoderStatusText;

    [Header("Referencia al Jugador")]
    [SerializeField] private PlayerInteraction playerInteraction;

    private DoorInteractable activeDoor;
    private SecurityPanelInteractable activeSecurityPanel;
    private float warningTimer = 0f;

    // Propiedades públicas para compatibilidad o acceso externo
    public GameObject LockpickProgressContainer => lockpickProgressContainer != null ? lockpickProgressContainer : progressContainer;
    public Image LockpickProgressBarFill => lockpickProgressBarFill != null ? lockpickProgressBarFill : progressBarFill;
    public GameObject DecoderProgressContainer => decoderProgressContainer;
    public Image DecoderProgressBarFill => decoderProgressBarFill;

    private void Awake()
    {
        if (playerInteraction == null)
        {
            playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        }

        if (promptContainer == null)
        {
            promptContainer = gameObject;
        }

        ResolveProgressBars();
        ResetAllProgressBars();
    }

    private void Start()
    {
        ResetAllProgressBars();

        if (playerInteraction != null)
        {
            playerInteraction.OnInteractableChanged += HandleInteractableChanged;
            HandleInteractableChanged(playerInteraction.currentInteractable);
        }
        else
        {
            HideAll();
        }
    }

    private void OnDestroy()
    {
        if (playerInteraction != null)
        {
            playerInteraction.OnInteractableChanged -= HandleInteractableChanged;
        }
    }

    private void ResolveProgressBars()
    {
        // 1. Resolver y configurar barra de ganzúa (horizontal)
        if (lockpickProgressContainer == null)
        {
            lockpickProgressContainer = progressContainer;
        }
        if (lockpickProgressBarFill == null)
        {
            lockpickProgressBarFill = progressBarFill;
        }

        if (lockpickProgressContainer == null)
        {
            Transform bg = transform.Find("Progress_Bar_Bg");
            if (bg != null) lockpickProgressContainer = bg.gameObject;
        }

        if (lockpickProgressBarFill == null && lockpickProgressContainer != null)
        {
            lockpickProgressBarFill = lockpickProgressContainer.GetComponentInChildren<Image>();
        }

        if (lockpickProgressBarFill != null)
        {
            lockpickProgressBarFill.type = Image.Type.Filled;
            lockpickProgressBarFill.fillMethod = Image.FillMethod.Horizontal;
            lockpickProgressBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        // 2. Resolver y configurar barra del decodificador (vertical)
        if (decoderProgressContainer == null)
        {
            Transform decTr = transform.Find("Decoder_Progress_Container");
            if (decTr == null) decTr = transform.Find("Decoder_Progress_Bar");
            if (decTr == null && promptContainer != null)
            {
                decTr = promptContainer.transform.Find("Decoder_Progress_Container");
                if (decTr == null) decTr = promptContainer.transform.Find("Decoder_Progress_Bar");
            }

            if (decTr != null)
            {
                decoderProgressContainer = decTr.gameObject;
                if (decoderProgressBarFill == null)
                {
                    decoderProgressBarFill = decoderProgressContainer.GetComponentInChildren<Image>();
                }
            }
        }

        // Si no existe ningún objeto para el decodificador, generar la pantalla vertical automáticamente
        if (decoderProgressContainer == null)
        {
            CreateDefaultDecoderBar();
        }

        if (decoderProgressBarFill != null)
        {
            decoderProgressBarFill.type = Image.Type.Filled;
            decoderProgressBarFill.fillMethod = Image.FillMethod.Vertical;
            decoderProgressBarFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        }
    }

    private void CreateDefaultDecoderBar()
    {
        Transform parentTransform = promptContainer != null ? promptContainer.transform : transform;

        GameObject containerObj = new GameObject("Decoder_Progress_Container", typeof(RectTransform));
        containerObj.transform.SetParent(parentTransform, false);

        RectTransform containerRect = containerObj.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0f, 75f);
        containerRect.sizeDelta = new Vector2(65f, 100f);

        // Fondo de pantalla del decodificador (estilo terminal oscura con borde)
        Image bgImg = containerObj.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.08f, 0.12f, 0.95f);
        Image sourceFill = LockpickProgressBarFill;
        if (sourceFill != null && sourceFill.sprite != null)
        {
            bgImg.sprite = sourceFill.sprite;
        }

        Outline outline = containerObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.8f, 0.44f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Barra de relleno vertical dentro de la pantalla
        GameObject fillObj = new GameObject("Decoder_Progress_Fill", typeof(RectTransform));
        fillObj.transform.SetParent(containerObj.transform, false);

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.18f, 0.92f, 0.48f, 0.95f);
        if (sourceFill != null && sourceFill.sprite != null)
        {
            fillImg.sprite = sourceFill.sprite;
        }
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Vertical;
        fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImg.fillAmount = 0f;

        // Texto de estado en la parte superior de la pantalla del decodificador
        GameObject textObj = new GameObject("Decoder_Status_Text", typeof(RectTransform));
        textObj.transform.SetParent(containerObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -4f);
        textRect.sizeDelta = new Vector2(0f, 18f);

        TextMeshProUGUI statusTMP = textObj.AddComponent<TextMeshProUGUI>();
        statusTMP.text = "DECODING";
        statusTMP.fontSize = 9f;
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.color = new Color(0.9f, 1f, 0.9f, 0.95f);
        if (promptText != null && promptText.font != null)
        {
            statusTMP.font = promptText.font;
        }

        decoderProgressContainer = containerObj;
        decoderProgressBarFill = fillImg;
        decoderStatusText = statusTMP;

        containerObj.SetActive(false);
    }

    private void Update()
    {
        // Temporizador de aviso/advertencia temporal
        if (warningTimer > 0f)
        {
            warningTimer -= Time.deltaTime;
            if (warningTimer <= 0f)
            {
                if (playerInteraction != null && playerInteraction.currentInteractable != null)
                {
                    HandleInteractableChanged(playerInteraction.currentInteractable);
                }
                else
                {
                    HideAll();
                }
            }
        }

        // Si hay una puerta enfocada y el jugador está manteniendo E para ganzuear
        if (activeDoor != null && activeDoor.IsPicking())
        {
            ShowLockpickProgress(activeDoor.GetPickProgressNormalized());
        }
        // Si hay un panel de seguridad enfocado y el jugador está manteniendo E para hackear con el decodificador
        else if (activeSecurityPanel != null && activeSecurityPanel.IsHacking())
        {
            ShowDecoderProgress(activeSecurityPanel.GetHackProgressNormalized());
        }
        else
        {
            ResetAllProgressBars();
        }

        if (warningTimer <= 0f && playerInteraction != null &&
            playerInteraction.currentInteractable is HideSpot spot)
        {
            string prompt = spot.canInteract
                ? "[E] " + spot.interactionPrompt
                : spot.interactionPrompt;
            if (promptText != null && promptText.text != prompt)
            {
                ShowPrompt(prompt);
            }
        }
    }

    private void HandleInteractableChanged(Interactable interactable)
    {
        activeDoor = interactable as DoorInteractable;
        activeSecurityPanel = interactable as SecurityPanelInteractable;
        ResetAllProgressBars();

        if (interactable != null && (interactable.canInteract || interactable is HideSpot))
        {
            string prompt = interactable.interactionPrompt;
            if (interactable.canInteract && !prompt.StartsWith("["))
            {
                prompt = "[E] " + prompt;
            }
            ShowPrompt(prompt);
        }
        else
        {
            HideAll();
        }
    }

    public void ShowTemporaryWarning(string warningMessage, float duration = 2.0f)
    {
        ShowPrompt(warningMessage);
        warningTimer = duration;
    }

    public void ShowPrompt(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
        }

        if (promptContainer != null)
        {
            promptContainer.SetActive(true);
        }
    }

    public void ShowLockpickProgress(float progressNormalized)
    {
        HideDecoderProgress();

        GameObject container = LockpickProgressContainer;
        if (container != null && !container.activeSelf)
        {
            container.SetActive(true);
        }

        Image fill = LockpickProgressBarFill;
        if (fill != null)
        {
            fill.fillAmount = Mathf.Clamp01(progressNormalized);
        }
    }

    public void ShowDecoderProgress(float progressNormalized)
    {
        HideLockpickProgress();

        if (decoderProgressContainer != null && !decoderProgressContainer.activeSelf)
        {
            decoderProgressContainer.SetActive(true);
        }

        if (decoderProgressBarFill != null)
        {
            decoderProgressBarFill.fillAmount = Mathf.Clamp01(progressNormalized);
        }

        if (decoderStatusText != null)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(progressNormalized) * 100f);
            decoderStatusText.text = $"DECODING... {percent}%";
        }
    }

    public void ShowProgress(float progressNormalized)
    {
        if (activeSecurityPanel != null && activeSecurityPanel.IsHacking())
        {
            ShowDecoderProgress(progressNormalized);
        }
        else
        {
            ShowLockpickProgress(progressNormalized);
        }
    }

    public void ResetAllProgressBars()
    {
        HideLockpickProgress();
        HideDecoderProgress();
    }

    private void HideLockpickProgress()
    {
        Image fill = LockpickProgressBarFill;
        if (fill != null)
        {
            fill.fillAmount = 0f;
        }

        GameObject container = LockpickProgressContainer;
        if (container != null && container.activeSelf)
        {
            container.SetActive(false);
        }
    }

    private void HideDecoderProgress()
    {
        if (decoderProgressBarFill != null)
        {
            decoderProgressBarFill.fillAmount = 0f;
        }

        if (decoderProgressContainer != null && decoderProgressContainer.activeSelf)
        {
            decoderProgressContainer.SetActive(false);
        }
    }

    public void ResetProgressBar()
    {
        ResetAllProgressBars();
    }

    public void HideAll()
    {
        if (promptContainer != null)
        {
            promptContainer.SetActive(false);
        }

        ResetAllProgressBars();
    }
}
