using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Mirror;
using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay;
using ROM.UI.Sprint01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace ROM.EditorTools {
[InitializeOnLoad] public static class RiftClassProbe {
const string Key="ROM.ClassProbe",Folder="Documentation/classes";static double began,next;static int step,originalClass;static List<string> report;static PlayerInstance player;static Health enemy;static ROM.Horde.Horde mode;static readonly List<PlayerInstance> fixtures=new List<PlayerInstance>();
static RiftClassProbe(){EditorApplication.playModeStateChanged+=Changed;}
public static void Run(){Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);EditorApplication.isPlaying=true;}
static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){step=0;began=EditorApplication.timeSinceStartup;next=began+3;report=new List<string>();originalClass=PlayerPrefs.GetInt(PlayerInstance.ClassPreference,0);EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}}
static void Assert(bool value,string name){if(!value)throw new Exception(name);report.Add(name+" PASS");}
static void Capture(string name){typeof(GameUIProbe).GetMethod("CaptureGame",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Folder+"/"+name+".png"});}
static void Tick(){try{
if(EditorApplication.timeSinceStartup-began>150)throw new Exception("Class probe timeout step="+step);if(EditorApplication.timeSinceStartup<next)return;
foreach(var bot in UnityEngine.Object.FindObjectsByType<BotAI>(FindObjectsSortMode.None)){bot.Passive=true;}
if(step==0){var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub||!hub.Stage||!hub.Stage.Ready)return;hub.Navigate(HubPage.Loadout);step=8;next=EditorApplication.timeSinceStartup+.7;return;}
if(step==8){var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();ROM.UI.EditorTools.SprintUIProbe.Capture(hub,Folder+"/selection.png",1920,1080);Assert(RiftClassRules.Load().Classes.Length==4,"Four independent class definitions");hub.Creator.BotsToggle.isOn=false;hub.Creator.HostGameButton.onClick.Invoke();step=1;next=EditorApplication.timeSinceStartup+14;return;}
if(step==1){player=ClientFrontend.ClientPlayerInstance;mode=ROM.Horde.Horde.Instance;if(!player||!player.MyCharacter)return;foreach(var input in UnityEngine.Object.FindObjectsByType<PlayerGameplayInput>(FindObjectsSortMode.None))input.enabled=false;Assert(player.MyCharacter.Health.MaxHealth==player.ClassDefinition.Health,"Selected class applies at spawn");player.MyCharacter.ServerTeleport(mode.Encounters[0].transform.position,0);step=2;next=EditorApplication.timeSinceStartup+5;return;}
if(step==2){
var actor=player.MyCharacter;enemy=GameManager.Players.Where(p=>p.BOT&&p.MyCharacter&&p.MyCharacter.Health.CurrentHealth>0).Select(p=>p.MyCharacter.Health).FirstOrDefault();if(!enemy)return;
foreach(var p in GameManager.Players.Where(p=>p.BOT&&p.MyCharacter&&p.MyCharacter.Health!=enemy))p.MyCharacter.ServerTeleport(mode.Checkpoints[0].Respawn.position+Vector3.up*20,0);
Vector3 origin=mode.Encounters[0].SpawnPoints[0].position;Vector3 target=origin;bool found=false;
for(int n=0;n<24;n++){var offset=Quaternion.Euler(0,n*15,0)*Vector3.forward*6;if(!NavMesh.SamplePosition(origin+offset,out var hit,2,NavMesh.AllAreas))continue;var delta=hit.position-origin;if(!actor.gameObject.scene.GetPhysicsScene().Raycast(origin+Vector3.up*1.3f,delta.normalized,out _,delta.magnitude,GameManager.environmentLayer,QueryTriggerInteraction.Ignore)){target=hit.position;found=true;break;}}
Assert(found,"Spell test has unobstructed target");enemy.GetComponent<CharacterInstance>().ServerTeleport(target,0);enemy.ServerSetVitals(10000,10000);((Dictionary<Health,int>)typeof(ROM.Horde.Horde).GetField("_classEnemyBase",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(mode))[enemy]=10000;
int skin=player.PlayerInfo.CharacterSkinID;
for(int id=0;id<4;id++){
actor.ServerTeleport(mode.Checkpoints[0].Respawn.position,0);if(player.ClassId!=id)Assert(player.ServerChooseClass(id),"Safe class selection "+id);else player.ServerConfigureClass();
Assert(player.MyCharacter.Health.MaxHealth==player.ClassDefinition.Health,"Class health "+id);Assert(player.PlayerInfo.CharacterSkinID==skin,"Skin independent "+id);
actor.ServerTeleport(origin,0);actor.Input.LookY=Quaternion.LookRotation(target-origin).eulerAngles.y;actor.Input.LookX=0;actor.transform.rotation=Quaternion.Euler(0,actor.Input.LookY,0);
player.FirstReadyAt=player.SecondReadyAt=0;enemy.VulnerableUntil=enemy.WeakenedUntil=0;int hp=enemy.CurrentHealth;enemy.Server_ChangeHealthStateRaw(20,0,AttackType.hitscan,actor.Health,0);Assert(enemy.CurrentHealth<hp,"Conventional damage pipeline "+id);
actor.Health.ServerSetVitals(player.ClassDefinition.Health,player.ClassDefinition.Health/2);int before=actor.Health.CurrentHealth;
Assert(player.ServerUseClassAbility(0),"First ability "+id);Assert(!player.ServerUseClassAbility(0),"Cooldown enforced "+id);Assert(player.ServerUseClassAbility(1),"Second ability "+id);Assert(actor.Health.CurrentHealth>before,"Solo active sustain "+id);
Assert(!player.ServerChooseClass((id+1)%4),"No class change in combat "+id);
if(id==3){Assert(!RiftClassCombat.CanTarget(actor.Health,origin+Vector3.right*15),"Stealth drops distant aggro");Assert(RiftClassCombat.CanTarget(actor.Health,origin+Vector3.right*2),"Nearby enemies reveal stealth");}
}
Assert(!player.ServerUseClassAbility(99),"Invalid ability rejected");
actor.ServerTeleport(mode.Checkpoints[0].Respawn.position,0);player.ServerChooseClass(1);actor.ServerTeleport(origin,0);player.FirstReadyAt=0;actor.Health.ServerSetVitals(180,90);Assert(player.ServerUseClassAbility(0)&&actor.Health.CurrentHealth>90,"Tank with DPS or solo keeps self sustain");
for(int i=0;i<3;i++)CreateAlly(i==0?0:i==1?2:3,origin+Vector3.right*(3+i*2),10001u+(uint)i);
step=3;next=EditorApplication.timeSinceStartup+1;return;}
if(step==3){
Assert(RiftClassCombat.SupportNear(player),"Nearby support detected");var actor=player.MyCharacter;actor.Health.ServerSetVitals(180,90);player.FirstReadyAt=0;Assert(player.ServerUseClassAbility(0),"Supported guard activates");Assert(actor.Health.CurrentHealth==90,"Supported tank redirects instead of extra self heal");
var support=fixtures[0];support.MyCharacter.Health.ServerSetVitals(130,130);int tankHp=actor.Health.CurrentHealth;support.MyCharacter.Health.Server_ChangeHealthStateRaw(40,0,AttackType.hitscan,enemy,0);Assert(actor.Health.CurrentHealth<tankHp,"Guard intercepts ally damage");
int healed=RiftClassCombat.Heal(support,actor.Health,.2f);Assert(healed>=44,"Paladin tank healing synergy");
Assert(RiftClassCombat.TargetPriority(actor.Health)<1,"Guard threat priority");
var rules=mode.ClassRules;var group=mode.Encounters[0];Assert(group.Participants.Count>=4,"Four nearby server participants registered");Assert(enemy.MaxHealth==Mathf.RoundToInt(10000*rules.HealthScale(4)),"Encounter health scales for four players");Assert(Mathf.Abs(enemy.CombatDamageMultiplier-rules.DamageScale(4))<.001f,"Encounter damage scales for four players");
for(int n=1;n<=4;n++)Assert(rules.HealthScale(n)>0&&rules.DamageScale(n)<=1.5f,"Bounded party scaling "+n);
foreach(var ally in fixtures)ally.MyCharacter.Health.CurrentHealth=0;
step=4;next=EditorApplication.timeSinceStartup+.7;return;}
if(step==4){Assert(enemy.MaxHealth==Mathf.RoundToInt(10000*mode.ClassRules.HealthScale(4)),"Ally deaths do not shrink active encounter");player.LastCombatAt=NetworkTime.time-20;var actor=player.MyCharacter;actor.Health.ServerSetVitals(180,90);player.ServerTickClass();step=5;next=EditorApplication.timeSinceStartup+1.2;return;}
if(step==5){Assert(player.MyCharacter.Health.CurrentHealth>90,"Out of combat recovery");foreach(var ally in fixtures){GameManager.RemovePlayerInstance(ally);NetworkServer.Destroy(ally.MyCharacter.gameObject);UnityEngine.Object.Destroy(ally.gameObject);}ClientFrontend.SetObservedCharacter(player.MyCharacter);Capture("class-hud");UIPauseMenu._instance.ShowPauseMenu(true);step=6;next=EditorApplication.timeSinceStartup+.5;return;}
if(step==6){UnityEngine.Object.FindFirstObjectByType<RiftPauseAdapter>().Hub.ShowClasses();step=7;next=EditorApplication.timeSinceStartup+.4;return;}
if(step==7){Capture("pause-class");Finish(true);}
}catch(Exception e){Debug.LogException(e);report?.Add(e.ToString());Finish(false);}}
static void CreateAlly(int id,Vector3 position,uint netId){
var go=new GameObject("Class QA ally",typeof(NetworkIdentity));typeof(NetworkIdentity).GetProperty("netId").SetValue(go.GetComponent<NetworkIdentity>(),netId);
var ally=go.AddComponent<PlayerInstance>();ally.enabled=false;typeof(NetworkIdentity).GetMethod("InitializeNetworkBehaviours",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(go.GetComponent<NetworkIdentity>(),null);ally.ClassId=id;ally.PlayerInfo=new ClientData{Username="QA Ally",ClassId=id,CharacterSkinID=player.PlayerInfo.CharacterSkinID};typeof(PlayerInstance).GetField("<Team>k__BackingField",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(ally,0);
var character=UnityEngine.Object.Instantiate(NetworkManager.singleton.spawnPrefabs[0],position,Quaternion.identity).GetComponent<CharacterInstance>();NetworkServer.Spawn(character.gameObject);character.GetComponent<CharacterMotor>().enabled=false;character.Health.Team=0;ally.MyCharacter=character;GameManager.AddPlayerInstance(ally);character.Health.ServerSetVitals(ally.ClassDefinition.Health,ally.ClassDefinition.Health);fixtures.Add(ally);
}
static void Finish(bool success){PlayerPrefs.SetInt(PlayerInstance.ClassPreference,originalClass);PlayerPrefs.Save();File.WriteAllText(Folder+"/validation.txt",(success?"PASS":"FAIL")+"\n"+string.Join("\n",report));SessionState.SetInt(Key+"Result",success?0:1);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
}}
