using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    internal static class CiudadDelSolJardinesNavigationPilot
    {
        const string Menu = "ROM/Dungeon Ciudad del Sol/NavMesh exterior/Piloto Jardines - ejecutar en Play Mode";
        const string City = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Garden = "02 - Jardines Dorados";
        const string Encounter = "Raid · Jardines Dorados · Encuentro 01";
        const float Radius = .57f, Climb = .36f, Voxel = .10f;
        static readonly string[] Markers = { "01 Frontal", "02 Distancia", "03 Patrulla A", "04 Patrulla B" };
        static readonly NavMeshQueryFilter Filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = 1 };
        static Trial current;

        [MenuItem(Menu, true)]
        static bool CanRun() => EditorApplication.isPlaying && !EditorApplication.isPaused && current == null;

        [MenuItem(Menu)]
        static void Run()
        {
            if (!CanRun()) return;
            try
            {
                Require(!SceneManager.GetSceneByPath(City).isLoaded &&
                    NavMesh.CalculateTriangulation().indices.Length == 0 &&
                    Object.FindObjectsByType<NavMeshModifier>(FindObjectsSortMode.None).Length == 0 &&
                    Object.FindObjectsByType<NavMeshModifierVolume>(FindObjectsSortMode.None).Length == 0,
                    "Usá una sesión de Play sin Ciudad del Sol ni otras fuentes o superficies de navegación.");
                current = new Trial();
                current.Prepare();
                EditorApplication.update += Tick;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }
            catch (Exception error) { Finish(error.Message); }
        }

        static void Tick()
        {
            if (EditorApplication.isPaused) return;
            try { if (current.Advance()) Finish(null); }
            catch (Exception error) { Finish(error.Message); }
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Finish("Play Mode interrumpido.");
        }

        static void Finish(string error)
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            var trial = current;
            current = null;
            trial?.Dispose();
            if (error != null) Debug.LogError("Piloto Jardines. " + error);
            else Debug.Log("Piloto Jardines. Prueba completa; agente y NavMesh temporal retirados.");
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        sealed class Trial : IDisposable
        {
            readonly Dictionary<Transform, Transform> copies = new Dictionary<Transform, Transform>();
            readonly List<Bounds> exclusions = new List<Bounds>();
            readonly Collider[] contacts = new Collider[1];
            Scene pilot;
            Transform root;
            NavMeshSurface surface;
            NavMeshAgent agent;
            Vector3[] points;
            string[] names;
            int target = 1;
            Vector3 previous;
            float travelled, started;

            internal void Prepare()
            {
                var settings = NavMesh.GetSettingsByID(0);
                Require(settings.agentTypeID == 0 && Mathf.Abs(settings.agentRadius - Radius) < .001f &&
                    Mathf.Abs(settings.agentClimb - Climb) < .001f && settings.agentHeight > 2 * Radius + Climb,
                    "La configuración instalada de Humanoid 0 difiere del piloto.");
                // The standard runtime collector has no local PhysicsScene argument.
                // Keep the scene disposable, but expose its colliders to the default physics world.
                pilot = SceneManager.CreateScene("Piloto temporal · Jardines Dorados");
                root = CreateObject("Fuentes físicas · Jardines Dorados").transform;
                var preview = EditorSceneManager.OpenPreviewScene(City);
                Require(preview.IsValid(), "No se pudo abrir Ciudad del Sol como escena de previsualización.");
                try
                {
                    var roots = preview.GetRootGameObjects();
                    var garden = roots.Single(item => item.name == "Dungeon_Ciudad_del_Sol_Generated")
                        .GetComponentsInChildren<Transform>().Single(item => item.name == Garden);
                    var encounter = roots.Single(item => item.name == Encounter);
                    var markerTransforms = encounter.GetComponentsInChildren<Transform>(true);
                    var markers = Markers.Select(name => markerTransforms.Single(item => item.name == name).position).ToArray();
                    names = new[] { "Paseo oeste sur", "Paseo oeste norte" }.Concat(Markers).ToArray();
                    points = new[] { new Vector3(-149, 4, -65), new Vector3(-149, 4, 25) }.Concat(markers).ToArray();
                    CopySources(garden);
                    exclusions.Add(CiudadDelSolExteriorSourceRoles.GardenPondInterior(garden));
                }
                finally { EditorSceneManager.ClosePreviewScene(preview); }

                Physics.SyncTransforms();
                foreach (var bounds in exclusions) AddExclusion(bounds);
                Require(NavMesh.CalculateTriangulation().indices.Length == 0,
                    "Hay un NavMesh ajeno registrado; no se puede aislar el piloto.");
                surface = root.gameObject.AddComponent<NavMeshSurface>();
                surface.agentTypeID = 0;
                surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.defaultArea = 1;
                surface.overrideVoxelSize = true;
                surface.voxelSize = Voxel;
                surface.buildHeightMesh = true;
                CheckCollectedSources();
                surface.BuildNavMesh();
                Require(surface.navMeshData, "El horneador estándar no devolvió NavMeshData.");
                Require(NavMesh.CalculateTriangulation().indices.Length > 0,
                    "NavMeshData existe, pero su registro no expuso triángulos.");
                foreach (var bounds in exclusions)
                    Require(!NavMesh.SamplePosition(bounds.center, out _, .15f, Filter),
                        $"El centro ornamental {bounds.center:F3} todavía tiene NavMesh.");
                for (int i = 0; i < points.Length; i++)
                {
                    Require(NavMesh.SamplePosition(points[i], out var hit, .65f, Filter) &&
                        Mathf.Abs(points[i].y - hit.position.y) <= Climb,
                        $"{names[i]} sin proyección a la cota esperada, {points[i]:F3}.");
                    points[i] = hit.position;
                }
                for (int i = 1; i < points.Length; i++)
                {
                    ValidatePath(i - 1, i);
                    ValidatePath(i, i - 1);
                }
                agent = CreateObject("Agente temporal · Humanoid 0").AddComponent<NavMeshAgent>();
                agent.agentTypeID = 0;
                agent.areaMask = 1;
                agent.autoTraverseOffMeshLink = false;
                agent.radius = Radius;
                agent.height = settings.agentHeight;
                agent.speed = 2.4f;
                agent.acceleration = 14f;
                agent.stoppingDistance = .15f;
                Require(agent.Warp(points[0]) && agent.isOnNavMesh, "El agente no pudo ocupar el inicio.");
                BeginLeg();
            }

            void CheckCollectedSources()
            {
                var markups = root.GetComponentsInChildren<NavMeshModifier>().Select(modifier => new NavMeshBuildMarkup
                {
                    root = modifier.transform,
                    overrideArea = modifier.overrideArea,
                    area = modifier.area,
                    applyToChildren = modifier.applyToChildren,
                    overrideGenerateLinks = true,
                    generateLinks = false
                }).ToList();
                var collected = new List<NavMeshBuildSource>();
                NavMeshBuilder.CollectSources(root, surface.layerMask, surface.useGeometry,
                    surface.defaultArea, false, markups, false, collected);
                int floors = collected.Count(source => source.area == 0 && source.component is Collider);
                int blockers = collected.Count(source => source.area == 1 && source.component is Collider);
                Debug.Log($"Piloto Jardines. Recolección física de runtime: {floors} pisos, {blockers} obstáculos.");
                Require(floors > 0 && blockers > 0 && collected.All(source =>
                    source.component is Collider collider && collider.gameObject.scene == pilot &&
                    collider.enabled && collider.gameObject.activeInHierarchy),
                    "La recolección física no recibió los pisos y obstáculos activos de la escena temporal.");
            }

            void CopySources(Transform garden)
            {
                var byCourt = new Dictionary<Transform, List<BoxCollider>>();
                int floors = 0, blockers = 0;
                foreach (var original in garden.GetComponentsInChildren<Collider>())
                {
                    if (!original.enabled || original.isTrigger) continue;
                    var node = CopyTransform(original.transform);
                    var collider = (Collider)node.gameObject.AddComponent(original.GetType());
                    EditorUtility.CopySerialized(original, collider);
                    bool walkable = IsGardenFloor(original.transform, garden);
                    var modifier = node.GetComponent<NavMeshModifier>();
                    if (!modifier) modifier = node.gameObject.AddComponent<NavMeshModifier>();
                    modifier.applyToChildren = false;
                    modifier.overrideArea = true;
                    modifier.area = walkable ? 0 : 1;
                    modifier.overrideGenerateLinks = true;
                    modifier.generateLinks = false;
                    if (walkable) floors++;
                    else blockers++;
                    if (collider is BoxCollider curb && HasAncestor(original.transform, garden, "Curb_01"))
                    {
                        for (var parent = original.transform.parent; parent && parent != garden; parent = parent.parent)
                        {
                            if (!parent.parent || parent.parent.name != "03 - Patios vegetales") continue;
                            if (!byCourt.TryGetValue(parent, out var pieces)) byCourt.Add(parent, pieces = new List<BoxCollider>());
                            pieces.Add(curb);
                            break;
                        }
                    }
                }
                Physics.SyncTransforms();
                Require(floors > 0 && blockers > 0 && byCourt.Count == 4,
                    $"Inventario inesperado: {floors} pisos, {blockers} obstáculos, {byCourt.Count} patios enmarcados.");
                foreach (var court in byCourt)
                {
                    var curbs = court.Value.Select(box => box.bounds).ToArray();
                    var sidesX = curbs.Where(bounds => bounds.size.z > bounds.size.x).OrderBy(bounds => bounds.center.x).ToArray();
                    var sidesZ = curbs.Where(bounds => bounds.size.x > bounds.size.z).OrderBy(bounds => bounds.center.z).ToArray();
                    Require(sidesX.Length == 4 && sidesZ.Length == 4, "Marco inesperado: " + court.Key.name);
                    var bounds = new Bounds();
                    bounds.SetMinMax(new Vector3(sidesX.Take(2).Max(b => b.max.x), 3.8f, sidesZ.Take(2).Max(b => b.max.z)),
                        new Vector3(sidesX.Skip(2).Min(b => b.min.x), 4.2f, sidesZ.Skip(2).Min(b => b.min.z)));
                    Require(bounds.size.x > 10f && bounds.size.x < 12f && bounds.size.z > 10f && bounds.size.z < 12f,
                        "Interior físico inesperado: " + court.Key.name);
                    exclusions.Add(bounds);
                }
                Debug.Log($"Piloto Jardines. {floors} colliders de suelo, {blockers} obstáculos y {byCourt.Count} patios ornamentales.");
            }

            static bool IsGardenFloor(Transform node, Transform garden)
            {
                bool platform = HasAncestor(node, garden, "Platform_Base_01");
                bool stair = HasAncestor(node, garden, "Staircase_01");
                return (platform && HasAncestor(node, garden, "Suelo modular - superficie Y=4")) ||
                    ((platform || stair) && HasAncestor(node, garden, "Galeria lateral elevada +4"));
            }

            static bool HasAncestor(Transform node, Transform stop, string prefix)
            {
                for (; node && node != stop; node = node.parent)
                    if (node.name.StartsWith(prefix, StringComparison.Ordinal)) return true;
                return false;
            }

            Transform CopyTransform(Transform original)
            {
                if (copies.TryGetValue(original, out var copy)) return copy;
                copy = CreateObject(original.name).transform;
                copy.SetParent(original.parent ? CopyTransform(original.parent) : root, false);
                copy.localPosition = original.localPosition;
                copy.localRotation = original.localRotation;
                copy.localScale = original.localScale;
                copies.Add(original, copy);
                return copy;
            }

            GameObject CreateObject(string name)
            {
                var item = new GameObject(name);
                SceneManager.MoveGameObjectToScene(item, pilot);
                return item;
            }

            void AddExclusion(Bounds bounds)
            {
                var item = CreateObject("Área ornamental · sin navegación");
                item.transform.SetParent(root, false);
                item.transform.position = bounds.center;
                var volume = item.AddComponent<NavMeshModifierVolume>();
                volume.area = 1;
                volume.size = bounds.size;
            }

            void ValidatePath(int from, int to)
            {
                var path = new NavMeshPath();
                Require(NavMesh.CalculatePath(points[from], points[to], Filter, path) &&
                    path.status == NavMeshPathStatus.PathComplete,
                    $"Ruta incompleta {names[from]} → {names[to]} ({path.status}).");
                if (from < 2 && to < 2)
                    Require(path.corners.All(corner => corner.x <= -138f && Mathf.Abs(corner.y - 4f) <= Climb),
                        "La ruta entre los extremos del paseo se desvía del corredor occidental a Y=4.");
            }

            void BeginLeg()
            {
                travelled = 0;
                previous = agent.transform.position;
                started = Time.unscaledTime;
                Require(agent.SetDestination(points[target]), "Destino rechazado: " + names[target]);
            }

            internal bool Advance()
            {
                Require(agent && agent.isOnNavMesh && !agent.isOnOffMeshLink, "El agente abandonó la malla.");
                Require(Time.unscaledTime - started < 90f, "Tiempo agotado en " + names[target]);
                if (agent.pathPending) return false;
                Require(agent.pathStatus == NavMeshPathStatus.PathComplete, "Ruta dinámica incompleta: " + names[target]);
                var position = agent.transform.position;
                if (target == 1)
                    Require(position.x <= -138f && Mathf.Abs(position.y - 4f) <= Climb,
                        "El agente salió del corredor occidental a Y=4.");
                Require(!exclusions.Any(bounds => bounds.Contains(position)), "El agente entró en un recinto ornamental.");
                var lower = Vector3.up * (Climb + Radius + .02f);
                var upper = Vector3.up * (agent.height - Radius + .02f);
                var motion = position - previous;
                var physics = pilot.GetPhysicsScene();
                Require(physics.OverlapCapsule(position + lower, position + upper, Radius, contacts,
                    ~0, QueryTriggerInteraction.Ignore) == 0, $"La cápsula invadió un collider en {position:F3}.");
                if (motion.sqrMagnitude > .000001f)
                    Require(!physics.CapsuleCast(previous + lower, previous + upper, Radius, motion.normalized,
                        out var hit, motion.magnitude, ~0, QueryTriggerInteraction.Ignore),
                        $"El desplazamiento cruzó {hit.collider?.name} cerca de {position:F3}.");
                travelled += motion.magnitude;
                previous = position;
                if (agent.remainingDistance > agent.stoppingDistance + .10f ||
                    Vector3.Distance(position, points[target]) > agent.stoppingDistance + .15f) return false;
                Require(travelled >= Mathf.Max(.02f, Vector3.Distance(points[target - 1], points[target]) - .5f),
                    "No se registró desplazamiento suficiente hasta " + names[target]);
                Debug.Log($"Piloto Jardines. {names[target - 1]} → {names[target]}: " +
                    $"{travelled:F2} m en {Time.unscaledTime - started:F2} s; final {position:F3}.");
                if (++target == points.Length) return true;
                BeginLeg();
                return false;
            }

            public void Dispose()
            {
                if (agent) agent.enabled = false;
                if (surface)
                {
                    surface.RemoveData();
                    var data = surface.navMeshData;
                    surface.navMeshData = null;
                    if (data) Object.Destroy(data);
                }
                if (pilot.IsValid() && pilot.isLoaded) SceneManager.UnloadSceneAsync(pilot);
            }
        }
    }
}
