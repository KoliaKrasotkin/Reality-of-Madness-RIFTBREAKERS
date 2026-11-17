using System;
using System.Linq;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftEnemies;
using MultiFPS.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    internal static partial class CiudadDelSolJardinesPlayCheck
    {
        const string OraclePrefab = "Assets/ROM/Enemies/Bestiary/broken-ember-oracle/broken-ember-oracle.prefab";
        const string OracleDefinition = "Assets/ROM/Enemies/Bestiary/broken-ember-oracle/broken-ember-oracle.asset";
        static OracleTrial oracleTrial;
        sealed class OracleTrial
        {
            internal RiftEnemyAgent Enemy;
            internal Vector3 Origin;
            internal bool Detected, Moved, Cast, Damaged, Dead, Removed;
            internal double NextBlockedLog;
        }

        static bool TryStartOracleTrial()
        {
            GameObject instance = null;
            try
            {
                var scene = SceneManager.GetActiveScene();
                var warrior = ClientFrontend.OwnedCharacterInstance;
                Check(developmentRunning && developmentOwnsHost && EditorApplication.isPlaying && scene.path == City &&
                    warriorTrialReady && NetworkServer.active && NetworkClient.ready && !NetworkServer.listen &&
                    warrior && warrior.isOwned && warrior.isServer && warrior.gameObject.scene == scene && oracleTrial == null,
                    "El Oráculo solo puede iniciarse una vez en el modo automático de Ciudad del Sol.");
                Check(demonTrial != null && demonTrial.Enemy && !RiftEnemyAgent.All.Any(e => e && e.gameObject.scene == scene &&
                    e.Definition && e.Definition.Id == "broken-ember-oracle"), "Falta el Diablillo original o ya existe un Oráculo.");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OraclePrefab);
                var definition = AssetDatabase.LoadAssetAtPath<RiftEnemyDefinition>(OracleDefinition);
                var original = prefab ? prefab.GetComponent<RiftEnemyAgent>() : null;
                var agent = prefab ? prefab.GetComponent<NavMeshAgent>() : null;
                Check(original && definition && original.Definition == definition && definition.Prefab == original &&
                    definition.Ranged && definition.Skill == RiftEnemySkill.EmberBolt && original.Body && original.Animator &&
                    original.Animator.runtimeAnimatorController && prefab.GetComponent<Health>() && prefab.GetComponent<NetworkIdentity>() &&
                    agent && agent.agentTypeID == 0 && !prefab.GetComponent<PlayerInstance>() && !prefab.GetComponent<CharacterInstance>(),
                    "El prefab o la definición originales del Oráculo no coinciden con el bestiario auditado.");
                uint assetId = NetworkIdentity.AssetGuidToUint(new Guid(AssetDatabase.AssetPathToGUID(OraclePrefab)));
                Check(DNNetworkManager.Instance.spawnPrefabs.Contains(prefab) &&
                    NetworkClient.prefabs.TryGetValue(assetId, out var registered) && registered == prefab,
                    "El Oráculo original debe estar registrado en el manager y en el cliente; no se alteró el registro.");
                var position = ValidateOraclePlacement(scene, original, agent, warrior.transform.position);
                instance = Object.Instantiate(prefab, position, Quaternion.Euler(0, 180, 0));
                instance.name = "TEMP Ciudad del Sol · Oráculo 01";
                SceneManager.MoveGameObjectToScene(instance, scene);
                var enemy = instance.GetComponent<RiftEnemyAgent>();
                enemy.ConfigureSpawn(1, cityRaidPursuit: true);
                NetworkServer.Spawn(instance, assetId);
                Check(enemy.isServer && enemy.netId != 0 && enemy.connectionToClient == null && enemy.CityRaidPursuitActive &&
                    instance.GetComponent<NavMeshAgent>().isOnNavMesh, "Mirror o el NavMesh rechazaron la instancia del Oráculo.");
                var state = new OracleTrial { Enemy = enemy, Origin = position };
                // Installed after OnStartServer. No prefab/definition or other enemy receives this gate.
                enemy.ServerAttackTargetGate = target => OracleFloorAllowed(state, target);
                oracleTrial = state;
                Health.ServerDamaged += ObserveOracleDamage;
                enemy.Health.RegisterNativeSpawn();
                Debug.Log($"CIUDAD_ORACULO CREADO netId={enemy.netId}, spawn={position:F3}, " +
                    $"vida={enemy.Health.CurrentHealth}, básico={definition.Damage}, especial={definition.SkillName}, " +
                    "persecución Raid activa, validación de plantas activa. Sin oleadas, respawn ni recompensas de ensayo.");
                return true;
            }
            catch (Exception error)
            {
                Health.ServerDamaged -= ObserveOracleDamage;
                oracleTrial = null;
                if (instance)
                {
                    if (NetworkServer.active && instance.GetComponent<NetworkIdentity>().netId != 0) NetworkServer.Destroy(instance);
                    else Object.Destroy(instance);
                }
                Debug.LogError("CIUDAD_ORACULO NO_CREADO: " + error.Message +
                    " El Host y el Diablillo se conservan. No se movió el spawn ni se reintentará automáticamente.");
                return false;
            }
        }

        static Vector3 ValidateOraclePlacement(Scene scene, RiftEnemyAgent prefab, NavMeshAgent agent, Vector3 entrance)
        {
            var roots = scene.GetRootGameObjects();
            var encounter = roots.Single(g => g.name == Encounter);
            var marker = encounter.transform.Find(Markers[0]);
            Check(marker && PatrolCourt.Contains(marker.position), "Falta el marcador Frontal del patio exterior validado de Jardines Dorados.");
            var garden = roots.Single(g => g.name == "Dungeon_Ciudad_del_Sol_Generated")
                .GetComponentsInChildren<Transform>().Single(t => t.name == "02 - Jardines Dorados");
            var physics = scene.GetPhysicsScene();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            Check(prefab.Body.transform == prefab.transform && prefab.Body.direction == 1 && prefab.transform.localScale == Vector3.one,
                "La cápsula del Oráculo necesita una validación diferente; no se modificará.");
            Vector3 Project(Vector3 point, bool clearance)
            {
                Check(physics.Raycast(point + Vector3.up, Vector3.down, out var floor, 2, ~0, QueryTriggerInteraction.Ignore) &&
                    floor.collider.transform.IsChildOf(garden) && Mathf.Abs(floor.point.y - 4f) <= .1f &&
                    Vector3.Angle(floor.normal, Vector3.up) <= 5,
                    $"Sin piso exterior plano de Jardines en {point:F3}; no se admite geometría intermedia.");
                Check(NavMesh.SamplePosition(floor.point, out var nav, .25f, filter) &&
                    Mathf.Abs(nav.position.y - floor.point.y) <= .36f &&
                    NavMesh.FindClosestEdge(nav.position, out var edge, filter) && edge.distance >= agent.radius + .25f,
                    $"Sin NavMesh o margen de borde suficiente en {point:F3}.");
                var position = nav.position + Vector3.up * .03f;
                if (clearance)
                {
                    var body = prefab.Body; var center = position + body.center;
                    float half = Mathf.Max(0, body.height * .5f - body.radius);
                    var overlaps = new Collider[16];
                    Check(physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half,
                        body.radius, overlaps, ~0, QueryTriggerInteraction.Ignore) == 0,
                        $"Cápsula del Oráculo obstruida en {position:F3}.");
                }
                return nav.position;
            }
            var ground = Project(marker.position, true);
            // Check actual room to manoeuvre, not just a single valid triangle.
            foreach (float x in new[] { -3f, 0f, 3f }) foreach (float z in new[] { -3f, 0f, 3f })
                Project(ground + new Vector3(x, 0, z), true);
            var path = new NavMeshPath();
            foreach (string name in Markers)
            {
                var reference = encounter.transform.Find(name);
                Check(reference && PatrolCourt.Contains(reference.position), "Falta referencia del patio: " + name);
                var point = Project(reference.position, true);
                Check(NavMesh.CalculatePath(ground, point, filter, path) && path.status == NavMeshPathStatus.PathComplete &&
                    NavMesh.CalculatePath(point, ground, filter, path) && path.status == NavMeshPathStatus.PathComplete,
                    "Ruta incompleta entre Oráculo y " + name);
            }
            // A 10 m firing lane, physically supported and unobstructed, within the approved court.
            for (int i = 1; i <= 10; i++)
            {
                var proposed = ground + Vector3.right * i;
                Check(PatrolCourt.Contains(proposed), "La línea de tiro de 10 m sale del patio aprobado.");
                var point = Project(proposed, true);
                Check(!NavMesh.Raycast(ground, point, out _, filter) &&
                    !physics.Raycast(ground + Vector3.up * 1.2f, (point - ground).normalized, out _,
                        Vector3.Distance(ground, point), GameManager.environmentLayer, QueryTriggerInteraction.Ignore),
                    "La línea de tiro del patio está cortada por geometría o navegación.");
            }
            Check(NavMesh.SamplePosition(entrance, out var start, .6f, filter) && Mathf.Abs(start.position.y - entrance.y) <= .36f &&
                NavMesh.CalculatePath(start.position, ground, filter, path) && path.status == NavMeshPathStatus.PathComplete &&
                NavMesh.CalculatePath(ground, start.position, filter, path) && path.status == NavMeshPathStatus.PathComplete,
                "No hay ruta bidireccional completa desde la entrada hasta el patio del Oráculo.");
            Debug.Log($"CIUDAD_ORACULO APOYO_OK Jardines/Encuentro 01, marcador={marker.position:F3}, suelo={ground:F3}; " +
                "despeje 6x6 m, línea de tiro 10 m, 4 referencias y conexión con entrada comprobadas. No se generó navegación.");
            return ground + Vector3.up * .03f;
        }

        static bool OracleFloorAllowed(OracleTrial state, Health target)
        {
            var enemy = state.Enemy;
            bool allowed = enemy && enemy.isServer && target && target.gameObject.scene == enemy.gameObject.scene;
            float enemyFloor = float.NaN, targetFloor = float.NaN;
            if (allowed)
            {
                var agent = enemy.GetComponent<NavMeshAgent>();
                var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                bool Support(Transform actor, out float y)
                {
                    y = float.NaN;
                    // Permit ordinary jumps above the same floor; never snap through a solid upper floor.
                    if (!enemy.gameObject.scene.GetPhysicsScene().Raycast(actor.position + Vector3.up * .25f, Vector3.down,
                        out var floor, 2.75f, GameManager.environmentLayer, QueryTriggerInteraction.Ignore) ||
                        !NavMesh.SamplePosition(floor.point, out var nav, .25f, filter) || Mathf.Abs(nav.position.y - floor.point.y) > .36f)
                        return false;
                    y = floor.point.y; return true;
                }
                allowed = Support(enemy.transform, out enemyFloor) && Support(target.transform, out targetFloor) &&
                    Mathf.Abs(enemyFloor - targetFloor) <= .36f;
            }
            if (!allowed && EditorApplication.timeSinceStartup >= state.NextBlockedLog)
            {
                state.NextBlockedLog = EditorApplication.timeSinceStartup + 5;
                Debug.Log($"CIUDAD_ORACULO DISPARO_BLOQUEADO: escena/apoyo/planta incompatible; piso Oráculo={enemyFloor:F3}, " +
                    $"piso objetivo={targetFloor:F3}. Se conserva la persecución por rutas completas.");
            }
            return allowed;
        }

        static void ObserveOracleDamage(Health victim, Health attacker, int amount)
        {
            var state = oracleTrial;
            if (state == null || !state.Enemy || attacker != state.Enemy.Health || state.Damaged) return;
            state.Damaged = true;
            Debug.Log($"CIUDAD_ORACULO DAÑO_SERVIDOR amount={amount}, objetivo={victim.name}; validación de planta aplicada antes del impacto.");
        }

        static void ObserveOracleTrial()
        {
            var state = oracleTrial;
            if (state == null) return;
            var enemy = state.Enemy;
            if (!enemy) { if (!state.Removed) { state.Removed = true; Debug.Log("CIUDAD_ORACULO CADAVER_RETIRADO; sin respawn automático."); } return; }
            if (!state.Detected && enemy.Target) { state.Detected = true; Debug.Log("CIUDAD_ORACULO OBJETIVO_DETECTADO: " + enemy.Target.name); }
            if (!state.Moved && Vector3.Distance(state.Origin, enemy.transform.position) > .5f)
            { state.Moved = true; Debug.Log("CIUDAD_ORACULO MOVIMIENTO_REAL: " + enemy.transform.position.ToString("F3")); }
            if (!state.Cast && enemy.IsSkillCasting) { state.Cast = true; Debug.Log("CIUDAD_ORACULO HABILIDAD_INICIADA: Brasa fracturada."); }
            if (!state.Dead && enemy.Dead) { state.Dead = true; Debug.Log("CIUDAD_ORACULO MUERTE registrada; retirada por el ciclo nativo."); }
        }

        static void StopOracleTrial()
        {
            Health.ServerDamaged -= ObserveOracleDamage;
            var state = oracleTrial; oracleTrial = null;
            if (state == null || !state.Enemy) return;
            state.Enemy.StopAllCoroutines();
            state.Enemy.ServerAttackTargetGate = null;
            if (NetworkServer.active && state.Enemy.netId != 0) state.Enemy.Despawn();
            else Object.Destroy(state.Enemy.gameObject);
        }
    }
}
