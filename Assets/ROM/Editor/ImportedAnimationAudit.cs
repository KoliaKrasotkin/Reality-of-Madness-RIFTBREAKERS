using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace ROM.EditorTools {
public static class ImportedAnimationAudit {
public static readonly string[] Roots={"Assets/Mobility_Starter","Assets/Pistol 27A Starter","Assets/Rifle_27A2_Starter","Assets/Deaths_01","Assets/Scared","Assets/Zombie_27","Assets/Raise Creation/Samurai Animation Pack"};
public static void Run(){try{Audit();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
public static void Audit(){var report=new List<string>();foreach(var root in Roots){foreach(var p in AssetDatabase.FindAssets("t:Model",new[]{root}).Select(AssetDatabase.GUIDToAssetPath)){
var importer=AssetImporter.GetAtPath(p) as ModelImporter;if(!importer)continue;var model=AssetDatabase.LoadAssetAtPath<GameObject>(p);var a=model?model.GetComponent<Animator>():null;
report.Add("MODEL "+p+" human="+(a&&a.avatar&&a.avatar.isHuman)+" valid="+(a&&a.avatar&&a.avatar.isValid));
foreach(var c in AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")))report.Add($"  CLIP {c.name} seconds={c.length} loop={c.isLooping} human={c.humanMotion} root={c.averageSpeed}");
}}File.WriteAllLines("Documentation/imported-animation-audit.txt",report);}
}}
