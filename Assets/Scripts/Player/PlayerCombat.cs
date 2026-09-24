using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerHotbar playerHotbar;

    [Header("Configuracion de Pistola Silenciada")]
    [SerializeField] private float range = 50f;
    [SerializeField] private int damage = 100;

    [Header("Municion")]
    [SerializeField] private int maxAmmo = 12;
    [SerializeField] private int currentAmmo = 12;

    private Camera playerCamera;

    private void Awake()
    {
        playerCamera = Camera.main;

        if (playerHotbar == null)
        {
            playerHotbar = GetComponent<PlayerHotbar>();
        }

        currentAmmo = maxAmmo;
    }

    private void Update()
    {
        // Disparar con Click Izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            if (CanShootSilencedPistol())
            {
                Shoot();
            }
            else if (currentAmmo <= 0 && IsSilencedPistolEquipped())
            {
                Debug.Log("Pistola Silenciada: ¡Sin municion! Presiona R para recargar.");
            }
        }

        // Recargar con tecla R
        if (Input.GetKeyDown(KeyCode.R) && IsSilencedPistolEquipped())
        {
            Reload();
        }
    }

    private bool IsSilencedPistolEquipped()
    {
        if (playerHotbar == null)
        {
            return false;
        }

        ItemData currentItem = playerHotbar.GetSelectedItem();
        if (currentItem != null && currentItem.ItemType == ItemType.SilencedPistol)
        {
            return true;
        }

        return false;
    }

    private bool CanShootSilencedPistol()
    {
        if (currentAmmo <= 0)
        {
            return false;
        }

        return IsSilencedPistolEquipped();
    }

    private void Shoot()
    {
        currentAmmo--;

        Debug.Log("Pistola Silenciada: ¡Disparo realizado! Balas restantes: " + currentAmmo + "/" + maxAmmo);

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.green, 1.0f);
            Debug.Log("Pistola Silenciada: Impacto en " + hit.collider.name);

            NPCHealth npcHealth = hit.collider.GetComponentInParent<NPCHealth>();
            if (npcHealth != null)
            {
                Debug.Log("Pistola Silenciada: Impacto confirmado en NPC -> " + hit.collider.name);

                // 👇 ESTA ES LA LÍNEA QUE APLICARÁ EL DAÑO (borrar comentarios al estar el daño ya implementado):
                // npcHealth.TakeDamage(damage);

            }
        }
        else
        {
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * range, Color.red, 1.0f);
        }
    }

    public void Reload()
    {
        if (currentAmmo == maxAmmo)
        {
            return;
        }

        currentAmmo = maxAmmo;
        Debug.Log("Pistola Silenciada: Recargada. Municion completa: " + currentAmmo + "/" + maxAmmo);
    }

    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    public int GetMaxAmmo()
    {
        return maxAmmo;
    }
}
