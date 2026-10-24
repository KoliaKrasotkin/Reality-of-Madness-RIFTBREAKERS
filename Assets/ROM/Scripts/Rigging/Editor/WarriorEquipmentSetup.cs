using System;
using System.IO;
using System.Linq;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public static class WarriorEquipmentSetup
{
    public const string Weapons = "Assets/ROM/Characters/Warrior";
    public const string Folder = "Assets/ROM/Animation/WarriorEquipment";
    const string BasePath = "Assets/ROM/Characters/Warrior/WarriorLocomotion.controller";
    const string Pack = "Assets/DoubleL/FBX Unity/One Hand Up/";
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
    static T Asset<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
    static AnimationClip Clip(string relative)
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(Pack + relative + ".fbx").OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (!clip || !clip.isHumanMotion) throw new InvalidOperationException("Missing Humanoid clip: " + relative);
        return clip;
    }
    static WeaponDefinition Weapon(string name)
    {
        string path = Weapons + "/" + name + ".asset";
        var weapon = Asset<WeaponDefinition>(path);
        if (!weapon) { weapon = ScriptableObject.CreateInstance<WeaponDefinition>(); AssetDatabase.CreateAsset(weapon, path); }
        return weapon;
    }

    [MenuItem("ROM/Animation Rigging/Configure Warrior Equipment")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before configuring equipment.");
        EnsureFolder(Folder);
        var controller = Asset<AnimatorController>(BasePath);
        if (!controller.parameters.Any(p => p.name == "EquipmentBackWeight"))
            controller.AddParameter("EquipmentBackWeight", AnimatorControllerParameterType.Float);
        var axe = Weapon("WarriorAxe");
        axe.Id = "warrior-axe"; axe.Controller = controller; axe.UsesBuiltInWeapon = true;
        axe.AdditionalEquipment = Array.Empty<WeaponDefinition>();
        var sword = Weapon("WarriorSword");
        var shield = Weapon("WarriorShield");
        shield.Id = "warrior-shield"; shield.DisplayName = "Escudo de Warrior"; shield.WeaponType = "Shield";
        shield.Model = Asset<GameObject>(Weapons + "/WarriorShield.prefab");
        shield.Socket = HumanBodyBones.LeftHand;
        shield.Role = EquipmentRole.Shield;
        shield.DamageMultiplier = shield.AttackSpeed = shield.Reach = 0;
        sword.Model = Asset<GameObject>(Weapons + "/WarriorSword.prefab");
        sword.Socket = HumanBodyBones.RightHand;
        sword.WeaponType = "OneHandSword";
        sword.Id = "warrior-sword"; sword.UsesBuiltInWeapon = false;
        string overridePath = Folder + "/WarriorSwordShield.overrideController";
        var overrides = Asset<AnimatorOverrideController>(overridePath);
        if (!overrides) { overrides = new AnimatorOverrideController(controller); AssetDatabase.CreateAsset(overrides, overridePath); }
        overrides.runtimeAnimatorController = controller;
        // Clear obsolete mappings when this setup is run again.
        var mappings = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
        overrides.GetOverrides(mappings);
        overrides.ApplyOverrides(mappings.Select(p => new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(p.Key, null)).ToList());
        void Map(string source, AnimationClip target)
        {
            if (!controller.animationClips.Any(c => c.name == source)) throw new InvalidOperationException("Unknown base clip: " + source);
            overrides[source] = target;
        }
        Map("2Hand-Sword-Idle", Clip("Movement/Idle/Idle/1Hand_Up_Stand_Idle_A_2"));
        Map("2Hand-Sword-Run-Forward", Clip("Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_InPlace"));
        Map("2Hand-Sword-Run-Backward", Clip("Movement/Run/Base/InPlace/1Hand_Up_Run_A_B_InPlace"));
        Map("2Hand-Sword-Run-Left", Clip("Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_L90_A_InPlace"));
        Map("2Hand-Sword-Run-Right", Clip("Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_R90_A_InPlace"));
        Map("2Hand-Sword-Jump", Clip("Jump/InPlace/1Hand_Up_Jump_B_InPlace"));
        var attacks = new[] {
            "Attack_A/InPlace/1Hand_Up_Attack_A_1_InPlace", "Attack_A/InPlace/1Hand_Up_Attack_A_2_InPlace",
            "Attack_B/InPlace/1Hand_Up_Attack_B_1_InPlace", "Attack_B/InPlace/1Hand_Up_Attack_B_2_InPlace",
            "Attack_A/InPlace/1Hand_Up_Attack_A_3_InPlace", "Attack_B/InPlace/1Hand_Up_Attack_B_3_InPlace",
            "Sheild/Idle/1Hand_Up_Shield_Block_Idle_1", "Attack_B/InPlace/1Hand_Up_Attack_B_2_InPlace",
            "Attack_A/InPlace/1Hand_Up_Attack_A_3_InPlace" };
        for (int i = 0; i < attacks.Length; i++) Map("2Hand-Sword-Attack" + (i + 1), Clip(attacks[i]));
        foreach (string name in new[] { "WeaponDrawn", "WeaponHolstered", "WeaponDrawing", "WeaponSheathing" })
        {
            var source = controller.animationClips.Single(c => c.name == name);
            var copy = Asset<AnimationClip>(Folder + "/SwordShield" + name + ".anim");
            if (!copy) { copy = new AnimationClip(); AssetDatabase.CreateAsset(copy, Folder + "/SwordShield" + name + ".anim"); }
            EditorUtility.CopySerialized(source, copy); copy.name = "SwordShield" + name;
            foreach (var binding in AnimationUtility.GetCurveBindings(copy))
            {
                // Independent hand attachments follow the authored DoubleL wrist poses.
                // The axe's two-hand reach solver and arm targets must have no influence.
                if (binding.propertyName == "m_Weight")
                    AnimationUtility.SetEditorCurve(copy, binding, AnimationCurve.Constant(0, source.length, 0));
                if (binding.propertyName == "m_Data.backWeight")
                    AnimationUtility.SetEditorCurve(copy, EditorCurveBinding.FloatCurve("", typeof(Animator), "EquipmentBackWeight"), AnimationUtility.GetEditorCurve(source, binding));
            }
            Map(name, copy); EditorUtility.SetDirty(copy); AssetDatabase.SaveAssetIfDirty(copy);
        }
        sword.Controller = overrides;
        WarriorSwordIdleSetup.ApplyConfiguredClips(overrides);
        sword.AdditionalEquipment = new[] { shield };
        if (!sword.UseGripFrame || !shield.UseGripFrame) CalibrateAttachments(sword, shield);
        var progression = Asset<WarriorProgressionDefinition>("Assets/ROM/Resources/WarriorProgression.asset");
        progression.Sword = sword;
        EditorUtility.SetDirty(progression); AssetDatabase.SaveAssetIfDirty(progression);
        ConfigurePrefab(axe, sword);
        foreach (var asset in new UnityEngine.Object[] { controller, overrides, axe, sword, shield })
        { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
        Debug.Log("WARRIOR_EQUIPMENT_CONFIGURED: axe and independent right-sword/left-shield WeaponDefinitions.");
    }

    static void ConfigurePrefab(WeaponDefinition axe, WeaponDefinition sword)
    {
        var root = PrefabUtility.LoadPrefabContents(WarriorWeaponStateSetup.PrefabPath);
        try
        {
            var avatar = root.GetComponent<WarriorAvatar>();
            var presenter = root.GetComponent<CharacterWeaponPresentation>() ?? root.AddComponent<CharacterWeaponPresentation>();
            presenter.Animator = avatar.Animator; presenter.BuiltInWeapon = avatar.EquippedWeapon;
            var adapter = root.GetComponent<RiftWarriorEquipment>() ?? root.AddComponent<RiftWarriorEquipment>();
            adapter.AxeWeapon = axe; adapter.SwordShieldWeapon = sword;
            PrefabUtility.SaveAsPrefabAsset(root, WarriorWeaponStateSetup.PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var identity = Asset<GameObject>(WarriorWeaponStateSetup.PrefabPath).GetComponent<Mirror.NetworkIdentity>();
        _ = identity.assetId;
        EditorUtility.SetDirty(identity); AssetDatabase.SaveAssetIfDirty(identity);
    }

    static void CalibrateAttachments(WeaponDefinition sword, WeaponDefinition shield)
    {
        var source = UnityEngine.Object.Instantiate(Asset<GameObject>("Assets/DoubleL/Model/Armature (1).prefab"));
        try
        {
            source.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var animator = source.GetComponentInChildren<Animator>();
            animator.enabled = false;
            void Calibrate(WeaponDefinition weapon, string reference, bool isShield)
            {
                if (weapon.UseGripFrame) return; // Preserve subsequent artist adjustments.
                var model = source.GetComponentsInChildren<Transform>(true).Single(t => t.name == reference);
                var grip = WeaponGripSpace.Rotation(animator, weapon.Socket);
                var instance = UnityEngine.Object.Instantiate(weapon.Model);
                try
                {
                    instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    instance.transform.localScale = Vector3.one;
                    var renderers = instance.GetComponentsInChildren<Renderer>();
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    var size = bounds.size;
                    int axis = isShield ? (size.x < size.y ? (size.x < size.z ? 0 : 2) : (size.y < size.z ? 1 : 2))
                        : (size.x > size.y ? (size.x > size.z ? 0 : 2) : (size.y > size.z ? 1 : 2));
                    Vector3 direction = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
                    if (!isShield && bounds.center[axis] < 0) direction = -direction;
                    var conversion = Quaternion.FromToRotation(direction, isShield ? Vector3.forward : Vector3.up);
                    weapon.Rotation = (Quaternion.Inverse(grip) * model.rotation * conversion).eulerAngles;
                    weapon.Position = isShield ? new Vector3(0, -.06f, .03f) : new Vector3(0, 0, .055f);
                    weapon.StoredPosition = isShield ? new Vector3(0, .05f, -.38f) : new Vector3(.12f, .18f, -.3f);
                    weapon.StoredRotation = ((isShield ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 145)) * conversion).eulerAngles;
                    weapon.UseGripFrame = true;
                    Debug.Log("Calibrated " + weapon.name + " original model bounds: " + bounds);
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            Calibrate(sword, "SM_Wep_Sword_03", false);
            Calibrate(shield, "SM_Wep_Shield_01", true);
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
    }
}
