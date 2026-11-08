using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string PlayDecorMenu = "ROM/Dungeon Ciudad del Sol/Decoración jugable/";
        const string PlayDecorOutput = "Backups/CiudadDelSol/DecoracionJugable";
        const string PlayDecorHash = "2128133CCBD5A94504306F1685E8A6138193E1F5C1012DB8AC520D017013B2B4";
        const string SunProps = "Assets/Standard Assets/Sun_Temple/Prefabs/Props/";
        const string FirePrefab = Temple + "VFX/FX_Fire_01.prefab";
        const string Pedestal = Temple + "Lanterns/Pedestal_01";
        const string Torch = Temple + "Lanterns/Torch_01";
        const string Lantern = Temple + "Lanterns/Lantern_Small_02_Unlit";
        const string Pots = SunProps + "Prop_ClayPot_Grouped_A";
        enum Flame { Gold, Violet, Crimson }

        sealed class PlayableDecor
        {
            public GameObject Root;
            public readonly List<ParticleSystem> Fires = new List<ParticleSystem>();
            public readonly List<(string name, Vector3 eye, Vector3 target)> Views = new List<(string, Vector3, Vector3)>();
            public readonly List<string> Audit = new List<string>();
        }

        [MenuItem(PlayDecorMenu + "1. Abrir propuesta temporal")]
        static void OpenPlayableDecor()
        {
            var source = SceneManager.GetActiveScene();
            Require(source.path == Original && !source.isDirty && Hash() == PlayDecorHash &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                StageUtility.GetCurrentStage() == StageUtility.GetMainStage(), "Abrí la escena definitiva limpia en modo edición.");
            var stage = ScriptableObject.CreateInstance<RiftAtmosphereStage>();
            stage.Source = source;
            stage.DecorationMode = true;
            StageUtility.GoToStage(stage, true);
        }

        [MenuItem(PlayDecorMenu + "2. Siguiente vista")] static void NextPlayableDecor() => NextAtmosphereView();
        [MenuItem(PlayDecorMenu + "3. Alternar decoración")] static void TogglePlayableDecor() => ToggleAtmosphere();
        [MenuItem(PlayDecorMenu + "4. Cerrar sin guardar")] static void ClosePlayableDecor() => CloseAtmosphere();

        [MenuItem(PlayDecorMenu + "5. Validar y capturar")]
        public static void CapturePlayableDecor()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && Hash() == PlayDecorHash &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "La propuesta requiere la escena integrada y un dispositivo gráfico.");
            var scene = EditorSceneManager.OpenPreviewScene(Original);
            var resources = new List<Object>();
            try
            {
                ValidateFinalLook(scene);
                var decor = BuildPlayableDecor(scene, resources);
                CaptureAtmosphereViews(scene, null, decor.Root, decor.Views.ToArray(), PlayDecorOutput, () =>
                {
                    foreach (var fire in decor.Fires) fire.Simulate(1.36f, false, true, true);
                });
                File.WriteAllLines(Path.Combine(PlayDecorOutput, "Validacion.txt"), decor.Audit);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
            Require(Hash() == PlayDecorHash, "Cambió la escena definitiva.");
            Debug.Log("DECORACION_JUGABLE_OK");
        }

        static PlayableDecor BuildPlayableDecor(Scene scene, List<Object> resources)
        {
            var original = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>()).ToArray();
            var city = original.Single(t => t.name == "Dungeon_Ciudad_del_Sol_Generated");
            var corridors = city.Cast<Transform>().Where(t => t.name.Contains("ancho util")).ToArray();
            Require(corridors.Length == 9 && corridors.Count(t => t.name.EndsWith("ancho util 6", StringComparison.Ordinal)) == 1,
                "Faltan recorridos aprobados.");
            Physics.SyncTransforms();
            var physics = scene.GetPhysicsScene();
            var colliders = original.SelectMany(t => t.GetComponents<Collider>()).Where(c => c.enabled && !c.isTrigger).ToArray();
            var solids = colliders.Select(c => c.bounds).Where(b => b.size.sqrMagnitude > .001f).ToArray();
            var keepClear = corridors.SelectMany(t => t.GetComponentsInChildren<Collider>())
                .Where(c => c.enabled && !c.isTrigger)
                .Select(c =>
                {
                    var b = c.bounds;
                    b.Expand(new Vector3(1f, 0, 1f));
                    b.Encapsulate(b.max + Vector3.up * 2.7f);
                    return b;
                }).ToList();
            Require(keepClear.Count > 60, "Faltan colliders de los recorridos.");
            foreach (var t in original.Where(t => t.name.IndexOf("Staircase", StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.name.IndexOf("Spawn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.name.IndexOf("Portal", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                var bounds = t.GetComponentsInChildren<Collider>().Select(c => c.bounds)
                    .Concat(t.GetComponentsInChildren<MeshRenderer>().Select(r => r.bounds)).ToArray();
                var reserve = bounds.Aggregate(new Bounds(t.position, Vector3.one), (a, b) => { a.Encapsulate(b); return a; });
                reserve.Expand(new Vector3(1.2f, 4f, 1.2f));
                keepClear.Add(reserve);
            }

            var root = new GameObject("Decoración jugable · temporal");
            SceneManager.MoveGameObjectToScene(root, scene);
            var result = new PlayableDecor { Root = root };
            var occupied = new List<Bounds>();
            var overlap = new Collider[64];
            var fireSamples = new Dictionary<Flame, (Vector3 position, Vector3 inward)>();
            var counts = new Dictionary<string, int>();
            var firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirePrefab);
            var shader = Shader.Find("ROM/Ciudad del Sol/Ceremonial Fire");
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Standard Assets/Eternal Temple/Textures/Atlas/FX_Fire_01.psd");
            Require(firePrefab && shader && shader.isSupported && atlas, "Falta el fuego animado o el shader URP.");
            var fireMaterials = new Dictionary<Flame, Material>();
            AddFireMaterial(Flame.Gold, new Color(2.8f, 1.15f, .24f));
            AddFireMaterial(Flame.Violet, new Color(1.55f, .20f, 2.65f));
            AddFireMaterial(Flame.Crimson, new Color(2.8f, .23f, .30f));

            var addedReserves = new[]
            {
                new Bounds(new Vector3(18, 14, 12), new Vector3(46, 6, 46)),
                new Bounds(new Vector3(180, 38, 156), new Vector3(63, 8, 66)),
                new Bounds(new Vector3(18, -35, 6), new Vector3(21, 7, 26)),
                new Bounds(new Vector3(150, 5, 28), new Vector3(6, 6, 18)),
                new Bounds(new Vector3(153, -14, 99), new Vector3(10, 5, 7))
            };
            Transform plaza = Sector("Plaza del Eclipse");
            foreach (float x in new[] { -15f, 51f })
            {
                float side = Mathf.Sign(x - 18);
                Place(plaza, Pedestal, new Vector3(x + side * 4, 16, -29.3f), 0, 1, Flame.Gold);
                Place(plaza, Pots, new Vector3(x + side * 4, 16, -22), side * 23, .88f);
                Place(plaza, Lantern, new Vector3(x + side * 2, 16, -30), 0, .78f);
                Place(plaza, SunProps + "Prop_ClayPot_D", new Vector3(x + side * 3.7f, 16, -25), 15, .9f);
            }

            Transform archives = Sector("Archivos Solares");
            var archiveArchitecture = city.GetComponentsInChildren<Transform>().Single(t => t.name == "ArchivosSolares_Architecture");
            var tables = archiveArchitecture.GetComponentsInChildren<Transform>()
                .Where(t => t.name.StartsWith("Prop_Table_A", StringComparison.Ordinal) &&
                    t.GetComponentsInChildren<MeshRenderer>().Any()).Take(2).ToArray();
            Require(tables.Length == 2, "No se encontraron las dos mesas de lectura.");
            foreach (var table in tables)
            {
                var top = StaticBounds(table.gameObject);
                Place(archives, SunProps + "Prop_BookStash_A", Surface(top, -.23f, -.15f), 12, .84f, onFurniture:true);
                Place(archives, SunProps + "Prop_Book_A", Surface(top, .23f, -.14f), -22, .9f, onFurniture:true);
                Place(archives, SunProps + "Prop_Candles_A", Surface(top, .20f, .23f), 18, .8f, onFurniture:true);
                Place(archives, SunProps + "Prop_Book_D", Surface(top, -.20f, .23f), 68, .83f, onFurniture:true);
                Place(archives, Pedestal, new Vector3(top.center.x + 2.4f, 20, top.center.z + 1.6f), 0, .85f, Flame.Gold);
            }

            Transform portico = Sector("Portico Solar");
            Place(portico, Torch, new Vector3(-152, 0, -142), 24, .9f, Flame.Gold);
            Place(portico, Torch, new Vector3(-100, 0, -142), -24, .9f, Flame.Gold);
            Place(portico, Pots, new Vector3(-152, 0, -138), -16, .95f);
            Place(portico, Pots, new Vector3(-100, 0, -138), 20, .88f);
            Place(portico, Lantern, new Vector3(-154, 0, -133), 25, .88f);

            Transform cloister = Sector("Claustro del Eclipse");
            foreach (float x in new[] { -15f, 51f })
            {
                Place(cloister, Pedestal, new Vector3(x, 36, 154), 24, .85f, Flame.Gold);
                Place(cloister, Pots, new Vector3(x, 36, 158), -22, .80f);
                Place(cloister, Lantern, new Vector3(x, 36, 150), 25, .80f);
            }

            Transform sanctuary = Sector("Santuario Profano");
            Place(sanctuary, Pedestal, new Vector3(119, 8, -101), 0, .92f, Flame.Violet);
            Place(sanctuary, Torch, new Vector3(180, 8, -101), 180, .85f, Flame.Violet);
            Place(sanctuary, RiftCrystal + "CrystalGround", new Vector3(117, 4, -94), 47, .7f);
            Place(sanctuary, Lantern, new Vector3(175, 8, -101), -50, .84f);
            Place(sanctuary, Pots, new Vector3(179, 8, -95), 22, .8f);

            Transform arena = Sector("El Sol Corrompido");
            Place(arena, Pedestal, new Vector3(150, 36, 114), 15, 1, Flame.Crimson);
            Place(arena, Pedestal, new Vector3(210, 36, 114), -15, 1, Flame.Crimson);

            Transform access = Group("Acceso al subsuelo");
            Place(access, Pedestal, new Vector3(154.8f, 4, 32), 0, .78f, Flame.Violet);
            Place(access, Lantern, new Vector3(145.4f, 4, 34), -23, .72f);
            Place(access, Pedestal, new Vector3(161.4f, -16, 104), 0, .84f, Flame.Violet);
            Place(access, Lantern, new Vector3(143, -16, 103), 21, .8f);

            Transform underground = Group("Cruces del subsuelo");
            foreach (var point in new[] { new Vector3(-86, -16, -26), new Vector3(142, -16, -71),
                new Vector3(19, -16, 153), new Vector3(196, -16, 171), new Vector3(161, -16, 111),
                new Vector3(143, -16, 109) })
            {
                Place(underground, Pedestal, point, 15, .82f, Flame.Violet);
                Place(underground, Lantern, point + new Vector3(-1.5f, 0, 1.2f), -17, .72f);
            }

            Require(counts.Values.Sum() == 49 && result.Fires.Count == 20,
                "La composición de base debe conservar sus 49 piezas y sus 20 llamas.");
            string urn = SunProps + "Prop_ClayPot_E";
            string planter = SunProps + "Prop_FlowerPot_A";
            string bench = SunProps + "Prop_StoneBench_B";
            string fragments = Ruins + "Column_Broken_02";
            string wall = Ruins + "Broken_Wall_05";
            string ivy = Temple + "Flora/Ivy_02";
            string bush = Temple + "Flora/Bush_02";
            string altar = Temple + "Props/Furniture/Stone_Altar_02";
            string statue = "Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/SM_LadyStatue_Low";
            string crystal = RiftCrystal + "CrystalGround";
            string road = Temple + "Road and Plaza/Road_End_01";
            foreach (float x in new[] { -18f, 54f })
            {
                Display(plaza, new Vector3(x, 16, -20.5f), 0, (statue, .44f), (urn, .68f), (planter, .62f));
                Display(plaza, new Vector3(x, 16, -27.2f), 180, (bench, .49f), (ivy, .43f), (Lantern, .57f));
            }
            foreach (var table in tables)
            {
                var top = StaticBounds(table.gameObject);
                float side = top.center.x < -108 ? -1 : 1;
                var readingDesk = Place(archives, SunProps + "Prop_Table_A",
                    new Vector3(top.center.x + side * 3.1f, 20, top.center.z + 2.1f), 0, .82f, expansion:true);
                if (!readingDesk) continue;
                var surface = StaticBounds(readingDesk);
                Place(archives, SunProps + "Prop_BookStash_D", Surface(surface, -.20f, -.18f), 13, .65f,
                    onFurniture:true, expansion:true);
                Place(archives, SunProps + "Prop_Book_E", Surface(surface, .20f, -.17f), -31, .73f,
                    onFurniture:true, expansion:true);
                Place(archives, SunProps + "Prop_Candles_A", Surface(surface, .18f, .19f), 12, .66f,
                    onFurniture:true, expansion:true);
            }

            Display(portico, new Vector3(-151, 0, -135), 20, (statue, .56f), (bench, .59f), (planter, .76f));
            Display(portico, new Vector3(-102, 0, -134), 160, (fragments, .45f), (urn, .83f), (ivy, .62f));
            Display(portico, new Vector3(-146, 0, -151), 55, (wall, .35f), (urn, .62f), (Lantern, .65f));
            var gardens = Group("Jardines Dorados");
            Display(gardens, new Vector3(-126, 4, -26), 37, (bench, .65f), (planter, .85f), (bush, .65f), (ivy, .62f));
            Display(gardens, new Vector3(-80, 4, -31), 142, (bench, .69f), (planter, .80f), (bush, .54f), (urn, .64f));
            Display(gardens, new Vector3(-129, 4, 6), 68, (bush, .62f), (ivy, .54f), (planter, .77f));
            Display(gardens, new Vector3(-78, 4, 3), 169, (bench, .55f), (planter, .70f), (ivy, .56f));
            foreach (float x in new[] { -14f, 51f })
            {
                Display(cloister, new Vector3(x, 36, 158.7f), 0, (altar, .49f), (urn, .72f), (Lantern, .65f));
                Display(cloister, new Vector3(x, 36, 154), 180, (bench, .48f), (ivy, .57f), (planter, .59f));
            }
            Display(sanctuary, new Vector3(120, 8, -98), 18, (crystal, .42f), (fragments, .50f), (urn, .72f), (ivy, .44f));
            Display(sanctuary, new Vector3(180, 8, -98), -28, (altar, .60f), (urn, .72f), (Lantern, .65f), (wall, .33f));
            Display(sanctuary, new Vector3(126, 4, -83), 100, (statue, .51f), (urn, .70f), (fragments, .48f), (Lantern, .61f));
            Display(sanctuary, new Vector3(173, 8, -95), 42, (fragments, .42f), (urn, .61f), (Lantern, .58f), (ivy, .40f));
            Display(arena, new Vector3(151, 36, 114), 23, (fragments, .56f), (wall, .40f), (urn, .76f));
            Display(arena, new Vector3(209, 36, 114), 152, (statue, .56f), (crystal, .47f), (urn, .64f));
            Display(arena, new Vector3(216, 36, 188), 52, (wall, .38f), (fragments, .49f), (urn, .66f), (Lantern, .67f));

            // Lateral destinations are dressed as distinct pairs of ritual and ruin groups.
            Display(underground, new Vector3(-87, -16, -24), 14, (bench, .65f), (urn, .73f), (ivy, .46f), (fragments, .52f));
            Display(underground, new Vector3(144, -16, -70), 145, (wall, .42f), (fragments, .53f), (urn, .66f), (Lantern, .62f));
            Display(underground, new Vector3(18, -16, 155), 40, (statue, .56f), (urn, .76f), (altar, .52f), (Lantern, .66f));
            Display(underground, new Vector3(197, -16, 173), 126, (fragments, .58f), (wall, .44f), (road, .35f), (Lantern, .63f));
            Display(underground, new Vector3(94, -16, 132), 22, (bench, .58f), (altar, .56f), (urn, .72f), (Lantern, .61f));
            var inverted = Group("El Sol Invertido V3");
            Display(inverted, new Vector3(40, -38, -7), 52, (crystal, .43f), (fragments, .50f), (Lantern, .57f));
            Display(inverted, new Vector3(-3, -38, 25), 160, (wall, .32f), (urn, .67f), (Lantern, .65f));
            Display(access, new Vector3(156, 4, 34), 0, (urn, .62f), (fragments, .41f), (Lantern, .62f), (ivy, .39f));
            Display(access, new Vector3(160.5f, -16, 107), 28, (urn, .69f), (wall, .30f), (Lantern, .59f), (bench, .48f));

            AddView("01-Plaza-Warrior", new Vector3(-12, 17.9f, -23), new Vector3(-19, 17.1f, -28));
            AddView("02-Archivos-Warrior", StaticBounds(tables[0].gameObject).center + new Vector3(0, 1.1f, -3.1f),
                StaticBounds(tables[0].gameObject).center + Vector3.up * .35f);
            AddView("03-Portico-Warrior", new Vector3(-104, 1.9f, -139), new Vector3(-101, 1.1f, -134));
            AddView("04-Claustro-Warrior", new Vector3(45, 37.9f, 152), new Vector3(51, 37.3f, 155));
            AddView("05-Santuario-Warrior", new Vector3(125, 9.9f, -105), new Vector3(119, 9.6f, -100));
            AddView("06-Acceso-Warrior", new Vector3(156, -14.1f, 101), new Vector3(161, -14.3f, 104));
            AddView("07-Nave-Warrior", new Vector3(-81, -14.1f, -31), new Vector3(-86, -14.5f, -25));
            AddView("08-Jardines-Warrior", new Vector3(-118, 5.9f, -32), new Vector3(-126, 5.4f, -26));
            AddView("09-Arena-Warrior", new Vector3(156, 37.9f, 119), new Vector3(150, 37.4f, 114));
            AddView("10-Invertido-Warrior", new Vector3(32, -36.1f, -12), new Vector3(40, -36.8f, -7));
            foreach (var pair in fireSamples)
            {
                var p = pair.Value.position;
                AddView("Fuego-" + pair.Key, p + pair.Value.inward * 2.8f + Vector3.up * .38f, p + Vector3.up * .26f);
            }

            Require(counts.Values.Sum() > 110 && result.Fires.Count == 20 && fireSamples.Count == 3,
                "La pasada quedó incompleta: " + string.Join(", ", counts.Select(p => p.Key + "=" + p.Value)) + " / " +
                string.Join(", ", fireSamples.Keys) + "\n" + string.Join("\n", result.Audit.Where(line => line.Contains("omitido"))));
            var filters = root.GetComponentsInChildren<MeshFilter>();
            var renderers = root.GetComponentsInChildren<Renderer>();
            int triangles = filters.Sum(f => f.sharedMesh ? Enumerable.Range(0, f.sharedMesh.subMeshCount)
                .Sum(s => (int)f.sharedMesh.GetIndexCount(s) / 3) : 0);
            Require(root.GetComponentsInChildren<Collider>(true).Length == 0 &&
                root.GetComponentsInChildren<Light>(true).Length == 0 && occupied.All(b => !keepClear.Any(b.Intersects)),
                "Se encontró un collider, una luz o una interferencia con las reservas.");
            string summary = $"{counts.Values.Sum()} piezas colocadas; {result.Fires.Count} llamas; {renderers.Length} renderizadores; " +
                $"{triangles} triángulos de malla (incluye posibles LOD); {renderers.SelectMany(r => r.sharedMaterials).Distinct().Count()} materiales; " +
                $"{result.Fires.Count * 20} partículas máximas; 0 luces y 0 colliders nuevos.";
            result.Audit.Insert(0, summary);
            result.Audit.Insert(1, string.Join(" | ", counts.Select(p => $"{p.Key}: {p.Value}")));
            result.Audit.Insert(2, $"Reservas comprobadas: {keepClear.Count}, nueve conexiones y ruta meridional de 6 m; " +
                $"{solids.Length} envolventes sólidas de referencia. SHA-256: {Hash()}.");
            foreach (var line in result.Audit.Take(3)) Debug.Log("DECORACION " + line);
            return result;

            Transform Sector(string name) => Group(name);
            Transform Group(string name)
            {
                var child = new GameObject(name);
                child.transform.SetParent(root.transform, false);
                return child.transform;
            }

            void AddView(string name, Vector3 eye, Vector3 target) => result.Views.Add((name, eye, target));
            Vector3 Surface(Bounds b, float x, float z) => new Vector3(b.center.x + x * b.size.x, b.max.y + .01f, b.center.z + z * b.size.z);

            void Display(Transform sector, Vector3 anchor, float angle, params (string path, float scale)[] pieces)
            {
                var rotation = Quaternion.Euler(0, angle, 0);
                var offsets = new[] { Vector3.zero, new Vector3(-2, 0, .8f),
                    new Vector3(1.8f, 0, -.65f), new Vector3(2.3f, 0, 1.45f) };
                for (int i = 0; i < pieces.Length; i++)
                    Place(sector, pieces[i].path, anchor + rotation * offsets[i],
                        angle + i * 41f, pieces[i].scale, expansion:true);
            }

            void AddFireMaterial(Flame color, Color tint)
            {
                var material = new Material(shader) { name = "Fuego " + color };
                material.SetTexture("_MainTex", atlas);
                material.SetColor("_Tint", tint);
                resources.Add(material);
                fireMaterials.Add(color, material);
            }

            GameObject Place(Transform sector, string path, Vector3 anchor, float yaw, float scale,
                Flame? color = null, bool onFurniture = false, bool expansion = false)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path + ".prefab");
                Require(prefab, "Falta el prefab " + path);
                var item = Object.Instantiate(prefab, sector);
                item.name = prefab.name;
                item.transform.localRotation = Quaternion.Euler(0, yaw, 0);
                foreach (var behaviour in item.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
                foreach (var collider in item.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                foreach (var system in item.GetComponentsInChildren<ParticleSystem>(true))
                {
                    system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var renderer = system.GetComponent<ParticleSystemRenderer>();
                    if (renderer) Object.DestroyImmediate(renderer);
                    Object.DestroyImmediate(system);
                }
                foreach (var light in item.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(light);
                foreach (var animation in item.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
                foreach (var renderer in item.GetComponentsInChildren<MeshRenderer>())
                {
                    Require(renderer.sharedMaterials.All(m => m && m.shader && m.shader.name != "Hidden/InternalErrorShader"),
                        "Falta material de malla en " + path);
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                item.transform.localScale *= scale;
                var bounds = StaticBounds(item);
                var offsets = onFurniture ? new[] { Vector3.zero } : new[] { Vector3.zero,
                    new Vector3(.9f,0,0), new Vector3(-.9f,0,0), new Vector3(0,0,.9f), new Vector3(0,0,-.9f),
                    new Vector3(1.6f,0,1.2f), new Vector3(-1.6f,0,1.2f), new Vector3(1.6f,0,-1.2f),
                    new Vector3(-1.6f,0,-1.2f), new Vector3(2.6f,0,0), new Vector3(-2.6f,0,0),
                    new Vector3(0,0,2.6f), new Vector3(0,0,-2.6f) };
                foreach (var offset in offsets)
                {
                    var point = anchor + offset;
                    if (!onFurniture)
                    {
                        if (!physics.Raycast(point + Vector3.up * .57f, Vector3.down, out var hit, 1.10f, ~0,
                            QueryTriggerInteraction.Ignore) || hit.normal.y < .85f) continue;
                        point.y = hit.point.y + .012f;
                    }
                    var body = new Bounds(point + Vector3.up * (bounds.size.y * .5f), bounds.size);
                    var envelope = body;
                    if (color.HasValue)
                    {
                        envelope.Encapsulate(new Bounds(new Vector3(point.x, body.max.y + .35f, point.z), new Vector3(.82f, .9f, .82f)));
                    }
                    if (keepClear.Any(envelope.Intersects) || occupied.Any(envelope.Intersects)) continue;
                    if (expansion && addedReserves.Any(envelope.Intersects)) continue;
                    if (expansion && !onFurniture && !FootprintSupported(point, bounds.extents)) continue;
                    var test = envelope;
                    test.SetMinMax(test.min + Vector3.up * .06f, test.max + Vector3.up * .06f);
                    int touching = physics.OverlapBox(test.center, test.extents, overlap, Quaternion.identity, ~0,
                        QueryTriggerInteraction.Ignore);
                    if (touching == overlap.Length || Enumerable.Range(0, touching)
                        .Any(index => overlap[index].bounds.max.y > point.y + .065f)) continue;
                    item.transform.position += point - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    occupied.Add(envelope);
                    counts.TryGetValue(sector.name, out int count);
                    counts[sector.name] = count + 1;
                    result.Audit.Add($"{sector.name} · {item.name} · {point:F2}" + (color.HasValue ? " · " + color : ""));
                    if (color.HasValue)
                    {
                        var position = new Vector3(point.x, body.max.y - .06f, point.z);
                        var fireObject = Object.Instantiate(firePrefab, item.transform);
                        fireObject.name = "Llama " + color;
                        fireObject.transform.SetPositionAndRotation(position, Quaternion.identity);
                        fireObject.transform.localScale = Vector3.one;
                        var fire = fireObject.GetComponent<ParticleSystem>();
                        var main = fire.main;
                        main.scalingMode = ParticleSystemScalingMode.Local;
                        main.maxParticles = 20;
                        main.startSize = new ParticleSystem.MinMaxCurve(.40f, .63f);
                        main.startSpeed = new ParticleSystem.MinMaxCurve(.22f, .42f);
                        main.startLifetime = new ParticleSystem.MinMaxCurve(.62f, .89f);
                        main.startColor = Color.white;
                        main.cullingMode = ParticleSystemCullingMode.PauseAndCatchup;
                        var emission = fire.emission;
                        emission.rateOverTime = 15f;
                        var shape = fire.shape;
                        shape.shapeType = ParticleSystemShapeType.Cone;
                        shape.radius = .045f;
                        shape.angle = 8;
                        fireObject.transform.rotation = Quaternion.Euler(-90, 0, 0);
                        fire.GetComponent<ParticleSystemRenderer>().sharedMaterial = fireMaterials[color.Value];
                        fire.useAutoRandomSeed = false;
                        fire.randomSeed = (uint)(120 + result.Fires.Count * 91);
                        fire.Simulate(1.36f, false, true, true);
                        result.Fires.Add(fire);
                        var toward = new Vector3(18 - point.x, 0, 12 - point.z).normalized;
                        if (!fireSamples.ContainsKey(color.Value)) fireSamples.Add(color.Value, (position, toward));
                    }
                    return item;
                }
                Object.DestroyImmediate(item);
                result.Audit.Add($"{sector.name} · omitido {path} en {anchor:F2}: apoyo o reserva insuficiente.");
                return null;
            }

            bool FootprintSupported(Vector3 center, Vector3 extents)
            {
                foreach (float x in new[] { -.55f, .55f })
                foreach (float z in new[] { -.55f, .55f })
                {
                    var sample = center + new Vector3(extents.x * x, .21f, extents.z * z);
                    if (!physics.Raycast(sample, Vector3.down, out var hit, .45f, ~0,
                        QueryTriggerInteraction.Ignore) || hit.normal.y < .8f) return false;
                }
                return true;
            }

            Bounds StaticBounds(GameObject item)
            {
                var renderers = item.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled).ToArray();
                Require(renderers.Length > 0, "Falta malla decorativa: " + item.name);
                return renderers.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            }
        }
    }
}
