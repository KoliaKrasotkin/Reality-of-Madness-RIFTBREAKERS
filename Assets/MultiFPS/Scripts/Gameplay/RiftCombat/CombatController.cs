using System;
using Mirror;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
// Health remains the single death/reward/RPC authority. No VFX calls this API.
public static class CombatController {
 public static event Action<Health,Health,int> Damaged;
 public static int Resolve(Health victim,Health attacker,int damage)=>Resolve(victim,attacker,damage,out _);
 public static int Resolve(Health victim,Health attacker,int damage,out bool critical){
  critical=false;var dealer=ForDealer(attacker);if(NetworkServer.active&&damage>0&&dealer&&!dealer.Player.BOT&&victim&&attacker!=victim&&attacker.Team!=victim.Team&&!victim.CombatTransfer){var profile=dealer.AutoProfile;damage=Mathf.Max(1,Mathf.RoundToInt(damage*dealer.PowerMultiplier));critical=UnityEngine.Random.value<dealer.CriticalChance;if(critical)damage=Mathf.Max(1,Mathf.RoundToInt(damage*Mathf.Max(1,profile.CriticalMultiplier)));}
  if(!NetworkServer.active||damage<=0||attacker==victim)return damage;
  var player=AbilityController.For(victim);if(player&&player.Prototype)player.Player.LastCombatAt=NetworkTime.time;
  var native=attacker?attacker.GetComponent<MultiFPS.Gameplay.RiftEnemies.RiftEnemyAgent>():null;
  if(player&&player.Prototype&&!player.InCrilium&&!victim.TrainingTarget&&native&&native.Definition)damage=MultiFPS.Gameplay.RiftEnemies.RiftEnemyLevelTuning.Load.DamageAgainstPlayer(damage,native.Level,player.Level);
  if(player&&player.Prototype)damage=Mathf.Max(1,Mathf.RoundToInt(damage*(1-player.Specialization.Armor)*(player.BuffUntil>NetworkTime.time?1-player.BuffReduction:1)));
  if(player){damage=Mathf.Max(1,Mathf.RoundToInt(damage*(1-player.ExtraArmor)*(player.DefensiveUntil>NetworkTime.time?.5f:1)));}
  if(victim)damage=victim.ServerAbsorbRadiant(damage);
  if(player&&damage>0)player.ReportIncomingThreat(attacker);Damaged?.Invoke(victim,attacker,damage);return damage;
 }
 static AbilityController ForDealer(Health health)=>health?AbilityController.For(health):null;
 public static void Feedback(Health attacker,Health victim,int amount,bool critical){var dealer=ForDealer(attacker);if(dealer&&victim&&attacker!=victim&&attacker.Team!=victim.Team)dealer.ReportDamage(victim.transform.position+Vector3.up*1.8f,amount,critical);}
 public static bool Hit(Health attacker,Health victim,int damage){
  if(!NetworkServer.active||!attacker||!victim||attacker==victim||victim.CurrentHealth<=0||attacker.Team==victim.Team)return false;
  if(GameManager.Gamemode&&GameManager.Gamemode.BlockDamage(victim,attacker))return false;
  victim.Server_ChangeHealthStateRaw(Mathf.Max(1,damage),0,AttackType.hitscan,attacker,0);return true;
 }
 public static bool Visible(CharacterInstance actor,Health target){Vector3 start=actor.transform.position+Vector3.up*1.2f;Vector3 delta=target.transform.position+Vector3.up-start;return !actor.gameObject.scene.GetPhysicsScene().Raycast(start,delta.normalized,out _,delta.magnitude,GameManager.environmentLayer,QueryTriggerInteraction.Ignore);}
}}
