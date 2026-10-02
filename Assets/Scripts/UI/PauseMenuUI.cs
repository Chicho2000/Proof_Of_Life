using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMP_Text objectivesText;
    
    [Header("Botones")]
    [SerializeField] private Button btnContinue;
    [SerializeField] private Button btnRestart;

    private bool isPaused = false;

    private void Start()
    {
        // Asegurarse de que empiece apagado
        if (pausePanel != null) pausePanel.SetActive(false);

        // Conectar botones
        if (btnContinue != null) btnContinue.onClick.AddListener(ResumeGame);
        if (btnRestart != null) btnRestart.onClick.AddListener(RestartGame);
    }

    private void Update()
    {
        // Escuchar la tecla de pausa (Escape o P)
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            // Solo pausar si el juego está corriendo o si ya estamos en el menú de pausa
            // Evita abrir la pausa si el jugador está muerto o en la pantalla de inicio
            if (isPaused)
            {
                ResumeGame();
            }
            else if (Time.timeScale > 0f)
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshBriefingText(); // Actualiza los objetivos automáticamente al pausar
    }

    public void ResumeGame()
    {
        isPaused = false;
        
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RefreshBriefingText()
    {
        if (objectivesText == null) return;
        
        if (MissionManager.Instance == null || MissionManager.Instance.Objectives == null)
        {
            objectivesText.text = "Sin datos de misión.";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>Misión Actual:</b>\n");
        
        for (int i = 0; i < MissionManager.Instance.Objectives.Count; i++)
        {
            var obj = MissionManager.Instance.Objectives[i];
            string status = obj.IsComplete ? "<color=green>[COMPLETADO]</color>" : "<color=orange>[PENDIENTE]</color>";
            sb.AppendLine($"{status} {obj.ObjectiveTitle}");
        }
        
        objectivesText.text = sb.ToString();
    }
}
