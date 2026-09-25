using System;
using UnityEngine;

/// <summary>
/// Clase base para todos los objetivos de misión del juego.
/// Proporciona estado de completitud, descripción y eventos desacoplados para UI y gestión.
/// </summary>
public abstract class MissionObjective : MonoBehaviour
{
    [Header("Identificación del Objetivo")]
    [SerializeField] protected string objectiveTitle = "Objetivo de Misión";
    [TextArea(2, 4)]
    [SerializeField] protected string objectiveDescription = "Descripción del objetivo a cumplir.";
    [SerializeField] protected bool isOptional = false;

    public string ObjectiveTitle => objectiveTitle;
    public string ObjectiveDescription => objectiveDescription;
    public bool IsOptional => isOptional;
    public bool IsComplete { get; protected set; }

    /// <summary>
    /// Evento disparado cada vez que el estado del objetivo cambia (completado o reiniciado).
    /// </summary>
    public event Action<MissionObjective> OnObjectiveStateChanged;

    /// <summary>
    /// Marca el objetivo como completado y notifica a los oyentes.
    /// </summary>
    public virtual void CompleteObjective()
    {
        if (IsComplete) return;

        IsComplete = true;
        Debug.Log($"🎯 <color=#4CAF50><b>[Misión] Objetivo Completado:</b></color> {objectiveTitle}");
        OnObjectiveStateChanged?.Invoke(this);
    }

    /// <summary>
    /// Restablece el objetivo a no completado.
    /// </summary>
    public virtual void SetIncomplete()
    {
        if (!IsComplete) return;

        IsComplete = false;
        Debug.Log($"⚠️ <color=#FF9800><b>[Misión] Objetivo Incompleto:</b></color> {objectiveTitle}");
        OnObjectiveStateChanged?.Invoke(this);
    }
}
