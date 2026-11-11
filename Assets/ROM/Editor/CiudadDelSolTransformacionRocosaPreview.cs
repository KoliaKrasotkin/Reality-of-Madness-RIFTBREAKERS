using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string GeologyMenu = IslandMenu + "Transformación rocosa/";
        const string GeologyOutput = "Backups/CiudadDelSol/TransformacionRocosa";

        [MenuItem(GeologyMenu + "1. Abrir propuesta temporal")]
        static void OpenGeology()
        {
            var source = SceneManager.GetActiveScene();
            if (source.path != Original || source.isDirty || EditorApplication.isPlayingOrWillChangePlaymode ||
                StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
            {
                EditorUtility.DisplayDialog("Transformación rocosa", "Abrí Dungeon_Isla_del_Sol limpia, en modo edición.", "Aceptar");
                return;
            }
            var stage = ScriptableObject.CreateInstance<GeologyStage>();
            stage.Source = source;
            StageUtility.GoToStage(stage, true);
        }

        [MenuItem(GeologyMenu + "2. Siguiente vista")]
        static void NextGeology()
        {
            if (StageUtility.GetCurrentStage() is GeologyStage stage) stage.Frame();
        }

        [MenuItem(GeologyMenu + "3. Alternar antes y después")]
        static void ToggleGeology()
        {
            if (StageUtility.GetCurrentStage() is GeologyStage stage) stage.Toggle();
        }

        [MenuItem(GeologyMenu + "4. Cerrar sin guardar")]
        static void CloseGeology()
        {
            if (StageUtility.GetCurrentStage() is GeologyStage) StageUtility.GoToMainStage();
        }

        sealed class GeologyStage : PreviewSceneStage
        {
            public Scene Source;
            readonly List<Object> resources = new List<Object>();
            GeologyLook look;
            int view;
            bool after = true;

            protected override GUIContent CreateHeaderContent() => new GUIContent("Ciudad del Sol · transformación rocosa");

            protected override bool OnOpenStage()
            {
                if (!base.OnOpenStage()) return false;
                var context = new GameObject("Ciudad del Sol · copia de inspección");
                SceneManager.MoveGameObjectToScene(context, scene);
                context.SetActive(false);
                foreach (var root in Source.GetRootGameObjects().Where(r => IsIslandArchitecture(r) ||
                    r.name == IslandMount || r.name == FinalSeamRoot || r.name == ExteriorMount ||
                    r.name == FinalLookRoot || r.name.StartsWith("Subsuelo", StringComparison.Ordinal)))
                {
                    var clone = Object.Instantiate(root, context.transform);
                    clone.name = root.name;
                    foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
                        if (behaviour && !(behaviour is Volume)) behaviour.enabled = false;
                }
                context.SetActive(true);
                look = BuildGeology(context.transform.Cast<Transform>().Select(t => t.gameObject).ToArray(), resources);
                look.Set(true);
                EditorApplication.delayCall += () => { if (this && StageUtility.GetCurrentStage() == this) Frame(); };
                return true;
            }

            public void Frame()
            {
                var views = GeologyViews();
                var shot = views[view++ % views.Length];
                var window = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
                window.sceneLighting = true;
                window.LookAt(shot.target, Quaternion.LookRotation(shot.target - shot.eye),
                    Vector3.Distance(shot.eye, shot.target) * .47f, false, true);
                window.ShowNotification(new GUIContent(shot.name));
            }

            public void Toggle()
            {
                if (look == null) return;
                after = !after;
                look.Set(after);
                SceneView.RepaintAll();
            }

            protected override void OnCloseStage()
            {
                base.OnCloseStage();
                foreach (var obj in resources) if (obj) Object.DestroyImmediate(obj);
            }
        }

        sealed class GeologyLook
        {
            public readonly List<(MeshFilter filter, Mesh before, Mesh after, MeshRenderer renderer, Material[] original, Material[] proposed)> Changes =
                new List<(MeshFilter, Mesh, Mesh, MeshRenderer, Material[], Material[])>();
            public string Report;

            public void Set(bool after)
            {
                foreach (var part in Changes)
                {
                    part.filter.sharedMesh = after ? part.after : part.before;
                    part.renderer.sharedMaterials = after ? part.proposed : part.original;
                }
            }
        }

        static GeologyLook BuildGeology(GameObject[] roots, List<Object> resources)
        {
            var island = roots.Single(r => r.name == IslandMount);
            var seam = roots.Single(r => r.name == FinalSeamRoot);
            var exterior = roots.Single(r => r.name == ExteriorMount);
            var cliffs = island.GetComponentsInChildren<MeshFilter>();
            var seams = seam.GetComponentsInChildren<MeshFilter>();
            Require(cliffs.Length == 8 && seams.Length == 8 && !island.GetComponentsInChildren<Collider>(true).Any() &&
                !seam.GetComponentsInChildren<Collider>(true).Any(), "La masa o la unión integrada difieren de las esperadas.");
            Require(exterior.transform.childCount == 70 &&
                exterior.transform.Cast<Transform>().Count(t => t.name.StartsWith("Cascada ", StringComparison.Ordinal)) == 10,
                "Faltan las cascadas y fragmentos integrados.");
            var waterAndDebris = exterior.transform.Cast<Transform>().Select(t =>
                t.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.bounds).ToArray())
                .Where(b => b.Length > 0).Select(b => b.Aggregate((a, p) => { a.Encapsulate(p); return a; })).ToArray();
            var architecture = roots.Where(IsIslandArchitecture).SelectMany(r => r.GetComponentsInChildren<Renderer>())
                .Where(r => r.enabled).Select(r => r.bounds).ToArray();
            Require(architecture.Length > 0, "No se encontró arquitectura protegida.");
            float cutoff = Mathf.Min(-44, architecture.Min(b => b.min.y) - 2);
            const int detail = 3;
            float verticalReach = 0;
            foreach (var filter in cliffs)
            {
                var mesh = filter.sharedMesh;
                var vertices = mesh.vertices;
                var indices = mesh.triangles;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    float a = vertices[indices[i]].y, b = vertices[indices[i + 1]].y, c = vertices[indices[i + 2]].y;
                    verticalReach = Mathf.Max(verticalReach, (Mathf.Max(a, b, c) - Mathf.Min(a, b, c)) / detail);
                }
            }
            float reliefStart = cutoff - verticalReach - .02f;
            var limits = cliffs.Select(f => f.GetComponent<Renderer>().bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            var look = new GeologyLook();
            var minerals = new[]
            {
                IslandMaterial(RockMaterial, new Color(.56f, .62f, .72f), resources),
                IslandMaterial(RockMaterial, new Color(.45f, .51f, .61f), resources),
                IslandMaterial(RockMaterial, new Color(.64f, .67f, .73f), resources),
                IslandMaterial(RockMaterial, new Color(.54f, .55f, .59f), resources)
            };
            string[] names = { "Pizarra fría", "Roca mineral profunda", "Estrato gris", "Cresta erosionada" };
            for (int i = 0; i < minerals.Length; i++)
            {
                minerals[i].name = names[i] + " · temporal";
                minerals[i].SetFloat("_BumpScale", i == 3 ? .82f : 1.13f);
            }
            var seamCopies = new Dictionary<Material, Material>();
            int triangles = 0, sourceTriangles = 0, moved = 0;
            float maxInset = 0;
            foreach (var filter in cliffs)
            {
                Require(filter.transform.localToWorldMatrix == Matrix4x4.identity, "La masa requiere coordenadas mundiales originales.");
                var renderer = filter.GetComponent<MeshRenderer>();
                var source = filter.sharedMesh;
                var vertices = source.vertices;
                var originalNormals = source.normals;
                var tex = source.uv;
                var points = new List<Vector3>();
                var normals = new List<Vector3>();
                var uv = new List<Vector2>();
                var slots = minerals.Select(_ => new List<int>()).ToArray();
                for (int slot = 0; slot < source.subMeshCount; slot++)
                {
                    var idx = source.GetTriangles(slot);
                    sourceTriangles += idx.Length / 3;
                    for (int i = 0; i < idx.Length; i += 3)
                    {
                        int a = idx[i], b = idx[i + 1], c = idx[i + 2];
                        for (int x = 0; x < detail; x++)
                        for (int y = 0; y < detail - x; y++)
                        {
                            Emit(x, y, x + 1, y, x, y + 1);
                            if (x + y < detail - 1) Emit(x + 1, y, x + 1, y + 1, x, y + 1);
                        }

                        void Emit(int x, int y, int j, int k, int m, int n)
                        {
                            var p = At(x, y); var q = At(j, k); var r = At(m, n);
                            var tri = new Bounds(p.after, Vector3.zero);
                            tri.Encapsulate(q.after); tri.Encapsulate(r.after);
                            bool movedTriangle = p.before != p.after || q.before != q.after || r.before != r.after;
                            if (movedTriangle)
                                Require(tri.max.y < cutoff && !waterAndDebris.Any(box =>
                                    tri.Intersects(box) &&
                                    CliffIntersects(p.after, q.after, r.after, new[] { box }) &&
                                    !CliffIntersects(p.before, q.before, r.before, new[] { box })), "El relieve invade agua o fragmentos.");
                            Vector3 normal = Vector3.Cross(q.after - p.after, r.after - p.after).normalized;
                            var center = (p.after + q.after + r.after) / 3;
                            float fracture = Mathf.PerlinNoise(center.x * .026f + 10, center.z * .021f + 32);
                            float fold = Mathf.Sin((center.y + center.x * .14f - center.z * .06f) * .24f + fracture * 4);
                            int mineral = slot == 3 ? 3 : fold > .68f && fracture > .43f ? 2 :
                                (fold < -.66f && fracture < .64f || slot == 1 && fracture < .55f) ? 1 : 0;
                            Add(p); Add(q); Add(r);
                            triangles++;

                            void Add((Vector3 before, Vector3 after, Vector3 smooth, Vector2 uv) v)
                            {
                                if (v.before != v.after) moved++;
                                maxInset = Mathf.Max(maxInset, Vector3.Distance(v.before, v.after));
                                slots[mineral].Add(points.Count);
                                points.Add(v.after);
                                normals.Add(Vector3.Lerp(v.smooth, normal, .38f).normalized);
                                uv.Add(v.uv);
                            }
                        }

                        (Vector3 before, Vector3 after, Vector3 smooth, Vector2 uv) At(int x, int y)
                        {
                            float s = x / (float)detail, t = y / (float)detail, w = 1 - s - t;
                            var p = vertices[a] * w + vertices[b] * s + vertices[c] * t;
                            var normal = (originalNormals[a] * w + originalNormals[b] * s + originalNormals[c] * t).normalized;
                            return (p, Relief(p), normal, tex[a] * w + tex[b] * s + tex[c] * t);
                        }
                    }
                }

                var mesh = new Mesh { name = source.name + " · roca fracturada" };
                resources.Add(mesh);
                mesh.SetVertices(points);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uv);
                mesh.subMeshCount = minerals.Length;
                for (int i = 0; i < slots.Length; i++) mesh.SetTriangles(slots[i], i);
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                look.Changes.Add((filter, source, mesh, renderer, renderer.sharedMaterials, minerals));
            }

            foreach (var filter in seams)
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                var original = renderer.sharedMaterials;
                var proposed = original.Select(m =>
                {
                    if (seamCopies.TryGetValue(m, out var copy)) return copy;
                    copy = new Material(m) { name = m.name + " · encuentro mineral temporal" };
                    var tint = m.GetColor("_BaseColor");
                    copy.SetColor("_BaseColor", new Color(tint.r * .68f, tint.g * .87f, tint.b * 1.11f, tint.a));
                    resources.Add(copy);
                    seamCopies.Add(m, copy);
                    return copy;
                }).ToArray();
                look.Changes.Add((filter, filter.sharedMesh, filter.sharedMesh, renderer, original, proposed));
            }
            var newLimits = look.Changes.Take(cliffs.Length).Select(c => c.after.bounds)
                .Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Require((newLimits.min - limits.min).magnitude < .03f && (newLimits.max - limits.max).magnitude < .03f &&
                moved > 0 && triangles < 40000, "Cambió la envolvente aprobada o el relieve no pasó el presupuesto.");
            look.Report = $"Masa: {island.name}; unión: {seam.name}; {cliffs.Length} mallas de reemplazo temporal.\n" +
                $"Dimensiones X/Y/Z: {limits.size:F2} m; {sourceTriangles} -> {triangles} triángulos ({triangles - sourceTriangles} adicionales).\n" +
                $"0 objetos, 0 colliders y 0 renderizadores nuevos de roca; {minerals.Length} materiales compartidos y {seamCopies.Count} copias de materiales de encuentro.\n" +
                $"Relieve hacia dentro hasta {maxInset:F2} m; vértices modificados por debajo de Y={reliefStart:F2}.\n" +
                $"Se protegieron {architecture.Length} envolventes de arquitectura y {waterAndDebris.Length} conjuntos de cascadas y fragmentos.\n" +
                "Geometría de la unión y franja superior conservada. Se reutilizaron mapas albedo/normal de FracturedCliff.\n" +
                "Validación geométrica conservadora por envolventes; sin medición de GPU.";
            return look;

            Vector3 Relief(Vector3 p)
            {
                if (p.y >= reliefStart) return p;
                float edge = Mathf.Min(Mathf.Min(p.x - limits.min.x, limits.max.x - p.x),
                    Mathf.Min(p.z - limits.min.z, limits.max.z - p.z));
                float angle = Mathf.Atan2(p.z - limits.center.z, p.x - limits.center.x);
                var inward = new Vector3(limits.center.x - p.x, 0, limits.center.z - p.z).normalized;
                float vertical = Mathf.Sin(17 * angle + p.y * .038f + .7f * Mathf.Sin(6 * angle));
                float stratum = Mathf.Sin(p.y * .23f + Mathf.Sin(4 * angle) * 1.7f);
                float massif = 5.8f * Mathf.Max(0, Mathf.Sin(5 * angle + p.y * .017f));
                float depth = (1.4f + 7.6f * Mathf.Pow(Mathf.Abs(vertical), 3) + 2.2f * Mathf.Max(0, stratum) + massif) *
                    Mathf.SmoothStep(0, 1, (reliefStart - p.y) / 12) * Mathf.SmoothStep(0, 1, edge / 11);
                if (depth < .01f) return p;
                var next = p + inward * depth;
                var sweep = new Bounds(p, Vector3.one * 12f);
                sweep.Encapsulate(next - Vector3.one * 6f);
                sweep.Encapsulate(next + Vector3.one * 6f);
                if (waterAndDebris.Any(box => box.Intersects(sweep))) return p;
                return next;
            }
        }

        static (string name, Vector3 eye, Vector3 target)[] GeologyViews() => new[]
        {
            ("01-Isla-completa", new Vector3(525, 125, -470), new Vector3(28, -53, 12)),
            ("02-Acantilado-oeste", new Vector3(-419, -49, -215), new Vector3(-100, -76, -18)),
            ("03-Parte-inferior", new Vector3(-485, -285, -440), new Vector3(27, -85, 14)),
            ("04-Union-roca-murallas", new Vector3(354, 18, -259), new Vector3(147, -19, -84)),
            ("05-Franja-entre-niveles", new Vector3(-307, -1, 61), new Vector3(-115, -14, 21))
        };

        public static void ReviewGeologyInBatch()
        {
            var hash = Hash();
            var scene = EditorSceneManager.OpenPreviewScene(Original);
            var resources = new List<Object>();
            try
            {
                ValidateFinalLook(scene);
                var look = BuildGeology(scene.GetRootGameObjects(), resources);
                Directory.CreateDirectory(GeologyOutput);
                File.WriteAllText(Path.Combine(GeologyOutput, "Validacion.txt"), look.Report + "\nSHA-256: " + hash);
                var cameraObject = new GameObject("Cámara geológica temporal");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.fieldOfView = 51;
                camera.nearClipPlane = .12f;
                camera.farClipPlane = 3000;
                camera.allowHDR = true;
                var options = camera.GetUniversalAdditionalCameraData();
                options.requiresDepthTexture = true;
                options.renderPostProcessing = true;
                options.volumeLayerMask = 1 << PreviewVolumeLayer;
                var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGBHalf);
                var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                resources.Add(target);
                resources.Add(pixels);
                var previous = RenderTexture.active;
                try
                {
                    camera.targetTexture = target;
                    foreach (var shot in GeologyViews())
                    {
                        camera.transform.SetPositionAndRotation(shot.eye, Quaternion.LookRotation(shot.target - shot.eye));
                        for (int pass = 0; pass < 2; pass++)
                        {
                            look.Set(pass == 1);
                            camera.Render();
                            RenderTexture.active = target;
                            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                            File.WriteAllBytes(Path.Combine(GeologyOutput, shot.name + (pass == 1 ? "-despues.png" : "-antes.png")), pixels.EncodeToPNG());
                        }
                    }
                }
                finally { RenderTexture.active = previous; }
                look.Set(false);
                ValidateFinalLook(scene);
                Require(Hash() == hash, "Cambió la escena original.");
                Debug.Log("GEOLOGIA_PREVIEW_OK " + look.Report);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var obj in resources) if (obj) Object.DestroyImmediate(obj);
            }
        }
    }
}

