using System;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;

namespace ROM.UI.Sprint01
{
    [ExecuteAlways]
    [DefaultExecutionOrder(300)]
    public sealed class MenuHandPose : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] EquipmentBoneRotation[] fingers = Array.Empty<EquipmentBoneRotation>();
        [SerializeField] Transform supportGrip;
        [SerializeField] Vector3 rightHandOffset;
        [SerializeField] Vector3 rightHandOffsetRotation;
        [SerializeField] Transform shoulderCarrySocket;
        EquipmentArmIK supportArm, rightArm;
        Transform[] bones;

        void OnEnable()
        {
            if (!animator || !animator.isHuman) return;
            supportArm = supportGrip ? new EquipmentArmIK(animator, HumanBodyBones.LeftHand) : null;
            rightArm = supportGrip || shoulderCarrySocket ? new EquipmentArmIK(animator, HumanBodyBones.RightHand) : null;
            bones = new Transform[fingers.Length];
            for (int i = 0; i < fingers.Length; i++)
                bones[i] = animator.GetBoneTransform(fingers[i].Bone);
        }

        void Update() { if (Application.IsPlaying(gameObject)) RestoreArms(); }

        void OnDisable() => RestoreArms();

        void RestoreArms()
        {
            supportArm?.Restore();
            rightArm?.Restore();
        }

        void LateUpdate() { if (Application.IsPlaying(gameObject)) ApplyPose(); }

#if UNITY_EDITOR
        public void ApplyAnimationPreview()
        {
            if (!animator || !animator.isHuman) return;
            if (bones == null || bones.Length != fingers.Length) OnEnable();
            var previewBones = new System.Collections.Generic.List<Transform>(bones);
            if (supportGrip || shoulderCarrySocket)
                foreach (var joint in new[] { HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                    HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand })
                    previewBones.Add(animator.GetBoneTransform(joint));
            foreach (var bone in previewBones)
                if (bone)
                    foreach (var axis in new[] { "x", "y", "z", "w" })
                    {
                        var path = UnityEditor.AnimationUtility.CalculateTransformPath(bone, animator.transform);
                        var binding = UnityEditor.EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + axis);
                        if (UnityEditor.AnimationMode.IsPropertyAnimated(bone, binding.propertyName)) continue;
                        UnityEditor.AnimationMode.AddPropertyModification(binding, new UnityEditor.PropertyModification
                        {
                            target = bone,
                            propertyPath = binding.propertyName,
                            value = bone.localRotation["xyzw".IndexOf(axis, StringComparison.Ordinal)]
                                .ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                        }, true);
                    }
            RestoreArms();
            ApplyPose();
        }
#endif

        public void ApplyPose()
        {
            if (bones == null || bones.Length != fingers.Length) OnEnable();
            if (bones == null) return;
            if (rightArm != null && (shoulderCarrySocket || rightHandOffset.sqrMagnitude > .000001f || rightHandOffsetRotation.sqrMagnitude > .000001f))
            {
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand)
                {
                    var target = hand.position + animator.transform.rotation * (rightHandOffset * animator.transform.lossyScale.y);
                    var rotation = hand.rotation;
                    var hint = Vector3.zero;
                    if (shoulderCarrySocket)
                    {
                        var shoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                        if (shoulder)
                        {
                            var frame = animator.transform;
                            var rest = shoulder.position + frame.rotation * new Vector3(.04f, .13f, -.04f) * frame.lossyScale.y;
                            var shaft = rest - target;
                            if (shaft.sqrMagnitude > .000001f)
                                rotation = Quaternion.LookRotation(shaft, frame.right) * Quaternion.Inverse(shoulderCarrySocket.localRotation);
                            hint = frame.rotation * new Vector3(.18f, -.18f, 0f) * frame.lossyScale.y;
                        }
                    }
                    target += rightArm.ReachCorrection(target);
                    rightArm.Solve(target, rotation * Quaternion.Euler(rightHandOffsetRotation), hint, 1f);
                }
            }
            if (supportGrip) supportArm?.Solve(supportGrip.position, supportGrip.rotation, Vector3.zero, 1f, useFixedPole: false);
            for (int i = 0; i < bones.Length; i++)
                if (bones[i]) bones[i].localRotation = fingers[i].Rotation;
        }
    }
}
