using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftEnemies;
using ROM.Progression;
using UnityEngine;
namespace ROM.Horde {
public partial class Horde {
 // Legacy test callers without a victim identity report progression only; physical drops require the real corpse context.
 [Server]public void ServerNativeEnemyReward(RiftEnemyDefinition definition,Health killer,RiftEncounter encounter,bool cleared,Vector3 deathPosition=default,uint victimId=0,int enemyLevel=1){var levelTuning=RiftEnemyLevelTuning.Load;int rewardXp=levelTuning.Experience(definition.Experience,enemyLevel),rewardGold=levelTuning.Gold(definition.Gold,enemyLevel);
  var player=RiftClassCombat.Owner(killer);if(player){var journal=player.GetComponent<RiftJournalPlayer>();if(journal){if(definition.IsWildlife)journal.ServerGrantExperience(rewardXp);else journal.ServerRecord(RiftProgressEvent.Kill,definition.Id,1,rewardXp,encounter?encounter.Title:"");if(definition.IsBoss)journal.ServerRecord(RiftProgressEvent.MiniBoss,definition.Id,1,0,encounter?encounter.Title:"");}RiftGroundLoot.ServerDropForKill(player,victimId,deathPosition,rewardGold,definition.LootTable);}
  if(cleared&&encounter)foreach(var human in GameManager.Players)if(human&&!human.BOT&&human.MyCharacter&&(human.MyCharacter.transform.position-encounter.transform.position).sqrMagnitude<180*180){RiftJournalPlayer.Record(human,RiftProgressEvent.Encounter,encounter.Title);TargetExplorationNotice(human.connectionToClient,75,encounter.Title+" despejado · +75 oro local",null);}
 }
}
}
