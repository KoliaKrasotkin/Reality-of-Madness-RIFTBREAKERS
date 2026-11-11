using System.Collections.Generic;
using System.IO;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROM.EditorTools
{
    [CustomEditor(typeof(CharacterSkinController))]
    public sealed class CharacterSkinControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var skins = (CharacterSkinController)target;
            EditorGUILayout.HelpBox("Each skin owns its animation overrides and visual rig. Use the common locomotion controller; clone that controller only when adding new states/transitions. Weapon presentation entries never change gameplay stats.", MessageType.Info);
            if (!Application.isPlaying && GUILayout.Button("Assign starter profile to current character"))
            {
                CharacterSkinAuthoring.CreateStarterProfiles();
                var motor = skins.GetComponent<WarriorAvatar>();
                var paladin = skins.GetComponent<PaladinPrototypePresentation>();
                var animator = paladin ? paladin.Animator : motor ? motor.Animator : skins.GetComponentInChildren<Animator>();
                if (!animator || animator.transform == skins.transform) return;
                Undo.RecordObject(skins, "Assign character skin");
                skins.Skins = new[] { new CharacterSkinController.SkinSlot {
                    Definition = AssetDatabase.LoadAssetAtPath<CharacterSkinDefinition>(CharacterSkinAuthoring.Folder + (paladin ? "/PaladinSkin.asset" : "/WarriorSkin.asset")),
                    ExistingAnimator = animator, ExistingVisualRoot = animator.gameObject, BuiltInWeapon = motor ? motor.EquippedWeapon : null } };
                skins.InitialIndex = 0;
                EditorUtility.SetDirty(skins);
                PrefabUtility.RecordPrefabInstancePropertyModifications(skins);
            }
            if (Application.isPlaying)
                for (int i = 0; i < skins.Skins.Length; i++)
                {
                    int index = i;
                    if (skins.Skins[i]?.Definition && GUILayout.Button("Use " + skins.Skins[i].Definition.name)) skins.SelectSkin(index);
                }
        }
    }

    public static class CharacterSkinAuthoring
    {
        public const string Folder = "Assets/ROM/Characters/Skins";
        public const string BasePath = "Assets/ROM/Characters/Warrior/WarriorLocomotion.controller";

        [MenuItem("ROM/Characters/Create Starter Skin Profiles")]
        public static void CreateStarterProfiles()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/ROM/Characters", "Skins");
            var common = AssetDatabase.LoadAssetAtPath<AnimatorController>(BasePath);
            if (!common) throw new FileNotFoundException(BasePath);
            Create("Warrior", 1, common, "Assets/ROM/Combat/Warrior.asset", "Assets/ROM/Characters/Warrior/WarriorAxe.asset");
            Create("Paladin", PaladinPrototypeSettings.Skin, common, "Assets/ROM/Characters/PaladinPrototype/PaladinPrototypeClass.asset", "Assets/ROM/Characters/PaladinPrototype/PaladinPracticeSword.asset");
            AssetDatabase.SaveAssets();
        }

        static void Create(string name, int id, AnimatorController common, string classPath, string weaponPath)
        {
            string path = Folder + "/" + name + "Skin.asset";
            if (AssetDatabase.LoadAssetAtPath<CharacterSkinDefinition>(path)) return;
            var controller = new AnimatorOverrideController(common) { name = name + "Animations" };
            var definition = ScriptableObject.CreateInstance<CharacterSkinDefinition>();
            definition.Id = name.ToLowerInvariant(); definition.SkinId = id; definition.Controller = controller;
            definition.DefaultWeapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(weaponPath);
            var combat = AssetDatabase.LoadAssetAtPath<CombatClassDefinition>(classPath);
            var actions = new List<SkinAction>();
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            controller.GetOverrides(overrides);
            if (combat)
            {
                AddAction(combat.BasicAttack, 0);
                for (int i = 0; i < combat.Abilities.Length; i++) AddAction(combat.Abilities[i], i + 1);
            }
            definition.Actions = actions.ToArray();
            if (name == "Warrior")
            {
                var sword = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/ROM/Characters/Warrior/WarriorSword.asset");
                if (sword && sword.Controller)
                {
                    var swordController = new AnimatorOverrideController(common) { name = "WarriorSwordAnimations" };
                    if (sword.Controller is AnimatorOverrideController source)
                    {
                        var clips = new List<KeyValuePair<AnimationClip, AnimationClip>>(); source.GetOverrides(clips);
                        swordController.ApplyOverrides(clips);
                    }
                    AssetDatabase.CreateAsset(swordController, Folder + "/WarriorSwordAnimations.overrideController");
                    definition.Weapons = new[] { new SkinWeaponPresentation { Weapon = sword, Controller = swordController } };
                }
            }
            controller.ApplyOverrides(overrides);
            AssetDatabase.CreateAsset(controller, Folder + "/" + name + "Animations.overrideController");
            AssetDatabase.CreateAsset(definition, path);

            void AddAction(AbilityDefinition ability, int slot)
            {
                if (!ability) return;
                string stateName = "Attack" + slot;
                AnimationClip original = null;
                foreach (var state in common.layers[0].stateMachine.states)
                    if (state.state.name == stateName) original = state.state.motion as AnimationClip;
                if (!original) return;
                if (ability.Animation)
                    for (int i = 0; i < overrides.Count; i++)
                        if (overrides[i].Key == original) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, ability.Animation);
                actions.Add(new SkinAction { Id = ability.Id, State = "Base Layer." + stateName, TimingClip = ability.Animation ? ability.Animation : original, SpeedParameter = "AttackRate" });
            }
        }
    }
}
