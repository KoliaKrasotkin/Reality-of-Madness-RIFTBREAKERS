using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEngine;

public static class WeaponEquipmentPreviewValidation
{
    [MenuItem("ROM/Animation Rigging/Validate Equipment Inspector Preview")]
    public static void Validate()
    {
        var root = PrefabUtility.LoadPrefabContents(WarriorWeaponStateSetup.PrefabPath);
        var controller = root.GetComponentInChildren<WeaponIKController>();
        var avatar = root.GetComponent<WarriorAvatar>();
        var adapter = root.GetComponent<RiftWarriorEquipment>();
        var animator = controller.Animator;
        var originalController = animator.runtimeAnimatorController;
        bool originalActive = avatar.EquippedWeapon.activeSelf;
        int originalChildren = root.GetComponentsInChildren<Transform>(true).Length;
        var editor = (WeaponIKControllerEditor)UnityEditor.Editor.CreateEditor(controller);
        var report = new StringBuilder();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object Call(string name, params object[] args) => typeof(WeaponIKControllerEditor).GetMethod(name, flags).Invoke(editor, args);
        object Get(string name) => typeof(WeaponIKControllerEditor).GetField(name, flags).GetValue(editor);
        void Set(string name, object value) => typeof(WeaponIKControllerEditor).GetField(name, flags).SetValue(editor, value);
        void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); report.AppendLine("PASS " + message); }
        var swordPosition = adapter.SwordShieldWeapon.Position;
        try
        {
            foreach (var weapon in new[] { adapter.AxeWeapon, adapter.SwordShieldWeapon, adapter.AxeWeapon })
            {
                Call("SetPreviewWeapon", weapon);
                foreach (string name in new[] { "Idle", "Attack1", "Jump", "Unarmed/Idle", "Sheath-Back-Unarmed", "Unsheath-Back-Unarmed" })
                {
                    var clip = (AnimationClip)Call("FindClip", name);
                    Check(clip, weapon.name + " resolves " + name);
                    Call("SelectClip", clip);
                    Call("StartPreview");
                    Set("previewTime", clip.length * .65f);
                    Call("SamplePreview");
                    Check(animator.runtimeAnimatorController == weapon.Controller, weapon.name + " uses its Animator variant in preview");
                    if (weapon == adapter.SwordShieldWeapon)
                    {
                        var temporary = (GameObject)Get("previewEquipmentRoot");
                        Check(temporary && temporary.GetComponentsInChildren<MeshRenderer>(true).Length >= 2 && !avatar.EquippedWeapon.activeSelf,
                            "Sword/shield models appear without the axe: " + name);
                        Check((temporary.hideFlags & HideFlags.DontSaveInEditor) != 0, "Preview models cannot be saved into the prefab");
                        if (name == "Idle")
                        {
                            Check(WarriorSwordIdleSetup.IsDoubleLDerived(clip), "Inspector resolves DoubleL-derived locomotion");
                            var model = temporary.transform.Find(weapon.Id + " " + weapon.Socket);
                            var before = model.position;
                            weapon.Position += new Vector3(.02f, 0, 0);
                            Call("SamplePreview");
                            Check(Mathf.Abs(Vector3.Distance(before, model.position) - .02f) < .001f, "Asset offset edits immediately update the sampled model");
                            weapon.Position = swordPosition;
                        }
                        if (name == "Unarmed/Idle")
                        {
                            var body = WeaponGripSpace.BodyRotation(animator);
                            foreach (var part in weapon.EquippedParts)
                            {
                                var model = temporary.transform.Find(weapon.Id + " " + part.Socket);
                                var bone = animator.GetBoneTransform(part.StoredBone);
                                Check(Vector3.Distance(model.position, bone.position + body * part.StoredPosition) < .001f, "Stored preview follows " + part.name + " back offsets");
                            }
                        }
                    }
                    Call("StopPreview");
                    Check(animator.runtimeAnimatorController == originalController && avatar.EquippedWeapon.activeSelf == originalActive,
                        "Stop restores Animator and axe visibility");
                    Check(root.GetComponentsInChildren<Transform>(true).Length == originalChildren, "Stop removes all temporary models");
                }
            }
            Directory.CreateDirectory("Documentation/warrior-rebuild");
            File.WriteAllText("Documentation/warrior-rebuild/equipment-inspector-preview-validation.txt", report.ToString());
            Debug.Log("WARRIOR_EQUIPMENT_INSPECTOR_PREVIEW_PASS");
        }
        finally
        {
            adapter.SwordShieldWeapon.Position = swordPosition;
            Call("StopPreview");
            UnityEngine.Object.DestroyImmediate(editor);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
