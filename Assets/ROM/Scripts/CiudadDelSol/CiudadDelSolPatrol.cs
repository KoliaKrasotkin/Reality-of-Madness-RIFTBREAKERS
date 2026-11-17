using System;
using Mirror;
using MultiFPS.Gameplay.RiftEnemies;
using UnityEngine;
using UnityEngine.AI;

namespace ROM.CiudadDelSol
{
    // Supplies idle destinations to the native agent; it never competes with it by writing the NavMeshAgent.
    [DisallowMultipleComponent]
    public sealed class CiudadDelSolPatrol : MonoBehaviour
    {
        RiftEnemyAgent enemy; NavMeshAgent navigation;
        Vector3[] points; Func<bool> idleCallback;
        string postId; int nextPoint, failures;
        float wait;
        double nextPlan, waitUntil;
        bool sawCombat, waiting, hasRoute;
        Vector3 destination;

        public void Configure(RiftEnemyAgent actor, string id, Vector3[] route, float dwell)
        {
            if (!actor || !actor.isServer || route == null || route.Length < 2 || actor.Definition.IsWildlife)
                throw new InvalidOperationException("Patrulla sin agente nativo o recorrido validado.");
            enemy = actor; navigation = actor.GetComponent<NavMeshAgent>(); points = (Vector3[])route.Clone();
            postId = id; wait = dwell; nextPoint = 0;
            idleCallback = TickIdle; enemy.ServerIdleRoute = idleCallback;
        }

        void Update()
        {
            // Observation only: all navigation requests originate in the native idle branch.
            if (enemy && enemy.isServer && (enemy.Target || enemy.Attacking || enemy.IsSkillCasting)) sawCombat = true;
        }

        bool TickIdle()
        {
            if (!enabled || !enemy || !enemy.isServer || enemy.Dead || points == null) return false;
            if (sawCombat)
            {
                sawCombat = false; waiting = false; hasRoute = false; nextPoint = 0; nextPlan = 0;
                enemy.ServerReturnAfterPatrolCombat(); // Original path, heal/reset, no warp.
                return true;
            }
            double now = NetworkTime.time;
            if (waiting)
            {
                enemy.ServerStopIdleRoute();
                if (now < waitUntil) return true;
                waiting = false; hasRoute = false; nextPoint = (nextPoint + 1) % points.Length; nextPlan = 0;
            }
            var goal = points[nextPoint];
            if (Vector3.Distance(enemy.transform.position, goal) <= .65f)
            { enemy.ServerStopIdleRoute(); hasRoute = false; waiting = true; waitUntil = now + wait; return true; }
            if (now < nextPlan) return true;
            nextPlan = now + .75;
            if (hasRoute && navigation.hasPath && !navigation.pathPending && navigation.pathStatus == NavMeshPathStatus.PathComplete &&
                (destination - goal).sqrMagnitude < .01f) return true;
            if (enemy.ServerTryIdleRoute(goal)) { destination = goal; hasRoute = true; failures = 0; return true; }
            enemy.ServerStopIdleRoute(); hasRoute = false; failures++;
            nextPlan = now + Math.Min(3, .75 * Math.Pow(2, Math.Min(2, failures - 1)));
            if (failures == 1) Debug.LogWarning("CIUDAD_PATRULLA RUTA_RECHAZADA " + postId + "; detenido, sin salto ni teletransporte.", this);
            return true;
        }

        void OnDisable()
        {
            if (enemy && enemy.ServerIdleRoute == idleCallback) enemy.ServerIdleRoute = null;
        }
    }
}
