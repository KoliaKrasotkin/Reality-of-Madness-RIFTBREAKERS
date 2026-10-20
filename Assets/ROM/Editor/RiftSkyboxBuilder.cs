using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ROM.EditorTools
{
    public static class RiftSkyboxBuilder
    {
        public const string TexturePath = "Assets/ROM/Rendering/Skyboxes/RiftVoid_Panorama.png";
        public const string MaterialPath = "Assets/ROM/Rendering/Skyboxes/RiftVoid_Skybox.mat";

        [MenuItem("ROM/El Rift/Aplicar cielo de grieta dimensional")]
        public static void ApplyFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);
            ApplyToCurrentScene();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        public static void ApplyToCurrentScene()
        {
            AssetDatabase.ImportAsset(TexturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            if (!importer) throw new FileNotFoundException("Falta el panorama del Rift", TexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            var shader = Shader.Find("Skybox/Panoramic");
            if (!shader || !shader.isSupported) throw new InvalidOperationException("Skybox/Panoramic no disponible");
            var sky = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!sky)
            {
                sky = new Material(shader) { name = "El Rift - Vacio dimensional" };
                AssetDatabase.CreateAsset(sky, MaterialPath);
            }
            sky.shader = shader;
            sky.SetTexture("_MainTex", texture);
            sky.SetFloat("_Mapping", 1); // Latitude-longitude / equirectangular.
            sky.SetFloat("_ImageType", 0); // Full 360 degrees.
            sky.SetFloat("_Layout", 0); // Monoscopic.
            sky.SetFloat("_Exposure", 1.15f);
            sky.SetFloat("_Rotation", 25);
            sky.SetColor("_Tint", Color.gray);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.36f, .34f, .48f);
            RenderSettings.ambientEquatorColor = new Color(.25f, .24f, .31f);
            RenderSettings.ambientGroundColor = new Color(.09f, .085f, .12f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.16f, .12f, .23f);
            RenderSettings.fogDensity = .0009f;
            if (RenderSettings.sun)
            {
                RenderSettings.sun.color = new Color(.88f, .88f, 1);
                RenderSettings.sun.intensity = 1.6f;
                EditorUtility.SetDirty(RenderSettings.sun);
            }
            var night = UnityEngine.Object.FindFirstObjectByType<ROM.Horde.RiftNightAtmosphere>();
            if (night) night.Apply();
            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        public static void FromCommandLine()
        {
            try
            {
                ApplyFromMenuForBatch();
                Directory.CreateDirectory("Documentation/map-visual-review");
                MapVisualRepair.Capture("rift-sky-refugio", new Vector3(0, 5, -20), new Vector3(0, 35, 130));
                MapVisualRepair.Capture("rift-sky-sun-temple", new Vector3(500, 210, 650), new Vector3(820, 30, 370));
                MapVisualRepair.Capture("rift-sky-zenith", new Vector3(0, 100, 0), new Vector3(5, 110, 1));
                // Reopen the saved scene to verify the reference survives serialization.
                EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);
                if (AssetDatabase.GetAssetPath(RenderSettings.skybox) != MaterialPath ||
                    RenderSettings.skybox.GetTexture("_MainTex") == null)
                    throw new InvalidOperationException("El cielo no se guardó en la escena");
                Debug.Log("[RiftSky] PASS: panoramic texture, material and saved scene verified.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void ApplyFromMenuForBatch()
        {
            EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);
            ApplyToCurrentScene();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }
    }
}
