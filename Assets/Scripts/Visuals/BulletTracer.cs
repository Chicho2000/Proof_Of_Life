using System;
using UnityEngine;

public class BulletTracer : MonoBehaviour
{
    [Header("Configuración de Velocidad")]
    [SerializeField, Min(10f)] private float speed = 250f;
    [SerializeField, Min(0.1f)] private float maxLifetime = 2.0f;

    private Vector3 targetPosition;
    private bool isInitialized = false;
    private float lifeTimer = 0f;
    private Action onHitCallback;
    private TrailRenderer trail;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
    }

    public void Initialize(Vector3 startPoint, Vector3 endPoint, Action onHit = null)
    {
        transform.position = startPoint;
        targetPosition = endPoint;
        onHitCallback = onHit;

        Vector3 direction = endPoint - startPoint;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.forward = direction.normalized;
        }

        if (trail != null)
        {
            trail.Clear();
        }

        isInitialized = true;
        lifeTimer = 0f;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            TriggerHit();
            Destroy(gameObject);
            return;
        }

        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

        if (Vector3.Distance(transform.position, targetPosition) <= 0.05f)
        {
            TriggerHit();
            Destroy(gameObject);
        }
    }

    private void TriggerHit()
    {
        if (onHitCallback != null)
        {
            Action callback = onHitCallback;
            onHitCallback = null;
            callback.Invoke();
        }
    }
}
