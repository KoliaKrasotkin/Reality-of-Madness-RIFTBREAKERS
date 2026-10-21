using UnityEditor;
using UnityEngine;
using MultiFPS.Gameplay.RiftCombat;
namespace ROM.EditorTools {
public static class RiftCombatFeedbackBuilder {
 public static void Build(){const string path="Assets/ROM/Resources/RiftCombatTuning.asset";var tuning=AssetDatabase.LoadAssetAtPath<RiftCombatTuning>(path);if(!tuning){tuning=ScriptableObject.CreateInstance<RiftCombatTuning>();AssetDatabase.CreateAsset(tuning,path);}AssetDatabase.SaveAssets();}
 public static void Run(){Build();EditorApplication.Exit(0);}
}}
