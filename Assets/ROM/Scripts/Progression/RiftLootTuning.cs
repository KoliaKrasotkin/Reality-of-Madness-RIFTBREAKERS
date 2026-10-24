using UnityEngine;using System.Linq;using MultiFPS.Gameplay.RiftCombat;
namespace ROM.Progression {
[CreateAssetMenu(menuName="ROM/Loot/Tuning")]
public sealed class RiftLootTuning:ScriptableObject {
 public RiftLootTableDefinition DefaultTable;public RiftLootItemDefinition[] Catalog;
 [HideInInspector]public float AxeChance=.15f;
 [HideInInspector]public float WeaponDamagePerLevel=.03f;
 [HideInInspector]public int ItemLevelOffset=0;
 [Min(.2f)]public float AutoPickupRadius=1.25f,ClickRange=3;
 [Min(5)]public float Lifetime=300;
 public static RiftLootTuning Load=>Resources.Load<RiftLootTuning>("RiftLootTuning");
 public RiftLootItemDefinition Item(string id)=>Catalog?.FirstOrDefault(i=>i&&i.Id==(string.IsNullOrEmpty(id)?"nertor-axe":id));
 public float Multiplier(int level)=>DefaultTable?DefaultTable.LevelPower(level):1+Mathf.Max(0,level-1)*Mathf.Max(0,WeaponDamagePerLevel);
}}
