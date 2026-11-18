using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ROM.EditorTools
{
 public static partial class CiudadDelSolInteriorStageTwo
 {
  const string Work7="Backups/CiudadDelSol/Interior/Etapa7_20261118";
  const string Request7=Work7+"/Recepcion.pending";
  [InitializeOnLoadMethod] static void InstallStageSevenRequest()
  {
   if(!Application.isBatchMode&&File.Exists(Request7))EditorApplication.delayCall+=ConsumeStageSevenRequest;
  }
  static void ConsumeStageSevenRequest()
  {
   if(!File.Exists(Request7))return;
   if(EditorApplication.isUpdating||EditorApplication.isCompiling){EditorApplication.delayCall+=ConsumeStageSevenRequest;return;}
   string id=File.ReadAllText(Request7).Trim();if(!Guid.TryParse(id,out _))return;
   File.Move(Request7,Request7+".consumed."+id);
   try{ReceiveStageSeven();}catch(Exception e){File.WriteAllText(Work7+"/Error_"+id+".txt",e.ToString());Debug.LogException(e);}
  }
  static Plan StageSevenPlan()=>P("VS06","Acceso D03","P01 (acceso, no arena)",V(18,-38,9),V(12,-38,9),V(9,-38,15));
  // Join only identical, physically documented anchors. No invented connectors.
  static Plan JoinSeven(string id,params Plan[] parts)
  {
   var points=new List<Vector3>();
   foreach(var part in parts)
   {
    if(points.Count>0&&Vector3.Distance(points.Last(),part.points[0])>.001f)throw new InvalidOperationException("Junta sin continuidad documental en "+id+": "+points.Last()+" -> "+part.points[0]);
    foreach(var p in part.points)if(points.Count==0||Vector3.Distance(points.Last(),p)>.00001f)points.Add(p);
   }
   return P(id,parts[0].from,parts.Last().to,points.ToArray());
  }
  static Plan[] ChainsSeven()
  {
   var entry=StageFourNorthEntry();
   int junction=Array.FindIndex(entry.points,p=>Vector3.Distance(p,V(120,-16,117))<.001f);
   if(junction<0)throw new InvalidOperationException("No existe el ancla comun R21/VS01.");
   var r21=P("R21-union","R21","Union VS01",V(117,-16,123),V(120,-16,117));
   var fromUnion=P("VS01-desde-union","Union VS01","B01",entry.points.Skip(junction).ToArray());
   var bridge=StageFivePlans()[1];var returnBridge=P("VS05-regreso","B04 sur","B02",bridge.points.Reverse().ToArray());
   return new[]{
    JoinSeven("CONT-A03-P01",entry,StageFourPlans()[1],StageSixPlan(),StageSevenPlan()),
    JoinSeven("CONT-R21-P01",r21,fromUnion,StageFourPlans()[1],StageSixPlan(),StageSevenPlan()),
    JoinSeven("CONT-B04-P01",returnBridge,StageSixPlan(),StageSevenPlan())
   };
  }
  [Serializable] sealed class DenseSeven
  {
   public int stations,failures,agentFailures;
   public float minimumWidth=float.MaxValue,minimumHeight=float.MaxValue,maximumSlope;
   public List<string> problems=new List<string>();
  }
  static bool DenseReceptionSeven()
  {
   var cfg=ConstructionConfig4(SceneManager.GetSceneByPath(City));var p=StageSevenPlan();var r=new DenseSeven();var agent=NavMesh.GetSettingsByID(0);
   var agentCfg=ConstructionConfig4(cfg.scene);agentCfg.radius=agent.agentRadius;agentCfg.height=Mathf.Max(agent.agentHeight,cfg.height);
   for(int i=1;i<p.points.Length;i++)
   {
    var a=p.points[i-1];var b=p.points[i];var side=Vector3.Cross(Vector3.up,(b-a).normalized).normalized;
    int n=Mathf.CeilToInt(Vector3.Distance(a,b)/.1f);Station previous=null,previousAgent=null;
    for(int j=0;j<=n;j++)
    {
     var q=Vector3.Lerp(a,b,(float)j/n);var s=Probe(q,cfg,false,.03f);var w=MeasureWidth(s,side,cfg,false);r.stations++;
     bool sweep=previous==null||Sweep(previous,s,cfg,false,out _,out _);
     if(!s.Clear||!s.nav||!sweep||!w.fourStaticCapsulesClear||w.leftUnsupportedUnprotected||w.rightUnsupportedUnprotected)
     {r.failures++;r.problems.Add(JsonUtility.ToJson(s)+" width="+JsonUtility.ToJson(w));}
     var ai=Probe(q,agentCfg,false,.03f);bool agentSweep=previousAgent==null||Sweep(previousAgent,ai,agentCfg,false,out _,out _);
     if(!ai.Clear||!ai.nav||!agentSweep)r.agentFailures++;
     r.minimumWidth=Mathf.Min(r.minimumWidth,w.usefulWidthLowerBound);r.maximumSlope=Mathf.Max(r.maximumSlope,s.slope);
     foreach(var offset in new[]{Vector3.zero,Vector3.right*cfg.radius,Vector3.left*cfg.radius,Vector3.forward*cfg.radius,Vector3.back*cfg.radius})
      if(cfg.physics.Raycast(s.floor+offset+Vector3.up*.05f,Vector3.up,out var h,50,cfg.mask,QueryTriggerInteraction.Ignore))r.minimumHeight=Mathf.Min(r.minimumHeight,h.distance+.05f);
     previous=s;previousAgent=ai;
    }
   }
   File.WriteAllText(Work7+"/Perfil_denso.json",JsonUtility.ToJson(r,true));return r.failures==0&&r.agentFailures==0;
  }
  static bool AgentEvidenceSeven(Report report,string folder)
  {
   var cfg=ConstructionConfig4(SceneManager.GetSceneByPath(City));var a=NavMesh.GetSettingsByID(0);cfg.radius=a.agentRadius;cfg.height=Mathf.Max(a.agentHeight,cfg.height);
   bool pass=true;Directory.CreateDirectory(folder);
   foreach(var c in report.connections)foreach(var pair in new[]{(name:"ida",route:c.forward),(name:"vuelta",route:c.reverse)})
   {
    var trace=InspectTrace("Humanoid "+c.plan.id+" "+pair.name,pair.route.corners,c.plan,cfg,false);
    File.WriteAllText(folder+"/"+c.plan.id+"_"+pair.name+".json",JsonUtility.ToJson(trace,true));pass&=pair.route.complete&&trace.clear&&trace.missingNav==0;
   }
   return pass;
  }
  [MenuItem("ROM/Etapa 7 interior/Recibir VS06 y continuidad")]
  public static void ReceiveStageSeven()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("Requiere Edit Mode y escenas limpias; no se descartan cambios.");
   if(!SceneManager.GetSceneByPath(City).isLoaded)EditorSceneManager.OpenScene(City,OpenSceneMode.Single);
   Directory.CreateDirectory(Work7);string before=Hash(City),nav=Hash(Nav),population=Hash(Population);
   var single=InspectSelected(new[]{StageSevenPlan()},Work7+"/VS06","Etapa 7 VS06",false,false);
   bool dense=DenseReceptionSeven();var chains=ChainsSeven();
   var joined=InspectSelected(chains,Work7+"/ContinuidadSeleccionada","Etapa 7 continuidad encadenada",false,true);
   var natural=InspectSelected(chains,Work7+"/ContinuidadGlobal","Etapa 7 recorridos globales exploratorios",false,false);
   bool unforcedNatural=Raw4(natural,3)&&natural.connections.All(c=>!c.forward.viaOriginalWaypoints&&!c.reverse.viaOriginalWaypoints);
   bool agentSingle=AgentEvidenceSeven(single,Work7+"/HumanoidVS06"),agentChains=AgentEvidenceSeven(joined,Work7+"/HumanoidContinuidad");
   bool regressions=ReceiveAllSix(Work7+"/Regresiones",false)&&CheckHumanoidSix(Work7+"/Regresiones");
   var proof=JsonUtility.FromJson<Proof4>(File.ReadAllText(Work6+"/Prueba_navegacion.json"));bool loadedData=proof.digest==TriangleDigest4();
   bool unchanged=before==Hash(City)&&nav==Hash(Nav)&&population==Hash(Population)&&!SceneManager.GetSceneByPath(City).isDirty;
   bool pass=Raw4(single,1)&&dense&&Raw4(joined,3)&&agentSingle&&agentChains&&regressions&&loadedData&&unchanged;
   File.WriteAllText(Work7+"/Resultado.txt","VS06="+Raw4(single,1)+"; Denso="+dense+"; Continuidad="+Raw4(joined,3)+"; GlobalExploratoria="+unforcedNatural+"; HumanoidVS06="+agentSingle+"; HumanoidContinuidad="+agentChains+"; Regresiones1a6="+regressions+"; DatosRecargados="+loadedData+"; Integridad="+unchanged+"; RecepcionEtapa7="+pass);
   Debug.Log("CIUDAD_INTERIOR_ETAPA7_FINAL: VS06="+Raw4(single,1)+" continuidad="+Raw4(joined,3)+" recepcion="+pass+"; cuatro jugadores reales pendientes. "+Path.GetFullPath(Work7));
   ShowSevenView(0);
  }
  static int view7;
  static readonly (string title,Vector3 eye,Vector3 target)[] Views7={
   ("VS06 - conjunto de acceso",V(7,-33,4),V(12,-38,12)),
   ("VS03 - junta compartida",V(24,-35,5),V(18,-38,9)),
   ("VS06 - giro y llegada",V(4,-35,20),V(9,-38,15)),
   ("VS06 - vuelta hacia VS03",V(9,-35,19),V(15,-38,9))
  };
  static void ShowSevenView(int index)
  {
   view7=(index+Views7.Length)%Views7.Length;var scene=SceneManager.GetSceneByPath(City);
   SceneVisibilityManager.instance.Isolate(scene.GetRootGameObjects().Where(g=>g.name=="Nucleo_Monumental_Conjunto"||g.name==BuildRoot4||g.name==Root6).ToArray(),true);
   Selection.objects=Array.Empty<UnityEngine.Object>();var v=SceneView.lastActiveSceneView??EditorWindow.GetWindow<SceneView>();var shot=Views7[view7];
   v.cameraSettings.fieldOfView=60;v.drawGizmos=false;v.LookAtDirect(shot.target,Quaternion.LookRotation(shot.target-shot.eye),Vector3.Distance(shot.eye,shot.target)*.5f);
   SceneView.duringSceneGui-=ToolbarSeven;SceneView.duringSceneGui+=ToolbarSeven;v.Focus();v.Repaint();
  }
  static void ToolbarSeven(SceneView view)
  {
   Handles.BeginGUI();GUILayout.BeginArea(new Rect(60,42,870,60),EditorStyles.helpBox);GUILayout.BeginHorizontal();
   if(GUILayout.Button("Anterior",GUILayout.Width(90)))ShowSevenView(view7-1);
   if(GUILayout.Button("Siguiente",GUILayout.Width(90)))ShowSevenView(view7+1);
   if(GUILayout.Button("Restaurar visibilidad",GUILayout.Width(160))){SceneVisibilityManager.instance.ExitIsolation();SceneView.duringSceneGui-=ToolbarSeven;SceneView.RepaintAll();}
   GUILayout.Label(Views7[view7].title);GUILayout.EndHorizontal();GUILayout.EndArea();Handles.EndGUI();
  }
 }
}
