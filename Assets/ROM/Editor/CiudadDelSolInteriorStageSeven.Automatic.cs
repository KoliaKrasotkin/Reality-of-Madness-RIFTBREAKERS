using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace ROM.EditorTools
{
 public static partial class CiudadDelSolInteriorStageTwo
 {
  const string AutoWork="Backups/CiudadDelSol/Interior/RutasAutomaticas_20261118";
  const string AutoRequest=AutoWork+"/Recepcion.pending";
  [Serializable] sealed class AutomaticEvidence
  {
   public string route,direction,status,started,ended;
   public float radius,height,minimumWidth=float.MaxValue,maximumSlope,maximumHorizontalProjection;
   public bool complete,navigable,accepted,filesUnchanged,preview;
   public int samples,missingNav,physicalFailures,sweepFailures,navFailures,unsafeEdges,fourLaneFailures,completedLegs;
   public Vector3[] corners;
   public List<Station> stations=new List<Station>();
   public List<Problem> problems=new List<Problem>();
   public List<RayEvidence> rays=new List<RayEvidence>();
  }
  [InitializeOnLoadMethod] static void InstallAutomaticRequest()
  {
   if(!Application.isBatchMode&&File.Exists(AutoRequest))EditorApplication.delayCall+=ConsumeAutomaticRequest;
  }
  static void ConsumeAutomaticRequest()
  {
   if(!File.Exists(AutoRequest))return;
   if(EditorApplication.isUpdating||EditorApplication.isCompiling){EditorApplication.delayCall+=ConsumeAutomaticRequest;return;}
   string id=File.ReadAllText(AutoRequest).Trim();if(!Guid.TryParse(id,out _))return;
   File.Move(AutoRequest,AutoRequest+".consumed."+id);
   try{if(File.Exists(AutoWork+"/Verificar.pending")){File.Delete(AutoWork+"/Verificar.pending");VerifySavedAutomaticRoutes();}else if(File.Exists(AutoWork+"/Corregir.pending")){File.Move(AutoWork+"/Corregir.pending",AutoWork+"/Corregir.consumed."+id);CorrectAutomaticSources();}else ReceiveAutomaticRoutes();}catch(Exception e){File.WriteAllText(AutoWork+"/Error_"+id+".txt",e.ToString());Debug.LogException(e);}
  }
  [Serializable] sealed class AutomaticFinalProof
  {
   public bool saved,reloaded,navigable,integrity;
   public string sceneHash,navHash,digest,ended;
   public int volumes;
  }
  static bool CurrentAutomaticProof()
  {
   string path=AutoWork+"/Prueba_final.json";if(!File.Exists(path))return false;
   var p=JsonUtility.FromJson<AutomaticFinalProof>(File.ReadAllText(path));
   return p.saved&&p.reloaded&&p.navigable&&p.integrity&&p.sceneHash==Hash(City)&&p.navHash==Hash(Nav)&&p.digest==TriangleDigest4();
  }
  static void VerifySavedAutomaticRoutes()
  {
   var patch=JsonUtility.FromJson<AutoPatch>(File.ReadAllText(AutoWork+"/Correccion.json"));
   if(!patch.saved||patch.sceneAfter!=Hash(City)||patch.navAfter!=Hash(Nav)||SceneManager.GetSceneByPath(City).isDirty)throw new InvalidOperationException("La correccion guardada no coincide o hay cambios sin guardar.");
   var proof=new AutomaticFinalProof{saved=true,sceneHash=Hash(City),navHash=Hash(Nav),digest=TriangleDigest4()};
   UnityEditor.SceneManagement.EditorSceneManager.OpenScene(City,UnityEditor.SceneManagement.OpenSceneMode.Single);
   proof.reloaded=proof.digest==TriangleDigest4();
   var results=RunAutomaticRoutes(AutoWork+"/Recarga");proof.navigable=results.All(r=>r.navigable&&!r.preview&&r.filesUnchanged);
   proof.integrity=proof.sceneHash==Hash(City)&&proof.navHash==Hash(Nav)&&!SceneManager.GetSceneByPath(City).isDirty;
   var root=SceneManager.GetSceneByPath(City).GetRootGameObjects().Single(g=>g.name==AutoRoot);
   var volumes=root.GetComponentsInChildren<Unity.AI.Navigation.NavMeshModifierVolume>(true);proof.volumes=volumes.Length;
   File.WriteAllLines(AutoWork+"/Volumenes.csv",new[]{"Id;X;Y;Z;Ancho;Alto;Fondo;Area"}.Concat(volumes.Select(v=>v.name+";"+string.Join(";",new[]{v.transform.position.x,v.transform.position.y,v.transform.position.z,v.size.x,v.size.y,v.size.z}.Select(n=>n.ToString("R",System.Globalization.CultureInfo.InvariantCulture)))+";"+v.area)));
   proof.ended=DateTime.UtcNow.ToString("o");File.WriteAllText(AutoWork+"/Prueba_final.json",JsonUtility.ToJson(proof,true));
   if(!proof.reloaded||!proof.navigable||!proof.integrity)throw new InvalidOperationException("Recepcion tras recarga no satisfactoria.");
   Debug.Log("CIUDAD_AUTOMATICAS_FINAL: A03 y R21 ida/vuelta Warrior y Humanoid: 8/8 navegables tras recarga. Bandas laterales y cuatro jugadores no certificados.");
  }
  static AutomaticEvidence InspectAutomaticRoute(string name,Vector3 start,Vector3 end,bool humanoid,string direction,bool preview=false)
  {
   var cfg=ConstructionConfig4(SceneManager.GetSceneByPath(City));var agent=NavMesh.GetSettingsByID(0);
   if(humanoid){cfg.radius=agent.agentRadius;cfg.height=Mathf.Max(cfg.height,agent.agentHeight);}
   var r=new AutomaticEvidence{route=name,direction=direction,radius=cfg.radius,height=cfg.height,started=DateTime.UtcNow.ToString("o"),preview=preview};
   var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};string sceneHash=Hash(City),navHash=Hash(Nav);
   if(!NavMesh.SamplePosition(start,out var a,.35f,filter)||!NavMesh.SamplePosition(end,out var b,.35f,filter)||Mathf.Abs(a.position.y-start.y)>.35f||Mathf.Abs(b.position.y-end.y)>.35f)
   {r.status="SIN_PROYECCION_LOCAL";return r;}
   var path=new NavMeshPath();r.complete=NavMesh.CalculatePath(a.position,b.position,filter,path)&&path.status==NavMeshPathStatus.PathComplete;r.corners=path.corners;r.status=path.status.ToString();
   Station previous=null;Vector3 previousNav=a.position;bool stopped=false;
   if(r.complete)for(int leg=1;leg<r.corners.Length&&!stopped;leg++)
   {
    var from=r.corners[leg-1];var to=r.corners[leg];int n=Mathf.Max(1,Mathf.CeilToInt(new Vector2(to.x-from.x,to.z-from.z).magnitude/.1f));
    var side=Vector3.Cross(Vector3.up,(to-from).normalized).normalized;
    for(int j=leg==1?0:1;j<=n;j++)
    {
     // Preserve the native XZ corridor. Follow the connected local NavMesh
     // height instead of borrowing Y from a different documentary itinerary.
     var query=Vector3.Lerp(from,to,(float)j/n);query.y=previousNav.y;
     if(j==0)query=from;
     if(!NavMesh.SamplePosition(query,out var hit,.35f,filter))
     {r.missingNav++;Add(r.problems,Issue("SIN_SUPERFICIE_LOCAL_AUTOMATICA",query,null));stopped=true;break;}
     r.maximumHorizontalProjection=Mathf.Max(r.maximumHorizontalProjection,new Vector2(query.x-hit.position.x,query.z-hit.position.z).magnitude);
     if(j==n&&Vector3.Distance(hit.position,to)>.35f)
     {r.navFailures++;Add(r.problems,Issue("NO_SE_ALCANZA_ESQUINA_NATIVA_EN_SU_PLANTA",hit.position,null));stopped=true;break;}
     var s=Probe(hit.position,cfg,false,.35f);s.rawPathPoint=Vector3.Lerp(from,to,(float)j/n);r.stations.Add(s);r.samples++;
     if(!s.Clear){r.physicalFailures++;Add(r.problems,s.problem);}
     if(!s.nav)r.missingNav++;
     if(previous!=null)
     {
      if(!Sweep(previous,s,cfg,previous.gradedSupport||s.gradedSupport,out var p,out _)){r.sweepFailures++;Add(r.problems,p);}
      if(previous.nav&&s.nav&&Vector3.Distance(previous.navPosition,s.navPosition)>.00001f)
      {
       bool ca=r.corners.Any(c=>Vector3.Distance(c,previous.navPosition)<=.001f),cb=r.corners.Any(c=>Vector3.Distance(c,s.navPosition)<=.001f);
       if(!Connected(previous.navPosition,s.navPosition,cfg,ca,cb,out var proof))r.navFailures++;
       if(proof.rawBlocked)r.rays.Add(proof);
      }
     }
     if(s.Clear&&j%10==0)
     {
      var width=MeasureWidth(s,side,cfg,s.gradedSupport);r.minimumWidth=Mathf.Min(r.minimumWidth,width.usefulWidthLowerBound);
      if(!width.fourStaticCapsulesClear)r.fourLaneFailures++;
      if(width.leftUnsupportedUnprotected||width.rightUnsupportedUnprotected)r.unsafeEdges++;
     }
     r.maximumSlope=Mathf.Max(r.maximumSlope,s.slope);previous=s;previousNav=hit.position;
    }
    if(!stopped)r.completedLegs++;
   }
   r.filesUnchanged=sceneHash==Hash(City)&&navHash==Hash(Nav)&&!SceneManager.GetSceneByPath(City).isDirty;
   r.navigable=r.complete&&!stopped&&r.completedLegs==r.corners.Length-1&&r.samples>0&&r.missingNav==0&&r.physicalFailures==0&&r.sweepFailures==0&&r.navFailures==0&&(r.filesUnchanged||preview);
   r.accepted=r.navigable&&r.unsafeEdges==0&&(!humanoid?r.fourLaneFailures==0:true);
   r.ended=DateTime.UtcNow.ToString("o");return r;
  }
  [MenuItem("ROM/Etapa 7 interior/Comprobar automaticas A03 R21")]
  public static void ReceiveAutomaticRoutes()
  {
   RunAutomaticRoutes(AutoWork);
  }
  static List<AutomaticEvidence> RunAutomaticRoutes(string output,bool preview=false)
  {
   var scene=SceneManager.GetSceneByPath(City);
   if(EditorApplication.isPlayingOrWillChangePlaymode||!scene.isLoaded||(scene.isDirty&&!preview))throw new InvalidOperationException("Requiere Ciudad del Sol limpia en Edit Mode.");
   Directory.CreateDirectory(output);var results=new List<AutomaticEvidence>();
   foreach(var item in new[]{(id:"A03-P01",start:V(156,-16,99)),(id:"R21-P01",start:V(117,-16,123))})
    foreach(bool humanoid in new[]{false,true})foreach(bool reverse in new[]{false,true})
    {
     var r=InspectAutomaticRoute(item.id,reverse?V(9,-38,15):item.start,reverse?item.start:V(9,-38,15),humanoid,reverse?"vuelta":"ida",preview);results.Add(r);
     File.WriteAllText(output+"/"+item.id+"_"+(humanoid?"Humanoid":"Warrior")+"_"+r.direction+".json",JsonUtility.ToJson(r,true));
    }
   File.WriteAllLines(output+"/Resultado.txt",results.Select(r=>r.route+" "+r.radius+" "+r.direction+" navigable="+r.navigable+" accepted="+r.accepted+" samples="+r.samples+" physical="+r.physicalFailures+" sweeps="+r.sweepFailures+" nav="+r.navFailures+" missing="+r.missingNav+" edges="+r.unsafeEdges+" four="+r.fourLaneFailures));
   Debug.Log("CIUDAD_INTERIOR_AUTOMATICAS: capsulas navegables="+results.Count(r=>r.navigable)+"/"+results.Count+"; bandas laterales completas="+results.Count(r=>r.accepted)+"/"+results.Count+"; cuatro jugadores reales pendientes. "+Path.GetFullPath(output));
   return results;
  }
 }
}
