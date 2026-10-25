using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public enum RiftLootItemKind {NertorAxe}
[CreateAssetMenu(menuName="ROM/Loot/Item")]
public sealed class RiftLootItemDefinition:ScriptableObject {public string Id="nertor-axe",DisplayName="Hacha de Nertör";public RiftLootItemKind Kind;public WeaponDefinition Weapon;public int LevelOffset;public bool Supported=>Kind==RiftLootItemKind.NertorAxe&&!string.IsNullOrEmpty(Id)&&Weapon&&Weapon.Model&&Weapon.WeaponType=="HeavyAxe";}
}
