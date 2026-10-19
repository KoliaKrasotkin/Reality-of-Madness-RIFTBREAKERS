using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using MultiFPS;
using MultiFPS.Gameplay;
namespace ROM.EditorTools {
[InitializeOnLoad] public static class ArmedMotionReview {
const string Key="ROM.ArmedMotionReview";
static double began,phaseTime,nextFrame;static int phase=-1,frame;static CharacterInstance actor;static Camera camera;static string folder;static List<string> report;static bool starting;
static ArmedMotionReview(){EditorApplication.playModeStateChanged+=Changed;}
public static void Before(){Start("before");}public static void After(){Start("after");}
public static void Lizardman(){Start("lizardman-after");}
static void Start(string suffix){SessionState.SetString(Key+"Folder","Documentation/armed-motion-"+suffix);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/MultiFPS/Levels/hub.unity");EditorApplication.isPlaying=true;}
static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){began=EditorApplication.timeSinceStartup;phase=-1;starting=false;actor=null;camera=null;folder=SessionState.GetString(Key+"Folder","");Directory.CreateDirectory(folder);report=new List<string>();EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}}
static void Tick(){try{
if(EditorApplication.timeSinceStartup-began>120)throw new Exception("Timeout stage="+phase);
if(!starting){if(EditorApplication.timeSinceStartup-began<3)return;var hub=UnityEngine.Object.FindFirstObjectByType<ROM.UI.Sprint01.RiftHub>();if(!hub)return;var creator=hub.Creator;creator.MapselectionDropdown.value=0;creator.MapselectionDropdown.onValueChanged.Invoke(0);int raid=creator.GamemodeSelectionDropdown.options.FindIndex(o=>o.text=="Raid");if(raid>=0)creator.GamemodeSelectionDropdown.value=raid;creator.BotsToggle.isOn=false;creator.HostGameButton.onClick.Invoke();starting=true;return;}
if(!actor){actor=UnityEngine.Object.FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None).FirstOrDefault(c=>c.isOwned);if(!actor)return;
foreach(var input in UnityEngine.Object.FindObjectsByType<PlayerGameplayInput>(FindObjectsSortMode.None))input.enabled=false;
actor.SetFppPerspective(false);actor.CharacterAnimator.ApplySkin(folder.Contains("lizardman")?1:2);actor.Health.CurrentHealth=30000;actor.Health.MaxHealth=30000;
var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.name="Temporary animation review platform";platform.transform.position=new Vector3(0,248,0);platform.transform.localScale=new Vector3(1000,2,1000);var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.22f,.24f,.27f));platform.GetComponent<Renderer>().material=material;
var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=new Vector3(0,249.1f,0);cc.enabled=true;
var light=new GameObject("Review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-30,0);
camera=new GameObject("Armed motion review camera").AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=45;camera.nearClipPlane=.1f;camera.farClipPlane=40;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.16f,.2f);camera.cullingMask=~((1<<5)|(1<<7)|(1<<30));
var slots=actor.CharacterItemManager.Slots;int pistol=slots.FindIndex(s=>s!=null&&s.Item&&s.Item.AnimatorControllerForCharacter&&s.Item.AnimatorControllerForCharacter.name.IndexOf("pistol",StringComparison.OrdinalIgnoreCase)>=0);if(pistol<0)throw new Exception("No pistol in real starter loadout");actor.CharacterItemManager.ClientTakeItem(pistol);phase=0;BeginPhase();return;}
if(phase<0)return;
int yaw=phase<8?(phase/2)*90:90;bool sprint=phase%2==1;
actor.Input.LookY=yaw;actor.Input.LookX=0;actor.Input.Movement=Vector2.up;actor.SetActionKeyCode(ActionCodes.Sprint,sprint);actor.SetActionKeyCode(ActionCodes.Trigger1,false);actor.SetActionKeyCode(ActionCodes.Trigger2,false);
if(folder.Contains("lizardman"))actor.SetActionKeyCode(ActionCodes.Crouch,phase==2);
if(EditorApplication.timeSinceStartup<nextFrame)return;
var so=new SerializedObject(actor.CharacterAnimator);var chest=(OverrideTransform)so.FindProperty("_chestIK").objectReferenceValue;var head=(OverrideTransform)so.FindProperty("_headIK").objectReferenceValue;
float speed=actor.GetComponent<CharacterMotor>().PlanarVelocity.magnitude;
if(folder.EndsWith("after") && speed>(sprint?7.65f:4.35f))throw new Exception("Velocity spike "+speed);
float chestError=Mathf.Abs(Mathf.DeltaAngle((folder.EndsWith("after")?chest.data.constrainedObject:chest.transform).eulerAngles.y,yaw)),headError=Mathf.Abs(Mathf.DeltaAngle((folder.EndsWith("after")?head.data.constrainedObject:head.transform).eulerAngles.y,yaw));
if(folder.EndsWith("after") && (chestError>35f || headError>15f))throw new Exception("Aim world-space error at yaw="+yaw+" chest="+chestError+" head="+headError);
var visual=UnityEngine.Object.FindObjectsByType<IDSGameplayPose>(FindObjectsSortMode.None).First(v=>v.Character==actor);var center=visual.Target.GetBoneTransform(HumanBodyBones.Hips).position;
if(folder.Contains("lizardman")){
var own=visual.Target.GetComponent<CharacterAnimationSet>();
if(phase==5){if(own)throw new Exception("Exclusive Lizardman profile leaked to another skin");}
else {
if(!own||!own.HasOwnLocomotion||own.Library.Length<300)throw new Exception("Lizardman imported animation set missing");
var played=visual.Target.GetCurrentAnimatorClipInfo(0);
if(!played.Any(c=>c.weight>.5f&&c.clip.name.StartsWith(phase>=3?"W2_":"W1_")))throw new Exception("Imported weapon locomotion not active: "+string.Join(",",played.Select(c=>c.clip.name)));
}
}
camera.transform.position=center+Quaternion.Euler(0,yaw,0)*new Vector3(2,1,-4);camera.transform.LookAt(center+Vector3.up*.05f);
Capture(folder+"/"+(folder.Contains("lizardman")?"phase-"+phase+"-":"")+(phase>=8?"skin-"+actor.CharacterAnimator.MySkin.SkinName.Replace(" ","")+"-":"")+(sprint?"run":"walk")+"-"+yaw+"-"+frame.ToString("00")+".png");
if(folder.Contains("lizardman"))report.Add("native clips="+string.Join(",",visual.Target.GetCurrentAnimatorClipInfo(0).Select(c=>c.clip.name+"("+c.weight.ToString("F3")+")"))+" cadence="+visual.Target.GetFloat("locomotionRate"));
report.Add($"skin={actor.CharacterAnimator.MySkin.SkinName} {(sprint?"run":"walk")} yaw={yaw} frame={frame}: chest error={chestError:F3}; head error={headError:F3}; speed={actor.GetComponent<CharacterMotor>().PlanarVelocity.magnitude:F2}; controller={actor.CharacterItemManager.CurrentlyUsedItem.AnimatorControllerForCharacter.name}");
frame++;nextFrame=EditorApplication.timeSinceStartup+.1;
if(frame>=12){phase++;if(phase>=(folder.Contains("lizardman")?6:16)){Finish(true);return;}BeginPhase();}
}catch(Exception e){Debug.LogException(e);report?.Add(e.ToString());Finish(false);}}
static void BeginPhase(){if(folder.Contains("lizardman")){
if(phase==3){var slots=actor.CharacterItemManager.Slots;int rifle=slots.FindIndex(s=>s!=null&&s.Item&&s.Item.AnimatorControllerForCharacter&&s.Item.AnimatorControllerForCharacter.name.IndexOf("rifle",StringComparison.OrdinalIgnoreCase)>=0);if(rifle<0){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MultiFPS/Levels/Prefabs/Gameplay/item_rifle.prefab");var item=UnityEngine.Object.Instantiate(prefab).GetComponent<Item>();item.TeamOwnership=-1;Mirror.NetworkServer.Spawn(item.gameObject);actor.CharacterItemManager.AttachItemToCharacter(item,0);rifle=slots.FindIndex(s=>s.Item==item);if(rifle<0)throw new Exception("Could not equip review rifle");}actor.CharacterItemManager.ClientTakeItem(rifle);}
if(phase==5)actor.CharacterAnimator.ApplySkin(2);
}if(phase>=8&&phase%2==0){int[] skins={0,1,3,4};actor.CharacterAnimator.ApplySkin(skins[(phase-8)/2]);}frame=0;phaseTime=EditorApplication.timeSinceStartup;nextFrame=phaseTime+1;}
static void Capture(string file){var rt=new RenderTexture(640,640,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(640,640,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();File.WriteAllBytes(file,tex.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
static void Finish(bool ok){EditorApplication.update-=Tick;if(report!=null)File.WriteAllLines(folder+"/validation.txt",new[]{ok?"PASS":"FAIL"}.Concat(report));SessionState.SetInt(Key+"Result",ok?0:1);EditorApplication.isPlaying=false;}
}}

