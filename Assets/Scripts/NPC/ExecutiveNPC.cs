using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ExecutiveNPC : NPCBase
{
    [Header("Configuración de Huida")]
    [SerializeField] private float fleeSpeed = 5.0f;
    [SerializeField] private List<Transform> fleePoints = new List<Transform>();
    [SerializeField] private float reachThreshold = 1.2f;

    private int currentPointIndex = 0;
    private bool hasEscaped = false;
    private bool isFleeing = false;

    protected override void Awake()
    {
        base.Awake();

        // Asegurar que el componente NavMeshAgent esté asignado o creado
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
                agent.speed = fleeSpeed;
                agent.radius = 0.35f;
                agent.height = 2.0f;
                Debug.Log($"[ExecutiveNPC] NavMeshAgent añadido automáticamente a {gameObject.name}.");
            }
        }
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
        AutoFindFleePointsIfEmpty();

        if (currentState == NPCState.Flee)
        {
            StartFleeing();
        }
    }

    protected override void OnStateChanged(NPCState newState)
    {
        base.OnStateChanged(newState);

        if (newState == NPCState.Flee)
        {
            StartFleeing();
        }
        else
        {
            isFleeing = false;
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

    private void AutoFindFleePointsIfEmpty()
    {
        if (fleePoints == null)
        {
            fleePoints = new List<Transform>();
        }

        // Quitar entradas nulas
        fleePoints.RemoveAll(p => p == null);

        if (fleePoints.Count == 0)
        {
            // Intentar auto-vincular si el usuario creó el contenedor en la escena
            GameObject container = GameObject.Find("Puntos_Huida_Ejecutivo") 
                                ?? GameObject.Find("Puntos_Huida") 
                                ?? GameObject.Find("Waypoints_Ejecutivo") 
                                ?? GameObject.Find("Waypoints");

            if (container != null && container.transform.childCount > 0)
            {
                foreach (Transform child in container.transform)
                {
                    fleePoints.Add(child);
                }
                Debug.Log($"[ExecutiveNPC] Se auto-detectaron {fleePoints.Count} puntos de huida desde '{container.name}'.");
            }
        }
    }

    private void Update()
    {
        if (hasEscaped || currentState == NPCState.Dead)
        {
            return;
        }

        // Si el estado es Flee (ya sea por alarma o cambiado desde el Inspector en Play Mode)
        if (currentState == NPCState.Flee)
        {
            if (!isFleeing)
            {
                StartFleeing();
            }

            MoveAlongFleePoints();
        }
        else
        {
            isFleeing = false;
        }

        UpdateAnimation();
    }

    private void HandleAlarm(Transform intruder)
    {
        if (hasEscaped || currentState == NPCState.Dead)
        {
            return;
        }

        SetState(NPCState.Flee);
    }

    public void StartFleeing()
    {
        if (hasEscaped || currentState == NPCState.Dead)
        {
            return;
        }

        EnsureNavMeshPlacement();
        AutoFindFleePointsIfEmpty();

        if (fleePoints == null || fleePoints.Count == 0)
        {
            Debug.LogWarning($"⚠️ [ExecutiveNPC] ¡{gameObject.name} no tiene waypoints asignados en 'Flee Points'! Asigna los puntos en el Inspector o crea un objeto llamado 'Puntos_Huida_Ejecutivo' con puntos hijos.");
            return;
        }

        isFleeing = true;
        currentPointIndex = 0;

        if (agent != null && agent.enabled)
        {
            agent.speed = fleeSpeed;

            if (agent.isOnNavMesh && fleePoints[0] != null)
            {
                agent.isStopped = false;
                agent.SetDestination(fleePoints[0].position);
                Debug.Log($"🏃 [ExecutiveNPC] Huida iniciada hacia Punto 1: {fleePoints[0].name}");
            }
        }
    }

    private void MoveAlongFleePoints()
    {
        if (hasEscaped || fleePoints == null || fleePoints.Count == 0 || agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        // Si no tiene destino fijado o está detenido
        if (!agent.pathPending && !agent.hasPath)
        {
            if (currentPointIndex < fleePoints.Count && fleePoints[currentPointIndex] != null)
            {
                agent.isStopped = false;
                agent.SetDestination(fleePoints[currentPointIndex].position);
            }
        }

        // Comprobación de llegada al waypoint actual
        float threshold = Mathf.Max(reachThreshold, agent.stoppingDistance + 0.3f);

        if (!agent.pathPending)
        {
            // Llegó al punto actual si la distancia es menor al umbral
            if (agent.remainingDistance <= threshold)
            {
                Debug.Log($"✅ [ExecutiveNPC] Llegó a punto {currentPointIndex + 1}/{fleePoints.Count} ({fleePoints[currentPointIndex].name})");

                // ¿Era el último punto de huida?
                if (currentPointIndex >= fleePoints.Count - 1)
                {
                    EscapeAndTriggerAlarm();
                }
                else
                {
                    currentPointIndex++;
                    if (currentPointIndex < fleePoints.Count && fleePoints[currentPointIndex] != null)
                    {
                        agent.SetDestination(fleePoints[currentPointIndex].position);
                        Debug.Log($"🏃 [ExecutiveNPC] Dirigiéndose a siguiente punto ({currentPointIndex + 1}/{fleePoints.Count}): {fleePoints[currentPointIndex].name}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Al alcanzar el punto final de huida, el ejecutivo escapa definitivamente,
    /// activa la alarma general y se elimina sin dejar colliders ni objetos residuales (sin fantasma).
    /// </summary>
    public void EscapeAndTriggerAlarm()
    {
        if (hasEscaped) return;
        hasEscaped = true;

        Debug.Log($"🚨 [ExecutiveNPC] ¡{gameObject.name} llegó al punto final, escapó con éxito y activó la alarma general!");

        // 1. Activar la alarma general del juego
        AlarmManager alarm = AlarmManager.Instance;
        if (alarm == null)
        {
            alarm = FindFirstObjectByType<AlarmManager>();
            if (alarm == null)
            {
                GameObject alarmObj = new GameObject("AlarmManager");
                alarm = alarmObj.AddComponent<AlarmManager>();
            }
        }

        if (alarm != null)
        {
            alarm.TriggerAlarm();
        }

        // 2. Desactivar NavMeshAgent para liberar la ruta y agente de inmediato
        if (agent != null)
        {
            agent.enabled = false;
        }

        // 3. Desactivar todos los colliders inmediatamente para evitar colisiones invisibles o raycasts fantasma
        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in allColliders)
        {
            col.enabled = false;
        }

        // 4. Desactivar todas las mallas visuales (renderers)
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in allRenderers)
        {
            rend.enabled = false;
        }

        // 5. Desactivar el GameObject completo
        gameObject.SetActive(false);

        // 6. Destruir definitivamente el GameObject de la escena
        Destroy(gameObject);
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        float speed = 0.0f;
        if (agent != null && agent.enabled && agent.isOnNavMesh && agent.velocity.magnitude > 0.1f)
        {
            speed = (currentState == NPCState.Flee) ? 2.0f : 1.0f;
        }

        animator.SetFloat("SpeedY", speed);
    }
}
