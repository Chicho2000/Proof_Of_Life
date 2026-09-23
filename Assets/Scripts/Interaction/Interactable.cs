using UnityEngine;


public abstract class Interactable : MonoBehaviour
{
    [Header("Interactable Base")]
    public string interactionPrompt = "Interactuar";
    public bool canInteract = true;

    public abstract void Interact(GameObject interactor);

    // Funciones opciones que se sobreescribirian con override en las clases hijas,sirve para poner reacciones
    // dependiendo de si se esta dentro o fuera del area de deteccion del jugador
    public virtual void OnFocus(GameObject interactor) { }
    public virtual void OnLoseFocus(GameObject interactor) { }
}

