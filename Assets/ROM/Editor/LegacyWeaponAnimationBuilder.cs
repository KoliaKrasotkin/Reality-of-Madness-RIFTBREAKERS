using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MultiFPS.Gameplay;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace ROM.EditorTools {
public static class LegacyWeaponAnimationBuilder {
public static CharacterAnimationSet.WeaponController[] Build(AnimatorController generic,AnimatorController pistol,AnimatorController rifle,List<string> report){
var clips=AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/MultiFPS/Art/Animations/Player"}).Select(AssetDatabase.GUIDToAssetPath).Distinct().SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>().Where(c=>c.humanMotion&&!c.name.StartsWith("__preview__")).Distinct().ToArray();
var sources=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/MultiFPS/Levels/Prefabs/Gameplay"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<GameObject>).Select(go=>go.GetComponent<Item>()).Where(i=>i&&i.AnimatorControllerForCharacter).Select(i=>i.AnimatorControllerForCharacter).Distinct();
var result=new List<CharacterAnimationSet.WeaponController>();
foreach(var source in sources){string name=source.name.ToLowerInvariant();string family=name.Contains("pistol")?"pistol":name.Contains("rocket")?"rocketlauncher":name.Contains("machinegun")?"lmg":name.Contains("shotgun")?"shotgun":name.Contains("rifle")?"rifle":name.Contains("grenade")?"grenade":name.Contains("knife")?"knife":"bomb";
var template=family=="pistol"?pistol:(family=="rifle"||family=="lmg"||family=="shotgun")?rifle:generic;
var native=new AnimatorOverrideController(template);var pairs=new List<KeyValuePair<AnimationClip,AnimationClip>>();native.GetOverrides(pairs);
if(source is AnimatorOverrideController old){var overrides=new List<KeyValuePair<AnimationClip,AnimationClip>>();old.GetOverrides(overrides);for(int i=0;i<pairs.Count;i++)foreach(var p in overrides)if(p.Key==pairs[i].Key){pairs[i]=p;break;}}
var upper=template.layers[1].stateMachine;
AnimationClip Find(string suffix)=>clips.FirstOrDefault(c=>c.name.StartsWith("rig_soldier",StringComparison.OrdinalIgnoreCase)&&c.name.ToLowerInvariant().EndsWith(family+"_"+suffix));
AnimationClip Imported(string prefix)=>AssetDatabase.FindAssets("t:AnimationClip",new[]{family=="pistol"?"Assets/Pistol 27A Starter":"Assets/Rifle_27A2_Starter"}).Select(AssetDatabase.GUIDToAssetPath).Distinct().SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>().First(c=>c.name==prefix);
void Replace(AnimationClip key,AnimationClip value){if(!key||!value)return;for(int i=0;i<pairs.Count;i++)if(pairs[i].Key==key)pairs[i]=new KeyValuePair<AnimationClip,AnimationClip>(key,value);report.Add(source.name+" "+key.name+" -> "+value.name);}
foreach(var child in upper.states){var state=child.state;
if(state.name=="reload"){
var reload=Find("reload");if(!reload&&family=="shotgun"){reload=clips.FirstOrDefault(c=>c.name=="rig_soldier@rifle_reload");report.Add("Shotgun: no dedicated third-person reload in kit; legacy humanoid rifle reload fallback.");}
Replace(state.motion as AnimationClip,reload);
}else if(state.name=="take")Replace(state.motion as AnimationClip,Find("equip"));
else if(state.name=="melee")Replace(state.motion as AnimationClip,Find("melee"));
else if(state.name=="recoil"){
var fire=(family=="pistol"||family=="rifle")?Imported(family=="pistol"?"W1_Stand_Fire_Single":"W2_Stand_Fire_Single"):clips.FirstOrDefault(c=>c.name.ToLowerInvariant().Contains(family)&&c.name.ToLowerInvariant().EndsWith("_fire")&&c.name.StartsWith("rig_player",StringComparison.OrdinalIgnoreCase));
if(fire)Replace(state.motion as AnimationClip,fire);
else if(family=="lmg"||family=="shotgun")Replace(state.motion as AnimationClip,Imported("W2_Stand_Fire_Single"));
else if(family=="rocketlauncher")Replace(state.motion as AnimationClip,Find("idle"));
}
else if(state.name=="look")foreach(var key in MotionClips(state.motion)){
string n=key.name.ToLowerInvariant();Replace(key,(family=="pistol"||family=="rifle")?Imported(family=="pistol"?"W1_Stand_Aim_Idle_IPC":"W2_Stand_Aim_Idle_v2_IPC"):Find(n.EndsWith("_m90")?"idle_m90":n.EndsWith("_90")?"idle_90":"idle"));}
}
native.ApplyOverrides(pairs);string path="Assets/ROM/Animation/SharedCharacters/Legacy-"+source.name+".overrideController";var existing=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
if(existing){EditorUtility.CopySerialized(native,existing);UnityEngine.Object.DestroyImmediate(native);EditorUtility.SetDirty(existing);}else{AssetDatabase.CreateAsset(native,path);existing=native;}
result.Add(new CharacterAnimationSet.WeaponController{Original=source,Native=existing});
}
return result.ToArray();
}
static IEnumerable<AnimationClip> MotionClips(Motion motion){if(motion is AnimationClip clip)yield return clip;else if(motion is BlendTree tree)foreach(var child in tree.children)foreach(var c in MotionClips(child.motion))yield return c;}
}}
