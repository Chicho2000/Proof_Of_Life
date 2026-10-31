using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(NPCHealth), typeof(DetectionSystem))]
[RequireComponent(typeof(NPCSuspicionSystem))]
public class CivilianNPC : NPCBase
{
    private const string DefaultExitName = "CivilianExit";

    [Header("Huida")]
    [SerializeField] private Transform exitPoint;
    [SerializeField, Min(0.1f)] private float fleeSpeed = 5f;
    [SerializeField, Min(0.1f)] private float reachThreshold = 1.2f;

    private NPCSuspicionSystem suspicionSystem;
    private bool hasDestination;
    private bool hasEscaped;
    private bool warnedAboutExit;
    private bool warnedAboutNavMesh;
    private float nextDoorCheckTime;
    private float nextPathRetryTime;

    protected override void Awake()
    {
        base.Awake();

        DetectionSystem detection = GetComponent<DetectionSystem>();
        detection.DetectionPolicy = DetectionSystem.TargetDetectionPolicy.VisiblePistolOnly;

        suspicionSystem = GetComponent<NPCSuspicionSystem>();
        suspicionSystem.CanInvestigateMovement = false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        AlarmManager.OnAlarmTriggered += HandleAlarm;
        if (suspicionSystem != null)
        {
            suspicionSystem.OnSuspicionLevel2Reached += HandleFullSuspicion;
        }
    }

    protected override void OnDisable()
    {
        AlarmManager.OnAlarmTriggered -= HandleAlarm;
        if (suspicionSystem != null)
        {
            suspicionSystem.OnSuspicionLevel2Reached -= HandleFullSuspicion;
        }
        base.OnDisable();
    }

    protected override void Start()
    {
        // Cualquier impacto de bala mata al civil, sin alterar la salud de guardias y ejecutivo.
        health.SetMaxHealth(1);
        EnsureNavMeshPlacement();
        currentState = NPCState.Idle;
        base.Start();

        if (AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive)
        {
            SetState(NPCState.Flee);
        }
    }

    private void Update()
    {
        if (currentState == NPCState.Flee)
        {
            TickFlee();
        }

        UpdateAnimation();
    }

    private void HandleAlarm()
    {
        BeginFlee();
    }

    private void HandleFullSuspicion()
    {
        // Ver la pistola asusta al civil, pero no activa la alarma.
        BeginFlee();
    }

    private void BeginFlee()
    {
        if (!hasEscaped && currentState != NPCState.Dead && (health == null || !health.IsDead)
            && currentState != NPCState.Flee)
        {
            SetState(NPCState.Flee);
        }
    }

    protected override void OnStateChanged(NPCState newState)
    {
        base.OnStateChanged(newState);

        if (newState == NPCState.Flee)
        {
            ResolveExit();
            hasDestination = false;
            if (CanUseAgent())
            {
                agent.isStopped = false;
                agent.speed = fleeSpeed;
                agent.stoppingDistance = reachThreshold;
            }
        }
        else
        {
            StopAgent();
        }
    }

    private void TickFlee()
    {
        if (!CanUseAgent())
        {
            if (!warnedAboutNavMesh)
            {
                Debug.LogWarning($"[CivilianNPC] {name}: no está sobre un NavMesh válido.", this);
                warnedAboutNavMesh = true;
            }
            return;
        }

        if (!ResolveExit())
        {
            return;
        }

        if (!hasDestination || (agent.pathStatus != NavMeshPathStatus.PathComplete && Time.time >= nextPathRetryTime))
        {
            agent.isStopped = false;
            agent.speed = fleeSpeed;
            hasDestination = agent.SetDestination(exitPoint.position);
            nextPathRetryTime = Time.time + 0.5f;
        }

        if (Time.time >= nextDoorCheckTime)
        {
            OpenNearbyDoors();
            nextDoorCheckTime = Time.time + 0.25f;
        }

        bool hasReachedExit = agent.hasPath || Vector3.Distance(agent.nextPosition, exitPoint.position) <= reachThreshold;
        if (hasDestination && hasReachedExit && !agent.pathPending
            && agent.pathStatus == NavMeshPathStatus.PathComplete
            && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, reachThreshold))
        {
            hasEscaped = true;
            StopAgent();
            gameObject.SetActive(false);
        }
    }

    private bool ResolveExit()
    {
        if (exitPoint != null)
        {
            return true;
        }

        GameObject exit = GameObject.Find(DefaultExitName);
        if (exit != null)
        {
            exitPoint = exit.transform;
            return true;
        }

        if (!warnedAboutExit)
        {
            Debug.LogWarning($"[CivilianNPC] {name}: asigna Exit Point o crea '{DefaultExitName}' en la escena.", this);
            warnedAboutExit = true;
        }
        return false;
    }

    private void EnsureNavMeshPlacement()
    {
        if (agent == null || !agent.enabled || agent.isOnNavMesh)
        {
            return;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    private void StopAgent()
    {
        if (!CanUseAgent())
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
    }

    protected override void HandleDeath()
    {
        base.HandleDeath();
        StopAgent();
    }

    private void UpdateAnimation()
    {
        if (animators == null)
        {
            return;
        }

        float speed = currentState == NPCState.Flee && CanUseAgent() && agent.velocity.sqrMagnitude > 0.01f ? 2f : 0f;
        foreach (Animator animator in animators)
        {
            if (animator != null)
            {
                animator.SetFloat("SpeedX", 0f);
                animator.SetFloat("SpeedY", speed);

                int handLayer = animator.GetLayerIndex("Hand layer");
                if (handLayer >= 0) animator.SetLayerWeight(handLayer, 0f);
            }
        }
    }
}
