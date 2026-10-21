using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mirror;
using MultiFPS;
using MultiFPS.UI;
using ROM.Horde;
using ROM.UI.Sprint01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ROM.EditorTools
{
    [InitializeOnLoad]
    public static class VillageCommerceProbe
    {
        const string Key = "ROM.VillageCommerceProbe";
        static int step;
        static double began, next;
        static string goldKey;
        static int gold, small, large;
        static bool goldExists, smallExists, largeExists, walletSaved;
        static readonly System.Collections.Generic.List<string> report = new System.Collections.Generic.List<string>();
        static VillageCommerceProbe() { EditorApplication.playModeStateChanged += Changed; }
        public static void Run() { SessionState.SetBool(Key, true); EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu); EditorApplication.isPlaying = true; }
        static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) { step = 0; began = EditorApplication.timeSinceStartup; next = began + 3; report.Clear(); EditorApplication.update += Tick; }
            if (state == PlayModeStateChange.EnteredEditMode) { SessionState.SetBool(Key, false); EditorApplication.Exit(SessionState.GetInt(Key + "Result", 1)); }
        }
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); report.Add("PASS " + message); }
        static void Click(string label) { RiftVillageUI.Instance.GetComponentsInChildren<Button>().First(b => b.name == label).onClick.Invoke(); }
        static void Later(int value) { step = value; next = EditorApplication.timeSinceStartup + .8; }
        static void Capture(string name) { typeof(GameUIProbe).GetMethod("CaptureGame", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { "Documentation/village-night/" + name + ".png" }); }
        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - began > 130) throw new Exception("Commerce test timeout " + step);
                if (EditorApplication.timeSinceStartup < next) return;
                if (step == 0)
                {
                    var hub = UnityEngine.Object.FindFirstObjectByType<RiftHub>(); if (!hub || !hub.Stage || !hub.Stage.Ready) return;
                    hub.Creator.BotsToggle.isOn = false; hub.Creator.HostGameButton.onClick.Invoke(); step = 1; next = EditorApplication.timeSinceStartup + 18; return;
                }
                var player = ClientFrontend.ClientPlayerInstance; var actor = ClientFrontend.OwnedCharacterInstance;
                if (!player || !actor) return;
                var trade = player.GetComponent<RiftVillagePlayer>();
                var alma = UnityEngine.Object.FindObjectsByType<RiftVillageNpc>(FindObjectsSortMode.None).FirstOrDefault(n => n.Id == "alma");
                if (step == 1)
                {
                    Check(trade && alma && UnityEngine.Object.FindObjectsByType<RiftVillageNpc>(FindObjectsSortMode.None).Length == 3, "Three IDS NPCs and player trade component");
                    goldKey = "ROM.Exploration.Gold." + UserSettings.UserNickname;
                    goldExists = PlayerPrefs.HasKey(goldKey); smallExists = PlayerPrefs.HasKey(RiftVillageSupplies.Key(0)); largeExists = PlayerPrefs.HasKey(RiftVillageSupplies.Key(1));
                    gold = RiftExplorationProgress.Gold; small = RiftVillageSupplies.Count(0); large = RiftVillageSupplies.Count(1); walletSaved = true;
                    PlayerPrefs.SetInt(goldKey, 100); PlayerPrefs.SetInt(RiftVillageSupplies.Key(0), 0); PlayerPrefs.SetInt(RiftVillageSupplies.Key(1), 0); PlayerPrefs.Save();
                    actor.ServerTeleport(alma.transform.position + Vector3.forward * 3, 180); Later(2); return;
                }
                if (step == 2)
                {
                    Capture("npc-alma-world"); RiftVillageUI.Instance.Open(alma); Check(RiftVillageUI.Instance.TalkingTo == alma && !UICursorManager.GamePlayInput(), "Dialogue opens and blocks gameplay input");
                    Click("HABLAR DEL REFUGIO"); Click("VER MERCANCÍA"); Capture("npc-dialogue-shop"); Click("COMPRAR · 25 ORO"); Later(3); return;
                }
                if (step == 3)
                {
                    Check(RiftExplorationProgress.Gold == 75 && RiftVillageSupplies.Count(0) == 1, "UI purchase debits 25 earned gold and persists potion");
                    actor.Health.ServerHeal(actor.Health.MaxHealth, actor.DNID); trade.CmdDrinkVillagePotion(); Later(4); return;
                }
                if (step == 4)
                {
                    Check(RiftVillageSupplies.Count(0) == 1, "Full health does not consume potion");
                    actor.Health.ServerSetVitals(actor.Health.MaxHealth, actor.Health.MaxHealth / 2); player.LastCombatAt = NetworkTime.time;
                    trade.CmdDrinkVillagePotion(); Later(5); return;
                }
                if (step == 5)
                {
                    Check(RiftVillageSupplies.Count(0) == 0 && actor.Health.CurrentHealth > actor.Health.MaxHealth * .85f && trade.PotionReadyAt > NetworkTime.time, "Potion heals on server, consumes once and starts cooldown");
                    Click("COMPRAR · 60 ORO"); Later(6); return;
                }
                if (step == 6)
                {
                    Check(RiftExplorationProgress.Gold == 15 && RiftVillageSupplies.Count(1) == 1, "Greater potion costs 60 earned gold");
                    Click("COMPRAR · 60 ORO"); Later(7); return;
                }
                if (step == 7)
                {
                    Check(RiftExplorationProgress.Gold == 15 && RiftVillageSupplies.Count(1) == 1, "Insufficient balance rejects purchase without losing items or gold");
                    Click("VENDER · 30 ORO"); Later(8); return;
                }
                if (step == 8)
                {
                    Check(RiftExplorationProgress.Gold == 45 && RiftVillageSupplies.Count(1) == 0, "Resale returns 30 gold and removes potion");
                    Capture("npc-trade-complete"); RiftVillageUI.Instance.Close(); Check(UICursorManager.GamePlayInput(), "Closing dialogue restores gameplay input");
                    actor.ServerTeleport(new Vector3(0, .2f, 65), 0); Later(9); return;
                }
                if (step == 9) { trade.CmdVillageTrade("alma", 0, false); Later(10); return; }
                Check(RiftExplorationProgress.Gold == 45 && RiftVillageSupplies.Count(0) == 0, "Remote purchase from outside interaction range rejected");
                Finish(true);
            }
            catch (Exception e) { Debug.LogException(e); report.Add(e.ToString()); Finish(false); }
        }
        static void Restore(string key, bool existed, int value) { if (existed) PlayerPrefs.SetInt(key, value); else PlayerPrefs.DeleteKey(key); }
        static void Finish(bool success)
        {
            if (walletSaved) { Restore(goldKey, goldExists, gold); Restore(RiftVillageSupplies.Key(0), smallExists, small); Restore(RiftVillageSupplies.Key(1), largeExists, large); PlayerPrefs.Save(); walletSaved = false; }
            File.WriteAllLines("Documentation/village-night/commerce-validation.txt", report);
            SessionState.SetInt(Key + "Result", success ? 0 : 1); EditorApplication.update -= Tick; EditorApplication.isPlaying = false;
        }
    }
}
