using System;
using MultiFPS.Gameplay;
using UnityEngine;
namespace ROM.Progression
{
    [Serializable]public sealed class JournalObjective
    {
        public RiftProgressEvent Event;public string Filter,Label;public int Required=1;public bool Unique;public bool CreditPriorCompletion;public string EncounterFilter;
        public bool Matches(RiftProgressEvent kind,string key,string encounter="")=>Event==kind&&(string.IsNullOrEmpty(Filter)||Filter==key)&&(string.IsNullOrEmpty(EncounterFilter)||EncounterFilter==encounter);
    }
    [CreateAssetMenu(menuName="ROM/Progression/Journal catalog")]
    public sealed class RiftJournalCatalog:ScriptableObject
    {
        public RiftQuestDefinition[] Quests;public RiftAchievementDefinition[] Achievements;
        public RiftQuestDefinition Quest(string id)=>Array.Find(Quests,q=>q&&q.Id==id);
        static RiftJournalCatalog cached;public static RiftJournalCatalog Load=>ROM.Tutorial.Crilium.CriliumSession.Active&&ROM.Tutorial.Crilium.CriliumDefinition.Load?ROM.Tutorial.Crilium.CriliumDefinition.Load.Journal:cached?cached:(cached=Resources.Load<RiftJournalCatalog>("RiftJournalCatalog"));
    }
}
