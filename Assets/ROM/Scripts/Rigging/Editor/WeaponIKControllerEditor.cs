using System;
using System.Linq;
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
    private CharacterSkinDefinition previewSkin;
    private AnimatorCullingMode originalCullingMode;
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
        var skins = ((WeaponIKController)target).GetComponentInParent<CharacterSkinController>();
        if (skins)
        {
            foreach (var slot in skins.Skins)
                if (slot != null && slot.ExistingAnimator == animator) { previewSkin = slot.Definition; break; }
            if (!previewSkin && skins.InitialIndex >= 0 && skins.InitialIndex < skins.Skins.Length)
                previewSkin = skins.Skins[skins.InitialIndex]?.Definition;
        }
        if (previewSkin) previewWeapon = VisualDefinition(previewSkin.DefaultWeapon);
        else if (equipment) previewWeapon = equipment.AxeWeapon;
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

        if (animator == null || !animator.isHuman)
        {
            EditorGUILayout.HelpBox("Assign a Humanoid Animator. Skin equipment previews do not require the axe RigBuilder or Animated Weapon Grip constraint.", MessageType.Warning);
            return;
        }
        if (gripConstraint) EditorGUILayout.ObjectField("Grip Constraint", gripConstraint, typeof(AnimatedWeaponGripConstraint), true);

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
            using (new EditorGUI.DisabledScope(!gripConstraint || !rigBuilder || (previewWeapon && !previewWeapon.UsesBuiltInWeapon)))
                if (GUILayout.Button("Validate Axe Animation Grips")) ValidateAnimationGrips();
            if (!string.IsNullOrEmpty(validationReport))
            {
                showValidation = EditorGUILayout.Foldout(showValidation, "Last Grip Validation");
                if (showValidation) EditorGUILayout.HelpBox(validationReport, MessageType.Info);
            }

            EditorGUI.BeginChangeCheck();
            var actions = PreviewActions();
            var options = ClipNames.Concat(actions.Select(action => action.State + " / " + action.Id)).ToArray();
            selectedPreview = Mathf.Clamp(selectedPreview, 0, options.Length - 1);
            int next = EditorGUILayout.Popup("Animation", selectedPreview, options);
            if (EditorGUI.EndChangeCheck() || previewClip == null)
            {
                selectedPreview = next;
                SelectClip(FindPreviewClip(next, actions));
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
            DrawHandIK();
        }
    }

    private void DrawEquipmentSelector()
    {
        var equipment = animator.GetComponentInParent<RiftWarriorEquipment>();
        if (previewSkin)
        {
            EditorGUILayout.ObjectField("Skin Profile", previewSkin, typeof(CharacterSkinDefinition), false);
            var choices = new System.Collections.Generic.List<WeaponDefinition>();
            var labels = new System.Collections.Generic.List<string>();
            if (previewSkin.DefaultWeapon) { choices.Add(VisualDefinition(previewSkin.DefaultWeapon)); labels.Add(previewSkin.DefaultWeapon.DisplayName); }
            foreach (var entry in previewSkin.Weapons)
            {
                if (entry == null || !entry.Weapon) continue;
                var visual = VisualDefinition(entry.Weapon);
                if (!choices.Contains(visual)) { choices.Add(visual); labels.Add(entry.Weapon.DisplayName); }
            }
            choices.Add(null); labels.Add("Custom definition");
            int current = choices.IndexOf(previewWeapon);
            if (current < 0) current = choices.Count - 1;
            int next = EditorGUILayout.Popup("Equipment", current, labels.ToArray());
            if (next != current) SetPreviewWeapon(choices[next]);
        }
        else if (equipment)
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
        previewClip = FindPreviewClip(selectedPreview, PreviewActions());
        previewTime = 0;
        if (restart) StartPreview();
    }

    private WeaponDefinition VisualDefinition(WeaponDefinition weapon)
    {
        var entry = previewSkin ? previewSkin.FindWeapon(weapon) : null;
        return entry != null && entry.Presentation ? entry.Presentation : weapon;
    }

    private RuntimeAnimatorController PreviewController()
    {
        if (previewSkin)
        {
            foreach (var entry in previewSkin.Weapons)
                if (entry != null && (entry.Weapon == previewWeapon || entry.Presentation == previewWeapon) && entry.Controller) return entry.Controller;
            return previewSkin.Controller;
        }
        return previewWeapon && previewWeapon.Controller ? previewWeapon.Controller : originalController ? originalController : animator.runtimeAnimatorController;
    }

    private AnimationClip ResolveEquipmentClip(AnimationClip clip)
    {
        if (!clip) return null;
        var overrides = PreviewController() as AnimatorOverrideController;
        if (overrides)
        {
            var pairs = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
            overrides.GetOverrides(pairs);
            foreach (var pair in pairs) if (pair.Key == clip) return pair.Value ? pair.Value : clip;
        }
        return clip;
    }

    private AnimationClip FindClip(string name)
    {
        var controller = PreviewController();
        while (controller is AnimatorOverrideController overrides) controller = overrides.runtimeAnimatorController;
        if (!controller) return null;
        var source = Array.Find(controller.animationClips, clip => clip.name ==
            (name.StartsWith("Unarmed/") ? name.Replace("Unarmed/", "Unarmed-") : "2Hand-Sword-" + name));
        if (previewSkin && previewSkin.PreviewAssignedAttacksOnly && name.StartsWith("Attack", StringComparison.Ordinal)
            && !PreviewActions().Any(action => action.TimingClip == source)) return null;
        return ResolveEquipmentClip(source);
    }

    private SkinAction[] PreviewActions() => previewSkin
        ? previewSkin.Actions.Where(action => action != null && action.TimingClip).ToArray()
        : Array.Empty<SkinAction>();

    private AnimationClip FindPreviewClip(int index, SkinAction[] actions)
    {
        if (index < ClipNames.Length) return FindClip(ClipNames[index]);
        int actionIndex = index - ClipNames.Length;
        return actionIndex < actions.Length ? ResolveEquipmentClip(actions[actionIndex].TimingClip) : null;
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
            foreach (string name in new[] { "Socket", "UseGripFrame", "Position", "Rotation", "Scale", "StoredBone", "StoredPosition", "StoredRotation" })
                EditorGUILayout.PropertyField(settings.FindProperty(name));
            if (EditorGUI.EndChangeCheck())
            {
                settings.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(part);
                SamplePreview();
            }
        }
    }

    private HandPosePreset handPosePreset;
    private HumanBodyBones presetHand = HumanBodyBones.LeftHand;

    private void DrawHandPosePreset(WeaponDefinition part)
    {
        handPosePreset = (HandPosePreset)EditorGUILayout.ObjectField("Hand Pose Preset", handPosePreset, typeof(HandPosePreset), false);
        presetHand = EditorGUILayout.Popup("Capture Hand", presetHand == HumanBodyBones.LeftHand ? 0 : 1,
            new[] { "Left", "Right" }) == 0 ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
        using (new EditorGUI.DisabledScope(!previewActive || !animator))
            if (GUILayout.Button("Save Current Hand Pose"))
            {
                var path = EditorUtility.SaveFilePanelInProject("Save Hand Pose", "HandPose", "asset", "Choose a location for the preset.");
                if (!string.IsNullOrEmpty(path))
                {
                    var preset = CreateInstance<HandPosePreset>();
                    preset.Hand = presetHand;
                    preset.SourceRig = animator.avatar ? animator.avatar.name : animator.name;
                    preset.SourceAvatar = animator.avatar;
                    var rotations = new System.Collections.Generic.List<EquipmentBoneRotation>();
                    int first = (int)(presetHand == HumanBodyBones.LeftHand ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal);
                    for (int i = first; i < first + 15; i++)
                    {
                        var bone = animator.GetBoneTransform((HumanBodyBones)i);
                        if (bone) rotations.Add(new EquipmentBoneRotation { Bone = (HumanBodyBones)i, Rotation = bone.localRotation });
                    }
                    preset.Fingers = rotations.ToArray();
                    AssetDatabase.CreateAsset(preset, path);
                    handPosePreset = preset;
                }
            }
        if (!handPosePreset) return;
        EditorGUILayout.LabelField("Source Rig", handPosePreset.SourceRig);
        bool wrongRig = handPosePreset.SourceAvatar && (!animator || animator.avatar != handPosePreset.SourceAvatar);
        if (wrongRig)
            EditorGUILayout.HelpBox("Choose a preset made for this character's Avatar.", MessageType.Warning);
        using (new EditorGUI.DisabledScope(handPosePreset.Fingers.Length == 0 || wrongRig))
            if (GUILayout.Button("Apply Preset to Selected Animation"))
            {
                Undo.RecordObject(part, "Apply Hand Pose Preset");
                var entry = Array.Find(part.AnimationIK, item => item != null && item.Clip == previewClip);
                if (entry == null)
                {
                    entry = new EquipmentAnimationIK { Clip = previewClip };
                    part.AnimationIK = part.AnimationIK.Append(entry).ToArray();
                }
                var poses = new System.Collections.Generic.List<EquipmentBoneRotation>(
                    entry.FingerOverrides ?? Array.Empty<EquipmentBoneRotation>());
                foreach (var rotation in handPosePreset.Fingers)
                {
                    int index = poses.FindIndex(item => item.Bone == rotation.Bone);
                    if (index < 0) poses.Add(rotation);
                    else poses[index] = rotation;
                }
                entry.FingerOverrides = poses.ToArray();
                EditorUtility.SetDirty(part);
                AssetDatabase.SaveAssetIfDirty(part);
                SamplePreview();
            }
    }

    private void DrawHandIK()
    {
        if (!previewWeapon || !previewClip) return;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hand IK — selected animation", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Grip positions are wrist targets in weapon model space. Animation offsets only affect the selected clip. Weapon offsets move the item and its hands together; hand offsets adjust the wrist independently. Values are saved to the definition assets.", MessageType.Info);
        var parts = previewWeapon.UsesBuiltInWeapon ? new[] { previewWeapon } : new System.Collections.Generic.List<WeaponDefinition>(previewWeapon.EquippedParts).ToArray();
        foreach (var part in parts)
        {
            EditorGUILayout.LabelField(part.DisplayName, EditorStyles.boldLabel);
            DrawHandPosePreset(part);
            var settings = new SerializedObject(part);
            settings.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(settings.FindProperty("EnableHandIK"));
            EditorGUILayout.PropertyField(settings.FindProperty("HandGrips"), true);
            EditorGUILayout.PropertyField(settings.FindProperty("UseFixedFingerPoses"));
            EditorGUILayout.PropertyField(settings.FindProperty("FingerPoses"), true);
            var entries = settings.FindProperty("AnimationIK");
            int index = Array.FindIndex(part.AnimationIK, item => item != null && item.Clip == previewClip);
            if (index < 0)
            {
                if (GUILayout.Button("Add IK settings for " + previewClip.name))
                {
                    index = entries.arraySize++;
                    var entry = entries.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("Clip").objectReferenceValue = previewClip;
                    entry.FindPropertyRelative("Weight").floatValue = 1;
                    foreach (string field in new[] { "WeaponPosition", "WeaponRotation", "HandPosition", "HandRotation", "ElbowOffset", "LeftHandPosition", "LeftHandRotation", "LeftElbowOffset", "RightThumbRotation", "LeftThumbRotation" })
                        entry.FindPropertyRelative(field).vector3Value = Vector3.zero;
                }
            }
            if (index >= 0) EditorGUILayout.PropertyField(entries.GetArrayElementAtIndex(index), new GUIContent(previewClip.name), true);
            if (EditorGUI.EndChangeCheck() || settings.hasModifiedProperties)
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
        if (Application.isPlaying || animator == null || EditorUtility.IsPersistent(animator) || !animator.isHuman || previewClip == null) return;
        StopPreview();
        originalController = animator.runtimeAnimatorController;
        originalCullingMode = animator.cullingMode;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var avatar = animator.GetComponentInParent<WarriorAvatar>();
        builtInWeapon = avatar ? avatar.EquippedWeapon : null;
        builtInWasActive = builtInWeapon && builtInWeapon.activeSelf;
        previewRigs = animator.GetComponentsInChildren<UnityEngine.Animations.Rigging.Rig>(true);
        previewRigWeights = Array.ConvertAll(previewRigs, rig => rig.weight);
        previewArms = animator.GetComponentsInChildren<TwoBoneIKConstraint>(true);
        previewArmWeights = Array.ConvertAll(previewArms, arm => arm.weight);
        previewRenderers = animator.GetComponentsInChildren<Renderer>(true);
        previewRendererEnabled = Array.ConvertAll(previewRenderers, renderer => renderer.enabled);
        previewHandFollow = gripConstraint ? gripConstraint.data.rightHandFollow : 0;
        previewBackWeight = gripConstraint ? gripConstraint.data.backWeight : 0;
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
            if (rigBuilder) rigBuilder.Clear();
            if (previewWeapon)
            {
                previewEquipmentRoot = new GameObject("Weapon equipment preview") { hideFlags = HideFlags.HideAndDontSave };
                previewEquipmentRoot.transform.SetParent(animator.transform.parent, false);
                equipmentPreview = previewEquipmentRoot.AddComponent<CharacterWeaponPresentation>();
                equipmentPreview.Animator = animator;
                equipmentPreview.BuiltInWeapon = builtInWeapon;
                equipmentPreview.Equip(previewWeapon);
                animator.runtimeAnimatorController = PreviewController();
                foreach (var item in previewEquipmentRoot.GetComponentsInChildren<Transform>(true))
                    item.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            previewGraph = PlayableGraph.Create("Weapon IK Preview");
            previewGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            clipPlayable = AnimationClipPlayable.Create(previewGraph, previewClip);
            clipPlayable.SetApplyFootIK(false);
            // An override can reuse an unarmed clip in a drawn locomotion slot.
            var actions = PreviewActions();
            string motionName = previewClip == FindPreviewClip(selectedPreview, actions)
                ? (selectedPreview < ClipNames.Length ? ClipNames[selectedPreview] : actions[selectedPreview - ClipNames.Length].State)
                : previewClip.name;
            string presentationName = motionName.StartsWith("Unarmed/") || motionName.StartsWith("Unarmed-") ? "WeaponHolstered"
                : motionName.Contains("Unsheath-") ? "WeaponDrawing"
                : motionName.Contains("Sheath-") ? "WeaponSheathing" : "WeaponDrawn";
            presentationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ROM/Animation/WarriorWeaponStates/" + presentationName + ".anim");
            presentationClip = ResolveEquipmentClip(presentationClip);
            Playable source = clipPlayable;
            // Socket equipment only needs the stored-weight curve sampled below.
            if (presentationClip != null && gripConstraint)
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
            output.SetSourcePlayable(rigBuilder ? rigBuilder.BuildPreviewGraph(previewGraph, source) : source);
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
        if (equipmentPreview) equipmentPreview.BeginPoseEvaluation();
        clipPlayable.SetTime(previewTime);
        if (presentationPlayable.IsValid()) presentationPlayable.SetTime(Mathf.Min(previewTime, presentationClip.length));
        if (rigBuilder) rigBuilder.UpdatePreviewGraph(previewGraph);
        previewGraph.Evaluate(0f);
        if (equipmentPreview)
        {
            float stored = 0;
            if (presentationClip)
                foreach (var binding in AnimationUtility.GetCurveBindings(presentationClip))
                    if (binding.type == typeof(Animator) && binding.propertyName == "EquipmentBackWeight")
                    { stored = AnimationUtility.GetEditorCurve(presentationClip, binding).Evaluate(previewTime); break; }
            equipmentPreview.RefreshPose(stored, previewClip, previewTime / Mathf.Max(.0001f, previewClip.length));
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
        if (hadPreview && animator)
        {
            animator.runtimeAnimatorController = originalController;
            animator.cullingMode = originalCullingMode;
        }
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
        if (!animator || !animator.isHuman || !gripConstraint || !gripConstraint.data.IsValid())
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
                    Vector3 leftPosition = data.leftTarget.position, rightPosition = data.rightTarget.position;
                    Quaternion leftRotation = data.leftTarget.rotation, rightRotation = data.rightTarget.rotation;
                    if (equipmentPreview && previewWeapon && previewWeapon.EnableHandIK)
                    {
                        var model = equipmentPreview.ModelFor(previewWeapon);
                        var settings = equipmentPreview.Settings(previewWeapon, previewClip, previewTime / Mathf.Max(.0001f, previewClip.length));
                        if (model) foreach (var grip in previewWeapon.HandGrips)
                        {
                            bool left = grip.Hand == HumanBodyBones.LeftHand;
                            var position = model.transform.TransformPoint(grip.Position + (left ? settings.LeftHandPosition : settings.HandPosition));
                            var rotation = model.transform.rotation * Quaternion.Euler(grip.Rotation) * Quaternion.Euler(left ? settings.LeftHandRotation : settings.HandRotation);
                            if (left) { leftPosition = position; leftRotation = rotation; }
                            else { rightPosition = position; rightRotation = rotation; }
                        }
                    }
                    leftError = Mathf.Max(leftError, Vector3.Distance(data.leftHand.position, leftPosition));
                    rightError = Mathf.Max(rightError, Vector3.Distance(data.rightHand.position, rightPosition));
                    leftAngle = Mathf.Max(leftAngle, Quaternion.Angle(data.leftHand.rotation, leftRotation));
                    rightAngle = Mathf.Max(rightAngle, Quaternion.Angle(data.rightHand.rotation, rightRotation));
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
