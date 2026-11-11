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
        const string IslandMount = "Masa continental · exterior";
        const string IslandAssets = "Assets/ROM/CiudadDelSolMasaFlotante";
        const string IslandCandidate = IslandAssets + "/Candidata.unity";

        public static void IntegrateIslandInBatch()
        {
            Require(Application.isBatchMode && Hash() == ClearanceHash, "La escena no coincide con la versión aprobada.");
            var scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            Require(!scene.isDirty && !Directory.Exists(IslandAssets) &&
                roots.All(root => !root.name.StartsWith("Masa continental", StringComparison.Ordinal)),
                "La escena no está limpia o ya existe una masa flotante.");

            Directory.CreateDirectory(IslandOutput);
            string backup = Path.Combine(IslandOutput,
                $"Dungeon_Isla_del_Sol.antes-masa.{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.unity");
            File.Copy(Original, backup);
            Require(SceneFileHash(backup) == ClearanceHash, "Falló el SHA-256 del respaldo.");
            File.WriteAllText(backup + ".sha256", ClearanceHash + Environment.NewLine);
            Debug.Log("ISLA_RESPALDO_OK " + backup + " " + ClearanceHash);

            var resources = new List<Object>();
            bool folderCreated = false, promoted = false;
            try
            {
                var island = BuildIsland(scene, roots.Where(IsIslandArchitecture).ToArray(), resources, out _, out var report);
                island.name = IslandMount;
                ValidateIslandMount(scene, false);
                foreach (var line in report.Take(6)) Debug.Log("ISLA_VALIDACION " + line);

                Require(!string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/ROM", "CiudadDelSolMasaFlotante")),
                    "No se pudo crear la carpeta de assets.");
                folderCreated = true;
                int i = 0;
                foreach (var mesh in resources.OfType<Mesh>())
                    AssetDatabase.CreateAsset(mesh, IslandAssets + $"/Acantilado_{++i:00}.asset");
                i = 0;
                foreach (var material in resources.OfType<Material>())
                {
                    var names = new[] { "Roca", "RocaOscura", "RocaClara", "Tierra" };
                    material.name = names[i++];
                    AssetDatabase.CreateAsset(material, IslandAssets + "/" + material.name + ".mat");
                }
                AssetDatabase.SaveAssets();
                ValidateIslandMount(scene, true);
                Require(Hash() == ClearanceHash && EditorSceneManager.SaveScene(scene, IslandCandidate, true),
                    "La escena original cambió o no se pudo guardar la candidata.");

                ValidateIslandSceneDelta(backup, IslandCandidate);
                var candidate = EditorSceneManager.OpenScene(IslandCandidate, OpenSceneMode.Single);
                ValidateIslandMount(candidate, true);
                Require(!candidate.isDirty && Hash() == ClearanceHash && SceneFileHash(backup) == ClearanceHash,
                    "Cambió la escena original o su respaldo antes de la integración.");

                var expected = SceneFileHash(IslandCandidate);
                var swap = Path.Combine(IslandOutput, "MasaFlotante-montaje.tmp");
                File.Copy(IslandCandidate, swap);
                File.Replace(swap, Original, null);
                promoted = true;
                AssetDatabase.ImportAsset(Original, ImportAssetOptions.ForceSynchronousImport);
                var reopened = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
                ValidateIslandMount(reopened, true);
                Require(!reopened.isDirty && Hash() == expected, "Falló la persistencia de la escena definitiva.");
                Require(AssetDatabase.DeleteAsset(IslandCandidate), "No se pudo retirar la escena candidata.");
                Debug.Log($"ISLA_INTEGRACION_PERSISTENCIA_OK SHA256={expected}; respaldo={backup}");
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (promoted)
                {
                    File.Copy(backup, Original, true);
                    Require(Hash() == ClearanceHash, "Falló la restauración del respaldo.");
                    AssetDatabase.ImportAsset(Original, ImportAssetOptions.ForceSynchronousImport);
                }
                if (folderCreated) AssetDatabase.DeleteAsset(IslandAssets);
                foreach (var resource in resources)
                    if (resource && !EditorUtility.IsPersistent(resource)) Object.DestroyImmediate(resource);
                throw;
            }
        }

        static void ValidateIslandMount(Scene scene, bool persistent)
        {
            var root = scene.GetRootGameObjects().Single(item => item.name == IslandMount);
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            var materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
            Require(root.activeInHierarchy && root.transform.childCount == 8 &&
                root.GetComponentsInChildren<Component>(true).Length == 25 &&
                filters.Length == 8 && renderers.Length == 8 && renderers.All(r => r.enabled && r.sharedMaterials.Length == 4) &&
                filters.All(f => f.sharedMesh && f.sharedMesh.subMeshCount == 4) &&
                filters.Select(f => f.sharedMesh).Distinct().Count() == 8 &&
                filters.Sum(f => f.sharedMesh.triangles.Length / 3) == 4032 &&
                materials.Length == 4 && materials.All(m => m && m.shader &&
                    m.shader.name == "Universal Render Pipeline/Lit" && m.GetTexture("_BaseMap")) &&
                root.GetComponentsInChildren<Collider>(true).Length == 0 &&
                root.GetComponentsInChildren<Light>(true).Length == 0, "Masa distinta de la versión aprobada.");
            var limits = renderers.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Require((limits.size - new Vector3(481.06f, 178.49f, 495.30f)).magnitude < .02f &&
                Mathf.Abs(limits.min.y + 184.56f) < .01f && Mathf.Abs(limits.max.y + 6.07f) < .01f,
                "Las dimensiones no coinciden con la versión refinada.");
            if (persistent)
                Require(filters.Select(f => (Object)f.sharedMesh).Concat(materials).All(asset =>
                    AssetDatabase.GetAssetPath(asset).StartsWith(IslandAssets + "/", StringComparison.Ordinal)) &&
                    materials.All(m => EditorUtility.IsPersistent(m.GetTexture("_BaseMap")) &&
                        (!m.IsKeywordEnabled("_NORMALMAP") || EditorUtility.IsPersistent(m.GetTexture("_BumpMap")))),
                    "Faltan referencias persistentes de mallas, materiales o texturas.");
        }

        static void ValidateIslandSceneDelta(string before, string after, int addedRecords = 34)
        {
            const string rootsId = "9223372036854775807";
            Dictionary<string, string> Read(string path) => Regex.Matches(File.ReadAllText(path).Replace("\r\n", "\n"),
                    @"^--- !u!\d+ &(-?\d+)[^\n]*\n[\s\S]*?(?=^--- !u!|\z)", RegexOptions.Multiline)
                .Cast<Match>().ToDictionary(m => m.Groups[1].Value, m => m.Value);
            var baseline = Read(before);
            var result = Read(after);
            Require(baseline.Count > 10000 && result.Count == baseline.Count + addedRecords &&
                result[rootsId].StartsWith(baseline[rootsId].TrimEnd('\n') + "\n  - {fileID: ", StringComparison.Ordinal) &&
                result[rootsId].Split('\n').Length == baseline[rootsId].Split('\n').Length + 1,
                "La candidata no contiene exactamente la raíz nueva y los registros previstos.");
            var changed = baseline.Keys.Where(id => id != rootsId &&
                (!result.TryGetValue(id, out var record) || record != baseline[id])).Take(8).ToArray();
            Require(changed.Length == 0, "Se alteraron registros originales: " + string.Join(", ", changed));
            Debug.Log($"ISLA_DELTA_OK {baseline.Count - 1} registros originales intactos.");
        }
    }
}
