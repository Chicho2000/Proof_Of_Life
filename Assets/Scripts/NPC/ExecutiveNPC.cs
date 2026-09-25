using System.Collections.Generic;
using UnityEngine;

public class ExecutiveNPC : NPCBase
{
    [Header("Configuración de Huida")]
    [SerializeField] private float fleeSpeed = 5.0f;
    [SerializeField] private List<Transform> fleePoints = new List<Transform>();
    [SerializeField] private float reachThreshold = 1.0f;

    private int currentPointIndex = 0;

    protected override void OnEnable()
    {
        base.OnEnable();
        AlarmManager.OnAlarmTriggered += HandleAlarm;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        AlarmManager.OnAlarmTriggered -= HandleAlarm;
    }

    private void Update()
    {
        if (currentState == NPCState.Dead)
        {
            return;
        }

        if (currentState == NPCState.Flee)
        {
            MoveAlongFleePoints();
        }

        UpdateAnimation();
    }

    private void HandleAlarm(Transform intruder)
    {
        if (currentState == NPCState.Dead)
        {
            return;
        }

        SetState(NPCState.Flee);

        if (agent != null && agent.enabled)
        {
            agent.speed = fleeSpeed;
        }

        if (fleePoints.Count > 0)
        {
            currentPointIndex = 0;
            agent.SetDestination(fleePoints[currentPointIndex].position);
        }
    }

    private void MoveAlongFleePoints()
    {
        if (fleePoints.Count == 0 || agent == null || !agent.enabled)
        {
            return;
        }

        // Cuando llega al punto actual, pasa al siguiente en bucle
        if (!agent.pathPending && agent.remainingDistance <= reachThreshold)
        {
            currentPointIndex = (currentPointIndex + 1) % fleePoints.Count;
            agent.SetDestination(fleePoints[currentPointIndex].position);
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        float speed = 0.0f;
        if (agent != null && agent.velocity.magnitude > 0.1f)
        {
            speed = (currentState == NPCState.Flee) ? 2.0f : 1.0f;
        }

        animator.SetFloat("SpeedY", speed);
    }
}
