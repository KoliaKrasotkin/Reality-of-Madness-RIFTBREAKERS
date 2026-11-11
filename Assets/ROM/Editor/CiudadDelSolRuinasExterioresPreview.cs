using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string RuinsMenu = IslandMenu + "Ruinas exteriores/";
        const string RuinsOutput = "Backups/CiudadDelSol/BarriosPulido";
        const string RuinsExpansion = "Ampliación de barrios · temporal";
        const string RuinsRoot = "Ruinas exteriores · temporal";

        [MenuItem(RuinsMenu + "1. Abrir propuesta temporal")]
        static void OpenExteriorRuins() => OpenRockCover(true);

        [MenuItem(RuinsMenu + "2. Siguiente sector")]
        static void NextExteriorRuins() => NextRockCover();

        [MenuItem(RuinsMenu + "3. Alternar antes y después")]
        static void ToggleExteriorRuins() => ToggleRockCover();

        [MenuItem(RuinsMenu + "4. Cerrar sin guardar")]
        static void CloseExteriorRuins() => CloseRockCover();

        delegate bool VaultSample(float x, float z, out Vector3 point, out Vector3 normal);

        sealed class RuinPiece
        {
            public string Path;
            public (Mesh mesh, Material[] materials, Matrix4x4 local)[] Parts;
            public Bounds Bounds;
            public int Triangles;
        }

        static GameObject BuildExteriorRuins(Scene scene, GameObject[] roots, List<Object> resources,
            out (string name, Vector3 eye, Vector3 target)[] views, out string report, bool exportAudit = false, bool ambiance = false)
        {
            ValidateRockCoverMount(roots.Single(r => r.name == RockCoverMount), true);
            var architecture = roots.Where(IsIslandArchitecture).ToArray();
            var roofs = architecture.Where(r => r.name.StartsWith("Subsuelo_", StringComparison.Ordinal) &&
                    r.name != "Subsuelo_Techo_Etapa01" && r.name != "Subsuelo_Acceso_Etapa01")
                .SelectMany(r => r.GetComponentsInChildren<MeshFilter>())
                .Where(f => f.sharedMesh && f.sharedMesh.isReadable && IsDecorativeRoof(f.transform))
                .ToArray();
            var roofNodes = new HashSet<GameObject>(roofs.Select(f => f.gameObject));
            var buildings = architecture.SelectMany(r => r.GetComponentsInChildren<Renderer>())
                .Where(r => r.enabled && !roofNodes.Contains(r.gameObject)).Select(r => r.bounds).ToArray();
            var routeColliders = architecture.SelectMany(r => r.GetComponentsInChildren<Collider>())
                .Where(c => c.enabled && !c.isTrigger && !roofNodes.Contains(c.gameObject)).ToArray();
            var routes = routeColliders.Select(c => c.bounds).ToArray();
            var excluded = roots.Where(r => r.name == ExteriorMount || r.name == RockCoverMount)
                .SelectMany(r => r.GetComponentsInChildren<Renderer>()).Where(r => r.enabled).Select(r => r.bounds).ToArray();
            Require(buildings.Length > 1000 && routes.Length > 1000, "Faltan las reservas de la ciudad.");
            var terrain = roots.Where(r => r.name == IslandMount || r.name == FinalSeamRoot)
                .SelectMany(r => r.GetComponentsInChildren<MeshFilter>()).ToArray();
            var support = new List<(Vector3 a, Vector3 b, Vector3 c, Bounds box, bool land)>();
            var terrainSet = new HashSet<MeshFilter>(terrain);
            foreach (var part in terrain.Concat(roofs))
            {
                var vertices = part.sharedMesh.vertices.Select(part.transform.TransformPoint).ToArray();
                var indices = part.sharedMesh.triangles;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var a = vertices[indices[i]];
                    var b = vertices[indices[i + 1]];
                    var c = vertices[indices[i + 2]];
                    var normal = Vector3.Cross(b - a, c - a).normalized;
                    var box = new Bounds(a, Vector3.zero);
                    box.Encapsulate(b);
                    box.Encapsulate(c);
                    if (normal.y >= .46f && box.max.y >= -64)
                        support.Add((a, b, c, box, terrainSet.Contains(part)));
                }
            }
            Require(support.Count > 30, "No se encontraron lomas exteriores aptas.");
            var supportIndex = IndexBounds(support.Select(f => f.box).ToArray());
            var protectedBounds = routes.Concat(buildings).ToArray();
            var protectedIndex = IndexBounds(protectedBounds);
            var routeIndex = IndexBounds(routes);
            var excludedIndex = IndexBounds(excluded);

            const string temple = "Assets/Standard Assets/Eternal Temple/Prefabs/";
            const string palm = "Assets/LeartesStudios/PirateTavern/Art/Prefabs/";
            var charcoal = new Dictionary<Material, Material>();
            var sharedStone = new Dictionary<Material, Material>();
            var rubbleStone = IslandMaterial(RockMaterial, new Color(.64f, .60f, .55f), resources);
            rubbleStone.enableInstancing = true;
            var used = new Dictionary<string, int>();
            var large = new[] { "Bases/Base_Arch_Tall_Broken_01", "Damaged/Broken_Entrance_01", "Damaged/Building_Ruins_04",
                "Damaged/Broken_Wall_06", "Damaged/Building_Ruins_08" }.Select(p => Read(temple + p + ".prefab")).ToArray();
            var secondary = new[] { "Damaged/Column_Broken_01", "Damaged/Column_Broken_03", "Damaged/Broken_Wall_03",
                "Bases/Base_Arch_Broken_02", "Staircases/Staircase_Broken_01" }.Select(p => Read(temple + p + ".prefab")).ToArray();
            var debris = new[] { "Bases/Thin/Base_Floor_Broken_03", "Bases/Thin/Base_Floor_Broken_06",
                "Railings and Decors/Railing_Broken_02", "Bases/Base_Broken_02" }.Select(p => Read(temple + p + ".prefab"))
                .Concat(new[] { Read(SunRocks + "Rock_Boulder_C.prefab") }).ToArray();
            var dry = new[] { Read(palm + "SM_PalmTrunk_C.prefab"),
                Read("Assets/Viking Village/Prefabs/Props/pf_logpile_01.prefab"),
                Read("Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Demo Elements/Prefabs/Stage/Tree-Stump.prefab") };
            var complements = new[] { Read(temple + "Railings and Decors/Bottom_Decor_Broken_01.prefab"),
                Read(temple + "Bases/Thin/Base_Wall_Broken_01.prefab"), Read(temple + "Bases/Base_Broken_Corner_01.prefab") };
            var banks = new[] { large, secondary, debris, dry, complements };
            var budgetBanks = banks.Select(b => b.OrderBy(p => p.Triangles).ToArray()).ToArray();
            Require(large.Concat(secondary).Concat(debris).Concat(dry).All(p => p.Parts.Length > 0), "Faltan mallas utilizables.");

            var root = new GameObject(RuinsRoot);
            SceneManager.MoveGameObjectToScene(root, scene);
            var random = new Random(541193);
            var positions = new List<Vector3>();
            var shots = new List<(string name, Vector3 eye, Vector3 target)>();
            var sections = new List<string>();
            var occupied = new List<Bounds>();
            var sectors = new List<(string label, Transform root, Vector3 center, Vector3 facing, int[] counts,
                List<Renderer>[] lod)>();
            var categoryCounts = new int[5];
            int[] zoneCounts = new int[3];
            var zones = new List<int>();
            var categoryCost = new int[5];
            int[] categoryBudget = { 1200000, 950000, 650000, 310000, 850000 };
            const int totalTarget = 3500;
            bool expanding = false;
            Bounds[] crystalArray = null;
            Dictionary<Vector2Int, List<int>> crystalIndex = null;
            int[] expansionCounts = new int[5], expansionCost = new int[5];
            int[] expansionBudget = { 330000, 300000, 120000, 45000, 95000 };
            var expansionSites = new List<(int index, int plot, List<Vector3> usable, int initial)>();
            var giantReserves = new List<Bounds>();
            int surveyedLots = 0, availableLots = 0, surveyPoints = 0;
            var survey = new List<string>();
            var acceptedLots = new Dictionary<(Vector2Int tile, int plot), int>();
            var visibleExpansion = new List<(string name, Vector3 center)>();
            int triangles = 0, added = 0, rejected = 0, renderers = 0;
            var limits = terrain.Select(f => f.GetComponent<Renderer>().bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            const float spacing = 6;
            var cells = new List<(Vector3 p, Vector2Int tile, int status, int reason)>();
            var green = new Dictionary<Vector2Int, List<Vector3>>();
            var tileCounts = new Dictionary<Vector2Int, int>();
            var siteTiles = new List<Vector2Int>();
            var plotPlans = new List<Dictionary<int, int[]>>();
            var crystalBoxes = new List<Bounds>();
            for (float x = Mathf.Ceil(limits.min.x / spacing) * spacing; x <= limits.max.x; x += spacing)
            for (float z = Mathf.Ceil(limits.min.z / spacing) * spacing; z <= limits.max.z; z += spacing)
            {
                var tile = Tile(x, z);
                bool found = Ground(x, z, out float floor);
                var p = new Vector3(x, found ? floor : -45, z);
                var column = new Bounds(new Vector3(x, (p.y + 54) * .5f, z), new Vector3(5, 54 - p.y, 5));
                int reason = Hits(column, routes, routeIndex) ? 1 : !found ? 2 :
                    Blocked(new Bounds(p + Vector3.up * 3, new Vector3(5, 6, 5))) ? 3 :
                    Hits(new Bounds(p + Vector3.up * 3, new Vector3(5, 6, 5)), excluded, excludedIndex) ? 4 : 0;
                int status = reason == 1 ? 1 : reason != 0 ? 2 : 0;
                if (status == 2 && !found && !limits.Contains(new Vector3(x, limits.center.y, z))) continue;
                cells.Add((p, tile, status, reason));
                if (status != 0) continue;
                if (!green.TryGetValue(tile, out var supports)) green[tile] = supports = new List<Vector3>();
                supports.Add(p);
            }
            Require(green.Count > 12, "No se identificó suficiente superficie exterior disponible.");
            if (exportAudit) WriteMap();
            foreach (var tile in green.Keys.OrderBy(t => t.x).ThenBy(t => t.y))
            {
                var samples = green[tile];
                var center = samples.Aggregate(Vector3.zero, (a, b) => a + b) / samples.Count;
                int tier = Zone(center);
                if (samples.Count < 3) continue;
                foreach (var point in samples.OrderBy(p => (p - center).sqrMagnitude))
                {
                    var facing = Vector3.ProjectOnPlane(point - limits.center, Vector3.up).normalized;
                    if (facing.sqrMagnitude < .1f) facing = Vector3.forward;
                    var site = new GameObject($"{tile.x:+00;-00;00}_{tile.y:+00;-00;00}");
                    site.transform.SetParent(root.transform, false);
                    site.transform.position = point;
                    sectors.Add((site.name, site.transform, point, facing, new int[5],
                        Enumerable.Range(0, 5).Select(_ => new List<Renderer>()).ToArray()));
                    zones.Add(tier);
                    siteTiles.Add(tile);
                    plotPlans.Add(new Dictionary<int, int[]>());
                    tileCounts[tile] = 0;
                    bool hasLandmark = false;
                    float yaw = Quaternion.LookRotation(facing).eulerAngles.y;
                    for (int i = 0; i < large.Length && !hasLandmark; i++)
                        hasLandmark = Place(large[(sectors.Count + i) % large.Length], point, 12.9f - i * 1.6f,
                            Quaternion.Euler(0, i % 2 * 90, 0), sectors.Count - 1, 0);
                    if (hasLandmark)
                    {
                        positions.Add(point);
                        break;
                    }
                    sectors.RemoveAt(sectors.Count - 1);
                    zones.RemoveAt(zones.Count - 1);
                    siteTiles.RemoveAt(siteTiles.Count - 1);
                    plotPlans.RemoveAt(plotPlans.Count - 1);
                    tileCounts.Remove(tile);
                    Object.DestroyImmediate(site);
                }
            }

            for (int cycle = 0; cycle < 220 && added < totalTarget; cycle++)
            {
                var order = Enumerable.Range(0, sectors.Count)
                    .OrderBy(i => tileCounts[siteTiles[i]] / (float)Mathf.Max(3, green[siteTiles[i]].Count) *
                        (zones[i] == 0 ? .79f : zones[i] == 1 ? 1f : 2.3f)).ToArray();
                foreach (int site in order)
                {
                    if (added >= totalTarget || zones[site] == 2 && zoneCounts[2] >= 350) continue;
                    int count = tileCounts[siteTiles[site]];
                    if (count >= Mathf.Clamp(green[siteTiles[site]].Count * 5, 14, 140)) continue;
                    int[] pattern = { 0, 1, 1, 4, 1, 2, 3, 4, 2, 1, 2, 4 };
                    int category = pattern[(cycle + site) % pattern.Length];
                    for (int attempt = 0; attempt < 48; attempt++)
                        if (Scatter(site, attempt < 32 ? category : attempt % 2 == 0 ? 2 : 4)) break;
                }
            }

            var crystalLook = BuildCrystalFields();
            Require(added == totalTarget && crystalLook.counts.SequenceEqual(new[] { 600, 300, 250, 350 }) &&
                crystalLook.fields == 34 && crystalLook.giants == 2, "La base de ruinas y cristales difiere de la previsualización.");
            int baseSites = sectors.Count;
            var baseTriangles = triangles;
            var baseRenderers = renderers;
            var baseObjects = root.GetComponentsInChildren<Transform>(true).Length;
            var expansion = new GameObject(RuinsExpansion);
            expansion.transform.SetParent(root.transform, false);
            var baseGeometry = occupied.Concat(crystalBoxes).ToArray();
            var baseIndex = IndexBounds(baseGeometry);
            crystalArray = crystalBoxes.ToArray();
            crystalIndex = IndexBounds(crystalArray);
            expanding = true;
            BuildExpansion();
            var occupiedIndex = IndexBounds(occupied.ToArray());
            var occupancy = green.ToDictionary(g => g.Key, g => Coverage(g.Value));
            if (exportAudit)
            {
                WriteDistribution();
                File.WriteAllLines(Path.Combine(RuinsOutput, "Lotes-pulido.csv"),
                    new[] { "sector_x,sector_z,lote,centro_x,centro_z,apoyos_libres,sin_apoyo,reserva_jugable,arquitectura_o_borde,decoracion_existente,piezas_nuevas" }
                    .Concat(survey.Select(line =>
                    {
                        var values = line.Split(',');
                        int x = int.Parse(values[0]), z = int.Parse(values[1]), lot = int.Parse(values[2]);
                        acceptedLots.TryGetValue((new Vector2Int(x, z), lot - 1), out int count);
                        return line + "," + count;
                    })));
            }

            foreach (var site in sectors)
            {
                var lod = site.root.gameObject.AddComponent<LODGroup>();
                var all = site.lod.SelectMany(bucket => bucket).ToArray();
                var middle = site.lod[0].Concat(site.lod[1]).Concat(site.lod[3]).ToArray();
                lod.SetLODs(new[] { new LOD(.115f, all), new LOD(.035f, middle), new LOD(.001f, site.lod[0].ToArray()) });
                lod.RecalculateBounds();
                sections.Add($"{site.label} ({site.center:F1}) · {site.counts.Sum()} · {string.Join("/", site.counts)}");
                var tangent = Vector3.Cross(Vector3.up, site.facing);
                shots.Add((site.label, site.center + site.facing * 42 + tangent * 13 + Vector3.up * 24, site.center + Vector3.up * 2));
            }
            Require(positions.Count >= 9 && zoneCounts[0] + zoneCounts[1] > zoneCounts[2] && added > 0 && root.GetComponentsInChildren<Collider>(true).Length == 0,
                $"Ruinas insuficientes: {positions.Count} sectores / {added} piezas / {string.Join("/", zoneCounts)}.");
            var buildingSite = shots[Enumerable.Range(0, sectors.Count).OrderByDescending(i => sectors[i].counts[0]).First()];
            var woodSite = sectors.Where(s => s.label != buildingSite.name)
                .OrderByDescending(s => s.counts[3]).First();
            var woodBounds = woodSite.lod[3].Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            var woodEye = woodBounds.center + woodSite.facing * 24 + Vector3.up * 12;
            var monumentSite = shots[Enumerable.Range(0, sectors.Count).Where(i => shots[i].name != buildingSite.name)
                .OrderByDescending(i => sectors[i].counts[0] + sectors[i].counts[1]).First()];
            var groundView = FindGroundView();
            var upper = expansionSites.OrderByDescending(e => e.usable.Average(p => p.y)).FirstOrDefault();
            var interior = expansionSites.OrderByDescending(e => sectors[e.index].counts.Sum()).FirstOrDefault();
            var upperShot = expansionSites.Count == 0 ? buildingSite : shots[upper.index];
            var interiorShot = expansionSites.Count == 0 ? monumentSite : shots[interior.index];
            var field = crystalLook.hero;
            var fieldDirection = Vector3.ProjectOnPlane(field - limits.center, Vector3.up).normalized;
            if (fieldDirection.sqrMagnitude < .1f) fieldDirection = Vector3.forward;
            views = new[] { ("00-Panoramica-aerea", new Vector3(29, 506, -54), new Vector3(29, -4, 12)),
                ("01-Edificios-derrumbados", buildingSite.eye, buildingSite.target),
                ("02-Arboles-quemados", woodEye, woodBounds.center),
                ("03-Ruinas-monumentales", monumentSite.eye, monumentSite.target), groundView,
                ("05-Campo-violeta", field + fieldDirection * 30 + Vector3.up * 18, field + Vector3.up * 4),
                ("06-Gigantes-y-flotantes", field + fieldDirection * 46 + Vector3.up * 11, field + Vector3.up * 10),
                ("07-Barrios-y-cristales", new Vector3(236, 280, -305), new Vector3(23, -12, 0)),
                ("08-Explanada-superior", upperShot.eye, upperShot.target),
                ("09-Barrio-nuevo", interiorShot.eye, interiorShot.target) }
                .Concat(visibleExpansion.Select(v =>
                {
                    var outward = Vector3.ProjectOnPlane(v.center - limits.center, Vector3.up).normalized;
                    return (v.name, v.center + outward * 38 + Vector3.up * 27, v.center + Vector3.up * 2);
                }))
                .Concat(shots).ToArray();
            report = $"{siteTiles.Distinct().Count()} sectores / {added} piezas / {renderers} renderizadores / {triangles} triángulos; 0 colliders.\n" +
                $"Ampliación real: {added - totalTarget} piezas; {expansionSites.Count} lotes en {expansionSites.Select(e => siteTiles[e.index]).Distinct().Count()} sectores.\n" +
                $"Sondeo de ampliación a 2 m: {surveyedLots} lotes, {availableLots} con claros; {surveyPoints} apoyos libres muestreados.\n" +
                $"Nuevas por categoría: {string.Join("/", expansionCounts)}; presupuesto incremental por categoría: {string.Join("/", expansionCost)} triángulos.\n" +
                $"Incremento frente a la base: +{triangles - baseTriangles} triángulos, +{renderers - baseRenderers} renderizadores, " +
                $"+{root.GetComponentsInChildren<Transform>(true).Length - baseObjects} objetos, +{sectors.Count - baseSites} grupos LOD.\n" +
                $"Monumentales/arquitectónicas/escombros/madera/complementos: {string.Join("/", categoryCounts)}.\n" +
                $"Interior/intermedio/borde: {string.Join("/", zoneCounts)}. Cubiertas decorativas inspeccionadas: {roofs.Length}.\n" +
                $"Triángulos por categoría: {string.Join("/", categoryCost)}.\n" +
                $"Materiales compartidos utilizados: {root.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Distinct().Count()}; " +
                $"{sectors.Count + crystalLook.fields} grupos LOD totales. Triángulos a LOD cercano; sin medición de GPU.\n" +
                $"Manzanas con al menos dos construcciones reconocibles: {plotPlans.Count(p => p.Values.Count(v => v[0] > 0 && v[1] >= 2) >= 2)}; " +
                $"lotes con hito y al menos dos piezas arquitectónicas: {plotPlans.Sum(p => p.Values.Count(v => v[0] > 0 && v[1] >= 2))}.\n" +
                $"Cristales violeta/ámbar/obsidiana/flotantes: {string.Join("/", crystalLook.counts)}; " +
                $"{crystalLook.fields} campos; {crystalLook.giants} gigantes; {crystalLook.renderers} renderizadores; {crystalLook.triangles} triángulos; 3 materiales; 0 luces añadidas.\n" +
                $"Totales temporales: {root.GetComponentsInChildren<Transform>(true).Length} objetos, {renderers + crystalLook.renderers} renderizadores y " +
                $"{triangles + crystalLook.triangles} triángulos al LOD cercano.\n" +
                $"{green.Sum(g => g.Value.Count)} celdas verdes; {rejected} colocaciones descartadas.\n" +
                $"Concentración: {occupancy.Count(t => t.Value >= 80 && green[t.Key].Count >= 10)} sectores con al menos 80% de huella muestreada ocupada; " +
                $"sectores con al menos 360 m² útiles y menos de 8 piezas: {green.Count(g => g.Value.Count >= 10 && (!tileCounts.TryGetValue(g.Key, out var n) || n < 8))}.\n" +
                $"Reservas: {routes.Length} colliders sólidos, {buildings.Length} renderizadores de ciudad y {excluded.Length} de exterior integrado.\n" +
                "Soporte por intersección con triángulos de la masa y la unión; se excluyen las superficies de los sectores jugables.\n" +
                string.Join("\n", sections) + "\n\nRecursos visuales:\n" +
                string.Join("\n", used.OrderBy(v => v.Key).Select(v => $"{v.Value} × {v.Key}"));
            if (ambiance)
            {
                var decorBounds = occupied.Concat(crystalBoxes).ToArray();
                var decorIndex = IndexBounds(decorBounds);
                var additions = BuildAmbientLayers(scene, resources, routeColliders, green.Values.SelectMany(points => points).ToArray(),
                    (x, z) => Ground(x, z, out float height) ? (float?)height : null,
                    box => Blocked(box) || Hits(box, excluded, excludedIndex) || Hits(box, decorBounds, decorIndex),
                    box => Hits(box, protectedBounds, protectedIndex) || Hits(box, excluded, excludedIndex) ||
                        Hits(box, decorBounds, decorIndex));
                var surfaceLayer = new GameObject(SurfaceCorrectionRoot);
                surfaceLayer.transform.SetParent(root.transform, false);
                BuildSurfaceCorrections(surfaceLayer, out var comparisons, out var surfaceReport);
                views = comparisons;
                report += "\n\n" + surfaceReport;
                report += "\n\n" + additions.report;
            }

            return root;

                        void BuildSurfaceCorrections(GameObject layer, out (string name, Vector3 eye, Vector3 target)[] comparisons, out string details)
            {
                var rng = new Random(700184);
                var meshBuckets = new Dictionary<(int x, int z, int material), List<Vector3>>();
                var placements = new List<Bounds>();
                var decorated = new List<Bounds>();
                var flats = new List<Vector3>();
                var groundSites = new HashSet<Vector3>();
                var existing = occupied.Concat(crystalBoxes).ToArray();
                var existingIndex = IndexBounds(existing);
                var tones = new[] { new Color(.29f, .32f, .15f), new Color(.39f, .38f, .22f),
                    new Color(.42f, .16f, .64f), new Color(.21f, .14f, .26f), new Color(.75f, .49f, .19f) };
                var materials = tones.Select((tint, i) =>
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Superficie recuperada " + i };
                    mat.SetColor("_BaseColor", tint);
                    mat.SetFloat("_Smoothness", i < 2 ? .06f : .46f);
                    if (i == 2 || i == 4)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", tint * (i == 2 ? 1.8f : .6f));
                    }
                    resources.Add(mat);
                    return mat;
                }).ToArray();
                int shards = 0, ruinsAdded = 0, moss = 0, roofSamples = 0;
                var longitudinal = roofs.Where(f => f.name == "Boveda longitudinal" &&
                    f.TryGetComponent<MeshRenderer>(out var r) && r.enabled).ToArray();
                var roofSites = new List<(Bounds bounds, List<(Vector3 point, Vector3 normal)> points, VaultSample sample)>();
                foreach (var roof in longitudinal)
                {
                    var box = roof.GetComponent<MeshRenderer>().bounds;
                    var mesh = roof.sharedMesh;
                    var verts = mesh.vertices.Select(roof.transform.TransformPoint).ToArray();
                    var ix = mesh.triangles;
                    int before = moss;
                    var samples = new List<(Vector3 point, Vector3 normal)>();
                    roofSites.Add((box, samples, RoofPoint));
                    // Sample the actual roof skin rather than fitting a flat lot to its footprint.
                    for (int patch = 0; patch < Mathf.Clamp((int)(box.size.x * box.size.z / 26), 8, 78); patch++)
                    {
                        float x = Mix(box.min.x + .7f, box.max.x - .7f);
                        float z = Mix(box.min.z + .7f, box.max.z - .7f);
                        if (!RoofPoint(x, z, out var p, out var n)) continue;
                        roofSamples++;
                        samples.Add((p, n));
                        if (!Free(new Bounds(p + Vector3.up * .45f, new Vector3(2.4f, .85f, 2.4f)))) continue;
                        float radius = Mix(.9f, 2.25f);
                        for (int j = 0; j < 13; j++)
                        {
                            float angle = j * 2.399963f;
                            float offset = radius * Mathf.Sqrt(j / 13f);
                            float u = x + Mathf.Cos(angle) * offset, v = z + Mathf.Sin(angle) * offset;
                            float size = Mix(.3f, .83f);
                            if (!RoofPoint(u, v, out var a, out var na) ||
                                !RoofPoint(u + size, v - size * .3f, out var b, out var nb) ||
                                !RoofPoint(u - size * .4f, v + size, out var c, out var nc)) continue;
                            var turf = new Bounds(a, Vector3.zero);
                            turf.Encapsulate(b);
                            turf.Encapsulate(c);
                            turf.center += Vector3.up * .12f;
                            if (!Free(turf)) continue;
                            Bucket(p, patch % 4 == 0 ? 1 : 0).AddRange(new[] { a + na * .075f, c + nc * .075f, b + nb * .075f });
                            moss++;
                        }
                        if (patch % 5 == 0 && n.y >= .74f)
                        {
                            Crystal(p + n * .05f, n, Mix(.45f, 1.65f), 2 + patch % 3);
                            if (patch % 10 == 0)
                                Crystal(p + Vector3.up * 1.9f, Vector3.up, Mix(.45f, .9f), 2);
                        }
                        if (patch % 7 == 0 && n.y >= .92f)
                            Rubble(debris[patch % debris.Length], p, Mix(.45f, 1.2f), true);
                    }
                    if (moss > before) decorated.Add(box);

                    bool RoofPoint(float x, float z, out Vector3 point, out Vector3 normal)
                    {
                        point = new Vector3(x, float.NegativeInfinity, z);
                        normal = Vector3.zero;
                        for (int i = 0; i < ix.Length; i += 3)
                        {
                            var a = verts[ix[i]];
                            var b = verts[ix[i + 1]] - a;
                            var c = verts[ix[i + 2]] - a;
                            float det = b.x * c.z - b.z * c.x;
                            if (Mathf.Abs(det) < .0001f) continue;
                            float u = ((x - a.x) * c.z - (z - a.z) * c.x) / det;
                            float v = (b.x * (z - a.z) - b.z * (x - a.x)) / det;
                            if (u < 0 || v < 0 || u + v > 1) continue;
                            var n = Vector3.Cross(b, c).normalized;
                            if (n.y < .51f) continue;
                            float y = a.y + u * b.y + v * c.y;
                            if (y <= point.y) continue;
                            point.y = y;
                            normal = n;
                        }
                        return normal != Vector3.zero;
                    }
                }

                // Independent survey of flat ground, including sites outside the old lot grid.
                for (float x = -166; x <= 205; x += 5)
                for (float z = -173; z <= 167; z += 5)
                {
                    float sx = x + Mix(-1.8f, 1.8f), sz = z + Mix(-1.8f, 1.8f);
                    bool terrainSupport = Ground(sx, sz, out float y, true) && y >= -35 && y <= 3 &&
                        (!Ground(sx, sz, out float topSurface) || topSurface <= y + 2);
                    if (!terrainSupport && (!Ground(sx, sz, out y) || y < -13 || y > 3)) continue;
                    var p = new Vector3(sx, y, sz);
                    if (longitudinal.Any(r => r.GetComponent<MeshRenderer>().bounds.SqrDistance(p) < 1.2f)) continue;
                    if (!Level(p, 2.1f, .37f)) continue;
                    var box = new Bounds(p + Vector3.up * 2, new Vector3(6, 3.9f, 6));
                    if (!Free(box) || ProtectedVertical(box, y)) continue;
                    flats.Add(p);
                    if (terrainSupport) groundSites.Add(p);
                }
                var hubs = new List<Vector3>();
                foreach (var hub in flats.OrderByDescending(groundSites.Contains).ThenByDescending(p => flats.Count(q => FlatDistance(p, q) < 14)))
                {
                    if (hubs.Any(p => FlatDistance(p, hub) < 23)) continue;
                    if (!Rubble(large.Where(p => p.Parts.Length > 0).OrderBy(p => p.Triangles).First(), hub, 5.3f, false)) continue;
                    hubs.Add(hub);
                    foreach (var p in flats.Where(p => FlatDistance(p, hub) >= 4 && FlatDistance(p, hub) < 11).Take(12))
                    {
                        int idx = rng.Next(secondary.Length);
                        Rubble(secondary[idx], p, Mix(1.7f, 3.6f), false);
                        var edge = p + new Vector3(Mix(-1.5f, 1.5f), 0, Mix(-1.5f, 1.5f));
                        if (Ground(edge.x, edge.z, out var y) && Mathf.Abs(y - p.y) < .3f)
                        {
                            edge.y = y;
                            Crystal(edge, Vector3.up, Mix(.7f, 2.1f), 2 + rng.Next(3));
                        }
                    }
                    if (hubs.Count == 24) break;
                }

                RenderBuckets(layer.transform, materials);
                Require(decorated.Count >= 7 && hubs.Count > 0 && layer.GetComponentsInChildren<Collider>().Length == 0,
                    $"Corrección incompleta: {decorated.Count} cubiertas y {hubs.Count} explanadas.");
                var viewsByRoof = decorated.OrderBy(b => b.center.z).ToArray();
                var broad = viewsByRoof.OrderByDescending(b => decorated.Count(c => (c.center - b.center).sqrMagnitude < 38 * 38)).First();
                var second = viewsByRoof.OrderByDescending(b => (b.center - broad.center).sqrMagnitude).First();
                var longRoof = viewsByRoof.OrderByDescending(b => Mathf.Max(b.size.x, b.size.z)).First();
                var side = viewsByRoof.OrderBy(b => FlatDistance(b.center, new Vector3(135, 0, 90))).First();
                var top = viewsByRoof.OrderBy(b => FlatDistance(b.center, new Vector3(-48, 0, -90))).First();
                var court = hubs.OrderByDescending(groundSites.Contains).ThenBy(p => p.y).ThenBy(p => FlatDistance(p, new Vector3(20, 0, -120))).First();
                comparisons = new[]
                {
                    ("01-Bovedas-paralelas", broad.center + new Vector3(-21, 24, -26), broad.center),
                    ("02-Bovedas-bajo-plataformas", second.center + new Vector3(18, 15, -24), second.center),
                    ("03-Boveda-longitudinal", longRoof.center + new Vector3(8, 34, -20), longRoof.center),
                    ("04-Explanada-entre-muros", court + new Vector3(-14, 13, -13), court),
                    ("05-Laterales-de-plataformas", side.center + new Vector3(23, 28, -24), side.center),
                    ("06-Corredor-de-cubiertas", top.center + new Vector3(-7, 40, -4), top.center),
                    ("07-Corredor-cenital", top.center + new Vector3(-4, 49, -2), top.center)
                };
                var addedRenderers = layer.GetComponentsInChildren<MeshRenderer>();
                int trianglesAdded = layer.GetComponentsInChildren<MeshFilter>().Sum(f =>
                    Enumerable.Range(0, f.sharedMesh.subMeshCount).Sum(j => (int)f.sharedMesh.GetIndexCount(j) / 3));
                details = $"{decorated.Count}/{longitudinal.Length} bóvedas decoradas, {roofSamples} apoyos de cubierta medidos; " +
                    $"{hubs.Count} conjuntos planos ({hubs.Count(groundSites.Contains)} sobre tierra) / {flats.Count} puntos aptos; {ruinsAdded} ruinas, {shards} cristales y {moss} manchas elementales añadidas. " +
                    $"{trianglesAdded} triángulos, {addedRenderers.Length} renderizadores, 0 colliders.\n" +
                    string.Join("\n", comparisons.Select(v => $"{v.name} mira a {v.target:F2}")) + "\nCubiertas sin colocación segura: " +
                    string.Join("; ", longitudinal.Select(f => f.GetComponent<MeshRenderer>().bounds)
                        .Where(b => !decorated.Contains(b)).Select(b => b.center.ToString("F2")));

                // Keep the previous pass intact so both states share the same composition and cameras.
                int baselineShards = shards, baselineRuins = ruinsAdded;
                var denseLayer = new GameObject(VaultDensityRoot);
                denseLayer.transform.SetParent(layer.transform, false);
                var output = denseLayer.transform;
                meshBuckets.Clear();
                var violet = new Material(materials[2]) { name = "Violeta HDR · vetas de bóveda" };
                violet.SetColor("_BaseColor", new Color(.26f, .045f, .55f));
                violet.SetColor("_EmissionColor", new Color(.48f, .11f, 1f) * 2.45f);
                resources.Add(violet);
                var denseMaterials = (Material[])materials.Clone();
                denseMaterials[2] = violet;

                int floating = 0, largeCrystals = 0, supportedRoofs = 0, airOnlyRoofs = 0;
                int flatCrystals = 0;
                var stillClear = new List<Vector3>();
                var pendingRoofResults = new List<string>();
                var roofRubble = secondary.Concat(debris).Where(piece => piece.Parts.Length > 0 && piece.Triangles <= 2500)
                    .OrderBy(piece => piece.Path, StringComparer.Ordinal).ToArray();
                Require(roofRubble.Length > 0, "Faltan recursos de ruinas ligeras.");
                foreach (var roof in roofSites)
                {
                    int start = shards;
                    int startFloating = floating;
                    int startRuins = ruinsAdded;
                    bool previouslyDecorated = decorated.Contains(roof.bounds);
                    float profile = Mathf.PerlinNoise(roof.bounds.center.x * .031f + 10, roof.bounds.center.z * .031f + 10);
                    bool featured = new[] { broad.center, top.center, side.center }.Any(p => FlatDistance(p, roof.bounds.center) < 33);
                    int budget = previouslyDecorated ? Mathf.RoundToInt(Mathf.Lerp(7, 18, profile)) + (featured ? 3 : 0) : 4;
                    bool alongX = roof.bounds.size.x > roof.bounds.size.z;
                    var anchors = new List<Vector3>();
                    var ordered = roof.points.OrderByDescending(sample => sample.normal.y + Mix(-.22f, .22f)).ToArray();
                    foreach (var sample in ordered)
                    {
                        if (shards - start >= budget || shards - baselineShards >= 515) break;
                        if (anchors.Any(p => Mathf.Abs(alongX ? p.x - sample.point.x : p.z - sample.point.z) < 4.4f)) continue;
                        if (previouslyDecorated && sample.normal.y > .72f)
                        {
                            float height = sample.normal.y > .84f && anchors.Count == 0 && (featured || profile > .47f)
                                ? Mix(3.3f, 4.6f) : Mix(1.45f, 2.75f);
                            int tint = profile < .30f && anchors.Count % 2 == 0 ? 3 : 2;
                            if (AddSupported(sample.point, sample.normal, height, tint))
                            {
                                anchors.Add(sample.point);
                                int satellites = profile < .40f ? 3 : 5;
                                for (int j = 0; j < satellites && shards - start < budget; j++)
                                {
                                    float angle = Mix(0, Mathf.PI * 2);
                                    float radius = Mix(.65f, 2.35f);
                                    var offset = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                                    if (!roof.sample(sample.point.x + offset.x, sample.point.z + offset.z, out var p, out var n) || n.y < .57f) continue;
                                    AddSupported(p, n, Mix(.55f, 1.5f), j == 0 ? 3 : 2);
                                }
                                if ((anchors.Count + Mathf.FloorToInt(profile * 5)) % 3 != 0)
                                    AddFloating(sample.point, Mix(.72f, 1.5f));
                                continue;
                            }
                        }
                        if (anchors.Count < (previouslyDecorated ? 3 : 2) && AddFloating(sample.point, Mix(.85f, 1.7f)))
                            anchors.Add(sample.point);
                    }
                    if (previouslyDecorated)
                    {
                        foreach (var sample in ordered)
                        {
                            if (ruinsAdded - startRuins >= (profile < .55f ? 4 : 2)) break;
                            if (sample.normal.y < .86f || anchors.Any(p => FlatDistance(p, sample.point) < 1.4f)) continue;
                            var piece = roofRubble[rng.Next(roofRubble.Length)];
                            Rubble(piece, sample.point, Mix(1.3f, 3.8f), true, roof.sample, output);
                        }
                    }
                    if (!previouslyDecorated)
                        pendingRoofResults.Add($"{roof.bounds.center:F2}: {floating - startFloating} flotantes seguros");
                    if (shards - start > floating - startFloating || ruinsAdded > startRuins) supportedRoofs++;
                    else if (floating > startFloating) airOnlyRoofs++;
                    else stillClear.Add(roof.bounds.center);

                    bool AddSupported(Vector3 point, Vector3 normal, float height, int tint)
                    {
                        if (Ground(point.x, point.z, out var highest) && highest > point.y + .3f) return false;
                        float footprint = height * .24f;
                        foreach (var direction in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                        {
                            var edge = point + direction * footprint;
                            if (!roof.sample(edge.x, edge.z, out var surface, out _) ||
                                Mathf.Abs(Vector3.Dot(surface - point, normal)) > .19f) return false;
                        }
                        if (!Crystal(point + normal * .055f, normal, height, tint, true)) return false;
                        if (height >= 2.6f) largeCrystals++;
                        return true;
                    }
                }
                foreach (var hub in hubs)
                {
                    foreach (var site in flats.Where(p => FlatDistance(p, hub) < 10).Take(7))
                    {
                        if (shards - baselineShards >= 580) break;
                        float x = site.x + Mix(-2.3f, 2.3f), z = site.z + Mix(-2.3f, 2.3f);
                        if (!Ground(x, z, out float y) || Mathf.Abs(y - site.y) > .36f) continue;
                        var p = new Vector3(x, y, z);
                        if (!Level(p, .42f, .21f) || ProtectedVertical(new Bounds(p + Vector3.up, Vector3.one * 2), y)) continue;
                        if (Crystal(p + Vector3.up * .04f, Vector3.up, Mix(.85f, 2.4f), rng.NextDouble() < .22 ? 3 : 2, true))
                            flatCrystals++;
                    }
                }
                RenderBuckets(output, denseMaterials);
                var densityMeshes = denseLayer.GetComponentsInChildren<MeshFilter>();
                int densityTriangles = densityMeshes.Sum(f => Enumerable.Range(0, f.sharedMesh.subMeshCount)
                    .Sum(j => (int)f.sharedMesh.GetIndexCount(j) / 3));
                Require(denseLayer.GetComponentsInChildren<Collider>().Length == 0 && shards > baselineShards,
                    "La densificación no produjo una capa visual válida.");
                details += $"\nDensificación: {shards - baselineShards} cristales ({floating} flotantes, {largeCrystals} grandes, " +
                    $"{flatCrystals} en explanadas), {ruinsAdded - baselineRuins} ruinas; {supportedRoofs} bóvedas con apoyo, " +
                    $"{airOnlyRoofs} solo con flotantes. {densityTriangles} triángulos y " +
                    $"{denseLayer.GetComponentsInChildren<MeshRenderer>().Length} renderizadores adicionales, 0 colliders.\n" +
                    "Bóvedas sin adiciones seguras: " + string.Join("; ", stillClear.Select(p => p.ToString("F2"))) +
                    "\nRevisión de las tres pendientes: " + string.Join("; ", pendingRoofResults);

                var vaultEye = top.center + new Vector3(-22, 8, -24);
                comparisons = comparisons.Concat(new[] { ("08-Boveda-lateral-baja", vaultEye, top.center + Vector3.up * 2) }).ToArray();

                bool AddFloating(Vector3 point, float size)
                {
                    var start = point + Vector3.up * Mix(1.7f, 3.35f);
                    if (Ground(start.x, start.z, out var highest) && highest + 1f > start.y) return false;
                    var envelope = new Bounds(start + Vector3.up * size * .55f, new Vector3(size + 1, size + .8f, size + 1));
                    if (!Free(envelope) || ProtectedVertical(envelope, start.y)) return false;
                    if (!Crystal(start, Vector3.up, size, 2, true)) return false;
                    floating++;
                    return true;
                }


                void RenderBuckets(Transform parent, Material[] palette)
                {
                    foreach (var item in meshBuckets)
                    {
                        var obj = new GameObject($"Agrupación {item.Key.x}/{item.Key.z}/{item.Key.material}");
                        obj.transform.SetParent(parent, false);
                        var mesh = new Mesh { name = obj.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                        mesh.SetVertices(item.Value);
                        mesh.SetTriangles(Enumerable.Range(0, item.Value.Count).ToArray(), 0);
                        mesh.RecalculateNormals();
                        resources.Add(mesh);
                        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var renderer = obj.AddComponent<MeshRenderer>();
                        renderer.sharedMaterial = palette[item.Key.material];
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                    }
                }

                float Mix(float min, float max) => Mathf.Lerp(min, max, (float)rng.NextDouble());
                bool Free(Bounds box) => !Hits(box, protectedBounds, protectedIndex) && !Hits(box, excluded, excludedIndex) &&
                    !Hits(box, existing, existingIndex) && !placements.Any(b => b.Intersects(box));
                bool Level(Vector3 point, float radius, float tolerance)
                {
                    foreach (var axis in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                    {
                        var edge = point + axis * radius;
                        if (!Ground(edge.x, edge.z, out var y) || Mathf.Abs(y - point.y) > tolerance) return false;
                    }
                    return true;
                }

                List<Vector3> Bucket(Vector3 point, int material)
                {
                    var key = (Mathf.FloorToInt(point.x / 48), Mathf.FloorToInt(point.z / 48), material);
                    if (!meshBuckets.TryGetValue(key, out var vertices)) meshBuckets[key] = vertices = new List<Vector3>();
                    return vertices;
                }

                bool Crystal(Vector3 point, Vector3 axis, float height, int material, bool tight = false)
                {
                    var box = new Bounds(point + axis * height * .55f, Vector3.one * (height * 1.12f));
                    box.min = Vector3.Max(box.min, new Vector3(box.min.x, point.y + .03f, box.min.z));
                    if (!tight && !Free(box)) return false;
                    var vertices = new List<Vector3>(30);
                    var rotation = Quaternion.FromToRotation(Vector3.up, axis) * Quaternion.Euler(0, Mix(0, 360), 0);
                    var tip = point + axis * height;
                    float width = height * (tight ? .26f : .15f);
                    float shoulder = height * (tight ? .49f : .22f);
                    for (int i = 0; i < 5; i++)
                    {
                        var a = point + rotation * (Quaternion.Euler(0, i * 72, 0) * new Vector3(width, shoulder, 0));
                        var b = point + rotation * (Quaternion.Euler(0, (i + 1) * 72, 0) * new Vector3(width, shoulder, 0));
                        vertices.AddRange(new[] { a, tip, b, a, b, point });
                    }
                    if (tight)
                    {
                        box = new Bounds(vertices[0], Vector3.zero);
                        foreach (var vertex in vertices) box.Encapsulate(vertex);
                        box.Expand(.12f);
                        if (!Free(box)) return false;
                    }
                    Bucket(point, material).AddRange(vertices);
                    placements.Add(box);
                    shards++;
                    return true;
                }

                bool Rubble(RuinPiece piece, Vector3 point, float size, bool roof, VaultSample sample = null, Transform parentLayer = null)
                {
                    if (piece.Parts.Length == 0 || piece.Triangles > 16000) return false;
                    float scale = size / Mathf.Max(piece.Bounds.size.x, piece.Bounds.size.y, piece.Bounds.size.z);
                    var rotation = Quaternion.Euler(0, Mix(0, 360), 0);
                    var bounds = TransformBounds(piece.Bounds, Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one * scale));
                    if (!roof && !Level(point, Mathf.Max(bounds.extents.x, bounds.extents.z), .30f)) return false;
                    if (sample != null)
                    {
                        float highest = point.y, lowest = point.y;
                        for (int x = -1; x <= 1; x += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            float px = point.x + x * bounds.extents.x * .82f;
                            float pz = point.z + z * bounds.extents.z * .82f;
                            if (!sample(px, pz, out var contact, out var normal) || normal.y < .77f) return false;
                            highest = Mathf.Max(highest, contact.y);
                            lowest = Mathf.Min(lowest, contact.y);
                        }
                        if (highest - lowest > .24f || Ground(point.x, point.z, out var top) && top > highest + .25f) return false;
                        point.y = highest - .035f;
                    }
                    var position = point - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    bounds.center += position;
                    var clearance = bounds;
                    clearance.min += Vector3.up * .045f;
                    if (!Free(clearance) || !roof && ProtectedVertical(clearance, point.y)) return false;
                    placements.Add(clearance);
                    var parent = new GameObject(Path.GetFileNameWithoutExtension(piece.Path));
                    parent.transform.SetParent(parentLayer ? parentLayer : layer.transform, false);
                    parent.transform.SetPositionAndRotation(position, rotation);
                    parent.transform.localScale = Vector3.one * scale;
                    foreach (var part in piece.Parts)
                    {
                        var obj = new GameObject(part.mesh.name);
                        obj.transform.SetParent(parent.transform, false);
                        obj.transform.localPosition = part.local.GetColumn(3);
                        obj.transform.localRotation = part.local.rotation;
                        obj.transform.localScale = part.local.lossyScale;
                        obj.AddComponent<MeshFilter>().sharedMesh = part.mesh;
                        var renderer = obj.AddComponent<MeshRenderer>();
                        renderer.sharedMaterials = part.materials.Select(Instanced).ToArray();
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                    }
                    ruinsAdded++;
                    return true;
                }
            }


            float Next() => (float)random.NextDouble();
            void BuildExpansion()
            {
                var expansionLandmarks = large.Concat(new[] {
                    Read(temple + "Damaged/Building_Ruins_02.prefab"),
                    Read(temple + "Damaged/Building_Ruins_06.prefab")
                }).OrderBy(p => p.Triangles).ToArray();
                var facades = Enumerable.Range(0, 3).Select(BuildRuinFrontage).ToArray();
                var baseline = Enumerable.Range(0, baseSites).ToDictionary(i => siteTiles[i], i => plotPlans[i]);
                var candidates = new List<(Vector2Int tile, int plot, List<Vector3> usable, float score)>();
                var fullSurvey = cells.Select(c => c.tile).Distinct().OrderBy(t => t.x).ThenBy(t => t.y);
                foreach (var tile in fullSurvey)
                {
                    for (int plot = 0; plot < 4; plot++)
                    {
                        var pad = Lot(tile, plot);
                        if (Zone(pad) == 2) continue;
                        surveyedLots++;
                        var free = new List<Vector3>();
                        int supportless = 0, route = 0, structure = 0, occupiedSamples = 0;
                        for (float dx = -8f; dx <= 8f; dx += 2f)
                        for (float dz = -8f; dz <= 8f; dz += 2f)
                        {
                            float x = pad.x + dx, z = pad.z + dz;
                            if (!Ground(x, z, out float floor))
                            {
                                supportless++;
                                continue;
                            }
                            var point = new Vector3(x, floor, z);
                            var box = new Bounds(point + Vector3.up * 1.35f, new Vector3(2.3f, 2.8f, 2.3f));
                            if (ProtectedVertical(box, floor))
                            {
                                route++;
                                continue;
                            }
                            if (!InsideLot(tile, box, 2.8f) || Blocked(box) || Hits(box, excluded, excludedIndex))
                            {
                                structure++;
                                continue;
                            }
                            if (Hits(box, baseGeometry, baseIndex) || giantReserves.Any(b => b.Intersects(box)))
                            {
                                occupiedSamples++;
                                continue;
                            }
                            free.Add(point);
                        }
                        survey.Add(FormattableString.Invariant($"{tile.x},{tile.y},{plot + 1},{pad.x},{pad.z},{free.Count},{supportless},{route},{structure},{occupiedSamples}"));
                        if (free.Count < 4) continue;
                        availableLots++;
                        surveyPoints += free.Count;
                        int originalCount = baseline.TryGetValue(tile, out var oldPlots) && oldPlots.TryGetValue(plot, out var plan)
                            ? plan.Sum() : 0;
                        float score = free.Count * 4 + free.Average(p => p.y) * .19f - originalCount * 2.1f;
                        candidates.Add((tile, plot, free, score));
                    }
                }
                foreach (var candidate in candidates.OrderByDescending(c => c.score))
                {
                    var center = candidate.usable.Aggregate(Vector3.zero, (a, b) => a + b) / candidate.usable.Count;
                    var group = new GameObject($"Nuevo {candidate.tile.x:+00;-00;00}_{candidate.tile.y:+00;-00;00} · lote {candidate.plot + 1}");
                    group.transform.SetParent(expansion.transform, false);
                    group.transform.position = center;
                    var facing = Vector3.ProjectOnPlane(center - limits.center, Vector3.up).normalized;
                    if (facing.sqrMagnitude < .1f) facing = Vector3.forward;
                    int index = sectors.Count;
                    sectors.Add((group.name, group.transform, center, facing, new int[5],
                        Enumerable.Range(0, 5).Select(_ => new List<Renderer>()).ToArray()));
                    zones.Add(Zone(candidate.usable[0]));
                    siteTiles.Add(candidate.tile);
                    plotPlans.Add(new Dictionary<int, int[]>());
                    if (!tileCounts.ContainsKey(candidate.tile)) tileCounts.Add(candidate.tile, 0);
                    expansionSites.Add((index, candidate.plot, candidate.usable, added));
                    bool accepted = false;
                    for (int attempt = 0; attempt < 52 && !accepted; attempt++)
                        accepted = AddToLot(expansionSites.Count - 1, attempt < 28 ? 0 : 1, attempt);
                    if (!accepted)
                    {
                        expansionSites.RemoveAt(expansionSites.Count - 1);
                        sectors.RemoveAt(index);
                        zones.RemoveAt(index);
                        siteTiles.RemoveAt(index);
                        plotPlans.RemoveAt(index);
                        Object.DestroyImmediate(group);
                    }
                }

                // Establish architectural anchors before smaller debris. Each side uses the same frontage axis.
                for (int pass = 0; pass < 4 && added < 6500; pass++)
                for (int lot = 0; lot < expansionSites.Count && added < 6500; lot++)
                for (int retry = 0; retry < 24; retry++)
                    if (AddToLot(lot, pass == 0 ? 0 : 1, retry + pass * 11)) break;

                int[] sequence = { 1, 1, 2, 4, 1, 2, 3, 1, 2, 4, 1, 2 };
                for (int pass = 0; pass < 65 && added < 6500; pass++)
                {
                    int before = added;
                    foreach (int lot in Enumerable.Range(0, expansionSites.Count).OrderBy(i =>
                        sectors[expansionSites[i].index].counts.Sum() / (float)expansionSites[i].usable.Count))
                    {
                        var site = expansionSites[lot];
                        if (added >= 6500 || sectors[site.index].counts.Sum() >= Mathf.Min(64, site.usable.Count * 5)) continue;
                        for (int retry = 0; retry < 18; retry++)
                        {
                            int category = retry < 13 ? sequence[(pass + lot) % sequence.Length] : retry % 2 == 0 ? 2 : 4;
                            if (AddToLot(lot, category, retry)) break;
                        }
                    }
                    if (before == added && pass > 15) break;
                }

                foreach (var lot in expansionSites)
                    acceptedLots[(siteTiles[lot.index], lot.plot)] = sectors[lot.index].counts.Sum();
                foreach (var lot in expansionSites.Where(e => sectors[e.index].counts[0] > 0)
                    .OrderByDescending(e => e.usable.Average(p => p.y) * .16f + sectors[e.index].counts[1])
                    .GroupBy(e => siteTiles[e.index]).Select(g => g.First()).Take(7))
                    visibleExpansion.Add(($"Lote-{siteTiles[lot.index].x:+00;-00;00}_{siteTiles[lot.index].y:+00;-00;00}-{lot.plot + 1}",
                        sectors[lot.index].center));

                bool AddToLot(int lot, int category, int attempt)
                {
                    var site = expansionSites[lot];
                    var tile = siteTiles[site.index];
                    var pad = Lot(tile, site.plot);
                    var anchor = site.usable[(attempt + sectors[site.index].counts.Sum()) % site.usable.Count];
                    int face = (sectors[site.index].counts.Sum() + attempt) % 4;
                    var axis = face < 2 ? Vector3.right : Vector3.forward;
                    if ((face & 1) == 0) axis = -axis;
                    var tangent = Vector3.Cross(Vector3.up, axis);
                    var point = anchor + (category == 0 ? tangent * ((Next() - .5f) * 2) :
                        new Vector3((Next() - .5f) * 2.5f, 0, (Next() - .5f) * 2.5f));
                    if (Tile(point.x, point.z) != tile || Plot(tile, point) != site.plot ||
                        FlatDistance(point, pad) < (category == 0 ? 0 : 2.8f)) return false;
                    var bank = category == 0 ? expansionLandmarks : budgetBanks[category];
                    int variety = category == 0 || category == 1 ? bank.Length : Mathf.Min(2, bank.Length);
                    var piece = category == 0 && attempt < 18 && site.usable.Count >= 6
                        ? facades[(lot + attempt) % facades.Length] : bank[random.Next(variety)];
                    bool fallen = category == 1 && piece.Path.Contains("Column_") && attempt % 3 == 0;
                    float size = category == 0 ? Mathf.Lerp(5.7f, 11.9f, Next()) : category == 1 ?
                        Mathf.Lerp(2.8f, 6.8f, Next()) : category == 3 ? Mathf.Lerp(2.8f, 4.8f, Next()) : Mathf.Lerp(1.25f, 3.4f, Next());
                    return Place(piece, point, size, Quaternion.Euler(fallen ? 82 : 0,
                        (face < 2 ? 90 : 0) + (Next() - .5f) * (category < 2 ? 12 : 72), fallen ? -8 : 0),
                        site.index, category);
                }

                RuinPiece BuildRuinFrontage(int variant)
                {
                    const float thickness = .65f;
                    var vertices = new List<Vector3>();
                    var uv = new List<Vector2>();
                    var indices = new List<int>();
                    float width = 7.6f + variant * .55f;
                    float depth = 6.1f + variant * .5f;
                    // Two connected walls, an interrupted frontage and an open entry.
                    Wall(new Vector3(-width * .5f, 0, depth * .5f), Vector3.right, width, 0);
                    Wall(new Vector3(-width * .5f, 0, -depth * .5f), Vector3.forward, depth, 1);
                    Wall(new Vector3(width * .5f, 0, -depth * .5f), Vector3.forward, depth * .47f, 2);
                    Wall(new Vector3(-width * .5f, 0, -depth * .5f), Vector3.right, width * .31f, 3);
                    Wall(new Vector3(width * .23f, 0, -depth * .5f), Vector3.right, width * .27f, 4);
                    var mesh = new Mesh { name = $"Restos de vivienda solar {variant + 1}" };
                    resources.Add(mesh);
                    mesh.SetVertices(vertices);
                    mesh.SetUVs(0, uv);
                    mesh.SetTriangles(indices, 0);
                    mesh.RecalculateNormals();
                    mesh.RecalculateTangents();
                    mesh.RecalculateBounds();
                    return new RuinPiece { Path = "Módulo temporal/" + mesh.name, Bounds = mesh.bounds,
                        Triangles = indices.Count / 3, Parts = new[] { (mesh, new[] { large[0].Parts[0].materials[0] }, Matrix4x4.identity) } };

                    void Wall(Vector3 start, Vector3 direction, float length, int side)
                    {
                        var across = Vector3.Cross(Vector3.up, direction) * thickness * .5f;
                        int bays = Mathf.Max(2, Mathf.RoundToInt(length / 1.3f));
                        for (int i = 0; i < bays; i++)
                        {
                            var a = start + direction * (i * length / bays);
                            var b = start + direction * ((i + 1) * length / bays);
                            float h0 = Height(i), h1 = Height(i + 1);
                            var at = a + Vector3.up * h0;
                            var bt = b + Vector3.up * h1;
                            Quad(a - across, b - across, bt - across, at - across);
                            Quad(b + across, a + across, at + across, bt + across);
                            Quad(at - across, bt - across, bt + across, at + across);
                            if (i == 0) Quad(a + across, a - across, at - across, at + across);
                            if (i == bays - 1) Quad(b - across, b + across, bt + across, bt - across);
                        }

                        float Height(int i)
                        {
                            float decay = side == 0 ? .84f : side == 1 ? .77f : .54f;
                            return .85f + (2.9f + variant * .22f) * decay *
                                Mathf.Lerp(.28f, 1f, Mathf.Abs(Mathf.Sin((i + side * 1.21f + variant * .81f) * 1.17f)));
                        }
                    }

                    void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
                    {
                        int first = vertices.Count;
                        vertices.AddRange(new[] { a, b, c, d });
                        var normal = Vector3.Cross(b - a, c - a);
                        foreach (var p in new[] { a, b, c, d })
                            uv.Add(Mathf.Abs(normal.y) > Mathf.Abs(normal.x) + Mathf.Abs(normal.z) ?
                                new Vector2(p.x, p.z) * .23f :
                                new Vector2(Mathf.Abs(normal.x) > Mathf.Abs(normal.z) ? p.z : p.x, p.y) * .23f);
                        indices.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
                    }
                }
            }

            bool Scatter(int siteIndex, int category)
            {
                var site = sectors[siteIndex];
                var tile = siteTiles[siteIndex];
                var seed = green[tile];
                var sample = seed[random.Next(seed.Count)];
                int plot = Plot(tile, sample);
                var lot = Lot(tile, plot);
                var counts = plotPlans[siteIndex];
                counts.TryGetValue(plot, out var built);
                if (category == 0 && built != null && built[0] > 0) category = 1;
                bool edge = category == 0 || category == 1 || category == 4;
                int side = (built == null ? 0 : built.Sum()) % 4;
                var along = side < 2 ? Vector3.forward : Vector3.right;
                var normal = side < 2 ? Vector3.right : Vector3.forward;
                if ((side & 1) != 0) normal = -normal;
                var line = lot + normal * (category == 0 ? 3.2f : 5.1f) + along * ((Next() - .5f) * 11);
                var point = edge ? Vector3.Lerp(line, sample, .30f + Next() * .25f) :
                    Vector3.Lerp(lot, sample, .72f) + new Vector3((Next() - .5f) * 5, 0, (Next() - .5f) * 5);
                if (Tile(point.x, point.z) != tile || Plot(tile, point) != plot) return false;
                if (category != 0 && FlatDistance(point, lot) < 2.6f) return false;
                var bank = budgetBanks[category];
                int variety = category == 2 || category == 3 ? Mathf.Min(2, bank.Length) : bank.Length;
                var piece = bank[random.Next(variety)];
                bool fallen = category == 3 && piece != dry[2] && Next() < .68f ||
                    category == 1 && piece.Path.Contains("Column_") && Next() < .45f;
                float yaw = side < 2 ? 90 : 0;
                float[] min = { 7f, 3.2f, 1.0f, 2.6f, 1.25f };
                float[] max = { 12.8f, 7.4f, 2.9f, 5.8f, 3.5f };
                float size = Mathf.Lerp(min[category], max[category], Next());
                return Place(piece, point, size, Quaternion.Euler(fallen ? Mathf.Lerp(71, 94, Next()) : 0,
                    yaw + (Next() - .5f) * (edge ? 15 : 130), fallen ? Mathf.Lerp(-12, 12, Next()) : 0), siteIndex, category);
            }

            (string, Vector3, Vector3) FindGroundView()
            {
                Physics.SyncTransforms();
                var physics = scene.GetPhysicsScene();
                foreach (var site in sectors)
                for (int dx = 8; dx <= 46; dx += 4)
                {
                    var xz = site.center - site.facing * dx;
                    if (!physics.Raycast(xz + Vector3.up * 45, Vector3.down, out var hit, 61, ~0,
                        QueryTriggerInteraction.Ignore) || hit.normal.y < .85f) continue;
                    var eye = hit.point + Vector3.up * 1.9f;
                    var aim = site.center + Vector3.up * 2;
                    if (!physics.Raycast(eye, (aim - eye).normalized, (aim - eye).magnitude - .7f, ~0,
                        QueryTriggerInteraction.Ignore)) return ("04-Altura-Warrior", eye, aim);
                }
                var fallback = sectors[0];
                return ("04-Altura-Warrior-borde-exterior", fallback.center + fallback.facing * 13 + Vector3.up * 1.9f,
                    fallback.center + Vector3.up * 2);
            }
            int Plot(Vector2Int tile, Vector3 point) =>
                (point.x >= tile.x * 42 + 21 ? 1 : 0) + (point.z >= tile.y * 42 + 21 ? 2 : 0);

            Vector3 Lot(Vector2Int tile, int plot) => new Vector3(tile.x * 42 + ((plot & 1) == 0 ? 10.5f : 31.5f),
                0, tile.y * 42 + ((plot & 2) == 0 ? 10.5f : 31.5f));

            bool ProtectedVertical(Bounds box, float supportY)
            {
                // Reserve the real plaza footprint and test walking surfaces in the local vertical band.
                var plaza = new Vector3(18, 12, 12);
                if (FlatDistance(box.ClosestPoint(plaza), plaza) < 62 && box.max.y > 5 && box.min.y < 22)
                    return true;
                float lower = supportY - .25f;
                float upper = Mathf.Max(box.max.y + 2.2f, supportY + 7.5f);
                var prism = new Bounds(new Vector3(box.center.x, (lower + upper) * .5f, box.center.z),
                    new Vector3(box.size.x + 3, upper - lower, box.size.z + 3));
                return Hits(prism, routes, routeIndex);
            }

            bool InsideLot(Vector2Int tile, Bounds box, float halfStreet)
            {
                int plot = Plot(tile, box.center);
                var center = Lot(tile, plot);
                float minX = center.x - 10.5f + ((plot & 1) == 0 ? 1.2f : halfStreet);
                float maxX = center.x + 10.5f - ((plot & 1) == 0 ? halfStreet : 1.2f);
                float minZ = center.z - 10.5f + ((plot & 2) == 0 ? 1.2f : halfStreet);
                float maxZ = center.z + 10.5f - ((plot & 2) == 0 ? halfStreet : 1.2f);
                return box.min.x >= minX && box.max.x <= maxX && box.min.z >= minZ && box.max.z <= maxZ;
            }

            (int[] counts, int fields, int giants, int renderers, int triangles, Vector3 hero) BuildCrystalFields()
            {
                var land = Read(RiftCrystal + "CrystalGround.prefab");
                var air = Read(RiftCrystal + "CrystalAerial.prefab");
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                Require(shader, "Falta el shader URP/Lit para los cristales.");
                Color[] baseColors = { new Color(.28f, .035f, .44f), new Color(.64f, .26f, .035f),
                    new Color(.075f, .028f, .12f) };
                Color[] emissions = { new Color(.57f, .035f, 1f) * 2.6f, new Color(1f, .43f, .06f) * 1.65f,
                    new Color(.15f, .035f, .48f) * .65f };
                var palette = baseColors.Select((color, i) =>
                {
                    var m = new Material(shader) { name = new[] { "Rift violeta", "Rift solar", "Rift obsidiana" }[i],
                        enableInstancing = true };
                    resources.Add(m);
                    m.SetColor("_BaseColor", color);
                    m.SetColor("_EmissionColor", emissions[i]);
                    m.SetFloat("_Smoothness", .53f);
                    m.EnableKeyword("_EMISSION");
                    return m;
                }).ToArray();
                // Small shards share a faceted proxy. The larger silhouettes use the ROM meshes.
                var shardMesh = new Mesh { name = "Cristal facetado · 24 triángulos" };
                resources.Add(shardMesh);
                var facets = new List<Vector3>();
                for (int face = 0; face < 6; face++)
                {
                    float a = face * Mathf.PI / 3, b = (face + 1) * Mathf.PI / 3;
                    var left = new Vector3(Mathf.Cos(a) * .35f, .25f, Mathf.Sin(a) * .35f);
                    var right = new Vector3(Mathf.Cos(b) * .35f, .25f, Mathf.Sin(b) * .35f);
                    var leftTop = left + Vector3.up * .92f;
                    var rightTop = right + Vector3.up * .92f;
                    facets.AddRange(new[] { left, leftTop, right, right, leftTop, rightTop,
                        leftTop, new Vector3(.13f, 1.82f, -.08f), rightTop,
                        right, new Vector3(-.10f, 0, .05f), left });
                }
                shardMesh.SetVertices(facets);
                shardMesh.SetTriangles(Enumerable.Range(0, facets.Count).ToArray(), 0);
                shardMesh.RecalculateNormals();
                shardMesh.RecalculateBounds();
                var shard = new RuinPiece { Bounds = shardMesh.bounds, Triangles = facets.Count / 3,
                    Parts = new[] { (shardMesh, new[] { palette[0] }, Matrix4x4.identity) } };
                var fields = new GameObject("Campos dimensionales · temporales");
                fields.transform.SetParent(root.transform, false);
                var counts = new int[4];
                int[] goals = { 600, 300, 250, 350 };
                int draw = 0, triangles = 0, giants = 0, fieldCount = 0;
                Vector3 hero = sectors[0].center;
                var centers = new List<(int site, Vector3 center, Transform group, List<Renderer> fine, List<Renderer> wide)>();
                var ruinas = occupied.ToArray();
                var ruinsIndex = IndexBounds(ruinas);
                foreach (int i in Enumerable.Range(0, sectors.Count).Where(i => zones[i] < 2)
                    .OrderByDescending(i => green[siteTiles[i]].Count))
                {
                    if (centers.Count >= 34) break;
                    var tile = siteTiles[i];
                    var candidates = green[tile].OrderBy(p => Mathf.Abs(p.x - sectors[i].center.x) +
                        Mathf.Abs(p.z - sectors[i].center.z)).ToArray();
                    var group = new GameObject("Campo " + sectors[i].label);
                    group.transform.SetParent(fields.transform, false);
                    var fine = new List<Renderer>();
                    var wide = new List<Renderer>();
                    bool found = false;
                    foreach (var point in candidates)
                    {
                        int color = fieldCount % 5 == 3 ? 1 : 0;
                        if (!Crystal(i, point, color, fieldCount % 3 == 0 ? 10.2f : 6.0f, group.transform, fine, wide) &&
                            !Crystal(i, point, color, 2.6f, group.transform, fine, wide)) continue;
                        centers.Add((i, point, group.transform, fine, wide));
                        fieldCount++;
                        if (giants == 1) hero = point;
                        found = true;
                        break;
                    }
                    if (!found) Object.DestroyImmediate(group);
                }
                for (int pass = 0; pass < 220 && counts.Sum() < 1500; pass++)
                {
                    int progress = counts.Sum();
                    foreach (var field in centers)
                    {
                        int color = (pass + field.site) % 10;
                        color = color < 4 ? 0 : color < 6 ? 3 : color < 8 ? 1 : 2;
                        if (counts[color] >= goals[color]) continue;
                        for (int retry = 0; retry < 19; retry++)
                        {
                            float angle = Next() * Mathf.PI * 2;
                            float radius = Mathf.Lerp(1.7f, 11.5f, Mathf.Sqrt(Next()));
                            var position = field.center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                            float size = color == 3 ? Mathf.Lerp(1.0f, 5.2f, Next()) : Mathf.Lerp(.85f, 4.2f, Mathf.Pow(Next(), 1.8f));
                            if (Crystal(field.site, position, color, size, field.group, field.fine, field.wide)) break;
                        }
                    }
                    if (counts.Sum() == progress && pass > 170) break;
                }
                foreach (var field in centers.Where((_, i) => i % 4 == 0))
                {
                    if (counts[3] >= goals[3]) break;
                    for (int retry = 0; retry < 12; retry++)
                    {
                        var offset = new Vector3((Next() - .5f) * 8, 0, (Next() - .5f) * 8);
                        if (Crystal(field.site, field.center + offset, 3, 9.4f, field.group, field.fine, field.wide)) break;
                    }
                }
                foreach (var field in centers)
                {
                    var lod = field.group.gameObject.AddComponent<LODGroup>();
                    lod.SetLODs(new[] { new LOD(.075f, field.fine.ToArray()), new LOD(.001f, field.wide.ToArray()) });
                    lod.RecalculateBounds();
                }
                return (counts, fieldCount, giants, draw, triangles, hero);

                bool Crystal(int site, Vector3 point, int color, float height, Transform group,
                    List<Renderer> fine, List<Renderer> wide)
                {
                    bool floating = color == 3;
                    var piece = height < 3.8f ? shard : floating ? air : land;
                    if (!Ground(point.x, point.z, out float floor)) return false;
                    var rotation = Quaternion.Euler(floating ? (Next() - .5f) * 66 : 0, Next() * 360,
                        floating ? (Next() - .5f) * 58 : 0);
                    float scale = height / Mathf.Max(piece.Bounds.size.x, piece.Bounds.size.y, piece.Bounds.size.z);
                    var local = TransformBounds(piece.Bounds, Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one * scale));
                    var pos = new Vector3(point.x - local.center.x, floor - local.min.y + (floating ? Mathf.Lerp(4, 18, Next()) : -.16f),
                        point.z - local.center.z);
                    var box = new Bounds(local.center + pos, local.size);
                    var upper = new Bounds(new Vector3(point.x, (box.min.y + 54) * .5f, point.z),
                        new Vector3(box.size.x + 1, Mathf.Max(0, 54 - box.min.y), box.size.z + 1));
                    bool intrudesOnRuin = Hits(box, ruinas, ruinsIndex) && ruinas.Any(b => b.Intersects(box) &&
                        FlatDistance(b.center, box.center) < Mathf.Min(b.extents.magnitude, box.extents.magnitude) *
                        (floating ? .8f : height >= 5 ? .28f : .58f));
                    if (!InsideLot(siteTiles[site], box, 2.8f) || Hits(upper, routes, routeIndex) || Blocked(box) ||
                        Hits(box, excluded, excludedIndex) || intrudesOnRuin ||
                        crystalBoxes.Any(b => b.Intersects(box) && (b.center - box.center).sqrMagnitude < .42f *
                            Mathf.Min(b.extents.sqrMagnitude, box.extents.sqrMagnitude)) ||
                        triangles + piece.Triangles > 1100000) return false;
                    var obj = new GameObject(floating ? "Fragmento suspendido" : height >= 8 ? "Cristal gigante" : "Cristal terrestre");
                    obj.transform.SetParent(group, false);
                    obj.transform.SetPositionAndRotation(pos, rotation);
                    obj.transform.localScale = Vector3.one * scale;
                    foreach (var part in piece.Parts)
                    {
                        var visual = new GameObject(part.mesh.name);
                        visual.transform.SetParent(obj.transform, false);
                        visual.transform.localPosition = part.local.GetColumn(3);
                        visual.transform.localRotation = part.local.rotation;
                        visual.transform.localScale = part.local.lossyScale;
                        visual.AddComponent<MeshFilter>().sharedMesh = part.mesh;
                        var renderer = visual.AddComponent<MeshRenderer>();
                        renderer.sharedMaterials = Enumerable.Repeat(palette[floating ? (site % 4 == 0 ? 2 : 0) : color],
                            part.mesh.subMeshCount).ToArray();
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        renderer.lightProbeUsage = LightProbeUsage.Off;
                        fine.Add(renderer);
                        if (height >= 3.3f) wide.Add(renderer);
                    }
                    draw += piece.Parts.Length;
                    triangles += piece.Triangles;
                    counts[color]++;
                    if (height >= 8)
                    {
                        giants++;
                        var clear = box;
                        clear.Expand(new Vector3(10, 1, 10));
                        giantReserves.Add(clear);
                    }
                    crystalBoxes.Add(box);
                    return true;
                }
            }

            Vector2Int Tile(float x, float z) => new Vector2Int(Mathf.FloorToInt(x / 42), Mathf.FloorToInt(z / 42));

            int Zone(Vector3 p) => p.x > -135 && p.x < 202 && p.z > -132 && p.z < 180 ? 0 :
                p.x > -169 && p.x < 234 && p.z > -157 && p.z < 211 ? 1 : 2;

            float Coverage(List<Vector3> samples)
            {
                int covered = 0;
                foreach (var point in samples)
                foreach (float dx in new[] { -2f, 0f, 2f })
                foreach (float dz in new[] { -2f, 0f, 2f })
                {
                    var sample = point + new Vector3(dx, 0, dz);
                    if (occupiedIndex.TryGetValue(new Vector2Int(Cell(sample.x), Cell(sample.z)), out var nearby) &&
                        nearby.Any(i => sample.x >= occupied[i].min.x && sample.x <= occupied[i].max.x &&
                            sample.z >= occupied[i].min.z && sample.z <= occupied[i].max.z &&
                            sample.y >= occupied[i].min.y - 3 && sample.y <= occupied[i].max.y + 3)) covered++;
                }
                return covered * 100f / (samples.Count * 9);
            }

            void WriteDistribution()
            {
                Directory.CreateDirectory(RuinsOutput);
                    var table = cells.GroupBy(c => c.tile).OrderBy(g => g.Key.x).ThenBy(g => g.Key.y)
                        .Select(g =>
                        {
                            tileCounts.TryGetValue(g.Key, out int count);
                            occupancy.TryGetValue(g.Key, out float coverage);
                            float useful = g.Count(c => c.status == 0) * spacing * spacing;
                            return FormattableString.Invariant($"{g.Key.x},{g.Key.y},{useful},{count},{coverage:F1},{g.Count(c => c.reason == 1)},{g.Count(c => c.reason == 2)},{g.Count(c => c.reason == 3)},{g.Count(c => c.reason == 4)}");
                        });
                    File.WriteAllLines(Path.Combine(RuinsOutput, "Sectores.csv"), new[] {
                        "sector_x,sector_z,m2_utiles_muestreados,piezas,ocupacion_muestreada_pct,rojo_reserva,amarillo_sin_apoyo,amarillo_arquitectura,amarillo_exterior"
                    }.Concat(table));
            }

            void WriteMap()
            {
                Directory.CreateDirectory(RuinsOutput);
                using var writer = new StreamWriter(Path.Combine(RuinsOutput, "Mapa-disponibilidad.svg"));
                writer.WriteLine("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 650 610'><rect width='650' height='610' fill='#151621'/>");
                writer.WriteLine("<g font-family='sans-serif' fill='white'><text x='28' y='31' font-size='22'>Ciudad del Sol · disponibilidad exterior</text><text x='28' y='55' font-size='13'>Verde: apoyo libre · Rojo: reserva o estructura superior · Amarillo: revisar</text></g>");
                string[] colors = { "#53bc77", "#e55663", "#e8bf57" };
                foreach (var cell in cells)
                    writer.WriteLine(FormattableString.Invariant($"<rect x='{60 + cell.p.x - limits.min.x:F1}' y='{80 + limits.max.z - cell.p.z:F1}' width='5.7' height='5.7' fill='{colors[cell.status]}'/>"));
                writer.WriteLine("<text x='560' y='105' fill='white' font-family='sans-serif'>+Z ↑</text><text x='560' y='126' fill='white' font-family='sans-serif'>+X →</text></svg>");
            }

            bool IsDecorativeRoof(Transform node)
            {
                for (var t = node; t; t = t.parent)
                    if (t.name.StartsWith("02 - Bovedas generales Y-4", StringComparison.Ordinal) ||
                        t.name == "Boveda longitudinal") return true;
                return false;
            }

            float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
            bool Blocked(Bounds b)
            {
                var reserve = b;
                reserve.Expand(new Vector3(6, 2, 6));
                return Hits(reserve, protectedBounds, protectedIndex);
            }

            int Cell(float coordinate) => Mathf.FloorToInt(coordinate / 12);

            Dictionary<Vector2Int, List<int>> IndexBounds(Bounds[] items)
            {
                var index = new Dictionary<Vector2Int, List<int>>();
                for (int i = 0; i < items.Length; i++)
                for (int x = Cell(items[i].min.x); x <= Cell(items[i].max.x); x++)
                for (int z = Cell(items[i].min.z); z <= Cell(items[i].max.z); z++)
                {
                    var key = new Vector2Int(x, z);
                    if (!index.TryGetValue(key, out var list)) index[key] = list = new List<int>();
                    list.Add(i);
                }
                return index;
            }

            bool Hits(Bounds box, Bounds[] items, Dictionary<Vector2Int, List<int>> index)
            {
                for (int x = Cell(box.min.x); x <= Cell(box.max.x); x++)
                for (int z = Cell(box.min.z); z <= Cell(box.max.z); z++)
                    if (index.TryGetValue(new Vector2Int(x, z), out var list) &&
                        list.Any(i => items[i].Intersects(box))) return true;
                return false;
            }

            bool Ground(float x, float z, out float y, bool terrainOnly = false)
            {
                y = float.NegativeInfinity;
                var p = new Vector2(x, z);
                if (!supportIndex.TryGetValue(new Vector2Int(Cell(x), Cell(z)), out var faces)) return false;
                foreach (int index in faces)
                {
                    var face = support[index];
                    if (terrainOnly && !face.land) continue;
                    if (x < face.box.min.x || x > face.box.max.x || z < face.box.min.z || z > face.box.max.z ||
                        face.box.max.y < y) continue;
                    var a = new Vector2(face.a.x, face.a.z);
                    var b = new Vector2(face.b.x, face.b.z) - a;
                    var c = new Vector2(face.c.x, face.c.z) - a;
                    var offset = p - a;
                    float det = b.x * c.y - b.y * c.x;
                    if (Mathf.Abs(det) < .001f) continue;
                    float u = (offset.x * c.y - offset.y * c.x) / det;
                    float v = (b.x * offset.y - b.y * offset.x) / det;
                    if (u >= 0 && v >= 0 && u + v <= 1)
                        y = Mathf.Max(y, face.a.y + u * (face.b.y - face.a.y) + v * (face.c.y - face.a.y));
                }
                return y >= -63 && y <= 5;
            }

            bool Place(RuinPiece piece, Vector3 point, float size, Quaternion rotation, int siteIndex, int category)
            {
                var site = sectors[siteIndex];
                var parent = site.root;
                bool burned = category == 3, small = category == 2 || category == 4;
                float scale = size / Mathf.Max(piece.Bounds.size.x, piece.Bounds.size.y, piece.Bounds.size.z);
                var oriented = TransformBounds(piece.Bounds, Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one * scale));
                if (!Ground(point.x, point.z, out var floor))
                {
                    rejected++;
                    return false;
                }
                float lower = floor, upper = floor;
                foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                {
                    var check = point + Vector3.Scale(oriented.extents, dir) * .66f;
                    if (!Ground(check.x, check.z, out float edge))
                    {
                        rejected++;
                        return false;
                    }
                    lower = Mathf.Min(lower, edge);
                    upper = Mathf.Max(upper, edge);
                }
                if (upper - lower > Mathf.Min(2.4f, size * .33f))
                {
                    rejected++;
                    return false;
                }
                var position = new Vector3(point.x - oriented.center.x, lower - oriented.min.y - Mathf.Min(.35f, size * .07f),
                    point.z - oriented.center.z);
                var box = new Bounds(oriented.center + position, oriented.size);
                var above = new Bounds(new Vector3(point.x, (box.max.y + 54) * .5f, point.z),
                    new Vector3(box.size.x + 1, Mathf.Max(0, 54 - box.max.y), box.size.z + 1));
                if (!InsideLot(siteTiles[siteIndex], box, 2.8f) || (expanding ? ProtectedVertical(box, floor) : Hits(above, routes, routeIndex)) || Blocked(box) || Hits(box, excluded, excludedIndex) ||
                    occupied.Any(b => b.Intersects(box) && FlatDistance(b.center, box.center) <
                        Mathf.Min(b.extents.magnitude, box.extents.magnitude) * (small ? .45f : .74f)))
                {
                    rejected++;
                    return false;
                }
                int cost = piece.Triangles;
                if (expanding)
                {
                    if (added >= 6500 || expansionCost[category] + cost > expansionBudget[category] ||
                        Hits(box, crystalArray, crystalIndex) || giantReserves.Any(b => b.Intersects(box))) return false;
                }
                else if (categoryCost[category] + cost > categoryBudget[category] || added >= totalTarget) return false;

                var obj = new GameObject(System.IO.Path.GetFileNameWithoutExtension(piece.Path));
                obj.transform.SetParent(parent, false);
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.transform.localScale = Vector3.one * scale;
                foreach (var part in piece.Parts)
                {
                    var visual = piece.Parts.Length == 1 && part.local == Matrix4x4.identity ? obj : new GameObject(part.mesh.name);
                    if (visual != obj)
                    {
                        visual.transform.SetParent(obj.transform, false);
                        visual.transform.localPosition = part.local.GetColumn(3);
                        visual.transform.localRotation = part.local.rotation;
                        visual.transform.localScale = part.local.lossyScale;
                    }
                    visual.AddComponent<MeshFilter>().sharedMesh = part.mesh;
                    var renderer = visual.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = burned ? part.materials.Select(Charred).ToArray() :
                        piece.Path.EndsWith("Rock_Boulder_C.prefab", StringComparison.Ordinal) ? new[] { rubbleStone } :
                        part.materials.Select(Instanced).ToArray();
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    site.lod[category].Add(renderer);
                }
                occupied.Add(box);
                if (!used.TryAdd(piece.Path, 1)) used[piece.Path]++;
                added++;
                categoryCounts[category]++;
                if (expanding)
                {
                    expansionCounts[category]++;
                    expansionCost[category] += cost;
                }
                zoneCounts[zones[siteIndex]]++;
                var tile = siteTiles[siteIndex];
                tileCounts[tile]++;
                categoryCost[category] += cost;
                site.counts[category]++;
                int plot = Plot(tile, point);
                if (!plotPlans[siteIndex].TryGetValue(plot, out var plan)) plotPlans[siteIndex][plot] = plan = new int[5];
                plan[category]++;
                triangles += cost;
                renderers += piece.Parts.Length;
                return true;
            }

            Material Instanced(Material original)
            {
                if (original.enableInstancing || !original.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal))
                    return original;
                if (sharedStone.TryGetValue(original, out var material)) return material;
                material = new Material(original) { enableInstancing = true };
                sharedStone.Add(original, material);
                resources.Add(material);
                return material;
            }

            Material Charred(Material source)
            {
                if (charcoal.TryGetValue(source, out var result)) return result;
                result = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                    { name = source.name + " · madera carbonizada", enableInstancing = true };
                var albedo = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : null;
                if (!albedo && source.HasProperty("_MainTex")) albedo = source.GetTexture("_MainTex");
                if (albedo) result.SetTexture("_BaseMap", albedo);
                if (source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap"))
                {
                    result.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                    result.EnableKeyword("_NORMALMAP");
                }
                result.SetColor("_BaseColor", new Color(.37f, .32f, .33f, 1));
                result.SetFloat("_Smoothness", .08f);
                charcoal.Add(source, result);
                resources.Add(result);
                return result;
            }

            RuinPiece Read(string path)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Require(prefab, "Falta el recurso: " + path);
                var lod = prefab.GetComponentInChildren<LODGroup>();
                var sources = lod && lod.GetLODs().Length > 0 ? lod.GetLODs()[0].renderers.OfType<MeshRenderer>() :
                    prefab.GetComponentsInChildren<MeshRenderer>().AsEnumerable();
                var parts = sources.Where(r => r && r.enabled).Select(r =>
                    (mesh: r.GetComponent<MeshFilter>()?.sharedMesh, materials: r.sharedMaterials,
                     local: prefab.transform.worldToLocalMatrix * r.transform.localToWorldMatrix))
                    .Where(p => p.mesh && p.materials.Length == p.mesh.subMeshCount && p.materials.All(m => m && m.shader && m.shader.isSupported))
                    .ToArray();
                Require(parts.Length > 0, "No se encontró geometría compatible en " + path);
                var box = parts.Select(p => TransformBounds(p.mesh.bounds, p.local))
                    .Aggregate((a, b) => { a.Encapsulate(b); return a; });
                int triangles = parts.Sum(p => Enumerable.Range(0, p.mesh.subMeshCount).Sum(i => (int)p.mesh.GetIndexCount(i) / 3));
                return new RuinPiece { Path = path, Parts = parts, Bounds = box, Triangles = triangles };
            }

            Bounds TransformBounds(Bounds input, Matrix4x4 transform)
            {
                var x = transform.MultiplyVector(Vector3.right * input.extents.x);
                var y = transform.MultiplyVector(Vector3.up * input.extents.y);
                var z = transform.MultiplyVector(Vector3.forward * input.extents.z);
                var half = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                    Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
                return new Bounds(transform.MultiplyPoint3x4(input.center), half * 2);
            }
        }

        [MenuItem(RuinsMenu + "5. Validar y exportar comparativas")]
        public static void ReviewExteriorRuinsInBatch()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Las capturas requieren modo edición.");
            var hash = Hash();
            if (Environment.GetCommandLineArgs().Contains("-verify-rock-idempotence"))
            {
                IntegrateRockCoverInBatch();
                Require(Hash() == hash, "La repetición alteró la escena.");
            }
            var scene = EditorSceneManager.OpenPreviewScene(Original);
            var resources = new List<Object>();
            try
            {
                ValidateFinalLook(scene);
                var root = BuildExteriorRuins(scene, scene.GetRootGameObjects(), resources, out var views, out var report, true);
                var extension = root.transform.Find(RuinsExpansion).gameObject;
                CaptureAtmosphereViews(scene, null, extension, views.Take(17).ToArray(), RuinsOutput);
                Require(Hash() == hash, "Cambió la escena definitiva.");
                File.WriteAllText(Path.Combine(RuinsOutput, "Validacion.txt"), report + "\nEscena SHA-256: " + hash);
                Debug.Log("RUINAS_EXTERIORES_OK " + report);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
        }
    }
}


