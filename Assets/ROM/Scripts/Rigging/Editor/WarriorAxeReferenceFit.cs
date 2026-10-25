using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

public static class WarriorAxeReferenceFit
{
    const string Folder = "Documentation/warrior-rebuild";
    static readonly string[] Approved = { "Idle", "Run-Forward", "Run-Backward", "Run-Left", "Run-Right", "Attack1" };
    static readonly string[] Targets = Enumerable.Range(2, 8).Select(i => "Attack" + i).Concat(new[] { "Jump", "Fall", "Land", "DiveRoll-Forward1", "Sheath-Back-Unarmed", "Unsheath-Back-Unarmed" }).ToArray();
    sealed class Pose
    {
        public Vector3 position, hint;
        public Quaternion rotation;
        public string reference;
        public float score, twist, slide;
    }
    internal struct Arm
    {
        public Vector3 shoulder, elbow, wrist, forward;
        public Quaternion rotation;
        public float a, b;
    }
    struct Sample
    {
        public Vector3 position;
        public Quaternion rotation, body;
        public float scale;
        public Arm right, left;
    }
    static AnimationClip Clip(WarriorAxeIKCalibration.Context c, string name) => (AnimationClip)c.Call("FindClip", name);
    static EquipmentAnimationIK Entry(WeaponDefinition axe, AnimationClip clip) => axe.AnimationIK.Single(e => e.Clip == clip);
    static string ManualSnapshot(EquipmentAnimationIK entry)
    {
        var copy=JsonUtility.FromJson<EquipmentAnimationIK>(JsonUtility.ToJson(entry));
        copy.RightThumbRotation=copy.LeftThumbRotation=Vector3.zero;
        return JsonUtility.ToJson(copy);
    }
    static bool Handoff(AnimationClip clip) => clip.name.Contains("Sheath-") || clip.name.Contains("Unsheath-");
    static Vector3 Signed(Vector3 p) => new Vector3(Mathf.DeltaAngle(0, p.x), Mathf.DeltaAngle(0, p.y), Mathf.DeltaAngle(0, p.z));
    internal static Arm ReadArm(Animator animator, bool left)
    {
        var upper = animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
        var lower = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
        var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
        var finger = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
        return new Arm { shoulder = upper.position, elbow = lower.position, wrist = hand.position, rotation = hand.rotation,
            forward = Quaternion.Inverse(hand.rotation) * (finger.position - hand.position).normalized,
            a = Vector3.Distance(upper.position, lower.position), b = Vector3.Distance(lower.position, hand.position) };
    }
    static Sample ReadSample(WarriorAxeIKCalibration.Context c)
    {
        var model = c.Presenter.ModelFor(c.axe).transform;
        return new Sample { position = model.position, rotation = model.rotation, scale = Mathf.Abs(model.lossyScale.x), body = WeaponGripSpace.BodyRotation(c.Animator), right = ReadArm(c.Animator, false), left = ReadArm(c.Animator, true) };
    }
    static List<Sample> ReadSamples(WarriorAxeIKCalibration.Context c, AnimationClip clip)
    {
        var samples = new List<Sample>();
        bool enabled = c.axe.EnableHandIK;
        try
        {
            c.axe.EnableHandIK = false; c.Start(clip);
            for (int i = 0; i <= 60; i++) { c.Sample(clip, i / 60f); samples.Add(ReadSample(c)); }
        }
        finally { c.Stop(); c.axe.EnableHandIK = enabled; }
        return samples;
    }
    static Pose Template(WeaponDefinition axe, EquipmentAnimationIK entry, bool left)
    {
        var grip = axe.HandGrips.Single(p => p.Hand == (left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand));
        return new Pose { position = grip.Position + (left ? entry.LeftHandPosition : entry.HandPosition),
            rotation = Quaternion.Euler(grip.Rotation) * Quaternion.Euler(left ? entry.LeftHandRotation : entry.HandRotation),
            hint = left ? entry.LeftElbowOffset : entry.ElbowOffset, reference = entry.Clip.name };
    }
    internal static Vector3 Limit(Arm arm, Vector3 target)
    {
        var offset = target - arm.shoulder; float distance = offset.magnitude;
        float reach = (arm.a + arm.b) * .995f;
        return distance > reach ? offset * (reach / distance - 1) : Vector3.zero;
    }
    internal static float ArmScore(Arm arm, Vector3 target, Quaternion rotation, Vector3 hint, out float wristAngle, out float elbowAngle)
    {
        var direction = (target - arm.shoulder).normalized;
        float distance = Mathf.Clamp(Vector3.Distance(target, arm.shoulder), Mathf.Abs(arm.a - arm.b) + .00001f, arm.a + arm.b - .00001f);
        var pole = Vector3.ProjectOnPlane(arm.elbow + hint - arm.shoulder, direction);
        if (pole.sqrMagnitude < .0000001f) pole = Vector3.Cross(direction, Vector3.up);
        float along = (arm.a * arm.a - arm.b * arm.b + distance * distance) / (2 * distance);
        var elbow = arm.shoulder + direction * along + pole.normalized * Mathf.Sqrt(Mathf.Max(0, arm.a * arm.a - along * along));
        elbowAngle = Vector3.Angle(arm.shoulder - elbow, target - elbow);
        wristAngle = Vector3.Angle(target - elbow, rotation * arm.forward);
        float wristPenalty = wristAngle * wristAngle / 1200f + Mathf.Pow(Mathf.Max(0, wristAngle - 45), 2) / 90f;
        float elbowPenalty = Mathf.Pow(Mathf.Max(0, 65 - elbowAngle), 2) / 60f + Mathf.Pow(Mathf.Max(0, elbowAngle - 165), 2) / 200f;
        return wristPenalty + elbowPenalty + Quaternion.Angle(arm.rotation, rotation) / 500f;
    }
    static List<Pose> Candidates(List<Sample> samples, List<Pose> templates, bool left, float desiredContact, Vector3 palm)
    {
        var choices = new List<Pose>();
        foreach (var template in templates)
        for (int angle = -180; angle < 180; angle += 15)
        foreach (var hint in new[] { template.hint, new Vector3(left ? -.08f : .08f,-.04f,.03f), new Vector3(left ? -.14f : .14f,.02f,-.03f), new Vector3(0,-.04f,.1f) })
        {
            var turn = Quaternion.AngleAxis(angle, Vector3.forward);
            var pose = new Pose { rotation = turn * template.rotation, position = turn * template.position, reference = template.reference, hint = hint };
            float referenceContact = (pose.position + pose.rotation * palm).z;
            pose.position.z += desiredContact - referenceContact;
            float sum = 0, maximum = 0;
            foreach (var sample in samples)
            {
                var arm = left ? sample.left : sample.right;
                var target = sample.position + sample.rotation * (pose.position * sample.scale);
                var score = ArmScore(arm, target + Limit(arm, target), sample.rotation * pose.rotation, sample.body * pose.hint, out _, out _);
                sum += score; maximum = Mathf.Max(maximum, score);
            }
            pose.score = sum / samples.Count + maximum * .4f;
            choices.Add(pose);
        }
        return choices.OrderBy(p => p.score).Take(5).ToList();
    }
    static float PairScore(List<Sample> samples, Pose right, Pose left, Vector3 offset)
    {
        float sum = 0, maximum = 0;
        foreach (var sample in samples)
        {
            var origin = sample.position + sample.body * offset;
            var r = origin + sample.rotation * (right.position * sample.scale);
            var l = origin + sample.rotation * (left.position * sample.scale);
            Vector3 correction = Vector3.zero;
            for (int i = 0; i < 32; i++) { var previous = correction; correction += Limit(sample.right, r + correction); correction += Limit(sample.left, l + correction); if ((previous - correction).sqrMagnitude < .0000000001f) break; }
            float score = ArmScore(sample.right, r + correction, sample.rotation * right.rotation, sample.body * right.hint, out _, out _)
                + ArmScore(sample.left, l + correction, sample.rotation * left.rotation, sample.body * left.hint, out _, out _);
            score += correction.sqrMagnitude * 20;
            sum += score; maximum = Mathf.Max(maximum, score);
        }
        return sum / samples.Count + maximum * .6f + offset.sqrMagnitude * 15 + Mathf.Pow(Mathf.Max(0,.15f-Mathf.Abs(right.position.z-left.position.z)),2)*2000;
    }
    static Pose AdaptiveBasis(List<Sample> samples, List<Pose> templates, bool left, float contact, Vector3 palm, Vector3 offset)
    {
        Pose best=null; float bestScore=float.MaxValue;
        foreach(var template in templates)
        {
            var basis=new Pose{position=template.position,rotation=template.rotation,hint=template.hint,reference=template.reference};
            basis.position.z+=contact-(basis.position+basis.rotation*palm).z;
            float sum=0,maximum=0; int count=0;
            for(int i=0;i<samples.Count;i+=5)
            {
                var choices=FrameChoices(samples[i],basis,left,offset,0,true);
                float score=choices[0].score;sum+=score;maximum=Mathf.Max(maximum,score);count++;
            }
            float total=sum/count+maximum;
            if(total<bestScore){bestScore=total;best=basis;}
        }
        return best;
    }
    static List<Pose> FrameChoices(Sample sample, Pose basis, bool left, Vector3 offset, float previous, bool first)
    {
        var choices = new List<Pose>();
        foreach (var hint in new[] { basis.hint, new Vector3(left ? -.1f : .1f,-.04f,.06f), new Vector3(left ? -.2f : .2f,.03f,-.04f), new Vector3(0,-.08f,.16f) })
        for (int twist = -180; twist < 180; twist += 10)
        foreach (float slide in new[] { -.06f, -.03f, 0f, left ? .03f : .015f, left ? .06f : .015f })
        {
            var turn = Quaternion.AngleAxis(twist, Vector3.forward);
            var pose = new Pose { position = turn * basis.position + Vector3.forward * slide, rotation = turn * basis.rotation, hint = hint, twist = twist, slide = slide };
            var arm = left ? sample.left : sample.right;
            var target = sample.position + sample.body * offset + sample.rotation * (pose.position * sample.scale);
            pose.score = ArmScore(arm, target + Limit(arm,target), sample.rotation * pose.rotation, sample.body * hint, out _, out _) + slide*slide*30;
            if (!first) pose.score += Mathf.Pow(Mathf.DeltaAngle(previous, twist), 2) / 2500f;
            choices.Add(pose);
        }
        return choices.OrderBy(p => p.score).Take(4).ToList();
    }
    static EquipmentPoseKey[] FitTrajectory(List<Sample> samples, Pose right, Pose left, Vector3 baseOffset, bool loop, bool allowTilt, out EquipmentPoseKey[] unfiltered)
    {
        var keys = new EquipmentPoseKey[samples.Count];
        float previousRight = 0, previousLeft = 0;
        var previousOffset = baseOffset;
        var previousRotation=Vector3.zero;
        var single = new List<Sample> { samples[0] };
        for (int frame = 0; frame < samples.Count; frame++)
        {
            var sample = samples[frame]; single[0] = sample;
            var rChoices = FrameChoices(sample, right, false, previousOffset, previousRight, frame == 0);
            var lChoices = FrameChoices(sample, left, true, previousOffset, previousLeft, frame == 0);
            var offsets = new[] { previousOffset, previousOffset+new Vector3(0,0,.025f), previousOffset-new Vector3(0,0,.025f), previousOffset+new Vector3(0,.025f,0), previousOffset-new Vector3(0,.025f,0),
                baseOffset, new Vector3(0,0,.16f), new Vector3(0,0,.26f), new Vector3(0,-.1f,.18f), new Vector3(0,.1f,.18f) };
            Pose bestR = null, bestL = null; Vector3 bestOffset = previousOffset, bestRotation=previousRotation; float best = float.MaxValue;
            var rotations=allowTilt?new[]{Vector3.zero,previousRotation, new Vector3(-25,0,0),new Vector3(25,0,0),new Vector3(0,-25,0),new Vector3(0,25,0),new Vector3(0,0,-25),new Vector3(0,0,25)}:new[]{Vector3.zero};
            foreach (var r in rChoices) foreach (var l in lChoices) foreach (var rawOffset in offsets)
            foreach(var rotation in rotations)
            {
                var offset = new Vector3(Mathf.Clamp(rawOffset.x,-.12f,.12f), Mathf.Clamp(rawOffset.y,-.18f,.18f),Mathf.Clamp(rawOffset.z,0,.3f));
                var tilted=sample; tilted.rotation=sample.body*Quaternion.Euler(rotation)*Quaternion.Inverse(sample.body)*sample.rotation; single[0]=tilted;
                float score = PairScore(single, r, l, offset)+rotation.sqrMagnitude*.0004f;
                if(frame>0)score+=(rotation-previousRotation).sqrMagnitude*.001f;
                if (frame > 0) score += Mathf.Pow(Mathf.DeltaAngle(previousRight,r.twist),2)/1000f + Mathf.Pow(Mathf.DeltaAngle(previousLeft,l.twist),2)/1000f + (offset-previousOffset).sqrMagnitude*100;
                if (score < best) { best = score; bestR=r; bestL=l; bestOffset=offset;bestRotation=rotation; }
            }
            previousRight += Mathf.DeltaAngle(previousRight,bestR.twist); previousLeft += Mathf.DeltaAngle(previousLeft,bestL.twist);
            previousOffset = bestOffset;
            previousRotation=bestRotation;
            keys[frame] = new EquipmentPoseKey { Time = frame/(float)(samples.Count-1), RightGripTwist=previousRight, LeftGripTwist=previousLeft, WeaponPosition=bestOffset-baseOffset,
                RightGripSlide=bestR.slide, LeftGripSlide=bestL.slide,
                WeaponRotation=bestRotation,
                RightElbowOffset=bestR.hint-right.hint, LeftElbowOffset=bestL.hint-left.hint };
        }
        unfiltered=(EquipmentPoseKey[])keys.Clone();
        if(allowTilt)
        {
            var source=(EquipmentPoseKey[])keys.Clone();
            for(int i=0;i<keys.Length;i++)
            {
                var rotation=source[i].WeaponRotation;
                for(int j=Mathf.Max(0,i-2);j<=Mathf.Min(keys.Length-1,i+2);j++)
                    if(source[j].WeaponRotation.sqrMagnitude>rotation.sqrMagnitude)rotation=source[j].WeaponRotation;
                var key=keys[i];key.WeaponRotation=rotation;keys[i]=key;
            }
        }
        for(int pass=0;pass<2;pass++)
        {
            var source=(EquipmentPoseKey[])keys.Clone();
            for(int i=1;i<keys.Length-1;i++)
            {
                var a=source[i-1];var b=source[i];var d=source[i+1];
                b.RightGripTwist=(a.RightGripTwist+2*b.RightGripTwist+d.RightGripTwist)*.25f;
                b.LeftGripTwist=(a.LeftGripTwist+2*b.LeftGripTwist+d.LeftGripTwist)*.25f;
                b.RightGripSlide=(a.RightGripSlide+2*b.RightGripSlide+d.RightGripSlide)*.25f;
                b.LeftGripSlide=(a.LeftGripSlide+2*b.LeftGripSlide+d.LeftGripSlide)*.25f;
                b.WeaponPosition=(a.WeaponPosition+2*b.WeaponPosition+d.WeaponPosition)*.25f;
                b.WeaponRotation=(a.WeaponRotation+2*b.WeaponRotation+d.WeaponRotation)*.25f;
                b.RightElbowOffset=(a.RightElbowOffset+2*b.RightElbowOffset+d.RightElbowOffset)*.25f;
                b.LeftElbowOffset=(a.LeftElbowOffset+2*b.LeftElbowOffset+d.LeftElbowOffset)*.25f;
                keys[i]=b;
            }
        }
        if (loop) { var last=keys[0]; last.Time=1; keys[keys.Length-1]=last; }
        return keys;
    }
    static EquipmentPoseKey MixKey(EquipmentPoseKey a,EquipmentPoseKey b,float t)
    {
        return new EquipmentPoseKey{Time=a.Time,RightGripTwist=Mathf.Lerp(a.RightGripTwist,b.RightGripTwist,t),LeftGripTwist=Mathf.Lerp(a.LeftGripTwist,b.LeftGripTwist,t),
            RightGripSlide=Mathf.Lerp(a.RightGripSlide,b.RightGripSlide,t),LeftGripSlide=Mathf.Lerp(a.LeftGripSlide,b.LeftGripSlide,t),WeaponPosition=Vector3.Lerp(a.WeaponPosition,b.WeaponPosition,t),
            WeaponRotation=Vector3.Lerp(a.WeaponRotation,b.WeaponRotation,t),RightElbowOffset=Vector3.Lerp(a.RightElbowOffset,b.RightElbowOffset,t),LeftElbowOffset=Vector3.Lerp(a.LeftElbowOffset,b.LeftElbowOffset,t)};
    }
    static void PreserveComfort(WarriorAxeIKCalibration.Context c,AnimationClip clip,EquipmentAnimationIK entry,EquipmentPoseKey[] original)
    {
        c.Start(clip);
        try
        {
            for(int i=0;i<entry.PoseKeys.Length;i++)
            {
                var smooth=entry.PoseKeys[i];
                foreach(float blend in new[]{1f,.75f,.5f,.25f,0f})
                {
                    entry.PoseKeys[i]=MixKey(original[i],smooth,blend);
                    c.Sample(clip,original[i].Time);
                    float bend=0,elbow=180;
                    foreach(bool left in new[]{false,true})
                    {
                        var arm=ReadArm(c.Animator,left);
                        bend=Mathf.Max(bend,Vector3.Angle(arm.wrist-arm.elbow,arm.rotation*arm.forward));
                        elbow=Mathf.Min(elbow,Vector3.Angle(arm.shoulder-arm.elbow,arm.wrist-arm.elbow));
                    }
                    if(bend<=74.9f&&elbow>=60.1f)break;
                }
            }
        }
        finally{c.Stop();}
    }
    [MenuItem("ROM/Animation Rigging/Fit Remaining Axe Animations From Approved Poses")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Folder);
        using (var c = new WarriorAxeIKCalibration.Context())
        {
            var axe = c.axe;
            var approved = Approved.Select(n => Entry(axe, Clip(c, n))).ToArray();
            var snapshots = approved.Select(ManualSnapshot).ToArray();
            var gripSnapshot = string.Join("\n", axe.HandGrips.Select(JsonUtility.ToJson));
            var rightTemplates = approved.Select(e => Template(axe, e, false)).ToList();
            var leftTemplates = approved.Select(e => Template(axe, e, true)).ToList();
            var data = c.controller.GripConstraint.data;
            var report = new StringBuilder("Fit from artist-approved grip templates. Original wrist/weapon/elbow values for Idle, four Run clips, Attack1 and global HandGrips are preserved. Thumb corrections are independent.\n");
            Undo.RecordObject(axe, "Fit remaining axe poses from approved hand grips");
            foreach (var name in Targets)
            {
                var clip = Clip(c, name); if (!clip) throw new InvalidOperationException("Missing axe clip: " + name);
                var entry = Entry(axe, clip);
                if (Handoff(clip)) { entry.Weight = 0; report.AppendLine(clip.name + ": authored hand release; approved right-hand carry grip."); continue; }
                var samples = ReadSamples(c, clip);
                float leftZ = samples.Select(s => (Quaternion.Inverse(s.rotation) * (s.left.wrist + s.left.rotation * (data.leftPalmOffset * s.scale) - s.position) / s.scale).z).OrderBy(z => z).ElementAt(samples.Count / 2);
                leftZ = Mathf.Clamp(leftZ, -.58f, -.26f);
                var right = Candidates(samples, rightTemplates, false, -.08f, data.rightPalmOffset);
                var left = Candidates(samples, leftTemplates, true, leftZ, data.leftPalmOffset);
                Pose bestRight = null, bestLeft = null; Vector3 bestOffset = Vector3.zero; float best = float.MaxValue;
                var offsets = new[] { Vector3.zero, new Vector3(0,0,.04f), new Vector3(0,0,.08f), new Vector3(0,0,.12f), new Vector3(0,-.04f,.06f), new Vector3(0,.04f,.06f), new Vector3(-.04f,0,.06f), new Vector3(.04f,0,.06f) };
                foreach (var r in right) foreach (var l in left) foreach (var offset in offsets)
                {
                    float score = PairScore(samples, r, l, offset);
                    if (score < best) { best = score; bestRight = r; bestLeft = l; bestOffset = offset; }
                }
                if(name=="Attack9")
                {
                    bestRight=AdaptiveBasis(samples,rightTemplates,false,-.08f,data.rightPalmOffset,bestOffset);
                    bestLeft=AdaptiveBasis(samples,leftTemplates,true,leftZ,data.leftPalmOffset,bestOffset);
                }
                var rGrip = axe.HandGrips.Single(g => g.Hand == HumanBodyBones.RightHand);
                var lGrip = axe.HandGrips.Single(g => g.Hand == HumanBodyBones.LeftHand);
                entry.Weight = 1; entry.WeaponPosition = bestOffset; entry.WeaponRotation = Vector3.zero;
                entry.HandPosition = bestRight.position - rGrip.Position;
                entry.HandRotation = Signed((Quaternion.Inverse(Quaternion.Euler(rGrip.Rotation)) * bestRight.rotation).eulerAngles);
                entry.LeftHandPosition = bestLeft.position - lGrip.Position;
                entry.LeftHandRotation = Signed((Quaternion.Inverse(Quaternion.Euler(lGrip.Rotation)) * bestLeft.rotation).eulerAngles);
                entry.ElbowOffset = bestRight.hint; entry.LeftElbowOffset = bestLeft.hint;
                entry.RightThumbRotation = entry.LeftThumbRotation = Vector3.zero;
                entry.PoseKeys = FitTrajectory(samples, bestRight, bestLeft, bestOffset, clip.isLooping,name=="Attack9",out var unfiltered);
                PreserveComfort(c,clip,entry,unfiltered);
                report.AppendLine(clip.name + ": right " + bestRight.reference + "; left " + bestLeft.reference + "; comfort score " + best.ToString("F3", CultureInfo.InvariantCulture));
            }
            var carry = rightTemplates[0];
            var carryPosition = carry.position;
            carryPosition.z += -.55f - (carry.position + carry.rotation * data.rightPalmOffset).z;
            axe.UseApprovedCarryGrip = true; axe.CarryGripPosition = carryPosition; axe.CarryGripRotation = carry.rotation.eulerAngles;
            for (int i = 0; i < approved.Length; i++) if (ManualSnapshot(approved[i]) != snapshots[i]) throw new InvalidOperationException("Approved pose changed: " + approved[i].Clip.name);
            if (string.Join("\n", axe.HandGrips.Select(JsonUtility.ToJson)) != gripSnapshot) throw new InvalidOperationException("Global hand grips changed.");
            FitThumbs(c, report);
            for (int i = 0; i < approved.Length; i++) if (ManualSnapshot(approved[i]) != snapshots[i]) throw new InvalidOperationException("Approved manual values changed during thumb fitting.");
            report.AppendLine("PASS original six manual wrist/weapon/elbow profiles and global grips preserved; independent thumb corrections applied.");
            EditorUtility.SetDirty(axe); AssetDatabase.SaveAssetIfDirty(axe);
            File.WriteAllText(Folder + "/axe-reference-fit.txt", report.ToString());
            File.WriteAllText(Folder + "/axe-approved-grips.json.txt", gripSnapshot + "\n" + string.Join("\n", snapshots));
            ProbeThumbs(c, report);
            ValidateAndRender(c, report);
        }
        Debug.Log("WARRIOR_AXE_APPROVED_REFERENCE_FIT_PASS");
    }
    static float ThumbClearance(WarriorAxeIKCalibration.Context c, bool left, float radius)
    {
        var thumb = c.Animator.GetBoneTransform(left ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal);
        var model = c.Presenter.ModelFor(c.axe).transform;
        float minimum = float.MaxValue;
        foreach (var skin in c.root.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!skin.sharedMesh) continue;
            var weights = skin.sharedMesh.boneWeights; if (weights.Length == 0) continue;
            var bones = skin.bones;
            bool Thumb(int index) => index >= 0 && index < bones.Length && bones[index] && (bones[index] == thumb || bones[index].IsChildOf(thumb));
            if (!bones.Any(b => b && (b == thumb || b.IsChildOf(thumb)))) continue;
            var mesh = new Mesh(); skin.BakeMesh(mesh); var vertices = mesh.vertices;
            for (int i = 0; i < weights.Length; i++)
            {
                var w = weights[i];
                float total = (Thumb(w.boneIndex0)?w.weight0:0)+(Thumb(w.boneIndex1)?w.weight1:0)+(Thumb(w.boneIndex2)?w.weight2:0)+(Thumb(w.boneIndex3)?w.weight3:0);
                if (total < .65f) continue;
                var p = model.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
                if (p.z < -.75f || p.z > -.02f) continue;
                minimum = Mathf.Min(minimum, (new Vector2(p.x,p.y).magnitude-radius)*Mathf.Abs(model.lossyScale.x));
            }
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        return minimum;
    }
    static void ProbeThumbs(WarriorAxeIKCalibration.Context c, StringBuilder report)
    {
        c.Start(Clip(c,"Idle")); c.Sample(Clip(c,"Idle"),.5f);
        var mesh = c.Presenter.ModelFor(c.axe).GetComponent<MeshFilter>().sharedMesh;
        var radii = mesh.vertices.Where(v=>v.z < -.08f && v.z > -.65f).Select(v=>new Vector2(v.x,v.y).magnitude).OrderBy(r=>r).ToArray();
        float radius = radii.Length > 0 ? radii[radii.Length/2] : .02f;
        report.AppendLine("Handle median mesh radius: " + radius.ToString("F5",CultureInfo.InvariantCulture)+" model units");
        c.Stop();
        foreach (string name in Approved.Concat(Targets.Where(n=>!n.Contains("Sheath-")&&!n.Contains("Unsheath-"))))
        {
            var clip=Clip(c,name); c.Start(clip); c.Sample(clip,.5f);
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,"Thumb mesh clearance {0}: right {1:F2} mm, left {2:F2} mm",name,ThumbClearance(c,false,radius)*1000,ThumbClearance(c,true,radius)*1000));
            c.Stop();
        }
    }
    static void FitThumbs(WarriorAxeIKCalibration.Context c, StringBuilder report)
    {
        var probes = new List<Vector3> { Vector3.zero };
        foreach (float angle in new[] { -40f,-30f,-20f,-10f,10f,20f,30f,40f })
        { probes.Add(new Vector3(angle,0,0)); probes.Add(new Vector3(0,angle,0)); probes.Add(new Vector3(0,0,angle)); }
        foreach (float a in new[] { -20f,20f }) foreach (float b in new[] { -20f,20f })
        { probes.Add(new Vector3(a,b,0)); probes.Add(new Vector3(a,0,b)); probes.Add(new Vector3(0,a,b)); }
        foreach (var name in Approved.Concat(Targets))
        {
            var clip=Clip(c,name); if(Handoff(clip)) continue;
            var entry=Entry(c.axe,clip); c.Start(clip);
            var model=c.Presenter.ModelFor(c.axe);
            var radii=model.GetComponent<MeshFilter>().sharedMesh.vertices.Where(v=>v.z<-.08f&&v.z>-.65f).Select(v=>new Vector2(v.x,v.y).magnitude).OrderBy(r=>r).ToArray();
            float radius=radii.Length>0?radii[radii.Length/2]:.02f;
            foreach(bool left in new[]{false,true})
            {
                float Clearance(Vector3 offset)
                {
                    if(left)entry.LeftThumbRotation=offset;else entry.RightThumbRotation=offset;
                    float minimum=float.MaxValue;
                    foreach(float phase in new[]{0f,.25f,.5f,.75f,1f}){c.Sample(clip,phase);minimum=Mathf.Min(minimum,ThumbClearance(c,left,radius));}
                    return minimum;
                }
                float baseline=Clearance(Vector3.zero), bestScore=float.MaxValue, bestClearance=baseline; Vector3 best=Vector3.zero;
                if(baseline<-.001f) foreach(var probe in probes)
                {
                    float clearance=Clearance(probe);
                    float score=Mathf.Pow(Mathf.Max(0,.0005f-clearance)*1000,2)*10+probe.sqrMagnitude*.002f;
                    if(score<bestScore){bestScore=score;best=probe;bestClearance=clearance;}
                }
                if(left)entry.LeftThumbRotation=best;else entry.RightThumbRotation=best;
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,"Thumb fit {0} {1}: {2:F2} -> {3:F2} mm; joint offset {4}",name,left?"left":"right",baseline*1000,bestClearance*1000,best));
            }
            c.Stop();
        }
    }
    static void ValidateAndRender(WarriorAxeIKCalibration.Context c, StringBuilder report)
    {
        var all = Approved.Concat(Targets).ToArray();
        foreach (var name in all)
        {
            var clip = Clip(c, name); c.Start(clip);
            float maxError = 0, maxBend = 0, minElbow = 180;
            float worstTime=0;bool worstLeft=false;
            for (int i = 0; i <= 60; i++)
            {
                c.Sample(clip, i / 60f);
                var model = c.Presenter.ModelFor(c.axe).transform;
                var settings = c.Presenter.Settings(c.axe, clip, i / 60f);
                foreach (bool left in new[] { false, true })
                {
                    var arm = ReadArm(c.Animator, left);
                    if (!float.IsFinite(arm.wrist.x)) throw new InvalidOperationException("Invalid wrist: " + name);
                    if (Handoff(clip)) continue;
                    var grip = c.axe.HandGrips.Single(g => g.Hand == (left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand));
                    var target = model.TransformPoint(grip.Position + (left ? settings.LeftHandPosition : settings.HandPosition));
                    maxError = Mathf.Max(maxError, Vector3.Distance(arm.wrist, target));
                    float bend=Vector3.Angle(arm.wrist - arm.elbow, arm.rotation * arm.forward);
                    if(bend>maxBend){maxBend=bend;worstTime=i/60f;worstLeft=left;}
                    minElbow = Mathf.Min(minElbow, Vector3.Angle(arm.shoulder - arm.elbow, arm.wrist - arm.elbow));
                }
            }
            report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}: wrist error {1:F3} mm, max wrist bend {2:F1} deg at {4:F3} ({5}), minimum elbow angle {3:F1} deg", name, maxError * 1000, maxBend, minElbow,worstTime,worstLeft?"left":"right"));
            if (maxError > .003f) throw new InvalidOperationException("Grip cannot reach target: " + name);
            File.WriteAllText(Folder+"/axe-reference-fit.txt",report.ToString());
            if(!Approved.Contains(name)&&!Handoff(clip)&&(maxBend>75||minElbow<60))throw new InvalidOperationException("New pose exceeds comfort limits: "+name+" wrist="+maxBend+" elbow="+minElbow);
            c.Stop();
        }
        File.WriteAllText(Folder + "/axe-reference-fit.txt", report.ToString());
        ValidateRuntime(c,report);
        File.WriteAllText(Folder + "/axe-reference-fit.txt", report.ToString());
        RenderReview(c);
    }
    [MenuItem("ROM/Animation Rigging/Validate Axe Against Approved Grips")]
    public static void Validate()
    {
        using(var c=new WarriorAxeIKCalibration.Context())
        {
            var report=new StringBuilder("Validation against current artist-approved grips.\n");
            ProbeThumbs(c,report);ValidateAndRender(c,report);
        }
        Debug.Log("WARRIOR_AXE_APPROVED_REFERENCE_VALIDATION_PASS");
    }
    static void ValidateRuntime(WarriorAxeIKCalibration.Context c,StringBuilder report)
    {
        c.Stop();var presenter=c.root.GetComponent<CharacterWeaponPresentation>();
        presenter.Animator=c.Animator;presenter.BuiltInWeapon=c.root.GetComponent<WarriorAvatar>().EquippedWeapon;presenter.Equip(c.axe);
        var rig=c.controller.RigBuilder;rig.StopPreview();
        if(!rig.Build())throw new InvalidOperationException("Cannot build runtime reference-fit rig.");
        rig.graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var animator=c.Animator;int frames=0,carryFrames=0,animatedFrames=0;float maximum=0;
        try
        {
            animator.Rebind();animator.Update(0);animator.SetBool("WeaponDrawn",true);animator.SetBool("Grounded",true);animator.SetFloat("AttackRate",1);animator.SetFloat("RollSpeed",1);
            void Step(float seconds)
            {
                for(int f=0;f<Mathf.CeilToInt(seconds*60);f++)
                {
                    presenter.BeginPoseEvaluation();rig.SyncLayers();animator.Update(1f/60);rig.Evaluate(0);presenter.RefreshPose();
                    var settings=presenter.Settings(c.axe);var model=presenter.ModelFor(c.axe).transform;
                    float stored=animator.GetFloat("EquipmentBackWeight");
                    if(settings.Weight>.999f&&stored<.001f)foreach(var grip in c.axe.HandGrips)
                    {
                        bool left=grip.Hand==HumanBodyBones.LeftHand;
                        var target=model.TransformPoint(grip.Position+(left?settings.LeftHandPosition:settings.HandPosition));
                        float error=Vector3.Distance(c.Animator.GetBoneTransform(grip.Hand).position,target);maximum=Mathf.Max(maximum,error);
                        if(error>.003f)throw new InvalidOperationException("Runtime wrist lost fitted grip.");
                    }
                    if(stored>.999f&&Vector3.Distance(model.position,c.controller.GripConstraint.data.backSocket.position)>.002f)throw new InvalidOperationException("Holstered axe moved off back socket.");
                    var state=animator.GetCurrentAnimatorStateInfo(0);
                    if(!animator.IsInTransition(0))
                    {
                        var infos=animator.GetCurrentAnimatorClipInfo(0);
                        if(infos.Length==1)
                        {
                            var entry=c.axe.AnimationIK.FirstOrDefault(e=>e.Clip==infos[0].clip);
                            if(entry!=null&&entry.PoseKeys.Length>0)
                            {
                                float time=entry.Clip.isLooping?Mathf.Repeat(state.normalizedTime,1):Mathf.Clamp01(state.normalizedTime);
                                var expected=entry.WeaponPosition+entry.PoseAt(time).WeaponPosition;
                                if(Vector3.Distance(expected,settings.WeaponPosition)>.001f)throw new InvalidOperationException("Runtime pose keys use incorrect clip time.");
                                animatedFrames++;
                            }
                        }
                    }
                    float follow=c.controller.GripConstraint.data.rightHandFollow;
                    if(follow>.999f&&stored<.001f)
                    {
                        var hand=c.Animator.GetBoneTransform(HumanBodyBones.RightHand);
                        if(Vector3.Distance(model.InverseTransformPoint(hand.position),c.axe.CarryGripPosition)>.003f)throw new InvalidOperationException("Carry grip does not match approved hand shape.");
                        carryFrames++;
                    }
                    frames++;
                }
            }
            Step(.3f);
            for(int attack=0;attack<9;attack++)
            {
                animator.SetBool("DefensiveHold",attack==6);animator.CrossFadeInFixedTime("Attack"+attack,.09f,0,0);Step(.8f);
                animator.SetBool("DefensiveHold",false);animator.CrossFadeInFixedTime("Locomotion",.09f,0,0);Step(.15f);
            }
            animator.SetBool("Grounded",false);Step(1.4f);animator.SetBool("Grounded",true);Step(.8f);
            animator.SetBool("Rolling",true);animator.CrossFadeInFixedTime("RollForward",.04f,0,0);Step(.8f);animator.SetBool("Rolling",false);Step(.2f);
            animator.SetBool("WeaponDrawn",false);Step(3);if(animator.GetFloat("EquipmentBackWeight")<.99f)throw new InvalidOperationException("Holster incomplete.");
            animator.SetBool("WeaponDrawn",true);Step(3);if(animator.GetFloat("EquipmentBackWeight")>.01f)throw new InvalidOperationException("Draw incomplete.");
            if(carryFrames==0||animatedFrames==0)throw new InvalidOperationException("Carry and animated pose-key paths were not exercised.");
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,"PASS runtime Animator: {0} frames, {1} pose-key frames, {2} carry-grip frames; maximum wrist error {3:F3} mm. Holster/draw and back attachment passed.",frames,animatedFrames,carryFrames,maximum*1000));
        }
        finally{presenter.BeginPoseEvaluation();rig.Clear();}
    }
    static void RenderReview(WarriorAxeIKCalibration.Context c)
    {
        var preview = c.preview;
        preview.camera.orthographic = true; preview.camera.nearClipPlane = .01f; preview.camera.farClipPlane = 30;
        preview.lights[0].intensity = 1.4f; preview.lights[0].transform.rotation = Quaternion.Euler(35, 210, 0);
        preview.lights[1].intensity = 1; preview.ambientColor = new Color(.4f, .4f, .45f);
        var clips = Targets.Select(n => Clip(c, n)).ToArray();
        const int width = 400, height = 400, columns = 4;
        for (int phase = 0; phase < 3; phase++)
        {
            int rows = Mathf.CeilToInt(clips.Length / (float)columns);
            var sheet = new Texture2D(width * columns, height * rows, TextureFormat.RGB24, false);
            for (int n = 0; n < clips.Length; n++)
            {
                c.Start(clips[n]); c.Sample(clips[n], .2f + .3f * phase);
                var left = c.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                var right = c.Animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                var focus = Handoff(clips[n])?right:(left + right) * .5f;
                preview.camera.orthographicSize = .48f;
                var body = WeaponGripSpace.BodyRotation(c.Animator);
                preview.camera.transform.position = focus + body * (Handoff(clips[n])?new Vector3(1.5f,.3f,-1.5f):new Vector3(1.2f,.45f,1.8f));
                preview.camera.transform.LookAt(focus);
                var skins = c.root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s => s.enabled).ToArray();
                var objects = new List<GameObject>(); var meshes = new List<Mesh>();
                foreach (var skin in skins)
                {
                    var mesh = new Mesh(); skin.BakeMesh(mesh); meshes.Add(mesh);
                    var item = new GameObject("Review skin"); item.transform.SetParent(skin.transform, false); objects.Add(item);
                    item.AddComponent<MeshFilter>().sharedMesh = mesh;
                    item.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials; skin.enabled = false;
                }
                preview.BeginStaticPreview(new Rect(0,0,width,height)); preview.Render(true);
                var image = preview.EndStaticPreview();
                sheet.SetPixels(n % columns * width, (rows - 1 - n / columns) * height, width, height, image.GetPixels());
                UnityEngine.Object.DestroyImmediate(image);
                foreach (var obj in objects) UnityEngine.Object.DestroyImmediate(obj);
                foreach (var mesh in meshes) UnityEngine.Object.DestroyImmediate(mesh);
                foreach (var skin in skins) skin.enabled = true;
                c.Stop();
            }
            sheet.Apply(); File.WriteAllBytes(Folder + "/axe-reference-hands-" + phase + ".png", sheet.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(sheet);
        }
        File.WriteAllText(Folder + "/axe-reference-hands-index.txt", string.Join("\n", Targets.Select((n,i) => (i+1)+". "+n)));
    }
}
