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

    [Header("Configuración de Cable de Fibra (FiberWire)")]
    [SerializeField] private float fiberWireRange = 2.2f;
    [SerializeField] private float behindAngleThreshold = 60f;
    [SerializeField] private AudioClip fiberWireKillSound;

    private Camera playerCamera;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        if (playerHotbar == null)
        {
            playerHotbar = GetComponent<PlayerHotbar>();
        }

        currentAmmo = maxAmmo;
    }

    private void Update()
    {
        // Disparar o Atacar con Click Izquierdo según el ítem equipado
        if (Input.GetMouseButtonDown(0))
        {
            if (IsSilencedPistolEquipped())
            {
                if (CanShootSilencedPistol())
                {
                    Shoot();
                }
                else if (currentAmmo <= 0)
                {
                    Debug.Log("Pistola Silenciada: ¡Sin munición! Presiona R para recargar.");
                }
            }
            else if (IsFiberWireEquipped())
            {
                TryFiberWireTakedown();
            }
            else
            {
                Debug.Log("[PlayerCombat] Para atacar necesitas tener seleccionada la Pistola Silenciada o el Cable de Fibra en la Hotbar (con teclas 1-5).");
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
            if (npcHealth == null)
            {
                // Auto-asignar NPCHealth si es un NPC de la escena que aún no tenía el componente en el inspector
                if (hit.collider.name.StartsWith("Guard") || hit.collider.name.Contains("Executive") || (hit.collider.transform.parent != null && hit.collider.transform.parent.name == "NPCs"))
                {
                    npcHealth = hit.collider.gameObject.AddComponent<NPCHealth>();
                }
            }

            if (npcHealth != null)
            {
                Debug.Log("Pistola Silenciada: Impacto confirmado en NPC -> " + hit.collider.name);
                npcHealth.TakeDamage(damage);
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

    private bool IsFiberWireEquipped()
    {
        if (playerHotbar == null)
        {
            return false;
        }

        ItemData currentItem = playerHotbar.GetSelectedItem();
        return currentItem != null && currentItem.ItemType == ItemType.FiberWire;
    }

    private void TryFiberWireTakedown()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, fiberWireRange))
        {
            NPCHealth targetHealth = hit.collider.GetComponentInParent<NPCHealth>();
            if (targetHealth == null)
            {
                if (hit.collider.name.StartsWith("Guard") || hit.collider.name.Contains("Executive") || (hit.collider.transform.parent != null && hit.collider.transform.parent.name == "NPCs"))
                {
                    targetHealth = hit.collider.gameObject.AddComponent<NPCHealth>();
                }
            }

            if (targetHealth != null && !targetHealth.IsDead)
            {
                Transform targetTransform = targetHealth.transform;
                Vector3 toNpc = (targetTransform.position - transform.position).normalized;

                // Verificación de ángulo: el jugador debe estar DETRÁS del NPC
                // El vector 'forward' del NPC debe apuntar en la misma dirección general que 'toNpc'
                float dotBehind = Vector3.Dot(targetTransform.forward, toNpc);
                bool isBehind = dotBehind > Mathf.Cos(behindAngleThreshold * Mathf.Deg2Rad);

                if (isBehind)
                {
                    Debug.DrawLine(ray.origin, hit.point, Color.cyan, 1.0f);
                    Debug.Log($"Cable de Fibra: ¡Eliminación silenciosa ejecutada por la espalda sobre {hit.collider.name}!");

                    if (fiberWireKillSound != null)
                    {
                        AudioSource.PlayClipAtPoint(fiberWireKillSound, hit.point);
                    }

                    targetHealth.ExecuteSilentTakedown();
                }
                else
                {
                    Debug.Log("Cable de Fibra: Debes estar detrás del objetivo para realizar la eliminación sigilosa.");
                }
                return;
            }
        }

        Debug.Log("Cable de Fibra: Ningún objetivo al alcance.");
    }
}
