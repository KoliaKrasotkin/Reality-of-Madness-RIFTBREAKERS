using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEngine;

public static class EquipmentHandIKSetup
{
    [MenuItem("ROM/Animation Rigging/Configure Equipment Hand IK")]
    public static void Configure()
    {
        WithPreview((root, editor, call, get, set) =>
        {
            var adapter = root.GetComponent<RiftWarriorEquipment>();
            var animator = root.GetComponentInChildren<WeaponIKController>().Animator;
            foreach (var weapon in new[] { adapter.SwordShieldWeapon, adapter.AxeWeapon })
            {
                call("SetPreviewWeapon", new object[] { weapon });
                var clip = (AnimationClip)call("FindClip", new object[] { "Idle" });
                call("SelectClip", new object[] { clip }); call("StartPreview", Array.Empty<object>());
                var presenter = (CharacterWeaponPresentation)get("equipmentPreview");
                foreach (var part in weapon.UsesBuiltInWeapon ? new[] { weapon } : weapon.EquippedParts.ToArray())
                {
                    if (part.HandGrips.Length > 0) continue; // Keep all later artist adjustments.
                    var model = presenter.ModelFor(part).transform;
                    var matrix = model.localToWorldMatrix;
                    var rotation = model.rotation;
                    if (part == adapter.SwordShieldWeapon)
                    {
                        // Original fitted palm frame is the handle reference. The
                        // user's current model placement stays completely unchanged.
                        var hand = animator.GetBoneTransform(part.Socket);
                        var grip = WeaponGripSpace.Rotation(animator, part.Socket);
                        rotation = grip * Quaternion.Euler(-.000034f, 87.60869f, 182.40646f);
                        matrix = Matrix4x4.TRS(hand.position + grip * new Vector3(.052240554f, -.0052457782f, .116447635f), rotation, part.Scale);
                    }
                    var hands = part.UsesBuiltInWeapon ? new[] { HumanBodyBones.RightHand, HumanBodyBones.LeftHand } : new[] { part.Socket };
                    part.HandGrips = hands.Select(bone =>
                    {
                        var hand = animator.GetBoneTransform(bone);
                        return new EquipmentHandGrip { Hand = bone, Position = matrix.inverse.MultiplyPoint3x4(hand.position), Rotation = (Quaternion.Inverse(rotation) * hand.rotation).eulerAngles };
                    }).ToArray();
                    part.AnimationIK = weapon.Controller.animationClips.Distinct().Where(c => !c.name.Contains("Weapon")).Select(c => new EquipmentAnimationIK
                    {
                        Clip = c,
                        WeaponPosition = part.IsShield ? new Vector3(-.025f, 0, 0) : Vector3.zero
                    }).ToArray();
                    part.EnableHandIK = true;
                    EditorUtility.SetDirty(part); AssetDatabase.SaveAssetIfDirty(part);
                }
                call("StopPreview", Array.Empty<object>());
            }
        });
        Debug.Log("EQUIPMENT_HAND_IK_CONFIGURED");
    }

    delegate object Call(string name, object[] args);
    delegate object Get(string name);
    delegate void Set(string name, object value);
    static void WithPreview(Action<GameObject, WeaponIKControllerEditor, Call, Get, Set> action)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var root = PrefabUtility.LoadPrefabContents(WarriorWeaponStateSetup.PrefabPath);
        var editor = (WeaponIKControllerEditor)UnityEditor.Editor.CreateEditor(root.GetComponentInChildren<WeaponIKController>());
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object Invoke(string name, object[] args) => typeof(WeaponIKControllerEditor).GetMethod(name, flags).Invoke(editor, args);
        try { action(root, editor, Invoke, name => typeof(WeaponIKControllerEditor).GetField(name, flags).GetValue(editor),
            (name, value) => typeof(WeaponIKControllerEditor).GetField(name, flags).SetValue(editor, value)); }
        finally { Invoke("StopPreview", Array.Empty<object>()); UnityEngine.Object.DestroyImmediate(editor); PrefabUtility.UnloadPrefabContents(root); }
    }

    [MenuItem("ROM/Animation Rigging/Validate Equipment Hand IK")]
    public static void Validate()
    {
        var report = new StringBuilder();
        int samples = 0; float maxIdleError = 0;
        void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        WithPreview((root, editor, call, get, set) =>
        {
            var adapter = root.GetComponent<RiftWarriorEquipment>();
            var animator = root.GetComponentInChildren<WeaponIKController>().Animator;
            foreach (var weapon in new[] { adapter.SwordShieldWeapon, adapter.AxeWeapon })
            {
                call("SetPreviewWeapon", new object[] { weapon });
                foreach (string name in new[] { "Idle", "Run-Forward", "Run-Backward", "Run-Left", "Run-Right", "Attack1", "Attack2", "Attack3", "Attack4", "Attack5", "Attack6", "Attack7", "Attack8", "Attack9", "Jump", "DiveRoll-Forward1", "Unarmed/Idle", "Sheath-Back-Unarmed", "Unsheath-Back-Unarmed" })
                {
                    var clip = (AnimationClip)call("FindClip", new object[] { name });
                    if (!clip) continue;
                    call("SelectClip", new object[] { clip }); call("StartPreview", Array.Empty<object>());
                    var presenter = (CharacterWeaponPresentation)get("equipmentPreview");
                    for (int i = 0; i <= 10; i++)
                    {
                        set("previewTime", clip.length * i / 10f); call("SamplePreview", Array.Empty<object>());
                        var right = animator.GetBoneTransform(HumanBodyBones.RightHand);
                        var left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                        var rightPosition = right.position; var leftPosition = left.position;
                        Check(float.IsFinite(rightPosition.x) && float.IsFinite(leftPosition.x), "Finite wrists: " + name);
                        if (name == "Idle") foreach (var part in weapon.UsesBuiltInWeapon ? new[] { weapon } : weapon.EquippedParts.ToArray())
                        {
                            var model = presenter.ModelFor(part).transform;
                            foreach (var grip in part.HandGrips)
                            {
                                float error = Vector3.Distance(animator.GetBoneTransform(grip.Hand).position, model.TransformPoint(grip.Position));
                                maxIdleError = Mathf.Max(maxIdleError, error);
                                Check(error < .015f, part.name + " idle hand reaches grip: " + error);
                            }
                        }
                        call("SamplePreview", Array.Empty<object>());
                        Check(Vector3.Distance(rightPosition, right.position) < .0001f && Vector3.Distance(leftPosition, left.position) < .0001f, "No repeated-preview feedback: " + name);
                        samples++;
                    }
                    report.AppendLine("PASS " + weapon.name + " " + name + ": finite hands, repeatable sampling");
                    if (name == "Idle" && weapon == adapter.SwordShieldWeapon)
                    {
                        var entry = weapon.AnimationIK.First(p => p.Clip == clip);
                        var original = entry.WeaponPosition;
                        var model = presenter.ModelFor(weapon).transform;
                        var before = model.position;
                        var beforeHand = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                        try
                        {
                            entry.WeaponPosition += new Vector3(.01f, 0, 0);
                            call("SamplePreview", Array.Empty<object>());
                            Check(Mathf.Abs(Vector3.Distance(before, model.position) - .01f) < .0001f, "Weapon offset moves model 10mm");
                            Check(Mathf.Abs(Vector3.Distance(beforeHand, animator.GetBoneTransform(HumanBodyBones.RightHand).position) - .01f) < .002f, "Hand follows moved weapon");
                        }
                        finally { entry.WeaponPosition = original; }
                        report.AppendLine("PASS sword offset moves sword and wrist together; definition mount unchanged");
                    }
                    call("StopPreview", Array.Empty<object>());
                }
            }
        });
        report.AppendLine("Samples: " + samples + "; max idle wrist-to-grip error: " + maxIdleError.ToString("F6") + " m");
        File.WriteAllText("Documentation/warrior-rebuild/equipment-hand-ik-validation.txt", report.ToString());
        Debug.Log("EQUIPMENT_HAND_IK_VALIDATION_PASS: " + samples + " samples; max idle error " + maxIdleError);
    }
}
