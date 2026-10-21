using UnityEngine;
namespace ROM.Progression
{
    [CreateAssetMenu(menuName="ROM/Progression/Achievement")]
    public sealed class RiftAchievementDefinition:ScriptableObject
    {
        public string Id,Title,Description;public int Points=10;public JournalObjective Criterion;
    }
}
