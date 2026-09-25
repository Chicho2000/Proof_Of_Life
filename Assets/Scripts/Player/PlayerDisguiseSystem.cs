using UnityEngine;
using System;

public class PlayerDisguiseSystem : MonoBehaviour
{
    public enum DisguiseType
    {
        None,
        GuardDisguise
        // Se pueden agregar más tipos aquí a futuro: Waiter, Executive, etc.
    }

    [Header("Estado Actual")]
    [SerializeField] private DisguiseType currentDisguise = DisguiseType.None;

    public event Action<DisguiseType> OnDisguiseChanged;

    public DisguiseType CurrentDisguise => currentDisguise;

    public void EquipDisguise(DisguiseType newDisguise)
    {
        currentDisguise = newDisguise;
        Debug.Log($"[PlayerDisguiseSystem] Disfraz cambiado a: {newDisguise}");
        OnDisguiseChanged?.Invoke(currentDisguise);
    }
}
