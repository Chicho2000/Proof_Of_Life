using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ExecutiveNPC : NPCBase
{
    private const string FleeWaypointsContainerName = "Flee_Waypoints";

    [Header("Configuración de Huida")]
    [SerializeField] private float fleeSpeed = 5f;
    [SerializeField] private List<Transform> fleePoints = new List<Transform>();
    [SerializeField] private float reachThreshold = 1.2f;

    [Header("Misión")]
    [Tooltip("Si está activo, el escape de este NPC hará fracasar la misión (para misiones de asesinato donde es el VIP objetivo).")]
    [SerializeField] private bool failMissionOnEscape = false;

    private int currentPointIndex;
    private bool hasCurrentDestination;
    private bool hasEscaped;

    protected override void OnEnable()
    {
        base.OnEnable();
        AlarmManager.OnAlarmTriggered += HandleAlarm;
        AlarmManager.OnAlarmStopped += HandleAlarmStopped;
    }

    protected override void OnDisable()
    {
        AlarmManager.OnAlarmTriggered -= HandleAlarm;
        AlarmManager.OnAlarmStopped -= HandleAlarmStopped;
        base.OnDisable();
    }

    protected override void Start()
    {
        EnsureNavMeshPlacement();
        LoadFleePointsIfEmpty();

        currentState = NPCState.Idle;
        base.Start();

        if (AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive)
        {
            SetState(NPCState.Flee);
        }
    }

    private void Update()
    {
        TickState();
        UpdateAnimation();
    }

    private void TickState()
    {
        switch (currentState)
        {
            case NPCState.Idle:
                break;

            case NPCState.Flee:
                TickFlee();
                break;

            case NPCState.Dead:
                break;
        }
    }

    private void TickFlee()
    {
        if (hasEscaped || !CanUseAgent() || fleePoints.Count == 0)
        {
            return;
        }

        if (currentPointIndex >= fleePoints.Count)
        {
            Escape();
            return;
        }

        Transform currentPoint = fleePoints[currentPointIndex];
        if (currentPoint == null)
        {
            AdvanceToNextPoint();
            return;
        }

        if (!hasCurrentDestination)
        {
            agent.isStopped = false;
            agent.speed = fleeSpeed;
            hasCurrentDestination = agent.SetDestination(currentPoint.position);
            return;
        }

        if (agent.pathPending)
        {
            return;
        }

        float arrivalDistance = Mathf.Max(agent.stoppingDistance, reachThreshold);
        if (agent.remainingDistance <= arrivalDistance)
        {
            AdvanceToNextPoint();
        }
    }

    private void AdvanceToNextPoint()
    {
        bool reachedLastPoint = currentPointIndex >= fleePoints.Count - 1;
        if (reachedLastPoint)
        {
            Escape();
            return;
        }

        currentPointIndex++;
        hasCurrentDestination = false;
    }

    private void HandleAlarm()
    {
        if (hasEscaped || currentState == NPCState.Dead)
        {
            return;
        }

        SetState(NPCState.Flee);
    }

    private void HandleAlarmStopped()
    {
        if (hasEscaped || currentState == NPCState.Dead)
        {
            return;
        }

        Debug.Log($"[ExecutiveNPC] {gameObject.name}: Alarma detenida. Deteniendo huida.");
        SetState(NPCState.Idle);
    }

    protected override void OnStateChanged(NPCState newState)
    {
        base.OnStateChanged(newState);

        switch (newState)
        {
            case NPCState.Idle:
                StopAgent();
                break;

            case NPCState.Flee:
                EnterFleeState();
                break;

            case NPCState.Dead:
                StopAgent();
                break;
        }
    }

    private void EnterFleeState()
    {
        EnsureNavMeshPlacement();
        LoadFleePointsIfEmpty();

        currentPointIndex = 0;
        hasCurrentDestination = false;

        if (fleePoints.Count == 0)
        {
            Debug.LogWarning(
                $"[ExecutiveNPC] {gameObject.name} no tiene puntos de huida. " +
                $"Asigná Flee Points en el Inspector o creá '{FleeWaypointsContainerName}'."
            );
            return;
        }

        if (!CanUseAgent())
        {
            Debug.LogWarning($"[ExecutiveNPC] {gameObject.name} no está ubicado sobre un NavMesh válido.");
            return;
        }

        agent.speed = fleeSpeed;
        agent.isStopped = false;
    }

    private void LoadFleePointsIfEmpty()
    {
        if (fleePoints == null)
        {
            fleePoints = new List<Transform>();
        }

        for (int i = fleePoints.Count - 1; i >= 0; i--)
        {
            if (fleePoints[i] == null)
            {
                fleePoints.RemoveAt(i);
            }
        }

        if (fleePoints.Count > 0)
        {
            return;
        }

        GameObject container = GameObject.Find(FleeWaypointsContainerName);
        if (container == null)
        {
            return;
        }

        foreach (Transform child in container.transform)
        {
            fleePoints.Add(child);
        }

        fleePoints.Sort(CompareTransformNames);
    }

    private static int CompareTransformNames(Transform a, Transform b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;
        return string.CompareOrdinal(a.name, b.name);
    }

    private void EnsureNavMeshPlacement()
    {
        if (agent == null || !agent.enabled || agent.isOnNavMesh)
        {
            return;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    private bool CanUseAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void StopAgent()
    {
        hasCurrentDestination = false;

        if (!CanUseAgent())
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
        agent.speed = 3.5f;
        agent.stoppingDistance = 0f;
    }

    protected override void HandleDeath()
    {
        if (hasEscaped)
        {
            return;
        }

        base.HandleDeath();
        StopAgent();
    }

    private void Escape()
    {
        if (hasEscaped || currentState == NPCState.Dead)
        {
            return;
        }

        hasEscaped = true;
        StopAgent();

        Debug.Log($"[ExecutiveNPC] {gameObject.name} llegó al último punto y escapó.");

        // Solo falla la misión si este NPC en particular tiene activada la condición de VIP objetivo
        if (failMissionOnEscape && MissionManager.Instance != null && !MissionManager.Instance.IsMissionCompleted)
        {
            MissionManager.Instance.FailMissionTargetEscape();
        }

        gameObject.SetActive(false);
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        float speed = CanUseAgent() && agent.velocity.sqrMagnitude > 0.01f ? 2f : 0f;
        animator.SetFloat("SpeedY", speed);
    }
}
