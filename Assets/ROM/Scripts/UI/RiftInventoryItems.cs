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
 public string Id,Name,Description,Binding;public RiftItemKind Kind;public int Count,Supply=-1;public bool Equipped,ShieldSet;public GameObject Model;
 public bool IsConsumable=>Kind==RiftItemKind.Consumable;
}
public static class RiftInventoryItems {
 public static List<RiftInventoryItem> Read(PlayerInstance player,AbilityController combat){
  var list=new List<RiftInventoryItem>();if(!player||!combat||!combat.Prototype)return list;
  var axe=combat.Specialization?combat.Specialization.Weapon:null;var sword=combat.Progression.Sword;
  if(axe&&axe.Model)list.Add(new RiftInventoryItem{Id="starter:axe",Name=axe.DisplayName,Kind=RiftItemKind.Equipment,Count=1,Equipped=!combat.SwordAndShield,Model=axe.Model,Description="Equipo inicial · Dos manos\nMultiplicador de arma ×"+axe.DamageMultiplier.ToString("0.##")+"\nCambiar equipo: refugio o fuera de combate."});
  if(sword&&sword.Model)list.Add(new RiftInventoryItem{Id="starter:sword",Name=sword.DisplayName,Kind=RiftItemKind.Equipment,Count=1,Equipped=combat.SwordAndShield,ShieldSet=true,Model=sword.Model,Description="Equipo inicial · Mano principal\nConjunto espada y escudo: daño ×0.85.\nSe equipa junto al escudo; conserva las reglas de combate actuales."});
  if(combat.Progression.Shield)list.Add(new RiftInventoryItem{Id="starter:shield",Name="Escudo de Warrior",Kind=RiftItemKind.Equipment,Count=1,Equipped=combat.SwordAndShield,ShieldSet=true,Model=combat.Progression.Shield,Description="Equipo inicial · Mano secundaria\nConjunto espada y escudo: reducción +12%, vida máxima +15% antes de talentos.\nSe equipa junto a la espada."});
  bool local=player.isLocalPlayer&&NetworkServer.active;
  for(int i=0;i<2;i++)list.Add(new RiftInventoryItem{Id="potion:"+i,Name=RiftVillageSupplies.Name(i),Kind=RiftItemKind.Consumable,Count=local?RiftVillageSupplies.Count(i):0,Supply=i,Binding=i==0?"Potion":"PotionMajor",Description="Recupera "+(RiftVillageSupplies.Healing(i)*100).ToString("0")+"% de vida máxima.\nRecarga compartida: 5 s.\nMáximo por stack: "+RiftVillageSupplies.MaxStack+"."});
  return list;
 }
}
[Serializable] public sealed class RiftBagLayout {
 public const int Slots=12;public string[] Items=new string[Slots];
 public static string Key(string character)=>"ROM.BagLayout.v1."+character;
 public static RiftBagLayout Load(string character,IEnumerable<string> known){
  var ids=known.Distinct().ToArray();RiftBagLayout layout=null;
  if(!string.IsNullOrEmpty(character))try{layout=JsonUtility.FromJson<RiftBagLayout>(PlayerPrefs.GetString(Key(character),""));}catch{}
  if(layout==null||layout.Items==null||layout.Items.Length!=Slots)layout=new RiftBagLayout();
  var seen=new HashSet<string>();for(int i=0;i<Slots;i++)if(string.IsNullOrEmpty(layout.Items[i])||!ids.Contains(layout.Items[i])||!seen.Add(layout.Items[i]))layout.Items[i]="";
  foreach(var id in ids){if(seen.Contains(id))continue;int free=Array.FindIndex(layout.Items,string.IsNullOrEmpty);if(free<0)break;layout.Items[free]=id;seen.Add(id);}return layout;
 }
 public bool Move(string id,int destination,string character){int source=Array.IndexOf(Items,id);if(source<0||destination<0||destination>=Slots||string.IsNullOrEmpty(character))return false;string other=Items[destination];Items[destination]=id;Items[source]=other;Save(character);return true;}
 public void Save(string character){if(string.IsNullOrEmpty(character))return;PlayerPrefs.SetString(Key(character),JsonUtility.ToJson(this));PlayerPrefs.Save();}
}
}
