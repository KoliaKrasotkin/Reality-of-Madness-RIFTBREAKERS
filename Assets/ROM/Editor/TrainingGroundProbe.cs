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
using UnityEngine.UI;
namespace ROM.EditorTools {
[InitializeOnLoad] public static class TrainingGroundProbe {
const string Key="ROM.TrainingProbe",Folder="Documentation/training";static int step,originalClass;static double began,next;static List<string> report;static PlayerInstance player;static Health dummy;static int gold;
static TrainingGroundProbe(){EditorApplication.playModeStateChanged+=Changed;}
public static void Run(){Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);EditorApplication.isPlaying=true;}
static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){step=0;began=EditorApplication.timeSinceStartup;next=began+3;report=new List<string>();originalClass=PlayerPrefs.GetInt(PlayerInstance.ClassPreference,0);EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}}
static void Assert(bool value,string name){if(!value)throw new Exception(name);report.Add(name+" PASS");}
static void Capture(string name){typeof(GameUIProbe).GetMethod("CaptureGame",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Folder+"/"+name+".png"});}
static void Tick(){try{
if(EditorApplication.timeSinceStartup-began>130)throw new Exception("Training timeout step="+step);if(EditorApplication.timeSinceStartup<next)return;
if(step==0){var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub||!hub.Stage||!hub.Stage.Ready)return;hub.ToggleHomePanel("Class");var card=hub.Design.Find("Screen").GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="ChooseClass");Assert(card,"Class selection directly on Home");for(int id=0;id<4;id++){var button=hub.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()&&b.GetComponentInChildren<Text>().text==RiftClassRules.Load().Classes[id].Name);button.onClick.Invoke();Assert(PlayerPrefs.GetInt(PlayerInstance.ClassPreference)==id,"Home class button persists "+id);}PlayerPrefs.SetInt(PlayerInstance.ClassPreference,0);hub.ToggleHomePanel("Class");ROM.UI.EditorTools.SprintUIProbe.Capture(hub,Folder+"/main-menu.png",1920,1080);hub.Creator.BotsToggle.isOn=false;hub.Creator.HostGameButton.onClick.Invoke();step=1;next=EditorApplication.timeSinceStartup+12;return;}
if(step==1){player=ClientFrontend.ClientPlayerInstance;if(!player||!player.MyCharacter)return;dummy=GameManager.Players.Where(p=>p.BOT&&p.MyCharacter&&p.MyCharacter.Health.TrainingTarget).Select(p=>p.MyCharacter.Health).FirstOrDefault();if(!dummy)return;var mode=ROM.Horde.Horde.Instance;Assert(mode.IsSafe(player.MyCharacter.transform.position),"Initial spawn remains safe");Assert(GameManager.Players.Where(p=>p.BOT&&p.MyCharacter).All(p=>p.MyCharacter.Health.TrainingTarget),"Only passive training target at start");Assert(dummy.GetComponent<BotAI>().Passive,"Dummy does not attack");Assert(UnityEngine.Object.FindFirstObjectByType<RiftClassHUD>().GetComponent<Canvas>().isRootCanvas,"Independent ability overlay exists");foreach(var input in UnityEngine.Object.FindObjectsByType<PlayerGameplayInput>(FindObjectsSortMode.None))input.enabled=false;var point=dummy.transform.position+(player.MyCharacter.transform.position-dummy.transform.position).normalized*5;player.MyCharacter.ServerTeleport(point,Quaternion.LookRotation(dummy.transform.position-point).eulerAngles.y);gold=RiftExplorationProgress.Gold;step=2;next=EditorApplication.timeSinceStartup+1;return;}
if(step==2){var actor=player.MyCharacter;var mode=ROM.Horde.Horde.Instance;Assert(mode.CanUseClassAbilities(actor.transform.position),"Abilities enabled in safe training area");for(int id=0;id<4;id++){if(player.ClassId!=id)Assert(player.ServerChooseClass(id),"Safe class switch "+id);player.FirstReadyAt=player.SecondReadyAt=0;actor.Input.LookY=Quaternion.LookRotation(dummy.transform.position-actor.transform.position).eulerAngles.y;actor.Input.LookX=0;int before=dummy.TrainingTotalDamage;Assert(player.ServerUseClassAbility(0),"Training first ability "+id);Assert(!player.ServerUseClassAbility(0),"Training cooldown enforced "+id);if(!player.ServerUseClassAbility(1)){Capture("failed-aim");Vector3 delta=dummy.transform.position+Vector3.up-actor.transform.position-Vector3.up*1.3f;bool blocked=actor.gameObject.scene.GetPhysicsScene().Raycast(actor.transform.position+Vector3.up*1.3f,delta.normalized,out var obstacle,delta.magnitude,GameManager.environmentLayer,QueryTriggerInteraction.Ignore);throw new Exception("Second ability "+id+" actor="+actor.transform.position+" dummy="+dummy.transform.position+" obstruction="+(blocked?obstacle.collider.name:"none"));}report.Add("Training second ability "+id+" PASS");Assert(dummy.TrainingTotalDamage>before,"Skill damage counted "+id);}
int total=dummy.TrainingTotalDamage;dummy.Server_ChangeHealthState(30,0,AttackType.hitscan,actor.Health,0);Assert(dummy.TrainingTotalDamage>total,"Weapon damage accepted in safe zone");Assert(dummy.CurrentHealth==dummy.MaxHealth&&dummy.CurrentHealth>0,"Dummy survives all attacks");int hp=actor.Health.CurrentHealth;actor.Health.Server_ChangeHealthStateRaw(99,0,AttackType.hitscan,dummy,0);Assert(actor.Health.CurrentHealth==hp,"Dummy cannot hurt player");Assert(RiftExplorationProgress.Gold==gold,"Training grants no gold");var overlay=UnityEngine.Object.FindFirstObjectByType<RiftClassHUD>();Assert(overlay.GetComponentsInChildren<Text>().Any(t=>t.text.StartsWith("[F]")),"F ability label visible");Assert(overlay.GetComponentsInChildren<Text>().Any(t=>t.text.StartsWith("[Z]")),"Z ability label visible");step=3;next=EditorApplication.timeSinceStartup+.7;return;}
if(step==3){Capture("training");Finish(true);}
}catch(Exception e){Debug.LogException(e);report?.Add(e.ToString());Finish(false);}}
static void Finish(bool success){PlayerPrefs.SetInt(PlayerInstance.ClassPreference,originalClass);PlayerPrefs.Save();File.WriteAllText(Folder+"/validation.txt",(success?"PASS":"FAIL")+"\n"+string.Join("\n",report));SessionState.SetInt(Key+"Result",success?0:1);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
}
}
