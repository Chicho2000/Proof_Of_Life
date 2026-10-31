using UnityEngine;

/// <summary>
/// Conserva la pose del brazo derecho en el espacio del jugador mientras camina.
/// Se aplica después de la animación para que caminar no desplace la mano ni el objeto equipado.
/// </summary>
[DefaultExecutionOrder(10000)]
public sealed class PlayerRightArmPoseLock : MonoBehaviour
{
    private static readonly HumanBodyBones[] ArmBones =
    {
        HumanBodyBones.RightShoulder,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.RightHand
    };

    private struct BonePose
    {
        public Transform bone;
        public Vector3 positionInPlayer;
        public Quaternion rotationInPlayer;
    }

    private FPSPlayerController player;
    private Transform playerRoot;
    private BonePose[] idlePose;
    private bool hasIdlePose;

    public void Initialize(FPSPlayerController owner)
    {
        Animator animator = GetComponent<Animator>();
        if (animator == null || !animator.isHuman) return;

        player = owner;
        playerRoot = owner.transform;
        idlePose = new BonePose[ArmBones.Length];
        for (int i = 0; i < ArmBones.Length; i++)
            idlePose[i].bone = animator.GetBoneTransform(ArmBones[i]);

        hasIdlePose = false;
    }

    private void LateUpdate()
    {
        if (player == null || playerRoot == null || idlePose == null) return;

        if (!player.IsMovingForArmPose)
        {
            CaptureIdlePose();
            return;
        }

        if (!hasIdlePose) return;

        for (int i = 0; i < idlePose.Length; i++)
        {
            Transform bone = idlePose[i].bone;
            if (bone == null) continue;

            bone.position = playerRoot.TransformPoint(idlePose[i].positionInPlayer);
            bone.rotation = playerRoot.rotation * idlePose[i].rotationInPlayer;
        }
    }

    private void CaptureIdlePose()
    {
        for (int i = 0; i < idlePose.Length; i++)
        {
            Transform bone = idlePose[i].bone;
            if (bone == null) continue;

            idlePose[i].positionInPlayer = playerRoot.InverseTransformPoint(bone.position);
            idlePose[i].rotationInPlayer = Quaternion.Inverse(playerRoot.rotation) * bone.rotation;
        }

        hasIdlePose = true;
    }
}
