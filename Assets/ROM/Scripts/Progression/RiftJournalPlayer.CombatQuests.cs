using System.Linq;
using Mirror;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
namespace ROM.Progression {
public sealed partial class RiftJournalPlayer {
 void QuestDamage(Health victim,Health attacker,int amount){
  if(!isServer||amount<=0||!victim||!attacker||victim==attacker||victim.TrainingTarget||victim.Team==attacker.Team||!Player||!Player.MyCharacter||Player.MyCharacter.Health!=attacker)return;
  RecordCombatObjective(RiftProgressEvent.DamageDealt,amount);
 }
 void QuestHealing(Health target,Health healer,int amount){
  if(!isServer||amount<=0||!target||!healer||target.TrainingTarget||target.Team!=healer.Team||!Player||!Player.MyCharacter||Player.MyCharacter.Health!=healer)return;
  // Only effective health restored to living player characters, including the healer.
  var recipient=RiftClassCombat.Owner(target);if(!recipient||recipient.BOT)return;
  RecordCombatObjective(RiftProgressEvent.HealthRestored,amount);
 }
 void RecordCombatObjective(RiftProgressEvent kind,int amount){
  if(!Ensure()||!State.Quests.Any(q=>q.Status==QuestStatus.Active&&Catalog.Quest(q.Id)&&Catalog.Quest(q.Id).Objectives.Any(o=>o.Matches(kind,""))))return;
  ServerRecord(kind,"",amount);
 }
}
}
