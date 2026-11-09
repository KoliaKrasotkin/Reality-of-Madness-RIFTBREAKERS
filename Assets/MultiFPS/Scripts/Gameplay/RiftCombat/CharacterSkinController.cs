using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS.Gameplay.RiftCombat
{
    // Cosmetic selection. Existing PlayerInfo skin replication reaches this through WarriorAvatar.SetSkin.
    [DefaultExecutionOrder(170), DisallowMultipleComponent]
    public sealed class CharacterSkinController : MonoBehaviour
    {
        [Serializable]
        public sealed class SkinSlot
        {
            public CharacterSkinDefinition Definition;
            [Tooltip("Optional visual already inside this player prefab. Otherwise Definition.VisualPrefab is instantiated.")]
            public Animator ExistingAnimator;
            [Tooltip("Root to show/hide when switching. Must be a child of the player, never the gameplay root.")]
            public GameObject ExistingVisualRoot;
            [Tooltip("Optional weapon already inside this visual, for definitions that use UsesBuiltInWeapon.")]
            public GameObject BuiltInWeapon;
        }

        public SkinSlot[] Skins = Array.Empty<SkinSlot>();
        [Min(0)] public int InitialIndex;
        public Transform VisualParent;
        public CharacterSkinDefinition Current { get; private set; }
        public Animator Animator { get; private set; }
        public bool IsActive => isActiveAndEnabled && Current && Animator;
        readonly Dictionary<int, GameObject> instances = new Dictionary<int, GameObject>();
        readonly Dictionary<int, AnimatorControllerParameterType> parameters = new Dictionary<int, AnimatorControllerParameterType>();
        WarriorAvatar motor;
        CharacterInstance actor;
        CharacterWeaponPresentation presentation;
        GameObject currentRoot;
        Vector3 previous;
        int currentIndex = -1;
        int requestedIndex = -1;

        void Awake() { motor = GetComponent<WarriorAvatar>(); actor = GetComponent<CharacterInstance>(); previous = transform.position; }
        void Start() { if (!Current) SelectSkin(requestedIndex >= 0 ? requestedIndex : InitialIndex); }

        public bool SelectSkinId(int id)
        {
            for (int i = 0; i < Skins.Length; i++)
                if (Skins[i]?.Definition && Skins[i].Definition.SkinId == id) return SelectSkin(i);
            return false;
        }

        public bool SelectSkin(int index)
        {
            if (index < 0 || index >= Skins.Length || Skins[index] == null || !Skins[index].Definition) return false;
            var definition = Skins[index].Definition;
            if (!definition.Controller) return false;
            if (!Skins[index].ExistingAnimator && (!definition.VisualPrefab ||
                definition.VisualPrefab.GetComponent<WarriorAvatar>() || definition.VisualPrefab.GetComponent<Mirror.NetworkIdentity>())) return false;
            requestedIndex = index;
            if (!Application.isPlaying) return false;
            if (currentIndex == index && IsActive) return true;
            // Keep action timing and roll distance stable until the current action finishes.
            if (motor && motor.IsRolling) return false;
            return ApplySkin(index);
        }

        bool ApplySkin(int index)
        {
            var slot = Skins[index];
            var next = slot.ExistingAnimator;
            var root = slot.ExistingVisualRoot;
            if (!next)
            {
                if (!slot.Definition.VisualPrefab) return false;
                if (!instances.TryGetValue(index, out root))
                {
                    root = Instantiate(slot.Definition.VisualPrefab, VisualParent ? VisualParent : transform);
                    root.name = slot.Definition.name + " Visual";
                    instances.Add(index, root);
                }
                next = root.GetComponentInChildren<Animator>(true);
            }
            if (!root && next) root = next.gameObject;
            if (!next || !next.transform.IsChildOf(transform) || next.transform == transform ||
                (root && (root == gameObject || !root.transform.IsChildOf(transform) || !next.transform.IsChildOf(root.transform)))) return false;
            var controller = slot.Definition.Controller;
            if (!controller) return false;
            var equipment = GetComponent<CharacterWeaponPresentation>();
            if (!equipment && slot.Definition.DefaultWeapon) equipment = gameObject.AddComponent<CharacterWeaponPresentation>();
            presentation = equipment;
            if (equipment) equipment.UnbindSkin();
            if (!Current && motor && motor.Animator && motor.Animator != next)
            {
                motor.Animator.enabled = false;
                if (motor.Animator.gameObject != gameObject && motor.Animator.transform.IsChildOf(transform)) motor.Animator.gameObject.SetActive(false);
            }
            foreach (var other in Skins)
                if (other != null && other.ExistingVisualRoot && other.ExistingVisualRoot != root &&
                    other.ExistingVisualRoot != gameObject && other.ExistingVisualRoot.transform.IsChildOf(transform) &&
                    !next.transform.IsChildOf(other.ExistingVisualRoot.transform)) other.ExistingVisualRoot.SetActive(false);
            if (currentRoot && currentRoot != root) currentRoot.SetActive(false);
            if (Animator && Animator != next) Animator.enabled = false;
            Current = slot.Definition; Animator = next; currentRoot = root; currentIndex = index;
            if (root) root.SetActive(true);
            Animator.enabled = true;
            SetController(controller);
            if (motor) motor.Animator = Animator;
            if (!Animator.GetComponent<RiftFootstepEvents>()) Animator.gameObject.AddComponent<RiftFootstepEvents>();
            if (equipment)
            {
                equipment.BuiltInWeapon = slot.BuiltInWeapon;
                equipment.BindSkin(Animator);
            }
            if (equipment && !equipment.CurrentWeapon && Current.DefaultWeapon) equipment.Equip(Current.DefaultWeapon);
            previous = transform.position;
            return true;
        }

        public void SetController(RuntimeAnimatorController controller)
        {
            if (!Animator || !controller) return;
            if (Animator.runtimeAnimatorController != controller) Animator.runtimeAnimatorController = controller;
            parameters.Clear();
            foreach (var parameter in Animator.parameters) parameters[parameter.nameHash] = parameter.type;
        }

        public bool HasParameter(string name, AnimatorControllerParameterType type) => !string.IsNullOrEmpty(name) && parameters.TryGetValue(UnityEngine.Animator.StringToHash(name), out var found) && found == type;
        public void SetFloat(string name, float value) { if (HasParameter(name, AnimatorControllerParameterType.Float)) Animator.SetFloat(name, value); }
        public void SetBool(string name, bool value) { if (HasParameter(name, AnimatorControllerParameterType.Bool)) Animator.SetBool(name, value); }

        public bool TryPlayAction(string id, float duration = 0)
        {
            if (!IsActive || (actor && actor.Health && actor.Health.CurrentHealth <= 0)) return false;
            var action = Current.FindAction(id);
            if (action == null || string.IsNullOrEmpty(action.State) || action.Layer < 0 || action.Layer >= Animator.layerCount) return false;
            int state = UnityEngine.Animator.StringToHash(action.State);
            if (!Animator.HasState(action.Layer, state)) return false;
            var equipment = GetComponent<CharacterWeaponPresentation>();
            var clip = equipment ? equipment.ResolveClip(action.TimingClip) : action.TimingClip;
            if (clip && duration > 0) SetFloat(action.SpeedParameter, clip.length / Mathf.Max(.1f, duration));
            Animator.CrossFadeInFixedTime(state, action.Fade, action.Layer, 0);
            return true;
        }

        void LateUpdate()
        {
            if (!IsActive) return;
            if (requestedIndex != currentIndex && (!motor || !motor.IsRolling)) SelectSkin(requestedIndex);
            bool alive = !actor || !actor.Health || actor.Health.CurrentHealth > 0;
            if (presentation) presentation.Alive = alive;
            var velocity = motor && motor.isOwned ? motor.Velocity : (transform.position - previous) / Mathf.Max(.001f, Time.deltaTime);
            previous = transform.position;
            if (velocity.magnitude > 15) velocity = Vector3.zero;
            SetFloat(Current.SpeedParameter, alive ? velocity.magnitude : 0);
        }
    }
}
