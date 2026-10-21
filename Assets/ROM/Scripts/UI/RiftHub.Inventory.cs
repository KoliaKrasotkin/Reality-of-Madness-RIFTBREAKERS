using MultiFPS;using MultiFPS.Gameplay;using MultiFPS.Gameplay.RiftCombat;using UnityEngine;using UnityEngine.UI;using ROM.UI.Gameplay;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 void BuildInventory(RectTransform body,PlayerInstance player,AbilityController combat){body.gameObject.AddComponent<RiftInventoryView>().Build(player,combat);}
}
// Existing inventory only: supplies are real stacks, equipment is the active Warrior loadout.
// Empty cells reserve space visually; they do not create fake loot or extra storage capacity.
public sealed class RiftInventoryView:MonoBehaviour {
 PlayerInstance player;AbilityController combat;Text gold,detail,stock,key,cooldown;Button drink;int selected;float next;
 readonly Text[] counts=new Text[2];readonly Button[] slots=new Button[2];
 public void Build(PlayerInstance owner,AbilityController abilities){player=owner;combat=abilities;var body=(RectTransform)transform;
 gold=RiftWidgets.Label(body,"",20,0,900,48,26,RiftWidgets.Gold);RiftWidgets.Label(body,"PROVISIONES",20,65,900,40,22,RiftWidgets.Gold);
 for(int i=0;i<24;i++){int index=i;float x=20+i%8*100,y=115+i/8*100;var slot=RiftWidgets.Button(body,"",x,y,84,84,()=>{if(index<2){selected=index;Refresh();}});slot.name=i<2?"Supply slot "+i:"Empty bag cell "+i;if(i>=2){slot.interactable=false;continue;}slots[i]=slot;var icon=RiftWidgets.Rect(slot.transform,"Potion icon",25,7,34,48).gameObject.AddComponent<RiftHUDGraphic>();icon.Shape=HUDShape.Potion;icon.color=i==0?new Color(.95f,.05f,.12f):new Color(1,.25f,.5f);icon.Border=RiftWidgets.Gold;icon.raycastTarget=false;counts[i]=RiftWidgets.Label(slot.transform,"",4,57,75,22,16);counts[i].alignment=TextAnchor.MiddleRight;var tooltip=slot.gameObject.AddComponent<RiftTooltipTrigger>();tooltip.Title=RiftVillageSupplies.Name(i);tooltip.Description="Recupera "+(RiftVillageSupplies.Healing(i)*100).ToString("0")+"% de vida. Recarga compartida: 5 s.";tooltip.ActionId=i==0?"Potion":"PotionMajor";}
 RiftWidgets.Label(body,"Comprá provisiones en la boticaria con oro local.\nLas casillas vacías están reservadas para futuros objetos.",20,440,850,75,19,RiftWidgets.Muted);
 var panel=RiftWidgets.Panel(body,"Item details",900,65,850,475);detail=RiftWidgets.Label(panel,"",25,15,800,145,25,RiftWidgets.Gold);stock=RiftWidgets.Label(panel,"",25,170,800,50,22);key=RiftWidgets.Label(panel,"",25,225,800,60,20,RiftWidgets.Muted);cooldown=RiftWidgets.Label(panel,"",25,290,800,55,20);drink=RiftWidgets.Button(panel,"USAR POCIÓN",25,365,500,65,()=>{if(player)player.GetComponent<ROM.Horde.RiftVillagePlayer>().CmdDrinkPotion(selected);});
 var weapon=combat&&combat.Prototype?(combat.SwordAndShield?combat.Progression.Sword:combat.Specialization.Weapon):null;RiftWidgets.Label(body,"EQUIPO ACTUAL · "+(weapon?weapon.DisplayName:"Equipo de personaje")+(combat&&combat.SwordAndShield?" + escudo":""),20,555,1700,55,24,RiftWidgets.Gold);RiftWidgets.Button(body,"VER PERSONAJE ["+RiftControls.Label("Character")+"]",20,615,650,55,()=>GetComponentInParent<RiftHub>().ShowGameplayPage(HubPage.Character));Refresh();
 }
 void Update(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.15f;Refresh();}
 void Refresh(){if(!player||!detail)return;gold.text="ORO LOCAL  "+RiftExplorationProgress.Gold.ToString("N0");for(int i=0;i<2;i++)counts[i].text="× "+RiftVillageSupplies.Count(i);detail.text=RiftVillageSupplies.Name(selected)+"\nRecupera "+(RiftVillageSupplies.Healing(selected)*100).ToString("0")+"% de vida máxima.";stock.text="Cantidad: "+RiftVillageSupplies.Count(selected)+" / "+RiftVillageSupplies.MaxStack;key.text="Barra rápida: "+RiftControls.Label(selected==0?"Potion":"PotionMajor")+" · Reasignable en Controles.";var village=player.GetComponent<ROM.Horde.RiftVillagePlayer>();double remaining=System.Math.Max(0,village.PotionReadyAt-Mirror.NetworkTime.time);cooldown.text=remaining>0?"Recarga compartida: "+remaining.ToString("0.0")+" s":"Lista para usar · Recarga compartida: 5 s";drink.interactable=RiftVillageSupplies.Count(selected)>0&&remaining<=0&&player.MyCharacter&&player.MyCharacter.Health.CurrentHealth<player.MyCharacter.Health.MaxHealth;}
}
}
