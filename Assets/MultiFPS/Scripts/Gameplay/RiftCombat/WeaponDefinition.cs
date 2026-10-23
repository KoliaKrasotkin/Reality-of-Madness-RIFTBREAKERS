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
