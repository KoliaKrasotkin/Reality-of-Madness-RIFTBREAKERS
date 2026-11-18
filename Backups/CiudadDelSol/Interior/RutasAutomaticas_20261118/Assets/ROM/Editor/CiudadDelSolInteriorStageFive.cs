using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolInteriorStageTwo
    {
        const string Work5="Backups/CiudadDelSol/Interior/Etapa5_20261118";
        const string Request5=Work5+"/Recepcion.pending";
        static double deadline5;
        [InitializeOnLoadMethod]
        static void InstallRequest5()
        {
            if(Application.isBatchMode||!File.Exists(Request5))return;
            deadline5=EditorApplication.timeSinceStartup+120;
            EditorApplication.update-=RunRequest5;EditorApplication.update+=RunRequest5;
        }
        static void RunRequest5()
        {
            if(!File.Exists(Request5)){EditorApplication.update-=RunRequest5;return;}
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            EditorApplication.update-=RunRequest5;
            var nonce=File.ReadAllText(Request5).Trim();if(!Guid.TryParse(nonce,out _))return;
            File.Move(Request5,Request5+".consumed."+nonce);
            try{if(EditorApplication.timeSinceStartup>deadline5)throw new TimeoutException("Recepción no iniciada a tiempo.");InspectStageFive();}
            catch(Exception e){File.WriteAllText(Work5+"/Error_solicitud.txt",e.ToString());Debug.LogException(e);}
        }
        static Plan[] StageFivePlans()=>new[]{
            P("VS04","B01","B03",V(51,-24,39),V(45,-24,39),V(18,-24,39),V(-15,-24,39)),
            P("VS05","B02","B04",V(24,-30,45),V(21,-30,45),V(21,-30,39),V(21,-30,-3),V(21,-30,-9))
        };
        public static void InspectStageFiveBatch()=>InspectStageFive();
        [MenuItem("ROM/Etapa 5 interior/Recibir VS04 VS05 sin obras")]
        static void InspectStageFive()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Recepción exclusiva Edit Mode.");
            if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("No se inspeccionan ni sustituyen escenas con cambios pendientes.");
            if(!SceneManager.GetSceneByPath(City).isLoaded)EditorSceneManager.OpenScene(City,OpenSceneMode.Single);
            Directory.CreateDirectory(Work5);
            var r=InspectSelected(StageFivePlans(),Work5+"/Inicial","Etapa 5 - inicial",false,true);
            File.WriteAllText(Work5+"/Estado.txt",r.connections.Count(c=>c.technicalIndividual)+"/2 conexiones recibidas; no se guardó escena ni navegación.");
            File.WriteAllText(Work5+"/RegresionesIniciales.txt",ConstructionReception4(Work5+"/RegresionesIniciales",false).ToString());
            var direct=InspectSelected(StageFivePlans(),Work5+"/GlobalDirecta","Etapa 5 - caminos globales sin selección forzada",false,false);
            var dense=InspectDenseStageFive();
            File.WriteAllText(Work5+"/Resultado.txt",(Raw4(r,2)&&Raw4(direct,2)&&dense).ToString());
            ShowStageFiveView(0);
        }
        [Serializable] sealed class Dense5
        {
            public string id;public int stations,failures;public float minimumWidth=float.MaxValue,maximumSlope,minimumOverhead=float.MaxValue;
            public List<string> problems=new List<string>();
        }
        static bool InspectDenseStageFive()
        {
            var cfg=ConstructionConfig4(SceneManager.GetSceneByPath(City));var list=new List<Dense5>();
            foreach(var p in StageFivePlans())
            {
                var row=new Dense5{id=p.id};list.Add(row);
                for(int i=1;i<p.points.Length;i++)
                {
                    var a=p.points[i-1];var b=p.points[i];int n=Mathf.CeilToInt(Vector3.Distance(a,b)/.1f);
                    var side=Vector3.Cross(Vector3.up,(b-a).normalized).normalized;
                    for(int j=0;j<=n;j++)
                    {
                        var q=Vector3.Lerp(a,b,(float)j/n);bool graded=NearGrade(q,p,cfg.radius+.25f);
                        var s=Probe(q,cfg,graded,graded?cfg.step:.03f);row.stations++;row.maximumSlope=Mathf.Max(row.maximumSlope,s.slope);
                        var w=MeasureWidth(s,side,cfg,graded);row.minimumWidth=Mathf.Min(row.minimumWidth,w.usefulWidthLowerBound);
                        if(!s.Clear||!w.fourStaticCapsulesClear||w.leftUnsupportedUnprotected||w.rightUnsupportedUnprotected)
                        {row.failures++;row.problems.Add(q.ToString("F4")+" "+JsonUtility.ToJson(s.problem)+" "+JsonUtility.ToJson(w));}
                        if(s.support&&cfg.physics.Raycast(s.floor+Vector3.up*.05f,Vector3.up,out var hit,20,cfg.mask,QueryTriggerInteraction.Ignore))row.minimumOverhead=Mathf.Min(row.minimumOverhead,hit.distance+.05f);
                    }
                }
            }
            File.WriteAllLines(Work5+"/Perfil_denso.jsonl",list.Select(x=>JsonUtility.ToJson(x)));
            return list.All(x=>x.failures==0);
        }
        static int view5Index;
        static readonly (string name,Vector3 eye,Vector3 target)[] Views5={
            ("VS04 - conjunto",new Vector3(18,-19,52),new Vector3(18,-24,39)),
            ("VS04 - acceso este",new Vector3(43,-21,47),new Vector3(48,-24,39)),
            ("VS04 - tramo central",new Vector3(27,-21,33),new Vector3(18,-24,39)),
            ("VS04 - acceso oeste",new Vector3(-12,-21,31),new Vector3(-15,-24,39)),
            ("VS05 - enlace Etapa 4",new Vector3(30,-27,48),new Vector3(22,-30,44)),
            ("Cruce a distinta altura",new Vector3(29,-27,43),new Vector3(21,-27,39)),
            ("VS05 - tramo central",new Vector3(32,-27,18),new Vector3(21,-30,18)),
            ("VS05 - acceso sur",new Vector3(30,-27,-6),new Vector3(21,-30,-6)),
            ("VS05 - contracara",new Vector3(11,-27,18),new Vector3(21,-30,18))
        };
        [MenuItem("ROM/Etapa 5 interior/Inspección visual")]
        static void VisualStageFive()=>ShowStageFiveView(0);
        static void ShowStageFiveView(int index)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Inspección exclusiva Edit Mode.");
            view5Index=(index+Views5.Length)%Views5.Length;
            var scene=SceneManager.GetSceneByPath(City);if(!scene.isLoaded)throw new InvalidOperationException("Abrir Ciudad del Sol.");
            var roots=scene.GetRootGameObjects().Where(g=>g.name=="Nucleo_Monumental_Conjunto"||g.name==BuildRoot4).ToArray();
            SceneVisibilityManager.instance.Isolate(roots,true);Selection.objects=Array.Empty<UnityEngine.Object>();
            var v=SceneView.lastActiveSceneView??EditorWindow.GetWindow<SceneView>();var s=Views5[view5Index];
            v.cameraSettings.fieldOfView=60;v.drawGizmos=false;v.LookAtDirect(s.target,Quaternion.LookRotation(s.target-s.eye),Vector3.Distance(s.eye,s.target)*.5f);
            SceneView.duringSceneGui-=StageFiveToolbar;SceneView.duringSceneGui+=StageFiveToolbar;v.Focus();v.Repaint();
            File.AppendAllText(Work5+"/Encuadres.txt",DateTime.UtcNow.ToString("o")+" "+s.name+" eye="+s.eye+" target="+s.target+"\n");
        }
        static void StageFiveToolbar(SceneView view)
        {
            Handles.BeginGUI();GUILayout.BeginArea(new Rect(60,42,850,65),EditorStyles.helpBox);GUILayout.BeginHorizontal();
            if(GUILayout.Button("Anterior",GUILayout.Width(90)))ShowStageFiveView(view5Index-1);
            if(GUILayout.Button("Siguiente",GUILayout.Width(90)))ShowStageFiveView(view5Index+1);
            if(GUILayout.Button("Restaurar visibilidad",GUILayout.Width(160)))RestoreStageFiveVisibility();
            GUILayout.Label(Views5[view5Index].name);GUILayout.EndHorizontal();
            GUILayout.Label("Cámara real: "+view.camera.transform.position.ToString("F3")+" · pivote: "+view.pivot.ToString("F3"));GUILayout.EndArea();Handles.EndGUI();
        }
        [MenuItem("ROM/Etapa 5 interior/Restaurar visibilidad")]
        static void RestoreStageFiveVisibility(){SceneVisibilityManager.instance.ExitIsolation();SceneView.duringSceneGui-=StageFiveToolbar;SceneView.RepaintAll();}
    }
}
