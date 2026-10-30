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
    public RectTransform fondoOscuro;

    [Header("Posicionamiento y Escala")]
    public Transform headReference;
    [HideInInspector] public float alturaExtra = 0.5f;
    [Tooltip("Distancia desde el headReference hasta el borde INFERIOR del cartel")]
    public float distanciaSobreCabeza = 0.22f;
    [Tooltip("Escala general del Canvas en el mundo")]
    public float escalaCanvas = 0.065f;

    [Header("Márgenes del Cuadro (en unidades UI)")]
    public float margenAncho = 55f;
    public float paddingIzquierdo = 12f;
    public float paddingSuperior = 16f;
    public float paddingInferior = 20f;

    [Header("Datos del NPC")]
    public string nombreNPC = "Guardia Elite";

    private Camera camaraPrincipal;
    private float offsetInferiorLocalY = 0f;

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

        if (canvasDialogo != null && escalaCanvas > 0.001f)
        {
            canvasDialogo.transform.localScale = Vector3.one * escalaCanvas;
        }

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
            if (escalaCanvas > 0.001f)
            {
                canvasDialogo.transform.localScale = Vector3.one * escalaCanvas;
            }

            float escalaActualY = canvasDialogo.transform.localScale.y;
            float compensacionCaja = -offsetInferiorLocalY * escalaActualY;

            canvasDialogo.transform.position = headReference.position + Vector3.up * (distanciaSobreCabeza + compensacionCaja);
            canvasDialogo.transform.LookAt(
                canvasDialogo.transform.position + camaraPrincipal.transform.rotation * Vector3.forward,
                camaraPrincipal.transform.rotation * Vector3.up
            );
        }
    }

    public void ActualizarCajaDialogo(string nuevoTexto)
    {
        if (textoDialogo == null) return;

        textoDialogo.text = nuevoTexto;
        Canvas.ForceUpdateCanvases();
        textoDialogo.ForceMeshUpdate();

        Vector2 tamañoRealTexto = textoDialogo.GetRenderedValues(false);
        Vector2 tamañoRealNombre = Vector2.zero;

        if (textoNombre != null)
        {
            textoNombre.ForceMeshUpdate();
            tamañoRealNombre = textoNombre.GetRenderedValues(false);
        }

        if (barraLateral != null && fondoOscuro != null)
        {
            barraLateral.pivot = new Vector2(0f, 1f);
            fondoOscuro.pivot = new Vector2(0f, 1f);

            float escalaX = barraLateral.localScale.x > 0.0001f ? barraLateral.localScale.x : 0.04f;
            float escalaY = barraLateral.localScale.y > 0.0001f ? barraLateral.localScale.y : 0.04f;

            RectTransform rectDialogo = textoDialogo.rectTransform;
            float topeDialogoLocalY = rectDialogo.anchoredPosition.y
                + (1f - rectDialogo.pivot.y) * rectDialogo.sizeDelta.y * rectDialogo.localScale.y;

            float alturaEncabezadoNombre = Mathf.Max(0f, (barraLateral.anchoredPosition.y - topeDialogoLocalY) / escalaY);

            float altoTotalContenido = alturaEncabezadoNombre + tamañoRealTexto.y;
            barraLateral.sizeDelta = new Vector2(barraLateral.sizeDelta.x, altoTotalContenido);

            fondoOscuro.anchoredPosition = new Vector2(
                barraLateral.anchoredPosition.x - paddingIzquierdo * escalaX,
                barraLateral.anchoredPosition.y + paddingSuperior * escalaY
            );

            float anchoMaximoTexto = Mathf.Max(tamañoRealTexto.x, tamañoRealNombre.x);
            float anchoFondo = anchoMaximoTexto + margenAncho;
            float altoFondo = altoTotalContenido + paddingSuperior + paddingInferior;

            fondoOscuro.sizeDelta = new Vector2(anchoFondo, altoFondo);

            offsetInferiorLocalY = fondoOscuro.anchoredPosition.y - (altoFondo * escalaY);
        }
    }

    IEnumerator RutinaDePrueba()
    {
        while (true)
        {
            canvasDialogo.SetActive(true);
            string frase = frasesPrueba[Random.Range(0, frasesPrueba.Length)];
            ActualizarCajaDialogo(frase);

            yield return new WaitForSeconds(3f);

            canvasDialogo.SetActive(false);
            yield return new WaitForSeconds(1f);
        }
    }
}
