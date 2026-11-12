using System;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;

public sealed class HandPosePreset : ScriptableObject
{
    public string SourceRig;
    public Avatar SourceAvatar;
    public HumanBodyBones Hand;
    public EquipmentBoneRotation[] Fingers = Array.Empty<EquipmentBoneRotation>();
}
