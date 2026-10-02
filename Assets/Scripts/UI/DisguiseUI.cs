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

    [Header("Elementos de UI")]
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

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (disguiseContainer == null)
        {
            disguiseContainer = gameObject;
        }

        if (disguiseIcon == null)
        {
            disguiseIcon = disguiseContainer.GetComponentInChildren<Image>();
        }

        if (disguiseText == null)
        {
            disguiseText = disguiseContainer.GetComponentInChildren<TMP_Text>();
        }

        canvasGroup = disguiseContainer.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = disguiseContainer.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
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
            HandleDisguiseChanged(playerDisguise.CurrentDisguise);
        }
        else
        {
            HideDisguiseBadge();
        }
    }

    private void HandleDisguiseChanged(PlayerDisguiseSystem.DisguiseType disguise)
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
                if (disguises[i].disguiseType == disguise)
                {
                    return disguises[i];
                }
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
        if (disguiseText != null)
        {
            disguiseText.text = config.displayName;
        }

        if (disguiseIcon != null)
        {
            if (config.iconSprite != null)
            {
                disguiseIcon.sprite = config.iconSprite;
            }
            disguiseIcon.enabled = true;
        }

        if (disguiseContainer != null && !disguiseContainer.activeSelf)
        {
            disguiseContainer.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        Debug.Log($"[DisguiseUI] Disfraz activado en HUD: {config.displayName}");
    }

    public void HideDisguiseBadge()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }
}
