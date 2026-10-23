using System;
using System.Linq;
using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using ROM.Progression;
using ROM.UI.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 void BuildCompactHeader(){
  compactGameplayHeader=W.Panel(Design,"Compact gameplay navigation",1140,110,744,108);compactGameplayHeader.GetComponent<Image>().raycastTarget=true;
  var labels=new[]{"BOLSA","PERSONAJE","HABILIDADES","DIARIO","AJUSTES","CERRAR"};var actions=new[]{"Inventory","Character","Abilities","Journal","Settings","Pause"};var pages=new[]{HubPage.Inventory,HubPage.Character,HubPage.Abilities,HubPage.Missions,HubPage.Settings,HubPage.Home};
  for(int i=0;i<6;i++){int index=i;BoundButton(compactGameplayHeader,labels[i],actions[i],12+(i%3)*240,8+(i/3)*49,232,43,()=>{if(index==5)ResumeRequested?.Invoke();else TogglePanel(actions[index],pages[index]);});}
  compactGameplayHeader.gameObject.SetActive(false);BuildModernInventoryHeader();
 }
 internal static void EquipmentPortrait(Transform parent,PlayerInstance player,AbilityController combat,float y=0){
  var hud=UnityEngine.Object.FindFirstObjectByType<RiftGameplayHUD>();var portrait=W.Image(parent,"Current skin portrait",0,y,94,94,Color.white,hud&&hud.Portrait?hud.Portrait.sprite:null);portrait.preserveAspect=true;
  W.Label(parent,player.MyCharacter.Health.CharacterName,112,y,550,35,24,W.Gold);W.Label(parent,"Warrior · Nivel "+(combat?combat.Level:1),112,y+37,550,30,20,W.Muted);
  var weapon=combat&&combat.Prototype?(combat.SwordAndShield?combat.Progression.Sword:combat.Specialization.Weapon):null;
  var equipment=W.Panel(parent,"Equipped weapon",0,y+112,676,74);W.Label(equipment,"ARMA",16,5,90,25,16,W.Gold);W.Label(equipment,(weapon?weapon.DisplayName:"Sin arma equipada")+(combat&&combat.SwordAndShield?" + escudo":""),16,30,640,33,22);
  var tip=equipment.gameObject.AddComponent<RiftTooltipTrigger>();equipment.GetComponent<Image>().raycastTarget=true;tip.Title="Equipo actual";tip.Description="Configuración real del personaje. Podés cambiar hacha / espada y escudo desde Personaje. La bolsa aún no admite equipo de botín.";
 }
 void CompactCharacter(RectTransform body,PlayerInstance player,AbilityController combat){
  EquipmentPortrait(body,player,combat);
  var actor=player.MyCharacter;W.Label(body,"VIDA  "+actor.Health.CurrentHealth+" / "+actor.Health.MaxHealth,0,208,676,36,23,W.Gold);
  if(combat&&combat.Prototype){W.Label(body,"EXPERIENCIA  "+combat.Progression.InLevel(combat.Experience)+" / "+combat.Progression.Required(combat.Level)+"\nDaño +"+((combat.PowerMultiplier-1)*100).ToString("0")+"%   ·   Crítico "+(combat.CriticalChance*100).ToString("0")+"%",0,260,676,100,22);
   W.Button(body,combat.SwordAndShield?"EQUIPAR HACHA A DOS MANOS":"EQUIPAR ESPADA Y ESCUDO",0,394,676,62,()=>player.GetComponent<RiftJournalPlayer>().CmdWarriorWeapon(!combat.SwordAndShield));}
  BoundButton(body,"ABRIR BOLSA","Inventory",0,480,330,54,()=>ShowGameplayPage(HubPage.Inventory));BoundButton(body,"HABILIDADES","Abilities",346,480,330,54,ShowSkills);
  W.Label(body,"El personaje sigue visible en el mundo.\nEl resto de ranuras de equipo aún no está implementado.",0,550,676,54,18,W.Muted);
 }
 void CompactJournal(Transform body){ModernJournal((RectTransform)body);}
}
}
