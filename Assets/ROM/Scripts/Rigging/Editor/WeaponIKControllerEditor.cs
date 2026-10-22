using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

[CustomEditor(typeof(WeaponIKController))]
public class WeaponIKControllerEditor : Editor
{
    private static readonly string[] LocomotionNames =
        { "Idle", "Run-Forward", "Run-Backward", "Run-Left", "Run-Right" };
    private const int SampleIntervals = 20;

    private Animator animator;
    private RigBuilder rigBuilder;
    private AnimatedWeaponGripConstraint gripConstraint;
    private AnimationClip previewClip;
    private int locomotionPreview;
    private float previewTime;
    private PlayableGraph previewGraph;
    private AnimationClipPlayable clipPlayable;
    private bool previewActive;
    private string validationReport;
    private bool showValidation;
    private PoseSnapshot[] previewPose;

    private struct PoseSnapshot
    {
        public Transform transform;
        public Vector3 position, scale;
        public Quaternion rotation;
    }

    private void OnEnable() => RefreshReferences();
    private void OnDisable() => StopPreview();

    private void RefreshReferences()
    {
        var controller = (WeaponIKController)target;
        animator = controller.Animator;
        rigBuilder = controller.RigBuilder;
        gripConstraint = controller.GripConstraint;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            StopPreview();
            RefreshReferences();
            previewClip = null;
        }

        if (animator == null || rigBuilder == null || gripConstraint == null)
        {
            EditorGUILayout.HelpBox("Assign an Animator with a RigBuilder and an Animated Weapon Grip constraint.", MessageType.Warning);
            return;
        }
        EditorGUILayout.ObjectField("Grip Constraint", gripConstraint, typeof(AnimatedWeaponGripConstraint), true);

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Preview", EditorStyles.boldLabel);
            if (GUILayout.Button("Validate Locomotion Grip")) ValidateLocomotionGrip();
            if (!string.IsNullOrEmpty(validationReport))
            {
                showValidation = EditorGUILayout.Foldout(showValidation, "Last Grip Validation");
                if (showValidation) EditorGUILayout.HelpBox(validationReport, MessageType.Info);
            }

            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.Popup("Locomotion Clip", locomotionPreview, LocomotionNames);
            if (EditorGUI.EndChangeCheck() || previewClip == null)
            {
                locomotionPreview = next;
                SelectClip(FindLocomotionClip(LocomotionNames[next]));
            }
            EditorGUI.BeginChangeCheck();
            var clip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", previewClip, typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck()) SelectClip(clip);
            if (previewClip == null) return;

            previewTime = EditorGUILayout.Slider("Preview Time", previewTime, 0f, previewClip.length);
            if (GUILayout.Button(previewActive ? "Stop Preview" : "Start Preview"))
            {
                if (previewActive) StopPreview();
                else StartPreview();
            }
            if (previewActive) SamplePreview();
        }
    }

    private AnimationClip FindLocomotionClip(string name) => animator.runtimeAnimatorController == null
        ? null
        : Array.Find(animator.runtimeAnimatorController.animationClips, clip => clip.name == "2Hand-Sword-" + name);

    private void SelectClip(AnimationClip clip)
    {
        StopPreview();
        previewClip = clip;
        previewTime = 0f;
    }

    private void StartPreview()
    {
        if (Application.isPlaying || animator == null || rigBuilder == null || previewClip == null) return;
        StopPreview();
        Transform[] transforms = animator.GetComponentsInChildren<Transform>(true);
        previewPose = new PoseSnapshot[transforms.Length];
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform item = transforms[i];
            previewPose[i] = new PoseSnapshot
            {
                transform = item, position = item.localPosition,
                rotation = item.localRotation, scale = item.localScale
            };
        }
        try
        {
            rigBuilder.Clear();
            previewGraph = PlayableGraph.Create("Weapon IK Preview");
            previewGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            clipPlayable = AnimationClipPlayable.Create(previewGraph, previewClip);
            clipPlayable.SetApplyFootIK(false);
            var output = AnimationPlayableOutput.Create(previewGraph, "Animation", animator);
            output.SetSourcePlayable(rigBuilder.BuildPreviewGraph(previewGraph, clipPlayable));
            previewGraph.Play();
            previewActive = true;
            SamplePreview();
        }
        catch
        {
            StopPreview();
            throw;
        }
    }

    private void SamplePreview()
    {
        if (!previewActive || !previewGraph.IsValid()) return;
        clipPlayable.SetTime(previewTime);
        rigBuilder.UpdatePreviewGraph(previewGraph);
        previewGraph.Evaluate(0f);
        SceneView.RepaintAll();
    }

    private void StopPreview()
    {
        bool hadPreview = previewPose != null;
        previewActive = false;
        if (previewGraph.IsValid()) previewGraph.Destroy();
        if (hadPreview && rigBuilder != null) rigBuilder.StopPreview();
        if (previewPose == null) return;
        foreach (var pose in previewPose)
        {
            if (pose.transform == null) continue;
            pose.transform.localPosition = pose.position;
            pose.transform.localRotation = pose.rotation;
            pose.transform.localScale = pose.scale;
        }
        previewPose = null;
        SceneView.RepaintAll();
    }

    private void ValidateLocomotionGrip()
    {
        if (!animator.isHuman || !gripConstraint.data.IsValid())
        {
            validationReport = "A Humanoid Animator and valid grip references are required.";
            return;
        }
        AnimationClip originalClip = previewClip;
        float originalTime = previewTime;
        StopPreview();
        var data = gripConstraint.data;
        var report = new StringBuilder($"Maximum wrist-to-target error ({SampleIntervals + 1} samples per clip):");
        try
        {
            foreach (string name in LocomotionNames)
            {
                previewClip = FindLocomotionClip(name);
                if (previewClip == null)
                {
                    report.Append($"\n{name}: clip missing");
                    continue;
                }
                previewTime = 0f;
                StartPreview();
                Vector3 firstPosition = animator.transform.InverseTransformPoint(data.weapon.position);
                Quaternion firstRotation = Quaternion.Inverse(animator.transform.rotation) * data.weapon.rotation;
                float leftError = 0f, rightError = 0f, leftAngle = 0f, rightAngle = 0f, travel = 0f, turn = 0f;
                for (int i = 0; i <= SampleIntervals; i++)
                {
                    previewTime = previewClip.length * i / SampleIntervals;
                    SamplePreview();
                    leftError = Mathf.Max(leftError, Vector3.Distance(data.leftHand.position, data.leftTarget.position));
                    rightError = Mathf.Max(rightError, Vector3.Distance(data.rightHand.position, data.rightTarget.position));
                    leftAngle = Mathf.Max(leftAngle, Quaternion.Angle(data.leftHand.rotation, data.leftTarget.rotation));
                    rightAngle = Mathf.Max(rightAngle, Quaternion.Angle(data.rightHand.rotation, data.rightTarget.rotation));
                    travel = Mathf.Max(travel, Vector3.Distance(firstPosition, animator.transform.InverseTransformPoint(data.weapon.position)));
                    turn = Mathf.Max(turn, Quaternion.Angle(firstRotation, Quaternion.Inverse(animator.transform.rotation) * data.weapon.rotation));
                }
                report.Append($"\n{name}: left {leftError * 1000f:F1} mm / {leftAngle:F1} deg; right {rightError * 1000f:F1} mm / {rightAngle:F1} deg; axe motion {travel * 100f:F1} cm / {turn:F1} deg");
                StopPreview();
            }
        }
        finally
        {
            StopPreview();
            previewClip = originalClip;
            previewTime = originalTime;
        }
        validationReport = report.ToString();
        Debug.Log(validationReport, target);
    }
}
