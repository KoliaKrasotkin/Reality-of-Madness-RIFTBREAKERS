using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
using ROM.Progression;
using ROM.UI.Gameplay;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 void ModernPause(){
  var panel=RiftSkillStyle.Panel(content,"Modern pause landing",1104,0,744,810,.83f);
  RiftWidgets.Label(panel,"MENÚ",28,20,688,54,34,RiftSkillStyle.White);
  RiftWidgets.Label(panel,"La partida online continúa mientras este menú está abierto.",28,82,688,50,18,RiftSkillStyle.Muted);
  string[] labels={"REANUDAR","PERSONAJE E INVENTARIO","HABILIDADES","TALENTOS","DIARIO DE MISIONES","SOCIAL LOCAL","AJUSTES","EQUIPO ACTUAL"};
  string[] actions={"Pause","Inventory","Abilities","Talents","Journal","Friends","Settings","Equipment"};
  System.Action[] callbacks={()=>ResumeRequested?.Invoke(),()=>ShowGameplayPage(HubPage.Inventory),ShowSkills,ShowTalents,()=>ShowGameplayPage(HubPage.Missions),()=>ShowGameplayPage(HubPage.Social),ShowGameSettings,ShowEquipmentActions};
  for(int i=0;i<labels.Length;i++){int index=i;var key=RiftControls.ActionLabel(actions[i]);var title=labels[i]+(i==7?"":"  ["+key+"]");var button=RiftSkillStyle.Button(panel,title,28,150+i*64,688,52,()=>callbacks[index](),i==0);var tip=button.gameObject.AddComponent<RiftTooltipTrigger>();tip.Title=labels[i];tip.Description=i==5?"Contactos y mensajes guardados solamente en esta PC.":i==7?"Ver progreso y cambiar entre las armas soportadas por el servidor.":"Abrir la acción existente sin detener la simulación online.";if(i!=7)tip.ActionId=actions[i];}
  RiftSkillStyle.Button(panel,"SALIR DE PARTIDA",28,716,688,54,()=>Dialog("Volver al menú","Vas a desconectarte de esta partida.",()=>LeaveRequested?.Invoke()));
  if(EventSystem.current)EventSystem.current.SetSelectedGameObject(panel.GetComponentInChildren<Button>().gameObject);
 }
 void ShowEquipmentActions(){
  var player=ClientFrontend.ClientPlayerInstance;var combat=player?player.GetComponent<AbilityController>():null;var journal=player?player.GetComponent<RiftJournalPlayer>():null;
  if(!combat||!combat.Prototype||!journal){Notify("El equipo está disponible cuando el Warrior está en partida.");return;}
  bool sword=combat.SwordAndShield;string current=sword?"Espada y escudo":"Hacha a dos manos";string next=sword?"Hacha a dos manos":"Espada y escudo";
  string info="Actual: "+current+"\nNivel "+combat.Level+" · XP "+combat.Progression.InLevel(combat.Experience)+" / "+combat.Progression.Required(combat.Level)+"\nCambiar a: "+next+"\nEl servidor valida el cambio de arma.";
  Dialog("Equipo del Warrior",info,()=>{if(journal&&combat)journal.CmdWarriorWeapon(!sword);});
 }
}
}
