using System;
using MultiFPS.Gameplay;
using UnityEngine;
namespace ROM.Progression
{
    [Serializable]public sealed class JournalObjective
    {
        public RiftProgressEvent Event;public string Filter,Label;public int Required=1;public bool Unique;
        public bool Matches(RiftProgressEvent kind,string key)=>Event==kind&&(string.IsNullOrEmpty(Filter)||Filter==key);
    }
    [CreateAssetMenu(menuName="ROM/Progression/Journal catalog")]
    public sealed class RiftJournalCatalog:ScriptableObject
    {
        public RiftQuestDefinition[] Quests;public RiftAchievementDefinition[] Achievements;
        public RiftQuestDefinition Quest(string id)=>Array.Find(Quests,q=>q&&q.Id==id);
        public static RiftJournalCatalog Load=>Resources.Load<RiftJournalCatalog>("RiftJournalCatalog");
    }
}
