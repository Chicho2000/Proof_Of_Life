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

    [Header("Dialogo")]
    [TextArea(2, 4)] public string frasePredeterminada;
    [Min(0f)] public float duracionPredeterminada = 3f;
    public bool hablarAlIniciar;
    private Coroutine hideRoutine;

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

        if (canvasDialogo != null) canvasDialogo.SetActive(false);
        if (hablarAlIniciar) MostrarFrasePredeterminada();
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

    public void MostrarFrasePredeterminada()
    {
        MostrarFrase(frasePredeterminada, duracionPredeterminada);
    }

    public void MostrarFrase(string frase, float duracion = 3f)
    {
        if (canvasDialogo == null || string.IsNullOrWhiteSpace(frase)) return;
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = null;
        canvasDialogo.SetActive(true);
        ActualizarCajaDialogo(frase);
        if (duracion > 0f) hideRoutine = StartCoroutine(OcultarDespuesDe(duracion));
    }

    public void OcultarFrase()
    {
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = null;
        if (canvasDialogo != null) canvasDialogo.SetActive(false);
    }

    private IEnumerator OcultarDespuesDe(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        hideRoutine = null;
        if (canvasDialogo != null) canvasDialogo.SetActive(false);
    }
}
