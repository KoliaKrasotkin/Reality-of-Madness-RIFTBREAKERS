using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using MultiFPS.UI;
using ROM.UI.Sprint01;

namespace ROM.EditorTools
{
    [InitializeOnLoad]
    public static class RiftExpansionPlayProbe
    {
        const string Key = "ROM.RallyPiratePlayProbe";
        static int step, index;
        static double began, next;
        static Vector3 expected;
        static readonly Vector3[] Points = {
            new Vector3(-265, 2.2f, -80), new Vector3(-525, 2.2f, 383),
            new Vector3(164, 2.9f, 417), new Vector3(120, 6.22f, 450), new Vector3(139, 8.2f, -335)
        };
        static readonly string[] Names = { "west-interior", "eclipse-interior", "harbor-dock", "beach-shore", "rally-storefront" };
        static readonly System.Collections.Generic.List<string> Report = new System.Collections.Generic.List<string>();
        static RiftExpansionPlayProbe() { EditorApplication.playModeStateChanged += Changed; }
        public static void Run() { SessionState.SetBool(Key, true); EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu); EditorApplication.isPlaying = true; }
        static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) { step = index = 0; began = EditorApplication.timeSinceStartup; next = began + 3; Report.Clear(); EditorApplication.update += Tick; }
            if (state == PlayModeStateChange.EnteredEditMode) { SessionState.SetBool(Key, false); EditorApplication.Exit(SessionState.GetInt(Key + "Result", 1)); }
        }
        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - began > 150) throw new Exception("Expansion probe timeout " + step);
                if (EditorApplication.timeSinceStartup < next) return;
                if (step == 0)
                {
                    var hub = UnityEngine.Object.FindFirstObjectByType<RiftHub>(); if (!hub || !hub.Stage || !hub.Stage.Ready) return;
                    hub.Creator.BotsToggle.isOn = false; hub.Creator.HostGameButton.onClick.Invoke(); step = 1; next = EditorApplication.timeSinceStartup + 18; return;
                }
                var actor = ClientFrontend.OwnedCharacterInstance; if (!actor) return;
                if (step == 1)
                {
                    actor.Health.ServerSetVitals(10000, 10000);
                    if (!NavMesh.SamplePosition(Points[index], out var hit, 3, NavMesh.AllAreas)) throw new Exception("No navmesh at " + Names[index]);
                    expected = hit.position;
                    actor.ServerTeleport(expected + Vector3.up * .15f, index == 2 ? 90 : index == 4 ? 90 : 0);
                    step = 2; next = EditorApplication.timeSinceStartup + 2.5; return;
                }
                if (actor.Health.CurrentHealth <= 0 || Vector3.Distance(actor.transform.position, expected) > 3)
                    throw new Exception("Character fell or left ground at " + Names[index] + ": " + actor.transform.position);
                if (!Physics.Raycast(actor.transform.position + Vector3.up * .4f, Vector3.down, out var floor, 3, MultiFPS.GameManager.environmentLayer, QueryTriggerInteraction.Ignore))
                    throw new Exception("No collider beneath player at " + Names[index]);
                Report.Add("PASS " + Names[index] + " character=" + actor.transform.position + " floor=" + floor.collider.name + " thirdPerson=" + !actor.FPP);
                typeof(GameUIProbe).GetMethod("CaptureGame", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { "Documentation/rally-pirate-expansion/game-" + Names[index] + ".png" });
                index++;
                if (index == Points.Length) Finish(true); else { step = 1; next = EditorApplication.timeSinceStartup + .3; }
            }
            catch (Exception e) { Debug.LogException(e); Report.Add(e.ToString()); Finish(false); }
        }
        static void Finish(bool success)
        {
            File.WriteAllLines("Documentation/rally-pirate-expansion/play-validation.txt", Report);
            SessionState.SetInt(Key + "Result", success ? 0 : 1); EditorApplication.update -= Tick; EditorApplication.isPlaying = false;
        }
    }
}
