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
        const string RockCoverMenu = IslandMenu + "Cobertura de rocas/";
        const string RockCoverOutput = "Backups/CiudadDelSol/CoberturaRocosa";
        const string RockCoverRoot = "Cobertura rocosa · temporal";
        const string SunRocks = "Assets/Standard Assets/Sun_Temple/Prefabs/Natural/";

        [MenuItem(RockCoverMenu + "1. Abrir propuesta temporal")]
        static void OpenRockCover() => OpenRockCover(false);

        static void OpenRockCover(bool ruins, bool ambiance = false)
        {
            var source = SceneManager.GetActiveScene();
            if (source.path != Original || source.isDirty || EditorApplication.isPlayingOrWillChangePlaymode ||
                StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
            {
                EditorUtility.DisplayDialog("Cobertura de rocas", "Abrí Dungeon_Isla_del_Sol limpia, en modo edición.", "Aceptar");
                return;
            }

            var stage = ScriptableObject.CreateInstance<RockCoverStage>();
            stage.Source = source;
            stage.Ruins = ruins;
            stage.Ambiance = ambiance;
            StageUtility.GoToStage(stage, true);
        }

        [MenuItem(RockCoverMenu + "2. Siguiente vista")]
        static void NextRockCover()
        {
            if (StageUtility.GetCurrentStage() is RockCoverStage stage) stage.Frame();
        }

        [MenuItem(RockCoverMenu + "3. Alternar antes y después")]
        static void ToggleRockCover()
        {
            if (StageUtility.GetCurrentStage() is RockCoverStage stage) stage.Toggle();
        }

        [MenuItem(RockCoverMenu + "4. Cerrar sin guardar")]
        static void CloseRockCover()
        {
            if (StageUtility.GetCurrentStage() is RockCoverStage) StageUtility.GoToMainStage();
        }

        sealed class RockCoverStage : PreviewSceneStage
        {
            public Scene Source;
            public bool Ruins;
            public bool Ambiance;
            readonly List<Object> resources = new List<Object>();
            GameObject cover;
            (string name, Vector3 eye, Vector3 target)[] views;
            int view;

            protected override GUIContent CreateHeaderContent() => new GUIContent(Ruins ?
                "Ciudad del Sol · ruinas exteriores" : "Ciudad del Sol · cobertura rocosa");

            protected override bool OnOpenStage()
            {
                if (!base.OnOpenStage()) return false;
                var context = new GameObject("Ciudad del Sol · copia de inspección");
                SceneManager.MoveGameObjectToScene(context, scene);
                context.SetActive(false);
                foreach (var root in Source.GetRootGameObjects().Where(r => IsIslandArchitecture(r) ||
                    r.name == IslandMount || r.name == FinalSeamRoot || r.name == ExteriorMount ||
                    r.name == FinalLookRoot || r.name == RockCoverMount))
                {
                    var clone = Object.Instantiate(root, context.transform);
                    clone.name = root.name;
                    foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
                        if (behaviour && !(behaviour is Volume)) behaviour.enabled = false;
                }
                context.SetActive(true);
                var roots = context.transform.Cast<Transform>().Select(t => t.gameObject).ToArray();
                if (Ruins) cover = BuildExteriorRuins(scene, roots, resources, out views, out _, ambiance: Ambiance);
                else
                {
                    cover = roots.SingleOrDefault(r => r.name == RockCoverMount);
                    if (!cover) cover = BuildRockCover(scene, roots, resources, out _);
                    views = RockCoverViews();
                }
                EditorApplication.delayCall += () => { if (this && StageUtility.GetCurrentStage() == this) Frame(); };
                return true;
            }

            public void Frame()
            {
                var shot = views[view++ % views.Length];
                var window = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
                window.sceneLighting = true;
                window.LookAt(shot.target, Quaternion.LookRotation(shot.target - shot.eye),
                    Vector3.Distance(shot.eye, shot.target) * .47f, false, true);
                window.ShowNotification(new GUIContent(shot.name));
            }

            public void ToggleLayer(string name)
            {
                var layer = scene.GetRootGameObjects().SingleOrDefault(r => r.name == AmbientRoot)?.transform.Find(name);
                if (!layer) return;
                layer.gameObject.SetActive(!layer.gameObject.activeSelf);
                SceneView.RepaintAll();
            }

            public void Toggle()
            {
                if (!cover) return;
                cover.SetActive(!cover.activeSelf);
                SceneView.RepaintAll();
            }

            protected override void OnCloseStage()
            {
                base.OnCloseStage();
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
        }

        static GameObject BuildRockCover(Scene scene, GameObject[] roots, List<Object> resources, out string report)
        {
            var mass = roots.Single(r => r.name == IslandMount);
            var seam = roots.Single(r => r.name == FinalSeamRoot);
            var exterior = roots.Single(r => r.name == ExteriorMount);
            var originalMeshes = mass.GetComponentsInChildren<MeshFilter>();
            Require(originalMeshes.Length == 8 && originalMeshes.All(f => EditorUtility.IsPersistent(f.sharedMesh)),
                "No se encontró la masa continental integrada.");
            var bounds = originalMeshes.Select(f => f.GetComponent<Renderer>().bounds)
                .Aggregate((a, b) => { a.Encapsulate(b); return a; });
            var playing = roots.Where(IsIslandArchitecture).SelectMany(r => r.GetComponentsInChildren<Collider>())
                .Where(c => c.enabled && !c.isTrigger).Select(c => c.bounds).ToArray();
            var architecture = roots.Where(IsIslandArchitecture).SelectMany(r => r.GetComponentsInChildren<Renderer>())
                .Where(r => r.enabled).Select(r => r.bounds).ToArray();
            Require(playing.Length > 0 && architecture.Length > 0, "No se encontraron las reservas de arquitectura.");
            var reserved = exterior.transform.Cast<Transform>().Select(t =>
                t.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.bounds).ToArray())
                .Where(b => b.Length > 0).Select(b => b.Aggregate((a, p) => { a.Encapsulate(p); return a; })).ToArray();
            Require(reserved.Length == 70, "Faltan cascadas o fragmentos exteriores.");
            var joins = seam.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.bounds).ToArray();

            string[] variants = { "Rock_Facing_A", "Rock_Facing_C", "Rock_Boulder_A", "Rock_Boulder_C", "Bgr_Cliff_C", "Rock_Long" };
            var meshes = variants.Select(name =>
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SunRocks + name + ".prefab");
                Require(prefab, "Falta roca de Sun Temple: " + name);
                var mesh = prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
                Require(mesh && mesh.subMeshCount == 1, "Malla de roca inesperada: " + name);
                return mesh;
            }).ToArray();
            var costs = meshes.Select(m => (int)m.GetIndexCount(0) / 3).ToArray();
            var materials = new[]
            {
                IslandMaterial(RockMaterial, new Color(.74f, .79f, .86f), resources),
                IslandMaterial(RockMaterial, new Color(.51f, .56f, .64f), resources),
                IslandMaterial(RockMaterial, new Color(.77f, .74f, .68f), resources),
                IslandMaterial(RockMaterial, new Color(.65f, .69f, .81f), resources)
            };
            foreach (var material in materials) material.enableInstancing = true;

            var sides = new List<(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, float cumulative)>();
            var underside = new List<(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, float cumulative)>();
            float sideArea = 0, undersideArea = 0;
            foreach (var part in originalMeshes)
            {
                var mesh = part.sharedMesh;
                var vertices = mesh.vertices.Select(part.transform.TransformPoint).ToArray();
                var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = vertices[triangles[i]];
                    var b = vertices[triangles[i + 1]];
                    var c = vertices[triangles[i + 2]];
                    var center = (a + b + c) / 3;
                    var cross = Vector3.Cross(b - a, c - a);
                    var area = cross.magnitude * .5f;
                    if (area < 1 || center.y > -10) continue;
                    var normal = cross.normalized;
                    var radial = Vector3.ProjectOnPlane(center - bounds.center, Vector3.up).normalized;
                    if (normal.y < -.32f && center.y < -52)
                    {
                        undersideArea += area;
                        underside.Add((a, b, c, normal, undersideArea));
                    }
                    else if (Vector3.Dot(normal, radial) > .28f && normal.y < .67f)
                    {
                        sideArea += area;
                        sides.Add((a, b, c, normal, sideArea));
                    }
                }
            }
            Require(sides.Count > 0 && underside.Count > 0, "Faltan caras exteriores muestreables.");

            var root = new GameObject(RockCoverRoot);
            SceneManager.MoveGameObjectToScene(root, scene);
            var groups = new Transform[2, 8];
            var random = new Random(731198);
            var positions = new List<(Vector3 center, float radius)>();
            int[] counts = new int[4];
            int[] zoneCounts = new int[2];
            int sumTriangles = 0, nearWater = 0, nearProtected = 0;
            var usedMeshes = new HashSet<Mesh>();
            Place(0, 58, 36, 32, 56, 16.5f);
            Place(1, 150, 90, 18, 34, 8.6f);
            Place(2, 185, 110, 9, 19, 4.0f);
            Place(3, 205, 118, 3.5f, 9.2f, 2.0f);
            Require(counts.Sum() >= 700 && zoneCounts[1] > 160 && root.GetComponentsInChildren<Collider>(true).Length == 0,
                "Cobertura incompleta o colliders inesperados.");
            report = $"Rocas individuales: {counts.Sum()} (gigantes {counts[0]}, grandes {counts[1]}, medianas {counts[2]}, pequeñas {counts[3]}).\n" +
                $"Laterales y bordes: {zoneCounts[0]}; parte inferior: {zoneCounts[1]}.\n" +
                $"Objetos totales con grupos: {root.GetComponentsInChildren<Transform>().Length}; renderizadores: {counts.Sum()}; triángulos instanciados: {sumTriangles}; colliders: 0.\n" +
                $"Mallas reutilizadas: {usedMeshes.Count}; materiales URP compartidos: {materials.Length}; comprobación estática por envolventes.\n" +
                $"Reservas: {playing.Length} colliders y {architecture.Length} renderizadores de arquitectura; {joins.Length} encuentros; {reserved.Length} conjuntos exteriores.\n" +
                $"Candidatas descartadas por cascadas o fragmentos: {nearWater}; por arquitectura o franja: {nearProtected}.\n" +
                "Las ocho mallas, los materiales de la masa y la unión rocosa original siguen asignados.\n" +
                string.Join("\n", variants.Select((name, i) => $"{name}: {costs[i]} triángulos por instancia."));
            return root;

            void Place(int tier, int sideTarget, int underTarget, float minSize, float maxSize, float spacing)
            {
                Populate(sides, sideArea, sideTarget, 0);
                Populate(underside, undersideArea, underTarget, 1);

                void Populate(List<(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, float cumulative)> surfaces,
                    float area, int target, int zone)
                {
                    int placed = 0;
                    for (int attempt = 0; attempt < target * 38 && placed < target; attempt++)
                    {
                        float sample = Next() * area;
                        int left = 0, right = surfaces.Count - 1;
                        while (left < right)
                        {
                            int mid = (left + right) / 2;
                            if (surfaces[mid].cumulative < sample) left = mid + 1;
                            else right = mid;
                        }
                        var face = surfaces[left];
                        float u = Mathf.Sqrt(Next()), v = Next();
                        var anchor = face.a * (1 - u) + face.b * (u * (1 - v)) + face.c * (u * v);
                        float patch = Mathf.PerlinNoise(anchor.x * .023f + 13 + anchor.y * .017f, anchor.z * .023f + 18);
                        if (tier >= 2 && Next() > Mathf.Lerp(.48f, 1, patch)) continue;
                        if (positions.Any(p => (p.center - anchor).sqrMagnitude < Mathf.Pow(Mathf.Min(p.radius, spacing), 2))) continue;

                        int variant = tier == 0 ? new[] { 0, 1, 4, 5 }[random.Next(4)] :
                            tier == 1 ? new[] { 0, 1, 2, 4 }[random.Next(4)] : tier == 2 ? random.Next(1, 4) : random.Next(2, 4);
                        var mesh = meshes[variant];
                        float size = Mathf.Lerp(minSize, maxSize, Mathf.Pow(Next(), 1.28f));
                        var shape = new Vector3(size * Mathf.Lerp(.71f, 1.14f, Next()),
                            size * Mathf.Lerp(.70f, 1.39f, Next()), size * Mathf.Lerp(.60f, 1.02f, Next()));
                        var scale = new Vector3(shape.x / mesh.bounds.size.x, shape.y / mesh.bounds.size.y, shape.z / mesh.bounds.size.z);
                        var normal = face.normal;
                        var rot = Quaternion.LookRotation(normal, Mathf.Abs(normal.y) > .93f ? Vector3.forward : Vector3.up) *
                            Quaternion.Euler(Mathf.Lerp(-23, 23, Next()), Mathf.Lerp(-27, 27, Next()), Next() * 360);
                        var extents = ScaledBounds(rot, shape * .5f);
                        float reach = Mathf.Abs(Vector3.Dot(normal, rot * Vector3.right)) * shape.x * .5f +
                            Mathf.Abs(Vector3.Dot(normal, rot * Vector3.up)) * shape.y * .5f +
                            Mathf.Abs(Vector3.Dot(normal, rot * Vector3.forward)) * shape.z * .5f;
                        var center = anchor - normal * (reach * Mathf.Lerp(-.12f, .26f, Next()));
                        var box = new Bounds(center, extents * 2);
                        if (box.max.y > -10 || joins.Any(b => b.Intersects(box)) ||
                            architecture.Any(b => b.Intersects(box)) || playing.Any(b => b.Intersects(box)))
                        {
                            nearProtected++;
                            continue;
                        }
                        var padded = box;
                        padded.Expand(1.5f);
                        if (reserved.Any(b => b.Intersects(padded)))
                        {
                            nearWater++;
                            continue;
                        }

                        float azimuth = Mathf.Atan2(anchor.z - bounds.center.z, anchor.x - bounds.center.x);
                        int sector = Mathf.Clamp(Mathf.FloorToInt((azimuth + Mathf.PI) * 8 / (2 * Mathf.PI)), 0, 7);
                        if (!groups[zone, sector])
                        {
                            var group = new GameObject($"{(zone == 0 ? "Acantilado" : "Fondo")} {sector + 1:00}");
                            group.transform.SetParent(root.transform, false);
                            groups[zone, sector] = group.transform;
                        }
                        var rock = new GameObject($"{variants[variant]} · {tier + 1}.{counts[tier] + 1:000}");
                        rock.transform.SetParent(groups[zone, sector], false);
                        rock.transform.SetPositionAndRotation(center - rot * Vector3.Scale(mesh.bounds.center, scale), rot);
                        rock.transform.localScale = scale;
                        rock.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var renderer = rock.AddComponent<MeshRenderer>();
                        renderer.sharedMaterial = materials[random.Next(materials.Length)];
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        renderer.receiveShadows = true;
                        renderer.lightProbeUsage = LightProbeUsage.Off;
                        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                        usedMeshes.Add(mesh);
                        positions.Add((anchor, spacing));
                        sumTriangles += costs[variant];
                        counts[tier]++;
                        zoneCounts[zone]++;
                        placed++;
                    }
                }
            }

            float Next() => (float)random.NextDouble();
        }

        static Vector3 ScaledBounds(Quaternion rotation, Vector3 half)
        {
            var a = rotation * Vector3.right * half.x;
            var b = rotation * Vector3.up * half.y;
            var c = rotation * Vector3.forward * half.z;
            return new Vector3(Mathf.Abs(a.x) + Mathf.Abs(b.x) + Mathf.Abs(c.x),
                Mathf.Abs(a.y) + Mathf.Abs(b.y) + Mathf.Abs(c.y), Mathf.Abs(a.z) + Mathf.Abs(b.z) + Mathf.Abs(c.z));
        }

        static (string name, Vector3 eye, Vector3 target)[] RockCoverViews() => new[]
        {
            ("01-Isla-completa", new Vector3(525, 125, -470), new Vector3(28, -53, 12)),
            ("02-Acantilado-oeste", new Vector3(-419, -49, -215), new Vector3(-100, -76, -18)),
            ("03-Parte-inferior", new Vector3(-424, -248, -395), new Vector3(27, -97, 14)),
            ("04-Union-roca-murallas", new Vector3(354, 18, -259), new Vector3(147, -19, -84)),
            ("05-Rocas-cercanas", new Vector3(-284, -93, -105), new Vector3(-126, -102, -33))
        };

        public static void ReviewRockCoverInBatch()
        {
            var hash = Hash();
            var scene = EditorSceneManager.OpenPreviewScene(Original);
            var resources = new List<Object>();
            try
            {
                ValidateFinalLook(scene);
                var cover = BuildRockCover(scene, scene.GetRootGameObjects(), resources, out var report);
                CaptureAtmosphereViews(scene, null, cover, RockCoverViews(), RockCoverOutput);
                ValidateFinalLook(scene);
                Require(Hash() == hash, "Cambió la escena original.");
                File.WriteAllText(Path.Combine(RockCoverOutput, "Validacion.txt"), report + "\nSHA-256: " + hash);
                Debug.Log("COBERTURA_ROCOSA_OK " + report);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
        }
    }
}

