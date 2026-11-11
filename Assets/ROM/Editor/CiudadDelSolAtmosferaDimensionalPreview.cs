using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        const string AtmosphereMenu = IslandMenu + "Atmósfera dimensional/";
        const string AtmosphereOutput = "Backups/CiudadDelSol/AtmosferaDimensional";
        const string AtmosphereSourceHash = "8BBE2EA14C86A94633A6521959AEFAED27EC330962C4A4156542DB932D2D7536";
        const int PreviewVolumeLayer = 31;

        [MenuItem(AtmosphereMenu + "1. Abrir propuesta temporal")]
        static void OpenAtmosphere()
        {
            var source = SceneManager.GetActiveScene();
            Require(source.path == Original && !source.isDirty && Hash() == AtmosphereSourceHash &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                StageUtility.GetCurrentStage() == StageUtility.GetMainStage(), "Abrí la escena integrada limpia en modo edición.");
            var stage = ScriptableObject.CreateInstance<RiftAtmosphereStage>();
            stage.Source = source;
            StageUtility.GoToStage(stage, true);
        }

        [MenuItem(AtmosphereMenu + "2. Siguiente vista")]
        static void NextAtmosphereView()
        {
            if (StageUtility.GetCurrentStage() is RiftAtmosphereStage stage) stage.Frame();
        }

        [MenuItem(AtmosphereMenu + "3. Alternar antes y después")]
        static void ToggleAtmosphere()
        {
            if (StageUtility.GetCurrentStage() is RiftAtmosphereStage stage) stage.Toggle();
        }

        [MenuItem(AtmosphereMenu + "4. Cerrar sin guardar")]
        static void CloseAtmosphere()
        {
            if (StageUtility.GetCurrentStage() is RiftAtmosphereStage) StageUtility.GoToMainStage();
        }

        [MenuItem(AtmosphereMenu + "5. Validar y capturar comparativas")]
        public static void CaptureAtmosphereInBatch()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && Hash() == AtmosphereSourceHash,
                "La escena no coincide con la integración aprobada o está en Play.");
            Require(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "Se requiere un dispositivo gráfico para las capturas.");
            var scene = EditorSceneManager.OpenPreviewScene(Original);
            var resources = new List<Object>();
            try
            {
                var atmosphere = BuildAtmosphere(scene, resources);
                CaptureAtmosphereViews(scene, atmosphere);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
            Require(Hash() == AtmosphereSourceHash, "Cambió el archivo definitivo.");
            Debug.Log("ATMOSFERA_PREVIEW_OK");
        }

        sealed class RiftAtmosphereStage : PreviewSceneStage
        {
            public Scene Source;
            public bool SeamMode;
            public bool DecorationMode;
            AtmosphereLook look;
            GameObject seam;
            PlayableDecor decor;
            double lastParticleTick;
            readonly List<Object> resources = new List<Object>();
            int next;
            bool enabledLook = true;
            protected override GUIContent CreateHeaderContent() => new GUIContent(DecorationMode
                ? "Ciudad del Sol · decoración jugable" : SeamMode
                ? "Ciudad del Sol · unión de roca y murallas" : "Ciudad del Sol · atmósfera dimensional");
            protected override bool OnOpenStage()
            {
                if (!base.OnOpenStage()) return false;
                var context = new GameObject("Ciudad integrada · copia temporal");
                SceneManager.MoveGameObjectToScene(context, scene);
                context.SetActive(false);
                foreach (var root in Source.GetRootGameObjects().Where(r =>
                    ExteriorContext(r) || r.name == ExteriorMount || r.GetComponent<Light>() ||
                    DecorationMode && (r.name == FinalLookRoot || r.name == FinalSeamRoot)))
                {
                    var copy = Object.Instantiate(root, context.transform);
                    copy.name = root.name;
                    foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
                        if (behaviour && !(DecorationMode && behaviour is Volume)) behaviour.enabled = false;
                }
                context.SetActive(true);
                if (DecorationMode)
                {
                    decor = BuildPlayableDecor(scene, resources);
                    lastParticleTick = EditorApplication.timeSinceStartup;
                    EditorApplication.update += TickDecoration;
                }
                else look = BuildAtmosphere(scene, resources);
                if (SeamMode) seam = BuildWallSeam(scene, resources, out _);
                EditorApplication.delayCall += () => { if (this && StageUtility.GetCurrentStage() == this) Frame(); };
                return true;
            }
            public void Frame()
            {
                var views = DecorationMode ? decor.Views.ToArray() : SeamMode ? WallSeamViews() : AtmosphereViews();
                var view = views[next++ % views.Length];
                var window = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
                window.LookAt(view.target, Quaternion.LookRotation(view.target - view.eye),
                    Vector3.Distance(view.eye, view.target) * .47f, false, true);
                window.ShowNotification(new GUIContent(view.name));
            }
            public void Toggle()
            {
                enabledLook = !enabledLook;
                if (DecorationMode) decor.Root.SetActive(enabledLook);
                else if (SeamMode) seam.SetActive(enabledLook);
                else look.Set(enabledLook);
                SceneView.RepaintAll();
            }
            protected override void OnCloseStage()
            {
                EditorApplication.update -= TickDecoration;
                base.OnCloseStage();
                foreach (var item in resources) if (item) Object.DestroyImmediate(item);
            }

            void TickDecoration()
            {
                double now = EditorApplication.timeSinceStartup;
                float delta = (float)(now - lastParticleTick);
                if (delta < .04f) return;
                lastParticleTick = now;
                if (decor == null || !decor.Root || !decor.Root.activeInHierarchy) return;
                foreach (var fire in decor.Fires) fire.Simulate(Mathf.Min(delta, .09f), false, false, false);
                SceneView.RepaintAll();
            }
        }

        sealed class AtmosphereLook
        {
            public GameObject Root;
            public Light[] OriginalSun;
            public readonly List<(Renderer renderer, Material[] before, Material[] after)> Materials =
                new List<(Renderer, Material[], Material[])>();
            public string Report;
            public void Set(bool enabled)
            {
                Root.SetActive(enabled);
                foreach (var light in OriginalSun) light.enabled = !enabled;
                foreach (var change in Materials) change.renderer.sharedMaterials = enabled ? change.after : change.before;
            }
        }

        static AtmosphereLook BuildAtmosphere(Scene scene, List<Object> resources)
        {
            Require(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset, "La propuesta requiere URP.");
            var roots = scene.GetRootGameObjects();
            var decor = roots.SelectMany(r => r.GetComponentsInChildren<Transform>())
                .Single(t => t.name == ExteriorMount);
            Require(decor.childCount == 70 && decor.GetComponentsInChildren<Renderer>().Length == 394 &&
                roots.SelectMany(r => r.GetComponentsInChildren<Transform>()).Count(t => t.name == IslandMount) == 1,
                "Falta la composición integrada.");
            var root = new GameObject("Ambiente de la Grieta · temporal");
            SceneManager.MoveGameObjectToScene(root, scene);
            var look = new AtmosphereLook { Root = root, OriginalSun = roots.SelectMany(r => r.GetComponentsInChildren<Light>())
                .Where(l => l.enabled && l.type == LightType.Directional).ToArray() };

            var panorama = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ROM/Rendering/Skyboxes/RiftVoid_Panorama.png");
            Require(panorama, "Falta la panorámica RiftVoid de ROM.");
            var sky = MaterialFor("ROM/Ciudad del Sol/Rift Horizon");
            sky.SetTexture("_Panorama", panorama);
            Box("Cielo RiftVoid", new Vector3(30, -50, 20), Vector3.one * 3050, sky);
            var clouds = MaterialFor("ROM/Ciudad del Sol/Lower Rift Clouds");
            clouds.SetColor("_CloudColor", new Color(.65f, .37f, .83f));
            var fog = Box("Banco dimensional bajo las rutas", new Vector3(36, -150, 14),
                new Vector3(1010, 196, 980), clouds);
            Require(fog.bounds.max.y < -45, "La niebla invade las cotas jugables.");

            MakeLight("Sol dorado", LightType.Directional, Vector3.zero, Quaternion.Euler(42, -30, 0),
                new Color(1f, .88f, .68f), 4.1f, 0);
            MakeLight("Relleno lavanda del horizonte", LightType.Directional, Vector3.zero, Quaternion.Euler(24, 145, 0),
                new Color(.83f, .75f, 1f), 1.65f, 0);
            MakeLight("Rebote violeta bajo la masa", LightType.Directional, Vector3.zero, Quaternion.Euler(-27, 44, 0),
                new Color(.70f, .46f, 1f), 1.75f, 0);
            MakeLight("Acento violeta occidental", LightType.Point, new Vector3(-205, -42, -101), Quaternion.identity,
                new Color(.57f, .21f, 1f), 84f, 78);
            MakeLight("Acento violeta sur", LightType.Point, new Vector3(91, -47, -201), Quaternion.identity,
                new Color(.58f, .20f, 1f), 105f, 92);
            MakeLight("Acento violeta noreste", LightType.Point, new Vector3(255, -51, 165), Quaternion.identity,
                new Color(.60f, .24f, 1f), 93f, 96);
            MakeLight("Reflejo solar · Nave de Jardines", LightType.Point, new Vector3(-81, -10, -21), Quaternion.identity,
                new Color(1f, .85f, .69f), 83f, 43);
            MakeLight("Reflejo de la Grieta · Sol Invertido", LightType.Point, new Vector3(18, -31, 4), Quaternion.identity,
                new Color(.83f, .63f, 1f), 80f, 48);

            var copies = new Dictionary<Material, Material>();
            int crystals = 0;
            foreach (var cluster in decor.Cast<Transform>())
            {
                var crystal = cluster.Find("CrystalGround") ?? cluster.Find("CrystalAerial");
                if (crystal) crystals++;
                var affected = crystal ? crystal.GetComponentsInChildren<Renderer>() :
                    cluster.name.StartsWith("Cascada ", StringComparison.Ordinal) ? cluster.GetComponentsInChildren<Renderer>() :
                    Array.Empty<Renderer>();
                foreach (var renderer in affected)
                {
                    var before = renderer.sharedMaterials;
                    var after = before.Select(m => crystal ? CrystalMaterial(m) : WaterMaterial(m)).ToArray();
                    if (!before.SequenceEqual(after)) look.Materials.Add((renderer, before, after));
                }
            }
            Require(crystals == 12 && look.Materials.Count > 20, "Faltan materiales de agua o cristales.");

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            resources.Add(profile);
            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(1.15f);
            bloom.intensity.Override(.31f);
            bloom.scatter.Override(.58f);
            var exposure = profile.Add<ColorAdjustments>();
            exposure.postExposure.Override(.42f);
            var volume = new GameObject("Resplandor de inspección");
            volume.transform.SetParent(root.transform, false);
            volume.layer = PreviewVolumeLayer;
            var effect = volume.AddComponent<Volume>();
            effect.isGlobal = true;
            effect.sharedProfile = profile;

            Require(root.GetComponentsInChildren<Collider>(true).Length == 0 &&
                root.GetComponentsInChildren<ParticleSystem>(true).Length == 0, "El ambiente debe ser puramente visual.");
            look.Report = $"2 renderizadores adicionales (cielo y banco de nubes); 3 direccionales y 5 luces locales sin sombras; " +
                $"1 Bloom y ajuste de exposición, 6 muestras de niebla por píxel cubierto, 0 emisores, 0 colliders; " +
                $"{crystals} formaciones, {copies.Count} variantes de material y {look.Materials.Count} renderizadores ajustados.";
            look.Set(true);
            Debug.Log("ATMOSFERA " + look.Report);
            return look;

            Material MaterialFor(string shaderName)
            {
                var shader = Shader.Find(shaderName);
                Require(shader && shader.isSupported, "Shader no compatible: " + shaderName);
                var material = new Material(shader);
                resources.Add(material);
                return material;
            }

            MeshRenderer Box(string name, Vector3 center, Vector3 scale, Material material)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = name;
                box.transform.SetParent(root.transform, false);
                box.transform.position = center;
                box.transform.localScale = scale;
                Object.DestroyImmediate(box.GetComponent<Collider>());
                var renderer = box.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                return renderer;
            }

            void MakeLight(string name, LightType type, Vector3 position, Quaternion rotation,
                Color color, float intensity, float range)
            {
                var lamp = new GameObject(name);
                lamp.transform.SetParent(root.transform, false);
                lamp.transform.SetPositionAndRotation(position, rotation);
                var light = lamp.AddComponent<Light>();
                light.type = type;
                light.color = color;
                light.intensity = intensity;
                light.shadows = LightShadows.None;
                if (type == LightType.Point) light.range = range;
            }

            Material CrystalMaterial(Material source)
            {
                if (!source || !source.HasProperty("_EmissionColor")) return source;
                var emission = source.GetColor("_EmissionColor");
                if (emission.maxColorComponent < .2f) return source;
                return Copy(source, m => m.SetColor("_EmissionColor", new Color(
                    emission.r * 1.22f, emission.g * .75f, emission.b * 1.18f, emission.a)));
            }

            Material WaterMaterial(Material source)
            {
                if (!source || source.shader.name != "ROM/Ciudad del Sol/Coastal Cascade Preview") return source;
                return Copy(source, m => m.SetColor("_BaseColor", new Color(.57f, .58f, .83f, .96f)));
            }

            Material Copy(Material source, Action<Material> configure)
            {
                if (copies.TryGetValue(source, out var material)) return material;
                material = new Material(source) { name = source.name + " · Grieta temporal" };
                configure(material);
                copies.Add(source, material);
                resources.Add(material);
                return material;
            }
        }

        static (string name, Vector3 eye, Vector3 target)[] AtmosphereViews() => new[]
        {
            ("01-Sur", new Vector3(525, 125, -470), new Vector3(28, -53, 12)),
            ("02-Oeste", new Vector3(-560, 99, -210), new Vector3(20, -59, 4)),
            ("03-Noreste", new Vector3(520, 132, 564), new Vector3(33, -47, 20)),
            ("04-Inferior", new Vector3(-485, -300, -460), new Vector3(27, -75, 14)),
            ("05-Plaza-Warrior", new Vector3(18, 13.9f, -28), new Vector3(18, 14.6f, 22)),
            ("06-Nave-Warrior", new Vector3(-81, -14.1f, -35), new Vector3(-81, -13.6f, -5)),
            ("07-Sol-Invertido-Warrior", new Vector3(18, -36.1f, -5), new Vector3(18, -33.5f, 24))
        };

        static void CaptureAtmosphereViews(Scene scene, AtmosphereLook look, GameObject seam = null,
            (string name, Vector3 eye, Vector3 target)[] views = null, string output = AtmosphereOutput,
            Action prepareVisible = null)
        {
            Directory.CreateDirectory(output);
            var cameraViews = views ?? AtmosphereViews();
            var cameraObject = new GameObject("Cámara de comparación · temporal");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = .08f;
            camera.farClipPlane = 3000;
            camera.allowHDR = true;
            var options = camera.GetUniversalAdditionalCameraData();
            options.requiresDepthTexture = true;
            options.volumeLayerMask = 1 << PreviewVolumeLayer;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGBHalf);
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                foreach (var view in cameraViews)
                {
                    camera.fieldOfView = view.name.Contains("Warrior") ? 62 : 51;
                    camera.transform.SetPositionAndRotation(view.eye, Quaternion.LookRotation(view.target - view.eye));
                    for (int state = 0; state <= 1; state++)
                    {
                        if (seam) seam.SetActive(state == 1);
                        else look.Set(state == 1);
                        if (state == 1) prepareVisible?.Invoke();
                        options.renderPostProcessing = seam || state == 1;
                        camera.Render();
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                        File.WriteAllBytes(Path.Combine(output, view.name +
                            (state == 1 ? "-despues.png" : "-antes.png")), pixels.EncodeToPNG());
                    }
                }
                if (!seam) File.WriteAllText(Path.Combine(output, "Validacion.txt"), look.Report + Environment.NewLine +
                    "Banco de niebla hasta Y=-52; vistas de Warrior a 1,90 m sobre el suelo; 14 capturas 1280 × 720." +
                    Environment.NewLine + "SHA-256 de origen: " + Hash() + Environment.NewLine);
                Debug.Log($"ATMOSFERA_CAPTURAS_OK {cameraViews.Length * 2}");
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
