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
        const string ExteriorMount = "Cascadas y fragmentos · exterior";
        const string ExteriorAssets = "Assets/ROM/CiudadDelSolDecoracionExterior";
        const string ExteriorCandidate = ExteriorAssets + "/Candidata.unity";
        const string ExteriorApprovedReport = "70B543B2F2BC8A208AAF9B4EBB8514F168E40273A8F9F2B552CC3EB6DE5DA136";

        public static void IntegrateExteriorDecorInBatch()
        {
            Require(Application.isBatchMode && Hash() == DecorHash, "La escena no coincide con el origen aprobado.");
            var reportPath = Path.Combine(DecorOutput, "Mediciones.txt");
            Require(File.Exists(reportPath) && SceneFileHash(reportPath) == ExteriorApprovedReport,
                "No se encontró el inventario aprobado exacto.");
            var approved = File.ReadAllLines(reportPath);
            var scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            Require(!scene.isDirty && !Directory.Exists(ExteriorAssets) && roots.Count(r => r.name == IslandMount) == 1 &&
                roots.All(r => !r.name.StartsWith("Cascadas y fragmentos", StringComparison.Ordinal)),
                "La escena no está limpia, falta la masa integrada o ya existe una decoración exterior.");
            ValidateIslandMount(scene, true);

            string backup = Path.Combine(DecorOutput,
                $"Dungeon_Isla_del_Sol.antes-decoracion.{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.unity");
            File.Copy(Original, backup);
            Require(SceneFileHash(backup) == DecorHash, "El respaldo no coincide con el SHA-256 original.");
            File.WriteAllText(backup + ".sha256", DecorHash + Environment.NewLine);
            Debug.Log("EXTERIOR_RESPALDO_OK " + backup + " " + DecorHash);

            var resources = new List<Object>();
            bool folderCreated = false, promoted = false;
            try
            {
                var report = new List<string>();
                var decor = BuildExteriorDecor(scene, roots.Where(ExteriorContext).ToArray(), resources, report);
                Require(report.SequenceEqual(approved), "La generación difiere del inventario aprobado: " +
                    string.Join(" | ", report.Where((line, i) => i >= approved.Length || line != approved[i]).Take(3)));
                decor.name = ExteriorMount;

                Require(!string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/ROM", "CiudadDelSolDecoracionExterior")),
                    "No se pudo crear la carpeta de recursos persistentes.");
                folderCreated = true;
                int meshCount = 0;
                foreach (var resource in resources.Where(resource => resource))
                {
                    if (resource is Mesh mesh)
                    {
                        AssetDatabase.CreateAsset(mesh, ExteriorAssets + $"/Malla_{++meshCount:000}.asset");
                    }
                    else if (resource is Material material)
                    {
                        material.name = "Agua aérea · Ciudad del Sol";
                        AssetDatabase.CreateAsset(material, ExteriorAssets + "/AguaAerea.mat");
                    }
                }
                AssetDatabase.SaveAssets();

                var originalDecor = ValidateExteriorMount(scene);
                var expected = originalDecor.GetComponentsInChildren<Transform>(true)
                    .Select(t => (t.name, position: t.localPosition, rotation: t.localRotation, scale: t.localScale)).ToArray();
                int records = expected.Length + originalDecor.GetComponentsInChildren<Component>(true).Length;
                Require(Hash() == DecorHash && EditorSceneManager.SaveScene(scene, ExteriorCandidate, true),
                    "No se pudo guardar la candidata o cambió la escena original.");
                ValidateIslandSceneDelta(backup, ExteriorCandidate, records);

                var candidate = EditorSceneManager.OpenScene(ExteriorCandidate, OpenSceneMode.Single);
                VerifyTransforms(ValidateExteriorMount(candidate));
                Require(!candidate.isDirty && Hash() == DecorHash && SceneFileHash(backup) == DecorHash,
                    "Cambió la escena original o su respaldo antes de guardar.");
                string candidateHash = SceneFileHash(ExteriorCandidate);
                string swap = backup + ".montaje.tmp";
                File.Copy(ExteriorCandidate, swap);
                File.Replace(swap, Original, null);
                promoted = true;
                AssetDatabase.ImportAsset(Original, ImportAssetOptions.ForceSynchronousImport);
                var reopened = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
                VerifyTransforms(ValidateExteriorMount(reopened));
                Require(!reopened.isDirty && Hash() == candidateHash, "Falló la comprobación después de reabrir.");
                Require(AssetDatabase.DeleteAsset(ExteriorCandidate), "No se pudo retirar la candidata.");
                Debug.Log($"EXTERIOR_INTEGRACION_OK {meshCount} mallas nuevas persistentes y 1 material nuevo; SHA256={candidateHash}; respaldo={backup}");

                void VerifyTransforms(GameObject mounted)
                {
                    var transforms = mounted.GetComponentsInChildren<Transform>(true);
                    Require(transforms.Length == expected.Length && transforms.Select((t, i) =>
                        t.name == expected[i].name && (t.localPosition - expected[i].position).sqrMagnitude < 1e-8f &&
                        Quaternion.Angle(t.localRotation, expected[i].rotation) < .05f &&
                        (t.localScale - expected[i].scale).sqrMagnitude < 1e-8f).All(ok => ok),
                        "Cambió la jerarquía o las transformaciones al reabrir.");
                }
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (promoted)
                {
                    File.Copy(backup, Original, true);
                    Require(Hash() == DecorHash, "Falló la restauración del respaldo.");
                    AssetDatabase.ImportAsset(Original, ImportAssetOptions.ForceSynchronousImport);
                }
                if (folderCreated) AssetDatabase.DeleteAsset(ExteriorAssets);
                foreach (var resource in resources)
                    if (resource && !EditorUtility.IsPersistent(resource)) Object.DestroyImmediate(resource);
                throw;
            }
        }

        static GameObject ValidateExteriorMount(Scene scene)
        {
            ValidateIslandMount(scene, true);
            var root = scene.GetRootGameObjects().Single(item => item.name == ExteriorMount);
            var children = root.transform.Cast<Transform>().ToArray();
            var falls = children.Where(t => t.name.StartsWith("Cascada ", StringComparison.Ordinal)).ToArray();
            var renderers = root.GetComponentsInChildren<Renderer>();
            var filters = root.GetComponentsInChildren<MeshFilter>();
            var materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
            Require(root.activeInHierarchy && children.Length == 70 && falls.Length == 10 &&
                children.Count(t => t.name.StartsWith("Fragmento ", StringComparison.Ordinal)) == 60 &&
                renderers.Length == 394 && materials.Length == 19 && materials.All(m => m && m.shader &&
                    EditorUtility.IsPersistent(m) && EditorUtility.IsPersistent(m.shader)) &&
                filters.All(f => f.sharedMesh && EditorUtility.IsPersistent(f.sharedMesh)) &&
                filters.Sum(f => Enumerable.Range(0, f.sharedMesh.subMeshCount)
                    .Sum(s => (int)f.sharedMesh.GetIndexCount(s) / 3)) == 466708 &&
                root.GetComponentsInChildren<Collider>(true).Length == 0 &&
                root.GetComponentsInChildren<Light>(true).Length == 0 &&
                root.GetComponentsInChildren<ParticleSystem>(true).Length == 0 &&
                root.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "Falló el inventario o las referencias persistentes.");
            Require(falls.All(t =>
            {
                var water = t.GetComponent<MeshRenderer>().sharedMaterials;
                return water.Length == 2 && water[0].shader.name == "ROM/Ciudad del Sol/Coastal Cascade Preview" &&
                    Mathf.Approximately(water[0].GetFloat("_FlowSpeed"), 1.18f) &&
                    water[1].shader.name == "ROM/Coastal Cascade" && Mathf.Approximately(water[1].GetFloat("_Mist"), 1f);
            }), "Faltan referencias o propiedades del agua animada.");
            return root;
        }
    }
}
