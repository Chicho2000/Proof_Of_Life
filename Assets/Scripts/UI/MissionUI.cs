using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz de usuario para el sistema de misiones.
/// Muestra los objetivos actuales en pantalla, notificaciones/advertencias (como el requisito de equipar el USB)
/// y la pantalla final de victoria al completar la extracción.
/// </summary>
public class MissionUI : MonoBehaviour
{
    [Header("Referencias a Paneles")]
    [SerializeField] private GameObject objectivesPanel;
    [SerializeField] private Text objectivesTitleText;
    [SerializeField] private Text objectivesContentText;

    [Header("Posición del Panel de Objetivos")]
    [Tooltip("Coloca el panel de objetivos en la esquina superior derecha")]
    [SerializeField] private bool placeTopRight = true;
    [SerializeField] private Vector2 topRightOffset = new Vector2(-25f, -25f);

    [Header("Banner de Mensajes / Avisos")]
    [SerializeField] private GameObject messageBanner;
    [SerializeField] private Text messageText;
    [SerializeField] private float messageDuration = 3.5f;

    [Header("Pantalla de Fin de Juego (Victoria)")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Text victoryTitleText;
    [SerializeField] private Text victorySubtitleText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    private Coroutine currentMessageCoroutine;

    private void Awake()
    {
        EnsureUIStructure();
    }

    private void Start()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        if (messageBanner != null)
        {
            messageBanner.SetActive(false);
        }

        UpdateObjectivesDisplay();
    }

    private void Update()
    {
        if (victoryPanel != null && victoryPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.QuitGame();
                }
            }
        }
    }

    private void OnEnable()
    {
        MissionManager.OnObjectiveChanged += HandleObjectiveChanged;
        MissionManager.OnMissionCompleted += HandleMissionCompleted;
        MissionManager.OnMissionMessage += ShowMessageBanner;
    }

    private void OnDisable()
    {
        MissionManager.OnObjectiveChanged -= HandleObjectiveChanged;
        MissionManager.OnMissionCompleted -= HandleMissionCompleted;
        MissionManager.OnMissionMessage -= ShowMessageBanner;
    }

    private void HandleObjectiveChanged(MissionObjective objective)
    {
        UpdateObjectivesDisplay();
    }

    private void HandleMissionCompleted()
    {
        ShowVictoryScreen();
    }

    /// <summary>
    /// Actualiza la lista de objetivos visible en el HUD.
    /// </summary>
    public void UpdateObjectivesDisplay()
    {
        if (objectivesContentText == null) return;

        if (MissionManager.Instance == null || MissionManager.Instance.Objectives.Count == 0)
        {
            objectivesContentText.text = "• [ ] Robar el dispositivo USB confidencial\n• [ ] Extraer en el punto de escape (USB equipado)";
            return;
        }

        StringBuilder sb = new StringBuilder();
        var list = MissionManager.Instance.Objectives;

        for (int i = 0; i < list.Count; i++)
        {
            var obj = list[i];
            if (obj == null) continue;

            string checkMark = obj.IsComplete ? "<color=#4CAF50><b>[✓]</b></color>" : "<color=#FFC107>[ ]</color>";
            string statusSuffix = obj.IsComplete ? " <color=#4CAF50>(Completado)</color>" : "";
            string optionalTag = obj.IsOptional ? " <color=#9E9E9E>(Opcional)</color>" : "";

            sb.AppendLine($"{checkMark} {obj.ObjectiveTitle}{statusSuffix}{optionalTag}");
        }

        // Agregar recordatorio de extracción si no está entre los objetivos directos
        sb.AppendLine("<color=#00BCD4>[➤]</color> Extraer en el punto de escape <color=#E0E0E0><i>(Requiere USB equipado)</i></color>");

        objectivesContentText.text = sb.ToString();
    }

    /// <summary>
    /// Muestra un banner flotante con un aviso o advertencia de misión.
    /// </summary>
    public void ShowMessageBanner(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        if (messageBanner != null)
        {
            messageBanner.SetActive(true);
        }

        if (currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
        }

        currentMessageCoroutine = StartCoroutine(HideMessageRoutine());
    }

    private IEnumerator HideMessageRoutine()
    {
        yield return new WaitForSeconds(messageDuration);

        if (messageBanner != null)
        {
            messageBanner.SetActive(false);
        }
        currentMessageCoroutine = null;
    }

    /// <summary>
    /// Muestra la pantalla final de Victoria / Fin del juego.
    /// </summary>
    public void ShowVictoryScreen()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        if (victoryTitleText != null)
        {
            victoryTitleText.text = "¡MISIÓN CUMPLIDA!";
        }

        if (victorySubtitleText != null)
        {
            victorySubtitleText.text = "Has entregado el pendrive y completado la extracción.\n\n<color=#4CAF50><b>Presiona [R] para reiniciar el nivel</b></color>";
        }

        if (objectivesPanel != null)
        {
            objectivesPanel.SetActive(false);
        }
    }

    #region Generación Dinámica de UI (Fallback Automático)

    private void EnsureUIStructure()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null) return;

        EnsureEventSystem();

        // 1. Panel de Objetivos si no está asignado
        if (objectivesPanel == null)
        {
            BuildDefaultObjectivesPanel(canvas);
        }
        else if (placeTopRight)
        {
            ApplyTopRightPosition(objectivesPanel.GetComponent<RectTransform>());
        }

        // 2. Banner de Mensajes si no está asignado
        if (messageBanner == null)
        {
            BuildDefaultMessageBanner(canvas);
        }

        // 3. Pantalla de Victoria si no está asignada
        if (victoryPanel == null)
        {
            BuildDefaultVictoryPanel(canvas);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }
    }

    private void ApplyTopRightPosition(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(1f, 1f); // Esquina Superior Derecha
        rt.anchorMax = new Vector2(1f, 1f); // Esquina Superior Derecha
        rt.pivot = new Vector2(1f, 1f);     // Esquina Superior Derecha
        rt.anchoredPosition = topRightOffset;
    }

    private void BuildDefaultObjectivesPanel(Canvas canvas)
    {
        GameObject panelObj = new GameObject("MissionObjectives_Panel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = panelObj.GetComponent<RectTransform>();
        ApplyTopRightPosition(rt);
        rt.sizeDelta = new Vector2(380f, 130f);

        Image bg = panelObj.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

        // Título
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(12f, -10f);
        titleRt.sizeDelta = new Vector2(-24f, 26f);

        Text title = titleObj.GetComponent<Text>();
        title.text = "OBJETIVOS DE MISIÓN";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 15;
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(0.3f, 0.8f, 1f, 1f);

        // Contenido
        GameObject contentObj = new GameObject("ContentText", typeof(RectTransform), typeof(Text));
        contentObj.transform.SetParent(panelObj.transform, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 0f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0f, 1f);
        contentRt.anchoredPosition = new Vector2(12f, -38f);
        contentRt.sizeDelta = new Vector2(-24f, -48f);

        Text content = contentObj.GetComponent<Text>();
        content.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        content.fontSize = 13;
        content.color = Color.white;
        content.lineSpacing = 1.2f;

        objectivesPanel = panelObj;
        objectivesTitleText = title;
        objectivesContentText = content;
    }

    private void BuildDefaultMessageBanner(Canvas canvas)
    {
        GameObject bannerObj = new GameObject("MissionMessage_Banner", typeof(RectTransform), typeof(Image));
        bannerObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = bannerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.75f);
        rt.anchorMax = new Vector2(0.5f, 0.75f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600f, 60f);

        Image bg = bannerObj.GetComponent<Image>();
        bg.color = new Color(0.85f, 0.2f, 0.2f, 0.9f);

        GameObject txtObj = new GameObject("BannerText", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(bannerObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        Text txt = txtObj.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 18;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        messageBanner = bannerObj;
        messageText = txt;
        bannerObj.SetActive(false);
    }

    private void BuildDefaultVictoryPanel(Canvas canvas)
    {
        GameObject vicObj = new GameObject("Victory_Panel", typeof(RectTransform), typeof(Image));
        vicObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = vicObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image bg = vicObj.GetComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.08f, 0.94f);

        // Título de Victoria
        GameObject titleObj = new GameObject("VictoryTitle", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(vicObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.65f);
        titleRt.anchorMax = new Vector2(0.5f, 0.65f);
        titleRt.sizeDelta = new Vector2(700f, 70f);

        Text title = titleObj.GetComponent<Text>();
        title.text = "¡MISIÓN CUMPLIDA!";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 38;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.3f, 0.95f, 0.5f, 1f);

        // Subtítulo
        GameObject subObj = new GameObject("VictorySubtitle", typeof(RectTransform), typeof(Text));
        subObj.transform.SetParent(vicObj.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0.55f);
        subRt.anchorMax = new Vector2(0.5f, 0.55f);
        subRt.sizeDelta = new Vector2(700f, 50f);

        Text sub = subObj.GetComponent<Text>();
        sub.text = "Has recuperado los datos del USB y extraído sin ser detectado.";
        sub.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sub.fontSize = 20;
        sub.alignment = TextAnchor.MiddleCenter;
        sub.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        // Botón Reiniciar
        GameObject restartBtnObj = CreateButton(vicObj.transform, "Reiniciar Nivel [R]", new Vector2(0.5f, 0.4f), new Color(0.2f, 0.6f, 0.9f, 1f));
        Button rBtn = restartBtnObj.GetComponent<Button>();
        rBtn.onClick.AddListener(() =>
        {
            if (MissionManager.Instance != null) MissionManager.Instance.RestartMission();
        });

        // Botón Salir
        GameObject quitBtnObj = CreateButton(vicObj.transform, "Salir [Esc]", new Vector2(0.5f, 0.3f), new Color(0.85f, 0.25f, 0.25f, 1f));
        Button qBtn = quitBtnObj.GetComponent<Button>();
        qBtn.onClick.AddListener(() =>
        {
            if (MissionManager.Instance != null) MissionManager.Instance.QuitGame();
        });

        victoryPanel = vicObj;
        victoryTitleText = title;
        victorySubtitleText = sub;
        restartButton = rBtn;
        quitButton = qBtn;

        vicObj.SetActive(false);
    }

    private GameObject CreateButton(Transform parent, string label, Vector2 anchorPos, Color btnColor)
    {
        GameObject btnObj = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.sizeDelta = new Vector2(240f, 48f);

        Image img = btnObj.GetComponent<Image>();
        img.color = btnColor;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        Text txt = txtObj.GetComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 17;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        return btnObj;
    }

    #endregion
}
