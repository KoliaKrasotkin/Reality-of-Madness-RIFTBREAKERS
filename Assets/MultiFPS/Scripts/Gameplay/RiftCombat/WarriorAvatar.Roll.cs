using Mirror;
using UnityEngine;

namespace MultiFPS.Gameplay.RiftCombat
{
    public sealed partial class WarriorAvatar
    {
        [Header("Forward roll")]
        public AnimationClip RollAnimation;
        public AnimationClip UnarmedRollAnimation;
        [Range(.1f, 4f)] public float RollPlaybackSpeed = 1f;
        [Tooltip("Exit the clip before its final standing/recovery tail. Playback speed is unchanged.")]
        [Range(.7f, 1f)] public float RollExitNormalizedTime = .85f;
        [Tooltip("Average travel speed of repeated rolls, including recovery, relative to sprint.")]
        [Range(1f, 2f)] public float RollTravelMultiplier = 1.2f;
        [Min(.05f)] public float RollRecovery = .15f;
        [SyncVar] private float rollFacing;
        [SyncVar] private bool rollArmed;
        [SyncVar] private double rollStartedAt = -1000;
        private double presentedRoll = -1000;
        [SyncVar] private int queuedWeaponState = -1;
        private double movedRoll = -1000;
        private float movedRollFraction;

        private AnimationClip ActiveRollAnimation => rollArmed ? RollAnimation : UnarmedRollAnimation;
        public float RollDuration => ActiveRollAnimation ? Mathf.Max(.1f, ActiveRollAnimation.length * Mathf.Clamp(RollExitNormalizedTime, .7f, 1f) / Mathf.Max(.1f, RollPlaybackSpeed)) : 1f;
        public float RollDistance => SprintSpeed * RollTravelMultiplier * (RollDuration + Mathf.Max(.05f, RollRecovery));
        public float RollFacing => rollFacing;
        public bool RequestedWeaponDrawn => queuedWeaponState >= 0 ? queuedWeaponState != 0 : weaponDrawn;
        public bool IsRolling => ActiveRollAnimation && NetworkTime.time >= rollStartedAt && NetworkTime.time < rollStartedAt + RollDuration;
        public Vector3 RollDirection => Quaternion.Euler(0, rollFacing, 0) * Vector3.forward;

        // Integrated trapezoid: accelerate into the tumble, carry momentum, then
        // settle before standing. The integral is exactly one, regardless of tick rate.
        public static float RollTravelFraction(float progress)
        {
            float t = Mathf.Clamp01(progress);
            const float accelerationEnd = .12f, brakingStart = .65f, brakingEnd = .9f;
            const float area = accelerationEnd * .5f + brakingStart - accelerationEnd + (brakingEnd - brakingStart) * .5f;
            if (t < accelerationEnd) return t * t / (2 * accelerationEnd * area);
            if (t < brakingStart) return (accelerationEnd * .5f + t - accelerationEnd) / area;
            if (t < brakingEnd)
            {
                float brake = t - brakingStart;
                return (accelerationEnd * .5f + brakingStart - accelerationEnd + brake - brake * brake / (2 * (brakingEnd - brakingStart))) / area;
            }
            return 1;
        }

        private Vector3 RollStepVelocity(float deltaTime) => RollStepDisplacement(NetworkTime.time) / Mathf.Max(.0001f, deltaTime);

        private Vector3 RollStepDisplacement(double now)
        {
            if (movedRoll != rollStartedAt) { movedRoll = rollStartedAt; movedRollFraction = 0; }
            float progress = (float)(now - rollStartedAt) / RollDuration;
            float fraction = RollTravelFraction(progress);
            float distance = Mathf.Max(0, fraction - movedRollFraction) * RollDistance;
            movedRollFraction = fraction;
            return RollDirection * distance;
        }

        // Called by the existing gameplay input gate, so chat, menus and rebinding suppress CTRL.
        public bool RequestRoll()
        {
            bool offline = !NetworkClient.active && !NetworkServer.active;
            if (!offline && !isOwned && !isServer) return false;
            if (!CanBeginRoll()) return false;
            if (offline || isServer) BeginRoll();
            else CmdRequestRoll();
            return true;
        }

        [Command]
        private void CmdRequestRoll()
        {
            if (CanBeginRoll()) BeginRoll();
        }

        private bool CanBeginRoll()
        {
            if (!actor) actor = GetComponent<CharacterInstance>();
            if (!capsule) capsule = GetComponent<CharacterController>();
            if (!(weaponDrawn ? RollAnimation : UnarmedRollAnimation) || !Animator || !actor || !actor.Health || actor.Health.CurrentHealth <= 0 || actor.Block || !capsule || !capsule.enabled)
                return false;
            if (NetworkTime.time < rollStartedAt + RollDuration + Mathf.Max(.05f, RollRecovery) || NetworkTime.time < weaponReadyAt) return false;
            var combat = AbilityController.For(actor.Health);
            if (combat && combat.Casting >= 0) return false;
            // Client and server both check the ground; no mid-air or queued rolls.
            if (!capsule.isGrounded && !Physics.Raycast(transform.position + Vector3.up * .2f, Vector3.down, .4f,
                    GameManager.environmentLayer, QueryTriggerInteraction.Ignore)) return false;
            var state = Animator.GetCurrentAnimatorStateInfo(0);
            bool transitioning = Animator.IsInTransition(0);
            if (!transitioning && (state.IsName("Locomotion") || state.IsName("UnarmedLocomotion"))) return true;
            // Once recovery has elapsed, a completed roll's exit blend must not
            // add another cooldown. Weapon changes still finish their own animation.
            bool finishedRoll = state.IsName("RollForward") || state.IsName("UnarmedRollForward");
            if (!finishedRoll || queuedWeaponState >= 0 || weaponDrawn != rollArmed) return false;
            if (!transitioning) return true;
            var next = Animator.GetNextAnimatorStateInfo(0);
            return next.IsName(rollArmed ? "Locomotion" : "UnarmedLocomotion");
        }

        private void BeginRoll()
        {
            rollFacing = actor.Input.LookY;
            rollArmed = weaponDrawn;
            rollStartedAt = NetworkTime.time;
            activeAttackSlot = -1;
            jumpRequested = -100;
            UpdateRollPresentation();
        }

        private void UpdateRollPresentation()
        {
            if (!Animator) return;
            Animator.SetFloat("RollSpeed", Mathf.Max(.1f, RollPlaybackSpeed));
            bool rolling = IsRolling && (!actor || !actor.Health || actor.Health.CurrentHealth > 0);
            Animator.SetBool("Rolling", rolling);
            if (rolling && presentedRoll != rollStartedAt)
            {
                presentedRoll = rollStartedAt;
                Animator.SetBool("RollArmed", rollArmed);
                Animator.SetBool("DefensiveHold", false);
                activeAttackSlot = -1;
                float elapsed = Mathf.Clamp((float)(NetworkTime.time - rollStartedAt), 0, RollDuration);
                Animator.CrossFadeInFixedTime(rollArmed ? "Base Layer.RollForward" : "Base Layer.UnarmedRollForward", .04f, 0, elapsed);
            }
            if (!rolling && queuedWeaponState >= 0 && (!NetworkClient.active || isServer))
            {
                bool drawn = queuedWeaponState != 0;
                queuedWeaponState = -1;
                SetWeaponDrawn(drawn);
            }
        }
    }
}
