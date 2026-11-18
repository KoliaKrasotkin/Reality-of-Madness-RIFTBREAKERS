using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    // One scoped reception tool. No startup callback, generator, Bake, save, or gameplay dependency.
    public static class CiudadDelSolInteriorStageOne
    {
        const string Target = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Warrior = "Assets/ROM/Characters/Warrior/WarriorPlayer.prefab";
        const string Menu = "ROM/Dungeon Ciudad del Sol/Interior - Etapa 1/Validar A03-R21 (solo lectura)";
        static readonly Bounds Scope = new Bounds(new Vector3(141, -12, 117), new Vector3(78, 16, 54));
        const float StandingOffset = .05f; // Same standing offset as the existing Warrior bridge trial.
        const float PhysicalWidth = 6f;
        const float Step = .25f;
        const float GroundTolerance = .03f;
        static readonly Collider[] Overlaps = new Collider[256];
        static readonly RaycastHit[] Rays = new RaycastHit[128];

        [Serializable] public sealed class Issue
        {
            public string type, collider, hierarchy;
            public Vector3 point;
            public float value;
        }
        [Serializable] public sealed class PathResult
        {
            public string direction, status;
            public bool projected, complete, physical;
            public Vector3 start, end;
            public Vector3[] corners;
            public int checkedSamples, problems;
            public List<Issue> issues = new List<Issue>();
        }
        [Serializable] public sealed class RouteResult
        {
            public string name;
            public Vector3[] guide;
            public bool negativeControl, centerClear, widthSixClear, scopeValid;
            public int checkedSamples, centerProblems, widthProblems;
            public List<Issue> issues = new List<Issue>();
            public PathResult forward, reverse;
        }
        [Serializable] public sealed class Movement
        {
            public string direction, error;
            public bool executed, complete, sourceSceneUnchanged;
            public int agents, steps, arrived;
            public Vector3[] finalPositions;
        }
        [Serializable] public sealed class Report
        {
            public string unity, scene, beforeSHA256, afterSHA256, error, result;
            public bool batch, playMode, originalDirty, sceneUnchanged, noSave = true;
            public float warriorRadius, warriorHeight, warriorStep, warriorSlope, agentRadius, agentClimb;
            public int collisionMask, vertices, activeColliders, prefabLayer, warriorRuntimeLayer;
            public bool characterSelfCollision;
            public List<RouteResult> routes = new List<RouteResult>();
            public List<Movement> movement = new List<Movement>();
            public string selectedRoute;
        }
        sealed class Configuration
        {
            public CharacterController warrior;
            public float radius, height, slope;
            public int layer, mask;
            public PhysicsScene physics;
        }

        [MenuItem(Menu)]
        public static void InspectOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Detén Play y espera la compilación. Esta recepción no inicia el juego.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != Target || scene.isDirty)
                throw new InvalidOperationException("Activa la escena definitiva fuera de Play, sin cambios pendientes. La herramienta no la guarda.");
            Execute(scene, Path.Combine("Backups/CiudadDelSol/Interior", "Recepcion_" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ")), false);
        }

        public static void BatchInspect()
        {
            int exit = 1;
            string output = Environment.GetEnvironmentVariable("ROM_CITY_STAGE1_OUT");
            if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("Falta ROM_CITY_STAGE1_OUT.");
            try
            {
                if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Entrada exclusiva de batch en Edit Mode.");
                var scene = EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
                Execute(scene, output, true);
                exit = 0;
            }
            catch (Exception e) { Debug.LogException(e); }
            finally { EditorApplication.Exit(exit); }
        }

        static void Execute(Scene scene, string output, bool batch)
        {
            Directory.CreateDirectory(output);
            var report = new Report { unity = Application.unityVersion, scene = scene.path,
                batch = batch, playMode = Application.isPlaying, originalDirty = scene.isDirty, beforeSHA256 = Hash(Target) };
            try
            {
                if (scene.path != Target || scene.isDirty) throw new InvalidOperationException("La escena de recepción no es la original limpia.");
                Physics.SyncTransforms();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Warrior);
                var cc = prefab ? prefab.GetComponentInChildren<CharacterController>(true) : null;
                if (!cc) throw new InvalidOperationException("No se encontró el CharacterController original del Warrior.");
                var scale = cc.transform.lossyScale;
                if ((scale - Vector3.one).sqrMagnitude > .000001f)
                    throw new InvalidOperationException("La escala del controlador cambió; revisar dimensiones antes de probar.");
                // Match CharacterInstance/Health, as does the existing bridge validator.
                // The serialized prefab is on hitbox; that layer intentionally ignores world geometry.
                int runtimeLayer = (int)MultiFPS.GameLayers.character;
                int mask = 0;
                for (int i = 0; i < 32; i++) if (!Physics.GetIgnoreLayerCollision(runtimeLayer, i)) mask |= 1 << i;
                var agent = NavMesh.GetSettingsByID(0);
                var cfg = new Configuration { warrior = cc, radius = cc.radius, height = cc.height,
                    slope = Mathf.Min(cc.slopeLimit, agent.agentSlope), layer = runtimeLayer, mask = mask, physics = scene.GetPhysicsScene() };
                report.warriorRadius = cc.radius; report.warriorHeight = cc.height; report.warriorStep = cc.stepOffset;
                report.warriorSlope = cc.slopeLimit; report.agentRadius = agent.agentRadius; report.agentClimb = agent.agentClimb;
                report.collisionMask = mask; report.characterSelfCollision = !Physics.GetIgnoreLayerCollision(cfg.layer, cfg.layer);
                report.prefabLayer = cc.gameObject.layer; report.warriorRuntimeLayer = runtimeLayer;
                if (Mathf.Abs(agent.agentRadius - .57f) > .0001f || Mathf.Abs(agent.agentClimb - .36f) > .0001f)
                    throw new InvalidOperationException("Humanoid 0 difiere de los parámetros protegidos; no se cambiará.");
                report.activeColliders = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Collider>(true))
                    .Count(c => c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger);
                report.vertices = NavMesh.CalculateTriangulation().vertices.Length;
                report.routes.Add(CheckRoute("Control negativo: eje 132/105", new[] { V(135,105), V(120,105) }, cfg, true));
                report.routes.Add(CheckRoute("A03-R21 por distribuidor norte Z117", new[] { V(156,99),V(174,99),V(174,117),V(120,117),V(117,123) }, cfg));
                report.routes.Add(CheckRoute("A03-R21 por rodeo Z111", new[] { V(156,99),V(174,99),V(174,111),V(135,111),V(120,111),V(120,123),V(117,123) }, cfg));
                var selected = report.routes.FirstOrDefault(r => !r.negativeControl && r.scopeValid && r.centerClear && r.widthSixClear &&
                    r.forward != null && r.reverse != null && r.forward.complete && r.reverse.complete && r.forward.physical && r.reverse.physical);
                if (selected != null)
                {
                    report.selectedRoute = selected.name;
                    report.movement.Add(MoveFour(scene, selected.guide, cfg, "ida"));
                    report.movement.Add(MoveFour(scene, selected.guide.Reverse().ToArray(), cfg, "vuelta"));
                    report.result = report.movement.All(m => m.complete) ? "RECEPCION_FISICA_EDITMODE_APROBADA; PLAY_COOPERATIVO_PENDIENTE" : "MOVIMIENTO_PROXIES_NO_COMPLETO";
                }
                else report.result = "NINGUNA_CANDIDATA_APROBADA; SIN_CAMBIOS";
            }
            catch (Exception e) { report.error = e.ToString(); report.result = "ERROR_SIN_GUARDADO"; }
            finally
            {
                report.afterSHA256 = Hash(Target); report.sceneUnchanged = report.beforeSHA256 == report.afterSHA256 && !scene.isDirty;
                File.WriteAllText(Path.Combine(output, "Recepcion.json"), JsonUtility.ToJson(report, true));
                Debug.Log("INTERIOR_ETAPA1: " + report.result + "; informe=" + Path.GetFullPath(output) + "; escenaIntacta=" + report.sceneUnchanged);
            }
        }

        static RouteResult CheckRoute(string name, Vector3[] points, Configuration cfg, bool negative = false)
        {
            var r = new RouteResult { name = name, guide = points, negativeControl = negative, scopeValid = points.All(p => Scope.Contains(p)) };
            for (int i = 1; i < points.Length; i++)
            {
                var a = points[i-1]; var b = points[i]; var dir = b-a;
                var side = Vector3.Cross(Vector3.up, dir.normalized);
                int samples = Mathf.Max(1, Mathf.CeilToInt(dir.magnitude / Step));
                for (int j = 0; j <= samples; j++)
                {
                    var p = Vector3.Lerp(a,b,(float)j/samples); r.checkedSamples++;
                    if (!Standing(p, cfg, GroundTolerance, out _, out var issue))
                    { r.centerProblems++; Add(r.issues, issue); }
                    if (negative) continue;
                    float limit = PhysicalWidth * .5f - cfg.radius;
                    int lanes = Mathf.CeilToInt(limit * 2 / .5f);
                    for (int k = 0; k <= lanes; k++)
                    {
                        var q = p + side * Mathf.Lerp(-limit,limit,(float)k/lanes);
                        if (!Standing(q,cfg,GroundTolerance,out _,out issue)) { r.widthProblems++; Add(r.issues,issue); }
                    }
                }
                if (!negative)
                {
                    for (int j = 0; j < samples; j++)
                    {
                        var p = Vector3.Lerp(a,b,(float)j/samples); var q = Vector3.Lerp(a,b,(float)(j+1)/samples);
                        if (!Sweep(p,q,cfg,out var issue)) { r.centerProblems++; Add(r.issues,issue); }
                    }
                }
            }
            r.centerClear = r.centerProblems == 0; r.widthSixClear = !negative && r.widthProblems == 0;
            if (!negative) { r.forward = NativePath(points[0],points[points.Length-1],cfg,"ida"); r.reverse = NativePath(points[points.Length-1],points[0],cfg,"vuelta"); }
            return r;
        }

        static bool Ground(Vector3 p, Configuration cfg, float tolerance, out RaycastHit selected)
        {
            selected = default;
            int n = cfg.physics.Raycast(p + Vector3.up * .6f, Vector3.down, Rays, 1.2f, cfg.mask, QueryTriggerInteraction.Ignore);
            if (n == Rays.Length) return false;
            float best = float.PositiveInfinity;
            for (int i = 0; i < n; i++)
            {
                var hit=Rays[i]; if (!hit.collider || hit.collider.gameObject.scene.path != Target) continue;
                float delta=Mathf.Abs(hit.point.y-p.y);
                if (delta > tolerance || Vector3.Angle(hit.normal,Vector3.up)>cfg.slope+.01f) continue;
                if (!(hit.collider.name.StartsWith("Platform_Base_01",StringComparison.Ordinal) || hit.collider.name.StartsWith("Staircase_01",StringComparison.Ordinal))) continue;
                if (delta<best) { best=delta; selected=hit; }
            }
            return selected.collider;
        }

        static bool Standing(Vector3 p, Configuration cfg, float tolerance, out Vector3 foot, out Issue issue)
        {
            foot=p; issue=null;
            if (!Ground(p,cfg,tolerance,out var ground)) { issue=new Issue {type="SIN_APOYO_LOCAL",point=p}; return false; }
            foot=ground.point;
            for (int k=0;k<8;k++)
            {
                float angle=k*Mathf.PI*.25f;
                var q=foot+new Vector3(Mathf.Cos(angle)*cfg.radius,0,Mathf.Sin(angle)*cfg.radius);
                if (!Ground(q,cfg,tolerance,out var other) || Mathf.Abs(other.point.y-foot.y)>GroundTolerance)
                { issue=new Issue {type="HUELLA_SIN_APOYO",point=q};return false; }
            }
            var basePoint=foot+Vector3.up*StandingOffset;
            int count=cfg.physics.OverlapCapsule(basePoint+Vector3.up*cfg.radius,basePoint+Vector3.up*(cfg.height-cfg.radius),cfg.radius,Overlaps,cfg.mask,QueryTriggerInteraction.Ignore);
            if(count==Overlaps.Length){issue=new Issue{type="SATURACION_CONSULTA",point=p};return false;}
            for(int i=0;i<count;i++) if(Overlaps[i])
            { issue=Hit("CAPSULA_OBSTRUIDA",foot,Overlaps[i]);return false; }
            return true;
        }

        static bool Sweep(Vector3 a, Vector3 b, Configuration cfg, out Issue issue)
        {
            issue=null;
            if(!Ground(a,cfg,.35f,out var ga)||!Ground(b,cfg,.35f,out var gb)) {issue=new Issue{type="BARRIDO_SIN_APOYO",point=a};return false;}
            var pa=ga.point+Vector3.up*StandingOffset; var pb=gb.point+Vector3.up*StandingOffset; var d=pb-pa;
            if(d.sqrMagnitude<.000001f)return true;
            if(cfg.physics.CapsuleCast(pa+Vector3.up*cfg.radius,pa+Vector3.up*(cfg.height-cfg.radius),cfg.radius,d.normalized,out var hit,d.magnitude,cfg.mask,QueryTriggerInteraction.Ignore))
            {issue=Hit("BARRIDO_OBSTRUIDO",hit.point,hit.collider);return false;}
            return true;
        }

        static PathResult NativePath(Vector3 start, Vector3 end, Configuration cfg, string direction)
        {
            var r=new PathResult{direction=direction,start=start,end=end};
            var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
            if(!NavMesh.SamplePosition(start,out var a,.35f,filter)||!NavMesh.SamplePosition(end,out var b,.35f,filter)||Mathf.Abs(a.position.y-start.y)>.35f||Mathf.Abs(b.position.y-end.y)>.35f)
            {r.status="PROYECCION_LOCAL_FALLIDA";return r;}
            r.projected=true; var path=new NavMeshPath();
            bool calculated=NavMesh.CalculatePath(a.position,b.position,filter,path);
            r.status=path.status.ToString();r.complete=calculated&&path.status==NavMeshPathStatus.PathComplete;r.corners=path.corners;
            if(!r.complete)return r;
            for(int i=1;i<r.corners.Length;i++)
            {
                var pa=r.corners[i-1];var pb=r.corners[i];int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(pa,pb)/Step));
                for(int j=0;j<=n;j++){var p=Vector3.Lerp(pa,pb,(float)j/n);r.checkedSamples++;Issue issue=null;if(!Scope.Contains(p)||Mathf.Abs(p.y+16)>.36f||!Standing(p,cfg,.35f,out _,out issue)){r.problems++;Add(r.issues,issue??new Issue{type="RUTA_FUERA_SECTOR_O_COTA",point=p});}}
                if(!Sweep(pa,pb,cfg,out var swept)){r.problems++;Add(r.issues,swept);}
            }
            r.physical=r.problems==0;return r;
        }

        static Movement MoveFour(Scene source, Vector3[] guide, Configuration cfg, string direction)
        {
            var result=new Movement{direction=direction,agents=4};Scene preview=default;var previousActive=SceneManager.GetActiveScene();
            try
            {
                preview=EditorSceneManager.NewPreviewScene();var physics=preview.GetPhysicsScene();
                if(physics==cfg.physics)throw new InvalidOperationException("La escena temporal no tiene física aislada.");
                if(!SceneManager.SetActiveScene(preview))throw new InvalidOperationException("No se pudo activar la escena temporal; no se crearán objetos en la original.");
                var bounds=new Bounds(guide[0],Vector3.zero);foreach(var p in guide)bounds.Encapsulate(p);bounds.Expand(new Vector3(16,16,16));
                var colliders=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy&&c.bounds.Intersects(bounds)).ToArray();
                foreach(var original in colliders)
                {
                    var go=new GameObject("Recepcion temporal: "+original.name);SceneManager.MoveGameObjectToScene(go,preview);
                    go.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);go.transform.localScale=original.transform.lossyScale;go.layer=original.gameObject.layer;
                    if(original is BoxCollider box){var c=go.AddComponent<BoxCollider>();c.center=box.center;c.size=box.size;}
                    else if(original is MeshCollider mesh){var c=go.AddComponent<MeshCollider>();c.sharedMesh=mesh.sharedMesh;c.convex=mesh.convex;c.cookingOptions=mesh.cookingOptions;}
                    else if(original is SphereCollider sphere){var c=go.AddComponent<SphereCollider>();c.center=sphere.center;c.radius=sphere.radius;}
                    else throw new InvalidOperationException("Collider no reproducible sin alterar: "+original.GetType().Name);
                }
                var controllers=new CharacterController[4];var routes=new Vector3[4][];var indices=new int[4];
                var startDirection=(guide[1]-guide[0]).normalized;var startSide=Vector3.Cross(Vector3.up,startDirection);
                for(int i=0;i<4;i++)
                {
                    float lateral=i%2==0?-.75f:.75f;float trail=i<2?0:1.5f;
                    var go=new GameObject("Temporal cápsula Warrior "+i);SceneManager.MoveGameObjectToScene(go,preview);go.layer=cfg.layer;
                    go.transform.position=guide[0]+startSide*lateral-startDirection*trail+Vector3.up*StandingOffset;
                    var c=go.AddComponent<CharacterController>();c.radius=cfg.warrior.radius;c.height=cfg.warrior.height;c.center=cfg.warrior.center;c.skinWidth=cfg.warrior.skinWidth;c.stepOffset=cfg.warrior.stepOffset;c.slopeLimit=cfg.warrior.slopeLimit;c.minMoveDistance=0;
                    controllers[i]=c;routes[i]=new Vector3[guide.Length];
                    for(int j=0;j<guide.Length;j++){var tangent=j==guide.Length-1?guide[j]-guide[j-1]:j==0?guide[1]-guide[0]:(guide[j]-guide[j-1]).normalized+(guide[j+1]-guide[j]).normalized;var side=Vector3.Cross(Vector3.up,tangent.normalized);routes[i][j]=guide[j]+side*lateral;}
                }
                Physics.SyncTransforms();result.executed=true;
                var previousPositions=controllers.Select(c=>c.transform.position).ToArray();var stalled=new int[4];
                for(int step=0;step<20000;step++)
                {
                    bool all=true;
                    for(int i=0;i<4;i++)
                    {
                        var c=controllers[i];if(indices[i]>=routes[i].Length)continue;all=false;
                        var goal=routes[i][indices[i]];var d=goal-c.transform.position;d.y=0;
                        if(d.magnitude<.12f){indices[i]++;continue;}
                        c.Move(Vector3.ClampMagnitude(d,3f*.02f)+Vector3.down*.04f);
                        if(Vector3.Distance(c.transform.position,previousPositions[i])<.0001f)stalled[i]++;else stalled[i]=0;
                        previousPositions[i]=c.transform.position;
                        if(stalled[i]>400)throw new InvalidOperationException("Cápsula sin progreso durante ocho segundos simulados: "+i+" en "+c.transform.position);
                        if(c.transform.position.y< -16.25f||c.transform.position.y> -15.5f)throw new InvalidOperationException("Cápsula fuera del piso esperado: "+c.transform.position);
                    }
                    physics.Simulate(.02f);result.steps=step+1;
                    if(all)break;
                }
                result.arrived=indices.Count(i=>i>=guide.Length);result.complete=result.arrived==4;result.finalPositions=controllers.Select(c=>c.transform.position).ToArray();
                if(!result.complete)result.error="Alguna cápsula no completó el recorrido sin teletransportes.";
            }
            catch(Exception e){result.error=e.ToString();}
            finally{if(previousActive.IsValid())SceneManager.SetActiveScene(previousActive);if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);result.sourceSceneUnchanged=!source.isDirty;}
            return result;
        }

        static Vector3 V(float x,float z)=>new Vector3(x,-16,z);
        static Issue Hit(string kind,Vector3 p,Collider c)=>new Issue{type=kind,point=p,collider=c?c.name:"",hierarchy=c?Hierarchy(c.transform):""};
        static string Hierarchy(Transform t)=>t.parent?Hierarchy(t.parent)+"/"+t.name:t.name;
        static void Add(List<Issue> list,Issue issue){if(issue!=null&&list.Count<24)list.Add(issue);}
        static string Hash(string file){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(File.ReadAllBytes(file))).Replace("-","");}
    }
}
