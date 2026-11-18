using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;

namespace ROM.EditorTools
{
 public static partial class CiudadDelSolInteriorStageTwo
 {
  static List<GameObject> InstallCornerMarginsSix(GameObject root)
  {
   var settings=root.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NavMeshSurface>(true)).Single(s=>s.name=="NavMesh_Exterior_CiudadDelSol").GetBuildSettings();var volumes=new List<GameObject>();
    float halfSize=Mathf.Ceil((settings.agentRadius+settings.voxelSize)/settings.voxelSize)*settings.voxelSize;
    foreach(var corner in new[]{V(-6,-30,42.05f),V(-4.95f,-38,12)})
    {
     var go=new GameObject("Margen_nav_esquina_"+volumes.Count);volumes.Add(go);go.transform.SetParent(root.transform,false);go.transform.position=corner+Vector3.up;
     var volume=go.AddComponent<NavMeshModifierVolume>();volume.center=Vector3.zero;volume.size=V(halfSize*2,3,halfSize*2);volume.area=1;
     var serialized=new SerializedObject(volume);var agents=serialized.FindProperty("m_AffectedAgents");
     if(agents==null)throw new InvalidOperationException("API de agentes del modificador no disponible.");
     agents.arraySize=1;agents.GetArrayElementAtIndex(0).intValue=0;serialized.ApplyModifiedPropertiesWithoutUndo();
    }
   return volumes;
  }
  static void FixCornerMarginsSix()
  {
   var scene=SceneManager.GetSceneByPath(City);
   if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty)throw new InvalidOperationException("Requiere Edit Mode y escena limpia.");
   var root=scene.GetRootGameObjects().Single(g=>g.name==Root6);
   if(root.GetComponentsInChildren<NavMeshModifierVolume>(true).Length!=0)throw new InvalidOperationException("Margenes ya instalados; no se duplican.");
   string backup=Work6+"/AntesMargenes";Directory.CreateDirectory(backup);
   foreach(var path in new[]{City,City+".meta",Nav,Nav+".meta"})
   {string target=backup+"/"+path;Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target,false);if(Hash(path)!=Hash(target))throw new InvalidOperationException("Respaldo no coincide.");}
   File.Copy(Work6+"/Construccion.json",backup+"/Construccion.json");File.Copy(Work6+"/Prueba_navegacion.json",backup+"/Prueba_navegacion.json");
   var surface=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NavMeshSurface>(true)).Single(s=>s.name=="NavMesh_Exterior_CiudadDelSol");
   var data=surface.navMeshData;var original=UnityEngine.Object.Instantiate(data);NavMeshData candidate=null;
   var volumes=new List<GameObject>();bool written=false,saved=false;
   try
   {
    var settings=surface.GetBuildSettings();
    if(Mathf.Abs(settings.agentRadius-.57f)>.0001f||Mathf.Abs(settings.agentClimb-.36f)>.0001f||Mathf.Abs(settings.voxelSize-.1f)>.0001f)throw new InvalidOperationException("Parametros protegidos distintos.");
    PhysicalBaseline6.Clear();var outside=OutsideSix(NavMesh.CalculateTriangulation());CapturePhysicalSix(outside);var exterior=ExteriorPaths4(NavMesh.CalculateTriangulation());
    volumes.AddRange(InstallCornerMarginsSix(root));
    float halfSize=Mathf.Ceil((settings.agentRadius+settings.voxelSize)/settings.voxelSize)*settings.voxelSize;
    Physics.SyncTransforms();var collect=typeof(NavMeshSurface).GetMethod("CollectSources",BindingFlags.Instance|BindingFlags.NonPublic);
    var sources=(List<NavMeshBuildSource>)collect.Invoke(surface,null);candidate=UnityEngine.Object.Instantiate(data);
    if(!NavMeshBuilder.UpdateNavMeshData(candidate,settings,sources,data.sourceBounds))throw new InvalidOperationException("No se genero candidata de margenes.");
    Swap4(surface,candidate);
    if(!CoverageSix(outside,"Margenes_original_en_candidata")||!CompareExterior4(exterior))throw new InvalidOperationException("Regresion fuera del sector.");
    var newOutside=OutsideSix(NavMesh.CalculateTriangulation());CapturePhysicalSix(newOutside);Swap4(surface,original);
    if(!CoverageSix(newOutside,"Margenes_candidata_en_original"))throw new InvalidOperationException("Cambio fuera del sector.");
    Swap4(surface,candidate);
    if(!ReceiveAllSix(Work6+"/Margenes/Prevalidacion",true)||!CheckHumanoidSix(Work6+"/Margenes/Prevalidacion"))throw new InvalidOperationException("Margenes no superan fisica, capsulas y regresiones.");
    written=true;surface.RemoveData();EditorUtility.CopySerialized(candidate,data);EditorUtility.SetDirty(data);surface.navMeshData=data;surface.AddData();AssetDatabase.SaveAssetIfDirty(data);
    if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("No se guardo la escena.");
    if(!ReceiveAllSix(Work6+"/Margenes/Recepcion",false)||!CheckHumanoidSix(Work6+"/Margenes/Recepcion"))throw new InvalidOperationException("Recepcion persistente fallida.");
    var build=JsonUtility.FromJson<Build4>(File.ReadAllText(Work6+"/Construccion.json"));build.sceneAfter=Hash(City);build.navAfter=Hash(Nav);build.confirmedReload=false;File.WriteAllText(Work6+"/Construccion.json",JsonUtility.ToJson(build,true));
    File.WriteAllText(Work6+"/Prueba_navegacion.json",JsonUtility.ToJson(new Proof4{digest=TriangleDigest4(),outside=outside,exterior=exterior},true));
    saved=true;File.WriteAllText(Work6+"/Margenes/Estado.txt","Dos volumenes Not Walkable; agente 0; radio original; semiancho="+halfSize+"; persistencia recibida.");
   }
   finally
   {
    if(!saved)
    {
     if(written){surface.RemoveData();EditorUtility.CopySerialized(original,data);EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);File.Copy(backup+"/"+City,City,true);File.Copy(backup+"/"+Nav,Nav,true);AssetDatabase.ImportAsset(Nav,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);}
     surface.RemoveData();surface.navMeshData=data;surface.AddData();EditorSceneManager.OpenScene(City,OpenSceneMode.Single);
    }
    if(candidate)UnityEngine.Object.DestroyImmediate(candidate);UnityEngine.Object.DestroyImmediate(original);
   }
   VerifySavedSix();ShowStageSixView(5);
  }
 }
}
