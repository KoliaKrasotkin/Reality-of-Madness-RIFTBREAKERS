using System;
using UnityEngine;

namespace MultiFPS.Gameplay.RiftCombat
{
    [Serializable]
    public sealed class EquipmentFingerPose
    {
        public HumanBodyBones Bone;
        public Vector3 Rotation;
        public Vector3 RelaxedRotation;
        public Vector3 ExtendedRotation;
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
        [Tooltip("Attach the weapon to this animated hand using the grip's model-space pose.")]
        public bool DrivesSocket;
        [Tooltip("Only solve this support hand when the current clip requests it.")]
        public bool UseClipGripWeight;
        [Tooltip("Wrist position in model local coordinates, including model scale.")]
        public Vector3 Position, Rotation;
        [Tooltip("Preserve this palm contact when blending calibrated wrist poses between clips.")]
        public bool PreservePalmContact;
        public Vector3 PalmOffset;
    }

    [Serializable]
    public struct EquipmentBoneRotation
    {
        public HumanBodyBones Bone;
        public Quaternion Rotation;
    }
    [Serializable]
    public sealed class EquipmentAnimationIK
    {
        public AnimationClip Clip;
        [Tooltip("Finger rotations that override the equipment's default pose for this clip.")]
        public EquipmentBoneRotation[] FingerOverrides = Array.Empty<EquipmentBoneRotation>();
        [Tooltip("Blend these finger overrides in only while the weapon is stored. Useful when armed and unarmed share a clip.")]
        public bool FingerOverridesWhileStored;
        [Range(0, 1)] public float Weight = 1;
        [Tooltip("Model offset in the character body frame; independent of the solved hands.")]
        public Vector3 WeaponPosition, WeaponRotation;
        [Tooltip("Right wrist offset in model space. Left wrist has separate fields below.")]
        public Vector3 HandPosition, HandRotation;
        [Tooltip("Primary grip offsets in weapon model space for a grip with Drives Socket enabled.")]
        public Vector3 PrimaryGripPosition, PrimaryGripRotation;
        public bool AnchorBuiltInWeaponToPrimaryHand;
        [Tooltip("Move the anchoring hand in character body space before attaching the weapon.")]
        public Vector3 PrimaryCarryOffset;
        public float PrimaryCarryPitch;
        public bool UseTorsoRelativeCarry;
        [Tooltip("Elbow displacement in the character body frame.")]
        public Vector3 ElbowOffset;
        public Vector3 LeftHandPosition, LeftHandRotation;
        [Tooltip("Left elbow IK guide in character body space, in meters. X is sideways, Y is up and Z is forward. The wrist target stays on the grip.")]
        public Vector3 LeftElbowOffset;
        [Tooltip("Rotate the support hand around its palm contact on the handle, in degrees.")]
        public Vector3 LeftGripPivotRotation;
        [Tooltip("Additional left wrist pitch in degrees around the palm contact.")]
        public float LeftGripPitch;
        [Tooltip("Support hand contact over normalized clip time.")]
        public AnimationCurve LeftGripWeight = new AnimationCurve();
        [Range(0, 1)] public float LeftFistWeight;
        [Range(0, 1)]
        [Tooltip("Straighten lateral bend of the free left wrist while retaining the cast's palm tilt.")]
        public float FreeLeftWristAlignment;
        public float LeftArmSmoothStart, LeftArmSmoothEnd;
        [Range(0, 1)] public float LeftForwardRelease;
        public float CastPoseArrival;
        [Tooltip("Optional continuous local wrist arc over the end of a cast, in clip seconds.")]
        public float LeftWristArcStart, LeftWristArcEnd;
        public Quaternion LeftWristArcFrom = Quaternion.identity, LeftWristArcTo = Quaternion.identity;
        public EquipmentBoneRotation[] CastPose = Array.Empty<EquipmentBoneRotation>();
        public Quaternion LeftUpperArmStart, LeftUpperArmEnd, LeftForearmStart, LeftForearmEnd;
        [Range(0, 1)] public float RightWristAlignment;
        [Range(0, 1)] public float UprightLeftPalm;
        [Range(0, 1)]
        [Tooltip("Blend the right arm into a low carry using the animated left arm as reference.")]
        public float LowRightCarry;
        [Tooltip("Swing the carry arm opposite the left arm in locomotion cycles.")]
        public bool CounterSwingRightCarry;
        [Range(0, 1)] public float ForwardScepterAim;
        [Range(0, 1)] public float LeftCastFingerExtension;
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
        readonly Quaternion referenceWristRotation;
        Quaternion upperPose, lowerPose, handPose;
        bool captured;
        public EquipmentArmIK(Animator animator, HumanBodyBones bone)
        {
            bool left = bone == HumanBodyBones.LeftHand;
            upper = animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            lower = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            hand = animator.GetBoneTransform(bone);
            referenceWristRotation = hand ? hand.localRotation : Quaternion.identity;
        }
        public void Restore()
        {
            if (!captured) return;
            if (upper) upper.localRotation = upperPose;
            if (lower) lower.localRotation = lowerPose;
            if (hand) hand.localRotation = handPose;
            captured = false;
        }
        public Vector3 ReachCorrection(Vector3 target, float reachFraction = .995f)
        {
            if (!upper || !lower || !hand) return Vector3.zero;
            float reach = (Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position)) * reachFraction;
            var offset = target - upper.position;
            float distance = offset.magnitude;
            return distance > reach ? offset * (reach / distance - 1) : Vector3.zero;
        }
        public void Solve(Vector3 target, Quaternion rotation, Vector3 hintOffset, float weight,
            bool preserveLocalHandRotation = false, bool useFixedPole = false, float distributeWristTwist = 0f)
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
            var pole = Vector3.ProjectOnPlane(useFixedPole ? hintOffset : lower.position + hintOffset * weight - origin, direction);
            if (pole.sqrMagnitude < .00000001f) pole = Vector3.ProjectOnPlane(upper.up, direction);
            if (pole.sqrMagnitude < .00000001f) pole = Vector3.Cross(direction, Vector3.right);
            float along = (a * a - b * b + distance * distance) / (2 * distance);
            var elbow = origin + direction * along + pole.normalized * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            if (useFixedPole) target = origin + direction * distance;
            upper.rotation = Quaternion.FromToRotation(lower.position - origin, elbow - origin) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
            if (preserveLocalHandRotation) return;
            if (distributeWristTwist > 0f)
            {
                var axis = (hand.position - lower.position).normalized;
                var referenceRotation = hand.parent.rotation * referenceWristRotation;
                var wristDelta = rotation * Quaternion.Inverse(referenceRotation);
                float axial = Vector3.Dot(new Vector3(wristDelta.x, wristDelta.y, wristDelta.z), axis);
                float twist = Mathf.DeltaAngle(0f, 2f * Mathf.Atan2(axial, wristDelta.w) * Mathf.Rad2Deg);
                // Fade near the shortest-angle wrap to avoid a sudden forearm flip.
                float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(145f, 180f, Mathf.Abs(twist)));
                float turn = Mathf.Clamp(twist * .8f, -85f, 85f) * fade * distributeWristTwist;
                lower.rotation = Quaternion.AngleAxis(turn, axis) * lower.rotation;
            }
            hand.rotation = rotation;
        }
    }
}
