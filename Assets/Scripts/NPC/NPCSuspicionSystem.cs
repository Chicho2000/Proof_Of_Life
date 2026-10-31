using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(DetectionSystem))]
public class NPCSuspicionSystem : MonoBehaviour
{
    private static readonly List<NPCSuspicionSystem> activeSystems = new List<NPCSuspicionSystem>();

    [Header("Referencias")]
    [SerializeField] private DetectionSystem detectionSystem;

    [Header("Tiempos de Sospecha")]
    [Tooltip("Tiempo en segundos que tarda en llenarse la barra del Nivel 1 (Sospecha).")]
    [SerializeField] [Min(0.1f)] private float level1TimeToFill = 2.0f;

    [Tooltip("Tiempo en segundos que tarda en llenarse la barra del Nivel 2 (Alerta) tras alcanzar el Nivel 1.")]
    [SerializeField] [Min(0.1f)] private float level2TimeToFill = 1.5f;

    [Tooltip("Velocidad de enfriamiento por segundo cuando el jugador rompe la línea de visión.")]
    [SerializeField] [Min(0.05f)] private float decaySpeed = 0.35f;

    [Header("Investigación en Nivel 1")]
    [Tooltip("Tiempo que el NPC se queda mirando en el punto sospechoso antes de regresar.")]
    [SerializeField] [Min(0.5f)] private float investigateDuration = 4.5f;
    [SerializeField] [Min(0.5f)] private float investigateSpeed = 2.4f;

    [Header("Comportamiento")]
    [Tooltip("Permite que el emisor de sospecha gire hacia el jugador.")]
    [SerializeField] private bool canRotateTowardsTarget = true;
    [Tooltip("Permite que el emisor de sospecha se desplace físicamente hacia la posición a investigar.")]
    [SerializeField] private bool canInvestigateMovement = true;

    [Header("Audio (Opcional)")]
    [SerializeField] private AudioClip suspicionSound;
    [SerializeField] private AudioClip alertSound;
    [Range(0f, 1f)] [SerializeField] private float soundVolume = 0.85f;

    // Eventos de instancia
    public event Action<Vector3> OnSuspicionLevel1Reached;
    public event Action OnSuspicionLevel2Reached;
    public event Action OnSuspicionReset;

    // Evento estático global para el HUD (progreso de 0 a 1, nivel 0/1/2, transform del enemigo)
    public static event Action<float, int, Transform> OnGlobalSuspicionUpdated;

    private float currentSuspicion = 0f; // Rango de 0.0f a 2.0f
    private bool hasTriggeredLevel1 = false;
    private bool hasTriggeredLevel2 = false;
    private Vector3 lastSeenPosition;
    private Transform playerTarget;
    private PlayerHealth playerHealth;
    private AudioSource audioSource;
    private NavMeshAgent agent;
    private NPCBase npcBase;
    private NPCHealth npcHealth;

    // Control de desplazamiento e investigación
    private bool isInvestigating = false;
    private bool hasInvestigateDestination = false;
    private bool isWaitingAtInvestigatePoint = false;
    private float investigateWaitTimer = 0f;
    private float investigationMinSuspicion = 0f;
    private float targetVisibleTime = 0f;
    private Vector3 investigateLookTarget;
    private NPCState stateBeforeSuspicion = NPCState.Idle;

    public float CurrentSuspicion
    {
        get { return currentSuspicion; }
    }

    public int CurrentLevel
    {
        get
        {
            if (currentSuspicion >= 2.0f)
            {
                return 2;
            }
            if (currentSuspicion > 1.0f)
            {
                return 2;
            }
            if (currentSuspicion > 0.01f)
            {
                return 1;
            }
            return 0;
        }
    }

    public float CurrentLevelProgress
    {
        get
        {
            return Mathf.Clamp01(currentSuspicion / 2.0f);
        }
    }

    public bool IsInvestigating
    {
        get { return isInvestigating; }
    }

    public bool CanRotateTowardsTarget
    {
        get { return canRotateTowardsTarget; }
        set { canRotateTowardsTarget = value; }
    }

    public bool CanInvestigateMovement
    {
        get { return canInvestigateMovement; }
        set { canInvestigateMovement = value; }
    }

    private void Awake()
    {
        if (detectionSystem == null)
        {
            detectionSystem = GetComponent<DetectionSystem>();
        }

        agent = GetComponent<NavMeshAgent>();
        npcBase = GetComponent<NPCBase>();
        npcHealth = GetComponent<NPCHealth>();
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (!activeSystems.Contains(this))
        {
            activeSystems.Add(this);
        }

        AlarmManager.OnAlarmTriggered += HandleAlarmTriggered;
        AlarmManager.OnAlarmStopped += HandleAlarmStopped;

        if (npcHealth != null)
        {
            npcHealth.OnDeath += HandleNPCDeath;
        }
    }

    private void OnDisable()
    {
        AlarmManager.OnAlarmTriggered -= HandleAlarmTriggered;
        AlarmManager.OnAlarmStopped -= HandleAlarmStopped;

        if (npcHealth != null)
        {
            npcHealth.OnDeath -= HandleNPCDeath;
        }

        if (activeSystems.Contains(this))
        {
            activeSystems.Remove(this);
        }

        ResetSuspicion();
        BroadcastGlobalSuspicion();
    }

    private void HandleNPCDeath()
    {
        CancelInvestigation();
        ResetSuspicion();
        enabled = false;
        BroadcastGlobalSuspicion();
    }

    private void HandleAlarmTriggered()
    {
        CancelInvestigation();
        ResetSuspicion();
        BroadcastGlobalSuspicion();
    }

    private void HandleAlarmStopped()
    {
        CancelInvestigation();
        ResetSuspicion();
        BroadcastGlobalSuspicion();
    }

    private void Start()
    {
        FindPlayerTarget();
    }

    private void Update()
    {
        // Si este NPC murió, cancelar sospecha y desactivar componente
        if ((npcHealth != null && npcHealth.IsDead)
            || (npcBase != null && npcBase.GetCurrentState() == NPCState.Dead))
        {
            if (currentSuspicion > 0f || isInvestigating)
            {
                CancelInvestigation();
                ResetSuspicion();
                BroadcastGlobalSuspicion();
            }
            enabled = false;
            return;
        }

        FindPlayerTarget();

        // Si la alarma ya está activa o el jugador murió, cancelar sospecha
        if ((AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive)
            || playerHealth == null || playerHealth.IsDead)
        {
            if (currentSuspicion > 0f || isInvestigating)
            {
                CancelInvestigation();
                ResetSuspicion();
                BroadcastGlobalSuspicion();
            }
            return;
        }

        bool isTargetVisible = false;
        if (detectionSystem != null && playerTarget != null)
        {
            isTargetVisible = detectionSystem.CanDetectTarget(playerTarget);
        }

        if (isTargetVisible)
        {
            lastSeenPosition = playerTarget.position;
            targetVisibleTime += Time.deltaTime;

            // Multiplicador por distancia suave:
            // Lejos (más de 5m) velocidad normal a moderada (0.85x - 1.0x).
            // Solo cuando está realmente cerca (menos de 5m) sube gradualmente hasta un máximo suave de 1.45x.
            float distance = Vector3.Distance(transform.position, playerTarget.position);
            float distanceMultiplier = 1.0f;
            if (distance <= 5.0f)
            {
                float closeFactor = Mathf.Clamp01(1f - (distance / 5.0f));
                distanceMultiplier = Mathf.Lerp(1.0f, 1.45f, closeFactor);
            }
            else
            {
                float maxRange = detectionSystem != null ? detectionSystem.DetectionRange : 15f;
                float farFactor = Mathf.Clamp01((distance - 5.0f) / Mathf.Max(1f, maxRange - 5.0f));
                distanceMultiplier = Mathf.Lerp(1.0f, 0.85f, farFactor);
            }

            float proximityRadius = detectionSystem != null ? detectionSystem.ProximityRange : 1.4f;
            if (distance <= proximityRadius)
            {
                if (currentSuspicion < 0.4f)
                {
                    currentSuspicion = 0.4f;
                }
                if (targetVisibleTime < 0.35f)
                {
                    targetVisibleTime = 0.35f;
                }
            }

            // Nivel 1 (0.0f a 1.0f)
            if (currentSuspicion < 1.0f || (isInvestigating && targetVisibleTime < 0.35f))
            {
                float rate = 1.0f / Mathf.Max(0.1f, level1TimeToFill);
                currentSuspicion += (rate * distanceMultiplier) * Time.deltaTime;

                // Al llegar al 40% de sospecha, se detiene en seco y gira el cuerpo hacia el jugador
                if (currentSuspicion >= 0.4f && (!isInvestigating || targetVisibleTime >= 0.35f))
                {
                    if (isInvestigating)
                    {
                        CancelInvestigation();
                    }

                    if (npcBase != null && npcBase.GetCurrentState() != NPCState.Suspicious)
                    {
                        if (npcBase.GetCurrentState() != NPCState.Investigate)
                        {
                            stateBeforeSuspicion = npcBase.GetCurrentState();
                        }
                        npcBase.SetState(NPCState.Suspicious);
                    }

                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                    }

                    if (canRotateTowardsTarget && playerTarget != null)
                    {
                        FaceTarget(playerTarget.position);
                    }
                }

                // Al llegar al 100% de la barra azul (entrada al naranja), recién ahí empieza a caminar hacia el lugar
                if (currentSuspicion >= 1.0f)
                {
                    currentSuspicion = 1.0f;
                    if (!hasTriggeredLevel1)
                    {
                        hasTriggeredLevel1 = true;
                        PlayAudio(suspicionSound);
                        if (canInvestigateMovement)
                        {
                            StartInvestigation(lastSeenPosition);
                        }
                        if (OnSuspicionLevel1Reached != null)
                        {
                            OnSuspicionLevel1Reached(lastSeenPosition);
                        }
                    }
                }
            }
            // Nivel 2 (1.0f a 2.0f) - De Naranja a Rojo
            else if (currentSuspicion < 2.0f)
            {
                if (isInvestigating)
                {
                    CancelInvestigation();
                }

                if (canRotateTowardsTarget && playerTarget != null)
                {
                    FaceTarget(playerTarget.position);
                }

                float rate = 1.0f / Mathf.Max(0.1f, level2TimeToFill);
                currentSuspicion += (rate * distanceMultiplier) * Time.deltaTime;

                if (currentSuspicion >= 2.0f)
                {
                    currentSuspicion = 2.0f;
                    targetVisibleTime = 0f;
            if (!hasTriggeredLevel2)
                    {
                        hasTriggeredLevel2 = true;
                        CancelInvestigation();
                        PlayAudio(alertSound);
                        if (OnSuspicionLevel2Reached != null)
                        {
                            OnSuspicionLevel2Reached();
                        }
                    }
                }
            }
        }
        else
        {
            // El jugador no está a la vista: la sospecha decae de inmediato
            targetVisibleTime = 0f;
            if (!hasTriggeredLevel2)
            {
                // Si el NPC te vio y llegó al 40% antes de que te escondas, va al último lugar donde te vio
                if (canInvestigateMovement && currentSuspicion >= 0.4f && !isInvestigating)
                {
                    StartInvestigation(lastSeenPosition);
                }

                // La sospecha se enfría inmediatamente hacia 0
                if (currentSuspicion > 0f)
                {
                    float minFloor = isInvestigating ? Mathf.Max(0.5f, investigationMinSuspicion) : 0f;
                    currentSuspicion = Mathf.Max(minFloor, currentSuspicion - decaySpeed * Time.deltaTime);

                    if (currentSuspicion < 1.0f && hasTriggeredLevel1)
                    {
                        hasTriggeredLevel1 = false;
                    }

                    if (currentSuspicion <= 0f)
                    {
                        ResetSuspicion();
                        if (npcBase != null && (npcBase.GetCurrentState() == NPCState.Suspicious || (!isInvestigating && npcBase.GetCurrentState() == NPCState.Investigate)))
                        {
                            npcBase.SetState(GetReturnState());
                        }
                    }
                }
            }
        }

        if (isInvestigating)
        {
            TickInvestigationMovement();
        }

        BroadcastGlobalSuspicion();
    }

    public void StartInvestigation(Vector3 targetPosition, float customDuration = -1f, float minSuspicion = 0.5f)
    {
        if (AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive)
        {
            return;
        }

        if (customDuration > 0f)
        {
            investigateDuration = customDuration;
        }

        // Si la investigación es llamada por un ruido de distracción u otro estímulo desde 0,
        // asegurar que haya sospecha mínima activa sin forzar la barra al final
        investigateLookTarget = targetPosition;
        investigationMinSuspicion = Mathf.Max(investigationMinSuspicion, Mathf.Clamp(minSuspicion, 0.5f, 1.0f));
        if (currentSuspicion < investigationMinSuspicion)
        {
            currentSuspicion = investigationMinSuspicion;
            hasTriggeredLevel1 = true;
            PlayAudio(suspicionSound);
            BroadcastGlobalSuspicion();
        }

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
        {
            lastSeenPosition = hit.position;
        }
        else
        {
            lastSeenPosition = targetPosition;
        }

        if (npcBase != null && npcBase.GetCurrentState() != NPCState.Investigate)
        {
            if (npcBase.GetCurrentState() != NPCState.Suspicious)
            {
                stateBeforeSuspicion = npcBase.GetCurrentState();
            }
            npcBase.SetState(NPCState.Investigate);
        }

        isInvestigating = true;
        hasInvestigateDestination = false;
        isWaitingAtInvestigatePoint = false;
        investigateWaitTimer = 0f;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = investigateSpeed;
            agent.stoppingDistance = 0.8f;
            hasInvestigateDestination = agent.SetDestination(lastSeenPosition);
        }
    }

    private void TickInvestigationMovement()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        if (!hasInvestigateDestination)
        {
            agent.isStopped = false;
            agent.speed = investigateSpeed;
            agent.stoppingDistance = 0.8f;
            if (agent.SetDestination(lastSeenPosition))
            {
                hasInvestigateDestination = true;
                isWaitingAtInvestigatePoint = false;
                investigateWaitTimer = 0f;
            }
            return;
        }

        if (!isWaitingAtInvestigatePoint)
        {
            if (detectionSystem != null && playerTarget != null && detectionSystem.CanDetectTarget(playerTarget))
            {
                agent.SetDestination(lastSeenPosition);
            }

            if (agent.pathPending)
            {
                return;
            }

            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                FinishInvestigation();
                return;
            }

            float reachDistance = Mathf.Max(0.8f, agent.stoppingDistance);
            if (agent.hasPath && agent.remainingDistance <= reachDistance)
            {
                agent.isStopped = true;
                isWaitingAtInvestigatePoint = true;
                investigateWaitTimer = 0f;
            }
        }
        else
        {
            if (canRotateTowardsTarget)
            {
                FaceTarget(investigateLookTarget, 240f);
            }
            investigateWaitTimer += Time.deltaTime;
            if (investigateWaitTimer >= investigateDuration)
            {
                FinishInvestigation();
            }
        }
    }

    private void FinishInvestigation()
    {
        isInvestigating = false;
        hasInvestigateDestination = false;
        isWaitingAtInvestigatePoint = false;
        investigateWaitTimer = 0f;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = false;
        }

        if (npcBase != null && npcBase.GetCurrentState() == NPCState.Investigate)
        {
            npcBase.SetState(GetReturnState());
        }

        ResetSuspicion();
        BroadcastGlobalSuspicion();
    }

    public void SetReturnState(NPCState state)
    {
        stateBeforeSuspicion = state;
    }

    private NPCState GetReturnState()
    {
        NPCState returnState = stateBeforeSuspicion;
        if (returnState == NPCState.Suspicious || returnState == NPCState.Investigate)
        {
            returnState = (npcBase != null && npcBase.HasPatrolPoints()) ? NPCState.Patrol : NPCState.Idle;
        }
        else if (returnState == NPCState.Patrol && npcBase != null && !npcBase.HasPatrolPoints())
        {
            returnState = NPCState.Idle;
        }
        return returnState;
    }

    public void CancelInvestigation()
    {
        isInvestigating = false;
        hasInvestigateDestination = false;
        isWaitingAtInvestigatePoint = false;
        investigateWaitTimer = 0f;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    public void TriggerInstantAlert()
    {
        if (AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive)
        {
            return;
        }

        currentSuspicion = 2.0f;
        targetVisibleTime = 0f;
        hasTriggeredLevel1 = true;

        if (!hasTriggeredLevel2)
        {
            hasTriggeredLevel2 = true;
            CancelInvestigation();
            PlayAudio(alertSound);
            BroadcastGlobalSuspicion();
            if (OnSuspicionLevel2Reached != null)
            {
                OnSuspicionLevel2Reached();
            }
        }
    }

    public static void NotifyIllegalAction(Transform actor, Vector3 actionPosition)
    {
        if (actor == null || (AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive))
        {
            return;
        }

        for (int i = 0; i < activeSystems.Count; i++)
        {
            NPCSuspicionSystem system = activeSystems[i];
            if (system == null || !system.isActiveAndEnabled)
            {
                continue;
            }

            // Los civiles solo se asustan gradualmente al ver la pistola o al sonar la alarma.
            if (system.npcBase is CivilianNPC)
            {
                continue;
            }

            if ((system.npcHealth != null && system.npcHealth.IsDead)
                || (system.npcBase != null && system.npcBase.GetCurrentState() == NPCState.Dead))
            {
                continue;
            }

            if (system.detectionSystem != null && system.detectionSystem.CanDetectTarget(actor, true))
            {
                if (system.canRotateTowardsTarget)
                {
                    system.FaceTarget(actor.position, 720f);
                }
                system.TriggerInstantAlert();
            }
        }
    }

    public void ResetSuspicion()
    {
        currentSuspicion = 0f;
        investigationMinSuspicion = 0f;
        targetVisibleTime = 0f;
        hasTriggeredLevel1 = false;
        hasTriggeredLevel2 = false;

        if (OnSuspicionReset != null)
        {
            OnSuspicionReset();
        }
    }

    private void FaceTarget(Vector3 targetPosition, float turnSpeed = 360f)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    private void PlayAudio(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, soundVolume);
        }
    }

    private void FindPlayerTarget()
    {
        if (playerTarget != null && playerHealth != null)
        {
            return;
        }

        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerTarget = playerHealth.transform;
            return;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            playerTarget = taggedPlayer.transform;
            playerHealth = taggedPlayer.GetComponent<PlayerHealth>();
        }
    }

    private static void BroadcastGlobalSuspicion()
    {
        float highestSuspicion = 0f;
        int highestLevel = 0;
        float highestProgress = 0f;
        Transform highestSource = null;

        for (int i = 0; i < activeSystems.Count; i++)
        {
            NPCSuspicionSystem system = activeSystems[i];
            if (system != null && system.isActiveAndEnabled && (system.npcHealth == null || !system.npcHealth.IsDead))
            {
                if (system.CurrentSuspicion > highestSuspicion)
                {
                    highestSuspicion = system.CurrentSuspicion;
                    highestLevel = system.CurrentLevel;
                    highestProgress = system.CurrentLevelProgress;
                    highestSource = system.transform;
                }
            }
        }

        if (OnGlobalSuspicionUpdated != null)
        {
            OnGlobalSuspicionUpdated(highestProgress, highestLevel, highestSource);
        }
    }
}
