using MultiFPS;
using MultiFPS.UI;
using UnityEngine;
namespace MultiFPS.Gameplay {
    [DefaultExecutionOrder(200)]
    public sealed class RiftThirdPersonCamera : MonoBehaviour {
        public float Distance=3.6f;
        Transform conversationTarget,conversationHead;CharacterInstance conversationOwner;
        RiftMMOController conversationInput;System.Action conversationCancelled;
        Vector3 conversationPosition;Quaternion conversationRotation;
        float savedYaw,savedPitch,savedDistance,savedNear,conversationRange;int openingHealth;
        double openingCombat;bool returning;float returnStarted;
        public bool ConversationActive=>conversationTarget;
        public bool ConversationReturning=>returning;
        public bool ConversationHasClearShot{get;private set;}
        public bool BeginConversation(Transform npc,Transform head,float range,System.Action cancel)
        {
            var owner=ClientFrontend.OwnedCharacterInstance;var input=RiftMMOController.Instance;
            if(!npc||!owner||!input||input.Actor!=owner||!owner.Health||owner.Health.CurrentHealth<=0||Vector3.Distance(owner.transform.position,npc.position)>range)return false;
            var combatOwner=ClientFrontend.ClientPlayerInstance?ClientFrontend.ClientPlayerInstance.GetComponent<MultiFPS.Gameplay.RiftCombat.AbilityController>():null;if(combatOwner&&combatOwner.Casting>=0)return false;EndConversation(true);conversationTarget=npc;conversationOwner=owner;conversationInput=input;
            conversationHead=head;var animator=npc.GetComponentInChildren<Animator>();
            if(!conversationHead&&animator&&animator.isHuman)conversationHead=animator.GetBoneTransform(HumanBodyBones.Head);
            if(!conversationHead)foreach(var skin in npc.GetComponentsInChildren<SkinnedMeshRenderer>())foreach(var bone in skin.bones)if(bone&&bone.name.ToLowerInvariant().Contains("head")&&!bone.name.ToLowerInvariant().Contains("end")){conversationHead=bone;break;}
            savedYaw=input.CameraYaw;savedPitch=input.CameraPitch;savedDistance=input.CameraDistance;savedNear=GetComponent<Camera>().nearClipPlane;
            openingHealth=owner.Health.CurrentHealth;var player=ClientFrontend.ClientPlayerInstance;openingCombat=player?player.LastCombatAt:0;
            conversationPosition=transform.position;conversationRotation=transform.rotation;conversationRange=range+1;conversationCancelled=cancel;returning=false;return true;
        }
        public void EndConversation(bool immediate=false)
        {
            if(!conversationTarget&&returning&&!immediate)return;if(!conversationTarget&&!returning&&conversationOwner==null)return;
            conversationTarget=null;conversationHead=null;conversationCancelled=null;
            if(conversationInput){conversationInput.CameraYaw=savedYaw;conversationInput.CameraPitch=savedPitch;conversationInput.CameraDistance=savedDistance;}
            returning=!immediate;returnStarted=Time.unscaledTime;if(immediate){var camera=GetComponent<Camera>();if(camera)camera.nearClipPlane=savedNear;conversationOwner=null;conversationInput=null;}
        }
        void CancelConversation(){var callback=conversationCancelled;EndConversation();callback?.Invoke();}
        bool InvalidConversation()
        {
            if(!conversationTarget||!conversationTarget.gameObject.activeInHierarchy||!conversationOwner||ClientFrontend.OwnedCharacterInstance!=conversationOwner||conversationTarget.gameObject.scene!=conversationOwner.gameObject.scene||!conversationOwner.Health||conversationOwner.Health.CurrentHealth<=0||conversationOwner.Health.CurrentHealth<openingHealth||Vector3.Distance(conversationOwner.transform.position,conversationTarget.position)>conversationRange)return true;
            var player=ClientFrontend.ClientPlayerInstance;var combat=player?player.GetComponent<MultiFPS.Gameplay.RiftCombat.AbilityController>():null;
            return combat&&combat.Casting>=0||player&&player.LastCombatAt>openingCombat+.0001;
        }
        bool ClearSegment(Vector3 from,Vector3 to,out float available)
        {
            var delta=to-from;available=delta.magnitude;if(available<.001f)return true;
            foreach(var hit in Physics.SphereCastAll(from,.18f,delta.normalized,available,GameManager.environmentLayer,QueryTriggerInteraction.Ignore))
            {
                if(conversationTarget&&hit.collider.transform.IsChildOf(conversationTarget)||conversationOwner&&hit.collider.transform.IsChildOf(conversationOwner.transform))continue;
                available=Mathf.Min(available,Mathf.Max(0,hit.distance-.12f));
            }
            return available>=delta.magnitude-.01f;
        }
        bool ConversationPose(out Vector3 position,out Quaternion rotation)
        {
            position=transform.position;rotation=transform.rotation;var focus=conversationHead?conversationHead.position:conversationTarget.position+Vector3.up*1.55f;
            var forward=conversationTarget.forward;var animator=conversationTarget.GetComponentInChildren<Animator>();if(animator)forward=animator.transform.forward;forward.y=0;if(forward.sqrMagnitude<.01f)forward=Vector3.forward;forward.Normalize();
            foreach(float angle in new[]{15f,-15f,35f,-35f,60f,-60f})
            {
                var direction=Quaternion.AngleAxis(angle,Vector3.up)*forward;float distanceToHead=Mathf.Abs(focus.y-conversationTarget.position.y);float framingDistance=Mathf.Clamp(distanceToHead*.85f,1.25f,2.5f);var candidate=focus+direction*framingDistance+Vector3.up*.04f;
                ClearSegment(focus,candidate,out float distance);if(distance<1.2f)continue;candidate=focus+(candidate-focus).normalized*Mathf.Min((candidate-focus).magnitude,distance);
                var facing=Quaternion.LookRotation(focus-candidate,Vector3.up);var aim=focus+facing*Vector3.right*.6f-Vector3.up*.12f;
                position=candidate;rotation=Quaternion.LookRotation(aim-candidate,Vector3.up);return true;
            }
            return false;
        }
        void ApplyConversation(Vector3 normalPosition,Quaternion normalRotation)
        {
            if(conversationOwner&&!returning&&InvalidConversation())CancelConversation();
            Vector3 destination=normalPosition;Quaternion rotation=normalRotation;
            if(conversationTarget)ConversationHasClearShot=ConversationPose(out destination,out rotation);
            else ConversationHasClearShot=false;
            if(!conversationTarget&&!returning)return;
            var next=Vector3.Lerp(conversationPosition,destination,1-Mathf.Exp(-Time.unscaledDeltaTime*9));
            if(!ClearSegment(conversationPosition,next,out float available))next=conversationPosition+(next-conversationPosition).normalized*available;
            conversationPosition=next;conversationRotation=Quaternion.Slerp(conversationRotation,rotation,1-Mathf.Exp(-Time.unscaledDeltaTime*9));
            transform.SetPositionAndRotation(conversationPosition,conversationRotation);
            if(returning&&(Time.unscaledTime-returnStarted>.8f||Vector3.Distance(conversationPosition,normalPosition)<.025f&&Quaternion.Angle(conversationRotation,normalRotation)<.5f)){GetComponent<Camera>().nearClipPlane=savedNear;returning=false;conversationOwner=null;conversationInput=null;transform.SetPositionAndRotation(normalPosition,normalRotation);}
        }
        void OnDisable(){EndConversation(true);}

        void LateUpdate(){
            var character=ClientFrontend.ObservedCharacterInstance;if(!character||!character.FPPCameraTarget){if(conversationOwner)CancelConversation();return;}
            var camera=GetComponent<Camera>();camera.nearClipPlane=.08f;
            if(GameplayCamera._instance.FPPCamera)GameplayCamera._instance.FPPCamera.enabled=false;
            var mmo=RiftMMOController.Instance;bool local=mmo&&mmo.Actor==character;
            Quaternion rotation=local?Quaternion.Euler(mmo.CameraPitch,mmo.CameraYaw,0):character.FPPCameraTarget.rotation;
            float distance=local?mmo.CameraDistance:Distance;
            Vector3 pivot=character.transform.position+Vector3.up*(character.IsCrouching?1.2f:1.7f);
            Vector3 offset=rotation*new Vector3(0,.25f,-distance);
            float length=offset.magnitude;
            if(Physics.SphereCast(pivot,.22f,offset.normalized,out var hit,length,GameManager.environmentLayer,QueryTriggerInteraction.Ignore))length=Mathf.Max(.35f,hit.distance-.12f);
            var position=pivot+offset.normalized*length;
            transform.SetPositionAndRotation(position,rotation);
            ApplyConversation(position,rotation);
        }
    }
}
