using System;
using System.Linq;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.Gamemodes;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    internal static partial class CiudadDelSolJardinesPlayCheck
    {
        static readonly Vector3 BridgeSpawn = new Vector3(30f, -1.95f, -206f);
        const float BridgeYaw = 0f;
        static Action stopWarriorTrial;
        static bool warriorTrialReady;

        [MenuItem("ROM/Dungeon Ciudad del Sol/Prueba directa Warrior/Iniciar Host local de ensayo")]
        static void StartWarriorTrial()
        {
            try
            {
                var spawn = ValidateWarriorBridge();
                var manager = DNNetworkManager.Instance;
                Check(stopWarriorTrial == null && manager && NetworkManager.singleton == manager && manager.transport &&
                    Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1,
                    "Se necesita un único DNNetworkManager existente del arranque normal.");
                Check(!RoomManager._instance && !GameSync.Singleton && !GameManager.Gamemode &&
                    Object.FindObjectsByType<RoomManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0 &&
                    Object.FindObjectsByType<GameSync>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
                    "Ya existen servicios de sala o de sincronización.");
                var ui = ClientInterfaceManager.Instance;
                Check(ui && ui.GameplayCamera && ui.PlayerHudUI &&
                    Object.FindObjectsByType<ClientInterfaceManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1,
                    "Falta el ClientInterfaceManager original con cámara y HUD, o hay más de uno.");
                var player = manager.playerPrefab ? manager.playerPrefab.GetComponent<PlayerInstance>() : null;
                Check(player && player.WarriorCharacterPrefab == AssetDatabase.LoadAssetAtPath<GameObject>(WarriorPrefab),
                    "El prefab de jugador del manager no tiene vinculado el Warrior original.");
                Check(UserSettings.CharacterSkinID == 1 && player.CanEnterWarrior(new ClientData
                {
                    Warrior = PlayerPrefs.GetInt(AbilityController.Preference, 0) > 0,
                    CharacterSkinID = UserSettings.CharacterSkinID,
                    CharacterId = PlayerPrefs.GetString("ROM.Character.ActiveId", UserSettings.UserNickname)
                }), "Seleccioná y confirmá un Warrior válido en el menú antes de cargar Ciudad del Sol.");

                var city = SceneManager.GetActiveScene();
                var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                    .Where(camera => camera.isActiveAndEnabled).ToArray();
                var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)
                    .Where(listener => listener.isActiveAndEnabled).ToArray();
                Check(cameras.Length <= 1 && listeners.Length <= 1 && cameras.All(camera =>
                    camera.gameObject.scene == city && camera.CompareTag("MainCamera")) &&
                    listeners.All(listener => cameras.Length == 1 && listener.gameObject == cameras[0].gameObject) &&
                    !GameplayCamera._instance, "La cámara existente no coincide con la cámara de inspección esperada.");

                RunWarriorTrial(manager, spawn, cameras, listeners);
            }
            catch (Exception error)
            {
                Debug.LogError("Ciudad del Sol. No se pudo iniciar el ensayo. " + error.Message);
            }
        }

        static void RunWarriorTrial(DNNetworkManager manager, Vector3 spawn, Camera[] cameras, AudioListener[] listeners)
        {
            var oldRoom = RoomSetup.Properties;
            var oldOnline = manager.onlineScene;
            var oldOffline = manager.offlineScene;
            var oldAutoPlayer = manager.autoCreatePlayer;
            var oldListen = NetworkServer.listen;
            GameObject room = null;
            Action releaseTrialHud = null;
            bool hostRequested = false;
            bool playerRequested = false;
            bool finished = false;
            warriorTrialReady = false;
            double deadline = EditorApplication.timeSinceStartup + 30;

            void Finish()
            {
                if (finished) return;
                finished = true;
                warriorTrialReady = false;
                EditorApplication.update -= WaitForWarrior;
                EditorApplication.playModeStateChanged -= PlayChanged;
                stopWarriorTrial = null;
                try
                {
                    if (hostRequested && manager && (NetworkServer.active || NetworkClient.active))
                        manager.StopHost();
                }
                finally
                {
                    releaseTrialHud?.Invoke();
                    releaseTrialHud = null;
                    if (room) Object.Destroy(room);
                    foreach (var camera in cameras) if (camera) camera.enabled = true;
                    foreach (var listener in listeners) if (listener) listener.enabled = true;
                    RoomSetup.Properties = oldRoom;
                    NetworkServer.listen = oldListen;
                    if (manager)
                    {
                        manager.onlineScene = oldOnline;
                        manager.offlineScene = oldOffline;
                        manager.autoCreatePlayer = oldAutoPlayer;
                    }
                }
            }

            void PlayChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Finish();
            }

            void WaitForWarrior()
            {
                try
                {
                    Check(NetworkServer.active && SceneManager.GetActiveScene().path == City,
                        "El Host se detuvo o cambió la escena activa.");
                    Check(EditorApplication.timeSinceStartup < deadline, playerRequested
                        ? "El flujo original no entregó un Warrior controlable en 30 segundos."
                        : "La sala Raid o el cliente local no quedaron listos en 30 segundos.");
                    if (!playerRequested && GameManager.Gamemode is Raid && GameManager.Gamemode.isServer &&
                        NetworkClient.ready && NetworkServer.localConnection?.isReady == true)
                    {
                        Check(NetworkClient.AddPlayer(), "Mirror rechazó la solicitud original de PlayerInstance.");
                        playerRequested = true;
                    }

                    var actor = ClientFrontend.OwnedCharacterInstance;
                    if (!actor) return;
                    Check(playerRequested && actor.isOwned && actor.isServer &&
                        actor.connectionToClient == NetworkServer.localConnection && actor.GetComponent<WarriorAvatar>() &&
                        UserSettings.CharacterSkinID == 1, "El personaje recibido no coincide con el Warrior local esperado.");
                    actor.ServerTeleport(spawn, BridgeYaw);
                    releaseTrialHud = PrepareCityRaidHud();
                    warriorTrialReady = true;
                    EditorApplication.update -= WaitForWarrior;
                    Debug.Log($"Ciudad del Sol. Warrior local controlable en {spawn:F3}, giro Y={BridgeYaw}°. Salí de Play para cerrar el ensayo.");
                }
                catch (Exception error)
                {
                    Finish();
                    Debug.LogError("Ciudad del Sol. Ensayo interrumpido. " + error.Message + " Salí de Play antes de repetir.");
                }
            }

            stopWarriorTrial = Finish;
            EditorApplication.playModeStateChanged += PlayChanged;
            try
            {
                foreach (var camera in cameras) camera.enabled = false;
                foreach (var listener in listeners) listener.enabled = false;
                RoomSetup.Properties = new RoomProperties
                {
                    P_Gamemode = Gamemodes.Raid, P_MaxPlayers = 1, P_Map = City, P_GameDuration = 9999,
                    P_RespawnCooldown = 6f
                };
                manager.onlineScene = "";
                manager.offlineScene = "";
                manager.autoCreatePlayer = false;
                NetworkServer.listen = false;
                hostRequested = true;
                manager.StartHost();
                Check(NetworkServer.active && NetworkServer.localConnection != null, "Mirror no abrió un Host local.");

                // This identity is spawned explicitly. The local client shares the server object.
                room = new GameObject("TEMP Ciudad del Sol Raid (solo Play)");
                room.SetActive(false);
                room.AddComponent<NetworkIdentity>();
                room.AddComponent<RoomManager>();
                var raid = room.AddComponent<Raid>();
                var marker = new GameObject("Puente Pórtico Solar");
                marker.transform.SetParent(room.transform, false);
                marker.transform.SetPositionAndRotation(spawn, Quaternion.Euler(0, BridgeYaw, 0));
                var serialized = new SerializedObject(raid);
                var point = serialized.FindProperty("playerSpawn");
                Check(point != null, "Raid ya no expone la referencia serializada playerSpawn.");
                point.objectReferenceValue = marker.AddComponent<PlayerSpawn>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                room.SetActive(true);
                NetworkServer.Spawn(room);
                EditorApplication.update += WaitForWarrior;
            }
            catch
            {
                Finish();
                throw;
            }
        }
    }
}
