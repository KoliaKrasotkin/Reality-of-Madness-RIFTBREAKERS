using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat
{
    // Bridges the existing replicated Warrior build to the reusable visual profiles.
    [DefaultExecutionOrder(200)]
    public sealed class RiftWarriorEquipment : MonoBehaviour
    {
        public WeaponDefinition AxeWeapon, SwordShieldWeapon;
        WarriorAvatar avatar;
        CharacterInstance actor;
        CharacterWeaponPresentation presentation;
        void Awake()
        {
            avatar = GetComponent<WarriorAvatar>();
            actor = GetComponent<CharacterInstance>();
            presentation = GetComponent<CharacterWeaponPresentation>();
            if (!presentation) presentation = gameObject.AddComponent<CharacterWeaponPresentation>();
            presentation.Animator = avatar.Animator;
            presentation.BuiltInWeapon = avatar.EquippedWeapon;
            if (!AxeWeapon) AxeWeapon = avatar.Weapon;
            if (!SwordShieldWeapon) SwordShieldWeapon = WarriorProgressionDefinition.Load.Sword;
        }
        void LateUpdate()
        {
            if (!avatar || !actor || !presentation) return;
            var owner = AbilityController.For(actor.Health);
            var desired = owner && owner.Prototype && owner.SwordAndShield ? SwordShieldWeapon : AxeWeapon;
            presentation.Alive = actor.Health && actor.Health.CurrentHealth > 0;
            if (!avatar.IsRolling && !avatar.Animator.IsInTransition(0)) presentation.Equip(desired);
        }
    }
}
