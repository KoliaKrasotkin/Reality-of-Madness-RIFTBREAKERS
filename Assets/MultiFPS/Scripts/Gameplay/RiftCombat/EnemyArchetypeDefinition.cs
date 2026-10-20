using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
[CreateAssetMenu(menuName="ROM/Combat/Enemy archetype")]
public sealed class EnemyArchetypeDefinition:ScriptableObject {public string Id,DisplayName;public bool Caster;public float Detection=24,Range=2.8f,PreferredDistance=11,Damage=16,Windup=.8f,Cooldown=2.8f,MoveSpeed=3.2f;public AbilityDefinition Attack;}
}
