using UnityEngine;
using System;

public class PlayerDisguiseSystem : MonoBehaviour
{
    public enum DisguiseType
    {
        None,
        GuardDisguise,
        EliteGuardDisguise
        // Se pueden agregar más tipos aquí a futuro: Waiter, Executive, etc.
    }

    [Header("Estado Actual")]
    [SerializeField] private DisguiseType currentDisguise = DisguiseType.None;

    public event Action<DisguiseType> OnDisguiseChanged;
    public static event Action<DisguiseType> OnDisguiseChangedStatic;

    public DisguiseType CurrentDisguise
    {
        get { return currentDisguise; }
    }

    [Header("Audio")]
    [SerializeField] private AudioClip changeDisguiseSound;

    public void EquipDisguise(DisguiseType newDisguise)
    {
        if (currentDisguise == newDisguise)
            return;

        currentDisguise = newDisguise;

        if (changeDisguiseSound != null)
            AudioSource.PlayClipAtPoint(changeDisguiseSound, transform.position);

        Debug.Log($"[PlayerDisguiseSystem] Disfraz cambiado a: {newDisguise}");
        OnDisguiseChanged?.Invoke(currentDisguise);
        OnDisguiseChangedStatic?.Invoke(currentDisguise);
    }
}
