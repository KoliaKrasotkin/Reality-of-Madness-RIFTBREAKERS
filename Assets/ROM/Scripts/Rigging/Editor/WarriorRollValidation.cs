using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public static class WarriorRollValidation
{
    [MenuItem("ROM/Animation Rigging/Validate Warrior Forward Roll")]
    public static void Validate()
    {
        var report = new StringBuilder();
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            report.AppendLine("PASS " + message);
        }
        var root = PrefabUtility.LoadPrefabContents(WarriorWeaponStateSetup.PrefabPath);
        var avatar = root.GetComponent<WarriorAvatar>();
        var animator = avatar.Animator;
        var rig = animator.GetComponent<RigBuilder>();
        var grip = animator.GetComponentInChildren<AnimatedWeaponGripConstraint>();
        var graph = PlayableGraph.Create("Warrior Roll Validation");
        GameObject floor = null, wall = null, apiRoot = null, gate = null;
        var previousScene = SceneManager.GetActiveScene();
        Scene physicsScene = default;
        try
        {
            Check(avatar.RollAnimation && avatar.RollAnimation.name == "2Hand-Sword-DiveRoll-Forward1", "Prefab uses requested forward roll clip");
            Check(avatar.UnarmedRollAnimation && avatar.UnarmedRollAnimation.name == "Unarmed-DiveRoll-Forward1", "Prefab uses separate requested Unarmed roll clip");
            Check(Mathf.Abs(avatar.RollPlaybackSpeed - 1) < .001f, "Roll animation plays at natural speed");
            Check(avatar.RollRecovery >= .05f, "Configured minimum recovery is preserved; early CTRL presses are not buffered");
            Check(avatar.RollExitNormalizedTime < 1, "Roll exits before the final recovery tail without speeding up the clip");
            Check(!avatar.RollAnimation.isLooping && avatar.RollAnimation.isHumanMotion, "Roll is a non-looping Humanoid animation");
            var binding = RiftControls.Find("Roll");
            Check(binding.Bindings.Any(b => b.Path == "<Keyboard>/leftCtrl") && binding.Bindings.Any(b => b.Path == "<Keyboard>/rightCtrl") && binding.Continuous,
                "Left/right CTRL binding also matches while CTRL itself is held");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimatorControllerPlayable.Create(graph, animator.runtimeAnimatorController);
            var output = AnimationPlayableOutput.Create(graph, "Roll", animator);
            output.SetSourcePlayable(rig.BuildPreviewGraph(graph, playable));
            graph.Play();
            playable.SetFloat("RollSpeed", avatar.RollPlaybackSpeed);
            void Step(float duration)
            {
                for (int i = 0; i < Mathf.Max(1, Mathf.CeilToInt(duration * 60)); i++)
                {
                    rig.UpdatePreviewGraph(graph);
                    graph.Evaluate(duration == 0 ? 0 : 1f / 60);
                }
            }
            foreach (bool armed in new[] { true, false })
            {
                string rollState = armed ? "RollForward" : "UnarmedRollForward";
                var rollClip = armed ? avatar.RollAnimation : avatar.UnarmedRollAnimation;
                playable.SetBool("WeaponDrawn", armed);
                playable.SetBool("RollArmed", armed);
                playable.SetBool("Grounded", true);
                playable.SetBool("Rolling", false);
                playable.Play(armed ? "Locomotion" : "UnarmedLocomotion", 0, 0);
                playable.Play(armed ? "Drawn" : "Holstered", 1, 0);
                Step(.2f);
                playable.SetBool("Rolling", true);
                playable.CrossFadeInFixedTime(rollState, .06f, 0, 0);
                Step(.1f);
                Check(playable.GetCurrentAnimatorStateInfo(0).IsName(rollState), "Enter roll " + (armed ? "armed" : "unarmed"));
                var info = playable.GetCurrentAnimatorStateInfo(0);
                Check(Mathf.Abs(info.speedMultiplier - avatar.RollPlaybackSpeed) < .001f, "Animator speed matches roll movement timer: " + rollState);
                Check(playable.GetCurrentAnimatorClipInfo(0).Single().clip == rollClip, "Animator plays the matching clip: " + rollClip.name);
                playable.CrossFadeInFixedTime(rollState, 0, 0, rollClip.length * .25f / avatar.RollPlaybackSpeed);
                Step(1f / 60);
                float seek = playable.GetCurrentAnimatorStateInfo(0).normalizedTime;
                float expectedSeek = .25f + avatar.RollPlaybackSpeed / (60 * rollClip.length);
                Check(Mathf.Abs(seek - expectedSeek) < .035f,
                    "Network seek follows state time; normalized=" + seek.ToString("F3"));
                float error = 0;
                for (int sample = 0; sample <= 60; sample++)
                {
                    playable.Play(rollState, 0, sample * .99f / 60);
                    Step(0);
                    var data = grip.data;
                    if (armed)
                        error = Mathf.Max(error, Vector3.Distance(data.leftHand.position, data.leftTarget.position), Vector3.Distance(data.rightHand.position, data.rightTarget.position));
                    else
                        error = Mathf.Max(error, Vector3.Distance(data.weapon.position, data.backSocket.position));
                }
                Check(error < .002f, (armed ? "Two-hand grip" : "Back attachment") + " throughout 61 roll samples; max error " + (error * 1000).ToString("F2") + " mm");
                Step(rollClip.length / avatar.RollPlaybackSpeed + .2f);
                Check(playable.GetCurrentAnimatorStateInfo(0).IsName(rollState), "Roll does not exit before movement timer finishes");
                playable.SetBool("Rolling", false);
                Step(.25f);
                Check(playable.GetCurrentAnimatorStateInfo(0).IsName(armed ? "Locomotion" : "UnarmedLocomotion"), "Return to matching locomotion after roll");
                playable.SetBool("Rolling", true);
                playable.Play(rollState, 0, .99f);
                playable.SetBool("Grounded", false);
                Step(.1f);
                playable.SetBool("Rolling", false);
                Step(.2f);
                Check(playable.GetCurrentAnimatorStateInfo(0).IsName(armed ? "Air" : "UnarmedAir"), "Roll off a ledge enters matching fall state");
            }
            graph.Destroy();
            rig.StopPreview();

            // Exercise the public API offline with the real controller and a physics floor.
            physicsScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            apiRoot = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WarriorWeaponStateSetup.PrefabPath), physicsScene);
            avatar = apiRoot.GetComponent<WarriorAvatar>();
            animator = avatar.Animator;
            apiRoot.transform.position = new Vector3(10000, 10000, 10000);
            var actor = apiRoot.GetComponent<CharacterInstance>();
            var health = apiRoot.GetComponent<Health>();
            typeof(CharacterInstance).GetProperty("Health").SetValue(actor, health);
            health.CurrentHealth = 100;
            apiRoot.layer = (int)GameLayers.character;
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(floor, apiRoot.scene);
            floor.transform.SetPositionAndRotation(apiRoot.transform.position - Vector3.up * .1f, Quaternion.identity);
            floor.transform.localScale = new Vector3(800, .2f, 30);
            Physics.SyncTransforms();
            animator.fireEvents = false;
            animator.Rebind();
            animator.SetBool("WeaponDrawn", true);
            animator.SetBool("Grounded", true);
            animator.Play("Locomotion", 0, 0);
            animator.Update(.2f);
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var startField = typeof(WarriorAvatar).GetField("rollStartedAt", flags);
            var armedField = typeof(WarriorAvatar).GetField("rollArmed", flags);
            var facingField = typeof(WarriorAvatar).GetField("rollFacing", flags);
            var travelStep = typeof(WarriorAvatar).GetMethod("RollStepDisplacement", flags);
            var controller = apiRoot.GetComponent<CharacterController>();
            facingField.SetValue(avatar, 90f);
            foreach (bool armed in new[] { true, false })
            {
                armedField.SetValue(avatar, armed);
                float duration = avatar.RollDuration;
                float distance = avatar.RollDistance;
                float before = apiRoot.transform.position.x;
                const int rolls = 5;
                for (int roll = 0; roll < rolls; roll++)
                {
                    double start = 100000 + (armed ? 100 : 0) + roll * 10;
                    startField.SetValue(avatar, start);
                    int ticks = Mathf.CeilToInt(duration * 60);
                    for (int tick = 0; tick <= ticks; tick++)
                    {
                        double now = start + Mathf.Min(duration, tick / 60f);
                        controller.Move((Vector3)travelStep.Invoke(avatar, new object[] { now }));
                    }
                }
                float travelled = apiRoot.transform.position.x - before;
                Check(Mathf.Abs(travelled - rolls * distance) < .08f, "Five impulse rolls cover their configured distance through CharacterController: " + armed);
                float average = travelled / (rolls * (duration + avatar.RollRecovery));
                Check(Mathf.Abs(average / avatar.SprintSpeed - avatar.RollTravelMultiplier) < .015f,
                    (armed ? "Armed" : "Unarmed") + " repeated-roll average including delay: " + average.ToString("F2") + " m/s vs sprint " + avatar.SprintSpeed.ToString("F2"));
                report.AppendLine("Roll tuning: duration " + duration.ToString("F2") + " s, distance " + distance.ToString("F2") + " m, recovery " + avatar.RollRecovery.ToString("F2") + " s");
            }
            startField.SetValue(avatar, -1000d);
            Check(WarriorAvatar.RollTravelFraction(.1f) < WarriorAvatar.RollTravelFraction(.5f) && WarriorAvatar.RollTravelFraction(.9f) == 1,
                "Impulse accelerates, carries momentum, and finishes before standing");
            actor.Input.LookY = 90;
            report.AppendLine($"Physics API prerequisites: state={animator.GetCurrentAnimatorStateInfo(0).shortNameHash}, transition={animator.IsInTransition(0)}, ground={Physics.Raycast(apiRoot.transform.position+Vector3.up*.2f,Vector3.down,.4f,GameManager.environmentLayer,QueryTriggerInteraction.Ignore)}, health={health.CurrentHealth}, block={actor.Block}, server={NetworkServer.active}, client={NetworkClient.active}");
            Check(avatar.RequestRoll() && avatar.IsRolling, "Public RequestRoll starts on ground");
            Check(Mathf.Abs(avatar.RollDuration - avatar.RollAnimation.length * avatar.RollExitNormalizedTime / avatar.RollPlaybackSpeed) < .001f,
                "Active roll duration respects the configured clip exit point");
            Check(!avatar.CanUseWeapon && !avatar.RequestRoll(), "Rolling blocks attacks and repeated roll requests");
            Check(Vector3.Distance(avatar.RollDirection * avatar.RollDistance, Vector3.right * avatar.RollDistance) < .001f,
                "Forward displacement follows character facing and configured distance");
            var capsule = apiRoot.GetComponent<CharacterController>();
            var origin = apiRoot.transform.position;
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(wall, apiRoot.scene);
            wall.transform.position = origin + Vector3.right * 2 + Vector3.up;
            wall.transform.localScale = new Vector3(.2f, 3, 5);
            Physics.SyncTransforms();
            var collision = capsule.Move(avatar.RollDirection * avatar.RollDistance);
            Check((collision & CollisionFlags.Sides) != 0 && apiRoot.transform.position.x < origin.x + 2,
                "Roll displacement is stopped by a wall through CharacterController.Move");
            avatar.SetWeaponDrawn(false);
            Check(avatar.WeaponDrawn, "Weapon trigger requests wait until roll finishes");
            gate = new GameObject("Roll gate request regression");
            SceneManager.MoveGameObjectToScene(gate, physicsScene);
            var weaponTrigger = gate.AddComponent<WarriorWeaponTrigger>();
            weaponTrigger.Action = WarriorWeaponTrigger.WeaponAction.Draw;
            Check(weaponTrigger.TryApply(capsule) && avatar.RequestedWeaponDrawn,
                "Crossing a second gate replaces the pending holster with draw during a long roll");
            weaponTrigger.Action = WarriorWeaponTrigger.WeaponAction.Holster;
            Check(weaponTrigger.TryApply(capsule) && !avatar.RequestedWeaponDrawn,
                "The last gate determines the requested weapon state");
            typeof(WarriorAvatar).GetField("rollStartedAt", flags).SetValue(avatar, NetworkTime.time - avatar.RollDuration - .01);
            apiRoot.SendMessage("UpdateRollPresentation");
            Check(!avatar.IsRolling && !avatar.WeaponDrawn, "Queued holster applies after roll");
            Check(!avatar.RequestRoll(), "Recovery prevents immediate roll spam");
            foreach (bool armed in new[] { true, false })
            {
                armedField.SetValue(avatar, armed);
                typeof(WarriorAvatar).GetField("weaponDrawn", flags).SetValue(avatar, armed);
                typeof(WarriorAvatar).GetField("weaponReadyAt", flags).SetValue(avatar, 0d);
                animator.SetBool("WeaponDrawn", armed);
                animator.SetBool("RollArmed", armed);
                animator.SetBool("Grounded", true);
                animator.SetBool("Rolling", true);
                animator.Play(armed ? "RollForward" : "UnarmedRollForward", 0, .84f);
                animator.Update(0);
                startField.SetValue(avatar, NetworkTime.time - avatar.RollDuration - avatar.RollRecovery + .02);
                Check(!avatar.RequestRoll(), "Fresh press before recovery ends is rejected: " + armed);
                startField.SetValue(avatar, NetworkTime.time - avatar.RollDuration - avatar.RollRecovery - .01);
                apiRoot.SendMessage("UpdateRollPresentation");
                Check(!avatar.IsRolling, "Rejected early press is not replayed automatically: " + armed);
                // Deliberately use a longer blend than the configured one to expose
                // the old extra wait caused by rejecting every Animator transition.
                animator.CrossFadeInFixedTime(armed ? "Locomotion" : "UnarmedLocomotion", .2f, 0, 0);
                animator.Update(.01f);
                Check(animator.IsInTransition(0), "Exercise a roll exit blend still in progress: " + armed);
                Check(avatar.RequestRoll() && avatar.IsRolling, "New roll starts after recovery during the old exit blend: " + armed);
            }
            health.CurrentHealth = 0;
            Check(!avatar.RequestRoll(), "Dead characters cannot roll");
            report.AppendLine("Networking: server validates requests and replicates start time/facing; movement uses the existing owning-client CharacterController path.");
            Debug.Log(report.ToString());
        }
        finally
        {
            if (graph.IsValid()) graph.Destroy();
            rig.StopPreview();
            if (floor) UnityEngine.Object.DestroyImmediate(floor);
            if (wall) UnityEngine.Object.DestroyImmediate(wall);
            if (gate) UnityEngine.Object.DestroyImmediate(gate);
            if (apiRoot) UnityEngine.Object.DestroyImmediate(apiRoot);
            if (physicsScene.IsValid()) EditorSceneManager.CloseScene(physicsScene, true);
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            PrefabUtility.UnloadPrefabContents(root);
            File.WriteAllText("Documentation/warrior-rebuild/roll-validation.txt", report.ToString());
        }
    }
}
