using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat
{
    // A palm frame makes socket offsets independent of a humanoid rig's wrist axes.
    public static class WeaponGripSpace
    {
        public static Quaternion BodyRotation(Animator animator)
        {
            var left = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var right = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (!left || !right || !chest || !hips) return animator.transform.rotation;
            var up = chest.position - hips.position;
            var forward = Vector3.Cross(right.position - left.position, up);
            return up.sqrMagnitude > .000001f && forward.sqrMagnitude > .000001f
                ? Quaternion.LookRotation(forward, up) : animator.transform.rotation;
        }
        public static Quaternion Rotation(Animator animator, HumanBodyBones hand)
        {
            bool left = hand == HumanBodyBones.LeftHand;
            var wrist = animator.GetBoneTransform(hand);
            var index = animator.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
            var little = animator.GetBoneTransform(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
            if (!index || !little) return wrist.rotation;
            var forward = (index.position + little.position) * .5f - wrist.position;
            var up = Vector3.Cross(forward, index.position - little.position) * (left ? -1 : 1);
            return forward.sqrMagnitude > .000001f && up.sqrMagnitude > .000001f
                ? Quaternion.LookRotation(forward, up) : wrist.rotation;
        }
    }
}
