using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using ROM.Progression;
using UnityEngine;
namespace ROM.UI.Sprint01 {
public enum RiftItemKind { Equipment, Consumable }
// Adapters expose existing ownership. Layout never grants, destroys or duplicates items.
public sealed class RiftInventoryItem {
 public string Id,Name,Description,Binding,LootId;public int RequiredLevel=1;public Color NameColor=new Color(.85f,.8f,.98f);public RiftItemKind Kind;public int Count,Supply=-1;public bool Equipped,ShieldSet;public GameObject Model;
 public bool IsConsumable=>Kind==RiftItemKind.Consumable;
}
public static class RiftInventoryItems {
 public static string WeaponDetails(RiftLootAxe item){var definition=RiftLootTuning.Load.Item(item.DefinitionId);return definition&&definition.Weapon?definition.Weapon.DamageSummary(item.DurabilityNow<=0&&RiftRepairTuning.Load.SuspendBonusesAtZero?1:item.Multiplier):"";}
 public static List<RiftInventoryItem> Read(PlayerInstance player,AbilityController combat){
  var list=new List<RiftInventoryItem>();if(!player||!combat||!combat.Prototype)return list;
  var lootState=RiftJournalView.Read(player.PlayerInfo.CharacterId);string equippedLoot=lootState?.EquippedLootAxe??"";
  var axe=combat.Specialization?combat.Specialization.Weapon:null;var sword=combat.Progression.Sword;
  if(axe&&axe.Model)list.Add(new RiftInventoryItem{Id="starter:axe",Name=axe.DisplayName,Kind=RiftItemKind.Equipment,Count=1,Equipped=!combat.SwordAndShield&&string.IsNullOrEmpty(equippedLoot),Model=axe.Model,Description="Equipo inicial · Dos manos\nMultiplicador de arma ×"+axe.DamageMultiplier.ToString("0.##")+"\n"+axe.DamageSummary()+"\nDurabilidad: "+(lootState?.StarterDurabilityNow??100)+" / "+(lootState?.StarterDurabilityMax??100)+"\nCambiar equipo: refugio o fuera de combate."});
  if(sword&&sword.Model)list.Add(new RiftInventoryItem{Id="starter:sword",Name=sword.DisplayName,Kind=RiftItemKind.Equipment,Count=1,Equipped=combat.SwordAndShield,ShieldSet=true,Model=sword.Model,Description="Equipo inicial · Mano principal\n"+sword.DamageSummary()+"\nSe equipa junto al escudo; conserva las reglas de combate actuales."});
  if(combat.Progression.Shield)list.Add(new RiftInventoryItem{Id="starter:shield",Name="Escudo de Warrior",Kind=RiftItemKind.Equipment,Count=1,Equipped=combat.SwordAndShield,ShieldSet=true,Model=combat.Progression.Shield,Description="Equipo inicial · Mano secundaria\nConjunto espada y escudo: reducción +12%, vida máxima +15% antes de talentos.\nSe equipa junto a la espada."});
  if(lootState?.LootAxes!=null&&axe&&axe.Model)foreach(var item in lootState.LootAxes)list.Add(new RiftInventoryItem{Id="loot:"+item.Id,LootId=item.Id,RequiredLevel=item.Level,Name=item.RarityLabel+" · "+axe.DisplayName+" · Nv. "+item.Level,NameColor=item.DisplayColor,Kind=RiftItemKind.Equipment,Count=1,Equipped=!combat.SwordAndShield&&equippedLoot==item.Id,Model=axe.Model,Description="Rareza: "+item.RarityLabel+" · Dos manos\nRequiere nivel "+item.Level+" para equipar; recoger no requiere nivel.\nMultiplicador del objeto ×"+item.Multiplier.ToString("0.##")+"\n"+axe.DamageSummary(item.DurabilityNow<=0&&RiftRepairTuning.Load.SuspendBonusesAtZero?1:item.Multiplier)+"\nDurabilidad: "+item.DurabilityNow+" / "+item.DurabilityMax+(item.DurabilityNow<=0?"\nBonus del arma suspendidos: conserva daño base.":"")});
  bool local=player.isLocalPlayer&&NetworkServer.active;
  for(int i=0;i<2;i++)list.Add(new RiftInventoryItem{Id="potion:"+i,Name=RiftVillageSupplies.Name(i),Kind=RiftItemKind.Consumable,Count=local?RiftVillageSupplies.Count(i):0,Supply=i,Binding=i==0?"Potion":"PotionMajor",Description="Recupera "+(RiftVillageSupplies.Healing(i)*100).ToString("0")+"% de vida máxima.\nRecarga compartida: 5 s.\nMáximo por stack: "+RiftVillageSupplies.MaxStack+"."});
  return list;
 }
}
[Serializable] public sealed class RiftBagLayout {
 static RiftBagLayout crilium;public static void ClearCriliumLayout(){crilium=null;}
 public const int Slots=12;public string[] Items=new string[Slots];
 public static string Key(string character)=>"ROM.BagLayout.v1."+character;
 public static RiftBagLayout Load(string character,IEnumerable<string> known){
  var ids=known.Distinct().ToArray();RiftBagLayout layout=ROM.Tutorial.Crilium.CriliumSession.Active?crilium:null;
  if(!ROM.Tutorial.Crilium.CriliumSession.Active&&!string.IsNullOrEmpty(character))try{layout=JsonUtility.FromJson<RiftBagLayout>(PlayerPrefs.GetString(Key(character),""));}catch{}
  if(layout==null||layout.Items==null||layout.Items.Length!=Slots)layout=new RiftBagLayout();
  var seen=new HashSet<string>();for(int i=0;i<Slots;i++)if(string.IsNullOrEmpty(layout.Items[i])||!ids.Contains(layout.Items[i])||!seen.Add(layout.Items[i]))layout.Items[i]="";
  foreach(var id in ids){if(seen.Contains(id))continue;int free=Array.FindIndex(layout.Items,string.IsNullOrEmpty);if(free<0)break;layout.Items[free]=id;seen.Add(id);}return layout;
 }
 public bool Move(string id,int destination,string character){int source=Array.IndexOf(Items,id);if(source<0||destination<0||destination>=Slots||string.IsNullOrEmpty(character))return false;string other=Items[destination];Items[destination]=id;Items[source]=other;Save(character);return true;}
 public void Save(string character){if(ROM.Tutorial.Crilium.CriliumSession.Active){crilium=this;return;}if(string.IsNullOrEmpty(character))return;PlayerPrefs.SetString(Key(character),JsonUtility.ToJson(this));PlayerPrefs.Save();}
}
}
