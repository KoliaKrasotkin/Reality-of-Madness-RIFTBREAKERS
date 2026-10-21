using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.UI;
using ROM.UI.Gameplay;
using ROM.UI.Sprint01;
namespace ROM.EditorTools
{
    [InitializeOnLoad]public static class RiftMMOProbe
    {
        const string Key="ROM.MMOProbe",Folder="Documentation/mmo-controls";
        static int step,before;static float distance;static double start,next;static List<string> report;static Keyboard keyboard,originalKeyboard;static Mouse mouse,originalMouse;
        static Vector3 position;static float yaw,cameraYaw;static Health dummy,other;static Vector2 point;static RiftHub settings;
        static RiftMMOProbe(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){Directory.CreateDirectory(Folder);RiftGameplayHUDBuilder.Build();SessionState.SetString(Key+"Bindings",PlayerPrefs.GetString(RiftControls.Preference,""));SessionState.SetInt(Key+"Combat",PlayerPrefs.GetInt(AbilityController.Preference,0));SessionState.SetString(Key+"Name",UserSettings.UserNickname);SessionState.SetInt(Key+"Skin",UserSettings.CharacterSkinID);SessionState.SetBool(Key,true);PlayerPrefs.SetInt(AbilityController.Preference,2);EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){step=0;start=EditorApplication.timeSinceStartup;next=start+3;report=new List<string>();EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);string bindings=SessionState.GetString(Key+"Bindings","");if(bindings.Length==0)PlayerPrefs.DeleteKey(RiftControls.Preference);else PlayerPrefs.SetString(RiftControls.Preference,bindings);RiftControls.Reload();PlayerPrefs.SetInt(AbilityController.Preference,SessionState.GetInt(Key+"Combat",0));UserSettings.UserNickname=SessionState.GetString(Key+"Name","");UserSettings.CharacterSkinID=SessionState.GetInt(Key+"Skin",1);PlayerPrefs.Save();EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);report.Add("PASS "+message);}
        static void Advance(double delay=.25){step++;next=EditorApplication.timeSinceStartup+delay;}
        static void Keys(params UnityEngine.InputSystem.Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        static void MouseAt(Vector2 p,ushort buttons=0,Vector2 delta=default)=>InputSystem.QueueStateEvent(mouse,new MouseState{position=p,buttons=buttons,delta=delta});
        static void Tick(){try{
            if(EditorApplication.timeSinceStartup-start>210)throw new Exception("Timeout MMO step "+step);if(EditorApplication.timeSinceStartup<next)return;
            if(step==0){var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub||!hub.Stage||!hub.Stage.Ready)return;hub.ApplyActiveCharacter();UserSettings.CharacterSkinID=1;PlayerPrefs.SetInt(AbilityController.Preference,2);hub.Creator.BotsToggle.isOn=false;hub.Creator.HostGameButton.onClick.Invoke();Advance(16);return;}
            var actor=ClientFrontend.OwnedCharacterInstance;var input=RiftMMOController.Instance;var hud=UnityEngine.Object.FindFirstObjectByType<RiftGameplayHUD>();if(!actor||!input||!hud)return;var combat=ClientFrontend.ClientPlayerInstance.GetComponent<AbilityController>();var camera=GameplayCamera._instance.GetComponent<Camera>();
            switch(step){
            case 1:
                Check(combat.Prototype&&actor.GetComponent<WarriorAvatar>(),"Mirror host uses native Warrior and shared MMO controls");
                SessionState.SetInt(Key+"Background",(int)InputSystem.settings.backgroundBehavior);SessionState.SetInt(Key+"EditorInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode);InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;originalKeyboard=Keyboard.current;originalMouse=Mouse.current;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keyboard.MakeCurrent();mouse.MakeCurrent();InputSystem.EnableDevice(keyboard);InputSystem.EnableDevice(mouse);RiftControls.Reset();dummy=GameManager.Players.Where(p=>p&&p.MyCharacter&&p.MyCharacter.Health.TrainingTarget).Select(p=>p.MyCharacter.Health).First();actor.ServerTeleport(dummy.transform.position-Vector3.forward*8,0);input.CameraYaw=actor.Input.LookY=0;input.CameraPitch=12;MouseAt(new Vector2(Screen.width*.75f,Screen.height*.6f));Advance(.6);break;
            case 2:Check(Cursor.visible&&Cursor.lockState==CursorLockMode.None,"Cursor is free without holding mouse buttons");position=actor.transform.position;yaw=actor.Input.LookY;Keys(UnityEngine.InputSystem.Key.A);Advance(.5);break;
            case 3:Check(Mathf.Abs(Mathf.DeltaAngle(yaw,actor.Input.LookY))>20&&Vector3.Distance(position,actor.transform.position)<.15f,"A turns in place without strafing");Keys();actor.Input.LookY=input.CameraYaw=0;position=actor.transform.position;Advance(.1);break;
            case 4:Keys(UnityEngine.InputSystem.Key.Q);Advance(.4);break;
            case 5:Check(actor.transform.position.x<position.x-.5f&&Mathf.Abs(Mathf.DeltaAngle(actor.Input.LookY,0))<1,"Q strafes left without turning");Keys();actor.ServerTeleport(dummy.transform.position-Vector3.forward*8,0);input.CameraYaw=actor.Input.LookY=0;point=new Vector2(Screen.width*.7f,Screen.height*.5f);MouseAt(point,1);Advance(.1);break;
            case 6:yaw=actor.Input.LookY;cameraYaw=input.CameraYaw;MouseAt(point,1,new Vector2(90,0));Advance(.1);break;
            case 7:Check(Mathf.Abs(Mathf.DeltaAngle(yaw,actor.Input.LookY))<1&&Mathf.Abs(Mathf.DeltaAngle(cameraYaw,input.CameraYaw))>2,"Left drag orbits camera without changing facing");MouseAt(point);Advance(.1);break;
            case 8:MouseAt(point,2);Advance(.1);break;
            case 9:cameraYaw=input.CameraYaw;MouseAt(point,2,new Vector2(70,0));Advance(.1);break;
            case 10:Check(Mathf.Abs(Mathf.DeltaAngle(input.CameraYaw,actor.Input.LookY))<1&&Mathf.Abs(Mathf.DeltaAngle(cameraYaw,input.CameraYaw))>2,"Right drag steers both camera and character");position=actor.transform.position;yaw=actor.Input.LookY;Keys(UnityEngine.InputSystem.Key.D);MouseAt(point,2);Advance(.35);break;
            case 11:Check(Vector3.Distance(position,actor.transform.position)>.5f&&Mathf.Abs(Mathf.DeltaAngle(yaw,actor.Input.LookY))<1,"D becomes strafe while right mouse is held");Keys();position=actor.transform.position;MouseAt(point,3);Advance(.4);break;
            case 12:Check(Vector3.Distance(position,actor.transform.position)>.7f,"Both mouse buttons move forward");MouseAt(point);actor.ServerTeleport(dummy.transform.position-Vector3.forward*8,0);input.CameraYaw=actor.Input.LookY=0;Advance(.7);break;
            case 13:Check(!input.Selected&&!hud.TargetPanel.gameObject.activeSelf,"Facing a target does not select it");point=camera.WorldToScreenPoint(dummy.transform.position+Vector3.up*1.3f);MouseAt(point);Advance(.25);break;
            case 14:report.Add("Hover diagnostics pointer="+point+" picked="+(input.Hovered?input.Hovered.CharacterName:"null")+" ui="+RiftMMOController.PointerOnUI+" UI hits="+string.Join(",",((System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>)typeof(RiftMMOController).GetField("uiHits",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).Select(h=>h.gameObject.name)));Check(input.Hovered==dummy&&hud.HoverPanel.gameObject.activeSelf&&!input.Selected,"Mouseover shows independent tooltip without selecting");Capture("mouseover");MouseAt(point,1);Advance(.1);break;
            case 15:MouseAt(point);Advance(.3);break;
            case 16:Check(input.Selected==dummy&&hud.SelectedTarget==dummy&&combat.TargetNetId==dummy.netId,"Left click selects and synchronizes target identity");MouseAt(new Vector2(Screen.width*.9f,Screen.height*.7f));Advance(.2);break;
            case 17:Check(!input.Hovered&&!hud.HoverPanel.gameObject.activeSelf&&input.Selected==dummy,"Tooltip disappears while selection persists");input.CameraYaw+=100;Advance(.2);break;
            case 18:Check(input.Selected==dummy,"Moving camera does not change selected target");input.CameraYaw=actor.Input.LookY=0;other=CreateTarget(actor.transform.position+new Vector3(2,0,6));Advance(.5);break;
            case 19:Keys(UnityEngine.InputSystem.Key.Tab);Advance(.15);break;
            case 20:Check(input.Selected==other,"Tab cycles to another visible nearby enemy");Keys();Advance(.1);break;
            case 21:Keys(UnityEngine.InputSystem.Key.LeftShift,UnityEngine.InputSystem.Key.Tab);Advance(.2);break;
            case 22:Check(input.Selected==dummy,"Shift+Tab cycles backwards without double cycling");Keys();input.Select(dummy);actor.ServerTeleport(dummy.transform.position-Vector3.forward*2.4f,0);actor.Input.LookY=input.CameraYaw=0;Advance(.4);break;
            case 23:before=dummy.TrainingTotalDamage;Check(!combat.ServerSetTarget(uint.MaxValue)&&combat.TargetNetId==dummy.netId,"Server rejects invalid target identity");Check(typeof(PlayerInstance).GetMethod("AimEnemy",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ClientFrontend.ClientPlayerInstance,new object[]{45f}) as Health==dummy,"Legacy targeted abilities resolve the explicit selection");Check(combat.ServerUse(0),"Server accepts selected target within reach and facing");Advance(.9);break;
            case 24:Check(dummy.TrainingTotalDamage>before,"Selected-target attack deals authoritative damage");input.Select(null);combat.ServerSetTarget(0);Check(!combat.ServerUse(0),"Basic attack cannot hit arbitrary enemies with no selection");input.Select(dummy);actor.ServerTeleport(dummy.transform.position-Vector3.forward*9,0);Advance(.3);break;
            case 25:Check(!combat.ServerUse(0),"Server rejects target outside melee reach");Keys(UnityEngine.InputSystem.Key.Escape);Advance(.15);break;
            case 26:Check(!input.Selected&&!UIPauseMenu._instance.pauseMenuShowing,"Escape clears selection before opening menu");Keys();Advance(.1);break;
            case 27:Keys(UnityEngine.InputSystem.Key.Escape);Advance(.15);break;
            case 28:Check(UIPauseMenu._instance.pauseMenuShowing,"Escape opens menu when no target remains");Keys();settings=UIPauseMenu._instance.pauseMenuOverlay.GetComponentInChildren<RiftHub>();settings.ShowControlBindings();Advance(.3);break;
            case 29:Check(settings.GetComponentsInChildren<Button>().Length>RiftControls.Entries.Count,"Settings exposes primary/secondary bindings");Capture("bindings");settings.BeginBinding("Forward",1);Advance(.2);break;
            case 30:Keys(UnityEngine.InputSystem.Key.P);Advance(.15);break;
            case 31:Check(RiftControls.Find("Forward").Bindings[1].Path=="<Keyboard>/p"&&!RiftControls.Rebinding,"Real settings capture persists a secondary binding");Keys();RiftControls.Reload();Check(RiftControls.Find("Forward").Bindings[1].Path=="<Keyboard>/p","Binding survives preference reload");Check(RiftControls.Conflict("Forward","<Keyboard>/q",0)!=null,"Conflicting strafe assignment is detected");UIPauseMenu._instance.ShowPauseMenu(false);actor.ServerTeleport(new Vector3(10,.2f,0),0);input.CameraYaw=actor.Input.LookY=0;position=actor.transform.position;Advance(.3);break;
            case 32:Keys(UnityEngine.InputSystem.Key.P);Advance(.55);break;
            case 33:report.Add("Rebind movement diagnostics moved="+Vector3.Distance(position,actor.transform.position)+" held="+RiftControls.Held("Forward")+" available="+input.Available+" movement="+actor.Input.Movement+" blocked="+actor.Block+" paused="+ClientFrontend.Pause+" cursor="+UICursorManager.GamePlayInput());Check(Vector3.Distance(position,actor.transform.position)>.7f,"Rebound movement drives actual character");Keys();RiftControls.Bind("Ability1",1,"<Keyboard>/p",RiftControls.Modifiers.Ctrl);actor.ServerTeleport(dummy.transform.position-Vector3.forward*2.4f,0);Advance(.2);break;
            case 34:before=dummy.TrainingTotalDamage;Keys(UnityEngine.InputSystem.Key.LeftCtrl,UnityEngine.InputSystem.Key.P);Advance(.7);break;
            case 35:Check(dummy.TrainingTotalDamage>before,"Ctrl-modified rebound ability executes on server");Keys();input.Select(other);other.TrainingTarget=false;other.Server_ChangeHealthStateRaw(999999,0,AttackType.falldamage,other,0);Advance(.3);break;
            case 36:Check(!input.Selected,"Dead targets are cleared");Keys(UnityEngine.InputSystem.Key.Enter);Advance(.15);break;
            case 37:Check(ChatUI.Instance.ChatWriting,"Enter opens chat without immediately closing");position=actor.transform.position;Keys(UnityEngine.InputSystem.Key.W);Advance(.3);break;
            case 38:Check(Vector3.Distance(position,actor.transform.position)<.1f,"Chat input blocks character movement");Keys();ChatUI.Instance.ShowChatInput(false);input.CameraYaw=actor.Input.LookY=0;input.Select(dummy);Capture("target-selected");distance=input.CameraDistance;MouseAt(new Vector2(Screen.width*.9f,Screen.height*.6f));InputSystem.QueueDeltaStateEvent(mouse.scroll,new Vector2(0,120));Advance(.2);break;
            case 39:Check(input.CameraDistance<distance,"Mouse wheel zooms camera");actor.ServerTeleport(new Vector3(10,.2f,0),0);input.CameraYaw=actor.Input.LookY=0;Keys(UnityEngine.InputSystem.Key.NumLock);position=actor.transform.position;Advance(.5);break;
            case 40:report.Add("Autorun diagnostics active="+input.Autorun+" moved="+Vector3.Distance(position,actor.transform.position)+" input="+actor.Input.Movement+" available="+input.Available);Check(input.Autorun&&Vector3.Distance(position,actor.transform.position)>.5f,"Autorun moves without holding forward");Keys(UnityEngine.InputSystem.Key.S);Advance(.1);break;
            case 41:Check(!input.Autorun,"Manual backward movement cancels autorun");Keys();var shortcut=hud.transform.Find("Gameplay shortcuts/Shortcut I");point=RectTransformUtility.WorldToScreenPoint(null,shortcut.position+new Vector3(10,-10,0));MouseAt(point);Advance(.2);break;
            case 42:MouseAt(point,1);Advance(.1);break;
            case 43:MouseAt(point);Advance(.3);break;
            case 44:report.Add("UI click diagnostics module="+UnityEngine.EventSystems.EventSystem.current.currentInputModule.GetType().Name+" point="+point+" pointerBlocked="+RiftMMOController.PointerOnUI+" paused="+UIPauseMenu._instance.pauseMenuShowing);Check(UIPauseMenu._instance.pauseMenuShowing&&UIPauseMenu._instance.pauseMenuOverlay.GetComponentInChildren<RiftHub>().Page==HubPage.Inventory,"Mouse can click inventory shortcut with cursor already free");Check(input.Selected==dummy,"Clicking UI does not select the world behind it");settings=UIPauseMenu._instance.pauseMenuOverlay.GetComponentInChildren<RiftHub>();settings.ShowControlBindings();settings.BeginBinding("Jump",1);Advance(.2);break;
            case 45:Keys(UnityEngine.InputSystem.Key.Escape);Advance(.2);break;
            case 46:Check(!RiftControls.Rebinding&&UIPauseMenu._instance.pauseMenuShowing&&settings.Page==HubPage.Settings,"Escape cancels key capture without closing settings");Keys();UIPauseMenu._instance.ShowPauseMenu(false);Finish(true);break;
            }
        }catch(Exception e){Debug.LogException(e);report.Add(e.ToString());Finish(false);}}
        static Health CreateTarget(Vector3 point){var alive=(System.Collections.IDictionary)typeof(ROM.Horde.Horde).GetField("_aliveEnemies",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ROM.Horde.Horde.Instance);var before=alive.Keys.Cast<Health>().ToArray();var method=typeof(ROM.Horde.Horde).GetMethod("SpawnEnemy",BindingFlags.Instance|BindingFlags.NonPublic);var spawn=new GameObject("MMO probe spawn");spawn.transform.position=point;bool spawned=(bool)method.Invoke(ROM.Horde.Horde.Instance,new object[]{Enum.Parse(method.GetParameters()[0].ParameterType,"Melee"),spawn.transform});UnityEngine.Object.Destroy(spawn);if(!spawned)throw new Exception("Cannot create second test encounter");var health=alive.Keys.Cast<Health>().First(h=>!before.Contains(h));health.CharacterName="MMO test target";health.ServerMakeTrainingTarget();return health;}
        static void Capture(string name){typeof(RiftGameplayHUDProbe).GetMethod("CaptureGame",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{Folder+"/"+name+".png",1920,1080});}
        static void Finish(bool ok){if(keyboard!=null){InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);keyboard=null;mouse=null;if(originalKeyboard!=null)originalKeyboard.MakeCurrent();if(originalMouse!=null)originalMouse.MakeCurrent();InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)SessionState.GetInt(Key+"Background",0);InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Key+"EditorInput",0);}RiftControls.Rebinding=false;File.WriteAllLines(Folder+"/validation.txt",report);SessionState.SetInt(Key+"Result",ok?0:1);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
    }
}
