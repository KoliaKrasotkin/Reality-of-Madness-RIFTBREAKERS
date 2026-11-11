using System;
using System.Linq;
using Mirror;
using ROM.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.UI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    internal static partial class CiudadDelSolJardinesPlayCheck
    {
        const string AssistMenu = "ROM/Dungeon Ciudad del Sol/Jardines Dorados/Asistencia contextual/";
        static CiudadDelSolJardinesAssistChallenge assistTrial;

        [MenuItem(AssistMenu + "1. Preparar dos placas")]
        static void PrepareAssistTrial()
        {
            if (assistTrial)
            {
                Debug.LogWarning("Jardines Dorados. Ya hay un ensayo de placas. Cancelalo antes de preparar otro.");
                return;
            }
            try
            {
                var scene = SceneManager.GetActiveScene();
                var actor = ClientFrontend.OwnedCharacterInstance;
                Check(AssistHostActive() && scene.path == City && actor && actor.isOwned && actor.isServer &&
                    actor.connectionToClient == NetworkServer.localConnection && actor.GetComponent<WarriorAvatar>() &&
                    actor.Health && actor.Health.CurrentHealth > 0 && PatrolCourt.Contains(actor.transform.position),
                    "Entrá al patio del encuentro 01 con el Warrior vivo en el Host local de ensayo.");
                CreateAssistTrial(scene);
            }
            catch (Exception error)
            {
                StopAssistTrial();
                Debug.LogError("Jardines Dorados. " + error.Message);
            }
        }

        [MenuItem(AssistMenu + "2. Reiniciar intento")]
        static void ResetAssistTrial()
        {
            if (assistTrial && AssistHostActive()) assistTrial.ResetAttempt();
        }

        [MenuItem(AssistMenu + "3. Cancelar y retirar placas")]
        static void StopAssistTrial()
        {
            var trial = assistTrial;
            assistTrial = null;
            EditorApplication.playModeStateChanged -= AssistPlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= StopAssistTrial;
            if (trial) trial.CancelTrial();
        }

        static bool AssistHostActive() => EditorApplication.isPlaying && NetworkServer.active &&
            NetworkClient.active && NetworkServer.localConnection != null && !NetworkServer.listen;

        static void CreateAssistTrial(Scene scene)
        {
            var root = scene.GetRootGameObjects().Single(g => g.name == Encounter && g.activeInHierarchy);
            var surface = root.GetComponent<NavMeshSurface>();
            var data = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavAsset);
            Check(surface && surface.isActiveAndEnabled && data && surface.navMeshData == data && surface.agentTypeID == 0,
                "Falta el NavMesh aprobado del patio.");
            var filter = new NavMeshQueryFilter { agentTypeID = surface.agentTypeID, areaMask = NavMesh.AllAreas };
            Vector3 Project(string name)
            {
                var marker = root.transform.Find(name);
                Check(marker && marker.gameObject.activeInHierarchy, "Falta el marcador " + name);
                Check(NavMesh.SamplePosition(marker.position, out var hit, .05f, filter) && PatrolCourt.Contains(hit.position),
                    "El marcador no está sobre el NavMesh aprobado: " + name);
                return hit.position;
            }
            const string startId = "Placa 1 · Warrior";
            var posts = new[]
            {
                new CiudadDelSolJardinesAssistChallenge.Post(startId, Project(Markers[0]), allowsAssistant: false),
                new CiudadDelSolJardinesAssistChallenge.Post("Placa 2 · Asistencia", Project(Markers[1]), allowsAssistant: true)
            };
            var entry = Project(Markers[2]);
            var props = new GameObject("TEMP Jardines · Dos placas");
            props.hideFlags = HideFlags.DontSave;
            SceneManager.MoveGameObjectToScene(props, scene);
            assistTrial = props.AddComponent<CiudadDelSolJardinesAssistChallenge>();
            assistTrial.Initialize(posts, startId, entry, PatrolCourt);
            EditorApplication.playModeStateChanged -= AssistPlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= StopAssistTrial;
            EditorApplication.playModeStateChanged += AssistPlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload += StopAssistTrial;
        }

        static void AssistPlayChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) StopAssistTrial();
        }
    }
}
