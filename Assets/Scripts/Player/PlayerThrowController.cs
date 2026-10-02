using System;
using UnityEngine;

[RequireComponent(typeof(PlayerHotbar))]
public class PlayerThrowController : MonoBehaviour
{
    [Header("Lanzamiento")]
    [Min(0.1f)] [SerializeField] private float minThrowForce = 5f;
    [Min(0.1f)] [SerializeField] private float maxThrowForce = 18f;
    [Min(0.01f)] [SerializeField] private float chargeDuration = 1.5f;
    [SerializeField] private Transform throwOrigin = null;
    [Min(0.01f)] [SerializeField] private float spawnClearanceRadius = 0.18f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    private PlayerHotbar hotbar;
    private Camera playerCamera;
    private int chargingSlot = -1;
    private ItemInteractable chargingItem;
    private float chargeTime;

    public bool IsCharging { get; private set; }
    public float ChargeNormalized => IsCharging
        ? Mathf.Clamp01(chargeTime / Mathf.Max(0.01f, chargeDuration))
        : 0f;

    private void Awake()
    {
        hotbar = GetComponent<PlayerHotbar>();
        playerCamera = GetComponentInChildren<Camera>();
    }

    private void OnEnable()
    {
        if (hotbar == null) hotbar = GetComponent<PlayerHotbar>();
        hotbar.OnSlotSelected += HandleSlotSelected;
    }

    private void OnDisable()
    {
        if (hotbar != null) hotbar.OnSlotSelected -= HandleSlotSelected;
        CancelCharge();
    }

    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f
            || BodyInteractable.IsDraggingAnyBody)
        {
            CancelCharge();
            return;
        }

        if (IsCharging)
        {
            HotbarSlot slot = hotbar.GetSlot(chargingSlot);
            if (hotbar.SelectedSlotIndex != chargingSlot || slot == null || slot.IsEmpty
                || slot.Item != chargingItem || slot.Item.ItemType != ItemType.Throwable)
            {
                CancelCharge();
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                CancelCharge();
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                float force = Mathf.Lerp(minThrowForce, Mathf.Max(minThrowForce, maxThrowForce), ChargeNormalized);
                TryThrow(chargingSlot, chargingItem, force);
                CancelCharge();
                return;
            }

            chargeTime = Mathf.Min(chargeDuration, chargeTime + Time.deltaTime);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            ItemInteractable item = hotbar.GetSelectedItem();
            if (item != null && item.ItemType == ItemType.Throwable)
            {
                chargingSlot = hotbar.SelectedSlotIndex;
                chargingItem = item;
                chargeTime = 0f;
                IsCharging = true;
            }
        }
    }

    private void HandleSlotSelected(int selectedSlot)
    {
        if (IsCharging && selectedSlot != chargingSlot) CancelCharge();
    }

    private void CancelCharge()
    {
        IsCharging = false;
        chargingSlot = -1;
        chargingItem = null;
        chargeTime = 0f;
    }

    private bool TryThrow(int slotIndex, ItemInteractable item, float force)
    {
        HotbarSlot slot = hotbar.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.Item != item || item.WorldPrefab == null)
        {
            Debug.LogWarning("[Throw] No hay un arrojable válido con WorldPrefab en el slot seleccionado.", this);
            return false;
        }

        // Algunas instancias de escena tienen WorldPrefab remapeado a sí mismas.
        // En ese caso se clona la fuente guardada; si es asset, se usa el prefab.
        GameObject spawnSource = item.WorldPrefab.scene.IsValid() ? item.gameObject : item.WorldPrefab;
        ItemInteractable sourcePickup = spawnSource.GetComponent<ItemInteractable>();
        if (sourcePickup == null || !sourcePickup.HasSameIdentity(item))
        {
            Debug.LogWarning("[Throw] WorldPrefab debe ser un pickup raíz con la misma identidad del item.", this);
            return false;
        }

        Transform origin = throwOrigin != null ? throwOrigin
            : playerCamera != null ? playerCamera.transform : transform;
        Vector3 direction = origin.forward.normalized;
        if (!TryGetSpawnPosition(origin.position, direction, out Vector3 spawnPosition))
        {
            Debug.LogWarning("[Throw] No hay espacio libre para lanzar el objeto.", this);
            return false;
        }

        GameObject spawned = null;
        try
        {
            spawned = Instantiate(spawnSource, spawnPosition, Quaternion.LookRotation(direction));
            ItemInteractable pickup = spawned.GetComponent<ItemInteractable>();
            ThrowableObject throwable = spawned.GetComponent<ThrowableObject>();
            Rigidbody body = spawned.GetComponent<Rigidbody>();
            Collider collider = spawned.GetComponent<Collider>();
            NoiseEmitter noise = spawned.GetComponent<NoiseEmitter>();
            if (pickup == null || throwable == null || body == null || collider == null || noise == null)
            {
                Debug.LogWarning("[Throw] El prefab debe tener ItemInteractable, ThrowableObject, NoiseEmitter, Rigidbody y Collider en la raíz.", this);
                Destroy(spawned);
                return false;
            }

            pickup.Initialize(1);
            pickup.enabled = true;
            collider.enabled = true;
            Collider[] playerColliders = GetComponentsInChildren<Collider>(true);
            spawned.SetActive(true);
            if (!throwable.Launch(direction * force, playerColliders))
            {
                spawned.SetActive(false);
                Destroy(spawned);
                return false;
            }

            if (!hotbar.RemoveItem(slotIndex, 1))
            {
                spawned.SetActive(false);
                Destroy(spawned);
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            if (spawned != null) Destroy(spawned);
            Debug.LogWarning($"[Throw] No se pudo lanzar '{item.ItemName}': {exception.Message}", this);
            return false;
        }
    }

    private bool TryGetSpawnPosition(Vector3 origin, Vector3 direction, out Vector3 position)
    {
        CharacterController controller = GetComponent<CharacterController>();
        float distance = (controller != null ? controller.radius : 0.4f) + spawnClearanceRadius + 0.45f;
        RaycastHit[] hits = Physics.SphereCastAll(origin, spawnClearanceRadius, direction,
            distance, obstacleMask, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && !hit.collider.transform.IsChildOf(transform))
            {
                position = default;
                return false;
            }
        }

        position = origin + direction * distance;
        Collider[] overlaps = Physics.OverlapSphere(position, spawnClearanceRadius,
            obstacleMask, QueryTriggerInteraction.Ignore);
        foreach (Collider overlap in overlaps)
        {
            if (overlap != null && !overlap.transform.IsChildOf(transform)) return false;
        }

        return true;
    }
}
