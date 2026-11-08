using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    public static class CiudadDelSolColumnAudit
    {
        const string ScenePath = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string ReportFolder = "Backups/CiudadDelSol/Columnas";
        const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        const string AuditedScene = "0896C23E4D0AC2EC5979BD30DB44A44DF3FFC6D89A45DAB136000F53E655774F";

        public static void Audit() => Run(false);
        public static void Apply() => Run(true);
        public static void ApplyExterior() => Run(false, true);

        static void Run(bool apply, bool exterior = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Cerrar Play y resolver cambios sin guardar antes de auditar.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var roots = scene.GetRootGameObjects();
            var routes = new List<(string Name, Bounds Box)>();
            void Reserve(string name, Vector3 origin, Bounds box)
            {
                box.center += origin;
                routes.Add((name, box));
            }
            void FromGenerator(Type type, params string[] fields)
            {
                foreach (string field in fields)
                    foreach (var box in (Bounds[])type.GetField(field, Static).GetValue(null))
                        Reserve(type.Name + "/" + field, Vector3.zero, box);
            }
            FromGenerator(typeof(JardinesDoradosArchitectureGenerator), "Reservations");
            FromGenerator(typeof(ArchivosSolaresArchitectureGenerator), "Corridors");
            FromGenerator(typeof(ClaustroDelEclipseArchitectureGenerator), "MainClearance", "StairClearance");
            FromGenerator(typeof(SantuarioProfanoArchitectureGenerator), "MainClearance", "StairClearance");
            FromGenerator(typeof(ElSolCorrompidoArchitectureGenerator), "MainClearance", "StairClearance");

            // Keep four 0.8 m lanes and their clearance when crossing the plaza ring.
            Reserve("Plaza / Jardines a monumento", new Vector3(18, 12, 12),
                new Bounds(new Vector3(-34, 1.25f, 0), new Vector3(44, 2.5f, 4.8f)));
            Reserve("Plaza / monumento a Santuario", new Vector3(18, 12, 12),
                new Bounds(new Vector3(34, 1.25f, 0), new Vector3(44, 2.5f, 4.8f)));
            Reserve("Plaza / eje sur", new Vector3(18, 12, 12),
                new Bounds(new Vector3(0, 1.25f, -34), new Vector3(4.8f, 2.5f, 44)));
            Reserve("Plaza / eje norte", new Vector3(18, 12, 12),
                new Bounds(new Vector3(0, 1.25f, 34), new Vector3(4.8f, 2.5f, 44)));

            var bridgeRoutes = (IEnumerable)typeof(SunDungeonStructureGenerator).GetField("Routes", Static).GetValue(null);
            foreach (var route in bridgeRoutes)
            {
                var type = route.GetType();
                var points = (Vector3[])type.GetField("Points").GetValue(route);
                string name = (string)type.GetField("Name").GetValue(route);
                for (int i = 1; i < points.Length; i++)
                {
                    int count = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(points[i - 1], points[i])));
                    for (int j = 0; j <= count; j++)
                        Reserve(name, Vector3.Lerp(points[i - 1], points[i], j / (float)count),
                            new Bounds(Vector3.up * 1.25f, new Vector3(4.8f, 2.5f, 4.8f)));
                }
            }
            var underground = new[]
            {
                typeof(CiudadDelSolSubsueloCentralSoutheastGenerator), typeof(CiudadDelSolSubsueloWesternWingGenerator),
                typeof(CiudadDelSolSubsueloNorthernWingGenerator), typeof(CiudadDelSolSubsueloEasternClosureGenerator)
            }.Select(t => t.GetMethod("Reserved", Static)).ToArray();
            var columns = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Where(t => IsColumn(t.name) && !HasColumnAncestor(t))
                .ToArray();
            var report = new List<string> { "Identidad\tSector\tGrupo\tPieza\tActivo\tClase\tPosicion\tLimites\tReservas" };
            foreach (var column in columns)
            {
                var renderers = column.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
                if (renderers.Length == 0) continue;
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                var root = column.root.name;
                string group = column.parent ? column.parent.name : "";
                string sector = root;
                for (var t = column.parent; t; t = t.parent)
                    if (t.parent && t.parent.name == "Dungeon_Ciudad_del_Sol_Generated") sector = t.name;
                bool decoration = IsDecorativeGroup(group, column.name) && root == "Dungeon_Ciudad_del_Sol_Generated";
                var occupied = routes.Where(r => r.Box.Intersects(bounds)).Select(r => r.Name).Distinct().ToList();
                if (bounds.min.y < -13.5f && bounds.max.y > -15.95f && root.StartsWith("Subsuelo_", StringComparison.Ordinal) &&
                    underground.Any(m => (bool)m.Invoke(null, new object[] { bounds.center.x, bounds.center.z,
                        Mathf.Max(bounds.extents.x, bounds.extents.z) })))
                    occupied.Add("Subsuelo / reserva de paso Y-16 (envolvente)");
                report.Add($"{GlobalObjectId.GetGlobalObjectIdSlow(column.gameObject)}\t{sector}\t{group}\t{column.name}\t" +
                    $"{column.gameObject.activeInHierarchy}\t{(decoration ? "Decorativa documentada" : "Protegida o requiere revision")}\t" +
                    $"{column.position:F3}\t{bounds.min:F3} / {bounds.max:F3}\t{string.Join(", ", occupied)}");
            }
            Directory.CreateDirectory(ReportFolder);
            File.WriteAllLines(ReportFolder + (exterior ? "/Inventario-previo-pasada2.tsv" : "/Inventario.tsv"), report);
            Debug.Log("CIUDAD_COLUMNAS_AUDITADAS: " + (report.Count - 1) + " piezas con renderizador. " + ReportFolder);
            if (apply) OpenPlazaAxes(scene, roots, routes);
            if (exterior) ApplyExterior(scene, roots, routes);
        }

        static void ApplyExterior(Scene scene, GameObject[] roots, List<(string Name, Bounds Box)> routes)
        {
            const string baseline = "70B8557F7BEB36E9DA4307E013C4F4E73A31F6312A64105DB881DB84FF3D7C94";
            Require(Hash(ScenePath) == baseline, "La escena difiere de la version aprobada.");
            var plan = new (string Id, string Group, string Name, Vector3 From, Vector3? To)[]
            {
                (Id: "115024-498788001", Group: "08 - Ruinas asimetricas", Name: "Column_Broken_01",
                    From: new Vector3(0,28,106), To: (Vector3?)new Vector3(-8,28,110)),
                ("115024-1404703515", "09 - Ruinas y patios", "Column_Broken_01",
                    new Vector3(105,4,-93), (Vector3?)new Vector3(104,4,-99)),
                ("115024-2142839852", "09 - Ruinas meridionales", "Column_Broken_01",
                    new Vector3(177,36,120), (Vector3?)new Vector3(171,36,120)),
                ("182968-332217783", "09 - Ruinas meridionales", "Column_01",
                    new Vector3(180.001f,36,114), (Vector3?)null)
            };
            var backup = ReportFolder + "/Dungeon_Isla_del_Sol.before-columnas2." +
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".unity";
            File.Copy(ScenePath, backup);
            Require(Hash(backup) == baseline, "Respaldo incorrecto.");
            var results = new List<string> { "Backup=" + backup };
            int accepted = 0;
            foreach (var (suffix, group, name, from, to) in plan)
            {
                Require(GlobalObjectId.TryParse("GlobalObjectId_V1-2-e45696efca275a7419811d0359501b3d-" + suffix,
                    out var id), "Identidad invalida: " + suffix);
                var column = (GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject)?.transform;
                if (!column || !column.gameObject.activeInHierarchy || column.gameObject.scene != scene ||
                    column.name != name || column.parent.name != group || Vector3.Distance(column.position, from) >= .01f)
                {
                    results.Add(suffix + "\tOMITIDA\tIdentidad, jerarquia o posicion no coincidente");
                    continue;
                }
                bool Other(Transform t) => t != column && !t.IsChildOf(column);
                var fixedVolumes = roots.SelectMany(r => r.GetComponentsInChildren<Renderer>())
                    .Where(r => r.enabled && Other(r.transform)).Select(r => (r.name, r.bounds))
                    .Concat(roots.SelectMany(r => r.GetComponentsInChildren<Collider>())
                        .Where(c => c.enabled && !c.isTrigger && Other(c.transform)).Select(c => (c.name, c.bounds)))
                    .ToArray();
                var original = SolidEnvelope(column);
                var oldPosition = column.localPosition;
                string Contact(Bounds b, float margin)
                {
                    b.SetMinMax(b.min + new Vector3(-margin, .08f, -margin),
                        b.max + new Vector3(margin, margin, margin));
                    return string.Join(", ", fixedVolumes.Where(v => v.bounds.Intersects(b))
                        .Select(v => v.name).Distinct().Take(8));
                }
                bool Supported(Bounds b)
                {
                    foreach (float x in new[] { -.7f, 0, .7f })
                        foreach (float z in new[] { -.7f, 0, .7f })
                        {
                            var p = new Vector3(b.center.x + x * b.extents.x, b.min.y, b.center.z + z * b.extents.z);
                            if (!Physics.RaycastAll(p + Vector3.up * .16f, Vector3.down, .32f, ~0,
                                QueryTriggerInteraction.Ignore).Any(h => Other(h.collider.transform) &&
                                    h.normal.y > .9f && Mathf.Abs(h.point.y - p.y) < .08f))
                                return false;
                        }
                    return true;
                }
                string error = Contact(original, .10f);
                if (!string.IsNullOrEmpty(error)) error = "Contacto de origen: " + error;
                else if (!Supported(original)) error = "Piso de origen sin apoyo comprobado";
                else if (to.HasValue)
                {
                    column.position = to.Value;
                    Physics.SyncTransforms();
                    var proposed = SolidEnvelope(column);
                    string clash = Contact(proposed, .20f);
                    if (!string.IsNullOrEmpty(clash)) error = "Contacto en destino: " + clash;
                    else if (routes.Any(r => r.Box.Intersects(proposed))) error = "Destino dentro de una reserva de paso";
                    else if (!Supported(proposed)) error = "Apoyo insuficiente en destino";
                }
                if (!string.IsNullOrEmpty(error))
                {
                    column.localPosition = oldPosition;
                    Physics.SyncTransforms();
                    results.Add(suffix + "\tOMITIDA\t" + error);
                    continue;
                }
                if (!to.HasValue) column.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(to.HasValue ? (UnityEngine.Object)column : column.gameObject);
                accepted++;
                results.Add(suffix + "\t" + (to.HasValue ? "REUBICADA\t" + to.Value.ToString("F3") : "DESACTIVADA"));
            }
            if (accepted > 0)
            {
                Require(Hash(ScenePath) == baseline, "La escena guardada cambio durante la operacion.");
                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene), "Unity no pudo guardar las columnas.");
            }
            results.Add("SHA256=" + Hash(ScenePath));
            File.WriteAllLines(ReportFolder + "/Cambios-pasada2.tsv", results);
            Debug.Log("CIUDAD_COLUMNAS_PASADA2: " + string.Join(" | ", results));
        }

        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }

        static void Require(bool condition, string reason)
        {
            if (!condition) throw new InvalidOperationException(reason);
        }

        static Bounds SolidEnvelope(Transform piece)
        {
            var parts = piece.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.bounds)
                .Concat(piece.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).Select(c => c.bounds))
                .ToArray();
            Require(parts.Length > 0, "Columna sin geometria activa: " + piece.name);
            var result = parts[0];
            foreach (var part in parts.Skip(1)) result.Encapsulate(part);
            return result;
        }

        static void OpenPlazaAxes(Scene scene, GameObject[] roots, List<(string Name, Bounds Box)> routes)
        {
            Require(Hash(ScenePath) == AuditedScene, "La escena difiere de la auditada.");
            var originalPositions = new[] { new Vector3(18, 12, 58), new Vector3(18, 12, -34) };
            var instanceIds = new ulong[] { 824300935, 1083590114 };
            var columns = new Transform[instanceIds.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                string idText = "GlobalObjectId_V1-2-e45696efca275a7419811d0359501b3d-1930562665712168-" + instanceIds[i];
                Require(GlobalObjectId.TryParse(idText, out var id), "Identidad invalida: " + idText);
                var column = (GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject)?.transform;
                Require(column && column.gameObject.activeInHierarchy && column.gameObject.scene == scene &&
                    column.name == "Bld_Temple_Int_Column_A" && column.parent.name == "03 - Columnatas monumentales y vestigios" &&
                    Vector3.Distance(column.position, originalPositions[i]) < .01f, "Columna distinta de la auditada: " + idText);
                columns[i] = column;
            }
            bool Selected(Transform t) => columns.Any(c => t == c || t.IsChildOf(c));
            var fixedVolumes = roots.SelectMany(r => r.GetComponentsInChildren<Renderer>())
                .Where(r => r.enabled && !Selected(r.transform)).Select(r => r.bounds)
                .Concat(roots.SelectMany(r => r.GetComponentsInChildren<Collider>())
                    .Where(c => c.enabled && !c.isTrigger && !Selected(c.transform)).Select(c => c.bounds))
                .ToArray();
            foreach (var column in columns)
            {
                var bounds = SolidEnvelope(column);
                Require(routes.Any(r => r.Box.Intersects(bounds) && r.Name.StartsWith("Plaza / eje", StringComparison.Ordinal)),
                    "La columna no ocupa su reserva de paso.");
                var crown = new Bounds(new Vector3(bounds.center.x, bounds.max.y + .15f, bounds.center.z),
                    new Vector3(bounds.size.x + .4f, .65f, bounds.size.z + .4f));
                Require(!fixedVolumes.Any(crown.Intersects), "Posible encuentro estructural sobre " + column.position);
            }
            string backup = ReportFolder + "/Dungeon_Isla_del_Sol.before-columnas." +
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".unity";
            File.Copy(ScenePath, backup);
            Require(Hash(backup) == AuditedScene, "Respaldo incorrecto.");
            var startPositions = columns.Select(c => c.position).ToArray();
            var startRotations = columns.Select(c => c.rotation).ToArray();
            var center = new Vector3(18, 12, 12);
            float? usedAngle = null;
            try
            {
                foreach (float angle in new[] { 9f, -9f, 7f, -7f, 11f, -11f })
                {
                    var orbit = Quaternion.Euler(0, angle, 0);
                    for (int i = 0; i < columns.Length; i++)
                        columns[i].SetPositionAndRotation(center + orbit * (startPositions[i] - center), orbit * startRotations[i]);
                    Physics.SyncTransforms();
                    if (!columns.All(PlacementIsClear)) continue;
                    usedAngle = angle;
                    break;
                }
                Require(usedAngle.HasValue, "Ninguna ubicacion lateral verificada para el par de columnas.");
                int samples = 0;
                foreach (var from in startPositions)
                    for (int along = -6; along <= 6; along++)
                        foreach (float lane in new[] { -2.08f, -1.2f, -.4f, .4f, 1.2f, 2.08f })
                        {
                            var p = new Vector3(from.x + lane, 12, from.z + along * .4f);
                            Require(Physics.Raycast(p + Vector3.up * .18f, Vector3.down, out var foot, .33f, ~0, QueryTriggerInteraction.Ignore) &&
                                foot.normal.y > .9f, "Piso discontinuo en " + p);
                            Require(!Physics.CheckCapsule(foot.point + Vector3.up * .37f, foot.point + Vector3.up * 2.13f,
                                .32f, ~0, QueryTriggerInteraction.Ignore), "Paso o altura libre insuficiente en " + p);
                            samples++;
                        }
                foreach (var column in columns) PrefabUtility.RecordPrefabInstancePropertyModifications(column);
                Require(Hash(ScenePath) == AuditedScene, "La escena guardada cambio durante la operacion.");
                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene), "Unity no pudo guardar las columnas.");
                var summary = columns.Select((t, i) => instanceIds[i] + "\t" + startPositions[i].ToString("F4") + "\t" + t.position.ToString("F4"))
                    .Concat(new[] { "Muestras=" + samples + "; ancho comprobado=4.80; altura libre comprobada=2.45",
                        "Backup=" + backup, "SHA256=" + Hash(ScenePath) }).ToArray();
                File.WriteAllLines(ReportFolder + "/Cambios-verificados.tsv", summary);
                Debug.Log("CIUDAD_COLUMNAS_GUARDADAS: " + string.Join(" | ", summary));
            }
            catch
            {
                if (Hash(ScenePath) == AuditedScene)
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                throw;
            }

            bool PlacementIsClear(Transform column)
            {
                var bounds = SolidEnvelope(column);
                if (routes.Any(r => r.Box.Intersects(bounds))) return false;
                var fit = bounds;
                fit.SetMinMax(bounds.min + new Vector3(-.35f, .08f, -.35f), bounds.max + new Vector3(.35f, .05f, .35f));
                if (fixedVolumes.Any(fit.Intersects)) return false;
                foreach (float x in new[] { -.7f, 0, .7f })
                    foreach (float z in new[] { -.7f, 0, .7f })
                    {
                        var basePoint = new Vector3(bounds.center.x + x * bounds.extents.x, bounds.min.y,
                            bounds.center.z + z * bounds.extents.z);
                        bool supported = Physics.RaycastAll(basePoint + Vector3.up * .16f, Vector3.down, .32f, ~0, QueryTriggerInteraction.Ignore)
                            .Any(hit => !Selected(hit.collider.transform) && hit.normal.y > .9f &&
                                Mathf.Abs(hit.point.y - basePoint.y) < .08f);
                        if (!supported) return false;
                    }
                return true;
            }
        }

        static bool IsColumn(string name)
        {
            if (name.StartsWith("Columnatas", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Columnata", StringComparison.OrdinalIgnoreCase)) return false;
            return name.IndexOf("Column_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Pillar", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool HasColumnAncestor(Transform item)
        {
            for (var parent = item.parent; parent; parent = parent.parent)
                if (IsColumn(parent.name)) return true;
            return false;
        }

        static bool IsDecorativeGroup(string group, string name) =>
            group == "03 - Columnatas monumentales y vestigios" ||
            group == "06 - Columnatas y ruinas" ||
            group == "09 - Ruinas meridionales" ||
            (name.StartsWith("Column_Broken", StringComparison.Ordinal) &&
             (group == "07 - Arcadas y ruinas" || group == "08 - Ruinas asimetricas" ||
              group == "09 - Ruinas y patios" || group == "15 - Ruinas del atrio"));
    }
}
