using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.Preloader;
using MultiFPS.UI;
using ROM.UI.Sprint01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    // Coordinates the existing trials. Nothing is placed or serialized in the saved dungeon.
    [InitializeOnLoad]
    internal static partial class CiudadDelSolJardinesPlayCheck
    {
        const string DevelopmentMenu = "ROM/Preparar Ciudad del Sol para pruebas";
        const string PreviousStartKey = "ROM.CityDevelopment.PreviousStartScene";
        const string GameCore = "Assets/MultiFPS/Levels/Prefabs/Core/MultiFPS_GameManager.prefab";
        const string ClientCore = "Assets/MultiFPS/Levels/Prefabs/Core/MultiFPS_ClientManager.prefab";
        static readonly List<GameObject> developmentObjects = new List<GameObject>();
        static bool developmentRunning, developmentOwnsHost, developmentEncounterReady;
        static double developmentDeadline;
        static Action restoreDevelopmentSettings;
        static bool DevelopmentEnabled => SessionState.GetBool(Preloader.CityDevelopmentEnabledKey, false);

        static CiudadDelSolJardinesPlayCheck()
        {
            Preloader.EditorCityDevelopmentBootstrap = BootstrapDevelopment;
            EditorApplication.playModeStateChanged += DevelopmentPlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload += CleanupDevelopment;
            EditorApplication.quitting += DisableDevelopment;
        }

        [MenuItem(DevelopmentMenu, false, -200)]
        static void ToggleDevelopment()
        {
            try
            {
                Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Cambiar el modo de pruebas fuera de Play.");
                if (DevelopmentEnabled) { DisableDevelopment(); return; }
                ReadSelectedDevelopmentWarrior();
                Check(AssetDatabase.LoadAssetAtPath<GameObject>(GameCore) && AssetDatabase.LoadAssetAtPath<GameObject>(ClientCore),
                    "Faltan los prefabs originales de servicios.");
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    Check(!SceneManager.GetSceneAt(i).isDirty, "Hay cambios de escena sin guardar. Guardalos o descartalos manualmente antes de preparar el ensayo.");
                var scene = SceneManager.GetSceneByPath(City);
                if (!scene.IsValid() || !scene.isLoaded || SceneManager.sceneCount != 1)
                    scene = EditorSceneManager.OpenScene(City, OpenSceneMode.Single);
                SceneManager.SetActiveScene(scene);
                SessionState.SetString(PreviousStartKey, EditorSceneManager.playModeStartScene ?
                    AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : "");
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(City);
                SessionState.SetBool(Preloader.CityDevelopmentEnabledKey, true);
                Debug.Log("CIUDAD_DESARROLLO HABILITADO. Pulsá Play: Host local, Warrior y población exterior, previa validación de cada encuentro. La misma opción desactiva el modo. No se guardó la escena.");
            }
            catch (Exception error) { Debug.LogError("CIUDAD_DESARROLLO Preparación cancelada: " + error.Message); }
        }

        [MenuItem(DevelopmentMenu, true)]
        static bool ValidateDevelopmentMenu()
        {
            UnityEditor.Menu.SetChecked(DevelopmentMenu, DevelopmentEnabled);
            return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
        }

        static void DisableDevelopment()
        {
            CleanupDevelopment();
            if (!DevelopmentEnabled) return;
            SessionState.SetBool(Preloader.CityDevelopmentEnabledKey, false);
            var previous = SessionState.GetString(PreviousStartKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            SessionState.EraseString(PreviousStartKey);
            Debug.Log("CIUDAD_DESARROLLO DESACTIVADO. Se restauró la escena de inicio anterior; Preloader conserva el arranque normal.");
        }

        static RiftPlayerCharacter ReadSelectedDevelopmentWarrior()
        {
            // Read only: constructing LocalCharacterRoster can create a default identity.
            string account = "local-player";
            string socialPath = Path.Combine(Application.persistentDataPath, "rom-local-social.json");
            if (File.Exists(socialPath))
            {
                var social = JsonUtility.FromJson<LocalSocialState>(File.ReadAllText(socialPath));
                Check(social != null && !string.IsNullOrEmpty(social.ActiveId), "El perfil local guardado no es válido.");
                account = social.ActiveId;
            }
            var roster = JsonUtility.FromJson<CharacterRosterData>(PlayerPrefs.GetString("ROM.CharacterRoster.v1." + account, ""));
            string id = PlayerPrefs.GetString("ROM.Character.ActiveId", "");
            var record = roster?.Characters?.SingleOrDefault(c => c != null && c.Id == id);
            Check(record != null && roster.ActiveId == id && Guid.TryParse(id, out _) && record.ClassKey == "warrior" &&
                record.Skin == 1 && record.Available && PlayerPrefs.GetInt("_playerPrefs_CharacterSkinID", -1) == record.Skin &&
                PlayerPrefs.GetInt(AbilityController.Preference, 0) > 0,
                "No existe un Warrior seleccionado y confirmado válido. Seleccionalo mediante Personajes en el flujo normal, salí de Play y prepará el modo otra vez. No se creó ni cambió ningún personaje.");
            return record;
        }

        // Called by the Editor-only Preloader branch, before sceneLoaded. This lets the original
        // ClientInterfaceManager receive the real dungeon load and create its own basic UI normally.
        static void BootstrapDevelopment()
        {
            try
            {
                Check(DevelopmentEnabled && SceneManager.GetActiveScene().path == City && !developmentRunning,
                    "Contexto de arranque automático inválido o duplicado.");
                Check(!NetworkBusy() && stopWarriorTrial == null && demonTrial == null,
                    "Ya existe una sesión o ensayo; no se creará ni detendrá otro.");
                developmentRunning = true;
                var record = ReadSelectedDevelopmentWarrior();
                var nickname = UserSettings.UserNickname; int skin = UserSettings.CharacterSkinID;
                var loadout = UserSettings.PlayerLodout; var skins = UserSettings.SelectedItemSkins;
                restoreDevelopmentSettings = () => { UserSettings.UserNickname = nickname; UserSettings.CharacterSkinID = skin;
                    UserSettings.PlayerLodout = loadout; UserSettings.SelectedItemSkins = skins; };
                UserSettings.UserNickname = record.DisplayName;
                UserSettings.CharacterSkinID = record.Skin;
                EnsureDevelopmentCore<DNNetworkManager>(GameCore);
                EnsureDevelopmentCore<ClientInterfaceManager>(ClientCore);
                var player = DNNetworkManager.Instance.playerPrefab.GetComponent<PlayerInstance>();
                Check(player && player.CanEnterWarrior(new ClientData { Warrior = true, CharacterSkinID = record.Skin, CharacterId = record.Id }),
                    "El PlayerInstance original no admite el Warrior guardado.");
                if (UserSettings.PlayerLodout == null)
                {
                    var slots = player.WarriorCharacterPrefab.GetComponent<CharacterItemManager>();
                    Check(slots, "Falta el CharacterItemManager del Warrior original.");
                    UserSettings.PlayerLodout = Enumerable.Repeat(-1, slots.Slots.Count).ToArray();
                }
                Debug.Log("CIUDAD_DESARROLLO Servicios originales preparados antes de sceneLoaded; sin MainMenu ni Host todavía.");
            }
            catch (Exception error) { AbortDevelopment(error); }
        }

        static void EnsureDevelopmentCore<T>(string path) where T : Component
        {
            var existing = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Check(existing.Length == 0, "Ya existe " + typeof(T).Name + "; se canceló para evitar apropiarse de servicios ajenos.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(prefab && prefab.GetComponent<T>(), "Prefab de servicio original inválido: " + path);
            var instance = Object.Instantiate(prefab);
            instance.name = "TEMP Ciudad del Sol · " + prefab.name;
            developmentObjects.Add(instance);
        }

        static void DevelopmentPlayChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            { CleanupDevelopment(); return; }
            if (state != PlayModeStateChange.EnteredPlayMode || !developmentRunning) return;
            // Loading the large saved scene is not a stalled Host handshake.
            developmentDeadline = EditorApplication.timeSinceStartup + 45;
            EditorApplication.update -= TickDevelopment;
            EditorApplication.update += TickDevelopment;
        }

        static void TickDevelopment()
        {
            try
            {
                Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path == City && SceneManager.sceneCount == 1,
                    "El ensayo perdió la escena única de Ciudad del Sol.");
                if (developmentEncounterReady)
                { Check(stopWarriorTrial != null && NetworkServer.active, "El Host del ensayo se detuvo."); ObserveOracleTrial(); return; }
                Check(EditorApplication.timeSinceStartup < developmentDeadline, "El arranque superó 45 segundos. No se reintentará automáticamente.");
                if (!developmentOwnsHost)
                {
                    var events = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    Check(events.Length <= 1, "Hay EventSystems duplicados.");
                    if (events.Length == 0)
                        developmentObjects.Add(new GameObject("TEMP Ciudad del Sol · EventSystem", typeof(EventSystem)));
                    RiftInputUI.Ensure();
                    StartWarriorTrial();
                    Check(stopWarriorTrial != null, "El Host rechazó el inicio; revisar el diagnóstico anterior.");
                    developmentOwnsHost = true;
                    return;
                }
                Check(stopWarriorTrial != null, "El ensayo del Warrior falló durante su preparación.");
                if (!warriorTrialReady) return;
                StartDemonTrial();
                Check(demonTrial != null && demonTrial.Enemy, "El Diablillo no pudo crearse; revisar el diagnóstico anterior.");
                bool oracleReady = TryStartOracleTrial();
                bool populationReady = oracleReady && StartDevelopmentPopulation();
                developmentEncounterReady = true;
                if (populationReady)
                    Debug.Log("CIUDAD_DESARROLLO ARRANQUE_COMPLETO: Host local + Warrior + HUD/cartografía originales + gestor exterior. " +
                        "Validación por sectores pendiente: buscar CIUDAD_EXTERIOR PREFLIGHT_TERMINADO. Combate y rendimiento requieren prueba manual.");
                else
                    Debug.LogWarning("CIUDAD_DESARROLLO ARRANQUE_PARCIAL: población exterior no iniciada. Revisar CIUDAD_ORACULO NO_CREADO / CIUDAD_POBLACION EXTERIOR_NO_INICIADO. Sin reintentos automáticos.");
            }
            catch (Exception error) { AbortDevelopment(error); }
        }

        static void AbortDevelopment(Exception error)
        {
            Debug.LogError("CIUDAD_DESARROLLO CANCELADO: " + error.Message + " No habrá reintento automático.");
            CleanupDevelopment();
            EditorApplication.delayCall += StopDevelopmentPlay;
        }
        static void StopDevelopmentPlay() { if (EditorApplication.isPlaying) EditorApplication.isPlaying = false; }

        static void CleanupDevelopment()
        {
            EditorApplication.update -= TickDevelopment;
            if (!developmentRunning) return;
            developmentRunning = false;
            try
            {
                if (developmentOwnsHost)
                {
                    try { StopDevelopmentPopulation(); }
                    finally
                    {
                        try { StopOracleTrial(); }
                        finally { try { StopDemonTrial(); } finally { stopWarriorTrial?.Invoke(); } }
                    }
                }
            }
            finally
            {
                developmentOwnsHost = developmentEncounterReady = false;
                foreach (var instance in developmentObjects)
                    if (instance) { try { Object.Destroy(instance); } catch (Exception error) { Debug.LogException(error); } }
                developmentObjects.Clear();
                restoreDevelopmentSettings?.Invoke(); restoreDevelopmentSettings = null;
            }
        }
    }
}
