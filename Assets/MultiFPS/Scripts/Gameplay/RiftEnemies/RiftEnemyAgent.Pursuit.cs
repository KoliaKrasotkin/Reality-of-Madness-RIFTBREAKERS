using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using MultiFPS.Gameplay.Gamemodes;

namespace MultiFPS.Gameplay.RiftEnemies
{
    public sealed partial class RiftEnemyAgent
    {
        const string CityRaidScene = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const double UnreachableGraceSeconds = 8;
        bool requestedCityRaidPursuit, savedLinkTraversal, changedLinkTraversal;
        int pathFailures;
        double unreachableSince = -1;
        Health retryTarget;
        public bool CityRaidPursuitActive => isServer && requestedCityRaidPursuit &&
            gameObject.scene.path == CityRaidScene && GameManager.Gamemode is Raid;
        public string PursuitPathState { get; private set; } = "Sin solicitud";
        public int PursuitPathFailures => pathFailures;

        bool WithinHomeLeash(Vector3 point) => CityRaidPursuitActive ||
            (point - Home).sqrMagnitude < EffectiveLeash * EffectiveLeash;

        void StartCityRaidPursuit()
        {
            if (!CityRaidPursuitActive) return;
            savedLinkTraversal = navigation.autoTraverseOffMeshLink;
            changedLinkTraversal = true;
            navigation.autoTraverseOffMeshLink = false;
        }

        void ResetCityRaidPursuit()
        {
            if (changedLinkTraversal && navigation) navigation.autoTraverseOffMeshLink = savedLinkTraversal;
            changedLinkTraversal = false;
            requestedCityRaidPursuit = false;
            pathFailures = 0; unreachableSince = -1; retryTarget = null;
            PursuitPathState = "Sin solicitud";
        }

        bool TryCityRaidPath(Vector3 destination)
        {
            if (navigation.isOnOffMeshLink)
            { PursuitPathState = "Enlace fuera de superficie: detenido, sin salto ni teletransporte"; return false; }
            var filter = new NavMeshQueryFilter { agentTypeID = navigation.agentTypeID, areaMask = navigation.areaMask };
            // A small projection plus physical floor agreement prevents snapping to another storey.
            if (!NavMesh.SamplePosition(destination, out var sample, .75f, filter) ||
                Mathf.Abs(sample.position.y - destination.y) > .36f)
            { PursuitPathState = "Destino sin proyección en su nivel"; return false; }
            var physics = gameObject.scene.GetPhysicsScene();
            if (!physics.Raycast(destination + Vector3.up * .2f, Vector3.down, out var floor, .75f,
                    GameManager.environmentLayer, QueryTriggerInteraction.Ignore) ||
                Mathf.Abs(floor.point.y - sample.position.y) > .36f)
            { PursuitPathState = "Destino sin apoyo físico en el nivel proyectado"; return false; }
            if (!PathAvoidsProtected(sample.position))
            { PursuitPathState = "Ruta no completa o protegida"; return false; }
            if (!navigation.SetPath(boundaryPath))
            { PursuitPathState = "NavMeshAgent rechazó la ruta"; return false; }
            navigation.isStopped = false;
            PursuitPathState = "PathComplete";
            return true;
        }

        void MoveCityRaid(Vector3 point, double now, bool chasing)
        {
            if (retryTarget != Target)
            { retryTarget = Target; pathFailures = 0; unreachableSince = -1; }
            if (TryCityRaidPath(point) || (chasing && Valid(Target) && TryCityRaidPath(Target.transform.position)))
            {
                pathFailures = 0; unreachableSince = -1;
                nextPath = now + .4 + (netId % 5) * .02;
                return;
            }
            Stop();
            if (navigation.isOnNavMesh && !navigation.isOnOffMeshLink) navigation.ResetPath();
            pathFailures++;
            nextPath = now + System.Math.Min(3, .75 * System.Math.Pow(2, System.Math.Min(2, pathFailures - 1)));
            if (unreachableSince < 0) unreachableSince = now;
            // Preserve the target through transient failures, but never attack or move along a partial path.
            if (!returning && now - unreachableSince >= UnreachableGraceSeconds)
            {
                ResetAggroState(); returning = true; attacking = skillCasting = false; SkillEnds = 0;
                retryTarget = null; unreachableSince = -1;
                PursuitPathState += "; regreso tras 8 s sin ruta";
            }
        }
    }
}
