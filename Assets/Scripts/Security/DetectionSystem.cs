using UnityEngine;

/// <summary>
/// Sistema sensorial de detección reutilizable para cámaras de seguridad y NPCs.
/// Evalúa distancia, ángulo cónico (FOV) y línea de visión directa con raycast/linecast contra obstáculos.
/// </summary>
public class DetectionSystem : MonoBehaviour
{
    [Header("Configuración de Visión")]
    [Tooltip("Distancia máxima en metros a la que el sensor puede detectar objetivos.")]
    [SerializeField] private float viewDistance = 8f;

    [Tooltip("Ángulo total de apertura del cono de visión en grados.")]
    [Range(10f, 180f)]
    [SerializeField] private float viewAngle = 60f;

    [Tooltip("Punto de origen de la visión (ej: lente de la cámara). Si es null, usa este transform.")]
    [SerializeField] private Transform eyePoint;

    [Tooltip("Offset vertical respecto a la posición del objetivo (ej: 1 metro para apuntar al pecho/cuerpo).")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f);

    [Header("Filtros de Capas")]
    [Tooltip("Capas consideradas obstáculos sólidos que bloquean la línea de visión (Paredes, Puertas, Coberturas).")]
    [SerializeField] private LayerMask obstacleMask = 1; // Default layer

    [Tooltip("Capa específica del objetivo (opcional, para optimización de queries).")]
    [SerializeField] private LayerMask targetMask = ~0; // Everything

    [Header("Zona de Pre-Aviso (Blanco)")]
    [Tooltip("Margen angular adicional alrededor del cono para advertencia previa en blanco.")]
    [SerializeField] private float warningAngleBuffer = 16f;

    [Tooltip("Margen de distancia adicional para advertencia previa en blanco.")]
    [SerializeField] private float warningDistanceBuffer = 1.8f;

    public float ViewDistance => viewDistance;
    public float ViewAngle => viewAngle;
    public Transform EyePoint => eyePoint != null ? eyePoint : transform;

    private void Reset()
    {
        eyePoint = transform;
        obstacleMask = LayerMask.GetMask("Default");
    }

    private void Awake()
    {
        if (eyePoint == null)
        {
            eyePoint = transform;
        }

        // Si la máscara de obstáculos no fue configurada, asignamos Default
        if (obstacleMask.value == 0)
        {
            obstacleMask = LayerMask.GetMask("Default");
        }
    }

    /// <summary>
    /// Comprueba si el objetivo se encuentra en rango, dentro del ángulo y sin obstáculos bloqueando la vista.
    /// </summary>
    /// <param name="target">Transform del objetivo (normalmente el Player).</param>
    /// <param name="hitInfo">Información del obstáculo si la visión fue bloqueada.</param>
    /// <returns>True si el objetivo es visible directamente, false en caso contrario.</returns>
    public bool CanSeeTarget(Transform target, out RaycastHit hitInfo)
    {
        hitInfo = default;

        if (target == null)
        {
            return false;
        }

        Transform origin = EyePoint;
        Vector3 originPos = origin.position;
        Vector3 targetCenter = target.position + targetOffset;
        Vector3 directionToTarget = targetCenter - originPos;
        float distanceToTarget = directionToTarget.magnitude;

        // 1. Verificación de distancia
        if (distanceToTarget > viewDistance)
        {
            return false;
        }

        // 2. Verificación de ángulo de cono (FOV)
        Vector3 dirNormalized = directionToTarget.normalized;
        float angleToTarget = Vector3.Angle(origin.forward, dirNormalized);

        if (angleToTarget > viewAngle * 0.5f)
        {
            return false;
        }

        // 3. Verificación de línea de visión con Linecast contra obstáculos
        // Si Linecast golpea un obstáculo antes de llegar al target, la visión está obstruida.
        if (Physics.Linecast(originPos, targetCenter, out hitInfo, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // Verificamos si lo que golpeó es el objetivo o parte de su jerarquía
            if (hitInfo.transform != target && !hitInfo.transform.IsChildOf(target))
            {
                return false; // Obstáculo bloqueando la visión
            }
        }

        return true;
    }

    /// <summary>
    /// Comprueba si el objetivo está en la zona de pre-aviso periférica (sin activar sospecha todavía).
    /// </summary>
    public bool IsInWarningZone(Transform target)
    {
        if (target == null) return false;

        Transform origin = EyePoint;
        Vector3 originPos = origin.position;
        Vector3 targetCenter = target.position + targetOffset;
        Vector3 directionToTarget = targetCenter - originPos;
        float distanceToTarget = directionToTarget.magnitude;

        // Comprobación de distancia ampliada
        if (distanceToTarget > viewDistance + warningDistanceBuffer)
        {
            return false;
        }

        // Comprobación de ángulo ampliado
        Vector3 dirNormalized = directionToTarget.normalized;
        float angleToTarget = Vector3.Angle(origin.forward, dirNormalized);
        if (angleToTarget > (viewAngle + warningAngleBuffer) * 0.5f)
        {
            return false;
        }

        // Comprobar que no haya paredes intermedias
        if (Physics.Linecast(originPos, targetCenter, out RaycastHit hitInfo, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hitInfo.transform != target && !hitInfo.transform.IsChildOf(target))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Dibuja el cono de visión y las líneas guía en la vista de escena para fácil calibración.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Transform origin = EyePoint;
        if (origin == null) return;

        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.35f);
        Vector3 originPos = origin.position;

        // Dibujar arco de rango
        Gizmos.DrawWireSphere(originPos, viewDistance);

        // Dibujar bordes del cono de visión
        float halfAngle = viewAngle * 0.5f;
        Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, origin.up);
        Quaternion rightRot = Quaternion.AngleAxis(halfAngle, origin.up);
        Quaternion upRot = Quaternion.AngleAxis(-halfAngle, origin.right);
        Quaternion downRot = Quaternion.AngleAxis(halfAngle, origin.right);

        Vector3 leftRay = leftRot * origin.forward;
        Vector3 rightRay = rightRot * origin.forward;
        Vector3 upRay = upRot * origin.forward;
        Vector3 downRay = downRot * origin.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(originPos, leftRay * viewDistance);
        Gizmos.DrawRay(originPos, rightRay * viewDistance);
        Gizmos.DrawRay(originPos, upRay * viewDistance);
        Gizmos.DrawRay(originPos, downRay * viewDistance);

        // Línea central
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(originPos, origin.forward * viewDistance);
    }
}
