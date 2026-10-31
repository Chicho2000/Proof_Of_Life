using UnityEngine;

public class DetectionSystem : MonoBehaviour
{
    public enum TargetDetectionPolicy
    {
        Standard,
        VisiblePistolOnly,
        RestrictedAreaOrWeapon
    }

    public enum DisguiseAuthorization
    {
        AllowGuardAndElite, // Autoriza disfraces de Guardia común y de Élite (Cámaras estándar, Guardia normal)
        AllowEliteOnly,     // Solo autoriza disfraz de Élite (Cámaras de zonas críticas, Guardia de Élite)
        AllowNone           // No autoriza ningún disfraz (Zonas prohibidas / detección absoluta)
    }

    [Header("Detección")]
    [SerializeField] private Transform detectionOrigin;
    [SerializeField, Min(0f)] private float detectionRange = 15f;
    [SerializeField, Range(0f, 360f)] private float detectionAngle = 70f;
    [Tooltip("Radio cercano donde el enemigo te detecta si estás en su semicírculo frontal.")]
    [SerializeField, Min(0f)] private float proximityRange = 1.4f;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private bool drawDebug = true;

    [Header("Permisos de Disfraz")]
    [SerializeField] private DisguiseAuthorization disguiseAuthorization = DisguiseAuthorization.AllowGuardAndElite;

    [Header("Regla de sospecha visual")]
    [SerializeField] private TargetDetectionPolicy targetDetectionPolicy = TargetDetectionPolicy.Standard;

    [Header("Gizmos")]
    [SerializeField] private Color gizmoColor = Color.cyan;

    public float DetectionRange
    {
        get => detectionRange;
        set => detectionRange = Mathf.Max(0f, value);
    }

    public float DetectionAngle
    {
        get => detectionAngle;
        set => detectionAngle = Mathf.Clamp(value, 0f, 360f);
    }

    public float ProximityRange
    {
        get => proximityRange;
        set => proximityRange = Mathf.Max(0f, value);
    }

    public DisguiseAuthorization DisguiseAuth
    {
        get => disguiseAuthorization;
        set => disguiseAuthorization = value;
    }

    public TargetDetectionPolicy DetectionPolicy
    {
        get => targetDetectionPolicy;
        set => targetDetectionPolicy = value;
    }

    public LayerMask ObstacleMask => obstacleMask;
    public Transform DetectionOrigin => detectionOrigin != null ? detectionOrigin : transform;
    public Color GizmoColor
    {
        get => gizmoColor;
        set => gizmoColor = value;
    }

    public bool CanDetectTarget(Transform target, bool ignoreDisguise = false)
    {
        if (target == null || IsTargetHidden(target) || !PassesTargetPolicy(target))
        {
            return false;
        }

        bool restrictedAccess = targetDetectionPolicy == TargetDetectionPolicy.RestrictedAreaOrWeapon
            && PlayerAccessState.IsRestricted(target);
        if (!ignoreDisguise && !restrictedAccess && IsTargetDisguised(target))
        {
            return false;
        }

        if (!IsLayerInMask(target.gameObject.layer, targetMask))
        {
            return false;
        }

        Transform origin = detectionOrigin != null ? detectionOrigin : transform;
        Vector3 originPos = origin.position;
        Vector3 targetCenter = GetTargetPosition(target);
        Vector3 toTargetVector = targetCenter - originPos;
        float distanceToTarget = toTargetVector.magnitude;

        if (distanceToTarget <= Mathf.Epsilon || distanceToTarget > detectionRange)
        {
            DrawDetectionRay(originPos, toTargetVector, Color.gray);
            return false;
        }

        Vector3 horizontalDir = Vector3.ProjectOnPlane(toTargetVector, Vector3.up);

        // 1. Proximidad cercana: si estás a menos de proximityRange frente al enemigo
        bool isInsideProximity = false;
        if (distanceToTarget <= (ignoreDisguise ? Mathf.Max(proximityRange, 3.5f) : proximityRange))
        {
            float hAngle = horizontalDir.sqrMagnitude > 0.001f 
                ? Vector3.Angle(origin.forward, horizontalDir.normalized) 
                : 0f;
            if (hAngle <= (ignoreDisguise ? 105f : 90f))
            {
                isInsideProximity = true;
            }
        }

        // 2. Cono de visión
        if (!isInsideProximity)
        {
            float horizontalAngle = horizontalDir.sqrMagnitude > 0.001f 
                ? Vector3.Angle(origin.forward, horizontalDir.normalized) 
                : 0f;

            if (horizontalAngle > detectionAngle * 0.5f)
            {
                DrawDetectionRay(originPos, toTargetVector, Color.yellow);
                return false;
            }

            // Límite vertical cónico (permite mirar objetivos en altura o desniveles hasta 50°)
            float verticalAngle = Mathf.Abs(Mathf.Asin(Mathf.Clamp(toTargetVector.y / distanceToTarget, -1f, 1f)) * Mathf.Rad2Deg);
            if (verticalAngle > 50f)
            {
                DrawDetectionRay(originPos, toTargetVector, Color.yellow);
                return false;
            }
        }

        // 3. Comprobación de línea de visión multi-punto (centro y cabeza)
        Vector3 targetHead = GetTargetHeadPosition(target);
        bool seeCenter = HasLineOfSight(originPos, targetCenter, target);
        bool seeHead = HasLineOfSight(originPos, targetHead, target);

        bool canSee = seeCenter || seeHead;
        Vector3 visiblePoint = seeHead ? targetHead : targetCenter;

        DrawDetectionRay(originPos, visiblePoint - originPos, canSee ? Color.green : Color.red);
        return canSee;
    }

    private bool PassesTargetPolicy(Transform target)
    {
        switch (targetDetectionPolicy)
        {
            case TargetDetectionPolicy.VisiblePistolOnly:
                return HasEquippedItem(target, ItemType.SilencedPistol);

            case TargetDetectionPolicy.RestrictedAreaOrWeapon:
                return PlayerAccessState.IsRestricted(target)
                    || HasEquippedItem(target, ItemType.SilencedPistol)
                    || HasEquippedItem(target, ItemType.FiberWire);

            default:
                return true;
        }
    }

    private static bool HasEquippedItem(Transform target, ItemType itemType)
    {
        PlayerHotbar hotbar = target.GetComponentInParent<PlayerHotbar>();
        ItemInteractable item = hotbar != null ? hotbar.GetSelectedItem() : null;
        return item != null && item.ItemType == itemType;
    }

    private bool HasLineOfSight(Vector3 originPos, Vector3 targetPoint, Transform target)
    {
        Vector3 dir = targetPoint - originPos;
        float dist = dir.magnitude;
        if (dist <= 0.01f)
        {
            return true;
        }

        RaycastHit[] hits = Physics.RaycastAll(
            originPos,
            dir / dist,
            dist,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        RaycastHit closestHit = default;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (IsDetectorTransform(hit.transform) || hit.distance >= closestDistance)
            {
                continue;
            }

            closestHit = hit;
            closestDistance = hit.distance;
        }

        if (closestDistance < float.PositiveInfinity)
        {
            return IsTargetTransform(closestHit.transform, target);
        }

        return true;
    }

    private static Vector3 GetTargetPosition(Transform target)
    {
        Collider targetCollider = target.GetComponentInChildren<Collider>();
        return targetCollider != null ? targetCollider.bounds.center : target.position;
    }

    private static Vector3 GetTargetHeadPosition(Transform target)
    {
        Collider targetCollider = target.GetComponentInChildren<Collider>();
        if (targetCollider != null)
        {
            return targetCollider.bounds.center + Vector3.up * (targetCollider.bounds.extents.y * 0.85f);
        }
        return target.position + Vector3.up * 1.6f;
    }

    private static bool IsTargetTransform(Transform hitTransform, Transform target)
    {
        return hitTransform == target
            || hitTransform.IsChildOf(target)
            || target.IsChildOf(hitTransform);
    }

    private bool IsDetectorTransform(Transform hitTransform)
    {
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }

    private static bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private bool IsTargetDisguised(Transform target)
    {
        PlayerDisguiseSystem disguiseSystem = target.GetComponentInParent<PlayerDisguiseSystem>();
        if (disguiseSystem == null || disguiseSystem.CurrentDisguise == PlayerDisguiseSystem.DisguiseType.None)
        {
            return false;
        }

        // Si está arrastrando un cadáver, el disfraz queda comprometido y los enemigos/cámaras sospechan
        if (BodyInteractable.IsDraggingAnyBody)
        {
            return false;
        }

        // Si sostiene un arma en la mano (pistola silenciada o cable de fibra), el disfraz queda comprometido
        PlayerHotbar hotbar = target.GetComponentInParent<PlayerHotbar>();
        if (hotbar != null)
        {
            ItemInteractable selectedItem = hotbar.GetSelectedItem();
            if (selectedItem != null && (selectedItem.ItemType == ItemType.SilencedPistol || selectedItem.ItemType == ItemType.FiberWire || selectedItem.ItemType == ItemType.MissionItem))
            {
                return false;
            }
        }

        switch (disguiseAuthorization)
        {
            case DisguiseAuthorization.AllowGuardAndElite:
                return disguiseSystem.CurrentDisguise == PlayerDisguiseSystem.DisguiseType.GuardDisguise
                    || disguiseSystem.CurrentDisguise == PlayerDisguiseSystem.DisguiseType.EliteGuardDisguise;

            case DisguiseAuthorization.AllowEliteOnly:
                return disguiseSystem.CurrentDisguise == PlayerDisguiseSystem.DisguiseType.EliteGuardDisguise;

            case DisguiseAuthorization.AllowNone:
            default:
                return false;
        }
    }

    private static bool IsTargetHidden(Transform target)
    {
        PlayerHidingSystem hidingSystem = target.GetComponentInParent<PlayerHidingSystem>();
        return hidingSystem != null && hidingSystem.IsHiding;
    }

    private void DrawDetectionRay(Vector3 origin, Vector3 direction, Color color)
    {
        if (drawDebug)
        {
            Debug.DrawRay(origin, direction, color);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
        {
            return;
        }

        Transform origin = detectionOrigin != null ? detectionOrigin : transform;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(origin.position, detectionRange);
        Gizmos.DrawRay(origin.position, origin.forward * detectionRange);
        DrawHorizontalCone(origin);
        DrawVerticalCone(origin);
    }

    private void DrawHorizontalCone(Transform origin)
    {
        const int segmentCount = 24;
        float halfAngle = detectionAngle * 0.5f;
        Vector3 previousDirection = Quaternion.AngleAxis(-halfAngle, origin.up) * origin.forward;
        Vector3 previousPoint = origin.position + previousDirection * detectionRange;

        Gizmos.DrawLine(origin.position, previousPoint);

        for (int index = 1; index <= segmentCount; index++)
        {
            float progress = index / (float)segmentCount;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, progress);
            Vector3 direction = Quaternion.AngleAxis(angle, origin.up) * origin.forward;
            Vector3 point = origin.position + direction * detectionRange;

            Gizmos.DrawLine(previousPoint, point);
            Gizmos.DrawLine(origin.position, point);
            previousPoint = point;
        }
    }

    private void DrawVerticalCone(Transform origin)
    {
        float halfAngle = detectionAngle * 0.5f;
        Vector3 upperBoundary = Quaternion.AngleAxis(-halfAngle, origin.right) * origin.forward;
        Vector3 lowerBoundary = Quaternion.AngleAxis(halfAngle, origin.right) * origin.forward;

        Gizmos.DrawRay(origin.position, upperBoundary * detectionRange);
        Gizmos.DrawRay(origin.position, lowerBoundary * detectionRange);
    }
}
