using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace ROM.EditorTools {
public static class UprightLocomotionBuilder {
const string Root="Assets/ROM/Animation/Upright";
static Dictionary<AnimationClip,AnimationClip> converted;
public static void Build(){Directory.CreateDirectory(Root);AssetDatabase.Refresh();converted=new Dictionary<AnimationClip,AnimationClip>();
var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/MultiFPS/Art/Animations/Player/Soldier_EmptyHanded.controller");
foreach(var state in controller.layers[0].stateMachine.states){if(state.state.name=="walk"||state.state.name=="run")Rewrite(state.state.motion,state.state.name=="run");}
AssetDatabase.SaveAssets();File.WriteAllLines("Documentation/upright-clip-conversion.txt",converted.Select(p=>p.Key.name+" -> "+AssetDatabase.GetAssetPath(p.Value)));}
static void Rewrite(Motion motion,bool running){if(!(motion is BlendTree tree))return;var children=tree.children;for(int i=0;i<children.Length;i++){
if(children[i].motion is BlendTree nested)Rewrite(nested,running);
else if(children[i].motion is AnimationClip clip){string path=AssetDatabase.GetAssetPath(clip);if(path.StartsWith(Root+"/"))continue;
if((running&&clip.name.Contains("run_"))||(!running&&path.EndsWith("soldier_animations_armedWalking.fbx")))children[i].motion=Convert(clip,running);}
}tree.children=children;EditorUtility.SetDirty(tree);}
static AnimationClip Convert(AnimationClip source,bool running){if(converted.TryGetValue(source,out var existing))return existing;
string path=Root+"/"+(running?"MilitaryRun-":"UprightWalk-")+source.name.Replace("|","-").Replace("@","-")+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}EditorUtility.CopySerialized(source,clip);clip.hideFlags=HideFlags.None;clip.name=Path.GetFileNameWithoutExtension(path);
foreach(var binding in AnimationUtility.GetCurveBindings(clip)){
string name=binding.propertyName;float scale=1,offset=0,minimum=-2,maximum=2;bool edit=true;
if(name=="RootT.y")offset=running?.025f:.065f;
else if(name=="Spine Twist Left-Right")scale=running?.25f:.12f;
else if(name=="Spine Left-Right")scale=.2f;
else if(name=="Spine Front-Back")scale=running?.7f:.6f;
else if(name.Contains("Upper Leg In-Out")){scale=.15f;offset=.025f;minimum=-.05f;maximum=.13f;}
else if(name.Contains("Leg Twist In-Out"))scale=.15f;
else if(name.Contains("Upper Leg Front-Back"))scale=running?.8f:.6f;
else if(name.Contains("Lower Leg Stretch")){scale=running?.65f:.55f;offset=running?.35f:.65f;minimum=-.25f;maximum=1.03f;}
else edit=false;
if(!edit)continue;var curve=AnimationUtility.GetEditorCurve(clip,binding);var keys=curve.keys;
for(int i=0;i<keys.Length;i++){float raw=keys[i].value*scale+offset;keys[i].value=Mathf.Clamp(raw,minimum,maximum);keys[i].inTangent=raw==keys[i].value?keys[i].inTangent*scale:0;keys[i].outTangent=raw==keys[i].value?keys[i].outTangent*scale:0;}
curve.keys=keys;AnimationUtility.SetEditorCurve(clip,binding,curve);
}
var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.loopBlend=true;AnimationUtility.SetAnimationClipSettings(clip,settings);clip.EnsureQuaternionContinuity();EditorUtility.SetDirty(clip);converted[source]=clip;return clip;}
public static void FromCommandLine(){try{ArmedLocomotionFix.Build();Build();ArmedMotionReview.After();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
