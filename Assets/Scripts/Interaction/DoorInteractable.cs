using UnityEngine;

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

    private void Start()
    {
        UpdatePrompt();
    }

    public override void Interact(GameObject interactor)
    {
        if (!isLocked)
        {
            ToggleOpen();
            return;
        }

        if (!HasLockpickEquipped(interactor))
        {
            Debug.Log("Necesitás tener la ganzúa equipada para abrir esta puerta");
            return;
        }

        if (!isPicking)
        {
            playerInteraction = interactor.GetComponent<PlayerInteraction>();
            isPicking = true;
            pickProgress = 0f;
            Debug.Log("Empezando a ganzuear...");
        }
    }

    private bool HasLockpickEquipped(GameObject interactor)
    {
        PlayerHotbar hotbar = interactor.GetComponent<PlayerHotbar>();
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

        if (!stillFocused || !keyHeld)
        {
            CancelPicking();
            return;
        }

        pickProgress += Time.deltaTime;

        if (pickProgress >= pickTime)
        {
            FinishPicking();
        }
    }

    // Para en el futuro una UI convierte el progreso en una escala de 0 a 1
    public float GetPickProgressNormalized()
    {
        if (pickTime <= 0f)
        {
            return 0f;
        }
        return pickProgress / pickTime;
    }

    private void FinishPicking()
    {
        isLocked = false;
        isPicking = false;
        pickProgress = 0f;
        Debug.Log("Puerta ganzuada con éxito");
        ToggleOpen();
    }

    private void CancelPicking()
    {
        isPicking = false;
        pickProgress = 0f;
        Debug.Log("Ganzúa cancelada");
    }

    private void ToggleOpen()
    {
        isOpen = !isOpen;
        UpdatePrompt();

        // Rotación instantánea simple (placeholder hasta meter animación o Slerp)
        transform.Rotate(0f, isOpen ? openAngle : -openAngle, 0f);

        Debug.Log(isOpen ? "Puerta abierta" : "Puerta cerrada");
    }

    private void UpdatePrompt()
    {
        if (isLocked)
        {
            interactionPrompt = "Ganzuear puerta";
        }
        else
        {
            interactionPrompt = isOpen ? "Cerrar puerta" : "Abrir puerta";
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