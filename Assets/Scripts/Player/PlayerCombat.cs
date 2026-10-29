using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerCombat : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerHotbar playerHotbar;

    [Header("Configuracion de Pistola Silenciada")]
    [SerializeField] private float range = 50f;
    [FormerlySerializedAs("damage")]
    [SerializeField] private int headshotDamage = 100;
    [SerializeField] private int torsoDamage = 34;
    [SerializeField, Min(0.05f)] private float shotCooldown = 0.45f;
    [SerializeField, Min(0.1f)] private float reloadDuration = 2.25f;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip emptyAmmoSound;

    [Header("Efectos Visuales")]
    [SerializeField] private GameObject bulletTracerPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Municion")]
    [SerializeField] private int maxAmmo = 12;
    [SerializeField] private int currentAmmo = 12;

    [Header("Configuración de Cable de Fibra (FiberWire)")]
    [SerializeField] private float fiberWireRange = 2.2f;
    [FormerlySerializedAs("behindAngleThreshold")]
    [Range(0f, 90f)]
    [SerializeField] private float backstabAngle = 60f;
    [SerializeField] private AudioClip fiberWireKillSound;

    // Evento desacoplado para UI de munición (currentAmmo, maxAmmo)
    public event System.Action<int, int> OnAmmoChanged;

    private Camera playerCamera;
    private float nextShotTime;
    private bool isReloading;

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

    private void Start()
    {
        if (OnAmmoChanged != null)
        {
            OnAmmoChanged.Invoke(currentAmmo, maxAmmo);
        }
    }

    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f)
        {
            return;
        }

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
                    if (emptyAmmoSound != null)
                    {
                        Vector3 soundPos = playerCamera != null ? playerCamera.transform.position : transform.position;
                        AudioSource.PlayClipAtPoint(emptyAmmoSound, soundPos);
                    }
                    Debug.Log("Pistola Silenciada: ¡Sin munición! Presiona R para recargar.");
                }
            }
            else if (IsFiberWireEquipped())
            {
                TryFiberWireTakedown();
            }
            else if (playerHotbar == null || playerHotbar.GetSelectedItem() == null
                || playerHotbar.GetSelectedItem().ItemType != ItemType.Throwable)
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

        ItemInteractable currentItem = playerHotbar.GetSelectedItem();
        if (currentItem != null && currentItem.ItemType == ItemType.SilencedPistol)
        {
            return true;
        }

        return false;
    }

    private bool CanShootSilencedPistol()
    {
        if (currentAmmo <= 0 || isReloading || Time.time < nextShotTime)
        {
            return false;
        }

        return IsSilencedPistolEquipped();
    }

    private void Shoot()
    {
        nextShotTime = Time.time + shotCooldown;
        currentAmmo--;

        Debug.Log("Pistola Silenciada: ¡Disparo realizado! Balas restantes: " + currentAmmo + "/" + maxAmmo);

        if (shootSound != null)
        {
            Vector3 soundPos = playerCamera != null ? playerCamera.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(shootSound, soundPos);
        }

        if (OnAmmoChanged != null)
        {
            OnAmmoChanged.Invoke(currentAmmo, maxAmmo);
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Vector3 targetPoint;
        System.Action onHitAction = null;

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            targetPoint = hit.point;
            NPCHealth targetHealth = hit.collider.GetComponentInParent<NPCHealth>();
            string hitColliderName = hit.collider.name;
            Vector3 hitPosition = hit.point;

            onHitAction = delegate
            {
                Debug.DrawLine(ray.origin, hitPosition, Color.green, 1.0f);
                Debug.Log("Pistola Silenciada: Impacto en " + hitColliderName);

                if (targetHealth != null)
                {
                    bool wasHeadshot = targetHealth.TakeBulletDamage(torsoDamage, headshotDamage, hitPosition);
                    Debug.Log("Pistola Silenciada: Impacto confirmado en NPC -> " + hitColliderName
                        + (wasHeadshot ? " (cabeza)" : " (torso)"));

                    if (!targetHealth.IsDead && AlarmManager.Instance != null)
                    {
                        Debug.Log("Pistola Silenciada: El NPC sobrevivió al disparo. Activando alarma.");
                        AlarmManager.Instance.TriggerAlarm();
                    }
                }
            };
        }
        else
        {
            targetPoint = ray.origin + ray.direction * range;
            onHitAction = delegate
            {
                Debug.DrawLine(ray.origin, targetPoint, Color.red, 1.0f);
            };
        }

        if (bulletTracerPrefab != null)
        {
            SpawnBulletTracer(targetPoint, onHitAction);
        }
        else
        {
            if (onHitAction != null)
            {
                onHitAction.Invoke();
            }
        }
    }

    private void SpawnBulletTracer(Vector3 targetPoint, System.Action onHit)
    {
        Vector3 originPoint = GetMuzzleOrigin();

        GameObject tracerObj = Instantiate(bulletTracerPrefab, originPoint, Quaternion.identity);
        BulletTracer tracer = tracerObj.GetComponent<BulletTracer>();
        if (tracer != null)
        {
            tracer.Initialize(originPoint, targetPoint, onHit);
        }
    }

    private Vector3 GetMuzzleOrigin()
    {
        if (firePoint != null)
        {
            return firePoint.position;
        }

        // Si el arma fue recogida y equipada dinámicamente en la mano
        if (playerHotbar != null && playerHotbar.CurrentInHandObject != null)
        {
            Transform inHand = playerHotbar.CurrentInHandObject.transform;
            Transform foundPoint = inHand.Find("FirePoint");
            if (foundPoint != null)
            {
                return foundPoint.position;
            }

            // inHand.forward apunta exactamente a lo largo del cañón del arma
            return inHand.position + (inHand.forward * 0.32f) + (inHand.up * 0.05f);
        }

        // Fallback calibrado a la posición visual del arma en pantalla (offset en mano + cañón)
        if (playerCamera != null)
        {
            return playerCamera.transform.position
                + playerCamera.transform.right * 0.25f
                + playerCamera.transform.up * -0.16f
                + playerCamera.transform.forward * 0.75f;
        }

        return transform.position;
    }

    public void Reload()
    {
        if (currentAmmo == maxAmmo || isReloading)
        {
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        Debug.Log("Pistola Silenciada: Recargando...");

        if (reloadSound != null)
        {
            Vector3 soundPos = playerCamera != null ? playerCamera.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(reloadSound, soundPos);
        }

        yield return new WaitForSeconds(reloadDuration);

        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("Pistola Silenciada: Recargada. Municion completa: " + currentAmmo + "/" + maxAmmo);

        if (OnAmmoChanged != null)
        {
            OnAmmoChanged.Invoke(currentAmmo, maxAmmo);
        }
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

        ItemInteractable currentItem = playerHotbar.GetSelectedItem();
        return currentItem != null && currentItem.ItemType == ItemType.FiberWire;
    }

    private void TryFiberWireTakedown()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, fiberWireRange))
        {
            NPCHealth targetHealth = hit.collider.GetComponentInParent<NPCHealth>();

            if (targetHealth != null && !targetHealth.IsDead)
            {
                Vector3 targetForward = Vector3.ProjectOnPlane(targetHealth.Forward, Vector3.up).normalized;
                Vector3 targetToPlayer = Vector3.ProjectOnPlane(
                    transform.position - targetHealth.transform.position,
                    Vector3.up
                ).normalized;

                // Detrás significa estar dentro del cono opuesto al frente visual del NPC.
                float requiredDotThreshold = -Mathf.Cos(backstabAngle * Mathf.Deg2Rad);
                float playerPositionDot = Vector3.Dot(targetForward, targetToPlayer);
                bool isBehind = targetForward.sqrMagnitude > 0f
                    && targetToPlayer.sqrMagnitude > 0f
                    && playerPositionDot <= requiredDotThreshold;

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
