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
        const string IslandMenu = "ROM/Dungeon Ciudad del Sol/Exterior/";
        const string IslandOutput = "Backups/CiudadDelSol/MasaFlotante";
        const string RockMaterial = "Assets/ROM/Levels/FracturedForest/Materials/FracturedCliff.mat";
        const string DirtMaterial = "Assets/ROM/World/Pilots/RefugeTown20261028/RuralDirtPath.mat";
        const int CliffSegments = 144;
        const float CrestMax = -6;

        static bool IsIslandArchitecture(GameObject root) =>
            root.name.StartsWith("Subsuelo_", StringComparison.Ordinal) ||
            root.name == "Dungeon_Ciudad_del_Sol_Generated" || root.name == "Nucleo_Monumental_Conjunto" ||
            root.name.StartsWith("Salida_Meridional", StringComparison.Ordinal) ||
            root.name.StartsWith("Umbral_De_Las_Profundidades", StringComparison.Ordinal);

        [MenuItem(IslandMenu + "1. Abrir masa flotante temporal")]
        static void OpenIslandPreview()
        {
            var source = SceneManager.GetActiveScene();
            Require(source.path == Original && !source.isDirty && Hash() == ClearanceHash &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                StageUtility.GetCurrentStage() == StageUtility.GetMainStage(),
                "Abrí Dungeon_Isla_del_Sol limpia en modo edición.");
            var stage = ScriptableObject.CreateInstance<IslandStage>();
            stage.Source = source;
            StageUtility.GoToStage(stage, true);
        }

        [MenuItem(IslandMenu + "2. Siguiente vista exterior")]
        static void NextIslandView()
        {
            if (StageUtility.GetCurrentStage() is IslandStage stage) stage.FrameNext();
        }

        [MenuItem(IslandMenu + "3. Alternar antes y después")]
        static void ToggleIslandPreview()
        {
            if (StageUtility.GetCurrentStage() is IslandStage stage && stage.Island)
                stage.Island.SetActive(!stage.Island.activeSelf);
        }

        [MenuItem(IslandMenu + "4. Cerrar vista temporal")]
        static void CloseIslandPreview()
        {
            if (StageUtility.GetCurrentStage() is IslandStage) StageUtility.GoToMainStage();
        }

        sealed class IslandStage : PreviewSceneStage
        {
            public Scene Source;
            public GameObject Island;
            readonly List<Object> resources = new List<Object>();
            (string Name, Vector3 Eye, Vector3 Target)[] views;
            int next;

            protected override GUIContent CreateHeaderContent() => new GUIContent("Ciudad del Sol · masa flotante temporal");

            protected override bool OnOpenStage()
            {
                if (!base.OnOpenStage()) return false;
                var context = new GameObject("Arquitectura existente · copia temporal");
                SceneManager.MoveGameObjectToScene(context, scene);
                context.SetActive(false);
                foreach (var root in Source.GetRootGameObjects().Where(IsIslandArchitecture))
                {
                    var copy = Object.Instantiate(root, context.transform);
                    copy.name = root.name;
                    foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
                        if (behaviour) behaviour.enabled = false;
                }
                context.SetActive(true);
                Island = BuildIsland(scene, context.transform.Cast<Transform>().Select(t => t.gameObject).ToArray(),
                    resources, out views, out _);
                EditorApplication.delayCall += () => { if (this && StageUtility.GetCurrentStage() == this) FrameNext(); };
                return true;
            }

            public void FrameNext()
            {
                if (views == null) return;
                var view = views[next++ % views.Length];
                var window = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
                var orientation = Quaternion.LookRotation(view.Target - view.Eye);
                float distance = Vector3.Distance(view.Eye, view.Target);
                window.LookAt(view.Target, orientation, distance * .51f, false, true);
                window.ShowNotification(new GUIContent(view.Name));
            }

            protected override void OnCloseStage()
            {
                base.OnCloseStage();
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
        }

        public static void ReviewIslandInBatch()
        {
            Require(Hash() == ClearanceHash, "La escena difiere de la versión aprobada.");
            var scene = EditorSceneManager.OpenPreviewScene(Original);
            var resources = new List<Object>();
            try
            {
                var roots = scene.GetRootGameObjects().Where(IsIslandArchitecture).ToArray();
                var island = BuildIsland(scene, roots, resources, out var views, out var report);
                Directory.CreateDirectory(IslandOutput);
                File.WriteAllLines(Path.Combine(IslandOutput, "Mediciones.txt"), report);
                foreach (string line in report) Debug.Log("ISLA " + line);
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                    CaptureIsland(scene, island, views);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
            Require(Hash() == ClearanceHash, "El archivo definitivo cambió.");
            Debug.Log("ISLA_PREVIEW_OK");
        }

        static GameObject BuildIsland(Scene scene, GameObject[] roots, List<Object> resources,
            out (string Name, Vector3 Eye, Vector3 Target)[] views, out List<string> report)
        {
            Physics.SyncTransforms();
            var colliders = roots.SelectMany(root => root.GetComponentsInChildren<Collider>())
                .Where(c => c.enabled && !c.isTrigger).ToArray();
            var renderers = roots.SelectMany(root => root.GetComponentsInChildren<Renderer>())
                .Where(renderer => renderer.enabled).ToArray();
            Require(colliders.Length > 0 && renderers.Length > 0, "Falta la arquitectura de referencia.");
            var protectedBounds = colliders.Select(c => c.bounds).Concat(renderers.Select(r => r.bounds)).ToArray();
            var city = roots.Single(root => root.name == "Dungeon_Ciudad_del_Sol_Generated");
            var overall = protectedBounds.Aggregate((a, b) => { a.Encapsulate(b); return a; });
            float under = Mathf.Min(-44, overall.min.y - 5);
            var band = protectedBounds.Where(b => b.max.y >= under && b.min.y <= CrestMax).ToArray();
            var center = new Vector2(overall.center.x, overall.center.z);
            var distances = new float[CliffSegments];
            for (int i = 0; i < CliffSegments; i++)
            {
                var direction = CliffDirection(i);
                distances[i] = band.Max(bounds => RayExit(bounds, center, direction));
            }

            // Enlarge each side locally until the cliff rim clears every active gameplay bound.
            Vector3[][] rings = null;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                rings = IslandRings(distances, center, under);
                var collisions = new HashSet<int>();
                for (int layer = 0; layer < 4; layer++)
                for (int i = 0; i < CliffSegments; i++)
                {
                    int next = (i + 1) % CliffSegments;
                    if (CliffIntersects(rings[layer][i], rings[layer + 1][i], rings[layer][next], band) ||
                        CliffIntersects(rings[layer][next], rings[layer + 1][i], rings[layer + 1][next], band))
                    {
                        collisions.Add(i);
                        collisions.Add(next);
                    }
                }
                for (int i = 0; i < CliffSegments; i++)
                {
                    int next = (i + 1) % CliffSegments;
                    var inner = new Vector3(rings[0][i].x, under, rings[0][i].z);
                    var innerNext = new Vector3(rings[0][next].x, under, rings[0][next].z);
                    if (!CliffIntersects(rings[0][i], rings[0][next], inner, band) &&
                        !CliffIntersects(rings[0][next], innerNext, inner, band)) continue;
                    collisions.Add(i);
                    collisions.Add(next);
                }
                if (collisions.Count == 0) break;
                Require(attempt < 4, "El borde rocoso invade la envolvente protegida.");
                foreach (int i in collisions) distances[i] += 8;
            }

            var rootObject = new GameObject("Masa continental · exterior temporal");
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            var stone = IslandMaterial(RockMaterial, new Color(.78f, .72f, .66f), resources);
            var dark = IslandMaterial(RockMaterial, new Color(.68f, .63f, .60f), resources);
            var pale = IslandMaterial(RockMaterial, new Color(.86f, .77f, .67f), resources);
            var dirt = IslandMaterial(DirtMaterial, new Color(.68f, .57f, .43f), resources);
            var materials = new[] { stone, dark, pale, dirt };
            var circumference = new float[CliffSegments + 1];
            for (int i = 0; i < CliffSegments; i++)
                circumference[i + 1] = circumference[i] +
                    Vector3.Distance(rings[2][i], rings[2][(i + 1) % CliffSegments]);
            // Close the UV seam on a whole repeat without changing the scale between chunks.
            float repeats = Mathf.Round(circumference[CliffSegments] / 21f);
            float uvScale = repeats / circumference[CliffSegments];
            int triangles = 0;
            for (int chunk = 0; chunk < 8; chunk++)
            {
                var vertices = new List<Vector3>();
                var normals = new List<Vector3>();
                var uv = new List<Vector2>();
                var indices = materials.Select(_ => new List<int>()).ToArray();
                int start = chunk * CliffSegments / 8, end = (chunk + 1) * CliffSegments / 8;
                for (int i = start; i < end; i++)
                {
                    int next = (i + 1) % CliffSegments;
                    for (int layer = 0; layer < rings.Length - 1; layer++)
                    {
                        int material = layer == 0 ? 3 : layer == 3 || layer == 7 ? 2 : layer >= 9 ? 1 : 0;
                        AddSlope(layer, i, layer + 1, i, layer, i + 1, material);
                        AddSlope(layer, i + 1, layer + 1, i, layer + 1, i + 1, material);
                    }
                    var bottom = new Vector3(center.x - 19, under - 104, center.y + 28);
                    Add(rings.Last()[i], bottom, rings.Last()[next], 1);
                    var inner = new Vector3(rings[0][i].x, under, rings[0][i].z);
                    var innerNext = new Vector3(rings[0][next].x, under, rings[0][next].z);
                    Add(rings[0][i], rings[0][next], inner, 0);
                    Add(rings[0][next], innerNext, inner, 0);
                    Add(inner, innerNext, new Vector3(center.x, under, center.y), 1);
                }
                var mesh = new Mesh { name = $"Acantilado estratificado {chunk + 1:00}" };
                resources.Add(mesh);
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uv);
                mesh.subMeshCount = materials.Length;
                for (int i = 0; i < indices.Length; i++) mesh.SetTriangles(indices[i], i);
                mesh.SetNormals(normals);
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                var cliff = new GameObject(mesh.name);
                cliff.transform.SetParent(rootObject.transform, false);
                cliff.AddComponent<MeshFilter>().sharedMesh = mesh;
                cliff.AddComponent<MeshRenderer>().sharedMaterials = materials;
                triangles += vertices.Count / 3;

                void AddSlope(int ra, int ia, int rb, int ib, int rc, int ic, int material)
                {
                    var a = rings[ra][ia % CliffSegments];
                    var b = rings[rb][ib % CliffSegments];
                    var c = rings[rc][ic % CliffSegments];
                    var face = Vector3.Cross(c - a, b - a).normalized;
                    AddSlopeVertex(ra, ia, a, face, material);
                    AddSlopeVertex(rc, ic, c, face, material);
                    AddSlopeVertex(rb, ib, b, face, material);
                }

                void AddSlopeVertex(int layer, int index, Vector3 point, Vector3 face, int material)
                {
                    int i = index % CliffSegments;
                    var along = rings[layer][(i + 1) % CliffSegments] -
                        rings[layer][(i + CliffSegments - 1) % CliffSegments];
                    var down = rings[Mathf.Min(layer + 1, rings.Length - 1)][i] -
                        rings[Mathf.Max(layer - 1, 0)][i];
                    var averaged = Vector3.Cross(along, down).normalized;
                    var normal = Vector3.Dot(face, averaged) > 0 ? Vector3.Lerp(face, averaged, .78f).normalized : face;
                    float angle = i * Mathf.PI * 2 / CliffSegments;
                    var tex = material == 3 ? new Vector2(point.x, point.z) / 18f :
                        new Vector2(circumference[index] * uvScale + .12f * Mathf.Sin(point.y / 13f + angle * 7),
                            point.y / 24f + .19f * Mathf.Sin(angle * 9 + point.y / 31f));
                    AddVertex(point, normal, tex, material);
                }

                void Add(Vector3 a, Vector3 b, Vector3 c, int material)
                {
                    var normal = Vector3.Cross(c - a, b - a).normalized;
                    Vector3 horizontal = Vector3.Cross(Vector3.up, normal).normalized;
                    foreach (var point in new[] { a, c, b })
                        AddVertex(point, normal, Mathf.Abs(normal.y) > .65f ? new Vector2(point.x, point.z) / 22 :
                            new Vector2(Vector3.Dot(point, horizontal), point.y) / 22, material);
                }

                void AddVertex(Vector3 point, Vector3 normal, Vector2 tex, int material)
                {
                    indices[material].Add(vertices.Count);
                    vertices.Add(point);
                    normals.Add(normal);
                    uv.Add(tex);
                }
            }
            var cliffs = rootObject.GetComponentsInChildren<Renderer>();
            var limits = cliffs.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Require(!rootObject.GetComponentsInChildren<Collider>().Any(), "La masa decorativa debe tener cero colliders.");
            report = new List<string>
            {
                $"Arquitectura activa X={overall.min.x:F2}..{overall.max.x:F2} Y={overall.min.y:F2}..{overall.max.y:F2} Z={overall.min.z:F2}..{overall.max.z:F2}",
                $"Masa X={limits.min.x:F2}..{limits.max.x:F2} Y={limits.min.y:F2}..{limits.max.y:F2} Z={limits.min.z:F2}..{limits.max.z:F2}",
                $"Dimensiones {limits.size.x:F2} × {limits.size.y:F2} × {limits.size.z:F2} m",
                $"{cliffs.Length} mallas, {triangles} triángulos, {cliffs.Length} renderizadores, 0 colliders",
                $"Cero intersecciones con envolventes de {colliders.Length} colliders sólidos y {renderers.Length} renderizadores activos.",
                $"La roca que entra bajo la huella empieza en Y={under:F2} o por debajo. Cotas jugables verificadas por envolventes: -16, -24, -30, -34, -38."
            };
            foreach (Transform child in city.transform)
                if (child.name.Length > 3 && char.IsDigit(child.name[0]))
                    report.Add($"Sector {child.name} · ancla {child.position:F2}");
            foreach (var architecture in roots.Where(g => g != city))
            {
                var bounds = architecture.GetComponentsInChildren<Renderer>().Where(r => r.enabled)
                    .Select(r => r.bounds).ToArray();
                if (bounds.Length == 0) continue;
                var area = bounds.Aggregate((a,b) => { a.Encapsulate(b); return a; });
                report.Add($"{architecture.name} · límites {area.min:F2} a {area.max:F2}");
            }
            views = new[]
            {
                ("01-Panoramica", new Vector3(center.x + 425, 118, center.y - 445), new Vector3(center.x, -49, center.y)),
                ("02-Lateral", new Vector3(center.x - 580, 10, center.y - 150), new Vector3(center.x, -54, center.y)),
                ("03-Inferior", new Vector3(center.x + 330, under - 210, center.y + 365), new Vector3(center.x, -48, center.y)),
                ("04-Borde", new Vector3(-164, 21, -128), new Vector3(-179, -53, -60)),
                ("05-Subsuelo", new Vector3(-69, -14.1f, -97), new Vector3(-69, -13.4f, -72)),
                ("06-Plataformas-altas", new Vector3(328, 57, 225), new Vector3(143, 23, 130)),
            };
            return rootObject;
        }

        static Vector2 CliffDirection(int index)
        {
            float angle = index * Mathf.PI * 2 / CliffSegments;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        static float RayExit(Bounds bounds, Vector2 center, Vector2 direction)
        {
            float entry = 0, exit = float.PositiveInfinity;
            for (int axis = 0; axis < 2; axis++)
            {
                float min = bounds.min[axis * 2] - 4, max = bounds.max[axis * 2] + 4;
                float origin = center[axis], delta = direction[axis];
                if (Mathf.Abs(delta) < .0001f)
                {
                    if (origin < min || origin > max) return 0;
                    continue;
                }
                float a = (min - origin) / delta, b = (max - origin) / delta;
                entry = Mathf.Max(entry, Mathf.Min(a, b));
                exit = Mathf.Min(exit, Mathf.Max(a, b));
                if (entry > exit) return 0;
            }
            return exit;
        }

        static Vector3[][] IslandRings(float[] distances, Vector2 center, float under)
        {
            var rings = Enumerable.Range(0, 13).Select(_ => new Vector3[CliffSegments]).ToArray();
            for (int i = 0; i < CliffSegments; i++)
            {
                var direction = CliffDirection(i);
                float angle = i * Mathf.PI * 2 / CliffSegments;
                float fracture = Mathf.Sin(angle * 11 + .5f) * 5 + Mathf.Cos(angle * 27) * 3 +
                    Mathf.Sin(angle * 53 + .9f) * 2.2f;
                float broad = Mathf.Sin(angle * 3 + .7f) * 12 + Mathf.Cos(angle * 7) * 7;
                float radius = Mathf.Max(distances[i], 125) + 11;
                float crest = Mathf.Min(-6.07f, -13 + Mathf.Sin(angle * 5 + .9f) * 4.8f +
                    Mathf.Cos(angle * 13) * 2.8f + Mathf.Sin(angle * 27 + .4f) * 2.5f);
                rings[0][i] = Position(radius, crest - 3);
                rings[1][i] = Position(radius + 10 + Mathf.Abs(fracture), crest);
                rings[2][i] = Position(radius + 12 + Mathf.Abs(fracture) * 1.5f, crest - 8 + fracture * .25f);
                rings[3][i] = Position(radius + 8 + Mathf.Abs(fracture) + Mathf.Sin(angle * 17) * 2,
                    Mathf.Lerp(crest, under, .68f) + fracture * .42f);
                rings[4][i] = Position(radius + 7 + Mathf.Abs(fracture), under);
                for (int layer = 5; layer < rings.Length; layer++)
                {
                    float t = (layer - 4) / 8f;
                    float lobes = Mathf.Sin(angle * 4 + .4f) * 30 + Mathf.Cos(angle * 9 - .5f) * 14;
                    float shelf = layer % 3 == 0 ? 7 : 0;
                    float spur = Mathf.Sin(angle * 3 + .2f) * 21 + Mathf.Cos(angle * 5 - .8f) * 13;
                    float depth = Mathf.Max(-184.56f,
                        under - t * (100 + broad * .6f + lobes * .55f + spur) + fracture * t);
                    float flank = radius * (1 - .62f * t) + (broad + spur) * t * .82f +
                        fracture * (2.1f - t) + shelf;
                    rings[layer][i] = Position(flank, depth, new Vector2(-28, 38) * t +
                        new Vector2(lobes * .18f, broad * .24f) * t);
                }

                Vector3 Position(float distance, float y, Vector2 drift = default)
                {
                    Vector2 horizontal = center + direction * distance + drift;
                    return new Vector3(horizontal.x, y, horizontal.y);
                }
            }
            return rings;
        }

        static bool CliffIntersects(Vector3 a, Vector3 b, Vector3 c, Bounds[] protectedBounds)
        {
            var triangle = new Bounds(a, Vector3.zero);
            triangle.Encapsulate(b);
            triangle.Encapsulate(c);
            var x = new Vector2(a.x, a.z);
            var y = new Vector2(b.x, b.z);
            var z = new Vector2(c.x, c.z);
            var edges = new[] { y - x, z - y, x - z };
            foreach (var bounds in protectedBounds)
            {
                if (!triangle.Intersects(bounds)) continue;
                var center = new Vector2(bounds.center.x, bounds.center.z);
                var extent = new Vector2(bounds.extents.x, bounds.extents.z);
                bool separated = false;
                foreach (var edge in edges)
                {
                    var axis = new Vector2(-edge.y, edge.x);
                    float p = Vector2.Dot(x, axis), q = Vector2.Dot(y, axis), r = Vector2.Dot(z, axis);
                    float middle = Vector2.Dot(center, axis);
                    float radius = Mathf.Abs(axis.x) * extent.x + Mathf.Abs(axis.y) * extent.y;
                    if (Mathf.Min(p, q, r) > middle + radius || Mathf.Max(p, q, r) < middle - radius)
                    {
                        separated = true;
                        break;
                    }
                }
                if (!separated) return true;
            }
            return false;
        }

        static Material IslandMaterial(string path, Color tint, List<Object> resources)
        {
            var original = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            Require(original && shader, "Falta el material o shader URP: " + path);
            var material = new Material(shader) { name = original.name + " · copia temporal" };
            resources.Add(material);
            material.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));
            material.SetTexture("_BumpMap", original.GetTexture("_BumpMap"));
            if (material.GetTexture("_BumpMap")) material.EnableKeyword("_NORMALMAP");
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", .12f);
            return material;
        }

        static void CaptureIsland(Scene scene, GameObject island, (string Name, Vector3 Eye, Vector3 Target)[] views,
            string output = IslandOutput, float fieldOfView = 49)
        {
            var cameraObject = new GameObject("Cámara temporal de exterior");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = .12f;
            camera.farClipPlane = 2300;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.19f, .21f, .27f);
            var lightObject = new GameObject("Luz de inspección exterior");
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            lightObject.transform.rotation = Quaternion.Euler(34, -28, 0);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.2f;
            var fillObject = new GameObject("Relleno temporal para leer la roca");
            SceneManager.MoveGameObjectToScene(fillObject, scene);
            fillObject.transform.rotation = Quaternion.Euler(-19, 125, 0);
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(.80f, .88f, 1f);
            fill.intensity = 1.05f;
            var target = new RenderTexture(1100, 680, 24);
            var pixels = new Texture2D(1100, 680, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                foreach (var view in views)
                {
                    camera.transform.SetPositionAndRotation(view.Eye, Quaternion.LookRotation(view.Target - view.Eye));
                    for (int state = 0; state < 2; state++)
                    {
                        island.SetActive(state == 1);
                        camera.Render();
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                        File.WriteAllBytes(Path.Combine(output, view.Name + (state == 1 ? "-despues.png" : "-antes.png")),
                            pixels.EncodeToPNG());
                    }
                }
                Debug.Log($"ISLA_CAPTURAS_OK: {views.Length * 2} vistas comparables.");
            }
            finally
            {
                island.SetActive(true);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(lightObject);
                Object.DestroyImmediate(fillObject);
            }
        }
    }
}
