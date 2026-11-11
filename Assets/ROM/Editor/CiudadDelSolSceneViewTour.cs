using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    static class CiudadDelSolSceneViewTour
    {
        const string Menu = "ROM/Dungeon Ciudad del Sol/Recorrido visual sin Play/";
        const string ScenePath = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const float EyeHeight = 1.90f;
        const float UndergroundFloor = -16f;

        [MenuItem(Menu + "1. Ir a Nave de Jardines Dorados", false, 10)]
        static void Gardens() => Interior(-81, UndergroundFloor, -35, Vector3.forward);

        [MenuItem(Menu + "2. Ir a Salon bajo Santuario Profano", false, 11)]
        static void Sanctuary() => Interior(147, UndergroundFloor, -82, Vector3.forward);

        [MenuItem(Menu + "3. Ir a Galeria bajo el Claustro", false, 12)]
        static void Cloister() => Interior(24, UndergroundFloor, 145, Vector3.forward);

        [MenuItem(Menu + "4. Ir a Galeria bajo El Sol Corrompido", false, 13)]
        static void CorruptedSun() => Interior(189, UndergroundFloor, 157, Vector3.forward);

        [MenuItem(Menu + "5. Ir a Plaza del Eclipse", false, 14)]
        static void EclipseSquare() => Interior(18, 12, -28, Vector3.forward);

        [MenuItem(Menu + "6. Ir a El Sol Invertido V3", false, 15)]
        static void InvertedSun() => Interior(18, -38, -5, Vector3.forward);

        [MenuItem(Menu + "7. Volver a vista panoramica de Ciudad del Sol", false, 16)]
        static void Panorama() => Navigate(view => view.LookAt(
            new Vector3(39, 0, 5), Quaternion.LookRotation(new Vector3(.36f, -.80f, .48f)),
            300f, true, true));

        static void Interior(float x, float floor, float z, Vector3 forward) =>
            Navigate(view => view.LookAt(
                new Vector3(x, floor + EyeHeight, z), Quaternion.LookRotation(forward),
                5f, false, true));

        static void Navigate(Action<SceneView> frame)
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded ||
                StageUtility.GetCurrentStage() != StageUtility.GetMainStage() ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Recorrido visual de Ciudad del Sol",
                    "Abrí Assets/Scenes/Dungeon_Isla_del_Sol.unity en el escenario principal de Unity, fuera de Play Mode y de cualquier previsualización.",
                    "Aceptar");
                return;
            }

            var view = SceneView.lastActiveSceneView
                ?? SceneView.sceneViews.OfType<SceneView>().FirstOrDefault()
                ?? EditorWindow.GetWindow<SceneView>();
            frame(view);
            view.Focus();
            view.Repaint();
        }
    }
}
