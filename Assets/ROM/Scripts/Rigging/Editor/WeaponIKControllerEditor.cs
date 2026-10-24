using System;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;
using MultiFPS.Gameplay.RiftCombat;

[CustomEditor(typeof(WeaponIKController))]
public class WeaponIKControllerEditor : Editor
{
    private static readonly string[] ClipNames =
        { "Idle", "Run-Forward", "Run-Backward", "Run-Left", "Run-Right",
          "Attack1", "Attack2", "Attack3", "Attack4", "Attack5", "Attack6", "Attack7", "Attack8", "Attack9",
          "Jump", "Fall", "Land", "DiveRoll-Forward1", "Sheath-Back-Unarmed", "Unsheath-Back-Unarmed",
          "Unarmed/Idle", "Unarmed/Run-Forward", "Unarmed/Run-Backward", "Unarmed/Run-Left", "Unarmed/Run-Right",
          "Unarmed/Jump", "Unarmed/Fall", "Unarmed/DiveRoll-Forward1" };
    private const int SampleIntervals = 60;

    private Animator animator;
    private RigBuilder rigBuilder;
    private AnimatedWeaponGripConstraint gripConstraint;
    private AnimationClip previewClip;
    private int selectedPreview;
    private float previewTime;
    private PlayableGraph previewGraph;
    private AnimationClipPlayable clipPlayable;
    private AnimationClipPlayable presentationPlayable;
    private AnimationClip presentationClip;
    private UnityEngine.Animations.Rigging.Rig[] previewRigs;
    private float[] previewRigWeights;
    private TwoBoneIKConstraint[] previewArms;
    private float[] previewArmWeights;
    private Renderer[] previewRenderers;
    private bool[] previewRendererEnabled;
    private float previewHandFollow;
    private float previewBackWeight;
    private bool previewActive;
    private string validationReport;
    private bool showValidation;
    private PoseSnapshot[] previewPose;
    private WeaponDefinition previewWeapon;
    private RuntimeAnimatorController originalController;
    private GameObject previewEquipmentRoot;
    private CharacterWeaponPresentation equipmentPreview;
    private GameObject builtInWeapon;
    private bool builtInWasActive;
    private bool showEquipmentOffsets = true;
    private double nextPreviewUpdate;

    private struct PoseSnapshot
    {
        public Transform transform;
        public Vector3 position, scale;
        public Quaternion rotation;
    }

    private void OnEnable()
    {
        RefreshReferences();
        var equipment = animator ? animator.GetComponentInParent<RiftWarriorEquipment>() : null;
        if (equipment) previewWeapon = equipment.AxeWeapon;
        EditorApplication.update += UpdatePreview;
        AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }
    private void OnDisable()
    {
        StopPreview();
        EditorApplication.update -= UpdatePreview;
        AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }
    private void OnPlayModeChanged(PlayModeStateChange state) => StopPreview();
    private void UpdatePreview()
    {
        if (!previewActive || EditorApplication.timeSinceStartup < nextPreviewUpdate) return;
        nextPreviewUpdate = EditorApplication.timeSinceStartup + .05;
        SamplePreview();
    }

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

        bool persistentAsset = EditorUtility.IsPersistent(animator);
        if (persistentAsset && !Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Open the prefab to preview equipment in the Scene view.", MessageType.Info);
            if (GUILayout.Button("Open Prefab for Preview"))
            {
                var stage = PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(target));
                Selection.activeGameObject = stage.prefabContentsRoot;
                return;
            }
        }
        using (new EditorGUI.DisabledScope(Application.isPlaying || persistentAsset))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Preview", EditorStyles.boldLabel);
            DrawEquipmentSelector();
            using (new EditorGUI.DisabledScope(previewWeapon && !previewWeapon.UsesBuiltInWeapon))
                if (GUILayout.Button("Validate Axe Animation Grips")) ValidateAnimationGrips();
            if (!string.IsNullOrEmpty(validationReport))
            {
                showValidation = EditorGUILayout.Foldout(showValidation, "Last Grip Validation");
                if (showValidation) EditorGUILayout.HelpBox(validationReport, MessageType.Info);
            }

            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.Popup("Animation", selectedPreview, ClipNames);
            if (EditorGUI.EndChangeCheck() || previewClip == null)
            {
                selectedPreview = next;
                SelectClip(FindClip(ClipNames[next]));
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
            DrawEquipmentOffsets();
        }
    }

    private void DrawEquipmentSelector()
    {
        var equipment = animator.GetComponentInParent<RiftWarriorEquipment>();
        if (equipment)
        {
            int current = previewWeapon == equipment.AxeWeapon ? 0 : previewWeapon == equipment.SwordShieldWeapon ? 1 : 2;
            int next = EditorGUILayout.Popup("Equipment", current, new[] { "Axe", "Sword and shield", "Custom definition" });
            if (next != current && next < 2) SetPreviewWeapon(next == 0 ? equipment.AxeWeapon : equipment.SwordShieldWeapon);
            else if (next != current) SetPreviewWeapon(null);
        }
        EditorGUI.BeginChangeCheck();
        var weapon = (WeaponDefinition)EditorGUILayout.ObjectField("Weapon Definition", previewWeapon, typeof(WeaponDefinition), false);
        if (EditorGUI.EndChangeCheck()) SetPreviewWeapon(weapon);
    }

    private void SetPreviewWeapon(WeaponDefinition weapon)
    {
        bool restart = previewActive;
        StopPreview();
        previewWeapon = weapon;
        previewClip = FindClip(ClipNames[selectedPreview]);
        previewTime = 0;
        if (restart) StartPreview();
    }

    private AnimationClip ResolveEquipmentClip(AnimationClip clip)
    {
        var overrides = previewWeapon ? previewWeapon.Controller as AnimatorOverrideController : null;
        return clip && overrides && overrides[clip] ? overrides[clip] : clip;
    }

    private AnimationClip FindClip(string name)
    {
        var controller = originalController ? originalController : animator.runtimeAnimatorController;
        if (previewWeapon && previewWeapon.Controller) controller = previewWeapon.Controller;
        while (controller is AnimatorOverrideController overrides) controller = overrides.runtimeAnimatorController;
        if (!controller) return null;
        return ResolveEquipmentClip(Array.Find(controller.animationClips, clip => clip.name ==
            (name.StartsWith("Unarmed/") ? name.Replace("Unarmed/", "Unarmed-") : "2Hand-Sword-" + name)));
    }

    private void DrawEquipmentOffsets()
    {
        if (!previewWeapon || previewWeapon.UsesBuiltInWeapon) return;
        showEquipmentOffsets = EditorGUILayout.Foldout(showEquipmentOffsets, "Equipment offsets (definition assets)", true);
        if (!showEquipmentOffsets) return;
        EditorGUILayout.HelpBox("Position, rotation and scale update the preview immediately and are saved to each definition asset. Stop Preview restores the character pose, keeping your asset adjustments.", MessageType.Info);
        foreach (var part in previewWeapon.EquippedParts)
        {
            EditorGUILayout.LabelField(part.DisplayName + " — " + part.Socket, EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Definition", part, typeof(WeaponDefinition), false);
            var settings = new SerializedObject(part);
            settings.Update();
            EditorGUI.BeginChangeCheck();
            foreach (string name in new[] { "Position", "Rotation", "Scale", "StoredBone", "StoredPosition", "StoredRotation" })
                EditorGUILayout.PropertyField(settings.FindProperty(name));
            if (EditorGUI.EndChangeCheck())
            {
                settings.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(part);
                SamplePreview();
            }
        }
    }

    private void SelectClip(AnimationClip clip)
    {
        bool restart = previewActive;
        StopPreview();
        previewClip = clip;
        previewTime = 0f;
        if (restart) StartPreview();
    }

    private void StartPreview()
    {
        if (Application.isPlaying || animator == null || EditorUtility.IsPersistent(animator) || rigBuilder == null || previewClip == null) return;
        StopPreview();
        originalController = animator.runtimeAnimatorController;
        var avatar = animator.GetComponentInParent<WarriorAvatar>();
        builtInWeapon = avatar ? avatar.EquippedWeapon : null;
        builtInWasActive = builtInWeapon && builtInWeapon.activeSelf;
        previewRigs = animator.GetComponentsInChildren<UnityEngine.Animations.Rigging.Rig>(true);
        previewRigWeights = Array.ConvertAll(previewRigs, rig => rig.weight);
        previewArms = animator.GetComponentsInChildren<TwoBoneIKConstraint>(true);
        previewArmWeights = Array.ConvertAll(previewArms, arm => arm.weight);
        previewRenderers = animator.GetComponentsInChildren<Renderer>(true);
        previewRendererEnabled = Array.ConvertAll(previewRenderers, renderer => renderer.enabled);
        previewHandFollow = gripConstraint.data.rightHandFollow;
        previewBackWeight = gripConstraint.data.backWeight;
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
            if (previewWeapon)
            {
                previewEquipmentRoot = new GameObject("Weapon equipment preview") { hideFlags = HideFlags.HideAndDontSave };
                previewEquipmentRoot.transform.SetParent(animator.transform.parent, false);
                equipmentPreview = previewEquipmentRoot.AddComponent<CharacterWeaponPresentation>();
                equipmentPreview.Animator = animator;
                equipmentPreview.BuiltInWeapon = builtInWeapon;
                equipmentPreview.Equip(previewWeapon);
                foreach (var item in previewEquipmentRoot.GetComponentsInChildren<Transform>(true))
                    item.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            previewGraph = PlayableGraph.Create("Weapon IK Preview");
            previewGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            clipPlayable = AnimationClipPlayable.Create(previewGraph, previewClip);
            clipPlayable.SetApplyFootIK(false);
            string presentationName = previewClip.name.StartsWith("Unarmed-") ? "WeaponHolstered"
                : previewClip.name.Contains("Unsheath-") ? "WeaponDrawing"
                : previewClip.name.Contains("Sheath-") ? "WeaponSheathing" : "WeaponDrawn";
            presentationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ROM/Animation/WarriorWeaponStates/" + presentationName + ".anim");
            presentationClip = ResolveEquipmentClip(presentationClip);
            Playable source = clipPlayable;
            if (presentationClip != null)
            {
                presentationPlayable = AnimationClipPlayable.Create(previewGraph, presentationClip);
                var mixer = AnimationLayerMixerPlayable.Create(previewGraph, 2);
                previewGraph.Connect(clipPlayable, 0, mixer, 0);
                previewGraph.Connect(presentationPlayable, 0, mixer, 1);
                mixer.SetInputWeight(0, 1);
                mixer.SetInputWeight(1, 1);
                source = mixer;
            }
            var output = AnimationPlayableOutput.Create(previewGraph, "Animation", animator);
            output.SetSourcePlayable(rigBuilder.BuildPreviewGraph(previewGraph, source));
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
        if (presentationPlayable.IsValid()) presentationPlayable.SetTime(Mathf.Min(previewTime, presentationClip.length));
        rigBuilder.UpdatePreviewGraph(previewGraph);
        previewGraph.Evaluate(0f);
        if (equipmentPreview)
        {
            float stored = 0;
            if (presentationClip)
                foreach (var binding in AnimationUtility.GetCurveBindings(presentationClip))
                    if (binding.type == typeof(Animator) && binding.propertyName == "EquipmentBackWeight")
                    { stored = AnimationUtility.GetEditorCurve(presentationClip, binding).Evaluate(previewTime); break; }
            equipmentPreview.RefreshPose(stored);
        }
        SceneView.RepaintAll();
    }

    private void StopPreview()
    {
        bool hadPreview = previewPose != null;
        previewActive = false;
        if (previewGraph.IsValid()) previewGraph.Destroy();
        if (previewEquipmentRoot) DestroyImmediate(previewEquipmentRoot);
        previewEquipmentRoot = null;
        equipmentPreview = null;
        if (originalController && animator) animator.runtimeAnimatorController = originalController;
        originalController = null;
        if (builtInWeapon) builtInWeapon.SetActive(builtInWasActive);
        builtInWeapon = null;
        if (hadPreview && rigBuilder != null) rigBuilder.StopPreview();
        if (previewRigs != null)
        {
            for (int i = 0; i < previewRigs.Length; i++) if (previewRigs[i]) previewRigs[i].weight = previewRigWeights[i];
            if (gripConstraint)
            {
                var data = gripConstraint.data;
                data.rightHandFollow = previewHandFollow;
                data.backWeight = previewBackWeight;
                gripConstraint.data = data;
            }
        }
        if (previewRenderers != null)
            for (int i = 0; i < previewRenderers.Length; i++) if (previewRenderers[i]) previewRenderers[i].enabled = previewRendererEnabled[i];
        if (previewArms != null)
            for (int i = 0; i < previewArms.Length; i++) if (previewArms[i]) previewArms[i].weight = previewArmWeights[i];
        previewRigs = null;
        previewRenderers = null;
        previewArms = null;
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

    public string ValidateAnimationGrips()
    {
        if (!animator.isHuman || !gripConstraint.data.IsValid())
        {
            validationReport = "A Humanoid Animator and valid grip references are required.";
            return validationReport;
        }
        AnimationClip originalClip = previewClip;
        float originalTime = previewTime;
        StopPreview();
        var data = gripConstraint.data;
        var report = new StringBuilder($"Maximum wrist-to-target error ({SampleIntervals + 1} samples per clip):");
        try
        {
            foreach (string name in ClipNames)
            {
                if (name.StartsWith("Unarmed/") || name.Contains("Sheath-") || name.Contains("Unsheath-")) continue;
                previewClip = FindClip(name);
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
        return validationReport;
    }
}
