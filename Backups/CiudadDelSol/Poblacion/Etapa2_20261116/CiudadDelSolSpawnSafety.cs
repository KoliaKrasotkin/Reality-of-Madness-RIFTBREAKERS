using System;
using System.Linq;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftEnemies;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace ROM.CiudadDelSol
{
    public static class CiudadDelSolSpawnSafety
    {
        public static Vector3 Resolve(Scene scene, CiudadEnemyPost post)
        {
            if (string.IsNullOrEmpty(post.AnchorPath)) return post.Position;
            int slash = post.AnchorPath.IndexOf('/');
            string rootName = slash < 0 ? post.AnchorPath : post.AnchorPath.Substring(0, slash);
            var root = scene.GetRootGameObjects().SingleOrDefault(g => g.name == rootName);
            var anchor = root ? (slash < 0 ? root.transform : root.transform.Find(post.AnchorPath.Substring(slash + 1))) : null;
            if (!anchor) throw new InvalidOperationException("Falta el ancla persistente: " + post.AnchorPath);
            return anchor.position;
        }

        public static bool Recovery(Vector3 point, CiudadDelSolPopulationDefinition config) =>
            config.RecoveryAreas != null && config.RecoveryAreas.Any(a =>
                a != null && (point - a.Position).sqrMagnitude <= a.Radius * a.Radius);

        public static bool HasHumanNear(Vector3 point, float radius)
        {
            foreach (var player in GameManager.Players)
                if (player && !player.BOT && player.MyCharacter && player.MyCharacter.Health.CurrentHealth > 0 &&
                    (player.MyCharacter.transform.position - point).sqrMagnitude <= radius * radius) return true;
            return false;
        }

        public static bool PlayersClear(Vector3 point, CiudadDelSolPopulationDefinition config)
        {
            foreach (var player in GameManager.Players)
            {
                if (!player || player.BOT || !player.MyCharacter) continue;
                float radius = config.PlayerExclusionRadius;
                if (player.MyCharacter.Health.CurrentHealth > 0 && NetworkTime.time - player.LastCombatAt < config.CombatQuietSeconds)
                    radius = Mathf.Max(radius, config.CombatExclusionRadius);
                if ((player.MyCharacter.transform.position - point).sqrMagnitude <= radius * radius) return false;
            }
            return true;
        }

        public static bool Placement(Scene scene, CiudadEnemyPost post, Vector3 reference, out Vector3 spawn, out string error)
        {
            spawn = default; error = "";
            var original = post.Enemy.Prefab;
            var agent = original.GetComponent<NavMeshAgent>(); var body = original.Body;
            var physics = scene.GetPhysicsScene();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!physics.Raycast(reference + Vector3.up, Vector3.down, out var floor, 2,
                GameManager.environmentLayer, QueryTriggerInteraction.Ignore) ||
                Mathf.Abs(floor.point.y - reference.y) > .36f || Vector3.Angle(floor.normal, Vector3.up) > 5)
            { error = "Sin apoyo físico plano en la cota configurada"; return false; }
            bool ancestor = false;
            for (var t = floor.collider.transform; t; t = t.parent) if (t.name == post.PlayableAncestor) ancestor = true;
            if (floor.collider.transform.root.name != post.PlayableRoot || !ancestor)
            { error = "El apoyo no pertenece al recorrido jugable autorizado"; return false; }
            if (!NavMesh.SamplePosition(floor.point, out var nav, .25f, filter) ||
                Mathf.Abs(nav.position.y - floor.point.y) > .36f ||
                !NavMesh.FindClosestEdge(nav.position, out var edge, filter) || edge.distance < agent.radius + .25f)
            { error = "NavMesh ausente o margen insuficiente al borde"; return false; }
            spawn = nav.position + Vector3.up * .03f;
            if (body.transform != original.transform || body.direction != 1 || original.transform.localScale != Vector3.one)
            { error = "Cápsula original no compatible con esta validación"; return false; }
            float half = Mathf.Max(0, body.height * .5f - body.radius);
            var center = spawn + body.center; var overlaps = new Collider[16];
            if (physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half,
                body.radius, overlaps, ~0, QueryTriggerInteraction.Ignore) > 0)
            { error = "Puesto ocupado por geometría, jugadores u otro NPC"; return false; }
            foreach (var delta in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                if (!physics.Raycast(spawn + delta * (body.radius + .1f) + Vector3.up * .3f, Vector3.down,
                    out var support, .75f, GameManager.environmentLayer, QueryTriggerInteraction.Ignore) ||
                    Mathf.Abs(support.point.y - floor.point.y) > .1f)
                { error = "Apoyo incompleto bajo la huella"; return false; }
            return true;
        }

        public static bool AttackOnSameFloor(RiftEnemyAgent enemy, Health target)
        {
            if (!enemy || !enemy.isServer || !target || target.gameObject.scene != enemy.gameObject.scene) return false;
            var agent = enemy.GetComponent<NavMeshAgent>();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            bool Support(Transform actor, out float y)
            {
                y = float.NaN;
                if (!enemy.gameObject.scene.GetPhysicsScene().Raycast(actor.position + Vector3.up * .25f, Vector3.down,
                    out var floor, 2.75f, GameManager.environmentLayer, QueryTriggerInteraction.Ignore) ||
                    !NavMesh.SamplePosition(floor.point, out var nav, .25f, filter) || Mathf.Abs(nav.position.y - floor.point.y) > .36f)
                    return false;
                y = floor.point.y; return true;
            }
            return Support(enemy.transform, out var a) && Support(target.transform, out var b) && Mathf.Abs(a - b) <= .36f;
        }
    }
}
