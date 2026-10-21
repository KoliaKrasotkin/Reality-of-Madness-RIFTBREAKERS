using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay;
using ROM.Horde;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
[InitializeOnLoad] public static class RiftExplorationProbe {
const string Key="ROM.ExplorationProbe";static int step;static double began,next;static RiftHub hub;static CharacterInstance actor;static ROM.Horde.Horde mode;static int gold,originalGold;static List<string> report;
static RiftExplorationProbe(){EditorApplication.playModeStateChanged+=Changed;}
public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/ROM/Scenes/MainMenu.unity");EditorApplication.isPlaying=true;}
static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){step=0;began=EditorApplication.timeSinceStartup;next=began+3;report=new List<string>();EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}}
static void Check(bool condition,string message){if(!condition)throw new Exception(message);report.Add(message+" PASS");}
static void Capture(string name){typeof(GameUIProbe).GetMethod("CaptureGame",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"Documentation/exploration/"+name+".png"});}
static void Tick(){try{
if(EditorApplication.timeSinceStartup-began>180)throw new Exception("Timeout step="+step);if(EditorApplication.timeSinceStartup<next)return;
if(step==0){hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub||!hub.Stage||!hub.Stage.Ready)return;originalGold=RiftExplorationProgress.Gold;hub.Navigate(HubPage.Settings);var choices=hub.GetComponentsInChildren<Dropdown>();Check(choices.Length==9,"Nine video dropdowns");step=10;next=EditorApplication.timeSinceStartup+.5;return;}
if(step==10){hub.GetComponentsInChildren<Dropdown>()[0].Show();step=1;next=EditorApplication.timeSinceStartup+.3;return;}
if(step==1){Check(UnityEngine.Object.FindObjectsByType<Toggle>(FindObjectsSortMode.None).Length>0,"Dropdown opens option list");ROM.UI.EditorTools.SprintUIProbe.Capture(hub,"Documentation/exploration/dropdown.png",1920,1080);foreach(var d in hub.GetComponentsInChildren<Dropdown>())d.Hide();hub.Navigate(HubPage.Home);hub.Creator.BotsToggle.isOn=false;hub.Creator.HostGameButton.onClick.Invoke();step=2;next=EditorApplication.timeSinceStartup+18;return;}
if(step==2){actor=ClientFrontend.OwnedCharacterInstance;mode=ROM.Horde.Horde.Instance;if(!actor||!mode)return;Check(mode.ExplorationMode&&mode.Checkpoints.Length==7&&mode.Encounters.Length==18,"Seven checkpoints and eighteen encounter pockets");Check(mode.IsSafe(actor.transform.position),"Existing player spawn is safe");Check(!GameManager.Players.Any(p=>p.BOT&&p.MyCharacter&&p.MyCharacter.Health.CurrentHealth>0),"No enemies at initial camp after wave delay");actor.SetFppPerspective(true);Check(!actor.FPP,"First person remains disabled");Capture("safe-start");actor.ServerTeleport(mode.Checkpoints[1].Respawn.position,0);step=3;next=EditorApplication.timeSinceStartup+5;return;}
if(step==3){Check(mode.LocalCheckpoint==mode.Checkpoints[1].Title,"Crystal registers checkpoint");var enemy=GameManager.Players.FirstOrDefault(p=>p.BOT&&p.MyCharacter&&p.MyCharacter.Health.CurrentHealth>0);Check(enemy!=null,"Nearby island patrols spawn");Check(!enemy.MyCharacter.GetComponent<RiftEnemyTerritory>().CanTarget(actor.Health),"Patrol ignores protected crystal");int hp=actor.Health.CurrentHealth;actor.Health.Server_ChangeHealthStateRaw(40,0,AttackType.falldamage,enemy.MyCharacter.Health,0);Check(hp==actor.Health.CurrentHealth,"Safe zone blocks enemy damage");Capture("checkpoint");int cleared=mode.Encounters.Sum(e=>e.Defeated),balance=RiftExplorationProgress.Gold;enemy.MyCharacter.Health.Server_ChangeHealthStateRaw(99999,0,AttackType.falldamage,enemy.MyCharacter.Health,0);Check(cleared==mode.Encounters.Sum(e=>e.Defeated)&&balance==RiftExplorationProgress.Gold,"Environmental enemy death gives no progress or rewards");actor.Health.MaxHealth=10000;actor.Health.ServerHeal(10000,actor.Health.DNID);actor.ServerTeleport(mode.Encounters[0].transform.position+Vector3.back*10,0);step=4;next=EditorApplication.timeSinceStartup+5;return;}
if(step==4){gold=RiftExplorationProgress.Gold;var victims=GameManager.Players.Where(p=>p.BOT&&p.MyCharacter&&p.MyCharacter.Health.CurrentHealth>0&&Vector3.Distance(p.MyCharacter.transform.position,mode.Encounters[0].transform.position)<70).ToArray();Check(victims.Length>0,"Exploration combat encounter active");foreach(var p in victims)p.MyCharacter.Health.Server_ChangeHealthStateRaw(99999,0,AttackType.falldamage,actor.Health,0);step=5;next=EditorApplication.timeSinceStartup+1;return;}
if(step==5){Check(RiftExplorationProgress.Gold>gold,"Combat grants local gold");Check(mode.Encounters[0].Cleared,"Encounter completion tracked");actor.ServerTeleport(mode.Encounters[2].transform.position,0);step=6;next=EditorApplication.timeSinceStartup+5;return;}
if(step==6){var boss=GameManager.Players.FirstOrDefault(p=>p.BOT&&p.MyCharacter&&p.MyCharacter.Health.CurrentHealth>0&&p.MyCharacter.Health.MaxHealth>200);Check(boss!=null,"Island mini boss spawns with extra health");Capture("guardian");gold=RiftExplorationProgress.Gold;boss.MyCharacter.Health.Server_ChangeHealthStateRaw(99999,0,AttackType.falldamage,actor.Health,0);step=7;next=EditorApplication.timeSinceStartup+1;return;}
if(step==7){Check(RiftExplorationProgress.Gold>=gold+150,"Guardian reward granted");actor.ServerTeleport(mode.Checkpoints[1].Respawn.position,0);actor.Health.Server_ChangeHealthStateRaw(99999,0,AttackType.falldamage,actor.Health,0);Check(actor.Health.CurrentHealth==0,"Self fall damage remains lethal in safe zone");step=8;next=EditorApplication.timeSinceStartup+1;return;}
if(step==8){mode.PlayerSpawnCharacterRequest(ClientFrontend.ClientPlayerInstance);step=9;next=EditorApplication.timeSinceStartup+2;return;}
if(step==9){actor=ClientFrontend.OwnedCharacterInstance;Check(actor&&actor.Health.CurrentHealth>0&&Vector3.Distance(actor.transform.position,mode.Checkpoints[1].Respawn.position)<10,"Respawn at last crystal without reserve cost");Check(!actor.FPP,"Respawn remains third person");Capture("respawn");Finish(true);}
}catch(Exception e){Debug.LogException(e);report?.Add(e.ToString());Finish(false);}}
static void Finish(bool success){PlayerPrefs.SetInt("ROM.Exploration.Gold."+UserSettings.UserNickname,originalGold);PlayerPrefs.Save();File.WriteAllText("Documentation/exploration/validation.txt",(success?"PASS":"FAIL")+"\n"+string.Join("\n",report));SessionState.SetInt(Key+"Result",success?0:1);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
}
}
