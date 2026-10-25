using System;using System.Linq;using System.Collections.Generic;using Mirror;using UnityEngine;using MultiFPS;using MultiFPS.Gameplay;using MultiFPS.Gameplay.RiftCombat;using MultiFPS.Gameplay.RiftEnemies;
namespace ROM.Progression {
public sealed partial class JournalState {
 // Shared quest credit deliberately does not touch XP, wallet, global kill counters or achievements.
 public bool CreditPartyKill(RiftJournalCatalog catalog,string life,RiftProgressEvent kind,string key,string encounter,List<string> notices){
  if(!catalog||string.IsNullOrEmpty(life)||(kind!=RiftProgressEvent.Kill&&kind!=RiftProgressEvent.MiniBoss))return false;bool changed=false;
  foreach(var record in Quests){if(record.Status!=QuestStatus.Active)continue;var definition=catalog.Quest(record.Id);if(!definition||definition.Objectives==null)continue;bool questChanged=false;
   for(int i=0;i<definition.Objectives.Length;i++){var objective=definition.Objectives[i];if(objective==null||!objective.Matches(kind,key,encounter))continue;if(record.Progress==null||record.Progress.Length!=definition.Objectives.Length)record.Progress=new int[definition.Objectives.Length];if(record.Progress[i]>=objective.Required)continue;
    string receipt="party-death:"+i+":"+(int)kind+":"+life,unique=i+":"+key;if(record.Seen==null)record.Seen=new List<string>();if(record.Seen.Contains(receipt)||(objective.Unique&&record.Seen.Contains(unique)))continue;record.Seen.Add(receipt);if(objective.Unique)record.Seen.Add(unique);record.Progress[i]=Mathf.Min(objective.Required,record.Progress[i]+1);changed=questChanged=true;
   }
   if(questChanged&&definition.Objectives.Select((o,i)=>record.Progress!=null&&i<record.Progress.Length&&record.Progress[i]>=o.Required).All(v=>v)){record.Status=QuestStatus.Ready;notices.Add("Misión lista: "+definition.Title);}
  }return changed;
 }
}
public sealed partial class RiftJournalPlayer {
 [Server]public bool ServerPartyQuestKill(string life,RiftProgressEvent kind,string key,string encounter){if(!Ensure()||!Player||!Player.MyCharacter||Player.MyCharacter.Health.CurrentHealth<=0)return false;string previous=JsonUtility.ToJson(State);var notices=new List<string>();if(!State.CreditPartyKill(Catalog,life,kind,key,encounter,notices))return false;if(!Persist()){State=JsonUtility.FromJson<JournalState>(previous);return false;}Push(string.Join("\n",notices));return true;}
}
public static class RiftPartyQuestCredit {
 sealed class Death {public string Life;public readonly HashSet<RiftProgressEvent> Dispatched=new HashSet<RiftProgressEvent>();}
 static readonly Dictionary<Health,Death> deaths=new Dictionary<Health,Death>();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>deaths.Clear();
 public static int Share(PlayerInstance killer,Health victim,RiftProgressEvent kind,string key,string encounter){
  if(!NetworkServer.active||!killer||killer.BOT||!killer.MyCharacter||!victim||victim.CurrentHealth>0||victim.TrainingTarget||victim.Team==killer.MyCharacter.Health.Team||(kind!=RiftProgressEvent.Kill&&kind!=RiftProgressEvent.MiniBoss))return 0;
  foreach(var old in deaths.Keys.Where(h=>!h).ToArray())deaths.Remove(old);if(!deaths.TryGetValue(victim,out var death)||death.Life!=victim.ServerLifeToken)deaths[victim]=death=new Death{Life=victim.ServerLifeToken};if(!death.Dispatched.Add(kind))return 0;
  var native=victim.GetComponent<RiftEnemyAgent>();var legacy=victim.GetComponent<RiftEnemyCombat>();var participants=native?native.LastKillParticipants:legacy?legacy.QuestParticipants:Array.Empty<Health>();var tuning=RiftPartyQuestTuning.Load;int credited=0;
  // A contributing party keeps quest credit even if an unrelated player lands the final hit.
  var sources=new HashSet<uint>{killer.netId};if(participants!=null)foreach(var participant in participants){if(!RiftThreatRules.ActivePlayer(participant)||(participant.transform.position-victim.transform.position).sqrMagnitude>tuning.Participant*tuning.Participant)continue;var owner=RiftClassCombat.Owner(participant);if(owner)sources.Add(owner.netId);}
  var recipients=new HashSet<uint>();foreach(uint source in sources)foreach(uint member in RiftSessionParty.Members(source))recipients.Add(member);
  foreach(uint id in recipients){if(id==killer.netId)continue;var member=RiftSessionParty.Find(id);if(!member||!member.Ready||!member.Alive)continue;var player=member.Player;var actor=player.MyCharacter;var connection=player.connectionToClient;if(connection==null||!NetworkServer.connections.TryGetValue(connection.connectionId,out var current)||current!=connection||connection.identity!=player.netIdentity||actor.gameObject.scene!=victim.gameObject.scene)continue;
   float distance=(actor.transform.position-victim.transform.position).sqrMagnitude;bool near=distance<=tuning.Nearby*tuning.Nearby,participated=participants!=null&&participants.Contains(actor.Health)&&distance<=tuning.Participant*tuning.Participant;if(!near&&!participated)continue;var journal=player.GetComponent<RiftJournalPlayer>();if(journal&&journal.ServerPartyQuestKill(victim.ServerLifeToken,kind,key,encounter))credited++;
  }return credited;
 }
}}
