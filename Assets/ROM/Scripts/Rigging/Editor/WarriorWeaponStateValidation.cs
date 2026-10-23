using System;
using System.IO;
using System.Linq;
using System.Text;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

public static class WarriorWeaponStateValidation
{
    [MenuItem("ROM/Animation Rigging/Validate Warrior Weapon States")]
    public static void Validate()
    {
        var root = PrefabUtility.LoadPrefabContents(WarriorWeaponStateSetup.PrefabPath);
        var graph = PlayableGraph.Create("Warrior Weapon State Validation");
        var avatar = root.GetComponent<WarriorAvatar>();
        var animator = avatar.Animator;
        var rigBuilder = animator.GetComponent<RigBuilder>();
        var rigs = animator.GetComponentsInChildren<Rig>(true);
        var arms = animator.GetComponentsInChildren<TwoBoneIKConstraint>(true);
        var renderers = avatar.EquippedWeapon.GetComponentsInChildren<Renderer>(true);
        var grip = animator.GetComponentInChildren<AnimatedWeaponGripConstraint>(true);
        var gripData = grip.data;
        var carryOffset = gripData.carryPoseCalibrated ? gripData.carryRotation : Quaternion.Inverse(gripData.rightHand.rotation) * gripData.weapon.rotation;
        var report = new StringBuilder();
        try
        {
            void Check(bool condition, string description)
            {
                if (!condition) throw new InvalidOperationException(description);
                report.AppendLine("PASS " + description);
            }
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimatorControllerPlayable.Create(graph, animator.runtimeAnimatorController);
            var output = AnimationPlayableOutput.Create(graph, "Warrior", animator);
            output.SetSourcePlayable(rigBuilder.BuildPreviewGraph(graph, playable));
            graph.Play();
            void Step(float seconds)
            {
                for (int frame = 0; frame < Mathf.CeilToInt(seconds * 60); frame++)
                {
                    rigBuilder.UpdatePreviewGraph(graph);
                    graph.Evaluate(1f / 60f);
                }
            }
            void State(string name) => Check(playable.GetCurrentAnimatorStateInfo(0).IsName(name), "Animator state " + name);
            void Presentation(bool armed)
            {
                Check(rigs.All(r => Mathf.Abs(r.weight - 1) < .001f), "Weapon rig stays active for hand/back attachment");
                Check(arms.All(r => Mathf.Abs(r.weight - (armed ? 1 : 0)) < .001f), "Animator arm IK " + (armed ? "enabled" : "released"));
                Check(renderers.All(r => r.enabled), "Axe stays visible in hand or on back");
                if (!armed)
                    Check(gripData.backSocket && Vector3.Distance(gripData.weapon.position, gripData.backSocket.position) < .002f
                        && Quaternion.Angle(gripData.weapon.rotation, gripData.backSocket.rotation) < .2f,
                        "Holstered axe follows animated chest socket");
            }
            void Carry()
            {
                float scale = Mathf.Abs(gripData.weapon.lossyScale.x);
                Quaternion rotation = gripData.rightHand.rotation * carryOffset;
                Vector3 palm = gripData.rightHand.position + gripData.rightHand.rotation * (gripData.rightPalmOffset * scale);
                Vector3 position = palm - rotation * ((gripData.carryPoseCalibrated ? gripData.carryContact : gripData.rightContact) * scale);
                Check(Vector3.Distance(gripData.weapon.position, position) < .002f && Quaternion.Angle(gripData.weapon.rotation, rotation) < .2f,
                    "Visible transition axe follows the animated right hand");
            }
            float Length(string name) => animator.runtimeAnimatorController.animationClips.Single(c => c.name == name).length;
            float sheath = Length("2Hand-Sword-Sheath-Back-Unarmed");
            float draw = Length("2Hand-Sword-Unsheath-Back-Unarmed");
            var source = animator.runtimeAnimatorController.animationClips.Single(c => c.name == "2Hand-Sword-Unsheath-Back-Unarmed");
            var reverse = animator.runtimeAnimatorController.animationClips.Single(c => c.name == "2Hand-Sword-Sheath-Back-Unarmed");
            Check(Mathf.Abs(sheath - draw) < .001f && source != reverse, "Sheathing uses a separate reversed copy of Unsheath-Back-Unarmed");
            Check(reverse.humanMotion, "Reversed clip retains Humanoid retargeting");
            var savedIdentity = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorWeaponStateSetup.PrefabPath).GetComponent<Mirror.NetworkIdentity>();
            Check(savedIdentity.assetId == Mirror.NetworkIdentity.AssetGuidToUint(new Guid(AssetDatabase.AssetPathToGUID(WarriorWeaponStateSetup.PrefabPath)))
                && savedIdentity.assetId != 0, "Prefab retains its Mirror network asset ID");
            float curveError = 0;
            var bindings = AnimationUtility.GetCurveBindings(source);
            foreach (var binding in bindings)
            {
                var a = AnimationUtility.GetEditorCurve(source, binding);
                var b = AnimationUtility.GetEditorCurve(reverse, binding);
                if (b == null) throw new InvalidOperationException("Missing reversed curve: " + binding.propertyName);
                for (int i = 0; i <= 60; i++)
                {
                    float time = draw * i / 60;
                    curveError = Mathf.Max(curveError, Mathf.Abs(a.Evaluate(time) - b.Evaluate(draw - time)));
                }
            }
            Check(bindings.Length > 0 && curveError < .001f, "All reversed animation curves match source at mirrored time; max error " + curveError);
            playable.SetBool("Grounded", true);
            playable.SetBool("WeaponDrawn", true);
            Step(.3f);
            State("Locomotion");
            Presentation(true);
            playable.SetBool("WeaponDrawn", false);
            Step(.2f);
            State("SheathWeapon");
            playable.Play("SheathWeapon", 0, .35f);
            playable.Play("Sheathing", 1, .35f);
            Step(1f / 60f);
            Carry();
            Check(renderers.All(r => r.enabled), "Axe stays visible while being carried to storage");
            Step(sheath + .3f);
            State("UnarmedLocomotion");
            Presentation(false);
            foreach (var direction in new[] { Vector2.zero, Vector2.up, Vector2.down, Vector2.left, Vector2.right })
            {
                playable.SetFloat("MoveX", direction.x * .5f);
                playable.SetFloat("MoveY", direction.y * .5f);
                Step(.3f);
                State("UnarmedLocomotion");
                Presentation(false);
            }
            playable.SetBool("Grounded", false);
            Step(.2f);
            State("UnarmedJump");
            Step(Length("Unarmed-Jump") + .3f);
            State("UnarmedAir");
            Presentation(false);
            playable.SetBool("Grounded", true);
            Step(.3f);
            State("UnarmedLocomotion");
            playable.SetBool("WeaponDrawn", true);
            Step(.2f);
            State("DrawWeapon");
            playable.Play("DrawWeapon", 0, .4f);
            playable.Play("Drawing", 1, .4f);
            rigBuilder.UpdatePreviewGraph(graph);
            graph.Evaluate(0);
            Carry();
            playable.Play("DrawWeapon", 0, .65f);
            playable.Play("Drawing", 1, .65f);
            Step(1f / 60f);
            Carry();
            Step(draw + .3f);
            State("Locomotion");
            Presentation(true);
            // Reverse either transition while it is still playing.
            playable.SetBool("WeaponDrawn", false);
            Step(.2f);
            playable.SetBool("WeaponDrawn", true);
            Step(draw + .6f);
            State("Locomotion");
            Presentation(true);
            // A defensive pose must not prevent sheathing.
            playable.SetBool("DefensiveHold", true);
            playable.CrossFadeInFixedTime("Attack6", .09f, 0, 0);
            Step(.3f);
            playable.SetBool("WeaponDrawn", false);
            Step(sheath + .6f);
            State("UnarmedLocomotion");
            Presentation(false);
            graph.Destroy();
            rigBuilder.StopPreview();
            // Exercise the public offline API as another script would call it.
            avatar.SetWeaponDrawn(false);
            Check(!avatar.WeaponDrawn && !avatar.CanUseWeapon && !animator.GetBool("WeaponDrawn"), "SetWeaponDrawn(false) updates Animator and blocks weapon use");
            var ability = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/ROM/Combat/BasicAttack.asset");
            Check(!avatar.PresentAttack(0, ability), "Holstered character rejects attack presentation");
            avatar.SetWeaponDrawn(true);
            Check(avatar.WeaponDrawn && animator.GetBool("WeaponDrawn") && !avatar.CanUseWeapon, "SetWeaponDrawn(true) starts drawing and waits before allowing attacks");
            Debug.Log(report.ToString());
        }
        finally
        {
            Directory.CreateDirectory("Documentation/warrior-rebuild");
            File.WriteAllText("Documentation/warrior-rebuild/weapon-state-validation.txt", report.ToString());
            if (graph.IsValid()) graph.Destroy();
            rigBuilder.StopPreview();
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
