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
        const string SeamMenu = IslandMenu + "Unión roca y murallas/";
        const string SeamOutput = "Backups/CiudadDelSol/UnionRocaMurallas";
        const int SeamSamples = CliffSegments * 2;

        [MenuItem(SeamMenu + "1. Abrir propuesta temporal")]
        static void OpenWallSeam()
        {
            var source = SceneManager.GetActiveScene();
            Require(source.path == Original && !source.isDirty && Hash() == AtmosphereSourceHash &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                StageUtility.GetCurrentStage() == StageUtility.GetMainStage(), "Abrí la escena integrada en edición.");
            var stage = ScriptableObject.CreateInstance<RiftAtmosphereStage>();
            stage.Source = source;
            stage.SeamMode = true;
            StageUtility.GoToStage(stage, true);
        }

        [MenuItem(SeamMenu + "2. Siguiente vista")]
        static void NextWallSeam() => NextAtmosphereView();

        [MenuItem(SeamMenu + "3. Alternar antes y después")]
        static void ToggleWallSeam() => ToggleAtmosphere();

        [MenuItem(SeamMenu + "4. Cerrar sin guardar")]
        static void CloseWallSeam() => CloseAtmosphere();

        [MenuItem(SeamMenu + "5. Validar y capturar comparativas")]
        public static void CaptureWallSeamInBatch()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && Hash() == AtmosphereSourceHash &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "La comparación requiere la escena aprobada y un dispositivo gráfico.");
            var scene = EditorSceneManager.OpenPreviewScene(Original);
            var resources = new List<Object>();
            try
            {
                var atmosphere = BuildAtmosphere(scene, resources);
                var seam = BuildWallSeam(scene, resources, out var report);
                CaptureAtmosphereViews(scene, atmosphere, seam, WallSeamViews(), SeamOutput);
                File.WriteAllLines(Path.Combine(SeamOutput, "Mediciones.txt"), report);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
            Require(Hash() == AtmosphereSourceHash, "Cambió el archivo de la escena definitiva.");
            Debug.Log("UNION_PREVIEW_OK");
        }

        static (string name, Vector3 eye, Vector3 target)[] WallSeamViews() => new[]
        {
            ("01-Sur", new Vector3(525, 125, -470), new Vector3(28, -53, 12)),
            ("02-Oeste", new Vector3(-560, 99, -210), new Vector3(20, -59, 4)),
            ("03-Lateral-cercano-occidental", new Vector3(-258, -29, -202), new Vector3(-188, -48, -120)),
            ("04-Junta-cercana-oeste", new Vector3(-239, -7, -95), new Vector3(-146, -15, -76)),
            ("05-Junta-cercana-este", new Vector3(317, -5, 120), new Vector3(223, -15, 132)),
            ("06-Noreste", new Vector3(520, 132, 564), new Vector3(33, -47, 20))
        };

        static GameObject BuildWallSeam(Scene scene, List<Object> resources, out List<string> report)
        {
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>()).ToArray();
            var mass = all.Single(t => t.name == IslandMount);
            var decor = all.Single(t => t.name == ExteriorMount);
            var walls = all.Where(t => t.name.StartsWith("Base_Wall_02", StringComparison.Ordinal))
                .SelectMany(t => t.GetComponentsInChildren<MeshRenderer>())
                .Where(r => r.enabled && r.bounds.min.y < -14 && r.bounds.min.y > -22 && r.bounds.max.y > -11)
                .Select(r => r.bounds).Distinct().ToArray();
            Require(walls.Length > 100, "No se identificaron los tramos perimetrales del subsuelo.");
            var architecture = all.Where(t => IsIslandArchitecture(t.gameObject)).Select(t => t.gameObject).ToArray();
            Physics.SyncTransforms();
            var protectedBounds = architecture.Concat(new[] { decor.gameObject })
                .SelectMany(r => r.GetComponentsInChildren<Renderer>().Where(v => v.enabled).Select(v => v.bounds)
                    .Concat(r.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).Select(c => c.bounds)))
                .Distinct().ToArray();
            var rim = new List<Vector3>();
            foreach (var filter in mass.GetComponentsInChildren<MeshFilter>().OrderBy(f => f.name, StringComparer.Ordinal))
            {
                var mesh = filter.sharedMesh;
                var vertices = mesh.vertices;
                var crest = mesh.GetTriangles(3);
                Require(crest.Length == CliffSegments / 8 * 6, "Cambió la topología de la cresta integrada.");
                for (int i = 0; i < crest.Length; i += 6)
                    rim.Add(filter.transform.TransformPoint(vertices[crest[i]]));
            }
            Require(rim.Count == CliffSegments, "Falta parte de la cresta integrada.");
            var center = new Vector3(rim[CliffSegments / 4].x, 0, rim[0].z);
            var edge = new Vector3[SeamSamples];
            var directions = new Vector3[SeamSamples];
            var outerRadius = new float[SeamSamples];
            var wallRadius = new float[SeamSamples];
            var wallBase = new float[SeamSamples];

            for (int i = 0; i < SeamSamples; i++)
            {
                edge[i] = Vector3.Lerp(rim[i / 2], rim[(i / 2 + 1) % CliffSegments], (i % 2) * .5f);
                var radial = edge[i] - center;
                radial.y = 0;
                outerRadius[i] = radial.magnitude;
                directions[i] = radial.normalized;
                foreach (var wall in walls)
                {
                    float hit = WallFaceDistance(wall, center, directions[i]);
                    if (hit <= wallRadius[i] || hit > outerRadius[i] - .5f) continue;
                    wallRadius[i] = hit;
                    wallBase[i] = wall.min.y;
                }
            }

            var root = new GameObject("Costura exterior de roca · temporal");
            SceneManager.MoveGameObjectToScene(root, scene);
            var stone = mass.GetComponentInChildren<MeshRenderer>().sharedMaterials[0];
            var probes = mass.GetComponentsInChildren<MeshFilter>().Select(filter =>
            {
                var probe = new GameObject("Consulta temporal de visibilidad");
                probe.transform.SetParent(root.transform, false);
                probe.transform.SetPositionAndRotation(filter.transform.position, filter.transform.rotation);
                probe.transform.localScale = filter.transform.lossyScale;
                var collider = probe.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                return collider;
            }).ToArray();
            Physics.SyncTransforms();
            int accepted = 0, additional = 0, partial = 0, skipped = 0, triangles = 0;
            var samples = new List<(float before, float after, float vertical)>();
            var audit = new List<string>
            {
                $"Perímetro real: {walls.Length} envolventes de muro; {protectedBounds.Length} envolventes de protección.",
                "Separaciones horizontales radiales medidas entre la envolvente exterior del muro y el borde de la roca."
            };
            for (int chunk = 0; chunk < 8; chunk++)
            {
                var vertices = new List<Vector3>();
                var indices = new List<int>();
                var uv = new List<Vector2>();
                for (int i = chunk * SeamSamples / 8; i < (chunk + 1) * SeamSamples / 8; i++)
                {
                    int next = (i + 1) % SeamSamples;
                    float gapA = outerRadius[i] - wallRadius[i], gapB = outerRadius[next] - wallRadius[next];
                    bool discontinuity = wallRadius[i] < 50 || wallRadius[next] < 50 || gapA > 78 || gapB > 78 ||
                        Mathf.Abs(wallRadius[i] - wallRadius[next]) > 11;
                    if (discontinuity)
                    {
                        Recover(i, next, "discontinuidad del muro o alcance");
                        continue;
                    }
                    var area = new Bounds(edge[i], Vector3.zero);
                    area.Encapsulate(edge[next]);
                    area.Encapsulate(Point(i, wallRadius[i], wallBase[i] - 4));
                    area.Encapsulate(Point(next, wallRadius[next], wallBase[next] - 4));
                    area.Expand(3);
                    var nearby = protectedBounds.Where(b => b.Intersects(area)).ToArray();
                    bool found = false;
                    for (int attempt = 0; attempt < 9; attempt++)
                    {
                        float retreat = .32f + attempt * .74f;
                        if (retreat >= Mathf.Min(gapA, gapB) - .1f) break;
                        var a = Section(i, retreat);
                        var b = Section(next, retreat);
                        var proposed = new List<(Vector3 a, Vector3 b, Vector3 c)>();
                        for (int row = 0; row < a.Length - 1; row++)
                        {
                            proposed.Add((a[row], b[row], a[row + 1]));
                            proposed.Add((b[row], b[row + 1], a[row + 1]));
                        }
                        if (proposed.Any(t => CliffIntersects(t.a, t.b, t.c, nearby))) continue;
                        Emit(proposed);
                        samples.Add(((gapA + gapB) * .5f, retreat, (wallBase[i] + wallBase[next] - a[0].y - b[0].y) * .5f));
                        audit.Add($"Tramo {i:000}, centro {(a[0] + b[0]) * .5f:F2}, antes {(gapA + gapB) * .5f:F2} m, después {retreat:F2} m.");
                        accepted++;
                        found = true;
                        break;
                    }
                    if (!found) Recover(i, next, "proximidad a geometría protegida");
                }
                if (indices.Count == 0) continue;
                var mesh = new Mesh { name = $"Costura de estratos {chunk + 1:00}" };
                resources.Add(mesh);
                mesh.SetVertices(vertices);
                mesh.SetTriangles(indices, 0);
                mesh.SetUVs(0, uv);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                var part = new GameObject(mesh.name);
                part.transform.SetParent(root.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = stone;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                triangles += indices.Count / 3;

                void Emit(IEnumerable<(Vector3 a, Vector3 b, Vector3 c)> proposed)
                {
                    foreach (var t in proposed)
                    {
                        var normal = Vector3.Cross(t.b - t.a, t.c - t.a);
                        var ordered = normal.y < 0 ? new[] { t.a, t.c, t.b } : new[] { t.a, t.b, t.c };
                        foreach (var vertex in ordered)
                        {
                            indices.Add(vertices.Count);
                            vertices.Add(vertex);
                            uv.Add(new Vector2((vertex.x + vertex.z * .31f) / 17f,
                                (vertex.z - vertex.x * .24f + vertex.y * .65f) / 17f));
                        }
                    }
                }

                void Recover(int i, int next, string reason)
                {
                    var middle = ReadSection(Vector3.Lerp(edge[i], edge[next], .5f));
                    string label = $"Pendiente {i:000}, centro de cresta {middle.edge:F2}";
                    float before = middle.radius - middle.wall;
                    if (middle.wall < 50 || before > 78)
                    {
                        audit.Add($"{label}: conservado, sin apoyo exterior próximo ({reason}).");
                        skipped++;
                        return;
                    }
                    var sight = center + middle.direction * (middle.wall + before * .42f);
                    sight.y = middle.baseY - .36f;
                    if (!Visible(sight, middle.direction))
                    {
                        audit.Add($"{label}: conservado, hueco de {before:F2} m ocluido en las sondas exteriores.");
                        skipped++;
                        return;
                    }
                    const int slices = 4;
                    int recovered = 0;
                    var local = new List<(float before, float after, float vertical)>();
                    for (int slice = 0; slice < slices; slice++)
                    {
                        var a = ReadSection(Vector3.Lerp(edge[i], edge[next], slice / (float)slices));
                        var b = ReadSection(Vector3.Lerp(edge[i], edge[next], (slice + 1) / (float)slices));
                        float gap = Mathf.Min(a.radius - a.wall, b.radius - b.wall);
                        if (a.wall < 50 || b.wall < 50 || gap < 2 || gap > 78 ||
                            Mathf.Abs(a.wall - b.wall) > 13) continue;
                        var area = new Bounds(a.edge, Vector3.zero);
                        area.Encapsulate(b.edge);
                        area.Encapsulate(center + a.direction * a.wall + Vector3.up * (a.baseY - 6));
                        area.Encapsulate(center + b.direction * b.wall + Vector3.up * (b.baseY - 6));
                        area.Expand(4);
                        var nearby = protectedBounds.Where(bound => bound.Intersects(area)).ToArray();
                        foreach (float retreat in new[] { .7f, 1.8f, 3.0f, 4.8f, 6.5f, 8.5f })
                        {
                            if (retreat > gap - .5f) break;
                            var left = Spur(a, retreat, i + slice * .25f);
                            var right = Spur(b, retreat, i + (slice + 1) * .25f);
                            var proposed = new List<(Vector3 a, Vector3 b, Vector3 c)>();
                            for (int row = 0; row < left.Length - 1; row++)
                            {
                                proposed.Add((left[row], right[row], left[row + 1]));
                                proposed.Add((right[row], right[row + 1], left[row + 1]));
                            }
                            if (proposed.Any(t => CliffIntersects(t.a, t.b, t.c, nearby))) continue;
                            Emit(proposed);
                            local.Add(((a.radius - a.wall + b.radius - b.wall) * .5f, retreat,
                                (a.baseY - left[0].y + b.baseY - right[0].y) * .5f));
                            recovered++;
                            break;
                        }
                    }
                    if (recovered == 0)
                    {
                        audit.Add($"{label}: visible, conservado por riesgo de intersección o falta de continuidad; {before:F2} m.");
                        skipped++;
                        return;
                    }
                    if (recovered == slices) additional++;
                    else partial++;
                    samples.AddRange(local);
                    audit.Add($"{label}: visible, {recovered}/{slices} subdivisiones resueltas; " +
                        $"antes {local.Min(s => s.before):F2}–{local.Max(s => s.before):F2} m; " +
                        $"después {local.Min(s => s.after):F2}–{local.Max(s => s.after):F2} m; " +
                        (recovered == slices ? "resuelto." : "resto reservado por protección o discontinuidad."));
                }
            }
            foreach (var probe in probes) Object.DestroyImmediate(probe.gameObject);
            Require(accepted > 20 && root.GetComponentsInChildren<Collider>(true).Length == 0,
                "No se pudo resolver una transición exterior suficiente.");
            string summary = $"{accepted} tramos previos conservados; {additional} adicionales completos, {partial} parciales, {skipped} sin ajuste. " +
                $"Antes {samples.Min(s => s.before):F2}–{samples.Max(s => s.before):F2} m (mediana {Median(samples.Select(s => s.before)):F2}); " +
                $"después {samples.Min(s => s.after):F2}–{samples.Max(s => s.after):F2} m (mediana {Median(samples.Select(s => s.after)):F2}). " +
                $"Separación vertical a base de muro {samples.Min(s => s.vertical):F2}–{samples.Max(s => s.vertical):F2} m. " +
                $"{triangles} triángulos, {root.transform.childCount} renderizadores, 0 colliders, 0 cruces detectados.";
            audit.Insert(0, summary);
            audit.Add("SHA-256: " + Hash());
            Debug.Log("UNION_MEDICIONES " + summary);
            report = audit;
            return root;

            Vector3 Point(int i, float radius, float y) => center + directions[i] * radius + Vector3.up * y;

            (Vector3 edge, Vector3 direction, float radius, float wall, float baseY) ReadSection(Vector3 rimPoint)
            {
                var radial = rimPoint - center;
                radial.y = 0;
                float radius = radial.magnitude;
                var direction = radial / radius;
                float wallRadius = 0, baseY = 0;
                foreach (var wall in walls)
                {
                    float hit = WallFaceDistance(wall, center, direction);
                    if (hit <= wallRadius || hit > radius - .5f) continue;
                    wallRadius = hit;
                    baseY = wall.min.y;
                }
                return (rimPoint, direction, radius, wallRadius, baseY);
            }

            bool Visible(Vector3 point, Vector3 direction)
            {
                var eyes = WallSeamViews().Select(v => v.eye).Concat(new[]
                {
                    point + direction * 73 + Vector3.up * 24,
                    point + direction * 95 + Vector3.up * 48
                });
                foreach (var eye in eyes)
                {
                    var delta = point - eye;
                    float length = delta.magnitude - .45f;
                    var ray = new Ray(eye, delta.normalized);
                    if (probes.Any(probe => probe.Raycast(ray, out _, length))) continue;
                    if (protectedBounds.Any(bound => bound.IntersectRay(ray, out float distance) && distance < length)) continue;
                    return true;
                }
                return false;
            }

            Vector3[] Spur((Vector3 edge, Vector3 direction, float radius, float wall, float baseY) section,
                float retreat, float phase)
            {
                float y = section.baseY - 1.1f - .4f * Mathf.Sin(phase * 1.6f);
                var tip = center + section.direction * (section.wall + retreat) + Vector3.up * y;
                var outer = section.edge + section.direction * .12f;
                return new[] { tip, Vector3.Lerp(tip, outer, .29f) - Vector3.up * (1.3f + .55f * Mathf.Sin(phase)),
                    Vector3.Lerp(tip, outer, .67f) - Vector3.up * (.65f + .4f * Mathf.Cos(phase * 1.7f)), outer };
            }

            Vector3[] Section(int i, float retreat)
            {
                float start = wallRadius[i] + retreat;
                float span = outerRadius[i] - start;
                float y = wallBase[i] - .55f - .31f * Mathf.Sin(i * .73f);
                var tip = Point(i, start, y);
                var shoulder = Point(i, start + span * .32f, Mathf.Lerp(y, edge[i].y, .28f) - 1.05f +
                    Mathf.Sin(i * .69f) * .85f);
                var shelf = Point(i, start + span * .68f, Mathf.Lerp(y, edge[i].y, .63f) - .7f +
                    Mathf.Sin(i * 1.15f) * .55f);
                return new[] { tip, shoulder, shelf, edge[i] + directions[i] * .12f };
            }
        }

        static float Median(IEnumerable<float> values)
        {
            var sorted = values.OrderBy(x => x).ToArray();
            return (sorted[sorted.Length / 2] + sorted[(sorted.Length - 1) / 2]) * .5f;
        }

        static float WallFaceDistance(Bounds bounds, Vector3 center, Vector3 direction)
        {
            float near = 0, far = float.PositiveInfinity;
            for (int axis = 0; axis <= 2; axis += 2)
            {
                if (Mathf.Abs(direction[axis]) < .00001f)
                {
                    if (center[axis] < bounds.min[axis] || center[axis] > bounds.max[axis]) return 0;
                    continue;
                }
                float first = (bounds.min[axis] - center[axis]) / direction[axis];
                float second = (bounds.max[axis] - center[axis]) / direction[axis];
                near = Mathf.Max(near, Mathf.Min(first, second));
                far = Mathf.Min(far, Mathf.Max(first, second));
                if (near > far) return 0;
            }
            return far;
        }
    }
}
