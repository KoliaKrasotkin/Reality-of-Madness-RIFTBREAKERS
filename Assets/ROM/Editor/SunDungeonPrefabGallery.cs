using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    /// <summary>Manual, disposable gallery. Never saves scenes or source assets.</summary>
    public static class SunDungeonPrefabGallery
    {
        const string Eternal = "Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string Sun = "Assets/Standard Assets/Sun_Temple/Prefabs/";
        const string Egyptian = "Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/";
        const string Menu = "ROM/Dungeon Ciudad del Sol/Crear galeria temporal de prefabs";

        static readonly string[] Paths =
        {
            Eternal + "Road and Plaza/Plaza_Circle_01.prefab",
            Eternal + "Road and Plaza/Plaza_Hemicycle_01.prefab",
            Eternal + "Pits and Platforms/Platform_Base_01.prefab",
            Eternal + "Pits and Platforms/Platform_Part_01.prefab",
            Eternal + "Road and Plaza/Road_01.prefab",
            Eternal + "Railings and Decors/Railing_01.prefab",
            Eternal + "Staircases/Staircase_01.prefab",
            Eternal + "Walls/Wall_Big_01.prefab",
            Eternal + "Columns/Column_01.prefab",
            Eternal + "Bases/Thin/Entrance_Arch_01.prefab",
            Eternal + "Arch Alley/Arch_Alley_01.prefab",
            Sun + "Buildings_Prefabbed/Bld_TempleTower_A.prefab",
            Egyptian + "SM_MainGate.prefab",
            Egyptian + "SM_Altar.prefab",
            Egyptian + "SM_SunDisk2.prefab"
        };

        [MenuItem(Menu, false, 200)]
        public static void CreateGallery()
        {
            // Validate everything before creating or activating any scene.
            var assets = new List<GameObject>();
            foreach (string path in Paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab)
                {
                    EditorUtility.DisplayDialog("Galeria de prefabs", "No se encontro el prefab:\n" + path, "Cerrar");
                    return;
                }
                assets.Add(prefab);
            }

            Scene previous = SceneManager.GetActiveScene();
            Scene gallery = default;
            try
            {
                gallery = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                gallery.name = "TEMP_Galeria_Ciudad_del_Sol";
                SceneManager.SetActiveScene(gallery);
                var instances = new List<GameObject>();
                var bounds = new List<Bounds>();
                float width = 1, depth = 1, height = 1;
                foreach (var asset in assets)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, gallery);
                    // Retain the source root scale, rotation, materials and prefab connection.
                    instance.transform.position = Vector3.zero;
                    Bounds b = VisibleBounds(instance);
                    instances.Add(instance);
                    bounds.Add(b);
                    width = Mathf.Max(width, b.size.x);
                    depth = Mathf.Max(depth, b.size.z);
                    height = Mathf.Max(height, b.size.y);
                }

                float gap = Mathf.Max(5, Mathf.Max(width, depth) * 0.2f);
                float labelSize = Mathf.Max(0.5f, width / 32f);
                float pitchX = width + gap;
                float pitchZ = depth + gap + labelSize * 5;
                Bounds total = default;
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                for (int i = 0; i < instances.Count; i++)
                {
                    Vector3 center = new Vector3((i % 5) * pitchX, 0, (i / 5) * pitchZ);
                    Bounds b = bounds[i];
                    Vector3 offset = center - new Vector3(b.center.x, b.min.y, b.center.z);
                    instances[i].transform.position += offset;
                    b.center += offset;
                    if (i == 0) total = b;
                    else total.Encapsulate(b);

                    var labelObject = new GameObject("Etiqueta - " + assets[i].name);
                    SceneManager.MoveGameObjectToScene(labelObject, gallery);
                    labelObject.transform.position = new Vector3(center.x, labelSize, b.min.z - gap * 0.35f);
                    labelObject.transform.rotation = Quaternion.Euler(60, 0, 0);
                    var label = labelObject.AddComponent<TextMesh>();
                    label.text = assets[i].name;
                    label.font = font;
                    label.fontSize = 64;
                    label.characterSize = labelSize;
                    label.anchor = TextAnchor.MiddleCenter;
                    label.alignment = TextAlignment.Center;
                    label.color = Color.white;
                    labelObject.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                    total.Encapsulate(labelObject.GetComponent<Renderer>().bounds);
                }

                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.42f);
                RenderSettings.fog = false;
                AddLight(gallery, "Galeria - luz principal", new Vector3(50, -35, 0), 1.2f, LightShadows.Soft);
                AddLight(gallery, "Galeria - luz de relleno", new Vector3(35, 145, 0), 0.6f, LightShadows.None);

                var cameraObject = new GameObject("Galeria - camara general");
                SceneManager.MoveGameObjectToScene(cameraObject, gallery);
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.065f, 0.085f);
                camera.orthographic = true;
                camera.transform.rotation = Quaternion.Euler(60, 0, 0);
                // Fit all eight corners in camera space, including the height of towers.
                float halfX = 0, halfY = 0;
                Quaternion inverse = Quaternion.Inverse(camera.transform.rotation);
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 corner = Vector3.Scale(total.extents, new Vector3(x, y, z));
                            Vector3 local = inverse * corner;
                            halfX = Mathf.Max(halfX, Mathf.Abs(local.x));
                            halfY = Mathf.Max(halfY, Mathf.Abs(local.y));
                        }
                camera.orthographicSize = Mathf.Max(halfY, halfX / Mathf.Max(0.1f, camera.aspect)) * 1.15f;
                float distance = total.size.magnitude + height + 10;
                camera.transform.position = total.center - camera.transform.forward * distance;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = distance * 3;

                EditorSceneManager.MarkSceneDirty(gallery);
                Selection.activeGameObject = instances[0];
                if (SceneView.lastActiveSceneView)
                    SceneView.lastActiveSceneView.Frame(total, false);
                Debug.Log("Galeria temporal creada: 15 prefabs originales a escala original. " +
                          "Selecciona una pieza y pulsa F para inspeccionarla. " +
                          "Cierra TEMP_Galeria_Ciudad_del_Sol sin guardar al terminar.");
            }
            catch (Exception exception)
            {
                // Roll back only this newly created scene; never touch existing scenes.
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (gallery.IsValid() && gallery.isLoaded) EditorSceneManager.CloseScene(gallery, true);
                Debug.LogException(exception);
            }
        }

        [MenuItem(Menu, true)]
        static bool CanCreateGallery() => !EditorApplication.isPlayingOrWillChangePlaymode;

        static Bounds VisibleBounds(GameObject instance)
        {
            bool found = false;
            Bounds result = new Bounds(instance.transform.position, Vector3.one);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!found) { result = renderer.bounds; found = true; }
                else result.Encapsulate(renderer.bounds);
            }
            return result;
        }

        static void AddLight(Scene scene, string name, Vector3 angles, float intensity, LightShadows shadows)
        {
            var lightObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            lightObject.transform.rotation = Quaternion.Euler(angles);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = intensity;
            light.shadows = shadows;
        }
    }
}
