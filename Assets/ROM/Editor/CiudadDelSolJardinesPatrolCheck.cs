using System;
using System.Linq;
using Mirror;
using MultiFPS.Gameplay.RiftEnemies;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    internal static partial class CiudadDelSolJardinesPlayCheck
    {
        static readonly Bounds PatrolCourt = new Bounds(new Vector3(-119.5f, 4.8f, -49.5f), new Vector3(20, 2.5f, 16));
        static readonly int[] PatrolOrder = { 0, 1, 0, 2, 0, 3, 1, 2, 1, 3, 2, 3, 0 };
        static PatrolCheck patrol;

        [MenuItem(Menu + "3. Probar movimiento del Diablillo")]
        static void StartPatrol()
        {
            if (!CanStartPatrol()) return;
            try
            {
                patrol = new PatrolCheck(SceneManager.GetSceneByPath(City));
                patrol.Start();
            }
            catch (Exception error)
            {
                if (patrol != null) patrol.Stop("ERROR: " + error.Message, true);
                else Report("ERROR: " + error.Message, true);
            }
        }

        [MenuItem(Menu + "3. Probar movimiento del Diablillo", true)]
        static bool CanStartPatrol() => patrol == null && pending == null && EditorApplication.isPlaying &&
            !EditorApplication.isPaused && SceneManager.GetActiveScene().path == City && !NetworkBusy();

        sealed class PatrolCheck
        {
            const float GroundClearance = .06f;
            const float ArrivalAllowance = .06f;
            const float MinimumMotion = .05f;
            readonly Scene scene;
            readonly Vector3[] points;
            readonly NavMeshAgent source;
            readonly RiftEnemyDefinition definition;
            readonly Collider[] overlap = new Collider[1];
            GameObject probe;
            NavMeshAgent agent;
            int leg = 1;
            double started, progressAt, nextSample;
            Vector3 previous, progressPosition;
            float edgeMinimum, courtMinimum, distance, requiredMotion;

            public PatrolCheck(Scene scene)
            {
                this.scene = scene;
                var root = scene.GetRootGameObjects().Single(g => g.name == Encounter && g.activeInHierarchy);
                var surface = root.GetComponent<NavMeshSurface>();
                var data = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavAsset);
                Check(surface && surface.isActiveAndEnabled && data && surface.navMeshData == data && surface.agentTypeID == 0,
                    "La superficie aprobada no está disponible.");
                definition = AssetDatabase.LoadAssetAtPath<RiftEnemyDefinition>("Assets/ROM/Enemies/LittleDemon/LittleDemon.asset");
                Check(definition && definition.Prefab, "Falta la definición del Diablillo o su prefab.");
                source = definition.Prefab.GetComponentInChildren<NavMeshAgent>(true);
                Check(source && source.agentTypeID == surface.agentTypeID, "El agente del Diablillo no es compatible.");
                var settings = NavMesh.GetSettingsByID(source.agentTypeID);
                Check(settings.agentRadius >= source.radius && settings.agentHeight >= source.height,
                    "El agente excede las dimensiones del horneado.");
                var filter = new NavMeshQueryFilter { agentTypeID = source.agentTypeID, areaMask = source.areaMask };
                points = Markers.Select(name =>
                {
                    var marker = root.transform.Find(name);
                    Check(marker && marker.gameObject.activeInHierarchy, "Falta el marcador " + name + ".");
                    Check(NavMesh.SamplePosition(marker.position, out var hit, .05f, filter) && PatrolCourt.Contains(hit.position),
                        "Marcador fuera del NavMesh aprobado: " + name + ".");
                    return hit.position;
                }).ToArray();
            }

            public void Start()
            {
                probe = new GameObject("Sondeo temporal · Diablillo");
                probe.hideFlags = HideFlags.DontSave;
                SceneManager.MoveGameObjectToScene(probe, scene);
                probe.SetActive(false);
                probe.transform.position = points[0];
                agent = probe.AddComponent<NavMeshAgent>();
                agent.agentTypeID = source.agentTypeID;
                agent.areaMask = source.areaMask;
                agent.radius = source.radius;
                agent.height = source.height;
                agent.baseOffset = source.baseOffset;
                agent.speed = definition.MoveSpeed;
                agent.acceleration = source.acceleration;
                agent.angularSpeed = source.angularSpeed;
                agent.autoBraking = source.autoBraking;
                agent.autoRepath = false;
                agent.autoTraverseOffMeshLink = false;
                agent.obstacleAvoidanceType = source.obstacleAvoidanceType;
                agent.stoppingDistance = .35f;
                probe.SetActive(true);
                Check(agent.Warp(points[0]) && agent.isOnNavMesh, "No se pudo ubicar el agente temporal.");
                Report($"Prueba dinámica iniciada. Tipo {agent.agentTypeID}, radio {agent.radius:F2} m, altura {agent.height:F2} m, " +
                    $"velocidad {agent.speed:F2} m/s, aceleración {agent.acceleration:F2} m/s², parada {agent.stoppingDistance:F2} m.");
                Depart();
                EditorApplication.update += Tick;
                EditorApplication.playModeStateChanged += PlayModeChanged;
                AssemblyReloadEvents.beforeAssemblyReload += Cancel;
            }

            void Depart()
            {
                var target = points[PatrolOrder[leg]];
                var path = new NavMeshPath();
                Check(agent.CalculatePath(target, path) && path.status == NavMeshPathStatus.PathComplete &&
                    path.corners.All(PatrolCourt.Contains) && agent.SetDestination(target), "Ruta inválida hacia " + Markers[PatrolOrder[leg]]);
                previous = progressPosition = agent.transform.position;
                started = progressAt = nextSample = Time.timeAsDouble;
                edgeMinimum = courtMinimum = float.PositiveInfinity;
                distance = 0;
                // A short leg only needs to cover the distance to the arrival envelope.
                requiredMotion = Mathf.Max(MinimumMotion,
                    Vector3.Distance(previous, target) - (agent.stoppingDistance + ArrivalAllowance) - .01f);
                Report($"INICIO {leg}/12 {Markers[PatrolOrder[leg - 1]]} → {Markers[PatrolOrder[leg]]}: {previous:F3} → {target:F3}");
            }

            void Tick()
            {
                try
                {
                    Check(EditorApplication.isPlaying && scene.isLoaded && SceneManager.GetActiveScene() == scene &&
                        !NetworkBusy(), "Cambió la escena o el estado de Mirror.");
                    if (EditorApplication.isPaused) return;
                    double now = Time.timeAsDouble;
                    var position = agent.transform.position;
                    Check(agent.isOnNavMesh && !agent.isOnOffMeshLink && PatrolCourt.Contains(position), "El agente salió del patio o del NavMesh.");
                    Check(agent.FindClosestEdge(out var edge), "No se pudo medir el borde del NavMesh.");
                    edgeMinimum = Mathf.Min(edgeMinimum, edge.distance);
                    courtMinimum = Mathf.Min(courtMinimum, Mathf.Min(position.x - PatrolCourt.min.x, PatrolCourt.max.x - position.x,
                        position.z - PatrolCourt.min.z, PatrolCourt.max.z - position.z));
                    Check(Mathf.Min(edgeMinimum, courtMinimum) >= agent.radius, $"Margen menor que el radio: {edgeMinimum:F3}/{courtMinimum:F3} m.");

                    var delta = position - previous;
                    CheckClearance(previous, position, delta);
                    distance += delta.magnitude;
                    previous = position;
                    if ((position - progressPosition).sqrMagnitude > .0025f)
                    {
                        progressPosition = position;
                        progressAt = now;
                    }
                    if (now >= nextSample)
                    {
                        Report($"MUESTRA {leg}/12 t={now - started:F2} s pos={position:F3} borde={edge.distance:F3} m.");
                        nextSample = now + .5;
                    }

                    Check(now - started <= 18 && now - progressAt <= 2.5, "Tiempo agotado o agente sin progreso.");
                    if (agent.pathPending) return;
                    Check(agent.pathStatus == NavMeshPathStatus.PathComplete, "Estado de ruta: " + agent.pathStatus);
                    float error = Vector3.Distance(position, points[PatrolOrder[leg]]);
                    if (error > agent.stoppingDistance + ArrivalAllowance || agent.velocity.magnitude > .18f) return;
                    Check(distance >= requiredMotion,
                        $"No se registró desplazamiento real: {distance:F3} m recorridos, {requiredMotion:F3} m requeridos.");
                    Report($"OK {leg}/12 pos={position:F3} t={now - started:F2} s recorrido={distance:F2} m error={error:F3} m " +
                        $"borde mín.={edgeMinimum:F3} m límite patio mín.={courtMinimum:F3} m.");
                    if (++leg == PatrolOrder.Length) Stop("OK: 12/12 trayectos dirigidos completados.");
                    else Depart();
                }
                catch (Exception error)
                {
                    Stop($"ERROR tramo {leg}/12 en {(agent ? agent.transform.position : previous):F3}: {error.Message}", true);
                }
            }

            void CheckClearance(Vector3 from, Vector3 to, Vector3 delta)
            {
                // The colliderless probe tests its body volume above the walking surface.
                Vector3 lower = Vector3.up * (agent.radius + GroundClearance);
                Vector3 upper = Vector3.up * (agent.height - agent.radius);
                var physics = scene.GetPhysicsScene();
                if (delta.sqrMagnitude > .000001f &&
                    physics.CapsuleCast(from + lower, from + upper, agent.radius, delta.normalized, out var hit,
                        delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException($"Cruce de collider sólido: {hit.collider.name} en {hit.point:F3}.");
                Check(physics.OverlapCapsule(to + lower, to + upper, agent.radius, overlap, ~0, QueryTriggerInteraction.Ignore) == 0,
                    "La cápsula de comprobación se superpone a un collider sólido.");
            }

            void PlayModeChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Cancel();
            }

            void Cancel() => Stop("Prueba interrumpida al salir de Play Mode o recargar scripts.");

            public void Stop(string result, bool error = false)
            {
                EditorApplication.update -= Tick;
                EditorApplication.playModeStateChanged -= PlayModeChanged;
                AssemblyReloadEvents.beforeAssemblyReload -= Cancel;
                if (probe)
                {
                    probe.SetActive(false);
                    Object.DestroyImmediate(probe);
                }
                patrol = null;
                Report(result, error);
            }
        }
    }
}

