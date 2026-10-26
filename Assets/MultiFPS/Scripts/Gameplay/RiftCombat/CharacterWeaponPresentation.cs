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
        readonly List<WeaponDefinition> parts = new List<WeaponDefinition>();
        readonly List<GameObject> models = new List<GameObject>();
        readonly List<AnimatorClipInfo> currentClips = new List<AnimatorClipInfo>();
        readonly List<AnimatorClipInfo> nextClips = new List<AnimatorClipInfo>();
        EquipmentArmIK leftArm, rightArm;
        bool builtInPoseCaptured;
        Vector3 builtInPosition;
        Quaternion builtInRotation;
        Transform leftThumb, rightThumb;
        Quaternion leftThumbPose, rightThumbPose;
        bool leftThumbCaptured, rightThumbCaptured;
        struct FingerSnapshot { public Transform bone; public Quaternion rotation; }
        readonly List<FingerSnapshot> fingerSnapshots = new List<FingerSnapshot>();
        RuntimeAnimatorController originalController;
        static readonly int StoredWeight = UnityEngine.Animator.StringToHash("EquipmentBackWeight");
        public bool Alive { get; set; } = true;
        public struct IKSettings
        {
            public Vector3 WeaponPosition, WeaponRotation, HandPosition, HandRotation, ElbowOffset;
            public Vector3 LeftHandPosition, LeftHandRotation, LeftElbowOffset;
            public Vector3 RightThumbRotation, LeftThumbRotation;
            public float Weight;
            internal Quaternion rightBlend, leftBlend;
            internal float blendWeight;
            internal Vector3 rightContactDelta, leftContactDelta;
        }
        void Awake()
        {
            if (!Animator) Animator = GetComponentInChildren<Animator>();
            if (Animator) originalController = Animator.runtimeAnimatorController;
        }
        public void Equip(WeaponDefinition weapon)
        {
            if (!weapon || !Animator || CurrentWeapon == weapon) return;
            if (!originalController) originalController = Animator.runtimeAnimatorController;
            ClearModels();
            CurrentWeapon = weapon;
            leftArm = new EquipmentArmIK(Animator, HumanBodyBones.LeftHand);
            rightArm = new EquipmentArmIK(Animator, HumanBodyBones.RightHand);
            leftThumb = Animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
            rightThumb = Animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
            Animator.runtimeAnimatorController = weapon.Controller ? weapon.Controller : originalController;
            if (weapon.UsesBuiltInWeapon)
            {
                parts.Add(weapon); models.Add(BuiltInWeapon);
            }
            else foreach (var attachment in weapon.EquippedParts)
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
        public AnimationClip ResolveClip(AnimationClip source)
        {
            if (!source) return null;
            var overrides = Animator ? Animator.runtimeAnimatorController as AnimatorOverrideController : null;
            return overrides && overrides[source] ? overrides[source] : source;
        }
        // Undo only our previous solve before Animator samples again, including
        // bones omitted by a clip. Manual preview calls the same boundary.
        public void BeginPoseEvaluation()
        {
            leftArm?.Restore(); rightArm?.Restore();
            if (leftThumbCaptured && leftThumb) leftThumb.localRotation = leftThumbPose;
            if (rightThumbCaptured && rightThumb) rightThumb.localRotation = rightThumbPose;
            leftThumbCaptured = rightThumbCaptured = false;
            foreach (var pose in fingerSnapshots) if (pose.bone) pose.bone.localRotation = pose.rotation;
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
            int index = parts.IndexOf(data);
            return index >= 0 ? models[index] : null;
        }
        public void RefreshPose(float storedWeight, AnimationClip previewClip = null, float normalizedTime = 0)
        {
            if (!CurrentWeapon || !Animator) return;
            BeginPoseEvaluation();
            if (BuiltInWeapon) BuiltInWeapon.SetActive(Alive && CurrentWeapon.UsesBuiltInWeapon);
            float stored = Mathf.Clamp01(storedWeight);
            var bodyRotation = WeaponGripSpace.BodyRotation(Animator);
            // Place every item before solving either arm. Attachment anchors come
            // from the original animation (or the axe's existing stream rig).
            for (int i = 0; i < models.Count; i++)
            {
                var model = models[i];
                if (!model) continue;
                model.SetActive(Alive);
                var data = parts[i];
                var settings = Settings(data, previewClip, normalizedTime);
                if (!data.UsesBuiltInWeapon)
                {
                    var hand = Animator.GetBoneTransform(data.Socket);
                    var back = Animator.GetBoneTransform(data.StoredBone);
                    if (!hand || !back) { model.SetActive(false); continue; }
                    var grip = data.UseGripFrame ? WeaponGripSpace.Rotation(Animator, data.Socket) : hand.rotation;
                    var drawnPosition = hand.position + grip * data.Position;
                    var drawnRotation = grip * Quaternion.Euler(data.Rotation);
                    if(data.UseApprovedCarryGrip)
                    {
                        float carry=Mathf.Clamp01(1-settings.Weight);
                        var carryRotation=hand.rotation*Quaternion.Inverse(Quaternion.Euler(data.CarryGripRotation));
                        var carryPosition=hand.position-carryRotation*Vector3.Scale(data.CarryGripPosition,Vector3.Scale(transform.lossyScale,data.Scale));
                        drawnPosition=Vector3.Lerp(drawnPosition,carryPosition,carry);
                        drawnRotation=Quaternion.Slerp(drawnRotation,carryRotation,carry);
                    }
                    model.transform.SetPositionAndRotation(
                        Vector3.Lerp(drawnPosition, back.position + bodyRotation * data.StoredPosition, stored),
                        Quaternion.Slerp(drawnRotation, bodyRotation * Quaternion.Euler(data.StoredRotation), stored));
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
                if (!model || !Alive || !data.EnableHandIK) continue;
                var settings = Settings(data, previewClip, normalizedTime);
                if (data.KeepGripsReachable && settings.Weight > .999f && stored < .001f)
                    KeepWithinReach(data, model.transform, settings);
                foreach (var pose in data.FingerPoses)
                {
                    if (pose == null) continue;
                    var bone = Animator.GetBoneTransform(pose.Bone);
                    if (!bone) continue;
                    fingerSnapshots.Add(new FingerSnapshot { bone = bone, rotation = bone.localRotation });
                    float fingerWeight=data.UseApprovedCarryGrip?1:Mathf.Clamp01(settings.Weight);
                    bone.localRotation = Quaternion.Slerp(bone.localRotation, Quaternion.Euler(pose.Rotation), fingerWeight * (1 - stored));
                }
                foreach (var grip in data.HandGrips)
                {
                    if (grip == null) continue;
                    var arm = grip.Hand == HumanBodyBones.LeftHand ? leftArm : rightArm;
                    bool left = grip.Hand == HumanBodyBones.LeftHand;
                    arm?.Solve(model.transform.TransformPoint(grip.Position + (left ? settings.LeftHandPosition : settings.HandPosition)),
                        model.transform.rotation * Quaternion.Euler(grip.Rotation) * Quaternion.Euler(left ? settings.LeftHandRotation : settings.HandRotation),
                        bodyRotation * (left ? settings.LeftElbowOffset : settings.ElbowOffset), Mathf.Clamp01(settings.Weight) * (1 - stored));
                    var thumb = left ? leftThumb : rightThumb;
                    var thumbOffset = left ? settings.LeftThumbRotation : settings.RightThumbRotation;
                    float thumbWeight=data.UseApprovedCarryGrip?1:Mathf.Clamp01(settings.Weight);
                    if (thumb && thumbOffset.sqrMagnitude > .000001f && thumbWeight > 0 && stored < 1)
                    {
                        if (left) { leftThumbPose = thumb.localRotation; leftThumbCaptured = true; }
                        else { rightThumbPose = thumb.localRotation; rightThumbCaptured = true; }
                        thumb.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(thumbOffset), thumbWeight * (1 - stored));
                    }
                }
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
        }
        void OnDisable() => BeginPoseEvaluation();
        void OnDestroy() => ClearModels();
    }
}
