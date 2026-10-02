using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("Referencias UI - Mensaje")]
    [SerializeField] private GameObject promptContainer;
    [SerializeField] private TMP_Text promptText;

    [Header("Referencias UI - Barra de Progreso (Ganzuado / Acciones)")]
    [SerializeField] private GameObject progressContainer;
    [SerializeField] private Image progressBarFill;

    [Header("Referencia al Jugador")]
    [SerializeField] private PlayerInteraction playerInteraction;

    private DoorInteractable activeDoor;
    private SecurityPanelInteractable activeSecurityPanel;
    private float warningTimer = 0f;

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

        ResetProgressBar();
    }

    private void Start()
    {
        ResetProgressBar();

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
            ShowProgress(activeDoor.GetPickProgressNormalized());
        }
        else if (activeSecurityPanel != null && activeSecurityPanel.IsHacking())
        {
            ShowProgress(activeSecurityPanel.GetHackProgressNormalized());
        }
        else
        {
            if (progressContainer != null && progressContainer.activeSelf)
            {
                ResetProgressBar();
            }
        }
    }

    private void HandleInteractableChanged(Interactable interactable)
    {
        activeDoor = interactable as DoorInteractable;
        activeSecurityPanel = interactable as SecurityPanelInteractable;
        ResetProgressBar();

        if (interactable != null && interactable.canInteract)
        {
            string prompt = interactable.interactionPrompt;
            if (!prompt.StartsWith("["))
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

    public void ShowProgress(float progressNormalized)
    {
        if (progressContainer != null)
        {
            progressContainer.SetActive(true);
        }

        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = Mathf.Clamp01(progressNormalized);
        }
    }

    private void ResetProgressBar()
    {
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = 0f;
        }

        if (progressContainer != null)
        {
            progressContainer.SetActive(false);
        }
    }

    public void HideAll()
    {
        if (promptContainer != null)
        {
            promptContainer.SetActive(false);
        }

        ResetProgressBar();
    }
}
