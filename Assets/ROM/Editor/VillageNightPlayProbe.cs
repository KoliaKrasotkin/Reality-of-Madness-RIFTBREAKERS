using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ROM.UI.Sprint01;
using MultiFPS;
using MultiFPS.UI;

namespace ROM.EditorTools
{
    [InitializeOnLoad]
    public static class VillageNightPlayProbe
    {
        const string Key = "ROM.VillageNightPlayProbe";
        static int step;
        static double began, next;
        static VillageNightPlayProbe() { EditorApplication.playModeStateChanged += Changed; }
        public static void Run()
        {
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);
            EditorApplication.isPlaying = true;
        }
        static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                step = 0; began = EditorApplication.timeSinceStartup; next = began + 3;
                EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key, false);
                EditorApplication.Exit(SessionState.GetInt(Key + "Result", 1));
            }
        }
        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - began > 120) throw new Exception("Village play probe timeout, step " + step);
                if (EditorApplication.timeSinceStartup < next) return;
                if (step == 0)
                {
                    var hub = UnityEngine.Object.FindFirstObjectByType<RiftHub>();
                    if (!hub || !hub.Stage || !hub.Stage.Ready) return;
                    hub.Creator.BotsToggle.isOn = false;
                    hub.Creator.HostGameButton.onClick.Invoke();
                    step = 1; next = EditorApplication.timeSinceStartup + 18; return;
                }
                var actor = ClientFrontend.OwnedCharacterInstance;
                if (!actor) return;
                if (step == 1)
                {
                    var atmosphere = UnityEngine.Object.FindFirstObjectByType<ROM.Horde.RiftNightAtmosphere>();
                    if (!atmosphere || !RenderSettings.fog || RenderSettings.sun.intensity > 1 || actor.FPP)
                        throw new Exception("Night atmosphere / third person not applied");
                    Capture("gameplay-plaza");
                    var target = new Vector3(0, 0, 64);
                    if (!UnityEngine.AI.NavMesh.SamplePosition(target, out var hit, 15, UnityEngine.AI.NavMesh.AllAreas))
                        throw new Exception("Northern village path missing");
                    actor.ServerTeleport(hit.position + Vector3.up * .15f, 0);
                    step = 2; next = EditorApplication.timeSinceStartup + 3; return;
                }
                Capture("gameplay-road");
                Finish(true, "PASS: local host spawned in third person; night lighting and fog active; plaza and northern road captured with gameplay camera.");
            }
            catch (Exception e) { Debug.LogException(e); Finish(false, e.ToString()); }
        }
        static void Capture(string name)
        {
            typeof(GameUIProbe).GetMethod("CaptureGame", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { "Documentation/village-night/" + name + ".png" });
        }
        static void Finish(bool success, string message)
        {
            File.WriteAllText("Documentation/village-night/play-validation.txt", message);
            SessionState.SetInt(Key + "Result", success ? 0 : 1);
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
        }
    }
}
