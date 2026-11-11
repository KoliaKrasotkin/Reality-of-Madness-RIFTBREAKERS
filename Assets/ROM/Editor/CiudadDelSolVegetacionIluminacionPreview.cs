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

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string AmbientMenu = IslandMenu + "Vegetacion e iluminacion dimensional/";
        const string AmbientRoot = "Ambientación exterior · temporal";
        const string AmbientPlants = "Vegetación";
        const string SurfaceCorrectionRoot = "Superficies recuperadas · temporales";
        const string VaultDensityRoot = "Cristales y ruinas de bóvedas · temporales";
        const string AmbientEnergy = "Iluminación dimensional";
        const string AmbientOutput = "Backups/CiudadDelSol/DensificacionBovedas";

        [MenuItem(AmbientMenu + "1. Abrir propuesta temporal")]
        static void OpenAmbientLayers() => OpenRockCover(true, true);

        [MenuItem(AmbientMenu + "2. Siguiente vista")]
        static void NextAmbientView() => NextRockCover();

        [MenuItem(AmbientMenu + "3. Alternar vegetacion")]
        static void ToggleAmbientPlants() => ToggleAmbient(AmbientPlants);

        [MenuItem(AmbientMenu + "4. Alternar iluminacion dimensional")]
        static void ToggleAmbientEnergy() => ToggleAmbient(AmbientEnergy);

        [MenuItem(AmbientMenu + "5. Alternar ruinas y cristales")]
        static void ToggleAmbientRuins()
        {
            if (StageUtility.GetCurrentStage() is RockCoverStage stage && stage.Ambiance) stage.Toggle();
        }

        [MenuItem(AmbientMenu + "6. Cerrar sin guardar")]
        static void CloseAmbientLayers() => CloseRockCover();

        static void ToggleAmbient(string layer)
        {
            if (StageUtility.GetCurrentStage() is RockCoverStage stage && stage.Ambiance) stage.ToggleLayer(layer);
        }

        [MenuItem(AmbientMenu + "7. Exportar comparativas")]
        public static void ReviewAmbientLayersInBatch()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Las capturas requieren modo edición.");
            var hash = Hash();
            var scene = EditorSceneManager.OpenPreviewScene(Original);
            var resources = new List<Object>();
            try
            {
                ValidateFinalLook(scene);
                var ruins = BuildExteriorRuins(scene, scene.GetRootGameObjects(), resources, out var views, out var report, ambiance: true);
                var layers = ruins.transform.Find(SurfaceCorrectionRoot + "/" + VaultDensityRoot).gameObject;
                Directory.CreateDirectory(AmbientOutput);
                File.WriteAllText(Path.Combine(AmbientOutput, "Validacion.txt"), report + "\nEscena SHA-256: " + hash);
                CaptureAtmosphereViews(scene, null, layers, views, AmbientOutput);
                Require(Hash() == hash, "Cambió la escena definitiva.");
                Debug.Log("VEGETACION_ILUMINACION_OK");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
        }

        static ((string name, Vector3 eye, Vector3 target)[] views, string report) BuildAmbientLayers(
            Scene scene, List<Object> resources, Collider[] routeColliders, Vector3[] lots, Func<float, float, float?> ground,
            Func<Bounds, bool> reserved, Func<Bounds, bool> occupied)
        {
            var random = new System.Random(830721);
            var root = new GameObject(AmbientRoot);
            SceneManager.MoveGameObjectToScene(root, scene);
            var plants = Child(AmbientPlants, root.transform);
            var energy = Child(AmbientEnergy, root.transform);
            var palette = new[]
            {
                Surface("Verde hoja", new Color(.27f, .39f, .16f)),
                Surface("Oliva", new Color(.36f, .41f, .17f)),
                Surface("Paja seca", new Color(.41f, .33f, .16f)),
                Surface("Oliva corrompido", new Color(.27f, .24f, .20f))
            };
            var cells = new Dictionary<(int x, int z, int tint), List<Vector3>>();
            var planted = new HashSet<Vector3Int>();
            var lotSites = lots.GroupBy(p => new Vector2Int(Mathf.FloorToInt(p.x / 5), Mathf.FloorToInt(p.z / 5)))
                .Select(g => g.First()).OrderBy(p => p.x).ThenBy(p => p.z).ToArray();
            int stems = 0, pavedStems = 0;
            Vector3 pavedView = Vector3.zero, pavedEye = Vector3.zero, ruinView = Vector3.zero, ruinEye = Vector3.zero;
            foreach (var site in lotSites)
            {
                float mood = Mathf.PerlinNoise(site.x * .035f + 25, site.z * .035f + 25);
                int tint = mood > .72f ? 2 : mood < .25f ? 3 : mood < .57f ? 0 : 1;
                int before = stems;
                for (int i = 0; i < 36; i++)
                {
                    float angle = Range(0, 2 * Mathf.PI);
                    float radius = Mathf.Sqrt(Range(0, 1)) * Range(1.2f, 3.2f);
                    var point = site + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    var y = ground(point.x, point.z);
                    if (!y.HasValue || Mathf.Abs(y.Value - site.y) > .8f) continue;
                    point.y = y.Value;
                    if (reserved(new Bounds(point + Vector3.up * .32f, new Vector3(1.6f, .68f, 1.6f)))) continue;
                    AddTuft(point, tint, Range(.26f, .62f));
                }
                if (stems - before > 12 && (ruinView == Vector3.zero || site.y > ruinView.y))
                {
                    for (int direction = 0; direction < 8; direction++)
                    {
                        var eye = site + Quaternion.Euler(0, direction * 45, 0) * Vector3.forward * 5;
                        var y = ground(eye.x, eye.z);
                        if (!y.HasValue || Mathf.Abs(y.Value - site.y) > .8f) continue;
                        eye.y = y.Value + 1.9f;
                        if (!ClearView(eye, site + Vector3.up * .55f)) continue;
                        ruinView = site;
                        ruinEye = eye;
                        break;
                    }
                }
            }

            var routes = routeColliders.Select(c => c.bounds).ToArray();
            var platformColliders = routeColliders.Where(c => IsPlatform(c.bounds)).ToArray();
            var platforms = platformColliders.Select(c => c.bounds).ToArray();
            bool IsPlatform(Bounds b) => b.max.y >= -.1f && b.max.y <= 36.1f &&
                b.size.y <= 2.5f && b.size.x >= 4 && b.size.z >= 4;
            // Allow low shoots only in exterior corners with no neighboring pavement.
            foreach (var floorCollider in platformColliders)
            {
                var floor = floorCollider.bounds;
                if (Mathf.PerlinNoise(floor.center.x * .06f + 40, floor.center.z * .06f + 40) < .55f) continue;
                if (Mathf.Abs(floor.center.x) < 67 && Mathf.Abs(floor.center.z) < 67) continue;
                for (int side = 0; side < 4; side++)
                {
                    var direction = side < 2 ? Vector3.right * (side == 0 ? -1 : 1) :
                        Vector3.forward * (side == 2 ? -1 : 1);
                    var tangent = Vector3.Cross(Vector3.up, direction);
                    float radius = side < 2 ? floor.extents.x : floor.extents.z;
                    float length = side < 2 ? floor.extents.z : floor.extents.x;
                    var edge = floor.center + direction * radius;
                    edge.y = floor.max.y;
                    if (routes.Any(b => Mathf.Abs(b.max.y - edge.y) < 1.1f &&
                        b.Contains(edge + direction * .44f - Vector3.up * .02f))) continue;
                    int before = stems;
                    for (int i = 0; i < 12; i++)
                    {
                        var p = edge - direction * Range(.24f, .42f) + tangent * Range(-length * .52f, length * .52f);
                        if (!floorCollider.Raycast(new Ray(p + Vector3.up, Vector3.down), out var hit, 1.15f) ||
                            hit.normal.y < .94f || Mathf.Abs(hit.point.y - p.y) > .15f) continue;
                        p.y = hit.point.y;
                        if (occupied(new Bounds(p + Vector3.up * .24f, new Vector3(.55f, .42f, .55f)))) continue;
                        AddTuft(p, side % 2, Range(.12f, .22f));
                    }
                    pavedStems += stems - before;
                    if (stems - before <= 5 || pavedView != Vector3.zero) continue;
                    foreach (float along in new[] { -2.5f, 2.5f, 0 })
                    {
                        var eye = edge - direction * Mathf.Min(4, radius) + tangent * along + Vector3.up * 1.9f;
                        var target = edge - direction * .35f + Vector3.up * .45f;
                        if (occupied(new Bounds(eye, Vector3.one * .25f))) continue;
                        pavedEye = eye;
                        pavedView = target;
                        break;
                    }
                }
            }

            foreach (var cell in cells)
            {
                var group = Child($"Mancha {cell.Key.x}/{cell.Key.z}/{cell.Key.tint}", plants.transform);
                var renderer = Render(group, cell.Value, palette[cell.Key.tint]);
                var lod = group.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(.017f, new Renderer[] { renderer }) });
                lod.RecalculateBounds();
            }

            var gaps = new List<(Vector3 center, float bottom, float top, Vector3 outward)>();
            var chamber = new Bounds(new Vector3(18, 0, 18), new Vector3(100, 200, 100));
            var candidates = platforms.Where(b => b.min.y > 4).OrderByDescending(b => b.min.y)
                .SelectMany(b => new[] { b.center, b.center + new Vector3(b.extents.x * .6f, 0, 0),
                    b.center - new Vector3(b.extents.x * .6f, 0, 0),
                    b.center + new Vector3(0, 0, b.extents.z * .6f),
                    b.center - new Vector3(0, 0, b.extents.z * .6f) });
            foreach (var sample in candidates)
            {
                if (chamber.Contains(sample)) continue;
                var bottom = ground(sample.x, sample.z);
                if (!bottom.HasValue || bottom.Value < -7 || bottom.Value > -1) continue;
                var cap = platforms.Where(b => b.min.y > bottom.Value && sample.x > b.min.x + .2f && sample.x < b.max.x - .2f &&
                    sample.z > b.min.z + .2f && sample.z < b.max.z - .2f).OrderBy(b => b.min.y).FirstOrDefault();
                if (cap.size == Vector3.zero || cap.min.y - bottom.Value < 8f) continue;
                float height = cap.min.y - bottom.Value;
                var center = new Vector3(sample.x, bottom.Value + height * .45f, sample.z);
                if (gaps.Any(g => Vector3.Distance(g.center, center) < 32)) continue;
                var box = new Bounds(center, new Vector3(8, Mathf.Min(height - 3, 9), 8));
                if (box.min.y < bottom.Value + .8f || box.max.y > cap.min.y - .8f || occupied(box)) continue;
                var outward = new Vector3(center.x - 18, 0, center.z - 18).normalized;
                gaps.Add((center, bottom.Value, cap.min.y, outward));
                if (gaps.Count == 6) break;
            }

            Require(stems > 100 && gaps.Count > 0, $"Cobertura o huecos insuficientes: {stems} / {gaps.Count}.");
            var colors = new[] { new Color(.52f, .06f, 1f), new Color(1f, .08f, .67f),
                new Color(.79f, .72f, 1f), new Color(1f, .57f, .14f) };
            var glows = colors.Select((color, i) => Surface("Emisión " + i, color, i == 2 ? 6.5f : 4.5f)).ToArray();
            int shards = 0;
            for (int i = 0; i < gaps.Count; i++)
            {
                var gap = gaps[i];
                var formation = Child($"Fractura {i + 1:00}", energy.transform);
                var tangent = Vector3.Cross(Vector3.up, gap.outward);
                for (int color = 0; color < glows.Length; color++)
                {
                    var vertices = new List<Vector3>();
                    float verticalScale = Mathf.Min(1, (gap.top - gap.bottom - 3) / 7f);
                    int count = color == 0 ? 16 : color == 1 ? 9 : color == 2 ? 4 : 3;
                    for (int j = 0; j < count; j++)
                    {
                        var point = gap.center + tangent * Range(-2.8f, 2.8f) +
                            gap.outward * Range(-.75f, .75f) + Vector3.up * (Range(-1.4f, 1.4f) * verticalScale);
                        var axis = (Vector3.up + tangent * Range(-.82f, .82f)).normalized * (Range(.35f, 1.85f) * verticalScale);
                        var width = tangent * Range(.06f, .33f);
                        var depth = gap.outward * Range(.07f, .26f);
                        var baseRing = new[] { point - width, point + depth, point + width, point - depth };
                        for (int k = 0; k < 4; k++)
                        {
                            var a = baseRing[k];
                            var b = baseRing[(k + 1) % 4];
                            Triangle(vertices, point + axis, a, b);
                            Triangle(vertices, point - axis * .8f, b, a);
                        }
                        shards++;
                    }
                    Render(Child(glows[color].name, formation.transform), vertices, glows[color]);
                }
                if (i >= 4) continue;
                var lightObject = Child("Rebote local sin sombras", formation.transform);
                lightObject.transform.position = gap.center;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.shadows = LightShadows.None;
                light.color = colors[i % 2];
                light.range = Mathf.Min(17, gap.top - gap.bottom + 2);
                light.intensity = 13;
            }

            Require(root.GetComponentsInChildren<Collider>().Length == 0, "La ambientación introdujo colliders.");
            if (ruinView == Vector3.zero)
            {
                ruinView = lotSites.OrderByDescending(p => p.y).First();
                ruinEye = ruinView + new Vector3(5, 1.9f, 3);
            }
            if (pavedView == Vector3.zero)
            {
                pavedEye = ruinEye;
                pavedView = ruinView + Vector3.up * .45f;
            }
            var first = gaps[0];
            var second = gaps[Mathf.Min(1, gaps.Count - 1)];
            var third = gaps[Mathf.Min(2, gaps.Count - 1)];
            var views = new[]
            {
                ("01-Aerea", new Vector3(25, 465, -85), new Vector3(25, 2, 18)),
                ("02-Camino-Warrior", pavedEye, pavedView),
                ("03-Barrio-Warrior", ruinEye, ruinView + Vector3.up * .6f),
                ("04-Base-exterior", first.center + first.outward * 38 + Vector3.up * 4, first.center),
                ("05-Techo-dungeon", second.center + second.outward * 25 - Vector3.up * 4, second.center),
                ("06-Espacio-intermedio", third.center + third.outward * 58 + Vector3.up * 8, third.center),
                ("07-Conjunto", new Vector3(236, 188, -305), new Vector3(23, -9, 0))
            };
            var report = $"Vegetación: {stems} matas ({pavedStems} en bordes de pavimento), {cells.Count} renderizadores/LOD, " +
                $"{cells.Sum(c => c.Value.Count / 3)} triángulos máximos, 4 materiales opacos URP/Lit.\n" +
                $"Iluminación: {gaps.Count} formaciones, {shards} fragmentos emisivos, {gaps.Count * 4} renderizadores, " +
                $"{shards * 8} triángulos, {Mathf.Min(gaps.Count, 4)} luces sin sombras, 4 materiales URP/Lit.\n" +
                string.Join("\n", gaps.Select(g => $"Centro {g.center:F2}; techo Y={g.bottom:F2}, " +
                    $"intradós Y={g.top:F2}, separación {g.top - g.bottom:F2} m.")) +
                "\n0 colliders nuevos. Coste geométrico, sin perfilado de GPU ni prueba dinámica.";
            return (views, report);

            bool ClearView(Vector3 eye, Vector3 target)
            {
                for (int i = 0; i < 5; i++)
                {
                    var point = Vector3.Lerp(eye, target, i / 8f);
                    if (occupied(new Bounds(point, Vector3.one * (i == 0 ? .55f : .18f)))) return false;
                }
                return true;
            }

            float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

            void AddTuft(Vector3 point, int tint, float height)
            {
                if (!planted.Add(Vector3Int.RoundToInt(point * 5))) return;
                var key = (Mathf.FloorToInt(point.x / 24), Mathf.FloorToInt(point.z / 24), tint);
                if (!cells.TryGetValue(key, out var vertices)) cells[key] = vertices = new List<Vector3>();
                float angle = Range(0, 360);
                for (int blade = 0; blade < 5; blade++)
                {
                    var direction = Quaternion.Euler(0, angle + blade * 137.5f, 0) * Vector3.forward;
                    var width = Vector3.Cross(Vector3.up, direction) * Range(.04f, .083f);
                    var basePoint = point + direction * Range(0, height * .29f);
                    var joint = basePoint + direction * height * .39f + Vector3.up * height * .57f;
                    var tip = joint + direction * height * .42f + Vector3.up * height * Range(.26f, .49f);
                    Triangle(vertices, basePoint - width, joint - width * .65f, basePoint + width);
                    Triangle(vertices, basePoint + width, joint - width * .65f, joint + width * .65f);
                    Triangle(vertices, joint - width * .65f, tip, joint + width * .65f);
                }
                stems++;
            }

            MeshRenderer Render(GameObject obj, List<Vector3> vertices, Material material)
            {
                var mesh = new Mesh { name = obj.name };
                mesh.SetVertices(vertices);
                mesh.SetIndices(Enumerable.Range(0, vertices.Count).ToArray(), MeshTopology.Triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                resources.Add(mesh);
                obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                return renderer;
            }

            Material Surface(string name, Color color, float emission = 0)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                Require(shader && shader.isSupported, "URP/Lit no está disponible.");
                var material = new Material(shader) { name = name };
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Cull", (float)CullMode.Off);
                material.SetFloat("_Smoothness", .12f);
                if (emission > 0)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * emission);
                }
                resources.Add(material);
                return material;
            }

            GameObject Child(string name, Transform parent)
            {
                var child = new GameObject(name);
                child.transform.SetParent(parent, false);
                return child;
            }

            void Triangle(List<Vector3> vertices, Vector3 a, Vector3 b, Vector3 c)
            {
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
            }
        }
    }
}

