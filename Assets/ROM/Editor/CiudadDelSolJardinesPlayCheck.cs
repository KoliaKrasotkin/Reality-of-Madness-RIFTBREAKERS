using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirror;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    internal static partial class CiudadDelSolJardinesPlayCheck
    {
        const string Menu = "ROM/Dungeon Ciudad del Sol/Jardines Dorados/Prueba de carga/";
        const string City = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string MainMenu = "Assets/ROM/Scenes/MainMenu.unity";
        const string NavAsset = "Assets/ROM/CiudadDelSolJardinesRaid/Jardines_Encuentro01_NavMesh.asset";
        const string Encounter = "Raid · Jardines Dorados · Encuentro 01";
        const string LogPath = "Backups/CiudadDelSol/JardinesRaid/PruebaEditor/Resultado.txt";

        static readonly string[] Markers = { "01 Frontal", "02 Distancia", "03 Patrulla A", "04 Patrulla B" };
        static readonly List<string> LoadErrors = new List<string>();
        static AsyncOperation pending;
        static bool cityLoadVerified;
        static string cityLoadFailure;

        [MenuItem(Menu + "1. Revisar escena de inicio")]
        static void CheckStartScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Jardines Dorados", "Ejecutá esta opción en modo edición.", "Aceptar");
                return;
            }

            var configured = EditorSceneManager.playModeStartScene;
            string path = configured ? AssetDatabase.GetAssetPath(configured) : "";
            string message = "Escena de inicio al entrar en Play: " + (path.Length == 0 ? "<ninguna>" : path);
            if (path == City)
            {
                EditorSceneManager.playModeStartScene = null;
                message += "\nSe restauró el valor original registrado: <ninguna>.";
            }
            else if (path.Length > 0)
            {
                message += "\nSe conservó esa configuración.";
            }

            Report(message);
            EditorUtility.DisplayDialog("Jardines Dorados", message, "Aceptar");
        }

        [MenuItem(Menu + "2. Cargar y verificar en Play")]
        static void LoadInPlay()
        {
            if (pending != null)
            {
                Debug.LogWarning("Jardines Dorados: ya hay una carga de prueba en curso.");
                return;
            }

            try
            {
                Check(EditorApplication.isPlaying && !EditorApplication.isPaused &&
                    SceneManager.GetActiveScene().path == MainMenu && SceneManager.sceneCount == 1,
                    "Entrá manualmente en Play y esperá a que MainMenu sea la única escena cargada.");
                Check(!NetworkBusy(), "Hay una sesión o carga de Mirror activa. Se canceló el ensayo.");
                Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(City), "No se encontró el asset de Ciudad del Sol.");

                Report("Inicio de prueba en Play Mode. " + DescribeScenes());
                cityLoadVerified = false;
                cityLoadFailure = null;
                LoadErrors.Clear();
                Application.logMessageReceived += CaptureError;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                // Single replaces the menu only for this Play session and avoids overlapping scene objects.
                pending = EditorSceneManager.LoadSceneAsyncInPlayMode(City, new LoadSceneParameters(LoadSceneMode.Single));
                Check(pending != null, "Unity no inició la carga de Ciudad del Sol.");
                pending.completed += VerifyLoadedScene;
            }
            catch (Exception error)
            {
                StopListening();
                cityLoadFailure = error.Message;
                Report("ERROR: " + error.Message, true);
            }
        }

        static void VerifyLoadedScene(AsyncOperation operation)
        {
            var errors = LoadErrors.ToArray();
            StopListening();
            try
            {
                Report("Después de cargar. " + DescribeScenes());
                Check(errors.Length == 0, "Unity informó errores durante la carga:\n" + string.Join("\n", errors));
                var scene = SceneManager.GetSceneByPath(City);
                Check(EditorApplication.isPlaying && scene.IsValid() && scene.isLoaded &&
                    SceneManager.GetActiveScene() == scene && SceneManager.sceneCount == 1,
                    "Ciudad del Sol no quedó como única escena activa. Se detuvo la prueba.");
                Check(!NetworkBusy(), "Mirror cambió de estado durante la carga. Se detuvo la prueba.");
                var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
                Check(listeners.Count(listener => listener.isActiveAndEnabled) <= 1,
                    "Hay más de un AudioListener activo tras la carga. Se detuvo la prueba.");

                var root = scene.GetRootGameObjects().SingleOrDefault(item => item.name == Encounter);
                Check(root && root.activeInHierarchy, "No se encontró la raíz activa del encuentro en Ciudad del Sol.");
                var surfaces = scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<NavMeshSurface>(true))
                    .Where(item => item.name == "NavMesh_Exterior_CiudadDelSol").ToArray();
                Check(surfaces.Length == 1, "Carga/NavMesh: se esperaba una única superficie manual NavMesh_Exterior_CiudadDelSol.");
                var surface = surfaces[0];
                var dataPath = surface.navMeshData ? AssetDatabase.GetAssetPath(surface.navMeshData) : "";
                Check(surface.isActiveAndEnabled && surface.agentTypeID == 0 && !string.IsNullOrEmpty(dataPath) &&
                    AssetDatabase.LoadAssetAtPath<NavMeshData>(dataPath) == surface.navMeshData,
                    "Carga/NavMesh: la superficie manual debe estar activa, usar Humanoid 0 y vincular un NavMeshData persistente.");
                Report("Carga/NavMesh manual: " + dataPath);

                var filter = new NavMeshQueryFilter { agentTypeID = surface.agentTypeID, areaMask = NavMesh.AllAreas };
                foreach (string name in Markers)
                {
                    var marker = root.transform.Find(name);
                    Check(marker && marker.gameObject.activeInHierarchy, "Falta el marcador activo " + name + ".");
                    Check(NavMesh.SamplePosition(marker.position, out var hit, .05f, filter),
                        "El NavMesh no está disponible en " + name + ".");
                    Report($"{name}: {marker.position:F3}. NavMesh: {hit.position:F3}.");
                }

                cityLoadVerified = true;
                Report("OK: Ciudad del Sol cargada, NavMesh persistente disponible y 4/4 marcadores. No se creó ningún agente.");
            }
            catch (Exception error)
            {
                cityLoadFailure = error.Message;
                Report("ERROR: " + error.Message, true);
            }
        }

        static bool NetworkBusy() => NetworkServer.active || NetworkClient.active ||
            (NetworkManager.singleton && NetworkManager.singleton.mode != NetworkManagerMode.Offline) ||
            (NetworkManager.loadingSceneAsync != null && !NetworkManager.loadingSceneAsync.isDone);

        static string DescribeScenes() => $"Activa: {SceneManager.GetActiveScene().path}. Cargadas: " +
            string.Join(", ", Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Where(scene => scene.isLoaded).Select(scene => scene.path));

        static void CaptureError(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                LoadErrors.Add(message);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingPlayMode) return;
            StopListening();
            Report("Prueba interrumpida al salir de Play Mode.");
        }

        static void StopListening()
        {
            if (pending != null) pending.completed -= VerifyLoadedScene;
            pending = null;
            Application.logMessageReceived -= CaptureError;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        static void Report(string message, bool error = false)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                File.AppendAllText(LogPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
            }
            catch (IOException ex)
            {
                Debug.LogWarning("Jardines Dorados: no se pudo escribir el informe. " + ex.Message);
            }
            if (error) Debug.LogError(message);
            else Debug.Log(message);
        }
    }
}
