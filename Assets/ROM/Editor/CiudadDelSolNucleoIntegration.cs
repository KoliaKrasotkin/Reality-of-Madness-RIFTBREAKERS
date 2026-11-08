using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ROM.Horde;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNucleoConjuntoPreviewGenerator
    {
        const string SourceHash = "0E96A4D334A7A6FECCF75AF0124D8DDF5F8641B57620920264EFC20852314506";
        const string TrialHash = "5C0B4E4E8D0888EACD18A677AAC1CEB1425DEAAC49D8E34717A87E5DCC435154";
        const string TrialGuid = "450b132d8394ca54f86ff67fca648d8a";
        const string IntegratedName = "Nucleo_Monumental_Conjunto";
        const float VoidHeight = -48f;

        static string FileHash(string path)
        {
            using var algorithm = SHA256.Create();
            using var file = File.OpenRead(path);
            return BitConverter.ToString(algorithm.ComputeHash(file)).Replace("-", "");
        }

        static void RequireIntegration(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [MenuItem("ROM/Dungeon Ciudad del Sol/Integracion/Integrar nucleo profundo aprobado")]
        public static void IntegrateApprovedCore()
        {
            RequireIntegration(!EditorApplication.isPlayingOrWillChangePlaymode, "Integrar fuera de Play Mode.");
            RequireIntegration(StageUtility.GetCurrentStage() == StageUtility.GetMainStage(), "Cerrar la previsualizacion primero.");
            string trialPath = AssetDatabase.GUIDToAssetPath(TrialGuid);
            RequireIntegration(FileHash(Target) == SourceHash && File.Exists(trialPath) && FileHash(trialPath) == TrialHash,
                "Los hashes de la escena o del ensayo no coinciden con los aprobados.");
            RequireIntegration(Enumerable.Range(0, SceneManager.sceneCount).All(i => !SceneManager.GetSceneAt(i).isDirty),
                "Hay escenas con cambios sin guardar. Integracion detenida.");
            var source = EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
            RequireIntegration(!source.isDirty && source.GetRootGameObjects().All(g => g.name != IntegratedName),
                "La escena no es una fuente limpia para la integracion.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(trialPath);
            var approvedCore = prefab ? prefab.transform.Find("Nucleo_Monumental_Conjunto_TEMPORAL") : null;
            var approvedContext = prefab ? prefab.transform.Find("Contexto_UNICO_por_identidad") : null;
            RequireIntegration(approvedCore && approvedContext, "Jerarquia aprobada incompleta.");
            string backupDir = "Backups/CiudadDelSol";
            Directory.CreateDirectory(backupDir);
            string backup = Path.Combine(backupDir, "Dungeon_Isla_del_Sol.before-nucleo." + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".unity");
            File.Copy(Target, backup);
            RequireIntegration(FileHash(backup) == SourceHash, "No se pudo verificar el respaldo.");
            Debug.Log("NUCLEO_RESPALDO: " + backup);

            var stage = ScriptableObject.CreateInstance<JointStage>();
            stage.Source = source;
            stage.Original = Guard(source);
            stage.auditScene = EditorSceneManager.NewPreviewScene();
            try
            {
                stage.Compose();
                stage.ApplyApprovedCore(approvedCore, approvedContext);
                RequireIntegration(FileHash(Target) == SourceHash && FileHash(trialPath) == TrialHash,
                    "Cambio de archivo durante la integracion. No guardar.");
                RequireIntegration(EditorSceneManager.SaveScene(source), "Unity no pudo guardar la escena.");
                Debug.Log("NUCLEO_INTEGRACION_GUARDADA: " + Target + " SHA256=" + FileHash(Target));
            }
            catch
            {
                Debug.LogError("NUCLEO_INTEGRACION_DETENIDA: " + backup);
                EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
                throw;
            }
            finally
            {
                foreach (var builder in stage.builders) Object.DestroyImmediate(builder);
                EditorSceneManager.ClosePreviewScene(stage.auditScene);
                Object.DestroyImmediate(stage);
            }
        }

        sealed partial class JointStage
        {
            // The exported prefab has only geometry. Reconstruct identity from the
            // original objects, then compare both hierarchies before editing the scene.
            static void MatchApproved(Transform rebuilt, Transform approved,
                System.Collections.Generic.Dictionary<Transform, Transform> pairs)
            {
                string path = AnimationUtility.CalculateTransformPath(rebuilt, null);
                RequireIntegration(rebuilt.name == approved.name && rebuilt.gameObject.activeSelf == approved.gameObject.activeSelf &&
                    rebuilt.childCount == approved.childCount && rebuilt.gameObject.layer == approved.gameObject.layer &&
                    Vector3.Distance(rebuilt.localPosition, approved.localPosition) < .0002f &&
                    Quaternion.Angle(rebuilt.localRotation, approved.localRotation) < .05f &&
                    Vector3.Distance(rebuilt.localScale, approved.localScale) < .0002f,
                    "Jerarquia, estado o transformacion distintos del ensayo en " + path + " / " + approved.name);
                var a = rebuilt.GetComponent<MeshFilter>();
                var b = approved.GetComponent<MeshFilter>();
                RequireIntegration((bool)a == (bool)b && (!a || a.sharedMesh == b.sharedMesh), "Malla distinta del ensayo en " + path);
                var r = rebuilt.GetComponent<MeshRenderer>();
                var s = approved.GetComponent<MeshRenderer>();
                RequireIntegration((bool)r == (bool)s && (!r || (r.enabled == s.enabled && r.sharedMaterials.SequenceEqual(s.sharedMaterials))),
                    "Renderer o materiales distintos del ensayo en " + path);
                var left = rebuilt.GetComponents<Collider>();
                var right = approved.GetComponents<Collider>();
                RequireIntegration(left.Length == right.Length, "Cantidad de colliders distinta en " + path);
                for (int i = 0; i < left.Length; i++)
                {
                    var c = left[i]; var d = right[i];
                    RequireIntegration(c.GetType() == d.GetType() && c.enabled == d.enabled && c.isTrigger == d.isTrigger &&
                        c.sharedMaterial == d.sharedMaterial, "Collider distinto del ensayo en " + path);
                    if (c is BoxCollider box && d is BoxCollider other)
                        RequireIntegration(box.center == other.center && box.size == other.size, "Caja distinta del ensayo en " + path);
                    else if (c is MeshCollider mesh && d is MeshCollider otherMesh)
                        RequireIntegration(mesh.sharedMesh == otherMesh.sharedMesh && mesh.convex == otherMesh.convex &&
                            mesh.cookingOptions == otherMesh.cookingOptions, "MeshCollider distinto del ensayo en " + path);
                    else
                        throw new InvalidOperationException("Collider sin comparador en " + path + ": " + c.GetType().Name);
                }
                pairs.Add(rebuilt, approved);
                for (int i = 0; i < rebuilt.childCount; i++) MatchApproved(rebuilt.GetChild(i), approved.GetChild(i), pairs);
            }

            internal void ApplyApprovedCore(Transform approvedCore, Transform approvedContext)
            {
                var pairs = new System.Collections.Generic.Dictionary<Transform, Transform>();
                MatchApproved(context, approvedContext, pairs);
                MatchApproved(generated, approvedCore, pairs);
                var moved = originalChanged.Where(g => approvedFloors.Contains(g)).ToArray();
                var opened = originalChanged.Where(g => !g.activeSelf && g.name.StartsWith("Platform_Base_01", StringComparison.Ordinal)).ToArray();
                var capitals = originalChanged.Where(g => !g.activeSelf && g.name == "Column_Ceiling_02" &&
                    identities[g].transform.root.name == "Subsuelo_Central_Sureste_Etapa02").ToArray();
                var authorizedWidening = new System.Collections.Generic.Dictionary<ulong, Vector2>
                {
                    { 599394617, new Vector2(-6, -12) },
                    { 1428331448, new Vector2(-6, 48) },
                    { 1455862406, new Vector2(42, -12) },
                    { 283171896, new Vector2(42, 48) }
                };
                var widened = originalChanged.Where(g => g.activeSelf && g.name == "Column_Ceiling_02" &&
                    identities[g].transform.root.name == "Subsuelo_Central_Sureste_Etapa02").ToArray();
                RequireIntegration(moved.Length == 194 && opened.Length == 144 && capitals.Length == 23 &&
                    widened.Length == 4 && originalChanged.Count == 365 && portal.Count == 12,
                    "Conjunto de cambios estructurales fuera del autorizado.");
                RequireIntegration(capitals.All(g => identities[g].activeSelf && !pairs[g.transform].gameObject.activeSelf &&
                    initial[g].Substring(4) == Snapshot(g).Substring(5)), "Un capitel difiere en algo mas que su estado activo.");
                foreach (var cap in widened)
                {
                    var original = identities[cap].transform;
                    var approved = pairs[cap.transform];
                    var id = GlobalObjectId.GetGlobalObjectIdSlow(original.gameObject);
                    var position = original.position;
                    var scale = Vector3.Scale(original.localScale, new Vector3(1.6f, 1f, 1.6f));
                    RequireIntegration(id.targetObjectId == 159204 && authorizedWidening.Remove(id.targetPrefabId, out var xz) &&
                        Vector2.Distance(new Vector2(position.x, position.z), xz) < .0001f &&
                        original.gameObject.activeSelf && approved.gameObject.activeSelf &&
                        Vector3.Distance(original.localPosition, approved.localPosition) < .00002f &&
                        Quaternion.Angle(original.localRotation, approved.localRotation) < .001f &&
                        approved.localScale.Equals(scale),
                        "Identidad, posicion, rotacion o escala del capitel fuera de lo autorizado: " + id);
                }
                RequireIntegration(moved.All(g => g.transform.IsChildOf(generated)), "Piso reutilizado fuera de la jerarquia aprobada.");
                RequireIntegration(Source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HordeVoidKillPlane>(true)).Count() == 0,
                    "Existe un plano de muerte en la escena que requiere revision.");
                var originalObjects = identities.Values.ToHashSet();
                var protectedRoots = Source.GetRootGameObjects().Where(Root).ToArray();
                var added = Object.Instantiate(approvedCore.gameObject);
                added.name = approvedCore.name;
                SceneManager.MoveGameObjectToScene(added, Source);
                var map = new System.Collections.Generic.Dictionary<Transform, Transform>();
                MatchApproved(added.transform, approvedCore, map);
                var byApproved = map.ToDictionary(p => p.Value, p => p.Key);
                foreach (var floor in moved)
                {
                    var placeholder = byApproved[pairs[floor.transform]];
                    var original = identities[floor].transform;
                    int index = placeholder.GetSiblingIndex();
                    original.SetParent(placeholder.parent, false);
                    original.SetLocalPositionAndRotation(placeholder.localPosition, placeholder.localRotation);
                    original.localScale = placeholder.localScale;
                    original.SetSiblingIndex(index);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(original);
                    Object.DestroyImmediate(placeholder.gameObject);
                }
                foreach (var g in opened.Concat(capitals))
                {
                    var original = identities[g];
                    original.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(original);
                }
                foreach (var g in widened)
                {
                    var original = identities[g].transform;
                    original.localScale = pairs[g.transform].localScale;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(original);
                    Debug.Log("NUCLEO_CAPITEL_ENSANCHADO: " + GlobalObjectId.GetGlobalObjectIdSlow(original.gameObject) +
                        " " + original.position.ToString("F4") + " escala=" + original.localScale.ToString("F6"));
                }
                map.Clear();
                MatchApproved(added.transform, approvedCore, map);
                foreach (var root in protectedRoots)
                    MatchApproved(root.transform, approvedContext.Find(root.name), map);
                RequireIntegration(added.GetComponentsInChildren<Renderer>().Length == 1979 &&
                    added.GetComponentsInChildren<Collider>().Length == 1965, "Conteos finales distintos de los aprobados.");
                added.name = IntegratedName;
                var deathPlane = new GameObject("Ciudad del Sol - Plano de muerte");
                SceneManager.MoveGameObjectToScene(deathPlane, Source);
                deathPlane.transform.position = new Vector3(0, VoidHeight, 0);
                deathPlane.AddComponent<HordeVoidKillPlane>().KillHeight = VoidHeight;
                Physics.SyncTransforms();
                var structural = Source.GetRootGameObjects().Where(g => Root(g) || g == added).ToArray();
                var colliders = structural.SelectMany(g => g.GetComponentsInChildren<Collider>()).Where(c => c.enabled && !c.isTrigger).ToArray();
                ValidateRoutes(colliders, Source.GetPhysicsScene());
                ValidateDuplicates(structural.SelectMany(g => g.GetComponentsInChildren<MeshFilter>()), f => originalObjects.Contains(f.gameObject));
                var renderers = structural.SelectMany(g => g.GetComponentsInChildren<Renderer>()).Where(r => r.enabled).ToArray();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                foreach (var collider in colliders) bounds.Encapsulate(collider.bounds);
                Debug.Log($"NUCLEO_INTEGRACION_VALIDADA: {moved.Length} pisos trasladados, {opened.Length} aperturas, {capitals.Length} capiteles desactivados, {widened.Length} ensanchados, " +
                    $"{portal.Count} muros inactivos, {duplicateRails.Count} barandas duplicadas inactivas. " +
                    $"Nucleo: {added.GetComponentsInChildren<Renderer>().Length} renderizadores, {added.GetComponentsInChildren<Collider>().Length} colliders. " +
                    $"Envolvente min={bounds.min:F3} max={bounds.max:F3} dimensiones={bounds.size:F3}. " +
                    $"{samples} muestras, altura libre minima {minimumClearance:F3} m, muerte bajo Y={VoidHeight}.");
                foreach (var capital in capitals)
                    Debug.Log("NUCLEO_CAPITEL_DESACTIVADO: " + GlobalObjectId.GetGlobalObjectIdSlow(identities[capital]) +
                        " " + identities[capital].transform.position.ToString("F4"));
            }
        }
    }
}
