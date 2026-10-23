using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using ROM.Progression;
using ROM.UI.Gameplay;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
public static partial class RiftBagReview {
 static Mouse uiMouse;static double firstReady;static int supplyBefore;static string firstQuest;static bool dialogCancelled;static Health originalSelection;static uint originalTarget;
 static void ReleaseKeys()=>InputSystem.QueueStateEvent(keyboard,new KeyboardState());
 static void Tap(UnityEngine.InputSystem.Key key)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
 static void UIAt(Transform target,ushort buttons=0){Canvas.ForceUpdateCanvases();var rect=(RectTransform)target;var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));InputSystem.QueueStateEvent(uiMouse,new MouseState{position=point,buttons=buttons});}
 static Button Starts(Transform root,string text)=>root.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text.StartsWith(text)==true);
 static void InteractionTick(PlayerInstance player,CharacterInstance actor,RiftGameplayHUD hud,RiftHub hub){
  var pause=UIPauseMenu._instance;var combat=player.GetComponent<AbilityController>();var field=UnityEngine.Object.FindFirstObjectByType<RiftFieldUI>();
  if(step==300){InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();uiMouse=InputSystem.AddDevice<Mouse>();uiMouse.MakeCurrent();Check(RiftControls.Find("Inventory").Bindings.Length==2,"Existing two-slot rebinding registry is present");Check(RiftControls.Find("JournalTrack")!=null&&RiftControls.Find("Settings")!=null,"Journal and settings actions extend the existing binding screen");RiftControls.Bind("Inventory",0,"");RiftControls.Bind("Inventory",1,"<Keyboard>/f8");RiftControls.Bind("Ability0",0,"<Keyboard>/1");RiftControls.Bind("Potion",0,"<Keyboard>/h");RiftControls.Bind("Settings",0,"<Keyboard>/f10");Tap(UnityEngine.InputSystem.Key.F8);Advance();return;}
  if(step==301){Check(ClientFrontend.Pause&&hub.Page==HubPage.Inventory,"Rebound F8 opens bag");Check(hub.GetComponentsInChildren<Text>().Any(t=>t.text=="BOLSA [F8]"),"Header displays actual secondary binding when primary is empty");Check(RiftControls.Conflict("Inventory","<Keyboard>/b",0)!=null,"Binding screen detects a conflicting character key");Capture("rift-ui-bindings-bag",1920,1080);ReleaseKeys();Advance();return;}
  if(step==302){Tap(UnityEngine.InputSystem.Key.F8);Advance();return;}
  if(step==303){Check(!ClientFrontend.Pause,"Rebound F8 closes bag without reopening in same frame");ReleaseKeys();Advance();return;}
  if(step==304){Tap(UnityEngine.InputSystem.Key.B);Advance();return;}
  if(step==305){Check(ClientFrontend.Pause&&hub.Page==HubPage.Character,"B opens character panel");ReleaseKeys();Advance();return;}
  if(step==306){Tap(UnityEngine.InputSystem.Key.B);Advance();return;}
  if(step==307){Check(!ClientFrontend.Pause,"B toggles character closed consistently");ReleaseKeys();hud.OpenJournal();var journal=player.GetComponent<RiftJournalPlayer>();journal.State.Accept(journal.Catalog.Quests[0]);var second=journal.Catalog.Quests[1];journal.State.Quests.Add(new QuestRecord{Id=second.Id,Status=QuestStatus.Active,Progress=new int[second.Objectives.Length]});journal.ServerGrantExperience(0);RiftControls.Bind("JournalNext",0,"<Keyboard>/pageDown");RiftControls.Bind("JournalTrack",0,"<Keyboard>/g");Advance();return;}
  if(step==308){firstQuest=journalSelectionOf(hub);Tap(UnityEngine.InputSystem.Key.PageDown);Advance();return;}
  if(step==309){Check(journalSelectionOf(hub)!=firstQuest,"PageDown selects another visible quest");ReleaseKeys();Advance();return;}
  if(step==310){Tap(UnityEngine.InputSystem.Key.G);Advance();return;}
  if(step==311){Check(RiftJournalView.Tracked==journalSelectionOf(hub),"G tracks selected active quest through shared UI action");ReleaseKeys();var track=Starts(hub.transform,"OCULTAR SEGUIMIENTO");track.onClick.Invoke();Check(RiftJournalView.Tracked=="-","Track button toggles same state as key");Capture("rift-ui-journal-keys",1280,720);hub.Dialog("Prueba de capa superior","Escape debe cerrar solo este diálogo.",()=>{},()=>dialogCancelled=true);Tap(UnityEngine.InputSystem.Key.Escape);Advance();return;}
  if(step==312){Check(dialogCancelled&&ClientFrontend.Pause,"Escape cancels top dialog and keeps underlying journal open");Check(!hub.transform.Find("SafeAreaDesign/Modal"),"Dismissed modal has no lingering blocker");ReleaseKeys();hub.ShowGameSettings();Advance();return;}
  if(step==313){var dropdown=hub.GetComponentsInChildren<Dropdown>().First();dropdown.Show();Tap(UnityEngine.InputSystem.Key.Escape);Advance();return;}
  if(step==314){Check(ClientFrontend.Pause,"Escape on dropdown keeps underlying settings menu open");Check(!hub.GetComponentsInChildren<Dropdown>().Any(d=>d.transform.Find("Dropdown List")),"Escape closes dropdown list layer");ReleaseKeys();pause.ShowPauseMenu(false);actor.Health.ServerSetVitals(actor.Health.MaxHealth,actor.Health.MaxHealth/2);player.GetComponent<ROM.Horde.RiftVillagePlayer>().PotionReadyAt=0;supplyBefore=RiftVillageSupplies.Count(0);field.Potions[0].onClick.Invoke();Advance();return;}
  if(step==315){Check(RiftVillageSupplies.Count(0)==supplyBefore-1,"Potion HUD click consumes actual server inventory");Check(!field.Potions[0].interactable,"Potion HUD click activates shared cooldown gate");field.Potions[0].onClick.Invoke();Advance();return;}
  if(step==316){Check(RiftVillageSupplies.Count(0)==supplyBefore-1,"Repeated potion click cannot bypass server cooldown");var journal=player.GetComponent<RiftJournalPlayer>();var iria=ROM.Horde.RiftVillageNpc.Active.First(n=>n.Id=="iria");var training=ROM.Horde.Horde.Instance.TrainingSpot;actor.ServerTeleport(training.position-Vector3.forward*2,0);Ready(combat);originalSelection=RiftMMOController.Instance.Selected;originalTarget=combat.TargetNetId;UIAt(hud.Abilities[0].transform);Advance(.3);return;}
  if(step==317){Check(RiftMMOController.PointerOnUI,"Hotbar pointer is captured by UI raycaster");UIAt(hud.Abilities[0].transform,1);Advance(.2);return;}
  if(step==318){UIAt(hud.Abilities[0].transform);Advance(.3);return;}
  if(step==319){int ability=RiftHotbar.Resolve(0);firstReady=combat.Ready[ability];Check(firstReady>Mirror.NetworkTime.time,"Actual pointer click on hotbar starts authoritative ability cooldown");Check(RiftMMOController.Instance.Selected==originalSelection&&combat.TargetNetId==originalTarget&&!RiftMMOController.Instance.Dragging,"Hotbar click does not select or orbit the world underneath");hud.Abilities[0].onClick.Invoke();Advance(.2);return;}
  if(step==320){Check(Math.Abs(combat.Ready[RiftHotbar.Resolve(0)]-firstReady)<.001,"Repeated hotbar click cannot bypass cooldown");var tip=hud.Abilities[0].GetComponent<RiftHotbarSlot>();tip.OnPointerEnter(new PointerEventData(EventSystem.current));Check(RiftAbilityTooltip.Current.Visible&&RiftAbilityTooltip.Current.Content.Contains(RiftControls.BindingLabel("Ability0")),"Ability tooltip shows actual action key binding");tip.OnSelect(new BaseEventData(EventSystem.current));Check(RiftAbilityTooltip.Current.Visible,"Keyboard focus exposes ability tooltip");tip.OnDeselect(new BaseEventData(EventSystem.current));tip.OnPointerExit(new PointerEventData(EventSystem.current));combat.Cancel();Ready(combat);supplyBefore=RiftVillageSupplies.Count(0);typingFixture=RiftWidgets.Input(hud.transform,"Prueba de escritura",40,250,500,v=>{}).gameObject;EventSystem.current.SetSelectedGameObject(typingFixture);typingFixture.GetComponent<InputField>().ActivateInputField();InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Digit1,UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.H));Advance();return;}
  if(step==321){Check(RiftControls.TextEntryFocused&&!RiftControls.Pressed("Ability0")&&!RiftControls.Held("Forward"),"Typing focus blocks both ability and held movement actions; selected="+(EventSystem.current.currentSelectedGameObject?EventSystem.current.currentSelectedGameObject.name:"none"));Check(combat.Casting<0&&!RiftMMOController.Instance.Available,"Typing does not launch abilities or camera/gameplay input");Check(RiftVillageSupplies.Count(0)==supplyBefore,"Typing H does not consume a potion");ReleaseKeys();EventSystem.current.SetSelectedGameObject(null);UnityEngine.Object.Destroy(typingFixture);actor.Health.ServerSetVitals(actor.Health.MaxHealth,actor.Health.MaxHealth/2);player.GetComponent<ROM.Horde.RiftVillagePlayer>().PotionReadyAt=0;Advance();return;}
  if(step==322){Tap(UnityEngine.InputSystem.Key.H);Advance();return;}
  if(step==323){Check(RiftVillageSupplies.Count(0)==supplyBefore-1,"H key uses same authoritative potion action as click");ReleaseKeys();Ready(combat);Advance();return;}
  if(step==324){Tap(UnityEngine.InputSystem.Key.Digit1);Advance(.2);return;}
  if(step==325){Check(combat.Ready[RiftHotbar.Resolve(0)]>Mirror.NetworkTime.time,"Ability key starts same authoritative cooldown as hotbar click");ReleaseKeys();combat.Cancel();Advance();return;}
  if(step==326){Tap(UnityEngine.InputSystem.Key.F10);Advance();return;}
  if(step==327){Check(ClientFrontend.Pause&&hub.Page==HubPage.Settings,"F10 opens settings through existing input registry");ReleaseKeys();hub.ShowControlBindings();Advance();return;}
  if(step==328){Check(hub.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("F8")),"Existing controls screen reflects the rebound inventory key");Capture("rift-ui-controls",1920,1080);var cursor=UnityEngine.Object.FindFirstObjectByType<RiftInterfaceCursor>();var texture=(Texture2D)typeof(RiftInterfaceCursor).GetField("texture",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(cursor);System.IO.File.WriteAllBytes(System.IO.Path.Combine(Output,"rift-cursor-actual.png"),texture.EncodeToPNG());Check(texture.width==32&&texture.height==32&&Cursor.visible&&Cursor.lockState!=CursorLockMode.Locked,"Original 32px cursor is active and free in menus");Tap(UnityEngine.InputSystem.Key.Escape);Advance();return;}
  if(step==329){Check(!ClientFrontend.Pause&&UICursorManager.GamePlayInput()&&RiftMMOController.Instance.Available,"Final Escape restores gameplay and camera input ownership");InputSystem.RemoveDevice(uiMouse);uiMouse=null;ReleaseKeys();Finish(true);}
 }
 static string journalSelectionOf(RiftHub hub)=>(string)typeof(RiftHub).GetField("journalSelection",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(hub);
}
}
