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
namespace ROM.EditorTools
{
    [InitializeOnLoad] public static class WarriorRebuildProbe
    {
        const string Key="ROM.WarriorRebuildProbe",Folder="Documentation/warrior-rebuild",TestAccount="__warrior-validation";
        static int step,before,oldSocialSkin;static double began,next;static List<string> report;
        static RiftHub hub;static LocalCharacterRoster roster;static Keyboard keyboard;static Keyboard originalKeyboard;
        static Vector3 start,walkPose;static float walkDistance;static Quaternion handBefore;static Health dummy;
        static WarriorRebuildProbe(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){WarriorRebuildBuilder.Build();SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){step=0;began=EditorApplication.timeSinceStartup;next=began+3;report=new List<string>();foreach(string key in new[]{AbilityController.Preference,PlayerInstance.ClassPreference,"_playerPrefs_CharacterSkinID"})SessionState.SetInt(Key+key,PlayerPrefs.GetInt(key,0));SessionState.SetString(Key+"Name",UserSettings.UserNickname);SessionState.SetInt(Key+"Skin",UserSettings.CharacterSkinID);EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);foreach(string key in new[]{AbilityController.Preference,PlayerInstance.ClassPreference,"_playerPrefs_CharacterSkinID"})PlayerPrefs.SetInt(key,SessionState.GetInt(Key+key,0));UserSettings.UserNickname=SessionState.GetString(Key+"Name","");PlayerPrefs.SetString("_playerPrefs_Username",UserSettings.UserNickname);UserSettings.CharacterSkinID=SessionState.GetInt(Key+"Skin",0);PlayerPrefs.DeleteKey("ROM.CharacterRoster.v1."+TestAccount);PlayerPrefs.Save();EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);report.Add("PASS "+message);}
        static void Advance(double delay){step++;next=EditorApplication.timeSinceStartup+delay;}
        static void Press(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
        static void Tick(){try{
            double now=EditorApplication.timeSinceStartup;if(now-began>180)throw new Exception("Timeout step="+step);if(now<next)return;
            if(step==0){hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub||!hub.Stage||!hub.Stage.Ready)return;
                oldSocialSkin=hub.Social.Self.Skin;PlayerPrefs.DeleteKey("ROM.CharacterRoster.v1."+TestAccount);roster=new LocalCharacterRoster(TestAccount,"Original",1,"warrior");
                typeof(RiftHub).GetField("roster",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hub,roster);typeof(RiftHub).GetField("rosterAccount",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hub,hub.Social.Self.Id);
                hub.ToggleHomePanel("Class");var labels=hub.GetComponentsInChildren<Button>().Select(b=>b.GetComponentInChildren<Text>()?.text).ToArray();Check(labels.Contains("Warrior")&&!labels.Any(t=>t!=null&&(t.Contains("GUARDIAN")||t.Contains("RAVAGER"))),"Class menu shows Warrior, no specialization choices");
                hub.ToggleHomePanel("Characters");var create=hub.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text=="+ CREAR PERSONAJE");create.onClick.Invoke();var input=hub.GetComponentsInChildren<InputField>().First();input.text="Warrior de prueba";hub.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text=="CREAR Y SELECCIONAR").onClick.Invoke();Check(roster.Active.Name=="Warrior de prueba"&&roster.Active.ClassKey=="warrior","Real creation UI creates and selects Warrior");
                var reloaded=new LocalCharacterRoster(TestAccount,"ignored",0,"legacy:0");Check(reloaded.Characters.Count==2&&reloaded.Active.Id==roster.Active.Id,"Character roster persists identities separately");Check(reloaded.Create("Segundo Warrior","warrior",1)==null&&reloaded.Characters.Count==3,"Multiple characters may share Warrior class");Check(reloaded.Create("Segundo Warrior","warrior",1)!=null,"Duplicate character names rejected");
                Check(hub.Stage.PreviewAnimator&&hub.Stage.PreviewAnimator.runtimeAnimatorController.name=="WarriorLocomotion","Menu uses new native animation controller");ROM.UI.EditorTools.SprintUIProbe.Capture(hub,Folder+"/menu.png",1920,1080);
                hub.Creator.BotsToggle.isOn=false;hub.ApplyActiveCharacter();hub.Creator.HostGameButton.onClick.Invoke();Advance(16);return;
            }
            var actor=ClientFrontend.OwnedCharacterInstance;if(!actor)return;var player=ClientFrontend.ClientPlayerInstance;var native=actor.GetComponent<WarriorAvatar>();var c=player.GetComponent<AbilityController>();
            switch(step){
            case 1:
                Check(native&&native.isOwned&&actor.netId!=0,"Mirror spawns and owns fresh Warrior prefab");Check(!actor.GetComponent<CharacterMotor>()&&!actor.GetComponent<CharacterAnimator>()&&!actor.CharacterParent.GetComponentInChildren<IDSGameplayPose>(),"No template motor, animator or retarget bridge");Check(native.Animator.avatar.isValid&&native.Animator.avatar.isHuman,"Native humanoid avatar valid");Check(native.EquippedWeapon&&native.EquippedWeapon.activeInHierarchy&&native.EquippedWeapon.GetComponentsInChildren<Renderer>().All(r=>r.enabled),"Axe is actually attached and rendered");Check(native.EquippedWeapon.transform.parent==native.Animator.GetBoneTransform(HumanBodyBones.RightHand),"Axe belongs to native hand, including detached presentation root");Check(actor.CharacterItemManager.Slots.Count==0&&!actor.CharacterItemManager.CurrentlyUsedItem,"No hidden starter pistol or template equipment");Check(c.Prototype,"Warrior ability authority initialized");
                SessionState.SetInt(Key+"InputBackground",(int)InputSystem.settings.backgroundBehavior);SessionState.SetInt(Key+"InputEditor",(int)InputSystem.settings.editorInputBehaviorInPlayMode);InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();InputSystem.EnableDevice(keyboard);actor.ServerTeleport(new Vector3(0,.2f,0),0);Advance(1);break;
            case 2:start=actor.transform.position;Press(UnityEngine.InputSystem.Key.W);Advance(1.2);break;
            case 3:walkDistance=Vector3.Distance(actor.transform.position,start);report.Add("Movement diagnostics distance="+walkDistance+" input="+actor.Input.Movement+" velocity="+native.Velocity+" keyboard="+keyboard.wKey.isPressed+" current="+(Keyboard.current==keyboard)+" gameplay="+UICursorManager.GamePlayInput()+" blocked="+actor.Block+" position="+actor.transform.position);Check(walkDistance>1.5f&&walkDistance<4.3f,"New motor walks from shared Input System");Check(native.Animator.GetFloat("MoveY")>.1f,"Native locomotion consumes actual movement");Check(Vector3.Distance(native.Animator.GetBoneTransform(HumanBodyBones.Hips).position,actor.transform.position+Vector3.up)<.75f,"Visible skeleton stays aligned with gameplay capsule");Capture("walk");start=actor.transform.position;Press(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.LeftShift);Advance(1.2);break;
            case 4:Check(Vector3.Distance(actor.transform.position,start)>walkDistance*1.35f,"Sprint speed differs from walk");Capture("run");Press();Advance(.6);break;
            case 5:Check(native.Velocity.magnitude<.2f,"Independent motor brakes to idle");Check(!actor.IsCrouching,"Warrior is not automatically crouching");dummy=GameManager.Players.Where(p=>p&&p.MyCharacter&&p.MyCharacter.Health.TrainingTarget).Select(p=>p.MyCharacter.Health).First();actor.ServerTeleport(dummy.transform.position-Vector3.forward*2.5f,0);Advance(.7);break;
            case 6:before=dummy.TrainingTotalDamage;handBefore=native.Animator.GetBoneTransform(HumanBodyBones.RightHand).rotation;c.CmdUse(0);Advance(.35);break;
            case 7:Check(native.Animator.GetCurrentAnimatorStateInfo(0).IsName("Attack0")||native.Animator.GetNextAnimatorStateInfo(0).IsName("Attack0"),"Mirror attack event reaches new Animator state");Check(Quaternion.Angle(handBefore,native.Animator.GetBoneTransform(HumanBodyBones.RightHand).rotation)>5,"Attack animates actual native weapon hand");Capture("attack");Advance(.8);break;
            case 8:Check(dummy.TrainingTotalDamage>before&&c.Resource>0,"Native axe attack deals server damage and generates Rage");Check(native.Animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Attack returns to native locomotion");c.CmdUse(1);Advance(1.1);break;
            case 9:Check(c.Resource>=30,"Cleave remains functional with new player");c.CmdUse(2);Advance(1.2);break;
            case 10:Check(c.Resource==5,"Ground slam damage and Rage cost preserved");Capture("idle-axe");actor.Health.Server_ChangeHealthStateRaw(99999,0,AttackType.falldamage,actor.Health,0);Advance(.4);break;
            case 11:Check(actor.Health.CurrentHealth<=0&&!native.EquippedWeapon.activeSelf,"Death uses native body state and hides equipped axe");actor.Health.ServerResurrect();Advance(.7);break;
            case 12:Check(actor.Health.CurrentHealth>0&&actor.GetComponent<CharacterController>().enabled&&native.EquippedWeapon.activeInHierarchy,"Resurrection restores native player and axe");Check(native.Animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Native Animator exits death after resurrection");Check(!actor.FPP,"Third person remains active");start=actor.transform.position;native.RequestJump();Advance(.3);break;
            case 13:Check(actor.transform.position.y>start.y+.3f,"Independent motor jumps");Advance(1.5);break;
            case 14:Check(Mathf.Abs(actor.transform.position.y-start.y)<.3f,"Jump lands on terrain");before=actor.Health.CurrentHealth;player.LastCombatAt=NetworkTime.time;actor.ServerTeleport(start+Vector3.up*9,0);Advance(2.5);break;
            case 15:Check(actor.Health.CurrentHealth>0&&actor.Health.CurrentHealth<before,"Long falls apply server-authoritative fall damage");Check(Mathf.Abs(actor.transform.position.y-start.y)<.4f,"Native capsule lands after long fall");CaptureClean(actor);Finish(true);break;
            }
        }catch(Exception e){Debug.LogException(e);report.Add(e.ToString());Finish(false);}}
        static void CaptureClean(CharacterInstance actor){var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled&&c.isRootCanvas).ToArray();foreach(var canvas in canvases)canvas.enabled=false;Quaternion rotation=actor.CharacterParent.rotation;actor.CharacterParent.rotation=rotation*Quaternion.Euler(0,180,0);Capture("warrior-clean");actor.CharacterParent.rotation=rotation;foreach(var canvas in canvases)if(canvas)canvas.enabled=true;}
        static void Capture(string name){typeof(GameUIProbe).GetMethod("CaptureGame",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Folder+"/"+name+".png"});}
        static void Finish(bool ok){if(keyboard!=null){InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)SessionState.GetInt(Key+"InputBackground",0);InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Key+"InputEditor",0);InputSystem.RemoveDevice(keyboard);keyboard=null;if(originalKeyboard!=null)originalKeyboard.MakeCurrent();}if(hub&&hub.Social.Self.Skin!=oldSocialSkin)hub.Social.SetAppearance(oldSocialSkin);File.WriteAllLines(Folder+"/play-validation.txt",report);SessionState.SetInt(Key+"Result",ok?0:1);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
    }
}
