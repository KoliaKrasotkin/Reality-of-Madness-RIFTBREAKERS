using System.Collections.Generic;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat
{
    // Visual equipment only. Combat adapters own gameplay and replication.
    [DefaultExecutionOrder(250)]
    public sealed class CharacterWeaponPresentation : MonoBehaviour
    {
        public Animator Animator;
        public GameObject BuiltInWeapon;
        public WeaponDefinition CurrentWeapon { get; private set; }
        public WeaponDefinition CurrentPresentation { get; private set; }
        readonly List<WeaponDefinition> parts = new List<WeaponDefinition>();
        readonly List<GameObject> models = new List<GameObject>();
        readonly List<AnimatorClipInfo> currentClips = new List<AnimatorClipInfo>();
        readonly List<AnimatorClipInfo> nextClips = new List<AnimatorClipInfo>();
        readonly List<KeyValuePair<AnimationClip, AnimationClip>> clipOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        EquipmentArmIK leftArm, rightArm;
        Transform weaponBackSocket;
        bool builtInPoseCaptured;
        Vector3 builtInPosition;
        Quaternion builtInRotation;
        Transform leftThumb, rightThumb;
        Quaternion leftThumbPose, rightThumbPose;
        bool leftThumbCaptured, rightThumbCaptured;
        struct FingerSnapshot { public Transform bone; public Quaternion rotation; }
        readonly List<FingerSnapshot> fingerSnapshots = new List<FingerSnapshot>();
        RuntimeAnimatorController originalController;
        CharacterSkinController skin;
        static readonly int StoredWeight = UnityEngine.Animator.StringToHash("EquipmentBackWeight");
        public bool Alive { get; set; } = true;
        public struct IKSettings
        {
            public Vector3 WeaponPosition, WeaponRotation, HandPosition, HandRotation, ElbowOffset;
            public Vector3 LeftHandPosition, LeftHandRotation, LeftElbowOffset;
            public Vector3 RightThumbRotation, LeftThumbRotation;
            public float Weight;
            internal float freeLeftWristAlignment;
            internal float leftGripWeight, leftFistWeight;
            internal Dictionary<HumanBodyBones, (Quaternion rotation, float weight)> fingerOverrides;
            internal float rightWristAlignment;
            internal float leftWristLimitWeight, leftWristLimit;
            internal float leftWristArcWeight;
            internal Quaternion leftWristArc;
            internal float leftArmSmoothWeight;
            internal float leftForwardRelease;
            internal float castPoseWeight;
            internal EquipmentBoneRotation[] castPose;
            internal Quaternion leftUpperArm, leftForearm;
            internal float lowRightCarry, counterSwingRightCarry;
            internal float forwardScepterAim, scepterAimProgress, leftCastFingerExtension, uprightLeftPalm;
            internal Quaternion rightBlend, leftBlend;
            internal float blendWeight;
            internal Vector3 rightContactDelta, leftContactDelta;
        }
        void Awake()
        {
            skin = GetComponent<CharacterSkinController>();
            if (!Animator) Animator = GetComponentInChildren<Animator>();
            if (Animator) originalController = Animator.runtimeAnimatorController;
        }
        public void Equip(WeaponDefinition weapon)
        {
            if (!weapon || !Animator || CurrentWeapon == weapon) return;
            if (!originalController) originalController = Animator.runtimeAnimatorController;
            ClearModels();
            CurrentWeapon = weapon;
            var binding = skin && skin.IsActive ? skin.Current.FindWeapon(weapon) : null;
            var visual = binding != null && binding.Presentation ? binding.Presentation : weapon;
            CurrentPresentation = visual;
            leftArm = new EquipmentArmIK(Animator, HumanBodyBones.LeftHand);
            rightArm = new EquipmentArmIK(Animator, HumanBodyBones.RightHand);
            leftThumb = Animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
            rightThumb = Animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
            if (skin && skin.IsActive)
                skin.SetController(binding != null && binding.Controller ? binding.Controller : skin.Current.Controller);
            else Animator.runtimeAnimatorController = weapon.Controller ? weapon.Controller : originalController;
            if (visual.UsesBuiltInWeapon)
            {
                parts.Add(visual); models.Add(BuiltInWeapon);
            }
            else foreach (var attachment in visual.EquippedParts)
            {
                parts.Add(attachment);
                var model = attachment.Model ? Instantiate(attachment.Model, transform) : null;
                if (model)
                {
                    model.name = weapon.Id + " " + attachment.Socket;
                    foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                    foreach (var body in model.GetComponentsInChildren<Rigidbody>(true))
                    { body.isKinematic = true; body.detectCollisions = false; }
                }
                models.Add(model);
            }
        }
        WeaponDefinition skinWeapon;
        public void UnbindSkin()
        {
            skinWeapon = CurrentWeapon;
            ClearModels();
            if (BuiltInWeapon) BuiltInWeapon.SetActive(false);
            CurrentWeapon = null;
            CurrentPresentation = null;
            leftArm = rightArm = null;
        }
        public void BindSkin(Animator animator)
        {
            Animator = animator;
            originalController = animator ? animator.runtimeAnimatorController : null;
            skin = GetComponent<CharacterSkinController>();
            if (skinWeapon) Equip(skinWeapon);
            skinWeapon = null;
        }
        public AnimationClip ResolveClip(AnimationClip source)
        {
            if (!source) return null;
            var overrides = Animator ? Animator.runtimeAnimatorController as AnimatorOverrideController : null;
            if (overrides)
            {
                overrides.GetOverrides(clipOverrides);
                foreach (var pair in clipOverrides)
                    if (pair.Key == source) return pair.Value ? pair.Value : source;
            }
            return source;
        }
        // Undo only our previous solve before Animator samples again, including
        // bones omitted by a clip. Manual preview calls the same boundary.
        public void BeginPoseEvaluation()
        {
            leftArm?.Restore(); rightArm?.Restore();
            if (leftThumbCaptured && leftThumb) leftThumb.localRotation = leftThumbPose;
            if (rightThumbCaptured && rightThumb) rightThumb.localRotation = rightThumbPose;
            leftThumbCaptured = rightThumbCaptured = false;
            for (int i = fingerSnapshots.Count - 1; i >= 0; i--)
            {
                var pose = fingerSnapshots[i];
                if (pose.bone) pose.bone.localRotation = pose.rotation;
            }
            fingerSnapshots.Clear();
            if (builtInPoseCaptured && BuiltInWeapon)
            {
                BuiltInWeapon.transform.localPosition = builtInPosition;
                BuiltInWeapon.transform.localRotation = builtInRotation;
            }
            builtInPoseCaptured = false;
        }
        void Update() => BeginPoseEvaluation();
        void LateUpdate() => RefreshPose();
        public void RefreshPose() => RefreshPose(Animator ? Animator.GetFloat(StoredWeight) : 0);
        public IKSettings Settings(WeaponDefinition data, AnimationClip previewClip = null, float normalizedTime = 0)
        {
            var result = new IKSettings { Weight = 1 };
            if (previewClip) return FinishGrips(AddSettings(result, data, previewClip, 1, normalizedTime), data);
            if (!Animator || !Animator.isInitialized) return result;
            currentClips.Clear(); nextClips.Clear();
            Animator.GetCurrentAnimatorClipInfo(0, currentClips);
            float transition = 0;
            if (Animator.IsInTransition(0))
            {
                transition = Mathf.Clamp01(Animator.GetAnimatorTransitionInfo(0).normalizedTime);
                Animator.GetNextAnimatorClipInfo(0, nextClips);
            }
            float currentTime = Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            float nextTime = Animator.IsInTransition(0) ? Animator.GetNextAnimatorStateInfo(0).normalizedTime : 0;
            foreach (var info in currentClips) result = AddSettings(result, data, info.clip, info.weight * (1 - transition), info.clip.isLooping ? Mathf.Repeat(currentTime, 1) : Mathf.Clamp01(currentTime));
            foreach (var info in nextClips) result = AddSettings(result, data, info.clip, info.weight * transition, info.clip.isLooping ? Mathf.Repeat(nextTime, 1) : Mathf.Clamp01(nextTime));
            return FinishGrips(result, data);
        }
        IKSettings AddSettings(IKSettings result, WeaponDefinition data, AnimationClip clip, float blend, float time)
        {
            // Unarmed and handoff motions retain their authored hands; stored
            // weight additionally fades IK throughout the draw/sheath transition.
            bool unarmed = clip && (clip.name.StartsWith("Unarmed-") || clip.name.Contains("Sheath-") || clip.name.Contains("Unsheath-"));
            EquipmentAnimationIK entry = null;
            foreach (var candidate in data.AnimationIK)
                if (candidate != null && candidate.Clip == clip) { entry = candidate; break; }
            var pose = entry != null ? entry.PoseAt(time) : default;
            Vector3 rightPosition = entry != null ? entry.HandPosition : Vector3.zero;
            Vector3 leftPosition = entry != null ? entry.LeftHandPosition : Vector3.zero;
            Vector3 rightRotation = entry != null ? entry.HandRotation : Vector3.zero;
            Vector3 leftRotation = entry != null ? entry.LeftHandRotation : Vector3.zero;
            if (entry != null && entry.PoseKeys != null && entry.PoseKeys.Length > 0)
                foreach (var grip in data.HandGrips)
                {
                    if (grip == null) continue;
                    bool left = grip.Hand == HumanBodyBones.LeftHand;
                    var turn = Quaternion.AngleAxis(left ? pose.LeftGripTwist : pose.RightGripTwist, Vector3.forward);
                    var baseRotation = Quaternion.Euler(grip.Rotation);
                    var position = turn * (grip.Position + (left ? leftPosition : rightPosition)) - grip.Position;
                    position.z += left ? pose.LeftGripSlide : pose.RightGripSlide;
                    var rotation = (Quaternion.Inverse(baseRotation) * turn * baseRotation * Quaternion.Euler(left ? leftRotation : rightRotation)).eulerAngles;
                    if (left) { leftPosition = position; leftRotation = rotation; }
                    else { rightPosition = position; rightRotation = rotation; }
                }
            result.Weight += ((unarmed ? 0 : entry != null ? entry.Weight : 1) - 1) * blend;
            if (data.KeepGripsReachable && blend > 0)
            {
                float sum = result.blendWeight + blend;
                result.rightBlend = Quaternion.Slerp(result.blendWeight == 0 ? Quaternion.identity : result.rightBlend,
                    Quaternion.Euler(rightRotation), blend / sum);
                result.leftBlend = Quaternion.Slerp(result.blendWeight == 0 ? Quaternion.identity : result.leftBlend,
                    Quaternion.Euler(leftRotation), blend / sum);
                result.blendWeight = sum;
                foreach (var grip in data.HandGrips)
                {
                    if (grip == null || !grip.PreservePalmContact || entry == null) continue;
                    bool left = grip.Hand == HumanBodyBones.LeftHand;
                    var baseRotation = Quaternion.Euler(grip.Rotation);
                    var rotation = baseRotation * Quaternion.Euler(left ? leftRotation : rightRotation);
                    var delta = (left ? leftPosition : rightPosition) + rotation * grip.PalmOffset - baseRotation * grip.PalmOffset;
                    if (left) result.leftContactDelta += delta * blend;
                    else result.rightContactDelta += delta * blend;
                }
            }
            if (entry == null) return result;
            if (blend > 0 && entry.FingerOverrides != null && entry.FingerOverrides.Length > 0)
            {
                result.fingerOverrides ??= new Dictionary<HumanBodyBones, (Quaternion, float)>();
                foreach (var finger in entry.FingerOverrides)
                {
                    result.fingerOverrides.TryGetValue(finger.Bone, out var previous);
                    float weight = previous.weight + blend;
                    var rotation = previous.weight > 0
                        ? Quaternion.Slerp(previous.rotation, finger.Rotation, blend / weight) : finger.Rotation;
                    result.fingerOverrides[finger.Bone] = (rotation, weight);
                }
            }
            float seconds = time * clip.length;
            if (entry.LeftGripWeight != null && entry.LeftGripWeight.length > 0)
                result.leftGripWeight += Mathf.Clamp01(entry.LeftGripWeight.Evaluate(time)) * blend;
            result.leftFistWeight += entry.LeftFistWeight * blend;
            if (entry.LeftWristArcEnd > entry.LeftWristArcStart && seconds >= entry.LeftWristArcStart)
            {
                float progress = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(entry.LeftWristArcStart,
                    entry.LeftWristArcEnd, seconds));
                var rotation = Quaternion.Slerp(entry.LeftWristArcFrom, entry.LeftWristArcTo, progress);
                float total = result.leftWristArcWeight + blend;
                result.leftWristArc = result.leftWristArcWeight > 0
                    ? Quaternion.Slerp(result.leftWristArc, rotation, blend / total) : rotation;
                result.leftWristArcWeight = total;
            }
            if (entry.CastPoseArrival > 0 && entry.CastPose.Length > 0)
            {
                result.castPose = entry.CastPose;
                float arrive = Mathf.SmoothStep(0, 1, seconds / entry.CastPoseArrival);
                float recover = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.55f, .95f, time));
                result.castPoseWeight += blend * arrive * recover;
            }
            if (entry.LeftArmSmoothEnd > entry.LeftArmSmoothStart && seconds > entry.LeftArmSmoothStart &&
                seconds < entry.LeftArmSmoothEnd && blend > 0)
            {
                float progress = Mathf.InverseLerp(entry.LeftArmSmoothStart, entry.LeftArmSmoothEnd, seconds);
                float weight = result.leftArmSmoothWeight + blend;
                var upper = Quaternion.Slerp(entry.LeftUpperArmStart, entry.LeftUpperArmEnd, progress);
                var lower = Quaternion.Slerp(entry.LeftForearmStart, entry.LeftForearmEnd, progress);
                result.leftUpperArm = result.leftArmSmoothWeight > 0 ? Quaternion.Slerp(result.leftUpperArm, upper, blend / weight) : upper;
                result.leftForearm = result.leftArmSmoothWeight > 0 ? Quaternion.Slerp(result.leftForearm, lower, blend / weight) : lower;
                result.leftArmSmoothWeight = weight;
            }
            result.freeLeftWristAlignment += entry.FreeLeftWristAlignment * blend;
            if (entry.LeftForwardRelease > 0)
            {
                float extend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.122f, .183f, seconds));
                float recover = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.24f, .38f, seconds));
                result.leftForwardRelease += entry.LeftForwardRelease * blend * extend * recover;
            }
            float wristWeight = entry.RightWristAlignment;
            if (entry.CastPoseArrival > 0)
            {
                wristWeight *= Mathf.SmoothStep(0, 1, seconds / entry.CastPoseArrival);
                if (entry.UprightLeftPalm > 0)
                {
                    float straighten = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, clip.length, seconds));
                    result.leftWristLimit += 80f * (1 - straighten) * blend;
                    result.leftWristLimitWeight += blend;
                }
            }
            result.rightWristAlignment += wristWeight * blend;
            result.lowRightCarry += entry.LowRightCarry * blend;
            if (entry.CounterSwingRightCarry) result.counterSwingRightCarry += entry.LowRightCarry * blend;
            float castWindow = Mathf.SmoothStep(0, 1, Mathf.Min(time, 1 - time) * 5);
            if (entry.CastPoseArrival > 0 && entry.CastPose.Length > 0)
                castWindow = Mathf.Max(castWindow, Mathf.SmoothStep(0, 1, seconds / entry.CastPoseArrival)
                    * Mathf.SmoothStep(0, 1, (1 - time) * 5));
            result.forwardScepterAim += entry.ForwardScepterAim * blend;
            result.scepterAimProgress += entry.ForwardScepterAim * blend * castWindow;
            result.uprightLeftPalm += entry.UprightLeftPalm * blend * castWindow;
            result.leftCastFingerExtension += entry.LeftCastFingerExtension * blend * castWindow;
            result.WeaponPosition += (entry.WeaponPosition + pose.WeaponPosition) * blend;
            result.WeaponRotation += (entry.WeaponRotation + pose.WeaponRotation) * blend;
            result.HandPosition += rightPosition * blend;
            result.HandRotation += rightRotation * blend;
            result.ElbowOffset += (entry.ElbowOffset + pose.RightElbowOffset) * blend;
            result.LeftHandPosition += leftPosition * blend;
            result.LeftHandRotation += leftRotation * blend;
            result.LeftElbowOffset += (entry.LeftElbowOffset + pose.LeftElbowOffset) * blend;
            result.RightThumbRotation += entry.RightThumbRotation * blend;
            result.LeftThumbRotation += entry.LeftThumbRotation * blend;
            return result;
        }
        static IKSettings FinishGrips(IKSettings result, WeaponDefinition data)
        {
            if (!data.KeepGripsReachable || result.blendWeight <= 0) return result;
            var right = Quaternion.Slerp(Quaternion.identity, result.rightBlend, Mathf.Min(1, result.blendWeight));
            var left = Quaternion.Slerp(Quaternion.identity, result.leftBlend, Mathf.Min(1, result.blendWeight));
            result.HandRotation = right.eulerAngles; result.LeftHandRotation = left.eulerAngles;
            foreach (var grip in data.HandGrips)
            {
                if (grip == null || !grip.PreservePalmContact) continue;
                bool isLeft = grip.Hand == HumanBodyBones.LeftHand;
                var baseRotation = Quaternion.Euler(grip.Rotation);
                var offset = baseRotation * grip.PalmOffset - baseRotation * (isLeft ? left : right) * grip.PalmOffset;
                if (isLeft) result.LeftHandPosition = result.leftContactDelta + offset;
                else result.HandPosition = result.rightContactDelta + offset;
            }
            return result;
        }
        public GameObject ModelFor(WeaponDefinition data)
        {
            if (data == CurrentWeapon && CurrentPresentation) data = CurrentPresentation;
            int index = parts.IndexOf(data);
            return index >= 0 ? models[index] : null;
        }
        public void RefreshPose(float storedWeight, AnimationClip previewClip = null, float normalizedTime = 0, CharacterSkinDefinition previewSkin = null)
        {
            if (!Animator) return;
            BeginPoseEvaluation();
            ApplyUnarmedRunLean(previewClip);
            if (!CurrentWeapon) return;
            if (BuiltInWeapon) BuiltInWeapon.SetActive(Alive && CurrentPresentation && CurrentPresentation.UsesBuiltInWeapon);
            float stored = Mathf.Clamp01(storedWeight);
            var bodyRotation = WeaponGripSpace.BodyRotation(Animator);
            var socketSkin = previewClip ? previewSkin : skin ? skin.Current : null;
            var socketPose = GetBackSocketPose(socketSkin, previewClip);
            // Resolve the carrying arm before attaching its weapon. Secondary
            // grip targets are solved after all equipment has been placed.
            for (int i = 0; i < models.Count; i++)
            {
                var model = models[i];
                if (!model) continue;
                model.SetActive(Alive);
                var data = parts[i];
                var settings = Settings(data, previewClip, normalizedTime);
                if (Alive && data.EnableHandIK && settings.castPoseWeight > 0)
                    foreach (var pose in settings.castPose)
                        ApplyBoneRotation(pose.Bone, pose.Rotation, settings.castPoseWeight);
                if (!data.UsesBuiltInWeapon)
                {
                    var hand = Animator.GetBoneTransform(data.Socket);
                    var back = Animator.GetBoneTransform(data.StoredBone);
                    if (!hand || !back) { model.SetActive(false); continue; }
                    EquipmentHandGrip socketGrip = null;
                    foreach (var attachment in data.HandGrips)
                    {
                        if (attachment == null || !attachment.DrivesSocket || attachment.Hand != data.Socket) continue;
                        socketGrip = attachment;
                        break;
                    }
                    var socketRotation = Quaternion.Euler(data.Rotation);
                    if (socketGrip != null) socketRotation *= Quaternion.Inverse(Quaternion.Euler(socketGrip.Rotation));
                    if (data.Socket == HumanBodyBones.RightHand)
                    {
                        var localShaft = socketRotation * Vector3.up;
                        if (settings.lowRightCarry > 0) ApplyLowRightCarry(hand, settings, stored, localShaft);
                        else if (settings.forwardScepterAim > 0) ApplyForwardScepterAim(hand, settings, stored, localShaft);
                        else if (settings.rightWristAlignment > 0)
                            AlignRightWrist(hand, localShaft, Mathf.Clamp01(settings.rightWristAlignment) * (1 - stored));
                    }
                    var grip = data.UseGripFrame ? WeaponGripSpace.Rotation(Animator, data.Socket) : hand.rotation;
                    var drawnPosition = hand.position + grip * data.Position;
                    var drawnRotation = grip * socketRotation;
                    if (socketGrip != null)
                    {
                        var scale = Vector3.Scale(transform.lossyScale, data.Scale);
                        drawnPosition -= drawnRotation * Vector3.Scale(socketGrip.Position, scale);
                    }
                    if(data.UseApprovedCarryGrip)
                    {
                        float carry=Mathf.Clamp01(1-settings.Weight);
                        var carryRotation=hand.rotation*Quaternion.Inverse(Quaternion.Euler(data.CarryGripRotation));
                        var carryPosition=hand.position-carryRotation*Vector3.Scale(data.CarryGripPosition,Vector3.Scale(transform.lossyScale,data.Scale));
                        drawnPosition=Vector3.Lerp(drawnPosition,carryPosition,carry);
                        drawnRotation=Quaternion.Slerp(drawnRotation,carryRotation,carry);
                    }
                    var storedPosition = back.position + bodyRotation * data.StoredPosition;
                    var storedRotation = bodyRotation * Quaternion.Euler(data.StoredRotation);
                    if (data.CreateBackSocket && data == CurrentPresentation)
                    {
                        storedPosition += bodyRotation * socketPose.position;
                        storedRotation = bodyRotation * socketPose.rotation * Quaternion.Euler(data.StoredRotation);
                        if (!weaponBackSocket)
                        {
                            weaponBackSocket = new GameObject("Weapon Back Socket").transform;
                            weaponBackSocket.SetParent(back, false);
                        }
                        weaponBackSocket.SetPositionAndRotation(storedPosition, storedRotation);
                        storedPosition = weaponBackSocket.position;
                        storedRotation = weaponBackSocket.rotation;
                    }
                    model.transform.SetPositionAndRotation(
                        Vector3.Lerp(drawnPosition, storedPosition, stored),
                        Quaternion.Slerp(drawnRotation, storedRotation, stored));
                    model.transform.localScale = data.Scale;
                }
                if (data.EnableHandIK)
                {
                    if (data.UsesBuiltInWeapon)
                    {
                        builtInPosition = model.transform.localPosition;
                        builtInRotation = model.transform.localRotation;
                        builtInPoseCaptured = true;
                    }
                    model.transform.position += bodyRotation * settings.WeaponPosition * (1 - stored);
                    model.transform.rotation = Quaternion.Slerp(Quaternion.identity,
                        bodyRotation * Quaternion.Euler(settings.WeaponRotation) * Quaternion.Inverse(bodyRotation), 1 - stored) * model.transform.rotation;
                }
            }
            for (int i = 0; i < models.Count; i++)
            {
                var data = parts[i]; var model = models[i];
                if (!model || !Alive) continue;
                var settings = Settings(data, previewClip, normalizedTime);
                if (!data.EnableHandIK)
                {
                    ApplyClipFingers(settings, stored);
                    continue;
                }
                if (data.KeepGripsReachable && settings.Weight > .999f && stored < .001f)
                    KeepWithinReach(data, model.transform, settings);
                float castAlignment = Mathf.Clamp01(settings.freeLeftWristAlignment);
                if (settings.leftArmSmoothWeight > 0)
                {
                    ApplyBoneRotation(HumanBodyBones.LeftUpperArm, settings.leftUpperArm, settings.leftArmSmoothWeight);
                    ApplyBoneRotation(HumanBodyBones.LeftLowerArm, settings.leftForearm, settings.leftArmSmoothWeight);
                }
                AimLeftRelease(Mathf.Clamp01(settings.leftForwardRelease));
                AlignFreeLeftWrist(castAlignment, Mathf.Clamp01(settings.uprightLeftPalm));
                if (settings.leftWristLimitWeight > 0)
                    LimitFreeLeftWrist(settings.leftWristLimit / settings.leftWristLimitWeight, settings.leftWristLimitWeight);
                if (settings.leftWristArcWeight > 0)
                    ApplyBoneRotation(HumanBodyBones.LeftHand, settings.leftWristArc, settings.leftWristArcWeight);
                bool usesTimedLeftGrip = System.Array.Exists(data.HandGrips,
                    grip => grip != null && grip.Hand == HumanBodyBones.LeftHand && grip.UseClipGripWeight);
                foreach (var pose in data.FingerPoses)
                {
                    if (pose == null) continue;
                    var bone = Animator.GetBoneTransform(pose.Bone);
                    if (!bone) continue;
                    fingerSnapshots.Add(new FingerSnapshot { bone = bone, rotation = bone.localRotation });
                    if (data.UseFixedFingerPoses)
                    {
                        bool leftFinger = pose.Bone >= HumanBodyBones.LeftThumbProximal && pose.Bone <= HumanBodyBones.LeftLittleDistal;
                        float relaxedWeight = leftFinger ? Mathf.Max(stored, castAlignment) : stored;
                        var rotation = Quaternion.Slerp(Quaternion.Euler(pose.Rotation), Quaternion.Euler(pose.RelaxedRotation), relaxedWeight);
                        if (leftFinger && settings.leftCastFingerExtension > 0)
                            rotation = Quaternion.Slerp(rotation, Quaternion.Euler(pose.ExtendedRotation),
                                Mathf.Clamp01(settings.leftCastFingerExtension) * (1 - stored));
                        bone.localRotation = rotation;
                    }
                    else
                    {
                        float fingerWeight=data.UseApprovedCarryGrip?1:Mathf.Clamp01(settings.Weight);
                        var target = Quaternion.Euler(pose.Rotation);
                        if (usesTimedLeftGrip && pose.Bone >= HumanBodyBones.LeftThumbProximal && pose.Bone <= HumanBodyBones.LeftLittleDistal)
                            target = Quaternion.Slerp(Quaternion.Euler(pose.RelaxedRotation), target,
                                Mathf.Clamp01(Mathf.Max(settings.leftGripWeight, settings.leftFistWeight)));
                        bone.localRotation = Quaternion.Slerp(bone.localRotation, target, fingerWeight * (1 - stored));
                    }
                }
                ApplyClipFingers(settings, stored);
                foreach (var grip in data.HandGrips)
                {
                    if (grip == null || grip.DrivesSocket) continue;
                    var arm = grip.Hand == HumanBodyBones.LeftHand ? leftArm : rightArm;
                    bool left = grip.Hand == HumanBodyBones.LeftHand;
                    float gripWeight = Mathf.Clamp01(settings.Weight) * (1 - stored);
                    if (left && grip.UseClipGripWeight) gripWeight *= Mathf.Clamp01(settings.leftGripWeight);
                    arm?.Solve(model.transform.TransformPoint(grip.Position + (left ? settings.LeftHandPosition : settings.HandPosition)),
                        model.transform.rotation * Quaternion.Euler(grip.Rotation) * Quaternion.Euler(left ? settings.LeftHandRotation : settings.HandRotation),
                        bodyRotation * (left ? settings.LeftElbowOffset : settings.ElbowOffset), gripWeight);
                    var thumb = left ? leftThumb : rightThumb;
                    var thumbOffset = left ? settings.LeftThumbRotation : settings.RightThumbRotation;
                    float thumbWeight=data.UseApprovedCarryGrip?1:Mathf.Clamp01(settings.Weight);
                    if (left && grip.UseClipGripWeight) thumbWeight *= Mathf.Clamp01(settings.leftGripWeight);
                    var thumbBone = left ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal;
                    if (settings.fingerOverrides != null && settings.fingerOverrides.TryGetValue(thumbBone, out var fingerOverride))
                        thumbWeight *= 1 - Mathf.Clamp01(fingerOverride.weight);
                    if (thumb && thumbOffset.sqrMagnitude > .000001f && thumbWeight > 0 && stored < 1)
                    {
                        if (left) { leftThumbPose = thumb.localRotation; leftThumbCaptured = true; }
                        else { rightThumbPose = thumb.localRotation; rightThumbCaptured = true; }
                        thumb.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(thumbOffset), thumbWeight * (1 - stored));
                    }
                }
            }
        }
        static bool UsesUnarmedRunLean(AnimationClip clip)
        {
            return clip && clip.name is "Unarmed-Run-Left" or "Unarmed-Run-Right"
                or "Unarmed-Run-Backward" or "Unarmed-Run-Forward-Left" or "Unarmed-Run-Forward-Right"
                or "Unarmed-Run-Backward-Left" or "Unarmed-Run-Backward-Right";
        }

        (Vector3 position, Quaternion rotation) GetBackSocketPose(CharacterSkinDefinition profile, AnimationClip previewClip)
        {
            if (!profile || profile.BackSocketPoses.Length == 0)
                return (Vector3.zero, Quaternion.identity);

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            void Accumulate(AnimationClip clip, float weight)
            {
                foreach (var pose in profile.BackSocketPoses)
                {
                    if (pose == null || pose.Clip != clip) continue;
                    position += pose.Position * weight;
                    rotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(pose.Rotation), weight);
                    break;
                }
            }

            if (previewClip) Accumulate(previewClip, 1);
            else if (Animator.isInitialized)
            {
                bool transitioning = Animator.IsInTransition(0);
                float transition = transitioning ? Mathf.Clamp01(Animator.GetAnimatorTransitionInfo(0).normalizedTime) : 0;
                currentClips.Clear();
                Animator.GetCurrentAnimatorClipInfo(0, currentClips);
                foreach (var info in currentClips) Accumulate(info.clip, info.weight * (1 - transition));
                if (transitioning)
                {
                    nextClips.Clear();
                    Animator.GetNextAnimatorClipInfo(0, nextClips);
                    foreach (var info in nextClips) Accumulate(info.clip, info.weight * transition);
                }
            }
            return (position, rotation);
        }

        void ApplyUnarmedRunLean(AnimationClip previewClip)
        {
            float weight = 0;
            if (previewClip)
                weight = UsesUnarmedRunLean(previewClip) ? 1 : 0;
            else if (Animator.isInitialized)
            {
                bool transitioning = Animator.IsInTransition(0);
                float transition = transitioning ? Mathf.Clamp01(Animator.GetAnimatorTransitionInfo(0).normalizedTime) : 0;
                currentClips.Clear();
                Animator.GetCurrentAnimatorClipInfo(0, currentClips);
                foreach (var info in currentClips)
                    if (UsesUnarmedRunLean(info.clip)) weight += info.weight * (1 - transition);
                if (transitioning)
                {
                    nextClips.Clear();
                    Animator.GetNextAnimatorClipInfo(0, nextClips);
                    foreach (var info in nextClips)
                        if (UsesUnarmedRunLean(info.clip)) weight += info.weight * transition;
                }
            }

            if (weight <= 0 || !Animator.isHuman) return;
            var spine = Animator.GetBoneTransform(HumanBodyBones.Spine);
            if (!spine) return;
            const float leanDegrees = 6f;
            fingerSnapshots.Add(new FingerSnapshot { bone = spine, rotation = spine.localRotation });
            weight = Mathf.Clamp01(weight);
            spine.rotation = Quaternion.AngleAxis(leanDegrees * weight, Animator.transform.right) * spine.rotation;
        }

        void ApplyClipFingers(IKSettings settings, float stored)
        {
            if (settings.fingerOverrides == null || stored >= 1) return;
            foreach (var pair in settings.fingerOverrides)
                ApplyBoneRotation(pair.Key, pair.Value.rotation, Mathf.Clamp01(pair.Value.weight) * (1 - stored));
        }
        void AlignRightWrist(Transform hand, Vector3 localShaft, float weight)
        {
            if (weight <= 0) return;
            var elbow = Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (!elbow) return;
            var palm = WeaponGripSpace.Rotation(Animator, HumanBodyBones.RightHand);
            var forearm = hand.position - elbow.position;
            if (forearm.sqrMagnitude < .000001f) return;
            var currentShaft = Vector3.ProjectOnPlane(palm * localShaft, forearm);
            var shaft = Vector3.ProjectOnPlane(Animator.transform.up, forearm);
            if (shaft.sqrMagnitude < .000001f) shaft = currentShaft;
            if (shaft.sqrMagnitude < .000001f) return;
            fingerSnapshots.Add(new FingerSnapshot { bone = hand, rotation = hand.localRotation });
            fingerSnapshots.Add(new FingerSnapshot { bone = elbow, rotation = elbow.localRotation });
            OrientScepterPalm(hand, forearm, shaft, localShaft, weight);
            var wristRotation = hand.rotation;
            if (currentShaft.sqrMagnitude >= .000001f)
            {
                float roll = Vector3.SignedAngle(currentShaft, shaft, forearm);
                elbow.rotation = Quaternion.AngleAxis(roll * weight * .8f, forearm) * elbow.rotation;
            }
            hand.rotation = wristRotation;
        }
        void ApplyLowRightCarry(Transform hand, IKSettings settings, float stored, Vector3 localShaft)
        {
            float weight = Mathf.Clamp01(settings.lowRightCarry) * (1 - stored);
            if (weight <= 0) return;
            var leftHand = Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var leftShoulder = Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var rightShoulder = Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (!leftHand || !leftShoulder || !rightShoulder) return;
            var facing = Animator.transform.rotation;
            var leftOffset = Quaternion.Inverse(facing) * (leftHand.position - leftShoulder.position);
            float counterSwing = Mathf.Clamp01(settings.counterSwingRightCarry / settings.lowRightCarry);
            float swing = Mathf.Lerp(leftOffset.z, -.65f * (leftOffset.z - .18f), counterSwing);
            var offset = new Vector3(.12f, Mathf.Clamp(leftOffset.y, -.46f, -.34f), Mathf.Clamp(.19f + swing, .09f, .32f));
            var target = rightShoulder.position + facing * offset;
            rightArm?.Solve(target, hand.rotation, facing * new Vector3(.55f, -.65f, -.50f),
                weight, preserveLocalHandRotation: true, useFixedPole: true);
            var elbow = Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (!elbow) return;
            var forearm = hand.position - elbow.position;
            var shaft = facing * new Vector3(.28f, 1, .30f);
            OrientScepterPalm(hand, Vector3.ProjectOnPlane(forearm, shaft), shaft, localShaft, weight);
        }
        void ApplyForwardScepterAim(Transform hand, IKSettings settings, float stored, Vector3 localShaft)
        {
            float weight = Mathf.Clamp01(settings.forwardScepterAim) * (1 - stored);
            if (weight <= 0) return;
            var shoulder = Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var elbow = Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (!shoulder || !elbow) return;
            var facing = Animator.transform.rotation;
            float reach = Mathf.Clamp01(settings.scepterAimProgress / settings.forwardScepterAim);
            var offset = Vector3.Lerp(new Vector3(.18f, -.28f, .19f), new Vector3(.15f, -.14f, .47f), reach);
            var target = shoulder.position + facing * offset;
            rightArm?.Solve(target, hand.rotation, facing * new Vector3(.65f, -.70f, -.22f),
                weight, preserveLocalHandRotation: true, useFixedPole: true);
            var shaft = facing * Vector3.Lerp(new Vector3(.28f, 1, .30f), new Vector3(0, .52f, 1), reach);
            var palmForward = Vector3.ProjectOnPlane(hand.position - elbow.position, shaft);
            OrientScepterPalm(hand, palmForward, shaft, localShaft, weight);
        }
        void OrientScepterPalm(Transform hand, Vector3 forward, Vector3 shaft, Vector3 localShaft, float weight)
        {
            if (forward.sqrMagnitude < .000001f || shaft.sqrMagnitude < .000001f) return;
            var palm = WeaponGripSpace.Rotation(Animator, HumanBodyBones.RightHand);
            var desired = Quaternion.LookRotation(forward, Vector3.Cross(forward, shaft))
                * Quaternion.FromToRotation(localShaft, Vector3.right);
            hand.rotation = Quaternion.Slerp(Quaternion.identity, desired * Quaternion.Inverse(palm),
                Mathf.Clamp01(weight)) * hand.rotation;
        }
        void ApplyBoneRotation(HumanBodyBones id, Quaternion rotation, float weight)
        {
            var bone = Animator.GetBoneTransform(id);
            if (!bone) return;
            fingerSnapshots.Add(new FingerSnapshot { bone = bone, rotation = bone.localRotation });
            bone.localRotation = Quaternion.Slerp(bone.localRotation, rotation, Mathf.Clamp01(weight));
        }
        void AimLeftRelease(float weight)
        {
            if (weight <= 0) return;
            var shoulder = Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var wrist = Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (!shoulder || !wrist) return;
            var up = Animator.transform.up;
            var direction = Vector3.ProjectOnPlane(wrist.position - shoulder.position, up);
            if (direction.sqrMagnitude < .000001f) return;
            float yaw = Vector3.SignedAngle(direction, Animator.transform.forward, up);
            fingerSnapshots.Add(new FingerSnapshot { bone = shoulder, rotation = shoulder.localRotation });
            shoulder.rotation = Quaternion.AngleAxis(yaw * weight, up) * shoulder.rotation;
        }
        void LimitFreeLeftWrist(float limit, float weight)
        {
            var wrist = Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var elbow = Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            if (!wrist || !elbow) return;
            var forearm = wrist.position - elbow.position;
            if (forearm.sqrMagnitude < .000001f) return;
            var fingers = WeaponGripSpace.Rotation(Animator, HumanBodyBones.LeftHand) * Vector3.forward;
            float excess = Vector3.Angle(fingers, forearm) - limit;
            if (excess <= 0) return;
            fingerSnapshots.Add(new FingerSnapshot { bone = wrist, rotation = wrist.localRotation });
            var straighten = Quaternion.FromToRotation(fingers, forearm);
            wrist.rotation = Quaternion.RotateTowards(Quaternion.identity, straighten,
                excess * Mathf.Clamp01(weight)) * wrist.rotation;
        }
        void AlignFreeLeftWrist(float weight, float upright)
        {
            if (weight <= 0 && upright <= 0) return;
            var wrist = Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var elbow = Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            if (!wrist || !elbow) return;
            var palm = WeaponGripSpace.Rotation(Animator, HumanBodyBones.LeftHand);
            var normal = palm * Vector3.up;
            var forearm = Vector3.ProjectOnPlane(wrist.position - elbow.position, normal);
            fingerSnapshots.Add(new FingerSnapshot { bone = wrist, rotation = wrist.localRotation });
            if (forearm.sqrMagnitude >= .000001f)
            {
                float angle = Vector3.SignedAngle(palm * Vector3.forward, forearm, normal);
                wrist.rotation = Quaternion.AngleAxis(Mathf.Clamp(angle, -65f, 65f) * weight, normal) * wrist.rotation;
            }
            if (upright > 0)
            {
                var currentPalm = WeaponGripSpace.Rotation(Animator, HumanBodyBones.LeftHand);
                var sidePalm = Quaternion.LookRotation(Animator.transform.up, -Animator.transform.right);
                var correction = sidePalm * Quaternion.Inverse(currentPalm);
                wrist.rotation = Quaternion.Slerp(Quaternion.identity, correction, upright) * wrist.rotation;
            }
        }
        void KeepWithinReach(WeaponDefinition data, Transform model, IKSettings settings)
        {
            Vector3 correction = Vector3.zero;
            for (int iteration = 0; iteration < 32; iteration++)
            {
                var previous = correction;
                foreach (var grip in data.HandGrips)
                {
                    if (grip == null) continue;
                    bool left = grip.Hand == HumanBodyBones.LeftHand;
                    var target = model.TransformPoint(grip.Position + (left ? settings.LeftHandPosition : settings.HandPosition));
                    var arm = left ? leftArm : rightArm;
                    if (arm != null) correction += arm.ReachCorrection(target + correction);
                }
                if ((correction - previous).sqrMagnitude < .0000000001f) break;
            }
            model.position += correction;
        }
        void ClearModels()
        {
            BeginPoseEvaluation();
            foreach (var model in models) if (model && model != BuiltInWeapon)
            { model.SetActive(false); if (Application.isPlaying) Destroy(model); else DestroyImmediate(model); }
            models.Clear(); parts.Clear();
            if (weaponBackSocket)
            {
                weaponBackSocket.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(weaponBackSocket.gameObject);
                else DestroyImmediate(weaponBackSocket.gameObject);
                weaponBackSocket = null;
            }
        }
        void OnDisable() => BeginPoseEvaluation();
        void OnDestroy() => ClearModels();
    }
}
