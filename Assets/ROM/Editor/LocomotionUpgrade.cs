using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using MultiFPS;
using MultiFPS.Gameplay;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
public static class LocomotionUpgrade {
const string Player="Assets/MultiFPS/Levels/Prefabs/Gameplay/MultiFPS_Character.prefab";
static void Require(bool valid,string detail,List<string> report){if(!valid)throw new Exception(detail);report.Add("PASS "+detail);}
public static void Build(){
var root=PrefabUtility.LoadPrefabContents(Player);var motor=root.GetComponent<CharacterMotor>();
motor.WalkSpeed=4.2f;motor.RunSpeed=7.5f;motor.CrouchSpeed=2.2f;motor.Gravity=20;motor.JumpHeight=1.2f;
var serialized=new SerializedObject(motor);serialized.FindProperty("maxAcceleration").floatValue=28;serialized.ApplyModifiedPropertiesWithoutUndo();
var capsule=root.GetComponent<CharacterController>();capsule.skinWidth=.04f;capsule.stepOffset=.3f;capsule.minMoveDistance=0;
var ca=root.GetComponent<CharacterAnimator>();serialized=new SerializedObject(ca);var catalog=AssetDatabase.LoadAssetAtPath<FrontEndCatalog>("Assets/ROM/UI/FrontEnd/FrontEndCatalog.asset");var models=serialized.FindProperty("_idsNativeModels");models.arraySize=5;foreach(var model in catalog.Characters)models.GetArrayElementAtIndex(model.SkinIndex).objectReferenceValue=model.Prefab;serialized.ApplyModifiedPropertiesWithoutUndo();
var controller=(AnimatorController)serialized.FindProperty("_baseAnimatorController").objectReferenceValue;
if(!controller.parameters.Any(p=>p.name=="locomotionRate"))controller.AddParameter("locomotionRate",AnimatorControllerParameterType.Float);
var parameters=controller.parameters;for(int i=0;i<parameters.Length;i++)if(parameters[i].name=="locomotionRate")parameters[i].defaultFloat=1;controller.parameters=parameters;
foreach(var l in controller.layers)foreach(var state in l.stateMachine.states){var s=state.state;if(l.name=="body"&&(s.name=="walk"||s.name=="run"||s.name=="crouch")){s.speed=1;s.speedParameter="locomotionRate";s.speedParameterActive=true;EditorUtility.SetDirty(s);}}
EditorUtility.SetDirty(controller);PrefabUtility.SaveAsPrefabAsset(root,Player);PrefabUtility.UnloadPrefabContents(root);AssetDatabase.SaveAssets();
}
public static void FromCommandLine(){try{Build();TestMotor();CaptureRigs();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
static void TestMotor(){
EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var report=new List<string>();
var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(200,1,200);
var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Player));var c=player.GetComponent<CharacterInstance>();
foreach(var behaviour in player.GetComponentsInChildren<Mirror.NetworkBehaviour>(true))typeof(Mirror.NetworkBehaviour).GetProperty("netIdentity").SetValue(behaviour,player.GetComponent<Mirror.NetworkIdentity>());
c.BOT=true;var health=player.GetComponent<Health>();health.CurrentHealth=100;typeof(CharacterInstance).GetField("<Health>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,health);
var motor=player.GetComponent<CharacterMotor>();typeof(CharacterMotor).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motor,null);
var controller=player.GetComponent<CharacterController>();player.layer=6;player.transform.position=new Vector3(0,.02f,0);Physics.SyncTransforms();c.Input.Movement=Vector2.up;
for(int i=0;i<100;i++){motor.MovementTick();Physics.SyncTransforms();}float straight=motor.PlanarVelocity.magnitude;Require(Mathf.Abs(straight-motor.WalkSpeed)<.15f,"walk speed "+straight,report);
c.Input.Movement=Vector2.one;for(int i=0;i<100;i++){motor.MovementTick();Physics.SyncTransforms();}Require(motor.PlanarVelocity.magnitude<=motor.WalkSpeed+.1f,"diagonal capped "+motor.PlanarVelocity.magnitude,report);
c.Input.Movement=Vector2.zero;for(int i=0;i<40;i++){motor.MovementTick();Physics.SyncTransforms();}Require(motor.PlanarVelocity.magnitude<.05f,"braking stops",report);
var headroom=typeof(CharacterMotor).GetMethod("CheckSphere",BindingFlags.Instance|BindingFlags.NonPublic);Require(!(bool)headroom.Invoke(motor,null),"open standing clearance",report);
var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.transform.localScale=new Vector3(3,.2f,3);ceiling.transform.position=player.transform.position+Vector3.up*1.75f;Physics.SyncTransforms();Require((bool)headroom.Invoke(motor,null),"ceiling prevents standing",report);UnityEngine.Object.DestroyImmediate(ceiling);
float preJump=player.transform.position.y;motor.Jump();motor.MovementTick();Require(player.transform.position.y>preJump+.01f,"jump begins upward from "+preJump+" to "+player.transform.position.y,report);
for(int i=0;i<120;i++){motor.MovementTick();Physics.SyncTransforms();}Require(Mathf.Abs(player.transform.position.y)<.15f,"jump returns to floor "+player.transform.position.y,report);
UnityEngine.Object.DestroyImmediate(floor);Physics.SyncTransforms();for(int i=0;i<120;i++)motor.MovementTick();Require(player.transform.position.y<-38,"edges allow lethal void fall",report);
File.WriteAllLines("Documentation/locomotion-motor-validation.txt",report);
UnityEngine.Object.DestroyImmediate(player);
}
static void CaptureRigs(){
var report=new List<string>();var catalog=AssetDatabase.LoadAssetAtPath<FrontEndCatalog>("Assets/ROM/UI/FrontEnd/FrontEndCatalog.asset");
var kit=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Player));var ca=kit.GetComponent<CharacterAnimator>();var so=new SerializedObject(ca);var source=(Animator)so.FindProperty("_tppModelAnimator").objectReferenceValue;
source.cullingMode=AnimatorCullingMode.AlwaysAnimate;source.Rebind();source.SetBool("isGrounded",true);source.SetFloat("speed",1);source.SetFloat("Y",1);source.SetFloat("locomotionRate",1);source.Play("body.walk",0,0);source.Update(0);
foreach(var r in kit.GetComponentsInChildren<Renderer>(true))r.enabled=false;
foreach(var model in catalog.Characters){
var clone=UnityEngine.Object.Instantiate(model.Prefab);var target=clone.GetComponent<Animator>();target.runtimeAnimatorController=null;clone.GetComponent<NaturalLegStance>().enabled=false;
var rs=clone.GetComponentsInChildren<SkinnedMeshRenderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);clone.transform.localScale*=2/Mathf.Max(.1f,b.size.y);
var sync=clone.AddComponent<IDSGameplayPose>();sync.Source=source;sync.Target=target;sync.UnarmedPreview=true;
float originalLeg=Vector3.Distance(target.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position,target.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position)+Vector3.Distance(target.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position,target.GetBoneTransform(HumanBodyBones.LeftFoot).position);
float minFoot=100,maxFoot=-100;for(int i=0;i<40;i++){source.Update(.025f);sync.ApplyNow();var foot=target.GetBoneTransform(HumanBodyBones.LeftFoot);minFoot=Mathf.Min(minFoot,foot.position.z);maxFoot=Mathf.Max(maxFoot,foot.position.z);Require(Vector3.Distance(target.GetBoneTransform(HumanBodyBones.Hips).position,source.GetBoneTransform(HumanBodyBones.Hips).position)<.5f,"hips within proportion adjustment "+model.Name+" "+i,report);}
float finalLeg=Vector3.Distance(target.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position,target.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position)+Vector3.Distance(target.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position,target.GetBoneTransform(HumanBodyBones.LeftFoot).position);
Require(Mathf.Abs(finalLeg-originalLeg)<.001f,"no bone stretching "+model.Name,report);
report.Add("Foot target error "+model.Name+": "+Vector3.Distance(target.GetBoneTransform(HumanBodyBones.LeftFoot).position,source.GetBoneTransform(HumanBodyBones.LeftFoot).position));
Require(maxFoot-minFoot>.15f,"retargeted walk swing "+model.Name+" "+(maxFoot-minFoot),report);
Capture("Documentation/locomotion-"+model.Key+".png",clone.transform.position+new Vector3(2,1.3f,3),clone.transform.position+Vector3.up);
UnityEngine.Object.DestroyImmediate(clone);
}
File.WriteAllLines("Documentation/locomotion-rig-validation.txt",report);UnityEngine.Object.DestroyImmediate(kit);
}
static void Capture(string path,Vector3 pos,Vector3 target){
var light=new GameObject("QA light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(40,-35,0);
var cam=new GameObject("QA camera").AddComponent<Camera>();cam.transform.position=pos;cam.transform.LookAt(target);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.19f,.25f);var rt=new RenderTexture(1000,1000,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1000,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,1000),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cam.gameObject);UnityEngine.Object.DestroyImmediate(light.gameObject);
}
}}
