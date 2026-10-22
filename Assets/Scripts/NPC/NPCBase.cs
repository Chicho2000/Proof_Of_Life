using UnityEngine;
using UnityEngine.AI;

public abstract class NPCBase : MonoBehaviour
{
    [Header("Estado del NPC")]
    [Tooltip("Estado con el que iniciará el NPC (configurable desde el Inspector)")]
    [SerializeField] protected NPCState currentState = NPCState.Idle;

    [Header("Componentes Base")]
    protected NavMeshAgent agent;
    protected NPCHealth health;
    protected Animator[] animators;

    [Header("Retorno a Posición Inicial (Sin Patrulla)")]
    [SerializeField] protected float returnToSpawnSpeed = 2f;
    [SerializeField] protected float returnStoppingDistance = 0.3f;

    protected Vector3 spawnPosition;
    protected Quaternion spawnRotation;
    protected bool hasSpawnPosition = false;

    public NPCState GetCurrentState()
    {
        return currentState;
    }

    public NavMeshAgent GetAgent()
    {
        return agent;
    }

    public NPCHealth GetHealth()
    {
        return health;
    }

    public Vector3 SpawnPosition
    {
        get { return spawnPosition; }
    }

    public Quaternion SpawnRotation
    {
        get { return spawnRotation; }
    }

    public virtual bool HasPatrolPoints()
    {
        return false;
    }

    protected bool CanUseAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<NPCHealth>();
        animators = GetComponentsInChildren<Animator>();
    }

    protected virtual void Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        hasSpawnPosition = true;

        // Aplica el estado seleccionado desde el Inspector al comenzar el juego
        SetState(currentState);
    }

    protected virtual void OnEnable()
    {
        if (health != null)
        {
            health.OnDeath += HandleDeath;
        }
    }

    protected virtual void OnDisable()
    {
        if (health != null)
        {
            health.OnDeath -= HandleDeath;
        }
    }

    public virtual void SetState(NPCState newState)
    {
        if (currentState == NPCState.Dead)
        {
            return;
        }

        currentState = newState;
        OnStateChanged(newState);
    }

    protected virtual void OnStateChanged(NPCState newState)
    {
    }

    protected virtual void HandleDeath()
    {
        currentState = NPCState.Dead;
    }

    protected virtual void TickIdle()
    {
        TickReturnToSpawn();
    }

    protected virtual void TickReturnToSpawn(float speed = -1f)
    {
        if (!hasSpawnPosition || HasPatrolPoints())
        {
            return;
        }

        if (!CanUseAgent())
        {
            return;
        }

        float currentSpeed = speed > 0f ? speed : returnToSpawnSpeed;
        float distToSpawn = Vector3.Distance(transform.position, spawnPosition);

        if (distToSpawn > 0.6f)
        {
            agent.isStopped = false;
            agent.speed = currentSpeed;
            agent.stoppingDistance = returnStoppingDistance;
            agent.SetDestination(spawnPosition);
        }
        else
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, spawnRotation, 180f * Time.deltaTime);
        }
    }
}
