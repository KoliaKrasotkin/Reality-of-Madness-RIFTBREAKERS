using System.Linq;
using MultiFPS;
using ROM.Progression;
using ROM.UI.Sprint01;
using UnityEngine;
using UnityEngine.UI;
namespace ROM.UI.Gameplay
{
    [DefaultExecutionOrder(950)]public sealed class RiftJournalHUD:MonoBehaviour
    {
        public RectTransform Tracker;public Text Title,Objectives,Notification;
        float next;
        void LateUpdate(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.15f;var catalog=RiftJournalCatalog.Load;if(!catalog)return;var state=RiftJournalView.Read();string tracked=RiftJournalView.Tracked;var record=tracked=="-"?null:state.Quest(tracked)??state.Quests.FirstOrDefault(q=>q.Status!=QuestStatus.TurnedIn);if(record?.Status==QuestStatus.TurnedIn)record=state.Quests.FirstOrDefault(q=>q.Status!=QuestStatus.TurnedIn);var definition=record!=null?catalog.Quest(record.Id):null;Tracker.gameObject.SetActive(definition);if(definition){Title.text="["+RiftControls.Label("Journal")+"] "+definition.Title;Objectives.text=record.Status==QuestStatus.Ready?"Lista para entregar · "+RiftHub.NpcName(definition.TurnIn):RiftJournalView.Objectives(definition,record);}Notification.text=Time.unscaledTime<RiftJournalView.NoticeUntil?RiftJournalView.Notice:"";}
    }
}
