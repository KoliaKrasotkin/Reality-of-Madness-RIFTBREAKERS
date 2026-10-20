using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
public static class ProjectSceneCleanup {
public const string Menu="Assets/ROM/Scenes/MainMenu.unity",Rift="Assets/ROM/Levels/map_rom_horde.unity";
[MenuItem("ROM/Escenas/Abrir Main Menu")]
public static void OpenMenu(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()){EditorSceneManager.OpenScene(Menu);var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(hub){Selection.activeGameObject=hub.gameObject;EditorGUIUtility.PingObject(hub.gameObject);}}}
[MenuItem("ROM/Escenas/Abrir El Rift")]
public static void OpenRift(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Rift);}
[MenuItem("ROM/Escenas/Actualizar vista previa del menú")]
public static void Preview(){OpenMenu();if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=Menu)return;var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();hub.Build();EditorUtility.SetDirty(hub);EditorSceneManager.MarkSceneDirty(hub.gameObject.scene);}
public static void Run(){try{
Directory.CreateDirectory("Assets/ROM/Scenes");AssetDatabase.Refresh();const string old="Assets/MultiFPS/Levels/hub.unity";
if(AssetDatabase.LoadAssetAtPath<SceneAsset>(old)){string error=AssetDatabase.MoveAsset(old,Menu);if(!string.IsNullOrEmpty(error))throw new Exception(error);}
if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(Menu))throw new Exception("MainMenu missing");
EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Menu,true),new EditorBuildSettingsScene(Rift,true)};
var scene=EditorSceneManager.OpenScene(Menu);var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub||!hub.Creator||!hub.Connector)throw new Exception("Menu bindings missing");hub.Build();EditorUtility.SetDirty(hub);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
string[] obsolete={"Assets/ROM/Levels/map_rom_horde_before_destination.unity","Assets/ROM/Levels/map_rom_horde_before_forest.unity","Assets/ROM/Levels/map_rom_horde_before_islands.unity","Assets/ROM/Levels/map_rom_horde_before_suntemple.unity","Assets/ROM/Levels/map_rom_horde_before_waterfalls.unity","Assets/ROM/Levels/SunTempleURP/DemoScene_BeforeURP.unity","Assets/ROM/Levels/map_rom_testbed.unity","Assets/ROM/Levels/Villintirius_Ruins.unity","Assets/ROM/UI/Sprint01/ROM_UI_Sprint01.unity","Assets/ROM/Levels/representer_map_rom_testbed.asset"};
var dependencies=new HashSet<string>(AssetDatabase.GetDependencies(new[]{Menu,Rift},true));var report=new List<string>{"MAIN MENU: "+Menu,"ACTIVE MAP: "+Rift,"Build scenes: 2; menu GUID preserved.","Recoverable archive outside Assets: Archive/DeprecatedScenes"};
long bytes=0;var archive=Path.GetFullPath("Archive/DeprecatedScenes")+Path.DirectorySeparatorChar;
foreach(string path in obsolete){if(!File.Exists(path))continue;if(dependencies.Contains(path))throw new Exception("Active dependency: "+path);string target=Path.GetFullPath(Path.Combine(archive,path));if(!target.StartsWith(archive,StringComparison.OrdinalIgnoreCase))throw new Exception("Invalid archive target");Directory.CreateDirectory(Path.GetDirectoryName(target));bytes+=new FileInfo(path).Length;File.Move(path,target);if(File.Exists(path+".meta"))File.Move(path+".meta",target+".meta");report.Add("ARCHIVED "+path);}
AssetDatabase.Refresh();AssetDatabase.SaveAssets();report.Add("Bytes removed from Unity asset imports: "+bytes);report.Add("Package demos and shared assets retained.");File.WriteAllLines("Documentation/scene-cleanup.txt",report);SceneAccessProbe.Run();
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
