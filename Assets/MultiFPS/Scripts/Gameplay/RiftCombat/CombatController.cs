using System;
using Mirror;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
// Health remains the single death/reward/RPC authority. No VFX calls this API.
public static class CombatController {
 public static event Action<Health,Health,int> Damaged;
 public static int Resolve(Health victim,Health attacker,int damage){
  if(!NetworkServer.active||damage<=0||attacker==victim)return damage;
  var player=AbilityController.For(victim);if(player&&player.Prototype)player.Player.LastCombatAt=NetworkTime.time;
  if(player&&player.Prototype)damage=Mathf.Max(1,Mathf.RoundToInt(damage*(1-player.Specialization.Armor)*(player.BuffUntil>NetworkTime.time?1-player.BuffReduction:1)));
  Damaged?.Invoke(victim,attacker,damage);return damage;
 }
 public static bool Hit(Health attacker,Health victim,int damage){
  if(!NetworkServer.active||!attacker||!victim||attacker==victim||victim.CurrentHealth<=0||attacker.Team==victim.Team)return false;
  if(GameManager.Gamemode&&GameManager.Gamemode.BlockDamage(victim,attacker))return false;
  victim.Server_ChangeHealthStateRaw(Mathf.Max(1,damage),0,AttackType.hitscan,attacker,0);return true;
 }
 public static bool Visible(CharacterInstance actor,Health target){Vector3 start=actor.transform.position+Vector3.up*1.2f;Vector3 delta=target.transform.position+Vector3.up-start;return !actor.gameObject.scene.GetPhysicsScene().Raycast(start,delta.normalized,out _,delta.magnitude,GameManager.environmentLayer,QueryTriggerInteraction.Ignore);}
}}
