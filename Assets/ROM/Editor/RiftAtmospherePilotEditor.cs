using System;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using ROM.Rendering;
namespace ROM.EditorTools {
 [InitializeOnLoad]public static class RiftAtmospherePilotEditor {
  public const string Root="Assets/ROM/Rendering/Pilots/NightFantasy20261028";public const string SettingsPath=Root+"/NightFantasyPilot.asset";public const string PrefabPath=Root+"/NightFantasyPilot.prefab";const string OptIn="ROM.Visual.NightFantasyPilot.Enabled";static double next;
  static RiftAtmospherePilotEditor(){EditorApplication.update+=Tick;}
  [MenuItem("ROM/Visual/Atmósfera piloto/Crear o abrir configuración")]
  public static void Open(){var settings=EnsureAssets();Selection.activeObject=settings;EditorGUIUtility.PingObject(settings);}
  [MenuItem("ROM/Visual/Atmósfera piloto/Activar sólo en Editor Play")]
  public static void Enable(){EnsureAssets();EditorPrefs.SetBool(OptIn,true);EnsureInPlay();}
  [MenuItem("ROM/Visual/Atmósfera piloto/Desactivar y restaurar")]
  public static void Disable(){EditorPrefs.SetBool(OptIn,false);foreach(var driver in UnityEngine.Object.FindObjectsByType<RiftAtmospherePilot>(FindObjectsSortMode.None))if(driver.Settings==AssetDatabase.LoadAssetAtPath<RiftAtmospherePilotSettings>(SettingsPath))driver.SetPreviewEnabled(false);}
  public static bool Enabled=>EditorPrefs.GetBool(OptIn,false);
  public static RiftAtmospherePilotSettings EnsureAssets(){var existing=AssetDatabase.LoadAssetAtPath<RiftAtmospherePilotSettings>(SettingsPath);if(existing)return existing;
   Folder("Assets/ROM/Rendering","Pilots");Folder("Assets/ROM/Rendering/Pilots","NightFantasy20261028");var shader=Shader.Find("ROM/Pilots/Violet Crystal Facets");if(!shader||!shader.isSupported||ShaderUtil.ShaderHasError(shader))throw new Exception("Candidate crystal shader unavailable or has errors");
   var settings=ScriptableObject.CreateInstance<RiftAtmospherePilotSettings>();settings.CrystalShader=shader;settings.MaterialRules=new[]{new RiftAtmospherePilotSettings.MaterialRule{Source=AssetDatabase.LoadAssetAtPath<Material>("Assets/ROM/Art/RiftCrystals/Materials/Basalt.mat"),Metallic=0,Smoothness=.12f},new RiftAtmospherePilotSettings.MaterialRule{Source=AssetDatabase.LoadAssetAtPath<Material>("Assets/ROM/Characters/PaladinPrototype/Materials/AldrenArticulatedGlove.mat"),Metallic=0,Smoothness=.18f}};AssetDatabase.CreateAsset(settings,SettingsPath);AssetDatabase.SaveAssetIfDirty(settings);
   var scene=EditorSceneManager.NewPreviewScene();try{var root=new GameObject("Night Fantasy Pilot (opt in)");SceneManager.MoveGameObjectToScene(root,scene);var driver=root.AddComponent<RiftAtmospherePilot>();driver.Settings=settings;driver.Enabled=true;PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);}finally{EditorSceneManager.ClosePreviewScene(scene);}return settings;
  }
  static void Folder(string parent,string child){if(!AssetDatabase.IsValidFolder(parent+"/"+child))AssetDatabase.CreateFolder(parent,child);}
  static void Tick(){if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating)return;next=EditorApplication.timeSinceStartup+.5;if(Enabled&&EditorApplication.isPlaying)EnsureInPlay();}
  static void EnsureInPlay(){if(!EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="map_rom_horde")return;var settings=AssetDatabase.LoadAssetAtPath<RiftAtmospherePilotSettings>(SettingsPath);if(!settings)return;foreach(var d in UnityEngine.Object.FindObjectsByType<RiftAtmospherePilot>(FindObjectsSortMode.None))if(d.Settings==settings)return;var root=new GameObject("Night Fantasy Pilot (Editor opt-in)");root.hideFlags=HideFlags.DontSave;var driver=root.AddComponent<RiftAtmospherePilot>();driver.Settings=settings;driver.Enabled=true;}
 }
}
