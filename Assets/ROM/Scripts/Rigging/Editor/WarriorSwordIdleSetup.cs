using System;
using System.IO;
using System.Linq;
using System.Text;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public static class WarriorSwordIdleSetup
{
    const string SourcePath = "Assets/DoubleL/FBX Unity/One Hand Up/Movement/Idle/Idle/1Hand_Up_Stand_Idle_A_2.fbx";
    public const string PoseFolder = "Assets/ROM/Animation/WarriorEquipment";
    static string PosePath(AnimationClip source) => PoseFolder + "/SwordGrip_" + source.name + ".anim";
    static float BladeSurfaceOffset(WeaponDefinition sword)
    {
        var vertices = sword.Model.GetComponentsInChildren<MeshFilter>(true).SelectMany(filter => filter.sharedMesh.vertices
            .Select(v => sword.Model.transform.InverseTransformPoint(filter.transform.TransformPoint(v))))
            .Where(v => v.z > .10f).ToArray();
        return Mathf.Abs(vertices.Min(v => v.y) * sword.Scale.y) + .001f;
    }
    public static bool IsDoubleLDerived(AnimationClip clip) => clip && (AssetDatabase.GetAssetPath(clip).StartsWith("Assets/DoubleL/") ||
        (AssetDatabase.GetAssetPath(clip).StartsWith(PoseFolder + "/SwordGrip_") && clip.name.StartsWith("1Hand_Up_")));

    public static void ApplyConfiguredClips(AnimatorOverrideController controller)
    {
        var pairs = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
        controller.GetOverrides(pairs);
        foreach (var pair in pairs)
        {
            var source = pair.Value;
            if (!source || !AssetDatabase.GetAssetPath(source).StartsWith("Assets/DoubleL/")) continue;
            var baked = AssetDatabase.LoadAssetAtPath<AnimationClip>(PosePath(source));
            if (baked) controller[pair.Key] = baked;
        }
    }

    static string CurveName(int muscle)
    {
        string name = HumanTrait.MuscleName[muscle];
        if (muscle < 75) return name;
        string[] fields = name.Split(new[] { ' ' }, 3);
        return "RightHand." + fields[1] + "." + fields[2];
    }

    [MenuItem("ROM/Animation Rigging/Validate Warrior Sword Shoulder Idle")]
    public static void Validate()
    {
        var root = PrefabUtility.LoadPrefabContents(WarriorWeaponStateSetup.PrefabPath);
        var avatar = root.GetComponent<WarriorAvatar>();
        var animator = avatar.Animator;
        var sword = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WarriorEquipmentSetup.Weapons + "/WarriorSword.asset");
        var presenter = root.GetComponent<CharacterWeaponPresentation>();
        presenter.Equip(sword);
        var graph = PlayableGraph.Create("Validate sword shoulder contact");
        var mesh = new Mesh();
        try
        {
            var clip = ((AnimatorOverrideController)sword.Controller)["2Hand-Sword-Idle"];
            var playable = AnimationClipPlayable.Create(graph, clip);
            AnimationPlayableOutput.Create(graph, "Pose", animator).SetSourcePlayable(playable);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual); graph.Play();
            var shoulder = animator.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "RightShoulderArmor");
            var model = root.transform.Find(sword.Id + " " + sword.Socket);
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var grip = animator.GetComponentInChildren<AnimatedWeaponGripConstraint>().data;
            float maxGap = 0, maxPalmError = 0, maxPenetration = float.NegativeInfinity;
            float supportOffset = BladeSurfaceOffset(sword);
            var report = new StringBuilder();
            for (int sample = 0; sample <= 60; sample++)
            {
                playable.SetTime(clip.length * sample / 60); graph.Evaluate(0); presenter.RefreshPose(0);
                SurfacePoint(animator, shoulder, mesh, out var surface, out var normal);
                var palm = hand.position + hand.rotation * (grip.rightPalmOffset * .88f);
                maxPalmError = Mathf.Max(maxPalmError, Vector3.Distance(model.TransformPoint(new Vector3(0, 0, -.04f)), palm));
                maxGap = Mathf.Max(maxGap, Vector3.Cross(surface + normal * supportOffset - model.position, model.forward).magnitude);
                foreach (var vertex in mesh.vertices) maxPenetration = Mathf.Max(maxPenetration, Vector3.Dot(shoulder.transform.TransformPoint(vertex) - model.position, model.up));
            }
            report.AppendLine("Sampled actual baked Humanoid idle: 61 frames.");
            report.AppendLine("Maximum handle-to-palm distance: " + maxPalmError.ToString("F6") + " m");
            report.AppendLine("Maximum mesh-support-to-blade-centerline distance: " + maxGap.ToString("F6") + " m");
            report.AppendLine("Maximum armor vertex above blade plane: " + maxPenetration.ToString("F6") + " m");
            report.AppendLine("Blade thickness plus surface clearance: " + supportOffset.ToString("F6") + " m");
            var curlBindings = AnimationUtility.GetCurveBindings(clip).Where(b => b.propertyName.StartsWith("RightHand.") && b.propertyName.Contains("Stretched") && !b.propertyName.Contains("Thumb")).ToArray();
            if (curlBindings.Length != 12 || curlBindings.Any(b => AnimationUtility.GetEditorCurve(clip, b).Evaluate(.4f) > -.5f))
                throw new InvalidOperationException("Right-hand finger curls are missing from the idle.");
            report.AppendLine("PASS All 12 non-thumb finger flexion curves close around the handle; thumb pose is also baked.");
            File.WriteAllText("Documentation/warrior-rebuild/sword-shoulder-validation.txt", report.ToString());
            if (maxPalmError > .003f || maxGap > .02f || maxPenetration > .005f) throw new InvalidOperationException(report.ToString());
            Debug.Log("SWORD_SHOULDER_IDLE_VALIDATION_PASS\n" + report);
        }
        finally { graph.Destroy(); UnityEngine.Object.DestroyImmediate(mesh); PrefabUtility.UnloadPrefabContents(root); }
        WarriorEquipmentValidation.Preview();
    }

    static AnimationClip CopyClip(AnimationClip source)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PosePath(source));
        if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, PosePath(source)); }
        EditorUtility.CopySerialized(source, clip);
        clip.name = source.name + " - Warrior Grip";
        return clip;
    }

    static void CompactConstantCurves(AnimationClip clip)
    {
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            var keys = curve.keys;
            if (keys.Length <= 2) continue;
            float value = keys[0].value;
            if (keys.All(k => Mathf.Abs(k.value - value) < .000001f && Mathf.Abs(k.inTangent) < .00001f && Mathf.Abs(k.outTangent) < .00001f))
                AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(keys[0].time, keys[keys.Length - 1].time, value));
        }
    }

    [MenuItem("ROM/Animation Rigging/Calibrate Warrior Sword Shoulder Idle")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before baking the sword pose.");
        var root = PrefabUtility.LoadPrefabContents(WarriorWeaponStateSetup.PrefabPath);
        var animator = root.GetComponent<WarriorAvatar>().Animator;
        var sword = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WarriorEquipmentSetup.Weapons + "/WarriorSword.asset");
        var overrides = (AnimatorOverrideController)sword.Controller;
        var source = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
        var graph = PlayableGraph.Create("Bake sword shoulder idle");
        var handler = new HumanPoseHandler(animator.avatar, animator.transform);
        var shoulder = animator.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "RightShoulderArmor");
        var mesh = new Mesh();
        try
        {
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimationClipPlayable.Create(graph, source);
            AnimationPlayableOutput.Create(graph, "Pose", animator).SetSourcePlayable(playable);
            graph.Play(); playable.SetTime(.4); graph.Evaluate(0);
            var grip = animator.GetComponentInChildren<AnimatedWeaponGripConstraint>().data;
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            for (int i = 0; i < grip.fingers.Length; i++)
                if (grip.fingers[i].IsChildOf(hand)) grip.fingers[i].localRotation = grip.fingerRotations[i];
            var closed = new HumanPose(); handler.GetHumanPose(ref closed);
            var mount = Quaternion.LookRotation(grip.rightGripAxis.normalized,
                Vector3.ProjectOnPlane(Vector3.up, grip.rightGripAxis).normalized);
            Vector3 palm = grip.rightPalmOffset * .88f;
            Vector3 handle = new Vector3(0, 0, -.04f);
            Quaternion frame = WeaponGripSpace.Rotation(animator, HumanBodyBones.RightHand);
            Quaternion frameMount = Quaternion.Inverse(frame) * hand.rotation * mount;
            Undo.RecordObject(sword, "Calibrate sword palm grip");
            sword.Position = Quaternion.Inverse(frame) * (hand.rotation * palm) - frameMount * Vector3.Scale(handle, sword.Scale);
            sword.Rotation = frameMount.eulerAngles;
            sword.UseGripFrame = true;

            var clip = CopyClip(source);
            float supportOffset = BladeSurfaceOffset(sword);
            var curves = Enumerable.Range(48, 7).Concat(Enumerable.Range(75, 20)).ToDictionary(i => i, i => new AnimationCurve());
            float largestError = 0;
            for (int sample = 0; sample <= 60; sample++)
            {
                float time = source.length * sample / 60;
                playable.SetTime(time); graph.Evaluate(0);
                var pose = new HumanPose(); handler.GetHumanPose(ref pose);
                var basis = (float[])pose.muscles.Clone();
                for (int i = 75; i < 95; i++) pose.muscles[i] = closed.muscles[i];
                handler.SetHumanPose(ref pose);
                // Hit the outer top of the actual armor triangles, rather than the shoulder joint.
                for (int pass = 0; pass < 3; pass++)
                {
                    SurfacePoint(animator, shoulder, mesh, out var point, out var normal);
                    float distance = Vector3.Distance(hand.position + hand.rotation * palm, point);
                    float bladeZ = Mathf.Clamp(distance / sword.Scale.z + handle.z, .18f, .8f);
                    var localContact = palm + mount * Vector3.Scale(new Vector3(0, 0, bladeZ) - handle, sword.Scale);
                    Optimize(handler, ref pose, basis, hand, localContact, mount * Vector3.up,
                        point + normal * supportOffset, normal);
                }
                SurfacePoint(animator, shoulder, mesh, out var finalPoint, out var finalNormal);
                Vector3 origin = hand.position + hand.rotation * palm;
                Vector3 direction = hand.rotation * mount * Vector3.forward;
                largestError = Mathf.Max(largestError, Vector3.Cross(finalPoint + finalNormal * supportOffset - origin, direction).magnitude);
                foreach (var pair in curves) pair.Value.AddKey(time, pose.muscles[pair.Key]);
            }
            foreach (var pair in curves)
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), CurveName(pair.Key)), pair.Value);
            // Match endpoints so the corrected idle loops without a wrist snap.
            foreach (var pair in curves)
            {
                var keys = pair.Value.keys;
                keys[keys.Length - 1].value = keys[0].value;
                pair.Value.keys = keys;
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), CurveName(pair.Key)), pair.Value);
            }
            CompactConstantCurves(clip);
            EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip);
            var mappings = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
            overrides.GetOverrides(mappings);
            foreach (var pair in mappings)
            {
                AnimationClip original = pair.Value;
                if (!original) continue;
                if (AssetDatabase.GetAssetPath(original).StartsWith(PoseFolder + "/SwordGrip_"))
                {
                    string originalName = original.name.Replace(" - Warrior Grip", "");
                    original = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/DoubleL/FBX Unity/One Hand Up" })
                        .Select(AssetDatabase.GUIDToAssetPath).SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>().First(c => c.name == originalName);
                }
                if (!AssetDatabase.GetAssetPath(original).StartsWith("Assets/DoubleL/")) continue;
                if (original == source) { overrides[pair.Key] = clip; continue; }
                var holding = CopyClip(original);
                for (int i = 75; i < 95; i++) AnimationUtility.SetEditorCurve(holding,
                    EditorCurveBinding.FloatCurve("", typeof(Animator), CurveName(i)), AnimationCurve.Constant(0, holding.length, closed.muscles[i]));
                CompactConstantCurves(holding);
                EditorUtility.SetDirty(holding); AssetDatabase.SaveAssetIfDirty(holding);
                overrides[pair.Key] = holding;
            }
            EditorUtility.SetDirty(overrides); AssetDatabase.SaveAssetIfDirty(overrides);
            EditorUtility.SetDirty(sword); AssetDatabase.SaveAssetIfDirty(sword);
            File.WriteAllText("Documentation/warrior-rebuild/sword-shoulder-bake.txt", "Baked 61 idle samples. Solver shoulder-line error: " + largestError.ToString("F6") + " m\n" +
                "Grip position: " + sword.Position.ToString("F5") + "\nGrip rotation: " + sword.Rotation.ToString("F3") + "\n" +
                string.Join("\n", Enumerable.Range(75, 20).Select(i => CurveName(i) + "=" + closed.muscles[i])));
        }
        finally { handler.Dispose(); graph.Destroy(); UnityEngine.Object.DestroyImmediate(mesh); PrefabUtility.UnloadPrefabContents(root); }
        WarriorEquipmentValidation.Preview();
        Debug.Log("SWORD_SHOULDER_POSE_BAKED");
    }

    public static void SurfacePoint(Animator animator, Renderer renderer, Mesh baked, out Vector3 point, out Vector3 normal)
    {
        if (renderer is SkinnedMeshRenderer skin) skin.BakeMesh(baked);
        else { var original = renderer.GetComponent<MeshFilter>().sharedMesh; baked.Clear(); baked.vertices = original.vertices; baked.triangles = original.triangles; }
        var vertices = baked.vertices.Select(renderer.transform.TransformPoint).ToArray();
        // A supporting plane at the outermost armor vertex keeps the entire
        // shoulder mesh below the blade; a downward ray can land inside the skull ornament.
        normal = (WeaponGripSpace.BodyRotation(animator) * new Vector3(.65f, .76f, 0)).normalized;
        Vector3 supportNormal = normal;
        point = vertices.OrderByDescending(v => Vector3.Dot(v, supportNormal)).First();
    }

    static void Optimize(HumanPoseHandler handler, ref HumanPose pose, float[] basis, Transform hand,
        Vector3 localPoint, Vector3 localNormal, Vector3 target, Vector3 normal)
    {
        double[] Residual()
        {
            Vector3 p = hand.position + hand.rotation * localPoint - target;
            Vector3 n = (hand.rotation * localNormal - normal) * .20f;
            return new double[] { p.x, p.y, p.z, n.x, n.y, n.z };
        }
        for (int iteration = 0; iteration < 65; iteration++)
        {
            handler.SetHumanPose(ref pose); var r = Residual(); var jacobian = new double[6, 7];
            for (int column = 0; column < 7; column++)
            {
                pose.muscles[48 + column] += .001f; handler.SetHumanPose(ref pose); var shifted = Residual();
                pose.muscles[48 + column] -= .001f;
                for (int row = 0; row < 6; row++) jacobian[row, column] = (shifted[row] - r[row]) / .001;
            }
            var matrix = new double[7, 8];
            for (int a = 0; a < 7; a++)
            {
                for (int b = 0; b < 7; b++)
                { for (int row = 0; row < 6; row++) matrix[a, b] += jacobian[row, a] * jacobian[row, b]; }
                matrix[a, a] += .00015;
                for (int row = 0; row < 6; row++) matrix[a, 7] -= jacobian[row, a] * r[row];
                matrix[a, 7] -= .000005 * (pose.muscles[48 + a] - basis[48 + a]);
            }
            for (int pivot = 0; pivot < 7; pivot++)
            {
                double diagonal = matrix[pivot, pivot];
                for (int j = pivot; j < 8; j++) matrix[pivot, j] /= diagonal;
                for (int row = 0; row < 7; row++) if (row != pivot)
                { double factor = matrix[row, pivot]; for (int j = pivot; j < 8; j++) matrix[row, j] -= factor * matrix[pivot, j]; }
            }
            for (int i = 0; i < 7; i++) pose.muscles[48 + i] = Mathf.Clamp(pose.muscles[48 + i] + Mathf.Clamp((float)matrix[i, 7], -.10f, .10f), -1.25f, 1.25f);
        }
        handler.SetHumanPose(ref pose);
    }
}
