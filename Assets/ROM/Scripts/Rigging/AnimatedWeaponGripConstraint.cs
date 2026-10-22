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
        leftTarget.SetGlobalTR(stream, leftPalm - leftRotation * (leftPalmOffset * scale), leftRotation);
        rightTarget.SetGlobalTR(stream, rightPalm - rightRotation * (rightPalmOffset * scale), rightRotation);
        float curl = fingerWeight.Get(stream) * weight;
        for (int i = 0; i < fingers.Length; i++)
        {
            var finger = fingers[i];
            finger.SetLocalRotation(stream, Quaternion.Slerp(finger.GetLocalRotation(stream), fingerRotations[i], curl));
            fingers[i] = finger;
        }
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
