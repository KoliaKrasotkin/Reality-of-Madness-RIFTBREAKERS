using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.UI;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
[InitializeOnLoad] public static class MissionAdaptationPlayProbe {
 const string Key="ROM.MissionAdaptationProbe",Folder="Documentation/quest-system",Account="__mission-adaptation-smoke";
 sealed class ReceiptConnection:NetworkConnectionToClient {public ReceiptConnection():base(424242){}protected override void SendToTransport(ArraySegment<byte> segment,int channelId=Channels.Reliable){}}
 static ROM.Progression.RiftJournalPlayer journal;static int rewardXP,rewardGold,relogGold;static string storage;static Health victim;static ISocialService social; static int step,skin,oldSocial;static double began,next;static RiftHub hub;static LocalCharacterRoster roster;static List<string> report;static bool finished;
 static readonly string[] Prefs={AbilityController.Preference,PlayerInstance.ClassPreference,"_playerPrefs_CharacterSkinID"};
 static MissionAdaptationPlayProbe(){EditorApplication.playModeStateChanged+=Changed;}
 public static void Run(){Directory.CreateDirectory(Folder);var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-playableSkin");SessionState.SetInt(Key+"SkinUnderTest",at>=0?int.Parse(args[at+1]):3);foreach(var pref in Prefs){SessionState.SetBool(Key+pref+"Exists",PlayerPrefs.HasKey(pref));SessionState.SetInt(Key+pref,PlayerPrefs.GetInt(pref,0));}SessionState.SetString(Key+"User",PlayerPrefs.GetString("_playerPrefs_Username",""));SessionState.SetString(Key+"Active",PlayerPrefs.GetString("ROM.Character.ActiveId",""));SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/ROM/Scenes/MainMenu.unity");EditorApplication.isPlaying=true;}
 static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){skin=SessionState.GetInt(Key+"SkinUnderTest",3);step=0;began=EditorApplication.timeSinceStartup;next=began+4;report=new List<string>();finished=false;EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){foreach(var pref in Prefs){if(SessionState.GetBool(Key+pref+"Exists",false))PlayerPrefs.SetInt(pref,SessionState.GetInt(Key+pref,0));else PlayerPrefs.DeleteKey(pref);}UserSettings.CharacterSkinID=SessionState.GetInt(Key+"_playerPrefs_CharacterSkinID",1);UserSettings.UserNickname=SessionState.GetString(Key+"User","");PlayerPrefs.SetString("_playerPrefs_Username",UserSettings.UserNickname);PlayerPrefs.SetString("ROM.Character.ActiveId",SessionState.GetString(Key+"Active",""));PlayerPrefs.DeleteKey("ROM.CharacterRoster.v1."+Account);PlayerPrefs.Save();SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);report.Add("PASS "+message);Debug.Log(message);}
 static void Advance(double delay=1){step++;next=EditorApplication.timeSinceStartup+delay;}
 static void Tick(){try{if(storage==null){storage=Path.Combine(Path.GetTempPath(),"rift-mission-play-"+Guid.NewGuid().ToString("N"));ROM.Progression.RiftJournalPlayer.StorageOverride=storage;}if(finished)return;double now=EditorApplication.timeSinceStartup;if(now-began>240)throw new Exception("Timeout step "+step);if(now<next)return;
 if(step==0){hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub||!hub.FrontEnd)return;social=hub.Social;oldSocial=social.Self.Skin;roster=new LocalCharacterRoster(Account,"test",1,"warrior");typeof(RiftHub).GetField("roster",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hub,roster);typeof(RiftHub).GetField("rosterAccount",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hub,hub.Social.Self.Id);hub.FrontEndAction("Create");Advance();return;}
 if(step==1){Check(hub.FrontEnd.Creation.activeInHierarchy,"Authored creation screen opens");var tab=skin==3?hub.FrontEnd.GetComponentsInChildren<Button>().First(b=>b.name=="Mage class"):hub.FrontEnd.Paladin;tab.onClick.Invoke();Advance();return;}
 if(step==2){Check(hub.FrontEnd.CreationName.text==RiftSkinIdentity.Name(skin),"Correct class preview identity");Check(hub.FrontEnd.GetComponentsInChildren<NetworkIdentity>(true).Length==0,"Menu previews have no network identities");hub.FrontEnd.Confirm.onClick.Invoke();Advance();return;}
 if(step==3){Check(roster.Active.Skin==skin&&roster.Active.Available,"UI creates/selects playable character");Check(hub.FrontEnd.Selection.activeInHierarchy,"Selection returns after creation");hub.FrontEnd.Continue.onClick.Invoke();Advance();return;}
 if(step==4){var confirm=hub.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.GetComponentInChildren<Text>()?.text=="CONFIRMAR");if(!confirm){report.Add("buttons "+string.Join(" | ",hub.GetComponentsInChildren<Button>().Select(b=>b.GetComponentInChildren<Text>()?.text)));throw new Exception("Host confirmation button missing");}confirm.onClick.Invoke();Advance(15);return;}
 if(step==14){hub=UnityEngine.Object.FindObjectsByType<RiftHub>(FindObjectsSortMode.None).FirstOrDefault(h=>!h.InGame&&h.FrontEnd);if(!hub)return;typeof(RiftHub).GetField("roster",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hub,roster);typeof(RiftHub).GetField("rosterAccount",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hub,hub.Social.Self.Id);hub.ApplyActiveCharacter();hub.FrontEndAction("Continue");Advance();return;}
 if(step==15){var confirm=hub.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.GetComponentInChildren<Text>()?.text=="CONFIRMAR");if(!confirm)return;confirm.onClick.Invoke();Advance(15);return;}
 var actor=ClientFrontend.OwnedCharacterInstance;var player=ClientFrontend.ClientPlayerInstance;if(!actor||!player)return;var motor=actor.GetComponent<WarriorAvatar>();var combat=player.GetComponent<AbilityController>();
 switch(step){
 case 5:
  journal=player.GetComponent<ROM.Progression.RiftJournalPlayer>();if(!journal||journal.State==null)return;
  Check(actor.netId!=0&&actor.isOwned&&journal.State.CharacterId==roster.Active.Id,"Real Mirror host owns selected mage and character journal");
  Check(journal.Catalog.Quests.Length>=37,"Runtime uses full existing catalog with RPG assignments");
  journal.State.Quests.Add(new ROM.Progression.QuestRecord{Id="refuge-training",Status=ROM.Progression.QuestStatus.TurnedIn});
  var bram=UnityEngine.Object.FindObjectsByType<ROM.Horde.RiftVillageNpc>(FindObjectsSortMode.None).First(n=>n.Id=="bram");
  actor.ServerTeleport(bram.transform.position+Vector3.forward*20,0);Check(!journal.ServerAccept("rift-vanguard","bram"),"Server rejects acceptance outside NPC radius");
  actor.ServerTeleport(bram.transform.position+Vector3.forward*2,0);journal.CmdAccept("rift-vanguard","bram");Advance();break;
 case 6:
  Check(journal.State.Quest("rift-vanguard")?.Status==ROM.Progression.QuestStatus.Active,"Accept command processed by server");
  Check(ROM.Progression.RiftJournalView.Read().Quest("rift-vanguard")?.Status==ROM.Progression.QuestStatus.Active,"Target snapshot reaches owned client journal");
  var alma=UnityEngine.Object.FindObjectsByType<ROM.Horde.RiftVillageNpc>(FindObjectsSortMode.None).First(n=>n.Id=="alma");actor.ServerTeleport(alma.transform.position+Vector3.forward*2,0);journal.CmdAccept("rift-field-medic","alma");Advance();break;
 case 7:
  Check(journal.State.Quest("rift-field-medic")!=null,"Healing assignment accepted separately");
  var world=ROM.Horde.Horde.Instance;world.enabled=false;var population=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.RiftEnemyPopulation>();if(population)population.enabled=false;
  var point=world.Encounters.First(e=>e&&e.SpawnPoints.Any(p=>p&&!world.IsSafe(p.position))).SpawnPoints.First(p=>p&&!world.IsSafe(p.position));actor.ServerTeleport(point.position,0);Advance();break;
 case 8:
  victim=(Health)typeof(RiftJournalProbe).GetMethod("SpawnTraining",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{actor,actor.transform.position+Vector3.right*3});Advance();break;
 case 9:
  victim.ServerSetVitals(1000,125);victim.Server_ChangeHealthStateRaw(20,0,AttackType.hitscan,actor.Health,0);
  Check(journal.State.Quest("rift-vanguard").Progress[0]==0,"Training damage excluded");
  victim.TrainingTarget=false;victim.Team=actor.Health.Team;victim.Server_ChangeHealthStateRaw(20,0,AttackType.hitscan,actor.Health,0);
  Check(journal.State.Quest("rift-vanguard").Progress[0]==0,"Allied damage excluded");
  victim.Team=(byte)(actor.Health.Team==0?1:0);victim.ServerSetVitals(1000,125);victim.Server_ChangeHealthStateRaw(100000,0,AttackType.hitscan,actor.Health,0);
  Check(journal.State.Quest("rift-vanguard").Progress[0]==125,"Overkill credits exactly remaining enemy health");
  victim.Server_ChangeHealthStateRaw(100000,0,AttackType.hitscan,actor.Health,0);Check(journal.State.Quest("rift-vanguard").Progress[0]==125,"Dead target cannot generate damage progress");victim=(Health)typeof(RiftJournalProbe).GetMethod("SpawnTraining",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{actor,actor.transform.position+Vector3.right*4});Advance();break;
 case 10:
  victim.TrainingTarget=false;victim.Team=(byte)(actor.Health.Team==0?1:0);victim.ServerSetVitals(1000,175);victim.Server_ChangeHealthStateRaw(100000,0,AttackType.hitscan,actor.Health,0);
  Check(journal.State.Quest("rift-vanguard").Status==ROM.Progression.QuestStatus.Ready,"Real health events complete accepted damage objective");
  actor.Health.ServerSetVitals(100,50);actor.Health.ServerHeal(100,actor.Health);
  Check(journal.State.Quest("rift-field-medic").Progress[0]==50,"Only effective healing counted, not overheal");
  actor.Health.ServerSetVitals(100,100);actor.Health.ServerHeal(100,actor.Health);Check(journal.State.Quest("rift-field-medic").Progress[0]==50,"Full-health healing gives no objective progress");
  actor.Health.ServerSetVitals(100,50);actor.Health.ServerHeal(100,actor.Health);Check(journal.State.Quest("rift-field-medic").Status==ROM.Progression.QuestStatus.Ready,"Actual healing completes assignment");
  rewardXP=journal.State.Experience;rewardGold=RiftExplorationProgress.Gold;
  var turnNpc=UnityEngine.Object.FindObjectsByType<ROM.Horde.RiftVillageNpc>(FindObjectsSortMode.None).First(n=>n.Id=="bram");actor.ServerTeleport(turnNpc.transform.position+Vector3.forward*2,0);journal.CmdTurnIn("rift-vanguard","bram");Advance();break;
 case 11:
  Check(journal.State.TurnedIn("rift-vanguard")&&journal.State.Experience==rewardXP+100&&RiftExplorationProgress.Gold==rewardGold+65,"Turn-in command commits authored XP and gold once");
  journal.CmdTurnIn("rift-vanguard","bram");Advance();break;
 case 12:
  Check(journal.State.Experience==rewardXP+100&&RiftExplorationProgress.Gold==rewardGold+65,"Repeated network turn-in does not duplicate rewards");
  typeof(ROM.Progression.RiftJournalPlayer).GetProperty("State").SetValue(journal,null);typeof(ROM.Progression.RiftJournalPlayer).GetMethod("Ensure",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(journal,null);
  Check(journal.State.TurnedIn("rift-vanguard")&&journal.State.Quest("rift-field-medic").Status==ROM.Progression.QuestStatus.Ready,"Server reload restores delivered and ready quests from disk");
  Check(journal.State.Experience==rewardXP+100,"Reload retains quest XP");
  string originalId=player.PlayerInfo.CharacterId;var info=player.PlayerInfo;info.CharacterId=originalId+"other";player.PlayerInfo=info;typeof(ROM.Progression.RiftJournalPlayer).GetProperty("State").SetValue(journal,null);typeof(ROM.Progression.RiftJournalPlayer).GetMethod("Ensure",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(journal,null);
  Check(journal.State.CharacterId==originalId+"other"&&journal.State.Quests.Count==0,"Server loads separate empty journal for second character");info.CharacterId=originalId;player.PlayerInfo=info;typeof(ROM.Progression.RiftJournalPlayer).GetProperty("State").SetValue(journal,null);typeof(ROM.Progression.RiftJournalPlayer).GetMethod("Ensure",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(journal,null);Check(journal.State.TurnedIn("rift-vanguard"),"Returning character reloads its own completion");
  Advance();break;
 case 13:
  // Synthetic non-local connection exercises remote authority/persistence; this is not a remote socket test.
  var healerNpc=UnityEngine.Object.FindObjectsByType<ROM.Horde.RiftVillageNpc>(FindObjectsSortMode.None).First(n=>n.Id=="alma");actor.ServerTeleport(healerNpc.transform.position+Vector3.forward*2,0);
  var ownerConnection=player.connectionToClient;int hostGold=RiftExplorationProgress.Gold;int xpBeforeRemote=journal.State.Experience;
  try{typeof(NetworkIdentity).GetProperty("connectionToClient").SetValue(player.netIdentity,new ReceiptConnection());
   Check(journal.ServerTurnIn("rift-field-medic","alma"),"Server accepts remote-path turn-in with persisted character receipt (synthetic connection)");
   Check(RiftExplorationProgress.Gold==hostGold&&journal.State.Experience==xpBeforeRemote+80,"Remote path grants server XP without modifying host wallet");
   Check(journal.State.Quest("rift-field-medic").RewardLedgerVersion==2,"Remote reward receipt version persisted");
  }finally{typeof(NetworkIdentity).GetProperty("connectionToClient").SetValue(player.netIdentity,ownerConnection);}
  typeof(ROM.Progression.RiftJournalPlayer).GetProperty("State").SetValue(journal,null);typeof(ROM.Progression.RiftJournalPlayer).GetMethod("Ensure",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(journal,null);
  Check(journal.State.TurnedIn("rift-field-medic")&&journal.State.Quest("rift-field-medic").GoldRewardCommitted==45,"Remote-path receipt survives server journal reload");
  string snapshot=JsonUtility.ToJson(journal.State);ROM.Progression.RiftJournalView.ApplyServerSnapshot(snapshot,"");ROM.Progression.RiftJournalView.ApplyServerSnapshot(snapshot,"");Check(RiftExplorationProgress.Gold==hostGold+45,"Client redeems replayed receipt exactly once");
  relogGold=RiftExplorationProgress.Gold;NetworkManager.singleton.StopHost();Advance(5);break;
 case 16:
  journal=player.GetComponent<ROM.Progression.RiftJournalPlayer>();if(!journal||journal.State==null)return;
  Check(journal.State.CharacterId==roster.Active.Id&&journal.State.TurnedIn("rift-vanguard")&&journal.State.TurnedIn("rift-field-medic"),"Full host logout and menu re-entry restore both delivered quests");
  Check(journal.State.Experience==rewardXP+180&&RiftExplorationProgress.Gold==relogGold,"Host reconnect restores XP and does not repeat receipt rewards");
  report.Add("LIMIT: real host disconnect/reconnect plus synthetic non-local server branch; no separate remote client process.");Finish(true);break;
 }

 }catch(Exception e){report?.Add("FAIL "+e);Debug.LogException(e);Finish(false);}}
 static void Finish(bool ok){if(finished)return;finished=true;if(roster!=null)foreach(var testCharacter in roster.Characters){string id=testCharacter.Id;PlayerPrefs.DeleteKey(ROM.Progression.RiftJournalView.Key(id));PlayerPrefs.DeleteKey("ROM.CharacterWallet.v1."+id+".Gold");PlayerPrefs.DeleteKey("ROM.CharacterWallet.v1."+id+".Potion.0");PlayerPrefs.DeleteKey("ROM.CharacterWallet.v1."+id+".Potion.1");PlayerPrefs.DeleteKey(ROM.Progression.RiftQuestRewardBridge.Receipt(id,"rift-field-medic"));PlayerPrefs.DeleteKey("ROM.Loot.GoldCredited.v1."+id);}if(social!=null)social.SetAppearance(oldSocial);ROM.Progression.RiftJournalPlayer.StorageOverride=null;File.WriteAllLines(Folder+"/mission-adaptation-host.txt",report??new List<string>());SessionState.SetInt(Key+"Result",ok?0:1);EditorApplication.update-=Tick;if(NetworkManager.singleton&&NetworkServer.active)NetworkManager.singleton.StopHost();EditorApplication.isPlaying=false;}
}
}
