using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor principal del sistema de misiones.
/// Administra los objetivos activos, valida el cumplimiento de requisitos
/// y ejecuta la finalización del juego al extraer.
/// </summary>
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Objetivos de la Misión")]
    [Tooltip("Lista de objetivos a cumplir en esta misión. Si está vacía, se autocompletará con los objetivos en escena.")]
    [SerializeField] private List<MissionObjective> objectives = new List<MissionObjective>();

    [Header("Audio y Feedback")]
    [SerializeField] private AudioClip missionCompleteSound;
    [SerializeField] private AudioClip objectiveUpdatedSound;

    [Header("Control del Cursor al Terminar")]
    [SerializeField] private bool unlockCursorOnEnd = true;

    [Header("Reinicio de Nivel")]
    [Tooltip("Permite reiniciar el nivel al pulsar una tecla tras completar la misión.")]
    [SerializeField] private bool allowRestartOnKey = true;
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private KeyCode secondaryRestartKey = KeyCode.Return;

    // Estado
    public bool IsMissionCompleted { get; private set; }
    public IReadOnlyList<MissionObjective> Objectives => objectives;

    // Eventos
    public static event Action<MissionObjective> OnObjectiveChanged;
    public static event Action OnMissionCompleted;
    public static event Action<string> OnMissionMessage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[MissionManager] Ya existe una instancia de MissionManager en '{Instance.gameObject.name}'. Destruyendo duplicado en '{gameObject.name}'.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        DiscoverSceneObjectives();
    }

    private void Update()
    {
        if (IsMissionCompleted && allowRestartOnKey)
        {
            if (Input.GetKeyDown(restartKey) || Input.GetKeyDown(secondaryRestartKey))
            {
                Debug.Log($"🔄 [MissionManager] Reiniciando nivel por pulsación de tecla ({restartKey})...");
                RestartMission();
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            UnsubscribeAllObjectives();
            Instance = null;
        }
    }

    /// <summary>
    /// Encuentra y registra automáticamente todos los objetivos presentes en la escena si la lista está vacía.
    /// </summary>
    public void DiscoverSceneObjectives()
    {
        if (objectives.Count == 0)
        {
            MissionObjective[] found = FindObjectsByType<MissionObjective>(FindObjectsSortMode.None);
            foreach (var obj in found)
            {
                RegisterObjective(obj);
            }
        }
        else
        {
            foreach (var obj in objectives)
            {
                if (obj != null)
                {
                    obj.OnObjectiveStateChanged += HandleObjectiveStateChanged;
                }
            }
        }

        Debug.Log($"📋 [MissionManager] Inicializado con {objectives.Count} objetivo(s).");
    }

    /// <summary>
    /// Registra un nuevo objetivo en el gestor.
    /// </summary>
    public void RegisterObjective(MissionObjective objective)
    {
        if (objective == null || objectives.Contains(objective)) return;

        objectives.Add(objective);
        objective.OnObjectiveStateChanged += HandleObjectiveStateChanged;
    }

    private void UnsubscribeAllObjectives()
    {
        foreach (var obj in objectives)
        {
            if (obj != null)
            {
                obj.OnObjectiveStateChanged -= HandleObjectiveStateChanged;
            }
        }
    }

    private void HandleObjectiveStateChanged(MissionObjective objective)
    {
        if (objectiveUpdatedSound != null)
        {
            AudioSource.PlayClipAtPoint(objectiveUpdatedSound, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        OnObjectiveChanged?.Invoke(objective);
    }

    /// <summary>
    /// Comprueba si todos los objetivos obligatorios (no opcionales) han sido completados.
    /// </summary>
    public bool AreMandatoryObjectivesComplete()
    {
        foreach (var obj in objectives)
        {
            if (obj != null && !obj.IsOptional && !obj.IsComplete)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Emite un mensaje de estado o advertencia de la misión (para UI y consola).
    /// </summary>
    public void NotifyMissionMessage(string message)
    {
        Debug.Log($"📢 <color=#FFC107><b>[Misión]:</b></color> {message}");
        OnMissionMessage?.Invoke(message);
    }

    /// <summary>
    /// Finaliza la misión exitosamente y termina la partida.
    /// Desactiva controles del jugador y desbloquea el cursor.
    /// </summary>
    public void CompleteMission()
    {
        if (IsMissionCompleted) return;

        IsMissionCompleted = true;
        Debug.Log("🏆 <color=#4CAF50><b>==========================================</b></color>");
        Debug.Log("🏆 <color=#4CAF50><b>¡MISIÓN CUMPLIDA! EXTRACCIÓN EXITOSA</b></color>");
        Debug.Log("🏆 <color=#4CAF50><b>==========================================</b></color>");

        if (missionCompleteSound != null)
        {
            AudioSource.PlayClipAtPoint(missionCompleteSound, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        // Desactivar controles del jugador para dar por finalizada la partida
        DisablePlayerControls();

        // Desbloquear cursor para interactuar con pantallas de fin de juego
        if (unlockCursorOnEnd)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        OnMissionCompleted?.Invoke();
    }

    private void DisablePlayerControls()
    {
        FPSPlayerController controller = FindFirstObjectByType<FPSPlayerController>();
        if (controller != null) controller.enabled = false;

        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        if (combat != null) combat.enabled = false;

        PlayerInteraction interaction = FindFirstObjectByType<PlayerInteraction>();
        if (interaction != null) interaction.enabled = false;
    }

    /// <summary>
    /// Reinicia la misión recargando la escena actual.
    /// </summary>
    public void RestartMission()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Cierra el juego o detiene el modo Play en el editor.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
