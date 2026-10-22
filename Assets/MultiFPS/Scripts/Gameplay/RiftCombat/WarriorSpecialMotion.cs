using System.Collections.Generic;
using Mirror;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
// Skeletal presentation only. Never translates/rotates the network root or collider.
[DefaultExecutionOrder(12500)]public sealed class WarriorSpecialMotion:MonoBehaviour {
 public int ActiveSlot {get;private set;}=-1;public bool IsAnimating=>ActiveSlot>=0;
 WarriorAvatar avatar;CharacterInstance actor;AbilityDefinition ability;float started;
 readonly Dictionary<Transform,Quaternion> rotations=new Dictionary<Transform,Quaternion>();Transform movedHips;Vector3 hipOffset;
 public bool Play(WarriorAvatar target,int slot,AbilityDefinition definition){
  Stop();if(!target||!target.Animator||!target.Animator.isHuman||slot<4||slot>6||!definition)return false;
  avatar=target;actor=target.GetComponent<CharacterInstance>();ability=definition;ActiveSlot=slot;started=Time.time;return true;
 }
 void Update()=>Restore();
 void Restore(){foreach(var entry in rotations)if(entry.Key)entry.Key.localRotation=entry.Value;rotations.Clear();if(movedHips)movedHips.localPosition-=hipOffset;movedHips=null;hipOffset=Vector3.zero;}
 void Turn(HumanBodyBones bone,Vector3 degrees,float weight){var t=avatar.Animator.GetBoneTransform(bone);if(!t)return;if(!rotations.ContainsKey(t))rotations.Add(t,t.localRotation);t.localRotation=t.localRotation*Quaternion.Slerp(Quaternion.identity,Quaternion.Euler(degrees),Mathf.Clamp01(weight));}
 void Spin(float degrees){var hips=avatar.Animator.GetBoneTransform(HumanBodyBones.Hips);if(!hips)return;if(!rotations.ContainsKey(hips))rotations.Add(hips,hips.localRotation);hips.rotation=Quaternion.AngleAxis(degrees,avatar.Animator.transform.up)*hips.rotation;}
 void Lift(float y){movedHips=avatar.Animator.GetBoneTransform(HumanBodyBones.Hips);if(!movedHips)return;hipOffset=new Vector3(0,y/Mathf.Max(.01f,movedHips.parent.lossyScale.y),0);movedHips.localPosition+=hipOffset;}
 void LateUpdate(){
  if(ActiveSlot<0)return;if(!avatar||!avatar.Animator||!actor||!actor.Health||actor.Health.CurrentHealth<=0){Stop();return;}
  var owner=AbilityController.For(actor.Health);float elapsed=Time.time-started,duration=Mathf.Max(.1f,ability.CastDuration);
  if(elapsed>.15f&&elapsed<duration-.04f&&owner&&owner.Casting!=ActiveSlot){Stop();return;}
  if(elapsed>=duration&&(ActiveSlot!=6||!owner||owner.DefensiveUntil<=NetworkTime.time)){Stop();return;}
  float blend=Mathf.SmoothStep(0,1,elapsed/.07f);
  if(ActiveSlot==4){
   float hit=Mathf.Max(.12f,ability.HitTime);float launch=Mathf.Clamp01((elapsed-.08f)/Mathf.Max(.05f,hit-.08f));float landing=Mathf.Clamp01((elapsed-hit)/Mathf.Max(.05f,duration-hit));
   float tuck=elapsed<hit?Mathf.Sin(launch*Mathf.PI):Mathf.Sin(landing*Mathf.PI)*.55f;
   float crouch=elapsed<.08f?Mathf.Sin(elapsed/.08f*Mathf.PI):elapsed>=hit?Mathf.Sin(landing*Mathf.PI):0;
   Lift(elapsed<hit?Mathf.Sin(launch*Mathf.PI)*.6f:-crouch*.16f);
   Turn(HumanBodyBones.Spine,new Vector3(18+18*tuck,0,0),blend*(1-landing));
   Turn(HumanBodyBones.LeftUpperLeg,new Vector3(-48,0,0),tuck);Turn(HumanBodyBones.RightUpperLeg,new Vector3(-32,0,0),tuck);
   Turn(HumanBodyBones.LeftLowerLeg,new Vector3(62,0,0),tuck);Turn(HumanBodyBones.RightLowerLeg,new Vector3(48,0,0),tuck);
   Turn(HumanBodyBones.RightUpperArm,new Vector3(-75,0,-15),tuck);Turn(HumanBodyBones.LeftUpperArm,new Vector3(-60,0,15),tuck);
  }else if(ActiveSlot==5){
   float spinEnd=Mathf.Max(.2f,duration-.2f);float turn=360*4*Mathf.Clamp01(elapsed/spinEnd);Spin(turn);
   float weight=blend*Mathf.Clamp01((duration-elapsed)/.18f);Turn(HumanBodyBones.Spine,new Vector3(8,0,0),weight);Turn(HumanBodyBones.RightUpperArm,new Vector3(-20,0,-68),weight);Turn(HumanBodyBones.LeftUpperArm,new Vector3(-12,0,52),weight);Turn(HumanBodyBones.RightLowerArm,new Vector3(0,0,-20),weight);
  }else{
   float left=owner?Mathf.Max(0,(float)(owner.DefensiveUntil-NetworkTime.time)):0;float weight=blend*(elapsed<duration?1:Mathf.Clamp01(left/.2f));
   Turn(HumanBodyBones.Spine,new Vector3(12,-8,0),weight);Turn(HumanBodyBones.Chest,new Vector3(5,0,0),weight);
   Turn(HumanBodyBones.LeftUpperArm,new Vector3(-38,20,30),weight);Turn(HumanBodyBones.LeftLowerArm,new Vector3(-45,0,30),weight);
   Turn(HumanBodyBones.RightUpperArm,new Vector3(-20,-20,-28),weight);Turn(HumanBodyBones.RightLowerArm,new Vector3(-40,0,-25),weight);
   if(avatar.Velocity.sqrMagnitude<.1f)Lift(-.08f*weight);
  }
 }
 public void Stop(){Restore();ActiveSlot=-1;ability=null;}
 void OnDisable()=>Stop();void OnDestroy()=>Stop();
}
}
