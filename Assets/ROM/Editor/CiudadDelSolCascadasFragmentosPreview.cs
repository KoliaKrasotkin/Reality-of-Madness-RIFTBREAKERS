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
        const string DecorHash = "76C0325774BB8AA98384F97B378879EF2DD319EFD4CAC645DE313C7250B0F015";
        const string DecorOutput = "Backups/CiudadDelSol/CascadasYFragmentos";
        const string DecorMenu = IslandMenu + "Cascadas y fragmentos/";
        const string Natural = "Assets/Standard Assets/Sun_Temple/Prefabs/Natural/";
        const string SunCommon = "Assets/Standard Assets/Sun_Temple/Prefabs/Modules_Common/";
        const string Temple = "Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string RiftCrystal = "Assets/ROM/Art/RiftCrystals/Prefabs/";
        const string Ruins = "Assets/Standard Assets/Eternal Temple/Prefabs/Damaged/";

        [MenuItem(DecorMenu + "1. Abrir propuesta temporal")]
        static void OpenExteriorDecor()
        {
            var source = SceneManager.GetActiveScene();
            Require(source.path == Original && !source.isDirty && Hash() == DecorHash &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                StageUtility.GetCurrentStage() == StageUtility.GetMainStage(), "Abrí la escena definitiva limpia en modo edición.");
            var stage = ScriptableObject.CreateInstance<ExteriorDecorStage>();
            stage.Source = source;
            StageUtility.GoToStage(stage, true);
        }

        [MenuItem(DecorMenu + "2. Siguiente vista")]
        static void NextExteriorDecor()
        {
            if (StageUtility.GetCurrentStage() is ExteriorDecorStage stage) stage.Frame();
        }

        [MenuItem(DecorMenu + "3. Alternar antes y después")]
        static void ToggleExteriorDecor()
        {
            if (StageUtility.GetCurrentStage() is ExteriorDecorStage stage && stage.Decor)
                stage.Decor.SetActive(!stage.Decor.activeSelf);
        }

        [MenuItem(DecorMenu + "4. Cerrar sin guardar")]
        static void CloseExteriorDecor()
        {
            if (StageUtility.GetCurrentStage() is ExteriorDecorStage) StageUtility.GoToMainStage();
        }

        sealed class ExteriorDecorStage : PreviewSceneStage
        {
            public Scene Source;
            public GameObject Decor;
            readonly List<Object> resources = new List<Object>();
            int next;
            protected override GUIContent CreateHeaderContent() => new GUIContent("Ciudad del Sol · cascadas y fragmentos");
            protected override bool OnOpenStage()
            {
                if (!base.OnOpenStage()) return false;
                var context = new GameObject("Ciudad y masa · copia temporal");
                SceneManager.MoveGameObjectToScene(context, scene);
                context.SetActive(false);
                foreach (var root in Source.GetRootGameObjects().Where(ExteriorContext))
                {
                    var copy = Object.Instantiate(root, context.transform);
                    copy.name = root.name;
                    foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>(true))
                        if (b) b.enabled = false;
                }
                context.SetActive(true);
                Decor = BuildExteriorDecor(scene, context.transform.Cast<Transform>().Select(t => t.gameObject).ToArray(), resources);
                EditorApplication.delayCall += () => { if (this && StageUtility.GetCurrentStage() == this) Frame(); };
                return true;
            }
            public void Frame()
            {
                var views = ExteriorDecorViews();
                var view = views[next++ % views.Length];
                var window = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
                window.LookAt(view.Target, Quaternion.LookRotation(view.Target - view.Eye),
                    Vector3.Distance(view.Target, view.Eye) * .46f, false, true);
                window.ShowNotification(new GUIContent(view.Name));
            }
            protected override void OnCloseStage()
            {
                base.OnCloseStage();
                foreach (var item in resources) if (item) Object.DestroyImmediate(item);
            }
        }

        static bool ExteriorContext(GameObject root) => IsIslandArchitecture(root) || root.name == IslandMount;

        [MenuItem(DecorMenu + "5. Validar y capturar panorámicas")]
        static void CaptureExteriorDecorFromEditor()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Capturá en modo edición.");
            ReviewExteriorDecorInBatch();
        }

        static (string Name, Vector3 Eye, Vector3 Target)[] ExteriorDecorViews() => new[]
        {
            ("01-Panoramica-sur", new Vector3(525, 125, -470), new Vector3(28, -55, 12)),
            ("02-Panoramica-occidental", new Vector3(-520, 65, -210), new Vector3(20, -62, 4)),
            ("03-Panoramica-noreste", new Vector3(490, 95, 520), new Vector3(33, -52, 20)),
            ("04-Lateral-cercano-occidental", new Vector3(-258, -29, -202), new Vector3(-188, -48, -120)),
            ("05-Lateral-cercano-sur", new Vector3(158, -31, -274), new Vector3(112, -54, -169)),
            ("06-Panoramica-norte", new Vector3(-305, 157, 675), new Vector3(30, -38, 25)),
            ("07-Panoramica-este", new Vector3(730, 118, 156), new Vector3(30, -42, 18)),
            ("08-Vista-inferior", new Vector3(-490, -355, -470), new Vector3(27, -75, 14))
        };

        public static void ReviewExteriorDecorInBatch()
        {
            Require(Hash() == DecorHash, "La escena definitiva no coincide con la masa aprobada.");
            var scene = EditorSceneManager.OpenPreviewScene(Original);
            var resources = new List<Object>();
            try
            {
                Directory.CreateDirectory(DecorOutput);
                var decor = BuildExteriorDecor(scene, scene.GetRootGameObjects().Where(ExteriorContext).ToArray(), resources);
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                    CaptureIsland(scene, decor, ExteriorDecorViews(), DecorOutput, 51);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var item in resources) if (item) Object.DestroyImmediate(item);
            }
            Require(Hash() == DecorHash, "La escena original cambió.");
            Debug.Log("EXTERIOR_PREVIEW_OK");
        }

        static GameObject BuildExteriorDecor(Scene scene, GameObject[] context, List<Object> resources,
            List<string> integrationReport = null)
        {
            var mass = context.Single(r => r.name == IslandMount);
            var massFilters = mass.GetComponentsInChildren<MeshFilter>();
            Require(massFilters.Length == 8, "Faltan mallas de la masa aprobada.");
            var rockVerts = massFilters.SelectMany(f => f.sharedMesh.vertices.Select(f.transform.TransformPoint)).Distinct().ToArray();
            var overall = mass.GetComponentsInChildren<MeshRenderer>().Select(r => r.bounds)
                .Aggregate((a,b) => { a.Encapsulate(b); return a; });
            var center = new Vector3(overall.center.x, 0, overall.center.z);
            Physics.SyncTransforms();
            var protectedBounds = context.Where(r => r != mass).SelectMany(r =>
                r.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).Select(c => c.bounds)
                .Concat(r.GetComponentsInChildren<Renderer>().Where(v => v.enabled).Select(v => v.bounds))).ToArray();
            Require(protectedBounds.Length > 1000, "Falta contexto de arquitectura.");

            // These query colliders are removed before the preview is shown. They belong to proxy objects only.
            var root = new GameObject("Cascadas y fragmentos · propuesta temporal");
            SceneManager.MoveGameObjectToScene(root, scene);
            var query = new List<MeshCollider>();
            foreach (var filter in massFilters)
            {
                var proxy = new GameObject("Consulta temporal de roca");
                proxy.transform.SetParent(root.transform, false);
                proxy.transform.SetPositionAndRotation(filter.transform.position, filter.transform.rotation);
                proxy.transform.localScale = filter.transform.lossyScale;
                var collider = proxy.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                query.Add(collider);
            }
            Physics.SyncTransforms();
            var waterOriginal = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/ROM/Levels/FracturedForest/Materials/CoastalCascade.mat");
            Require(waterOriginal && waterOriginal.shader.name == "ROM/Coastal Cascade", "Falta CoastalCascade URP.");
            var waterShader = Shader.Find("ROM/Ciudad del Sol/Coastal Cascade Preview");
            var spray = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/ROM/Levels/FracturedForest/Materials/CoastalImpactSpray.mat");
            Require(waterShader && waterShader.isSupported && spray && spray.GetFloat("_Mist") == 1,
                "Falta el shader URP de agua o el material de espuma.");
            var water = new Material(waterOriginal) { name = "Cascada aérea · copia temporal", shader = waterShader };
            resources.Add(water);
            water.SetColor("_BaseColor", new Color(.43f, .62f, .72f, .96f));
            water.SetFloat("_FlowSpeed", 1.18f);
            var report = new List<string>();
            var fallBounds = new List<Bounds>();
            var mouths = new List<(Vector3 point, float width)>();
            var falls = new[]
            {
                (angle:207f, width:24f, height:124f, category:"Monumental", search:14),
                (angle:243f, width:5f, height:78f, category:"Pequeña", search:9),
                (angle:286f, width:29f, height:154f, category:"Monumental", search:14),
                (angle:344f, width:11f, height:106f, category:"Mediana", search:10),
                (angle:33f, width:25f, height:132f, category:"Monumental", search:14),
                (angle:120f, width:11f, height:93f, category:"Mediana", search:10),
                (angle:190f, width:12f, height:94f, category:"Mediana", search:9),
                (angle:63f, width:13f, height:110f, category:"Mediana", search:11),
                (angle:305f, width:5f, height:66f, category:"Pequeña", search:7),
                (angle:148f, width:6f, height:73f, category:"Pequeña", search:9)
            };
            foreach (var spec in falls)
            {
                bool placedFall = false;
                foreach (float width in new[] { spec.width, spec.width - 2, spec.width - 4 }
                    .Where(w => w >= Mathf.Max(4, spec.width * .78f)))
                {
                    var candidates = Enumerable.Range(-spec.search, spec.search * 2 + 1)
                        .Select(i => TryFall(spec.angle + i * 1.1f, spec.angle, width, spec.height))
                        .Where(v => v.points != null).OrderBy(v => v.score);
                    foreach (var candidate in candidates)
                    {
                        if (!AddFall(candidate.points, candidate.side, width, spec.height, spec.category)) continue;
                        mouths.Add((candidate.points[0], width));
                        placedFall = true;
                        break;
                    }
                    if (placedFall) break;
                }
                Require(placedFall, $"No hay caída libre cerca de {spec.angle} grados.");
            }

            float RockFront(Vector3 outward, Vector3 side, float across, float y)
            {
                var ray = new Ray(center + outward * 460 + side * across + Vector3.up * y, -outward);
                float radius = 0;
                foreach (var piece in query)
                    if (piece.Raycast(ray, out var hit, 465)) radius = Mathf.Max(radius, 460 - hit.distance);
                return radius;
            }

            (Vector3[] points, Vector3 side, float score) TryFall(float angle, float anchor, float width, float height)
            {
                var outward = Direction(angle);
                var side = Vector3.Cross(Vector3.up, outward);
                float yTop = -25;
                for (float y = -6; y >= -25; y -= .5f)
                    if (RockFront(outward, side, 0, y) > 0) { yTop = y; break; }
                var count = Mathf.CeilToInt(height / 1.6f) + 1;
                var points = new Vector3[count];
                float radius = 0, launch = 0;
                for (int j = 0; j < count; j++)
                {
                    float y = yTop - height * j / (count - 1);
                    float w = WidthAt(width, j / (float)(count - 1));
                    for (int across = -3; across <= 3; across++)
                    for (int dy = 0; dy < 2; dy++)
                        radius = Mathf.Max(radius, RockFront(outward, side, across * w / 6, y - dy * .8f));
                    if (j == 0)
                    {
                        if (radius < 120) return default;
                        launch = radius;
                    }
                    // Let the sheet clear successive protrusions before continuing down into the void.
                    points[j] = center + outward * (radius + .85f + 3f * j / (count - 1)) + Vector3.up * y;
                    if (radius - launch > (width >= 18 ? 18 : 12)) return default;
                }
                // Carry protrusions forward slightly before the sheet reaches them.
                for (int j = points.Length - 2; j >= 0; j--)
                {
                    float jump = Vector3.Dot(points[j + 1] - points[j], outward);
                    if (jump > 1.75f) points[j] += outward * (jump - 1.75f);
                }
                var corridor = new Bounds(points[0], Vector3.zero);
                foreach (var point in points) corridor.Encapsulate(point);
                corridor.Expand(width * 2);
                var nearby = protectedBounds.Where(b => b.Intersects(corridor)).ToArray();
                for (int j = 0; j < points.Length - 1; j++)
                {
                    float w = WidthAt(width, j / (float)(count - 1)) * .5f;
                    float w2 = WidthAt(width, (j + 1) / (float)(count - 1)) * .5f;
                    var mid = (points[j] + points[j + 1]) * .5f;
                    for (int lane = -2; lane <= 2; lane++)
                        if (RockFront(outward, side, (w + w2) * lane / 4f, mid.y) >
                            Vector3.Dot(mid - center, outward) - .15f) return default;
                    var a = points[j] - side * w;
                    var b = points[j] + side * w;
                    var c = points[j + 1] - side * w2;
                    var d = points[j + 1] + side * w2;
                    if (CliffIntersects(a,b,c,nearby) || CliffIntersects(b,d,c,nearby)) return default;
                }
                float separation = Vector3.Dot(points[0] - center, outward) - launch;
                if (separation > (width >= 18 ? 7f : 4f) || mouths.Any(m =>
                    Vector2.Distance(new Vector2(m.point.x, m.point.z), new Vector2(points[0].x, points[0].z)) <
                    (m.width + width) * .65f + 6)) return default;
                return (points, side, separation * 2 + radius - launch + Mathf.Abs(angle - anchor) * .65f);
            }

            bool AddFall(Vector3[] points, Vector3 side, float width, float height, string category)
            {
                var vertices = new List<Vector3>();
                var uv = new List<Vector2>();
                var colors = new List<Color>();
                var indices = new List<int>();
                var foam = new List<int>();
                var outward = Vector3.Cross(side, Vector3.up);
                int fallIndex = fallBounds.Count;
                int strands = width >= 18 ? 6 : width >= 9 ? 4 : 3;
                int contacts = 0;
                for (int strand = 0; strand < strands; strand++)
                {
                    float seed = .13f + (fallIndex * 3 + strand * 7) % 17 / 23f;
                    float lane = (strand + .5f) / strands - .5f;
                    float across = lane * width;
                    float lipY = points[0].y;
                    float lip = 0;
                    // Each channel finds its own rock contact instead of sharing a straight mouth.
                    for (float y = lipY + 1f; y >= lipY - 6f; y -= .5f)
                    {
                        lip = RockFront(outward, side, across, y);
                        if (lip <= 0) continue;
                        lipY = y;
                        contacts++;
                        break;
                    }
                    if (lip <= 0) continue;
                    int start = vertices.Count;
                    var entry = center + outward * (lip - .28f) + side * across + Vector3.up * lipY;
                    for (int j = 0; j < points.Length; j++)
                    {
                        float t = j / (float)(points.Length - 1);
                        float blend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, 6f / height, t));
                        float localAcross = width * (lane * (1 + t * .20f) +
                            Mathf.Sin(t * 13 + seed * 16) * .035f * blend);
                        float span = width / strands * (.81f + seed * .34f +
                            .16f * Mathf.Sin(t * 21 + seed * 10) + .09f * Mathf.Sin(t * 53 + seed * 8));
                        var free = points[j] + side * localAcross + outward *
                            (.35f + seed * .53f + .27f * Mathf.Sin(t * 12 + seed * 8));
                        var axis = Vector3.Lerp(entry - Vector3.up * (height * t), free, blend);
                        for (int k = 0; k < 5; k++)
                        {
                            float u = k / 4f;
                            float depth = Mathf.Sin(u * Mathf.PI) * span * .28f;
                            vertices.Add(axis + side * ((u - .5f) * span) + outward * depth);
                            uv.Add(new Vector2(u, t));
                            colors.Add(new Color(seed, 1 - blend, 0, .86f + seed * .13f));
                            if (k == 4 || j == points.Length - 1) continue;
                            int i = start + j * 5 + k;
                            indices.AddRange(new[] {i,i+1,i+5,i+1,i+6,i+5});
                        }
                    }
                }
                if (contacts < strands - 1) return false;
                AddSpray(points[Mathf.Min(3, points.Length - 1)] + outward * .9f, width * .56f, width * .46f, .78f);
                if (width >= 18)
                {
                    AddSpray(points[2] + outward * 1.2f - side * width * .29f, width * .40f, width * .27f, .76f);
                    AddSpray(points[4] + outward * 1.6f + side * width * .27f, width * .39f, width * .32f, .68f);
                }
                if (width >= 9)
                    AddSpray(points[Mathf.RoundToInt((points.Length - 1) * .76f)] + outward, width * .65f, width * 1.24f, .44f);

                var waterBounds = new Bounds(vertices[0], Vector3.zero);
                foreach (var vertex in vertices) waterBounds.Encapsulate(vertex);
                var nearby = protectedBounds.Where(b => b.Intersects(waterBounds)).ToArray();
                foreach (var group in new[] { indices, foam })
                for (int i = 0; i < group.Count; i += 3)
                    if (CliffIntersects(vertices[group[i]], vertices[group[i+1]], vertices[group[i+2]], nearby))
                        return false;

                var mesh = new Mesh { name = "Corrientes, espuma y dispersión · geometría temporal" };
                resources.Add(mesh);
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uv);
                mesh.SetColors(colors);
                mesh.subMeshCount = 2;
                mesh.SetTriangles(indices, 0);
                mesh.SetTriangles(foam, 1);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var child = new GameObject($"Cascada {fallBounds.Count + 1:00}");
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = new[] { water, spray };
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                fallBounds.Add(renderer.bounds);
                report.Add($"{child.name} ({category}): boca {points[0]:F1}, caída {height:F0} m, ancho {width:F0} m, {contacts} canales con contacto.");

                void AddSpray(Vector3 pivot, float w, float h, float opacity)
                {
                    foreach (var horizontal in new[] { side, (side * .68f + outward * .73f).normalized })
                    {
                        int first = vertices.Count;
                        for (int k = 0; k < 4; k++)
                        {
                            float x = k % 2, y = k / 2;
                            vertices.Add(pivot + horizontal * ((x - .5f) * w) + Vector3.up * ((y - .5f) * h));
                            uv.Add(new Vector2(x, y));
                            colors.Add(new Color(1,1,1,opacity));
                        }
                        foam.AddRange(new[] {first,first+1,first+2,first+2,first+1,first+3});
                    }
                }
                return true;
            }

            var designs = new List<(float angle, float offset, float y, float size, string rock, string ruin, int tier)>
            {
                (172, 88, -75, 63, "Rock_Long", "Building_Ruins_04", 2),
                (180, 115, -105, 11, "Rock_Boulder_C", "", 0),
                (225, 41, -50, 9, "Rock_Facing_D", "", 0),
                (246, 59, -104, 29, "Rock_Facing_A", "Column_Broken_02", 1),
                (270, 101, -85, 72, "Rock_Long", "Building_Ruins_08", 2),
                (275, 132, -128, 14, "Rock_Boulder_A", "", 0),
                (320, 60, -63, 33, "Rock_Facing_C", "Broken_Wall_05", 1),
                (335, 48, -99, 10, "Rock_Short", "", 0),
                (9, 65, -74, 30, "Rock_Facing_B", "Column_Broken_01", 1),
                (49, 50, -111, 12, "Rock_Boulder_B", "", 0),
                (82, 92, -72, 58, "Rock_Long", "Building_Ruins_04", 2),
                (90, 120, -109, 10, "Rock_Facing_E", "", 0),
                (131, 62, -73, 27, "Rock_Short", "Column_Broken_03", 1),
                (158, 52, -115, 9, "Rock_Boulder_A", "", 0)
            };
            designs.AddRange(new (float, float, float, float, string, string, int)[]
            {
                (25, 107, -103, 58, "Rock_Long", "Building_Ruins_08", 2),
                (113, 98, -109, 62, "Rock_Long", "Building_Ruins_04", 2),
                (198, 116, -130, 66, "Rock_Long", "Building_Ruins_08", 2),
                (234, 107, -49, 52, "Rock_Long", "Building_Ruins_04", 2),
                (330, 116, -116, 68, "Rock_Long", "Building_Ruins_08", 2),
                (14, 46, -141, 28, "Rock_Facing_B", "Column_Broken_02", 1),
                (50, 68, -61, 30, "Rock_Short", "@Bases/Thin/Arch_06_Broken_01", 1),
                (100, 53, -136, 31, "Rock_Facing_E", "@Staircases/Staircase_Broken_01", 1),
                (152, 75, -52, 29, "Rock_Facing_C", "Broken_Wall_05", 1),
                (185, 69, -38, 33, "Rock_Short", "@Road and Plaza/Road_End_01", 1),
                (221, 57, -126, 28, "Rock_Facing_A", "@Bases/Base_Arch_Broken_01", 1),
                (292, 73, -47, 34, "Rock_Facing_D", "@Staircases/Staircase_Broken_01", 1),
                (352, 60, -129, 29, "Rock_Facing_B", "Column_Broken_03", 1),
                (10, 96, -27, 28, "Rock_Short", "Fol_TallTree_A", 3),
                (59, 104, -33, 26, "Rock_Long", "Fol_Pine_B", 3),
                (96, 102, 18, 21, "Rock_Short", "Fol_TallTree_B", 3),
                (125, 81, -34, 24, "Rock_Facing_E", "Fol_Pine_A", 3),
                (163, 95, -19, 26, "Rock_Long", "Fol_TallTree_A", 3),
                (205, 95, -24, 27, "Rock_Short", "Fol_TallTree_B", 3),
                (242, 81, -91, 25, "Rock_Long", "Fol_Pine_A", 3),
                (263, 101, 8, 23, "Rock_Short", "Fol_Pine_B", 3),
                (311, 89, -87, 28, "Rock_Long", "Fol_TallTree_B", 3),
                (347, 94, -22, 24, "Rock_Short", "Fol_TallTree_A", 3),
                (18, 55, -102, 16, "Rock_Boulder_C", "CrystalGround", 4),
                (36, 100, 31, 11, "", "CrystalAerial", 4),
                (74, 64, -132, 22, "Rock_Short", "CrystalGround", 4),
                (108, 126, -55, 15, "", "CrystalAerial", 4),
                (138, 66, -113, 18, "Rock_Boulder_A", "CrystalGround", 4),
                (166, 135, 49, 11, "", "CrystalAerial", 4),
                (193, 70, -119, 16, "Rock_Facing_D", "CrystalGround", 4),
                (219, 119, 16, 15, "", "CrystalAerial", 4),
                (258, 57, -146, 19, "Rock_Short", "CrystalGround", 4),
                (284, 132, 49, 12, "", "CrystalAerial", 4),
                (307, 46, -135, 17, "Rock_Facing_C", "CrystalGround", 4),
                (338, 101, 26, 14, "", "CrystalAerial", 4),
                (2, 68, -51, 13, "Rock_Boulder_C", "", 0),
                (31, 67, -147, 11, "Rock_Boulder_B", "", 0),
                (68, 76, -90, 13, "Rock_Facing_D", "", 0),
                (105, 69, -20, 8, "Rock_Boulder_C", "", 0),
                (139, 109, -124, 12, "Rock_Boulder_A", "", 0),
                (174, 62, -138, 15, "Rock_Facing_C", "", 0),
                (194, 87, -69, 11, "Rock_Boulder_B", "", 0),
                (226, 73, -154, 13, "Rock_Short", "", 0),
                (252, 63, -60, 9, "Rock_Facing_E", "", 0),
                (300, 82, -125, 12, "Rock_Boulder_A", "", 0),
                (326, 70, -49, 10, "Rock_Boulder_C", "", 0)
            });
            var placed = new List<Bounds>();
            var cliffStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/ROM/CiudadDelSolMasaFlotante/Roca.mat");
            var cliffShade = AssetDatabase.LoadAssetAtPath<Material>("Assets/ROM/CiudadDelSolMasaFlotante/RocaOscura.mat");
            Require(cliffStone && cliffShade, "Faltan los materiales de la masa integrada.");
            for (int i = 0; i < designs.Count; i++)
            {
                var d = designs[i];
                var outward = Direction(d.angle);
                var side = Vector3.Cross(Vector3.up, outward);
                // A separating plane uses the full footprint, including the farthest cliff spur.
                float front = rockVerts.Max(v => Vector3.Dot(v - center, outward));
                string sizeClass = d.tier == 4 ? "cristal" : d.tier == 3 ? "vegetación" :
                    d.tier == 2 ? "monumental" : d.tier == 1 ? "mediano" : "pequeño";
                var cluster = new GameObject($"Fragmento {i + 1:00} · {sizeClass}");
                cluster.transform.SetParent(root.transform, false);
                cluster.transform.position = center + outward * (front + d.offset) + Vector3.up * d.y;
                var rockGroup = new GameObject("Estratos de roca");
                rockGroup.transform.SetParent(cluster.transform, false);
                var core = d.rock == "" ? null : AddRock(d.rock, d.size, Vector3.zero,
                    new Vector3(d.tier == 2 ? 18 + i * 2 : 34 * Mathf.Sin(i * 3.2f), i * 111f, 31 * Mathf.Cos(i * 1.8f)),
                    cliffStone);
                if (d.tier == 1 || d.tier == 2 || d.tier == 3)
                {
                    float handedness = i % 3 == 0 ? -1 : 1;
                    AddRock("Rock_Facing_C", d.size * .68f, d.size * (side * (.19f * handedness) + Vector3.up * .10f),
                        new Vector3(13, i * 83 + 41, -24), cliffStone);
                    if (d.tier == 2)
                    {
                        AddRock("Rock_Facing_E", d.size * .57f, d.size * (-side * (.22f * handedness) - outward * .10f),
                            new Vector3(-21, i * 79 + 56, 37), cliffStone);
                        AddRock("Rock_Short", d.size * .54f, d.size * (outward * .10f - Vector3.up * .27f),
                            new Vector3(53, i * 71 + 19, -33), cliffShade);
                        AddRock("Rock_Boulder_C", d.size * .27f, d.size * (outward * .20f + side * .16f - Vector3.up * .08f),
                            new Vector3(31, 53 + i * 11, -17), cliffShade);
                    }
                }

                var anchor = core ? PropBounds(core).center : cluster.transform.position;
                if (d.tier == 4 && !core)
                {
                    var crystal = AddProp(RiftCrystal + d.ruin + ".prefab", cluster.transform,
                        Quaternion.Euler(i % 2 == 0 ? 33 : -23, i * 91, i % 3 * 17 - 20));
                    ResizeProp(crystal, d.size);
                    crystal.transform.position += anchor - PropBounds(crystal).center;
                }
                else if (d.tier == 4)
                    SeatRuin(RiftCrystal + d.ruin, anchor, i * 68f, 1f, d.size * .88f);
                else if (d.tier == 3)
                {
                    SeatRuin(Natural + d.ruin, anchor, i * 68f, 1f, d.size * .51f);
                    SeatRuin(Natural + "Fol_Bush_A", anchor + side * d.size * .17f, i * 41f, 1f, d.size * .15f);
                }
                else if (d.ruin != "")
                {
                    var path = d.ruin.StartsWith("@", StringComparison.Ordinal) ? Temple + d.ruin.Substring(1) : Ruins + d.ruin;
                    SeatRuin(path, anchor, i * 68f, d.tier == 2 ? 1.45f : 1.2f, 0,
                        i == 14 || i == 21 ? 66 : i == 16 || i == 20 || i == 24 ? -21 : 0);
                }
                if (d.tier == 2)
                    SeatRuin(SunCommon + "Bld_Deco_ShortColumn_A",
                        anchor + side * d.size * .15f, 49 + i * 37, 1.35f, 0, i >= 14 ? i % 2 == 0 ? 68 : -21 : 0);
                ConsolidateRuin(rockGroup, resources, 2);
                var bounds = PropBounds(cluster);
                for (int step = 0; step < 16 && (protectedBounds.Any(b => b.Intersects(bounds)) ||
                     fallBounds.Any(b => b.Intersects(bounds)) || placed.Any(b => b.Intersects(bounds)) ||
                     bounds.extents.magnitude + front + 3 > Vector3.Dot(bounds.center - center, outward)); step++)
                {
                    cluster.transform.position += outward * 8;
                    bounds = PropBounds(cluster);
                }
                Require(!protectedBounds.Any(b => b.Intersects(bounds)) && !fallBounds.Any(b => b.Intersects(bounds)) &&
                    !placed.Any(b => b.Intersects(bounds)) &&
                    bounds.extents.magnitude + front + 3 <= Vector3.Dot(bounds.center - center, outward),
                    "Fragmento sin separación: " + i);
                placed.Add(bounds);
                float gap = Vector3.Dot(bounds.center - center, outward) - front -
                    Mathf.Abs(outward.x) * bounds.extents.x - Mathf.Abs(outward.z) * bounds.extents.z;
                report.Add($"{cluster.name}: {d.rock} {d.ruin} · centro {bounds.center:F1} · tamaño {bounds.size:F1} m · separación proyectada {gap:F1} m.");

                GameObject AddRock(string prefab, float size, Vector3 offset, Vector3 rotation, Material stone)
                {
                    var rock = AddProp(Natural + prefab + ".prefab", rockGroup.transform, Quaternion.Euler(rotation));
                    ResizeProp(rock, size);
                    rock.transform.position += cluster.transform.position + offset - PropBounds(rock).center;
                    foreach (var renderer in rock.GetComponentsInChildren<MeshRenderer>())
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => stone).ToArray();
                    return rock;
                }

                void SeatRuin(string path, Vector3 anchor, float yaw, float scale, float size = 0, float tilt = 0)
                {
                    var ruin = AddProp(path + ".prefab", cluster.transform, Quaternion.Euler(tilt, yaw, 0));
                    ruin.transform.localScale *= scale;
                    if (size > 0) ResizeProp(ruin, size);
                    var bounds = PropBounds(ruin);
                    var rockFilters = rockGroup.GetComponentsInChildren<MeshFilter>();
                    var supports = new List<MeshCollider>();
                    try
                    {
                        foreach (var filter in rockFilters)
                        {
                            var proxy = new GameObject("Consulta temporal de apoyo");
                            proxy.transform.SetParent(filter.transform, false);
                            var collider = proxy.AddComponent<MeshCollider>();
                            collider.sharedMesh = filter.sharedMesh;
                            supports.Add(collider);
                        }
                        Physics.SyncTransforms();
                        float top = float.NegativeInfinity;
                        Vector3 seat = anchor;
                        for (int ring = 0; ring <= 4 && float.IsNegativeInfinity(top); ring++)
                        for (int sample = 0; sample < (ring == 0 ? 1 : 8) && float.IsNegativeInfinity(top); sample++)
                        {
                            seat = anchor + Direction(sample * 45f) * (d.size * .055f * ring);
                            var ray = new Ray(seat + Vector3.up * 95, Vector3.down);
                            foreach (var support in supports)
                                if (support.Raycast(ray, out var hit, 190))
                                    top = Mathf.Max(top, hit.point.y);
                        }
                        Require(!float.IsNegativeInfinity(top), "No hay roca debajo de " + path);
                        ruin.transform.position += new Vector3(seat.x - bounds.center.x,
                            top - bounds.min.y - Mathf.Min(2.5f, bounds.size.y * .22f), seat.z - bounds.center.z);
                    }
                    finally
                    {
                        foreach (var support in supports) Object.DestroyImmediate(support.gameObject);
                    }
                    ConsolidateRuin(ruin, resources);
                }
            }

            foreach (var collider in query) Object.DestroyImmediate(collider.gameObject);
            var filters = root.GetComponentsInChildren<MeshFilter>();
            var renderers = root.GetComponentsInChildren<Renderer>();
            int triangles = filters.Sum(f => f.sharedMesh ? Enumerable.Range(0, f.sharedMesh.subMeshCount)
                .Sum(s => (int)f.sharedMesh.GetIndexCount(s) / 3) : 0);
            Require(root.GetComponentsInChildren<Collider>(true).Length == 0 &&
                root.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "La decoración debe ser puramente visual.");
            Require(placed.Count == 60, "El inventario de fragmentos no suma 60.");
            report.Add($"{fallBounds.Count} cascadas; {placed.Count} fragmentos; " +
                $"{renderers.Length} renderizadores; {triangles} triángulos (incluye LOD si existe); " +
                $"{renderers.SelectMany(r => r.sharedMaterials).Distinct().Count()} materiales; 0 colliders; 0 emisores; 0 luces.");
            report.Add($"Protección: {protectedBounds.Length} envolventes de arquitectura; cortinas verificadas por triángulo.");
            foreach (var line in report) Debug.Log("EXTERIOR " + line);
            if (integrationReport != null) integrationReport.AddRange(report);
            else if (Directory.Exists(DecorOutput)) File.WriteAllLines(Path.Combine(DecorOutput, "Mediciones.txt"), report);
            return root;
        }

        static void ResizeProp(GameObject item, float size)
        {
            var dimensions = PropBounds(item).size;
            item.transform.localScale *= size / Mathf.Max(dimensions.x, dimensions.y, dimensions.z);
        }

        static Vector3 Direction(float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
        }

        static float WidthAt(float width, float t) =>
            width * (1 + t * .36f + Mathf.Sin(t * 9) * .07f) *
            Mathf.Lerp(1f, .045f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.79f, 1f, t)));

        static void ConsolidateRuin(GameObject ruin, List<Object> resources, int minimum = 5)
        {
            var renderers = ruin.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length < minimum) return;
            var lodRenderers = new HashSet<Renderer>(ruin.GetComponentsInChildren<LODGroup>()
                .SelectMany(l => l.GetLODs()).SelectMany(l => l.renderers));
            var merged = new List<MeshRenderer>();
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var renderer in renderers)
            {
                var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                var materials = renderer.sharedMaterials;
                if (!renderer.enabled || lodRenderers.Contains(renderer) || !mesh || materials.Length != 1 ||
                    mesh.subMeshCount != 1 || !materials[0] || materials[0].renderQueue >= 2500)
                    continue;
                merged.Add(renderer);
                var material = materials[0];
                if (!groups.TryGetValue(material, out var group))
                {
                    group = new List<CombineInstance>();
                    groups.Add(material, group);
                }
                group.Add(new CombineInstance { mesh = mesh,
                    transform = ruin.transform.worldToLocalMatrix * renderer.localToWorldMatrix });
            }
            if (merged.Count <= groups.Count) return;
            foreach (var group in groups)
            {
                var mesh = new Mesh { name = ruin.name + " · " + group.Key.name, indexFormat = IndexFormat.UInt32 };
                resources.Add(mesh);
                mesh.CombineMeshes(group.Value.ToArray(), true, true);
                Require(mesh.vertexCount > 0, "La malla agrupada de la ruina está vacía.");
                var part = new GameObject(mesh.name);
                part.transform.SetParent(ruin.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = group.Key;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (var renderer in merged)
            {
                Object.DestroyImmediate(renderer.GetComponent<MeshFilter>());
                Object.DestroyImmediate(renderer);
            }
        }

        static Bounds PropBounds(GameObject item) => item.GetComponentsInChildren<Renderer>()
            .Select(r => r.bounds).Aggregate((a,b) => { a.Encapsulate(b); return a; });

        static GameObject AddProp(string path, Transform parent, Quaternion rotation)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(prefab, "Falta el prefab " + path);
            var prop = Object.Instantiate(prefab, parent);
            prop.name = prefab.name;
            prop.transform.localRotation = rotation;
            foreach (var behaviour in prop.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
            foreach (var collider in prop.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            foreach (var renderer in prop.GetComponentsInChildren<Renderer>())
            {
                Require(renderer.sharedMaterials.All(m => m && m.shader && m.shader.name != "Hidden/InternalErrorShader"),
                    "Material no disponible: " + path);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            return prop;
        }
    }
}
