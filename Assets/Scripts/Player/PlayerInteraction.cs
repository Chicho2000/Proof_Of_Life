using System;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Detección")]
    public float interactionRange = 3f;

    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode secondaryInteractKey = KeyCode.F;

    public Interactable currentInteractable;

    // Evento para que la UI se entere instantáneamente de cambios de foco
    public event Action<Interactable> OnInteractableChanged;

    private Camera playerCamera;

    private void Awake()
    {
        playerCamera = Camera.main;
    }

    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f)
        {
            return;
        }

        DetectInteractable();

        if (currentInteractable != null)
        {
            if (Input.GetKeyDown(interactKey) && currentInteractable.canInteract)
            {
                currentInteractable.Interact(gameObject);
            }
            else if (Input.GetKeyDown(secondaryInteractKey))
            {
                currentInteractable.SecondaryInteract(gameObject);
            }
        }
    }

    public void ClearCurrentInteractable()
    {
        if (currentInteractable != null)
        {
            try
            {
                currentInteractable.OnLoseFocus(gameObject);
            }
            catch {}
        }

        currentInteractable = null;
        OnInteractableChanged?.Invoke(null);
    }

    private void DetectInteractable()
    {
        Interactable detected = null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, ~0, QueryTriggerInteraction.Ignore))
        {
            detected = hit.collider.GetComponentInParent<Interactable>();
        }

        // Si el interactable actual fue destruido o recogido
        if (currentInteractable == null && detected == null)
        {
            if (currentInteractable != null) // Caso referencia muerta de Unity
            {
                currentInteractable = null;
                OnInteractableChanged?.Invoke(null);
            }
            return;
        }

        if (detected != currentInteractable)
        {
            // Si había algo enfocado antes, pierde el foco al cambiar
            if (currentInteractable != null)
            {
                try
                {
                    currentInteractable.OnLoseFocus(gameObject);
                }
                catch {}
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

            OnInteractableChanged?.Invoke(currentInteractable);
        }
        else if (currentInteractable != null && !currentInteractable.canInteract)
        {
            try
            {
                currentInteractable.OnLoseFocus(gameObject);
            }
            catch {}

            currentInteractable = null;
            OnInteractableChanged?.Invoke(null);
        }
    }
}