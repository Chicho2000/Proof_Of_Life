using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GuardNPC : NPCBase
{
    [Header("Configuración de Persecución")]
    [SerializeField] private float chaseSpeed = 4.8f;
    [SerializeField] private float patrolSpeed = 2.0f;

    [Header("Configuración de Ataque")]
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackRate = 1.2f;
    [SerializeField] private AudioClip attackSound;

    private Transform playerTarget;
    private PlayerHealth playerHealth;
    private float lastAttackTime = -999f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    protected override void Awake()
    {
        base.Awake();

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
                agent.speed = chaseSpeed;
                agent.stoppingDistance = attackRange * 0.8f;
                agent.radius = 0.35f;
                agent.height = 2.0f;
            }
        }

        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

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

    protected override void Start()
    {
        base.Start();

        EnsureNavMeshPlacement();
        FindPlayerReference();

        // Si la alarma ya estaba sonando al arrancar, empezar a perseguir
        if (AlarmManager.Instance != null && AlarmManager.Instance.GetIsAlarmActive())
        {
            StartChasingPlayer();
        }
    }

    private void EnsureNavMeshPlacement()
    {
        if (agent != null && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }

    private void FindPlayerReference()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerTarget = playerHealth.transform;
            }
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
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
            }
            UpdateAnimation();
            return;
        }

        if (currentState == NPCState.Chase)
        {
            HandleChaseState();
        }
        else if (currentState == NPCState.Attack)
        {
            HandleAttackState();
        }

        UpdateAnimation();
    }

    private void HandleAlarm(Transform intruder)
    {
        if (currentState == NPCState.Dead)
        {
            return;
        }

        if (intruder != null)
        {
            playerTarget = intruder;
            playerHealth = intruder.GetComponent<PlayerHealth>();
        }

        Debug.Log($"🚨 [GuardNPC] {gameObject.name} escuchó la alarma general y sale a cazar al jugador.");
        StartChasingPlayer();
    }

    public void StartChasingPlayer()
    {
        if (currentState == NPCState.Dead) return;

        FindPlayerReference();
        if (playerTarget == null) return;

        EnsureNavMeshPlacement();
        SetState(NPCState.Chase);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = attackRange * 0.85f;
            agent.SetDestination(playerTarget.position);
        }
    }

    private void HandleChaseState()
    {
        if (playerTarget == null || agent == null || !agent.isOnNavMesh) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // Si está dentro del rango de ataque, pasar a atacar
        if (distance <= attackRange)
        {
            SetState(NPCState.Attack);
            agent.isStopped = true;
            return;
        }

        // Actualizar destino hacia la posición actual del jugador
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(playerTarget.position);
    }

    private void HandleAttackState()
    {
        if (playerTarget == null)
        {
            SetState(NPCState.Idle);
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // Si el jugador se alejó fuera del rango de ataque, reanudar persecución
        if (distance > attackRange + 0.6f)
        {
            SetState(NPCState.Chase);
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
            return;
        }

        // Girar para mirar de frente al jugador
        Vector3 targetDirection = (playerTarget.position - transform.position).normalized;
        targetDirection.y = 0f;
        if (targetDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDirection), Time.deltaTime * 10f);
        }

        // Ejecutar ataque si pasó el cooldown
        if (Time.time >= lastAttackTime + attackRate)
        {
            ExecuteAttack();
        }
    }

    private void ExecuteAttack()
    {
        lastAttackTime = Time.time;

        if (playerHealth != null && !playerHealth.IsDead)
        {
            Debug.Log($"⚔️ [GuardNPC] {gameObject.name} atacó al jugador. Daño infligido: {attackDamage}");

            if (attackSound != null)
            {
                AudioSource.PlayClipAtPoint(attackSound, transform.position);
            }

            playerHealth.TakeDamage(attackDamage);
        }
    }

    protected override void HandleDeath()
    {
        base.HandleDeath();

        if (agent != null)
        {
            agent.enabled = false;
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = 0.0f;
        if (agent != null && agent.enabled && agent.isOnNavMesh && agent.velocity.magnitude > 0.1f)
        {
            speed = (currentState == NPCState.Chase) ? 2.0f : 1.0f;
        }

        animator.SetFloat("SpeedY", speed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
