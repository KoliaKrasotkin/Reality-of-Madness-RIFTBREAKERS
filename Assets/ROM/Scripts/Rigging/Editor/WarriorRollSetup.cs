using System;
using System.Linq;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class WarriorRollSetup
{
    public const string ClipPath = "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/2Hand-Sword/RPG-Character@2Hand-Sword-DiveRoll-Forward1.max.FBX";

    public const string UnarmedClipPath = "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/Unarmed/RPG-Character@Unarmed-DiveRoll-Forward1.FBX";

    [MenuItem("ROM/Animation Rigging/Configure Warrior Forward Roll")]
    public static void Configure() => Configure(false);

    public static void Configure(bool tuneMovement)
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<AnimationClip>()
            .Single(c => c.name == "2Hand-Sword-DiveRoll-Forward1");
        var unarmedClip = AssetDatabase.LoadAllAssetsAtPath(UnarmedClipPath).OfType<AnimationClip>()
            .Single(c => c.name == "Unarmed-DiveRoll-Forward1");
        if (!unarmedClip.isHumanMotion || unarmedClip.isLooping) throw new InvalidOperationException("A non-looping Humanoid unarmed roll is required.");
        if (!clip.isHumanMotion || clip.isLooping) throw new InvalidOperationException("A non-looping Humanoid roll is required.");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(WarriorWeaponStateSetup.ControllerPath);
        foreach (string parameter in new[] { "Rolling", "RollArmed" })
            if (!controller.parameters.Any(p => p.name == parameter)) controller.AddParameter(parameter, AnimatorControllerParameterType.Bool);
        if (!controller.parameters.Any(p => p.name == "RollSpeed"))
        {
            controller.AddParameter("RollSpeed", AnimatorControllerParameterType.Float);
            var parameters = controller.parameters;
            parameters.Single(p => p.name == "RollSpeed").defaultFloat = 1f;
            controller.parameters = parameters;
        }
        if (tuneMovement)
        {
            var parameters = controller.parameters;
            parameters.Single(p => p.name == "RollSpeed").defaultFloat = 1f;
            controller.parameters = parameters;
        }
        var machine = controller.layers[0].stateMachine;
        foreach (bool rollArmed in new[] { true, false })
        {
            string rollName = rollArmed ? "RollForward" : "UnarmedRollForward";
            var roll = machine.states.FirstOrDefault(s => s.state.name == rollName).state
                ?? machine.AddState(rollName, new Vector3(rollArmed ? -250 : -500, -180));
            roll.motion = rollArmed ? clip : unarmedClip;
            roll.speed = 1;
            roll.speedParameter = "RollSpeed";
            roll.speedParameterActive = true;
            foreach (var old in roll.transitions) roll.RemoveTransition(old);
            foreach (bool armed in new[] { true, false })
            foreach (bool grounded in new[] { true, false })
            {
                string destination = armed ? (grounded ? "Locomotion" : "Air") : (grounded ? "UnarmedLocomotion" : "UnarmedAir");
                var done = roll.AddTransition(machine.states.Single(s => s.state.name == destination).state);
                // The synchronized roll timer controls completion, including late network arrival.
                done.hasExitTime = false;
                done.hasFixedDuration = true;
                done.duration = .04f;
                done.AddCondition(AnimatorConditionMode.IfNot, 0, "Rolling");
                done.AddCondition(armed ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "RollArmed");
                done.AddCondition(grounded ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "Grounded");
            }
            EditorUtility.SetDirty(roll);
        }
        EditorUtility.SetDirty(machine);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);

        var root = PrefabUtility.LoadPrefabContents(WarriorWeaponStateSetup.PrefabPath);
        try
        {
            var avatar = root.GetComponent<WarriorAvatar>();
            if (avatar.RollAnimation != clip || avatar.UnarmedRollAnimation != unarmedClip || tuneMovement)
            {
                avatar.RollAnimation = clip;
                avatar.UnarmedRollAnimation = unarmedClip;
                if (tuneMovement)
                {
                    avatar.RollPlaybackSpeed = 1f;
                    avatar.RollTravelMultiplier = 1.2f;
                    avatar.RollRecovery = .15f;
                }
                PrefabUtility.SaveAsPrefabAsset(root, WarriorWeaponStateSetup.PrefabPath);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var identity = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorWeaponStateSetup.PrefabPath).GetComponent<Mirror.NetworkIdentity>();
        _ = identity.assetId;
        EditorUtility.SetDirty(identity);
        AssetDatabase.SaveAssetIfDirty(identity);
        Debug.Log("Warrior forward roll configured for armed/unarmed use; CTRL binding is in RiftControls.");
    }
}
