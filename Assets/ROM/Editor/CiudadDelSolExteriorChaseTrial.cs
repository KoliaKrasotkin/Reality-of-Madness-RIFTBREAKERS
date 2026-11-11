using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
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
        const string ChaseMenu = "ROM/Dungeon Ciudad del Sol/Jardines Dorados/Persecucion exterior/";
        static ExteriorChase exteriorChase;

        [MenuItem(ChaseMenu + "1. Hornear en memoria e iniciar seguimiento")]
        static void StartExteriorChase()
        {
            if (exteriorChase != null)
            {
                Debug.LogWarning("Persecución exterior. Ya hay un ensayo activo.");
                return;
            }

            try
            {
                var city = SceneManager.GetActiveScene();
                var warrior = ClientFrontend.OwnedCharacterInstance;
                Check(EditorApplication.isPlaying && !EditorApplication.isPaused && city.path == City &&
                    NetworkServer.active && NetworkClient.active && NetworkServer.localConnection != null && !NetworkServer.listen &&
                    warrior && warrior.isServer && warrior.isOwned && warrior.connectionToClient == NetworkServer.localConnection &&
                    warrior.GetComponent<WarriorAvatar>() && warrior.Health && warrior.Health.CurrentHealth > 0 &&
                    PatrolCourt.Contains(warrior.transform.position), "Entrá al patio con el Warrior vivo en el Host local de ensayo.");
                Check(!assistTrial, "Cancelá primero el ensayo de placas.");
                exteriorChase = new ExteriorChase(city, warrior);
                exteriorChase.Build();
                EditorApplication.update += TickExteriorChase;
                EditorApplication.playModeStateChanged += ChasePlayChanged;
                AssemblyReloadEvents.beforeAssemblyReload += CancelExteriorChase;
            }
            catch (Exception error) { EndExteriorChase(error.Message, true); }
        }

        [MenuItem(ChaseMenu + "2. Volver al patio por la traza de ida")]
        static void ReturnExteriorChase()
        {
            if (exteriorChase == null || !exteriorChase.BeginReturn())
                Debug.LogWarning("Persecución exterior. Primero esperá el horneado y recorré un tramo de ida.");
        }

        [MenuItem(ChaseMenu + "3. Cancelar y retirar ensayo")]
        static void CancelExteriorChase() => EndExteriorChase("Ensayo cancelado.");

        static void ChasePlayChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) CancelExteriorChase();
        }

        static void TickExteriorChase()
        {
            if (EditorApplication.isPaused) return;
            try
            {
                if (exteriorChase.Tick()) EndExteriorChase("Regreso al patio completado.");
            }
            catch (Exception error) { EndExteriorChase(error.Message, true); }
        }

        static void EndExteriorChase(string result, bool error = false)
        {
            EditorApplication.update -= TickExteriorChase;
            EditorApplication.playModeStateChanged -= ChasePlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= CancelExteriorChase;
            var trial = exteriorChase;
            exteriorChase = null;
            try { trial?.Dispose(); }
            finally
            {
                var message = "Persecución exterior. " + result + (trial == null ? "" : " " + trial.Summary());
                if (error) Debug.LogError(message);
                else Debug.Log(message);
            }
        }

        sealed class ExteriorChase
        {
            // Encloses the measured approach at X=-119.5, the patio, and the turn toward the Pórtico.
            static readonly Bounds BakeVolume = new Bounds(new Vector3(-119.5f, 3.5f, -74.75f),
                new Vector3(24, 8, 69.5f));
            static readonly Vector3 Portico = new Vector3(-126, .025f, -105);
            static readonly NavMeshQueryFilter Filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
            const float Arrival = .40f;
            readonly Scene scene;
            readonly CharacterInstance warrior;
            readonly Vector3 marker;
            readonly List<Vector3> outbound = new List<Vector3>();
            readonly NavMeshPath path = new NavMeshPath();
            NavMeshData data;
            NavMeshDataInstance registration;
            AsyncOperation build;
            NavMeshAgent agent;
            GameObject probe;
            Vector3 home, previous, progressPosition;
            float maxDistance, hiddenTravel;
            double nextPlan, stalledSince = -1;
            bool returning, blocked;
            int returnIndex;

            internal ExteriorChase(Scene scene, CharacterInstance warrior)
            {
                this.scene = scene;
                this.warrior = warrior;
                var root = scene.GetRootGameObjects().Single(item => item.name == Encounter && item.activeInHierarchy);
                var point = root.transform.Find(Markers[0]);
                Check(point && PatrolCourt.Contains(point.position), "Falta el marcador Frontal en el patio.");
                marker = point.position;
            }

            internal void Build()
            {
                var sources = new List<NavMeshBuildSource>();
                NavMeshBuilder.CollectSources(BakeVolume, GameManager.environmentLayer,
                    NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
                sources.RemoveAll(source => !(source.component is Collider collider) || collider.gameObject.scene != scene ||
                    !collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy);
                Check(sources.Count > 0, "No se encontraron colliders sólidos en el corredor exterior.");
                var settings = NavMesh.GetSettingsByID(0);
                Check(settings.agentTypeID == 0 && settings.agentRadius >= .30f && settings.agentHeight >= 1.30f,
                    "Los ajustes de Humanoid 0 no admiten la cápsula de diagnóstico.");
                data = new NavMeshData(0) { name = "TEMP persecución exterior · solo memoria", hideFlags = HideFlags.DontSave };
                build = NavMeshBuilder.UpdateNavMeshDataAsync(data, settings, sources, BakeVolume);
                Check(build != null, "Unity no inició el horneado en memoria.");
                Debug.Log($"Persecución exterior. Horneando {sources.Count} colliders. Esperá la comprobación de ida y vuelta.");
            }

            void StartProbe()
            {
                registration = NavMesh.AddNavMeshData(data);
                Check(registration.valid && Project(marker, out home), "El NavMesh temporal no alcanza el marcador Frontal.");
                Check(Project(Portico, out var end), $"El NavMesh temporal no alcanza el Pórtico en {Portico:F3}.");
                Check(Route(home, end, out var issue), $"Patio → Pórtico: {issue}");
                Check(Route(end, home, out issue), $"Pórtico → patio: {issue}");

                probe = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                probe.name = "TEMP agente de persecución · sin combate";
                probe.hideFlags = HideFlags.DontSave;
                SceneManager.MoveGameObjectToScene(probe, scene);
                var collider = probe.GetComponent<Collider>();
                collider.enabled = false;
                Object.Destroy(collider);
                var mesh = probe.GetComponent<MeshFilter>();
                var renderer = probe.GetComponent<MeshRenderer>();
                var markerVisual = new GameObject("Indicador");
                markerVisual.transform.SetParent(probe.transform, false);
                markerVisual.transform.localPosition = Vector3.up * .65f;
                markerVisual.transform.localScale = new Vector3(.6f, .65f, .6f);
                markerVisual.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                markerVisual.AddComponent<MeshRenderer>().sharedMaterial = renderer.sharedMaterial;
                renderer.enabled = false;
                probe.SetActive(false);
                probe.transform.position = home;
                agent = probe.AddComponent<NavMeshAgent>();
                agent.agentTypeID = 0;
                agent.radius = .30f;
                agent.height = 1.30f;
                agent.speed = 2.40f;
                agent.acceleration = 14f;
                agent.stoppingDistance = Arrival;
                agent.autoRepath = false;
                agent.autoTraverseOffMeshLink = false;
                probe.SetActive(true);
                Check(agent.Warp(home) && agent.isOnNavMesh, "No se pudo posicionar al agente en el patio.");
                previous = home;
                progressPosition = home;
                outbound.Add(home);
                Debug.Log($"Persecución exterior. Caminos completos patio ↔ Pórtico {end:F3}. Caminá hacia el Pórtico y dejá que el agente te alcance.");
            }

            internal bool Tick()
            {
                Check(EditorApplication.isPlaying && NetworkServer.active && NetworkServer.localConnection != null &&
                    SceneManager.GetActiveScene() == scene && warrior && warrior.Health && warrior.Health.CurrentHealth > 0,
                    "Terminó el Host o el Warrior ya no está disponible.");
                if (build != null)
                {
                    if (!build.isDone) return false;
                    build = null;
                    StartProbe();
                }
                var position = probe.transform.position;
                Check(agent.isOnNavMesh && !agent.isOnOffMeshLink && HasSupport(position),
                    $"El agente perdió soporte o salió del NavMesh en {position:F3}.");
                var travel = position - previous;
                var physics = scene.GetPhysicsScene();
                if (travel.sqrMagnitude > .000001f)
                {
                    Check(!physics.SphereCast(previous + Vector3.up * .75f, .27f, travel.normalized, out var obstacle,
                        travel.magnitude, GameManager.environmentLayer, QueryTriggerInteraction.Ignore),
                        $"Posible cruce de obstáculo: {obstacle.collider?.name} en {obstacle.point:F3}.");
                }

                double now = Time.timeAsDouble;
                if (returning)
                {
                    if ((position - outbound[returnIndex]).sqrMagnitude <= Arrival * Arrival + .04f)
                    {
                        if (returnIndex == 0) return true;
                        returnIndex--;
                        nextPlan = 0;
                    }
                }
                else
                {
                    if ((position - outbound[outbound.Count - 1]).sqrMagnitude >= 2.25f) outbound.Add(position);
                    float distance = Vector2.Distance(new Vector2(home.x, home.z), new Vector2(position.x, position.z));
                    if (maxDistance <= 36 && distance > 36)
                        Debug.Log($"Persecución exterior. Agente superó 36 m desde Frontal en {position:F3}.");
                    maxDistance = Mathf.Max(maxDistance, distance);
                    var sight = warrior.transform.position + Vector3.up - (position + Vector3.up);
                    if (!blocked && travel.sqrMagnitude > .000001f && physics.Raycast(position + Vector3.up, sight.normalized, out _,
                        sight.magnitude, GameManager.environmentLayer, QueryTriggerInteraction.Ignore))
                        hiddenTravel += travel.magnitude;
                }
                previous = position;
                if (now < nextPlan) return false;
                nextPlan = now + .4;
                var target = returning ? outbound[returnIndex] : warrior.transform.position;
                if (!Project(target, out var destination))
                    StopForRoute($"Destino fuera de la superficie o en otra cota: {target:F3}.");
                else if (!returning && (destination - position).sqrMagnitude <= Arrival * Arrival)
                {
                    agent.isStopped = true;
                    blocked = false;
                    stalledSince = -1;
                }
                else if (!Route(position, destination, out var reason))
                    StopForRoute(reason);
                else if (!agent.SetPath(path))
                    StopForRoute($"Unity rechazó el camino hacia {destination:F3}.");
                else
                {
                    if (blocked) Debug.Log("Persecución exterior. Ruta recuperada.");
                    blocked = false;
                    agent.isStopped = false;
                    bool needsMovement = (destination - position).sqrMagnitude > .65f * .65f;
                    if (!needsMovement || (position - progressPosition).sqrMagnitude >= .05f * .05f)
                    {
                        progressPosition = position;
                        stalledSince = -1;
                    }
                    else if (stalledSince < 0) stalledSince = now;
                    Check(stalledSince < 0 || now - stalledSince < 4,
                        $"El agente quedó atascado en {position:F3} hacia {destination:F3}.");
                }
                return false;
            }

            void StopForRoute(string reason)
            {
                agent.isStopped = true;
                stalledSince = -1;
                if (returning) throw new InvalidOperationException("Regreso bloqueado. " + reason);
                if (!blocked) Debug.LogWarning("Persecución exterior. Seguimiento en espera. " + reason);
                blocked = true;
            }

            bool Route(Vector3 from, Vector3 to, out string issue)
            {
                if (!NavMesh.CalculatePath(from, to, Filter, path) || path.status != NavMeshPathStatus.PathComplete)
                {
                    issue = $"Camino {path.status} desde {from:F3} hacia {to:F3}.";
                    return false;
                }
                var corners = path.corners;
                for (int i = 1; i < corners.Length; i++)
                {
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(corners[i - 1], corners[i]) / .40f));
                    for (int step = 0; step <= steps; step++)
                    {
                        var point = Vector3.Lerp(corners[i - 1], corners[i], (float)step / steps);
                        if (HasSupport(point)) continue;
                        issue = $"Sin suelo sólido o fuera del corredor en {point:F3}.";
                        return false;
                    }
                }
                issue = null;
                return true;
            }

            bool HasSupport(Vector3 point) => BakeVolume.Contains(point) &&
                scene.GetPhysicsScene().Raycast(point + Vector3.up * .45f, Vector3.down, out var floor, .95f,
                    GameManager.environmentLayer, QueryTriggerInteraction.Ignore) && floor.normal.y >= .70f;

            static bool Project(Vector3 point, out Vector3 projected)
            {
                projected = default;
                if (!BakeVolume.Contains(point) || !NavMesh.SamplePosition(point, out var hit, .80f, Filter) ||
                    !BakeVolume.Contains(hit.position) || Mathf.Abs(hit.position.y - point.y) > .45f) return false;
                projected = hit.position;
                return true;
            }

            internal bool BeginReturn()
            {
                if (!agent || returning || outbound.Count < 2) return false;
                returning = true;
                returnIndex = outbound.Count - 1;
                nextPlan = 0;
                stalledSince = -1;
                Debug.Log($"Persecución exterior. Regreso por {outbound.Count} puntos registrados de ida.");
                return true;
            }

            internal string Summary() => $"Distancia máxima desde Frontal: {maxDistance:F2} m horizontales. " +
                $"Avance con visión obstruida: {hiddenTravel:F2} m. Puntos de ida: {outbound.Count}.";

            internal void Dispose()
            {
                if (probe)
                {
                    probe.SetActive(false);
                    Object.Destroy(probe);
                }
                if (registration.valid) registration.Remove();
                if (build != null && !build.isDone) NavMeshBuilder.Cancel(data);
                if (data) Object.Destroy(data);
            }
        }
    }
}
