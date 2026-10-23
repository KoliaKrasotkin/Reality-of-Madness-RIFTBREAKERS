using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

// Runs before the arm IK constraints: read the clip's hands, place the axe and
// publish wrist targets. No feedback from last frame's solved hands or LateUpdate.
[Serializable]
public struct AnimatedWeaponGripData : IAnimationJobData
{
    public Transform weapon, leftHand, rightHand, leftTarget, rightTarget;
    public Vector3 leftPalmOffset, rightPalmOffset;
    public Vector3 leftGripAxis, rightGripAxis;
    public Vector3 rightContact;
    public float minimumSpacing, maximumSpacing;
    public Transform[] fingers;
    public Quaternion[] fingerRotations;
    [SyncSceneToStream, Range(0f, 1f)] public float fingerWeight;

    public bool IsValid() => weapon && leftHand && rightHand && leftTarget && rightTarget;
    public void SetDefaultValues()
    {
        weapon = leftHand = rightHand = leftTarget = rightTarget = null;
        leftPalmOffset = rightPalmOffset = Vector3.zero;
        leftGripAxis = rightGripAxis = Vector3.up;
        rightContact = new Vector3(0f, 0f, -0.08f);
        minimumSpacing = 0.16f;
        maximumSpacing = 0.5f;
        fingerWeight = 1f;
        fingers = Array.Empty<Transform>();
        fingerRotations = Array.Empty<Quaternion>();
    }
}

public struct AnimatedWeaponGripJob : IWeightedAnimationJob
{
    public ReadOnlyTransformHandle leftHand, rightHand;
    public ReadOnlyTransformHandle leftUpperArm, leftLowerArm, rightUpperArm, rightLowerArm;
    public ReadWriteTransformHandle weapon, leftTarget, rightTarget;
    public Vector3 leftPalmOffset, rightPalmOffset, leftGripAxis, rightGripAxis, rightContact;
    public float minimumSpacing, maximumSpacing, scale;
    public NativeArray<ReadWriteTransformHandle> fingers;
    public NativeArray<Quaternion> fingerRotations;
    public FloatProperty fingerWeight;
    public FloatProperty jobWeight { get; set; }

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        float weight = jobWeight.Get(stream);
        if (weight <= 0f) return;
        Quaternion leftRotation = leftHand.GetRotation(stream);
        Quaternion rightRotation = rightHand.GetRotation(stream);
        Vector3 leftPalm = leftHand.GetPosition(stream) + leftRotation * (leftPalmOffset * scale);
        Vector3 rightPalm = rightHand.GetPosition(stream) + rightRotation * (rightPalmOffset * scale);
        Vector3 between = rightPalm - leftPalm;
        float distance = between.magnitude;
        if (distance < 0.001f) return;
        Vector3 shaft = between / distance;
        Vector3 up = Vector3.ProjectOnPlane(rightRotation * Vector3.up, shaft);
        if (up.sqrMagnitude < 0.001f)
            up = Vector3.ProjectOnPlane(rightRotation * Vector3.forward, shaft);
        Quaternion weaponRotation = Quaternion.LookRotation(shaft, up.normalized);
        float spacing = Mathf.Clamp(distance, minimumSpacing * scale, maximumSpacing * scale);
        // Preserve the animated midpoint if the hands exceed the available handle.
        Vector3 midpoint = (leftPalm + rightPalm) * 0.5f;
        rightPalm = midpoint + shaft * (spacing * 0.5f);
        leftPalm = midpoint - shaft * (spacing * 0.5f);
        Vector3 weaponPosition = rightPalm - weaponRotation * (rightContact * scale);
        weapon.SetGlobalTR(stream, Vector3.Lerp(weapon.GetPosition(stream), weaponPosition, weight),
            Quaternion.Slerp(weapon.GetRotation(stream), weaponRotation, weight));

        leftRotation = AlignGrip(leftRotation, leftGripAxis, shaft);
        rightRotation = AlignGrip(rightRotation, rightGripAxis, shaft);
        Vector3 leftWrist = leftPalm - leftRotation * (leftPalmOffset * scale);
        Vector3 rightWrist = rightPalm - rightRotation * (rightPalmOffset * scale);
        Vector3 leftShoulder = leftUpperArm.GetPosition(stream);
        Vector3 rightShoulder = rightUpperArm.GetPosition(stream);
        float leftReach = ArmReach(leftShoulder, leftLowerArm.GetPosition(stream), leftHand.GetPosition(stream));
        float rightReach = ArmReach(rightShoulder, rightLowerArm.GetPosition(stream), rightHand.GetPosition(stream));
        // Translate the rigid grip as a whole into both arms' reachable space.
        // Attack poses can fully extend a wrist before the palm is aligned to the axe.
        Vector3 correction = Vector3.zero;
        for (int i = 0; i < 24; i++)
        {
            Vector3 previous = correction;
            correction += ReachCorrection(leftWrist + correction, leftShoulder, leftReach);
            correction += ReachCorrection(rightWrist + correction, rightShoulder, rightReach);
            if ((correction - previous).sqrMagnitude < 0.0000000001f) break;
        }
        weapon.SetPosition(stream, weapon.GetPosition(stream) + correction * weight);
        leftTarget.SetGlobalTR(stream, leftWrist + correction, leftRotation);
        rightTarget.SetGlobalTR(stream, rightWrist + correction, rightRotation);
        float curl = fingerWeight.Get(stream) * weight;
        for (int i = 0; i < fingers.Length; i++)
        {
            var finger = fingers[i];
            finger.SetLocalRotation(stream, Quaternion.Slerp(finger.GetLocalRotation(stream), fingerRotations[i], curl));
            fingers[i] = finger;
        }
    }

    private static float ArmReach(Vector3 shoulder, Vector3 elbow, Vector3 wrist) =>
        (Vector3.Distance(shoulder, elbow) + Vector3.Distance(elbow, wrist)) * 0.999f;

    private static Vector3 ReachCorrection(Vector3 wrist, Vector3 shoulder, float reach)
    {
        Vector3 offset = wrist - shoulder;
        float distance = offset.magnitude;
        return distance > reach ? offset * (reach / distance - 1f) : Vector3.zero;
    }

    private static Quaternion AlignGrip(Quaternion rotation, Vector3 axis, Vector3 shaft)
    {
        Vector3 current = rotation * axis;
        Vector3 desired = Vector3.Dot(current, shaft) >= 0f ? shaft : -shaft;
        return Quaternion.FromToRotation(current, desired) * rotation;
    }
}

public class AnimatedWeaponGripBinder : AnimationJobBinder<AnimatedWeaponGripJob, AnimatedWeaponGripData>
{
    public override AnimatedWeaponGripJob Create(Animator animator, ref AnimatedWeaponGripData data, Component component)
    {
        int count = Mathf.Min(data.fingers?.Length ?? 0, data.fingerRotations?.Length ?? 0);
        var job = new AnimatedWeaponGripJob
        {
            leftHand = ReadOnlyTransformHandle.Bind(animator, data.leftHand),
            rightHand = ReadOnlyTransformHandle.Bind(animator, data.rightHand),
            leftUpperArm = ReadOnlyTransformHandle.Bind(animator, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)),
            leftLowerArm = ReadOnlyTransformHandle.Bind(animator, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)),
            rightUpperArm = ReadOnlyTransformHandle.Bind(animator, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)),
            rightLowerArm = ReadOnlyTransformHandle.Bind(animator, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)),
            weapon = ReadWriteTransformHandle.Bind(animator, data.weapon),
            leftTarget = ReadWriteTransformHandle.Bind(animator, data.leftTarget),
            rightTarget = ReadWriteTransformHandle.Bind(animator, data.rightTarget),
            leftPalmOffset = data.leftPalmOffset, rightPalmOffset = data.rightPalmOffset,
            leftGripAxis = data.leftGripAxis, rightGripAxis = data.rightGripAxis,
            rightContact = data.rightContact, minimumSpacing = data.minimumSpacing, maximumSpacing = data.maximumSpacing,
            scale = Mathf.Abs(data.weapon.lossyScale.x),
            fingerWeight = FloatProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.fingerWeight))),
            fingers = new NativeArray<ReadWriteTransformHandle>(count, Allocator.Persistent),
            fingerRotations = new NativeArray<Quaternion>(count, Allocator.Persistent)
        };
        for (int i = 0; i < count; i++)
        {
            job.fingers[i] = ReadWriteTransformHandle.Bind(animator, data.fingers[i]);
            job.fingerRotations[i] = data.fingerRotations[i];
        }
        return job;
    }

    public override void Destroy(AnimatedWeaponGripJob job)
    {
        if (job.fingers.IsCreated) job.fingers.Dispose();
        if (job.fingerRotations.IsCreated) job.fingerRotations.Dispose();
    }
}

[DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Animated Weapon Grip")]
public class AnimatedWeaponGripConstraint : RigConstraint<AnimatedWeaponGripJob, AnimatedWeaponGripData, AnimatedWeaponGripBinder> { }
