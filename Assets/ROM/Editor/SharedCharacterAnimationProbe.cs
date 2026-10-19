using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MultiFPS;
using MultiFPS.Gameplay;
namespace ROM.EditorTools {
[InitializeOnLoad] public static class SharedCharacterAnimationProbe {
const string Key="ROM.SharedCharacterAnimationProbe",Folder="Documentation/shared-character-animations";
static readonly string[] Weapons={"pistol","rifle","rifleV2","shotgun","lightMachineGun","rocketLauncher","sniperRifle","grenade","knife","bomb","pistol"};
static CharacterInstance actor;static Camera camera;static int phase,step;static bool starting;static double began,next;static List<string> report;
static SharedCharacterAnimationProbe(){EditorApplication.playModeStateChanged+=Changed;}
public static void Run(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/validation.txt","RUNNING");SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/MultiFPS/Levels/hub.unity");EditorApplication.isPlaying=true;}
static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){phase=-1;actor=null;starting=false;began=EditorApplication.timeSinceStartup;report=new List<string>();EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}}
static void Tick(){try{
if(EditorApplication.timeSinceStartup-began>300)throw new Exception("Timeout phase="+phase);
if(!starting){if(EditorApplication.timeSinceStartup-began<3)return;var hub=UnityEngine.Object.FindFirstObjectByType<ROM.UI.Sprint01.RiftHub>();if(!hub)return;hub.Creator.BotsToggle.isOn=false;hub.Creator.HostGameButton.onClick.Invoke();starting=true;return;}
if(!actor){actor=UnityEngine.Object.FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None).FirstOrDefault(c=>c.isOwned);if(!actor)return;
foreach(var input in UnityEngine.Object.FindObjectsByType<PlayerGameplayInput>(FindObjectsSortMode.None))input.enabled=false;
actor.SetFppPerspective(false);actor.Health.CurrentHealth=actor.Health.MaxHealth=30000;
var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.name="Temporary shared animation test platform";platform.transform.position=new Vector3(0,248,0);platform.transform.localScale=new Vector3(1000,2,1000);
var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=new Vector3(0,249.1f,0);cc.enabled=true;
var light=new GameObject("Review daylight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-30,0);
camera=new GameObject("Shared character review camera").AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=45;camera.nearClipPlane=.1f;camera.farClipPlane=40;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.16f,.2f);camera.cullingMask=~((1<<5)|(1<<7)|(1<<30));
phase=0;Begin();return;}
int index=phase%Weapons.Length;bool sprint=index==1||index==4||index==6||index==8;
actor.Input.LookY=90;actor.Input.LookX=0;actor.Input.Movement=step==0?Vector2.up:Vector2.zero;actor.SetActionKeyCode(ActionCodes.Sprint,step==0&&sprint);actor.SetActionKeyCode(ActionCodes.Crouch,index==10);actor.SetActionKeyCode(ActionCodes.Trigger1,false);actor.SetActionKeyCode(ActionCodes.Trigger2,false);
if(EditorApplication.timeSinceStartup<next)return;
var visual=UnityEngine.Object.FindObjectsByType<IDSGameplayPose>(FindObjectsSortMode.None).First(v=>v.Character==actor);var set=visual.Target.GetComponent<CharacterAnimationSet>();if(!set||!set.HasOwnLocomotion||set.Weapons.Length<7)throw new Exception("Missing shared animation profile");
string label=actor.CharacterAnimator.MySkin.SkinName+"/"+Weapons[index];
if(step==0){
var clips=visual.Target.GetCurrentAnimatorClipInfo(0);if(!clips.Any(c=>c.weight>.5f&&(c.clip.name.StartsWith("MOB1_")||c.clip.name.StartsWith("W1_")||c.clip.name.StartsWith("W2_"))))throw new Exception("Imported movement missing "+label+": "+string.Join(",",clips.Select(c=>c.clip.name)));
if(!visual.Target.isHuman||!visual.Target.avatar.isValid)throw new Exception("Invalid avatar "+label);
report.Add("MOVE "+label+" "+string.Join(",",clips.Where(c=>c.weight>.05).Select(c=>c.clip.name))+" speed="+actor.GetComponent<CharacterMotor>().PlanarVelocity.magnitude);
if(index==0||index==1||index==5||index==10)Capture(visual,phase/Weapons.Length+"-"+index+"-movement");
if(actor.CharacterItemManager.CurrentlyUsedItem is Gun gun){gun.Server_CurrentAmmo=Mathf.Max(0,gun.MagazineCapacity-1);gun.Server_CurrentAmmoSupply=100;gun.CurrentAmmoSupply=100;gun.PushReload();step=1;next=EditorApplication.timeSinceStartup+.55;return;}
Advance();return;
}
var upper=visual.Target.GetCurrentAnimatorStateInfo(1);var upperClips=visual.Target.GetCurrentAnimatorClipInfo(1);
if(step==2){if(!upper.IsName("recoil"))throw new Exception("Fire event not forwarded "+label);report.Add("FIRE "+label+" "+string.Join(",",upperClips.Select(c=>c.clip.name)));if(index==0||index==5)Capture(visual,phase/Weapons.Length+"-"+index+"-fire");Advance();return;}
if(!upper.IsName("reload")||!upperClips.Any(c=>c.clip.name.ToLowerInvariant().Contains("reload")))throw new Exception("Reload not forwarded "+label+" source="+visual.Source.GetCurrentAnimatorStateInfo(1).shortNameHash+" native="+upper.shortNameHash+" clips="+string.Join(",",upperClips.Select(c=>c.clip.name)));
report.Add("RELOAD "+label+" "+string.Join(",",upperClips.Select(c=>c.clip.name)));if(index==0||index==5)Capture(visual,phase/Weapons.Length+"-"+index+"-reload");actor.CharacterAnimator.PlayAnimation("upperBody.look");actor.CharacterAnimator.SetTrigger(AnimationNames.ITEM_FIRE);step=2;next=EditorApplication.timeSinceStartup+.15;
}catch(Exception e){Debug.LogException(e);report?.Add(e.ToString());Finish(false);}}
static void Advance(){phase++;if(phase>=5*Weapons.Length){Finish(true);return;}Begin();}
static void Begin(){int index=phase%Weapons.Length;if(index==0)actor.CharacterAnimator.ApplySkin(phase/Weapons.Length);
var manager=actor.CharacterItemManager;string asset="Assets/MultiFPS/Levels/Prefabs/Gameplay/item_"+Weapons[index]+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset);if(!prefab)throw new Exception("Missing weapon "+asset);
int slot=manager.Slots.FindIndex(s=>s.Item&&s.Item.ItemName==prefab.GetComponent<Item>().ItemName);if(slot<0){var item=UnityEngine.Object.Instantiate(prefab).GetComponent<Item>();item.TeamOwnership=-1;Mirror.NetworkServer.Spawn(item.gameObject);manager.AttachItemToCharacter(item,0);slot=manager.Slots.FindIndex(s=>s.Item==item);if(slot<0)throw new Exception("Cannot equip "+asset);}manager.ClientTakeItem(slot);step=0;next=EditorApplication.timeSinceStartup+1.4;
}
static void Capture(IDSGameplayPose visual,string name){var center=visual.Target.GetBoneTransform(HumanBodyBones.Hips).position;camera.transform.position=center+Quaternion.Euler(0,90,0)*new Vector3(2,1,-4);camera.transform.LookAt(center+Vector3.up*.05f);var rt=new RenderTexture(640,640,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(640,640,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();File.WriteAllBytes(Folder+"/"+name+".png",tex.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
static void Finish(bool ok){EditorApplication.update-=Tick;File.WriteAllLines(Folder+"/validation.txt",new[]{ok?"PASS":"FAIL"}.Concat(report??new List<string>()));SessionState.SetInt(Key+"Result",ok?0:1);EditorApplication.isPlaying=false;}
}}
