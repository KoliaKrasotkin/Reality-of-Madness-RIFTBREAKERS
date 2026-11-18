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
            public Vector3 requested,floor,normal,foot;
            public bool support,footprint,capsule,nav;
            public float slope,navEdge=-1,lift;
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
            public int samples,failures,sweepFailures,missingNav;
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
            public Vector3[] corners=new Vector3[0];
            public Trace trace;
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
            public string started,ended,unity,error,sceneBefore,sceneAfter,navBefore,navAfter,popBefore,popAfter;
            public bool playMode,filesUnchanged,sceneDirty,simulation=false;
            public float warriorRadius,warriorHeight,warriorStep,warriorSlope,agentRadius,agentClimb;
            public int mask;
            public List<Connection> connections=new List<Connection>();
            public List<ColliderInfo> colliders=new List<ColliderInfo>();
        }
        sealed class Config
        {
            public Scene scene;
            public PhysicsScene physics;
            public float radius,height,step,slope;
            public int mask;
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
                var cfg=new Config{scene=scene,physics=scene.GetPhysicsScene(),radius=cc.radius,height=cc.height,step=Mathf.Min(cc.stepOffset,agent.agentClimb),slope=Mathf.Min(cc.slopeLimit,agent.agentSlope),mask=mask};
                r.mask=mask;r.warriorRadius=cc.radius;r.warriorHeight=cc.height;r.warriorStep=cc.stepOffset;r.warriorSlope=cc.slopeLimit;r.agentRadius=agent.agentRadius;r.agentClimb=agent.agentClimb;
                var plans=Plans();
                foreach(var p in plans)
                {
                    var c=new Connection{plan=p};r.connections.Add(c);
                    c.guide=InspectTrace("Eje documental",p.points,p,cfg,true);
                    c.forward=Native(p.points[0],p.points[p.points.Length-1],p,cfg,"ida");
                    c.reverse=Native(p.points[p.points.Length-1],p.points[0],p,cfg,"vuelta");
                    c.technicalIndividual=c.forward.complete&&c.reverse.complete&&c.forward.physical&&c.reverse.physical;
                    var chosen=c.guide.clear?c.guide:c.forward.trace;
                    c.acceptedRoute=c.guide.clear?p.points:c.forward.corners;c.chosen=c.guide.clear?"Eje documental físico":"Replanteo sobre camino NavMesh local; no obras";
                    c.geometryFourLanes=chosen!=null&&chosen.widths.Count>0&&chosen.widths.All(w=>w.fourStaticCapsulesClear);
                    c.edgeProtectionClear=chosen!=null&&chosen.widths.Count>0&&chosen.widths.All(w=>!w.leftUnsupportedUnprotected&&!w.rightUnsupportedUnprotected);
                    c.technicalIndividual&=c.edgeProtectionClear&&c.forward.blockedRaycasts==0&&c.reverse.blockedRaycasts==0;
                    c.status=c.technicalIndividual?"RECEPCION_TECNICA_INDIVIDUAL; CUATRO_JUGADORES_PENDIENTES":"PENDIENTE; NO_SE_CONSTRUYE_PARA_FORZAR_VALIDACION";
                    Write(r,"Recepcion_parcial.json");
                }
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
                Write(r,"Recepcion.json");Debug.Log("CIUDAD_INTERIOR_ETAPA2: "+r.connections.Count(c=>c.technicalIndividual)+"/"+r.connections.Count+" conexiones con recepción técnica; escena intacta="+r.filesUnchanged+"; "+Path.GetFullPath(Output));
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
                if(d<closest){selected=h;closest=d;}
            }
            return selected.collider;
        }
        static Station Probe(Vector3 expected,Config cfg,bool graded,float tolerance)
        {
            var s=new Station{requested=expected};s.support=Ground(expected,tolerance,cfg,out var g);
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
            s.lift=.05f+cfg.radius*(1/Mathf.Max(.01f,g.normal.y)-1);s.foot=g.point+Vector3.up*s.lift;
            int count=cfg.physics.OverlapCapsule(s.foot+Vector3.up*cfg.radius,s.foot+Vector3.up*(cfg.height-cfg.radius),cfg.radius,Overlaps,cfg.mask,QueryTriggerInteraction.Ignore);
            s.capsule=count==0;
            if(count>0)s.problem=Issue(count==Overlaps.Length?"CONSULTA_SATURADA":"CAPSULA_OBSTRUIDA",g.point,Overlaps[0]);
            var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
            s.nav=NavMesh.SamplePosition(g.point,out var nav,.35f,filter)&&Mathf.Abs(nav.position.y-g.point.y)<=.35f;
            if(s.nav&&NavMesh.FindClosestEdge(nav.position,out var edge,filter))s.navEdge=edge.distance;
            return s;
        }
        static bool Sweep(Station a,Station b,Config cfg,out Problem problem)
        {
            problem=null;if(!a.support||!b.support)return false;
            var d=b.foot-a.foot;if(d.sqrMagnitude<.000001f)return true;
            if(cfg.physics.CapsuleCast(a.foot+Vector3.up*cfg.radius,a.foot+Vector3.up*(cfg.height-cfg.radius),cfg.radius,d.normalized,out var h,d.magnitude,cfg.mask,QueryTriggerInteraction.Ignore))
            {problem=Issue("BARRIDO_OBSTRUIDO",h.point,h.collider);return false;}
            return true;
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
                    var p=Vector3.Lerp(a,b,(float)j/n);bool grade=NearGrade(p,plan,cfg.radius+.25f);
                    var station=Probe(p,cfg,grade,guide?(grade?cfg.step:.03f):.35f);r.stations.Add(station);r.samples++;
                    if(!station.Clear){r.failures++;Add(r.problems,station.problem);}
                    if(!station.nav)r.missingNav++;
                    if(!guide&&!InsideCorridor(p,plan)){r.insideCorridor=false;Add(r.problems,Issue("CAMINO_FUERA_CONEXION_O_NIVEL",p,null));}
                    if(previous!=null&&!Sweep(previous,station,cfg,out var problem)){r.sweepFailures++;Add(r.problems,problem);}
                    previous=station;r.maximumSlope=Mathf.Max(r.maximumSlope,station.slope);if(station.navEdge>=0)r.minimumNavEdge=Mathf.Min(r.minimumNavEdge,station.navEdge);
                    if(station.Clear&&(j%4==0||j==n))r.widths.Add(MeasureWidth(station,side,cfg,grade));
                }
            }
            r.clear=r.failures==0&&r.sweepFailures==0&&r.insideCorridor;return r;
        }
        static NativeRoute Native(Vector3 start,Vector3 end,Plan p,Config cfg,string direction)
        {
            var r=new NativeRoute();var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
            if(!NavMesh.SamplePosition(start,out var a,.35f,filter)||!NavMesh.SamplePosition(end,out var b,.35f,filter)||Mathf.Abs(a.position.y-start.y)>.35f||Mathf.Abs(b.position.y-end.y)>.35f)
            {r.status="SIN_PROYECCION_LOCAL";return r;}
            var path=new NavMeshPath();r.complete=NavMesh.CalculatePath(a.position,b.position,filter,path)&&path.status==NavMeshPathStatus.PathComplete;r.status=path.status.ToString();r.corners=path.corners;
            if(r.corners.Length>1)
            {
                r.trace=InspectTrace("Camino nativo "+direction,r.corners,p,cfg,false);r.physical=r.complete&&r.trace.clear&&r.trace.missingNav==0;
                for(int i=1;i<r.corners.Length;i++)if(NavMesh.Raycast(r.corners[i-1],r.corners[i],out _,filter))r.blockedRaycasts++;
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
                            bool guarded=cfg.physics.CapsuleCast(center.foot+Vector3.up*cfg.radius,center.foot+Vector3.up*(cfg.height-cfg.radius),cfg.radius,horizontal.normalized,out _,horizontal.magnitude,cfg.mask,QueryTriggerInteraction.Ignore);
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
