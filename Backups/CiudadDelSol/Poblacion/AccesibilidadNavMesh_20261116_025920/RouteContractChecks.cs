// Headless contract tests of the ACTUAL Route method, compiled with query doubles.
// Does not execute Unity, PhysX, Mirror, or the real NavMeshData.
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using ROM.CiudadDelSol;

static class Model
{
    public static string Mode;
    public static bool ShortBody, Turn, SawTurn, SawPose;
    public static float NavY(float x) => .05f + .25f * x;
    public static bool Critical(float x) => x > .85f && x < .96f;
    public static Collider Ground = new Collider("Salida_Meridional_Generated");
    public static Collider Interior = new Collider("Interior_no_autorizado");
    public static NavMeshAgent Agent = new NavMeshAgent();
    public static CapsuleCollider Body => new CapsuleCollider { radius = ShortBody ? .41f : .36f,
        height = ShortBody ? .9f : 1.9f, center = new Vector3(0, ShortBody ? .45f : .95f, 0) };

    public static int Main()
    {
        int failed = 0, count = 0;
        foreach (var mode in new[] { "stairs", "turn", "short", "partial", "nav-hole", "ground-hole", "other-floor", "interior", "cliff", "obstacle", "nav-gap" })
        {
            Mode = mode; Turn = mode == "turn"; ShortBody = mode == "short"; SawTurn = SawPose = false;
            var post = new CiudadEnemyPost();
            var method = typeof(CiudadDelSolEncounterValidation).GetMethod("Route", BindingFlags.NonPublic | BindingFlags.Static);
            object[] args = { new UnityEngine.SceneManagement.Scene(), new CiudadDelSolPopulationDefinition(), post,
                new Vector3(0, .05f, 0), new Vector3(2, .55f, Turn ? 2 : 0), false, null };
            bool result = (bool)method.Invoke(null, args);
            bool positive = mode == "stairs" || mode == "turn" || mode == "short";
            string expected = mode switch { "partial" => "Ruta PathPartial", "nav-hole" => "RUTA_NAVMESH_INTERRUMPIDA",
                "ground-hole" => "RUTA_SIN_APOYO", "other-floor" => "RUTA_SIN_NAVMESH", "interior" => "RUTA_APOYO_INCOMPATIBLE",
                "cliff" => "RUTA_APOYO_INCOMPATIBLE", "obstacle" => "RUTA_CAPSULA_OBSTRUIDA", "nav-gap" => "RUTA_SIN_NAVMESH", _ => "" };
            bool pass = result == positive && (!Turn || SawTurn) && (!ShortBody || SawPose) &&
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
        public static float Angle(Vector3 a, Vector3 b) => 0; // All model treads are horizontal.
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
    }
    public class Transform
    {
        public string name; public Transform parent; public Transform root => this;
        public Transform(string name) { this.name = name; }
        public static implicit operator bool(Transform t) => t != null;
    }
    public class Collider
    {
        public Transform transform;
        public Collider(string name) { transform = new Transform(name); }
    }
    public class CapsuleCollider { public float radius, height; public Vector3 center; }
    public struct RaycastHit { public Collider collider; public Vector3 point, normal; }
    public enum QueryTriggerInteraction { Ignore }
    public struct PhysicsScene
    {
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float distance, int mask, QueryTriggerInteraction q)
        {
            hit = default;
            if (Model.Mode == "ground-hole" && Model.Critical(origin.x)) return false;
            float y = .25f * MathF.Floor(origin.x + .00001f);
            if (Model.Mode == "cliff" && origin.x > .85f) y += .5f;
            if (origin.y < y || origin.y - y > distance) return false;
            hit = new RaycastHit { point = new Vector3(origin.x, y, origin.z), normal = Vector3.up,
                collider = Model.Mode == "interior" && Model.Critical(origin.x) ? Model.Interior : Model.Ground };
            return true;
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
    public struct NavMeshHit { public UnityEngine.Vector3 position; }
    public struct NavMeshBuildSettings { public float agentClimb, agentSlope; }
    public class NavMeshPath { public NavMeshPathStatus status; public UnityEngine.Vector3[] corners; }
    public static class NavMesh
    {
        public const int AllAreas = -1;
        public static NavMeshBuildSettings GetSettingsByID(int id) => new NavMeshBuildSettings { agentClimb = .36f, agentSlope = 45 };
        public static bool CalculatePath(Vector3 a, Vector3 b, NavMeshQueryFilter f, NavMeshPath p)
        {
            p.status = Model.Mode == "partial" ? NavMeshPathStatus.PathPartial : NavMeshPathStatus.PathComplete;
            p.corners = Model.Turn ? new[] { a, new Vector3(2, .55f, 0), b } : new[] { a, b }; return true;
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
            return (Model.Mode == "nav-hole" && Model.Critical(b.x)) ||
                (Model.Turn && b.z > .1f && b.x < 1.99f);
        }
    }
}
namespace MultiFPS { public static class GameManager { public const int environmentLayer = 1; } }
namespace ROM.CiudadDelSol
{
    public class Sector { public string PlayableAncestor; }
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
    public class CiudadEnemyPost { public Enemy Enemy = new Enemy(); public Vector3[] PatrolPoints; }
    public static class CiudadDelSolSpawnSafety
    {
        public static bool Recovery(Vector3 p, CiudadDelSolPopulationDefinition c) => false;
        public static bool Placement(UnityEngine.SceneManagement.Scene s, CiudadEnemyPost p, Vector3 r, out Vector3 result, out string e, Transform t, bool st)
        { result = r; e = ""; return true; } // Placement is outside this method's contract.
        public static string SupportDescription(RaycastHit h) => h.collider.transform.name;
        public static string ColliderPath(Collider c) => c.transform.name;
    }
}
