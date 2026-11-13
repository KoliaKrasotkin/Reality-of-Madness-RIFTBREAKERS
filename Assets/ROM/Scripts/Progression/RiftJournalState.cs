using System;
using System.Collections.Generic;
using System.Linq;
using MultiFPS.Gameplay;
using UnityEngine;
namespace ROM.Progression
{
    public enum QuestStatus { Active=1,Ready=2,TurnedIn=3 }
    [Serializable]public sealed class QuestRecord {public string Id;public QuestStatus Status;public int[] Progress;public long CompletedAt;public int RewardLedgerVersion,GoldRewardCommitted;public List<string> Seen=new List<string>();}
    [Serializable]public sealed class AchievementRecord {public string Id;public long UnlockedAt;}
    [Serializable]public sealed class JournalCounter {public RiftProgressEvent Kind;public string Key;public int Value;}
    [Serializable]public sealed partial class JournalState
    {
        public int Experience;public int[] TalentRanks=new int[9];public bool SwordAndShield;
        public int Version=2;public string CharacterId;public List<QuestRecord> Quests=new List<QuestRecord>();public List<AchievementRecord> Achievements=new List<AchievementRecord>();public List<JournalCounter> Counters=new List<JournalCounter>();public List<string> UniqueKeys=new List<string>();
        public QuestRecord Quest(string id)=>Quests.Find(q=>q.Id==id);
        public bool TurnedIn(string id)=>Quest(id)?.Status==QuestStatus.TurnedIn;
        public bool PrerequisitesMet(RiftQuestDefinition definition)=>definition&&(string.IsNullOrEmpty(definition.Prerequisite)||TurnedIn(definition.Prerequisite))&&(definition.AdditionalPrerequisites==null||definition.AdditionalPrerequisites.All(id=>string.IsNullOrEmpty(id)||TurnedIn(id)));
        public bool Available(RiftQuestDefinition definition)=>definition&&ROM.Tutorial.Crilium.CriliumSession.CanOfferQuest(definition.Id)&&Quest(definition.Id)==null&&PrerequisitesMet(definition);
        public int Count(JournalObjective criterion){if(criterion==null)return 0;if(criterion.Unique){string prefix=((int)criterion.Event)+":";return UniqueKeys.Count(k=>k.StartsWith(prefix)&&(string.IsNullOrEmpty(criterion.Filter)||k==prefix+criterion.Filter));}return Counters.Where(c=>c.Kind==criterion.Event&&(string.IsNullOrEmpty(criterion.Filter)?c.Key=="":c.Key==criterion.Filter)).Sum(c=>c.Value);}
        public bool Accept(RiftQuestDefinition definition){if(!Available(definition))return false;var record=new QuestRecord{Id=definition.Id,Status=QuestStatus.Active,Progress=new int[definition.Objectives.Length]};
            for(int i=0;i<definition.Objectives.Length;i++){var o=definition.Objectives[i];if(o.CreditPriorCompletion&&(o.Event==RiftProgressEvent.Checkpoint||o.Event==RiftProgressEvent.Encounter))record.Progress[i]=Mathf.Min(o.Required,Count(o));}
            if(definition.Objectives.Select((o,i)=>record.Progress[i]>=o.Required).All(v=>v))record.Status=QuestStatus.Ready;
            Quests.Add(record);return true;}
        public List<string> Record(RiftJournalCatalog catalog,RiftProgressEvent kind,string key,int amount=1,string encounter="")
        {
            var notices=new List<string>();if(amount<=0)return notices;key=key??"";amount=Mathf.Clamp(amount,1,100000);string token=((int)kind)+":"+key;bool fresh=!UniqueKeys.Contains(token);if(fresh)UniqueKeys.Add(token);
            AddCounter(kind,"",amount);if(key.Length>0)AddCounter(kind,key,amount);
            foreach(var quest in Quests){if(quest.Status!=QuestStatus.Active)continue;var definition=catalog.Quest(quest.Id);if(!definition)continue;if(quest.Progress==null||quest.Progress.Length!=definition.Objectives.Length)quest.Progress=new int[definition.Objectives.Length];for(int i=0;i<definition.Objectives.Length;i++){var objective=definition.Objectives[i];if(objective.Matches(kind,key,encounter)){string objectiveKey=i+":"+key;if(objective.Unique&&quest.Seen.Contains(objectiveKey))continue;if(objective.Unique)quest.Seen.Add(objectiveKey);quest.Progress[i]=Mathf.Min(objective.Required,quest.Progress[i]+amount);}}if(definition.Objectives.Select((o,i)=>quest.Progress[i]>=o.Required).All(v=>v)){quest.Status=QuestStatus.Ready;notices.Add("Misión lista: "+definition.Title);}}
            foreach(var achievement in catalog.Achievements)if(achievement&&!Achievements.Any(a=>a.Id==achievement.Id)&&Count(achievement.Criterion)>=achievement.Criterion.Required){Achievements.Add(new AchievementRecord{Id=achievement.Id,UnlockedAt=DateTimeOffset.UtcNow.ToUnixTimeSeconds()});notices.Add("Logro desbloqueado: "+achievement.Title);}
            return notices;
        }
        void AddCounter(RiftProgressEvent kind,string key,int amount){var counter=Counters.Find(c=>c.Kind==kind&&c.Key==key);if(counter==null){counter=new JournalCounter{Kind=kind,Key=key};Counters.Add(counter);}counter.Value=Mathf.Min(10000000,counter.Value+amount);}
        public bool TurnIn(RiftQuestDefinition definition){var quest=Quest(definition.Id);if(quest==null||quest.Status!=QuestStatus.Ready)return false;quest.Status=QuestStatus.TurnedIn;quest.CompletedAt=DateTimeOffset.UtcNow.ToUnixTimeSeconds();return true;}
    }
}
