using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolInteriorStageTwo
    {
        const string Visual4Output="Backups/CiudadDelSol/Interior/AuditoriaVisualEtapa4_20261118";
        static int visual4Index=-1;
        static bool visual4Context;
        static readonly (string name,Vector3 eye,Vector3 target)[] Visual4Views={
            ("01 Acceso superior",new Vector3(80,-5,-2),new Vector3(63,-16,-3)),
            ("02 Giro superior",new Vector3(56,-6,-35),new Vector3(52,-16,-15)),
            ("03 VS01 vuelos 1-2",new Vector3(64,-8,-3),new Vector3(45,-18,-6)),
            ("04 VS01 vuelos 3-4",new Vector3(64,-12,9),new Vector3(45,-22,6)),
            ("05 Descanso B01",new Vector3(61,-14,13),new Vector3(48,-24,17)),
            ("06 Galeria sur",new Vector3(68,-14,24),new Vector3(52,-24,24)),
            ("07 Galeria norte",new Vector3(68,-14,38),new Vector3(52,-24,38)),
            ("08 Giro VS02",new Vector3(62,-13,56),new Vector3(46,-24,44)),
            ("09 VS02 vuelos 1-2",new Vector3(35,-15,62),new Vector3(36,-26,45)),
            ("10 VS02 vuelo 3",new Vector3(27,-17,62),new Vector3(27,-29,45)),
            ("11 Llegada B02",new Vector3(16,-20,60),new Vector3(24,-30,45)),
            ("12 VS01 contracara",new Vector3(27,-12,-2),new Vector3(45,-20,0)),
            ("13 VS02 contracara",new Vector3(34,-21,27),new Vector3(34,-27,45)),
            ("14 Atajo inverso",new Vector3(54,-12,-8),new Vector3(48.557f,-16,-12.257f))
        };
        [MenuItem("ROM/Etapa 4 visual/Siguiente encuadre")]
        static void NextVisual4(){visual4Index=(visual4Index+1)%Visual4Views.Length;ShowVisual4();}
        [MenuItem("ROM/Etapa 4 visual/Encuadre anterior")]
        static void PreviousVisual4(){visual4Index=(visual4Index+Visual4Views.Length-1)%Visual4Views.Length;ShowVisual4();}
        static void ShowVisual4()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Inspección exclusiva Edit Mode.");
            var scene=SceneManager.GetSceneByPath(City);if(!scene.isLoaded)throw new InvalidOperationException("Abrir la escena original antes de inspeccionar.");
            var roots=scene.GetRootGameObjects().Where(g=>g.name=="Nucleo_Monumental_Conjunto"||g.name==BuildRoot4).ToArray();
            if(roots.Length!=2)throw new InvalidOperationException("Faltan raíces de la intervención; no se altera la visibilidad.");
            if(visual4Context)SceneVisibilityManager.instance.Isolate(roots,true);
            else
            {
                var build=JsonUtility.FromJson<Build4>(File.ReadAllText(Work4+"/Construccion.json"));
                var routePlans=new[]{StageFourNorthEntry(),StageFourPlans()[1]};
                var selected=new System.Collections.Generic.List<GameObject>{roots.Single(g=>g.name==BuildRoot4)};
                foreach(var change in build.changes)
                    if(GlobalObjectId.TryParse(change.id,out var id)&&GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is Collider c)selected.Add(c.gameObject);
                selected.AddRange(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true))
                    // Display-only cutaway: exclude floors from unrelated overlapping
                    // storeys. This radius never participates in route acceptance.
                    .Where(c=>c.enabled&&c.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)&&c.bounds.Intersects(Patch4)&&c.bounds.max.y>=-30.03f&&c.bounds.max.y<=-15.97f
                        &&routePlans.Any(p=>Enumerable.Range(1,p.points.Length-1).Any(i=>DistanceSegment(new Vector3(c.bounds.center.x,c.bounds.max.y,c.bounds.center.z),p.points[i-1],p.points[i])<=4.5f))).Select(c=>c.gameObject));
                SceneVisibilityManager.instance.Isolate(selected.Distinct().ToArray(),true);
            }
            SceneView.duringSceneGui-=Visual4Toolbar;SceneView.duringSceneGui+=Visual4Toolbar;
            Selection.objects=Array.Empty<UnityEngine.Object>();
            var view=SceneView.lastActiveSceneView??EditorWindow.GetWindow<SceneView>();var shot=Visual4Views[visual4Index];
            view.cameraSettings.fieldOfView=60;view.drawGizmos=false;
            view.LookAtDirect(shot.target,Quaternion.LookRotation(shot.target-shot.eye),Vector3.Distance(shot.eye,shot.target)*.5f);
            view.Focus();view.Repaint();view.ShowNotification(new GUIContent(shot.name),2);
            Directory.CreateDirectory(Visual4Output);File.AppendAllText(Visual4Output+"/Encuadres.txt",DateTime.UtcNow.ToString("o")+" "+shot.name+" eye="+shot.eye+" target="+shot.target+"\n");
        }
        static void Visual4Toolbar(SceneView view)
        {
            Handles.BeginGUI();GUILayout.BeginArea(new Rect(60,42,950,65),EditorStyles.helpBox);GUILayout.BeginHorizontal();
            if(GUILayout.Button("Anterior",GUILayout.Width(80)))PreviousVisual4();
            if(GUILayout.Button("Siguiente",GUILayout.Width(80)))NextVisual4();
            if(GUILayout.Button(visual4Context?"Aislar piezas":"Ver contexto",GUILayout.Width(100))){visual4Context=!visual4Context;ShowVisual4();}
            if(GUILayout.Button("Atajo: prueba nativa",GUILayout.Width(150)))InspectShortcut4();
            if(GUILayout.Button("Restaurar visibilidad",GUILayout.Width(150)))RestoreVisual4();
            GUILayout.Label(visual4Index>=0?Visual4Views[visual4Index].name:"");GUILayout.EndHorizontal();
            GUILayout.Label("Cámara real: "+view.camera.transform.position.ToString("F3")+" · pivote: "+view.pivot.ToString("F3"));
            GUILayout.EndArea();Handles.EndGUI();
        }
        [MenuItem("ROM/Etapa 4 visual/Restaurar visibilidad")]
        static void RestoreVisual4(){SceneVisibilityManager.instance.ExitIsolation();SceneView.duringSceneGui-=Visual4Toolbar;SceneView.RepaintAll();}
        [MenuItem("ROM/Etapa 4 visual/Diagnosticar atajo sin modificar escena")]
        static void InspectShortcut4()
        {
            var r=InspectSelected(new[]{StageFourNorthEntry()},Visual4Output+"/Atajo","Atajo global VS01",false);
            var cfg=ConstructionConfig4(SceneManager.GetSceneByPath(City));
            var lines=new System.Collections.Generic.List<string>();
            foreach(var route in new[]{r.connections[0].forward,r.connections[0].reverse})
            {
                var stations=route.trace.stations;
                for(int i=1;i<stations.Count;i++)
                {
                    var a=stations[i-1];var b=stations[i];if(!a.nav||!b.nav)continue;
                    if(!Connected(a.navPosition,b.navPosition,cfg,false,false,out var proof))
                    {
                        var evidence=new Shortcut4{a=a,b=b,evidence=proof};var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
                        evidence.directBlocked=NavMesh.Raycast(a.navPosition,b.navPosition,out var direct,filter);evidence.directHit=direct.position;
                        evidence.reverseBlocked=NavMesh.Raycast(b.navPosition,a.navPosition,out var reverse,filter);evidence.reverseHit=reverse.position;
                        var local=new NavMeshPath();evidence.localComplete=NavMesh.CalculatePath(a.navPosition,b.navPosition,filter,local)&&local.status==NavMeshPathStatus.PathComplete;
                        evidence.localCorners=local.corners;if(evidence.localComplete)evidence.localTrace=InspectTrace("Atajo: ruta local nativa",local.corners,StageFourNorthEntry(),cfg,false);
                        var back=new NavMeshPath();evidence.backComplete=NavMesh.CalculatePath(b.navPosition,a.navPosition,filter,back)&&back.status==NavMeshPathStatus.PathComplete;
                        evidence.backCorners=back.corners;if(evidence.backComplete)evidence.backTrace=InspectTrace("Atajo: ruta local inversa nativa",back.corners,StageFourNorthEntry(),cfg,false);
                        if(NavMesh.FindClosestEdge(a.navPosition,out var edgeA,filter))evidence.edgeA=edgeA.distance;
                        if(NavMesh.FindClosestEdge(b.navPosition,out var edgeB,filter))evidence.edgeB=edgeB.distance;
                        evidence.rawProjected=NavMesh.SamplePosition(a.rawPathPoint,out var na,.35f,filter)&&NavMesh.SamplePosition(b.rawPathPoint,out var nb,.35f,filter);
                        if(evidence.rawProjected&&NavMesh.SamplePosition(a.rawPathPoint,out na,.35f,filter)&&NavMesh.SamplePosition(b.rawPathPoint,out nb,.35f,filter))
                        {evidence.rawA=na.position;evidence.rawB=nb.position;evidence.rawConnected=Connected(na.position,nb.position,cfg,false,false,out evidence.rawEvidence);}
                        lines.Add(JsonUtility.ToJson(evidence));
                    }
                }
            }
            File.WriteAllLines(Visual4Output+"/Atajo_detalle.jsonl",lines);
            File.WriteAllText(Visual4Output+"/Regresiones.txt",ConstructionReception4(Visual4Output+"/Regresiones",false).ToString());
        }
        [Serializable] sealed class Shortcut4
        {
            public Station a,b;public RayEvidence evidence,rawEvidence;
            public bool rawProjected,rawConnected,directBlocked,reverseBlocked,localComplete,backComplete;
            public Vector3 rawA,rawB,directHit,reverseHit;public Vector3[] localCorners,backCorners;
            public Trace localTrace,backTrace;public float edgeA,edgeB;
        }
    }
}
