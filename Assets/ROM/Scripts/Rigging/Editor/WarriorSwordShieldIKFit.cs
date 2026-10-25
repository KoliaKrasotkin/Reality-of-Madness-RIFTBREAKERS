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

public static class WarriorSwordShieldIKFit
{
    const string Folder="Documentation/warrior-rebuild";
    static readonly string[] Names=new[]{"Idle","Run-Forward","Run-Backward","Run-Left","Run-Right"}.Concat(Enumerable.Range(1,9).Select(i=>"Attack"+i)).Concat(new[]{"Jump","Fall","Land","DiveRoll-Forward1","Sheath-Back-Unarmed","Unsheath-Back-Unarmed","Unarmed/Idle","Unarmed/Run-Forward","Unarmed/Run-Backward","Unarmed/Run-Left","Unarmed/Run-Right","Unarmed/Jump","Unarmed/Fall","Unarmed/DiveRoll-Forward1"}).ToArray();
    struct Sample {public Vector3 position;public Quaternion rotation,body;public float scale;public WarriorAxeReferenceFit.Arm arm;}
    static AnimationClip Clip(WarriorAxeIKCalibration.Context c,string name)=>(AnimationClip)c.Call("FindClip",name);
    static bool Armed(AnimationClip clip)=>!clip.name.StartsWith("Unarmed-")&&!clip.name.Contains("Sheath-")&&!clip.name.Contains("Unsheath-")&&!clip.name.Contains("Death");
    static EquipmentAnimationIK Entry(WeaponDefinition part,AnimationClip clip)
    {
        var entry=part.AnimationIK.FirstOrDefault(e=>e.Clip==clip);
        if(entry==null){entry=new EquipmentAnimationIK{Clip=clip};part.AnimationIK=part.AnimationIK.Concat(new[]{entry}).ToArray();}
        return entry;
    }
    static string Manual(EquipmentAnimationIK entry)
    {
        var copy=JsonUtility.FromJson<EquipmentAnimationIK>(JsonUtility.ToJson(entry));copy.RightThumbRotation=copy.LeftThumbRotation=Vector3.zero;return JsonUtility.ToJson(copy);
    }
    static IEnumerable<HumanBodyBones> Fingers(bool left)
    {
        foreach(HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
        {string name=bone.ToString();if(name.StartsWith(left?"Left":"Right")&&(name.Contains("Thumb")||name.Contains("Index")||name.Contains("Middle")||name.Contains("Ring")||name.Contains("Little")))yield return bone;}
    }
    static List<Sample> Samples(WarriorAxeIKCalibration.Context c,WeaponDefinition part,AnimationClip clip,WeaponDefinition sword,WeaponDefinition shield)
    {
        bool s=sword.EnableHandIK,h=shield.EnableHandIK;var samples=new List<Sample>();
        try
        {
            sword.EnableHandIK=shield.EnableHandIK=false;c.Start(clip);
            for(int i=0;i<=60;i++)
            {
                c.Sample(clip,i/60f);var model=c.Presenter.ModelFor(part).transform;
                samples.Add(new Sample{position=model.position,rotation=model.rotation,body=WeaponGripSpace.BodyRotation(c.Animator),scale=Mathf.Abs(model.lossyScale.x),arm=WarriorAxeReferenceFit.ReadArm(c.Animator,part.IsShield)});
            }
        }
        finally{c.Stop();sword.EnableHandIK=s;shield.EnableHandIK=h;}
        return samples;
    }
    static float Score(Sample s,Vector3 wrist,Quaternion grip,Vector3 offset,Vector3 rotation,Vector3 hint,out float bend,out float elbow)
    {
        var worldRotation=s.body*Quaternion.Euler(rotation)*Quaternion.Inverse(s.body)*s.rotation;
        var target=s.position+s.body*offset+worldRotation*(wrist*s.scale);
        target+=WarriorAxeReferenceFit.Limit(s.arm,target);
        return WarriorAxeReferenceFit.ArmScore(s.arm,target,worldRotation*grip,s.body*hint,out bend,out elbow)+offset.sqrMagnitude*12+rotation.sqrMagnitude*.0004f;
    }
    static EquipmentPoseKey[] Fit(List<Sample> samples,WeaponDefinition part,EquipmentAnimationIK entry,Vector3 wrist,Quaternion grip)
    {
        var keys=new EquipmentPoseKey[samples.Count];Vector3 previousRotation=Vector3.zero,previousOffset=Vector3.zero,previousHint=Vector3.zero;
        for(int i=0;i<samples.Count;i++)
        {
            var rotations=new List<Vector3>{Vector3.zero,previousRotation};
            foreach(float angle in new[]{-45f,-30f,-15f,15f,30f,45f}){rotations.Add(new Vector3(angle,0,0));rotations.Add(new Vector3(0,angle,0));rotations.Add(new Vector3(0,0,angle));}
            foreach(var axis in new[]{Vector3.right,Vector3.up,Vector3.forward}){rotations.Add(previousRotation+axis*7.5f);rotations.Add(previousRotation-axis*7.5f);}
            var offsets=new[]{Vector3.zero,previousOffset,new Vector3(0,0,.06f),new Vector3(0,0,.12f),new Vector3(0,0,.2f),new Vector3(0,-.06f,.1f),new Vector3(0,.06f,.1f)};
            var hints=new[]{Vector3.zero,new Vector3(part.IsShield?-.07f:.07f,-.03f,.04f),new Vector3(part.IsShield?-.15f:.15f,.02f,-.03f),new Vector3(0,-.05f,.12f)};
            float best=float.MaxValue;Vector3 bestR=previousRotation,bestP=previousOffset,bestH=previousHint;
            foreach(var rawRotation in rotations)foreach(var offset in offsets)foreach(var hint in hints)
            {
                var rotation=new Vector3(Mathf.Clamp(rawRotation.x,-60,60),Mathf.Clamp(rawRotation.y,-60,60),Mathf.Clamp(rawRotation.z,-60,60));
                float score=Score(samples[i],wrist,grip,entry.WeaponPosition+offset,rotation,hint,out _,out _);
                if(i>0)score+=(rotation-previousRotation).sqrMagnitude*.001f+(offset-previousOffset).sqrMagnitude*80+(hint-previousHint).sqrMagnitude*10;
                if(score<best){best=score;bestR=rotation;bestP=offset;bestH=hint;}
            }
            previousRotation=bestR;previousOffset=bestP;previousHint=bestH;
            keys[i]=new EquipmentPoseKey{Time=i/60f,WeaponPosition=bestP,WeaponRotation=bestR,RightElbowOffset=part.IsShield?Vector3.zero:bestH,LeftElbowOffset=part.IsShield?bestH:Vector3.zero};
        }
        var original=(EquipmentPoseKey[])keys.Clone();
        for(int pass=0;pass<2;pass++)
        {
            var source=(EquipmentPoseKey[])keys.Clone();
            for(int i=1;i<keys.Length-1;i++)
            {
                var k=source[i];k.WeaponPosition=(source[i-1].WeaponPosition+2*k.WeaponPosition+source[i+1].WeaponPosition)*.25f;
                k.WeaponRotation=(source[i-1].WeaponRotation+2*k.WeaponRotation+source[i+1].WeaponRotation)*.25f;
                k.RightElbowOffset=(source[i-1].RightElbowOffset+2*k.RightElbowOffset+source[i+1].RightElbowOffset)*.25f;
                k.LeftElbowOffset=(source[i-1].LeftElbowOffset+2*k.LeftElbowOffset+source[i+1].LeftElbowOffset)*.25f;
                keys[i]=k;
            }
        }
        for(int i=0;i<keys.Length;i++)
        {
            Score(samples[i],wrist,grip,entry.WeaponPosition+keys[i].WeaponPosition,keys[i].WeaponRotation,part.IsShield?keys[i].LeftElbowOffset:keys[i].RightElbowOffset,out float bend,out float elbow);
            if(bend>75||elbow<60)keys[i]=original[i];
        }
        if(entry.Clip.isLooping){var last=keys[0];last.Time=1;keys[keys.Length-1]=last;}
        return keys;
    }
    [MenuItem("ROM/Animation Rigging/Calibrate Warrior Sword and Shield Grips")]
    public static void Configure()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Folder);
        using(var c=new WarriorAxeIKCalibration.Context())
        {
            var sword=c.root.GetComponent<RiftWarriorEquipment>().SwordShieldWeapon;var shield=sword.AdditionalEquipment.Single(p=>p.IsShield);
            c.Call("SetPreviewWeapon",sword);var idle=Clip(c,"Idle");
            string swordIdle=Manual(Entry(sword,idle)),shieldIdle=Manual(Entry(shield,idle));
            Vector3 mount=sword.Position,angles=sword.Rotation,scale=sword.Scale;
            c.Start(idle);c.Sample(idle,.5f);Vector3 idlePosition=c.Presenter.ModelFor(sword).transform.position;Quaternion idleRotation=c.Presenter.ModelFor(sword).transform.rotation;
            var data=c.controller.GripConstraint.data;
            foreach(var part in new[]{sword,shield})
            {
                Undo.RecordObject(part,"Calibrate sword and shield animation grips");
                if(part.FingerPoses.Length>0)continue;
                var poses=new List<EquipmentFingerPose>();
                foreach(var bone in Fingers(part.IsShield))
                {
                    var transform=c.Animator.GetBoneTransform(bone);if(!transform)continue;
                    int index=Array.IndexOf(data.fingers,transform);
                    var rotation=part.IsShield&&index>=0?data.fingerRotations[index]:transform.localRotation;
                    poses.Add(new EquipmentFingerPose{Bone=bone,Rotation=rotation.eulerAngles});
                }
                part.FingerPoses=poses.ToArray();
            }
            c.Stop();
            ConfigureShieldHandle(c,shield,Entry(shield,idle));
            var clips=Names.Select(n=>Clip(c,n)).Where(p=>p).Distinct().ToArray();
            foreach(var part in new[]{sword,shield})
            {
                var basis=Entry(part,idle);var grip=part.HandGrips.Single();
                Vector3 wrist=grip.Position+(part.IsShield?basis.LeftHandPosition:basis.HandPosition);
                Quaternion wristRotation=Quaternion.Euler(grip.Rotation)*Quaternion.Euler(part.IsShield?basis.LeftHandRotation:basis.HandRotation);
                part.UseApprovedCarryGrip=true;part.CarryGripPosition=wrist;part.CarryGripRotation=wristRotation.eulerAngles;
                foreach(var clip in clips)
                {
                    if(clip==idle)continue;
                    var entry=Entry(part,clip);entry.Weight=Armed(clip)?1:0;entry.PoseKeys=Array.Empty<EquipmentPoseKey>();
                    if(!Armed(clip)){entry.WeaponPosition=entry.WeaponRotation=Vector3.zero;continue;}
                    entry.WeaponPosition=basis.WeaponPosition;entry.WeaponRotation=Vector3.zero;
                    entry.HandPosition=basis.HandPosition;entry.HandRotation=basis.HandRotation;entry.LeftHandPosition=basis.LeftHandPosition;entry.LeftHandRotation=basis.LeftHandRotation;
                    entry.ElbowOffset=entry.LeftElbowOffset=Vector3.zero;
                    entry.PoseKeys=Fit(Samples(c,part,clip,sword,shield),part,entry,wrist,wristRotation);
                }
                part.EnableHandIK=true;part.KeepGripsReachable=true;
                EditorUtility.SetDirty(part);AssetDatabase.SaveAssetIfDirty(part);
            }
            FitThumbs(c,sword,shield,clips);
            foreach(var part in new[]{sword,shield})foreach(var clip in clips.Where(p=>!Armed(p)))
            {
                var entry=Entry(part,clip);var basis=Entry(part,idle);entry.RightThumbRotation=basis.RightThumbRotation;entry.LeftThumbRotation=basis.LeftThumbRotation;
            }
            foreach(var part in new[]{sword,shield}){EditorUtility.SetDirty(part);AssetDatabase.SaveAssetIfDirty(part);}
            if(sword.Position!=mount||sword.Rotation!=angles||sword.Scale!=scale||Manual(Entry(sword,idle))!=swordIdle||Manual(Entry(shield,idle))!=shieldIdle)throw new InvalidOperationException("Approved idle settings changed.");
            c.Start(idle);c.Sample(idle,.5f);
            if(Vector3.Distance(c.Presenter.ModelFor(sword).transform.position,idlePosition)>.00001f||Quaternion.Angle(c.Presenter.ModelFor(sword).transform.rotation,idleRotation)>.05f)throw new InvalidOperationException("Sword shoulder idle moved.");
            c.Stop();
            ValidateContext(c,sword,shield,clips);Render(c,sword,clips);
        }
        Debug.Log("WARRIOR_SWORD_SHIELD_GRIP_FIT_PASS");
    }
    static void ConfigureShieldHandle(WarriorAxeIKCalibration.Context c,WeaponDefinition shield,EquipmentAnimationIK idle)
    {
        var axeIdle=c.axe.AnimationIK.Single(e=>e.Clip.name=="2Hand-Sword-Idle");
        var axeGrip=c.axe.HandGrips.Single(g=>g.Hand==HumanBodyBones.LeftHand);
        var axeRotation=Quaternion.Euler(axeGrip.Rotation)*Quaternion.Euler(axeIdle.LeftHandRotation);
        var axeWrist=axeGrip.Position+axeIdle.LeftHandPosition;
        var virtualPalm=axeWrist+axeRotation*c.controller.GripConstraint.data.leftPalmOffset;
        var palm=Quaternion.Inverse(axeRotation)*(new Vector3(0,0,virtualPalm.z)-axeWrist);
        var handAxis=Quaternion.Inverse(axeRotation)*Vector3.forward;
        var grip=shield.HandGrips.Single();var rotation=Quaternion.Euler(grip.Rotation)*Quaternion.Euler(idle.LeftHandRotation);
        var center=grip.Position+idle.LeftHandPosition+rotation*palm;
        // The original shield has only a front plate/rim. Give the fingers a
        // real rear handle, with room for the closed palm behind the plate.
        float depth=Mathf.Min(center.y,-.12f);grip.Position.y+=depth-center.y;center.y=depth;
        var axis=rotation*handAxis;
        var path=AssetDatabase.GetAssetPath(shield.Model);var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var wood=root.transform.Find("Wooden face").GetComponent<Renderer>().sharedMaterial;
            var metal=root.transform.Find("Steel rim").GetComponent<Renderer>().sharedMaterial;
            void Part(string name,PrimitiveType type,Vector3 position,Quaternion orientation,Vector3 size,Material material)
            {
                var existing=root.transform.Find(name);var obj=existing?existing.gameObject:GameObject.CreatePrimitive(type);
                obj.name=name;obj.transform.SetParent(root.transform,false);obj.transform.localPosition=position;obj.transform.localRotation=orientation;obj.transform.localScale=size;
                var collider=obj.GetComponent<Collider>();if(collider)UnityEngine.Object.DestroyImmediate(collider);obj.GetComponent<Renderer>().sharedMaterial=material;
            }
            Part("Back grip",PrimitiveType.Cylinder,center,Quaternion.FromToRotation(Vector3.up,axis),new Vector3(.036f,.085f,.036f),wood);
            foreach(int side in new[]{-1,1})
            {
                var end=center+axis*(.07f*side);float height=Mathf.Max(.025f,-.05f-end.y);
                Part("Grip bracket "+side,PrimitiveType.Cube,end+Vector3.up*(height*.5f),Quaternion.identity,new Vector3(.025f,height,.025f),metal);
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    [MenuItem("ROM/Animation Rigging/Validate Warrior Sword and Shield Grips")]
    public static void Validate()
    {
        using(var c=new WarriorAxeIKCalibration.Context())
        {
            var sword=c.root.GetComponent<RiftWarriorEquipment>().SwordShieldWeapon;var shield=sword.AdditionalEquipment.Single(p=>p.IsShield);c.Call("SetPreviewWeapon",sword);
            var clips=Names.Select(n=>Clip(c,n)).Where(p=>p).Distinct().ToArray();ValidateContext(c,sword,shield,clips);Render(c,sword,clips);
        }
        Debug.Log("WARRIOR_SWORD_SHIELD_GRIP_VALIDATION_PASS");
    }
    static void ValidateContext(WarriorAxeIKCalibration.Context c,WeaponDefinition sword,WeaponDefinition shield,AnimationClip[] clips)
    {
        var report=new StringBuilder("Sword and shield: independent grips from approved idle; 61 samples per unique clip.\n");int samples=0;
        foreach(var clip in clips)
        {
            c.Start(clip);float maxError=0,bend=0,elbow=180;
            for(int i=0;i<=60;i++)
            {
                c.Sample(clip,i/60f);
                foreach(var part in new[]{sword,shield})
                {
                    var arm=WarriorAxeReferenceFit.ReadArm(c.Animator,part.IsShield);if(!float.IsFinite(arm.wrist.x))throw new InvalidOperationException("Invalid arm pose.");
                    if(!Armed(clip))continue;
                    var settings=c.Presenter.Settings(part,clip,i/60f);var grip=part.HandGrips.Single();var model=c.Presenter.ModelFor(part).transform;
                    float error=Vector3.Distance(arm.wrist,model.TransformPoint(grip.Position+(part.IsShield?settings.LeftHandPosition:settings.HandPosition)));maxError=Mathf.Max(maxError,error);
                    bend=Mathf.Max(bend,Vector3.Angle(arm.wrist-arm.elbow,arm.rotation*arm.forward));elbow=Mathf.Min(elbow,Vector3.Angle(arm.shoulder-arm.elbow,arm.wrist-arm.elbow));
                }
                var before=c.Animator.GetBoneTransform(HumanBodyBones.RightHand).position;c.Sample(clip,i/60f);
                if(Vector3.Distance(before,c.Animator.GetBoneTransform(HumanBodyBones.RightHand).position)>.0001f)throw new InvalidOperationException("Preview accumulates hand corrections.");samples++;
            }
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0}: wrist {1:F3} mm; max wrist bend {2:F1} deg; min elbow {3:F1} deg",clip.name,maxError*1000,bend,elbow));
            if(maxError>.003f)throw new InvalidOperationException("Cannot reach sword/shield grip: "+clip.name);c.Stop();
        }
        report.AppendLine("PASS "+samples+" samples; original idle mount and profile values preserved; independent fingers and stable preview.");
        Runtime(c,sword,shield,report);File.WriteAllText(Folder+"/sword-shield-grip-fit.txt",report.ToString());
    }
    static void Runtime(WarriorAxeIKCalibration.Context c,WeaponDefinition sword,WeaponDefinition shield,StringBuilder report)
    {
        var presenter=c.root.GetComponent<CharacterWeaponPresentation>();presenter.Animator=c.Animator;presenter.BuiltInWeapon=c.root.GetComponent<WarriorAvatar>().EquippedWeapon;presenter.Equip(sword);
        var rig=c.controller.RigBuilder;rig.StopPreview();if(!rig.Build())throw new InvalidOperationException("Cannot build runtime rig.");rig.graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var animator=c.Animator;int frames=0,carryFrames=0;
        try
        {
            animator.Rebind();animator.Update(0);animator.SetBool("WeaponDrawn",true);animator.SetBool("Grounded",true);animator.SetFloat("AttackRate",1);animator.SetFloat("RollSpeed",1);
            void Step(float seconds)
            {
                for(int i=0;i<Mathf.CeilToInt(seconds*60);i++)
                {
                    presenter.BeginPoseEvaluation();rig.SyncLayers();animator.Update(1f/60);rig.Evaluate(0);presenter.RefreshPose();float stored=animator.GetFloat("EquipmentBackWeight");
                    foreach(var part in new[]{sword,shield})
                    {
                        var model=presenter.ModelFor(part).transform;var settings=presenter.Settings(part);
                        if(settings.Weight>.999f&&stored<.001f)
                        {
                            var grip=part.HandGrips.Single();var wrist=c.Animator.GetBoneTransform(part.Socket);
                            if(Vector3.Distance(wrist.position,model.TransformPoint(grip.Position+(part.IsShield?settings.LeftHandPosition:settings.HandPosition)))>.003f)throw new InvalidOperationException("Runtime grip lost: "+part.name);
                        }
                        if(stored>.999f&&Vector3.Distance(model.position,c.Animator.GetBoneTransform(part.StoredBone).position+WeaponGripSpace.BodyRotation(c.Animator)*part.StoredPosition)>.002f)throw new InvalidOperationException("Stored item moved off back.");
                        if(settings.Weight<.001f&&stored<.001f)
                        {
                            var wrist=c.Animator.GetBoneTransform(part.Socket);
                            if(Vector3.Distance(model.InverseTransformPoint(wrist.position),part.CarryGripPosition)>.003f)throw new InvalidOperationException("Carry grip lost: "+part.name);
                            carryFrames++;
                        }
                    }
                    frames++;
                }
            }
            Step(.3f);for(int attack=0;attack<9;attack++){animator.SetBool("DefensiveHold",attack==6);animator.CrossFadeInFixedTime("Attack"+attack,.09f,0,0);Step(.75f);animator.SetBool("DefensiveHold",false);animator.CrossFadeInFixedTime("Locomotion",.09f,0,0);Step(.15f);}
            foreach(var v in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right,Vector2.zero}){animator.SetFloat("MoveX",v.x);animator.SetFloat("MoveY",v.y);Step(.3f);}
            animator.SetBool("Grounded",false);Step(1.4f);animator.SetBool("Grounded",true);Step(.8f);animator.SetBool("Rolling",true);animator.CrossFadeInFixedTime("RollForward",.04f,0,0);Step(.8f);animator.SetBool("Rolling",false);Step(.2f);
            animator.SetBool("WeaponDrawn",false);Step(3);if(animator.GetFloat("EquipmentBackWeight")<.99f)throw new InvalidOperationException("Holster incomplete.");animator.SetBool("WeaponDrawn",true);Step(3);if(animator.GetFloat("EquipmentBackWeight")>.01f)throw new InvalidOperationException("Draw incomplete.");
            if(carryFrames==0)throw new InvalidOperationException("Carry attachment was not exercised.");
            report.AppendLine("PASS runtime Animator: "+frames+" frames; "+carryFrames+" single-hand carry checks; nine attacks, four directions, air/land, roll and holster/draw; both items follow hands/back correctly.");
        }
        finally{presenter.BeginPoseEvaluation();rig.Clear();presenter.Equip(c.axe);}
    }
    static float ThumbClearance(WarriorAxeIKCalibration.Context c,WeaponDefinition part)
    {
        var model=c.Presenter.ModelFor(part).transform;var thumb=c.Animator.GetBoneTransform(part.IsShield?HumanBodyBones.LeftThumbProximal:HumanBodyBones.RightThumbProximal);
        Vector3 center=new Vector3(0,0,-.055f),axis=Vector3.forward;float radius=.018f,halfLength=.085f;
        if(part.IsShield)
        {
            var handle=model.Find("Back grip");if(!handle)return float.MaxValue;
            center=model.InverseTransformPoint(handle.position);axis=model.InverseTransformDirection(handle.up);radius=handle.localScale.x*.5f;halfLength=handle.localScale.y;
        }
        float minimum=float.MaxValue;
        foreach(var skin in c.root.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if(!skin.sharedMesh)continue;var bones=skin.bones;
            bool Thumb(int index)=>index>=0&&index<bones.Length&&bones[index]&&(bones[index]==thumb||bones[index].IsChildOf(thumb));
            if(!bones.Any(b=>b&&(b==thumb||b.IsChildOf(thumb))))continue;
            var weights=skin.sharedMesh.boneWeights;var baked=new Mesh();skin.BakeMesh(baked);var vertices=baked.vertices;
            for(int i=0;i<weights.Length;i++)
            {
                var w=weights[i];float amount=(Thumb(w.boneIndex0)?w.weight0:0)+(Thumb(w.boneIndex1)?w.weight1:0)+(Thumb(w.boneIndex2)?w.weight2:0)+(Thumb(w.boneIndex3)?w.weight3:0);if(amount<.65f)continue;
                var p=model.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]))-center;float along=Vector3.Dot(p,axis);
                if(Mathf.Abs(along)>halfLength)continue;
                minimum=Mathf.Min(minimum,(Vector3.ProjectOnPlane(p,axis).magnitude-radius)*Mathf.Abs(model.lossyScale.x));
            }
            UnityEngine.Object.DestroyImmediate(baked);
        }
        return minimum;
    }
    static void FitThumbs(WarriorAxeIKCalibration.Context c,WeaponDefinition sword,WeaponDefinition shield,AnimationClip[] clips)
    {
        var probes=new List<Vector3>{Vector3.zero};foreach(float angle in new[]{-30f,-20f,-10f,10f,20f,30f}){probes.Add(new Vector3(angle,0,0));probes.Add(new Vector3(0,angle,0));probes.Add(new Vector3(0,0,angle));}
        var report=new StringBuilder("Thumb mesh to grip clearance over five phases; positive values are outside grip cylinder.\n");
        foreach(var clip in clips.Where(Armed))
        {
            c.Start(clip);
            foreach(var part in new[]{sword,shield})
            {
                var entry=Entry(part,clip);
                float Clearance(Vector3 value)
                {
                    if(part.IsShield)entry.LeftThumbRotation=value;else entry.RightThumbRotation=value;
                    float minimum=float.MaxValue;foreach(float phase in new[]{0f,.25f,.5f,.75f,1f}){c.Sample(clip,phase);minimum=Mathf.Min(minimum,ThumbClearance(c,part));}return minimum;
                }
                float initial=Clearance(Vector3.zero),best=initial,score=float.MaxValue;Vector3 selected=Vector3.zero;
                if(initial<-.001f)foreach(var probe in probes)
                {
                    float clearance=Clearance(probe),cost=Mathf.Pow(Mathf.Max(0,.0005f-clearance)*1000,2)*10+probe.sqrMagnitude*.002f;
                    if(cost<score){score=cost;best=clearance;selected=probe;}
                }
                if(part.IsShield)entry.LeftThumbRotation=selected;else entry.RightThumbRotation=selected;
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0} {1}: {2:F2} -> {3:F2} mm; thumb {4}",part.name,clip.name,initial*1000,best*1000,selected));
            }
            c.Stop();
        }
        File.WriteAllText(Folder+"/sword-shield-thumb-fit.txt",report.ToString());
    }
    static void Render(WarriorAxeIKCalibration.Context c,WeaponDefinition sword,AnimationClip[] clips)
    {
        clips=clips.Where(Armed).ToArray();var preview=c.preview;preview.camera.orthographic=true;preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=30;preview.lights[0].intensity=1.4f;preview.lights[0].transform.rotation=Quaternion.Euler(35,210,0);preview.lights[1].intensity=1;preview.ambientColor=new Color(.4f,.4f,.45f);
        const int width=440,height=440,columns=4;
        foreach(bool back in new[]{false,true})
        {
            int rows=Mathf.CeilToInt(clips.Length/(float)columns);var sheet=new Texture2D(width*columns,height*rows,TextureFormat.RGB24,false);
            for(int n=0;n<clips.Length;n++)
            {
                c.Start(clips[n]);c.Sample(clips[n],.5f);var r=c.Animator.GetBoneTransform(HumanBodyBones.RightHand).position;var l=c.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position;var focus=(r+l)*.5f;var body=WeaponGripSpace.BodyRotation(c.Animator);
                preview.camera.orthographicSize=.75f;preview.camera.transform.position=focus+body*(back?new Vector3(-1.5f,.5f,-1.8f):new Vector3(1.5f,.45f,2));preview.camera.transform.LookAt(focus);
                var skins=c.root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();var objects=new List<GameObject>();var meshes=new List<Mesh>();
                foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);var obj=new GameObject("Grip review");obj.transform.SetParent(skin.transform,false);objects.Add(obj);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;}
                preview.BeginStaticPreview(new Rect(0,0,width,height));preview.Render(true);var picture=preview.EndStaticPreview();sheet.SetPixels(n%columns*width,(rows-1-n/columns)*height,width,height,picture.GetPixels());UnityEngine.Object.DestroyImmediate(picture);
                if(n==0&&!back)foreach(bool left in new[]{false,true})
                {
                    var wrist=c.Animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
                    var knuckle=c.Animator.GetBoneTransform(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);
                    var detail=(wrist.position+knuckle.position)*.5f;
                    preview.camera.orthographicSize=.22f;preview.camera.transform.position=detail+body*new Vector3(1,.25f,1.5f);preview.camera.transform.LookAt(detail);
                    preview.BeginStaticPreview(new Rect(0,0,700,700));preview.Render(true);var image=preview.EndStaticPreview();
                    File.WriteAllBytes(Folder+"/"+(left?"shield":"sword")+"-grip-detail.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                }
                foreach(var obj in objects)UnityEngine.Object.DestroyImmediate(obj);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);foreach(var skin in skins)skin.enabled=true;c.Stop();
            }
            sheet.Apply();File.WriteAllBytes(Folder+"/sword-shield-grips-"+(back?"back":"front")+".png",sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);
        }
        File.WriteAllText(Folder+"/sword-shield-grips-index.txt",string.Join("\n",clips.Select((p,i)=>(i+1)+". "+p.name)));
    }
}
