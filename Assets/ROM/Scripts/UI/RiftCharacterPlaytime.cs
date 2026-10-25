using System;
using UnityEngine;
using MultiFPS.UI;
using Mirror;
namespace ROM.UI.Sprint01
{
    public sealed class RiftCharacterPlaytime : MonoBehaviour
    {
        [Min(1)] public float FlushInterval = 15;
        float nextFlush;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            if (FindFirstObjectByType<RiftCharacterPlaytime>()) return;
            var go = new GameObject("Local character playtime"); DontDestroyOnLoad(go); go.AddComponent<RiftCharacterPlaytime>();
        }
        public static string PlayingCharacterId
        {
            get
            {
                var player = ClientFrontend.ClientPlayerInstance; var actor = ClientFrontend.OwnedCharacterInstance;
                var pause = UIPauseMenu._instance;
                if (!NetworkClient.active || !player || !actor || player.MyCharacter != actor || ClientFrontend.Hub || ClientFrontend.Pause ||
                    (pause && (pause.InterfaceShowing || pause.pauseMenuShowing)) || !actor.gameObject.scene.isLoaded || actor.gameObject.scene.name == "MainMenu") return null;
                return Guid.TryParse(player.PlayerInfo.CharacterId, out _) ? player.PlayerInfo.CharacterId : null;
            }
        }
        void Update()
        {
            string id = PlayingCharacterId;
            if (id != null) RiftCharacterMetadata.RecordSessionSeconds(id, Mathf.Min(1, Time.unscaledDeltaTime));
            if (Time.unscaledTime >= nextFlush) { nextFlush = Time.unscaledTime + Mathf.Max(1, FlushInterval); RiftCharacterMetadata.Flush(); }
        }
        void OnApplicationPause(bool paused) { if (paused) RiftCharacterMetadata.Flush(); }
        void OnApplicationQuit() { RiftCharacterMetadata.Flush(); }
        void OnDestroy() { RiftCharacterMetadata.Flush(); }
    }
}
