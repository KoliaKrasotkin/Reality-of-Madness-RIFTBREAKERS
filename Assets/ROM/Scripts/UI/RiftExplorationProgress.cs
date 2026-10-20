using UnityEngine;
namespace ROM.UI.Sprint01 {
    // Offline prototype rewards, deliberately separate from purchasable currency.
    public static class RiftExplorationProgress {
        internal static string Key => "ROM.Exploration.Gold."+MultiFPS.UserSettings.UserNickname;
        public static int Gold => PlayerPrefs.GetInt(Key,0);
        public static string Notice { get; private set; }
        public static float NoticeUntil { get; private set; }
        public static void Receive(int gold,string message) {
            if(gold>0){PlayerPrefs.SetInt(Key,Gold+gold);PlayerPrefs.Save();}
            Notice=message;NoticeUntil=Time.unscaledTime+6;
        }
    }
}
