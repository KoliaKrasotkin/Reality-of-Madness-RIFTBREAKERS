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

public static class WarriorEquipmentValidation
{
    [MenuItem("ROM/Animation Rigging/Validate Warrior Equipment")]
    public static void Validate()
    {
        var report = new StringBuilder();
        void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); report.AppendLine("PASS " + message); }
        var root = PrefabUtility.LoadPrefabContents(WarriorWeaponStateSetup.PrefabPath);
        var avatar = root.GetComponent<WarriorAvatar>();
        var animator = avatar.Animator;
        var rig = animator.GetComponent<RigBuilder>();
        var presenter = root.GetComponent<CharacterWeaponPresentation>() ?? root.AddComponent<CharacterWeaponPresentation>();
        presenter.Animator = animator; presenter.BuiltInWeapon = avatar.EquippedWeapon;
        PlayableGraph graph = default;
        try
        {
            var axe = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WarriorEquipmentSetup.Weapons + "/WarriorAxe.asset");
            var sword = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WarriorEquipmentSetup.Weapons + "/WarriorSword.asset");
            Check(axe && sword, "Both existing Warrior WeaponDefinitions are used");
            Check(sword.Model == AssetDatabase.LoadAssetAtPath<GameObject>(WarriorEquipmentSetup.Weapons + "/WarriorSword.prefab"), "Sword uses the original Warrior model");
            Check(sword.AdditionalEquipment.Length == 1 && sword.AdditionalEquipment[0].Model == AssetDatabase.LoadAssetAtPath<GameObject>(WarriorEquipmentSetup.Weapons + "/WarriorShield.prefab"), "Shield uses the original Warrior model through its own WeaponDefinition");
            var shield = sword.AdditionalEquipment[0];
            Check(shield.IsShield && shield.DamageMultiplier == 0 && shield.AttackSpeed == 0 && shield.Reach == 0, "Shield has a defensive role and no offensive stats");
            Check(Mathf.Abs(sword.DamageMultiplier - .85f) < .001f && Mathf.Abs(axe.DamageMultiplier - 1) < .001f, "Existing combat multipliers are preserved");
            var progression = AssetDatabase.LoadAssetAtPath<WarriorProgressionDefinition>("Assets/ROM/Resources/WarriorProgression.asset");
            Check(progression.Sword == sword && progression.Shield == sword.AdditionalEquipment[0].Model, "Inventory and progression use the same weapon definitions and shield model");
            Check(sword.EquippedParts.ToArray().Length == 2 && sword.EquippedParts.ToArray()[0].Socket == HumanBodyBones.RightHand && sword.EquippedParts.ToArray()[1].Socket == HumanBodyBones.LeftHand,
                "Sword uses right hand; shield uses left hand");
            foreach (var profile in new[] { sword, axe, sword })
            {
                presenter.Equip(profile);
                graph = PlayableGraph.Create("Equipment profile validation");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable = AnimatorControllerPlayable.Create(graph, animator.runtimeAnimatorController);
                var output = AnimationPlayableOutput.Create(graph, "Equipment", animator);
                output.SetSourcePlayable(rig.BuildPreviewGraph(graph, playable));
                graph.Play();
                playable.SetBool("WeaponDrawn", true); playable.SetBool("Grounded", true);
                playable.SetFloat("AttackRate", 1); playable.SetFloat("RollSpeed", 1);
                void Step(float seconds)
                {
                    for (int j = 0; j < Mathf.Max(1, Mathf.CeilToInt(seconds * 60)); j++)
                    { presenter.BeginPoseEvaluation(); rig.UpdatePreviewGraph(graph); graph.Evaluate(seconds == 0 ? 0 : 1f / 60); }
                    presenter.RefreshPose(playable.GetFloat("EquipmentBackWeight"));
                }
                Step(.3f);
                var states = Enumerable.Range(0, 9).Select(i => "Attack" + i).Concat(new[] { "Locomotion", "Jump", "RollForward" });
                foreach (string state in states)
                {
                    playable.SetBool("DefensiveHold", state == "Attack6");
                    if (state == "RollForward") playable.SetBool("Rolling", true);
                    for (int sample = 0; sample <= 10; sample++)
                    {
                        playable.Play(state, 0, sample * .08f); Step(0);
                        if (profile == sword)
                        {
                            Check(rig.layers.All(l => l.rig.weight < .001f), state + " sample " + sample + ": axe rig cannot pull either hand");
                            for (int hand = 0; hand < 2; hand++)
                            {
                                var data = profile.EquippedParts.ToArray()[hand];
                                var model = root.transform.Find(profile.Id + " " + data.Socket);
                                Check(model, state + " sample " + sample + ": " + data.Socket + " model exists");
                                foreach (var grip in data.HandGrips)
                                    Check(float.IsFinite(animator.GetBoneTransform(grip.Hand).position.x),
                                        state + " sample " + sample + ": " + grip.Hand + " IK is finite");
                            }
                        }
                        else
                        {
                            Check(float.IsFinite(animator.GetBoneTransform(HumanBodyBones.RightHand).position.x) && float.IsFinite(animator.GetBoneTransform(HumanBodyBones.LeftHand).position.x),
                                state + " sample " + sample + ": shared axe IK is finite");
                        }
                    }
                    if (profile == sword && state.StartsWith("Attack"))
                        Check(playable.GetCurrentAnimatorClipInfo(0).Any(c => WarriorSwordIdleSetup.IsDoubleLDerived(c.clip)), state + " plays a DoubleL-derived clip");
                    playable.SetBool("Rolling", false);
                }
                Check(avatar.EquippedWeapon.activeSelf == profile.UsesBuiltInWeapon, "Only the selected equipment is visible: " + profile.Id);
                playable.SetBool("DefensiveHold", false);
                playable.SetBool("WeaponDrawn", false); Step(3);
                Check(playable.GetCurrentAnimatorStateInfo(0).IsName("UnarmedLocomotion"), profile.Id + " returns to unarmed locomotion after holstering");
                if (profile == sword)
                {
                    Check(playable.GetFloat("EquipmentBackWeight") > .99f, "Stored equipment follows the back socket");
                    foreach (var data in profile.EquippedParts.ToArray())
                    {
                        var model = root.transform.Find(profile.Id + " " + data.Socket);
                        var back = animator.GetBoneTransform(data.StoredBone);
                        Check(Vector3.Distance(model.position, back.position + WeaponGripSpace.BodyRotation(animator) * data.StoredPosition) < .001f, data.Socket + " model rests on the back");
                    }
                }
                playable.SetBool("WeaponDrawn", true); Step(3);
                Check(playable.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), profile.Id + " draws and returns to armed locomotion");
                graph.Destroy(); rig.StopPreview();
            }
            // Also exercise Animator's own parameter path used by LateUpdate in game.
            presenter.BeginPoseEvaluation(); animator.Rebind(); animator.Update(0);
            animator.SetBool("Grounded", true); animator.SetBool("WeaponDrawn", false);
            for (int i = 0; i < 240; i++) { presenter.BeginPoseEvaluation(); animator.Update(1f / 60); presenter.RefreshPose(); }
            Check(animator.GetFloat("EquipmentBackWeight") > .99f, "Runtime Animator exposes the animated back attachment weight");
            presenter.RefreshPose();
            presenter.Alive = false; presenter.RefreshPose();
            Check(sword.EquippedParts.ToArray().All(a => !root.transform.Find(sword.Id + " " + a.Socket).gameObject.activeSelf), "Death hides both attached models");
            presenter.Alive = true;
            animator.SetBool("WeaponDrawn", true);
            for (int i = 0; i < 240; i++) { presenter.BeginPoseEvaluation(); animator.Update(1f / 60); presenter.RefreshPose(); }
            presenter.RefreshPose();
            Check(animator.GetFloat("EquipmentBackWeight") < .01f, "Runtime draw restores both hand attachments");
            var activeClip = animator.GetCurrentAnimatorClipInfo(0).OrderByDescending(c => c.weight).First().clip;
            var expectedShield = shield.AnimationIK.First(p => p.Clip == activeClip).WeaponPosition;
            Check(Vector3.Distance(presenter.Settings(shield).WeaponPosition, expectedShield) < .0001f,
                "Runtime Animator selects the active clip's shield IK offsets without preview overrides");
            var warrior = AssetDatabase.LoadAssetAtPath<CombatClassDefinition>("Assets/ROM/Combat/Warrior.asset");
            var actual = presenter.ResolveClip(warrior.BasicAttack.Animation);
            Check(WarriorSwordIdleSetup.IsDoubleLDerived(actual), "Autoattack resolves the selected equipment animation");
            Check(avatar.PresentAutoAttack(warrior.BasicAttack) && Mathf.Abs(animator.GetFloat("AttackRate") - actual.length / warrior.BasicAttack.CastDuration) < .001f,
                "Autoattack playback rate uses the DoubleL clip length and preserves gameplay timing");
            Check(AssetDatabase.LoadAssetAtPath<GameObject>(WarriorWeaponStateSetup.PrefabPath).GetComponent<Mirror.NetworkIdentity>().assetId == 2596278896u, "Mirror prefab identity is preserved");
            Directory.CreateDirectory("Documentation/warrior-rebuild");
            File.WriteAllText("Documentation/warrior-rebuild/equipment-validation.txt", report.ToString());
            Preview();
            Debug.Log("WARRIOR_EQUIPMENT_VALIDATION_PASS: " + report.ToString().Split('\n').Length + " checks; see Documentation/warrior-rebuild/equipment-validation.txt");
        }
        finally
        {
            if (graph.IsValid()) graph.Destroy();
            rig.StopPreview();
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("ROM/Animation Rigging/Preview Warrior Sword and Shield")]
    public static void Preview()
    {
        var preview = new PreviewRenderUtility();
        PlayableGraph graph = default;
        RigBuilder rig = null;
        try
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorWeaponStateSetup.PrefabPath);
            var root = UnityEngine.Object.Instantiate(asset);
            preview.AddSingleGO(root);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var avatar = root.GetComponent<WarriorAvatar>();
            var animator = avatar.Animator;
            var presenter = root.GetComponent<CharacterWeaponPresentation>() ?? root.AddComponent<CharacterWeaponPresentation>();
            presenter.Animator = animator; presenter.BuiltInWeapon = avatar.EquippedWeapon;
            presenter.Equip(AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WarriorEquipmentSetup.Weapons + "/WarriorSword.asset"));
            rig = animator.GetComponent<RigBuilder>();
            graph = PlayableGraph.Create("Equipment preview");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimatorControllerPlayable.Create(graph, animator.runtimeAnimatorController);
            AnimationPlayableOutput.Create(graph, "Equipment", animator).SetSourcePlayable(rig.BuildPreviewGraph(graph, playable));
            graph.Play();
            playable.SetBool("WeaponDrawn", true); playable.SetBool("Grounded", true);
            playable.SetFloat("AttackRate", 1);
            void Step(float seconds)
            {
                for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) { presenter.BeginPoseEvaluation(); rig.UpdatePreviewGraph(graph); graph.Evaluate(1f / 60); }
                presenter.RefreshPose(playable.GetFloat("EquipmentBackWeight"));
            }
            preview.camera.orthographic = true; preview.camera.orthographicSize = 1.35f;
            preview.camera.nearClipPlane = .01f; preview.camera.farClipPlane = 30;
            preview.lights[0].intensity = 1.4f; preview.lights[0].transform.rotation = Quaternion.Euler(35, 210, 0);
            preview.lights[1].intensity = 1;
            preview.ambientColor = new Color(.4f, .4f, .45f);
            void Render(string name, Vector3 camera, Vector3? focus = null)
            {
                preview.camera.transform.position = camera;
                preview.camera.transform.LookAt(focus ?? new Vector3(0, 1.05f, 0));
                preview.BeginStaticPreview(new Rect(0, 0, 850, 900)); preview.Render(true);
                var image = preview.EndStaticPreview();
                File.WriteAllBytes("Documentation/warrior-rebuild/" + name + ".png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            Step(.4f); Render("sword-shield-idle", new Vector3(3, 1.7f, 4));
            var shoulderCenter = animator.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "RightShoulderArmor").bounds.center;
            var focus = (shoulderCenter + animator.GetBoneTransform(HumanBodyBones.RightHand).position) * .5f;
            preview.camera.orthographicSize = .47f;
            Render("sword-idle-grip-front", focus + new Vector3(1, .2f, 1.5f), focus);
            Render("sword-idle-grip-side", focus + new Vector3(1.5f, .3f, -.3f), focus);
            preview.camera.orthographicSize = 1.35f;
            playable.Play("Attack0", 0, .35f); Step(.02f); Render("sword-shield-attack", new Vector3(3, 1.7f, 4));
            playable.SetBool("WeaponDrawn", false); Step(3);
            Render("sword-shield-stored", animator.GetBoneTransform(HumanBodyBones.Chest).position + WeaponGripSpace.BodyRotation(animator) * new Vector3(0, .3f, -4));
        }
        finally { if (graph.IsValid()) graph.Destroy(); if (rig) rig.StopPreview(); preview.Cleanup(); }
    }
}
