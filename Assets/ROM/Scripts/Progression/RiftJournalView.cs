using System;
using System.Linq;
using MultiFPS;
using UnityEngine;
using ROM.UI.Sprint01;
namespace ROM.Progression
{
    // Read-only local cache for menus/offline display. Only server snapshots grant rewards.
    public static class RiftJournalView
    {
        public static event Action Changed;
        public static string Notice {get;private set;}public static float NoticeUntil {get;private set;}
        static string cachedId;static JournalState cached;
        public static string ActiveId=>PlayerPrefs.GetString("ROM.Character.ActiveId",UserSettings.UserNickname);
        public static string Key(string id)=>"ROM.Journal.Cache.v1."+id;
        public static JournalState Read(string id=null){id=id??ActiveId;if(cached!=null&&cachedId==id)return cached;cachedId=id;try{cached=JsonUtility.FromJson<JournalState>(PlayerPrefs.GetString(Key(id),""));}catch{cached=null;}return cached??(cached=new JournalState{CharacterId=id});}
        public static void ApplyServerSnapshot(string json,string notice)
        {
            var measured=MultiFPS.Gameplay.RiftFrameDiagnostics.Begin();var state=JsonUtility.FromJson<JournalState>(json);if(state==null)return;cachedId=state.CharacterId;cached=state;PlayerPrefs.SetString(Key(cachedId),json);RiftLootWalletBridge.Apply(state);
            // Completed legacy quests are not paid retroactively. Rewards are committed by the Host with the journal.
            PlayerPrefs.Save();if(!string.IsNullOrEmpty(notice))Notify(notice);Changed?.Invoke();MultiFPS.Gameplay.RiftFrameDiagnostics.End(3,measured);
        }
        public static void Notify(string text){Notice=text;NoticeUntil=Time.unscaledTime+5;}
        public static string Tracked{get=>PlayerPrefs.GetString("ROM.Journal.Track."+ActiveId,"");set{PlayerPrefs.SetString("ROM.Journal.Track."+ActiveId,value);PlayerPrefs.Save();Changed?.Invoke();}}
        public static string Offer(string npcId){var catalog=RiftJournalCatalog.Load;if(!catalog)return "";var state=Read();if(catalog.Quests.Any(q=>q.TurnIn==npcId&&state.Quest(q.Id)?.Status==QuestStatus.Ready))return "?";return catalog.Quests.Any(q=>q.Giver==npcId&&state.Available(q))?"!":"";}
        public static string Objectives(RiftQuestDefinition definition,QuestRecord record)=>string.Join("\n",definition.Objectives.Select((o,i)=>o.Label+"  "+(record?.Progress!=null&&i<record.Progress.Length?record.Progress[i]:0)+" / "+o.Required));
    }
}
