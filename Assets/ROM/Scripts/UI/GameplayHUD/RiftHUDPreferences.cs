using UnityEngine;
namespace ROM.UI.Gameplay {
// Local presentation preferences only. Never modify progression, bindings or combat rules.
public static class RiftHUDPreferences {
 public const string CompactFramesKey="ROM.UI.CompactFrames",MinimalHUDKey="ROM.UI.MinimalHUD";
 public static bool CompactFrames=>PlayerPrefs.GetInt(CompactFramesKey,0)==1;
 public static bool MinimalHUD=>PlayerPrefs.GetInt(MinimalHUDKey,0)==1;
 public static float FrameScale=>CompactFrames?.8f:1f;
 public static void SetCompactFrames(bool value){PlayerPrefs.SetInt(CompactFramesKey,value?1:0);PlayerPrefs.Save();}
 public static void SetMinimalHUD(bool value){PlayerPrefs.SetInt(MinimalHUDKey,value?1:0);PlayerPrefs.Save();}
 public static void RestoreDefaults(){PlayerPrefs.SetInt(CompactFramesKey,0);PlayerPrefs.SetInt(MinimalHUDKey,0);PlayerPrefs.Save();}
}
}
