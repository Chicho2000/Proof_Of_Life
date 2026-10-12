using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCRagdoll : MonoBehaviour
{
    [Header("Humanoid")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider rootCollider;
    [SerializeField] private Rigidbody hipsBody;

    private Animator[] rigAnimators;
    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;
    private Transform humanoidRoot;
    private NavMeshAgent agent;
    private readonly List<Collider> ignoredCarrierColliders = new List<Collider>();
    private Vector3 carryTargetPosition;
    private Quaternion carryTargetRotation = Quaternion.identity;
    private bool hasCarryTarget;
    private bool isRagdollActive;
    private bool isCarried;
    private bool isStored;

    public bool IsRagdollActive => isRagdollActive;
    public bool IsCarried => isCarried;
    public Transform Pelvis => hipsBody != null ? hipsBody.transform : null;
    public bool HasConfiguredRagdoll => animator != null && rootCollider != null
        && rootCollider.transform == transform && humanoidRoot != null
        && hipsBody != null && ragdollBodies != null && ragdollBodies.Length > 1
        && ragdollColliders != null && ragdollColliders.Length > 0;

    private void Awake()
    {
        CacheReferences();
        SetLivingState();
    }

    private void OnDisable()
    {
        RestoreCarrierCollisions();
        hasCarryTarget = false;
    }

    private void FixedUpdate()
    {
        if (!isRagdollActive || !isCarried || isStored || !hasCarryTarget || hipsBody == null)
        {
            return;
        }

        hipsBody.MovePosition(carryTargetPosition);
        hipsBody.MoveRotation(carryTargetRotation);
    }

    private void LateUpdate()
    {
        if (isRagdollActive && !isStored)
        {
            FollowHipsWithLogicalRoot();
        }
    }

    [ContextMenu("Refresh Ragdoll References")]
    public void RefreshReferences()
    {
        CacheReferences();
    }

    [ContextMenu("Validate Ragdoll Configuration")]
    private void ValidateConfiguration()
    {
        CacheReferences();
        if (!HasConfiguredRagdoll)
        {
            Debug.LogWarning($"[NPCRagdoll] {name}: faltan Animator, CapsuleCollider raiz, Hips o componentes fisicos del humanoide.", this);
            return;
        }

        int jointCount = humanoidRoot.GetComponentsInChildren<CharacterJoint>(true).Length;
        Debug.Log($"[NPCRagdoll] {name}: Hips={hipsBody.name}, {ragdollBodies.Length} rigidbodies, "
            + $"{ragdollColliders.Length} colliders y {jointCount} joints.", this);
    }

    public bool ActivateRagdoll()
    {
        if (isRagdollActive)
        {
            return true;
        }

        CacheReferences();
        if (!HasConfiguredRagdoll)
        {
            Debug.LogWarning($"[NPCRagdoll] {name}: no se encontro un ragdoll Humanoid completo; se conserva la colision raiz.", this);
            return false;
        }

        isRagdollActive = true;
        isCarried = false;
        isStored = false;
        hasCarryTarget = false;

        foreach (Animator rigAnimator in rigAnimators)
        {
            if (rigAnimator != null)
            {
                rigAnimator.enabled = false;
            }
        }

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            agent.enabled = false;
        }

        rootCollider.enabled = false;
        SetRagdollCollidersEnabled(true);
        Physics.SyncTransforms();

        foreach (Rigidbody body in ragdollBodies)
        {
            body.isKinematic = false;
            body.useGravity = true;
            body.detectCollisions = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }

        FollowHipsWithLogicalRoot();
        return true;
    }

    public void SetCarried(bool carried)
    {
        SetCarried(carried, null);
    }

    public void SetCarried(bool carried, Collider[] carrierColliders)
    {
        if (!isRagdollActive || isStored || isCarried == carried)
        {
            return;
        }

        if (carried)
        {
            IgnoreCarrierCollisions(carrierColliders);
            hipsBody.linearVelocity = Vector3.zero;
            hipsBody.angularVelocity = Vector3.zero;
            hipsBody.isKinematic = true;
            hipsBody.useGravity = false;
            hipsBody.detectCollisions = true;
            carryTargetPosition = hipsBody.position;
            carryTargetRotation = hipsBody.rotation;
            hasCarryTarget = true;
            isCarried = true;
            return;
        }

        hasCarryTarget = false;
        isCarried = false;
        Physics.SyncTransforms();

        foreach (Rigidbody body in ragdollBodies)
        {
            body.isKinematic = false;
            body.useGravity = true;
            body.detectCollisions = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }

        RestoreCarrierCollisions();
        FollowHipsWithLogicalRoot();
    }

    public bool SetCarryTarget(Vector3 targetHipsPosition, Quaternion targetHipsRotation)
    {
        if (!isRagdollActive || !isCarried || isStored || hipsBody == null)
        {
            return false;
        }

        carryTargetPosition = targetHipsPosition;
        carryTargetRotation = targetHipsRotation;
        hasCarryTarget = true;
        return true;
    }

    public void PrepareForHide()
    {
        if (!isRagdollActive || isStored)
        {
            return;
        }

        hasCarryTarget = false;
        isCarried = false;

        foreach (Rigidbody body in ragdollBodies)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = false;
        }

        SetRagdollCollidersEnabled(false);
        RestoreCarrierCollisions();
        FollowHipsWithLogicalRoot();
        isStored = true;
    }

    private void CacheReferences()
    {
        CapsuleCollider livingCapsule = GetComponent<CapsuleCollider>();
        if (rootCollider != livingCapsule)
        {
            rootCollider = livingCapsule;
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        rigAnimators = GetComponentsInChildren<Animator>(true);
        ragdollBodies = GetComponentsInChildren<Rigidbody>(true);
        List<Collider> colliders = new List<Collider>();
        foreach (Rigidbody body in ragdollBodies)
        {
            foreach (Collider collider in body.GetComponents<Collider>())
            {
                if (collider != rootCollider)
                {
                    colliders.Add(collider);
                }
            }
        }
        ragdollColliders = colliders.ToArray();

        if (hipsBody == null || !ContainsBody(hipsBody))
        {
            hipsBody = FindHipsBody();
        }

        if (hipsBody != null)
        {
            humanoidRoot = hipsBody.transform;
            while (humanoidRoot.parent != null && humanoidRoot.parent != transform)
            {
                humanoidRoot = humanoidRoot.parent;
            }
            if (humanoidRoot.parent != transform)
            {
                humanoidRoot = null;
            }
        }
    }

    private Rigidbody FindHipsBody()
    {
        if (animator != null && animator.avatar != null && animator.avatar.isValid && animator.isHuman)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null)
            {
                Rigidbody humanHips = hips.GetComponent<Rigidbody>();
                if (humanHips != null && ContainsBody(humanHips))
                {
                    return humanHips;
                }
            }
        }

        Rigidbody unjointedBody = null;
        foreach (Rigidbody body in ragdollBodies)
        {
            if (body.GetComponent<Joint>() != null)
            {
                continue;
            }
            if (unjointedBody != null)
            {
                return null;
            }
            unjointedBody = body;
        }
        return unjointedBody;
    }

    private bool ContainsBody(Rigidbody candidate)
    {
        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == candidate)
            {
                return true;
            }
        }
        return false;
    }

    private void SetLivingState()
    {
        if (isRagdollActive)
        {
            return;
        }

        foreach (Rigidbody body in ragdollBodies)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = false;
        }
        SetRagdollCollidersEnabled(false);
        if (rootCollider != null)
        {
            rootCollider.enabled = true;
        }
    }

    private void SetRagdollCollidersEnabled(bool enabled)
    {
        foreach (Collider collider in ragdollColliders)
        {
            if (collider != null)
            {
                collider.enabled = enabled;
            }
        }
    }

    private void IgnoreCarrierCollisions(Collider[] carrierColliders)
    {
        RestoreCarrierCollisions();
        if (carrierColliders == null)
        {
            return;
        }

        foreach (Collider carrierCollider in carrierColliders)
        {
            if (carrierCollider == null || ignoredCarrierColliders.Contains(carrierCollider))
            {
                continue;
            }

            ignoredCarrierColliders.Add(carrierCollider);
            foreach (Collider ragdollCollider in ragdollColliders)
            {
                if (ragdollCollider != null)
                {
                    Physics.IgnoreCollision(ragdollCollider, carrierCollider, true);
                }
            }
        }
    }

    private void RestoreCarrierCollisions()
    {
        if (ragdollColliders == null)
        {
            ignoredCarrierColliders.Clear();
            return;
        }

        foreach (Collider carrierCollider in ignoredCarrierColliders)
        {
            if (carrierCollider == null)
            {
                continue;
            }
            foreach (Collider ragdollCollider in ragdollColliders)
            {
                if (ragdollCollider != null)
                {
                    Physics.IgnoreCollision(ragdollCollider, carrierCollider, false);
                }
            }
        }
        ignoredCarrierColliders.Clear();
    }

    private void FollowHipsWithLogicalRoot()
    {
        if (hipsBody == null || humanoidRoot == null)
        {
            return;
        }

        Vector3 hipsPosition = hipsBody.transform.position;
        if ((transform.position - hipsPosition).sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 humanoidWorldPosition = humanoidRoot.position;
        Quaternion humanoidWorldRotation = humanoidRoot.rotation;
        transform.position = hipsPosition;
        humanoidRoot.SetPositionAndRotation(humanoidWorldPosition, humanoidWorldRotation);
    }
}
