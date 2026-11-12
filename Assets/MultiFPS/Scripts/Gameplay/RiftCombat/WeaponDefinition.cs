using System.Collections.Generic;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public enum EquipmentRole { Weapon, Shield }
[CreateAssetMenu(menuName="ROM/Combat/Weapon")]
public sealed class WeaponDefinition : ScriptableObject
{
 public string Id,DisplayName,WeaponType;
 public EquipmentRole Role;
 public float DamageMultiplier=1,AttackSpeed=1,Reach=3;
 [Header("Weapon damage (version 0 retains legacy definitions)")]
 public int DamageModelVersion;
 [Min(0)]public float DamageMinimum=16,DamageMaximum=20;
 [Min(.2f)]public float SwingSeconds=1.8f;
 public float BaseDamageMinimum=>IsShield?0:DamageModelVersion>0?Mathf.Max(0,DamageMinimum):18*Mathf.Max(0,DamageMultiplier);
 public float BaseDamageMaximum=>IsShield?0:DamageModelVersion>0?Mathf.Max(BaseDamageMinimum,DamageMaximum):BaseDamageMinimum;
 public float SwingDuration=>DamageModelVersion>0?Mathf.Max(.2f,SwingSeconds):1.8f/Mathf.Max(.1f,AttackSpeed);
 public string DamageSummary(float power=1){power=float.IsNaN(power)||float.IsInfinity(power)?1:Mathf.Clamp(power,.01f,10);float min=BaseDamageMinimum*power,max=BaseDamageMaximum*power;return "Daño: "+min.ToString("0.##")+"–"+max.ToString("0.##")+"\nVelocidad: "+SwingDuration.ToString("0.##")+" s\nDPS del arma: "+((min+max)*.5f/SwingDuration).ToString("0.##");}
 public GameObject Model;
 public HumanBodyBones Socket=HumanBodyBones.RightHand;
 public Vector3 Position,Rotation,Scale=Vector3.one;
 public string[] AllowedClasses,AllowedSpecializations;
 public AnimationClip Attack;
 public GameObject ImpactVfx;
 [Header("Animation and attachment")]
 public RuntimeAnimatorController Controller;
 public bool UsesBuiltInWeapon,UseGripFrame;
 public HumanBodyBones StoredBone=HumanBodyBones.Chest;
 public Vector3 StoredPosition,StoredRotation;
 public bool CreateBackSocket;
 [Header("Hand IK")]
 public bool EnableHandIK;
 [Tooltip("Move the drawn model minimally so every enabled hand grip stays within arm reach.")]
 public bool KeepGripsReachable;
 public EquipmentHandGrip[] HandGrips=System.Array.Empty<EquipmentHandGrip>();
 public EquipmentAnimationIK[] AnimationIK=System.Array.Empty<EquipmentAnimationIK>();
 public EquipmentFingerPose[] FingerPoses=System.Array.Empty<EquipmentFingerPose>();
 [Tooltip("Use calibrated finger poses across clips, blending to the relaxed pose when the weapon is stored.")]
 public bool UseFixedFingerPoses;
 [Header("Single-hand carry grip")]
 public bool UseApprovedCarryGrip;
 public Vector3 CarryGripPosition,CarryGripRotation;
 [Tooltip("Companion items such as a shield or a second weapon. Each item owns its model and socket.")]
 public WeaponDefinition[] AdditionalEquipment=System.Array.Empty<WeaponDefinition>();
 public bool IsShield => Role==EquipmentRole.Shield;
 void OnValidate()
 {
  if(IsShield){DamageMultiplier=0;AttackSpeed=0;Reach=0;}
 }
 // Flat composition avoids cycles and keeps each companion's configuration in one asset.
 public IEnumerable<WeaponDefinition> EquippedParts
 {
  get
  {
   if(UsesBuiltInWeapon)yield break;
   yield return this;
   foreach(var item in AdditionalEquipment)if(item&&item!=this)yield return item;
  }
 }
}
}
