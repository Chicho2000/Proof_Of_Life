using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IconUI : MonoBehaviour
{
    [Header("Elementos del Icono por Defecto")]
    [Tooltip("Contenedor principal del icono por defecto (si está vacío, usa este GameObject)")]
    [SerializeField] private GameObject iconContainer;

    [Tooltip("Componente Image donde se muestra el icono del jugador")]
    [SerializeField] private Image defaultIconImage;

    [Tooltip("Sprite del icono por defecto del jugador (opcional si la Image ya tiene uno asignado)")]
    [SerializeField] private Sprite defaultIconSprite;

    [SerializeField] private float fadeDuration = 0.4f;


    private float fadeTimer = 0f;
    private bool isFading = false;
    private PlayerDisguiseSystem.DisguiseType pendingDisguise;
    private bool isInitialized = false;

    private void Awake()
    {
        if (iconContainer == null) iconContainer = gameObject;
        if (defaultIconImage == null) defaultIconImage = iconContainer.GetComponentInChildren<Image>();

        if (defaultIconSprite != null && defaultIconImage != null)
        {
            defaultIconImage.sprite = defaultIconSprite;
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
            UpdateIconVisibility(playerDisguise.CurrentDisguise);
        }
        else
        {
            // Visible por defecto si no hay sistema de disfraz
            ShowDefaultIcon();
        }

        isInitialized = true;
    }

    private void HandleDisguiseChanged(PlayerDisguiseSystem.DisguiseType disguise)
    {
        UpdateIconVisibility(disguise);
    }

    private void UpdateIconVisibility(PlayerDisguiseSystem.DisguiseType disguise)
    {
        // El icono por defecto es visible en todo momento EXCEPTO cuando hay un disfraz equipado
        if (disguise == PlayerDisguiseSystem.DisguiseType.None)
        {
            ShowDefaultIcon();
        }
        else
        {
            HideDefaultIcon();
        }
    }

    public void ShowDefaultIcon()
    {
        if (defaultIconImage != null)
        {
            if (defaultIconSprite != null)
            {
                defaultIconImage.sprite = defaultIconSprite;
            }
            defaultIconImage.enabled = true;
        }

        if (iconContainer != null && !iconContainer.activeSelf)
        {
            iconContainer.SetActive(true);
        }

        Debug.Log("[IconUI] Icono por defecto del jugador activado.");
    }

    public void HideDefaultIcon()
    {

        if (defaultIconImage != null)
        {
            defaultIconImage.enabled = false;
        }

        Debug.Log("[IconUI] Icono por defecto ocultado (reemplazado por el disfraz).");
    }
}