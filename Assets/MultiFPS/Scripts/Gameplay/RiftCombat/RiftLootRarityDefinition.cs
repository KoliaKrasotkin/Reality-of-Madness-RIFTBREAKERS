using UnityEngine;using ROM.UI.Sprint01;
namespace MultiFPS.Gameplay.RiftCombat {
[CreateAssetMenu(menuName="ROM/Loot/Rarity")]
public sealed class RiftLootRarityDefinition:ScriptableObject {public Rarity Kind;public string DisplayName;public Color Tint=Color.white;[Min(.01f)]public float PowerMultiplier=1;}
}
