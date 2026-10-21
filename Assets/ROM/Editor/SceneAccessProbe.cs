using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ROM.UI.Sprint01;
using MultiFPS;
using MultiFPS.UI;
namespace ROM.EditorTools {
[InitializeOnLoad] public static class SceneAccessProbe {
const string Key="ROM.SceneAccessProbe";static int step;static double started,next;
static SceneAccessProbe(){EditorApplication.playModeStateChanged+=Changed;}
public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);EditorApplication.isPlaying=true;}
static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){step=0;started=EditorApplication.timeSinceStartup;next=started+3;EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}}
static void Tick(){try{
if(EditorApplication.timeSinceStartup-started>90)throw new Exception("Menu flow timeout step="+step);if(EditorApplication.timeSinceStartup<next)return;
if(step==0){var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub||!hub.Stage||!hub.Stage.Ready)return;if(hub.Creator.Maps.Length!=1)throw new Exception("Map catalog must contain only El Rift");hub.Creator.BotsToggle.isOn=false;hub.Creator.HostGameButton.onClick.Invoke();step=1;next=EditorApplication.timeSinceStartup+5;return;}
if(step==1){if(!ClientFrontend.OwnedCharacterInstance||!ROM.Horde.Horde.Instance)return;UIPauseMenu._instance.ShowPauseMenu(true);var adapter=UnityEngine.Object.FindFirstObjectByType<RiftPauseAdapter>();if(!adapter)throw new Exception("Pause missing");adapter.Hub.LeaveRequested.Invoke();step=2;next=EditorApplication.timeSinceStartup+3;return;}
if(step==2){var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub||hub.InGame||!hub.Stage||!hub.Stage.Ready)return;if(SceneManager.GetActiveScene().path!=ProjectSceneCleanup.Menu)throw new Exception("Wrong return scene");ROM.UI.EditorTools.SprintUIProbe.Capture(hub,"Documentation/main-menu-scene.png",1920,1080);Finish(true,"PASS: MainMenu loads; 3D lobby ready; host enters El Rift; pause returns to MainMenu. Build scenes: MainMenu and El Rift.");}
}catch(Exception e){Debug.LogException(e);Finish(false,e.ToString());}}
static void Finish(bool success,string result){File.WriteAllText("Documentation/main-menu-scene-validation.txt",result);SessionState.SetInt(Key+"Result",success?0:1);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
}}
