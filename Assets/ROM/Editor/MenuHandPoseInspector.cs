using ROM.UI.Sprint01;
using UnityEditor;
using UnityEngine;

namespace ROM.EditorTools
{
    [CustomEditor(typeof(MenuHandPose))]
    sealed class MenuHandPoseInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var support = serializedObject.FindProperty("supportGrip");
            EditorGUILayout.PropertyField(support);
            var grip = support.objectReferenceValue as Transform;
            var shoulderSocket = serializedObject.FindProperty("shoulderCarrySocket").objectReferenceValue as Transform;
            if (grip || shoulderSocket)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rightHandOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rightHandOffsetRotation"));
                var socket = shoulderSocket ? shoulderSocket : grip.parent ? grip.parent.parent : null;
                if (socket && socket.name == "Menu Weapon Socket")
                    DrawPlacement("Axe Socket", socket);
                if (grip) DrawPlacement("Left Hand Grip", grip);
            }
            var fingers = serializedObject.FindProperty("fingers");
            for (int i = 0; i < fingers.arraySize; i++)
            {
                var finger = fingers.GetArrayElementAtIndex(i);
                var bone = (HumanBodyBones)finger.FindPropertyRelative("Bone").intValue;
                var rotation = finger.FindPropertyRelative("Rotation");
                EditorGUI.BeginChangeCheck();
                var angles = EditorGUILayout.Vector3Field(ObjectNames.NicifyVariableName(bone.ToString()),
                    rotation.quaternionValue.eulerAngles);
                if (EditorGUI.EndChangeCheck()) rotation.quaternionValue = Quaternion.Euler(angles);
            }
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Preview Grip") && !Application.isPlaying)
            {
                if (!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
                var pose = (MenuHandPose)target;
                var animator = pose.GetComponent<Animator>();
                var controller = animator ? animator.runtimeAnimatorController : null;
                var clips = controller ? controller.animationClips : System.Array.Empty<AnimationClip>();
                if (clips.Length == 1 && clips[0])
                {
                    AnimationMode.BeginSampling();
                    try { AnimationMode.SampleAnimationClip(animator.gameObject, clips[0], 0f); }
                    finally { AnimationMode.EndSampling(); }
                }
                pose.ApplyAnimationPreview();
                SceneView.RepaintAll();
            }
            if (AnimationMode.InAnimationMode() && GUILayout.Button("Stop Preview"))
                AnimationMode.StopAnimationMode();
        }
        static void DrawPlacement(string label, Transform transform)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var position = EditorGUILayout.Vector3Field("Local Position", transform.localPosition);
            var rotation = EditorGUILayout.Vector3Field("Local Rotation", transform.localEulerAngles);
            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(transform, "Adjust menu weapon grip");
            transform.SetLocalPositionAndRotation(position, Quaternion.Euler(rotation));
            PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
            if (transform.gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(transform.gameObject.scene);
            SceneView.RepaintAll();
        }
    }
}
