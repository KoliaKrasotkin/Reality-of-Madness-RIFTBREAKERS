using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
public static class UprightMotionAudit {
public static void Run(){try{
EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var report=new List<string>();
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ROM/UI/FrontEnd/Models/ids-character-2.prefab");var go=UnityEngine.Object.Instantiate(prefab);go.GetComponent<NaturalLegStance>().enabled=false;var a=go.GetComponent<Animator>();a.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MultiFPS/Art/Animations/Player/Pistol/Soldier_pistol.overrideController");a.Rebind();a.SetBool("isGrounded",true);a.SetFloat("X",0);a.SetFloat("Y",1);a.SetFloat("locomotionRate",1);a.SetLayerWeight(1,.8f);
using(var handler=new HumanPoseHandler(a.avatar,a.transform))foreach(string state in new[]{"idle","walk","run"}){
a.SetFloat("speed",state=="run"?2:state=="walk"?1:0);
for(int i=0;i<8;i++){a.Play("body."+state,0,i/8f);a.Update(0);var pose=new HumanPose();handler.GetHumanPose(ref pose);var hip=a.GetBoneTransform(HumanBodyBones.Hips);var knee=a.GetBoneTransform(HumanBodyBones.LeftLowerLeg);var ankle=a.GetBoneTransform(HumanBodyBones.LeftFoot);float kneeAngle=Vector3.Angle(a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position-knee.position,ankle.position-knee.position);
report.Add(state+" phase="+i+" hips="+hip.position.y+" ankle="+ankle.position.y+" knee="+kneeAngle+" bodyPos="+pose.bodyPosition);
if(i==0)foreach(string name in new[]{"Left Upper Leg Front-Back","Left Lower Leg Stretch","Spine Front-Back"}){int m=Array.IndexOf(HumanTrait.MuscleName,name);report.Add(name+"="+pose.muscles[m]+" min="+HumanTrait.GetMuscleDefaultMin(m)+" max="+HumanTrait.GetMuscleDefaultMax(m));}
}}
foreach(var clip in a.runtimeAnimatorController.animationClips.Where(c=>c.name.Contains("run_forward")||c.name.Contains("walk_forward"))){report.Add("CLIP "+clip.name+" "+AssetDatabase.GetAssetPath(clip));foreach(var binding in AnimationUtility.GetCurveBindings(clip)){if(binding.propertyName.Contains("RootT")||binding.propertyName.Contains("Leg")||binding.propertyName.Contains("Spine")){var curve=AnimationUtility.GetEditorCurve(clip,binding);report.Add(binding.propertyName+" min="+curve.keys.Min(k=>k.value)+" max="+curve.keys.Max(k=>k.value));}}}
File.WriteAllLines("Documentation/upright-motion-audit.txt",report);UnityEngine.Object.DestroyImmediate(go);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
