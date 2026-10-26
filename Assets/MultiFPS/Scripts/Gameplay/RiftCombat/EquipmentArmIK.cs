using System;
using UnityEngine;

namespace MultiFPS.Gameplay.RiftCombat
{
    [Serializable]
    public sealed class EquipmentFingerPose
    {
        public HumanBodyBones Bone;
        public Vector3 Rotation;
    }
    [Serializable]
    public struct EquipmentPoseKey
    {
        [Range(0, 1)] public float Time;
        public float RightGripTwist, LeftGripTwist;
        public float RightGripSlide, LeftGripSlide;
        public Vector3 WeaponPosition, WeaponRotation, RightElbowOffset, LeftElbowOffset;
    }
    [Serializable]
    public sealed class EquipmentHandGrip
    {
        public HumanBodyBones Hand = HumanBodyBones.RightHand;
        [Tooltip("Wrist position in model local coordinates, including model scale.")]
        public Vector3 Position, Rotation;
        [Tooltip("Preserve this palm contact when blending calibrated wrist poses between clips.")]
        public bool PreservePalmContact;
        public Vector3 PalmOffset;
    }

    [Serializable]
    public sealed class EquipmentAnimationIK
    {
        public AnimationClip Clip;
        [Range(0, 1)] public float Weight = 1;
        [Tooltip("Model offset in the character body frame; independent of the solved hands.")]
        public Vector3 WeaponPosition, WeaponRotation;
        [Tooltip("Right wrist offset in model space. Left wrist has separate fields below.")]
        public Vector3 HandPosition, HandRotation;
        [Tooltip("Elbow displacement in the character body frame.")]
        public Vector3 ElbowOffset;
        public Vector3 LeftHandPosition, LeftHandRotation, LeftElbowOffset;
        [Tooltip("Local adjustment of the first thumb joint; zero preserves the animation rig pose.")]
        public Vector3 RightThumbRotation, LeftThumbRotation;
        [Tooltip("Optional corrections over normalized clip time. Twists rotate the complete grip around the model shaft, preserving the hand's contact shape.")]
        public EquipmentPoseKey[] PoseKeys = Array.Empty<EquipmentPoseKey>();
        public EquipmentPoseKey PoseAt(float time)
        {
            if (PoseKeys == null || PoseKeys.Length == 0) return default;
            if (time <= PoseKeys[0].Time) return PoseKeys[0];
            for (int i = 1; i < PoseKeys.Length; i++)
            {
                if (time > PoseKeys[i].Time) continue;
                var a = PoseKeys[i - 1]; var b = PoseKeys[i];
                float t = Mathf.InverseLerp(a.Time, b.Time, time);
                return new EquipmentPoseKey { Time = time, RightGripTwist = Mathf.Lerp(a.RightGripTwist, b.RightGripTwist, t), LeftGripTwist = Mathf.Lerp(a.LeftGripTwist, b.LeftGripTwist, t),
                    RightGripSlide = Mathf.Lerp(a.RightGripSlide,b.RightGripSlide,t), LeftGripSlide = Mathf.Lerp(a.LeftGripSlide,b.LeftGripSlide,t),
                    WeaponPosition = Vector3.Lerp(a.WeaponPosition, b.WeaponPosition, t), WeaponRotation = Vector3.Lerp(a.WeaponRotation,b.WeaponRotation,t), RightElbowOffset = Vector3.Lerp(a.RightElbowOffset,b.RightElbowOffset,t), LeftElbowOffset = Vector3.Lerp(a.LeftElbowOffset,b.LeftElbowOffset,t) };
            }
            return PoseKeys[PoseKeys.Length - 1];
        }
    }

    // A two-bone solve shared by every equipment type. Targets are captured before
    // either arm changes; no weapon is reattached to an already solved wrist.
    public sealed class EquipmentArmIK
    {
        readonly Transform upper, lower, hand;
        Quaternion upperPose, lowerPose, handPose;
        bool captured;
        public EquipmentArmIK(Animator animator, HumanBodyBones bone)
        {
            bool left = bone == HumanBodyBones.LeftHand;
            upper = animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            lower = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            hand = animator.GetBoneTransform(bone);
        }
        public void Restore()
        {
            if (!captured) return;
            if (upper) upper.localRotation = upperPose;
            if (lower) lower.localRotation = lowerPose;
            if (hand) hand.localRotation = handPose;
            captured = false;
        }
        public Vector3 ReachCorrection(Vector3 target)
        {
            if (!upper || !lower || !hand) return Vector3.zero;
            float reach = (Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position)) * .995f;
            var offset = target - upper.position;
            float distance = offset.magnitude;
            return distance > reach ? offset * (reach / distance - 1) : Vector3.zero;
        }
        public void Solve(Vector3 target, Quaternion rotation, Vector3 hintOffset, float weight)
        {
            if (!upper || !lower || !hand || weight <= 0) return;
            upperPose = upper.localRotation; lowerPose = lower.localRotation; handPose = hand.localRotation;
            captured = true;
            target = Vector3.Lerp(hand.position, target, weight);
            rotation = Quaternion.Slerp(hand.rotation, rotation, weight);
            var origin = upper.position;
            float a = Vector3.Distance(origin, lower.position), b = Vector3.Distance(lower.position, hand.position);
            var delta = target - origin;
            if (a < .00001f || b < .00001f || delta.sqrMagnitude < .00000001f) return;
            var direction = delta.normalized;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .00001f, a + b - .00001f);
            var pole = Vector3.ProjectOnPlane(lower.position + hintOffset * weight - origin, direction);
            if (pole.sqrMagnitude < .00000001f) pole = Vector3.ProjectOnPlane(upper.up, direction);
            if (pole.sqrMagnitude < .00000001f) pole = Vector3.Cross(direction, Vector3.right);
            float along = (a * a - b * b + distance * distance) / (2 * distance);
            var elbow = origin + direction * along + pole.normalized * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - origin, elbow - origin) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
            hand.rotation = rotation;
        }
    }
}
