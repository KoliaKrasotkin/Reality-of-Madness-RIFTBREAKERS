using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace ROM.Gameplay
{
    internal sealed class CiudadDelSolJardinesAssistNavigation
    {
        internal const float ArrivalRadius = .25f;
        readonly NavMeshAgent agent;
        readonly Bounds court;

        internal CiudadDelSolJardinesAssistNavigation(GameObject npc, Bounds court)
        {
            this.court = court;
            agent = npc.AddComponent<NavMeshAgent>();
            agent.agentTypeID = 0;
            agent.radius = .30f;
            agent.height = 1.30f;
            agent.speed = 2.40f;
            agent.acceleration = 14f;
            agent.stoppingDistance = .15f;
            agent.autoRepath = false;
            agent.autoTraverseOffMeshLink = false;
        }

        internal bool Assign(Vector3 origin, Vector3 destination)
        {
            var path = new NavMeshPath();
            return agent.Warp(origin) && agent.isOnNavMesh &&
                agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete &&
                path.corners.All(court.Contains) && agent.SetPath(path);
        }

        internal bool IsNavigatingSafely => agent && agent.isOnNavMesh &&
            !agent.isOnOffMeshLink && court.Contains(agent.transform.position);

        internal bool Arrived(Vector3 destination) => IsNavigatingSafely &&
            !agent.pathPending && (agent.transform.position - destination).sqrMagnitude <= ArrivalRadius * ArrivalRadius;

        internal bool HasValidPath => agent.pathPending || (agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete);
    }
}
