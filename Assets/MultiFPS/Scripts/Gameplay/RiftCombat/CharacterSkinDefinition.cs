using System;
using UnityEngine;

namespace MultiFPS.Gameplay.RiftCombat
{
    [CreateAssetMenu(menuName = "ROM/Characters/Skin")]
    public sealed class CharacterSkinDefinition : ScriptableObject
    {
        public string Id;
        public int SkinId;
        [Tooltip("Visual prefab only: Animator, renderers and optional RigBuilder. No player motor or NetworkIdentity.")]
        public GameObject VisualPrefab;
        [Tooltip("Own state machine, not necessarily the Warrior controller. Weapon entries may select another controller owned by this skin.")]
        public RuntimeAnimatorController Controller;
        public WeaponDefinition DefaultWeapon;
        [Tooltip("Optional extra locomotion speed parameter. The shared motor owns the standard movement/jump/roll parameters.")]
        public string SpeedParameter = "Speed";
        public bool PreviewAssignedAttacksOnly;
        public BackSocketPose[] BackSocketPoses = Array.Empty<BackSocketPose>();
        public SkinAction[] Actions = Array.Empty<SkinAction>();
        public SkinWeaponPresentation[] Weapons = Array.Empty<SkinWeaponPresentation>();

        public SkinAction FindAction(string id)
        {
            foreach (var action in Actions) if (action != null && action.Id == id) return action;
            return null;
        }

        public SkinWeaponPresentation FindWeapon(WeaponDefinition weapon)
        {
            foreach (var entry in Weapons) if (entry != null && entry.Weapon == weapon) return entry;
            return null;
        }
    }

    [Serializable]
    public sealed class BackSocketPose
    {
        public AnimationClip Clip;
        public Vector3 Position;
        public Vector3 Rotation;
    }

    [Serializable]
    public sealed class SkinAction
    {
        [Tooltip("AbilityDefinition.Id, or a custom name used by TryPlayAction, such as aerial-attack.")]
        public string Id;
        [Tooltip("Full state path, e.g. Base Layer.AerialAttack. Add the state and its exit transitions to this skin's controller.")]
        public string State;
        [Min(0)] public int Layer;
        [Min(0)] public float Fade = .09f;
        [Tooltip("Optional float parameter used by the state's speed multiplier.")]
        public string SpeedParameter;
        [Tooltip("Optional clip for matching the ability cast duration. No clip means the state's authored speed.")]
        public AnimationClip TimingClip;
    }

    [Serializable]
    public sealed class SkinWeaponPresentation
    {
        [Tooltip("Gameplay weapon identity. Combat stats continue to come from this original definition.")]
        public WeaponDefinition Weapon;
        [Tooltip("Optional visual-only definition: model, grips, fingers and per-animation IK calibrated for THIS skin. Include its companion shield here.")]
        public WeaponDefinition Presentation;
        [Tooltip("Optional controller for this skin + weapon combination. Empty keeps the skin's base controller.")]
        public RuntimeAnimatorController Controller;
    }

}
