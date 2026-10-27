using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MultiFPS.Gameplay.Gamemodes;
using ROM.Horde;
namespace ROM.Tutorial.Crilium.Editor {
public static class CriliumSceneBuilder {
 const string Root="Assets/ROM/Tutorial/Crilium/";
 static GameObject Prefab(string name){var path=AssetDatabase.FindAssets(name+" t:Prefab",new[]{"Assets/Standard Assets/Sun_Temple"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>System.IO.Path.GetFileNameWithoutExtension(p)==name);return path==null?null:AssetDatabase.LoadAssetAtPath<GameObject>(path);}
 static GameObject Place(GameObject prefab,Transform parent,Vector3 at,Vector3 scale){if(!prefab)throw new Exception("Missing existing prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.SetParent(parent);go.transform.position=at;go.transform.localScale=scale;return go;}
 public static void ConfigureArrival(){
 var scene=EditorSceneManager.OpenScene(CriliumSession.ScenePath,OpenSceneMode.Single);var horde=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();var spawn=horde.defaultSpawnPoints.Spawnpoints[0];var camp=spawn.GetComponent<RiftCheckpoint>();if(!camp)camp=spawn.gameObject.AddComponent<RiftCheckpoint>();camp.Title="Umbral de Crilium";camp.StartingCamp=true;camp.SafeRadius=12;camp.Respawn=spawn;horde.Checkpoints=new[]{camp};EditorSceneManager.SaveScene(scene);
 }
 [MenuItem("ROM/Crilium/Create isolated scene")]
 public static void Build(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before scene creation");
  if(System.IO.File.Exists(Root+"Crilium.unity"))throw new Exception("Crilium already exists; refusing to overwrite");
  var original=EditorSceneManager.OpenScene(CriliumSession.RiftScene,OpenSceneMode.Single);
  var source=UnityEngine.Object.FindFirstObjectByType<RoomManager>();if(!source)throw new Exception("Original room manager missing");
  var core=UnityEngine.Object.Instantiate(source.gameObject);core.name="Crilium ROOMMANAGER";
  // Move the clone into its own scene before closing the source; the source scene is never saved.
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.MoveGameObjectToScene(core,scene);SceneManager.SetActiveScene(scene);
  var horde=core.GetComponent<ROM.Horde.Horde>();if(!horde)throw new Exception("Horde configuration missing");
  horde.ExplorationMode=true;horde.Checkpoints=Array.Empty<RiftCheckpoint>();horde.Encounters=Array.Empty<RiftEncounter>();horde.TrainingSpot=null;
  foreach(var mode in core.GetComponents<Gamemode>())if(mode!=horde)UnityEngine.Object.DestroyImmediate(mode);
  var serialized=new SerializedObject(horde);var portals=serialized.FindProperty("_portals");if(portals!=null)portals.arraySize=0;var cache=serialized.FindProperty("_weaponCache");if(cache!=null)cache.objectReferenceValue=null;serialized.ApplyModifiedPropertiesWithoutUndo();
  var terrain=new GameObject("Crilium organic route");SceneManager.MoveGameObjectToScene(terrain,scene);
  var floor=Prefab("Bld_Square_Floor_A");var rock=Prefab("Rock_Boulder_A");var tree=Prefab("Fol_TallTree_A");
  Vector3[] nodes={new Vector3(0,0,0),new Vector3(0,0,32),new Vector3(20,0,64),new Vector3(45,0,86),new Vector3(42,0,120),new Vector3(14,0,150),new Vector3(0,0,184)};
  // Continuous broad, gently winding platforms with open sightlines and natural rock borders.
  for(int n=0;n<nodes.Length;n++){
   var area=new GameObject("Space "+(n+1));area.transform.SetParent(terrain.transform);area.transform.position=nodes[n];
   var baseGo=GameObject.CreatePrimitive(PrimitiveType.Cube);baseGo.name="Walkable foundation";baseGo.transform.SetParent(area.transform);baseGo.transform.localPosition=new Vector3(0,-1,0);baseGo.transform.localScale=new Vector3(48,2,48);
   var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.FindAssets("t:Material",new[]{"Assets/Standard Assets/Sun_Temple"}).Select(AssetDatabase.GUIDToAssetPath).First(p=>p.EndsWith(".mat")));baseGo.GetComponent<Renderer>().sharedMaterial=material;
   Place(floor,area.transform,nodes[n],Vector3.one*3);
   for(int side=-1;side<=1;side+=2){for(int k=0;k<3;k++){var p=nodes[n]+new Vector3(side*(21+k%2*3),0,(k-1)*15);Place(rock,area.transform,p,new Vector3(4,5+(n+k)%3,4));if(k==1)Place(tree,area.transform,p+Vector3.forward*5,Vector3.one*2);}}
  }
  var spawn=new GameObject("Crilium arrival").transform;spawn.position=new Vector3(0,2,0);SceneManager.MoveGameObjectToScene(spawn.gameObject,scene);
  var container=new GameObject("Crilium spawn points").AddComponent<SpawnpointsContainer>();container.Spawnpoints=new List<Transform>{spawn};horde.defaultSpawnPoints=container;var camp=spawn.gameObject.AddComponent<RiftCheckpoint>();camp.Title="Umbral de Crilium";camp.StartingCamp=true;camp.SafeRadius=12;camp.Respawn=spawn;horde.Checkpoints=new[]{camp};
  var portalPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ROM/Combat/Prefabs/DimensionalPortal.prefab");var portal=Place(portalPrefab,terrain.transform,new Vector3(0,2,190),Vector3.one*3);portal.name="Rift that takes Nertor's power";
  var director=new GameObject("Crilium director").AddComponent<CriliumDirector>();director.Arrival=spawn;director.ExitPortal=portal.transform;
  var sun=new GameObject("Crilium sunlight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.15f;sun.transform.rotation=Quaternion.Euler(45,-25,0);RenderSettings.ambientLight=new Color(.32f,.37f,.46f);
  EditorSceneManager.CloseScene(original,true);
  EditorSceneManager.SaveScene(scene,Root+"Crilium.unity");
  var scenes=EditorBuildSettings.scenes.ToList();if(!scenes.Any(s=>s.path==CriliumSession.ScenePath)){scenes.Add(new EditorBuildSettingsScene(CriliumSession.ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();}
  AssetDatabase.SaveAssets();Debug.Log("Crilium isolated scene created: "+Root+"Crilium.unity");
 }
}
}
