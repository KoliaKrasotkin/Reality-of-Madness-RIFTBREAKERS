using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string ClearanceHash = "B3E9E06912A7EBD6F41FF5D5C35DBC5B5BF1D327E81FCBCB3D7ECF4E95B2D097";
        const string ClearanceBackup = "Backups/CiudadDelSol/SubsueloSinReticula/Dungeon_Isla_del_Sol.antes-integracion34.20261107-010007-709.unity";
        const string WarriorClearancePrefab = "Assets/ROM/Characters/Warrior/WarriorPlayer.prefab";

        public static void ReviewGridClearanceInBatch()
        {
            Require(Hash() == ClearanceHash && SceneFileHash(ClearanceBackup) == OpenHallsHash,
                "La escena o el respaldo difieren de la integración revisada.");
            string referencePath = AssetDatabase.GenerateUniqueAssetPath("Assets/ReferenciaHolguraTemporal.unity");
            Scene reference = default, integrated = default;
            try
            {
                File.Copy(ClearanceBackup, referencePath);
                AssetDatabase.ImportAsset(referencePath, ImportAssetOptions.ForceSynchronousImport);
                reference = EditorSceneManager.OpenPreviewScene(referencePath);
                integrated = EditorSceneManager.OpenPreviewScene(Original);
                Require(reference.GetPhysicsScene() != integrated.GetPhysicsScene(), "Las copias no tienen físicas aisladas.");
                Physics.SyncTransforms();
                var routes = RestrictedRouteSamples(reference.GetPhysicsScene());
                Require(routes.Sum(route => route.Value.Length) == 4801, "El muestreo difiere de la revisión original.");
                var world = integrated.GetPhysicsScene();
                var warrior = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorClearancePrefab).GetComponent<CharacterController>();
                var hits = new Collider[32];
                var report = new List<string>
                {
                    "Recorrido\tX\tY\tZ\tColisión cápsula anterior\tColisión cápsula Warrior\tDistancia al centro alternativo libre\tDesvío continuo de la cápsula"
                };
                foreach (var route in routes)
                {
                    Require(route.Value.All(p => GroundAt(world, p)), "Se alteró el piso de " + route.Key);
                    var clear = route.Value.Where(p => GridPassageClear(world, p)).ToArray();
                    Require(!GridRouteConnects(route.Value) ||
                        GridRouteConnects(clear, route.Value.Min(p => p.z), route.Value.Max(p => p.z)),
                        "Se interrumpió " + route.Key);
                    float south = route.Value.Min(p => p.z), north = route.Value.Max(p => p.z);
                    var referenceWorld = reference.GetPhysicsScene();
                    bool connectedBefore = WarriorPath(referenceWorld, route.Value, 1, south, north);
                    bool connectedAfter = WarriorPath(world, route.Value, 1, south, north);
                    Require(!connectedBefore || connectedAfter, "Se interrumpió el paso continuo del Warrior en " + route.Key);
                    Debug.Log($"HOLGURA_RUTA {route.Key}: {route.Value.Length} muestras; conexión continua antes={connectedBefore}, después={connectedAfter}");
                    foreach (var point in route.Value.Except(clear))
                    {
                        int previous = world.OverlapCapsule(point + Vector3.up * .36f, point + Vector3.up * 2.13f,
                            .32f, hits, ~0, QueryTriggerInteraction.Ignore);
                        string blockers = string.Join(" | ", hits.Take(previous).Select(ClearanceColliderPath));
                        int actual = WarriorOverlap(world, point);
                        string warriorBlockers = string.Join(" | ", hits.Take(actual).Select(ClearanceColliderPath));
                        float nearest = actual == 0 ? 0 : float.PositiveInfinity;
                        for (float offset = .1f; offset <= 1.51f && float.IsPositiveInfinity(nearest); offset += .1f)
                            for (int direction = 0; direction < 16; direction++)
                            {
                                float angle = direction * Mathf.PI / 8;
                                var shifted = point + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * offset;
                                if (GroundAt(world, shifted) && WarriorOverlap(world, shifted) == 0) nearest = offset;
                            }
                        var local = new List<Vector3>();
                        for (int z = -8; z <= 8; z++)
                        for (int x = -8; x <= 8; x++)
                            local.Add(point + new Vector3(x * .25f, 0, z * .25f));
                        bool bypass = WarriorPath(world, local, .25f, point.z - 2, point.z + 2);
                        string row = FormattableString.Invariant(
                            $"{route.Key}\t{point.x:F2}\t{point.y:F2}\t{point.z:F2}\t{blockers}\t{warriorBlockers}\t{nearest:F2}\t{bypass}");
                        report.Add(row);
                        Debug.Log("HOLGURA_PUNTO " + row);
                    }
                }
                Require(report.Count == 17, "La diferencia no coincide con los 16 puntos.");
                File.WriteAllLines("Backups/CiudadDelSol/Holgura-16.tsv", report);
                Debug.Log("HOLGURA_DIAGNOSTICO_OK: 4801 puntos, ocho trazados, 16 diferencias.");

                (Vector3 Lower, Vector3 Upper) WarriorCapsule(Vector3 p)
                {
                    // Small lift avoids counting the walkable floor as an obstruction.
                    var center = p + warrior.center + Vector3.up * .02f;
                    float span = warrior.height * .5f - warrior.radius;
                    return (center - Vector3.up * span, center + Vector3.up * span);
                }

                int WarriorOverlap(PhysicsScene scene, Vector3 p)
                {
                    var capsule = WarriorCapsule(p);
                    return scene.OverlapCapsule(capsule.Lower, capsule.Upper, warrior.radius, hits, ~0,
                        QueryTriggerInteraction.Ignore);
                }

                bool WarriorPath(PhysicsScene scene, IEnumerable<Vector3> points, float spacing, float south, float north)
                {
                    var available = points.Where(p => GroundAt(scene, p) && WarriorOverlap(scene, p) == 0).ToHashSet();
                    var starts = available.Where(p => p.z == south).ToArray();
                    var pending = new Queue<Vector3>(starts);
                    foreach (var start in starts) available.Remove(start);
                    var directions = new[] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
                    while (pending.Count > 0)
                    {
                        var point = pending.Dequeue();
                        if (point.z == north) return true;
                        var capsule = WarriorCapsule(point);
                        foreach (var direction in directions)
                        {
                            var next = point + direction * spacing;
                            if (!available.Contains(next) ||
                                !GroundAt(scene, point + direction * (spacing * .5f)) ||
                                scene.CapsuleCast(capsule.Lower, capsule.Upper, warrior.radius, direction, out _,
                                    spacing, ~0, QueryTriggerInteraction.Ignore)) continue;
                            available.Remove(next);
                            pending.Enqueue(next);
                        }
                    }
                    return false;
                }
            }
            finally
            {
                if (integrated.IsValid()) EditorSceneManager.ClosePreviewScene(integrated);
                if (reference.IsValid()) EditorSceneManager.ClosePreviewScene(reference);
                AssetDatabase.DeleteAsset(referencePath);
            }
            Require(Hash() == ClearanceHash, "La escena definitiva cambió.");
        }

        static string ClearanceColliderPath(Collider collider)
        {
            var parts = new Stack<string>();
            for (var part = collider.transform; part; part = part.parent) parts.Push(part.name);
            return string.Join(" / ", parts) + " [" + collider.GetType().Name + "]";
        }
    }
}
