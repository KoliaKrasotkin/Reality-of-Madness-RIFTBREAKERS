using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public static class WarriorAxeIKCalibration
{
    const int Intervals = 60;
    const string ReportFolder = "Documentation/warrior-rebuild";
    sealed class ClipFit
    {
        public AnimationClip clip;
        public Quaternion rightRotation, leftRotation;
        public float leftContact;
    }
    internal sealed class Context : IDisposable
    {
        public readonly PreviewRenderUtility preview = new PreviewRenderUtility();
        public readonly GameObject root;
        public readonly WeaponIKController controller;
        public readonly WeaponIKControllerEditor editor;
        public readonly WeaponDefinition axe;
        public Animator Animator => controller.Animator;
        static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        public Context()
        {
            root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(WarriorWeaponStateSetup.PrefabPath));
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            preview.AddSingleGO(root);
            controller = root.GetComponentInChildren<WeaponIKController>();
            axe = root.GetComponent<RiftWarriorEquipment>().AxeWeapon;
            editor = (WeaponIKControllerEditor)UnityEditor.Editor.CreateEditor(controller);
            Call("SetPreviewWeapon", axe);
        }
        public object Call(string method, params object[] args) => typeof(WeaponIKControllerEditor).GetMethod(method, Flags).Invoke(editor, args);
        public CharacterWeaponPresentation Presenter => (CharacterWeaponPresentation)typeof(WeaponIKControllerEditor).GetField("equipmentPreview", Flags).GetValue(editor);
        public void Start(AnimationClip clip) { Call("SelectClip", clip); Call("StartPreview"); }
        public void Sample(AnimationClip clip, float normalized)
        {
            typeof(WeaponIKControllerEditor).GetField("previewTime", Flags).SetValue(editor, clip.length * normalized);
            Call("SamplePreview");
        }
        public void Stop() => Call("StopPreview");
        public void Dispose() { Stop(); UnityEngine.Object.DestroyImmediate(editor); preview.Cleanup(); }
    }
    static bool Armed(AnimationClip clip) => !clip.name.StartsWith("Unarmed-") && !clip.name.Contains("Sheath-") && !clip.name.Contains("Unsheath-") && !clip.name.Contains("Death");
    static Vector3 Wrist(Vector3 contact, Quaternion rotation, Vector3 palmOffset) => contact - rotation * palmOffset;
    static Quaternion Mean(List<Quaternion> samples)
    {
        var mean = samples[0];
        for (int i = 1; i < samples.Count; i++) mean = Quaternion.Slerp(mean, samples[i], 1f / (i + 1));
        return mean;
    }
    static EquipmentAnimationIK Entry(WeaponDefinition axe, AnimationClip clip) => axe.AnimationIK.First(e => e.Clip == clip);

    [MenuItem("ROM/Animation Rigging/Calibrate All Warrior Axe Animations")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var current=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WarriorEquipmentSetup.Weapons+"/WarriorAxe.asset");
        if(current&&current.UseApprovedCarryGrip){WarriorAxeReferenceFit.Configure();return;}
        using (var c = new Context())
        {
            var axe = c.axe;
            bool wasEnabled = axe.EnableHandIK;
            var clips = axe.Controller.animationClips.Where(p => p.isHumanMotion).Distinct().OrderBy(p => p.name).ToArray();
            var fits = new List<ClipFit>();
            var data = c.controller.GripConstraint.data;
            try
            {
                // Measure the animation stream rig before the previous idle-only
                // equipment calibration can move either wrist.
                axe.EnableHandIK = false;
                foreach (var clip in clips.Where(Armed))
                {
                    c.Start(clip);
                    var right = new List<Quaternion>(); var left = new List<Quaternion>(); var contacts = new List<float>();
                    for (int i = 0; i <= Intervals; i++)
                    {
                        c.Sample(clip, (float)i / Intervals);
                        var model = c.Presenter.ModelFor(axe).transform;
                        right.Add(Quaternion.Inverse(model.rotation) * data.rightHand.rotation);
                        left.Add(Quaternion.Inverse(model.rotation) * data.leftHand.rotation);
                        var palm = data.leftHand.position + data.leftHand.rotation * (data.leftPalmOffset * Mathf.Abs(model.lossyScale.x));
                        contacts.Add(model.InverseTransformPoint(palm).z);
                    }
                    contacts.Sort();
                    fits.Add(new ClipFit { clip = clip, rightRotation = Mean(right), leftRotation = Mean(left),
                        leftContact = Mathf.Clamp(contacts[contacts.Count / 2], data.rightContact.z - data.maximumSpacing, data.rightContact.z - data.minimumSpacing) });
                    c.Stop();
                }
            }
            finally { c.Stop(); axe.EnableHandIK = wasEnabled; }
            var idle = fits.Single(f => f.clip.name == "2Hand-Sword-Idle");
            var rightAxis = Vector3.Dot(idle.rightRotation * data.rightGripAxis, Vector3.forward) >= 0 ? Vector3.forward : Vector3.back;
            var leftAxis = Vector3.Dot(idle.leftRotation * data.leftGripAxis, Vector3.forward) >= 0 ? Vector3.forward : Vector3.back;
            foreach (var fit in fits)
            {
                // Some source sword clips reverse their grip direction. Keep
                // the same finger-wrap axis on the axe instead of averaging
                // incompatible hand directions into a partly open grip.
                fit.rightRotation = Quaternion.FromToRotation(fit.rightRotation * data.rightGripAxis, rightAxis) * fit.rightRotation;
                fit.leftRotation = Quaternion.FromToRotation(fit.leftRotation * data.leftGripAxis, leftAxis) * fit.leftRotation;
            }
            var rightContact = data.rightContact;
            var leftContact = new Vector3(rightContact.x, rightContact.y, idle.leftContact);
            Undo.RecordObject(axe, "Calibrate all Warrior axe animation grips");
            axe.HandGrips = new[] {
                new EquipmentHandGrip { Hand = HumanBodyBones.RightHand, Position = Wrist(rightContact, idle.rightRotation, data.rightPalmOffset), Rotation = idle.rightRotation.eulerAngles, PreservePalmContact = true, PalmOffset = data.rightPalmOffset },
                new EquipmentHandGrip { Hand = HumanBodyBones.LeftHand, Position = Wrist(leftContact, idle.leftRotation, data.leftPalmOffset), Rotation = idle.leftRotation.eulerAngles, PreservePalmContact = true, PalmOffset = data.leftPalmOffset }
            };
            axe.AnimationIK = clips.Select(clip =>
            {
                var entry = new EquipmentAnimationIK { Clip = clip, Weight = Armed(clip) ? 1 : 0 };
                var fit = fits.FirstOrDefault(f => f.clip == clip);
                if (fit == null) return entry;
                entry.HandRotation = (Quaternion.Inverse(idle.rightRotation) * fit.rightRotation).eulerAngles;
                entry.LeftHandRotation = (Quaternion.Inverse(idle.leftRotation) * fit.leftRotation).eulerAngles;
                entry.HandRotation = Signed(entry.HandRotation); entry.LeftHandRotation = Signed(entry.LeftHandRotation);
                entry.HandPosition = Wrist(rightContact, fit.rightRotation, data.rightPalmOffset) - axe.HandGrips[0].Position;
                entry.LeftHandPosition = Wrist(new Vector3(rightContact.x, rightContact.y, fit.leftContact), fit.leftRotation, data.leftPalmOffset) - axe.HandGrips[1].Position;
                // A modest outward pole bias keeps the heavy bracers off the
                // torso without forcing every attack into the same elbow pose.
                entry.ElbowOffset = new Vector3(.035f, -.01f, 0);
                entry.LeftElbowOffset = new Vector3(-.035f, -.01f, 0);
                return entry;
            }).ToArray();
            axe.EnableHandIK = true; axe.KeepGripsReachable = true;
            EditorUtility.SetDirty(axe); AssetDatabase.SaveAssetIfDirty(axe);
            SyncBackWeight();
        }
        Validate();
        Debug.Log("WARRIOR_AXE_ALL_CLIPS_CALIBRATED");
    }
    static Vector3 Signed(Vector3 angles) => new Vector3(Mathf.DeltaAngle(0, angles.x), Mathf.DeltaAngle(0, angles.y), Mathf.DeltaAngle(0, angles.z));
    static void SyncBackWeight()
    {
        foreach (string name in new[] { "WeaponDrawn", "WeaponHolstered", "WeaponDrawing", "WeaponSheathing" })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ROM/Animation/WarriorWeaponStates/" + name + ".anim");
            var binding = AnimationUtility.GetCurveBindings(clip).Single(b => b.propertyName == "m_Data.backWeight");
            Undo.RecordObject(clip, "Synchronize axe back attachment weight");
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), "EquipmentBackWeight"), AnimationUtility.GetEditorCurve(clip, binding));
            EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip);
        }
    }

    [MenuItem("ROM/Animation Rigging/Validate All Warrior Axe Animations")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var current=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WarriorEquipmentSetup.Weapons+"/WarriorAxe.asset");
        if(current&&current.UseApprovedCarryGrip){WarriorAxeReferenceFit.Validate();return;}
        Directory.CreateDirectory(ReportFolder);
        var report = new StringBuilder("Warrior axe: 61 samples per clip. Wrist, palm and shaft checked after equipment IK.\n");
        using (var c = new Context())
        {
            var axe = c.axe; var data = c.controller.GripConstraint.data;
            var clips = axe.AnimationIK.Select(e => e.Clip).Where(p => p).ToArray();
            int samples = 0;
            var renderClips = new List<AnimationClip>();
            foreach (var clip in clips)
            {
                c.Start(clip);
                float maxWrist = 0, maxPalm = 0, maxAngle = 0, maxAxis = 0;
                for (int i = 0; i <= Intervals; i++)
                {
                    c.Sample(clip, (float)i / Intervals);
                    var presenter = c.Presenter; var model = presenter.ModelFor(axe).transform;
                    var settings = presenter.Settings(axe, clip);
                    if (Armed(clip)) foreach (var grip in axe.HandGrips)
                    {
                        bool left = grip.Hand == HumanBodyBones.LeftHand;
                        var hand = c.Animator.GetBoneTransform(grip.Hand);
                        var desired = model.TransformPoint(grip.Position + (left ? settings.LeftHandPosition : settings.HandPosition));
                        maxWrist = Mathf.Max(maxWrist, Vector3.Distance(hand.position, desired));
                        var palm = model.InverseTransformPoint(hand.position + hand.rotation * ((left ? data.leftPalmOffset : data.rightPalmOffset) * Mathf.Abs(model.lossyScale.x)));
                        maxPalm = Mathf.Max(maxPalm, Vector2.Distance(new Vector2(palm.x, palm.y), new Vector2(data.rightContact.x, data.rightContact.y)) * Mathf.Abs(model.lossyScale.x));
                        var targetRotation = model.rotation * Quaternion.Euler(grip.Rotation) * Quaternion.Euler(left ? settings.LeftHandRotation : settings.HandRotation);
                        maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(hand.rotation, targetRotation));
                        var axis = model.InverseTransformDirection(hand.rotation * (left ? data.leftGripAxis : data.rightGripAxis));
                        maxAxis = Mathf.Max(maxAxis, Mathf.Min(Vector3.Angle(axis, Vector3.forward), Vector3.Angle(axis, Vector3.back)));
                        if (!float.IsFinite(hand.position.x) || !float.IsFinite(hand.rotation.x)) throw new InvalidOperationException("Nonfinite axe pose: " + clip.name);
                    }
                    var before = data.rightHand.position; var weaponBefore = model.position;
                    c.Sample(clip, (float)i / Intervals);
                    if (Vector3.Distance(before, data.rightHand.position) > .0001f || Vector3.Distance(weaponBefore, model.position) > .0001f)
                        throw new InvalidOperationException("Axe preview feedback: " + clip.name);
                    samples++;
                }
                report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}: wrist {1:F3} mm; palm-to-shaft {2:F3} mm; wrist rotation {3:F3} deg; grip axis {4:F3} deg; IK {5}", clip.name, maxWrist * 1000, maxPalm * 1000, maxAngle, maxAxis, Entry(axe, clip).Weight));
                if (Armed(clip) && (maxWrist > .002f || maxPalm > .002f || maxAngle > .1f || maxAxis > .1f))
                {
                    File.WriteAllText(ReportFolder + "/axe-equipment-ik-validation.txt", report.ToString());
                    throw new InvalidOperationException("Axe grip does not reach handle: " + clip.name + " wrist=" + maxWrist + " palm=" + maxPalm);
                }
                if (Armed(clip)) renderClips.Add(clip);
                c.Stop();
            }
            report.AppendLine("PASS samples: " + samples + "; all armed palms on shaft; unarmed/handoff IK disabled; repeatable sampling.");
            ValidateTransitions(c, report);
            File.WriteAllText(ReportFolder + "/axe-equipment-ik-validation.txt", report.ToString());
            RenderSheets(c, renderClips);
            Debug.Log("WARRIOR_AXE_ALL_CLIPS_VALIDATION_PASS: " + samples + " samples");
        }
    }
    static void ValidateTransitions(Context c, StringBuilder report)
    {
        c.Stop();
        var presenter = c.root.GetComponent<CharacterWeaponPresentation>();
        presenter.Animator = c.Animator; presenter.BuiltInWeapon = c.root.GetComponent<WarriorAvatar>().EquippedWeapon;
        presenter.Equip(c.axe);
        var rig = c.controller.RigBuilder;
        int frames = 0; float maxWrist = 0, maxPalm = 0;
        try
        {
            rig.StopPreview();
            if (!rig.Build()) throw new InvalidOperationException("Cannot build runtime axe rig.");
            rig.graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = c.Animator;
            playable.Rebind(); playable.Update(0);
            playable.SetBool("Grounded", true); playable.SetBool("WeaponDrawn", true);
            playable.SetFloat("AttackRate", 1); playable.SetFloat("RollSpeed", 1);
            void Step(float seconds)
            {
                for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++)
                {
                    presenter.BeginPoseEvaluation(); rig.SyncLayers(); playable.Update(1f / 60); rig.Evaluate(0);
                    float stored = playable.GetFloat("EquipmentBackWeight");
                    presenter.RefreshPose(stored);
                    var settings = presenter.Settings(c.axe);
                    if (settings.Weight > .999f && stored < .001f) foreach (var grip in c.axe.HandGrips)
                    {
                        bool left = grip.Hand == HumanBodyBones.LeftHand;
                        var model = presenter.ModelFor(c.axe).transform;
                        var hand = c.Animator.GetBoneTransform(grip.Hand);
                        var target = model.TransformPoint(grip.Position + (left ? settings.LeftHandPosition : settings.HandPosition));
                        maxWrist = Mathf.Max(maxWrist, Vector3.Distance(hand.position, target));
                        var palm = model.InverseTransformPoint(hand.position + hand.rotation * (grip.PalmOffset * Mathf.Abs(model.lossyScale.x)));
                        maxPalm = Mathf.Max(maxPalm, new Vector2(palm.x, palm.y).magnitude * Mathf.Abs(model.lossyScale.x));
                        if (maxWrist > .003f || maxPalm > .003f) throw new InvalidOperationException("Axe loses handle during transition: wrist=" + maxWrist + " palm=" + maxPalm + " state=" + playable.GetCurrentAnimatorStateInfo(0).shortNameHash);
                    }
                    frames++;
                }
            }
            Step(.3f);
            for (int attack = 0; attack < 9; attack++)
            {
                playable.SetBool("DefensiveHold", attack == 6);
                playable.CrossFadeInFixedTime("Attack" + attack, .09f, 0, 0);
                Step(.4f);
                playable.SetBool("DefensiveHold", false);
                playable.CrossFadeInFixedTime("Locomotion", .09f, 0, 0); Step(.2f);
            }
            foreach (var direction in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right, Vector2.zero })
            { playable.SetFloat("MoveX", direction.x); playable.SetFloat("MoveY", direction.y); Step(.35f); }
            playable.SetBool("Grounded", false); Step(1.4f);
            playable.SetBool("Grounded", true); Step(.8f);
            playable.SetBool("Rolling", true); playable.CrossFadeInFixedTime("RollForward", .04f, 0, 0); Step(.8f);
            playable.SetBool("Rolling", false); Step(.25f);
            playable.SetBool("WeaponDrawn", false); Step(3);
            if (playable.GetFloat("EquipmentBackWeight") < .99f) throw new InvalidOperationException("Axe holster weight does not reach back attachment.");
            playable.SetBool("WeaponDrawn", true); Step(3);
            if (playable.GetFloat("EquipmentBackWeight") > .01f) throw new InvalidOperationException("Axe draw weight does not release back attachment.");
            report.AppendLine(string.Format(CultureInfo.InvariantCulture, "PASS Animator transitions: {0} frames; max wrist {1:F3} mm; max palm-to-shaft {2:F3} mm; attacks, four movement directions, jump/fall/land, roll, holster/draw.", frames, maxWrist * 1000, maxPalm * 1000));
        }
        finally { presenter.BeginPoseEvaluation(); rig.Clear(); }
    }
    static void RenderSheets(Context c, List<AnimationClip> clips)
    {
        const int width = 330, height = 420, columns = 6;
        var preview = c.preview;
        preview.camera.orthographic = true; preview.camera.orthographicSize = 1.25f;
        preview.camera.nearClipPlane = .01f; preview.camera.farClipPlane = 30;
        preview.camera.transform.position = new Vector3(2.7f, 1.9f, 4);
        preview.camera.transform.LookAt(new Vector3(0, 1.08f, 0));
        preview.lights[0].intensity = 1.4f; preview.lights[0].transform.rotation = Quaternion.Euler(35, 210, 0);
        preview.lights[1].intensity = 1; preview.ambientColor = new Color(.4f, .4f, .45f);
        for (int phase = 0; phase < 3; phase++)
        {
            int rows = Mathf.CeilToInt(clips.Count / (float)columns);
            var sheet = new Texture2D(width * columns, height * rows, TextureFormat.RGB24, false);
            for (int n = 0; n < clips.Count; n++)
            {
                c.Start(clips[n]); c.Sample(clips[n], .2f + phase * .3f);
                // Preview scenes can retain stale GPU skinning buffers when a
                // manual graph is sampled repeatedly within one editor tick.
                // Bake the current bones for each review image.
                var bakedObjects = new List<GameObject>(); var bakedMeshes = new List<Mesh>();
                var skins = c.root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s => s.enabled).ToArray();
                foreach (var skin in skins)
                {
                    var mesh = new Mesh(); skin.BakeMesh(mesh);
                    var item = new GameObject("Pose review mesh"); item.transform.SetParent(skin.transform, false);
                    item.AddComponent<MeshFilter>().sharedMesh = mesh;
                    item.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                    skin.enabled = false; bakedObjects.Add(item); bakedMeshes.Add(mesh);
                }
                preview.BeginStaticPreview(new Rect(0, 0, width, height)); preview.Render(true);
                var picture = preview.EndStaticPreview();
                sheet.SetPixels(n % columns * width, (rows - 1 - n / columns) * height, width, height, picture.GetPixels());
                UnityEngine.Object.DestroyImmediate(picture);
                foreach (var item in bakedObjects) UnityEngine.Object.DestroyImmediate(item);
                foreach (var mesh in bakedMeshes) UnityEngine.Object.DestroyImmediate(mesh);
                foreach (var skin in skins) skin.enabled = true;
                c.Stop();
            }
            sheet.Apply();
            File.WriteAllBytes(ReportFolder + "/axe-ik-poses-" + phase + ".png", sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
        }
        File.WriteAllText(ReportFolder + "/axe-ik-poses-index.txt", string.Join("\n", clips.Select((clip, i) => (i + 1) + ". " + clip.name)));
    }
}
