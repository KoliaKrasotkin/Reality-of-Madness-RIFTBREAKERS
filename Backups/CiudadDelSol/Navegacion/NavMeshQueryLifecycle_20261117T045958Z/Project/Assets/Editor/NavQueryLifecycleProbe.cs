using System;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public static class NavQueryLifecycleProbe
{
    const string ErrorText = "NavMeshQuery has already been cleaned up but not deregistered";
    const string ResumeKey = "NavQueryLifecycleProbe.resume";
    static int errors;
    static string Mode => Environment.GetCommandLineArgs().ContainsValue("-probeTemp") ? "Temp" : "Persistent";
    static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", Mode + ".result.txt"));

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Isolated batch project only.");
        Application.logMessageReceived += Observe;
        try
        {
            QueryAndUnload(Mode == "Temp" ? Allocator.Temp : Allocator.Persistent, "before reload", () =>
            {
                if (Mode == "Temp") { Finish("comparison completed; error expected for Temp"); return; }
                if (errors != 0) { Finish("Persistent failed before reload"); return; }
                SessionState.SetBool(ResumeKey, true);
                File.WriteAllText(Output, "Persistent before reload: errors=" + errors + "\nReload requested\n");
                EditorUtility.RequestScriptReload();
            });
        }
        catch (Exception exception) { File.WriteAllText(Output, exception.ToString()); Debug.LogException(exception); EditorApplication.Exit(2); }
    }

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (!SessionState.GetBool(ResumeKey, false)) return;
        SessionState.SetBool(ResumeKey, false);
        EditorApplication.delayCall += () =>
        {
            Application.logMessageReceived += Observe;
            try { QueryAndUnload(Allocator.Persistent, "after reload", () => Finish("assembly reload completed")); }
            catch (Exception exception) { File.AppendAllText(Output, exception.ToString()); Debug.LogException(exception); EditorApplication.Exit(2); }
        };
    }

    static void QueryAndUnload(Allocator allocator, string phase, Action completed)
    {
        var data = AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/ExistingNavMesh.asset");
        if (!data) throw new InvalidOperationException("Existing NavMeshData did not load.");
        var instance = NavMesh.AddNavMeshData(data);
        if (!instance.valid) throw new InvalidOperationException("Existing NavMeshData did not register.");
        Debug.Log("PROBE_BEGIN " + allocator + " " + phase);
        bool valid;
        using (var query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), allocator, 256))
        {
            var location = query.MapLocation(new Vector3(-106.6f, .05f, -141.2f), Vector3.one, 0);
            valid = query.IsValid(location.polygon);
            if (!valid) throw new InvalidOperationException("Probe could not locate native ground.");
        }
        Debug.Log("PROBE_DISPOSE_RETURNED " + allocator + " " + phase);
        // Equivalent native removal invoked by NavMeshSurface.OnDisable, without any
        // surface, scene edit, bake, or modification of the source asset.
        int frames = 0;
        void UnloadAfterFrames()
        {
            if (++frames < 3) return;
            EditorApplication.update -= UnloadAfterFrames;
            try
            {
                instance.Remove();
                Debug.Log("PROBE_UNLOAD_RETURNED " + allocator + " " + phase + " errors=" + errors + " registered=" + instance.valid);
                if (instance.valid) throw new InvalidOperationException("Native data instance remained registered.");
                completed();
            }
            catch (Exception exception) { File.AppendAllText(Output, exception.ToString()); Debug.LogException(exception); EditorApplication.Exit(2); }
        }
        EditorApplication.update += UnloadAfterFrames;
    }

    static void Observe(string text, string stack, LogType type)
    {
        if (text.Contains(ErrorText)) errors++;
    }

    static void Finish(string phase)
    {
        File.AppendAllText(Output, Mode + ": " + phase + "; nativeErrors=" + errors + "\n");
        Debug.Log("PROBE_COMPLETE " + Mode + " nativeErrors=" + errors + " " + phase);
        Application.logMessageReceived -= Observe;
        EditorApplication.Exit(Mode == "Temp" ? (errors > 0 ? 0 : 3) : (errors == 0 ? 0 : 1));
    }

    static bool ContainsValue(this string[] args, string value) => Array.IndexOf(args, value) >= 0;
}
