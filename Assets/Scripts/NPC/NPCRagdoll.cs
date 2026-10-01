using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NPCRagdoll : MonoBehaviour
{
    [Serializable]
    private sealed class DirectBoneBinding
    {
        [SerializeField] private string label;
        [SerializeField] private Transform proxyBone;
        [SerializeField] private Transform visualBone;
        [SerializeField] private bool drivesProxyWhenAlive = true;

        [NonSerialized] private Vector3 visualPositionOffset;
        [NonSerialized] private Quaternion visualRotationOffset = Quaternion.identity;
        [NonSerialized] private Vector3 visualLocalPosition;
        [NonSerialized] private bool hasCapturedOffset;

        public Transform ProxyBone => proxyBone;
        public Transform VisualBone => visualBone;
        public bool DrivesProxyWhenAlive => drivesProxyWhenAlive;
        public bool IsValid => proxyBone != null && visualBone != null;

        public DirectBoneBinding(
            string bindingLabel,
            Transform proxy,
            Transform visual,
            bool drivesProxy)
        {
            label = bindingLabel;
            proxyBone = proxy;
            visualBone = visual;
            drivesProxyWhenAlive = drivesProxy;
        }

        public void CaptureOffset()
        {
            if (!IsValid)
            {
                hasCapturedOffset = false;
                return;
            }

            visualPositionOffset = proxyBone.InverseTransformPoint(visualBone.position);
            visualRotationOffset = Quaternion.Inverse(proxyBone.rotation) * visualBone.rotation;
            visualLocalPosition = visualBone.localPosition;
            hasCapturedOffset = true;
        }

        public void MoveProxyFromVisual()
        {
            if (!IsValid || !drivesProxyWhenAlive || !hasCapturedOffset)
            {
                return;
            }

            Quaternion proxyRotation = visualBone.rotation * Quaternion.Inverse(visualRotationOffset);
            Vector3 scaledOffset = Vector3.Scale(visualPositionOffset, proxyBone.lossyScale);
            Vector3 proxyPosition = visualBone.position - proxyRotation * scaledOffset;
            proxyBone.SetPositionAndRotation(proxyPosition, proxyRotation);
        }

        public void MoveVisualFromProxy()
        {
            if (!IsValid || !hasCapturedOffset)
            {
                return;
            }

            visualBone.localPosition = visualLocalPosition;

            Quaternion targetWorldRotation = proxyBone.rotation * visualRotationOffset;
            visualBone.localRotation = visualBone.parent != null
                ? Quaternion.Inverse(visualBone.parent.rotation) * targetWorldRotation
                : targetWorldRotation;
        }
    }

    [Serializable]
    private sealed class IntermediateBoneChain
    {
        [SerializeField] private string label;
        [SerializeField] private Transform startProxy;
        [SerializeField] private Transform endProxy;
        [SerializeField] private Transform startVisual;
        [SerializeField] private Transform endVisual;
        [SerializeField] private Transform[] visualBones = Array.Empty<Transform>();
        [SerializeField, Range(0f, 1f)] private float distalInfluenceScale = 1f;

        [NonSerialized] private float[] interpolationWeights = Array.Empty<float>();
        [NonSerialized] private Vector3[] visualLocalPositions = Array.Empty<Vector3>();
        [NonSerialized] private Quaternion[] visualReferenceLocalRotations =
            Array.Empty<Quaternion>();
        [NonSerialized] private Quaternion[] visualReferenceParentWorldRotations =
            Array.Empty<Quaternion>();
        [NonSerialized] private Quaternion startProxyReferenceWorldRotation = Quaternion.identity;
        [NonSerialized] private Quaternion endProxyReferenceWorldRotation = Quaternion.identity;
        [NonSerialized] private bool hasCapturedOffsets;

        public bool IsValid =>
            startProxy != null &&
            endProxy != null &&
            startVisual != null &&
            endVisual != null &&
            visualBones != null &&
            visualBones.Length > 0;

        public IntermediateBoneChain(
            string chainLabel,
            Transform start,
            Transform end,
            Transform visualStart,
            Transform visualEnd,
            Transform[] intermediateBones,
            float endInfluenceScale)
        {
            label = chainLabel;
            startProxy = start;
            endProxy = end;
            startVisual = visualStart;
            endVisual = visualEnd;
            visualBones = intermediateBones ?? Array.Empty<Transform>();
            distalInfluenceScale = Mathf.Clamp01(endInfluenceScale);
        }

        public void CaptureOffsets()
        {
            if (!IsValid)
            {
                hasCapturedOffsets = false;
                return;
            }

            int count = visualBones.Length;
            interpolationWeights = new float[count];
            visualLocalPositions = new Vector3[count];
            visualReferenceLocalRotations = new Quaternion[count];
            visualReferenceParentWorldRotations = new Quaternion[count];

            Array.Sort(
                visualBones,
                (left, right) => GetHierarchyDepth(left).CompareTo(GetHierarchyDepth(right))
            );

            startProxyReferenceWorldRotation = startProxy.rotation;
            endProxyReferenceWorldRotation = endProxy.rotation;

            float totalPathLength = CalculateVisualPathLength();
            float travelledPathLength = 0f;
            Transform previousVisual = startVisual;

            for (int index = 0; index < count; index++)
            {
                Transform visualBone = visualBones[index];
                if (visualBone == null)
                {
                    continue;
                }

                travelledPathLength += Vector3.Distance(previousVisual.position, visualBone.position);
                float pathWeight = totalPathLength > 0.0001f
                    ? Mathf.Clamp01(travelledPathLength / totalPathLength)
                    : (index + 1f) / (count + 1f);

                interpolationWeights[index] = Mathf.Clamp01(pathWeight * distalInfluenceScale);
                visualLocalPositions[index] = visualBone.localPosition;
                visualReferenceLocalRotations[index] = visualBone.localRotation;
                visualReferenceParentWorldRotations[index] = visualBone.parent != null
                    ? visualBone.parent.rotation
                    : Quaternion.identity;
                previousVisual = visualBone;
            }

            hasCapturedOffsets = true;
        }

        public void ApplyVisualPose()
        {
            if (!IsValid || !hasCapturedOffsets)
            {
                return;
            }

            Quaternion startRotationDelta =
                startProxy.rotation * Quaternion.Inverse(startProxyReferenceWorldRotation);
            Quaternion endRotationDelta =
                endProxy.rotation * Quaternion.Inverse(endProxyReferenceWorldRotation);

            for (int index = 0; index < visualBones.Length; index++)
            {
                Transform visualBone = visualBones[index];
                if (visualBone == null)
                {
                    continue;
                }

                float weight = interpolationWeights[index];
                Quaternion distributedRotationDelta =
                    Quaternion.Slerp(startRotationDelta, endRotationDelta, weight);

                Quaternion referenceWorldRotation =
                    visualReferenceParentWorldRotations[index] *
                    visualReferenceLocalRotations[index];
                Quaternion targetWorldRotation =
                    distributedRotationDelta * referenceWorldRotation;

                visualBone.localPosition = visualLocalPositions[index];
                visualBone.localRotation = visualBone.parent != null
                    ? Quaternion.Inverse(visualBone.parent.rotation) * targetWorldRotation
                    : targetWorldRotation;
            }
        }

        private float CalculateVisualPathLength()
        {
            float totalLength = 0f;
            Transform previousVisual = startVisual;

            foreach (Transform visualBone in visualBones)
            {
                if (visualBone == null)
                {
                    continue;
                }

                totalLength += Vector3.Distance(previousVisual.position, visualBone.position);
                previousVisual = visualBone;
            }

            totalLength += Vector3.Distance(previousVisual.position, endVisual.position);
            return totalLength;
        }
    }

    private const int ExpectedPrimaryBindingCount = 11;

    [Header("Referencias principales")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider rootCollider;
    [Tooltip("Raiz comun del modelo importado. Debe incluir el mesh y todas las ramas ORG/DEF.")]
    [SerializeField] private Transform visualRigRoot;
    [Tooltip("Referencia visual legacy. La pelvis fisica real es Proxy Pelvis.")]
    [SerializeField] private Transform pelvis;

    [Header("Ragdoll Proxy")]
    [SerializeField] private Transform proxyRoot;
    [SerializeField] private Rigidbody proxyPelvis;
    [SerializeField] private Rigidbody[] ragdollBodies = Array.Empty<Rigidbody>();
    [SerializeField] private Collider[] ragdollColliders = Array.Empty<Collider>();

    [Header("Bindings del rig visual")]
    [SerializeField] private List<DirectBoneBinding> directBindings = new List<DirectBoneBinding>();
    [SerializeField] private List<IntermediateBoneChain> intermediateChains = new List<IntermediateBoneChain>();
    [SerializeField] private float ragdollVisualHeightOffset = 0.05f;

    [Header("Configuracion")]
    [SerializeField] private bool disableRootColliderOnDeath = true;
    [SerializeField] private float maxAngularVelocity = 20f;

    [Header("Colocacion al soltar")]
    [Min(0.1f)]
    [SerializeField] private float dropRaycastDistance = 4f;
    [Min(0f)]
    [SerializeField] private float dropPelvisGroundClearance = 0.45f;
    [SerializeField] private LayerMask dropGroundMask = ~0;

    [Header("Correccion inicial de suelo")]
    [Min(0.1f)]
    [SerializeField] private float initialGroundCheckDistance = 2f;
    [Min(0f)]
    [SerializeField] private float initialGroundClearance = 0.04f;
    [Min(0f)]
    [SerializeField] private float maxInitialGroundCorrection = 0.5f;

    private bool isRagdollActive;
    private bool isCarried;
    private bool isStored;
    private bool bindingsInitialized;
    private readonly List<DirectBoneBinding> visualBindingsInHierarchyOrder =
        new List<DirectBoneBinding>();
    private Vector3 visualRigRootPositionOffset;
    private Quaternion visualRigRootRotationOffset = Quaternion.identity;
    private bool hasCapturedVisualRigRootOffset;
    private Vector3 carryTargetPosition;
    private Quaternion carryTargetRotation = Quaternion.identity;
    private bool hasCarryTarget;

    public bool IsRagdollActive => isRagdollActive;
    public bool IsCarried => isCarried;
    public Transform Pelvis => proxyPelvis != null ? proxyPelvis.transform : pelvis;
    public bool HasConfiguredRagdoll =>
        proxyRoot != null &&
        proxyPelvis != null &&
        ragdollBodies != null &&
        ragdollBodies.Length > 0 &&
        ragdollColliders != null &&
        ragdollColliders.Length > 0;

    private void Awake()
    {
        CacheReferences();
        CaptureBindingOffsets();
        SetInitialState();
        SyncProxyFromVisual();
        CaptureVisualRigRootOffset();
        Physics.SyncTransforms();
    }

    private void LateUpdate()
    {
        if (isStored || !bindingsInitialized)
        {
            return;
        }

        if (isRagdollActive)
        {
            SyncVisualFromProxy();
        }
        else
        {
            SyncProxyFromVisual();
        }
    }

    private void FixedUpdate()
    {
        if (!isRagdollActive || !isCarried || isStored ||
            !hasCarryTarget || proxyPelvis == null)
        {
            return;
        }

        proxyPelvis.MovePosition(carryTargetPosition);
        proxyPelvis.MoveRotation(carryTargetRotation);
    }

    [ContextMenu("Refresh Ragdoll References")]
    public void RefreshReferences()
    {
        CacheReferences(true);
        CaptureBindingOffsets();
        CaptureVisualRigRootOffset();
    }

    [ContextMenu("Validate Ragdoll Configuration")]
    private void ValidateConfiguration()
    {
        CacheReferences(true);
        CaptureBindingOffsets();

        if (!HasConfiguredRagdoll)
        {
            Debug.LogWarning(
                $"[NPCRagdoll] {gameObject.name} necesita un proxy con Pelvis, Rigidbody y Collider.",
                this
            );
            return;
        }

        int jointCount = proxyRoot.GetComponentsInChildren<CharacterJoint>(true).Length;
        if (jointCount == 0)
        {
            Debug.LogWarning(
                $"[NPCRagdoll] {gameObject.name} no tiene CharacterJoint dentro del proxy.",
                this
            );
            return;
        }

        int primaryBindingCount = CountPrimaryBindings();
        if (primaryBindingCount < ExpectedPrimaryBindingCount)
        {
            Debug.LogWarning(
                $"[NPCRagdoll] {gameObject.name} tiene {primaryBindingCount}/" +
                $"{ExpectedPrimaryBindingCount} bindings principales validos.",
                this
            );
            return;
        }

        Debug.Log(
            $"[NPCRagdoll] Proxy valido en {gameObject.name}: " +
            $"{ragdollBodies.Length} rigidbodies, {ragdollColliders.Length} colliders, " +
            $"{jointCount} joints y {primaryBindingCount} bindings principales.",
            this
        );
    }

    public bool ActivateRagdoll()
    {
        CacheReferences();
        EnsureBindingsInitialized();

        if (!HasConfiguredRagdoll || CountPrimaryBindings() < ExpectedPrimaryBindingCount)
        {
            Debug.LogWarning(
                $"[NPCRagdoll] {gameObject.name} no tiene un RagdollProxy completo o sus bindings son invalidos.",
                this
            );
            return false;
        }

        SetBodiesState(true, false, false);
        SyncProxyFromVisual();
        CaptureBindingOffsets();
        CaptureVisualRigRootOffset();

        isRagdollActive = true;
        isCarried = false;
        isStored = false;

        if (animator != null)
        {
            animator.enabled = false;
        }

        if (disableRootColliderOnDeath && rootCollider != null)
        {
            rootCollider.enabled = false;
        }

        SetRagdollCollidersEnabled(true);
        Physics.SyncTransforms();
        ResolveInitialGroundPenetration();
        Physics.SyncTransforms();
        SetBodiesState(false, true, true);
        SyncVisualFromProxy();
        return true;
    }

    public void SetCarried(bool carried)
    {
        if (!isRagdollActive || isStored || isCarried == carried)
        {
            return;
        }

        if (carried)
        {
            SetRagdollCollidersEnabled(false);
            SetCarriedBodiesState();
            carryTargetPosition = proxyPelvis.position;
            carryTargetRotation = proxyPelvis.rotation;
            hasCarryTarget = true;
            isCarried = true;
            SyncVisualFromProxy();
            return;
        }

        hasCarryTarget = false;
        SetBodiesState(true, false, false);
        TryPlaceProxyAboveGround();
        RebaseOwnerRootPreservingWorldPose();
        Physics.SyncTransforms();
        SetRagdollCollidersEnabled(true);
        SetBodiesState(false, true, true);
        isCarried = false;
        SyncVisualFromProxy();
    }

    public bool MoveCarriedProxy(
        Vector3 targetPelvisPosition,
        Quaternion targetPelvisRotation)
    {
        if (!isRagdollActive || !isCarried || isStored || proxyPelvis == null)
        {
            return false;
        }

        carryTargetPosition = targetPelvisPosition;
        carryTargetRotation = targetPelvisRotation;
        hasCarryTarget = true;
        return true;
    }

    public void PrepareForHide()
    {
        if (!isRagdollActive)
        {
            return;
        }

        hasCarryTarget = false;
        SetBodiesState(true, false, false);
        SetRagdollCollidersEnabled(false);
        RebaseOwnerRootPreservingWorldPose();
        isCarried = false;
        isStored = true;
        SyncVisualFromProxy();
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

        if (proxyRoot == null)
        {
            proxyRoot = FindDescendantByName(transform, "RagdollProxy");
            if (proxyRoot == null)
            {
                proxyRoot = FindDescendantByName(transform, "Ragdoll");
            }
        }

        if (proxyRoot != null && proxyPelvis == null)
        {
            Transform proxyPelvisTransform = FindDescendantByName(proxyRoot, "Pelvis");
            if (proxyPelvisTransform != null)
            {
                proxyPelvis = proxyPelvisTransform.GetComponent<Rigidbody>();
            }
        }

        ResolveVisualRigRoot();

        if (forceRefresh || ragdollBodies == null || ragdollBodies.Length == 0)
        {
            ragdollBodies = proxyRoot != null
                ? proxyRoot.GetComponentsInChildren<Rigidbody>(true)
                : Array.Empty<Rigidbody>();

            Array.Sort(ragdollBodies, CompareBodiesByHierarchyDepth);
        }

        if (pelvis == null)
        {
            pelvis = FindVisualTransform("DEF-pelvis.R");
        }

        if (forceRefresh || ragdollColliders == null || ragdollColliders.Length == 0)
        {
            List<Collider> validColliders = new List<Collider>();

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

            ragdollColliders = validColliders.ToArray();
        }

        ConfigureBindings(forceRefresh);
    }

    private void ConfigureBindings(bool forceRefresh)
    {
        if (proxyRoot == null)
        {
            return;
        }

        if (forceRefresh || directBindings == null || directBindings.Count == 0)
        {
            directBindings = new List<DirectBoneBinding>();

            Transform proxyPelvisTransform = FindDescendantByName(proxyRoot, "Pelvis");
            Transform proxySpine = FindDescendantByName(proxyRoot, "Spine");
            Transform proxyHead = FindDescendantByName(proxyRoot, "Head");
            Transform proxyUpperArmLeft = FindDescendantByName(proxyRoot, "UpperArm.L");
            Transform proxyForearmLeft = FindDescendantByName(proxyRoot, "Forearm.L");
            Transform proxyUpperArmRight = FindDescendantByName(proxyRoot, "UpperArm.R");
            Transform proxyForearmRight = FindDescendantByName(proxyRoot, "Forearm.R");
            Transform proxyThighLeft = FindDescendantByName(proxyRoot, "Thigh.L");
            Transform proxyShinLeft = FindDescendantByName(proxyRoot, "Shin.L");
            Transform proxyThighRight = FindDescendantByName(proxyRoot, "Thigh.R");
            Transform proxyShinRight = FindDescendantByName(proxyRoot, "Shin.R");

            AddDirectBinding("Pelvis.R", proxyPelvisTransform, "DEF-pelvis.R", true);
            AddDirectBinding("Pelvis.L", proxyPelvisTransform, "DEF-pelvis.L", false);
            AddDirectBinding("Spine", proxySpine, "DEF-spine", true);
            AddDirectBinding("Head", proxyHead, "DEF-spine.006", true);
            AddDirectBinding("UpperArm.L", proxyUpperArmLeft, "DEF-upper_arm.L", true);
            AddDirectBinding("Forearm.L", proxyForearmLeft, "DEF-forearm.L", true);
            AddDirectBinding("UpperArm.R", proxyUpperArmRight, "DEF-upper_arm.R", true);
            AddDirectBinding("Forearm.R", proxyForearmRight, "DEF-forearm.R", true);
            AddDirectBinding("Thigh.L", proxyThighLeft, "DEF-thigh.L", true);
            AddDirectBinding("Shin.L", proxyShinLeft, "DEF-shin.L", true);
            AddDirectBinding("Thigh.R", proxyThighRight, "DEF-thigh.R", true);
            AddDirectBinding("Shin.R", proxyShinRight, "DEF-shin.R", true);

            directBindings.Sort(CompareBindingsByProxyDepth);
        }

        if (forceRefresh || IntermediateChainsNeedRefresh())
        {
            intermediateChains = new List<IntermediateBoneChain>();

            Transform proxySpine = FindDescendantByName(proxyRoot, "Spine");
            Transform proxyHead = FindDescendantByName(proxyRoot, "Head");
            Transform proxyUpperArmLeft = FindDescendantByName(proxyRoot, "UpperArm.L");
            Transform proxyForearmLeft = FindDescendantByName(proxyRoot, "Forearm.L");
            Transform proxyUpperArmRight = FindDescendantByName(proxyRoot, "UpperArm.R");
            Transform proxyForearmRight = FindDescendantByName(proxyRoot, "Forearm.R");
            Transform proxyThighLeft = FindDescendantByName(proxyRoot, "Thigh.L");
            Transform proxyShinLeft = FindDescendantByName(proxyRoot, "Shin.L");
            Transform proxyThighRight = FindDescendantByName(proxyRoot, "Thigh.R");
            Transform proxyShinRight = FindDescendantByName(proxyRoot, "Shin.R");

            AddIntermediateChain(
                "Spine",
                proxySpine,
                proxyHead,
                "DEF-spine",
                "DEF-spine.006",
                1f,
                "DEF-spine.001",
                "DEF-spine.002",
                "DEF-spine.003",
                "DEF-spine.004",
                "DEF-spine.005"
            );
            AddIntermediateChain(
                "UpperArm.L",
                proxyUpperArmLeft,
                proxyForearmLeft,
                "DEF-upper_arm.L",
                "DEF-forearm.L",
                0.5f,
                "DEF-upper_arm.L.001"
            );
            AddIntermediateChain(
                "UpperArm.R",
                proxyUpperArmRight,
                proxyForearmRight,
                "DEF-upper_arm.R",
                "DEF-forearm.R",
                0.5f,
                "DEF-upper_arm.R.001"
            );
            AddIntermediateChain(
                "Thigh.L",
                proxyThighLeft,
                proxyShinLeft,
                "DEF-thigh.L",
                "DEF-shin.L",
                0.5f,
                "DEF-thigh.L.001"
            );
            AddIntermediateChain(
                "Thigh.R",
                proxyThighRight,
                proxyShinRight,
                "DEF-thigh.R",
                "DEF-shin.R",
                0.5f,
                "DEF-thigh.R.001"
            );
        }
    }

    private void AddDirectBinding(
        string label,
        Transform proxyBone,
        string visualBoneName,
        bool drivesProxyWhenAlive)
    {
        directBindings.Add(
            new DirectBoneBinding(
                label,
                proxyBone,
                FindVisualTransform(visualBoneName),
                drivesProxyWhenAlive
            )
        );
    }

    private void AddIntermediateChain(
        string label,
        Transform startProxy,
        Transform endProxy,
        string startVisualBoneName,
        string endVisualBoneName,
        float distalInfluenceScale,
        params string[] visualBoneNames)
    {
        List<Transform> visualBones = new List<Transform>();
        foreach (string boneName in visualBoneNames)
        {
            Transform visualBone = FindVisualTransform(boneName);
            if (visualBone != null)
            {
                visualBones.Add(visualBone);
            }
        }

        intermediateChains.Add(
            new IntermediateBoneChain(
                label,
                startProxy,
                endProxy,
                FindVisualTransform(startVisualBoneName),
                FindVisualTransform(endVisualBoneName),
                visualBones.ToArray(),
                distalInfluenceScale
            )
        );
    }

    private bool IntermediateChainsNeedRefresh()
    {
        if (intermediateChains == null || intermediateChains.Count == 0)
        {
            return true;
        }

        foreach (IntermediateBoneChain chain in intermediateChains)
        {
            if (chain == null || !chain.IsValid)
            {
                return true;
            }
        }

        return false;
    }

    private void CaptureBindingOffsets()
    {
        bindingsInitialized = false;

        if (directBindings == null || directBindings.Count == 0)
        {
            return;
        }

        foreach (DirectBoneBinding binding in directBindings)
        {
            binding?.CaptureOffset();
        }

        visualBindingsInHierarchyOrder.Clear();
        visualBindingsInHierarchyOrder.AddRange(directBindings);
        visualBindingsInHierarchyOrder.Sort(CompareBindingsByVisualDepth);

        if (intermediateChains != null)
        {
            foreach (IntermediateBoneChain chain in intermediateChains)
            {
                chain?.CaptureOffsets();
            }
        }

        bindingsInitialized = CountPrimaryBindings() >= ExpectedPrimaryBindingCount;
    }

    private void EnsureBindingsInitialized()
    {
        if (!bindingsInitialized)
        {
            CaptureBindingOffsets();
        }
    }

    private void SyncProxyFromVisual()
    {
        if (!bindingsInitialized || directBindings == null)
        {
            return;
        }

        foreach (DirectBoneBinding binding in directBindings)
        {
            binding?.MoveProxyFromVisual();
        }
    }

    private void SyncVisualFromProxy()
    {
        if (!bindingsInitialized || directBindings == null)
        {
            return;
        }

        MoveVisualRigRootFromProxy();
        ApplyDirectVisualBindings();

        if (intermediateChains != null)
        {
            foreach (IntermediateBoneChain chain in intermediateChains)
            {
                chain?.ApplyVisualPose();
            }
        }

        // Los intermedios son ancestros de algunos bindings directos.
        ApplyDirectVisualBindings();
    }

    private void ApplyDirectVisualBindings()
    {
        IReadOnlyList<DirectBoneBinding> bindings = visualBindingsInHierarchyOrder.Count > 0
            ? visualBindingsInHierarchyOrder
            : directBindings;

        foreach (DirectBoneBinding binding in bindings)
        {
            binding?.MoveVisualFromProxy();
        }
    }

    private void ResolveVisualRigRoot()
    {
        if (visualRigRoot != null)
        {
            return;
        }

        if (animator != null && animator.transform != transform &&
            (proxyRoot == null || !animator.transform.IsChildOf(proxyRoot)))
        {
            visualRigRoot = animator.transform;
            return;
        }

        Transform visualBranch = FindVisualTransform("DEF-spine");
        if (visualBranch == null)
        {
            return;
        }

        while (visualBranch.parent != null && visualBranch.parent != transform)
        {
            visualBranch = visualBranch.parent;
        }

        if (visualBranch != proxyRoot)
        {
            visualRigRoot = visualBranch;
        }
    }

    private void CaptureVisualRigRootOffset()
    {
        hasCapturedVisualRigRootOffset = false;

        if (visualRigRoot == null || proxyPelvis == null)
        {
            return;
        }

        Transform physicalPelvis = proxyPelvis.transform;
        visualRigRootPositionOffset = physicalPelvis.InverseTransformPoint(visualRigRoot.position);
        visualRigRootRotationOffset =
            Quaternion.Inverse(physicalPelvis.rotation) * visualRigRoot.rotation;
        hasCapturedVisualRigRootOffset = true;
    }

    private void MoveVisualRigRootFromProxy()
    {
        if (!hasCapturedVisualRigRootOffset || visualRigRoot == null || proxyPelvis == null)
        {
            return;
        }

        Transform physicalPelvis = proxyPelvis.transform;
        Vector3 visualHeightOffset = isRagdollActive
            ? Vector3.up * ragdollVisualHeightOffset
            : Vector3.zero;
        visualRigRoot.position =
            physicalPelvis.TransformPoint(visualRigRootPositionOffset) + visualHeightOffset;
        visualRigRoot.rotation = physicalPelvis.rotation * visualRigRootRotationOffset;
    }

    private void SetInitialState()
    {
        isRagdollActive = false;
        isCarried = false;
        isStored = false;

        if (animator != null)
        {
            animator.enabled = true;
        }

        if (rootCollider != null)
        {
            rootCollider.enabled = true;
        }

        SetBodiesState(true, false, false);
        SetRagdollCollidersEnabled(false);
    }

    private void SetBodiesState(bool kinematic, bool useGravity, bool detectCollisions)
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

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.maxAngularVelocity = maxAngularVelocity;
            body.isKinematic = kinematic;
            body.useGravity = useGravity;
            body.detectCollisions = detectCollisions;

            if (!kinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
        }
    }

    private void SetCarriedBodiesState()
    {
        if (ragdollBodies == null || proxyPelvis == null)
        {
            return;
        }

        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == null)
            {
                continue;
            }

            bool isCarryAnchor = body == proxyPelvis;

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.maxAngularVelocity = maxAngularVelocity;
            body.isKinematic = isCarryAnchor;
            body.useGravity = !isCarryAnchor;
            body.detectCollisions = !isCarryAnchor;

            if (!isCarryAnchor)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
        }
    }

    private void SetRagdollCollidersEnabled(bool enabled)
    {
        if (ragdollColliders == null)
        {
            return;
        }

        foreach (Collider ragdollCollider in ragdollColliders)
        {
            if (ragdollCollider != null)
            {
                ragdollCollider.enabled = enabled;
            }
        }
    }

    private void MoveProxyAsRigidGroup(
        Vector3 targetPelvisPosition,
        Quaternion targetPelvisRotation)
    {
        if (proxyPelvis == null || ragdollBodies == null || ragdollBodies.Length == 0)
        {
            return;
        }

        Vector3 currentPelvisPosition = proxyPelvis.position;
        Quaternion rotationDelta = targetPelvisRotation * Quaternion.Inverse(proxyPelvis.rotation);
        Vector3[] targetPositions = new Vector3[ragdollBodies.Length];
        Quaternion[] targetRotations = new Quaternion[ragdollBodies.Length];

        for (int index = 0; index < ragdollBodies.Length; index++)
        {
            Rigidbody body = ragdollBodies[index];
            if (body == null)
            {
                continue;
            }

            Vector3 offsetFromPelvis = body.position - currentPelvisPosition;
            targetPositions[index] = targetPelvisPosition + rotationDelta * offsetFromPelvis;
            targetRotations[index] = rotationDelta * body.rotation;
        }

        for (int index = 0; index < ragdollBodies.Length; index++)
        {
            Rigidbody body = ragdollBodies[index];
            if (body == null)
            {
                continue;
            }

            body.position = targetPositions[index];
            body.rotation = targetRotations[index];
        }
    }

    private void ResolveInitialGroundPenetration()
    {
        if (proxyPelvis == null || ragdollColliders == null || ragdollColliders.Length == 0)
        {
            return;
        }

        bool hasBounds = false;
        Bounds combinedBounds = default;
        Bounds lowestColliderBounds = default;
        float lowestPoint = float.PositiveInfinity;

        foreach (Collider ragdollCollider in ragdollColliders)
        {
            if (ragdollCollider == null || !ragdollCollider.enabled ||
                !ragdollCollider.gameObject.activeInHierarchy || ragdollCollider.isTrigger)
            {
                continue;
            }

            Bounds colliderBounds = ragdollCollider.bounds;
            if (colliderBounds.size.sqrMagnitude <= Mathf.Epsilon)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = colliderBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(colliderBounds);
            }

            if (colliderBounds.min.y < lowestPoint)
            {
                lowestPoint = colliderBounds.min.y;
                lowestColliderBounds = colliderBounds;
            }
        }

        if (!hasBounds || float.IsPositiveInfinity(lowestPoint))
        {
            return;
        }

        int groundMask = dropGroundMask.value;
        if (proxyRoot != null)
        {
            groundMask &= ~(1 << proxyRoot.gameObject.layer);
        }

        Vector3 rayStart = new Vector3(
            lowestColliderBounds.center.x,
            combinedBounds.max.y + initialGroundClearance,
            lowestColliderBounds.center.z
        );
        float rayDistance =
            combinedBounds.size.y + initialGroundCheckDistance + initialGroundClearance;

        if (!Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                rayDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        float requiredCorrection = hit.point.y + initialGroundClearance - lowestPoint;
        if (requiredCorrection <= 0f)
        {
            return;
        }

        float appliedCorrection = Mathf.Min(requiredCorrection, maxInitialGroundCorrection);
        if (appliedCorrection <= 0f)
        {
            return;
        }

        Transform physicalPelvis = proxyPelvis.transform;
        MoveProxyAsRigidGroup(
            physicalPelvis.position + Vector3.up * appliedCorrection,
            physicalPelvis.rotation
        );

        if (requiredCorrection > maxInitialGroundCorrection + 0.0001f)
        {
            Debug.LogWarning(
                $"[NPCRagdoll] La correccion inicial de {gameObject.name} fue limitada a " +
                $"{maxInitialGroundCorrection:F2} m (requeria {requiredCorrection:F2} m).",
                this
            );
        }
    }

    private void TryPlaceProxyAboveGround()
    {
        if (proxyPelvis == null)
        {
            return;
        }

        Transform physicalPelvis = proxyPelvis.transform;
        Vector3 rayStart = physicalPelvis.position + Vector3.up * 0.5f;
        int groundMask = dropGroundMask.value;

        if (proxyRoot != null)
        {
            groundMask &= ~(1 << proxyRoot.gameObject.layer);
        }

        if (!Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                dropRaycastDistance + 0.5f,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Vector3 groundedPelvisPosition = physicalPelvis.position;
        groundedPelvisPosition.y = hit.point.y + dropPelvisGroundClearance;
        MoveProxyAsRigidGroup(groundedPelvisPosition, physicalPelvis.rotation);
    }

    private void RebaseOwnerRootPreservingWorldPose()
    {
        Transform physicalPelvis = proxyPelvis != null ? proxyPelvis.transform : pelvis;
        if (physicalPelvis == null || ragdollBodies == null || ragdollBodies.Length == 0)
        {
            return;
        }

        bool hasProxyRoot = proxyRoot != null;
        Vector3 proxyRootPosition = hasProxyRoot ? proxyRoot.position : Vector3.zero;
        Quaternion proxyRootRotation = hasProxyRoot ? proxyRoot.rotation : Quaternion.identity;

        bool hasVisualRoot = visualRigRoot != null;
        Vector3 visualRootPosition = hasVisualRoot ? visualRigRoot.position : Vector3.zero;
        Quaternion visualRootRotation = hasVisualRoot ? visualRigRoot.rotation : Quaternion.identity;

        Vector3[] positions = new Vector3[ragdollBodies.Length];
        Quaternion[] rotations = new Quaternion[ragdollBodies.Length];

        for (int index = 0; index < ragdollBodies.Length; index++)
        {
            Rigidbody body = ragdollBodies[index];
            if (body == null)
            {
                continue;
            }

            positions[index] = body.position;
            rotations[index] = body.rotation;
        }

        transform.position = physicalPelvis.position;

        if (hasProxyRoot)
        {
            proxyRoot.SetPositionAndRotation(proxyRootPosition, proxyRootRotation);
        }

        for (int index = 0; index < ragdollBodies.Length; index++)
        {
            Rigidbody body = ragdollBodies[index];
            if (body == null)
            {
                continue;
            }

            body.position = positions[index];
            body.rotation = rotations[index];
        }

        if (hasVisualRoot)
        {
            visualRigRoot.SetPositionAndRotation(visualRootPosition, visualRootRotation);
        }

        Physics.SyncTransforms();
    }

    private int CountPrimaryBindings()
    {
        if (directBindings == null)
        {
            return 0;
        }

        int count = 0;
        foreach (DirectBoneBinding binding in directBindings)
        {
            if (binding != null && binding.IsValid && binding.DrivesProxyWhenAlive)
            {
                count++;
            }
        }

        return count;
    }

    private Transform FindVisualTransform(string objectName)
    {
        Transform searchRoot = animator != null ? animator.transform : transform;
        Transform[] transforms = searchRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform candidate in transforms)
        {
            if (candidate != null && string.Equals(candidate.name, objectName, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    private static Transform FindDescendantByName(Transform searchRoot, string objectName)
    {
        if (searchRoot == null)
        {
            return null;
        }

        Transform[] transforms = searchRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform candidate in transforms)
        {
            if (candidate != null && string.Equals(candidate.name, objectName, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    private static int CompareBodiesByHierarchyDepth(Rigidbody left, Rigidbody right)
    {
        return GetHierarchyDepth(left != null ? left.transform : null)
            .CompareTo(GetHierarchyDepth(right != null ? right.transform : null));
    }

    private static int CompareBindingsByProxyDepth(DirectBoneBinding left, DirectBoneBinding right)
    {
        return GetHierarchyDepth(left != null ? left.ProxyBone : null)
            .CompareTo(GetHierarchyDepth(right != null ? right.ProxyBone : null));
    }

    private static int CompareBindingsByVisualDepth(DirectBoneBinding left, DirectBoneBinding right)
    {
        return GetHierarchyDepth(left != null ? left.VisualBone : null)
            .CompareTo(GetHierarchyDepth(right != null ? right.VisualBone : null));
    }

    private static int GetHierarchyDepth(Transform target)
    {
        int depth = 0;
        while (target != null)
        {
            depth++;
            target = target.parent;
        }

        return depth;
    }
}
