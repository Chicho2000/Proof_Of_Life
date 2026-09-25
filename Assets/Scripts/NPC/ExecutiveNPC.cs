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

    private int currentPointIndex;
    private bool hasCurrentDestination;
    private bool hasEscaped;

    protected override void OnEnable()
    {
        base.OnEnable();
        AlarmManager.OnAlarmTriggered += HandleAlarm;
    }

    protected override void OnDisable()
    {
        AlarmManager.OnAlarmTriggered -= HandleAlarm;
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

        fleePoints.RemoveAll(point => point == null);
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

        fleePoints.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
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
