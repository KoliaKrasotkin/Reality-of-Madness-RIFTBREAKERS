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
        void JournalChanged(){if(Page==HubPage.Missions||Page==HubPage.Progression||Page==HubPage.Abilities||Page==HubPage.Character)Navigate(Page);}
        public void ShowJournal(){ShowGameplayPage(HubPage.Missions);}
        public void ShowAchievements(){ShowGameplayPage(HubPage.Progression);}
        void QuestJournal(Transform body)
        {
            var catalog=RiftJournalCatalog.Load;if(!catalog){W.Label(body,"Diario no disponible.",0,0,1500,60,24);return;}var state=JournalState;
            W.Dropdown(body,"Quest filter",0,0,520,new[]{"Activas","Completadas","Disponibles"},Array.IndexOf(new[]{"Activas","Completadas","Disponibles"},journalFilter),i=>{journalFilter=new[]{"Activas","Completadas","Disponibles"}[i];journalSelection="";Navigate(HubPage.Missions);});
            var list=catalog.Quests.Where(q=>journalFilter=="Activas"?state.Quest(q.Id)!=null&&state.Quest(q.Id).Status!=QuestStatus.TurnedIn:journalFilter=="Completadas"?state.TurnedIn(q.Id):state.Available(q)).ToArray();
            var scroll=W.ScrollArea(body,"Quest list",0,75,560,570,list.Length*86);for(int i=0;i<list.Length;i++){var definition=list[i];W.Button(scroll,definition.Title,0,i*86,530,72,()=>{journalSelection=definition.Id;Navigate(HubPage.Missions);},journalSelection==definition.Id);}
            var selected=list.FirstOrDefault(q=>q.Id==journalSelection)??list.FirstOrDefault();if(!selected){W.Label(body,journalFilter=="Activas"?"No tenés misiones activas.\nBuscá el signo ! sobre los NPC del refugio.":"No hay misiones en esta categoría.",650,40,1050,170,25,W.Muted);return;}
            var record=state.Quest(selected.Id);W.Label(body,selected.Title,620,0,1120,55,30,W.Gold);W.Label(body,selected.Description,620,70,1100,120,23);W.Label(body,RiftJournalView.Objectives(selected,record),620,205,1100,160,22);
            W.Label(body,"Recompensa: "+selected.Gold+" oro local · "+MultiFPS.Gameplay.RiftCombat.WarriorProgressionDefinition.Load.QuestExperience+" XP\n"+(record?.Status==QuestStatus.TurnedIn?"Completada":record?.Status==QuestStatus.Ready?"Lista para entregar a "+NpcName(selected.TurnIn):"Hablar con "+NpcName(record==null?selected.Giver:selected.TurnIn)),620,380,1100,90,23,W.Gold);
            if(record!=null&&record.Status!=QuestStatus.TurnedIn){W.Button(body,RiftJournalView.Tracked==selected.Id?"OCULTAR SEGUIMIENTO":"SEGUIR MISIÓN",620,510,520,60,()=>{RiftJournalView.Tracked=RiftJournalView.Tracked==selected.Id?"-":selected.Id;Navigate(HubPage.Missions);});if(InGame)W.Button(body,"ABANDONAR",1160,510,500,60,()=>Dialog("Abandonar misión",selected.Title+"\nSe perderá el progreso de esta misión.",()=>ClientFrontend.ClientPlayerInstance.GetComponent<RiftJournalPlayer>().CmdAbandon(selected.Id)));}
        }
        public static string NpcName(string id)=>string.IsNullOrEmpty(id)?"NPC":char.ToUpperInvariant(id[0])+id.Substring(1);
        void AchievementJournal(Transform body)
        {
            var catalog=RiftJournalCatalog.Load;if(!catalog)return;var state=JournalState;int points=catalog.Achievements.Where(a=>state.Achievements.Any(r=>r.Id==a.Id)).Sum(a=>a.Points);
            W.Label(body,"LOGROS: "+state.Achievements.Count+" / "+catalog.Achievements.Length+" · "+points+" PUNTOS",0,0,1700,55,30,W.Gold);
            var scroll=W.ScrollArea(body,"Achievement list",0,78,1750,560,catalog.Achievements.Length*110);for(int i=0;i<catalog.Achievements.Length;i++){var a=catalog.Achievements[i];var unlocked=state.Achievements.FirstOrDefault(r=>r.Id==a.Id);int count=Mathf.Min(a.Criterion.Required,state.Count(a.Criterion));W.Label(scroll,(unlocked!=null?"✓ ":"")+a.Title+" · "+a.Points+" puntos",10,i*110,1700,40,25,unlocked!=null?W.Gold:W.White);W.Label(scroll,a.Description+"\n"+(unlocked!=null?"Desbloqueado · "+DateTimeOffset.FromUnixTimeSeconds(unlocked.UnlockedAt).ToString("dd/MM/yyyy"):"Progreso: "+count+" / "+a.Criterion.Required),10,i*110+42,1700,60,20,W.Muted);}
        }
    }
}
