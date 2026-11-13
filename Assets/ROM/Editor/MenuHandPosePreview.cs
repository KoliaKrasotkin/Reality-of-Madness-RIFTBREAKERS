using ROM.UI.Sprint01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ROM.EditorTools
{
    [InitializeOnLoad]
    static class MenuHandPosePreview
    {
        static MenuHandPosePreview() => EditorApplication.update += Sample;

        static void Sample()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !AnimationMode.InAnimationMode()) return;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || !stage.assetPath.StartsWith(MenuCharacterAuthoring.Folder + "/")) return;

            foreach (var pose in stage.prefabContentsRoot.GetComponentsInChildren<MenuHandPose>())
            {
                if (!pose.isActiveAndEnabled) continue;
                pose.ApplyAnimationPreview();
            }
        }
    }
}
