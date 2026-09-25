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
    protected Animator animator;

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

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<NPCHealth>();
        animator = GetComponentInChildren<Animator>();
    }

    protected virtual void Start()
    {
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
}
