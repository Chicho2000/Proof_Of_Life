using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisguiseUI : MonoBehaviour
{
    [System.Serializable]
    public struct DisguiseVisualConfig
    {
        public PlayerDisguiseSystem.DisguiseType disguiseType;
        public string displayName;
        public Sprite iconSprite;
    }

    [Header("Elementos del Badge")]
    [SerializeField] private GameObject disguiseContainer;
    [SerializeField] private TMP_Text disguiseText;
    [SerializeField] private Image disguiseIcon;

    [Header("Configuración de Disfraces")]
    [SerializeField] private DisguiseVisualConfig[] disguises = new DisguiseVisualConfig[]
    {
        new DisguiseVisualConfig
        {
            disguiseType = PlayerDisguiseSystem.DisguiseType.GuardDisguise,
            displayName = "[ DISFRAZ: GUARDIA ]",
            iconSprite = null
        }
    };

    [Header("Transición de Pantalla (Opcional)")]
    [Tooltip("Arrastra aquí un CanvasGroup que cubra la pantalla para oscurecerla al cambiarte de ropa.")]
    [SerializeField] private CanvasGroup screenFade;
    [SerializeField] private float fadeDuration = 0.4f;

    private CanvasGroup badgeCanvasGroup;
    private float fadeTimer = 0f;
    private bool isFading = false;
    private PlayerDisguiseSystem.DisguiseType pendingDisguise;
    private bool isInitialized = false;

    private void Awake()
    {
        if (disguiseContainer == null) disguiseContainer = gameObject;
        if (disguiseIcon == null) disguiseIcon = disguiseContainer.GetComponentInChildren<Image>();
        if (disguiseText == null) disguiseText = disguiseContainer.GetComponentInChildren<TMP_Text>();

        badgeCanvasGroup = disguiseContainer.GetComponent<CanvasGroup>();
        if (badgeCanvasGroup == null) badgeCanvasGroup = disguiseContainer.AddComponent<CanvasGroup>();
        badgeCanvasGroup.alpha = 0f;

        if (screenFade != null)
        {
            screenFade.alpha = 0f;
            screenFade.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        PlayerDisguiseSystem.OnDisguiseChangedStatic += HandleDisguiseChanged;
    }

    private void OnDisable()
    {
        PlayerDisguiseSystem.OnDisguiseChangedStatic -= HandleDisguiseChanged;
    }

    private void Start()
    {
        PlayerDisguiseSystem playerDisguise = FindFirstObjectByType<PlayerDisguiseSystem>();
        if (playerDisguise != null)
        {
            ApplyBadge(playerDisguise.CurrentDisguise);
        }
        else
        {
            HideDisguiseBadge();
        }
        isInitialized = true;
    }

    private void Update()
    {
        if (!isFading || screenFade == null) return;

        fadeTimer += Time.deltaTime;
        float half = fadeDuration * 0.5f;

        if (fadeTimer <= half)
        {
            screenFade.alpha = fadeTimer / half;
        }
        else
        {
            if (pendingDisguise != PlayerDisguiseSystem.DisguiseType.None || badgeCanvasGroup.alpha > 0f)
            {
                ApplyBadge(pendingDisguise);
            }
            screenFade.alpha = 1f - ((fadeTimer - half) / half);
        }

        if (fadeTimer >= fadeDuration)
        {
            screenFade.alpha = 0f;
            isFading = false;
        }
    }

    private void HandleDisguiseChanged(PlayerDisguiseSystem.DisguiseType disguise)
    {
        if (!isInitialized || screenFade == null)
        {
            ApplyBadge(disguise);
            return;
        }

        pendingDisguise = disguise;
        fadeTimer = 0f;
        isFading = true;
    }

    private void ApplyBadge(PlayerDisguiseSystem.DisguiseType disguise)
    {
        if (disguise == PlayerDisguiseSystem.DisguiseType.None)
        {
            HideDisguiseBadge();
            return;
        }

        DisguiseVisualConfig config = GetConfigForDisguise(disguise);
        ShowDisguiseBadge(config);
    }

    private DisguiseVisualConfig GetConfigForDisguise(PlayerDisguiseSystem.DisguiseType disguise)
    {
        if (disguises != null)
        {
            for (int i = 0; i < disguises.Length; i++)
            {
                if (disguises[i].disguiseType == disguise) return disguises[i];
            }
        }

        DisguiseVisualConfig fallback = new DisguiseVisualConfig();
        fallback.disguiseType = disguise;
        fallback.displayName = "[ DISFRAZ: " + disguise.ToString().ToUpper() + " ]";
        fallback.iconSprite = null;
        return fallback;
    }

    public void ShowDisguiseBadge(DisguiseVisualConfig config)
    {
        if (disguiseText != null) disguiseText.text = config.displayName;
        if (disguiseIcon != null)
        {
            if (config.iconSprite != null) disguiseIcon.sprite = config.iconSprite;
            disguiseIcon.enabled = true;
        }
        if (disguiseContainer != null && !disguiseContainer.activeSelf) disguiseContainer.SetActive(true);
        if (badgeCanvasGroup != null) badgeCanvasGroup.alpha = 1f;

        Debug.Log("[DisguiseUI] Disfraz activado en HUD: " + config.displayName);
    }

    public void HideDisguiseBadge()
    {
        if (badgeCanvasGroup != null) badgeCanvasGroup.alpha = 0f;
    }
}
