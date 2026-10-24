using UnityEngine;
namespace ROM.Progression
{
    [CreateAssetMenu(menuName="ROM/Progression/Quest")]
    public sealed class RiftQuestDefinition:ScriptableObject
    {
        public int RewardExperience=>Experience<0?MultiFPS.Gameplay.RiftCombat.WarriorProgressionDefinition.Load.QuestExperience:Mathf.Max(0,Experience);
        public string Id,Title,Description,Giver,TurnIn,Prerequisite;public int Gold;public int Experience=-1;public string[] AdditionalPrerequisites=System.Array.Empty<string>();public JournalObjective[] Objectives;
    }
}
