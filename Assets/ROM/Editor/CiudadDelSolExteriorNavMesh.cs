using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    internal static class CiudadDelSolExteriorNavMesh
    {
        const string Menu = "ROM/Dungeon Ciudad del Sol/NavMesh exterior/";
        const string City = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Asset = "Assets/ROM/CiudadDelSolJardinesRaid/CiudadDelSol_Exterior_NavMesh.asset";
        const string Patio = "Raid · Jardines Dorados · Encuentro 01";
        const string Exterior = "Raid · Ciudad del Sol · NavMesh exterior";
        static readonly string[] Sectors =
        {
            "01 - Entrada Portico Solar", "02 - Jardines Dorados", "03 - Plaza del Eclipse",
            "04 - Archivos Solares", "05 - Claustro del Eclipse", "06 - Santuario Profano", "07 - El Sol Corrompido"
        };
        static readonly string[] RouteIds = { "01-02", "01-06", "02-03", "03-04", "03-05", "03-06", "04-05", "05-07", "06-07" };
        static readonly string[] PatioMarkers = { "01 Frontal", "02 Distancia", "03 Patrulla A", "04 Patrulla B" };
        static readonly NavMeshQueryFilter Filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = 1 };
        static NavMeshData candidate;
        static string validatedHash;

        [MenuItem(Menu + "1. Hornear y validar candidata")]
        static void Bake()
        {
            Discard();
            try
            {
                var scene = CleanCity();
                CiudadDelSolNavMeshDiagnostics.Begin(scene);
                var hash = Hash(City);
                var audit = new ExteriorAudit(scene);
                var comparison = audit.Build(includeMonuments: false);
                try
                {
                    Debug.Log("NavMesh exterior. Comparación A: sin fuentes monumentales. Los colliders físicos siguen activos.");
                    audit.ValidateCandidate(comparison, requirePass: false);
                }
                finally
                {
                    Object.DestroyImmediate(comparison);
                    CiudadDelSolNavMeshDiagnostics.Begin(scene);
                }
                var gateControl = audit.Build(excludeGate: true);
                try
                {
                    Debug.Log("NavMesh exterior. Control del portón: todas las fuentes de B salvo SM_MainGate. Sus colliders físicos siguen activos.");
                    audit.ValidateCandidate(gateControl, gateControlOnly: true);
                }
                finally
                {
                    Object.DestroyImmediate(gateControl);
                }
                Debug.Log("NavMesh exterior. Candidata B: con fuentes monumentales. Solo esta versión puede guardarse.");
                candidate = audit.Build();
                audit.ValidateCandidate(candidate);
                Check(Hash(City) == hash && !scene.isDirty, "La escena cambió durante la validación.");
                validatedHash = hash;
                Debug.Log("NavMesh exterior. Candidata validada en memoria. Usá 2. Respaldar y guardar candidata validada para persistirla.");
            }
            catch (Exception error)
            {
                Discard();
                Debug.LogError("NavMesh exterior. Candidata rechazada. " + error.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                CiudadDelSolNavMeshDiagnostics.Refresh();
            }
        }

        [MenuItem(Menu + "2. Respaldar y guardar candidata validada")]
        static void Save()
        {
            string backup = null;
            GameObject root = null;
            NavMeshSurface previous = null;
            bool assetCreated = false, saved = false;
            try
            {
                var scene = CleanCity();
                Check(candidate && validatedHash == Hash(City), "Horneá y validá la candidata para la versión actual de la escena.");
                Check(!File.Exists(Asset) && !AssetDatabase.LoadMainAssetAtPath(Asset), "Ya existe un asset exterior. Se requiere revisar la integración existente.");
                previous = FindPatio(scene);
                backup = CreateBackup(previous);

                AssetDatabase.CreateAsset(candidate, Asset);
                assetCreated = true;
                root = new GameObject(Exterior);
                root.SetActive(false);
                var surface = root.AddComponent<NavMeshSurface>();
                surface.agentTypeID = 0;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.navMeshData = candidate;
                previous.enabled = false;
                root.SetActive(true);
                Check(EditorSceneManager.SaveScene(scene), "Unity no pudo guardar la escena.");
                saved = true;
                candidate = null;
                validatedHash = null;
                Debug.Log($"NavMesh exterior guardado en {Asset}. Respaldo verificado en {backup}. Reabrí la escena y ejecutá la opción 3.");
            }
            catch (Exception error)
            {
                if (!saved)
                {
                    if (root) Object.DestroyImmediate(root);
                    if (previous && !previous.enabled) previous.enabled = true;
                    if (assetCreated)
                    {
                        candidate = null;
                        validatedHash = null;
                        AssetDatabase.DeleteAsset(Asset);
                    }
                }
                Debug.LogError("NavMesh exterior. Guardado interrumpido. " + error.Message +
                    (backup == null ? "" : " Respaldo: " + backup));
            }
        }

        [MenuItem(Menu + "3. Verificar NavMesh persistente")]
        static void VerifySaved()
        {
            try
            {
                var scene = CleanCity();
                CiudadDelSolNavMeshDiagnostics.Begin(scene);
                var audit = new ExteriorAudit(scene);
                var root = scene.GetRootGameObjects().Single(item => item.name == Exterior);
                var surface = root.GetComponent<NavMeshSurface>();
                Check(surface && surface.isActiveAndEnabled && !audit.PatioSurface.enabled &&
                    surface.agentTypeID == 0 && surface.navMeshData == AssetDatabase.LoadAssetAtPath<NavMeshData>(Asset) &&
                    surface.navMeshData, "La referencia persistente o el estado de la superficie del patio no coinciden.");
                audit.ValidateRegistered();
                Debug.Log("NavMesh exterior. Referencia persistente y validaciones correctas.");
            }
            catch (Exception error) { Debug.LogError("NavMesh exterior. " + error.Message); }
            finally
            {
                EditorUtility.ClearProgressBar();
                CiudadDelSolNavMeshDiagnostics.Refresh();
            }
        }

        [MenuItem(Menu + "4. Descartar candidata en memoria")]
        static void Discard()
        {
            if (candidate) Object.DestroyImmediate(candidate);
            candidate = null;
            validatedHash = null;
        }

        static Scene CleanCity()
        {
            var scene = SceneManager.GetActiveScene();
            Check(!EditorApplication.isPlayingOrWillChangePlaymode && scene.path == City &&
                scene.isLoaded && SceneManager.sceneCount == 1 && !scene.isDirty,
                "Abrí únicamente Ciudad del Sol en modo edición, sin cambios pendientes.");
            return scene;
        }

        static NavMeshSurface FindPatio(Scene scene)
        {
            var root = scene.GetRootGameObjects().Single(item => item.name == Patio);
            var surface = root.GetComponent<NavMeshSurface>();
            Check(surface && surface.navMeshData && surface.agentTypeID == 0, "No se encontró la superficie aprobada del patio.");
            return surface;
        }

        static string CreateBackup(NavMeshSurface patio)
        {
            var folder = Path.Combine("Backups/CiudadDelSol/NavMeshExterior", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(folder);
            string nav = AssetDatabase.GetAssetPath(patio.navMeshData);
            var manifest = new List<string>();
            foreach (var path in new[] { City, City + ".meta", nav, nav + ".meta" })
            {
                var copy = Path.Combine(folder, Path.GetFileName(path));
                File.Copy(path, copy);
                var hash = Hash(path);
                Check(Hash(copy) == hash, "El respaldo no coincide con " + path);
                manifest.Add(hash + "  " + path);
            }
            File.WriteAllLines(Path.Combine(folder, "SHA256.txt"), manifest);
            return Path.GetFullPath(folder);
        }

        static string Hash(string path)
        {
            using var algorithm = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "");
        }

        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        sealed class ExteriorAudit
        {
            readonly Scene scene;
            readonly Transform[] zones, routes;
            readonly Transform entrance;
            readonly Dictionary<Transform, List<Collider>> floors = new Dictionary<Transform, List<Collider>>();
            readonly HashSet<Collider> walkable = new HashSet<Collider>();
            readonly List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
            readonly NavMeshBuildSource[] solidLandmarks;
            readonly NavMeshBuildSettings settings;
            readonly NavMeshPath path = new NavMeshPath();
            readonly Collider[] clearance = new Collider[1];
            readonly RaycastHit[] columnHits = new RaycastHit[32];
            readonly Dictionary<string, int> structuralSources = new Dictionary<string, int>();
            Bounds volume;
            readonly Bounds gardenPond;
            string diagnosticGroup;
            int diagnosticPriority;
            bool diagnosticIsland;
            internal readonly NavMeshSurface PatioSurface;

            internal ExteriorAudit(Scene scene)
            {
                this.scene = scene;
                Physics.SyncTransforms();
                var roots = scene.GetRootGameObjects();
                var city = roots.Single(root => root.name == "Dungeon_Ciudad_del_Sol_Generated");
                var nodes = city.GetComponentsInChildren<Transform>();
                zones = Sectors.Select(name => nodes.Single(node => node.name == name)).ToArray();
                routes = RouteIds.Select(id => nodes.Single(node => Regex.IsMatch(node.name, "^" + id + " - .*ancho util"))).ToArray();
                entrance = roots.Single(root => root.name == "Salida_Meridional_Generated" && root.activeInHierarchy).transform;
                PatioSurface = FindPatio(scene);
                var buildSettings = NavMesh.GetSettingsByID(0);
                // Resolve narrow contacts and the 25 cm treads without reducing agent clearance.
                buildSettings.overrideVoxelSize = true;
                buildSettings.voxelSize = .10f;
                buildSettings.buildHeightMesh = true;
                settings = buildSettings;
                Check(settings.agentTypeID == 0 && Mathf.Abs(settings.agentRadius - .57f) <= .001f &&
                    Mathf.Abs(settings.agentClimb - .36f) <= .001f && settings.agentHeight >= 1.30f,
                    $"Humanoid 0 fuera de la configuración auditada: tipo {settings.agentTypeID}, " +
                    $"radio {settings.agentRadius:F3}, escalón {settings.agentClimb:F3}, altura {settings.agentHeight:F3}.");

                foreach (var group in zones.Concat(routes).Append(entrance))
                {
                    var collected = new List<NavMeshBuildSource>();
                    NavMeshBuilder.CollectSources(group, ~0, NavMeshCollectGeometry.PhysicsColliders,
                        1, new List<NavMeshBuildMarkup>(), collected);
                    var groupFloors = floors[group] = new List<Collider>();
                    foreach (var item in collected)
                    {
                        if (!(item.component is Collider collider) || !collider.enabled || collider.isTrigger ||
                            !collider.gameObject.activeInHierarchy || collider.gameObject.scene != scene) continue;
                        var source = item;
                        if (CiudadDelSolExteriorSourceRoles.IsStructuralPlatform(collider, group))
                        {
                            structuralSources.TryGetValue(group.name, out int count);
                            structuralSources[group.name] = count + 1;
                        }
                        else if (IsWalkingSurface(collider, group))
                        {
                            source.area = 0;
                            if (walkable.Add(collider))
                            {
                                groupFloors.Add(collider);
                                if (walkable.Count == 1) volume = collider.bounds;
                                else volume.Encapsulate(collider.bounds);
                            }
                        }
                        sources.Add(source);
                    }
                    Check(groupFloors.Count > 0, "Sin fuentes de piso reconocidas en " + group.name);
                }
                var landmarks = nodes.Single(node => node.name == "Arquitectura monumental - fuera de los pasos");
                var landmarkSources = new List<NavMeshBuildSource>();
                NavMeshBuilder.CollectSources(landmarks, ~0, NavMeshCollectGeometry.PhysicsColliders,
                    1, new List<NavMeshBuildMarkup>(), landmarkSources);
                solidLandmarks = landmarkSources.Where(source => source.component is Collider collider &&
                    collider.enabled && !collider.isTrigger && collider.gameObject.activeInHierarchy &&
                    collider.gameObject.scene == scene).ToArray();
                Check(solidLandmarks.Length > 0, "El grupo monumental no aportó sus colliders sólidos al horneado.");
                Debug.Log($"NavMesh exterior. {solidLandmarks.Length} fuentes de torres, arcos y portón incorporadas solo como obstáculos.");
                gardenPond = CiudadDelSolExteriorSourceRoles.GardenPondInterior(zones[1]);
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.ModifierBox,
                    transform = Matrix4x4.Translate(gardenPond.center),
                    size = gardenPond.size,
                    area = 1
                });
                Debug.Log($"NavMesh exterior. Interior ornamental del estanque excluido entre {gardenPond.min:F2} y {gardenPond.max:F2}.");
                int curbs = IncludeSupportedLowCurbs();
                Debug.Log($"NavMesh exterior. {curbs} bordillos bajos incorporados con suelo comprobado a ambos lados.");
                int stairSources = CiudadDelSolExteriorStairSources.Normalize(sources, entrance, walkable);
                Debug.Log($"NavMesh exterior. Salida Meridional: 32 bloques físicos normalizados en {stairSources} fuentes de unión.");
                volume.Expand(new Vector3(2, 2 * settings.agentHeight, 2));
            }

            int IncludeSupportedLowCurbs()
            {
                var accepted = new List<(int index, Collider collider)>();
                for (int i = 0; i < sources.Count; i++)
                {
                    var source = sources[i];
                    if (!(source.component is BoxCollider box) || !CiudadDelSolExteriorSourceRoles.IsCrossableCurbCandidate(box))
                        continue;
                    bool crossed = HasFloorOnBothSides(box, out string detail);
                    Debug.Log($"NavMesh exterior. Bordillo {AnimationUtility.CalculateTransformPath(box.transform, null)} " +
                        $"{box.bounds.center:F3}: {detail}.");
                    if (crossed) accepted.Add((i, box));
                }
                foreach (var item in accepted)
                {
                    var source = sources[item.index];
                    source.area = 0;
                    sources[item.index] = source;
                    walkable.Add(item.collider);
                }
                return accepted.Count;
            }

            bool HasFloorOnBothSides(BoxCollider box, out string detail)
            {
                detail = "ejes incompatibles con una coronación plana";
                var xAxis = box.transform.TransformVector(Vector3.right * box.size.x);
                var zAxis = box.transform.TransformVector(Vector3.forward * box.size.z);
                if (Mathf.Abs(xAxis.normalized.y) > .001f || Mathf.Abs(zAxis.normalized.y) > .001f ||
                    Mathf.Abs(Vector3.Dot(xAxis.normalized, zAxis.normalized)) > .001f) return false;
                bool alongX = xAxis.sqrMagnitude > zAxis.sqrMagnitude;
                var along = alongX ? xAxis : zAxis;
                var across = alongX ? zAxis : xAxis;
                var bounds = box.bounds;
                var top = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                float highestStep = 0f;
                float reach = across.magnitude * .5f + settings.agentRadius + .10f;
                int steps = Mathf.CeilToInt(2f * reach / settings.voxelSize);
                var physics = scene.GetPhysicsScene();
                float radius = settings.agentRadius;
                foreach (float offset in new[] { -.45f, -.25f, 0f, .25f, .45f })
                {
                    for (int i = 0; i <= steps; i++)
                    {
                        var point = top + along * offset + across.normalized * Mathf.Lerp(-reach, reach, (float)i / steps);
                        bool found = physics.Raycast(point + Vector3.up * .04f, Vector3.down, out var hit,
                            settings.agentClimb + .04f, ~0, QueryTriggerInteraction.Ignore);
                        bool onCurb = found && hit.collider == box;
                        if (!found || (!onCurb && !walkable.Contains(hit.collider)) || !WalkableSlope(hit.normal) ||
                            Mathf.Abs(hit.point.y - top.y) > settings.agentClimb ||
                            ((i == 0 || i == steps) && onCurb) || gardenPond.Contains(hit.point))
                        {
                            detail = $"apoyo o escalón no demostrados en {point:F3}, impacto {hit.collider?.name ?? "ninguno"}";
                            return false;
                        }
                        if (physics.OverlapCapsule(
                            hit.point + Vector3.up * (settings.agentClimb + radius + .02f),
                            hit.point + Vector3.up * (settings.agentHeight - radius + .02f), radius,
                            clearance, ~0, QueryTriggerInteraction.Ignore) != 0)
                        {
                            detail = $"cápsula obstruida por {clearance[0].name} en {hit.point:F3}";
                            return false;
                        }
                        highestStep = Mathf.Max(highestStep, Mathf.Abs(top.y - hit.point.y));
                    }
                }
                detail = $"apoyo y cápsula comprobados a través del bordillo, desnivel máximo {highestStep:F3} m";
                return true;
            }

            static bool IsWalkingSurface(Collider collider, Transform group)
            {
                if (collider.bounds.max.y < -2.10f) return false;
                for (var node = collider.transform; node && node != group; node = node.parent)
                {
                    string name = node.name;
                    if (name.StartsWith("Platform_Base_01", StringComparison.Ordinal) ||
                        name.StartsWith("Staircase_01", StringComparison.Ordinal) ||
                        name.StartsWith("Road_01", StringComparison.Ordinal) ||
                        name.StartsWith("Road_End_01", StringComparison.Ordinal) ||
                        name.StartsWith("Plaza_Floor_01", StringComparison.Ordinal))
                        return true;
                }
                return false;
            }

            internal NavMeshData Build(bool includeMonuments = true, bool excludeGate = false)
            {
                Check(!scene.GetRootGameObjects().Any(root => root.name == Exterior) && PatioSurface.isActiveAndEnabled,
                    "La escena ya tiene una integración exterior o el patio aprobado no está activo.");
                ShowProgress("Horneando colliders del exterior", .05f);
                var buildSources = includeMonuments
                    ? sources.Concat(solidLandmarks.Where(source => !excludeGate || !IsGateCollider((Collider)source.component))).ToList()
                    : sources;
                if (excludeGate)
                    Check(buildSources.Count < sources.Count + solidLandmarks.Length, "No se identificaron fuentes de SM_MainGate.");
                var data = NavMeshBuilder.BuildNavMeshData(settings, buildSources, volume, Vector3.zero, Quaternion.identity);
                Check(data, "Unity no generó datos de navegación.");
                data.name = "CiudadDelSol_Exterior_NavMesh";
                Debug.Log($"NavMesh exterior. {buildSources.Count} fuentes, {walkable.Count} colliders clasificados como suelo.");
                Debug.Log($"NavMesh exterior. Vóxel {settings.voxelSize:F2} m, detalle de altura activo, " +
                    $"radio {settings.agentRadius:F2} m, escalón {settings.agentClimb:F2} m. Plataformas estructurales solo como obstáculos: " +
                    string.Join(" | ", structuralSources.Select(pair => $"{pair.Key}: {pair.Value}")));
                return data;
            }

            internal void ValidateCandidate(NavMeshData data, bool requirePass = true, bool gateControlOnly = false)
            {
                PatioSurface.RemoveData();
                NavMeshDataInstance instance = default;
                try
                {
                    Check(NavMesh.CalculateTriangulation().indices.Length == 0,
                        "Hay otra superficie de navegación registrada. La candidata no puede validarse aisladamente.");
                    instance = NavMesh.AddNavMeshData(data);
                    Check(instance.valid, "No se pudo registrar la candidata.");
                    if (gateControlOnly) ReportGatePaths();
                    else ValidateRegistered(requirePass);
                }
                finally
                {
                    if (instance.valid) instance.Remove();
                    PatioSurface.AddData();
                }
            }

            sealed class Findings
            {
                internal int Duplicate, Narrow, Covered;
                internal readonly List<string> Coverage = new List<string>();
                internal readonly List<string> Connectivity = new List<string>();
                internal readonly List<string> Geometry = new List<string>();
                internal int Failures => Coverage.Count + Connectivity.Count + Geometry.Count;

                internal void Report(string group)
                {
                    string summary = $"NavMesh exterior. {group}: cobertura {Covered} válidas / {Coverage.Count} fallos; " +
                        $"conectividad {Connectivity.Count} fallos; geometría {Geometry.Count} fallos; " +
                        $"duplicadas {Duplicate}; sin holgura física o de borde {Narrow}.";
                    if (Failures == 0) { Debug.Log(summary); return; }
                    var examples = Coverage.Take(6).Concat(Connectivity.Take(2)).Concat(Geometry.Take(2));
                    Debug.LogWarning(summary + "\n" + string.Join("\n", examples));
                }
            }

            internal void ValidateRegistered(bool requirePass = true)
            {
                Physics.SyncTransforms();
                var groups = zones.Concat(routes).Append(entrance).ToArray();
                var results = groups.ToDictionary(group => group, _ => new Findings());
                var probes = new Dictionary<Transform, List<Vector3>>();
                var physical = new Dictionary<Transform, List<Vector3>>();
                foreach (var group in groups)
                {
                    DiagnosticScope(group.name, routes.Contains(group) || group == entrance ? 0 : 1);
                    probes[group] = SampleFloors(group, results[group], out var floorPoints);
                    physical[group] = floorPoints;
                }

                foreach (var zone in zones)
                {
                    DiagnosticScope(zone.name, 1);
                    var points = probes[zone];
                    var finding = results[zone];
                    if (points.Count == 0)
                    {
                        finding.Coverage.Add("Sector sin piso navegable comprobado.");
                        Diagnostic(NavIssueKind.Coverage, floors[zone][0].bounds.center, "Sector sin piso navegable comprobado.");
                    }
                    foreach (var point in points)
                    {
                        if (!BothWays(points[0], point, out string issue)) finding.Connectivity.Add(issue);
                    }
                }

                for (int i = 0; i < routes.Length; i++)
                {
                    var route = routes[i];
                    DiagnosticScope(route.name, 0);
                    int from = int.Parse(RouteIds[i].Substring(0, 2)) - 1;
                    int to = int.Parse(RouteIds[i].Substring(3, 2)) - 1;
                    ValidateConnection(route, zones[from], zones[to], physical, probes, results[route]);
                }

                var entranceFinding = results[entrance];
                DiagnosticScope(entrance.name, 0);
                if (probes[entrance].Count < 2)
                {
                    entranceFinding.Coverage.Add("Faltan muestras navegables sobre la pasarela.");
                    Diagnostic(NavIssueKind.Coverage, floors[entrance][0].bounds.center, "Faltan muestras navegables sobre la pasarela.");
                }
                bool spawnCovered = Project(new Vector3(30f, -2f, -206f), out var entranceSpawn);
                if (!spawnCovered)
                {
                    entranceFinding.Coverage.Add("El extremo de aparición de la pasarela carece de NavMesh.");
                    Diagnostic(NavIssueKind.Coverage, new Vector3(30f, -2f, -206f), "Extremo de aparición sin NavMesh.");
                }
                var portico = probes[zones[0]];
                if (portico.Count > 0)
                {
                    var entrancePoints = probes[entrance].AsEnumerable();
                    if (spawnCovered) entrancePoints = entrancePoints.Append(entranceSpawn);
                    foreach (var point in entrancePoints)
                    {
                        if (!BothWays(portico[0], point, out string issue)) entranceFinding.Connectivity.Add(issue);
                    }
                }

                var patio = new Findings();
                DiagnosticScope(Patio, 0);
                ValidatePatio(probes[zones[1]], patio);
                var global = new Findings();
                DiagnosticScope("Malla global", 2);
                diagnosticIsland = true;
                ValidateTriangles(portico.Count > 0, portico.FirstOrDefault(), global);
                diagnosticIsland = false;
                ReportKnownCuts();
                ReportCloisterStairJoins();
                ReportPlazaMargins();
                ReportGatePaths();
                foreach (var group in groups) results[group].Report(group.name);
                patio.Report("Patio de Jardines Dorados");
                global.Report("Malla global");
                var all = results.Values.Append(patio).Append(global).ToArray();
                string totals = $"Cobertura: {all.Sum(item => item.Coverage.Count)} fallos; conectividad: " +
                    $"{all.Sum(item => item.Connectivity.Count)}; geometría: {all.Sum(item => item.Geometry.Count)}.";
                if (requirePass) Check(all.All(item => item.Failures == 0), totals);
                Debug.Log("NavMesh exterior. 7 sectores, 9 conexiones, pasarela y patio evaluados. " + totals);
            }

            void DiagnosticScope(string group, int priority)
            {
                diagnosticGroup = group;
                diagnosticPriority = priority;
            }

            void ReportKnownCuts()
            {
                var probes = new[]
                {
                    new Vector3(-145f, 4f, -57f), new Vector3(-143f, 4f, -59f),
                    new Vector3(-143f, 4f, 13f), new Vector3(-133f, 4f, -59f), new Vector3(-133f, 4f, 13f),
                    new Vector3(-23f, 28f, 157f), new Vector3(-22.3f, 32f, 155.4f),
                    new Vector3(165.7f, 36f, 201.1f), new Vector3(167f, 36f, 197f), new Vector3(229f, 36f, 127f),
                    new Vector3(3f, 12f, -39f), new Vector3(41f, 12f, 55f),
                    new Vector3(204f, 36f, 108f), new Vector3(22.667f, -2f, -165.633f)
                };
                var overlaps = new Collider[32];
                var physics = scene.GetPhysicsScene();
                foreach (var point in probes)
                {
                    bool floor = PhysicalFloor(point, .30f, .35f, out var hit);
                    var foot = floor ? hit.point : point;
                    float radius = settings.agentRadius;
                    int count = physics.OverlapCapsule(
                        foot + Vector3.up * (settings.agentClimb + radius + .02f),
                        foot + Vector3.up * (settings.agentHeight - radius + .02f),
                        radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
                    string support = hit.collider ? AnimationUtility.CalculateTransformPath(hit.collider.transform, null) +
                        $" a {hit.point.y:F3} (suelo compatible: {floor})" : "sin impacto";
                    string blockers = count == 0 ? "ninguno" : string.Join(" | ", overlaps.Take(count)
                        .Select(collider => AnimationUtility.CalculateTransformPath(collider.transform, null)).Distinct());
                    string projected = Project(foot, out var nav) ? nav.ToString("F3") : "sin proyección";
                    Debug.Log($"NavMesh exterior. Sonda {point:F3}: apoyo {support}; NavMesh {projected}; " +
                        $"huella física {(floor && HasAgentClearance(foot) ? "válida" : "no confirmada")}; " +
                        $"colliders en cápsula {blockers}. {NearbyMonuments(point)}");
                }
            }

            bool IsGateCollider(Collider collider)
            {
                for (var node = collider.transform; node; node = node.parent)
                    if (node.name == "SM_MainGate" && node.parent &&
                        node.parent.name == "Arquitectura monumental - fuera de los pasos") return true;
                return false;
            }

            void ReportCloisterStairJoins()
            {
                var sector = zones[4];
                var stairs = sector.GetComponentsInChildren<Transform>().SingleOrDefault(node => node.name == "03 - Escalinatas conectadas");
                if (!stairs)
                {
                    Debug.LogWarning("NavMesh exterior. Claustro: no se encontró el grupo de escalinatas.");
                    return;
                }
                var corridor = floors[sector].Select(collider => collider.bounds).ToArray();
                foreach (var stair in floors[sector].Where(collider => collider.transform.IsChildOf(stairs) &&
                    collider.bounds.min.y < 28.50f && Mathf.Abs(collider.bounds.max.y - 32f) <= .50f))
                {
                    var bounds = stair.bounds;
                    var south = new Vector3(bounds.center.x, 28f, bounds.min.z - .15f);
                    var north = new Vector3(bounds.center.x, 32f, bounds.max.z + .15f);
                    var result = DescribeStairJoin(south, north, corridor);
                    if (result != null)
                    {
                        south.y = 32f;
                        north.y = 28f;
                        result += " / orientación inversa: " + (DescribeStairJoin(north, south, corridor) ?? "continua");
                    }
                    Debug.Log($"NavMesh exterior. Claustro: {AnimationUtility.CalculateTransformPath(stair.transform, null)} " +
                        $"{bounds.center:F3}, pie y desembarco Y=28/32: {result ?? "apoyo continuo y rutas completas"}.");
                }
            }

            string DescribeStairJoin(Vector3 lower, Vector3 upper, Bounds[] corridor)
            {
                if (!PhysicalFloor(lower, .12f, .12f, out var bottom) || !PhysicalFloor(upper, .12f, .12f, out var top))
                    return $"apoyo de extremo sin confirmar entre {lower:F3} y {upper:F3}";
                var support = bottom.point;
                int steps = Mathf.CeilToInt(Vector3.Distance(bottom.point, top.point) / settings.voxelSize);
                for (int i = 1; i <= steps; i++)
                {
                    var probe = Vector3.Lerp(bottom.point, top.point, (float)i / steps);
                    if (!TryPathSupport(probe, support, corridor, out support, out _))
                        return $"interrupción de apoyo cerca de {probe:F3}";
                }
                if (Mathf.Abs(support.y - top.point.y) > .10f ||
                    !Project(bottom.point, out var from) || !Project(top.point, out var to))
                    return "el apoyo no llega a la cota proyectable del desembarco";
                if (NavMesh.Raycast(from, to, out var cut, Filter))
                    return $"la malla corta el eje cerca de {cut.position:F3}";
                if (NavMesh.Raycast(to, from, out cut, Filter))
                    return $"la malla corta el eje en sentido inverso cerca de {cut.position:F3}";
                if (!NavMesh.CalculatePath(from, to, Filter, path) || path.status != NavMeshPathStatus.PathComplete ||
                    !NavMesh.CalculatePath(to, from, Filter, path) || path.status != NavMeshPathStatus.PathComplete)
                    return "falta un camino completo en ambos sentidos";
                return null;
            }

            void ReportPlazaMargins()
            {
                var obstacles = sources.Where(source => source.area == 1).Select(source => source.component)
                    .OfType<Collider>().Concat(solidLandmarks.Select(source => (Collider)source.component));
                foreach (var point in new[] { new Vector3(3f, 12f, -39f), new Vector3(41f, 12f, 55f) })
                {
                    float unsupported = float.PositiveInfinity;
                    for (int direction = 0; direction < 16; direction++)
                    {
                        float angle = direction * Mathf.PI / 8f;
                        var axis = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                        for (int sample = 1; sample <= 40; sample++)
                        {
                            float distance = sample * .05f;
                            if (PhysicalFloor(point + axis * distance, .10f, .10f, out _)) continue;
                            unsupported = Mathf.Min(unsupported, distance);
                            break;
                        }
                    }
                    var body = point + Vector3.up * .75f;
                    var nearby = obstacles.Where(collider => collider.bounds.SqrDistance(body) <= 4f)
                        .Select(collider =>
                        {
                            bool exact = collider is BoxCollider || collider is SphereCollider ||
                                collider is CapsuleCollider || (collider is MeshCollider mesh && mesh.convex);
                            float distance = exact ? Vector3.Distance(body, collider.ClosestPoint(body)) :
                                Mathf.Sqrt(collider.bounds.SqrDistance(body));
                            return (collider, distance, exact);
                        }).OrderBy(item => item.distance).Take(4);
                    string hits = string.Join(" | ", nearby.Select(item =>
                        $"{AnimationUtility.CalculateTransformPath(item.collider.transform, null)} " +
                        $"{(item.exact ? "" : "cota inferior AABB ")}{item.distance:F3} m"));
                    string edge = float.IsPositiveInfinity(unsupported) ? ">2 m en las direcciones muestreadas" : $"{unsupported:F2} m";
                    Debug.Log($"NavMesh exterior. Plaza {point:F3}: primera pérdida de apoyo a la misma cota {edge}; " +
                        $"distancias a obstáculos desde 0,75 m sobre el piso: {hits}.");
                }
            }

            void ReportGatePaths()
            {
                var origin = new Vector3(121f, 36f, 139f);
                foreach (float x in new[] { 167f, 169f })
                {
                    var destination = new Vector3(x, 36f, 197f);
                    if (!Project(origin, out var start) || !Project(destination, out var end))
                    {
                        Debug.LogWarning($"NavMesh exterior. Portón: sin proyección del par {origin:F3} / {destination:F3}.");
                        continue;
                    }
                    ReportGatePath(start, end);
                    ReportGatePath(end, start);
                }
            }

            void ReportGatePath(Vector3 from, Vector3 to)
            {
                if (!NavMesh.CalculatePath(from, to, Filter, path) || path.status != NavMeshPathStatus.PathComplete)
                {
                    Debug.Log($"NavMesh exterior. Portón: {from:F3} → {to:F3}, {path.status}.");
                    return;
                }
                var corners = path.corners;
                var overlaps = new Collider[32];
                float radius = settings.agentRadius;
                var lower = Vector3.up * (settings.agentClimb + radius + .02f);
                var upper = Vector3.up * (settings.agentHeight - radius + .02f);
                for (int i = 1; i < corners.Length; i++)
                {
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(corners[i - 1], corners[i]) / settings.voxelSize));
                    for (int step = 0; step <= steps; step++)
                    {
                        var point = Vector3.Lerp(corners[i - 1], corners[i], (float)step / steps);
                        int count = scene.GetPhysicsScene().OverlapCapsule(point + lower, point + upper,
                            radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
                        var gate = overlaps.Take(count).FirstOrDefault(IsGateCollider);
                        if (gate)
                        {
                            Debug.LogWarning($"NavMesh exterior. Portón: {from:F3} → {to:F3} completo, pero la cápsula invade " +
                                $"{AnimationUtility.CalculateTransformPath(gate.transform, null)} en {point:F3}.");
                            return;
                        }
                        if (count == overlaps.Length)
                        {
                            Debug.LogWarning($"NavMesh exterior. Portón: consulta física saturada en {point:F3}.");
                            return;
                        }
                    }
                }
                Debug.Log($"NavMesh exterior. Portón: {from:F3} → {to:F3} completo, sin contacto con el portón en el muestreo.");
            }

            void Diagnostic(NavIssueKind kind, Vector3 point, string detail, Vector3? last = null,
                Vector3? destination = null)
            {
                CiudadDelSolNavMeshDiagnostics.Add(diagnosticGroup, diagnosticPriority, kind, point, detail, last, destination);
            }

            void ValidateConnection(Transform route, Transform from, Transform to,
                Dictionary<Transform, List<Vector3>> physical, Dictionary<Transform, List<Vector3>> probes, Findings finding)
            {
                var samples = physical[route];
                if (samples.Count < 2 || physical[from].Count == 0 || physical[to].Count == 0)
                {
                    finding.Connectivity.Add("Faltan muestras físicas en la conexión o sus sectores.");
                    Diagnostic(NavIssueKind.Support, samples.Count > 0 ? samples[0] : floors[route][0].bounds.center,
                        "Faltan muestras físicas en la conexión o sus sectores.");
                    return;
                }

                // Choose the joins from the physical floor, before projecting on the candidate.
                Vector3 aFloor = Nearest(samples, physical[from]);
                Vector3 bFloor = Nearest(samples, physical[to]);
                Vector3 fromFloor = Nearest(physical[from], new[] { aFloor });
                Vector3 toFloor = Nearest(physical[to], new[] { bFloor });
                if (Vector3.Distance(aFloor, bFloor) < .5f)
                {
                    finding.Connectivity.Add($"Los encuentros físicos de ambos sectores coinciden cerca de {aFloor:F3}.");
                    Diagnostic(NavIssueKind.Corridor, aFloor, "Los dos encuentros físicos coinciden en la misma zona.");
                    return;
                }
                if (!Project(aFloor, out var a) || !Project(bFloor, out var b) ||
                    !Project(fromFloor, out var aSector) || !Project(toFloor, out var bSector))
                {
                    finding.Coverage.Add($"Encuentro obligatorio sin NavMesh: {fromFloor:F3} → {aFloor:F3} / {bFloor:F3} → {toFloor:F3}.");
                    foreach (var point in new[] { fromFloor, aFloor, bFloor, toFloor })
                    {
                        if (!Project(point, out _)) Diagnostic(NavIssueKind.Coverage, point, "Encuentro obligatorio sin NavMesh.");
                    }
                    return;
                }

                var corridor = floors[route].Select(collider => collider.bounds).ToArray();
                if (route.name.StartsWith("01-02 - ", StringComparison.Ordinal))
                    ValidateGardenStairs(corridor, finding);
                if (!route.name.StartsWith("01-02 - ", StringComparison.Ordinal) &&
                    !route.name.StartsWith("02-03 - ", StringComparison.Ordinal))
                {
                    ValidateRouteProfile(route, from, to, a, b, probes[route], corridor, finding);
                }
                else
                {
                    if (!BothWays(a, b, out string corridorIssue, corridor: corridor))
                        finding.Connectivity.Add("Extremos del corredor: " + corridorIssue);
                    foreach (var point in probes[route])
                    {
                        if (!BothWays(a, point, out string localIssue, corridor: corridor))
                            finding.Connectivity.Add(localIssue);
                    }
                }
                if (!ValidateSectorJoin(a, aSector, route, from, out string issue))
                    finding.Connectivity.Add("Unión con " + from.name + ": " + issue);
                if (!ValidateSectorJoin(b, bSector, route, to, out issue))
                    finding.Connectivity.Add("Unión con " + to.name + ": " + issue);
            }

            bool ValidateSectorJoin(Vector3 port, Vector3 sectorPoint, Transform route, Transform sector, out string issue)
            {
                var corridor = floors[route].Concat(floors[sector]).Select(collider => collider.bounds).ToArray();
                return BothWays(port, sectorPoint, out issue, Vector3.Distance(port, sectorPoint) + 4f, corridor);
            }

            void ValidateRouteProfile(Transform route, Transform fromSector, Transform toSector,
                Vector3 from, Vector3 to, List<Vector3> samples,
                Bounds[] corridor, Findings finding)
            {
                string routeId = route.name.Substring(0, 5);
                var authoredPoints = SunDungeonStructureGenerator.GetExteriorRouteCheckpoints(routeId);
                var checkpoints = new Vector3?[authoredPoints.Length + 2];
                checkpoints[0] = from;
                checkpoints[checkpoints.Length - 1] = to;
                for (int i = 0; i < authoredPoints.Length; i++)
                {
                    var authored = authoredPoints[i];
                    if (!TryRouteCheckpoint(authored, corridor, out var support))
                    {
                        // The physical sector join is already required in both directions.
                        // Use it at an overlapping end only when it belongs to the same deck module and grade.
                        bool end = i == authoredPoints.Length - 1;
                        if ((i == 0 || end) && TryCoplanarPort(authored, end ? to : from, route,
                            end ? toSector : fromSector, out var port, out var portTiles))
                        {
                            checkpoints[i + 1] = port;
                            corridor = corridor.Concat(portTiles).ToArray();
                            Debug.Log($"NavMesh exterior. {routeId}: control {authored:F3} comprobado sobre la unión coplanar en {port:F3}.");
                            continue;
                        }
                        if ((i == 0 || end) && IsSamePortDeck(authored, end ? to : from, floors[route]))
                        {
                            checkpoints[i + 1] = end ? to : from;
                            Debug.Log($"NavMesh exterior. {routeId}: unión comprobable del control {authored:F3} en {(end ? to : from):F3}.");
                            continue;
                        }
                        var issue = $"Control de trazado sin apoyo en {authored:F3}.";
                        finding.Geometry.Add(issue);
                        Diagnostic(NavIssueKind.Support, authored, issue);
                        continue;
                    }
                    if (!Project(support.point, out var point))
                    {
                        var issue = $"Control de trazado sin NavMesh en {support.point:F3} ({support.collider.name}).";
                        finding.Coverage.Add(issue);
                        Diagnostic(NavIssueKind.Coverage, support.point, issue);
                        continue;
                    }
                    checkpoints[i + 1] = point;
                }
                for (int i = 1; i < checkpoints.Length; i++)
                {
                    if (!checkpoints[i - 1].HasValue || !checkpoints[i].HasValue)
                    {
                        Debug.LogWarning($"NavMesh exterior. {routeId}: tramo obligatorio {i}/{checkpoints.Length - 1} sin evaluar por control inválido.");
                        continue;
                    }
                    if (!BothWays(checkpoints[i - 1].Value, checkpoints[i].Value, out string issue, corridor: corridor))
                        finding.Connectivity.Add($"Tramo obligatorio {i}/{checkpoints.Length - 1}: {issue}");
                }
                var trace = new[] { from }.Concat(authoredPoints).Append(to).ToArray();
                int skipped = 0;
                foreach (var sample in samples)
                {
                    int segment = Enumerable.Range(0, trace.Length - 1)
                        .OrderBy(index => TraceDistance(sample, trace[index], trace[index + 1])).First();
                    var start = checkpoints[segment];
                    var end = checkpoints[segment + 1];
                    if (!start.HasValue || !end.HasValue) { skipped++; continue; }
                    var checkpoint = (start.Value - sample).sqrMagnitude <= (end.Value - sample).sqrMagnitude
                        ? start.Value : end.Value;
                    if (!BothWays(checkpoint, sample, out string issue, corridor: corridor))
                        finding.Connectivity.Add("Muestra lateral: " + issue);
                }
                Debug.Log($"NavMesh exterior. {routeId}: {checkpoints.Count(point => point.HasValue)}/{checkpoints.Length} " +
                    $"controles válidos y {samples.Count - skipped}/{samples.Count} muestras laterales evaluadas.");
            }

            bool TryCoplanarPort(Vector3 authored, Vector3 join, Transform route, Transform sector, out Vector3 port,
                out Bounds[] portTiles)
            {
                port = default;
                portTiles = Array.Empty<Bounds>();
                if (Mathf.Abs(authored.y - join.y) > .10f ||
                    !PhysicalFloor(authored, .10f, .10f, out var support) ||
                    !(support.collider is BoxCollider) || !floors[sector].Contains(support.collider) ||
                    !CiudadDelSolExteriorStairSources.HasWorldAxes(support.collider.transform) ||
                    !HasAgentClearance(support.point) || !Project(support.point, out var projected) ||
                    !SameColumn(support.point, projected) || Mathf.Abs(projected.y - support.point.y) > .10f)
                    return false;

                var routeDeck = floors[route].OfType<BoxCollider>().Where(box =>
                    CiudadDelSolExteriorStairSources.HasWorldAxes(box.transform) &&
                    Mathf.Abs(box.bounds.max.y - support.point.y) <= .02f).ToArray();
                if (!routeDeck.Any(box => ContainsXZ(box.bounds, join))) return false;
                var tiles = routeDeck.Cast<Collider>().Concat(floors[sector]).OfType<BoxCollider>()
                    .Where(box => CiudadDelSolExteriorStairSources.HasWorldAxes(box.transform) &&
                        Mathf.Abs(box.bounds.max.y - support.point.y) <= .02f).ToArray();
                var used = new HashSet<Collider>();
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(projected, join) / settings.voxelSize));
                for (int i = 0; i <= steps; i++)
                {
                    var point = Vector3.Lerp(projected, join, (float)i / steps);
                    point.y = support.point.y;
                    if (!PhysicalFloor(point, .10f, .10f, out var hit) || !tiles.Contains(hit.collider) ||
                        Mathf.Abs(hit.point.y - support.point.y) > .02f || !HasAgentClearance(hit.point))
                        return false;
                    used.Add(hit.collider);
                }
                portTiles = used.Select(collider => collider.bounds).ToArray();
                if (NavMesh.Raycast(projected, join, out _, Filter) ||
                    !BothWays(projected, join, out _, Vector3.Distance(projected, join) + .50f, portTiles)) return false;
                port = projected;
                return true;
            }

            static float TraceDistance(Vector3 sample, Vector3 start, Vector3 end)
            {
                var direction = end - start;
                float fraction = direction.sqrMagnitude > .0001f
                    ? Mathf.Clamp01(Vector3.Dot(sample - start, direction) / direction.sqrMagnitude) : 0f;
                return (sample - (start + fraction * direction)).sqrMagnitude;
            }

            bool IsSamePortDeck(Vector3 authored, Vector3 join, List<Collider> routeFloors)
            {
                if (Mathf.Abs(authored.y - join.y) > .10f ||
                    !PhysicalFloor(join, .10f, .10f, out var support) || !HasAgentClearance(support.point))
                    return false;
                foreach (var box in routeFloors.OfType<BoxCollider>())
                {
                    var bounds = box.bounds;
                    if (!CiudadDelSolExteriorStairSources.HasWorldAxes(box.transform) ||
                        Mathf.Abs(bounds.max.y - authored.y) > .10f ||
                        !ContainsXZ(bounds, authored) || !ContainsXZ(bounds, support.point)) continue;
                    // A sector tile may cover the route tile at the port, but their tops must coincide.
                    if (box.Raycast(new Ray(support.point + Vector3.up * .10f, Vector3.down), out var deck, .20f) &&
                        Mathf.Abs(deck.point.y - support.point.y) <= .02f) return true;
                }
                return false;
            }

            bool TryRouteCheckpoint(Vector3 authored, Bounds[] corridor, out RaycastHit support)
            {
                support = default;
                if (PhysicalFloor(authored, .35f, .35f, out var directHit) &&
                    corridor.Any(bounds => bounds.SqrDistance(directHit.point) <= .75f * .75f))
                {
                    support = directHit;
                    return true;
                }

                // At a modular seam, prove a full agent footprint at the same grade.
                foreach (float distance in new[] { .15f, .30f, .50f })
                for (int direction = 0; direction < 8; direction++)
                {
                    float angle = direction * Mathf.PI / 4f;
                    var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
                    if (!PhysicalFloor(authored + offset, .35f, .35f, out var hit) ||
                        Mathf.Abs(hit.point.y - authored.y) > .06f || !HasAgentClearance(hit.point) ||
                        !corridor.Any(bounds => bounds.SqrDistance(hit.point) <= .75f * .75f) ||
                        !Project(hit.point, out var projected) ||
                        new Vector2(projected.x - hit.point.x, projected.z - hit.point.z).sqrMagnitude > .04f) continue;
                    support = hit;
                    Debug.Log($"NavMesh exterior. Control sobre junta {authored:F3} comprobado en {hit.point:F3}.");
                    return true;
                }
                return false;
            }

            void ValidateGardenStairs(Bounds[] corridor, Findings finding)
            {
                // Floor heights measured on the approach, ramp collider and upper landing.
                var checkpoints = new[]
                {
                    new Vector3(-119.5f, .025f, -79f),
                    new Vector3(-119.5f, 1.601f, -76f),
                    new Vector3(-119.5f, 4f, -72f)
                };
                var projected = new Vector3[checkpoints.Length];
                for (int i = 0; i < checkpoints.Length; i++)
                {
                    if (Project(checkpoints[i], out projected[i])) continue;
                    finding.Coverage.Add($"Ascenso a Jardines: control obligatorio sin NavMesh en {checkpoints[i]:F3}.");
                    Diagnostic(NavIssueKind.Coverage, checkpoints[i], "Control obligatorio de la escalera sin NavMesh.");
                    return;
                }
                for (int i = 1; i < projected.Length; i++)
                {
                    if (!BothWays(projected[i - 1], projected[i], out string issue, corridor: corridor))
                        finding.Connectivity.Add("Ascenso a Jardines: " + issue);
                }
            }

            List<Vector3> SampleFloors(Transform group, Findings finding, out List<Vector3> physical)
            {
                DiagnosticScope(group.name, routes.Contains(group) || group == entrance ? 0 : 1);
                ShowProgress("Muestreando " + group.name, .25f);
                var points = new List<Vector3>();
                physical = new List<Vector3>();
                var seen = new HashSet<Vector3Int>();
                var groupFloors = new HashSet<Collider>(floors[group]);
                foreach (var collider in floors[group])
                {
                    var bounds = collider.bounds;
                    int xs = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / 2f));
                    int zs = Mathf.Max(1, Mathf.CeilToInt(bounds.size.z / 2f));
                    for (int x = 0; x < xs; x++)
                    for (int z = 0; z < zs; z++)
                    {
                        var origin = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, (x + .5f) / xs),
                            bounds.max.y + .15f, Mathf.Lerp(bounds.min.z, bounds.max.z, (z + .5f) / zs));
                        if (!collider.Raycast(new Ray(origin, Vector3.down), out var hit, bounds.size.y + .3f) ||
                            !WalkableSlope(hit.normal)) continue;
                        var p = hit.point;
                        if (!PhysicalFloor(p, settings.agentClimb + .06f, .10f, out var top)) continue;
                        // Keep coincident road/platform copies at joints, but do not
                        // attribute a different group's upper floor to this group.
                        if (!groupFloors.Contains(top.collider) && Mathf.Abs(top.point.y - p.y) > .10f) continue;
                        p = top.point;
                        var key = Vector3Int.RoundToInt(p * 10f);
                        if (!seen.Add(key)) { finding.Duplicate++; continue; }
                        if (!HasAgentClearance(p)) { finding.Narrow++; continue; }
                        physical.Add(p);
                        if (Project(p, out var projected))
                        {
                            points.Add(projected);
                            finding.Covered++;
                        }
                        else
                        {
                            var issue = $"{top.collider.name}: piso sin NavMesh en {p:F3}.";
                            finding.Coverage.Add(issue);
                            Diagnostic(NavIssueKind.Coverage, p, issue + " " + NearbyMonuments(p));
                        }
                    }
                }
                return points;
            }

            bool WalkableSlope(Vector3 normal) => normal.y >= Mathf.Cos(settings.agentSlope * Mathf.Deg2Rad) - .002f;

            bool PhysicalFloor(Vector3 point, float above, float below, out RaycastHit hit) =>
                scene.GetPhysicsScene().Raycast(point + Vector3.up * above, Vector3.down, out hit, above + below,
                    ~0, QueryTriggerInteraction.Ignore) && walkable.Contains(hit.collider) && WalkableSlope(hit.normal);

            bool TryPathSupport(Vector3 point, Vector3 previousSupport, Bounds[] corridor, out Vector3 support, out bool outsideCorridor)
            {
                support = default;
                outsideCorridor = false;
                // Follow the physical surface from the last supported step, not the linearly interpolated path height.
                var sample = new Vector3(point.x, previousSupport.y, point.z);
                float verticalRange = MaximumStep(previousSupport, sample);
                var physics = scene.GetPhysicsScene();
                int count = physics.Raycast(sample + Vector3.up * verticalRange, Vector3.down, columnHits,
                    2 * verticalRange, ~0, QueryTriggerInteraction.Ignore);
                if (count == columnHits.Length) return false;
                float nearest = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    var hit = columnHits[i];
                    if (!walkable.Contains(hit.collider) || !WalkableSlope(hit.normal)) continue;
                    if (physics.Raycast(hit.point + Vector3.up * .03f, Vector3.up, settings.agentHeight - .03f,
                        ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (!CanStep(previousSupport, hit.point)) continue;
                    if (!corridor.Any(bounds => bounds.SqrDistance(hit.point) <= .75f * .75f))
                    {
                        outsideCorridor = true;
                        continue;
                    }
                    float distance = Mathf.Abs(hit.point.y - previousSupport.y);
                    if (distance >= nearest) continue;
                    nearest = distance;
                    support = hit.point;
                }
                return !float.IsPositiveInfinity(nearest);
            }

            bool CanStep(Vector3 from, Vector3 to) => Mathf.Abs(to.y - from.y) <= MaximumStep(from, to);

            float MaximumStep(Vector3 from, Vector3 to)
            {
                var delta = to - from;
                float run = new Vector2(delta.x, delta.z).magnitude;
                return Mathf.Max(settings.agentClimb,
                    run * Mathf.Tan(settings.agentSlope * Mathf.Deg2Rad)) + .03f;
            }

            bool HasAgentClearance(Vector3 point)
            {
                if (gardenPond.Contains(point)) return false;
                float radius = settings.agentRadius;
                float reach = settings.agentClimb + Mathf.Tan(settings.agentSlope * Mathf.Deg2Rad) * radius;
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4f;
                    var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    if (gardenPond.Contains(point + offset) ||
                        !PhysicalFloor(point + offset, reach, reach, out var edge)) return false;
                    // Adjacent support may slope, but must not be an unrelated lower platform.
                    if (Mathf.Abs(edge.point.y - point.y) > Mathf.Max(settings.agentClimb, radius)) return false;
                }
                return scene.GetPhysicsScene().OverlapCapsule(
                    point + Vector3.up * (settings.agentClimb + radius + .02f),
                    point + Vector3.up * (settings.agentHeight - radius + .02f),
                    radius, clearance, ~0, QueryTriggerInteraction.Ignore) == 0;
            }

            void ValidatePatio(List<Vector3> gardenPoints, Findings finding)
            {
                var points = new List<Vector3>();
                foreach (var name in PatioMarkers)
                {
                    var marker = PatioSurface.transform.Find(name);
                    if (marker && Project(marker.position, out var point)) points.Add(point);
                    else
                    {
                        var issue = "Patio de Jardines Dorados: marcador sin proyección " + name;
                        finding.Coverage.Add(issue);
                        Diagnostic(NavIssueKind.Coverage, marker ? marker.position : PatioSurface.transform.position, issue);
                    }
                }
                if (points.Count > 0 && gardenPoints.Count > 0 &&
                    !BothWays(points[0], gardenPoints[0], out string connectionIssue))
                    finding.Connectivity.Add("Patio → Jardines Dorados: " + connectionIssue);
                for (int i = 0; i < points.Count; i++)
                for (int j = i + 1; j < points.Count; j++)
                {
                    if (!BothWays(points[i], points[j], out string issue)) finding.Connectivity.Add("Patio: " + issue);
                }
            }

            void ValidateTriangles(bool hasAnchor, Vector3 anchor, Findings finding)
            {
                var mesh = NavMesh.CalculateTriangulation();
                Check(mesh.indices.Length > 0, "El NavMesh registrado está vacío.");
                int outside = 0, unprojected = 0, unsupported = 0, supportedHeightSamples = 0;
                var disconnected = new List<Vector3>();
                var landing = entrance.Find("03 - Descanso y puente inferior");
                var deck = floors[entrance].OfType<BoxCollider>().Where(box => landing && box.transform.IsChildOf(landing) &&
                    Mathf.Abs(box.bounds.max.y + 2f) <= .01f &&
                    CiudadDelSolExteriorStairSources.HasWorldAxes(box.transform)).ToArray();
                for (int i = 0; i < mesh.indices.Length; i += 3)
                {
                    if (i % 300 == 0) ShowProgress("Comprobando islas y apoyo", .5f + .5f * i / mesh.indices.Length);
                    var a = mesh.vertices[mesh.indices[i]];
                    var b = mesh.vertices[mesh.indices[i + 1]];
                    var c = mesh.vertices[mesh.indices[i + 2]];
                    var point = (a + b + c) / 3;
                    Vector3 diagnosticPoint = point;
                    string geometryIssue = null;
                    NavMeshHit center = default;
                    if (!volume.Contains(point))
                    {
                        outside++;
                        geometryIssue = $"Triángulo fuera del volumen en {point:F3}.";
                    }
                    else if (!NavMesh.SamplePosition(point, out center, .35f, Filter) &&
                        !TryTriangleHeight(point, a, b, c, out center) &&
                        !TryLandingTriangle(deck, a, b, c, out center))
                    {
                        unprojected++;
                        geometryIssue = $"Centro del triángulo sin proyección a .35 m en {point:F3}.";
                    }
                    else if (gardenPond.Contains(center.position))
                    {
                        unsupported++;
                        geometryIssue = $"Triángulo navegable dentro del estanque en {center.position:F3}.";
                    }
                    else if (!PhysicalFloor(center.position, .30f, .35f, out var hit))
                    {
                        unsupported++;
                        diagnosticPoint = center.position;
                        string ground = hit.collider ? $"{hit.collider.name}, impacto {hit.point:F3}, normal Y={hit.normal.y:F3}, " +
                            $"fuente de suelo={walkable.Contains(hit.collider)}" : "sin collider en la banda vertical";
                        geometryIssue = $"Triángulo {point:F3}, NavMesh {center.position:F3}: {ground}.";
                    }
                    if (geometryIssue != null)
                    {
                        finding.Geometry.Add(geometryIssue);
                        Diagnostic(NavIssueKind.Support, diagnosticPoint, geometryIssue);
                    }
                    else
                    {
                        if (Mathf.Abs(center.position.y - point.y) > .35f) supportedHeightSamples++;
                        if (hasAnchor && !BothWays(anchor, center.position, out string issue))
                        {
                            finding.Connectivity.Add($"Isla o conexión incompleta en {point:F3}: {issue}");
                            disconnected.Add(center.position);
                        }
                    }
                }
                Debug.Log($"NavMesh exterior. {mesh.indices.Length / 3} triángulos revisados. Geometría: " +
                    $"{outside} fuera del volumen, {unprojected} sin proyección y {unsupported} sin apoyo compatible. " +
                    $"{supportedHeightSamples} centros comprobados a la altura física del piso.");
                ReportDisconnectedGroups(disconnected);
            }

            void ReportDisconnectedGroups(List<Vector3> disconnected)
            {
                var groups = new List<(Vector3 anchor, int count, Bounds bounds)>();
                foreach (var point in disconnected)
                {
                    int index = groups.FindIndex(group =>
                        (point - group.anchor).sqrMagnitude < .0001f ||
                        (NavMesh.CalculatePath(group.anchor, point, Filter, path) && path.status == NavMeshPathStatus.PathComplete &&
                         NavMesh.CalculatePath(point, group.anchor, Filter, path) && path.status == NavMeshPathStatus.PathComplete));
                    if (index < 0)
                    {
                        groups.Add((point, 1, new Bounds(point, Vector3.zero)));
                        continue;
                    }
                    var group = groups[index];
                    group.bounds.Encapsulate(point);
                    groups[index] = (group.anchor, group.count + 1, group.bounds);
                }
                foreach (var group in groups.OrderByDescending(item => item.count))
                    Debug.LogWarning($"NavMesh exterior. Grupo desconectado: {group.count} centros, " +
                        $"representante {group.anchor:F3}, extensión {group.bounds.min:F3} a {group.bounds.max:F3}. " +
                        NearbyMonuments(group.anchor));
            }

            string NearbyMonuments(Vector3 point)
            {
                var nearby = solidLandmarks.Select(source => (Collider)source.component)
                    .Where(collider => collider.bounds.SqrDistance(point) <= 16f)
                    .Select(collider => AnimationUtility.CalculateTransformPath(collider.transform, null))
                    .Distinct().ToArray();
                return nearby.Length == 0 ? "Sin fuente monumental a 4 m de la muestra." :
                    "Fuentes monumentales próximas: " + string.Join(" | ", nearby);
            }

            bool TryLandingTriangle(BoxCollider[] deck, Vector3 a, Vector3 b, Vector3 c, out NavMeshHit resolved)
            {
                resolved = default;
                var center = (a + b + c) / 3;
                foreach (var box in deck)
                {
                    var bounds = box.bounds;
                    var floor = new Vector3(center.x, bounds.max.y, center.z);
                    // Accept a single module or a fully covered coplanar footprint across a seam.
                    if (Mathf.Max(Mathf.Abs(a.y - floor.y), Mathf.Abs(b.y - floor.y), Mathf.Abs(c.y - floor.y)) > 1f ||
                        !ContainsXZ(bounds, center) ||
                        !PhysicalFloor(floor, .15f, .02f, out var support) ||
                        !deck.Contains(support.collider) || Mathf.Abs(support.point.y - floor.y) > .02f ||
                        !LandingCoversFootprint(deck, bounds, a, b, c) ||
                        !NavMesh.SamplePosition(floor, out var nav, .20f, Filter) ||
                        !SameColumn(floor, nav.position)) continue;
                    var floorA = new Vector3(a.x, floor.y, a.z);
                    var floorB = new Vector3(b.x, floor.y, b.z);
                    var floorC = new Vector3(c.x, floor.y, c.z);
                    if (!VisibleFromTriangleVertex(floorA, nav.position) ||
                        !VisibleFromTriangleVertex(floorB, nav.position) ||
                        !VisibleFromTriangleVertex(floorC, nav.position)) continue;
                    resolved = nav;
                    return true;
                }
                return false;
            }

            static bool LandingCoversFootprint(BoxCollider[] deck, Bounds module, Vector3 a, Vector3 b, Vector3 c)
            {
                if (ContainsXZ(module, a) && ContainsXZ(module, b) && ContainsXZ(module, c)) return true;
                var footprint = new Bounds(a, Vector3.zero);
                footprint.Encapsulate(b);
                footprint.Encapsulate(c);
                var tiles = deck.Select(box => box.bounds).Where(bounds =>
                    Mathf.Abs(bounds.max.y - module.max.y) <= .01f &&
                    bounds.min.x < footprint.max.x && bounds.max.x > footprint.min.x &&
                    bounds.min.z < footprint.max.z && bounds.max.z > footprint.min.z).ToArray();
                // Partition at the tile edges. Only cells that overlap the triangle
                // with positive area need support.
                var xs = LandingEdges(tiles, footprint, 0);
                var zs = LandingEdges(tiles, footprint, 2);
                if (xs.Length < 2 || zs.Length < 2) return false;
                for (int x = 1; x < xs.Length; x++)
                for (int z = 1; z < zs.Length; z++)
                {
                    var point = new Vector3((xs[x - 1] + xs[x]) * .5f, module.max.y, (zs[z - 1] + zs[z]) * .5f);
                    if (!tiles.Any(tile => ContainsXZ(tile, point)) &&
                        TriangleOverlapsCell(a, b, c, point, xs[x] - xs[x - 1], zs[z] - zs[z - 1]))
                        return false;
                }
                return true;
            }

            static bool TriangleOverlapsCell(Vector3 a, Vector3 b, Vector3 c, Vector3 center, float width, float depth)
            {
                // The grid is already clipped to the triangle's X/Z bounds.
                // The other separating axes are the normals of its three edges.
                return OverlapsEdgeAxis(a, b) && OverlapsEdgeAxis(b, c) && OverlapsEdgeAxis(c, a);

                bool OverlapsEdgeAxis(Vector3 start, Vector3 end)
                {
                    var axis = new Vector2(start.z - end.z, end.x - start.x);
                    if (axis.sqrMagnitude == 0f) return true;
                    float pa = Dot(a), pb = Dot(b), pc = Dot(c);
                    float extent = (Mathf.Abs(axis.x) * width + Mathf.Abs(axis.y) * depth) * .5f;
                    return Mathf.Min(pa, pb, pc) < extent && Mathf.Max(pa, pb, pc) > -extent;

                    float Dot(Vector3 vertex) => (vertex.x - center.x) * axis.x + (vertex.z - center.z) * axis.y;
                }
            }

            static float[] LandingEdges(Bounds[] tiles, Bounds footprint, int axis) =>
                tiles.SelectMany(tile => new[] { tile.min[axis], tile.max[axis] })
                    .Where(edge => edge > footprint.min[axis] && edge < footprint.max[axis])
                    .Append(footprint.min[axis]).Append(footprint.max[axis]).Distinct().OrderBy(edge => edge).ToArray();

            static bool ContainsXZ(Bounds bounds, Vector3 point) =>
                point.x >= bounds.min.x && point.x <= bounds.max.x && point.z >= bounds.min.z && point.z <= bounds.max.z;

            bool TryTriangleHeight(Vector3 center, Vector3 a, Vector3 b, Vector3 c, out NavMeshHit resolved)
            {
                resolved = default;
                float low = Mathf.Min(a.y, b.y, c.y);
                float high = Mathf.Max(a.y, b.y, c.y);
                if (high - low <= .35f) return false;
                // Triangulation corners can describe a sloped polygon without the detailed stair heights.
                // Probe the same X/Z only within the polygon's own vertical span.
                var origin = new Vector3(center.x, high + .05f, center.z);
                int count = scene.GetPhysicsScene().Raycast(origin, Vector3.down, columnHits,
                    high - low + .10f, ~0, QueryTriggerInteraction.Ignore);
                if (count == columnHits.Length) return false;
                bool found = false;
                for (int i = 0; i < count; i++)
                {
                    var hit = columnHits[i];
                    if (!walkable.Contains(hit.collider) || !WalkableSlope(hit.normal) ||
                        !NavMesh.SamplePosition(hit.point, out var nav, .20f, Filter) ||
                        !SameColumn(hit.point, nav.position)) continue;
                    if (!VisibleFromTriangleVertex(a, nav.position) || !VisibleFromTriangleVertex(b, nav.position) ||
                        !VisibleFromTriangleVertex(c, nav.position)) continue;
                    if (found && Mathf.Abs(resolved.position.y - nav.position.y) > .10f) return false;
                    resolved = nav;
                    found = true;
                }
                return found;
            }

            static bool SameColumn(Vector3 a, Vector3 b) =>
                new Vector2(a.x - b.x, a.z - b.z).sqrMagnitude <= .10f * .10f;

            bool VisibleFromTriangleVertex(Vector3 vertex, Vector3 center)
            {
                var inset = Vector3.Lerp(vertex, center, .06f);
                if (!NavMesh.SamplePosition(inset, out var edge, .35f, Filter) ||
                    !SameColumn(inset, edge.position) || NavMesh.Raycast(edge.position, center, out _, Filter))
                    return false;
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(edge.position, center) / .5f));
                var support = edge.position;
                var envelope = new[] { volume };
                for (int step = 0; step <= steps; step++)
                {
                    var point = Vector3.Lerp(edge.position, center, (float)step / steps);
                    if (!TryPathSupport(point, support, envelope, out var next, out _) || gardenPond.Contains(next)) return false;
                    support = next;
                }
                return Mathf.Abs(support.y - center.y) <= .10f;
            }

            static Vector3 Nearest(List<Vector3> points, IReadOnlyList<Vector3> sector)
            {
                var nearest = points[0];
                float minimum = float.PositiveInfinity;
                foreach (var point in points)
                foreach (var anchor in sector)
                {
                    float distance = (point - anchor).sqrMagnitude;
                    if (distance >= minimum) continue;
                    nearest = point;
                    minimum = distance;
                }
                return nearest;
            }

            static bool Project(Vector3 point, out Vector3 projected)
            {
                projected = default;
                if (!NavMesh.SamplePosition(point, out var hit, .65f, Filter) ||
                    Mathf.Abs(hit.position.y - point.y) > .35f) return false;
                projected = hit.position;
                return true;
            }

            bool BothWays(Vector3 from, Vector3 to, out string issue, float maximum = float.PositiveInfinity,
                Bounds[] corridor = null)
            {
                bool forward = CompletePath(from, to, maximum, corridor, out var outward);
                bool reverse = CompletePath(to, from, maximum, corridor, out var inward);
                issue = forward ? inward : reverse ? outward : outward + " / " + inward;
                return forward && reverse;
            }

            bool CompletePath(Vector3 from, Vector3 to, float maximum, Bounds[] corridor, out string issue)
            {
                issue = null;
                if ((from - to).sqrMagnitude < .0001f) return true;
                bool calculated = NavMesh.CalculatePath(from, to, Filter, path);
                var corners = path.corners;
                if (!calculated || path.status != NavMeshPathStatus.PathComplete || corners.Length < 2)
                {
                    string last = corners.Length == 0 ? "sin esquinas" : $"última esquina {corners[corners.Length - 1]:F3}";
                    issue = $"{path.status} {from:F3} → {to:F3}, {last}.";
                    Vector3? reachable = corners.Length == 0 ? (Vector3?)null : corners[corners.Length - 1];
                    Diagnostic(diagnosticIsland ? NavIssueKind.Island : NavIssueKind.Partial,
                        diagnosticIsland ? to : reachable ?? from, issue, reachable, to);
                    return false;
                }
                float length = 0;
                Vector3 previousSupport = from;
                for (int i = 1; i < corners.Length; i++)
                {
                    float segment = Vector3.Distance(corners[i - 1], corners[i]);
                    length += segment;
                    if (corridor == null) continue;
                    int steps = Mathf.Max(1, Mathf.CeilToInt(segment / .5f));
                    for (int step = 0; step <= steps; step++)
                    {
                        var point = Vector3.Lerp(corners[i - 1], corners[i], (float)step / steps);
                        if (!TryPathSupport(point, previousSupport, corridor, out var support, out bool outsideCorridor))
                        {
                            issue = outsideCorridor ? $"El apoyo físico queda fuera de la conexión en {point:F3}." :
                                $"La ruta no tiene apoyo físico continuo y compatible en {point:F3}.";
                            Diagnostic(outsideCorridor ? NavIssueKind.Corridor : NavIssueKind.Support,
                                point, issue, previousSupport, to);
                            return false;
                        }
                        previousSupport = support;
                    }
                }
                if (corridor != null && !CanStep(previousSupport, to))
                {
                    issue = $"El apoyo físico termina en otra cota: {previousSupport:F3} → {to:F3}.";
                    Diagnostic(NavIssueKind.Support, to, issue, previousSupport, to);
                    return false;
                }
                if (length <= maximum) return true;
                issue = $"Desvío de {length:F1} m entre {from:F3} y {to:F3}, por encima de {maximum:F1} m.";
                Diagnostic(NavIssueKind.Corridor, corners[corners.Length / 2], issue, destination: to);
                return false;
            }

            static void ShowProgress(string message, float progress)
            {
                if (EditorUtility.DisplayCancelableProgressBar("NavMesh exterior · Ciudad del Sol", message, progress))
                    throw new OperationCanceledException("Operación cancelada.");
            }
        }
    }
}
