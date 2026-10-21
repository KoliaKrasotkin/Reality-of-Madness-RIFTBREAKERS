using System;
using System.IO;
using System.Linq;
using MultiFPS;
using MultiFPS.Gameplay;
using ROM.UI.Sprint01;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace ROM.UI.EditorTools {
public static class LizardmanIntegration {
const string Root="Assets/ROM/Characters/Lizardman";
const string Model="Assets/Novozhilov3D/Lizardman_Berserker/Base_Mesh/Lizardman_Berserker.fbx";
public static void Build(){
Directory.CreateDirectory(Root);AssetDatabase.Refresh();
var source=AssetDatabase.LoadAssetAtPath<GameObject>(Model);if(!source)throw new Exception("Lizardman model missing");
var root=UnityEngine.Object.Instantiate(source);root.name="Lizardman Berserker";
var animator=root.GetComponent<Animator>();if(!animator||!animator.isHuman)throw new Exception("Lizardman avatar invalid");
var catalog=AssetDatabase.LoadAssetAtPath<FrontEndCatalog>(FrontEndBuilder.Root+"/FrontEndCatalog.asset");
var entry=catalog.Characters.Single(m=>m.SkinIndex==1);
var oldAnimator=entry.Prefab.GetComponent<Animator>();
string previewPath=Root+"/LizardmanPreview.overrideController";
var preview=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(previewPath);
if(!preview){preview=new AnimatorOverrideController(oldAnimator.runtimeAnimatorController);AssetDatabase.CreateAsset(preview,previewPath);}
animator.runtimeAnimatorController=preview;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
var animationSet=root.AddComponent<CharacterAnimationSet>();
foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){r.updateWhenOffscreen=true;r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();}
var prefab=PrefabUtility.SaveAsPrefabAsset(root,FrontEndBuilder.Root+"/Models/ids-character-1.prefab");UnityEngine.Object.DestroyImmediate(root);
entry.Prefab=prefab;entry.Name="Lizardman Berserker";EditorUtility.SetDirty(catalog);
var skin=AssetDatabase.LoadAssetAtPath<SkinContainer>("Assets/MultiFPS/Scripts/Gameplay/Skins/CharacterSkin_Dinork.asset");
skin.SkinName=entry.Name;skin.Renderer=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r=>r.sharedMesh.vertexCount).First();skin.Mesh=skin.Renderer.sharedMesh;skin.Materials=skin.Renderer.sharedMaterials.Select(Convert).ToArray();EditorUtility.SetDirty(skin);
string player="Assets/MultiFPS/Levels/Prefabs/Gameplay/MultiFPS_Character.prefab";var actor=PrefabUtility.LoadPrefabContents(player);var so=new SerializedObject(actor.GetComponent<CharacterAnimator>());so.FindProperty("_idsNativeModels").GetArrayElementAtIndex(1).objectReferenceValue=prefab;so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(actor,player);PrefabUtility.UnloadPrefabContents(actor);
AssetDatabase.SaveAssets();
File.WriteAllText("Documentation/lizardman-integration.txt","Slot 1: Lizardman Berserker replaces Dinork. Native humanoid avatar and URP materials. Per-character animation bindings prepared. Vendor FBX and downloaded unitypackage contain NO animation clips; awaiting separate animation package. Existing locomotion is temporary.\n");
if(AssetDatabase.IsValidFolder("Assets/Mobility_Starter")){
ROM.EditorTools.SharedCharacterAnimationBuilder.Build();
File.WriteAllText("Documentation/lizardman-integration.txt","Slot 1: Lizardman Berserker replaces Dinork. Native humanoid avatar, URP materials and exclusive imported animation set. See lizardman-animation-bindings.txt.\n");
}
}
static Material Convert(Material original){
if(!original)return null;string path=Root+"/"+original.name.Replace("/","_")+"-URP.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat)return mat;
mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name=original.name+" URP";
foreach(var pair in new[]{new[]{"_MainTex","_BaseMap"},new[]{"_BumpMap","_BumpMap"},new[]{"_OcclusionMap","_OcclusionMap"},new[]{"_MetallicGlossMap","_MetallicGlossMap"},new[]{"_EmissionMap","_EmissionMap"}}){if(original.HasProperty(pair[0])&&original.GetTexture(pair[0])){mat.SetTexture(pair[1],original.GetTexture(pair[0]));mat.SetTextureScale(pair[1],original.GetTextureScale(pair[0]));}}
mat.SetColor("_BaseColor",original.HasProperty("_Color")?original.GetColor("_Color"):Color.white);mat.SetFloat("_Smoothness",.35f);
if(mat.GetTexture("_BumpMap"))mat.EnableKeyword("_NORMALMAP");if(mat.GetTexture("_MetallicGlossMap")){mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Metallic",1);}
if(mat.GetTexture("_EmissionMap")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Color.white*.7f);}AssetDatabase.CreateAsset(mat,path);return mat;
}
public static void Run(){try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
