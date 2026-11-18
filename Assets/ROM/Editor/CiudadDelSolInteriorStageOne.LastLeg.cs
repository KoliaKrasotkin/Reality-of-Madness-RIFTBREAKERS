using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirror;
using MultiFPS.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolInteriorStageOne
    {
        const string LastOutput="Backups/CiudadDelSol/Interior/AutoLastLeg_20261117";
        const string WalkSource="Backups/CiudadDelSol/Interior/WarriorWalk_20261117T184352415Z/Recorrido_Warriors.json";
        const string ExteriorData="Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset";
        const string PopulationData="Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset";
        const string LastRequest=LastOutput+"/Solicitud_una_ejecucion.pending";
        static double requestDeadline;
        [InitializeOnLoadMethod]
        static void InstallExplicitLastLegRequest()
        {
            // Not a game bootstrap: absent an explicit file request this does nothing.
            if(!File.Exists(LastRequest))return;
            requestDeadline=EditorApplication.timeSinceStartup+90;
            EditorApplication.update-=RunExplicitLastLegRequest;
            EditorApplication.update+=RunExplicitLastLegRequest;
        }
        static void RunExplicitLastLegRequest()
        {
            if(!File.Exists(LastRequest)){EditorApplication.update-=RunExplicitLastLegRequest;return;}
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            EditorApplication.update-=RunExplicitLastLegRequest;
            string nonce=File.ReadAllText(LastRequest).Trim();
            if(!Guid.TryParse(nonce,out _))return;
            File.Move(LastRequest,LastRequest+".consumed."+nonce);
            try
            {
                if(EditorApplication.timeSinceStartup>requestDeadline)throw new TimeoutException("Solicitud de diagnóstico superó 90 s antes de ejecutarse.");
                InspectLastLegOpenScene();
            }
            catch(Exception e){File.WriteAllText(Path.Combine(LastOutput,"Error_solicitud.txt"),e.ToString());Debug.LogException(e);}
        }
        [Serializable] public sealed class SupportProbe
        {
            public string name, supportCollider, supportHierarchy;
            public Vector3 requested, floor, normal, navPoint, navEdge, navEdgeNormal;
            public bool support, capsuleClear, nav, hasNavEdge;
            public float slope, edgeDistance=-1, supportMarginLowerBound, supportSearchLimit=1.5f;
            public Issue issue;
        }
        [Serializable] public sealed class EdgeProfile
        {
            public double originalSeconds;
            public float originalEdgeDistance;
            public SupportProbe original;
            public List<SupportProbe> acrossBoundary=new List<SupportProbe>();
        }
        [Serializable] public sealed class SegmentProbe
        {
            public string name;
            public Vector3 start,end;
            public bool raycastForwardBlocked,raycastReverseBlocked,projection,clear;
            public Vector3 raycastForwardHit,raycastReverseHit;
            public RouteResult route;
            public List<SupportProbe> samples=new List<SupportProbe>();
        }
        [Serializable] public sealed class LastReport
        {
            public string startedUTC,endedUTC,unity,error,result,sourceSHA256,sceneBefore,sceneAfter,navBefore,navAfter,populationBefore,populationAfter;
            public bool playMode,hostActive,ownerFound,ownerIsWarrior,ownerControllerEnabled,filesUnchanged,warriorMovedByThisTest=false,simulation=false;
            public uint ownerNetId;
            public Vector3 ownerPosition;
            public float ownerLookY;
            public bool originalControllerAvailable;
            public string ownerExcluded="Only the local Warrior's own colliders are excluded from architecture queries; no collider is disabled. Other actors remain obstacles.";
            public List<SegmentProbe> segments=new List<SegmentProbe>();
            public List<EdgeProfile> edgeWarnings=new List<EdgeProfile>();
        }
        [MenuItem("ROM/Dungeon Ciudad del Sol/Interior - Etapa 1/Comprobar ultimo tramo y avisos (sin mover)")]
        public static void InspectLastLegOpenScene()
        {
            if(EditorApplication.isCompiling)throw new InvalidOperationException("Esperar la compilación.");
            var scene=SceneManager.GetSceneByPath(Target);
            if(!scene.isLoaded)throw new InvalidOperationException("La escena definitiva no está cargada; no se abre ni se inicia Host automáticamente.");
            InspectLastLeg(scene);
        }
        public static void BatchInspectLastLeg()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Entrada exclusiva de batch.");
            try{InspectLastLeg(EditorSceneManager.OpenScene(Target,OpenSceneMode.Single));}
            finally{EditorApplication.Exit(0);}
        }
        static void InspectLastLeg(Scene scene)
        {
            Directory.CreateDirectory(LastOutput);
            var r=new LastReport{startedUTC=DateTime.UtcNow.ToString("o"),unity=Application.unityVersion,playMode=Application.isPlaying,
                hostActive=NetworkServer.active,sceneBefore=Hash(Target),navBefore=Hash(ExteriorData),populationBefore=Hash(PopulationData),sourceSHA256=Hash(WalkSource)};
            try
            {
                Physics.SyncTransforms();
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Warrior);
                var cc=prefab?prefab.GetComponentInChildren<CharacterController>(true):null;
                if(!cc)throw new InvalidOperationException("Falta la cápsula original.");
                int layer=(int)MultiFPS.GameLayers.character,mask=0;
                for(int i=0;i<32;i++)if(!Physics.GetIgnoreLayerCollision(layer,i))mask|=1<<i;
                var settings=NavMesh.GetSettingsByID(0);
                if(Mathf.Abs(settings.agentRadius-.57f)>.0001f||Mathf.Abs(settings.agentClimb-.36f)>.0001f)throw new InvalidOperationException("Humanoid 0 cambió; no se modificará.");
                var owner=MultiFPS.UI.ClientFrontend.OwnedCharacterInstance;
                r.ownerFound=owner;r.ownerNetId=owner?owner.netId:0;r.ownerPosition=owner?owner.transform.position:Vector3.zero;
                r.ownerIsWarrior=owner&&owner.GetComponent<MultiFPS.Gameplay.RiftCombat.WarriorAvatar>();
                r.ownerControllerEnabled=owner&&owner.GetComponent<CharacterController>()&&owner.GetComponent<CharacterController>().enabled;
                r.ownerLookY=owner?owner.Input.LookY:0;
                r.originalControllerAvailable=RiftMMOController.Instance&&RiftMMOController.Instance.Available;
                var cfg=new Configuration{warrior=cc,radius=cc.radius,height=cc.height,slope=Mathf.Min(cc.slopeLimit,settings.agentSlope),layer=layer,mask=mask,physics=scene.GetPhysicsScene(),queryOwner=owner?owner.transform:null};
                r.segments.Add(InspectSegment("Ultimo regreso 7 a 8",V(174,99),V(156,99),cfg));
                r.segments.Add(InspectSegment("Mismo tramo sentido inverso",V(156,99),V(174,99),cfg));
                var capture=JsonUtility.FromJson<CiudadDelSolWalkEvidence.Capture>(File.ReadAllText(WalkSource));
                foreach(var s in capture.samples.Where(s=>s.edge>=0&&s.edge<.57f))
                {
                    var p=new Vector3(s.position.x,-16,s.position.z);
                    var profile=new EdgeProfile{originalSeconds=s.seconds,originalEdgeDistance=s.edge,original=Probe("Aviso real",p,cfg)};
                    if(profile.original.hasNavEdge)
                    {
                        var side=Vector3.ProjectOnPlane(profile.original.navEdgeNormal,Vector3.up).normalized;
                        if(side.sqrMagnitude>.5f)foreach(float distance in new[]{-1f,-.57f,-.32f,0,.32f,.57f,1f})
                        {
                            var q=profile.original.navEdge+side*distance;
                            // NavMesh is ~5 cm above the physical floor here. Retain the
                            // strict 3 cm ground tolerance at the measured physical level.
                            q.y=profile.original.floor.y;
                            profile.acrossBoundary.Add(Probe("Distancia firmada desde arista "+distance,q,cfg));
                        }
                    }
                    r.edgeWarnings.Add(profile);
                }
                // The 1.708 s gap remains a historical absence of movement samples.
                r.segments.Add(InspectSegment("Fisica localizada en intervalo sin muestras",new Vector3(168.09131f,-16,117.69969f),new Vector3(168.57707f,-16,117.71467f),cfg));
                r.result=r.segments.All(s=>s.clear)?"CONSULTAS_FISICAS_Y_NAVMESH_COMPLETAS; NO_ACREDITA_MOVIMIENTO_WARRIOR_FALTANTE":"REVISAR_FALLOS_REALES; SIN_APROBACION";
            }
            catch(Exception e){r.error=e.ToString();r.result="ERROR; SIN_APROBACION";}
            finally
            {
                r.endedUTC=DateTime.UtcNow.ToString("o");r.sceneAfter=Hash(Target);r.navAfter=Hash(ExteriorData);r.populationAfter=Hash(PopulationData);
                r.filesUnchanged=r.sceneBefore==r.sceneAfter&&r.navBefore==r.navAfter&&r.populationBefore==r.populationAfter;
                File.WriteAllText(Path.Combine(LastOutput,"Recepcion_ultimo_tramo.json"),JsonUtility.ToJson(r,true));
                Debug.Log("CIUDAD_ULTIMO_TRAMO: "+r.result+"; informe="+Path.GetFullPath(LastOutput));
            }
        }
        static SegmentProbe InspectSegment(string name,Vector3 a,Vector3 b,Configuration cfg)
        {
            var r=new SegmentProbe{name=name,start=a,end=b,route=CheckRoute(name,new[]{a,b},cfg)};
            var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
            r.projection=NavMesh.SamplePosition(a,out var pa,.35f,filter)&&NavMesh.SamplePosition(b,out var pb,.35f,filter);
            if(r.projection)
            {
                NavMesh.SamplePosition(b,out pb,.35f,filter);
                r.raycastForwardBlocked=NavMesh.Raycast(pa.position,pb.position,out var f,filter);r.raycastForwardHit=f.position;
                r.raycastReverseBlocked=NavMesh.Raycast(pb.position,pa.position,out var v,filter);r.raycastReverseHit=v.position;
            }
            int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/Step));
            for(int i=0;i<=n;i++)r.samples.Add(Probe("Muestra "+i,Vector3.Lerp(a,b,(float)i/n),cfg));
            r.clear=r.projection&&!r.raycastForwardBlocked&&!r.raycastReverseBlocked&&r.route.centerClear&&r.route.scopeValid&&
                r.route.forward.complete&&r.route.forward.physical&&r.route.reverse.complete&&r.route.reverse.physical&&r.samples.All(s=>s.support&&s.capsuleClear&&s.nav);
            return r;
        }
        static SupportProbe Probe(string name,Vector3 p,Configuration cfg)
        {
            var r=new SupportProbe{name=name,requested=p};
            r.support=Ground(p,cfg,GroundTolerance,out var hit);
            if(r.support){r.floor=hit.point;r.normal=hit.normal;r.slope=Vector3.Angle(hit.normal,Vector3.up);r.supportCollider=hit.collider.name;r.supportHierarchy=Hierarchy(hit.collider.transform);}
            r.capsuleClear=Standing(p,cfg,GroundTolerance,out _,out var issue);r.issue=issue;
            if(r.support)
            {
                r.supportMarginLowerBound=r.supportSearchLimit;
                for(int ray=0;ray<8;ray++)for(int step=1;step<=15;step++)
                {
                    float d=step*.1f;var q=hit.point+new Vector3(Mathf.Cos(ray*Mathf.PI/4),0,Mathf.Sin(ray*Mathf.PI/4))*d;
                    if(!Ground(q,cfg,GroundTolerance,out _)){r.supportMarginLowerBound=Mathf.Min(r.supportMarginLowerBound,(step-1)*.1f);break;}
                }
            }
            var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
            r.nav=NavMesh.SamplePosition(p,out var point,.35f,filter)&&Mathf.Abs(point.position.y-p.y)<=.35f;
            if(r.nav)
            {
                r.navPoint=point.position;r.hasNavEdge=NavMesh.FindClosestEdge(point.position,out var edge,filter);
                if(r.hasNavEdge){r.navEdge=edge.position;r.navEdgeNormal=edge.normal;r.edgeDistance=edge.distance;}
            }
            return r;
        }
    }
}
