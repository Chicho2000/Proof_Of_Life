using UnityEngine;

public class DetectionSystem : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private Transform detectionOrigin;
    [SerializeField, Min(0f)] private float detectionRange = 15f;
    [SerializeField, Range(0f, 360f)] private float detectionAngle = 70f;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private bool drawDebug = true;

    public bool CanDetectTarget(Transform target)
    {
        if (target == null || IsTargetDisguised(target))
        {
            return false;
        }

        if (!IsLayerInMask(target.gameObject.layer, targetMask))
        {
            return false;
        }

        Transform origin = detectionOrigin != null ? detectionOrigin : transform;
        Vector3 targetPosition = GetTargetPosition(target);
        Vector3 directionToTarget = targetPosition - origin.position;
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget <= Mathf.Epsilon || distanceToTarget > detectionRange)
        {
            DrawDetectionRay(origin.position, directionToTarget, Color.gray);
            return false;
        }

        Vector3 normalizedDirection = directionToTarget / distanceToTarget;
        float angleToTarget = Vector3.Angle(origin.forward, normalizedDirection);
        if (angleToTarget > detectionAngle * 0.5f)
        {
            DrawDetectionRay(origin.position, directionToTarget, Color.yellow);
            return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(
            origin.position,
            normalizedDirection,
            distanceToTarget,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        RaycastHit closestHit = default;
        float closestDistance = float.PositiveInfinity;

        foreach (RaycastHit hit in hits)
        {
            if (IsDetectorTransform(hit.transform) || hit.distance >= closestDistance)
            {
                continue;
            }

            closestHit = hit;
            closestDistance = hit.distance;
        }

        if (closestDistance < float.PositiveInfinity)
        {
            bool hitTarget = IsTargetTransform(closestHit.transform, target);
            DrawDetectionRay(origin.position, directionToTarget, hitTarget ? Color.green : Color.red);
            return hitTarget;
        }

        DrawDetectionRay(origin.position, directionToTarget, Color.green);
        return true;
    }

    private static Vector3 GetTargetPosition(Transform target)
    {
        Collider targetCollider = target.GetComponentInChildren<Collider>();
        return targetCollider != null ? targetCollider.bounds.center : target.position;
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

    private static bool IsTargetDisguised(Transform target)
    {
        PlayerDisguiseSystem disguiseSystem = target.GetComponentInParent<PlayerDisguiseSystem>();
        return disguiseSystem != null
            && disguiseSystem.CurrentDisguise == PlayerDisguiseSystem.DisguiseType.GuardDisguise;
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
        Gizmos.color = Color.cyan;
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
