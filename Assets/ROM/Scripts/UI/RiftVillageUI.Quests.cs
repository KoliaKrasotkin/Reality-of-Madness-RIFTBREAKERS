using System.Linq;
using MultiFPS.UI;
using ROM.Horde;
using ROM.Progression;
using UnityEngine;
using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.UI.Sprint01
{
    public sealed partial class RiftVillageUI
    {
        void JournalChanged(){if(TalkingTo&&dialog&&dialog.Find("Misiones"))ShowQuests(TalkingTo);}
        public void ShowQuests(RiftVillageNpc npc)
        {
            if(!dialog||TalkingTo!=npc)return;foreach(string name in new[]{"Misiones","Mercancia"}){var old=dialog.Find(name);if(old)DestroyImmediate(old.gameObject);}var catalog=RiftJournalCatalog.Load;if(!catalog)return;var state=RiftJournalView.Read();var journal=ClientFrontend.ClientPlayerInstance?ClientFrontend.ClientPlayerInstance.GetComponent<RiftJournalPlayer>():null;
            var definitions=catalog.Quests.Where(q=>q.Giver==npc.Id&&state.Available(q)||q.TurnIn==npc.Id&&state.Quest(q.Id)!=null&&!state.TurnedIn(q.Id)).ToArray();
            var area=W.ScrollArea(dialog,"Misiones",30,300,920,380,Mathf.Max(380,definitions.Length*200));area.name="Quest content";
            if(definitions.Length==0){W.Label(area,"No tengo nuevas misiones para vos por ahora.",0,10,860,60,23,W.Muted);return;}
            for(int i=0;i<definitions.Length;i++){var q=definitions[i];var record=state.Quest(q.Id);int y=i*200;W.Label(area,q.Title,0,y,850,32,24,W.White);W.Label(area,"Recompensa: "+q.Gold+" oro local · "+MultiFPS.Gameplay.RiftCombat.WarriorProgressionDefinition.Load.QuestExperience+" XP",0,y+36,850,25,16,W.Muted);W.Label(area,record==null?q.Description:RiftJournalView.Objectives(q,record),0,y+70,850,64,18);
                if(journal){if(record==null)W.Button(area,"ACEPTAR MISIÓN",0,y+142,410,45,()=>journal.CmdAccept(q.Id,npc.Id));else if(record.Status==QuestStatus.Ready)W.Button(area,"ENTREGAR · "+q.Gold+" ORO",0,y+142,410,45,()=>journal.CmdTurnIn(q.Id,npc.Id),true);else W.Label(area,"En progreso",0,y+142,410,40,20,W.Muted);}
                W.Button(area,"VER DIARIO",450,y+142,410,45,()=>{Close();var hud=Object.FindFirstObjectByType<ROM.UI.Gameplay.RiftGameplayHUD>();if(hud){hud.Open(HubPage.Missions);var active=UIPauseMenu._instance.pauseMenuOverlay.GetComponentInChildren<RiftHub>();if(active)active.ShowQuest(q.Id);}});
            }
            RiftPanelChrome.Apply(area);
        }
    }
}
