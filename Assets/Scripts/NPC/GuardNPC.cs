using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(DetectionSystem))]
public class GuardNPC : NPCBase
{
    private const float AttackGestureDuration = 0.4f;

    [Header("Configuración de Patrulla")]
    [SerializeField] private Transform[] patrolPoints = new Transform[0];
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtPatrolPoint = 2f;
    [SerializeField] private float waypointReachDistance = 0.5f;
    [SerializeField] private bool loopPatrol = true;
    [SerializeField] private int currentPatrolIndex;

    [Header("Configuración de Persecución")]
    [SerializeField] protected float chaseSpeed = 4.8f;

    [Header("Configuración de Ataque")]
    [SerializeField] protected int attackDamage = 25;
    [SerializeField] protected float attackRange = 1.8f;
    [SerializeField] private float attackRate = 1.2f;
    [SerializeField] private AudioClip attackSound;

    [Header("Configuración de Detección y Sospecha")]
    [SerializeField] private DetectionSystem detectionSystem;
    [SerializeField] private NPCSuspicionSystem suspicionSystem;
    [SerializeField, Min(0f)] private float armedImmediateAlertRange = 4f;

    [Header("Investigacion de ruido")]
    [Min(0f)] [SerializeField] private float investigateDuration = 5f;

    [Header("Configuración de Alerta")]
    [SerializeField] private float alertPatrolSpeed = 3.2f;

    public static event System.Action<GuardNPC, Vector3> OnDeadBodyDiscovered;

    private Transform playerTarget;
    private PlayerHealth playerHealth;
    private float lastAttackTime = -999f;
    private float patrolWaitTimer;
    private bool hasPatrolDestination;
    private bool isWaitingAtPatrolPoint;
    private float nextDoorCheckTime;
    private readonly HashSet<BodyInteractable> discoveredBodies = new HashSet<BodyInteractable>();


    protected override void Awake()
    {
        base.Awake();

        if (detectionSystem == null)
        {
            detectionSystem = GetComponent<DetectionSystem>();
        }

        if (suspicionSystem == null)
        {
            suspicionSystem = GetComponent<NPCSuspicionSystem>();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        AlarmManager.OnAlarmTriggered += HandleAlarm;
        AlarmManager.OnAlarmStopped += HandleAlarmStopped;
        NoiseEmitter.OnNoiseEmitted += HandleNoise;
        GuardNPC.OnDeadBodyDiscovered += HandleDeadBodyDiscovered;

        if (suspicionSystem != null)
        {
            suspicionSystem.OnSuspicionLevel2Reached += HandleSuspicionLevel2;
        }
    }

    protected override void OnDisable()
    {
        AlarmManager.OnAlarmTriggered -= HandleAlarm;
        AlarmManager.OnAlarmStopped -= HandleAlarmStopped;
        NoiseEmitter.OnNoiseEmitted -= HandleNoise;
        GuardNPC.OnDeadBodyDiscovered -= HandleDeadBodyDiscovered;

        if (suspicionSystem != null)
        {
            suspicionSystem.OnSuspicionLevel2Reached -= HandleSuspicionLevel2;
        }

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
        if (Time.time >= nextDoorCheckTime)
        {
            OpenNearbyDoors();
            nextDoorCheckTime = Time.time + 0.25f;
        }
        UpdateAnimation();
    }

    private void TickState()
    {
        switch (currentState)
        {
            case NPCState.Idle:
                TickIdle();
                break;

            case NPCState.Patrol:
                TickPatrol(patrolSpeed);
                break;

            case NPCState.Alert:
                TickAlert();
                break;

            case NPCState.Investigate:
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

    protected override void TickIdle()
    {
        TickReturnToSpawn(patrolSpeed);
    }

    private void TickAlert()
    {
        if (HasPatrolPoints())
        {
            TickPatrol(alertPatrolSpeed);
        }
        else
        {
            TickIdle();
        }
    }

    private void TickPatrol(float currentSpeed = -1f)
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

        float speedToUse = currentSpeed > 0f ? currentSpeed : patrolSpeed;

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
            agent.speed = speedToUse;
            agent.stoppingDistance = Mathf.Max(0f, waypointReachDistance);
            hasPatrolDestination = agent.SetDestination(currentPoint.position);
            return;
        }

        if (agent.pathPending)
        {
            return;
        }

        float reachDistance = Mathf.Max(waypointReachDistance, agent.stoppingDistance);
        if (agent.hasPath && agent.remainingDistance <= reachDistance)
        {
            isWaitingAtPatrolPoint = true;
            patrolWaitTimer = 0f;
            agent.isStopped = true;
        }
    }

    private void TickDetection()
    {
        CheckForArmedPlayer();
        CheckForDeadBodies();
    }

    private void CheckForArmedPlayer()
    {
        if (currentState == NPCState.Dead || playerTarget == null || playerHealth == null
            || playerHealth.IsDead || detectionSystem == null || suspicionSystem == null
            || BodyInteractable.IsDraggingAnyBody
            || armedImmediateAlertRange <= 0f || (AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive)) return;

        PlayerHotbar hotbar = playerTarget.GetComponent<PlayerHotbar>();
        ItemInteractable item = hotbar != null ? hotbar.GetSelectedItem() : null;
        if (item == null || (item.ItemType != ItemType.SilencedPistol && item.ItemType != ItemType.FiberWire)) return;

        if (Vector3.Distance(transform.position, playerTarget.position) <= armedImmediateAlertRange
            && detectionSystem.CanDetectTarget(playerTarget, true))
        {
            suspicionSystem.TriggerInstantAlert();
        }
    }

    private void CheckForDeadBodies()
    {
        if (currentState == NPCState.Dead || currentState == NPCState.Chase || currentState == NPCState.Attack)
        {
            return;
        }

        for (int i = 0; i < BodyInteractable.ActiveBodies.Count; i++)
        {
            BodyInteractable body = BodyInteractable.ActiveBodies[i];
            if (CanDiscoverBody(body) && CanSeeBody(body))
            {
                discoveredBodies.Add(body);
                body.IsDiscovered = true;
                body.LastDiscoveredTime = Time.time;
                body.InvestigatingGuard = this;
                Debug.Log($"[GuardNPC] {gameObject.name} descubrió un cuerpo en el suelo ({body.NPCName})! Entrando en SOSPECHA Y ALERTA.");
                Vector3 bodyPos = body.GetVisualPosition();

                if (OnDeadBodyDiscovered != null)
                {
                    OnDeadBodyDiscovered(this, bodyPos);
                }
                break;
            }
        }
    }

    private bool CanSeeBody(BodyInteractable body)
    {
        if (body == null || body.IsHidden || !body.gameObject.activeInHierarchy || detectionSystem == null)
        {
            return false;
        }

        Vector3 baseBodyPos = body.GetVisualPosition();
        Vector3 bodyPos = baseBodyPos + Vector3.up * 0.35f;
        Transform detOrigin = detectionSystem.DetectionOrigin;
        Vector3 originPos = (detOrigin != null && detOrigin != transform) ? detOrigin.position : transform.position + Vector3.up * 1.6f;
        Vector3 toBody = bodyPos - originPos;
        float dist = toBody.magnitude;

        if (dist > detectionSystem.DetectionRange)
        {
            return false;
        }

        // Verificación de ángulo horizontal
        Vector3 hDir = Vector3.ProjectOnPlane(toBody, Vector3.up);
        if (hDir.sqrMagnitude > 0.001f)
        {
            float angle = Vector3.Angle(transform.forward, hDir.normalized);
            float hDist = hDir.magnitude;
            bool inCloseFront = hDist <= 3.5f && angle <= 95f;
            float halfAngle = Mathf.Max(detectionSystem.DetectionAngle * 0.5f, 55f);
            if (!inCloseFront && angle > halfAngle)
            {
                return false;
            }
        }

        // Raycast de línea de visión hacia el cuerpo
        return HasLineOfSightToBody(originPos, bodyPos, body)
            || HasLineOfSightToBody(originPos, baseBodyPos + Vector3.up * 0.8f, body)
            || ((baseBodyPos - body.transform.position).sqrMagnitude > 0.05f && HasLineOfSightToBody(originPos, body.transform.position + Vector3.up * 0.45f, body));
    }

    private bool CanDiscoverBody(BodyInteractable body)
    {
        if (body == null || body.IsHidden || !body.gameObject.activeInHierarchy)
        {
            return false;
        }

        return !discoveredBodies.Contains(body);
    }

    private bool HasLineOfSightToBody(Vector3 originPos, Vector3 targetPoint, BodyInteractable body)
    {
        Vector3 dir = targetPoint - originPos;
        float dist = dir.magnitude;
        if (dist <= 0.05f)
        {
            return true;
        }

        RaycastHit[] hits = Physics.RaycastAll(originPos, dir / dist, dist, detectionSystem.ObstacleMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform == null
                || hit.transform == transform || hit.transform.IsChildOf(transform)
                || hit.transform == body.transform || hit.transform.IsChildOf(body.transform)
                || hit.transform.GetComponentInParent<BodyInteractable>() != null
                || hit.transform.GetComponentInParent<NPCBase>() != null
                || hit.transform.GetComponentInParent<PlayerHealth>() != null
                || hit.transform.GetComponentInParent<CharacterController>() != null
                || hit.distance >= dist - 0.6f)
            {
                continue;
            }
            return false;
        }
        return true;
    }

    private bool CheckDirectHitFallback(Vector3 originPos, Vector3 toBody, float dist, BodyInteractable body)
    {
        RaycastHit hit;
        if (Physics.Raycast(originPos, toBody.normalized, out hit, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == body.transform || hit.transform.IsChildOf(body.transform))
            {
                return true;
            }
            if (hit.distance >= dist - 0.5f)
            {
                return true;
            }
            return false;
        }

        return true;
    }

    private void HandleDeadBodyDiscovered(GuardNPC discoverer, Vector3 bodyPosition)
    {
        if (currentState == NPCState.Dead || currentState == NPCState.Chase || currentState == NPCState.Attack)
        {
            return;
        }

        if (discoverer != this && Vector3.Distance(transform.position, bodyPosition) > 10f) return;

        if (currentState != NPCState.Investigate && currentState != NPCState.Suspicious)
        {
            SetState(NPCState.Alert);
        }

        if (suspicionSystem != null)
        {
            suspicionSystem.SetReturnState(NPCState.Alert);
        }

        // Los guardias cercanos abandonan cualquier investigacion de ruido para revisar el cuerpo.
        if (suspicionSystem != null)
        {
            suspicionSystem.StartInvestigation(bodyPosition, 5.0f, 1.0f);
        }
    }

    private void HandleSuspicionLevel2()
    {
        if (currentState == NPCState.Dead)
        {
            return;
        }

        Debug.Log($"[GuardNPC] {gameObject.name} sospecha colmada (Nivel 2 - Alerta). Activando alarma y persecución.", this);
        if (AlarmManager.Instance != null)
        {
            AlarmManager.Instance.TriggerAlarm();
        }
        StartChasingPlayer();
    }

    private void HandleNoise(Vector3 position, float radius)
    {
        if (currentState != NPCState.Idle && currentState != NPCState.Patrol
            && currentState != NPCState.Alert
            && currentState != NPCState.Suspicious && currentState != NPCState.Investigate)
        {
            return;
        }

        if (AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive) return;
        if (Vector3.Distance(transform.position, position) > radius) return;

        if (suspicionSystem != null)
        {
            suspicionSystem.StartInvestigation(position, investigateDuration);
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
                if (CanUseAgent())
                {
                    agent.speed = patrolSpeed;
                    agent.stoppingDistance = 0f;
                }
                break;

            case NPCState.Patrol:
                EnterPatrolState();
                break;

            case NPCState.Alert:
                ResetPatrolTick();
                if (CanUseAgent())
                {
                    agent.isStopped = false;
                    agent.speed = alertPatrolSpeed;
                    agent.stoppingDistance = 0f;
                }
                break;

            case NPCState.Suspicious:
                ResetPatrolTick();
                StopAgent();
                break;

            case NPCState.Investigate:
                ResetPatrolTick();
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
            agent.ResetPath();
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

    public override bool HasPatrolPoints()
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

    private void HandleAlarmStopped()
    {
        if (currentState == NPCState.Dead)
        {
            return;
        }

        Debug.Log($"[GuardNPC] {gameObject.name}: Alarma detenida. Regresando a patrullaje.");
        InitializePatrolState();
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

    protected virtual void ExecuteAttack()
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

        playerHealth.TakeDamage(attackDamage, transform.position);
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
        if (animators == null)
        {
            return;
        }

        float speed = 0f;
        bool isMoving = CanUseAgent() && !agent.isStopped && agent.velocity.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            speed = currentState == NPCState.Chase ? 2f : 1f;
        }

        float gestureProgress = (Time.time - lastAttackTime) / AttackGestureDuration;
        float gestureWeight = currentState != NPCState.Dead && gestureProgress >= 0f && gestureProgress < 1f
            ? Mathf.Sin(gestureProgress * Mathf.PI)
            : 0f;

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
            {
                animators[i].SetFloat("SpeedX", 0f);
                animators[i].SetFloat("SpeedY", speed);

                int handLayer = animators[i].GetLayerIndex("Hand layer");
                if (handLayer >= 0)
                {
                    animators[i].SetLayerWeight(handLayer, gestureWeight);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (currentState == NPCState.Investigate)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1.0f);
        }

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
