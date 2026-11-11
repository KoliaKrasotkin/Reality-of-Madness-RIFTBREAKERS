using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ROM.EditorTools
{
    public static class PlayableSkinSetup
    {
        const string Warrior = "Assets/ROM/Characters/Warrior/WarriorPlayer.prefab";
        const string Paladin = "Assets/ROM/Characters/PaladinPrototype/PaladinPlayer.prefab";
        const string PaladinModel = "Assets/ROM/Characters/PaladinPrototype/Models/Paladin.fbx";
        const string Folder = CharacterSkinAuthoring.Folder;

        [MenuItem("ROM/Characters/Configure Warrior and Paladin Skins")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            CharacterSkinAuthoring.CreateStarterProfiles();
            ConfigurePaladinProfile();
            foreach (string path in new[] { Warrior, Paladin })
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try { ConfigureRoot(root, path == Paladin); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            // Keep all unrelated open-stage edits, without saving or discarding them.
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && (stage.assetPath == Warrior || stage.assetPath == Paladin))
                ConfigureRoot(stage.prefabContentsRoot, stage.assetPath == Paladin);
            AssetDatabase.SaveAssets();
        }

        static void ConfigurePaladinProfile()
        {
            var skin = AssetDatabase.LoadAssetAtPath<CharacterSkinDefinition>(Folder + "/PaladinSkin.asset");
            if (!skin.VisualPrefab || skin.VisualPrefab.GetComponent<WarriorAvatar>())
                skin.VisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaladinModel);
            var sword = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/ROM/Characters/PaladinPrototype/PaladinPracticeSword.asset");
            string visualPath = Folder + "/PaladinSwordPresentation.asset";
            var visual = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(visualPath);
            if (!visual)
            {
                visual = UnityEngine.Object.Instantiate(sword); visual.name = "PaladinSwordPresentation";
                // Keep the existing sword attachment, without restoring discarded IK fits.
                var adapter = AssetDatabase.LoadAssetAtPath<GameObject>(Paladin).GetComponent<PaladinPrototypePresentation>();
                visual.Position = adapter.GripPosition; visual.Rotation = adapter.GripRotation; visual.Scale = adapter.GripScale;
                AssetDatabase.CreateAsset(visual, visualPath);
            }
            else { EditorUtility.SetDirty(skin); return; } // Do not replace later manual clip/weapon edits.
            skin.Weapons = new[] { new SkinWeaponPresentation { Weapon = sword, Presentation = visual } };
            var target = (AnimatorOverrideController)skin.Controller;
            var source = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(Folder + "/WarriorSwordAnimations.overrideController");
            var clips = new List<KeyValuePair<AnimationClip, AnimationClip>>(); source.GetOverrides(clips);
            var common = AssetDatabase.LoadAssetAtPath<AnimatorController>(CharacterSkinAuthoring.BasePath);
            // Use authored sword attacks rather than the Paladin QA pose clip.
            // Healing and prayer use the sword guard until they have dedicated clips.
            var neutral = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ROM/Animation/WarriorEquipment/SwordGrip_1Hand_Up_Stand_Idle_A_2.anim");
            foreach (var action in skin.Actions)
            {
                var state = common.layers[0].stateMachine.states.First(s => "Base Layer." + s.state.name == action.State).state;
                var key = (AnimationClip)state.motion;
                action.TimingClip = key;
                if (action.Id == "aldren-healing-light" || action.Id == "aldren-prayer")
                    Replace(key, neutral);
                if (action.Id == "aldren-radiant-shield")
                    Replace(key, AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ROM/Animation/WarriorEquipment/SwordGrip_1Hand_Up_Shield_Block_Idle_1.anim"));
            }
            var unusedAttackClips = common.layers[0].stateMachine.states
                .Where(entry => entry.state.name.StartsWith("Attack", StringComparison.Ordinal)
                    && !skin.Actions.Any(action => action.State == "Base Layer." + entry.state.name))
                .Select(entry => entry.state.motion as AnimationClip)
                .ToHashSet();
            for (int i = 0; i < clips.Count; i++)
                if (unusedAttackClips.Contains(clips[i].Key))
                    clips[i] = new KeyValuePair<AnimationClip, AnimationClip>(clips[i].Key, null);
            target.ApplyOverrides(clips);
            EditorUtility.SetDirty(target); EditorUtility.SetDirty(skin);

            void Replace(AnimationClip key, AnimationClip value)
            {
                if (!value) throw new InvalidOperationException("Missing presentation clip for " + key.name);
                for (int i = 0; i < clips.Count; i++) if (clips[i].Key == key) clips[i] = new KeyValuePair<AnimationClip, AnimationClip>(key, value);
            }
        }

        static void ConfigureRoot(GameObject root, bool paladin)
        {
            var motor = root.GetComponent<WarriorAvatar>();
            var adapter = root.GetComponent<PaladinPrototypePresentation>();
            var animator = paladin ? adapter.Animator : motor.Animator;
            if (!animator || !animator.isHuman || !animator.avatar.isValid) throw new InvalidOperationException("Invalid humanoid: " + root.name);
            var definition = AssetDatabase.LoadAssetAtPath<CharacterSkinDefinition>(Folder + (paladin ? "/PaladinSkin.asset" : "/WarriorSkin.asset"));
            var skins = root.GetComponent<CharacterSkinController>();
            if (!skins) skins = root.AddComponent<CharacterSkinController>();
            var slot = new CharacterSkinController.SkinSlot { Definition = definition, ExistingAnimator = animator, ExistingVisualRoot = animator.gameObject, BuiltInWeapon = motor.EquippedWeapon };
            // Preserve any additional manually configured skins.
            if (skins.Skins.Length == 0) skins.Skins = new[] { slot };
            else
            {
                int index = Array.FindIndex(skins.Skins, s => s != null && s.Definition == definition);
                if (index < 0) { var list = skins.Skins.ToList(); list.Add(slot); skins.Skins = list.ToArray(); index = list.Count - 1; }
                else skins.Skins[index] = slot;
                skins.InitialIndex = index;
            }
            motor.Animator = animator;
            animator.runtimeAnimatorController = definition.Controller;
            var equipment = root.GetComponent<CharacterWeaponPresentation>();
            if (!equipment) equipment = root.AddComponent<CharacterWeaponPresentation>();
            equipment.Animator = animator; equipment.BuiltInWeapon = slot.BuiltInWeapon;
            foreach (var other in root.GetComponentsInChildren<Animator>(true))
                if (other != animator)
                {
                    other.enabled = false;
                    var oldRig = other.GetComponent<UnityEngine.Animations.Rigging.RigBuilder>();
                    if (oldRig) oldRig.enabled = false;
                }
            var ik = root.GetComponent<WeaponIKController>();
            if (ik)
            {
                var serialized = new SerializedObject(ik); serialized.FindProperty("animator").objectReferenceValue = animator; serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var warrior = AssetDatabase.LoadAssetAtPath<GameObject>(Warrior).GetComponent<WarriorAvatar>();
            if (!motor.RollAnimation) motor.RollAnimation = warrior.RollAnimation;
            if (!motor.UnarmedRollAnimation) motor.UnarmedRollAnimation = warrior.UnarmedRollAnimation;
            EditorUtility.SetDirty(skins); EditorUtility.SetDirty(motor); EditorUtility.SetDirty(animator); EditorUtility.SetDirty(equipment);
        }
    }
}
