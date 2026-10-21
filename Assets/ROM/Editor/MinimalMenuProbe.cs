using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ROM.UI.Sprint01;
using MultiFPS.Gameplay;
namespace ROM.EditorTools {
[InitializeOnLoad] public static class MinimalMenuProbe {
const string Key="ROM.MinimalMenuProbe",Folder="Documentation/minimal-menu";static RiftHub hub;static int step,originalClass;static double began,next;static List<string> report;
static MinimalMenuProbe(){EditorApplication.playModeStateChanged+=Changed;}
public static void BuildAndRun(){try{var scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);var view=UnityEngine.Object.FindFirstObjectByType<RiftHub>();view.Build();EditorUtility.SetDirty(view);EditorSceneManager.SaveScene(scene);Run();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
public static void Run(){Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);EditorApplication.isPlaying=true;}
static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){step=0;began=EditorApplication.timeSinceStartup;next=began+3;report=new List<string>();originalClass=PlayerPrefs.GetInt(PlayerInstance.ClassPreference,0);EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}}
static void Check(bool value,string name){if(!value)throw new Exception(name);report.Add(name+" PASS");}
static bool Has(string name)=>hub.GetComponentsInChildren<Transform>().Any(t=>t.name==name);
static void Click(string text){var button=hub.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.GetComponentInChildren<Text>()&&b.GetComponentInChildren<Text>().text==text);if(!button)throw new Exception("Missing button: "+text);button.onClick.Invoke();}
static void Capture(string name){ROM.UI.EditorTools.SprintUIProbe.Capture(hub,Folder+"/"+name+".png",1920,1080);}
static void Tick(){try{
if(EditorApplication.timeSinceStartup-began>90)throw new Exception("Menu test timeout");if(EditorApplication.timeSinceStartup<next)return;
if(step==0){hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub||!hub.Stage||!hub.Stage.Ready)return;Check(!Has("ChooseClass")&&!Has("Party")&&!Has("Emotes")&&!Has("MenuDrawer"),"Home starts with all drawers closed");Check(hub.GetComponentsInChildren<Button>().Length==6,"Six compact actions on Home");Capture("home");Click("CLASE");Check(Has("ChooseClass"),"Class drawer opens");Click(RiftClassRules.Load().Classes[(originalClass+1)%4].Name);Check(PlayerPrefs.GetInt(PlayerInstance.ClassPreference)==(originalClass+1)%4,"Class selection persists");Capture("class");Click("CERRAR");Check(!Has("ChooseClass"),"Class drawer closes");Click("EMOTES");Check(Has("Emotes"),"Emote drawer opens");Click(hub.Catalog.Emotes[0].Name);step=1;next=EditorApplication.timeSinceStartup+.7;return;}
if(step==1){Check(!string.IsNullOrEmpty(hub.Stage.CurrentEmoteName),"Emote plays from drawer");Capture("emotes");Click("DETENER");Check(string.IsNullOrEmpty(hub.Stage.CurrentEmoteName),"Emote stop works");Click("CERRAR");Click("PARTY · "+hub.Social.Party.Count+"/4");Check(Has("Party"),"Party drawer opens");Capture("party");Click("CERRAR");
string[] labels={"EQUIPAMIENTO","PROGRESO","TIENDA","AMIGOS","AJUSTES"};HubPage[] pages={HubPage.Loadout,HubPage.Progression,HubPage.Marketplace,HubPage.Social,HubPage.Settings};
for(int i=0;i<labels.Length;i++){Click("MENÚ");Check(Has("MenuDrawer"),"Menu drawer opens "+i);Click(labels[i]);Check(hub.Page==pages[i],"Hidden screen accessible: "+labels[i]);Click("VOLVER");Check(hub.Page==HubPage.Home&&!Has("MenuDrawer"),"Return restores clean Home "+i);}
Click("MENÚ");Capture("menu");Click("CERRAR");Check(hub.Creator&&hub.Creator.HostGameButton&&hub.Connector,"Existing hosting and connection bindings retained");Finish(true);}
}catch(Exception e){Debug.LogException(e);report?.Add(e.ToString());Finish(false);}}
static void Finish(bool success){PlayerPrefs.SetInt(PlayerInstance.ClassPreference,originalClass);PlayerPrefs.Save();File.WriteAllText(Folder+"/validation.txt",(success?"PASS":"FAIL")+"\n"+string.Join("\n",report));SessionState.SetInt(Key+"Result",success?0:1);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
}}
