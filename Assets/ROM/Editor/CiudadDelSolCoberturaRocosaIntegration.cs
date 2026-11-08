using System;
using System.Collections.Generic;
using System.Globalization;
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
        const string RockCoverMount = "CiudadDelSol_CoberturaRocosa";
        const string RockCoverAssets = "Assets/ROM/CiudadDelSolCoberturaRocosa";
        const string RockCoverApprovedScene = "2128133CCBD5A94504306F1685E8A6138193E1F5C1012DB8AC520D017013B2B4";

        public static void IntegrateRockCoverInBatch()
        {
            Require(Application.isBatchMode, "La integración requiere Unity en batch.");
            var scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            ValidateFinalLook(scene);
            var existing = scene.GetRootGameObjects().Where(r => r.name == RockCoverMount).ToArray();
            if (existing.Length == 1)
            {
                var signature = ValidateRockCoverMount(existing[0], true);
                var manifest = Path.Combine(RockCoverOutput, "Integracion.txt");
                Require(File.Exists(manifest) && File.ReadAllLines(manifest).Contains("Cobertura SHA-256: " + signature),
                    "Ya hay una cobertura, pero no coincide con el registro de integración.");
                Debug.Log("COBERTURA_YA_INTEGRADA " + signature);
                return;
            }
            Require(existing.Length == 0 && !scene.isDirty && Hash() == RockCoverApprovedScene &&
                !Directory.Exists(RockCoverAssets), "La escena o los assets no coinciden con el origen aprobado.");

            Directory.CreateDirectory(RockCoverOutput);
            string backup = Path.Combine(RockCoverOutput, $"Dungeon_Isla_del_Sol.antes-cobertura.{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.unity");
            File.Copy(Original, backup);
            Require(SceneFileHash(backup) == RockCoverApprovedScene, "Falló el SHA-256 del respaldo.");
            File.WriteAllText(backup + ".sha256", RockCoverApprovedScene + Environment.NewLine);
            var resources = new List<Object>();
            string candidatePath = RockCoverAssets + "/Candidata.unity";
            bool folderCreated = false;
            bool promoted = false;
            try
            {
                var cover = BuildRockCover(scene, scene.GetRootGameObjects(), resources, out var report);
                cover.name = RockCoverMount;
                string approved = ValidateRockCoverMount(cover, false);
                Require(!string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/ROM", "CiudadDelSolCoberturaRocosa")),
                    "No se pudo preparar la carpeta de materiales.");
                folderCreated = true;
                int index = 0;
                foreach (var material in resources.OfType<Material>())
                {
                    material.name = $"Cobertura mineral {++index:00}";
                    AssetDatabase.CreateAsset(material, RockCoverAssets + $"/Mineral_{index:00}.mat");
                }
                AssetDatabase.SaveAssets();
                Require(ValidateRockCoverMount(cover, true) == approved &&
                    EditorSceneManager.SaveScene(scene, candidatePath, true), "Falló la escena candidata.");
                ValidateIslandSceneDelta(backup, candidatePath, 17 * 2 + 952 * 4);
                var candidate = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Single);
                ValidateFinalLook(candidate);
                Require(ValidateRockCoverMount(candidate.GetRootGameObjects().Single(r => r.name == RockCoverMount), true) == approved &&
                    Hash() == RockCoverApprovedScene && SceneFileHash(backup) == RockCoverApprovedScene,
                    "La candidata, el respaldo o la escena cambiaron antes de promover.");
                string expected = SceneFileHash(candidatePath);
                string swap = Path.Combine(RockCoverOutput, "Cobertura-montaje.tmp");
                File.Copy(candidatePath, swap);
                File.Replace(swap, Original, null);
                promoted = true;
                AssetDatabase.ImportAsset(Original, ImportAssetOptions.ForceSynchronousImport);
                var reopened = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
                ValidateFinalLook(reopened);
                Require(!reopened.isDirty && Hash() == expected &&
                    ValidateRockCoverMount(reopened.GetRootGameObjects().Single(r => r.name == RockCoverMount), true) == approved,
                    "Falló la persistencia de la cobertura.");
                Require(AssetDatabase.DeleteAsset(candidatePath), "No se pudo retirar la candidata.");
                File.WriteAllLines(Path.Combine(RockCoverOutput, "Integracion.txt"), new[] {
                    "Escena SHA-256: " + expected, "Cobertura SHA-256: " + approved, "Respaldo: " + backup, report });
                Debug.Log("COBERTURA_INTEGRADA_OK " + expected + " | respaldo=" + backup);
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (promoted)
                {
                    File.Copy(backup, Original, true);
                    Require(Hash() == RockCoverApprovedScene, "Falló la restauración del respaldo.");
                    AssetDatabase.ImportAsset(Original, ImportAssetOptions.ForceSynchronousImport);
                }
                if (folderCreated) AssetDatabase.DeleteAsset(RockCoverAssets);
                foreach (var resource in resources)
                    if (resource && !EditorUtility.IsPersistent(resource)) Object.DestroyImmediate(resource);
                throw;
            }
        }

        static string ValidateRockCoverMount(GameObject root, bool persistent)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            var materials = renderers.Select(r => r.sharedMaterial).Distinct().ToArray();
            Require(root.activeInHierarchy && filters.Length == 952 && renderers.Length == 952 &&
                root.GetComponentsInChildren<Transform>(true).Length == 969 &&
                root.GetComponentsInChildren<Collider>(true).Length == 0 &&
                filters.Select(f => f.name).Distinct().Count() == 952 &&
                filters.Select(f => f.transform.position).Distinct().Count() == 952 &&
                filters.Select(f => f.sharedMesh).Distinct().Count() == 6 &&
                filters.All(f => f.sharedMesh && f.gameObject.activeInHierarchy && EditorUtility.IsPersistent(f.sharedMesh)) &&
                filters.Sum(f => (int)f.sharedMesh.GetIndexCount(0) / 3) == 1028026 &&
                materials.Length == 4 && materials.All(m => m && m.enableInstancing && m.shader.name == "Universal Render Pipeline/Lit" &&
                    (!persistent || AssetDatabase.GetAssetPath(m).StartsWith(RockCoverAssets + "/", StringComparison.Ordinal))),
                "La cobertura difiere de las 952 rocas aprobadas.");
            int[] expected = { 94, 240, 295, 323 };
            for (int tier = 0; tier < expected.Length; tier++)
                Require(filters.Count(f => f.name.Contains($" · {tier + 1}.")) == expected[tier], "Cambió la cantidad por escala.");
            // Six decimal places are stable across Unity's scene serialization.
            string Vec(Vector3 v) => FormattableString.Invariant($"{v.x:F4},{v.y:F4},{v.z:F4}");
            string Key(MeshFilter f)
            {
                var t = f.transform;
                var q = t.localRotation;
                var m = f.GetComponent<MeshRenderer>().sharedMaterial;
                return FormattableString.Invariant($"{f.name}|{t.parent.name}|{Vec(t.localPosition)}|{Vec(t.localScale)}|{q.x:F4},{q.y:F4},{q.z:F4},{q.w:F4}|{AssetDatabase.GetAssetPath(f.sharedMesh)}|{m.color}");
            }
            var data = System.Text.Encoding.UTF8.GetBytes(string.Join("\n", filters.OrderBy(f => f.name, StringComparer.Ordinal).Select(Key)));
            using var sha = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
        }
    }
}

