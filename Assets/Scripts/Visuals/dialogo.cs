using UnityEngine;
using TMPro;
using System.Collections;

public class DialogoNPC : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject canvasDialogo;
    public TMP_Text textoNombre;   
    public TMP_Text textoDialogo;  
    public RectTransform barraLateral; 

    [Header("Posicionamiento")]
    public Transform headReference; 
    public float alturaExtra = 0.5f;

    [Header("Datos del NPC")]
    public string nombreNPC = "Guardia Elite";

    private Camera camaraPrincipal;

    [Header("Textos de Prueba")]
    private string[] frasesPrueba = {
        "Sí.",
        "¿Qué fue ese ruido?",
        "Seguro fue el viento...",
        "¡ALERTA! ¡INTRUSO EN EL CALLEJÓN! PIDIENDO REFUERZOS INMEDIATOS.",
        "Hmm..."
    };

    void Start()
    {
        camaraPrincipal = Camera.main;

        if (textoNombre != null)
        {
            textoNombre.text = nombreNPC;
        }

        StartCoroutine(RutinaDePrueba());
    }

    void LateUpdate()
    {
        if (canvasDialogo != null && camaraPrincipal != null && headReference != null)
        {
            canvasDialogo.transform.position = headReference.position + Vector3.up * alturaExtra;
            canvasDialogo.transform.LookAt(canvasDialogo.transform.position + camaraPrincipal.transform.rotation * Vector3.forward,
                camaraPrincipal.transform.rotation * Vector3.up);
        }
    }

    IEnumerator RutinaDePrueba()
    {
        while (true)
        {
            canvasDialogo.SetActive(true);
            textoDialogo.text = frasesPrueba[Random.Range(0, frasesPrueba.Length)];

            Canvas.ForceUpdateCanvases();

            if (barraLateral != null && textoDialogo != null)
            {
                textoDialogo.ForceMeshUpdate();

               
                int lineas = textoDialogo.textInfo.lineCount;
                float alturaTotal = 5f + ((lineas - 1) * 3f);
                barraLateral.sizeDelta = new Vector2(barraLateral.sizeDelta.x, alturaTotal);
            }

            yield return new WaitForSeconds(3f);

            canvasDialogo.SetActive(false);
            yield return new WaitForSeconds(1f);
        }
    }
}