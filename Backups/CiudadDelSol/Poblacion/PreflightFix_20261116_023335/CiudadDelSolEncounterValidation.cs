using System;
using MultiFPS;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace ROM.CiudadDelSol
{
    // Read-only checks against the real scene. Never bakes, repairs, changes colliders, or relocates a rejected point.
    public static class CiudadDelSolEncounterValidation
    {
        static readonly string[] Connections = { "01-02", "01-06", "02-03", "03-04", "03-05", "03-06", "04-05", "05-07", "06-07" };

        static bool Exterior(Collider collider, CiudadDelSolPopulationDefinition config)
        {
            if (collider.transform.root.name == "Salida_Meridional_Generated") return true;
            if (collider.transform.root.name != "Dungeon_Ciudad_del_Sol_Generated") return false;
            for (var t = collider.transform; t; t = t.parent)
            {
                foreach (var sector in config.Sectors) if (t.name == sector.PlayableAncestor) return true;
                if (t.name.Contains("ancho util"))
                    foreach (var id in Connections) if (t.name.StartsWith(id + " - ", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public static bool Validate(Scene scene, CiudadDelSolPopulationDefinition config, CiudadEnemyPost post,
            Vector3 reference, Transform ignore, out Vector3[] patrol, out string error)
        {
            patrol = Array.Empty<Vector3>();
            if (CiudadDelSolSpawnSafety.Recovery(reference, config))
            { error = "Puesto dentro de recuperación"; return false; }
            if (!CiudadDelSolSpawnSafety.Placement(scene, post, reference, out var spawn, out error, ignore, true)) return false;
            var agent = post.Enemy.Prefab.GetComponent<NavMeshAgent>();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!Project(scene, config, config.AccessibilityOrigin, filter, out var entrance, out error)) return false;
            if (!Route(scene, config, post, entrance, spawn, false, out error) ||
                !Route(scene, config, post, spawn, entrance, false, out error)) return false;
            if (post.PatrolPoints == null || post.PatrolPoints.Length == 0) return true;
            if (post.Enemy.IsWildlife || post.PatrolPoints.Length < 2)
            { error = "Patrulla inválida: fauna conserva su conducta nativa; se requieren dos puntos"; return false; }
            patrol = new Vector3[post.PatrolPoints.Length];
            for (int i = 0; i < patrol.Length; i++)
            {
                if (CiudadDelSolSpawnSafety.Recovery(post.PatrolPoints[i], config) ||
                    !CiudadDelSolSpawnSafety.Placement(scene, post, post.PatrolPoints[i], out patrol[i], out error, ignore, true))
                { error = "Patrulla " + i + ": recuperación o " + error; return false; }
                if (!Route(scene, config, post, patrol[i], spawn, true, out error) ||
                    !Route(scene, config, post, spawn, patrol[i], true, out error)) return false;
            }
            for (int i = 0; i < patrol.Length; i++)
            {
                var a = patrol[i]; var b = patrol[(i + 1) % patrol.Length];
                if (Vector3.Distance(a, b) < 2) { error = "Puntos consecutivos de patrulla demasiado próximos"; return false; }
                if (!Route(scene, config, post, a, b, true, out error) || !Route(scene, config, post, b, a, true, out error)) return false;
            }
            return true;
        }

        static bool Project(Scene scene, CiudadDelSolPopulationDefinition config, Vector3 point, NavMeshQueryFilter filter,
            out Vector3 navPoint, out string error)
        {
            navPoint = default; error = "";
            if (!scene.GetPhysicsScene().Raycast(point + Vector3.up * .5f, Vector3.down, out var floor, 1,
                GameManager.environmentLayer, QueryTriggerInteraction.Ignore) || !Exterior(floor.collider, config) ||
                Mathf.Abs(point.y - floor.point.y) > .36f || !NavMesh.SamplePosition(floor.point, out var nav, .25f, filter) ||
                Mathf.Abs(nav.position.y - floor.point.y) > .36f)
            { error = "Sin apoyo exterior/NavMesh en su nivel: " + point.ToString("F3"); return false; }
            navPoint = nav.position; return true;
        }

        public static bool RecoveryArea(Scene scene, CiudadDelSolPopulationDefinition config, CiudadRecoveryArea area, out string error)
        {
            var filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
            var hits = new Collider[8]; error = "";
            foreach (var offset in new[] { Vector3.zero, Vector3.right * 2, Vector3.left * 2, Vector3.forward * 2, Vector3.back * 2 })
            {
                if (!Project(scene, config, area.Position + offset, filter, out var p, out error)) return false;
                if (scene.GetPhysicsScene().OverlapCapsule(p + Vector3.up * .35f, p + Vector3.up * 1.71f,
                    .32f, hits, GameManager.environmentLayer, QueryTriggerInteraction.Ignore) > 0)
                { error = "Recuperación sin despeje para el Warrior en " + p.ToString("F3"); return false; }
            }
            return true;
        }

        static bool Route(Scene scene, CiudadDelSolPopulationDefinition config, CiudadEnemyPost post,
            Vector3 from, Vector3 to, bool patrol, out string error)
        {
            error = "";
            var agent = post.Enemy.Prefab.GetComponent<NavMeshAgent>();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(from, to, filter, path) || path.status != NavMeshPathStatus.PathComplete)
            { error = $"Ruta {path.status}: {from:F3} -> {to:F3}"; return false; }
            var body = post.Enemy.Prefab.Body; var physics = scene.GetPhysicsScene(); var hits = new Collider[8];
            for (int i = 1; i < path.corners.Length; i++)
            {
                var a = path.corners[i - 1]; var b = path.corners[i];
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / .5f));
                for (int j = 0; j <= steps; j++)
                {
                    var p = Vector3.Lerp(a, b, (float)j / steps);
                    if (!Project(scene, config, p, filter, out _, out error)) return false;
                    if (patrol && CiudadDelSolSpawnSafety.Recovery(p, config))
                    { error = "La patrulla cruza recuperación en " + p.ToString("F3"); return false; }
                    // The approved Humanoid step is 0.36m: test torso/head clearance above that step band.
                    // Feet remain checked against real geometry at every sample; no geometry or agent setting changes.
                    float top = p.y + body.center.y + body.height * .5f + .03f;
                    float bottom = Mathf.Min(p.y + body.center.y - body.height * .5f + .39f, top - body.radius * 2);
                    var offset = new Vector3(body.center.x, 0, body.center.z);
                    if (physics.OverlapCapsule(new Vector3(p.x, bottom + body.radius, p.z) + offset,
                        new Vector3(p.x, top - body.radius, p.z) + offset, body.radius, hits,
                        GameManager.environmentLayer, QueryTriggerInteraction.Ignore) > 0)
                    { error = "Torso/cabeza obstruidos en " + p.ToString("F3"); return false; }
                }
            }
            return true;
        }
    }
}
