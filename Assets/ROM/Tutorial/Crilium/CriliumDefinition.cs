using UnityEngine;
using ROM.Progression;
using ROM.UI.Gameplay;
using MultiFPS.Gameplay.RiftEnemies;
namespace ROM.Tutorial.Crilium {
public enum CriliumLesson { Movement,Camera,AutoAttack,Fury,Cleave,Mobility,Defence,Loot,Equipment,Spellbook,Talents,Journal,Map,Final,Portal,Introduction }
[System.Serializable]public sealed class CriliumLessonDefinition {public CriliumLesson Lesson;public string Title;[TextArea]public string Instruction,Help;public Vector3 Goal;public float ArrivalRadius=9;}
[CreateAssetMenu(menuName="ROM/Crilium/Definition")]
public sealed class CriliumDefinition:ScriptableObject {
 public CriliumLessonDefinition[] Lessons;public RiftEnemyDefinition Target,Enemy,Sentinel;public RiftJournalCatalog Journal;public RiftMinimapAtlas Atlas;
 public float RetryHealthFraction=1;public string QuestId="crilium-before-the-rift";
 public static CriliumDefinition Load=>Resources.Load<CriliumDefinition>("Crilium/Definition");
}
}
