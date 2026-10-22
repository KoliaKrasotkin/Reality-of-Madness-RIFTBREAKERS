using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;
using UnityEngine.UI;
using ROM.UI.Gameplay;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 void BuildInventory(RectTransform body,PlayerInstance player,AbilityController combat){body.gameObject.AddComponent<RiftInventoryView>().Build(player,combat);}
}
// The bag exposes actual supply stacks only. Equipment remains owned by the combat system.
public sealed class RiftInventoryView:MonoBehaviour {
 PlayerInstance player; AbilityController combat; Text gold,detail,stock,key,cooldown,status; Button drink;
 readonly Text[] counts=new Text[2]; readonly Image[] borders=new Image[2]; int selected; float next;
 static readonly Color Bright=new Color(.88f,.73f,.44f), Slate=new Color(.065f,.08f,.105f,1);
 public void Build(PlayerInstance owner,AbilityController abilities){
  player=owner;combat=abilities;var body=(RectTransform)transform;
  var bag=RiftWidgets.Panel(body,"Supply bag",0,0,940,570);
  RiftWidgets.Label(bag,"PROVISIONES",28,20,700,44,28,Bright);
  RiftWidgets.Label(bag,"Tus provisiones · seleccioná un objeto para ver sus detalles",28,66,880,36,20,RiftWidgets.Muted);
  for(int i=0;i<2;i++){
   int id=i;float x=28+i*440;
   var slotRoot=RiftWidgets.Rect(bag,"Supply slot "+i,x,130,416,170);var surface=slotRoot.gameObject.AddComponent<Image>();surface.color=Slate;var slot=slotRoot.gameObject.AddComponent<Button>();slot.targetGraphic=surface;var colors=slot.colors;colors.highlightedColor=new Color(.7f,.76f,.85f);colors.selectedColor=Color.white;slot.colors=colors;slot.onClick.AddListener(()=>{selected=id;Refresh();});
   var outline=slot.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.26f,.3f,.36f);outline.effectDistance=new Vector2(1,-1);
   borders[i]=RiftWidgets.Image(slot.transform,"Selection",0,0,416,3,Bright);
   var icon=RiftWidgets.Rect(slot.transform,"Potion icon",26,28,42,65).gameObject.AddComponent<RiftHUDGraphic>();icon.Shape=HUDShape.Potion;icon.color=i==0?new Color(.85f,.15f,.23f):new Color(.76f,.27f,.61f);icon.Border=Bright;icon.raycastTarget=false;
   RiftWidgets.Label(slot.transform,RiftVillageSupplies.Name(i),90,24,300,52,21);
   RiftWidgets.Label(slot.transform,"Recupera "+(RiftVillageSupplies.Healing(i)*100).ToString("0")+"% de vida",90,82,300,30,19,RiftWidgets.Muted);
   counts[i]=RiftWidgets.Label(slot.transform,"",26,125,360,28,20,Bright);
   var hover=slot.gameObject.AddComponent<RiftTooltipTrigger>();hover.Title=RiftVillageSupplies.Name(i);hover.Description="Consumible · Recupera "+(RiftVillageSupplies.Healing(i)*100).ToString("0")+"% de vida. Comparte una recarga de 5 s con las otras pociones.";hover.ActionId=i==0?"Potion":"PotionMajor";
  }
  gold=RiftWidgets.Label(bag,"",28,335,880,44,25,Bright);
  RiftWidgets.Label(bag,"CONSEGUIR PROVISIONES",28,405,880,30,18,Bright);
  RiftWidgets.Label(bag,"Visitá a la boticaria en el refugio. El oro se gana jugando.\nUsá las pociones desde esta bolsa o con sus accesos rápidos.",28,445,880,88,21,RiftWidgets.Muted);
  var info=RiftWidgets.Panel(body,"Selected item",964,0,820,570);
  RiftWidgets.Label(info,"OBJETO SELECCIONADO",28,20,760,35,18,Bright);
  detail=RiftWidgets.Label(info,"",28,75,760,130,29);
  stock=RiftWidgets.Label(info,"",28,220,760,42,22);
  key=RiftWidgets.Label(info,"",28,270,760,42,20,RiftWidgets.Muted);
  cooldown=RiftWidgets.Label(info,"",28,320,760,42,21,Bright);
  drink=RiftWidgets.Button(info,"USAR POCIÓN",28,385,760,66,()=>{var village=player?player.GetComponent<ROM.Horde.RiftVillagePlayer>():null;if(village)village.CmdDrinkPotion(selected);});
  status=RiftWidgets.Label(info,"",28,467,760,78,20,RiftWidgets.Muted);
  var equipment=RiftWidgets.Panel(body,"Current equipment",0,588,1784,88);
  var weapon=combat&&combat.Prototype?(combat.SwordAndShield?combat.Progression.Sword:combat.Specialization.Weapon):null;
  RiftWidgets.Label(equipment,"EQUIPO · "+(weapon?weapon.DisplayName:"Equipo de personaje")+(combat&&combat.SwordAndShield?" + escudo":""),24,12,1100,60,23);
  RiftWidgets.Button(equipment,"VER PERSONAJE",1280,16,480,54,()=>GetComponentInParent<RiftHub>().ShowGameplayPage(HubPage.Character));Refresh();
 }
 void Update(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;Refresh();}
 void Refresh(){
  if(!player||!detail)return;
  gold.text="ORO LOCAL  "+RiftExplorationProgress.Gold.ToString("N0");
  for(int i=0;i<2;i++){counts[i].text="Cantidad: "+RiftVillageSupplies.Count(i)+" / "+RiftVillageSupplies.MaxStack;borders[i].color=i==selected?Bright:new Color(.2f,.23f,.28f);borders[i].transform.parent.GetComponent<Outline>().effectColor=i==selected?Bright:new Color(.26f,.3f,.36f);}
  detail.text=RiftVillageSupplies.Name(selected)+"\nRecupera "+(RiftVillageSupplies.Healing(selected)*100).ToString("0")+"% de vida máxima.";
  stock.text="En la bolsa: "+RiftVillageSupplies.Count(selected)+" / "+RiftVillageSupplies.MaxStack;
  key.text="Acceso rápido: "+RiftControls.ActionLabel(selected==0?"Potion":"PotionMajor")+" · Configurable en Controles";
  var village=player.GetComponent<ROM.Horde.RiftVillagePlayer>();double remaining=village?System.Math.Max(0,village.PotionReadyAt-NetworkTime.time):0;
  cooldown.text=remaining>0?"Recarga compartida: "+remaining.ToString("0.0")+" s":"Lista · Recarga compartida: 5 s";
  var actor=player.MyCharacter;bool local=player.isLocalPlayer&&NetworkServer.active;bool alive=actor&&actor.Health.CurrentHealth>0;bool full=alive&&actor.Health.CurrentHealth>=actor.Health.MaxHealth;
  drink.interactable=local&&village&&alive&&!full&&RiftVillageSupplies.Count(selected)>0&&remaining<=0;
  status.text=!local?"Disponible en host local. Las provisiones de clientes remotos requieren una cartera de servidor.":!alive?"Necesitás estar vivo para usar una poción.":RiftVillageSupplies.Count(selected)==0?"Sin existencias. Comprá esta poción en la boticaria.":full?"Tu vida ya está completa.":remaining>0?"Esperá a que termine la recarga.":"Seleccioná Usar poción o su acceso rápido.";
 }
}
}
