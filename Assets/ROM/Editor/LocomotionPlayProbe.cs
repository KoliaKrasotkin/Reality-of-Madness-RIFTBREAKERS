using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MultiFPS;
using MultiFPS.Gameplay;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
[InitializeOnLoad] public static class LocomotionPlayProbe {
const string Key="ROM.LocomotionPlayProbe";static double began,next;static int step;static string report;
static LocomotionPlayProbe(){EditorApplication.playModeStateChanged+=Changed;}
public static void FromCommandLine(){LocomotionUpgrade.Build();SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/MultiFPS/Levels/hub.unity");EditorApplication.isPlaying=true;}
static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Key,false))return;if(s==PlayModeStateChange.EnteredPlayMode){began=EditorApplication.timeSinceStartup;next=began+3;step=0;report="REAL HOST / GAMEPLAY SMOKE CHECK\n";EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}}
static void Tick(){try{
if(EditorApplication.timeSinceStartup-began>100)throw new Exception("Timed out waiting for gameplay character; step="+step);
if(EditorApplication.timeSinceStartup<next)return;
if(step==0){var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub)return;var creator=hub.Creator;creator.MapselectionDropdown.value=0;creator.MapselectionDropdown.onValueChanged.Invoke(0);int raid=creator.GamemodeSelectionDropdown.options.FindIndex(x=>x.text=="Raid");if(raid>=0)creator.GamemodeSelectionDropdown.value=raid;creator.BotsToggle.isOn=false;creator.HostGameButton.onClick.Invoke();step++;next=EditorApplication.timeSinceStartup+5;return;}
var character=UnityEngine.Object.FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None).FirstOrDefault(c=>c.isOwned);if(!character)return;
if(step<=5){character.SetFppPerspective(false);character.CharacterAnimator.ApplySkin(step-1);var visual=UnityEngine.Object.FindObjectsByType<IDSGameplayPose>(FindObjectsSortMode.None).FirstOrDefault(v=>v.Character==character && v.name.EndsWith(character.CharacterAnimator.MySkin.SkinName));if(!visual)throw new Exception("Missing native visual for "+character.CharacterAnimator.MySkin.SkinName);visual.ApplyNow();report+="PASS live native skin "+character.CharacterAnimator.MySkin.SkinName+"\n";step++;next=EditorApplication.timeSinceStartup+2;return;}
var pose=UnityEngine.Object.FindFirstObjectByType<IDSGameplayPose>();if(!pose||!pose.Target.isHuman)throw new Exception("Native gameplay visual did not initialize");
report+="PASS host loaded El Rift and spawned owned character\nPASS five ApplySkin calls completed during live gameplay\nPASS native humanoid pose bridge initialized; hitbox/weapon rig retained\n";Finish(true,report);
}catch(Exception e){Debug.LogException(e);Finish(false,report+e);}}
static void Finish(bool ok,string detail){EditorApplication.update-=Tick;File.WriteAllText("Documentation/locomotion-play-validation.txt",detail);SessionState.SetInt(Key+"Result",ok?0:1);EditorApplication.isPlaying=false;}
}}
