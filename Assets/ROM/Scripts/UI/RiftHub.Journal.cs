using System;
using System.Linq;
using MultiFPS;
using MultiFPS.UI;
using ROM.Progression;
using UnityEngine;
using UnityEngine.UI;
using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.UI.Sprint01
{
    public sealed partial class RiftHub
    {
        string journalSelection="",journalFilter="Activas";
        JournalState JournalState=>RiftJournalView.Read(InGame?null:Roster.Active.Id);
        void JournalChanged(){if(InGame&&Page==HubPage.Abilities&&abilityTab=="Talents")return;if(Page==HubPage.Missions||Page==HubPage.Progression||Page==HubPage.Abilities||Page==HubPage.Character)Navigate(Page);}
        public void ShowJournal(){ShowGameplayPage(HubPage.Missions);}
        public void ShowQuest(string questId){journalSelection=questId;var record=JournalState.Quest(questId);journalFilter=record==null?"Disponibles":record.Status==QuestStatus.TurnedIn?"Completadas":"Activas";ShowGameplayPage(HubPage.Missions); }
        public void ShowAchievements(){ShowGameplayPage(HubPage.Progression);}
        void QuestJournal(Transform body)
        {
            if(InGame){CompactJournal(body);return;}
            var catalog=RiftJournalCatalog.Load;if(!catalog){W.Label(body,"Diario no disponible.",0,0,1500,60,24);return;}var state=JournalState;
            W.Dropdown(body,"Quest filter",0,0,520,new[]{"Activas","Completadas","Disponibles"},Array.IndexOf(new[]{"Activas","Completadas","Disponibles"},journalFilter),i=>{journalFilter=new[]{"Activas","Completadas","Disponibles"}[i];journalSelection="";Navigate(HubPage.Missions);});
            var list=catalog.Quests.Where(q=>journalFilter=="Activas"?state.Quest(q.Id)!=null&&state.Quest(q.Id).Status!=QuestStatus.TurnedIn:journalFilter=="Completadas"?state.TurnedIn(q.Id):state.Available(q)).ToArray();
            visibleQuests=list;if(!list.Any(q=>q.Id==journalSelection))journalSelection=list.FirstOrDefault()?.Id??"";
            var scroll=W.ScrollArea(body,"Quest list",0,75,560,570,list.Length*86);journalScroll=scroll;for(int i=0;i<list.Length;i++){var definition=list[i];W.Button(scroll,definition.Title,0,i*86,530,72,()=>{journalSelection=definition.Id;Navigate(HubPage.Missions);},journalSelection==definition.Id);}
            var previous=BoundButton(body,"ANTERIOR","JournalPrevious",0,650,265,38,()=>SelectJournal(-1));previous.interactable=list.Length>1;var next=BoundButton(body,"SIGUIENTE","JournalNext",280,650,265,38,()=>SelectJournal(1));next.interactable=list.Length>1;
            var selected=list.FirstOrDefault(q=>q.Id==journalSelection)??list.FirstOrDefault();if(!selected){W.Label(body,journalFilter=="Activas"?"No tenés misiones activas.\nBuscá el signo ! sobre los NPC del refugio.":"No hay misiones en esta categoría.",650,40,1050,170,25,W.Muted);return;}
            var record=state.Quest(selected.Id);W.Label(body,selected.Title,620,0,1120,55,30,W.Gold);W.Label(body,selected.Description,620,70,1100,120,23);W.Label(body,RiftJournalView.Objectives(selected,record),620,205,1100,160,22);
            W.Label(body,"Recompensa: "+selected.Gold+" oro local · "+selected.RewardExperience+" XP\n"+(record?.Status==QuestStatus.TurnedIn?"Completada":record?.Status==QuestStatus.Ready?"Lista para entregar a "+NpcName(selected.TurnIn):"Hablar con "+NpcName(record==null?selected.Giver:selected.TurnIn)),620,380,1100,90,23,W.Gold);
            if(record!=null&&record.Status!=QuestStatus.TurnedIn){BoundButton(body,RiftJournalView.Tracked==selected.Id?"OCULTAR SEGUIMIENTO":"SEGUIR MISIÓN","JournalTrack",620,510,520,60,ToggleJournalTracking);if(InGame)W.Button(body,"ABANDONAR",1160,510,500,60,()=>Dialog("Abandonar misión",selected.Title+"\nSe perderá el progreso de esta misión.",()=>ClientFrontend.ClientPlayerInstance.GetComponent<RiftJournalPlayer>().CmdAbandon(selected.Id)));}
        }
        public static string NpcName(string id)=>string.IsNullOrEmpty(id)?"NPC":char.ToUpperInvariant(id[0])+id.Substring(1);
        void AchievementJournal(Transform body)
        {
            var catalog=RiftJournalCatalog.Load;if(!catalog){W.Label(body,"Logros no disponibles.",24,24,1000,80,24);return;}
            body.gameObject.AddComponent<RiftAchievementView>().Build(this,JournalState,catalog,()=>{if(InGame)ResumeRequested?.Invoke();else Navigate(HubPage.Home);},Roster.Active.Skin);
        }
    }
}
