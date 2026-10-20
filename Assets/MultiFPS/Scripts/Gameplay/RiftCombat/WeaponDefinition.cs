using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
[CreateAssetMenu(menuName="ROM/Combat/Weapon")]
public sealed class WeaponDefinition:ScriptableObject {public string Id,DisplayName,WeaponType;public float DamageMultiplier=1,AttackSpeed=1,Reach=3;public GameObject Model;public HumanBodyBones Socket=HumanBodyBones.RightHand;public Vector3 Position,Rotation,Scale=Vector3.one;public string[] AllowedClasses,AllowedSpecializations;public AnimationClip Attack; public GameObject ImpactVfx;}
}
