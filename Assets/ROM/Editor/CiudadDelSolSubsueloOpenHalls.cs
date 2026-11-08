using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string OpenHallsHash = "E61390DEC182ACF76DA9F8B4658DE14EF36896676BC658229E51802494C7B4A8";
        const string OpenHallsFolder = "Backups/CiudadDelSol/SubsueloAbierto";
        const string InventoryPath = "Backups/CiudadDelSol/AuditoriaEspacial/Piezas.tsv";
        const string OpenHallsPhase2 = "Backups/CiudadDelSol/SubsueloAbiertoFase2";

        static IEnumerable<Sector> OpenHallCandidates(Scene source, Dictionary<GameObject, GlobalObjectId> inventory,
            Sector[] baseline)
        {
            var roots = source.GetRootGameObjects();
            bool adaptive = baseline.Length > 0;
            var protectedHalls = Sectors.Concat(ExpansionDesigns()).Concat(baseline).ToArray();
            var volumes = roots.ToDictionary(root => root, root =>
                root.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.bounds)
                .Concat(root.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).Select(c => c.bounds))
                .Where(b => b.max.y > Floor + .05f).ToArray());
            foreach (var root in roots.Where(r => new[] { WestRoot, NorthRoot, SouthRoot, EastRoot }.Contains(r.name)))
            {
                var roof = root.transform.Find("02 - Bovedas generales Y-4");
                var beams = root.transform.Cast<Transform>().Single(t => t.name.StartsWith("03 - Vigas", StringComparison.Ordinal) &&
                    t.name.EndsWith("principales", StringComparison.Ordinal));
                var caps = root.transform.Cast<Transform>().Single(t => t.name.StartsWith("04 - Columnas monumentales", StringComparison.Ordinal));
                var tiles = new HashSet<Vector2Int>(roof.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy).Select(Grid));
                var lines = new HashSet<Vector2Int>(beams.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy).Select(Grid));
                var capitals = caps.Cast<Transform>().Where(t => inventory.ContainsKey(t.gameObject) &&
                    Mathf.Abs(t.position.y + 12.019f) < .03f).ToArray();
                var upper = volumes.Where(v => v.Key != root).SelectMany(v => v.Value).ToArray();
                var local = root.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger &&
                    c.bounds.max.y > Floor + .05f).Select(c => (Item: c.transform, Bounds: c.bounds)).ToArray();
                int minX = tiles.Min(p => p.x) - 3, maxX = tiles.Max(p => p.x) + 3;
                int minZ = tiles.Min(p => p.y) - 3, maxZ = tiles.Max(p => p.y) + 3;
                foreach (int width in adaptive ? new[] { 54, 42, 36, 30, 24, 18, 12 } : new[] { 42, 30, 18 })
                foreach (int length in adaptive ? new[] { 90, 78, 66, 54, 48, 42, 36, 30, 24, 18, 12 } :
                    new[] { 78, 66, 54, 42, 30, 18 })
                for (int w = minX; w + width <= maxX; w += 6)
                for (int s = minZ; s + length <= maxZ; s += 6)
                {
                    int e = w + width, n = s + length;
                    if (protectedHalls.Any(h => w < h.East && e > h.West && s < h.North && n > h.South)) continue;
                    if (baseline.Any(h => w < h.East && e > h.West && s < h.North + 3 && n > h.South - 3)) continue;
                    bool Inside(Vector3 p) => p.x > w + .02f && p.x < e - .02f && p.z > s + .02f && p.z < n - .02f;
                    bool Crosses(Bounds b) => b.max.x > w + .02f && b.min.x < e - .02f &&
                        b.max.z > s + .02f && b.min.z < n - .02f;
                    var rows = Enumerable.Range(0, length / 6).Select(i => s + 3 + i * 6).ToArray();
                    var cols = Enumerable.Range(0, width / 6).Select(i => w + 3 + i * 6).ToArray();
                    if (!cols.All(x => rows.Concat(new[] { s - 3, n + 3 }).All(z => tiles.Contains(new Vector2Int(x, z)))))
                        continue;
                    bool westLine = rows.All(z => lines.Contains(new Vector2Int(w, z)));
                    if (!westLine && !rows.All(z => lines.Contains(new Vector2Int(e, z)))) continue;
                    var selected = capitals.Where(t => Inside(t.position)).ToArray();
                    if (selected.Length == 0) continue;
                    var selectedSet = new HashSet<Transform>(selected);
                    bool Allowed(Transform t, Bounds b)
                    {
                        if ((b.center.x <= w + .05f && b.max.x < w + .85f) ||
                            (b.center.x >= e - .05f && b.min.x > e - .85f)) return true;
                        while (t.parent && t.parent.parent != root.transform && t != root.transform) t = t.parent;
                        if (!t.parent) return false;
                        if (t.parent == caps)
                            return selectedSet.Contains(t) || (t.name.StartsWith("Column_Ceiling_02", StringComparison.Ordinal) &&
                                (Mathf.Abs(t.position.z - s) < .025f || Mathf.Abs(t.position.z - n) < .025f) &&
                                b.max.y < Spring - .5f);
                        return (t.parent == roof || t.parent == beams) && Inside(t.position);
                    }
                    if (local.Any(c => Crosses(c.Bounds) && !Allowed(c.Item, c.Bounds))) continue;
                    var above = upper.Where(Crosses).ToArray();
                    if (!adaptive && above.Length == 0) continue;
                    foreach (float crown in adaptive ? new[] { 1.8f, .5f, -1.15f, -2.9f, -4.2f } : new[] { 2f, .5f, -1.15f, -2.9f })
                    {
                        Sector fitted = null;
                        foreach (float shift in adaptive ? new[] { 0f, -width/6f, width/6f } : new[] { 0f })
                        {
                        var design = new Sector($"Nave {w}/{s} ({width}×{length})", root.name, w, e, s, n, crown,
                            Enumerable.Range(0, (length - 6) / 18 + 1).Select(i => s + 3f + 18 * i).ToArray(),
                            selected.Select(t => inventory[t.gameObject].targetPrefabId).ToArray(), !westLine,
                            profilePower: crown < -2 ? 6 : 4, minimumPeak: adaptive ? 11.5f : 13,
                            crownOffset: shift, minimumClearance: adaptive ? 10.3f : 12, adaptive: adaptive);
                        if (above.Any(b => b.min.y < design.Soffit(Mathf.Clamp(design.CrownX,
                            Mathf.Max(w, b.min.x), Mathf.Min(e, b.max.x))) + Thickness + .12f)) continue;
                        fitted = design;
                        break;
                        }
                        if (fitted == null) continue;
                        yield return fitted;
                        break;
                    }
                }
            }
        }

        static Vector2Int Grid(Transform t) =>
            new Vector2Int(Mathf.RoundToInt(t.position.x), Mathf.RoundToInt(t.position.z));

        [MenuItem(ExpansionMenu + "11. Abrir propuesta global validada")]
        static void OpenGlobalHallPreview() => OpenGlobalHallPreview(OpenHallsFolder);

        [MenuItem(ExpansionMenu + "13. Abrir fase 2 validada")]
        static void OpenSecondPhasePreview() => OpenGlobalHallPreview(OpenHallsPhase2);

        static void OpenGlobalHallPreview(string folder)
        {
            if (StageUtility.GetCurrentStage() is ExpansionStage) StageUtility.GoToMainStage();
            var source = SceneManager.GetActiveScene();
            Require(source.path == Original && !source.isDirty && Hash() == OpenHallsHash &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                StageUtility.GetCurrentStage() == StageUtility.GetMainStage(),
                "Abrí la escena definitiva integrada y limpia fuera de Play.");
            var stage = ScriptableObject.CreateInstance<ExpansionStage>();
            stage.Source = source;
            stage.Designs = ReadOpenHallDesigns(folder);
            stage.SourceHash = OpenHallsHash;
            StageUtility.GoToStage(stage, true);
        }

        static Sector[] ReadOpenHallDesigns(string folder)
        {
            float Number(string value) => float.Parse(value, CultureInfo.InvariantCulture);
            return File.ReadLines(Path.Combine(folder, "Naves.tsv")).Select(line =>
            {
                var fields = line.Split('\t');
                float crown = Number(fields[6]);
                return new Sector(fields[0], fields[1], Number(fields[2]), Number(fields[3]),
                    Number(fields[4]), Number(fields[5]), crown, fields[8].Split(',').Select(Number).ToArray(),
                    fields[9].Split(',').Select(ulong.Parse).ToArray(), bool.Parse(fields[7]),
                    profilePower: crown < -2 ? 6 : 4, minimumPeak: fields.Length > 10 ? Number(fields[11]) : 13,
                    crownOffset: fields.Length > 10 ? Number(fields[10]) : 0,
                    minimumClearance: fields.Length > 10 ? Number(fields[12]) : 12,
                    adaptive: fields.Length > 10 && bool.Parse(fields[13]));
            }).ToArray();
        }

        [MenuItem(ExpansionMenu + "12. Alternar antes y después")]
        static void ToggleGlobalHallComparison()
        {
            if (StageUtility.GetCurrentStage() is ExpansionStage stage) stage.ToggleComparison();
        }

        public static void ReviewOpenHallsInBatch() =>
            ReviewOpenHalls(EditorSceneManager.OpenScene(Original, OpenSceneMode.Single));

        public static void ReviewOpenHallsPhase2InBatch() =>
            ReviewOpenHalls(EditorSceneManager.OpenScene(Original, OpenSceneMode.Single), true);

        [MenuItem(ExpansionMenu + "10. Generar propuesta global y comparativas")]
        static void ReviewOpenHallsFromMenu() => ReviewOpenHalls(SceneManager.GetActiveScene());

        static void ReviewOpenHalls(Scene source, bool phase2 = false)
        {
            Require(source.path == Original && !source.isDirty && Hash() == OpenHallsHash &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                StageUtility.GetCurrentStage() == StageUtility.GetMainStage(),
                "Abrí la escena definitiva integrada y limpia fuera de Play.");
            string output = phase2 ? OpenHallsPhase2 : OpenHallsFolder;
            Directory.CreateDirectory(output);
            var baseline = phase2 ? ReadOpenHallDesigns(OpenHallsFolder) : Array.Empty<Sector>();
            Require(!phase2 || baseline.Length == 31, "La fase 2 requiere las 31 naves validadas.");
            var ids = File.ReadLines(InventoryPath).Skip(1).Select(row => row.Split('\t'))
                .Where(row => row[3].StartsWith("Column_", StringComparison.Ordinal) ||
                    row[3].IndexOf("pilar", StringComparison.OrdinalIgnoreCase) >= 0);
            var inventory = new Dictionary<GameObject, GlobalObjectId>();
            foreach (var row in ids)
                if (GlobalObjectId.TryParse(row[0], out var id) &&
                    GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is GameObject piece && piece.activeInHierarchy)
                    inventory[piece] = id;

            var candidates = OpenHallCandidates(source, inventory, baseline).OrderByDescending(s => s.CapitalIds.Length)
                .ThenByDescending(s => s.Width * s.Length).ToArray();
            Debug.Log($"SUBSUELO_ABIERTO_CANDIDATOS: {candidates.Length}");
            string shortlist = Path.Combine(output, "Preseleccion.tsv");
            var passed = File.Exists(shortlist) ? new HashSet<string>(File.ReadLines(shortlist)) : null;
            var designs = new List<Sector>();
            foreach (var candidate in candidates)
            {
                if (passed != null && !passed.Contains(candidate.Name)) continue;
                if (designs.Any(s => candidate.West < s.East && candidate.East > s.West &&
                    candidate.South < s.North && candidate.North > s.South)) continue;
                try
                {
                    if (passed == null) Validate(source, candidate);
                    designs.Add(candidate);
                }
                catch (InvalidOperationException error)
                {
                    Debug.Log($"SUBSUELO_ABIERTO_RECHAZADO {candidate.Name}: {error.Message}");
                }
            }
            Require(designs.Count > 0, "Ninguna nave superó la comprobación individual.");
            var joint = EditorSceneManager.NewPreviewScene();
            var resources = new List<Object>();
            try
            {
                var plans = new List<MountPlan>();
                var deactivated = new List<GameObject>();
                BuildExpansionJoint(source, joint, resources, plans, baseline.Concat(designs), OpenHallsHash, deactivated,
                    allowRejected: true, protectedCount: baseline.Length);
                Require(plans.Count > baseline.Length, "Ninguna nave nueva pasó la comprobación con el contexto completo.");
                var removed = new HashSet<GlobalObjectId>(plans.SelectMany(p => p.Targets));
                var replaceable = new HashSet<ulong>(candidates.SelectMany(c => c.CapitalIds));
                var audit = new List<string> { "ID\tRaiz\tGrupo\tPieza\tPosicion\tClase" };
                int interior = 0, changed = 0;
                foreach (var entry in inventory)
                {
                    var t = entry.Key.transform;
                    bool perimeter = t.parent && t.parent.name.IndexOf("perim", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!perimeter) interior++;
                    bool cut = removed.Contains(entry.Value);
                    if (cut) changed++;
                    string classification = cut ? "Reemplazo validado" : perimeter ? "Perimetral conservada" :
                        replaceable.Contains(entry.Value.targetPrefabId) ? "Reserva: encaje parcial o solapado" :
                        "Conservar: apoyo protegido o sin reemplazo demostrado";
                    audit.Add($"{entry.Value}\t{t.root.name}\t{t.parent?.name}\t{t.name}\t{t.position:F2}\t{classification}");
                }
                File.WriteAllLines(Path.Combine(output, "Columnas.tsv"), audit);
                File.WriteAllLines(Path.Combine(output, "Naves.tsv"), plans.Select(p =>
                    FormattableString.Invariant($"{p.Design.Name}\t{p.Design.Root}\t{p.Design.West}\t{p.Design.East}\t{p.Design.South}\t{p.Design.North}\t{p.Design.Crown}\t{p.Design.EastBearing}\t{string.Join(",", p.Design.Ribs.Select(r => r.ToString(CultureInfo.InvariantCulture)))}\t{string.Join(",", p.Design.CapitalIds)}\t{p.Design.CrownOffset}\t{p.Design.MinimumPeak}\t{p.Design.MinimumClearance}\t{p.Design.Adaptive}")));
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    string after = Path.Combine(output, "Despues"), before = Path.Combine(output, "Antes");
                    Directory.CreateDirectory(after);
                    Directory.CreateDirectory(before);
                    var additions = plans.Skip(baseline.Length).ToList();
                    CaptureExpansion(joint, additions, after);
                    foreach (var plan in additions) plan.Hall.SetActive(false);
                    foreach (var piece in deactivated.Skip(plans.Take(baseline.Length).Sum(p => p.Targets.Length)))
                        piece.SetActive(true);
                    CaptureExpansion(joint, additions, before);
                }
                Debug.Log($"SUBSUELO_ABIERTO_OK: {plans.Count} naves; {plans.Sum(p => p.Design.Width * p.Design.Length)} m2; " +
                    $"{interior} columnas inventariadas fuera de grupos perimetrales; {changed} reemplazadas; " +
                    $"{replaceable.Count} con encaje preliminar; {removed.Count} recortes totales.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(joint);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
            Require(!source.isDirty && Hash() == OpenHallsHash, "La escena de origen cambió.");
        }
    }
}
