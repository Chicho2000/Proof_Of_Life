using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NPCRagdoll : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider rootCollider;
    [SerializeField] private Rigidbody[] ragdollBodies = new Rigidbody[0];
    [SerializeField] private Collider[] ragdollColliders = new Collider[0];

    [Header("Configuracion")]
    [SerializeField] private bool disableRootColliderOnDeath = true;
    [SerializeField] private float maxAngularVelocity = 20f;

    private bool isRagdollActive;

    public bool IsRagdollActive => isRagdollActive;
    public bool HasConfiguredRagdoll => ragdollBodies != null && ragdollBodies.Length > 0;

    private void Awake()
    {
        CacheReferences();
        SetInitialState();
    }

    [ContextMenu("Refresh Ragdoll References")]
    public void RefreshReferences()
    {
        CacheReferences(true);
    }

    public bool ActivateRagdoll()
    {
        CacheReferences();

        if (!HasConfiguredRagdoll)
        {
            Debug.LogWarning(
                $"[NPCRagdoll] {gameObject.name} no tiene Rigidbody de ragdoll en sus huesos. " +
                "Configuralos con el Ragdoll Wizard de Unity y ejecuta Refresh Ragdoll References.",
                this
            );
            return false;
        }

        isRagdollActive = true;

        if (animator != null)
        {
            animator.enabled = false;
        }

        if (disableRootColliderOnDeath && rootCollider != null)
        {
            rootCollider.enabled = false;
        }

        SetRagdollCollidersEnabled(true);
        SetBodiesKinematic(false);
        return true;
    }

    public void SetCarried(bool isCarried)
    {
        if (!isRagdollActive)
        {
            return;
        }

        SetBodiesKinematic(isCarried);
    }

    private void CacheReferences(bool forceRefresh = false)
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (rootCollider == null)
        {
            rootCollider = GetComponent<Collider>();
        }

        if (forceRefresh || ragdollBodies == null || ragdollBodies.Length == 0)
        {
            Rigidbody[] foundBodies = GetComponentsInChildren<Rigidbody>(true);
            List<Rigidbody> validBodies = new List<Rigidbody>();

            foreach (Rigidbody body in foundBodies)
            {
                if (body != null && body.gameObject != gameObject)
                {
                    validBodies.Add(body);
                }
            }

            ragdollBodies = validBodies.ToArray();
        }

        if (forceRefresh || ragdollColliders == null || ragdollColliders.Length == 0)
        {
            List<Collider> validColliders = new List<Collider>();

            if (ragdollBodies != null)
            {
                foreach (Rigidbody body in ragdollBodies)
                {
                    if (body == null)
                    {
                        continue;
                    }

                    Collider[] bodyColliders = body.GetComponents<Collider>();
                    foreach (Collider bodyCollider in bodyColliders)
                    {
                        if (bodyCollider != null && bodyCollider != rootCollider)
                        {
                            validColliders.Add(bodyCollider);
                        }
                    }
                }
            }

            ragdollColliders = validColliders.ToArray();
        }
    }

    private void SetInitialState()
    {
        isRagdollActive = false;
        SetBodiesKinematic(true);
        SetRagdollCollidersEnabled(false);
    }

    private void SetBodiesKinematic(bool isKinematic)
    {
        if (ragdollBodies == null)
        {
            return;
        }

        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == null)
            {
                continue;
            }

            body.isKinematic = isKinematic;
            body.detectCollisions = isRagdollActive;
            body.maxAngularVelocity = maxAngularVelocity;

            if (!isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
        }
    }

    private void SetRagdollCollidersEnabled(bool isEnabled)
    {
        if (ragdollColliders == null)
        {
            return;
        }

        foreach (Collider ragdollCollider in ragdollColliders)
        {
            if (ragdollCollider != null)
            {
                ragdollCollider.enabled = isEnabled;
            }
        }
    }
}
