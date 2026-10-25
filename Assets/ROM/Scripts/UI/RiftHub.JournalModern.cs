using System;
using System.Linq;
using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
using ROM.Progression;
using ROM.UI.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 void BuildJournalNavigation(RectTransform panel){
  var glass=panel.GetComponent<Image>();glass.color=new Color(.025f,.028f,.046f,.88f);foreach(Transform c in panel)if(c.name=="Top"||c.name=="Bottom"||c.name=="Left"||c.name=="Right"||c.name=="Accent")c.gameObject.SetActive(false);
  string[] titles={"INVENTARIO","TALENTOS","HABILIDADES","DIARIO"};Action[] actions={()=>ShowGameplayPage(HubPage.Inventory),ShowTalents,ShowSkills,ShowJournal};
  for(int i=0;i<4;i++){int id=i;var button=RiftSkillStyle.Button(panel,titles[i],16+i*218,14,210,54,()=>actions[id](),i==3);var t=button.GetComponentInChildren<Text>();t.fontSize=17;var tip=button.gameObject.AddComponent<RiftTooltipTrigger>();tip.Title=titles[i];tip.Description="Abrir "+titles[i].ToLowerInvariant()+".";tip.ActionId=new[]{"Inventory","Talents","Abilities","Journal"}[i];}
  RiftSkillStyle.Button(panel,"×",900,14,70,54,()=>ResumeRequested?.Invoke());W.Image(panel,"Journal navigation divider",20,84,948,1,new Color(.4f,.38f,.5f,.5f));
 }
 void ModernJournal(RectTransform body){
  var catalog=RiftJournalCatalog.Load;if(!catalog){W.Label(body,"Diario no disponible.",0,0,900,60,22);return;}
  var state=JournalState;W.Label(body,"DIARIO",0,0,900,42,30,RiftSkillStyle.White);W.Label(body,"Misiones de El Rift",0,43,900,28,18,RiftSkillStyle.Muted);
  string[] filters={"Activas","Completadas","Disponibles"};for(int i=0;i<filters.Length;i++){string filter=filters[i];RiftSkillStyle.Button(body,filter,i*316,88,304,42,()=>{journalFilter=filter;journalSelection="";Navigate(HubPage.Missions);},journalFilter==filter);}
  var list=catalog.Quests.Where(q=>journalFilter=="Activas"?state.Quest(q.Id)!=null&&state.Quest(q.Id).Status!=QuestStatus.TurnedIn:journalFilter=="Completadas"?state.TurnedIn(q.Id):state.Available(q)).ToArray();visibleQuests=list;
  if(!list.Any(q=>q.Id==journalSelection))journalSelection=list.FirstOrDefault()?.Id??"";
  if(list.Length==0){
   var symbol=W.Rect(body,"Empty journal emblem",415,220,72,72).gameObject.AddComponent<RiftHUDGraphic>();symbol.Shape=HUDShape.Diamond;symbol.color=new Color(.65f,.5f,.9f,.6f);symbol.Border=RiftSkillStyle.Violet;symbol.raycastTarget=false;
   var title=W.Label(body,journalFilter=="Activas"?"Tu próxima historia empieza en el refugio":journalFilter=="Completadas"?"Todavía no hay misiones entregadas":"No hay nuevas misiones disponibles",70,320,790,65,25,RiftSkillStyle.White);title.alignment=TextAnchor.MiddleCenter;
   var offer=catalog.Quests.FirstOrDefault(q=>state.Available(q));string guide=journalFilter=="Activas"&&offer?"Hablá con "+NpcName(offer.Giver)+" para conocer sus misiones.":journalFilter=="Completadas"?"Las misiones que entregues aparecerán aquí.":"Seguí las misiones activas y regresá al refugio.";
   var text=W.Label(body,guide,100,403,730,100,21,RiftSkillStyle.Muted);text.alignment=TextAnchor.UpperCenter;return;
  }
  var scroll=W.ScrollArea(body,"Quest list",0,158,300,566,list.Length*116);journalScroll=scroll;
  for(int i=0;i<list.Length;i++){var q=list[i];var card=RiftSkillStyle.Button(scroll,q.Title,0,i*116,284,104,()=>{journalSelection=q.Id;Navigate(HubPage.Missions);},q.Id==journalSelection);var t=card.GetComponentInChildren<Text>();t.fontSize=19;t.alignment=TextAnchor.MiddleLeft;t.rectTransform.anchorMin=t.rectTransform.anchorMax=t.rectTransform.pivot=new Vector2(0,1);t.rectTransform.anchoredPosition=new Vector2(16,-12);t.rectTransform.sizeDelta=new Vector2(252,80);var tip=card.gameObject.AddComponent<RiftTooltipTrigger>();tip.Title=q.Title;tip.Description=state.Quest(q.Id)?.Status==QuestStatus.Ready?"Lista para entregar.":q.Description;}
  if(list.Length>1){BoundButton(body,"ANTERIOR","JournalPrevious",0,740,140,40,()=>SelectJournal(-1));BoundButton(body,"SIGUIENTE","JournalNext",152,740,140,40,()=>SelectJournal(1));}
  var selected=list.First(q=>q.Id==journalSelection);var record=state.Quest(selected.Id);var detail=W.ScrollArea(body,"Quest details",324,158,608,556,580);
  W.Label(detail,selected.Title,0,0,588,72,28,RiftSkillStyle.White);W.Label(detail,record?.Status==QuestStatus.Ready?"LISTA PARA ENTREGAR":record?.Status==QuestStatus.TurnedIn?"COMPLETADA":record==null?"DISPONIBLE":"EN PROGRESO",0,80,588,28,16,RiftSkillStyle.Violet);
  W.Label(detail,selected.Description,0,126,588,110,20,RiftSkillStyle.Muted);W.Label(detail,"OBJETIVOS",0,255,588,28,16,RiftSkillStyle.Muted);W.Label(detail,RiftJournalView.Objectives(selected,record),0,296,588,140,21,RiftSkillStyle.White);
  W.Label(detail,"Recompensa: "+selected.Gold+" oro local · "+selected.RewardExperience+" XP\n"+(record?.Status==QuestStatus.Ready?"Entregar a ":"Hablar con ")+NpcName(record==null?selected.Giver:selected.TurnIn),0,454,588,100,19,RiftSkillStyle.Muted);
  if(record!=null&&record.Status!=QuestStatus.TurnedIn){BoundButton(body,RiftJournalView.Tracked==selected.Id?"OCULTAR SEGUIMIENTO":"SEGUIR MISIÓN","JournalTrack",324,740,365,44,ToggleJournalTracking);RiftSkillStyle.Button(body,"ABANDONAR",705,740,227,44,()=>Dialog("Abandonar misión",selected.Title+"\nSe perderá el progreso de esta misión.",()=>ClientFrontend.ClientPlayerInstance.GetComponent<RiftJournalPlayer>().CmdAbandon(selected.Id)));}
 }
}
}
