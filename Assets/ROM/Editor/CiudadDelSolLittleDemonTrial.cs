using System;
using System.Linq;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.Gamemodes;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.Gameplay.RiftEnemies;
using MultiFPS.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    internal static partial class CiudadDelSolJardinesPlayCheck
    {
        const string DemonMenu = "ROM/Dungeon Ciudad del Sol/Prueba directa Warrior/Diablillo/";
        const string DemonPrefab = "Assets/ROM/Enemies/LittleDemon/LittleDemon_Enemy.prefab";
        const string DemonDefinition = "Assets/ROM/Enemies/LittleDemon/LittleDemon.asset";
        static readonly Vector3 DemonSurface = new Vector3(30, -2, -170);
        static DemonTrial demonTrial;

        sealed class DemonTrial
        {
            internal RiftEnemyAgent Enemy;
            internal CharacterInstance Warrior;
            internal int WarriorLife, EnemyLife;
            internal Vector3 Origin;
            internal bool Moved, CorpseRemoved;
        }

        [MenuItem(DemonMenu + "1. Crear encuentro de prueba")]
        static void StartDemonTrial()
        {
            GameObject instance = null;
            try
            {
                Check(demonTrial == null, "Retirá el ensayo anterior antes de crear otro Diablillo.");
                var scene = SceneManager.GetActiveScene();
                var warrior = ClientFrontend.OwnedCharacterInstance;
                Check(EditorApplication.isPlaying && !EditorApplication.isPaused && scene.path == City &&
                    NetworkServer.active && NetworkClient.active && !NetworkServer.listen &&
                    NetworkServer.localConnection != null && NetworkClient.ready && GameSync.Singleton &&
                    GameManager.Gamemode is Raid && GameManager.Gamemode.gameObject.name == "TEMP Ciudad del Sol Raid (solo Play)" &&
                    warrior && warrior.isOwned && warrior.isServer && warrior.Health.CurrentHealth > 0 &&
                    warrior.connectionToClient == NetworkServer.localConnection && warrior.gameObject.scene == scene,
                    "Iniciá primero el Host local de ensayo con el Warrior vivo en Ciudad del Sol.");
                Check(Vector3.Distance(warrior.transform.position, BridgeSpawn) <= 2,
                    "Volvé al inicio de la pasarela (30, -1.95, -206) antes de preparar el encuentro.");
                Check(!RiftEnemyAgent.All.Any(e => e && e.gameObject.scene == scene),
                    "Ya existe un enemigo nativo en esta escena; no se agregará otro.");
                var prefab = ReadDemonPrefab();
                uint assetId = NetworkIdentity.AssetGuidToUint(new Guid(AssetDatabase.AssetPathToGUID(DemonPrefab)));
                Check(DNNetworkManager.Instance && DNNetworkManager.Instance.spawnPrefabs.Contains(prefab) &&
                    NetworkClient.prefabs.TryGetValue(assetId, out var registered) && registered == prefab,
                    "El manager y el cliente deben tener registrado el prefab original del Diablillo. No se cambió ningún registro.");
                var position = ValidateDemonPlacement(scene, prefab, warrior.transform.position);
                var combat = warrior.Health ? AbilityController.For(warrior.Health) : null;
                Check(combat && combat.Prototype, "Esperá a que se configure el combate original del Warrior.");
                var definition = prefab.GetComponent<RiftEnemyAgent>().Definition;
                float detection = RiftAggroTuning.Load.Detection(definition.Detection, 1, combat.Level);
                Check(Vector3.Distance(warrior.transform.position, position) > detection + 4,
                    "La distancia actual no conserva el margen inicial de detección.");

                instance = Object.Instantiate(prefab, position, Quaternion.Euler(0, 180, 0));
                instance.name = "TEMP Ciudad del Sol · Diablillo 01";
                SceneManager.MoveGameObjectToScene(instance, scene);
                var enemy = instance.GetComponent<RiftEnemyAgent>();
                enemy.ConfigureSpawn(1); // No leash override, Horde population or reward handler.
                NetworkServer.Spawn(instance, assetId);
                Check(enemy.isServer && enemy.netId != 0 && enemy.connectionToClient == null &&
                    instance.GetComponent<NavMeshAgent>().isOnNavMesh,
                    "Mirror o el NavMesh no aceptaron al enemigo; se retirará la instancia.");
                enemy.Health.RegisterNativeSpawn();
                demonTrial = new DemonTrial { Enemy = enemy, Warrior = warrior, Origin = position,
                    WarriorLife = warrior.Health.CurrentHealth, EnemyLife = enemy.Health.CurrentHealth };
                EditorApplication.update += ObserveDemonTrial;
                EditorApplication.playModeStateChanged += DemonPlayChanged;
                AssemblyReloadEvents.beforeAssemblyReload += StopDemonTrial;
                Debug.Log($"CIUDAD_DIABLILLO CREADO netId={enemy.netId}, posición={enemy.transform.position:F3}, " +
                    $"vida={enemy.Health.CurrentHealth}, leash={enemy.EffectiveLeash:F2}, detección={detection:F2}. " +
                    "Acercate por la pasarela. Un único enemigo, sin oleadas, respawn ni recompensas del ensayo.");
            }
            catch (Exception error)
            {
                if (instance)
                {
                    if (instance.GetComponent<NetworkIdentity>().netId != 0 && NetworkServer.active) NetworkServer.Destroy(instance);
                    else Object.Destroy(instance);
                }
                Debug.LogError("CIUDAD_DIABLILLO No se creó el encuentro: " + error.Message);
            }
        }

        [MenuItem(DemonMenu + "2. Retirar encuentro de prueba")]
        static void StopDemonTrial()
        {
            EditorApplication.update -= ObserveDemonTrial;
            EditorApplication.playModeStateChanged -= DemonPlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= StopDemonTrial;
            var state = demonTrial;
            demonTrial = null;
            if (state == null) return;
            if (state.Enemy)
            {
                if (NetworkServer.active && state.Enemy.netId != 0) NetworkServer.Destroy(state.Enemy.gameObject);
                else Object.Destroy(state.Enemy.gameObject);
            }
            Debug.Log("CIUDAD_DIABLILLO Ensayo retirado. No se modificó la escena ni el Host.");
        }

        static void DemonPlayChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) StopDemonTrial();
        }

        static void ObserveDemonTrial()
        {
            var state = demonTrial;
            if (state == null) return;
            if (!EditorApplication.isPlaying || !NetworkServer.active || !state.Warrior ||
                state.Warrior.gameObject.scene.path != City || SceneManager.GetActiveScene().path != City)
            { StopDemonTrial(); return; }
            if (!state.Enemy)
            {
                if (!state.CorpseRemoved) { state.CorpseRemoved = true; Debug.Log("CIUDAD_DIABLILLO Cadáver retirado por el ciclo nativo. No se recreará automáticamente."); }
                return;
            }
            if (!state.Moved && Vector3.Distance(state.Origin, state.Enemy.transform.position) > .5f)
            { state.Moved = true; Debug.Log("CIUDAD_DIABLILLO DESPLAZAMIENTO_REAL " + state.Enemy.transform.position.ToString("F3")); }
            if (state.EnemyLife != state.Enemy.Health.CurrentHealth)
            {
                Debug.Log($"CIUDAD_DIABLILLO VIDA_ENEMIGO {state.EnemyLife} → {state.Enemy.Health.CurrentHealth}");
                state.EnemyLife = state.Enemy.Health.CurrentHealth;
            }
            if (state.WarriorLife != state.Warrior.Health.CurrentHealth)
            {
                Debug.Log($"CIUDAD_DIABLILLO VIDA_WARRIOR {state.WarriorLife} → {state.Warrior.Health.CurrentHealth}");
                state.WarriorLife = state.Warrior.Health.CurrentHealth;
            }
        }

        static GameObject ReadDemonPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DemonPrefab);
            var definition = AssetDatabase.LoadAssetAtPath<RiftEnemyDefinition>(DemonDefinition);
            var enemy = prefab ? prefab.GetComponent<RiftEnemyAgent>() : null;
            var agent = prefab ? prefab.GetComponent<NavMeshAgent>() : null;
            Check(prefab && enemy && definition && definition.Prefab == enemy && enemy.Definition == definition &&
                prefab.GetComponent<NetworkIdentity>() && prefab.GetComponent<Health>() &&
                enemy.Animator && enemy.Animator.runtimeAnimatorController && enemy.Body &&
                agent && agent.agentTypeID == 0 && Mathf.Abs(agent.radius - .3f) < .001f &&
                Mathf.Abs(agent.height - 1.3f) < .001f && Mathf.Abs(definition.Leash - 36) < .001f &&
                !prefab.GetComponent<PlayerInstance>() && !prefab.GetComponent<CharacterInstance>(),
                "El prefab nativo o sus dependencias no coinciden con el Diablillo auditado.");
            return prefab;
        }

        static Vector3 ValidateDemonPlacement(Scene scene, GameObject prefab, Vector3 warriorStart)
        {
            var source = prefab.GetComponent<NavMeshAgent>();
            var filter = new NavMeshQueryFilter { agentTypeID = source.agentTypeID, areaMask = source.areaMask };
            NavMeshHit edge = default;
            var physics = scene.GetPhysicsScene();
            Check(physics.Raycast(DemonSurface + Vector3.up, Vector3.down, out var floor, 2, ~0, QueryTriggerInteraction.Ignore) &&
                floor.collider.gameObject.scene == scene && floor.collider.transform.root.name == BridgeRoot &&
                Mathf.Abs(floor.point.y - DemonSurface.y) < .05f && Vector3.Angle(floor.normal, Vector3.up) <= 5,
                "El punto previsto no tiene el apoyo físico plano esperado en Salida Meridional.");
            Check(NavMesh.SamplePosition(floor.point, out var nav, .5f, filter) && Mathf.Abs(nav.position.y - floor.point.y) <= .36f &&
                NavMesh.FindClosestEdge(nav.position, out edge, filter) && edge.distance >= source.radius + .25f,
                "No hay navegación Humanoid 0 con margen suficiente respecto del borde.");
            var position = nav.position + Vector3.up * .03f;
            var body = prefab.GetComponent<RiftEnemyAgent>().Body;
            Check(body.direction == 1 && prefab.transform.localScale == Vector3.one,
                "La cápsula original necesita una validación diferente; no se modificará.");
            var center = position + body.center;
            float half = Mathf.Max(0, body.height / 2 - body.radius);
            var overlaps = new Collider[8];
            Check(physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half,
                body.radius, overlaps, ~0, QueryTriggerInteraction.Ignore) == 0,
                "El punto previsto está ocupado por un collider. No se desplazó automáticamente el spawn.");
            foreach (var offset in new[] { Vector3.zero, Vector3.right * .5f, Vector3.left * .5f, Vector3.forward * .5f, Vector3.back * .5f })
                Check(physics.Raycast(position + offset + Vector3.up * .5f, Vector3.down, out var support, 1, ~0, QueryTriggerInteraction.Ignore) &&
                    Mathf.Abs(support.point.y - floor.point.y) <= .08f,
                    "Falta apoyo físico bajo la huella del enemigo.");
            Check(NavMesh.SamplePosition(warriorStart, out var start, .6f, filter) &&
                Mathf.Abs(start.position.y - warriorStart.y) <= .36f, "El inicio del Warrior no proyecta a su cota.");
            var path = new NavMeshPath();
            Check(NavMesh.CalculatePath(start.position, nav.position, filter, path) && path.status == NavMeshPathStatus.PathComplete &&
                NavMesh.CalculatePath(nav.position, start.position, filter, path) && path.status == NavMeshPathStatus.PathComplete,
                "No existe un camino completo en ambos sentidos entre el inicio y el encuentro.");
            Check(path.corners.All(p => Mathf.Abs(p.y - floor.point.y) <= .36f), "La ruta abandona el nivel de la pasarela.");
            Debug.Log($"CIUDAD_DIABLILLO APOYO_OK collider={floor.collider.name}, suelo={floor.point:F3}, " +
                $"spawn={position:F3}, borde={edge.distance:F2} m, distancia inicial={Vector3.Distance(warriorStart, position):F2} m, ruta bidireccional completa.");
            return position;
        }

        public static void VerifyLittleDemonInBatch()
        {
            Check(Application.isBatchMode, "Esta comprobación requiere Unity batch.");
            var scene = EditorSceneManager.OpenScene(City, OpenSceneMode.Single);
            Physics.SyncTransforms();
            ValidateDemonPlacement(scene, ReadDemonPrefab(), BridgeSpawn);
            Debug.Log("CIUDAD_DIABLILLO PREFLIGHT_OK sin guardar escena, NavMesh ni prefab.");
        }
    }
}
