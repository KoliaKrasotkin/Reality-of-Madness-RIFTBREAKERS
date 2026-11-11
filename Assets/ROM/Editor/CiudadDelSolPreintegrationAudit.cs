using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        public static void AuditAmbientIntegrationInBatch()
        {
            string output = File.ReadAllText("Backups/CiudadDelSol/AuditoriaPreintegracion/UltimaRuta.txt").Trim();
            var hash = Hash();
            var scene = EditorSceneManager.OpenPreviewScene(Original);
            var resources = new List<Object>();
            try
            {
                var roots = scene.GetRootGameObjects();
                ValidateFinalLook(scene);
                var rocks = roots.Single(r => r.name == RockCoverMount);
                var rockHash = ValidateRockCoverMount(rocks, true);
                var original = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                var originalSignature = original.Select(Signature).ToArray();
                var city = roots.Single(r => r.name == "Dungeon_Ciudad_del_Sol_Generated");
                var cityNodes = city.GetComponentsInChildren<Transform>(true);
                string[] zoneNames = { "01 - Entrada Portico Solar", "02 - Jardines Dorados", "03 - Plaza del Eclipse",
                    "04 - Archivos Solares", "05 - Claustro del Eclipse", "06 - Santuario Profano", "07 - El Sol Corrompido" };
                var zones = zoneNames.Select(n => cityNodes.Single(t => t.name == n)).ToArray();
                var routes = cityNodes.Where(t => Regex.IsMatch(t.name, @"^\d\d-\d\d - .*ancho util")).OrderBy(t => t.name).ToArray();
                Require(routes.Length == 9 && zones.All(t => t.gameObject.activeInHierarchy) &&
                    routes.All(t => t.gameObject.activeInHierarchy), "Falta alguna zona o recorrido activo.");
                var core = roots.Single(r => r.name == "Nucleo_Monumental_Conjunto");
                var vault = core.GetComponentsInChildren<Transform>(true).Single(t => t.name == "03 - El Sol Invertido V3");
                Require(vault.gameObject.activeInHierarchy &&
                    vault.GetComponentsInChildren<Transform>().Any(t => t.name == "SolInvertido - corona original inmovil"),
                    "No se encontró El Sol Invertido V3 con su corona.");

                var ruins = BuildExteriorRuins(scene, roots, resources, out _, out var generation, ambiance: true);
                Require(generation.Contains("6500 piezas") && generation.Contains("132 ruinas, 207 cristales") &&
                    generation.Contains("Densificación: 441 cristales") && generation.Contains("74 ruinas;") &&
                    generation.Contains("600/300/250/350"), "El contenido regenerado difiere de la propuesta anterior.");
                var ambiance = scene.GetRootGameObjects().Single(r => r.name == AmbientRoot);
                var layers = new[] { ruins, ambiance };
                var temporaryNodes = layers.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                Require(layers.Sum(r => r.GetComponentsInChildren<Collider>(true).Length) == 0 &&
                    layers.Sum(r => r.GetComponentsInChildren<Rigidbody>(true).Length) == 0,
                    "Se encontraron colliders o cuerpos físicos en la propuesta.");
                Require(original.Select(Signature).SequenceEqual(originalSignature) &&
                    ValidateRockCoverMount(rocks, true) == rockHash, "La propuesta alteró objetos originales.");
                Require(scene.GetRootGameObjects().Length == roots.Length + 2, "Aparecieron raíces temporales inesperadas.");
                ValidateFinalLook(scene);
                var filters = temporaryNodes.Select(t => t.GetComponent<MeshFilter>()).Where(f => f).ToArray();
                var renderers = filters.Select(f => f.GetComponent<MeshRenderer>()).ToArray();
                Require(filters.All(f => f.sharedMesh) && renderers.All(r => r && r.sharedMaterials.Length > 0 &&
                    r.sharedMaterials.All(m => m && m.shader && m.shader.isSupported)), "Referencias visuales incompletas.");

                var meshes = filters.Select(f => f.sharedMesh).Distinct().ToArray();
                var materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
                var keys = new Dictionary<(Mesh mesh, Matrix4x4 matrix), MeshFilter>();
                foreach (var f in original.Select(t => t.GetComponent<MeshFilter>()).Where(f => f && f.sharedMesh))
                    keys.TryAdd((f.sharedMesh, f.transform.localToWorldMatrix), f);
                var duplicates = new List<string>();
                foreach (var f in filters)
                {
                    var key = (f.sharedMesh, f.transform.localToWorldMatrix);
                    if (!keys.TryAdd(key, f))
                        duplicates.Add($"{NodePath(f.transform)} | {NodePath(keys[key].transform)} | {f.sharedMesh.name} | {f.transform.position:F3}");
                }
                var routeAudit = new List<string>();
                foreach (var route in routes)
                {
                    var slabs = route.GetComponentsInChildren<Collider>()
                        .Where(c => c.enabled && !c.isTrigger && c.bounds.size.x >= 1 && c.bounds.size.z >= 1)
                        .Select(c => c.bounds)
                        .Where(b => b.size.y <= 5)
                        .Select(b => new Bounds(new Vector3(b.center.x, b.max.y + .96f, b.center.z),
                            new Vector3(b.size.x, 1.85f, b.size.z))).ToArray();
                    var candidates = renderers.Where(r => slabs.Any(b => b.Intersects(r.bounds))).ToArray();
                    var contacts = new List<string>();
                    foreach (var r in candidates)
                    {
                        var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                        if (!mesh.isReadable)
                        {
                            contacts.Add("SIN LECTURA " + NodePath(r.transform));
                            continue;
                        }
                        var near = slabs.Where(b => b.Intersects(r.bounds)).ToArray();
                        if (mesh.vertices.Any(v => near.Any(b => b.Contains(r.transform.TransformPoint(v)))))
                            contacts.Add(NodePath(r.transform));
                    }
                    routeAudit.Add($"{route.name}: {slabs.Length} superficies; {candidates.Length} cruces de cajas; " +
                        $"{contacts.Count} con vértices en la envolvente o malla sin lectura. " + string.Join(" | ", contacts.Take(14)));
                }

                var lamps = layers.SelectMany(r => r.GetComponentsInChildren<Light>(true)).ToArray();
                int pointsInLook = roots.Single(r => r.name == FinalLookRoot).GetComponentsInChildren<Light>()
                    .Count(l => l.type == LightType.Point);
                int overlappingLights = lamps.Count(l => roots.Single(r => r.name == FinalLookRoot)
                    .GetComponentsInChildren<Light>().Any(p => p.type == LightType.Point &&
                        Vector3.Distance(p.transform.position, l.transform.position) < p.range + l.range));
                int triangles = filters.Sum(f => Enumerable.Range(0, f.sharedMesh.subMeshCount)
                    .Sum(i => (int)f.sharedMesh.GetIndexCount(i) / 3));
                File.WriteAllLines(Path.Combine(output, "Duplicados-exactos.txt"), duplicates);
                File.WriteAllLines(Path.Combine(output, "Recorridos.txt"), routeAudit);
                File.WriteAllLines(Path.Combine(output, "Dependencias-escena.txt"), AssetDatabase.GetDependencies(Original, true));
                File.WriteAllText(Path.Combine(output, "Auditoria.txt"), generation +
                    $"\n\nEscena abierta correctamente en contexto de solo inspección. SHA-256: {hash}\n" +
                    $"Firma de cobertura: {rockHash}\n" +
                    $"Originales con firma intacta: {original.Length}; 7 zonas activas, 9 recorridos activos, {vault.name}.\n" +
                    $"Capas: {temporaryNodes.Length} objetos, {renderers.Length} renderizadores, {triangles} triángulos máximos. " +
                    $"{meshes.Length} mallas únicas ({meshes.Count(m => !EditorUtility.IsPersistent(m))} temporales); " +
                    $"{materials.Length} materiales ({materials.Count(m => !EditorUtility.IsPersistent(m))} temporales).\n" +
                    $"Coincidencias exactas de malla y matriz: {duplicates.Count}. Consultar Duplicados-exactos.txt.\n" +
                    $"Luces: {lamps.Length} nuevas sin sombras; {pointsInLook} puntuales ya integradas en atmósfera; " +
                    $"{overlappingLights} luces nuevas con alcance coincidente con al menos una integrada.\n" +
                    "0 colliders y 0 cuerpos físicos añadidos. Auditoría de envolventes y vértices, sin prueba dinámica ni perfilado.\n");
                Require(Hash() == hash, "Cambió la escena definitiva.");
                Debug.Log($"AUDITORIA_PREINTEGRACION_OK duplicados={duplicates.Count}, renderizadores={renderers.Length}, triangulos={triangles}");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }

            string Signature(Transform t) =>
                $"{t.GetInstanceID()}|{t.parent?.GetInstanceID()}|{t.GetSiblingIndex()}|{t.name}|{t.gameObject.activeSelf}|" +
                $"{t.localPosition:R}|{t.localRotation:R}|{t.localScale:R}|" +
                string.Join(",", t.GetComponents<Component>().Select(c => c ? c.GetInstanceID().ToString() : "missing"));

            string NodePath(Transform t) => AnimationUtility.CalculateTransformPath(t, null);
        }
    }
}

