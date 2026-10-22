using System;
using System.Collections.Generic;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ROM.UI.Gameplay;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 void BuildInventory(RectTransform body,PlayerInstance player,AbilityController combat){body.gameObject.AddComponent<RiftInventoryPaperdoll>().Build(player,combat,this);}
}
// UI entries are separate from storage: adapters can expose additional real item types.
public sealed class RiftBagEntry {
 public string Id,Title,Description,ActionId;public int SupplyId;public Func<int> Count;
 public static List<RiftBagEntry> Supplies(){var entries=new List<RiftBagEntry>();for(int i=0;i<2;i++){int id=i;entries.Add(new RiftBagEntry{Id="potion:"+id,SupplyId=id,Title=RiftVillageSupplies.Name(id),Description="Recupera "+(RiftVillageSupplies.Healing(id)*100).ToString("0")+"% de vida máxima. Recarga compartida: 5 s.",ActionId=id==0?"Potion":"PotionMajor",Count=()=>RiftVillageSupplies.Count(id)});}return entries;}
}
public sealed class RiftBagSlotActions:MonoBehaviour,IPointerClickHandler {
 public Action RightClick;public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Right)RightClick?.Invoke();}
}
public sealed class RiftInventoryView:MonoBehaviour {
 PlayerInstance player;Text gold,detail,stock,key,cooldown,status;Button drink;int selected;float next;
 List<RiftBagEntry> entries;readonly List<Text> counts=new List<Text>();readonly List<Image> borders=new List<Image>();readonly List<RiftTooltipTrigger> tips=new List<RiftTooltipTrigger>();
 static readonly Color Bright=new Color(.88f,.73f,.44f);
 public void Build(PlayerInstance owner,AbilityController abilities){
  player=owner;var body=(RectTransform)transform;RiftHub.EquipmentPortrait(body,player,abilities);
  RiftWidgets.Label(body,"BOLSA",0,202,350,32,22,Bright);gold=RiftWidgets.Label(body,"",350,202,326,32,20,Bright);
  entries=RiftBagEntry.Supplies();var grid=RiftWidgets.Panel(body,"Supply bag",0,245,676,130);
  for(int i=0;i<entries.Count;i++){int index=i;var entry=entries[i];var slotRoot=RiftWidgets.Rect(grid,"Supply slot "+i,16+i*108,16,92,92);var surface=slotRoot.gameObject.AddComponent<Image>();surface.color=new Color(.065f,.06f,.075f);var slot=slotRoot.gameObject.AddComponent<Button>();slot.targetGraphic=surface;slot.onClick.AddListener(()=>{selected=index;Refresh();});var outline=slot.gameObject.AddComponent<Outline>();outline.effectColor=Bright;outline.effectDistance=new Vector2(1,-1);borders.Add(surface);
   var icon=RiftWidgets.Rect(slotRoot,"Potion icon",30,12,32,48).gameObject.AddComponent<RiftHUDGraphic>();icon.Shape=HUDShape.Potion;icon.color=i==0?new Color(.85f,.15f,.23f):new Color(.76f,.27f,.61f);icon.Border=Bright;icon.raycastTarget=false;
   var count=RiftWidgets.Label(slotRoot,"",8,61,76,26,21,Bright);count.alignment=TextAnchor.MiddleRight;counts.Add(count);
   var tip=slotRoot.gameObject.AddComponent<RiftTooltipTrigger>();tip.Title=entry.Title;tip.ActionId=entry.ActionId;tips.Add(tip);
   slotRoot.gameObject.AddComponent<RiftBagSlotActions>().RightClick=()=>{selected=index;Refresh();UseSelected();};
  }
  RiftWidgets.Label(grid,"Dos tipos de provisión disponibles.\nNo hay otros objetos de bolsa implementados.",252,18,405,88,19,RiftWidgets.Muted);
  detail=RiftWidgets.Label(body,"",0,394,676,32,24,Bright);stock=RiftWidgets.Label(body,"",0,435,676,32,20);key=RiftWidgets.Label(body,"",0,472,676,30,18,RiftWidgets.Muted);cooldown=RiftWidgets.Label(body,"",0,506,676,26,18,Bright);
  drink=RiftWidgets.Button(body,"USAR",0,550,200,52,UseSelected);status=RiftWidgets.Label(body,"",220,540,456,65,18,RiftWidgets.Muted);Refresh();
 }
 void Update(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;Refresh();}
 void UseSelected(){Refresh();if(!drink||!drink.interactable)return;var village=player.GetComponent<ROM.Horde.RiftVillagePlayer>();if(village)village.CmdDrinkPotion(entries[selected].SupplyId);}
 void Refresh(){
  if(!player||!detail)return;gold.text="ORO LOCAL  "+RiftExplorationProgress.Gold.ToString("N0");
  for(int i=0;i<entries.Count;i++){int count=entries[i].Count();counts[i].text=count>0?count.ToString():"0";borders[i].GetComponent<Outline>().effectColor=i==selected?Bright:new Color(.21f,.2f,.24f);borders[i].color=count>0?new Color(.095f,.075f,.085f):new Color(.035f,.035f,.045f);tips[i].Description=entries[i].Description+"\nCantidad: "+count+" / "+RiftVillageSupplies.MaxStack+"\nClic: seleccionar · Clic derecho: usar";}
  var entry=entries[selected];detail.text=entry.Title;stock.text=entry.Description;key.text="Cantidad: "+entry.Count()+" / "+RiftVillageSupplies.MaxStack+" · Tecla: "+RiftControls.ActionLabel(entry.ActionId)+" · Clic derecho: usar";
  var village=player.GetComponent<ROM.Horde.RiftVillagePlayer>();double remaining=village?Math.Max(0,village.PotionReadyAt-NetworkTime.time):0;cooldown.text=remaining>0?"Recarga compartida: "+remaining.ToString("0.0")+" s":"";
  var actor=player.MyCharacter;bool local=player.isLocalPlayer&&NetworkServer.active,alive=actor&&actor.Health.CurrentHealth>0,full=alive&&actor.Health.CurrentHealth>=actor.Health.MaxHealth;
  drink.interactable=local&&village&&alive&&!full&&entry.Count()>0&&remaining<=0;
  status.text=!local?"Uso disponible en host local; falta cartera de servidor para clientes remotos.":!alive?"Necesitás estar vivo.":entry.Count()==0?"Sin existencias. Visitá la boticaria.":full?"Tu vida ya está completa.":remaining>0?"Esperá la recarga.":"Usar, clic derecho o atajo.";
 }
}
}
