using System;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ROM.EditorTools
{
    public sealed class MenuCharacterRigWindow : EditorWindow
    {
        const string WeaponSocket = "Menu Weapon Socket";
        static readonly string[] Characters = { "Warrior", "Mage", "Paladin" };
        static readonly string[] Weapons =
        {
            "Warrior/WarriorAxe", "Mage/MageScepter", "Skins/PaladinSwordPresentation"
        };

        int character;
        AnimationClip source;

        [MenuItem("Rift/UI/Menu Characters/Animation and Rigging")]
        static void Open() => GetWindow<MenuCharacterRigWindow>("Menu Character Rig");

        string PrefabPath => MenuCharacterAuthoring.Folder + "/" + Characters[character] + "Menu.prefab";

        void OnGUI()
        {
            character = EditorGUILayout.Popup("Character", character, Characters);
            source = (AnimationClip)EditorGUILayout.ObjectField("Source Animation", source, typeof(AnimationClip), false);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Open Menu Prefab"))
                {
                    var stage = PrefabStageUtility.OpenPrefab(PrefabPath);
                    var animator = stage?.prefabContentsRoot.GetComponentInChildren<Animator>(true);
                    if (animator) Selection.activeGameObject = animator.gameObject;
                }
                if (GUILayout.Button("Attach Character Weapon")) AttachWeapon(character);
                if (GUILayout.Button("Attach All Three Weapons"))
                    for (int i = 0; i < Characters.Length; i++) AttachWeapon(i);
                using (new EditorGUI.DisabledScope(!source))
                    if (GUILayout.Button("Create Editable Menu Animation")) CreateAnimation();
                if (character == 1 && GUILayout.Button("Bake Mage Hand Presets Into Menu Clip"))
                    BakeMageHands();
                if (GUILayout.Button("Open Animation Window")) EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
            }
            EditorGUILayout.HelpBox("Attach the weapon first. Open the prefab, select its Animator object and use Animation to edit the dedicated clip. Key the wrist, fingers and Menu Weapon Socket as needed.", MessageType.Info);
        }

        static void AttachWeapon(int index)
        {
            string path = MenuCharacterAuthoring.Folder + "/" + Characters[index] + "Menu.prefab";
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == path)
            {
                AttachWeapon(stage.prefabContentsRoot, index);
                EditorSceneManager.MarkSceneDirty(stage.scene);
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                AttachWeapon(root, index);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void AttachWeapon(GameObject root, int index)
        {
            var data = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/ROM/Characters/" + Weapons[index] + ".asset");
            var animator = root.GetComponentInChildren<Animator>(true);
            if (!data || !animator || !animator.isHuman) throw new InvalidOperationException("Missing weapon or humanoid Animator.");
            var hand = animator.GetBoneTransform(data.UsesBuiltInWeapon ? HumanBodyBones.RightHand : data.Socket);
            if (!hand) throw new InvalidOperationException("Missing weapon hand.");
            if (hand.Find(WeaponSocket)) return;

            GameObject weapon = null;
            if (data.UsesBuiltInWeapon)
            {
                foreach (var node in root.GetComponentsInChildren<Transform>(true))
                    if (node.name == "Equipped Warrior Axe") { weapon = node.gameObject; break; }
            }
            else if (data.Model)
                weapon = (GameObject)PrefabUtility.InstantiatePrefab(data.Model, root.scene);

            if (!weapon) throw new InvalidOperationException("Missing menu weapon model.");
            var socket = new GameObject(WeaponSocket).transform;
            socket.SetParent(hand, false);
            Undo.RegisterCreatedObjectUndo(socket.gameObject, "Create menu weapon socket");
            if (data.UsesBuiltInWeapon)
            {
                Undo.SetTransformParent(weapon.transform, socket, "Attach menu weapon");
                Undo.RecordObjects(new UnityEngine.Object[] { socket, weapon.transform }, "Set menu weapon grip");
                socket.localPosition = weapon.transform.localPosition;
                socket.localRotation = weapon.transform.localRotation;
                weapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else
            {
                var rootScale = root.transform.lossyScale;
                var handScale = hand.lossyScale;
                socket.localScale = new Vector3(rootScale.x / handScale.x, rootScale.y / handScale.y, rootScale.z / handScale.z);
                var grip = data.UseGripFrame ? WeaponGripSpace.Rotation(animator, data.Socket) : hand.rotation;
                var rotation = Quaternion.Euler(data.Rotation);
                var position = hand.position + grip * Vector3.Scale(data.Position, rootScale);
                foreach (var attachment in data.HandGrips)
                {
                    if (attachment == null || !attachment.DrivesSocket || attachment.Hand != data.Socket) continue;
                    rotation *= Quaternion.Inverse(Quaternion.Euler(attachment.Rotation));
                    position -= grip * rotation * Vector3.Scale(attachment.Position, Vector3.Scale(rootScale, data.Scale));
                    break;
                }
                socket.SetPositionAndRotation(position, grip * rotation);
                Undo.RegisterCreatedObjectUndo(weapon, "Create menu weapon");
                Undo.SetTransformParent(weapon.transform, socket, "Attach menu weapon");
                weapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                weapon.transform.localScale = data.Scale;
            }
            weapon.SetActive(true);
        }

        [MenuItem("Rift/UI/Menu Characters/Generate Paladin Sword Idle")]
        public static void GeneratePaladinIdle()
        {
            const int paladin = 2;
            string prefix = MenuCharacterAuthoring.Folder + "/PaladinMenu_SwordIdle";
            string prefabPath = MenuCharacterAuthoring.Folder + "/PaladinMenu.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var sourceController = prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController;
            var idle = Array.Find(sourceController.animationClips, c =>
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c)) == "276ed19a76505ab49bb124def1d5947a");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(prefix + ".anim");
            if (!clip && !idle) throw new InvalidOperationException("Paladin menu idle is missing.");
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                AttachWeapon(root, paladin);
                var animator = root.GetComponentInChildren<Animator>(true);
                var preset = AssetDatabase.LoadAssetAtPath<HandPosePreset>(
                    "Assets/ROM/Characters/Paladin/HandPoses/RightHand_WeaponGrip_Paladin.asset");
                if (!preset || preset.SourceAvatar != animator.avatar)
                    throw new InvalidOperationException("Paladin grip preset does not match the menu avatar.");
                if (!clip)
                {
                    clip = Instantiate(idle);
                    clip.name = "Paladin Menu Sword Idle";
                    float duration = clip.length;
                    foreach (var finger in preset.Fingers)
                    {
                        var bone = animator.GetBoneTransform(finger.Bone);
                        if (!bone) throw new InvalidOperationException("Missing finger bone: " + finger.Bone);
                        string path = AnimationUtility.CalculateTransformPath(bone, animator.transform);
                        for (int axis = 0; axis < 4; axis++)
                            AnimationUtility.SetEditorCurve(clip,
                                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + "xyzw"[axis]),
                                AnimationCurve.Constant(0f, duration, finger.Rotation[axis]));
                    }
                    clip.EnsureQuaternionContinuity();
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                    AssetDatabase.CreateAsset(clip, prefix + ".anim");
                }
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(prefix + ".controller");
                if (!controller)
                    controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(prefix + ".controller", clip);
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("PALADIN_MENU_IDLE_READY", clip);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        void BakeMageHands()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            var animator = stage != null && stage.assetPath == PrefabPath
                ? stage.prefabContentsRoot.GetComponentInChildren<Animator>(true) : null;
            var clips = animator && animator.runtimeAnimatorController
                ? animator.runtimeAnimatorController.animationClips : Array.Empty<AnimationClip>();
            if (clips.Length != 1 || !AssetDatabase.GetAssetPath(clips[0]).StartsWith(
                MenuCharacterAuthoring.Folder + "/MageMenu_Presentation", StringComparison.Ordinal)
                || !AssetDatabase.GetAssetPath(clips[0]).EndsWith(".anim", StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("Mage hand presets", "Open MageMenu and create its editable menu animation first.", "OK");
                return;
            }

            var clip = clips[0];
            var bindings = new System.Collections.Generic.List<EditorCurveBinding>();
            var curves = new System.Collections.Generic.List<AnimationCurve>();
            foreach (var name in new[] { "RightHand_WeaponGrip_Mage", "LeftHand_Relaxed_Mage" })
            {
                var preset = AssetDatabase.LoadAssetAtPath<HandPosePreset>(
                    "Assets/ROM/Characters/Mage/HandPoses/" + name + ".asset");
                if (!preset || preset.SourceAvatar != animator.avatar)
                    throw new InvalidOperationException("Preset avatar does not match MageMenu.");
                foreach (var finger in preset.Fingers)
                {
                    var bone = animator.GetBoneTransform(finger.Bone);
                    if (!bone) throw new InvalidOperationException("Missing finger bone: " + finger.Bone);
                    string path = AnimationUtility.CalculateTransformPath(bone, animator.transform);
                    for (int axis = 0; axis < 4; axis++)
                    {
                        bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + "xyzw"[axis]));
                        curves.Add(AnimationCurve.Constant(0f, clip.length, finger.Rotation[axis]));
                    }
                }
            }

            Undo.RecordObject(clip, "Bake Mage hand presets");
            AnimationUtility.SetEditorCurves(clip, bindings.ToArray(), curves.ToArray());
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            var overlay = animator.GetComponent<ROM.UI.Sprint01.MenuHandPose>();
            if (overlay)
            {
                Undo.RecordObject(overlay, "Disable Mage hand pose overlay");
                overlay.enabled = false;
                EditorSceneManager.MarkSceneDirty(stage.scene);
            }
            Selection.activeGameObject = animator.gameObject;
            EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
        }

        void CreateAnimation()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || stage.assetPath != PrefabPath)
                stage = PrefabStageUtility.OpenPrefab(PrefabPath);
            if (stage == null) return;
            var animator = stage.prefabContentsRoot.GetComponentInChildren<Animator>(true);
            if (!animator) return;
            string prefix = MenuCharacterAuthoring.Folder + "/" + Characters[character] + "Menu_Presentation";
            var clip = Instantiate(source);
            clip.name = Characters[character] + "Menu Presentation";
            AssetDatabase.CreateAsset(clip, AssetDatabase.GenerateUniqueAssetPath(prefix + ".anim"));
            var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(
                AssetDatabase.GenerateUniqueAssetPath(prefix + ".controller"), clip);
            foreach (var parameter in new[] { "MoveX", "MoveY" }) controller.AddParameter(parameter, AnimatorControllerParameterType.Float);
            foreach (var parameter in new[] { "Grounded", "Dead" }) controller.AddParameter(parameter, AnimatorControllerParameterType.Bool);
            Undo.RecordObject(animator, "Assign menu presentation animation");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            EditorSceneManager.MarkSceneDirty(stage.scene);
            Selection.activeGameObject = animator.gameObject;
            EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
        }
    }
}
