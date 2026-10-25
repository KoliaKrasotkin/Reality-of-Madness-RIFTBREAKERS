using System.Collections;
using Mirror;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public sealed partial class AbilityController {
 Coroutine weaponDrawRoutine;bool weaponDrawQueued;uint weaponDrawTarget;int weaponDrawRevision;
 public bool ServerWeaponCombatActive=>Actor&&Actor.Health.CurrentHealth>0&&(weaponDrawQueued||autoPending||Casting>=0||(Player.LastCombatAt>0&&NetworkTime.time-Player.LastCombatAt<8));
 // No resource, cooldown, damage or swing is committed until the existing draw completes.
 bool QueueWeaponDraw(int slot){
  var avatar=Actor?Actor.GetComponent<WarriorAvatar>():null;
  if(!avatar||avatar.CanUseWeapon)return false;
  if(weaponDrawQueued||avatar.IsRolling)return true;
  weaponDrawQueued=true;weaponDrawTarget=TargetNetId;int revision=++weaponDrawRevision;
  avatar.SetWeaponDrawn(true);weaponDrawRoutine=StartCoroutine(WaitForWeapon(slot,avatar,revision));return true;
 }
 IEnumerator WaitForWeapon(int slot,WarriorAvatar avatar,int revision){
  double deadline=NetworkTime.time+6;
  while(revision==weaponDrawRevision&&avatar&&Actor&&Actor.Health.CurrentHealth>0&&!avatar.CanUseWeapon&&NetworkTime.time<deadline)yield return null;
  if(revision!=weaponDrawRevision)yield break;
  bool valid=avatar&&avatar.CanUseWeapon&&Actor&&Actor.Health.CurrentHealth>0&&TargetNetId==weaponDrawTarget;
  weaponDrawQueued=false;weaponDrawRoutine=null;
  if(valid){if(slot==0)ServerAutoAttack();else ServerUse(slot);}
 }
 void CancelWeaponDraw(){weaponDrawRevision++;weaponDrawQueued=false;if(weaponDrawRoutine!=null){StopCoroutine(weaponDrawRoutine);weaponDrawRoutine=null;}}
}
}
