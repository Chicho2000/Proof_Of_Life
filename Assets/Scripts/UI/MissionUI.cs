using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MissionUI : MonoBehaviour
{
    [Header("1. Briefing Inicial")]
    [SerializeField] private GameObject panelBriefing;
    [SerializeField] private TMP_Text briefingObjectivesListTMP;
    [SerializeField] private Button btnIniciarMision;
    [SerializeField] private bool showBriefingOnStart = true;

    [Header("2. HUD Objetivo Actual")]
    [SerializeField] private GameObject objectivesPanel;
    [SerializeField] private TMP_Text objectivesContentTMP;

    [Header("3. Pantalla de Victoria")]
    [SerializeField] private GameObject panelVictoria;
    [SerializeField] private TMP_Text victoriaTimeTMP;
    [SerializeField] private TMP_Text victoriaSummaryTMP;
    [SerializeField] private Button btnRepetirVictoria;
    [SerializeField] private Button btnSalirVictoria;

    [Header("4. Pantalla de Muerte")]
    [SerializeField] private GameObject panelMuerte;
    [SerializeField] private CanvasGroup deathCanvasGroup;
    [SerializeField] private Button btnReiniciarMuerte;
    [SerializeField] private float deathFadeDuration = 3.5f;

    [Header("5. Pantalla de Derrota")]
    [SerializeField] private GameObject panelDerrota;
    [SerializeField] private CanvasGroup defeatCanvasGroup;
    [SerializeField] private Button btnReiniciarDerrota;

    private float missionStartTime;

    private void Awake()
    {
        EnsureEventSystem();
        AutoFindReferences();
    }

    private void Start()
    {
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelMuerte != null) panelMuerte.SetActive(false);
        if (panelDerrota != null) panelDerrota.SetActive(false);

        SetupButtonListeners();

        if (showBriefingOnStart && panelBriefing != null)
        {
            ShowBriefing();
        }
        else
        {
            StartGameplay();
        }
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

    private bool IsAnyEndScreenActive()
    {
        return (panelMuerte != null && panelMuerte.activeSelf) ||
               (panelDerrota != null && panelDerrota.activeSelf) ||
               (panelVictoria != null && panelVictoria.activeSelf);
    }

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

    // --- BRIEFING ---
    private void ShowBriefing()
    {
        Time.timeScale = 0f;
        panelBriefing.SetActive(true);
        if (objectivesPanel != null) objectivesPanel.SetActive(false);

        if (briefingObjectivesListTMP != null && MissionManager.Instance != null)
        {
            if (MissionManager.Instance.Objectives.Count == 0)
            {
                MissionManager.Instance.DiscoverSceneObjectives();
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("OBJETIVOS DE LA OPERACIÓN:\n");
            var list = MissionManager.Instance.Objectives;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null) sb.AppendLine($"• [{i + 1}] {list[i].ObjectiveTitle}");
            }
            sb.AppendLine($"• [{list.Count + 1}] Escapar por el punto de extracción");
            briefingObjectivesListTMP.text = sb.ToString().TrimEnd();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void StartGameplay()
    {
        if (panelBriefing != null) panelBriefing.SetActive(false);
        Time.timeScale = 1f;
        missionStartTime = Time.time;

        if (objectivesPanel != null) objectivesPanel.SetActive(true);
        UpdateObjectivesDisplay();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // --- HUD OBJETIVO ACTUAL ---
    private void OnObjectiveChanged(MissionObjective obj)
    {
        UpdateObjectivesDisplay();
    }

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
            objectivesContentTMP.text = $"• {activeObj.ObjectiveTitle}";
        else
            objectivesContentTMP.text = "• Huir por el punto de extracción";
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
        if (btnIniciarMision != null) btnIniciarMision.onClick.AddListener(StartGameplay);
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

    private void AutoFindReferences()
    {
        if (panelBriefing != null && btnIniciarMision == null)
            btnIniciarMision = panelBriefing.GetComponentInChildren<Button>(true);

        if (panelMuerte != null)
        {
            if (deathCanvasGroup == null) deathCanvasGroup = panelMuerte.GetComponent<CanvasGroup>();
            if (btnReiniciarMuerte == null) btnReiniciarMuerte = panelMuerte.GetComponentInChildren<Button>(true);
        }

        if (panelVictoria != null)
        {
            if (btnRepetirVictoria == null)
            {
                Button[] btns = panelVictoria.GetComponentsInChildren<Button>(true);
                if (btns.Length > 0) btnRepetirVictoria = btns[0];
                if (btns.Length > 1) btnSalirVictoria = btns[1];
            }
        }
    }
}
