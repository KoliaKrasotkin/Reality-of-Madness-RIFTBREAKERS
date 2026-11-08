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
        const string GridIntegrationRoot = "Subsuelo_34_Naves_Sin_Reticula";
        const string GridMeshFolder = "Assets/ROM/CiudadDelSol34Naves";

        public static void IntegrateColumnGridInBatch()
        {
            var scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            Require(!scene.isDirty && Hash() == OpenHallsHash && !Directory.Exists(GridMeshFolder) &&
                scene.GetRootGameObjects().All(root => root.name != GridIntegrationRoot),
                "La escena no coincide con la versión aprobada o el montaje ya existe.");
            string backup = Path.Combine(GridReviewFolder, "Dungeon_Isla_del_Sol.antes-integracion34.20261107-010007-709.unity");
            Require(File.Exists(backup) && SceneFileHash(backup) == OpenHallsHash, "El respaldo aprobado no coincide.");
            var designs = ReadOpenHallDesigns(OpenHallsPhase2);
            Require(designs.Length == 34 && designs.Sum(d => d.Width * d.Length) == 24732, "Cambió el inventario de naves.");
            for (int i = 0; i < designs.Length; i++)
                Require(!Sectors.Concat(ExpansionDesigns()).Concat(designs.Take(i)).Any(other =>
                    designs[i].West < other.East && designs[i].East > other.West &&
                    designs[i].South < other.North && designs[i].North > other.South), "Huella no autorizada: " + designs[i].Name);

            var rows = File.ReadLines(Path.Combine(GridReviewFolder, "Reticula.tsv")).Skip(1)
                .Select(line => line.Split('\t')).ToDictionary(row =>
                {
                    Require(GlobalObjectId.TryParse(row[0], out var id), "Identidad inválida: " + row[0]);
                    return id;
                });
            var earlier = rows.Where(pair => pair.Value[3] == "Retirado por las naves").Select(pair => pair.Key).ToHashSet();
            var extra = rows.Where(pair => pair.Value[3] == "Desactivar retícula").Select(pair => pair.Key).ToHashSet();
            Require(rows.Count == 480 && earlier.Count == 88 && extra.Count == 291, "Cambió el inventario de retícula.");
            var authorizedSupports = earlier.Concat(extra).ToHashSet();
            var restrictedIds = new ulong[] { 981306585, 1752911320, 1448740102, 786692939, 703139566,
                1451771354, 1763290661, 1522184970, 983808960, 2005529955, 856710452 };
            var individuallyChecked = authorizedSupports.Where(id => restrictedIds.Contains(id.targetPrefabId)).ToArray();
            Require(individuallyChecked.Length == 11, "La excepción debe abarcar exactamente los 11 apoyos aprobados.");
            var retained = new HashSet<GlobalObjectId>(rows.Keys.Except(authorizedSupports));
            foreach (var entry in rows)
            {
                var piece = ResolveGridPiece(scene, entry.Key);
                Require(piece.activeInHierarchy && piece.name == "Column_Ceiling_02" &&
                    piece.transform.root.name == entry.Value[1] &&
                    piece.transform.parent.name.StartsWith("04 - Columnas monumentales", StringComparison.Ordinal),
                    "Identidad de apoyo incorrecta: " + entry.Key);
            }
            foreach (var id in individuallyChecked)
            {
                var piece = ResolveGridPiece(scene, id);
                var parts = piece.GetComponentsInChildren<Component>(true);
                Require(parts.All(part => part && (part is Transform || part is MeshFilter || part is MeshRenderer || part is Collider)) &&
                    piece.GetComponentsInChildren<Transform>(true).All(part => part == piece.transform ||
                        !IsEntrance(part.name) && part.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) < 0 &&
                        part.name.IndexOf("ceiling", StringComparison.OrdinalIgnoreCase) < 0),
                    "Dependencia no autorizada en el apoyo " + id);
                Debug.Log($"RETICULA_APOYO_OK {id.targetPrefabId} {piece.transform.position} componentes={parts.Length} colliders={parts.OfType<Collider>().Count()}");
            }

            Physics.SyncTransforms();
            var routeSamples = RestrictedRouteSamples(scene.GetPhysicsScene());
            var preview = EditorSceneManager.NewPreviewScene();
            var resources = new List<Object>();
            var halls = new List<MountPlan>();
            bool folderCreated = false, touched = false, saveAttempted = false;
            try
            {
                BuildExpansionJoint(scene, preview, resources, halls, designs, OpenHallsHash);
                var previewCuts = new List<GameObject>();
                ClearRepetitiveColumns(scene, preview, halls, previewCuts, verifyApproved: true);
                Require(previewCuts.Count == extra.Count, "La copia no produjo las 291 desactivaciones adicionales.");
                var hallCuts = halls.SelectMany(hall => hall.Targets).ToHashSet();
                Require(hallCuts.Count == 1025 && hallCuts.Intersect(rows.Keys).ToHashSet().SetEquals(earlier) &&
                    !hallCuts.Overlaps(extra), "Cambió la lista de piezas del montaje de naves.");
                var allCuts = hallCuts.Concat(extra).ToArray();
                foreach (var id in allCuts)
                {
                    var piece = ResolveGridPiece(scene, id);
                    string group = piece.transform.parent.name;
                    Require(piece.activeInHierarchy &&
                        (rows.ContainsKey(id) || group.StartsWith("02 - Bovedas generales", StringComparison.Ordinal) ||
                         group.StartsWith("03 - Vigas", StringComparison.Ordinal)), "Recorte fuera de alcance: " + id);
                }
                Require(Hash() == OpenHallsHash && !scene.isDirty, "La escena cambió durante la revisión.");
                folderCreated = !string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/ROM", "CiudadDelSol34Naves"));
                Require(folderCreated, "No se pudo crear la carpeta de mallas.");
                var stone = AssetDatabase.LoadAssetAtPath<Material>(AssetFolder + "/PiedraOpaca.mat");
                Require(stone && !stone.IsKeywordEnabled("_ALPHATEST_ON") && stone.GetFloat("_AlphaClip") == 0,
                    "El material aprobado no es válido.");

                touched = true;
                var root = new GameObject(GridIntegrationRoot);
                SceneManager.MoveGameObjectToScene(root, scene);
                foreach (var id in allCuts)
                {
                    var piece = ResolveGridPiece(scene, id);
                    piece.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(piece);
                }
                for (int i = 0; i < halls.Count; i++)
                {
                    var hall = halls[i].Hall;
                    hall.name = halls[i].Design.Name;
                    hall.transform.SetParent(null, true);
                    SceneManager.MoveGameObjectToScene(hall, scene);
                    hall.transform.SetParent(root.transform, true);
                    int part = 0;
                    foreach (var filter in hall.GetComponentsInChildren<MeshFilter>())
                    {
                        var mesh = Object.Instantiate(filter.sharedMesh);
                        mesh.hideFlags = HideFlags.None;
                        AssetDatabase.CreateAsset(mesh, GridMeshFolder + $"/Nave_{i + 1:00}_{++part}.asset");
                        filter.sharedMesh = mesh;
                        filter.GetComponent<MeshCollider>().sharedMesh = mesh;
                        filter.GetComponent<MeshRenderer>().sharedMaterial = stone;
                    }
                }
                EditorSceneManager.ClosePreviewScene(preview);
                AssetDatabase.SaveAssets();
                ValidateGridIntegration(scene, halls, allCuts, retained, routeSamples);
                Require(Hash() == OpenHallsHash, "Cambió el archivo antes del guardado.");
                EditorSceneManager.MarkSceneDirty(scene);
                saveAttempted = true;
                Require(EditorSceneManager.SaveScene(scene), "Unity no pudo guardar.");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
                ValidateGridIntegration(scene, halls, allCuts, retained, routeSamples);
                Debug.Log($"RETICULA_PERSISTENCIA_OK: 34 naves, 68 mallas, 379 apoyos desactivados, 101 activos, SHA256={Hash()}");
            }
            catch
            {
                if (touched) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (saveAttempted && Hash() != OpenHallsHash) File.Copy(backup, Original, true);
                Require(Hash() == OpenHallsHash, "No se pudo restaurar el respaldo.");
                if (folderCreated) AssetDatabase.DeleteAsset(GridMeshFolder);
                throw;
            }
            finally
            {
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
        }

        static GameObject ResolveGridPiece(Scene scene, GlobalObjectId id)
        {
            var piece = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
            Require(piece && piece.scene == scene, "Pieza ausente: " + id);
            return piece;
        }

        static Dictionary<string, Vector3[]> RestrictedRouteSamples(PhysicsScene world)
        {
            var codes = new HashSet<string> { "O1", "O2", "O7", "N2", "N4", "S1", "E1", "E5" };
            var result = new Dictionary<string, Vector3[]>();
            foreach (var route in ConnectionProposals.Where(route => codes.Contains(route.Name.Split(' ')[0])))
            {
                var samples = new List<Vector3>();
                for (float x = route.West + .5f; x < route.East; x += 1)
                for (float z = route.South - 1.5f; z < route.North + 2; z += 1)
                {
                    var point = new Vector3(x, Floor, z);
                    if (GroundAt(world, point) && GridPassageClear(world, point)) samples.Add(point);
                }
                Require(samples.Count > 0, "No hay piso transitable de referencia en " + route.Name);
                result.Add(route.Name, samples.ToArray());
            }
            Require(result.Count == 8, "Falta un recorrido autorizado.");
            return result;
        }

        static readonly Collider[] GridPassageHits = new Collider[1];

        static bool GridPassageClear(PhysicsScene world, Vector3 p) =>
            world.OverlapCapsule(p + Vector3.up * .36f, p + Vector3.up * 2.13f, .32f,
                GridPassageHits, ~0, QueryTriggerInteraction.Ignore) == 0;

        static void ValidateGridIntegration(Scene scene, List<MountPlan> halls, GlobalObjectId[] cuts,
            HashSet<GlobalObjectId> retained, Dictionary<string, Vector3[]> routeSamples)
        {
            var root = scene.GetRootGameObjects().Single(item => item.name == GridIntegrationRoot);
            Require(root.transform.childCount == 34, "Faltan naves.");
            foreach (var id in cuts)
            {
                var piece = ResolveGridPiece(scene, id);
                Require(!piece.activeSelf && piece.GetComponentsInChildren<Collider>(true).All(c => !c.gameObject.activeInHierarchy),
                    "Un recorte o sus colliders siguen activos: " + id);
            }
            Require(retained.Count == 101 && retained.All(id => ResolveGridPiece(scene, id).activeInHierarchy),
                "Cambió un apoyo estratégico.");
            var meshes = new HashSet<Mesh>();
            foreach (var plan in halls)
            {
                var hall = root.transform.Cast<Transform>().Single(child => child.name == plan.Design.Name).gameObject;
                var filters = hall.GetComponentsInChildren<MeshFilter>();
                Require(filters.Length == 2 && filters.All(filter => filter.sharedMesh && meshes.Add(filter.sharedMesh) &&
                    AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith(GridMeshFolder + "/", StringComparison.Ordinal) &&
                    filter.GetComponent<MeshCollider>().enabled &&
                    filter.GetComponent<MeshCollider>().sharedMesh == filter.sharedMesh),
                    "Referencias incorrectas: " + plan.Design.Name);
                ValidateMounted(hall, plan.Design, plan.EndCapitals);
            }
            Require(meshes.Count == 68, "Faltan mallas independientes.");
            var world = scene.GetPhysicsScene();
            foreach (var route in routeSamples)
            {
                Require(route.Value.All(point => GroundAt(world, point)), "Se alteró el piso de " + route.Key);
                var clear = route.Value.Where(point => GridPassageClear(world, point)).ToArray();
                Require(clear.Length >= .9f * route.Value.Length &&
                    (!GridRouteConnects(route.Value) || GridRouteConnects(clear, route.Value.Min(p => p.z), route.Value.Max(p => p.z))),
                    "Se interrumpió un recorrido de " + route.Key);
                Debug.Log($"RETICULA_RUTA_OK {route.Key}: {clear.Length}/{route.Value.Length} puntos de paso conservados.");
            }
        }

        static bool GridRouteConnects(Vector3[] points, float? south = null, float? north = null)
        {
            if (points.Length == 0) return false;
            float start = south ?? points.Min(p => p.z), end = north ?? points.Max(p => p.z);
            var remaining = points.ToHashSet();
            var queue = new Queue<Vector3>(points.Where(p => p.z == start));
            var directions = new[] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                if (point.z == end) return true;
                foreach (var direction in directions)
                    if (remaining.Remove(point + direction)) queue.Enqueue(point + direction);
            }
            return false;
        }
    }
}
