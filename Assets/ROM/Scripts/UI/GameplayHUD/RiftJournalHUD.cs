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
        void Start(){var hud=GetComponent<RiftGameplayHUD>();if(!RiftPresentationSettings.Load.KeepAuthoredHUDLayout&&hud&&hud.Theme&&hud.Theme.BodyFont){foreach(var label in new[]{Title,Objectives,Notification})if(label)label.font=RiftPresentationSettings.Load.BodyFont?RiftPresentationSettings.Load.BodyFont:hud.Theme.BodyFont;Title.fontSize=18;}if(!GetComponent<RiftQuestGuidance>())gameObject.AddComponent<RiftQuestGuidance>();}
        float next;
        void LateUpdate(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.15f;var catalog=RiftJournalCatalog.Load;if(!catalog)return;var state=RiftJournalView.Read();var record=RiftQuestGuidance.Tracked(state);var definition=record!=null?catalog.Quest(record.Id):null;Tracker.gameObject.SetActive(definition);if(definition){Title.text="["+RiftControls.Label("Journal")+"] "+definition.Title;Objectives.text=ROM.Tutorial.Crilium.CriliumSession.ObjectiveText(definition.Id)??(record.Status==QuestStatus.Ready?"Lista para entregar · "+RiftHub.NpcName(definition.TurnIn):RiftJournalView.Objectives(definition,record));}Notification.text=Time.unscaledTime<RiftJournalView.NoticeUntil?RiftJournalView.Notice:"";}
    }
}
