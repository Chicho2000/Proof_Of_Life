using UnityEngine;

public class GuardDisguise : MonoBehaviour
{
    [Header("Disguise Settings")]
    public PlayerDisguiseSystem.DisguiseType disguiseType = PlayerDisguiseSystem.DisguiseType.GuardDisguise;

    [Header("Audio")]
    [SerializeField] private AudioClip equipSound;

    public bool canBeStolen = true;

    private void Awake()
    {
        if (GetComponent<EliteGuardNPC>() != null)
        {
            disguiseType = PlayerDisguiseSystem.DisguiseType.EliteGuardDisguise;
        }
    }

    public void StealDisguise(GameObject interactor)
    {
        if (!canBeStolen) return;

        PlayerDisguiseSystem disguiseSystem = interactor.GetComponent<PlayerDisguiseSystem>();
        if (disguiseSystem != null)
        {
            if (GetComponent<EliteGuardNPC>() != null)
            {
                disguiseType = PlayerDisguiseSystem.DisguiseType.EliteGuardDisguise;
            }

            disguiseSystem.EquipDisguise(disguiseType);

            if (equipSound != null)
            {
                AudioSource.PlayClipAtPoint(equipSound, transform.position);
            }

            canBeStolen = false;
            Debug.Log($"[GuardDisguise] Disfraz ({disguiseType}) robado del cuerpo.");
        }
    }
}
