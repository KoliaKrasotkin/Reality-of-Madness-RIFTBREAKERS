using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using ROM.Horde;
using ROM.UI.Gameplay;
using Mirror;
namespace ROM.EditorTools {
public static partial class RiftBagReview {
 static Vector3 warriorRoot;static Quaternion warriorRotation,hipRotation;static uint warriorIdentity;static float headHeight;
 static void Ready(AbilityController combat){combat.Resource=100;combat.GlobalReady=0;for(int i=0;i<combat.Ready.Count;i++)combat.Ready[i]=0;}
 static void WarriorTick(PlayerInstance player,CharacterInstance actor,RiftGameplayHUD hud){
  var avatar=actor.GetComponent<WarriorAvatar>();var combat=player.GetComponent<AbilityController>();if(!avatar||!combat)return;var motion=actor.GetComponent<WarriorSpecialMotion>();
  if(step==200){
   Check(avatar.Animator.isHuman&&avatar.Animator.avatar.isValid,"Warrior uses a valid Humanoid avatar");var priorGuids=new[]{"ae08ba7ef4f6e74468f1a98f8790b9fc","fbc9ce0e022fe2244a4793a9367ba770","f92a8ef198d195145952e16b77f3b008"};Check(Enumerable.Range(0,3).All(i=>UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(combat.Definition(i+4).Icon))==priorGuids[i]),"Three Warrior icons match exact pre-change references");Check(new[]{4,5,6}.All(i=>!UnityEditor.AssetDatabase.GetAssetPath(combat.Definition(i).Icon).Contains("AbilityIcons")),"Draft icon assets are not assigned to gameplay abilities");
   var outside=UnityEngine.Object.FindObjectsByType<RiftEnemySpawnZone>(FindObjectsSortMode.None).Where(z=>z&&z.SpawnPoints!=null).SelectMany(z=>z.SpawnPoints).First(t=>t&&!ROM.Horde.Horde.Instance.IsSafe(t.position));Check(NavMesh.SamplePosition(outside.position,out var nav,15,NavMesh.AllAreas),"Animation fixture located outside safe zone on navigation mesh");actor.ServerTeleport(nav.position,0);RiftMMOController.Instance.CameraDistance=3;RiftMMOController.Instance.CameraPitch=8;RiftMMOController.Instance.CameraYaw=45;var lightFixture=new GameObject("Warrior presentation review light").AddComponent<Light>();lightFixture.type=LightType.Point;lightFixture.intensity=4;lightFixture.range=12;lightFixture.transform.SetParent(actor.transform,false);lightFixture.transform.localPosition=new Vector3(1,3,-2);warriorIdentity=actor.netId;warriorRoot=actor.transform.position;warriorRotation=actor.transform.rotation;Ready(combat);Check(combat.ServerUse(4),"Real server use starts Salto de guerra");Advance(.18);return;
  }
  if(step==201){Check(motion&&motion.ActiveSlot==4,"Leap RPC starts dedicated skeletal animation");Check((actor.transform.position-warriorRoot).sqrMagnitude<1,"Visual takeoff does not translate the network root before impact");Capture("rift-warrior-leap",1280,720);Advance(.7);return;}
  if(step==202){Check(!motion.IsAnimating,"Leap restores locomotion at cast end");Check(Vector3.Distance(actor.transform.position,warriorRoot)<=12.5f,"Leap retains existing server maximum displacement");Check(!avatar.Animator.applyRootMotion,"Animator root motion remains disabled");Ready(combat);warriorRoot=actor.transform.position;warriorRotation=actor.transform.rotation;hipRotation=avatar.Animator.GetBoneTransform(HumanBodyBones.Hips).localRotation;headHeight=avatar.Animator.GetBoneTransform(HumanBodyBones.Head).position.y-actor.transform.position.y;Check(combat.ServerUse(5),"Real server use starts Torbellino");Advance(.25);return;}
  if(step==203){Check(motion.IsAnimating&&motion.ActiveSlot==5,"Whirl RPC starts dedicated skeletal spin");Check(Mathf.Abs(avatar.Animator.GetBoneTransform(HumanBodyBones.Head).position.y-actor.transform.position.y-headHeight)<.4f,"Whirl rotates around world up without tipping the skeleton sideways");var hip=avatar.Animator.GetBoneTransform(HumanBodyBones.Hips);Check(Quaternion.Angle(hipRotation,hip.localRotation)>15,"Whirl visibly rotates the character skeleton");hipRotation=hip.localRotation;Check(Quaternion.Angle(warriorRotation,actor.transform.rotation)<.1f,"Skeletal spin does not rotate the network root");Capture("rift-warrior-whirl",1280,720);Advance(.3);return;}
  if(step==204){Check(Quaternion.Angle(hipRotation,avatar.Animator.GetBoneTransform(HumanBodyBones.Hips).localRotation)>15,"Whirl advances across distinct rotation poses");combat.Cancel();Advance(.2);return;}
  if(step==205){Check(!motion.IsAnimating,"Cancelling Whirl clears procedural pose");Ready(combat);Check(combat.ServerUse(6),"Real server use starts Última resistencia");Advance(.25);return;}
  if(step==206){Check(motion.ActiveSlot==6&&combat.DefensiveUntil>NetworkTime.time,"Defensive stance follows real buff activation");Capture("rift-warrior-laststand",1280,720);Advance(.6);return;}
  if(step==207){Check(motion.IsAnimating&&motion.ActiveSlot==6&&combat.Casting<0,"Defensive pose holds after cast while protection is active");Ready(combat);Check(combat.ServerUse(1),"Ordinary attack remains usable during defensive buff");Advance(.18);return;}
  if(step==208){Check(!motion.IsAnimating,"Ordinary attack yields the special pose to normal animation");combat.Cancel();Ready(combat);Check(combat.ServerUse(4),"Leap can be cast again after interruption");actor.Health.Server_ChangeHealthStateRaw(999999,0,AttackType.falldamage,actor.Health,0);Advance(.3);return;}
  if(step==209){Check(actor.Health.CurrentHealth<=0,"Actual server death interrupts active special");Check(!motion||!motion.IsAnimating,"Death clears special animation offsets");Advance(30);return;}
  if(step==210){Check(actor.Health.CurrentHealth>0,"Existing respawn returns a living Warrior");var fresh=actor.GetComponent<WarriorSpecialMotion>();Check(!fresh||!fresh.IsAnimating,"Respawn has no stale procedural ability pose");Check(!avatar.Animator.applyRootMotion,"Respawn preserves movement authority without root motion");Finish(true);}
 }
}
}
