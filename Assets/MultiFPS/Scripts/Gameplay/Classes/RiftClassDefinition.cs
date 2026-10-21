using UnityEngine;
namespace MultiFPS.Gameplay {
    public enum RiftClassId { Paladin, Bastion, BattleMage, VeilHunter }
    [CreateAssetMenu(menuName="ROM/Clase")]
    public sealed class RiftClassDefinition : ScriptableObject {
        public RiftClassId Id;
        public string Name,Role;
        [TextArea] public string Description,Passive,FirstDescription,SecondDescription;
        public string FirstAbility,SecondAbility;
        public int Health=120;
        public float WeaponDamage=1.1f;
        [Range(0, .8f)] public float Armor=.1f;
        public float FirstCooldown=12,SecondCooldown=16,FirstPower=80,SecondPower=90;
        public Color Color=Color.yellow;
    }
}
