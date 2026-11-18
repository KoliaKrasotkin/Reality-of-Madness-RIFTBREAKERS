using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
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
  const string AutoRoot="CiudadDelSol_Automaticas_Margenes_20261118";
  [Serializable] sealed class AutoTargets{public List<AutoTarget> targets;}
  [Serializable] sealed class AutoTarget{public string id,hierarchy;public List<Vector3> points;}
  [Serializable] sealed class AutoPatch
  {
   public bool saved,regressions,bodyRoutes,rolledBack;
   public string error,sceneBefore,navBefore,sceneAfter,navAfter;
   public int volumes,cells;
   public List<Bounds> areas=new List<Bounds>();
  }
  static List<Vector3> AutoOutside(NavMeshTriangulation t,List<Bounds> areas)
  {
   var pts=new List<Vector3>();var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
   for(int i=0;i<t.indices.Length;i+=3)
   {
    var a=t.vertices[t.indices[i]];var b=t.vertices[t.indices[i+1]];var c=t.vertices[t.indices[i+2]];
    foreach(var p in new[]{a,b,c,(a+b+c)/3})if(!areas.Any(box=>box.Contains(p))&&NavMesh.SamplePosition(p,out var hit,.35f,filter))pts.Add(hit.position);
   }
   return pts;
  }
  static bool AutoCoverage(List<Vector3> pts,string phase)
  {
   var f=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};var cfg=ConstructionConfig4(SceneManager.GetSceneByPath(City));var failures=new List<string>();var changes=new List<string>();
   foreach(var p in pts)
   {
    if(NavMesh.SamplePosition(p,out var exact,.001f,f)&&Vector3.Distance(p,exact.position)<=.001f)continue;
    bool same=false;
    if(PhysicalBaseline6.TryGetValue(p,out var old)&&old.Clear&&NavMesh.SamplePosition(p,out var h,.35f,f)&&new Vector2(h.position.x-p.x,h.position.z-p.z).magnitude<=.001f)
    {
     var now=Probe(h.position,cfg,true,.35f);same=now.Clear&&now.supportObject==old.supportObject&&Vector3.Distance(old.floor,now.floor)<=.001f&&Vector3.Angle(old.normal,now.normal)<=.001f;
     if(same)changes.Add(p.ToString("R")+" -> "+h.position.ToString("R")+" "+old.supportObject);
    }
    if(!same&&PhysicalBaseline6.TryGetValue(p,out var boundary)&&boundary.navEdge<=.001f&&boundary.navEdge>=0)
    {
     var settings=NavMesh.GetSettingsByID(0);var agentCfg=ConstructionConfig4(SceneManager.GetSceneByPath(City));agentCfg.radius=settings.agentRadius;agentCfg.height=Mathf.Max(agentCfg.height,settings.agentHeight);
     float voxel=SceneManager.GetSceneByPath(City).GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NavMeshSurface>(true)).Single(v=>v.name=="NavMesh_Exterior_CiudadDelSol").GetBuildSettings().voxelSize;
     if(NavMesh.SamplePosition(p,out var n,.35f,f)&&new Vector2(n.position.x-p.x,n.position.z-p.z).magnitude<=voxel&&NavMesh.FindClosestEdge(n.position,out var edge,f)&&edge.distance<=.001f)
     {
      var oldPhysical=Probe(p,agentCfg,true,.35f);var nowPhysical=Probe(n.position,agentCfg,true,.35f);
      bool floor=oldPhysical.Clear&&nowPhysical.Clear&&oldPhysical.supportObject==nowPhysical.supportObject&&Mathf.Abs(Vector3.Dot(nowPhysical.floor-oldPhysical.floor,oldPhysical.normal))<=.001f&&Vector3.Angle(oldPhysical.normal,nowPhysical.normal)<=.001f&&Sweep(oldPhysical,nowPhysical,agentCfg,true,out _,out _);
      int connectedInterior=0;
      if(floor)for(int i=0;i<8;i++)
      {
       var q=n.position+V(Mathf.Cos(i*Mathf.PI/4)*voxel,0,Mathf.Sin(i*Mathf.PI/4)*voxel);
       if(!NavMesh.SamplePosition(q,out var inside,.001f,f)||!NavMesh.FindClosestEdge(inside.position,out var interiorEdge,f)||interiorEdge.distance<voxel*.5f)continue;
       var path=new NavMeshPath();var back=new NavMeshPath();
       var interiorPhysical=Probe(inside.position,agentCfg,true,.35f);
       if(interiorPhysical.Clear&&interiorPhysical.supportObject==nowPhysical.supportObject&&Sweep(nowPhysical,interiorPhysical,agentCfg,true,out _,out _)&&Connected(n.position,inside.position,agentCfg,false,false,out _)&&Connected(inside.position,n.position,agentCfg,false,false,out _)&&NavMesh.CalculatePath(n.position,inside.position,f,path)&&path.status==NavMeshPathStatus.PathComplete&&NavMesh.CalculatePath(inside.position,n.position,f,back)&&back.status==NavMeshPathStatus.PathComplete)connectedInterior++;
      }
      same=floor&&connectedInterior>=2;
      if(!same)changes.Add("ARISTA_NO_DEMOSTRADA floor="+floor+" interior="+connectedInterior+" before="+JsonUtility.ToJson(oldPhysical)+" after="+JsonUtility.ToJson(nowPhysical));
      if(same)changes.Add("ARISTA_RETRIANGULADA_CON_PRUEBA_FISICA_Y_NAV_RECIPROCA "+p.ToString("R")+" -> "+n.position.ToString("R")+" interior="+connectedInterior+" support="+oldPhysical.supportObject);
     }
    }
    if(!same){bool found=NavMesh.SamplePosition(p,out var nearest,.35f,f);failures.Add(p.ToString("R")+" nearest="+(found?nearest.position.ToString("R"):"none")+" baseline="+(PhysicalBaseline6.TryGetValue(p,out var baseline)?JsonUtility.ToJson(baseline):"none"));}
   }
   File.WriteAllText(AutoWork+"/Cobertura_"+phase+".txt","Muestras="+pts.Count+"; fallos="+failures.Count+"; interpolaciones_fisicamente_iguales="+changes.Count+"\n"+string.Join("\n",failures)+"\n"+string.Join("\n",changes));return failures.Count==0;
  }
  static bool AutoRegressions(string output)
  {
   bool six=ReceiveAllSix(output+"/Etapas1a6",true)&&CheckHumanoidSix(output+"/Etapas1a6");
   var seven=InspectSelected(new[]{StageSevenPlan()},output+"/VS06","Regresion VS06",true,false);
   var joined=InspectSelected(ChainsSeven(),output+"/Encadenadas","Regresion continuidad recibida",true,true);
   return six&&Raw4(seven,1)&&Raw4(joined,3)&&AgentEvidenceSeven(joined,output+"/HumanoidEncadenadas");
  }
  static void CorrectAutomaticSources()
  {
   var scene=SceneManager.GetSceneByPath(City);
   if(EditorApplication.isPlayingOrWillChangePlaymode||!scene.isLoaded||scene.isDirty)throw new InvalidOperationException("Requiere escena definitiva limpia en Edit Mode.");
   if(scene.GetRootGameObjects().Any(g=>g.name==AutoRoot))throw new InvalidOperationException("Correccion ya existente; no duplicar.");
   var surface=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NavMeshSurface>(true)).Single(s=>s.name=="NavMesh_Exterior_CiudadDelSol");
   var settings=surface.GetBuildSettings();
   if(settings.agentTypeID!=0||Mathf.Abs(settings.agentRadius-.57f)>.0001f||Mathf.Abs(settings.agentClimb-.36f)>.0001f||Mathf.Abs(settings.voxelSize-.1f)>.0001f)throw new InvalidOperationException("Parametros protegidos incompatibles.");
   var targets=JsonUtility.FromJson<AutoTargets>("{\"targets\":"+File.ReadAllText(AutoWork+"/Objetivos_locales.json")+"}").targets;
   var cfg=ConstructionConfig4(scene);cfg.radius=settings.agentRadius;cfg.height=Mathf.Max(cfg.height,settings.agentHeight);
   var result=new AutoPatch{sceneBefore=Hash(City),navBefore=Hash(Nav)};var data=surface.navMeshData;var original=UnityEngine.Object.Instantiate(data);NavMeshData candidate=null;GameObject root=null;bool written=false;
   var originals=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var fingerprint=Fingerprint4(originals,new HashSet<int>());
   try
   {
    if(Hash(AutoWork+"/"+City)!=result.sceneBefore||Hash(AutoWork+"/"+Nav)!=result.navBefore)throw new InvalidOperationException("Respaldo no coincide.");
    // Occupancy is measured with the actual Humanoid capsule against each
    // offending collider, on its real floor. Only colliding centers are marked,
    // with one existing bake voxel of conservative discretization padding.
    var marked=new HashSet<(int y,int x,int z)>();var railCorners=new List<Vector3>();float v=settings.voxelSize;
    File.WriteAllText(AutoWork+"/Testigo_landing_original.json",JsonUtility.ToJson(Probe(V(42.6f,-23.95f,17.4f),cfg,true,.35f),true));
    foreach(var target in targets)
    {
     if(!GlobalObjectId.TryParse(target.id,out var id)||!(GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is Collider obstacle))throw new InvalidOperationException("Collider original no encontrado: "+target.hierarchy);
     float y=target.points[0].y;float loX=target.points.Min(p=>p.x)-3,hiX=target.points.Max(p=>p.x)+3,loZ=target.points.Min(p=>p.z)-3,hiZ=target.points.Max(p=>p.z)+3;
     var area=new Bounds(V((loX+hiX)/2,y+1,(loZ+hiZ)/2),V(hiX-loX+2,5,hiZ-loZ+2));result.areas.Add(area);
     if(target.hierarchy.StartsWith(BuildRoot4+"/",StringComparison.Ordinal)&&(obstacle.name=="Proteccion_16"||obstacle.name=="Proteccion_39"))
     {
      var b=obstacle.bounds;railCorners.Add(V(obstacle.name=="Proteccion_16"?b.max.x:b.min.x,y,b.min.z));continue;
     }
     for(int x=Mathf.FloorToInt(loX/v);x<=Mathf.CeilToInt(hiX/v);x++)for(int z=Mathf.FloorToInt(loZ/v);z<=Mathf.CeilToInt(hiZ/v);z++)
     {
      var p=Probe(V((x+.5f)*v,y,(z+.5f)*v),cfg,false,.03f);if(!p.support)continue;
      int n=cfg.physics.OverlapCapsule(p.foot+Vector3.up*cfg.radius,p.foot+Vector3.up*(cfg.height-cfg.radius),cfg.radius,Overlaps,cfg.mask,QueryTriggerInteraction.Ignore);
      if(n==Overlaps.Length)throw new InvalidOperationException("Consulta fisica saturada.");
      if(!Enumerable.Range(0,n).Any(i=>Overlaps[i]==obstacle))continue;
      for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)marked.Add((Mathf.RoundToInt(y*1000),x+dx,z+dz));
     }
    }
    // This existing landing shares the rebuilt stair/railing polygons. Its
    // marginal vertices are in the affected navigation domain, not frozen world
    // geometry. Its approved route still undergoes every regression below.
    var landingBounds=FloorAt4(scene,V(45,-24,15)).bounds;landingBounds.Expand(V(2*(cfg.radius+v),2*cfg.height,2*(cfg.radius+v)));result.areas.Add(landingBounds);
    result.cells=marked.Count;PhysicalBaseline6.Clear();var outside=AutoOutside(NavMesh.CalculateTriangulation(),result.areas);CapturePhysicalSix(outside);var exterior=ExteriorPaths4(NavMesh.CalculateTriangulation());
    root=new GameObject(AutoRoot);SceneManager.MoveGameObjectToScene(root,scene);
    while(marked.Count>0)
    {
     var p=marked.OrderBy(c=>c.y).ThenBy(c=>c.z).ThenBy(c=>c.x).First();int width=1,depth=1;
     while(marked.Contains((p.y,p.x+width,p.z)))width++;
     while(Enumerable.Range(0,width).All(x=>marked.Contains((p.y,p.x+x,p.z+depth))))depth++;
     for(int x=0;x<width;x++)for(int z=0;z<depth;z++)marked.Remove((p.y,p.x+x,p.z+z));
     var go=new GameObject("Margen_fisico_"+result.volumes++);go.transform.SetParent(root.transform,false);
     float low=-(settings.agentClimb+v),high=cfg.height+v;
     go.transform.position=V((p.x+width*.5f)*v,p.y/1000f+(low+high)/2,(p.z+depth*.5f)*v);
     var volume=go.AddComponent<NavMeshModifierVolume>();volume.center=Vector3.zero;volume.size=V(width*v,high-low,depth*v);volume.area=1;
     var serialized=new SerializedObject(volume);var agents=serialized.FindProperty("m_AffectedAgents");if(agents==null)throw new InvalidOperationException("API de agentes inesperada.");agents.arraySize=1;agents.GetArrayElementAtIndex(0).intValue=0;serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    foreach(var corner in railCorners)
    {
     float half=Mathf.Ceil((cfg.radius+v)/v)*v;var go=new GameObject("Margen_esquina_"+result.volumes++);go.transform.SetParent(root.transform,false);go.transform.position=corner+Vector3.up;
     var volume=go.AddComponent<NavMeshModifierVolume>();volume.center=Vector3.zero;volume.size=V(half*2,3,half*2);volume.area=1;
     var so=new SerializedObject(volume);var agents=so.FindProperty("m_AffectedAgents");agents.arraySize=1;agents.GetArrayElementAtIndex(0).intValue=0;so.ApplyModifiedPropertiesWithoutUndo();
    }
    if(!Same4(Fingerprint4(originals,new HashSet<int>()),fingerprint))throw new InvalidOperationException("Cambios en arquitectura original.");
    var sources=(List<NavMeshBuildSource>)typeof(NavMeshSurface).GetMethod("CollectSources",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(surface,null);
    candidate=UnityEngine.Object.Instantiate(data);if(!NavMeshBuilder.UpdateNavMeshData(candidate,settings,sources,data.sourceBounds))throw new InvalidOperationException("Fallo al preparar candidata.");Swap4(surface,candidate);
    if(!AutoCoverage(outside,"Original_en_candidata")||!CompareExterior4(exterior))throw new InvalidOperationException("Cambio de navegacion fuera de los obstaculos y descanso adyacente.");
    var candidateOutside=AutoOutside(NavMesh.CalculateTriangulation(),result.areas);CapturePhysicalSix(candidateOutside);Swap4(surface,original);
    if(!AutoCoverage(candidateOutside,"Candidata_en_original"))throw new InvalidOperationException("Nueva navegacion fuera del alcance.");Swap4(surface,candidate);
    var trials=RunAutomaticRoutes(AutoWork+"/Candidata",true);result.bodyRoutes=trials.All(t=>t.navigable);
    result.regressions=AutoRegressions(AutoWork+"/RegresionesCandidata");
    if(!result.bodyRoutes||!result.regressions)throw new InvalidOperationException("Candidata no recibida: consultar rutas y regresiones. No se guarda.");
    written=true;surface.RemoveData();EditorUtility.CopySerialized(candidate,data);EditorUtility.SetDirty(data);surface.navMeshData=data;surface.AddData();AssetDatabase.SaveAssetIfDirty(data);
    if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("No se guardo escena.");
    if(!RunAutomaticRoutes(AutoWork+"/Recepcion").All(t=>t.navigable)||!AutoRegressions(AutoWork+"/RegresionesRecepcion"))throw new InvalidOperationException("Recepcion tras guardado fallida.");
    result.saved=true;
   }
   catch(Exception e)
   {
    result.error=e.ToString();result.rolledBack=true;
    if(written){surface.RemoveData();EditorUtility.CopySerialized(original,data);EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);File.Copy(AutoWork+"/"+City,City,true);File.Copy(AutoWork+"/"+Nav,Nav,true);AssetDatabase.ImportAsset(Nav,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);}
    Debug.LogException(e);
   }
   finally
   {
    if(!result.saved){surface.RemoveData();surface.navMeshData=data;surface.AddData();EditorSceneManager.OpenScene(City,OpenSceneMode.Single);}
    if(candidate)UnityEngine.Object.DestroyImmediate(candidate);UnityEngine.Object.DestroyImmediate(original);result.sceneAfter=Hash(City);result.navAfter=Hash(Nav);File.WriteAllText(AutoWork+"/Correccion.json",JsonUtility.ToJson(result,true));
   }
  }
 }
}
