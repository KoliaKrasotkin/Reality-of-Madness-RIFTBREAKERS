using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ROM.Progression;
using ROM.UI.Sprint01;
using MultiFPS.Gameplay;
namespace ROM.EditorTools {
public static class MissionAdaptationValidation {
 static readonly List<string> report=new List<string>();
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);report.Add("PASS "+name);}
 public static void Run(){int code=0;string old=PlayerPrefs.GetString("ROM.Character.ActiveId","");bool had=PlayerPrefs.HasKey("ROM.Character.ActiveId");string id="__mission_validation_"+Guid.NewGuid().ToString("N");
 try{
  var catalog=RiftJournalCatalog.Load;var damage=catalog.Quest("rift-vanguard");var healing=catalog.Quest("rift-field-medic");
  Check(catalog.Quests.Length>=37&&catalog.Quests.Select(q=>q.Id).Distinct().Count()==catalog.Quests.Length,"Existing quest catalog preserved and two unique RPG assignments appended");
  Check(damage&&healing&&damage.RewardExperience==100&&healing.RewardExperience==80,"Authored damage/healing quests have explicit XP rewards");
  Check((int)RiftProgressEvent.Collect==9&&(int)RiftProgressEvent.DamageDealt==10&&(int)RiftProgressEvent.HealthRestored==11,"Saved event numbers preserved");
  var a=new JournalState{CharacterId=id};var b=new JournalState{CharacterId=id+"B"};
  Check(!a.Accept(damage),"Prerequisite enforced");a.Quests.Add(new QuestRecord{Id="refuge-training",Status=QuestStatus.TurnedIn});
  a.Record(catalog,RiftProgressEvent.DamageDealt,"",300);Check(a.Accept(damage)&&a.Quest(damage.Id).Progress[0]==0,"Damage before acceptance gives no quest credit");
  Check(!a.Accept(damage),"Cannot accept twice");a.Record(catalog,RiftProgressEvent.DamageDealt,"",0);a.Record(catalog,RiftProgressEvent.DamageDealt,"",-5);Check(a.Quest(damage.Id).Progress[0]==0,"Zero and negative amounts cannot manufacture progress");
  a.Record(catalog,RiftProgressEvent.HealthRestored,"",100);Check(a.Quest(damage.Id).Progress[0]==0,"Typed objectives do not cross-credit");
  a.Record(catalog,RiftProgressEvent.DamageDealt,"",299);Check(!a.TurnIn(damage),"Incomplete quest cannot turn in");
  a.Record(catalog,RiftProgressEvent.DamageDealt,"",100);Check(a.Quest(damage.Id).Progress[0]==300&&a.Quest(damage.Id).Status==QuestStatus.Ready,"Progress caps at authored target");
  Check(a.TurnIn(damage)&&!a.TurnIn(damage)&&!a.Accept(damage),"Completed assignment is not repeatable");Check(b.Quests.Count==0,"Other character remains independent");
  var reload=JsonUtility.FromJson<JournalState>(JsonUtility.ToJson(a));Check(reload.CharacterId==id&&reload.TurnedIn(damage.Id),"Identity and completion survive serialization");
  PlayerPrefs.SetString("ROM.Character.ActiveId",id);PlayerPrefs.SetInt(RiftCharacterWallet.GoldKey,10);
  a.Quest(damage.Id).RewardLedgerVersion=2;a.Quest(damage.Id).GoldRewardCommitted=65;
  RiftQuestRewardBridge.Apply(a);Check(RiftExplorationProgress.Gold==75,"Remote committed receipt credits selected character wallet");
  RiftQuestRewardBridge.Apply(a);RiftQuestRewardBridge.Apply(JsonUtility.FromJson<JournalState>(JsonUtility.ToJson(a)));Check(RiftExplorationProgress.Gold==75,"Duplicate and replayed snapshots cannot pay twice");
  var legacy=new JournalState{CharacterId=id};legacy.Quests.Add(new QuestRecord{Id="legacy",Status=QuestStatus.TurnedIn,RewardLedgerVersion=1,GoldRewardCommitted=100});RiftQuestRewardBridge.Apply(legacy);Check(RiftExplorationProgress.Gold==75,"Already-paid host and legacy quests are not paid again");
  PlayerPrefs.SetString("ROM.Character.ActiveId",id+"B");RiftQuestRewardBridge.Apply(a);Check(RiftExplorationProgress.Gold==0,"Snapshot for another character cannot credit active wallet");
  string receipt=RiftQuestRewardBridge.Receipt(id,"interrupted");PlayerPrefs.SetString("ROM.QuestReward.Pending.v1",JsonUtility.ToJson(new RiftQuestRewardBridge.Payment{Character=id,Receipt=receipt,Gold=90}));PlayerPrefs.Save();RiftQuestRewardBridge.Recover();RiftQuestRewardBridge.Recover();Check(PlayerPrefs.GetInt("ROM.CharacterWallet.v1."+id+".Gold")==90&&PlayerPrefs.GetInt(receipt)==1,"Interrupted wallet assignment recovers once to original character");
  Check(File.Exists("Documentation/TerrainRecovery/Assets/Terrain.asset")&&new FileInfo("Documentation/TerrainRecovery/Assets/Terrain.asset").Length>30000000,"Terrain recovery backup retained");
 }catch(Exception e){code=1;report.Add("FAIL "+e);Debug.LogException(e);}
 finally{foreach(string suffix in new[]{".Gold","B.Gold"})PlayerPrefs.DeleteKey("ROM.CharacterWallet.v1."+id+suffix);foreach(string quest in new[]{"rift-vanguard","interrupted"})PlayerPrefs.DeleteKey(RiftQuestRewardBridge.Receipt(id,quest));if(had)PlayerPrefs.SetString("ROM.Character.ActiveId",old);else PlayerPrefs.DeleteKey("ROM.Character.ActiveId");PlayerPrefs.Save();Directory.CreateDirectory("Documentation/quest-system");File.WriteAllLines("Documentation/quest-system/mission-adaptation-validation.txt",report);Debug.Log(string.Join("\n",report));EditorApplication.Exit(code);}
 }
}
}
