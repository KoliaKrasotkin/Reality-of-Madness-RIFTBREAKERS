using System;
using Mirror;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public sealed partial class AbilityController {
 public static event Action<Vector3,int,bool> DamageNumber;
 public AutoAttackProfile AutoProfile=>IsPaladin&&PaladinPrototypeSettings.Load?PaladinPrototypeSettings.Load.AutoAttack:RiftCombatTuning.Load.Find(Prototype?"warrior":Player?((RiftClassId)Player.ClassId).ToString().ToLowerInvariant():"warrior");
 [SyncVar]public double AutoReady;uint autoVictim;double autoHitAt;bool autoPending;
 [Command]public void CmdAutoAttack(){ServerAutoAttack();}
 public void RequestAutoAttack(){if(isLocalPlayer&&NetworkTime.time>=AutoReady)CmdAutoAttack();}
 bool AutoTargetValid(Health target){if(!Actor||Actor.Health.CurrentHealth<=0||!target||target==Actor.Health||target.CurrentHealth<=0||target.Team==Actor.Health.Team||!GameManager.Gamemode.CanUseClassAbilities(Actor.transform.position))return false;var delta=target.transform.position-Actor.transform.position;delta.y=0;return delta.magnitude<=AutoProfile.Range&&Vector3.Angle(Quaternion.Euler(0,Actor.Input.LookY,0)*Vector3.forward,delta)<=AutoProfile.Arc*.5f&&CombatController.Visible(Actor,target);}
 [Server]public bool ServerAutoAttack(){if(Player.BOT||NetworkTime.time<AutoReady||Casting>=0||autoPending||!AutoTargetValid(CombatTarget))return false;if(QueueWeaponDraw(0))return true;autoWeaponRoll=Prototype?RollWeaponDamage():AutoProfile.Damage;AutoReady=NetworkTime.time+Mathf.Max(.2f,AutoInterval);autoWearToken=BeginWearAction();autoVictim=TargetNetId;autoHitAt=NetworkTime.time+Mathf.Clamp(AutoProfile.Windup,0,AutoInterval);autoPending=true;Player.LastCombatAt=NetworkTime.time;RpcAutoSwing();return true;}
 void TickAutoAttack(){if(!isServer||!autoPending||NetworkTime.time<autoHitAt)return;autoPending=false;if(autoVictim!=TargetNetId||!AutoTargetValid(CombatTarget))return;float multiplier=Prototype&&Specialization?Specialization.DamageMultiplier:1;int damage=Mathf.RoundToInt(autoWeaponRoll*multiplier);if(CombatController.Hit(Actor.Health,CombatTarget,damage)){ConnectWearAction(autoWearToken);if(Prototype&&Class)Resource=Mathf.Min(Class.MaximumResource,Resource+Gain(AutoProfile.ResourceGain));}}
 [ClientRpc]void RpcAutoSwing(){var basic=Class?Class.BasicAttack:null;if(!Actor||!basic)return;var native=Actor.GetComponent<WarriorAvatar>();if(native)native.PresentAutoAttack(basic);else{var pose=Actor.CharacterParent?Actor.CharacterParent.GetComponentInChildren<IDSGameplayPose>(true):null;if(pose&&basic.Animation){var motion=pose.GetComponent<CombatMotion>();if(!motion)motion=pose.gameObject.AddComponent<CombatMotion>();motion.Play(pose.Target,basic.Animation,basic.CastDuration);}}}
 [Server]public void ReportDamage(Vector3 position,int amount,bool critical){if(amount>0&&connectionToClient!=null)TargetDamageNumber(connectionToClient,position,amount,critical);}
 [TargetRpc]void TargetDamageNumber(NetworkConnectionToClient owner,Vector3 position,int amount,bool critical){DamageNumber?.Invoke(position,amount,critical);}
}}
