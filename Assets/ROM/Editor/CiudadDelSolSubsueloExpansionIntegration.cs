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
        const string ExpansionRoot = "Subsuelo_Rediseno_16_Sectores";
        const string ExpansionAssetFolder = "Assets/ROM/CiudadDelSolRediseno16";
        static int nextIntegratedSector;

        [MenuItem(ExpansionMenu + "9. Siguiente sector integrado a 1,90 m")]
        static void FrameIntegratedSector()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != Original || StageUtility.GetCurrentStage() != StageUtility.GetMainStage() ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                !scene.GetRootGameObjects().Any(root => root.name == ExpansionRoot))
            {
                EditorUtility.DisplayDialog("Recorrido de los 16 sectores", "Abrí la escena definitiva integrada fuera de Play.", "Aceptar");
                return;
            }
            var designs = ExpansionDesigns();
            var design = designs[nextIntegratedSector++ % designs.Length];
            var view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            view.LookAt(new Vector3((design.West + design.East) * .5f, Floor + 1.9f, design.South + 10),
                Quaternion.LookRotation(Vector3.forward), 4, false, true);
            view.ShowNotification(new GUIContent(design.Name));
        }

        public static void IntegrateExpansionInBatch()
        {
            var source = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            CheckExpansionSource();
            Require(!Directory.Exists(ExpansionAssetFolder) &&
                source.GetRootGameObjects().All(root => root.name != ExpansionRoot), "La integración ya existe.");
            string reviewedBackup = Path.Combine(ReviewFolder, "Dungeon_Isla_del_Sol.preintegracion.unity");
            Require(File.Exists(reviewedBackup) && SceneFileHash(reviewedBackup) == ExpansionSourceHash,
                "Falta el respaldo revisado o su SHA-256 difiere.");
            string backup = Path.Combine(ReviewFolder, $"Dungeon_Isla_del_Sol.antes-montaje16.{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.unity");
            File.Copy(Original, backup);
            Require(SceneFileHash(backup) == ExpansionSourceHash, "No se pudo verificar el respaldo nuevo.");
            Debug.Log("EXPANSION_RESPALDO_OK " + backup);

            var preview = EditorSceneManager.NewPreviewScene();
            var resources = new List<Object>();
            var plans = new List<MountPlan>();
            bool folderCreated = false, touched = false, saveAttempted = false;
            try
            {
                BuildExpansionJoint(source, preview, resources, plans);
                var reviewed = File.ReadAllLines(Path.Combine(ReviewFolder, "Montaje-reversible.tsv"));
                var expected = plans.SelectMany(plan => plan.Targets.Select(id =>
                    $"{plan.Design.Name}\t{plan.Design.Root}\t{id}\tactivo\tinactivo")).ToArray();
                Require(reviewed.Length == 383 && reviewed[0] == "SHA256 de origen\t" + ExpansionSourceHash &&
                    expected.Length == 381 && expected.Distinct().Count() == 381 &&
                    new HashSet<string>(reviewed.Skip(2), StringComparer.Ordinal).SetEquals(expected),
                    "El montaje no coincide exactamente con el manifiesto aprobado.");
                CheckExpansionSource();

                Require(!string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/ROM", "CiudadDelSolRediseno16")),
                    "No se pudo crear la carpeta de assets.");
                folderCreated = true;
                var stone = AssetDatabase.LoadAssetAtPath<Material>(AssetFolder + "/PiedraOpaca.mat");
                Require(stone && !stone.IsKeywordEnabled("_ALPHATEST_ON") && stone.GetFloat("_AlphaClip") == 0,
                    "El material opaco integrado no es válido.");
                touched = true;
                var root = new GameObject(ExpansionRoot);
                SceneManager.MoveGameObjectToScene(root, source);
                for (int i = 0; i < plans.Count; i++)
                {
                    var plan = plans[i];
                    foreach (var id in plan.Targets)
                    {
                        var piece = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                        Require(piece && piece.scene == source && piece.activeSelf &&
                            piece.transform.root.name == plan.Design.Root, "La pieza original no coincide: " + id);
                        piece.SetActive(false);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(piece);
                    }

                    var hall = plan.Hall;
                    hall.name = plan.Design.Name;
                    hall.transform.SetParent(null, true);
                    SceneManager.MoveGameObjectToScene(hall, source);
                    hall.transform.SetParent(root.transform, true);
                    int part = 0;
                    foreach (var filter in hall.GetComponentsInChildren<MeshFilter>())
                    {
                        var mesh = Object.Instantiate(filter.sharedMesh);
                        mesh.hideFlags = HideFlags.None;
                        AssetDatabase.CreateAsset(mesh, ExpansionAssetFolder + $"/Sector_{i + 1:00}_{++part}.asset");
                        filter.sharedMesh = mesh;
                        filter.GetComponent<MeshCollider>().sharedMesh = mesh;
                        filter.GetComponent<MeshRenderer>().sharedMaterial = stone;
                    }
                }

                EditorSceneManager.ClosePreviewScene(preview);
                AssetDatabase.SaveAssets();
                ValidateExpansionMount(source, plans);
                Require(Hash() == ExpansionSourceHash, "La escena en disco cambió antes del guardado.");
                EditorSceneManager.MarkSceneDirty(source);
                saveAttempted = true;
                Require(EditorSceneManager.SaveScene(source), "Unity no pudo guardar la escena.");
                var reopened = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
                ValidateExpansionMount(reopened, plans);
                Debug.Log($"EXPANSION_PERSISTENCIA_OK: 16 sectores; 9504 m2; 381 desactivaciones; 32 mallas; respaldo={backup}; SHA256={Hash()}");
            }
            catch
            {
                if (touched)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    if (saveAttempted && Hash() != ExpansionSourceHash) File.Copy(backup, Original, true);
                    Require(Hash() == ExpansionSourceHash, "No se pudo restaurar el SHA-256 de la escena.");
                }
                if (folderCreated) AssetDatabase.DeleteAsset(ExpansionAssetFolder);
                throw;
            }
            finally
            {
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
        }

        static void ValidateExpansionMount(Scene scene, List<MountPlan> plans)
        {
            var roots = scene.GetRootGameObjects();
            var root = roots.Single(item => item.name == ExpansionRoot);
            Require(root.transform.childCount == 16 &&
                roots.Single(item => item.name == MountedRoot).transform.childCount == 4 &&
                roots.Any(item => item.name == "Nucleo_Monumental_Conjunto"), "Faltan sectores o raíces protegidas.");
            var meshes = new HashSet<Mesh>();
            int steps = 0, joints = 0;
            foreach (var plan in plans)
            {
                Require(!Sectors.Any(hall => plan.Design.West < hall.East && plan.Design.East > hall.West &&
                    plan.Design.South < hall.North && plan.Design.North > hall.South), "Huella superpuesta.");
                foreach (var id in plan.Targets)
                {
                    var piece = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                    Require(piece && piece.scene == scene && !piece.activeSelf &&
                        piece.transform.root.name == plan.Design.Root, "No persistió el recorte: " + id);
                }
                var hall = root.transform.Find(plan.Design.Name).gameObject;
                var filters = hall.GetComponentsInChildren<MeshFilter>();
                Require(filters.Length == 2 && filters.All(filter =>
                {
                    var mesh = filter.sharedMesh;
                    var collider = filter.GetComponent<MeshCollider>();
                    return mesh && meshes.Add(mesh) && collider && collider.enabled && collider.sharedMesh == mesh &&
                        AssetDatabase.GetAssetPath(mesh).StartsWith(ExpansionAssetFolder + "/", StringComparison.Ordinal) &&
                        AssetDatabase.GetAssetPath(filter.GetComponent<MeshRenderer>().sharedMaterial) == AssetFolder + "/PiedraOpaca.mat";
                }), "Referencias incorrectas: " + plan.Design.Name);
                var (_, _, samples, seamSamples) = ValidateMounted(hall, plan.Design, plan.EndCapitals);
                steps += samples;
                joints += seamSamples;
            }
            Require(meshes.Count == 32, "Faltan mallas independientes.");
            Debug.Log($"EXPANSION_MONTADA_OK: 16 sectores; {steps} muestras de recorrido; {joints} comprobaciones de juntas.");
        }

        static string SceneFileHash(string path)
        {
            using var algorithm = System.Security.Cryptography.SHA256.Create();
            using var file = File.OpenRead(path);
            return BitConverter.ToString(algorithm.ComputeHash(file)).Replace("-", "");
        }
    }
}
