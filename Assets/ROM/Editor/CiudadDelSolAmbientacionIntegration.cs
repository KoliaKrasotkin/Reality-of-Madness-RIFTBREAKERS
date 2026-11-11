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
        const string VisualAssets = "Assets/ROM/CiudadDelSolAmbientacion";
        const string VisualMount = "Ciudad del Sol · ambientación visual";
        const string VisualSceneHash = "360CEF605D027460F38A91CA6A48189BA1162FAF2D6C5FD58E968AF5AB1850BA";
        const string VisualMetaHash = "BAF742BC0B4748C397924E31FCABA240BF93E3972DC05EACFA7989B9A8CB2B17";
        const string AuditedRocksHash = "18DB8A93632B33A2C4961FE343174DB31544F216D57E543CB1D4E858971F0171";
        const string VisualOutput = "Backups/CiudadDelSol/IntegracionAmbientacion";
        const string VisualCandidate = VisualAssets + "/Candidata.unity";

        public static void IntegrateAuditedVisualsInBatch()
        {
            Require(Application.isBatchMode, "Se requiere Unity en modo batch.");
            string backup = VerifyVisualBackup();
            Require(Hash() == VisualSceneHash && !AssetDatabase.IsValidFolder(VisualAssets),
                "La escena cambió desde la auditoría o ya existe el directorio de integración.");
            Directory.CreateDirectory(VisualOutput);
            var scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            ValidateVisualBase(scene);
            Require(!scene.isDirty && scene.GetRootGameObjects().All(r => r.name != VisualMount),
                "La escena está modificada o ya contiene las capas.");
            var resources = new List<Object>();
            bool folderCreated = false, promoted = false;
            try
            {
                var ruins = BuildExteriorRuins(scene, scene.GetRootGameObjects(), resources, out _, out var report, ambiance: true);
                foreach (string expected in new[] { "6500 piezas", "600/300/250/350", "132 ruinas, 207 cristales",
                    "Densificación: 441 cristales", "74 ruinas;", "22539 matas", "6 formaciones, 192 fragmentos" })
                    Require(report.Contains(expected), "La propuesta no coincide con el inventario auditado: " + expected);

                var crystals = ruins.transform.Find("Campos dimensionales · temporales");
                var ambient = scene.GetRootGameObjects().Single(r => r.name == AmbientRoot);
                var plants = ambient.transform.Find(AmbientPlants);
                var energy = ambient.transform.Find(AmbientEnergy);
                var mount = new GameObject(VisualMount);
                crystals.SetParent(mount.transform, true);
                ruins.transform.SetParent(mount.transform, true);
                plants.SetParent(mount.transform, true);
                energy.SetParent(mount.transform, true);
                crystals.name = "Cristales";
                ruins.name = "Ruinas";
                Object.DestroyImmediate(ambient);
                var surface = ruins.transform.Find(SurfaceCorrectionRoot);
                var dense = surface.Find(VaultDensityRoot);
                foreach (var bucket in surface.Cast<Transform>().Concat(dense.Cast<Transform>())
                    .Where(t => t.name.StartsWith("Agrupación ", StringComparison.Ordinal)).ToArray())
                {
                    var parent = bucket.name.EndsWith("/0", StringComparison.Ordinal) ||
                        bucket.name.EndsWith("/1", StringComparison.Ordinal) ? plants : crystals;
                    bucket.SetParent(parent, true);
                }

                var integratedLights = scene.GetRootGameObjects().Single(r => r.name == FinalLookRoot)
                    .GetComponentsInChildren<Light>().Where(l => l.type == LightType.Point).ToArray();
                var lightDecisions = new List<string>();
                foreach (var light in energy.GetComponentsInChildren<Light>())
                {
                    var nearest = integratedLights.OrderBy(l => Vector3.Distance(l.transform.position, light.transform.position)
                        - l.range - light.range).First();
                    float distance = Vector3.Distance(light.transform.position, nearest.transform.position);
                    bool overlap = distance < nearest.range + light.range;
                    lightDecisions.Add($"{light.transform.parent.name} {light.transform.position:F2}: " +
                        $"distancia {distance:F2} m / alcances {light.range:F2} + {nearest.range:F2} m; " +
                        (overlap ? "puntual omitida; emisivos conservados" : "puntual conservada sin sombras"));
                    if (overlap) Object.DestroyImmediate(light.gameObject);
                }
                Require(lightDecisions.Count == 4 && energy.GetComponentsInChildren<Light>().Length == 2,
                    "La superposición de luces difiere de la auditoría.");

                Require(!string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/ROM", "CiudadDelSolAmbientacion")),
                    "No se pudo preparar la carpeta de assets.");
                folderCreated = true;
                PersistVisualAssets(mount);
                var details = ValidateVisualMount(scene);
                int records = mount.GetComponentsInChildren<Transform>(true).Sum(t => 1 + t.GetComponents<Component>().Length);
                Require(EditorSceneManager.SaveScene(scene, VisualCandidate, true), "No se pudo guardar la candidata.");
                ValidateIslandSceneDelta(backup, VisualCandidate, records);

                var candidate = EditorSceneManager.OpenScene(VisualCandidate, OpenSceneMode.Single);
                ValidateVisualBase(candidate);
                Require(ValidateVisualMount(candidate) == details && !candidate.isDirty &&
                    Hash() == VisualSceneHash && VerifyVisualBackup() == backup, "Falló la validación de la candidata.");
                string expectedHash = SceneFileHash(VisualCandidate);
                string swap = Path.Combine(VisualOutput, "Promocion.tmp");
                File.Copy(VisualCandidate, swap, true);
                File.Replace(swap, Original, null);
                promoted = true;
                AssetDatabase.ImportAsset(Original, ImportAssetOptions.ForceSynchronousImport);
                ValidateSavedVisualScene(expectedHash, details);
                Require(AssetDatabase.DeleteAsset(VisualCandidate), "No se pudo retirar la candidata.");
                File.WriteAllLines(Path.Combine(VisualOutput, "Control.txt"), new[] { expectedHash, backup });
                File.WriteAllText(Path.Combine(VisualOutput, "Integracion.txt"),
                    "Escena SHA-256: " + expectedHash + "\nRespaldo: " + backup + "\n\n" + details + "\n" +
                    string.Join("\n", lightDecisions) + "\n\n" + report);
                Debug.Log("AMBIENTACION_INTEGRADA_OK " + expectedHash);
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (promoted) RestoreVisualBackup(backup);
                if (folderCreated) AssetDatabase.DeleteAsset(VisualAssets);
                throw;
            }
            finally
            {
                foreach (var resource in resources)
                    if (resource && !EditorUtility.IsPersistent(resource)) Object.DestroyImmediate(resource);
            }
        }

        public static void VerifyAuditedVisualsAfterRestartInBatch()
        {
            Require(Application.isBatchMode, "Se requiere Unity en modo batch.");
            var control = File.ReadAllLines(Path.Combine(VisualOutput, "Control.txt"));
            string backup = VerifyVisualBackup();
            Require(control.Length == 2 && control[1] == backup && Hash() == control[0],
                "La escena guardada cambió antes de la verificación en un segundo proceso.");
            try
            {
                ValidateSavedVisualScene(control[0], null);
                File.AppendAllText(Path.Combine(VisualOutput, "Integracion.txt"),
                    "\n\nPersistencia confirmada al cerrar Unity y abrir la escena en un segundo proceso.\n");
                Debug.Log("AMBIENTACION_REAPERTURA_OK " + control[0]);
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RestoreVisualBackup(backup);
                AssetDatabase.DeleteAsset(VisualAssets);
                throw;
            }
        }

        static void PersistVisualAssets(GameObject mount)
        {
            var meshes = mount.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh)
                .Where(m => m && !EditorUtility.IsPersistent(m)).Distinct().ToArray();
            var materials = mount.GetComponentsInChildren<MeshRenderer>().SelectMany(r => r.sharedMaterials)
                .Where(m => m && !EditorUtility.IsPersistent(m)).Distinct().ToArray();
            Require(meshes.Length == 784 && materials.Length == 25, "Los recursos temporales difieren de la auditoría.");

            var persisted = new HashSet<Mesh>();
            foreach (var layer in mount.transform.Cast<Transform>())
            {
                var owned = layer.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh)
                    .Where(m => !EditorUtility.IsPersistent(m) && persisted.Add(m)).ToArray();
                if (owned.Length == 0) continue;
                string path = VisualAssets + "/Mallas_" + layer.name.Replace(" ", "_") + ".asset";
                AssetDatabase.CreateAsset(owned[0], path);
                foreach (var mesh in owned.Skip(1)) AssetDatabase.AddObjectToAsset(mesh, owned[0]);
            }
            Require(persisted.Count == meshes.Length, "Faltan mallas por persistir.");
            for (int i = 0; i < materials.Length; i++)
                AssetDatabase.CreateAsset(materials[i], VisualAssets + $"/Material_{i + 1:00}.mat");
            AssetDatabase.SaveAssets();
        }

        static string ValidateVisualMount(Scene scene)
        {
            var mount = scene.GetRootGameObjects().Single(r => r.name == VisualMount);
            var layers = mount.transform.Cast<Transform>().OrderBy(t => t.name, StringComparer.Ordinal).ToArray();
            Require(layers.Select(l => l.name).SequenceEqual(new[] { "Cristales", AmbientEnergy, "Ruinas", AmbientPlants }) &&
                layers.All(l => l.gameObject.activeInHierarchy) &&
                mount.GetComponentsInChildren<Collider>(true).Length == 0 &&
                mount.GetComponentsInChildren<Rigidbody>(true).Length == 0, "Jerarquía o seguridad de las capas incorrectas.");
            var filters = mount.GetComponentsInChildren<MeshFilter>(true);
            var renderers = mount.GetComponentsInChildren<MeshRenderer>(true);
            Require(filters.Length == 11844 && renderers.Length == 11844 &&
                layers.Single(l => l.name == AmbientEnergy).childCount == 6 &&
                mount.GetComponentsInChildren<Light>().Length == 2 &&
                mount.GetComponentsInChildren<Light>().All(l => l.shadows == LightShadows.None),
                "La geometría o el número de formaciones difieren de lo previsto.");
            Require(filters.All(f => f.sharedMesh && EditorUtility.IsPersistent(f.sharedMesh)) &&
                renderers.All(r => r.sharedMaterials.Length > 0 && r.sharedMaterials.All(m => m &&
                    EditorUtility.IsPersistent(m) && m.shader && m.shader.isSupported && EditorUtility.IsPersistent(m.shader) &&
                    m.GetTexturePropertyNames().All(p => !m.GetTexture(p) || EditorUtility.IsPersistent(m.GetTexture(p))))) &&
                filters.Select(f => f.sharedMesh).Distinct().Count(m => AssetDatabase.GetAssetPath(m).StartsWith(VisualAssets + "/")) == 784 &&
                renderers.SelectMany(r => r.sharedMaterials).Distinct().Count(m => AssetDatabase.GetAssetPath(m).StartsWith(VisualAssets + "/")) == 25,
                "Faltan mallas, materiales, shaders o texturas persistentes.");
            var occupied = scene.GetRootGameObjects().Where(r => r != mount).SelectMany(r => r.GetComponentsInChildren<MeshFilter>(true))
                .Where(f => f.sharedMesh).Select(f => (f.sharedMesh, f.transform.localToWorldMatrix)).ToHashSet();
            Require(filters.All(f => occupied.Add((f.sharedMesh, f.transform.localToWorldMatrix))), "Malla y transformación duplicadas.");
            return "Capas integradas (incluyen objetos activos e inactivos, triángulos máximos):\n" +
                string.Join("\n", layers.Select(l => VisualStats(l.gameObject.name, new[] { l.gameObject }))) +
                "\n" + VisualStats("Total nuevo", new[] { mount }) +
                "\n" + VisualStats("Escena completa", scene.GetRootGameObjects()) +
                "\n784 mallas y 25 materiales temporales convertidos a assets independientes.";
        }

        static string VisualStats(string name, GameObject[] roots)
        {
            int Count<T>() where T : Component => roots.Sum(r => r.GetComponentsInChildren<T>(true).Length);
            var renderers = roots.SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).ToArray();
            var meshes = roots.SelectMany(r => r.GetComponentsInChildren<MeshFilter>(true)).Select(f => f.sharedMesh)
                .Concat(roots.SelectMany(r => r.GetComponentsInChildren<SkinnedMeshRenderer>(true)).Select(r => r.sharedMesh));
            long triangles = meshes.Where(m => m).Sum(m => Enumerable.Range(0, m.subMeshCount)
                .Where(i => m.GetTopology(i) == MeshTopology.Triangles).Sum(i => (long)m.GetIndexCount(i) / 3));
            return $"{name}: {Count<Transform>()} GameObjects, {renderers.Length} renderizadores, {triangles} triángulos, " +
                $"{renderers.SelectMany(r => r.sharedMaterials).Where(m => m).Distinct().Count()} materiales, " +
                $"{Count<Light>()} luces, {Count<LODGroup>()} grupos LOD.";
        }

        static void ValidateVisualBase(Scene scene)
        {
            ValidateFinalLook(scene);
            var roots = scene.GetRootGameObjects();
            Require(ValidateRockCoverMount(roots.Single(r => r.name == RockCoverMount), true) == AuditedRocksHash,
                "La cobertura rocosa difiere de la auditada.");
            var city = roots.Single(r => r.name == "Dungeon_Ciudad_del_Sol_Generated");
            var nodes = city.GetComponentsInChildren<Transform>();
            var routes = nodes.Where(t => Regex.IsMatch(t.name, @"^\d\d-\d\d - .*ancho util")).ToArray();
            Require(routes.Length == 9 && routes.All(t => t.GetComponentsInChildren<Collider>().Any(c => c.enabled && !c.isTrigger)),
                "Faltan rutas activas o sus colliders.");
            var core = roots.Single(r => r.name == "Nucleo_Monumental_Conjunto");
            Require(core.GetComponentsInChildren<Transform>().Any(t => t.name == "03 - El Sol Invertido V3") &&
                core.GetComponentsInChildren<Transform>().Any(t => t.name == "SolInvertido - corona original inmovil"),
                "No se conserva El Sol Invertido V3.");
        }

        static void ValidateSavedVisualScene(string expected, string details)
        {
            var scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            ValidateVisualBase(scene);
            string savedDetails = ValidateVisualMount(scene);
            Require(!scene.isDirty && Hash() == expected && SceneFileHash(Original + ".meta") == VisualMetaHash &&
                (details == null || savedDetails == details), "Falló la persistencia de la escena definitiva.");
        }

        static string VerifyVisualBackup()
        {
            string folder = File.ReadAllText("Backups/CiudadDelSol/AuditoriaPreintegracion/UltimaRuta.txt").Trim();
            string backup = Path.Combine(folder, Path.GetFileName(Original));
            Require(File.Exists(backup) && SceneFileHash(backup) == VisualSceneHash &&
                File.Exists(backup + ".meta") && SceneFileHash(backup + ".meta") == VisualMetaHash &&
                SceneFileHash(Original + ".meta") == VisualMetaHash, "Respaldo o GUID de escena incorrectos.");
            return backup;
        }

        static void RestoreVisualBackup(string backup)
        {
            File.Copy(backup, Original, true);
            Require(Hash() == VisualSceneHash, "Falló la restauración del respaldo.");
            AssetDatabase.ImportAsset(Original, ImportAssetOptions.ForceSynchronousImport);
        }
    }
}

