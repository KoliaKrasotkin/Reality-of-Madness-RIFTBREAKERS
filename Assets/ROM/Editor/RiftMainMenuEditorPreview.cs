using System;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using ROM.UI.Sprint01;
namespace ROM.EditorTools {
public static class RiftMainMenuEditorPreview {
 public const string LayoutPath="Assets/ROM/Resources/RiftMenu/MainMenuLayout.asset";
 [MenuItem("Rift/UI/Edit Main Menu Layout")]
 public static void EditLayout(){Selection.activeObject=EnsureLayout();EditorGUIUtility.PingObject(Selection.activeObject);}
 public static RiftMainMenuLayout EnsureLayout(){var layout=AssetDatabase.LoadAssetAtPath<RiftMainMenuLayout>(LayoutPath);if(!layout){layout=ScriptableObject.CreateInstance<RiftMainMenuLayout>();AssetDatabase.CreateAsset(layout,LayoutPath);AssetDatabase.SaveAssets();}return layout;}
 [MenuItem("Rift/UI/Rebuild Main Menu Editor Preview")]
 public static void OpenAndRebuild(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before editing the preview.");foreach(var scene in LoadedScenes())if(scene.isDirty)throw new InvalidOperationException("Save or resolve unsaved scene changes before opening MainMenu.");EditorSceneManager.OpenScene("Assets/ROM/Scenes/MainMenu.unity");RebuildCurrent();}
 static System.Collections.Generic.IEnumerable<UnityEngine.SceneManagement.Scene> LoadedScenes(){for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)yield return UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);}
 public static RiftHub RebuildCurrent(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Preview editing is Edit Mode only.");var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>(FindObjectsInactive.Include);if(!hub||hub.InGame)throw new InvalidOperationException("Open Assets/ROM/Scenes/MainMenu.unity first.");EnsureLayout();Undo.RegisterFullObjectHierarchyUndo(hub.gameObject,"Rebuild real main-menu preview");hub.Build();EditorUtility.SetDirty(hub);EditorSceneManager.MarkSceneDirty(hub.gameObject.scene);Selection.activeGameObject=hub.gameObject;SceneView.RepaintAll();return hub;}
}
[CustomEditor(typeof(RiftMainMenuLayout))]public sealed class RiftMainMenuLayoutInspector:Editor {
 public override void OnInspectorGUI(){EditorGUILayout.HelpBox("Source of truth for the main menu. Coordinates use the 1920 x 1080 design canvas. Edit here, rebuild the preview, then save MainMenu. Runtime uses the same asset. The animated character and portal are created in Play; other game screens keep their own existing configuration.",MessageType.Info);DrawDefaultInspector();if(GUILayout.Button("Rebuild in current MainMenu scene"))RiftMainMenuEditorPreview.RebuildCurrent();if(GUILayout.Button("Select main-menu hierarchy")){var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>(FindObjectsInactive.Include);if(hub)Selection.activeGameObject=hub.gameObject;}}
}
[CustomEditor(typeof(RiftHub))]public sealed class RiftHubLayoutInspector:Editor {
 public override void OnInspectorGUI(){DrawDefaultInspector();var hub=(RiftHub)target;if(hub.InGame)return;EditorGUILayout.Space();EditorGUILayout.HelpBox("Edit the MainMenuLayout asset for persistent layout changes. Generated hierarchy changes alone are rebuilt on Play. Character/portal previews animate only in Play.",MessageType.Info);if(GUILayout.Button("Edit persistent main-menu layout"))RiftMainMenuEditorPreview.EditLayout();if(!EditorApplication.isPlaying&&GUILayout.Button("Rebuild current editor preview"))RiftMainMenuEditorPreview.RebuildCurrent();}
}
}
