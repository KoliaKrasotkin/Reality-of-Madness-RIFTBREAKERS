// Headless contract tests of the ACTUAL Route method, compiled with query doubles.
// Does not execute Unity, PhysX, Mirror, or the real NavMeshData.
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using ROM.CiudadDelSol;

static class Model
{
    public static string Mode; public static bool Endpoint;
    public static bool ShortBody, Turn, SawTurn, SawPose;
    public static float NavY(float x) => .05f + .25f * x;
    public static bool Critical(float x) => x > .85f && x < .96f;
    public static Collider Ground = new Collider("Salida_Meridional_Generated");
    public static Collider Interior = new Collider("Interior_no_autorizado");
    public static bool ArchMode => Mode.StartsWith("arch-");
    public static Collider ArchCollider()
    {
        var root = new Transform("Dungeon_Ciudad_del_Sol_Generated");
        var group = new Transform("Arquitectura monumental - fuera de los pasos") { parent = root };
        var collider = new Collider(Mode == "arch-other-monument" ? "SM_MainGate" : "Entrance_Arch_01");
        collider.transform.parent = group; return collider;
    }
    public static Collider Pavement()
    {
        var root = new Transform("Dungeon_Ciudad_del_Sol_Generated");
        var sector = new Transform(Mode == "arch-other-sector" ? "05 - Claustro del Eclipse" :
            Mode == "arch-interior-floor" ? "Interior" : "06 - Santuario Profano") { parent = root };
        var group = new Transform(Mode == "arch-no-modular" ? "Decoracion" : "Suelo modular - superficie Y=4") { parent = sector };
        var floor = new Collider("Platform_Base_01"); floor.transform.parent = group; return floor;
    }
    public static NavMeshAgent Agent = new NavMeshAgent();
    public static CapsuleCollider Body => new CapsuleCollider { radius = ShortBody ? .41f : .36f,
        height = ShortBody ? .9f : 1.9f, center = new Vector3(0, ShortBody ? .45f : .95f, 0) };

    public static int Main()
    {
        int failed = 0, count = 0;
        foreach (var mode in new[] { "stairs", "turn", "short", "partial", "nav-hole", "ground-hole", "other-floor", "interior", "cliff", "obstacle", "nav-gap",
            "corner-roundoff", "corner-end-roundoff", "corner-real-gap", "corner-local-partial", "corner-local-detour", "corner-near-not-corner", "corner-mid-hit", "corner-local-endpoint-shift",
            "edge-roundoff", "edge-too-far", "edge-missing", "edge-both-blocked", "edge-local-partial", "edge-local-detour",
            "arch-coplanar", "arch-no-floor", "arch-high", "arch-other-monument", "arch-other-sector", "arch-interior-floor", "arch-no-modular", "arch-saturated", "arch-solid", "arch-steep",
            "arch-055", "arch-055-solid", "arch-high-excessive", "arch-wrong-plane", "arch-no-sector-root",
            "native-connected", "native-adjacent-portal", "native-corner-connected", "native-clamped", "native-clamped-tiny", "native-reverse-clamped",
            "native-invalid-map", "native-wrong-agent", "native-other-floor", "native-link", "native-disconnected-coincident", "native-local-partial", "native-local-detour",
            "native-obstacle", "native-ground-hole", "native-edge-missing", "native-edge-far", "native-unavailable", "native-corner-disconnected" })
        {
            Mode = mode; Turn = mode == "turn" || mode.StartsWith("corner-") || mode.StartsWith("native-corner-"); ShortBody = mode == "short"; SawTurn = SawPose = false;
            var post = new CiudadEnemyPost();
            var method = typeof(CiudadDelSolEncounterValidation).GetMethod("Route", BindingFlags.NonPublic | BindingFlags.Static);
            var config = new CiudadDelSolPopulationDefinition();
            if (ArchMode) config.Sectors = new[] { new Sector { Id = "06-santuario", PlayableAncestor = "06 - Santuario Profano" } };
            object[] args = { new UnityEngine.SceneManagement.Scene(), config, post,
                new Vector3(0, .05f, 0), new Vector3(2, .55f, Turn ? 2 : 0), false, null };
            bool result = (bool)method.Invoke(null, args);
            bool positive = mode == "stairs" || mode == "turn" || mode == "short" || mode == "corner-roundoff" || mode == "corner-end-roundoff" ||
                mode == "edge-roundoff" || mode == "arch-coplanar" || mode == "arch-high" || mode == "arch-055" ||
                mode == "native-connected" || mode == "native-adjacent-portal" || mode == "native-corner-connected";
            string expected = mode switch { "partial" => "Ruta PathPartial", "nav-hole" => "RUTA_NAVMESH_INTERRUMPIDA",
                "ground-hole" => "RUTA_SIN_APOYO", "other-floor" => "RUTA_SIN_NAVMESH", "interior" => "RUTA_APOYO_NO_AUTORIZADO",
                "cliff" => "RUTA_APOYO_INCOMPATIBLE", "obstacle" => "RUTA_CAPSULA_OBSTRUIDA", "nav-gap" => "RUTA_SIN_NAVMESH",
                "arch-solid" => "RUTA_CAPSULA_OBSTRUIDA", "arch-steep" => "RUTA_APOYO_INCOMPATIBLE",
                "arch-no-floor" or "arch-high" or "arch-other-monument" or "arch-other-sector" or "arch-interior-floor" or "arch-no-modular" or "arch-saturated" => "RUTA_APOYO_NO_AUTORIZADO",
                "arch-055-solid" => "RUTA_CAPSULA_OBSTRUIDA", "arch-high-excessive" or "arch-wrong-plane" or "arch-no-sector-root" => "RUTA_APOYO_NO_AUTORIZADO",
                "native-obstacle" => "RUTA_CAPSULA_OBSTRUIDA", "native-ground-hole" => "RUTA_SIN_APOYO",
                "native-local-partial" or "native-local-detour" or "native-edge-missing" or "native-edge-far" => "RUTA_NAVMESH_INTERRUMPIDA",
                "edge-both-blocked" => "RUTA_NAVMESH_INTERRUMPIDA",
                "corner-real-gap" => "RUTA_CONTINUIDAD_NO_DEMOSTRADA",
                _ when mode.StartsWith("native-") => "RUTA_CONTINUIDAD_NO_DEMOSTRADA",
                _ => "RUTA_NAVMESH_INTERRUMPIDA" };
            bool pass = result == positive && (!positive || !Turn || SawTurn) && (!positive || !ShortBody || SawPose) &&
                (positive || ((string)args[6]).StartsWith(expected));
            Console.WriteLine((pass ? "PASS " : "FAIL ") + mode + " => " + result + " " + args[6]);
            count++;
            if (!pass) failed++;
            if (positive)
            {
                Model.SawTurn = Model.SawPose = false;
                (args[3], args[4]) = (args[4], args[3]); args[6] = null;
                result = (bool)method.Invoke(null, args);
                pass = result && (!Turn || SawTurn) && (!ShortBody || SawPose);
                Console.WriteLine((pass ? "PASS " : "FAIL ") + mode + "-return => " + result + " " + args[6]);
                count++; if (!pass) failed++;
            }
        }
        foreach (var mode in new[] { "native-connected", "native-adjacent-portal", "native-clamped", "native-reverse-clamped", "native-invalid-map", "native-wrong-agent", "native-other-floor", "native-link", "native-disconnected-coincident", "native-local-partial", "native-local-detour", "native-edge-missing", "native-edge-far", "native-unavailable", "endpoint-interior-hit" })
        {
            Endpoint = true; Mode = mode; Turn = false;
            var method = typeof(CiudadDelSolEncounterValidation).GetMethod("ConnectedStep", BindingFlags.NonPublic | BindingFlags.Static);
            var a = new Vector3(0, .05f, 0); var b = new Vector3(.1f, .075f, 0);
            object[] args = { a, b, a, new Vector3(2, .55f, 0), new NavMeshQueryFilter { agentTypeID = 0, areaMask = -1 }, false, false, null };
            bool result = (bool)method.Invoke(null, args);
            bool expected = mode == "native-connected" || mode == "native-adjacent-portal" || mode == "native-edge-missing" || mode == "native-edge-far";
            bool pass = result == expected && (!result || (bool)args[6]);
            Console.WriteLine((pass ? "PASS " : "FAIL ") + "endpoint-" + mode + " => " + result + " " + args[7]);
            count++; if (!pass) failed++;
        }
        foreach(var mode in new[]{"fan-connected","fan-far","fan-oneway","fan-link","fan-agent","fan-disconnected","fan-overflow","fan-budget","fan-area"}) { Mode=mode; var method=typeof(CiudadDelSolEncounterValidation).GetMethod("CornerFanConnected",BindingFlags.Static|BindingFlags.NonPublic); using(var query=new UnityEngine.Experimental.AI.NavMeshQuery(new UnityEngine.Experimental.AI.NavMeshWorld(),Unity.Collections.Allocator.Temp,256)) { object[] args={query,new UnityEngine.Experimental.AI.PolygonId{Id=1},new UnityEngine.Experimental.AI.PolygonId{Id=mode=="fan-budget"?34:3},Vector3.zero,new NavMeshQueryFilter{agentTypeID=0,areaMask=mode=="fan-area"?1:-1},.001f}; bool result=(bool)method.Invoke(null,args); bool pass=result==(mode=="fan-connected"); Console.WriteLine((pass?"PASS ":"FAIL ")+mode+" => "+result);count++;if(!pass)failed++; } }
        Console.WriteLine("Contract scenarios=" + count + ", failures=" + failed + "; simulated queries, NOT Unity validation");
        return failed == 0 ? 0 : 1;
    }
}

namespace UnityEngine
{
    public struct Vector3 : IFormattable
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3();
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 left => new Vector3(-1, 0, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 back => new Vector3(0, 0, -1);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float f) => new Vector3(a.x * f, a.y * f, a.z * f);
        public static float Distance(Vector3 a, Vector3 b) { var d = a - b; return MathF.Sqrt(d.x * d.x + d.y * d.y + d.z * d.z); }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * t;
        public static float Angle(Vector3 a, Vector3 b) => MathF.Acos(Math.Clamp(Dot(a, b) / MathF.Sqrt(a.sqrMagnitude * b.sqrMagnitude), -1, 1)) * 180 / MathF.PI;
        public float sqrMagnitude => x * x + y * y + z * z;
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public string ToString(string format) => ToString(format, null);
        public string ToString(string format, IFormatProvider p) => $"({x:F3},{y:F3},{z:F3})";
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static float Distance(Vector2 a, Vector2 b) => MathF.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
    }
    public static class Mathf
    {
        public static float Abs(float x) => MathF.Abs(x);
        public static float Min(float a, float b) => MathF.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int CeilToInt(float x) => (int)MathF.Ceiling(x);
        public static float Sqrt(float x) => MathF.Sqrt(x);
        public static float Clamp01(float x) => Math.Clamp(x, 0, 1);
    }
    public static class Debug { public static void Log(string s) { } }
    public class Transform
    {
        public string name; public Transform parent; public Transform root => parent != null ? parent.root : this;
        public Vector3 position;
        public Transform(string name) { this.name = name; }
        public Transform Find(string name) => name == "06 - Santuario Profano" && Model.Mode != "arch-no-sector-root" ?
            new Transform(name) { parent = this, position = new Vector3(0, Model.Mode == "arch-wrong-plane" ? -4 : 0, 0) } : null;
        public static implicit operator bool(Transform t) => t != null;
    }
    public class Collider
    {
        public Transform transform;
        public Collider(string name) { transform = new Transform(name); }
        public string name => transform.name;
    }
    public class CapsuleCollider { public float radius, height; public Vector3 center; }
    public struct RaycastHit { public Collider collider; public Vector3 point, normal; }
    public enum QueryTriggerInteraction { Ignore }
    public struct PhysicsScene
    {
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float distance, int mask, QueryTriggerInteraction q)
        {
            hit = default;
            if ((Model.Mode == "ground-hole" || Model.Mode == "native-ground-hole") && Model.Critical(origin.x)) return false;
            float y = .25f * MathF.Floor(origin.x + .00001f);
            if (Model.Mode == "cliff" && origin.x > .85f) y += .5f;
            if (Model.ArchMode && Model.Critical(origin.x)) y += Model.Mode == "arch-high" ? .05f :
                Model.Mode.StartsWith("arch-055") ? .055f : Model.Mode == "arch-high-excessive" ? .37f : .007f;
            if (origin.y < y || origin.y - y > distance) return false;
            hit = new RaycastHit { point = new Vector3(origin.x, y, origin.z), normal = Vector3.up,
                collider = Model.Mode == "interior" && Model.Critical(origin.x) ? Model.Interior : Model.Ground };
            if (Model.ArchMode && Model.Critical(origin.x))
            {
                hit.collider = Model.ArchCollider();
                hit.normal = Model.Mode == "arch-steep" ? new Vector3(0, .5f, .8660254f) : new Vector3(0, .799f, -.602f);
            }
            return true;
        }
        public int Raycast(Vector3 origin, Vector3 direction, RaycastHit[] results, float distance, int mask, QueryTriggerInteraction q)
        {
            if (!Model.ArchMode || Model.Mode == "arch-no-floor") return 0;
            float y = .25f * MathF.Floor(origin.x + .00001f);
            if (origin.y < y || origin.y - y > distance) return 0;
            var hit = new RaycastHit { point = new Vector3(origin.x, y, origin.z), normal = Vector3.up, collider = Model.Pavement() };
            if (Model.Mode == "arch-saturated") { for (int i = 0; i < results.Length; i++) results[i] = hit; return results.Length; }
            results[0] = hit; return 1;
        }
        public int OverlapCapsule(Vector3 top, Vector3 bottom, float radius, Collider[] hits, int mask, QueryTriggerInteraction q)
        {
            if (top.z > .1f && top.x > 1.99f) Model.SawTurn = true;
            if (Model.ShortBody && Model.Critical(top.x))
            {
                float upper = MathF.Max(top.y, bottom.y) + radius;
                Model.SawPose = MathF.Abs(upper - (Model.NavY(top.x) + .93f)) < .001f;
                if (!Model.SawPose) { hits[0] = Model.Ground; return 1; }
            }
            if (Model.Mode == "obstacle" && Model.Critical(top.x)) { hits[0] = Model.Ground; return 1; }
            if ((Model.Mode == "arch-solid" || Model.Mode == "arch-055-solid") && Model.Critical(top.x)) { hits[0] = Model.ArchCollider(); return 1; }
            if (Model.Mode == "native-obstacle" && Model.Critical(top.x)) { hits[0] = Model.Ground; return 1; }
            return 0;
        }
    }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public UnityEngine.PhysicsScene GetPhysicsScene() => new UnityEngine.PhysicsScene(); }
}
namespace UnityEngine.AI
{
    public class NavMeshAgent { public int agentTypeID = 0, areaMask = -1; public float baseOffset; }
    public struct NavMeshQueryFilter { public int agentTypeID, areaMask; }
    public enum NavMeshPathStatus { PathComplete, PathPartial }
    public struct NavMeshHit { public UnityEngine.Vector3 position; public int mask; public float distance; }
    public struct NavMeshBuildSettings { public float agentClimb, agentSlope; }
    public class NavMeshPath { public NavMeshPathStatus status; public UnityEngine.Vector3[] corners; }
    public static class NavMesh
    {
        public const int AllAreas = -1;
        public static NavMeshBuildSettings GetSettingsByID(int id) => new NavMeshBuildSettings { agentClimb = .36f, agentSlope = 45 };
        public static bool CalculatePath(Vector3 a, Vector3 b, NavMeshQueryFilter f, NavMeshPath p)
        {
            p.status = Model.Mode == "partial" ? NavMeshPathStatus.PathPartial : NavMeshPathStatus.PathComplete;
            p.corners = Model.Turn ? new[] { a, new Vector3(2, .55f, 0), b } : new[] { a, b };
            if (Vector3.Distance(a, b) < .2f && Model.Mode.StartsWith("corner-"))
            {
                p.corners = new[] { a, b };
                if (Model.Mode == "corner-local-partial") p.status = NavMeshPathStatus.PathPartial;
                if (Model.Mode == "corner-local-detour") p.corners = new[] { a, Vector3.Lerp(a, b, .5f) + Vector3.right * .04f, b };
                if (Model.Mode == "corner-local-endpoint-shift") p.corners = new[] { a + Vector3.right * .01f, b };
            }
            if (Vector3.Distance(a, b) < .2f && Model.Mode == "edge-local-partial") p.status = NavMeshPathStatus.PathPartial;
            if (Vector3.Distance(a, b) < .2f && Model.Mode == "edge-local-detour") p.corners = new[] { a, Vector3.Lerp(a, b, .5f) + Vector3.forward * .04f, b };
            if (Vector3.Distance(a, b) < .2f && Model.Mode == "native-local-partial") p.status = NavMeshPathStatus.PathPartial;
            if (Vector3.Distance(a, b) < .2f && Model.Mode == "native-local-detour") p.corners = new[] { a, Vector3.Lerp(a, b, .5f) + Vector3.forward * .04f, b };
            return true;
        }
        public static bool SamplePosition(Vector3 p, out NavMeshHit hit, float radius, NavMeshQueryFilter f)
        {
            hit = new NavMeshHit { position = new Vector3(p.x, Model.NavY(p.x), p.z) };
            if (Model.Mode == "nav-gap" && Model.Critical(p.x)) return false;
            if (Model.Mode == "other-floor" && Model.Critical(p.x)) hit.position.y += 4;
            return Vector3.Distance(p, hit.position) <= radius;
        }
        public static bool Raycast(Vector3 a, Vector3 b, out NavMeshHit hit, NavMeshQueryFilter f)
        {
            hit = new NavMeshHit { position = b };
            if (Model.Endpoint) { hit.position = Model.Mode == "endpoint-interior-hit" ? Vector3.Lerp(a,b,.5f) : (a.x < b.x ? a : b); return true; }
            if (Model.Mode.StartsWith("native-"))
            {
                if (Model.Mode.StartsWith("native-corner-"))
                {
                    var pivot = new Vector3(2, .55f, 0);
                    if (Vector3.Distance(a, pivot) < .0001f || Vector3.Distance(b, pivot) < .0001f)
                    { hit.position = a; return true; }
                }
                else if ((a.x > .75f && a.x < .85f && b.x > .85f && b.x < .95f) ||
                    (b.x > .75f && b.x < .85f && a.x > .85f && a.x < .95f))
                { hit.position = a; return true; }
            }
            if (Model.Mode.StartsWith("edge-"))
            {
                if (a.x > .75f && a.x < .85f) { hit.position = a; return true; }
                if (Model.Mode == "edge-both-blocked" && a.x > .85f && a.x < .95f && b.x > .75f && b.x < .85f) { hit.position = b; return true; }
            }
            var corner = new Vector3(2, .55f, 0);
            bool sourceCorner = Vector3.Distance(a, corner) < .0001f, targetCorner = Vector3.Distance(b, corner) < .0001f;
            if (Model.Mode.StartsWith("corner-"))
            {
                if (Model.Mode == "corner-near-not-corner" && a.x > 1.85f && a.x < 1.95f)
                { hit.position = a; return true; }
                if (Model.Mode == "corner-mid-hit" && a.x > 1.85f && a.x < 1.95f)
                { hit.position = Vector3.Lerp(a, b, .5f); return true; }
                if (Model.Mode == "corner-real-gap" && (sourceCorner || targetCorner))
                { hit.position = sourceCorner ? a : b; return true; }
                if (Model.Mode == "corner-end-roundoff")
                { hit.position = b; return targetCorner; }
                if (sourceCorner) { hit.position = a; return true; }
            }
            return (Model.Mode == "nav-hole" && Model.Critical(b.x)) ||
                (Model.Turn && b.z > .1f && b.x < 1.99f);
        }
        public static bool FindClosestEdge(Vector3 p, out NavMeshHit edge, NavMeshQueryFilter f)
        {
            float d = ((Model.Mode.StartsWith("edge-") && Model.Mode != "edge-too-far") ||
                (Model.Mode.StartsWith("native-") && Model.Mode != "native-edge-far")) ? .0005f : .002f;
            if (Model.Endpoint && p.x > .05f) d = .1f;
            edge = new NavMeshHit { position = p + Vector3.forward * d, distance = d };
            return Model.Mode != "edge-missing" && Model.Mode != "native-edge-missing";
        }
    }
}
namespace Unity.Collections { public enum Allocator { Temp, Persistent } public struct NativeArray<T> : IDisposable { T[] data; public NativeArray(int n, Allocator a) { data=new T[n]; } public int Length=>data.Length; public T this[int i] {get=>data[i]; set=>data[i]=value;} public void Dispose(){} } }
namespace UnityEngine.Experimental.AI
{
    [Flags] public enum PathQueryStatus { Failure=0, Success=1 }
    public struct PolygonId { public int Id; }
    public enum NavMeshPolyTypes { Ground, OffMeshConnection }
    public struct NavMeshLocation { public Vector3 position; public PolygonId polygon; }
    public struct NavMeshWorld { public static NavMeshWorld GetDefaultWorld() => new NavMeshWorld(); }
    public struct NavMeshQuery : IDisposable
    {
        public NavMeshQuery(NavMeshWorld world, Unity.Collections.Allocator allocator, int poolSize)
        { if (Model.Mode == "native-unavailable") throw new NotSupportedException("query unavailable"); }
        public void Dispose() { }
        static int Id(Vector3 p) => (int)MathF.Round(p.x * 1000 + p.z * 10000) + 1;
        public NavMeshLocation MapLocation(Vector3 p, Vector3 extent, int agentType, int areaMask)
        {
            if (Model.Mode == "native-invalid-map") return default;
            if (Model.Mode == "native-other-floor") p.y += 4;
            return new NavMeshLocation { position = p, polygon = new PolygonId { Id = Id(p) } };
        }
        public bool IsValid(PolygonId p) => p.Id != 0;
        public int GetAgentTypeIdForPolygon(PolygonId p) => Model.Mode == "native-wrong-agent" || (Model.Mode == "fan-agent" && p.Id==2) ? 1 : 0;
        public NavMeshPolyTypes GetPolygonType(PolygonId p) => Model.Mode == "native-link" || (Model.Mode == "fan-link" && p.Id==2) ? NavMeshPolyTypes.OffMeshConnection : NavMeshPolyTypes.Ground;
        public bool GetPortalPoints(PolygonId a, PolygonId b, out Vector3 left, out Vector3 right)
        { left=default; right=Vector3.right; if (Model.Mode.StartsWith("fan-")) { if(Model.Mode=="fan-far") {left.z=right.z=.1f;} if(Model.Mode=="fan-oneway" && a.Id>b.Id)return false; return Math.Abs(a.Id-b.Id)==1; } return Model.Mode == "native-adjacent-portal"; }
        public PathQueryStatus GetEdgesAndNeighbors(PolygonId p, Unity.Collections.NativeArray<Vector3> vertices, Unity.Collections.NativeArray<PolygonId> neighbors, Unity.Collections.NativeArray<byte> indices, out int vc, out int nc) {vc=3;nc=0; if(!Model.Mode.StartsWith("fan-")) return PathQueryStatus.Failure; if(Model.Mode=="fan-overflow"){nc=65;return PathQueryStatus.Success;} if(Model.Mode=="fan-disconnected" && p.Id==2)return PathQueryStatus.Success; if(p.Id>1)neighbors[nc++]=new PolygonId{Id=p.Id-1}; neighbors[nc++]=new PolygonId{Id=p.Id+1}; return PathQueryStatus.Success; }
        public NavMeshLocation MoveLocation(NavMeshLocation start, Vector3 target, int areaMask)
        {
            if (!Model.Mode.StartsWith("native-") || Model.Mode == "native-clamped" || Model.Mode == "native-corner-disconnected") return start;
            if (Model.Mode == "native-reverse-clamped" && start.position.x > target.x) return start;
            var result = new NavMeshLocation { position = target, polygon = new PolygonId { Id = Id(target) } };
            if (Model.Mode == "native-adjacent-portal" || Model.Mode == "native-disconnected-coincident") result.polygon.Id += 1000000;
            if (Model.Mode == "native-clamped-tiny") { result.position.x -= .0005f; result.polygon = start.polygon; }
            return result;
        }
    }
}
namespace MultiFPS { public static class GameManager { public const int environmentLayer = 1; } }
namespace ROM.CiudadDelSol
{
    public class Sector { public string Id, PlayableAncestor; }
    public class CiudadDelSolPopulationDefinition
    {
        public Sector[] Sectors = Array.Empty<Sector>(); public Vector3 AccessibilityOrigin;
    }
    public class CiudadRecoveryArea { public Vector3 Position; }
    public class Enemy
    {
        public Prefab Prefab = new Prefab(); public bool IsWildlife;
    }
    public class Prefab
    {
        public T GetComponent<T>() where T : class => Model.Agent as T;
        public CapsuleCollider Body => Model.Body;
    }
    public class CiudadEnemyPost { public string Id = "contract-post"; public Enemy Enemy = new Enemy(); public Vector3[] PatrolPoints; }
    public static class CiudadDelSolSpawnSafety
    {
        public static bool Recovery(Vector3 p, CiudadDelSolPopulationDefinition c) => false;
        public static bool Placement(UnityEngine.SceneManagement.Scene s, CiudadEnemyPost p, Vector3 r, out Vector3 result, out string e, Transform t, bool st)
        { result = r; e = ""; return true; } // Placement is outside this method's contract.
        public static string SupportDescription(RaycastHit h) => h.collider.transform.name;
        public static string ColliderPath(Collider c) => c.transform.name;
    }
}
