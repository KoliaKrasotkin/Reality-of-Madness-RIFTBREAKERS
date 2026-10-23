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
        RuntimeAnimatorController originalController;
        static readonly int StoredWeight = UnityEngine.Animator.StringToHash("EquipmentBackWeight");
        public bool Alive { get; set; } = true;
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
            // Variants share the base state machine and preserve its parameters/state.
            Animator.runtimeAnimatorController = weapon.Controller ? weapon.Controller : originalController;
            foreach (var attachment in weapon.EquippedParts)
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
            RefreshPose();
        }
        public AnimationClip ResolveClip(AnimationClip source)
        {
            if (!source) return null;
            var overrides = Animator ? Animator.runtimeAnimatorController as AnimatorOverrideController : null;
            return overrides && overrides[source] ? overrides[source] : source;
        }
        void LateUpdate() => RefreshPose();
        public void RefreshPose() => RefreshPose(Animator ? Animator.GetFloat(StoredWeight) : 0);
        // Editor preview graphs own their controller parameters independently of Animator.
        public void RefreshPose(float storedWeight)
        {
            if (!CurrentWeapon || !Animator) return;
            if (BuiltInWeapon) BuiltInWeapon.SetActive(Alive && CurrentWeapon.UsesBuiltInWeapon);
            float stored = Mathf.Clamp01(storedWeight);
            for (int i = 0; i < models.Count; i++)
            {
                var model = models[i];
                if (!model) continue;
                model.SetActive(Alive);
                var data = parts[i];
                var hand = Animator.GetBoneTransform(data.Socket);
                var back = Animator.GetBoneTransform(data.StoredBone);
                if (!hand || !back) { model.SetActive(false); continue; }
                var grip = data.UseGripFrame ? WeaponGripSpace.Rotation(Animator, data.Socket) : hand.rotation;
                var backRotation = WeaponGripSpace.BodyRotation(Animator);
                model.transform.SetPositionAndRotation(
                    Vector3.Lerp(hand.position + grip * data.Position, back.position + backRotation * data.StoredPosition, stored),
                    Quaternion.Slerp(grip * Quaternion.Euler(data.Rotation), backRotation * Quaternion.Euler(data.StoredRotation), stored));
                model.transform.localScale = data.Scale;
            }
        }
        void ClearModels()
        {
            foreach (var model in models) if (model)
            { model.SetActive(false); if (Application.isPlaying) Destroy(model); else DestroyImmediate(model); }
            models.Clear();
            parts.Clear();
        }
        void OnDestroy() => ClearModels();
    }
}
