using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string FinalLookAssets = "Assets/ROM/CiudadDelSolUnionYAtmosfera";
        const string FinalLookCandidate = FinalLookAssets + "/Candidata.unity";
        const string FinalLookOutput = "Backups/CiudadDelSol/UnionYAtmosferaIntegradas";
        const string FinalLookRoot = "Ciudad del Sol · atmósfera de la Grieta";
        const string FinalSeamRoot = "Ciudad del Sol · unión exterior de roca";
        const string ApprovedSeamHash = "8CA27EFEE452E1D58DF5A154E12B9019B21488F039DCBD51F62FA97F06B77B29";

        public static void IntegrateWallSeamAndAtmosphere()
        {
            Require(Application.isBatchMode && Hash() == AtmosphereSourceHash, "La escena no coincide con la versión aprobada.");
            string measurements = Path.Combine(SeamOutput, "Mediciones.txt");
            Require(File.Exists(measurements) && SceneFileHash(measurements) == ApprovedSeamHash,
                "No coincide el informe de la unión aprobada.");
            var scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            Require(!scene.isDirty && !Directory.Exists(FinalLookAssets) && scene.GetRootGameObjects().All(root =>
                root.name != FinalLookRoot && root.name != FinalSeamRoot), "La escena tiene cambios o ya contiene la integración.");
            ValidateExteriorMount(scene);
            Directory.CreateDirectory(FinalLookOutput);
            string backup = Path.Combine(FinalLookOutput,
                $"Dungeon_Isla_del_Sol.antes-union-atmosfera.{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.unity");
            File.Copy(Original, backup);
            Require(SceneFileHash(backup) == AtmosphereSourceHash, "Falló la verificación del respaldo.");
            File.WriteAllText(backup + ".sha256", AtmosphereSourceHash + Environment.NewLine);
            Debug.Log("UNION_ATMOSFERA_RESPALDO_OK " + backup + " " + AtmosphereSourceHash);

            var resources = new List<Object>();
            bool folderCreated = false, promoted = false;
            try
            {
                // Check the terrain against the original architecture and decor, before adding the visual volumes.
                var seam = BuildWallSeam(scene, resources, out var seamReport);
                Require(seamReport.SequenceEqual(File.ReadAllLines(measurements)), "La unión generada difiere de la propuesta aprobada.");
                seam.name = FinalSeamRoot;
                var atmosphere = BuildAtmosphere(scene, resources);
                atmosphere.Root.name = FinalLookRoot;
                atmosphere.Root.AddComponent<ROM.Rendering.CiudadDelSolAtmosphereCamera>();
                Require(atmosphere.Materials.Count == 208 && atmosphere.OriginalSun.Length == 1,
                    "La escena contiene un conjunto de luces o materiales distinto al aprobado.");
                var allowed = new Dictionary<string, string>();
                foreach (var change in atmosphere.Materials)
                    allowed.Add(RecordId(change.renderer), @"(?m)^  m_Materials:\n(?:  - [^\n]*\n)+");
                foreach (var sun in atmosphere.OriginalSun)
                    allowed.Add(RecordId(sun), @"(?m)^  m_Enabled: [01]\n");

                Require(!string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/ROM", "CiudadDelSolUnionYAtmosfera")),
                    "No se pudo crear la carpeta de assets persistentes.");
                folderCreated = true;
                int meshes = 0, materials = 0;
                foreach (var resource in resources)
                {
                    if (resource is Mesh mesh)
                        AssetDatabase.CreateAsset(mesh, FinalLookAssets + $"/Estrato_{++meshes:00}.asset");
                    else if (resource is Material material)
                        AssetDatabase.CreateAsset(material, FinalLookAssets + $"/Ambiente_{++materials:00}.mat");
                    else if (resource is VolumeProfile profile)
                    {
                        AssetDatabase.CreateAsset(profile, FinalLookAssets + "/AmbienteDimensional.asset");
                        foreach (var effect in profile.components) AssetDatabase.AddObjectToAsset(effect, profile);
                        EditorUtility.SetDirty(profile);
                    }
                }
                AssetDatabase.SaveAssets();
                Require(meshes == 8 && materials == 6, "Cambió el inventario de recursos visuales.");
                ValidateFinalLook(scene);
                Require(Hash() == AtmosphereSourceHash && EditorSceneManager.SaveScene(scene, FinalLookCandidate, true),
                    "No se pudo guardar la candidata o cambió la escena original.");
                ValidateFinalLookDelta(backup, FinalLookCandidate, allowed);

                var candidate = EditorSceneManager.OpenScene(FinalLookCandidate, OpenSceneMode.Single);
                var validated = ValidateFinalLook(candidate);
                Require(!candidate.isDirty && Hash() == AtmosphereSourceHash && SceneFileHash(backup) == AtmosphereSourceHash,
                    "La escena original o su respaldo cambió antes del guardado.");
                // Capture the persisted candidate with the same atmosphere on both sides of each comparison.
                CaptureAtmosphereViews(candidate, atmosphere, validated.seam,
                    AtmosphereViews().Where(v => v.name == "01-Sur" || v.name.Contains("Warrior")).ToArray(), FinalLookOutput);
                foreach (var view in AtmosphereViews().Where(v => v.name.Contains("Warrior")))
                {
                    string filename = view.name + "-despues.png";
                    float reviewed = FloorLuminance(Path.Combine(AtmosphereOutput, filename));
                    float persisted = FloorLuminance(Path.Combine(FinalLookOutput, filename));
                    Require(reviewed > .04f && persisted >= reviewed * .88f,
                        $"La visibilidad cayó en {view.name}: {reviewed:F3} → {persisted:F3}.");
                    Debug.Log($"UNION_ATMOSFERA_VISIBILIDAD {view.name}: {reviewed:F3} → {persisted:F3}");
                }
                Require(Hash() == AtmosphereSourceHash, "La escena original cambió durante la revisión gráfica.");
                string candidateHash = SceneFileHash(FinalLookCandidate);
                string swap = backup + ".montaje.tmp";
                File.Copy(FinalLookCandidate, swap);
                File.Replace(swap, Original, null);
                promoted = true;
                AssetDatabase.ImportAsset(Original, ImportAssetOptions.ForceSynchronousImport);
                var reopened = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
                ValidateFinalLook(reopened);
                Require(!reopened.isDirty && Hash() == candidateHash, "Falló la verificación tras reabrir la escena.");
                Require(AssetDatabase.DeleteAsset(FinalLookCandidate), "No se pudo retirar la candidata.");
                Debug.Log($"UNION_ATMOSFERA_INTEGRACION_OK SHA256={candidateHash}; respaldo={backup}");
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (promoted)
                {
                    File.Copy(backup, Original, true);
                    Require(Hash() == AtmosphereSourceHash, "Falló la restauración del respaldo.");
                    AssetDatabase.ImportAsset(Original, ImportAssetOptions.ForceSynchronousImport);
                }
                if (folderCreated) AssetDatabase.DeleteAsset(FinalLookAssets);
                foreach (var resource in resources)
                    if (resource && !EditorUtility.IsPersistent(resource)) Object.DestroyImmediate(resource);
                throw;
            }
        }

        static string RecordId(Object value) => GlobalObjectId.GetGlobalObjectIdSlow(value).targetObjectId.ToString();

        static float FloorLuminance(string path)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                Require(texture.LoadImage(File.ReadAllBytes(path)), "No se pudo leer la captura " + path);
                var pixels = texture.GetPixels32();
                float sum = 0;
                int count = 0;
                for (int y = texture.height / 12; y < texture.height / 3; y += 8)
                for (int x = texture.width / 4; x < texture.width * 3 / 4; x += 8)
                {
                    var color = pixels[y * texture.width + x];
                    sum += (color.r * .2126f + color.g * .7152f + color.b * .0722f) / 255;
                    count++;
                }
                return sum / count;
            }
            finally { Object.DestroyImmediate(texture); }
        }

        static (GameObject seam, GameObject look) ValidateFinalLook(Scene scene)
        {
            ValidateIslandMount(scene, true);
            var roots = scene.GetRootGameObjects();
            var seam = roots.Single(r => r.name == FinalSeamRoot);
            var look = roots.Single(r => r.name == FinalLookRoot);
            var decor = roots.Single(r => r.name == ExteriorMount);
            var terrain = seam.GetComponentsInChildren<MeshFilter>();
            Require(terrain.Length == 8 && terrain.All(f => f.sharedMesh && EditorUtility.IsPersistent(f.sharedMesh)) &&
                terrain.Sum(f => (int)f.sharedMesh.GetIndexCount(0) / 3) == 1530, "No persistieron los ocho estratos.");
            Require(decor.transform.childCount == 70 &&
                decor.transform.Cast<Transform>().Count(t => t.name.StartsWith("Cascada ", StringComparison.Ordinal)) == 10 &&
                decor.transform.Cast<Transform>().Count(t => t.name.StartsWith("Fragmento ", StringComparison.Ordinal)) == 60 &&
                decor.GetComponentsInChildren<Renderer>().Length == 394, "Se alteró la decoración aprobada.");
            var lamps = look.GetComponentsInChildren<Light>();
            Require(lamps.Length == 8 && lamps.Count(l => l.type == LightType.Directional) == 3 &&
                lamps.Count(l => l.type == LightType.Point) == 5 && lamps.All(l => l.enabled && l.shadows == LightShadows.None) &&
                roots.Where(r => r != look).SelectMany(r => r.GetComponentsInChildren<Light>())
                    .All(l => !l.enabled || l.type != LightType.Directional), "Hay luces direccionales duplicadas o inesperadas.");
            var volume = look.GetComponentInChildren<Volume>();
            Require(volume && volume.isGlobal && volume.gameObject.layer == PreviewVolumeLayer && volume.sharedProfile &&
                EditorUtility.IsPersistent(volume.sharedProfile) &&
                volume.sharedProfile.TryGet<Bloom>(out var bloom) && Mathf.Approximately(bloom.intensity.value, .31f) &&
                volume.sharedProfile.TryGet<ColorAdjustments>(out var exposure) &&
                Mathf.Approximately(exposure.postExposure.value, .42f), "Falta el perfil URP persistente.");
            Require(look.GetComponent<ROM.Rendering.CiudadDelSolAtmosphereCamera>() &&
                look.GetComponentsInChildren<MeshRenderer>().Length == 2 &&
                look.transform.Find("Banco dimensional bajo las rutas").GetComponent<Renderer>().bounds.max.y < -45,
                "No persistió la cámara de ambiente, el cielo o la cota de niebla.");
            foreach (var root in new[] { seam, look, decor })
            {
                Require(root.GetComponentsInChildren<Collider>(true).Length == 0 &&
                    root.GetComponentsInChildren<Renderer>().All(r => r.sharedMaterials.All(m =>
                        m && m.shader && EditorUtility.IsPersistent(m) && EditorUtility.IsPersistent(m.shader))),
                    "Se encontró un collider o una referencia visual no persistente.");
            }
            return (seam, look);
        }

        static void ValidateFinalLookDelta(string before, string after, Dictionary<string, string> allowed)
        {
            const string rootsId = "9223372036854775807";
            Dictionary<string, string> Read(string path) => Regex.Matches(File.ReadAllText(path).Replace("\r\n", "\n"),
                    @"^--- !u!\d+ &(-?\d+)[^\n]*\n[\s\S]*?(?=^--- !u!|\z)", RegexOptions.Multiline)
                .Cast<Match>().ToDictionary(m => m.Groups[1].Value, m => m.Value);
            var baseline = Read(before);
            var result = Read(after);
            Require(result[rootsId].StartsWith(baseline[rootsId].TrimEnd('\n') + "\n  - {fileID: ", StringComparison.Ordinal) &&
                result[rootsId].Split('\n').Length == baseline[rootsId].Split('\n').Length + 2 &&
                allowed.Keys.All(id => baseline.ContainsKey(id) && result.ContainsKey(id)), "Cambió el inventario de raíces o de ajustes.");
            var unexpected = baseline.Keys.Where(id => id != rootsId && (!result.TryGetValue(id, out var record) ||
                (allowed.TryGetValue(id, out var field) ?
                    Regex.Replace(record, field, "") != Regex.Replace(baseline[id], field, "") : record != baseline[id])))
                .Take(10).ToArray();
            Require(unexpected.Length == 0, "Se alteraron registros protegidos: " + string.Join(", ", unexpected));
            Debug.Log($"UNION_ATMOSFERA_DELTA_OK {baseline.Count - allowed.Count - 1} registros originales protegidos intactos.");
        }
    }
}
