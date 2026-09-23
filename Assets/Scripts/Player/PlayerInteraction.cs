using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Detección")]
    public float interactionRange = 3f;

    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;

    public Interactable currentInteractable;

    private Camera playerCamera;

    private void Awake()
    {
        playerCamera = Camera.main;
    }

    private void Update()
    {
        DetectInteractable();

        if (currentInteractable != null && Input.GetKeyDown(interactKey))
        {
            currentInteractable.Interact(gameObject);
        }
    }

    private void DetectInteractable()
    {
        Interactable detected = null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, ~0, QueryTriggerInteraction.Ignore))
        {
            detected = hit.collider.GetComponentInParent<Interactable>();
        }

        if (detected != currentInteractable)
        {
            // Si había algo enfocado antes, pierde el foco al cambiar,
   
            if (currentInteractable != null)
            {
                currentInteractable.OnLoseFocus(gameObject);
            }

            if (detected != null && detected.canInteract)
            {
                currentInteractable = detected;
                currentInteractable.OnFocus(gameObject);
            }
            else
            {
                currentInteractable = null;
            }
        }
        else if (currentInteractable != null && !currentInteractable.canInteract)
        {
            // Seguís apuntando al mismo objeto, pero cambió de estado mientras lo mirabas.
            currentInteractable.OnLoseFocus(gameObject);
            currentInteractable = null;
        }
    }
}