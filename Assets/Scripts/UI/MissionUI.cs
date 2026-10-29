using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MissionUI : MonoBehaviour
{
    // -- 1. Briefing ----------------------------------------------------------
    [Header("1. Briefing")]
    [SerializeField] private GameObject missionBriefRoot;  // Assign "Mission_Brief" in Inspector
    [SerializeField] private bool  showBriefingOnStart = true;
    [SerializeField] private Color btnSelectedColor    = new Color(0.96f, 0.62f, 0.04f, 1f);
    [SerializeField] private Color btnNormalColor      = new Color(0.22f, 0.25f, 0.32f, 1f);

    // Wired at runtime via transform.Find()
    private TMP_Text texto_Titulo;
    private TMP_Text texto_Descripcion;
    private TMP_Text texto_Objetivos;
    private Button   boton_Extraccion;
    private Button   boton_Eliminacion;
    private Button   boton_IniciarMision;

    private bool esModoEliminacion = false;

    // -- 2. HUD Objetivo Actual ------------------------------------------------
    [Header("2. HUD Objetivo Actual")]
    [SerializeField] private GameObject objectivesPanel;
    [SerializeField] private TMP_Text   objectivesContentTMP;

    // -- 3. Pantalla de Victoria -----------------------------------------------
    [Header("3. Pantalla de Victoria")]
    [SerializeField] private GameObject panelVictoria;
    [SerializeField] private TMP_Text   victoriaTimeTMP;
    [SerializeField] private TMP_Text   victoriaSummaryTMP;
    [SerializeField] private Button     btnRepetirVictoria;
    [SerializeField] private Button     btnSalirVictoria;

    // -- 4. Pantalla de Muerte -------------------------------------------------
    [Header("4. Pantalla de Muerte")]
    [SerializeField] private GameObject  panelMuerte;
    [SerializeField] private CanvasGroup deathCanvasGroup;
    [SerializeField] private Button      btnReiniciarMuerte;
    [SerializeField] private float       deathFadeDuration = 3.5f;

    // -- 5. Pantalla de Derrota ------------------------------------------------
    [Header("5. Pantalla de Derrota")]
    [SerializeField] private GameObject  panelDerrota;
    [SerializeField] private CanvasGroup defeatCanvasGroup;
    [SerializeField] private Button      btnReiniciarDerrota;

    private float missionStartTime;

    private void Awake()
    {
        EnsureEventSystem();
        WireBriefingFromRoot();
        AutoFindEndScreenRefs();
    }

    private void Start()
    {
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelMuerte != null) panelMuerte.SetActive(false);
        if (panelDerrota != null) panelDerrota.SetActive(false);

        SetupButtonListeners();

        if (showBriefingOnStart && missionBriefRoot != null)
            ShowBriefing();
        else
            StartGameplay();
    }

    private bool isDeathFading = false;
    private float deathFadeTimer = 0f;

    private void Update()
    {
        if (isDeathFading && deathCanvasGroup != null)
        {
            deathFadeTimer += Time.unscaledDeltaTime;
            deathCanvasGroup.alpha = Mathf.Clamp01(deathFadeTimer / deathFadeDuration);
            if (deathFadeTimer >= deathFadeDuration)
            {
                isDeathFading = false;
                deathCanvasGroup.alpha = 1f;
            }
        }

        if (IsAnyEndScreenActive())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (Input.GetKeyDown(KeyCode.R)) RestartGame();
            if (Input.GetKeyDown(KeyCode.Escape)) QuitGame();
        }
    }

    private bool IsAnyEndScreenActive() =>
        (panelMuerte   != null && panelMuerte  .activeSelf) ||
        (panelDerrota  != null && panelDerrota .activeSelf) ||
        (panelVictoria != null && panelVictoria.activeSelf);

    private void OnEnable()
    {
        MissionManager.OnObjectiveChanged += OnObjectiveChanged;
        MissionManager.OnMissionCompleted += OnMissionComplete;
        MissionManager.OnMissionFailed += HandleMissionFailed;

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null) playerHealth.OnDeath += OnPlayerDead;
    }

    private void OnDisable()
    {
        MissionManager.OnObjectiveChanged -= OnObjectiveChanged;
        MissionManager.OnMissionCompleted -= OnMissionComplete;
        MissionManager.OnMissionFailed -= HandleMissionFailed;

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null) playerHealth.OnDeath -= OnPlayerDead;
    }

    private void HandleMissionFailed(string reason)
    {
        if (panelMuerte != null && panelMuerte.activeSelf) return;

        if (panelDerrota != null)
        {
            panelDerrota.SetActive(true);
        }
        if (objectivesPanel != null) objectivesPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // -- Briefing -------------------------------------------------------------

    /// <summary>Locates every briefing widget inside Mission_Brief by name.</summary>
    private void WireBriefingFromRoot()
    {
        if (missionBriefRoot == null) return;
        Transform root = missionBriefRoot.transform;

        texto_Titulo        = root.Find("Titulo")                 ?.GetComponent<TMP_Text>();
        texto_Descripcion   = root.Find("Briefing_Descripcion")   ?.GetComponent<TMP_Text>();
        texto_Objetivos     = root.Find("Briefing_Objetivos")     ?.GetComponent<TMP_Text>();
        boton_Extraccion    = root.Find("Boton_Mision_Extraccion") ?.GetComponent<Button>();
        boton_Eliminacion   = root.Find("Boton_Mision_Eliminacion")?.GetComponent<Button>();
        boton_IniciarMision = root.Find("Boton_IniciarMision")    ?.GetComponent<Button>();
    }

    private void ShowBriefing()
    {
        Time.timeScale = 0f;
        missionBriefRoot.SetActive(true);
        if (objectivesPanel != null) objectivesPanel.SetActive(false);
        SeleccionarMisionExtraccion();      // default selection
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void SeleccionarMisionExtraccion()
    {
        esModoEliminacion = false;
        SetButtonColor(boton_Extraccion,  btnSelectedColor);
        SetButtonColor(boton_Eliminacion, btnNormalColor);
        if (texto_Titulo      != null) texto_Titulo.text      = "OPERACI\u00d3N: ROBO DE DATOS";
        if (texto_Descripcion != null) texto_Descripcion.text = "Infiltrarse en el edificio, asegurar el pendrive con los planos y evacuar. El ejecutivo es un objetivo opcional, pero si escapa no hay penalizaci\u00f3n.";
        ActualizarTextoObjetivosBriefing();
    }

    private void SeleccionarMisionEliminacion()
    {
        esModoEliminacion = true;
        SetButtonColor(boton_Extraccion,  btnNormalColor);
        SetButtonColor(boton_Eliminacion, btnSelectedColor);
        if (texto_Titulo      != null) texto_Titulo.text      = "OPERACI\u00d3N: ASESINATO";
        if (texto_Descripcion != null) texto_Descripcion.text = "El ejecutivo no debe escapar. Localiza y elimina al objetivo antes de que abandone el complejo. El pendrive no es relevante en esta misi\u00f3n.";
        ActualizarTextoObjetivosBriefing();
    }

    private void ActualizarTextoObjetivosBriefing()
    {
        if (texto_Objetivos == null) return;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("OBJETIVOS DE LA OPERACI\u00d3N:\n");
        if (esModoEliminacion)
        {
            sb.AppendLine("\u2022 [1] Eliminar al Ejecutivo H. P. (Obligatorio)");
            sb.AppendLine("\u2022 [2] Escapar por el punto de extracci\u00f3n");
        }
        else
        {
            sb.AppendLine("\u2022 [1] Robar el pendrive de los servidores (Obligatorio)");
            sb.AppendLine("\u2022 [2] Eliminar al Ejecutivo H. P. (Opcional)");
            sb.AppendLine("\u2022 [3] Escapar por el punto de extracci\u00f3n");
        }
        texto_Objetivos.text = sb.ToString().TrimEnd();
    }

    private static void SetButtonColor(Button btn, Color color)
    {
        if (btn != null) btn.GetComponent<Image>().color = color;
    }

    private void StartGameplay()
    {
        if (missionBriefRoot != null) missionBriefRoot.SetActive(false);
        Time.timeScale   = 1f;
        missionStartTime = Time.time;

        if (MissionManager.Instance != null)
        {
            if (esModoEliminacion) MissionManager.Instance.ConfigurarMisionEliminacion();
            else                   MissionManager.Instance.ConfigurarMisionExtraccion();
        }

        if (objectivesPanel != null) objectivesPanel.SetActive(true);
        UpdateObjectivesDisplay();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // -- HUD Objetivo Actual --------------------------------------------------
    private void OnObjectiveChanged(MissionObjective obj) => UpdateObjectivesDisplay();

    public void UpdateObjectivesDisplay()
    {
        if (objectivesContentTMP == null) return;

        MissionObjective activeObj = null;
        if (MissionManager.Instance != null)
        {
            foreach (var obj in MissionManager.Instance.Objectives)
            {
                if (obj != null && !obj.IsComplete)
                {
                    activeObj = obj;
                    break;
                }
            }
        }

        if (activeObj != null)
            objectivesContentTMP.text = $"{activeObj.ObjectiveTitle}";
        else
            objectivesContentTMP.text = "Huir por el punto de extracción";
    }

    // --- VICTORIA ---
    private void OnMissionComplete()
    {
        if (objectivesPanel != null) objectivesPanel.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(true);

        float elapsed = Time.time - missionStartTime;
        int m = Mathf.FloorToInt(elapsed / 60f);
        int s = Mathf.FloorToInt(elapsed % 60f);

        if (victoriaTimeTMP != null)
            victoriaTimeTMP.text = $"TIEMPO DE OPERACIÓN: {m:00}:{s:00}";

        if (victoriaSummaryTMP != null && MissionManager.Instance != null)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var obj in MissionManager.Instance.Objectives)
            {
                if (obj != null)
                {
                    sb.AppendLine($"<color=#22C55E>[X]</color> {obj.ObjectiveTitle}");
                }
            }
            sb.AppendLine("<color=#22C55E>[X]</color> Extracción exitosa");
            victoriaSummaryTMP.text = sb.ToString().TrimEnd();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // --- MUERTE Y DERROTA ---
    private void OnPlayerDead()
    {
        if (MissionManager.Instance != null && MissionManager.Instance.IsMissionCompleted) return;

        if (objectivesPanel != null) objectivesPanel.SetActive(false);
        if (panelMuerte != null)
        {
            panelMuerte.SetActive(true);
            if (deathCanvasGroup != null)
            {
                deathCanvasGroup.alpha = 0f;
                deathFadeTimer = 0f;
                isDeathFading = true;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // --- BOTONES Y ACCIONES ---
    private void SetupButtonListeners()
    {
        if (boton_IniciarMision != null) boton_IniciarMision.onClick.AddListener(StartGameplay);
        if (boton_Extraccion != null) boton_Extraccion.onClick.AddListener(SeleccionarMisionExtraccion);
        if (boton_Eliminacion != null) boton_Eliminacion.onClick.AddListener(SeleccionarMisionEliminacion);
        if (btnRepetirVictoria != null) btnRepetirVictoria.onClick.AddListener(RestartGame);
        if (btnSalirVictoria != null) btnSalirVictoria.onClick.AddListener(QuitGame);
        if (btnReiniciarMuerte != null) btnReiniciarMuerte.onClick.AddListener(RestartGame);
        if (btnReiniciarDerrota != null) btnReiniciarDerrota.onClick.AddListener(RestartGame);
    }

    public void RestartGame()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void AutoFindEndScreenRefs()
    {
        if (panelMuerte != null)
        {
            if (deathCanvasGroup   == null) deathCanvasGroup   = panelMuerte.GetComponent<CanvasGroup>();
            if (btnReiniciarMuerte == null) btnReiniciarMuerte = panelMuerte.GetComponentInChildren<Button>(true);
        }
        if (panelVictoria != null && btnRepetirVictoria == null)
        {
            Button[] btns = panelVictoria.GetComponentsInChildren<Button>(true);
            if (btns.Length > 0) btnRepetirVictoria = btns[0];
            if (btns.Length > 1) btnSalirVictoria   = btns[1];
        }
    }
}
