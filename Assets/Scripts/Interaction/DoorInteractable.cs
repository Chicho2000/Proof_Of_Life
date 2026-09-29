using UnityEngine;
using UnityEngine.AI;

public class DoorInteractable : Interactable
{
    [Header("Estado de la puerta")]
    public bool isLocked = true;
    public bool isOpen = false;

    [Header("Lockpick")]
    public float pickTime = 3f;

    [Header("Rotación")]
    [SerializeField] private float openAngle = 90f;

    private float pickProgress = 0f;
    private bool isPicking = false;
    private PlayerInteraction playerInteraction;
    private PlayerHotbar playerHotbar;
    private NavMeshObstacle navObstacle;

    private void Start()
    {
        navObstacle = GetComponentInChildren<NavMeshObstacle>();
        UpdatePrompt();
        UpdateNavMesh();
    }

    public override void Interact(GameObject interactor)
    {
        if (!isLocked)
        {
            ToggleOpen();
            return;
        }

        PlayerHotbar hotbar = interactor.GetComponent<PlayerHotbar>();
        if (!HasLockpickEquipped(hotbar))
        {
            InteractionPromptUI promptUI = FindFirstObjectByType<InteractionPromptUI>();
            if (promptUI != null)
            {
                promptUI.ShowTemporaryWarning("<color=#EF4444>[ ! ] SE REQUIERE GANZÚA</color>", 2.0f);
            }
            Debug.Log("Necesitás tener la ganzúa equipada para abrir esta puerta");
            return;
        }

        if (!isPicking)
        {
            playerInteraction = interactor.GetComponent<PlayerInteraction>();
            playerHotbar = hotbar;
            isPicking = true;
            pickProgress = 0f;
            Debug.Log("Empezando a ganzuear...");
        }
    }

    private bool HasLockpickEquipped(PlayerHotbar hotbar)
    {
        if (hotbar == null)
        {
            return false;
        }

        ItemData selectedItem = hotbar.GetSelectedItem();
        return selectedItem != null && selectedItem.ItemType == ItemType.Lockpick;
    }

    private void Update()
    {
        if (!isPicking)
        {
            return;
        }

        bool stillFocused = playerInteraction != null && playerInteraction.currentInteractable == this;
        bool keyHeld = playerInteraction != null && Input.GetKey(playerInteraction.interactKey);
        bool lockpickStillEquipped = HasLockpickEquipped(playerHotbar);

        if (!stillFocused || !keyHeld || !lockpickStillEquipped)
        {
            CancelPicking();
            return;
        }

        pickProgress += Time.deltaTime;

        if (pickProgress >= pickTime)
        {
            if (!HasLockpickEquipped(playerHotbar))
            {
                CancelPicking();
                return;
            }

            FinishPicking();
        }
    }

    public bool IsPicking()
    {
        return isPicking;
    }

    public float GetPickProgressNormalized()
    {
        if (pickTime <= 0f)
        {
            return 0f;
        }
        return Mathf.Clamp01(pickProgress / pickTime);
    }

    public override void OnFocus(GameObject interactor)
    {
        base.OnFocus(interactor);
        UpdatePromptWithInteractor(interactor);
    }

    public void UpdatePromptWithInteractor(GameObject interactor)
    {
        if (isLocked)
        {
            PlayerHotbar hotbar = null;
            if (interactor != null)
            {
                hotbar = interactor.GetComponent<PlayerHotbar>();
            }

            if (HasLockpickEquipped(hotbar))
            {
                interactionPrompt = "Forzar cerradura";
            }
            else
            {
                interactionPrompt = "Abrir puerta";
            }
        }
        else
        {
            interactionPrompt = isOpen ? "Cerrar puerta" : "Abrir puerta";
        }
    }

    private void UpdatePrompt()
    {
        if (isLocked)
        {
            interactionPrompt = "Abrir puerta";
        }
        else
        {
            interactionPrompt = isOpen ? "Cerrar puerta" : "Abrir puerta";
        }
    }

    private void FinishPicking()
    {
        isLocked = false;
        isPicking = false;
        pickProgress = 0f;
        playerInteraction = null;
        playerHotbar = null;
        Debug.Log("Puerta ganzuada con éxito");
        ToggleOpen();
    }

    private void CancelPicking()
    {
        isPicking = false;
        pickProgress = 0f;
        playerInteraction = null;
        playerHotbar = null;
        Debug.Log("Ganzúa cancelada");
    }

    private void ToggleOpen()
    {
        isOpen = !isOpen;
        UpdatePrompt();
        UpdateNavMesh();

        transform.Rotate(0f, isOpen ? openAngle : -openAngle, 0f);

        Debug.Log(isOpen ? "Puerta abierta" : "Puerta cerrada");
    }

    private void UpdateNavMesh()
    {
        if (navObstacle != null)
        {
            navObstacle.enabled = !isOpen;
        }
    }

    public override void OnLoseFocus(GameObject interactor)
    {
        if (isPicking)
        {
            CancelPicking();
        }
    }
}
