using UnityEngine;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
namespace ROM.UI.Gameplay
{
    public sealed class RiftHUDTargetMarker:MonoBehaviour
    {
        public LineRenderer Circle,Diamond;
        readonly RaycastHit[] groundHits=new RaycastHit[24];Health target;RiftWorldTarget world;Transform head;public Material HostileMaterial,FriendlyMaterial;
        public Health Target=>target;
        bool dialogueSuppressed,circleWasEnabled,diamondWasEnabled;
        void SetDialogueSuppressed(bool hide){
            if(hide==dialogueSuppressed)return;
            if(hide){circleWasEnabled=Circle&&Circle.enabled;diamondWasEnabled=Diamond&&Diamond.enabled;if(Circle)Circle.enabled=false;if(Diamond)Diamond.enabled=false;}
            else{if(Circle)Circle.enabled=circleWasEnabled;if(Diamond)Diamond.enabled=diamondWasEnabled;}
            dialogueSuppressed=hide;
        }
        void OnDisable(){SetDialogueSuppressed(false);}
        public void SetTarget(Health health,bool friendly=false){world=null;SetColor(friendly);if(!health){target=null;head=null;gameObject.SetActive(false);return;}if(target==health)return;target=health;head=null;if(target){var actor=target.GetComponent<CharacterInstance>();var native=target.GetComponent<WarriorAvatar>();Animator animator=native?native.Animator:null;if(!animator&&actor&&actor.CharacterParent){var pose=actor.CharacterParent.GetComponentInChildren<IDSGameplayPose>();if(pose)animator=pose.Target;}if(animator&&animator.isHuman)head=animator.GetBoneTransform(HumanBodyBones.Head);}gameObject.SetActive(target);}
        public void SetWorld(RiftWorldTarget value){if(!value){SetTarget(null);return;}if(world==value)return;target=null;world=value;head=value.Head;var animator=value.GetComponentInChildren<Animator>();if(!head&&animator&&animator.isHuman)head=animator.GetBoneTransform(HumanBodyBones.Head);SetColor(true);gameObject.SetActive(true);}
        void SetColor(bool friendly){var material=friendly&&FriendlyMaterial?FriendlyMaterial:HostileMaterial;if(material){Circle.sharedMaterial=material;Diamond.sharedMaterial=material;}Circle.startColor=Circle.endColor=Diamond.startColor=Diamond.endColor=friendly?Color.white:new Color(1,.12f,.1f);}
        public void Follow(Camera camera)
        {
            var conversation=ROM.UI.Sprint01.RiftVillageUI.Instance;
            SetDialogueSuppressed(RiftHUDPanelDock.PreviewOverlayActive||conversation&&conversation.CinematicDialogue);
            if(dialogueSuppressed||(!target&&!world)||!camera)return;
            var anchor=target?target.transform:world.transform;var center=anchor.position;
            int count=Physics.RaycastNonAlloc(center+Vector3.up*2,Vector3.down,groundHits,5,GameManager.environmentLayer,QueryTriggerInteraction.Ignore);float nearest=float.PositiveInfinity;for(int i=0;i<count;i++){var hit=groundHits[i];if(hit.collider.transform==anchor||hit.collider.transform.IsChildOf(anchor))continue;if(hit.distance<nearest){nearest=hit.distance;center.y=hit.point.y;}}
            center.y+=.04f;
            Circle.transform.position=center;
            Diamond.transform.position=(head?head.position:anchor.position+Vector3.up*1.9f)+Vector3.up*.47f;
            Diamond.transform.rotation=Quaternion.LookRotation(camera.transform.forward,camera.transform.up);
        }
    }
}
