using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mirror;
using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
using ROM.UI.Sprint01;
using ROM.UI.Gameplay;
using ROM.Progression;
namespace ROM.EditorTools {
[InitializeOnLoad] public static partial class RiftBagReview {
 const string Key="ROM.BagReview";
 static string Output {get {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-riftReviewOutput");return i>=0&&i+1<args.Length?args[i+1]:Path.GetFullPath("Logs/BagReview");}}
 static Keyboard keyboard;static GameObject typingFixture;
 static List<string> report;static int step;static double began,next;static string id;
 static RiftBagReview(){EditorApplication.playModeStateChanged+=Changed;}
 public static void Run(){
  Directory.CreateDirectory(Output);if(Environment.GetCommandLineArgs().Contains("-riftInteractionReview"))SessionState.SetString(Key+"SavedBindings",PlayerPrefs.GetString(RiftControls.Preference,""));
  SessionState.SetInt(Key+"Background",(int)InputSystem.settings.backgroundBehavior);SessionState.SetInt(Key+"EditorInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode);
  SessionState.SetString(Key+"Name",UserSettings.UserNickname);SessionState.SetInt(Key+"Skin",UserSettings.CharacterSkinID);SessionState.SetInt(Key+"Combat",PlayerPrefs.GetInt(AbilityController.Preference,0));SessionState.SetString(Key+"Active",PlayerPrefs.GetString("ROM.Character.ActiveId",""));
  SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/ROM/Scenes/MainMenu.unity");EditorApplication.isPlaying=true;
 }
 static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;
  if(state==PlayModeStateChange.EnteredPlayMode){report=new List<string>();step=0;began=next=EditorApplication.timeSinceStartup;id="BagReview_"+Guid.NewGuid().ToString("N");RiftJournalPlayer.StorageOverride=Path.Combine(Output,"bag-review-journals");EditorApplication.update+=Tick;}
  if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);if(Environment.GetCommandLineArgs().Contains("-riftInteractionReview")){var bindings=SessionState.GetString(Key+"SavedBindings","");if(bindings.Length==0)PlayerPrefs.DeleteKey(RiftControls.Preference);else PlayerPrefs.SetString(RiftControls.Preference,bindings);RiftControls.Reload();}InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)SessionState.GetInt(Key+"Background",0);InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Key+"EditorInput",0);UserSettings.UserNickname=SessionState.GetString(Key+"Name","");UserSettings.CharacterSkinID=SessionState.GetInt(Key+"Skin",1);PlayerPrefs.SetInt(AbilityController.Preference,SessionState.GetInt(Key+"Combat",0));PlayerPrefs.SetString("ROM.Character.ActiveId",SessionState.GetString(Key+"Active",""));PlayerPrefs.SetString("_playerPrefs_Username",UserSettings.UserNickname);PlayerPrefs.Save();EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}
 }
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);report.Add("PASS "+message);}
 static void Advance(double delay=.7){step++;next=EditorApplication.timeSinceStartup+delay;}
 static void Capture(string name,int w,int h){typeof(RiftGameplayHUDProbe).GetMethod("CaptureGame",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Path.Combine(Output,name+".png"),w,h});}
 static void Tick(){try{
  if(EditorApplication.timeSinceStartup-began>210)throw new Exception("Timeout step "+step);if(EditorApplication.timeSinceStartup<next)return;
  if(step==0){var menu=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!menu||!menu.Stage||!menu.Stage.Ready)return;menu.ApplyActiveCharacter();UserSettings.UserNickname=id;PlayerPrefs.SetString("ROM.Character.ActiveId",id);UserSettings.CharacterSkinID=1;PlayerPrefs.SetInt(AbilityController.Preference,2);menu.Creator.BotsToggle.isOn=false;menu.Creator.HostGameButton.onClick.Invoke();Advance(17);return;}
  var player=ClientFrontend.ClientPlayerInstance;var actor=player?player.MyCharacter:null;var hud=UnityEngine.Object.FindFirstObjectByType<RiftGameplayHUD>();if(!player||!actor||!hud)return;
  var pause=UIPauseMenu._instance;var hub=pause?pause.pauseMenuOverlay.GetComponentInChildren<RiftHub>(true):null;
  if(step==6&&Environment.GetCommandLineArgs().Contains("-riftQuestReview"))step=100;
  if(step==6&&Environment.GetCommandLineArgs().Contains("-riftWarriorReview"))step=200;
  if(step==6&&Environment.GetCommandLineArgs().Contains("-riftInteractionReview"))step=300;
  if(step>=300){InteractionTick(player,actor,hud,hub);return;}
  if(step>=200){WarriorTick(player,actor,hud);return;}
  if(step>=100){QuestTick(player,actor,hud,hub);return;}
  if(step==1){Check(hud.Design.Find("Always accessible inventory"),"Permanent bag button exists on gameplay HUD");Check(((RectTransform)hud.Design.Find("Always accessible inventory")).anchoredPosition.y>=250,"Bag button stays above existing shortcut keys");Check(UnityEngine.Object.FindFirstObjectByType<RiftInterfaceCursor>(),"Original cursor installed");PlayerPrefs.SetInt(RiftVillageSupplies.Key(0),3);PlayerPrefs.SetInt(RiftVillageSupplies.Key(1),2);hud.OpenInventory();Advance();return;}
  if(step==2){Check(hub&&hub.Page==HubPage.Inventory&&ClientFrontend.Pause,"Bag opens through gameplay pause input gate");Check(Cursor.visible&&Cursor.lockState!=CursorLockMode.Locked,"Bag releases pointer");var view=hub.GetComponentInChildren<RiftInventoryView>();Check(view&&view.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Supply slot"))==2,"Exactly two real supply slots, no fake empty storage");Check(view.GetComponentsInChildren<Text>().Any(t=>t.text=="Cantidad: 3 / 99"),"Bag reads actual saved supply quantity");Check(view.GetComponentsInChildren<RiftTooltipTrigger>().Count()==2,"Each item exposes an item tooltip");Capture("rift-bag-1920",1920,1080);actor.Health.ServerSetVitals(actor.Health.MaxHealth,actor.Health.MaxHealth/2);Advance();return;}
  if(step==3){var use=hub.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text=="USAR POCIÓN");Check(use.interactable,"Use enabled for injured host with stock");use.onClick.Invoke();Advance();return;}
  if(step==4){Check(RiftVillageSupplies.Count(0)==2,"Use consumes one actual potion");Check(player.GetComponent<ROM.Horde.RiftVillagePlayer>().PotionReadyAt>NetworkTime.time,"Server applies shared cooldown");Check(!hub.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text=="USAR POCIÓN").interactable,"Bag prevents repeat use during cooldown");var slot=hub.GetComponentsInChildren<Button>().First(b=>b.name=="Supply slot 1");slot.onClick.Invoke();Check(hub.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("75% de vida máxima")),"Selection updates major potion details");var trigger=slot.GetComponent<RiftTooltipTrigger>();trigger.OnPointerEnter(new PointerEventData(EventSystem.current));Check(RiftAbilityTooltip.Current&&RiftAbilityTooltip.Current.Visible,"Item tooltip opens");Capture("rift-bag-tooltip-1280",1280,720);trigger.OnPointerExit(new PointerEventData(EventSystem.current));Advance();return;}
  if(step==5){for(int i=0;i<5;i++){pause.ShowPauseMenu(false);hud.OpenInventory();}Check(hub.GetComponentsInChildren<RiftInventoryView>().Length==1,"Repeated openings keep one inventory view");hub.ShowGameplayPage(HubPage.Character);Check(hub.Page==HubPage.Character,"Character navigation available");hub.ShowSkills();Check(hub.Page==HubPage.Abilities,"Abilities navigation available");pause.ShowPauseMenu(false);Advance();return;}
  if(step==6){Check(hud.GetComponent<Canvas>().enabled&&!ClientFrontend.Pause,"Closing restores gameplay HUD");Check(UICursorManager.GamePlayInput(),"Closing releases pause input ownership");Capture("rift-hud-1920",1920,1080);InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.I));Advance();return;}
  if(step==7){Check(ClientFrontend.Pause&&hub.Page==HubPage.Inventory,"I opens inventory through actual input");InputSystem.QueueStateEvent(keyboard,new KeyboardState());Advance();return;}
  if(step==8){InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.I));Advance();return;}
  if(step==9){Check(!ClientFrontend.Pause,"I closes inventory through actual input");InputSystem.QueueStateEvent(keyboard,new KeyboardState());hud.OpenInventory();typingFixture=new GameObject("Typing review",typeof(RectTransform),typeof(InputField));typingFixture.transform.SetParent(hub.transform,false);EventSystem.current.SetSelectedGameObject(typingFixture);Advance();return;}
  if(step==10){Check(RiftInterfaceCursor.Typing,"Text field focus is recognized");InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.I));Advance();return;}
  if(step==11){Check(ClientFrontend.Pause&&hub.Page==HubPage.Inventory,"I does not close bag while typing");EventSystem.current.SetSelectedGameObject(null);UnityEngine.Object.Destroy(typingFixture);InputSystem.QueueStateEvent(keyboard,new KeyboardState());Advance();return;}
  if(step==12){InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Escape));Advance();return;}
  if(step==13){Check(!ClientFrontend.Pause,"Escape closes inventory through actual input");InputSystem.QueueStateEvent(keyboard,new KeyboardState());Finish(true);}
 }catch(Exception e){report.Add("FAIL "+e);Finish(false);}}
 static void Finish(bool success){EditorApplication.update-=Tick;CleanupQuests();if(keyboard!=null)InputSystem.RemoveDevice(keyboard);PlayerPrefs.DeleteKey("ROM.Journal.Cache.v1."+id);PlayerPrefs.DeleteKey(RiftVillageSupplies.Key(0));PlayerPrefs.DeleteKey(RiftVillageSupplies.Key(1));PlayerPrefs.DeleteKey("ROM.Exploration.Gold."+id);File.WriteAllLines(Path.Combine(Output,Environment.GetCommandLineArgs().Contains("-riftInteractionReview")?"rift-interaction-validation.txt":Environment.GetCommandLineArgs().Contains("-riftQuestReview")?"rift-quest-validation.txt":Environment.GetCommandLineArgs().Contains("-riftWarriorReview")?"rift-warrior-validation.txt":"rift-bag-validation.txt"),report);SessionState.SetInt(Key+"Result",success?0:1);if(NetworkServer.active)NetworkManager.singleton.StopHost();EditorApplication.isPlaying=false;}
}
}
