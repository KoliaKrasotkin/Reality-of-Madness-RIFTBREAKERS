using UnityEngine;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
namespace ROM.UI.Gameplay
{
    public sealed class RiftHUDTargetMarker:MonoBehaviour
    {
        public LineRenderer Circle,Diamond;
        Health target;Transform head;
        public Health Target=>target;
        public void SetTarget(Health health){if(!health){target=null;head=null;gameObject.SetActive(false);return;}if(target==health)return;target=health;head=null;if(target){var actor=target.GetComponent<CharacterInstance>();var native=target.GetComponent<WarriorAvatar>();Animator animator=native?native.Animator:null;if(!animator&&actor&&actor.CharacterParent){var pose=actor.CharacterParent.GetComponentInChildren<IDSGameplayPose>();if(pose)animator=pose.Target;}if(animator&&animator.isHuman)head=animator.GetBoneTransform(HumanBodyBones.Head);}gameObject.SetActive(target);}
        public void Follow(Camera camera)
        {
            if(!target||!camera)return;
            var center=target.transform.position;
            if(Physics.Raycast(center+Vector3.up*2,Vector3.down,out var hit,5,GameManager.environmentLayer,QueryTriggerInteraction.Ignore))center.y=hit.point.y;
            center.y+=.04f;
            Circle.transform.position=center;
            Diamond.transform.position=(head?head.position:target.transform.position+Vector3.up*1.9f)+Vector3.up*.47f;
            Diamond.transform.rotation=Quaternion.LookRotation(camera.transform.forward,camera.transform.up);
        }
    }
}
