using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject promptContainer;
    [SerializeField] private Text promptText;

    [Header("Referencia al Jugador")]
    [SerializeField] private PlayerInteraction playerInteraction;

    private void Start()
    {
        if (playerInteraction == null)
        {
            playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        }

        if (playerInteraction != null)
        {
            playerInteraction.OnInteractableChanged += HandleInteractableChanged;
            // Estado inicial
            HandleInteractableChanged(playerInteraction.currentInteractable);
        }
        else
        {
            HidePrompt();
        }
    }

    private void OnDestroy()
    {
        if (playerInteraction != null)
        {
            playerInteraction.OnInteractableChanged -= HandleInteractableChanged;
        }
    }

    private void HandleInteractableChanged(Interactable interactable)
    {
        if (interactable != null && interactable.canInteract)
        {
            ShowPrompt($"[E] {interactable.interactionPrompt}");
        }
        else
        {
            HidePrompt();
        }
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
        else if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (promptContainer != null)
        {
            promptContainer.SetActive(false);
        }
        else if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }
}
