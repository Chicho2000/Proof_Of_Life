using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(ItemInteractable))]
[RequireComponent(typeof(NoiseEmitter))]
public class ThrowableObject : MonoBehaviour
{
    [SerializeField] private bool breakOnImpact = true;
    [Min(0f)] [SerializeField] private float minimumImpactSpeed = 1.5f;
    [SerializeField] private AudioClip impactSound = null;

    private Rigidbody body;
    private NoiseEmitter noiseEmitter;
    private Collider objectCollider;
    private bool launched;
    private bool impacted;
    private Collider[] ignoredOwnerColliders;
    private float ignoreOwnerUntil;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        noiseEmitter = GetComponent<NoiseEmitter>();
        objectCollider = GetComponent<Collider>();
    }

    public bool Launch(Vector3 velocity, Collider[] ownerColliders)
    {
        if (launched || body == null || objectCollider == null || noiseEmitter == null
            || !objectCollider.enabled || velocity.sqrMagnitude < 0.001f
            || float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z))
        {
            return false;
        }

        ignoredOwnerColliders = ownerColliders;
        if (ignoredOwnerColliders != null)
        {
            foreach (Collider owner in ignoredOwnerColliders)
            {
                if (owner != null && owner != objectCollider)
                {
                    Physics.IgnoreCollision(objectCollider, owner, true);
                }
            }
        }

        ignoreOwnerUntil = Time.time + 0.35f;
        body.isKinematic = false;
        body.useGravity = true;
        body.detectCollisions = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = velocity;
        launched = true;
        return true;
    }

    private void FixedUpdate()
    {
        if (ignoredOwnerColliders == null || Time.time < ignoreOwnerUntil) return;

        foreach (Collider owner in ignoredOwnerColliders)
        {
            if (owner != null && objectCollider != null)
            {
                Physics.IgnoreCollision(objectCollider, owner, false);
            }
        }

        ignoredOwnerColliders = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!launched || impacted || collision.relativeVelocity.magnitude < minimumImpactSpeed) return;

        impacted = true;
        Vector3 impactPosition = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;
        noiseEmitter.EmitNoise(impactPosition);

        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, impactPosition);
        }

        if (breakOnImpact)
        {
            Destroy(gameObject);
        }
        else
        {
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
    }

    private void OnDestroy()
    {
        if (ignoredOwnerColliders == null || objectCollider == null) return;

        foreach (Collider owner in ignoredOwnerColliders)
        {
            if (owner != null)
            {
                Physics.IgnoreCollision(objectCollider, owner, false);
            }
        }
    }
}
