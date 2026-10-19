using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MultiFPS.Gameplay;
using ROM.UI.Sprint01;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace ROM.EditorTools {
public static class SharedCharacterAnimationBuilder {
const string Root="Assets/ROM/Animation/SharedCharacters";
const string Base="Assets/MultiFPS/Art/Animations/Player/Soldier_EmptyHanded.controller";
public static void Build(){
LizardmanAnimationBuilder.Build();Directory.CreateDirectory(Root);AssetDatabase.Refresh();
var reference=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ROM/UI/FrontEnd/Models/ids-character-1.prefab").GetComponent<CharacterAnimationSet>();
var generic=BuildController("Mobility",reference.Bindings);var pistol=BuildController("Pistol",reference.PistolBindings);var rifle=BuildController("Rifle",reference.RifleBindings);
var catalog=AssetDatabase.LoadAssetAtPath<FrontEndCatalog>("Assets/ROM/UI/FrontEnd/FrontEndCatalog.asset");
var idle=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/ROM/Characters/Lizardman/LizardmanIdle.controller");var library=reference.Library;
var report=new List<string>();
var weapons=LegacyWeaponAnimationBuilder.Build(generic,pistol,rifle,report);
foreach(var model in catalog.Characters){string path=AssetDatabase.GetAssetPath(model.Prefab);var root=PrefabUtility.LoadPrefabContents(path);var animator=root.GetComponent<Animator>();
if(!animator||!animator.isHuman||!animator.avatar.isValid)throw new Exception("Invalid native avatar: "+model.Name);
var set=root.GetComponent<CharacterAnimationSet>()??root.AddComponent<CharacterAnimationSet>();set.HasOwnLocomotion=true;set.Library=library;set.Weapons=weapons;
set.LocomotionController=generic;set.PistolController=pistol;set.RifleController=rifle;
set.Bindings=Array.Empty<CharacterAnimationSet.Binding>();set.PistolBindings=Array.Empty<CharacterAnimationSet.Binding>();set.RifleBindings=Array.Empty<CharacterAnimationSet.Binding>();
foreach(var component in root.GetComponents<MonoBehaviour>())if(component.GetType().Name=="NaturalLegStance")component.enabled=false;
animator.runtimeAnimatorController=idle;animator.applyRootMotion=false;
PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);report.Add(model.SkinIndex+": "+model.Name+" native avatar + imported locomotion + legacy weapon upper body");
}
foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/MultiFPS/Levels/Prefabs/Gameplay"})){
var path=AssetDatabase.GUIDToAssetPath(guid);var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);var item=go?go.GetComponent<Item>():null;if(!item)continue;
report.Add("ITEM "+go.name+" TPP="+(item.AnimatorControllerForCharacter?item.AnimatorControllerForCharacter.name:"none")+" FPP="+(item.AnimatorControllerForCharacterFPP?item.AnimatorControllerForCharacterFPP.name:"none"));
if(item.AnimatorControllerForCharacter is AnimatorOverrideController weapon){
var overrides=new List<KeyValuePair<AnimationClip,AnimationClip>>();weapon.GetOverrides(overrides);
foreach(var pair in overrides.Where(p=>p.Value))report.Add("  "+pair.Key.name+" -> "+pair.Value.name);
}
}
AssetDatabase.SaveAssets();File.WriteAllLines("Documentation/shared-character-animation-build.txt",report);
}
static AnimatorController BuildController(string name,CharacterAnimationSet.Binding[] bindings){
string path=Root+"/"+name+".controller";if(!File.Exists(path)&&!AssetDatabase.CopyAsset(Base,path))throw new Exception("Cannot clone controller");
var source=AssetDatabase.LoadAssetAtPath<AnimatorController>(Base);var target=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
UpdateMachine(target.layers[0].stateMachine,source.layers[0].stateMachine,bindings,path);EditorUtility.SetDirty(target);return target;
}
static void UpdateMachine(AnimatorStateMachine target,AnimatorStateMachine source,CharacterAnimationSet.Binding[] bindings,string path){
foreach(var child in source.states){var state=target.states.Single(s=>s.state.name==child.state.name).state;state.motion=Motion(state.motion,child.state.motion,bindings,path);EditorUtility.SetDirty(state);}
foreach(var child in source.stateMachines)UpdateMachine(target.stateMachines.Single(s=>s.stateMachine.name==child.stateMachine.name).stateMachine,child.stateMachine,bindings,path);
}
static Motion Motion(Motion current,Motion original,CharacterAnimationSet.Binding[] bindings,string path){
if(original is AnimationClip clip){foreach(var binding in bindings)if(binding.Original==clip)return binding.Replacement;return original;}
if(original is BlendTree source){var tree=current as BlendTree;if(!tree||AssetDatabase.GetAssetPath(tree)!=path){tree=UnityEngine.Object.Instantiate(source);AssetDatabase.AddObjectToAsset(tree,path);}
var old=tree.children;var children=source.children;for(int i=0;i<children.Length;i++)children[i].motion=Motion(i<old.Length?old[i].motion:null,children[i].motion,bindings,path);
tree.children=children;EditorUtility.SetDirty(tree);return tree;}
return original;
}
public static void Run(){try{Build();SharedCharacterAnimationProbe.Run();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
