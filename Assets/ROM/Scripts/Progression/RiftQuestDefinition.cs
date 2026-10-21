using UnityEngine;
namespace ROM.Progression
{
    [CreateAssetMenu(menuName="ROM/Progression/Quest")]
    public sealed class RiftQuestDefinition:ScriptableObject
    {
        public string Id,Title,Description,Giver,TurnIn,Prerequisite;public int Gold;public JournalObjective[] Objectives;
    }
}
