using UnityEngine;

/// <summary>
/// Objetivo de misión que requiere eliminar o neutralizar a un objetivo NPC específico (ej: el Ejecutivo).
/// </summary>
public class TargetObjective : MissionObjective
{
    [Header("Objetivo NPC")]
    [Tooltip("El componente NPCHealth del objetivo a eliminar.")]
    [SerializeField] private NPCHealth targetHealth;

    [Tooltip("Nombre de referencia del objetivo para la descripción")]
    [SerializeField] private string targetName = "el Ejecutivo";

    private void Awake()
    {
        if (string.IsNullOrEmpty(objectiveTitle) || objectiveTitle == "Objetivo de Misión")
        {
            objectiveTitle = $"Eliminar a {targetName}";
        }

        if (string.IsNullOrEmpty(objectiveDescription) || objectiveDescription == "Descripción del objetivo a cumplir.")
        {
            objectiveDescription = $"Localiza y neutraliza a {targetName} sin levantar sospechas.";
        }
    }

    private void OnEnable()
    {
        if (targetHealth != null)
        {
            targetHealth.OnDeath += HandleTargetDeath;
        }
    }

    private void OnDisable()
    {
        if (targetHealth != null)
        {
            targetHealth.OnDeath -= HandleTargetDeath;
        }
    }

    private void Start()
    {
        if (targetHealth == null)
        {
            // Intentar encontrar el ejecutivo en la escena si no se asignó
            ExecutiveNPC executive = FindFirstObjectByType<ExecutiveNPC>();
            if (executive != null)
            {
                targetHealth = executive.GetComponent<NPCHealth>();
                if (targetHealth != null)
                {
                    targetHealth.OnDeath += HandleTargetDeath;
                }
            }
        }

        if (targetHealth != null && targetHealth.IsDead)
        {
            CompleteObjective();
        }
    }

    private void HandleTargetDeath()
    {
        CompleteObjective();
    }
}
