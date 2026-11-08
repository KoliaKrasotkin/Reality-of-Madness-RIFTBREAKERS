using System;
using System.Collections.Generic;
using System.IO;
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
        const string GridReviewFolder = "Backups/CiudadDelSol/SubsueloSinReticula";

        [MenuItem(ExpansionMenu + "14. Abrir propuesta sin retícula")]
        static void OpenColumnGridPreview()
        {
            if (StageUtility.GetCurrentStage() is ExpansionStage) StageUtility.GoToMainStage();
            var source = SceneManager.GetActiveScene();
            Require(source.path == Original && !source.isDirty && Hash() == OpenHallsHash &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                StageUtility.GetCurrentStage() == StageUtility.GetMainStage(), "Abrí la escena definitiva limpia fuera de Play.");
            var stage = ScriptableObject.CreateInstance<ExpansionStage>();
            stage.Source = source;
            stage.Designs = ReadOpenHallDesigns(OpenHallsPhase2);
            stage.SourceHash = OpenHallsHash;
            stage.ClearColumnGrid = true;
            StageUtility.GoToStage(stage, true);
        }

        public static void ReviewColumnGridInBatch()
        {
            var source = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            Require(!source.isDirty && Hash() == OpenHallsHash, "La escena de origen cambió.");
            var designs = ReadOpenHallDesigns(OpenHallsPhase2);
            Require(designs.Length == 34, "La revisión requiere las 34 naves validadas.");
            var joint = EditorSceneManager.NewPreviewScene();
            var resources = new List<Object>();
            try
            {
                var halls = new List<MountPlan>();
                var cuts = new List<GameObject>();
                BuildExpansionJoint(source, joint, resources, halls, designs, OpenHallsHash);
                ClearRepetitiveColumns(source, joint, halls, cuts);
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    var views = GridViews(joint, cuts);
                    string after = Path.Combine(GridReviewFolder, "Despues");
                    string before = Path.Combine(GridReviewFolder, "Antes");
                    Directory.CreateDirectory(after);
                    Directory.CreateDirectory(before);
                    CaptureExpansion(joint, views, after);
                    foreach (var cut in cuts) cut.SetActive(true);
                    CaptureExpansion(joint, views, before);
                    foreach (var cut in cuts) cut.SetActive(false);
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(joint);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
            Require(!source.isDirty && Hash() == OpenHallsHash, "La escena de origen cambió.");
        }

        static void ClearRepetitiveColumns(Scene source, Scene joint, List<MountPlan> halls, List<GameObject> cuts,
            bool verifyApproved = false)
        {
            var context = joint.GetRootGameObjects().Single(root => root.name == "Subsuelo definitivo - contexto temporal");
            var sourceRoots = source.GetRootGameObjects()
                .Where(root => root.name == WestRoot || root.name == EastRoot || root.name == NorthRoot || root.name == SouthRoot)
                .ToDictionary(root => root.name);
            var records = File.ReadLines(Path.Combine(OpenHallsPhase2, "Columnas.tsv")).Skip(1)
                .Select(row => row.Split('\t')).Where(row => row[2].StartsWith("04 - Columnas monumentales", StringComparison.Ordinal)
                    && row[3] == "Column_Ceiling_02").ToArray();
            Require(records.Length == 480, "El inventario ya no coincide con los 480 apoyos de retícula.");
            var entranceBounds = context.GetComponentsInChildren<Collider>()
                .Where(c => c.enabled && !c.isTrigger && c.bounds.max.y > Floor + 1 &&
                    c.bounds.min.y < Floor + 3 && IsEntrance(c.name)).Select(c => c.bounds).ToArray();
            var boundaries = halls.SelectMany(hall => hall.EndCapitals).ToArray();
            var rows = new List<string> { "ID\tRaíz\tPosición\tDecisión" };
            var probes = new List<Vector3>();
            var excluded = new HashSet<Collider>();
            var targets = new List<GameObject>();
            var world = joint.GetPhysicsScene();
            Physics.SyncTransforms();
            foreach (var row in records)
            {
                Require(GlobalObjectId.TryParse(row[0], out var id), "Identidad de retícula inválida.");
                var original = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                Require(original && original.scene == source && original.transform.root.gameObject == sourceRoots[row[1]],
                    "Pieza original ausente: " + row[0]);
                var part = original.transform;
                Require(Mathf.Abs(part.position.y + 12.019f) < .03f && part.parent.name == row[2],
                    "Pieza fuera de la retícula de Y=-16: " + row[0]);
                var copy = context.transform.Find(row[1]).GetChild(part.parent.GetSiblingIndex()).GetChild(part.GetSiblingIndex());
                Require(copy.name == original.name && (copy.position - part.position).sqrMagnitude < .000001f,
                    "Pieza temporal no coincide: " + row[0]);
                var ground = new Vector3(copy.position.x, Floor, copy.position.z);
                string decision;
                if (!copy.gameObject.activeInHierarchy)
                    decision = "Retirado por las naves";
                else if (entranceBounds.Any(b => HorizontalDistance(b, ground) < 1.2f) ||
                    boundaries.Any(b => HorizontalDistance(b, ground) < .1f))
                    decision = "Conservar para enmarcar un acceso";
                else if (Mathf.Abs(Mathf.RoundToInt(ground.x / 6)) % 8 == 1 &&
                    Mathf.Abs(Mathf.RoundToInt(ground.z / 6)) % 6 <= 1 &&
                    new[] {Vector3.right, Vector3.left, Vector3.forward, Vector3.back}
                        .Any(direction => !GroundAt(world, ground + direction * 7)))
                    decision = "Conservar como hito en el borde";
                else
                {
                    decision = "Desactivar retícula";
                    targets.Add(copy.gameObject);
                    foreach (var collider in copy.GetComponentsInChildren<Collider>(true)) excluded.Add(collider);
                    foreach (var offset in new[] { Vector3.left, Vector3.right, Vector3.back, Vector3.forward })
                    {
                        var sample = ground + offset * 2;
                        if (GroundAt(world, sample)) probes.Add(sample);
                    }
                }
                rows.Add(FormattableString.Invariant($"{row[0]}\t{row[1]}\t{ground:F2}\t{decision}"));
            }
            int previouslyRemoved = rows.Count(row => row.EndsWith("Retirado por las naves", StringComparison.Ordinal));
            Require(previouslyRemoved == 88, "La copia no contiene los 88 recortes de las 34 naves.");
            var before = ActiveGridColliders(context);
            foreach (var target in targets)
            {
                target.SetActive(false);
                cuts.Add(target);
            }
            Physics.SyncTransforms();
            before.ExceptWith(excluded);
            Require(before.SetEquals(ActiveGridColliders(context)), "Cambió un collider ajeno a la retícula.");
            Require(excluded.All(collider => !collider.gameObject.activeInHierarchy),
                "Quedó activo un collider asociado a un apoyo retirado.");
            Require(probes.All(sample => GroundAt(world, sample)), "Se alteró el piso de una zona de paso.");
            foreach (var hall in halls) ValidateMounted(hall.Hall, hall.Design, hall.EndCapitals);
            Directory.CreateDirectory(GridReviewFolder);
            string inventory = Path.Combine(GridReviewFolder, "Reticula.tsv");
            if (verifyApproved)
                Require(File.ReadAllLines(inventory).SequenceEqual(rows), "La retícula reconstruida difiere del inventario aprobado.");
            else
                File.WriteAllLines(inventory, rows);
            Debug.Log($"RETICULA_OK: 480 originales, {previouslyRemoved} retirados antes, {cuts.Count} retirados ahora, " +
                $"{480 - previouslyRemoved - cuts.Count} activos; {excluded.Count} colliders asociados; {probes.Count} comprobaciones de piso.");
        }

        static HashSet<Collider> ActiveGridColliders(GameObject context) =>
            new HashSet<Collider>(context.GetComponentsInChildren<Collider>().Where(c => c.enabled));

        static bool GroundAt(PhysicsScene world, Vector3 position) =>
            world.Raycast(position + Vector3.up * .18f, Vector3.down, out var hit, .32f, ~0, QueryTriggerInteraction.Ignore) &&
            Mathf.Abs(hit.point.y - Floor) < .05f && hit.normal.y > .85f;

        static float HorizontalDistance(Bounds bounds, Vector3 position)
        {
            position.y = bounds.center.y;
            return Mathf.Sqrt(bounds.SqrDistance(position));
        }

        static bool IsEntrance(string name) =>
            name.StartsWith("Arch", StringComparison.OrdinalIgnoreCase) ||
            name.IndexOf("Escal", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Stair", StringComparison.OrdinalIgnoreCase) >= 0;

        static List<MountPlan> GridViews(Scene joint, List<GameObject> cuts)
        {
            var world = joint.GetPhysicsScene();
            var result = new List<MountPlan>();
            foreach (var group in cuts.GroupBy(piece => piece.transform.parent.parent.name))
            {
                var positions = group.Select(piece => piece.transform.position).ToArray();
                var center = positions.Aggregate(Vector3.zero, (sum, p) => sum + p) / positions.Length;
                foreach (var position in positions.OrderBy(p => (p - center).sqrMagnitude))
                {
                    var foot = new Vector3(position.x + 2.5f, Floor, position.z);
                    if (!GroundAt(world, foot + Vector3.forward * 5) || !GroundAt(world, foot - Vector3.forward * 5))
                        continue;
                    var view = new Sector(group.Key, group.Key, foot.x - 1, foot.x + 1, foot.z - 7, foot.z + 7,
                        0, Array.Empty<float>(), Array.Empty<ulong>());
                    result.Add(new MountPlan(view, null, Array.Empty<GlobalObjectId>(), Array.Empty<Bounds>()));
                    break;
                }
            }
            Require(result.Count >= 3, "No se encontraron suficientes cámaras sobre el piso existente.");
            return result;
        }
    }
}
