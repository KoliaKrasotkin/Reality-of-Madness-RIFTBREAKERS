using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    // Read-only reception of selected existing connections. Never builds or saves a scene.
    public static class CiudadDelSolInteriorStageTwo
    {
        const string City="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Warrior="Assets/ROM/Characters/Warrior/WarriorPlayer.prefab";
        const string Nav="Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset";
        const string Population="Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset";
        const string Output="Backups/CiudadDelSol/Interior/Etapa2_20261117";
        const string Request=Output+"/Recepcion.pending";
        static readonly RaycastHit[] Hits=new RaycastHit[256];
        static readonly Collider[] Overlaps=new Collider[256];
        static double deadline;

        [Serializable] public sealed class Plan
        {
            public string id,from,to;
            public Vector3[] points;
        }
        [Serializable] public sealed class Problem
        {
            public string kind,collider,hierarchy,globalId,prefab;
            public Vector3 position;
            public int occurrences=1;
        }
        [Serializable] public sealed class Station
        {
            public Vector3 requested,rawPathPoint,floor,normal,foot,navPosition;
            public bool support,footprint,capsule,nav,gradedSupport;
            public float slope,navEdge=-1,lift,centerPlaneLift;
            public string supportObject;
            public Problem problem;
            public bool Clear => support&&footprint&&capsule;
        }
        [Serializable] public sealed class Width
        {
            public Vector3 center;
            public float usefulWidthLowerBound;
            public bool fourStaticCapsulesClear;
            public bool leftUnsupportedUnprotected,rightUnsupportedUnprotected;
        }
        [Serializable] public sealed class Trace
        {
            public string name;
            public bool clear,insideCorridor=true;
            public int samples,failures,sweepFailures,missingNav,refinedSweeps,disconnectedSurfaceSteps;
            public float maximumInterpolatedHeightCorrection;
            public float maximumSlope,minimumNavEdge=float.MaxValue;
            public List<Station> stations=new List<Station>();
            public List<Width> widths=new List<Width>();
            public List<Problem> problems=new List<Problem>();
        }
        [Serializable] public sealed class NativeRoute
        {
            public string status;
            public bool complete,physical;
            public int blockedRaycasts;
            public int rawBlockedRaycasts;
            public List<RayEvidence> rayEvidence=new List<RayEvidence>();
            public bool viaOriginalWaypoints;
            public Vector3[] rejectedGlobalCorners;
            public Trace rejectedGlobalTrace;
            public List<RayEvidence> rejectedGlobalRays;
            public Vector3[] corners=new Vector3[0];
            public Trace trace;
        }
        [Serializable] public sealed class RayEvidence
        {
            public Vector3 from,to,hit,reverseHit;
            public bool rawBlocked,reverseBlocked,atSource,atTarget,sourceBoundary,targetBoundary,localPathsStraight,interiorRaysClear,resolved;
            public int mask;
            public string reason;
        }
        [Serializable] public sealed class Connection
        {
            public Plan plan;
            public Trace guide;
            public NativeRoute forward,reverse;
            public bool technicalIndividual,geometryFourLanes,edgeProtectionClear,realPlayersTested=false;
            public string status,chosen;
            public Vector3[] acceptedRoute;
        }
        [Serializable] public sealed class ColliderInfo
        {
            public string name,hierarchy,globalId,prefab,type;
            public bool enabled,active,trigger;
            public int layer;
            public Vector3 center,size;
        }
        [Serializable] public sealed class Report
        {
            public int procedureVersion=3;
            public string started,ended,unity,error,sceneBefore,sceneAfter,navBefore,navAfter,popBefore,popAfter;
            public bool playMode,filesUnchanged,sceneDirty,simulation=false;
            public float warriorRadius,warriorHeight,warriorStep,warriorSlope,agentRadius,agentClimb;
            public int mask;
            public bool activeNavMeshLinks;
            public List<Connection> connections=new List<Connection>();
            public List<ColliderInfo> colliders=new List<ColliderInfo>();
            public List<Station> negativeControls=new List<Station>();
            public bool negativeControlsRejectObstaclesAndOtherLevels;
        }
        sealed class Config
        {
            public Scene scene;
            public PhysicsScene physics;
            public float radius,height,step,slope,warriorSlope;
            public int mask;
            public bool activeLinks;
            public Bounds[] stairBounds;
        }

        [InitializeOnLoadMethod]
        static void InstallExplicitRequest()
        {
            if(!File.Exists(Request))return;
            deadline=EditorApplication.timeSinceStartup+120;
            EditorApplication.update-=RunRequest;EditorApplication.update+=RunRequest;
        }
        static void RunRequest()
        {
            if(!File.Exists(Request)){EditorApplication.update-=RunRequest;return;}
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            EditorApplication.update-=RunRequest;
            string nonce=File.ReadAllText(Request).Trim();if(!Guid.TryParse(nonce,out _))return;
            File.Move(Request,Request+".consumed."+nonce);
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Recepción no iniciada antes de 120 s.");
                Inspect();
            }
            catch(Exception e){File.WriteAllText(Output+"/Error_solicitud.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("ROM/Dungeon Ciudad del Sol/Interior - Etapa 2/Recibir conexiones seleccionadas (sin obras)")]
        public static void Inspect()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Recepción exclusiva de Edit Mode. No se detiene ni inicia Play automáticamente.");
            var scene=SceneManager.GetSceneByPath(City);
            if(!scene.isLoaded||scene.isDirty)throw new InvalidOperationException("Se necesita la escena definitiva cargada y sin cambios pendientes; no se guarda ni sustituye.");
            Directory.CreateDirectory(Output);
            var r=new Report{started=DateTime.UtcNow.ToString("o"),unity=Application.unityVersion,playMode=Application.isPlaying,sceneBefore=Hash(City),navBefore=Hash(Nav),popBefore=Hash(Population)};
            try
            {
                Physics.SyncTransforms();
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Warrior);var cc=prefab?prefab.GetComponentInChildren<CharacterController>(true):null;
                if(!cc||(cc.transform.lossyScale-Vector3.one).sqrMagnitude>.000001f)throw new InvalidOperationException("Cápsula Warrior no disponible o escala distinta de la original.");
                var agent=NavMesh.GetSettingsByID(0);
                if(Mathf.Abs(agent.agentRadius-.57f)>.0001f||Mathf.Abs(agent.agentClimb-.36f)>.0001f)throw new InvalidOperationException("Humanoid 0 difiere de los valores protegidos.");
                int mask=0;for(int layer=0;layer<32;layer++)if(!Physics.GetIgnoreLayerCollision((int)MultiFPS.GameLayers.character,layer))mask|=1<<layer;
                var cfg=new Config{scene=scene,physics=scene.GetPhysicsScene(),radius=cc.radius,height=cc.height,step=Mathf.Min(cc.stepOffset,agent.agentClimb),slope=Mathf.Min(cc.slopeLimit,agent.agentSlope),warriorSlope=cc.slopeLimit,mask=mask};
                cfg.activeLinks=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Behaviour>(true)).Any(b=>b&&b.enabled&&b.gameObject.activeInHierarchy&&(b.GetType().FullName=="Unity.AI.Navigation.NavMeshLink"||b.GetType().FullName=="UnityEngine.AI.OffMeshLink"));
                cfg.stairBounds=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&c.name.StartsWith("Staircase_01",StringComparison.Ordinal)).Select(c=>{var bounds=c.bounds;bounds.Expand(new Vector3(cfg.radius*2,cfg.step*2,cfg.radius*2));return bounds;}).ToArray();
                r.activeNavMeshLinks=cfg.activeLinks;
                r.mask=mask;r.warriorRadius=cc.radius;r.warriorHeight=cc.height;r.warriorStep=cc.stepOffset;r.warriorSlope=cc.slopeLimit;r.agentRadius=agent.agentRadius;r.agentClimb=agent.agentClimb;
                var plans=Plans();
                foreach(var p in plans)
                {
                    var c=new Connection{plan=p};r.connections.Add(c);
                    c.guide=InspectTrace("Eje documental",p.points,p,cfg,true);
                    c.forward=Native(p.points[0],p.points[p.points.Length-1],p,cfg,"ida",c.guide.clear&&c.guide.missingNav==0);
                    c.reverse=Native(p.points[p.points.Length-1],p.points[0],p,cfg,"vuelta",c.guide.clear&&c.guide.missingNav==0);
                    c.technicalIndividual=c.forward.complete&&c.reverse.complete&&c.forward.physical&&c.reverse.physical;
                    var chosen=c.guide.clear?c.guide:c.forward.trace;
                    c.acceptedRoute=c.guide.clear?p.points:(c.forward.trace==null?new Vector3[0]:c.forward.trace.stations.Where(s=>s.support).Select(s=>s.floor).ToArray());c.chosen=c.guide.clear?"Eje documental físico":"Replanteo sobre camino NavMesh local y suelo medido; no obras";
                    c.geometryFourLanes=chosen!=null&&chosen.widths.Count>0&&chosen.widths.All(w=>w.fourStaticCapsulesClear);
                    c.edgeProtectionClear=chosen!=null&&chosen.widths.Count>0&&chosen.widths.All(w=>!w.leftUnsupportedUnprotected&&!w.rightUnsupportedUnprotected);
                    c.technicalIndividual&=c.edgeProtectionClear&&c.forward.blockedRaycasts==0&&c.reverse.blockedRaycasts==0;
                    c.status=c.technicalIndividual?"RECEPCION_TECNICA_INDIVIDUAL; CUATRO_JUGADORES_PENDIENTES":"PENDIENTE; NO_SE_CONSTRUYE_PARA_FORZAR_VALIDACION";
                    Write(r,"Recepcion_parcial.json");
                }
                r.negativeControls.Add(Probe(V(132,-16,105),cfg,false,.03f));
                r.negativeControls.Add(Probe(V(117,-12,123),cfg,false,.03f));
                r.negativeControls.Add(Probe(V(105,-16,63),cfg,false,.03f));
                r.negativeControlsRejectObstaclesAndOtherLevels=!r.negativeControls[0].capsule&&!r.negativeControls[1].support&&!r.negativeControls[2].support;
                if(!r.negativeControlsRejectObstaclesAndOtherLevels)throw new InvalidOperationException("Un control negativo no fue rechazado; no se certifica la recepción.");
                foreach(var collider in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)))
                {
                    var bounds=WorldBounds(collider);
                    if(!plans.Any(p=>p.points.Zip(p.points.Skip(1),(a,b)=>SegmentBounds(a,b,6)).Any(b=>b.Intersects(bounds))))continue;
                    r.colliders.Add(new ColliderInfo{name=collider.name,hierarchy=Hierarchy(collider.transform),globalId=GlobalObjectId.GetGlobalObjectIdSlow(collider).ToString(),prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(collider.gameObject),type=collider.GetType().Name,enabled=collider.enabled,active=collider.gameObject.activeInHierarchy,trigger=collider.isTrigger,layer=collider.gameObject.layer,center=bounds.center,size=bounds.size});
                }
            }
            catch(Exception e){r.error=e.ToString();}
            finally
            {
                r.ended=DateTime.UtcNow.ToString("o");r.sceneAfter=Hash(City);r.navAfter=Hash(Nav);r.popAfter=Hash(Population);r.sceneDirty=scene.isDirty;
                r.filesUnchanged=r.sceneBefore==r.sceneAfter&&r.navBefore==r.navAfter&&r.popBefore==r.popAfter&&!r.sceneDirty;
                if(!string.IsNullOrEmpty(r.error)||!r.filesUnchanged)
                    foreach(var c in r.connections){c.technicalIndividual=false;c.status="RECEPCION_INVALIDA; REVISAR_ERROR_O_INTEGRIDAD";}
                Write(r,"Recepcion.json");
                var summary=new List<string>{"# Última recepción real de Etapa 2",r.started+" → "+r.ended,
                    "Aprobadas técnicamente: "+r.connections.Count(c=>c.technicalIndividual)+"/"+r.connections.Count+". Cuatro jugadores reales: no probado.",
                    "Integridad: "+r.filesUnchanged+". Error: "+r.error};
                summary.AddRange(r.connections.Select(c=>"- "+c.plan.id+": "+c.status));
                File.WriteAllLines(Path.Combine(Output,"Resultado_ultima_recepcion.md"),summary);
                Debug.Log("CIUDAD_INTERIOR_ETAPA2: "+r.connections.Count(c=>c.technicalIndividual)+"/"+r.connections.Count+" conexiones con recepción técnica; escena intacta="+r.filesUnchanged+"; "+Path.GetFullPath(Output));
            }
        }
        static Plan[] Plans()=>new[]{
            P("H12","R21","R23",V(117,-16,123),V(117,-16,165),V(123,-16,165)),
            P("H06","R23","G11",V(123,-16,165),V(105,-16,165)),
            P("H13","R21","G05",V(117,-16,123),V(75,-16,123)),
            P("H14","G05","R34",V(75,-16,123),V(57,-16,123),V(57,-16,117)),
            P("VC01","R21 (cola de VC01)","B05",V(117,-16,123),V(120,-16,123),V(120,-16,105),V(120,-16,90),V(114,-16,90),V(108,-18,90),V(102,-20,90),V(96,-22,90),V(90,-24,90),V(84,-24,90)),
            P("VC02","B05","D01 (acceso)",V(84,-24,90),V(81,-24,90),V(81,-24,84),V(81,-26,78),V(81,-28,72),V(81,-30,66),V(81,-30,60),V(81,-32,54),V(81,-34,48),V(81,-34,39),V(99,-34,39),V(99,-34,45),V(102,-34,45),V(102,-34,63),V(105,-34,63))};
        static Plan P(string id,string from,string to,params Vector3[] points)=>new Plan{id=id,from=from,to=to,points=points};
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);

        static bool Ground(Vector3 expected,float tolerance,Config cfg,out RaycastHit selected)
        {
            selected=default;int n=cfg.physics.Raycast(expected+Vector3.up*.8f,Vector3.down,Hits,1.6f,cfg.mask,QueryTriggerInteraction.Ignore);
            if(n==Hits.Length)return false;float closest=float.PositiveInfinity;
            for(int i=0;i<n;i++)
            {
                var h=Hits[i];if(!h.collider||h.collider.gameObject.scene!=cfg.scene)continue;
                float d=Mathf.Abs(h.point.y-expected.y);
                if(d>tolerance||Vector3.Angle(h.normal,Vector3.up)>cfg.slope+.01f)continue;
                // The first physical support in this local height window wins. Choosing
                // the closest expected Y could see THROUGH a ramp onto its basamento.
                if(h.distance<closest){selected=h;closest=h.distance;}
            }
            return selected.collider;
        }
        static Station Probe(Vector3 expected,Config cfg,bool graded,float tolerance)
        {
            graded|=cfg.stairBounds!=null&&cfg.stairBounds.Any(bounds=>bounds.Contains(expected));
            var s=new Station{requested=expected,gradedSupport=graded};s.support=Ground(expected,graded?Mathf.Max(tolerance,cfg.step):tolerance,cfg,out var g);
            if(!s.support){s.problem=Issue("SIN_APOYO_EN_NIVEL",expected,null);return s;}
            s.floor=g.point;s.normal=g.normal;s.slope=Vector3.Angle(g.normal,Vector3.up);s.supportObject=Hierarchy(g.collider.transform);s.footprint=true;
            for(int k=0;k<8;k++)
            {
                var offset=new Vector3(Mathf.Cos(k*Mathf.PI/4),0,Mathf.Sin(k*Mathf.PI/4))*cfg.radius;
                float planeY=g.point.y-(g.normal.x*offset.x+g.normal.z*offset.z)/g.normal.y;
                var q=g.point+offset;q.y=planeY;
                if(!Ground(q,graded?cfg.step:.03f,cfg,out var other)||
                    (Mathf.Abs(other.point.y-planeY)>.03f&&(!graded||Mathf.Abs(other.point.y-g.point.y)>cfg.step)))
                {s.footprint=false;s.problem=Issue("HUELLA_SIN_APOYO_COMPATIBLE",q,null);break;}
            }
            // Analytic clearance for the lower spherical cap tangent to a sloped floor.
            // This is geometry, not a bigger collision tolerance or a changed controller.
            s.centerPlaneLift=.05f+cfg.radius*(1/Mathf.Max(.01f,g.normal.y)-1);
            float baseY=g.point.y+s.centerPlaneLift;
            if(graded)
            {
                // A capsule at a ramp/landing joint rests on its lower hemisphere,
                // not necessarily on the center ray's horizontal face. Evaluate the
                // support envelope over the actual floor, without moving any collider.
                for(int ring=1;ring<=3;ring++)for(int k=0;k<8;k++)
                {
                    float rho=cfg.radius*ring/4f;
                    var offset=new Vector3(Mathf.Cos(k*Mathf.PI/4),0,Mathf.Sin(k*Mathf.PI/4))*rho;
                    var q=g.point+offset;q.y=g.point.y-(g.normal.x*offset.x+g.normal.z*offset.z)/g.normal.y;
                    if(Ground(q,cfg.step,cfg,out var contact))
                        baseY=Mathf.Max(baseY,contact.point.y-cfg.radius+Mathf.Sqrt(cfg.radius*cfg.radius-rho*rho)+.05f);
                }
            }
            s.lift=baseY-g.point.y;s.foot=new Vector3(g.point.x,baseY,g.point.z);
            if(s.lift>Mathf.Max(cfg.step,s.centerPlaneLift)+.05f)
            {s.footprint=false;s.problem=Issue("APOYO_EXCEDE_ESCALON_REAL",g.point,g.collider);}
            int count=cfg.physics.OverlapCapsule(s.foot+Vector3.up*cfg.radius,s.foot+Vector3.up*(cfg.height-cfg.radius),cfg.radius,Overlaps,cfg.mask,QueryTriggerInteraction.Ignore);
            s.capsule=count==0;
            if(count>0)s.problem=Issue(count==Overlaps.Length?"CONSULTA_SATURADA":"CAPSULA_OBSTRUIDA",g.point,Overlaps[0]);
            var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
            s.nav=NavMesh.SamplePosition(g.point,out var nav,.35f,filter)&&Mathf.Abs(nav.position.y-g.point.y)<=.35f;
            // Failed SamplePosition may return infinity; a missing position is not evidence.
            s.navPosition=s.nav?nav.position:Vector3.zero;
            if(s.nav&&NavMesh.FindClosestEdge(nav.position,out var edge,filter))s.navEdge=edge.distance;
            return s;
        }
        static bool DirectSweep(Station a,Station b,Config cfg,out Problem problem)
        {
            problem=null;if(!a.support||!b.support)return false;
            var d=b.foot-a.foot;if(d.sqrMagnitude<.000001f)return true;
            if(cfg.physics.CapsuleCast(a.foot+Vector3.up*cfg.radius,a.foot+Vector3.up*(cfg.height-cfg.radius),cfg.radius,d.normalized,out var h,d.magnitude,cfg.mask,QueryTriggerInteraction.Ignore))
            {problem=Issue("BARRIDO_OBSTRUIDO",h.point,h.collider);return false;}
            return true;
        }
        static bool Sweep(Station a,Station b,Config cfg,bool graded,out Problem problem,out bool refined)
        {
            refined=false;
            graded|=a.gradedSupport||b.gradedSupport;
            float horizontal=new Vector2(b.floor.x-a.floor.x,b.floor.z-a.floor.z).magnitude;
            if(graded&&Mathf.Abs(a.floor.y-b.floor.y)>Mathf.Max(cfg.step,horizontal*Mathf.Tan(cfg.slope*Mathf.Deg2Rad)))
            {problem=Issue("SALTO_SUPERA_ESCALON_Y_PENDIENTE",b.floor,null);return false;}
            if(DirectSweep(a,b,cfg,out problem))return true;
            if(!graded||!a.Clear||!b.Clear)return false;
            // The valid ground-following capsule center may curve at a joint.
            // Every subsegment still undergoes the same full collision sweep.
            var previous=a;int n=Mathf.Max(2,Mathf.CeilToInt(Vector3.Distance(a.floor,b.floor)/.05f));
            for(int i=1;i<=n;i++)
            {
                var next=i==n?b:Probe(Vector3.Lerp(a.floor,b.floor,(float)i/n),cfg,true,cfg.step);
                Problem hit=null;
                if(!next.Clear||!DirectSweep(previous,next,cfg,out hit)){problem=next.problem??hit;return false;}
                previous=next;
            }
            refined=true;problem=null;return true;
        }
        static Trace InspectTrace(string name,Vector3[] points,Plan plan,Config cfg,bool guide)
        {
            var r=new Trace{name=name};
            for(int i=1;i<points.Length;i++)
            {
                var a=points[i-1];var b=points[i];int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/.25f));Station previous=null;
                var side=Vector3.Cross(Vector3.up,(b-a).normalized).normalized;
                for(int j=0;j<=n;j++)
                {
                    var raw=Vector3.Lerp(a,b,(float)j/n);var p=raw;
                    // Funnel corners do not enumerate all changes of floor height.
                    // Preserve actual corner heights; between them use the selected
                    // physical profile and demand a local NavMesh surface at the floor.
                    if(!guide&&j>0&&j<n)p.y=GuideReference(raw,plan).y;
                    bool grade=NearGrade(p,plan,cfg.radius+.25f);
                    var station=Probe(p,cfg,grade,guide?(grade?cfg.step:.03f):.35f);station.rawPathPoint=raw;r.stations.Add(station);r.samples++;
                    r.maximumInterpolatedHeightCorrection=Mathf.Max(r.maximumInterpolatedHeightCorrection,Mathf.Abs(p.y-raw.y));
                    if(!station.Clear){r.failures++;Add(r.problems,station.problem);}
                    if(!station.nav)r.missingNav++;
                    if(!guide&&!InsideCorridor(p,plan)){r.insideCorridor=false;Add(r.problems,Issue("CAMINO_FUERA_CONEXION_O_NIVEL",p,null));}
                    if(previous!=null)
                    {
                        if(!Sweep(previous,station,cfg,grade||NearGrade(previous.requested,plan,cfg.radius+.25f),out var problem,out var refined)){r.sweepFailures++;Add(r.problems,problem);}
                        else if(refined)r.refinedSweeps++;
                        if(!guide&&previous.nav&&station.nav&&Vector3.Distance(previous.navPosition,station.navPosition)>.00001f)
                        {
                            bool cornerA=points.Any(c=>Vector3.Distance(c,previous.navPosition)<=.001f),cornerB=points.Any(c=>Vector3.Distance(c,station.navPosition)<=.001f);
                            if(!Connected(previous.navPosition,station.navPosition,cfg,cornerA,cornerB,out _))
                            {r.disconnectedSurfaceSteps++;Add(r.problems,Issue("CONTINUIDAD_LOCAL_NAVMESH_NO_DEMOSTRADA",station.floor,null));}
                        }
                    }
                    previous=station;r.maximumSlope=Mathf.Max(r.maximumSlope,station.slope);if(station.navEdge>=0)r.minimumNavEdge=Mathf.Min(r.minimumNavEdge,station.navEdge);
                    if(station.Clear&&(j%4==0||j==n))r.widths.Add(MeasureWidth(station,side,cfg,grade));
                }
            }
            r.clear=r.failures==0&&r.sweepFailures==0&&r.insideCorridor&&r.disconnectedSurfaceSteps==0;return r;
        }
        static NativeRoute Native(Vector3 start,Vector3 end,Plan p,Config cfg,string direction,bool guidePhysicallySafe)
        {
            var r=new NativeRoute();var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
            if(!NavMesh.SamplePosition(start,out var a,.35f,filter)||!NavMesh.SamplePosition(end,out var b,.35f,filter)||Mathf.Abs(a.position.y-start.y)>.35f||Mathf.Abs(b.position.y-end.y)>.35f)
            {r.status="SIN_PROYECCION_LOCAL";return r;}
            var path=new NavMeshPath();r.complete=NavMesh.CalculatePath(a.position,b.position,filter,path)&&path.status==NavMeshPathStatus.PathComplete;r.status=path.status.ToString();r.corners=path.corners;
            if(r.corners.Length>1)
            {
                r.trace=InspectTrace("Camino nativo "+direction,r.corners,p,cfg,false);r.physical=r.complete&&r.trace.clear&&r.trace.missingNav==0;
                for(int i=1;i<r.corners.Length;i++)
                {
                    bool connected=Connected(r.corners[i-1],r.corners[i],cfg,true,true,out var proof);
                    if(proof.rawBlocked){r.rawBlockedRaycasts++;r.rayEvidence.Add(proof);}
                    if(!connected)r.blockedRaycasts++;
                }
            }
            if(guidePhysicallySafe&&r.complete&&r.trace!=null&&!r.trace.insideCorridor&&r.trace.failures==0&&r.trace.sweepFailures==0&&r.trace.missingNav==0&&r.trace.disconnectedSurfaceSteps==0)
            {
                // A shortest global path may cut a documented turn. Do not widen the
                // corridor: require complete paths THROUGH the original control points.
                var waypoints=Vector3.Distance(start,p.points[0])<.001f?p.points:p.points.Reverse().ToArray();
                var joined=new List<Vector3>();bool all=true;
                for(int i=1;i<waypoints.Length;i++)
                {
                    if(!NavMesh.SamplePosition(waypoints[i-1],out var sa,.35f,filter)||!NavMesh.SamplePosition(waypoints[i],out var sb,.35f,filter)||Mathf.Abs(sa.position.y-waypoints[i-1].y)>.35f||Mathf.Abs(sb.position.y-waypoints[i].y)>.35f)
                    {all=false;break;}
                    var local=new NavMeshPath();
                    if(!NavMesh.CalculatePath(sa.position,sb.position,filter,local)||local.status!=NavMeshPathStatus.PathComplete||local.corners.Length<2)
                    {all=false;break;}
                    foreach(var corner in local.corners)if(joined.Count==0||Vector3.Distance(joined[joined.Count-1],corner)>.00001f)joined.Add(corner);
                }
                if(all)
                {
                    r.rejectedGlobalCorners=r.corners;r.rejectedGlobalTrace=r.trace;r.rejectedGlobalRays=r.rayEvidence;
                    r.corners=joined.ToArray();r.viaOriginalWaypoints=true;
                    r.trace=InspectTrace("Camino nativo "+direction+" por puntos originales",r.corners,p,cfg,false);
                    r.physical=r.trace.clear&&r.trace.missingNav==0;r.blockedRaycasts=r.rawBlockedRaycasts=0;r.rayEvidence=new List<RayEvidence>();
                    for(int i=1;i<r.corners.Length;i++)
                    {
                        bool connected=Connected(r.corners[i-1],r.corners[i],cfg,true,true,out var proof);
                        if(proof.rawBlocked){r.rawBlockedRaycasts++;r.rayEvidence.Add(proof);}
                        if(!connected)r.blockedRaycasts++;
                    }
                }
            }
            return r;
        }
        static Width MeasureWidth(Station center,Vector3 side,Config cfg,bool graded)
        {
            var w=new Width{center=center.floor,fourStaticCapsulesClear=true};float sum=0;
            foreach(int sign in new[]{-1,1})
            {
                float reach=0;
                for(int k=1;k<=27;k++)
                {
                    float d=Mathf.Min(k*.1f,3-cfg.radius);var q=center.floor+side*(sign*d);
                    q.y=center.floor.y-(center.normal.x*(q.x-center.floor.x)+center.normal.z*(q.z-center.floor.z))/center.normal.y;
                    var candidate=Probe(q,cfg,graded,graded?cfg.step:.03f);
                    if(!candidate.Clear)
                    {
                        if(!candidate.support||!candidate.footprint)
                        {
                            var horizontal=q-center.floor;horizontal.y=0;
                            bool capsuleStopped=cfg.physics.CapsuleCast(center.foot+Vector3.up*cfg.radius,center.foot+Vector3.up*(cfg.height-cfg.radius),cfg.radius,horizontal.normalized,out _,horizontal.magnitude,cfg.mask,QueryTriggerInteraction.Ignore);
                            // A walkable ramp or a riser below stepOffset is not a guardrail.
                            bool guarded=capsuleStopped&&cfg.physics.Raycast(center.floor+Vector3.up*(cfg.step+.05f),horizontal.normalized,out var barrier,horizontal.magnitude,cfg.mask,QueryTriggerInteraction.Ignore)&&Vector3.Angle(barrier.normal,Vector3.up)>cfg.warriorSlope;
                            if(sign<0)w.leftUnsupportedUnprotected=!guarded;else w.rightUnsupportedUnprotected=!guarded;
                        }
                        break;
                    }
                    reach=d;
                }
                sum+=reach;
            }
            w.usefulWidthLowerBound=sum+cfg.radius*2;
            foreach(float offset in new[]{-1.2f,-.4f,.4f,1.2f})
            {
                var q=center.floor+side*offset;q.y=center.floor.y-(center.normal.x*(q.x-center.floor.x)+center.normal.z*(q.z-center.floor.z))/center.normal.y;
                w.fourStaticCapsulesClear&=Probe(q,cfg,graded,graded?cfg.step:.03f).Clear;
            }
            return w;
        }
        static bool NearGrade(Vector3 p,Plan plan,float margin)
        {
            for(int i=1;i<plan.points.Length;i++)if(Mathf.Abs(plan.points[i].y-plan.points[i-1].y)>.01f&&DistanceSegment(p,plan.points[i-1],plan.points[i])<margin+.35f)return true;
            return false;
        }
        static Vector3 GuideReference(Vector3 p,Plan plan)
        {
            float best=float.PositiveInfinity;Vector3 result=p;
            for(int i=1;i<plan.points.Length;i++)
            {
                var a=plan.points[i-1];var b=plan.points[i];var d=new Vector2(b.x-a.x,b.z-a.z);var q=new Vector2(p.x-a.x,p.z-a.z);
                float t=Mathf.Clamp01(Vector2.Dot(q,d)/Mathf.Max(d.sqrMagnitude,.00001f));var candidate=Vector3.Lerp(a,b,t);
                float distance=(new Vector2(candidate.x-p.x,candidate.z-p.z)).sqrMagnitude;
                if(distance<best-.00001f||(Mathf.Abs(distance-best)<.00001f&&Mathf.Abs(candidate.y-p.y)<Mathf.Abs(result.y-p.y)))
                {best=distance;result=candidate;}
            }
            return result;
        }
        static bool Straight(Vector3 a,Vector3 b,NavMeshQueryFilter filter)
        {
            const float precision=.001f;var path=new NavMeshPath();
            if(!NavMesh.CalculatePath(a,b,filter,path)||path.status!=NavMeshPathStatus.PathComplete||path.corners.Length<2)return false;
            if(Vector3.Distance(path.corners[0],a)>precision||Vector3.Distance(path.corners[path.corners.Length-1],b)>precision)return false;
            return path.corners.All(c=>DistanceSegment(c,a,b)<=precision);
        }
        static bool Connected(Vector3 a,Vector3 b,Config cfg,bool originalCornerA,bool originalCornerB,out RayEvidence proof)
        {
            const float precision=.001f;var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
            proof=new RayEvidence{from=a,to=b};proof.rawBlocked=NavMesh.Raycast(a,b,out var hit,filter);
            if(!proof.rawBlocked){proof.resolved=true;proof.reason="Raycast libre";return true;}
            proof.hit=hit.position;proof.mask=hit.mask;proof.atSource=Vector3.Distance(a,hit.position)<=precision;proof.atTarget=Vector3.Distance(b,hit.position)<=precision;
            proof.sourceBoundary=NavMesh.FindClosestEdge(a,out var ea,filter)&&ea.distance<=precision&&Vector3.Distance(ea.position,a)<=precision;
            proof.targetBoundary=NavMesh.FindClosestEdge(b,out var eb,filter)&&eb.distance<=precision&&Vector3.Distance(eb.position,b)<=precision;
            if(!((proof.atSource&&(originalCornerA||proof.sourceBoundary))||(proof.atTarget&&(originalCornerB||proof.targetBoundary))))
            {proof.reason="Impacto no acreditado como extremo/esquina/arista";return false;}
            proof.reverseBlocked=NavMesh.Raycast(b,a,out var reverse,filter);proof.reverseHit=proof.reverseBlocked?reverse.position:Vector3.zero;
            proof.localPathsStraight=Straight(a,b,filter)&&Straight(b,a,filter);
            if(!proof.localPathsStraight){proof.reason="Sin camino local completo y recto en ambos sentidos";return false;}
            if(!proof.reverseBlocked){proof.resolved=true;proof.reason="Extremo numérico con raycast recíproco libre y caminos locales rectos";return true;}
            bool reverseEndpoint=Vector3.Distance(reverse.position,a)<=precision||Vector3.Distance(reverse.position,b)<=precision;
            if(!reverseEndpoint||cfg.activeLinks){proof.reason="Doble impacto no demostrable sin enlaces";return false;}
            // A connected, straight ground path without links plus reciprocal interior
            // rays proves continuity. The 0.5 mm witnesses are within the SAME original
            // 1 mm corner equality, not an increased clearance/gap tolerance.
            float inset=Mathf.Min(.0005f,Vector3.Distance(a,b)*.25f);var direction=(b-a).normalized;
            if(!NavMesh.SamplePosition(a+direction*inset,out var ia,precision,filter)||!NavMesh.SamplePosition(b-direction*inset,out var ib,precision,filter))
            {proof.reason="Sin testigos interiores locales";return false;}
            proof.interiorRaysClear=!NavMesh.Raycast(ia.position,ib.position,out _,filter)&&!NavMesh.Raycast(ib.position,ia.position,out _,filter);
            proof.resolved=proof.interiorRaysClear&&Straight(a,ia.position,filter)&&Straight(ia.position,a,filter)&&Straight(b,ib.position,filter)&&Straight(ib.position,b,filter);
            proof.reason=proof.resolved?"Extremos numéricos; suelo conectado sin enlaces y testigos recíprocos interiores":"Continuidad interior no acreditada";return proof.resolved;
        }
        static bool InsideCorridor(Vector3 p,Plan plan)
        {
            if(p.y> -15.5f||p.y< -34.5f)return false;
            for(int i=1;i<plan.points.Length;i++)
            {
                var a=plan.points[i-1];var b=plan.points[i];var d=b-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(d.sqrMagnitude,.00001f));var q=a+d*t;
                if(new Vector2(p.x-q.x,p.z-q.z).magnitude<=6&&Mathf.Abs(p.y-q.y)<=.36f)return true;
            }
            return false;
        }
        static float DistanceSegment(Vector3 p,Vector3 a,Vector3 b){var d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(.00001f,d.sqrMagnitude)));}
        static Bounds SegmentBounds(Vector3 a,Vector3 b,float expansion){var bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Expand(new Vector3(expansion,6,expansion));return bounds;}
        static Bounds WorldBounds(Collider c)
        {
            if(c.enabled&&c.gameObject.activeInHierarchy)return c.bounds;
            Bounds local;if(c is BoxCollider box)local=new Bounds(box.center,box.size);else if(c is MeshCollider mesh&&mesh.sharedMesh)local=mesh.sharedMesh.bounds;else return new Bounds(c.transform.position,Vector3.zero);
            var r=new Bounds(c.transform.TransformPoint(local.center),Vector3.zero);
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)r.Encapsulate(c.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3(x,y,z))));return r;
        }
        static Problem Issue(string kind,Vector3 p,Collider collider)=>new Problem{kind=kind,position=p,collider=collider?collider.name:"",hierarchy=collider?Hierarchy(collider.transform):"",globalId=collider?GlobalObjectId.GetGlobalObjectIdSlow(collider).ToString():"",prefab=collider?PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(collider.gameObject):""};
        static void Add(List<Problem> list,Problem issue){if(issue==null)return;var old=list.FirstOrDefault(p=>p.kind==issue.kind&&p.globalId==issue.globalId&&(issue.globalId!=""||Vector3.Distance(p.position,issue.position)<2));if(old!=null)old.occurrences++;else list.Add(issue);}
        static string Hierarchy(Transform t)=>t.parent?Hierarchy(t.parent)+"/"+t.name:t.name;
        static string Hash(string path){using(var hash=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","");}
        static void Write(Report report,string name)=>File.WriteAllText(Path.Combine(Output,name),JsonUtility.ToJson(report,true));
    }
}

// Explicit reception request prepared after offline compilation.

// Resume: verify measured ground profiles and retain all obstacle controls.

// Verify actual stair supports and non-walkable edge protections.

// Last connection: retain original waypoints instead of widening its corridor.
