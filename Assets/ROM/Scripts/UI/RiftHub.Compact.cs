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
 void CompactJournal(Transform body){
  var catalog=RiftJournalCatalog.Load;if(!catalog){W.Label(body,"Diario no disponible.",0,0,676,60,22);return;}var state=JournalState;
  W.Dropdown(body,"Quest filter",0,0,676,new[]{"Activas","Completadas","Disponibles"},Array.IndexOf(new[]{"Activas","Completadas","Disponibles"},journalFilter),i=>{journalFilter=new[]{"Activas","Completadas","Disponibles"}[i];journalSelection="";Navigate(HubPage.Missions);});
  var list=catalog.Quests.Where(q=>journalFilter=="Activas"?state.Quest(q.Id)!=null&&state.Quest(q.Id).Status!=QuestStatus.TurnedIn:journalFilter=="Completadas"?state.TurnedIn(q.Id):state.Available(q)).ToArray();visibleQuests=list;
  if(!list.Any(q=>q.Id==journalSelection))journalSelection=list.FirstOrDefault()?.Id??"";
  var scroll=W.ScrollArea(body,"Quest list",0,65,676,135,list.Length*52);journalScroll=scroll;
  for(int i=0;i<list.Length;i++){var q=list[i];W.Button(scroll,q.Title,0,i*52,656,46,()=>{journalSelection=q.Id;Navigate(HubPage.Missions);},journalSelection==q.Id);}
  BoundButton(body,"ANTERIOR","JournalPrevious",0,208,330,39,()=>SelectJournal(-1)).interactable=list.Length>1;BoundButton(body,"SIGUIENTE","JournalNext",346,208,330,39,()=>SelectJournal(1)).interactable=list.Length>1;
  var selected=list.FirstOrDefault(q=>q.Id==journalSelection);if(!selected){W.Label(body,"No hay misiones en esta categoría.\nBuscá el signo ! sobre los NPC del refugio.",0,275,676,120,23,W.Muted);return;}
  var record=state.Quest(selected.Id);var details=W.ScrollArea(body,"Quest details",0,265,676,245,430);
  W.Label(details,selected.Title,0,0,650,54,25,W.Gold);W.Label(details,selected.Description,0,60,650,100,21);W.Label(details,RiftJournalView.Objectives(selected,record),0,165,650,130,21);
  W.Label(details,"Recompensa: "+selected.Gold+" oro local · "+WarriorProgressionDefinition.Load.QuestExperience+" XP\n"+(record?.Status==QuestStatus.TurnedIn?"Completada":record?.Status==QuestStatus.Ready?"Entregar a "+NpcName(selected.TurnIn):"Hablar con "+NpcName(record==null?selected.Giver:selected.TurnIn)),0,305,650,100,21,W.Gold);
  if(record!=null&&record.Status!=QuestStatus.TurnedIn){BoundButton(body,RiftJournalView.Tracked==selected.Id?"OCULTAR SEGUIMIENTO":"SEGUIR MISIÓN","JournalTrack",0,535,430,58,ToggleJournalTracking);W.Button(body,"ABANDONAR",446,535,230,58,()=>Dialog("Abandonar misión",selected.Title+"\nSe perderá el progreso de esta misión.",()=>ClientFrontend.ClientPlayerInstance.GetComponent<RiftJournalPlayer>().CmdAbandon(selected.Id)));}
 }
}
}
