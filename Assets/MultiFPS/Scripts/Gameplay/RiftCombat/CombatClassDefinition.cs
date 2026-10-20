using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
[CreateAssetMenu(menuName="ROM/Combat/Class")]
public sealed class CombatClassDefinition:ScriptableObject {public string Id,DisplayName;public CombatResourceKind Resource;public float MaximumResource=100;public AbilityDefinition BasicAttack; public AbilityDefinition[] Abilities; public SpecializationDefinition[] Specializations;}
}
