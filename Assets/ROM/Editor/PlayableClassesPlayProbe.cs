using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.UI;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
[InitializeOnLoad] public static class PlayableClassesPlayProbe {
 const string Key="ROM.PlayableClassesProbe",Folder="Documentation/playable-classes",Account="__playable-class-smoke";
 static ISocialService social; static int step,skin,oldSocial;static double began,next;static RiftHub hub;static LocalCharacterRoster roster;static List<string> report;static Keyboard keyboard,originalKeyboard;static Vector3 start;static bool finished;
 static readonly string[] Prefs={AbilityController.Preference,PlayerInstance.ClassPreference,"_playerPrefs_CharacterSkinID"};
 static PlayableClassesPlayProbe(){EditorApplication.playModeStateChanged+=Changed;}
 public static void Run(){Directory.CreateDirectory(Folder);var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-playableSkin");SessionState.SetInt(Key+"SkinUnderTest",at>=0?int.Parse(args[at+1]):3);foreach(var pref in Prefs){SessionState.SetBool(Key+pref+"Exists",PlayerPrefs.HasKey(pref));SessionState.SetInt(Key+pref,PlayerPrefs.GetInt(pref,0));}SessionState.SetString(Key+"User",PlayerPrefs.GetString("_playerPrefs_Username",""));SessionState.SetString(Key+"Active",PlayerPrefs.GetString("ROM.Character.ActiveId",""));SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/ROM/Scenes/MainMenu.unity");EditorApplication.isPlaying=true;}
 static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){skin=SessionState.GetInt(Key+"SkinUnderTest",3);step=0;began=EditorApplication.timeSinceStartup;next=began+4;report=new List<string>();finished=false;EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){foreach(var pref in Prefs){if(SessionState.GetBool(Key+pref+"Exists",false))PlayerPrefs.SetInt(pref,SessionState.GetInt(Key+pref,0));else PlayerPrefs.DeleteKey(pref);}UserSettings.CharacterSkinID=SessionState.GetInt(Key+"_playerPrefs_CharacterSkinID",1);UserSettings.UserNickname=SessionState.GetString(Key+"User","");PlayerPrefs.SetString("_playerPrefs_Username",UserSettings.UserNickname);PlayerPrefs.SetString("ROM.Character.ActiveId",SessionState.GetString(Key+"Active",""));PlayerPrefs.DeleteKey("ROM.CharacterRoster.v1."+Account);PlayerPrefs.Save();SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);report.Add("PASS "+message);Debug.Log(message);}
 static void Advance(double delay=1){step++;next=EditorApplication.timeSinceStartup+delay;}
 static void Tick(){try{if(finished)return;double now=EditorApplication.timeSinceStartup;if(now-began>240)throw new Exception("Timeout step "+step);if(now<next)return;
 if(step==0){hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub||!hub.FrontEnd)return;social=hub.Social;oldSocial=social.Self.Skin;roster=new LocalCharacterRoster(Account,"test",1,"warrior");typeof(RiftHub).GetField("roster",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hub,roster);typeof(RiftHub).GetField("rosterAccount",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hub,hub.Social.Self.Id);hub.FrontEndAction("Create");Advance();return;}
 if(step==1){Check(hub.FrontEnd.Creation.activeInHierarchy,"Authored creation screen opens");var tab=skin==3?hub.FrontEnd.GetComponentsInChildren<Button>().First(b=>b.name=="Mage class"):hub.FrontEnd.Paladin;tab.onClick.Invoke();Advance();return;}
 if(step==2){Check(hub.FrontEnd.CreationName.text==RiftSkinIdentity.Name(skin),"Correct class preview identity");Check(hub.FrontEnd.GetComponentsInChildren<NetworkIdentity>(true).Length==0,"Menu previews have no network identities");ROM.UI.EditorTools.SprintUIProbe.Capture(hub,Folder+"/skin-"+skin+"-creation.png",1920,1080);hub.FrontEnd.Confirm.onClick.Invoke();Advance();return;}
 if(step==3){Check(roster.Active.Skin==skin&&roster.Active.Available,"UI creates/selects playable character");Check(hub.FrontEnd.Selection.activeInHierarchy,"Selection returns after creation");hub.FrontEnd.Continue.onClick.Invoke();Advance();return;}
 if(step==4){var confirm=hub.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.GetComponentInChildren<Text>()?.text=="CONFIRMAR");if(!confirm){report.Add("buttons "+string.Join(" | ",hub.GetComponentsInChildren<Button>().Select(b=>b.GetComponentInChildren<Text>()?.text)));throw new Exception("Host confirmation button missing");}confirm.onClick.Invoke();Advance(15);return;}
 var actor=ClientFrontend.OwnedCharacterInstance;var player=ClientFrontend.ClientPlayerInstance;if(!actor||!player)return;var motor=actor.GetComponent<WarriorAvatar>();var combat=player.GetComponent<AbilityController>();
 switch(step){case 5:Check(actor.netId!=0&&motor&&motor.isOwned,"Host spawned owned native player");Check(player.PlayerInfo.CharacterSkinID==skin,"Spawn preserves selected skin");report.Add("Authority diagnostics prototype="+combat.Prototype+" skin="+player.PlayerInfo.CharacterSkinID+" pref="+PlayerPrefs.GetInt(AbilityController.Preference)+" class="+(combat.Class?combat.Class.Id:"null"));Check(combat.Prototype&&(skin==3?combat.IsMage:combat.IsPaladin),"Correct authority class");Check(motor.Animator&&motor.Animator.avatar.isValid,"Spawn animator valid");Check(motor.Animator.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy),"Visible character renderer active");Capture("spawn");SessionState.SetInt(Key+"Background",(int)InputSystem.settings.backgroundBehavior);SessionState.SetInt(Key+"InputEditor",(int)InputSystem.settings.editorInputBehaviorInPlayMode);InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();InputSystem.EnableDevice(keyboard);start=actor.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.W));Advance(1.5);break;
 case 6:report.Add("Movement distance="+Vector3.Distance(start,actor.transform.position)+" blocked="+actor.Block+" input="+actor.Input.Movement+" gameplay="+UICursorManager.GamePlayInput());Check(Vector3.Distance(start,actor.transform.position)>.3f,"Moves through actual keyboard/controller input");InputSystem.QueueStateEvent(keyboard,new KeyboardState());Capture("moving");Advance();break;
 case 7:Check(motor.Velocity.magnitude<.5f,"Returns to idle");Check(combat.ResourceName==(skin==3?"Maná":"Fe"),"Correct resource label");Check(combat.AutoProfile.Range==(skin==3?25:2.8f),"Expected basic attack range");Capture("idle");Finish(true);break;}
 }catch(Exception e){report?.Add("FAIL "+e);Debug.LogException(e);Finish(false);}}
 static void Capture(string name){typeof(GameUIProbe).GetMethod("CaptureGame",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Folder+"/skin-"+skin+"-"+name+".png"});}
 static void Finish(bool ok){if(finished)return;finished=true;if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;if(originalKeyboard!=null)originalKeyboard.MakeCurrent();InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)SessionState.GetInt(Key+"Background",0);InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Key+"InputEditor",0);}if(social!=null)social.SetAppearance(oldSocial);File.WriteAllLines(Folder+"/skin-"+skin+"-play.txt",report??new List<string>());SessionState.SetInt(Key+"Result",ok?0:1);EditorApplication.update-=Tick;if(NetworkManager.singleton&&NetworkServer.active)NetworkManager.singleton.StopHost();EditorApplication.isPlaying=false;}
}
}
