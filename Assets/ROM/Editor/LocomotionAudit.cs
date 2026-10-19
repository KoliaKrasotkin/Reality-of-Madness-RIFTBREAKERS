using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using MultiFPS;
using MultiFPS.Gameplay;
namespace ROM.EditorTools {
public static class LocomotionAudit {
public static void Run(){try{
var lines=new List<string>();
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MultiFPS/Levels/Prefabs/Gameplay/MultiFPS_Character.prefab");
foreach(var a in prefab.GetComponentsInChildren<Animator>(true))lines.Add("RIG "+a.name+" avatar="+a.avatar+" scale="+a.transform.lossyScale);
foreach(var guid in AssetDatabase.FindAssets("t:SkinContainer")){
var skin=AssetDatabase.LoadAssetAtPath<SkinContainer>(AssetDatabase.GUIDToAssetPath(guid));if(!skin.Renderer||!AssetDatabase.GetAssetPath(skin.Renderer).Contains("IDS ASSETS"))continue;
lines.Add("SKIN "+skin.SkinName+" "+AssetDatabase.GetAssetPath(skin.Renderer)+" scale="+skin.Renderer.transform.lossyScale+" bounds="+skin.Mesh.bounds);
}
var controllers=AssetDatabase.FindAssets("t:AnimatorController",new[]{"Assets/MultiFPS"});
foreach(var guid in controllers){var p=AssetDatabase.GUIDToAssetPath(guid);var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(p);if(!c.parameters.Any(x=>x.name=="isGrounded"))continue;lines.Add("CONTROLLER "+p); foreach(var l in c.layers)foreach(var st in l.stateMachine.states)lines.Add("  "+l.name+"/"+st.state.name+" rate="+st.state.speed+" motion="+st.state.motion);}
foreach(var p in new[]{"Assets/MultiFPS/Art/Animations/Player/soldier_animations_armedWalking.fbx","Assets/MultiFPS/Art/Animations/Player/soldier_animations_emptyHanded.fbx"}) foreach(var c in AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>())lines.Add("CLIP "+p+" "+c.name+" length="+c.length+" human="+c.humanMotion+" vel="+c.averageSpeed+" loop="+c.isLooping);
File.WriteAllLines("Documentation/locomotion-audit.txt",lines);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
