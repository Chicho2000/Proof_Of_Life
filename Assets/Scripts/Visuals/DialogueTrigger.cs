using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogoNPC personajeQueHabla;
    [SerializeField, TextArea(2, 4)] private string frase;
    [SerializeField, Min(0f)] private float duracion = 3f;
    [SerializeField] private bool soloUnaVez = true;
    [SerializeField] private bool ocultarAlSalir;
    private bool activado;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null) trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activado && soloUnaVez) return;
        if (other.GetComponentInParent<PlayerHealth>() == null || personajeQueHabla == null) return;

        personajeQueHabla.MostrarFrase(frase, duracion);
        activado = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (ocultarAlSalir && other.GetComponentInParent<PlayerHealth>() != null && personajeQueHabla != null)
        {
            personajeQueHabla.OcultarFrase();
        }
    }
}
