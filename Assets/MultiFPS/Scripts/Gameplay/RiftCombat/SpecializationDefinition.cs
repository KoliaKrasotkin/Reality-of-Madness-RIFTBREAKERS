using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
[CreateAssetMenu(menuName="ROM/Combat/Specialization")]
public sealed class SpecializationDefinition:ScriptableObject {public string Id,DisplayName; public int Health=180; [Range(0,.8f)]public float Armor=.1f;public float DamageMultiplier=1,ThreatMultiplier=1;public WeaponDefinition Weapon;}
}
