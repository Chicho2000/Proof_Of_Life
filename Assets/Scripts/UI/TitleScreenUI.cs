using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla la pantalla de título del juego.
/// Adjunta este script al GameObject "TitleScreen" dentro de Level 1.
/// Al hacer clic en Iniciar, oculta el título y abre el briefing de misión
/// a través de MissionUI (que ya contiene el flujo completo de selección).
/// Si los campos de los botones están vacíos en el Inspector, el script
/// los busca automáticamente por nombre entre los hijos del objeto.
/// </summary>
public class TitleScreenUI : MonoBehaviour
{
    // ── Referencias ──────────────────────────────────────────────────────────

    [Header("Panel principal")]
    [Tooltip("El GameObject raíz de la pantalla de título. Si está vacío se usa este mismo objeto.")]
    [SerializeField] private GameObject titleScreenPanel;

    [Header("Botones")]
    [Tooltip("Botón para iniciar el juego (hijo 'BotonIniciar').")]
    [SerializeField] private Button botonIniciar;

    [Tooltip("Botón para salir (hijo 'BotonSalir').")]
    [SerializeField] private Button botonSalir;


    private MissionUI missionUI;

    private void Start()
    {
        if (titleScreenPanel == null)
            titleScreenPanel = gameObject;

        titleScreenPanel.SetActive(true);

        // Buscar MissionUI en la escena y decirle que espere a que el jugador
        // haga clic en Iniciar antes de mostrar el briefing por su cuenta.
        missionUI = FindFirstObjectByType<MissionUI>();
        if (missionUI != null)
            missionUI.SetShowBriefingOnStart(false);
        else
            Debug.LogWarning("[TitleScreenUI] No se encontró MissionUI en la escena.");

        // Resolver botones por nombre si no se asignaron en el Inspector
        ResolveButton(ref botonIniciar, "BotonIniciar");
        ResolveButton(ref botonSalir,   "BotonSalir");

        // Conectar listeners
        if (botonIniciar != null) botonIniciar.onClick.AddListener(OnClickIniciar);
        else Debug.LogWarning("[TitleScreenUI] No se encontró el botón 'BotonIniciar'.");

        if (botonSalir != null) botonSalir.onClick.AddListener(OnClickSalir);
        else Debug.LogWarning("[TitleScreenUI] No se encontró el botón 'BotonSalir'.");

        // Cursor libre mientras está el título
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // ── Handlers de botones ───────────────────────────────────────────────────

    /// <summary>
    /// Oculta el título y abre el briefing de misión (que ya incluye
    /// selección de misión e inicio de partida).
    /// </summary>
    private void OnClickIniciar()
    {
        titleScreenPanel.SetActive(false);

        if (missionUI != null)
            missionUI.ShowBriefingFromTitle();
        else
            Debug.LogWarning("[TitleScreenUI] No se encontró MissionUI — no se puede abrir el briefing.");
    }

    /// <summary>Cierra la aplicación. En el editor detiene el Play Mode.</summary>
    private void OnClickSalir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Utilidades ────────────────────────────────────────────────────────────

    /// <summary>
    /// Si el campo <paramref name="button"/> es null, busca un hijo con el
    /// nombre indicado y extrae su componente Button.
    /// </summary>
    private void ResolveButton(ref Button button, string childName)
    {
        if (button != null) return;

        Transform child = transform.Find(childName);
        if (child == null)
            child = FindChildRecursive(transform, childName);

        if (child != null)
        {
            button = child.GetComponent<Button>();
            if (button == null)
                Debug.LogWarning($"[TitleScreenUI] El hijo '{childName}' no tiene componente Button.");
        }
        else
        {
            Debug.LogWarning($"[TitleScreenUI] No se encontró ningún hijo llamado '{childName}'.");
        }
    }

    /// <summary>Búsqueda recursiva de un Transform por nombre.</summary>
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
