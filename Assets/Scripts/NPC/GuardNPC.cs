using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(DetectionSystem))]
public class GuardNPC : NPCBase
{
    [Header("Configuración de Patrulla")]
    [SerializeField] private Transform[] patrolPoints = new Transform[0];
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtPatrolPoint = 2f;
    [SerializeField] private float waypointReachDistance = 0.5f;
    [SerializeField] private bool loopPatrol = true;
    [SerializeField] private int currentPatrolIndex;

    [Header("Configuración de Persecución")]
    [SerializeField] private float chaseSpeed = 4.8f;

    [Header("Configuración de Ataque")]
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackRate = 1.2f;
    [SerializeField] private AudioClip attackSound;

    [Header("Configuración de Detección")]
    [SerializeField] private DetectionSystem detectionSystem;

    private Transform playerTarget;
    private PlayerHealth playerHealth;
    private float lastAttackTime = -999f;
    private float patrolWaitTimer;
    private bool hasPatrolDestination;
    private bool isWaitingAtPatrolPoint;

    protected override void Awake()
    {
        base.Awake();

        if (detectionSystem == null)
        {
            detectionSystem = GetComponent<DetectionSystem>();
        }
    }

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
        FindPlayerReference();

        currentState = NPCState.Idle;
        base.Start();
        InitializePatrolState();

        if (AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive)
        {
            StartChasingPlayer();
        }
    }

    private void Update()
    {
        if (currentState == NPCState.Dead)
        {
            return;
        }

        FindPlayerReference();

        if (playerHealth != null && playerHealth.IsDead)
        {
            if (currentState != NPCState.Idle)
            {
                SetState(NPCState.Idle);
            }

            UpdateAnimation();
            return;
        }

        TickDetection();
        TickState();
        UpdateAnimation();
    }

    private void TickState()
    {
        switch (currentState)
        {
            case NPCState.Idle:
                break;

            case NPCState.Patrol:
                TickPatrol();
                break;

            case NPCState.Chase:
                TickChase();
                break;

            case NPCState.Attack:
                TickAttack();
                break;

            case NPCState.Dead:
                break;
        }
    }

    private void TickPatrol()
    {
        if (!HasPatrolPoints())
        {
            SetState(NPCState.Idle);
            return;
        }

        if (!CanUseAgent())
        {
            return;
        }

        Transform currentPoint = GetCurrentPatrolPoint();
        if (currentPoint == null)
        {
            AdvancePatrolPoint();
            return;
        }

        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= Mathf.Max(0f, waitTimeAtPatrolPoint))
            {
                AdvancePatrolPoint();
            }

            return;
        }

        if (!hasPatrolDestination)
        {
            agent.isStopped = false;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = Mathf.Max(0f, waypointReachDistance);
            hasPatrolDestination = agent.SetDestination(currentPoint.position);
            return;
        }

        if (agent.pathPending)
        {
            return;
        }

        float reachDistance = Mathf.Max(waypointReachDistance, agent.stoppingDistance);
        if (agent.remainingDistance <= reachDistance)
        {
            isWaitingAtPatrolPoint = true;
            patrolWaitTimer = 0f;
            agent.isStopped = true;
        }
    }

    private void TickDetection()
    {
        if (currentState != NPCState.Idle
            && currentState != NPCState.Patrol
            && currentState != NPCState.Suspicious
            && currentState != NPCState.Alert)
        {
            return;
        }

        AlarmManager alarmManager = AlarmManager.Instance;
        if (alarmManager == null
            || alarmManager.IsAlarmActive
            || detectionSystem == null
            || playerTarget == null
            || playerHealth == null
            || playerHealth.IsDead)
        {
            return;
        }

        if (detectionSystem.CanDetectTarget(playerTarget))
        {
            Debug.Log($"[GuardNPC] {gameObject.name} detectó al jugador y activó la alarma.", this);
            alarmManager.TriggerAlarm();
        }
    }

    private void TickChase()
    {
        if (playerTarget == null || !CanUseAgent())
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance <= attackRange)
        {
            SetState(NPCState.Attack);
            return;
        }

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange * 0.85f;
        agent.SetDestination(playerTarget.position);
    }

    private void TickAttack()
    {
        if (playerTarget == null)
        {
            SetState(NPCState.Idle);
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance > attackRange + 0.6f)
        {
            SetState(NPCState.Chase);
            return;
        }

        Vector3 targetDirection = playerTarget.position - transform.position;
        targetDirection.y = 0f;

        if (targetDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        if (Time.time >= lastAttackTime + attackRate)
        {
            ExecuteAttack();
        }
    }

    protected override void OnStateChanged(NPCState newState)
    {
        base.OnStateChanged(newState);

        switch (newState)
        {
            case NPCState.Idle:
                StopAgent();
                ResetPatrolTick();
                break;

            case NPCState.Patrol:
                EnterPatrolState();
                break;

            case NPCState.Chase:
                ResetPatrolTick();
                if (CanUseAgent())
                {
                    agent.isStopped = false;
                    agent.speed = chaseSpeed;
                    agent.stoppingDistance = attackRange * 0.85f;
                }
                break;

            case NPCState.Attack:
                StopAgent();
                break;

            case NPCState.Dead:
                StopAgent();
                break;
        }
    }

    private void InitializePatrolState()
    {
        currentPatrolIndex = FindNextValidPatrolIndex(0, false);

        if (currentPatrolIndex >= 0)
        {
            SetState(NPCState.Patrol);
        }
        else
        {
            currentPatrolIndex = 0;
            SetState(NPCState.Idle);
        }
    }

    private void EnterPatrolState()
    {
        if (!HasPatrolPoints())
        {
            SetState(NPCState.Idle);
            return;
        }

        if (GetCurrentPatrolPoint() == null)
        {
            currentPatrolIndex = FindNextValidPatrolIndex(0, false);
        }

        ResetPatrolTick();

        if (CanUseAgent())
        {
            agent.isStopped = false;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = Mathf.Max(0f, waypointReachDistance);
        }
    }

    private void AdvancePatrolPoint()
    {
        int nextIndex = FindNextValidPatrolIndex(currentPatrolIndex + 1, loopPatrol);
        if (nextIndex < 0)
        {
            SetState(NPCState.Idle);
            return;
        }

        currentPatrolIndex = nextIndex;
        ResetPatrolTick();

        if (CanUseAgent())
        {
            agent.isStopped = false;
        }
    }

    private int FindNextValidPatrolIndex(int startIndex, bool allowLoop)
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return -1;
        }

        for (int index = Mathf.Max(0, startIndex); index < patrolPoints.Length; index++)
        {
            if (patrolPoints[index] != null)
            {
                return index;
            }
        }

        if (!allowLoop)
        {
            return -1;
        }

        int loopLimit = Mathf.Min(startIndex, patrolPoints.Length);
        for (int index = 0; index < loopLimit; index++)
        {
            if (patrolPoints[index] != null)
            {
                return index;
            }
        }

        return -1;
    }

    private bool HasPatrolPoints()
    {
        return FindNextValidPatrolIndex(0, false) >= 0;
    }

    private Transform GetCurrentPatrolPoint()
    {
        if (patrolPoints == null
            || currentPatrolIndex < 0
            || currentPatrolIndex >= patrolPoints.Length)
        {
            return null;
        }

        return patrolPoints[currentPatrolIndex];
    }

    private void ResetPatrolTick()
    {
        patrolWaitTimer = 0f;
        hasPatrolDestination = false;
        isWaitingAtPatrolPoint = false;
    }

    private void HandleAlarm()
    {
        if (currentState == NPCState.Dead)
        {
            return;
        }

        Debug.Log($"[GuardNPC] {gameObject.name} escuchó la alarma general y persigue al jugador.");
        StartChasingPlayer();
    }

    public void StartChasingPlayer()
    {
        if (currentState == NPCState.Dead)
        {
            return;
        }

        FindPlayerReference();
        if (playerTarget == null)
        {
            return;
        }

        EnsureNavMeshPlacement();
        SetState(NPCState.Chase);

        if (CanUseAgent())
        {
            agent.SetDestination(playerTarget.position);
        }
    }

    private void ExecuteAttack()
    {
        lastAttackTime = Time.time;

        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        Debug.Log($"[GuardNPC] {gameObject.name} atacó al jugador. Daño: {attackDamage}");

        if (attackSound != null)
        {
            AudioSource.PlayClipAtPoint(attackSound, transform.position);
        }

        playerHealth.TakeDamage(attackDamage);
    }

    private void FindPlayerReference()
    {
        if (playerHealth != null)
        {
            return;
        }

        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerTarget = playerHealth.transform;
        }
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

        if (agent != null)
        {
            agent.enabled = false;
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        float speed = 0f;
        bool isMoving = CanUseAgent() && !agent.isStopped && agent.velocity.sqrMagnitude > 0.01f;

        if (isMoving && currentState == NPCState.Patrol)
        {
            speed = 1f;
        }
        else if (isMoving && currentState == NPCState.Chase)
        {
            speed = 2f;
        }

        animator.SetFloat("SpeedY", speed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.blue;
        Transform firstPoint = null;
        Transform previousPoint = null;

        foreach (Transform patrolPoint in patrolPoints)
        {
            if (patrolPoint == null)
            {
                continue;
            }

            Gizmos.DrawSphere(patrolPoint.position, 0.2f);

            if (firstPoint == null)
            {
                firstPoint = patrolPoint;
            }

            if (previousPoint != null)
            {
                Gizmos.DrawLine(previousPoint.position, patrolPoint.position);
            }

            previousPoint = patrolPoint;
        }

        if (loopPatrol && firstPoint != null && previousPoint != null && firstPoint != previousPoint)
        {
            Gizmos.DrawLine(previousPoint.position, firstPoint.position);
        }
    }
}
