using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.Gamemodes;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.Gameplay.RiftEnemies;
using ROM.Progression;
using UnityEngine;
using UnityEngine.AI;

namespace ROM.CiudadDelSol
{
    // Server-owned spawn lifecycle only. Native RiftEnemyAgent remains the sole combat/movement authority.
    [DisallowMultipleComponent]
    public sealed partial class CiudadDelSolPopulation : MonoBehaviour
    {
        sealed class Post
        {
            internal CiudadEnemyPost Definition;
            internal Vector3 Reference;
            internal RiftEnemyAgent Occupant;
            internal readonly CiudadDelSolRespawnSchedule Schedule = new CiudadDelSolRespawnSchedule();
            internal double RetryAt;
            internal string LastReason;
            internal bool Approved;
            internal Vector3[] Patrol = Array.Empty<Vector3>();
        }
        readonly List<Post> posts = new List<Post>();
        readonly Dictionary<RiftEnemyAgent, Post> owners = new Dictionary<RiftEnemyAgent, Post>();
        [SerializeField] CiudadDelSolPopulationDefinition configuration;
        bool initialized, shuttingDown, faulted;
        double nextTick;
        public int OccupiedCount => posts.Count(p => p.Occupant);

        public void Initialize(CiudadDelSolPopulationDefinition source, IReadOnlyDictionary<string, RiftEnemyAgent> adopted)
        {
            Require(!initialized && source && NetworkServer.active && GameManager.Gamemode is Raid && GameSync.Singleton &&
                gameObject.scene.path == CiudadDelSolPopulationDefinition.ScenePath, "Población: contexto servidor Raid inválido.");
            Require(FindObjectsByType<CiudadDelSolPopulation>(FindObjectsSortMode.None).Count(p => p.gameObject.scene == gameObject.scene) == 1,
                "Ya hay otro gestor de población en Ciudad del Sol.");
            ValidateConfiguration(source);
            Require(adopted != null && adopted.Count <= source.GlobalMaximum, "Adopción ausente o presupuesto global excedido.");
            Require(!RiftEnemyAgent.All.Any(e => e && e.gameObject.scene == gameObject.scene && !adopted.Values.Contains(e)),
                "Hay enemigos ajenos a la adopción; no se duplicarán ni retirarán automáticamente.");
            var prepared = new List<Post>();
            foreach (var definition in source.Posts)
            {
                var post = new Post { Definition = definition, Reference = CiudadDelSolSpawnSafety.Resolve(gameObject.scene, definition),
                    Approved = !source.ValidateExteriorEncounters };
                Require(Finite(post.Reference.x) && Finite(post.Reference.y) && Finite(post.Reference.z), "Ancla con coordenadas inválidas: " + definition.Id);
                if (!source.ValidateExteriorEncounters)
                    Require(!CiudadDelSolSpawnSafety.Recovery(post.Reference, source), "Puesto dentro de recuperación: " + definition.Id);
                if (adopted.TryGetValue(definition.Id, out var enemy))
                {
                    Require(enemy && enemy.isServer && !enemy.Dead && enemy.Health.CurrentHealth == enemy.Health.MaxHealth &&
                        enemy.CityRaidPursuitActive && enemy.ReleaseToPool == null &&
                        enemy.gameObject.scene == gameObject.scene && enemy.Definition == definition.Enemy &&
                        Vector3.Distance(post.Reference, enemy.transform.position) < .75f,
                        "El actor adoptado no coincide con el puesto: " + definition.Id);
                    post.Occupant = enemy;
                }
                prepared.Add(post);
            }
            Require(adopted.Keys.All(id => prepared.Any(p => p.Definition.Id == id)), "Hay adopciones sin puesto persistente.");
            foreach (var sector in source.Sectors)
                Require(prepared.Count(p => p.Occupant && p.Definition.SectorId == sector.Id) <= sector.Maximum,
                    "La adopción excede el sector " + sector.Id);

            // Clone settings so Play never changes the authored resource. Enemy assets remain original references.
            configuration = Instantiate(source); configuration.hideFlags = HideFlags.HideAndDontSave;
            for (int i = 0; i < prepared.Count; i++) prepared[i].Definition = configuration.Posts[i];
            posts.AddRange(prepared);
            foreach (var post in posts) if (post.Occupant) { Scale(post, post.Occupant); Attach(post, post.Occupant); }
            PrepareEncounterValidation();
            initialized = true;
            Debug.Log($"CIUDAD_POBLACION INICIADA: {configuration.Stage}; {posts.Count} puestos; " +
                $"adoptados={OccupiedCount}, máximo={configuration.GlobalMaximum}. Sin oleadas ni retirada por distancia.", this);
        }

        static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static void ValidateConfiguration(CiudadDelSolPopulationDefinition source)
        {
            Require(source.Posts != null && source.Posts.Length > 0 && source.Sectors != null && source.Sectors.Length > 0 &&
                source.CompatibleEnemies != null && source.CompatibleEnemies.Length > 0 && source.GlobalMaximum > 0 && source.GlobalMaximum <= 64 &&
                Finite(source.TickSeconds) && source.TickSeconds >= .25f && Finite(source.RetrySeconds) && source.RetrySeconds >= 1 &&
                Finite(source.PlayerExclusionRadius) && source.PlayerExclusionRadius >= 5 &&
                Finite(source.CombatExclusionRadius) && source.CombatExclusionRadius >= source.PlayerExclusionRadius &&
                Finite(source.CombatQuietSeconds) && source.CombatQuietSeconds >= 1, "Configuración de población inválida.");
            Require(source.Sectors.All(s => s != null && !string.IsNullOrWhiteSpace(s.Id) && s.Maximum > 0 && s.Maximum <= source.GlobalMaximum) &&
                source.Sectors.Select(s => s.Id).Distinct().Count() == source.Sectors.Length, "Sectores inválidos o duplicados.");
            Require(source.Posts.All(p => p != null && !string.IsNullOrWhiteSpace(p.Id)) &&
                source.Posts.Select(p => p.Id).Distinct().Count() == source.Posts.Length, "Puestos duplicados o sin identidad.");
            foreach (var definition in source.CompatibleEnemies)
            {
                Require(definition && !definition.IsWildlife, "Ciudad del Sol no admite fauna en sus configuraciones de población.");
                var prefab = definition ? definition.Prefab : null;
                Require(prefab && prefab.Definition == definition && prefab.GetComponent<NetworkIdentity>() && prefab.GetComponent<Health>() &&
                    prefab.GetComponent<NavMeshAgent>() && prefab.GetComponent<NavMeshAgent>().agentTypeID == 0 && prefab.Body &&
                    prefab.Animator && prefab.Animator.runtimeAnimatorController &&
                    !prefab.GetComponent<PlayerInstance>() && !prefab.GetComponent<CharacterInstance>(),
                    "Catálogo contiene un recurso ajeno al bestiario nativo: " + (definition ? definition.Id : "null"));
                Require(NetworkClient.active == false || NetworkClient.prefabs.Values.Contains(prefab.gameObject),
                    "El cliente no tiene registrado el prefab " + definition.Id);
                Require(NetworkManager.singleton && NetworkManager.singleton.spawnPrefabs.Contains(prefab.gameObject),
                    "El servidor no tiene registrado el prefab " + definition.Id);
            }
            Require(source.CompatibleEnemies.Select(d => d.Id).Distinct().Count() == source.CompatibleEnemies.Length,
                "Identidades duplicadas en el catálogo.");
            foreach (var post in source.Posts)
                Require(post.Enemy && source.CompatibleEnemies.Contains(post.Enemy) && source.Sectors.Any(s => s.Id == post.SectorId) &&
                    !string.IsNullOrWhiteSpace(post.PlayableRoot) && !string.IsNullOrWhiteSpace(post.PlayableAncestor) && post.Level >= 1 &&
                    Finite(post.RespawnSeconds) && post.RespawnSeconds >= 1 && Finite(post.ActivationRadius) && post.ActivationRadius > 0 &&
                    Finite(post.HealthMultiplier) && post.HealthMultiplier >= 1 && Finite(post.DamageMultiplier) && post.DamageMultiplier >= 1 &&
                    Finite(post.Position.x) && Finite(post.Position.y) && Finite(post.Position.z) && Finite(post.Yaw) &&
                    Enum.IsDefined(typeof(CiudadEnemyRank), post.Rank) &&
                    (!post.Enemy.IsBoss || post.Rank >= CiudadEnemyRank.MiniBoss), "Puesto inválido: " + post.Id);
            Require(source.MaximumInitialGroupSize >= 1 && source.MaximumInitialGroupSize <= 4, "Tamaño de grupo inválido.");
            if (source.ValidateExteriorEncounters)
            {
                Require(source.Sectors.All(s => !string.IsNullOrWhiteSpace(s.PlayableAncestor)), "Sector sin raíz física.");
                Require(Finite(source.AccessibilityOrigin.x) && Finite(source.AccessibilityOrigin.y) && Finite(source.AccessibilityOrigin.z), "Entrada inválida.");
                foreach (var post in source.Posts)
                {
                    Require(!string.IsNullOrWhiteSpace(post.EncounterId) && Finite(post.PatrolWaitSeconds) && post.PatrolWaitSeconds >= 0,
                        "Encuentro/patrulla inválidos: " + post.Id);
                    if (post.PatrolPoints != null) Require(post.PatrolPoints.Length <= 16 &&
                        post.PatrolPoints.All(p => Finite(p.x) && Finite(p.y) && Finite(p.z)), "Coordenadas de patrulla inválidas: " + post.Id);
                }
                foreach (var group in source.Posts.GroupBy(p => p.EncounterId)) Require(group.Count() <= source.MaximumInitialGroupSize &&
                    group.Select(p => p.SectorId).Distinct().Count() == 1, "Grupo demasiado grande o repartido entre sectores: " + group.Key);
            }
            if (source.RecoveryAreas != null) foreach (var area in source.RecoveryAreas)
                Require(area != null && Finite(area.Radius) && area.Radius > 0 &&
                    Finite(area.Position.x) && Finite(area.Position.y) && Finite(area.Position.z), "Área de recuperación inválida.");
        }

        void Attach(Post post, RiftEnemyAgent enemy)
        {
            post.Occupant = enemy; post.Schedule.Spawned(); post.LastReason = null;
            owners.Add(enemy, post);
            enemy.ServerKilled += Killed;
            enemy.ServerTargetGate = target => target && target.gameObject.scene == gameObject.scene &&
                !CiudadDelSolSpawnSafety.Recovery(target.transform.position, configuration);
            enemy.ServerAttackTargetGate = target => enemy.ServerTargetGate != null && enemy.ServerTargetGate(target) &&
                CiudadDelSolSpawnSafety.AttackOnSameFloor(enemy, target);
            InstallPatrol(post);
        }

        void Killed(RiftEnemyAgent enemy, Health killer)
        {
            if (!NetworkServer.active || shuttingDown || !owners.TryGetValue(enemy, out var post) ||
                post.Schedule.Phase != CiudadDelSolRespawnSchedule.State.Occupied) return;
            try
            {
                post.Schedule.Lost(NetworkTime.time, post.Definition.RespawnSeconds, post.Definition.AutoRespawn);
                // Match native kill XP without depending on Horde or sharing XP as party quest credit.
                // Lost consumes this life before dispatch, so duplicate death callbacks cannot award twice.
                var player = RiftClassCombat.Owner(killer);
                var journal = player ? player.GetComponent<RiftJournalPlayer>() : null;
                if (journal)
                {
                    var definition = enemy.Definition;
                    int experience = RiftEnemyLevelTuning.Load.Experience(definition.Experience, enemy.Level);
                    journal.ServerRecord(RiftProgressEvent.Kill, definition.Id, 1, experience, enemy.QuestEncounter);
                    if (definition.IsBoss)
                        journal.ServerRecord(RiftProgressEvent.MiniBoss, definition.Id, 1, 0, enemy.QuestEncounter);
                }
                Debug.Log($"CIUDAD_POBLACION MUERTE {post.Definition.Id}; " +
                    (post.Definition.AutoRespawn ? $"respawn elegible en {post.Definition.RespawnSeconds:F0}s, sujeto a seguridad y presupuesto." : "sin respawn automático."), this);
            }
            catch (Exception error) { faulted = true; Debug.LogException(error, this); }
        }

        void Update()
        {
            if (!initialized || shuttingDown || faulted || !NetworkServer.active || NetworkTime.time < nextTick) return;
            nextTick = NetworkTime.time + configuration.TickSeconds;
            if (!(GameManager.Gamemode is Raid) || gameObject.scene.path != CiudadDelSolPopulationDefinition.ScenePath)
            { faulted = true; Debug.LogError("CIUDAD_POBLACION: contexto cambiado; se suspendieron las apariciones.", this); return; }
            if (ValidateNextEncounter()) return;
            foreach (var post in posts)
            {
                if (post.Occupant) continue; // Includes the native corpse: never stack a replacement on it.
                if (post.Schedule.Phase == CiudadDelSolRespawnSchedule.State.Occupied)
                {
                    post.Schedule.Lost(NetworkTime.time, post.Definition.RespawnSeconds, post.Definition.AutoRespawn);
                    Debug.LogWarning("CIUDAD_POBLACION: actor retirado sin muerte; espera normal, no reposición inmediata: " + post.Definition.Id, this);
                }
                foreach (var deadKey in owners.Where(pair => pair.Value == post).Select(pair => pair.Key).ToArray()) owners.Remove(deadKey);
                if (!post.Approved) continue;
                if (!post.Schedule.Due(NetworkTime.time) || NetworkTime.time < post.RetryAt) continue;
                if (configuration.ValidateExteriorEncounters && post.Schedule.Phase == CiudadDelSolRespawnSchedule.State.Initial)
                { if (SpawnInitialEncounter(post)) break; continue; }
                post.RetryAt = NetworkTime.time + configuration.RetrySeconds;
                if (EncounterFighting(post.Definition.EncounterId))
                { Deferred(post, "el encuentro sigue en combate, aunque se haya desplazado a otro sector"); continue; }
                if (!CiudadDelSolSpawnSafety.HasHumanNear(post.Reference, post.Definition.ActivationRadius)) continue;
                if (!CiudadDelSolSpawnSafety.PlayersClear(post.Reference, configuration))
                { Deferred(post, "jugador próximo, en combate o descanso"); continue; }
                if (OccupiedCount >= configuration.GlobalMaximum ||
                    posts.Count(p => p.Occupant && p.Definition.SectorId == post.Definition.SectorId) >=
                        configuration.Sectors.Single(s => s.Id == post.Definition.SectorId).Maximum)
                { Deferred(post, "presupuesto ocupado, incluidos cadáveres"); continue; }
                if (CiudadDelSolSpawnSafety.Recovery(post.Reference, configuration))
                { Deferred(post, "recuperación"); continue; }
                if (!CiudadDelSolSpawnSafety.Placement(gameObject.scene, post.Definition, post.Reference, out var spawn, out var reason))
                { Deferred(post, reason); continue; }
                Spawn(post, spawn);
                break; // At most one network spawn per tick, no burst allocation.
            }
        }

        void Deferred(Post post, string reason)
        {
            if (post.LastReason == reason) return;
            post.LastReason = reason;
            Debug.Log("CIUDAD_POBLACION APLAZADO " + post.Definition.Id + ": " + reason, this);
        }

        void Spawn(Post post, Vector3 position)
        {
            RiftEnemyAgent enemy = null;
            try
            {
                enemy = Instantiate(post.Definition.Enemy.Prefab, position, Quaternion.Euler(0, post.Definition.Yaw, 0));
                SceneManagementMove(enemy.gameObject);
                enemy.name = "Ciudad del Sol · " + post.Definition.Id;
                enemy.ConfigureSpawn(post.Definition.Level, encounter: post.Definition.EncounterId, cityRaidPursuit: true);
                NetworkServer.Spawn(enemy.gameObject);
                Require(enemy.isServer && enemy.netId != 0 && enemy.CityRaidPursuitActive && enemy.GetComponent<NavMeshAgent>().isOnNavMesh,
                    "Mirror o NavMesh rechazaron el respawn.");
                enemy.Health.RegisterNativeSpawn();
                Scale(post, enemy);
                Attach(post, enemy);
                Debug.Log($"CIUDAD_POBLACION APARICION {post.Definition.Id}, netId={enemy.netId}, " +
                    $"posición={position:F3}, vida={enemy.Health.MaxHealth}, ocupados={OccupiedCount}/{configuration.GlobalMaximum}.", this);
            }
            catch (Exception error)
            {
                faulted = true; // Fail closed: no endless instantiate/destroy loop.
                if (enemy) { if (enemy.netId != 0) enemy.Despawn(); else Destroy(enemy.gameObject); }
                Debug.LogError("CIUDAD_POBLACION ERROR: apariciones suspendidas, sin reintentos: " + error.Message, this);
            }
        }

        void Scale(Post post, RiftEnemyAgent enemy)
        {
            int participants = GameManager.Players.Count(p => p && !p.BOT && p.MyCharacter &&
                p.MyCharacter.gameObject.scene == gameObject.scene && p.MyCharacter.Health.CurrentHealth > 0 &&
                Vector3.Distance(p.MyCharacter.transform.position, post.Reference) <= post.Definition.ActivationRadius);
            var rules = configuration.ScaleForHumanPlayers ? RiftClassRules.Load() : null;
            int health = Mathf.Clamp(Mathf.RoundToInt(enemy.LevelHealth * post.Definition.HealthMultiplier *
                (rules ? rules.HealthScale(Mathf.Clamp(participants, 1, 4)) : 1)), 1, 100000);
            enemy.Health.ServerSetVitals(health, health);
            enemy.Health.CombatDamageMultiplier = post.Definition.DamageMultiplier * (rules ? rules.DamageScale(Mathf.Clamp(participants, 1, 4)) : 1);
        }

        void SceneManagementMove(GameObject instance) => UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, gameObject.scene);

        public void Shutdown()
        {
            if (shuttingDown) return;
            shuttingDown = true;
            foreach (var enemy in owners.Keys.ToArray()) if (enemy)
            {
                enemy.ServerKilled -= Killed;
                enemy.ServerTargetGate = null; enemy.ServerAttackTargetGate = null;
                enemy.StopAllCoroutines();
                if (NetworkServer.active && enemy.netId != 0) enemy.Despawn(); else Destroy(enemy.gameObject);
            }
            owners.Clear(); posts.Clear(); initialized = false;
            if (configuration) Destroy(configuration);
        }
        void OnDestroy() => Shutdown();
    }
}
