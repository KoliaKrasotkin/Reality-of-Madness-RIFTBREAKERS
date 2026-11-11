using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    // Manual menu commands only. No startup hooks, asset writes, gameplay or scene saving.
    public static class SunDungeonStructureGenerator
    {
        const string Target = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string PreviewName = "TEMP_Ciudad_del_Sol_Structure_Preview";
        const string RootName = "Dungeon_Ciudad_del_Sol_Generated";
        const string Signature = "__ROM_SolarStructure_12D9D67A_v1";
        const string Menu = "ROM/Dungeon Ciudad del Sol/Estructura/";
        const string ET = "Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string ST = "Assets/Standard Assets/Sun_Temple/Prefabs/";
        const string EG = "Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/";
        const float Module = 6f;
        const float Rise = 4f;

        enum Shape { Rectangle, Circle, HalfEllipse }

        sealed class Zone
        {
            public readonly string Name;
            public readonly Vector3 Center;
            public readonly Vector2 Size;
            public readonly Shape Form;
            public Zone(string name, Vector3 center, Vector2 size, Shape form = Shape.Rectangle)
            { Name = name; Center = center; Size = size; Form = form; }

            public bool Contains(Vector3 p)
            {
                float x = (p.x - Center.x) / (Size.x * .5f);
                if (Form == Shape.Rectangle)
                    return Mathf.Abs(x) <= 1.0001f && Mathf.Abs(p.z - Center.z) <= Size.y * .5f + .001f;
                // A D-shaped cloister: straight south frontage, curved north perimeter.
                float z = Form == Shape.HalfEllipse
                    ? (p.z - (Center.z - Size.y * .5f)) / Size.y
                    : (p.z - Center.z) / (Size.y * .5f);
                return x * x + z * z <= 1.0001f && (Form != Shape.HalfEllipse || z >= 0);
            }
        }

        sealed class Route
        {
            public readonly string Name;
            public readonly int From, To;
            public readonly float Width;
            public readonly Vector3[] Points;
            public Route(string name, int from, int to, float width, params Vector3[] points)
            { Name = name; From = from; To = to; Width = width; Points = points; }
        }

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static readonly Zone[] Zones =
        {
            new Zone("01 - Entrada Portico Solar", V(-126, 0, -120), new Vector2(72, 84)),
            new Zone("02 - Jardines Dorados", V(-102, 4, -18), new Vector2(96, 96)),
            new Zone("03 - Plaza del Eclipse", V(18, 12, 12), new Vector2(120, 120), Shape.Circle),
            new Zone("04 - Archivos Solares", V(-108, 20, 108), new Vector2(84, 72)),
            new Zone("05 - Claustro del Eclipse", V(18, 28, 144), new Vector2(108, 96), Shape.HalfEllipse),
            new Zone("06 - Santuario Profano", V(150, 4, -78), new Vector2(96, 108)),
            new Zone("07 - El Sol Corrompido", V(180, 36, 156), new Vector2(120, 120), Shape.Circle)
        };

        // Orthogonal bridges keep full-width landings and avoid diagonal tile seams.
        // Y is the WALKING surface, not the base prefab pivot.
        static readonly Route[] Routes =
        {
            new Route("01-02 - Ascenso a jardines", 0, 1, 12, V(-114,0,-78), V(-114,4,-66)),
            new Route("02-03 - Puente de la plaza", 1, 2, 12, V(-54,4,12), V(-42,12,12)),
            new Route("03-04 - Galeria occidental", 2, 3, 12,
                V(-36,12,42), V(-54,12,42), V(-54,20,84), V(-66,20,84)),
            // 24 rather than 18 gives whole 6m modules and 12m half-width landings.
            new Route("03-05 - Gran escalinata del claustro", 2, 4, 24,
                V(-12,12,60), V(-12,12,84), V(54,28,84), V(54,28,96)),
            new Route("04-05 - Galeria de los archivos", 3, 4, 12, V(-66,20,120), V(-36,28,120)),
            new Route("03-06 - Descenso al santuario", 2, 5, 12,
                V(78,12,12), V(90,12,12), V(90,4,-54), V(102,4,-54)),
            new Route("05-07 - Puente ceremonial", 4, 6, 12,
                V(72,28,120), V(102,28,120), V(102,36,156), V(120,36,156)),
            new Route("06-07 - Ascenso oriental", 5, 6, 12,
                V(198,4,-54), V(228,4,-54), V(228,36,108), V(204,36,108)),
            new Route("01-06 - Ruta secundaria meridional", 0, 5, 6,
                V(-90,0,-132), V(36,0,-132), V(36,4,-120), V(102,4,-120))
        };

        static readonly Dictionary<string, string> AssetPaths = new Dictionary<string, string>
        {
            { "Base", ET + "Pits and Platforms/Platform_Base_01.prefab" },
            { "Stair", ET + "Staircases/Staircase_01.prefab" },
            { "Rail", ET + "Railings and Decors/Railing_01.prefab" },
            { "StairRail", ET + "Railings and Decors/Railing_Staircase_01.prefab" },
            { "Column", ET + "Columns/Column_01.prefab" },
            { "Wall", ET + "Walls/Wall_Big_01.prefab" },
            { "Arch", ET + "Bases/Thin/Entrance_Arch_01.prefab" },
            { "Tower", ST + "Buildings_Prefabbed/Bld_TempleTower_A.prefab" },
            { "Gate", EG + "SM_MainGate.prefab" }
        };

        [MenuItem(Menu + "1. Previsualizar en escena temporal")]
        public static void Preview()
        {
            if (!CanRun()) return;
            Scene previous = SceneManager.GetActiveScene();
            Scene preview = default;
            bool created = false;
            try
            {
                ValidatePlan();
                var assets = LoadAssets();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    if (s.path == "" && s.name == PreviewName) { preview = s; break; }
                }
                if (!preview.IsValid())
                {
                    preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    preview.name = PreviewName;
                    created = true;
                }
                SceneManager.SetActiveScene(preview);
                Build(preview, assets, true);
            }
            catch (Exception e)
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (created && preview.IsValid()) EditorSceneManager.CloseScene(preview, true);
                Report(e);
            }
        }

        [MenuItem(Menu + "2. Generar o regenerar en Dungeon_Isla_del_Sol")]
        public static void Generate()
        {
            if (!CanRun()) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Target)
            {
                EditorUtility.DisplayDialog("Ciudad del Sol", "Activa manualmente " + Target +
                    ". El generador no abre ni cambia escenas automaticamente.", "Cerrar");
                return;
            }
            try { ValidatePlan(); Build(scene, LoadAssets(), false); }
            catch (Exception e) { Report(e); }
        }

        [MenuItem(Menu + "3. Eliminar lo generado en la escena activa")]
        public static void Remove()
        {
            if (!CanRun()) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Target && !(scene.path == "" && scene.name == PreviewName)) return;
            try
            {
                var roots = OwnedRoots(scene);
                if (roots.Count == 0) return;
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Eliminar estructura Ciudad del Sol");
                foreach (var root in roots) Undo.DestroyObjectImmediate(root);
                Undo.CollapseUndoOperations(group);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            catch (Exception e) { Report(e); }
        }

        [MenuItem(Menu + "4. Validar plan y prefabs (solo lectura)")]
        public static void Validate()
        {
            try
            {
                ValidatePlan();
                LoadAssets();
                Debug.Log("Ciudad del Sol: 7 zonas conectadas, 9 rutas, cotas multiples de 4, " +
                    "modulos de 6, escaleras y descansos coherentes. Huella nominal 402 x 378; " +
                    "presupuesto arquitectonico aproximado 420 x 400. No se crearon objetos.");
            }
            catch (Exception e) { Report(e); }
        }

        static bool CanRun() => !EditorApplication.isPlayingOrWillChangePlaymode &&
                                !EditorApplication.isCompiling && !EditorApplication.isUpdating;

        static void Report(Exception e)
        {
            if (e is OperationCanceledException) Debug.Log("Generacion cancelada; se conservo la estructura anterior.");
            else Debug.LogException(e);
        }

        static List<GameObject> OwnedRoots(Scene scene)
        {
            var result = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != RootName) continue;
                if (!root.transform.Find(Signature))
                    throw new InvalidOperationException("Existe una raiz con el mismo nombre sin firma del generador. No se modificara.");
                result.Add(root);
            }
            return result;
        }

        sealed class Asset
        {
            public GameObject Prefab;
            public Bounds Bounds;
        }

        static Dictionary<string, Asset> LoadAssets()
        {
            var result = new Dictionary<string, Asset>();
            foreach (var pair in AssetPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pair.Value);
                if (!prefab) throw new InvalidOperationException("Falta el prefab: " + pair.Value);
                if (!prefab.GetComponentsInChildren<Collider>(true).Any(c => c.enabled && !c.isTrigger &&
                    (!(c is MeshCollider mesh) || mesh.sharedMesh)))
                    throw new InvalidOperationException("Sin collider solido configurado: " + pair.Value);
                result.Add(pair.Key, new Asset { Prefab = prefab, Bounds = AssetBounds(prefab) });
            }
            var b = result["Base"].Bounds;
            if ((b.size - new Vector3(6, 2, 6)).sqrMagnitude > .01f)
                throw new InvalidOperationException("Platform_Base_01 ya no mide 6 x 2 x 6. Revisar el modulo.");
            var floorCollider = result["Base"].Prefab.GetComponent<BoxCollider>();
            if (!floorCollider || !floorCollider.enabled || floorCollider.isTrigger ||
                (floorCollider.size - new Vector3(6,2,6)).sqrMagnitude > .01f ||
                (floorCollider.center - new Vector3(0,1,0)).sqrMagnitude > .01f)
                throw new InvalidOperationException("El collider del suelo ya no coincide con el modulo de 6 x 2 x 6.");
            var stair = result["Stair"].Bounds;
            if (Mathf.Abs(stair.max.y - 4) > .05f || Mathf.Abs(stair.min.x) > .05f ||
                Mathf.Abs(stair.size.z - 6) > .05f)
                throw new InvalidOperationException("La escalera cambio de escala o pivote; revisar el ascenso hacia -X.");
            return result;
        }

        // Read mesh bounds on prefab assets, including child transforms, without instantiation.
        static Bounds AssetBounds(GameObject prefab)
        {
            bool found = false;
            Bounds bounds = default;
            Matrix4x4 root = Matrix4x4.TRS(Vector3.zero, prefab.transform.localRotation, prefab.transform.localScale);
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.sharedMesh) continue;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (!renderer || !renderer.enabled) continue;
                if (filter.GetComponents<MonoBehaviour>().Any(c => c && c.GetType().FullName == "SunTemple.DisableRenderer")) continue;
                Matrix4x4 matrix = root * prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                Bounds local = filter.sharedMesh.bounds;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 p = matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, V(x,y,z)));
                            if (!found) { bounds = new Bounds(p, Vector3.zero); found = true; }
                            else bounds.Encapsulate(p);
                        }
            }
            if (!found) throw new InvalidOperationException("Prefab sin MeshRenderer medible: " + prefab.name);
            return bounds;
        }

        static bool Multiple(float value, float step) => Mathf.Abs(value / step - Mathf.Round(value / step)) < .001f;

        static int Flights(Vector3 a, Vector3 b) => Mathf.RoundToInt(Mathf.Abs(b.y - a.y) / Rise);
        static float RunLength(Vector3 a, Vector3 b) => new Vector2(b.x-a.x, b.z-a.z).magnitude;
        static int RestCount(int flights) => Mathf.Max(0, (flights - 1) / 2);

        // Read-only route profile for the exterior NavMesh validator.
        internal static Vector3[] GetExteriorRouteCheckpoints(string routeId)
        {
            var route = Routes.Single(item => item.Name.StartsWith(routeId + " - ", StringComparison.Ordinal));
            var points = new List<Vector3> { route.Points[0] };
            for (int i = 0; i < route.Points.Length - 1; i++)
            {
                var a = route.Points[i];
                var b = route.Points[i + 1];
                bool reverse = a.y > b.y;
                var low = reverse ? b : a;
                var high = reverse ? a : b;
                int flights = Flights(a, b);
                var direction = new Vector3(high.x - low.x, 0, high.z - low.z).normalized;
                var segment = new List<Vector3>();
                if (flights == 0) segment.Add((a + b) * .5f);
                else
                {
                    float start = FlightStart(route, i, flights, reverse);
                    if (start > .01f) segment.Add(low + direction * (start * .5f));
                    var cursor = low + direction * start;
                    for (int flight = 0; flight < flights; flight++)
                    {
                        segment.Add(cursor + direction * 3f + Vector3.up * 2f);
                        cursor += direction * 6f + Vector3.up * 4f;
                        if ((flight + 1) % 2 == 0 && flight < flights - 1)
                        {
                            segment.Add(cursor + direction * 3f);
                            cursor += direction * 6f;
                        }
                    }
                    if (RunLength(cursor, high) > .01f) segment.Add((cursor + high) * .5f);
                }
                if (reverse) segment.Reverse();
                points.AddRange(segment);
                points.Add(b);
            }
            return points.ToArray();
        }

        static float FlightStart(Route route, int index, int flights, bool reverse)
        {
            float free = RunLength(route.Points[index], route.Points[index + 1]) - flights * 6 - RestCount(flights) * 6;
            float startClear = (reverse ? index < route.Points.Length - 2 : index > 0) ? route.Width * .5f : 0;
            float endClear = (reverse ? index > 0 : index < route.Points.Length - 2) ? route.Width * .5f : 0;
            return Mathf.Clamp(Mathf.Floor(free * .5f / 6) * 6, startClear, free - endClear);
        }

        static void ValidatePlan()
        {
            foreach (var zone in Zones)
                if (!Multiple(zone.Size.x, Module) || !Multiple(zone.Size.y, Module) || !Multiple(zone.Center.y, Rise))
                    throw new InvalidOperationException("Zona fuera de la reticula: " + zone.Name);
            var reached = new HashSet<int> { 0 };
            foreach (var route in Routes)
            {
                if (!Multiple(route.Width, Module) || route.Width < 6)
                    throw new InvalidOperationException("Ancho de ruta invalido: " + route.Name);
                if (Mathf.Abs(route.Points[0].y - Zones[route.From].Center.y) > .01f ||
                    Mathf.Abs(route.Points.Last().y - Zones[route.To].Center.y) > .01f)
                    throw new InvalidOperationException("Cotas de conexion incorrectas: " + route.Name);
                // Allow half a module for the stepped perimeter of curved platforms.
                if (!TouchesFloor(Zones[route.From], route.Points[0]) ||
                    !TouchesFloor(Zones[route.To], route.Points.Last()))
                    throw new InvalidOperationException("Puerto sin contacto con la plataforma: " + route.Name);
                for (int i = 0; i < route.Points.Length - 1; i++)
                {
                    Vector3 a = route.Points[i], b = route.Points[i+1];
                    float length = RunLength(a,b);
                    int flights = Flights(a,b);
                    float clear = (i > 0 ? route.Width*.5f : 0) +
                                  (i < route.Points.Length-2 ? route.Width*.5f : 0);
                    if ((Mathf.Abs(a.x-b.x) > .01f && Mathf.Abs(a.z-b.z) > .01f) ||
                        length < .1f || !Multiple(length, Module) || !Multiple(b.y-a.y, Rise) ||
                        length + .01f < flights*Module + RestCount(flights)*Module + (flights > 0 ? clear : 0))
                        throw new InvalidOperationException("No hay recorrido suficiente para escalones y descansos: " + route.Name);
                }
            }
            for (int pass = 0; pass < Zones.Length; pass++)
                foreach (var r in Routes)
                {
                    if (reached.Contains(r.From)) reached.Add(r.To);
                    if (reached.Contains(r.To)) reached.Add(r.From);
                }
            if (reached.Count != Zones.Length) throw new InvalidOperationException("Hay zonas desconectadas.");
        }

        static bool TouchesFloor(Zone zone, Vector3 p)
        {
            // Test actual included cells, not just an ideal mathematical circle.
            foreach (Vector3 center in Cells(zone))
                if (Mathf.Abs(p.x-center.x) <= 3.01f && Mathf.Abs(p.z-center.z) <= 3.01f) return true;
            return false;
        }

        static IEnumerable<Vector3> Cells(Zone zone)
        {
            int nx = Mathf.RoundToInt(zone.Size.x / Module), nz = Mathf.RoundToInt(zone.Size.y / Module);
            for (int x = 0; x < nx; x++)
                for (int z = 0; z < nz; z++)
                {
                    Vector3 p = zone.Center + V((x+.5f)*Module-zone.Size.x*.5f, 0,
                        (z+.5f)*Module-zone.Size.y*.5f);
                    if (zone.Contains(p)) yield return p;
                }
        }

        static void Build(Scene scene, Dictionary<string, Asset> assets, bool preview)
        {
            var oldRoots = OwnedRoots(scene);
            GameObject staging = null;
            try
            {
                staging = new GameObject(RootName + "__Building");
                SceneManager.MoveGameObjectToScene(staging, scene);
                var context = new Context(scene, staging.transform, assets);
                foreach (var zone in Zones) context.BuildZone(zone);
                foreach (var route in Routes) context.BuildRoute(route);
                context.BuildLandmarks();
                if (preview) context.PreviewRig();
                var signature = new GameObject(Signature);
                signature.transform.SetParent(staging.transform, false);
                signature.hideFlags = HideFlags.HideInHierarchy;
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Generar estructura Ciudad del Sol");
                staging.name = RootName;
                Undo.RegisterCreatedObjectUndo(staging, "Generar estructura Ciudad del Sol");
                foreach (var old in oldRoots) Undo.DestroyObjectImmediate(old);
                Undo.CollapseUndoOperations(group);
                EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = staging;
                if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.Frame(context.TotalBounds, false);
                Debug.Log("Ciudad del Sol: " + context.Count + " instancias, 7 zonas y 9 rutas. " +
                    "No se guardo la escena ni se alteraron prefabs originales. " +
                    "Verificar materiales, paso del controlador y rendimiento antes de guardar.", staging);
                staging = null;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                // Existing generated geometry survives cancellation or build errors.
                if (staging) Object.DestroyImmediate(staging);
            }
        }

        sealed class Context
        {
            readonly Scene scene;
            readonly Transform root;
            readonly Dictionary<string, Asset> assets;
            readonly HashSet<Vector3Int> floorCells = new HashSet<Vector3Int>();
            public int Count { get; private set; }
            public Bounds TotalBounds { get; private set; }

            public Context(Scene scene, Transform root, Dictionary<string, Asset> assets)
            { this.scene = scene; this.root = root; this.assets = assets; }

            Transform Group(string name, Transform parent)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                return go.transform;
            }

            GameObject Raw(string key, Vector3 pivot, float yaw, Transform parent)
            {
                if (Count % 64 == 0 && EditorUtility.DisplayCancelableProgressBar("Ciudad del Sol",
                    parent.name + " - " + Count + " prefabs", Mathf.Min(.95f, Count / 6000f)))
                    throw new OperationCanceledException();
                var asset = assets[key];
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset.Prefab, scene);
                // Parent immediately, so rollback also removes a partially configured instance.
                go.transform.SetParent(parent, false);
                go.transform.localScale = asset.Prefab.transform.localScale;
                go.transform.rotation = Quaternion.Euler(0,yaw,0) * asset.Prefab.transform.localRotation;
                go.transform.position = pivot;
                PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
                // Imported door/demo behaviours must not introduce gameplay into this structural pass.
                // These are instance overrides; the source prefab assets remain untouched.
                foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (!behaviour) continue;
                    if (behaviour.GetType().FullName == "SunTemple.DisableRenderer")
                    {
                        var helper = behaviour.GetComponent<Renderer>();
                        if (helper)
                        {
                            helper.enabled = false;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(helper);
                        }
                    }
                    behaviour.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                }
                Count++;
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject,
                        StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);
                }
                Bounds b = asset.Bounds;
                Quaternion rotation = Quaternion.Euler(0,yaw,0);
                for (int x=-1; x<=1; x+=2)
                    for (int y=-1; y<=1; y+=2)
                        for (int z=-1; z<=1; z+=2)
                        {
                            Vector3 p = pivot + rotation * (b.center + Vector3.Scale(b.extents,V(x,y,z)));
                            Bounds total = TotalBounds;
                            if (Count == 1 && x == -1 && y == -1 && z == -1) total = new Bounds(p, Vector3.zero);
                            else total.Encapsulate(p);
                            TotalBounds = total;
                        }
                return go;
            }

            void AtSurface(string key, Vector3 surface, float yaw, Transform parent, bool top)
            {
                Bounds b = assets[key].Bounds;
                Vector3 offset = V(b.center.x, top ? b.max.y : b.min.y, b.center.z);
                Raw(key, surface - Quaternion.Euler(0,yaw,0)*offset, yaw, parent);
            }

            void Floor(Vector3 p, Transform parent, float yaw = 0)
            {
                // Shared bridge/landing/platform cells must not produce coplanar duplicate meshes.
                var cell = new Vector3Int(Mathf.RoundToInt(p.x*100),Mathf.RoundToInt(p.y*100),Mathf.RoundToInt(p.z*100));
                if (floorCells.Add(cell)) AtSurface("Base",p,yaw,parent,true);
            }
            void Rail(Vector3 p, float yaw, Transform parent) => AtSurface("Rail",p,yaw,parent,false);

            void Supports(Vector3 p, Transform parent)
            {
                float height = assets["Column"].Bounds.size.y;
                for (int level=0; level<2; level++)
                    AtSurface("Column", p - Vector3.up*(2+level*height), 0, parent, true);
            }

            public void BuildZone(Zone zone)
            {
                Transform group = Group(zone.Name,root);
                // Zone group pivots expose the exact approved coordinates in the Inspector.
                group.position = zone.Center;
                Transform floor = Group("Suelo modular - superficie Y=" + zone.Center.y,group);
                Transform rim = Group("Perimetro y basamento",group);
                Transform supports = Group("Soportes suspendidos",group);
                var cells = new HashSet<Vector2Int>();
                var centers = Cells(zone).ToList();
                foreach (var p in centers) cells.Add(new Vector2Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.z)));
                Vector3[] directions = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
                foreach (var p in centers)
                {
                    Floor(p,floor);
                    bool boundary = false;
                    foreach (var normal in directions)
                    {
                        var neighbor = new Vector2Int(Mathf.RoundToInt(p.x+normal.x*6),Mathf.RoundToInt(p.z+normal.z*6));
                        if (cells.Contains(neighbor)) continue;
                        boundary = true;
                        Vector3 edge = p + normal*3;
                        if (Opening(edge)) continue;
                        float yaw = Mathf.Atan2(-normal.z,normal.x)*Mathf.Rad2Deg;
                        bool wall = (zone == Zones[3] && normal != Vector3.back) ||
                                    (zone == Zones[4] && normal == Vector3.forward) ||
                                    (zone == Zones[5] && normal == Vector3.right);
                        string key = wall ? "Wall" : "Rail";
                        AtSurface(key,edge+normal*(assets[key].Bounds.size.x*.5f+.05f),yaw,rim,false);
                    }
                    // A second perimeter course adds thickness without burying the whole map in modules.
                    if (boundary) Floor(p-Vector3.up*2,rim);
                    int ix = Mathf.RoundToInt((p.x-zone.Center.x+zone.Size.x*.5f-3)/6);
                    int iz = Mathf.RoundToInt((p.z-zone.Center.z+zone.Size.y*.5f-3)/6);
                    if (ix % 4 == 1 && iz % 4 == 1) Supports(p,supports);
                }
                // Real, accessible upper side galleries make the gardens and archives distinct.
                if (zone == Zones[1]) SideGallery(group,V(-132,8,-18),12,48);
                if (zone == Zones[3]) SideGallery(group,V(-132,24,108),12,36);
            }

            void SideGallery(Transform parent, Vector3 center, int width, int depth)
            {
                Transform group = Group("Galeria lateral elevada +4",parent);
                for (int x=0; x<width/6; x++)
                    for (int z=0; z<depth/6; z++) Floor(center+V((x+.5f)*6-width*.5f,0,(z+.5f)*6-depth*.5f),group);
                float lowY = center.y-4;
                // Ascend westwards from the courtyard; high end meets the gallery's east edge.
                Vector3 high = center+V(width*.5f,0,0);
                StairFlight(high+V(6,-4,0),Vector3.left,6,group);
                for (int z=0; z<depth/6; z++)
                {
                    Vector3 edge = center+V(width*.5f+.55f,0,(z+.5f)*6-depth*.5f);
                    if (Mathf.Abs(edge.z-center.z)>6) Rail(edge,0,group);
                    Rail(center+V(-width*.5f-.55f,0,edge.z-center.z),0,group);
                }
                foreach (float sign in new[] {-1f,1f})
                    for (int x=0; x<width/6; x++) Rail(center+V((x+.5f)*6-width*.5f,0,sign*(depth*.5f+.55f)),90,group);
                // The original zone floor supplies the low landing; no gameplay components are added.
                if (Mathf.Abs(lowY-parent.position.y)>.01f) throw new InvalidOperationException("Galeria sin llegada inferior.");
            }

            bool Opening(Vector3 p)
            {
                foreach (var r in Routes)
                    for (int i=0; i<r.Points.Length-1; i++)
                    {
                        Vector3 a=r.Points[i], b=r.Points[i+1];
                        Vector2 av=new Vector2(a.x,a.z), bv=new Vector2(b.x,b.z), pv=new Vector2(p.x,p.z);
                        float t=Mathf.Clamp01(Vector2.Dot(pv-av,bv-av)/(bv-av).sqrMagnitude);
                        if (Vector2.Distance(pv,Vector2.Lerp(av,bv,t))<=r.Width*.5f+3.1f) return true;
                    }
                return false;
            }

            public void BuildRoute(Route route)
            {
                Transform group=Group(route.Name+" - ancho util "+route.Width,root);
                // Fill the stepped circular perimeter at ports, keeping the full bridge width.
                Vector3 first=route.Points[1]-route.Points[0]; first.y=0; first.Normalize();
                Vector3 last=route.Points.Last()-route.Points[route.Points.Length-2]; last.y=0; last.Normalize();
                Deck(route.Points[0]-first*6,first,6,route.Width,route,group);
                for (int i=0; i<route.Points.Length-1; i++) Segment(route,i,group);
                Deck(route.Points.Last(),last,6,route.Width,route,group);
                for (int i=1; i<route.Points.Length-1; i++) Landing(route,i,group);
            }

            void Segment(Route r, int index, Transform group)
            {
                Vector3 a=r.Points[index], b=r.Points[index+1];
                bool reverse=a.y>b.y;
                Vector3 low=reverse?b:a, high=reverse?a:b;
                Vector3 direction=(new Vector3(high.x,0,high.z)-new Vector3(low.x,0,low.z)).normalized;
                float length=RunLength(a,b);
                int flights=Flights(a,b);
                if (flights==0) { Deck(a,direction,length,r.Width,r,group); return; }
                float free=length-flights*6-RestCount(flights)*6;
                float start=FlightStart(r,index,flights,reverse);
                Deck(low,direction,start,r.Width,r,group);
                Vector3 cursor=low+direction*start;
                for (int step=0; step<flights; step++)
                {
                    StairFlight(cursor,direction,r.Width,group);
                    cursor+=direction*6+Vector3.up*4;
                    if ((step+1)%2==0 && step<flights-1)
                    { Deck(cursor,direction,6,r.Width,r,group); cursor+=direction*6; }
                }
                Deck(cursor,direction,free-start,r.Width,r,group);
            }

            void Deck(Vector3 start, Vector3 direction, float length, float width, Route route, Transform group)
            {
                if (length<.01f) return;
                Vector3 side=Vector3.Cross(Vector3.up,direction);
                float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
                int rows=Mathf.RoundToInt(length/6), lanes=Mathf.RoundToInt(width/6);
                for (int i=0; i<rows; i++)
                {
                    Vector3 center=start+direction*((i+.5f)*6);
                    for (int lane=0; lane<lanes; lane++) Floor(center+side*((lane+.5f)*6-width*.5f),group,yaw);
                    if (i%3==1) Supports(center,group);
                    if (AtJunction(center,route)) continue;
                    foreach (float sign in new[] {-1f,1f}) Rail(center+side*(sign*(width*.5f+.55f)),yaw,group);
                }
            }

            static bool AtJunction(Vector3 center, Route route)
            {
                for (int i=1; i<route.Points.Length-1; i++)
                    if (Vector2.Distance(new Vector2(center.x,center.z),
                        new Vector2(route.Points[i].x,route.Points[i].z))<route.Width*.5f+.01f) return true;
                return false;
            }

            void StairFlight(Vector3 low, Vector3 direction, float width, Transform group)
            {
                Vector3 side=Vector3.Cross(Vector3.up,direction);
                float yaw=Mathf.Atan2(direction.z,-direction.x)*Mathf.Rad2Deg;
                Vector3 high=low+direction*6+Vector3.up*4;
                for (int lane=0; lane<Mathf.RoundToInt(width/6); lane++)
                    Raw("Stair",high-Vector3.up*4+side*((lane+.5f)*6-width*.5f),yaw,group);
                // The matched stair railing runs down its local +Z from the high landing.
                float railYaw=Mathf.Atan2(-direction.x,-direction.z)*Mathf.Rad2Deg;
                float railLift=1.5f-assets["StairRail"].Bounds.max.y;
                foreach (float sign in new[] {-1f,1f})
                    Raw("StairRail",high+side*(sign*(width*.5f+.55f))+Vector3.up*railLift,railYaw,group);
            }

            void Landing(Route route, int index, Transform group)
            {
                Vector3 p=route.Points[index];
                int n=Mathf.RoundToInt(route.Width/6);
                for (int x=0; x<n; x++)
                    for (int z=0; z<n; z++) Floor(p+V((x+.5f)*6-route.Width*.5f,0,(z+.5f)*6-route.Width*.5f),group);
                Vector3 incoming=(route.Points[index-1]-p); incoming.y=0; incoming.Normalize();
                Vector3 outgoing=(route.Points[index+1]-p); outgoing.y=0; outgoing.Normalize();
                foreach (var normal in new[] {Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                {
                    if (Vector3.Dot(normal,incoming)>.9f || Vector3.Dot(normal,outgoing)>.9f) continue;
                    Vector3 tangent=Vector3.Cross(Vector3.up,normal);
                    float yaw=Mathf.Atan2(tangent.x,tangent.z)*Mathf.Rad2Deg;
                    for (int i=0; i<n; i++) Rail(p+normal*(route.Width*.5f+.55f)+
                        tangent*((i+.5f)*6-route.Width*.5f),yaw,group);
                }
                Supports(p,group);
            }

            public void BuildLandmarks()
            {
                Transform group=Group("Arquitectura monumental - fuera de los pasos",root);
                AtSurface("Tower",V(-144,0,-120),0,group,false);
                AtSurface("Tower",V(-108,0,-120),0,group,false);
                AtSurface("Tower",V(-132,20,132),0,group,false);
                AtSurface("Tower",V(18,28,174),0,group,false);
                AtSurface("Tower",V(144,36,174),0,group,false);
                AtSurface("Tower",V(216,36,174),0,group,false);
                // Its solid box blocks passage: deliberately a rear monument, never an entrance.
                AtSurface("Gate",V(180,36,204),90,group,false);
                foreach (var p in new[] {V(-138,0,-150),V(-114,0,-150),V(138,4,-108),V(162,4,-108)})
                    AtSurface("Arch",p,90,group,false);
            }

            public void PreviewRig()
            {
                Transform rig=Group("Solo previsualizacion - camara y luces",root);
                var cameraObject=new GameObject("Camara general Ciudad del Sol");
                cameraObject.transform.SetParent(rig,false);
                var camera=cameraObject.AddComponent<Camera>();
                camera.orthographic=true;
                camera.orthographicSize=290;
                camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.055f,.045f,.09f);
                camera.transform.rotation=Quaternion.Euler(65,0,0);
                camera.transform.position=V(42,20,24)-camera.transform.forward*650;
                camera.nearClipPlane=.3f;
                camera.farClipPlane=1400;
                AddLight(rig,"Luz principal",V(50,-35,0),1.3f);
                AddLight(rig,"Luz de relleno",V(35,145,0),.65f);
            }

            static void AddLight(Transform parent,string name,Vector3 rotation,float intensity)
            {
                var go=new GameObject(name);
                go.transform.SetParent(parent,false);
                go.transform.rotation=Quaternion.Euler(rotation);
                var light=go.AddComponent<Light>();
                light.type=LightType.Directional;
                light.intensity=intensity;
                light.shadows=LightShadows.None;
            }
        }
    }
}
