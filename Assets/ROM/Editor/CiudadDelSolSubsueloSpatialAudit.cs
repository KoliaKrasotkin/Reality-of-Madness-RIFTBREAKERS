using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    public static class CiudadDelSolSubsueloSpatialAudit
    {
        const string ScenePath = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string ReportPath = "Backups/CiudadDelSol/AuditoriaEspacial";
        const int Step = 3;
        const float MaxWidthRay = 18;
        static readonly int[] Levels = { -16, -24, -30, -34, -38 };
        static readonly Vector3[] Sides = { Vector3.left, Vector3.right, Vector3.back, Vector3.forward };

        public static void Batch() => Scan(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));

        [MenuItem("ROM/Dungeon Ciudad del Sol/Rediseño subsuelo/Auditar alturas y recorridos")]
        static void ScanOpenScene() => Scan(SceneManager.GetActiveScene());

        static void Scan(Scene scene)
        {
            if (scene.path != ScenePath || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode ||
                StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
                throw new InvalidOperationException("Abrí la escena definitiva limpia en modo edición.");

            var roots = scene.GetRootGameObjects().Where(root =>
                root.name.StartsWith("Subsuelo_", StringComparison.Ordinal) ||
                root.name == "Nucleo_Monumental_Conjunto").ToArray();
            var colliders = roots.SelectMany(root => root.GetComponentsInChildren<Collider>())
                .Where(collider => collider.enabled && !collider.isTrigger).ToArray();
            if (colliders.Length == 0) throw new InvalidOperationException("No se encontraron colliders del subsuelo.");
            Bounds bounds = colliders[0].bounds;
            foreach (var collider in colliders) bounds.Encapsulate(collider.bounds);

            Physics.SyncTransforms();
            var hits = new RaycastHit[64];
            var floorColliders = new HashSet<Collider>(colliders);
            var physics = scene.GetPhysicsScene();
            int points = 0;
            Directory.CreateDirectory(ReportPath);
            using var grid = new StreamWriter(Path.Combine(ReportPath, "Muestras.tsv"));
            grid.WriteLine("X\tY\tZ\tRaiz piso\tAltura libre\tDespeje X a 1.90 m\tDespeje Z a 1.90 m\tObstaculo superior");
            foreach (int y in Levels)
            for (int z = Mathf.FloorToInt(bounds.min.z); z <= bounds.max.z; z += Step)
            for (int x = Mathf.FloorToInt(bounds.min.x); x <= bounds.max.x; x += Step)
            {
                var origin = new Vector3(x, y, z);
                if (!physics.Raycast(origin + Vector3.up * .25f, Vector3.down, out var floor, .4f,
                        ~0, QueryTriggerInteraction.Ignore) ||
                    floor.normal.y < .85f || !floorColliders.Contains(floor.collider))
                    continue;
                var foot = floor.point;
                var overhead = Nearest(foot + Vector3.up * .08f, Vector3.up, 36, floor.collider);
                var eye = foot + Vector3.up * 1.90f;
                var left = Nearest(eye, Sides[0], MaxWidthRay, floor.collider);
                var right = Nearest(eye, Sides[1], MaxWidthRay, floor.collider);
                var back = Nearest(eye, Sides[2], MaxWidthRay, floor.collider);
                var front = Nearest(eye, Sides[3], MaxWidthRay, floor.collider);
                grid.WriteLine(FormattableString.Invariant(
                    $"{x}\t{y}\t{z}\t{floor.collider.transform.root.name}\t{Value(overhead, .08f)}\t{Width(left, right)}\t{Width(back, front)}\t{overhead.collider?.name}"));
                points++;
            }
            if (points == 0) throw new InvalidOperationException("La grilla no encontró superficies físicas del subsuelo.");

            using var pieces = new StreamWriter(Path.Combine(ReportPath, "Piezas.tsv"));
            pieces.WriteLine("ID\tRaiz\tPadre\tPieza\tActiva\tCategoria preliminar\tX\tY\tZ");
            foreach (var root in roots)
            foreach (var piece in root.GetComponentsInChildren<Transform>(true))
            {
                string name = piece.name;
                string category;
                if (name.StartsWith("Column_Ceiling", StringComparison.OrdinalIgnoreCase))
                    category = "Capitel; comprobar encuentro";
                else if (name.StartsWith("Column_", StringComparison.OrdinalIgnoreCase) ||
                         name.IndexOf("pilar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         name.IndexOf("apoyo", StringComparison.OrdinalIgnoreCase) >= 0)
                    category = "Apoyo candidato; comprobar relaciones";
                else if (name.StartsWith("Ceiling_Beam", StringComparison.OrdinalIgnoreCase))
                    category = "Viga de cubierta";
                else if (name.StartsWith("Base_Ceiling", StringComparison.OrdinalIgnoreCase))
                    category = "Modulo de cubierta";
                else continue;
                var position = piece.position;
                pieces.WriteLine(FormattableString.Invariant(
                    $"{GlobalObjectId.GetGlobalObjectIdSlow(piece.gameObject)}\t{root.name}\t{piece.parent?.name}\t{name}\t{piece.gameObject.activeInHierarchy}\t{category}\t{position.x:F3}\t{position.y:F3}\t{position.z:F3}"));
            }
            Debug.Log($"SUBSUELO_AUDITORIA_ESPACIAL_OK: {points} muestras cada {Step} m, {colliders.Length} colliders de referencia. {ReportPath}");

            RaycastHit Nearest(Vector3 origin, Vector3 direction, float distance, Collider ignore)
            {
                int count = physics.Raycast(origin, direction, hits, distance, ~0, QueryTriggerInteraction.Ignore);
                if (count == hits.Length)
                    throw new InvalidOperationException($"Demasiadas superficies en la muestra {origin}.");
                RaycastHit nearest = default;
                for (int i = 0; i < count; i++)
                    if (hits[i].collider != ignore && (!nearest.collider || hits[i].distance < nearest.distance))
                        nearest = hits[i];
                return nearest;
            }
        }

        static string Value(RaycastHit hit, float offset) =>
            hit.collider ? (hit.distance + offset).ToString("F2", CultureInfo.InvariantCulture) : ">36";

        static string Width(RaycastHit a, RaycastHit b)
        {
            float sum = (a.collider ? a.distance : MaxWidthRay) + (b.collider ? b.distance : MaxWidthRay);
            return (a.collider && b.collider ? "" : ">") + sum.ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}
