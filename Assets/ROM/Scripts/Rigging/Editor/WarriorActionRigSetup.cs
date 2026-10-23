using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using MultiFPS.Gameplay.RiftCombat;

// Updates only action clips/transitions; preserves the authored prefab and grip rig.
public static class WarriorActionRigSetup
{
    private const string Root = "Assets/ROM/Characters/Warrior/";
    private const string Pack = "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/2Hand-Sword/";
    private static AbilityDefinition[] Abilities()
    {
        var warrior = AssetDatabase.LoadAssetAtPath<CombatClassDefinition>("Assets/ROM/Combat/Warrior.asset");
        return new[] { warrior.BasicAttack }.Concat(warrior.Abilities).ToArray();
    }

    private static AnimationClip Clip(string name)
    {
        string path = Pack + "RPG-Character@2Hand-Sword-" + name + ".FBX";
        var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Single(c => c.name == "2Hand-Sword-" + name);
        if (!clip.isHumanMotion) throw new InvalidOperationException("Humanoid clip required: " + path);
        return clip;
    }

    [MenuItem("ROM/Animation Rigging/Configure Warrior Attacks and Jump")]
    public static void Configure()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "WarriorLocomotion.controller");
        var machine = controller.layers[0].stateMachine;
        AnimatorState State(string name) => machine.states.Single(s => s.state.name == name).state;
        var abilities = Abilities();
        var attacks = Enumerable.Range(1, Mathf.Max(9, abilities.Length)).Select(i => Clip("Attack" + i)).ToArray();
        var jumpClip = Clip("Jump");
        var fallClip = Clip("Fall");
        var landClip = Clip("Land");
        if (!controller.parameters.Any(p => p.name == "DefensiveHold"))
            controller.AddParameter("DefensiveHold", AnimatorControllerParameterType.Bool);
        Undo.RegisterCompleteObjectUndo(controller, "Configure Warrior actions");
        for (int i = 0; i < attacks.Length; i++)
        {
            var state = machine.states.FirstOrDefault(s => s.state.name == "Attack" + i).state
                ?? machine.AddState("Attack" + i, new Vector3(460 + i % 3 * 230, 220 + i / 3 * 80));
            Undo.RecordObject(state, "Set Warrior attack clip");
            state.motion = attacks[i];
            state.speedParameter = "AttackRate";
            state.speedParameterActive = true;
            foreach (var old in state.transitions) state.RemoveTransition(old);
            var done = Transition(state, State("Locomotion"), true);
            done.exitTime = 1f;
            if (i == 6)
            {
                done.hasExitTime = false;
                done.AddCondition(AnimatorConditionMode.IfNot, 0, "DefensiveHold");
            }
            EditorUtility.SetDirty(state);
            if (i < abilities.Length)
            {
                var ability = abilities[i];
                Undo.RecordObject(ability, "Set Warrior ability animation");
                ability.Animation = attacks[i];
                EditorUtility.SetDirty(ability);
                AssetDatabase.SaveAssetIfDirty(ability);
            }
        }

        var locomotion = State("Locomotion");
        var air = State("Air");
        var jump = machine.states.FirstOrDefault(s => s.state.name == "Jump").state
            ?? machine.AddState("Jump", new Vector3(460, 50));
        var land = machine.states.FirstOrDefault(s => s.state.name == "Land").state
            ?? machine.AddState("Land", new Vector3(700, 50));
        jump.motion = jumpClip;
        air.motion = fallClip;
        land.motion = landClip;
        // Re-running this command does not accumulate transitions.
        foreach (var transition in locomotion.transitions.Where(t => t.destinationState == air || t.destinationState == jump).ToArray())
            locomotion.RemoveTransition(transition);
        foreach (var state in new[] { jump, air, land })
            foreach (var transition in state.transitions) state.RemoveTransition(transition);
        Transition(locomotion, jump, false).AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        Transition(jump, land, false).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        Transition(jump, air, true).AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        Transition(air, land, false).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        Transition(land, jump, false).AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        Transition(land, locomotion, true).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        foreach (var state in new[] { locomotion, jump, air, land }) EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(machine);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        WarriorWeaponStateSetup.Configure();
        Debug.Log($"Warrior: configured {attacks.Length} two-handed actions and Jump / Fall / Land. Grip rig preserved.");
    }

    private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, bool exitTime)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = exitTime;
        transition.exitTime = 0.9f;
        transition.hasFixedDuration = true;
        transition.duration = 0.08f;
        return transition;
    }

    [MenuItem("ROM/Animation Rigging/Validate Warrior Animation Grips")]
    public static void Validate()
    {
        var root = PrefabUtility.LoadPrefabContents(Root + "WarriorPlayer.prefab");
        WeaponIKControllerEditor editor = null;
        try
        {
            var controller = root.GetComponentInChildren<WeaponIKController>(true);
            editor = (WeaponIKControllerEditor)UnityEditor.Editor.CreateEditor(controller);
            string report = editor.ValidateAnimationGrips();
            UnityEngine.Object.DestroyImmediate(editor);
            editor = null;
            report += "\n" + ValidateTransitions(controller);
            report += "\n" + ValidateRuntimeRouting(root);
            Directory.CreateDirectory("Documentation/warrior-rebuild");
            File.WriteAllText("Documentation/warrior-rebuild/action-grip-validation.txt", report);
        }
        finally
        {
            if (editor != null) UnityEngine.Object.DestroyImmediate(editor);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static string ValidateRuntimeRouting(GameObject root)
    {
        var avatar = root.GetComponent<WarriorAvatar>();
        var abilities = Abilities();
        var animator = avatar.Animator;
        animator.Rebind();
        animator.Update(0);
        for (int slot = 0; slot < abilities.Length; slot++)
        {
            if (!avatar.PresentAttack(slot, abilities[slot]))
                throw new InvalidOperationException("PresentAttack rejected configured slot " + slot);
            animator.Update(.15f);
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack" + slot))
                throw new InvalidOperationException("PresentAttack routed to wrong state for slot " + slot);
            var special = root.GetComponent<WarriorSpecialMotion>();
            if (special != null && special.IsAnimating)
                throw new InvalidOperationException("LateUpdate special bypassed the rig for slot " + slot);
            animator.SetBool("DefensiveHold", false);
        }
        if (avatar.PresentAttack(-1, abilities[0]) || avatar.PresentAttack(99, abilities[0]))
            throw new InvalidOperationException("PresentAttack accepted an invalid state.");
        if (!avatar.PresentAutoAttack(abilities[0]))
            throw new InvalidOperationException("Autoattack presentation was rejected.");
        var activeSlot = typeof(WarriorAvatar).GetField("activeAttackSlot",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (activeSlot == null || (int)activeSlot.GetValue(avatar) != -1)
            throw new InvalidOperationException("Autoattack must not be cancelled by the ability cast-slot check.");
        animator.Update(.2f);
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack0"))
            throw new InvalidOperationException("Autoattack did not reach Attack0.");
        return $"PASS WarriorAvatar.PresentAttack routes all {abilities.Length} configured actions to their own Animator state without procedural bone overrides. Autoattack uses Attack0 independently of the ability cast slot.";
    }

    private static string ValidateTransitions(WeaponIKController controller)
    {
        var animator = controller.Animator;
        var rig = controller.RigBuilder;
        var graph = PlayableGraph.Create("Warrior Action Validation");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        try
        {
            var playable = AnimatorControllerPlayable.Create(graph, animator.runtimeAnimatorController);
            var output = AnimationPlayableOutput.Create(graph, "Warrior", animator);
            output.SetSourcePlayable(rig.BuildPreviewGraph(graph, playable));
            graph.Play();
            void Step(float seconds)
            {
                for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++)
                {
                    rig.UpdatePreviewGraph(graph);
                    graph.Evaluate(1f / 60f);
                }
            }
            void Expect(string state)
            {
                if (!playable.GetCurrentAnimatorStateInfo(0).IsName(state))
                    throw new InvalidOperationException("Warrior transition failed: expected " + state);
            }
            playable.SetBool("Grounded", true);
            playable.SetBool("WeaponDrawn", true);
            playable.SetFloat("AttackRate", 1f);
            Step(.2f);
            Expect("Locomotion");
            playable.SetBool("Grounded", false);
            Step(.2f);
            Expect("Jump");
            Step(1f);
            Expect("Air");
            playable.SetBool("Grounded", true);
            Step(.15f);
            Expect("Land");
            Step(Clip("Land").length + .2f);
            Expect("Locomotion");
            var abilities = Abilities();
            int attackCount = Mathf.Max(9, abilities.Length);
            for (int i = 0; i < attackCount; i++)
            {
                playable.SetBool("DefensiveHold", i == 6);
                float duration = i < abilities.Length ? abilities[i].CastDuration : Clip("Attack" + (i + 1)).length;
                playable.SetFloat("AttackRate", Clip("Attack" + (i + 1)).length / Mathf.Max(.1f, duration));
                playable.CrossFadeInFixedTime("Attack" + i, .09f, 0, 0);
                Step(.15f);
                Expect("Attack" + i);
                Step(duration + .2f);
                if (i == 6)
                {
                    Expect("Attack6");
                    playable.SetBool("DefensiveHold", false);
                    Step(.2f);
                }
                Expect("Locomotion");
            }
            playable.SetBool("DefensiveHold", true);
            playable.Play("Attack6", 0, 0);
            Step(abilities[6].CastDuration + .4f);
            Expect("Attack6");
            playable.SetBool("DefensiveHold", false);
            Step(.2f);
            Expect("Locomotion");
            return $"PASS Animator transitions: Jump / Air / Land; Attack0-{attackCount - 1}; {abilities.Length} configured gameplay actions; defensive hold and release.";
        }
        finally
        {
            graph.Destroy();
            rig.StopPreview();
        }
    }
}
