using UnityEngine;

public class GuardDisguise : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip equipSound;

    public bool canBeStolen = true;

    public void StealDisguise(GameObject interactor)
    {
        if (!canBeStolen) return;

        PlayerDisguiseSystem disguiseSystem = interactor.GetComponent<PlayerDisguiseSystem>();
        if (disguiseSystem != null)
        {
            disguiseSystem.EquipDisguise(PlayerDisguiseSystem.DisguiseType.GuardDisguise);

            if (equipSound != null)
            {
                AudioSource.PlayClipAtPoint(equipSound, transform.position);
            }

            canBeStolen = false;
            Debug.Log("[GuardDisguise] Disfraz de guardia robado del cuerpo.");
        }
    }
}
