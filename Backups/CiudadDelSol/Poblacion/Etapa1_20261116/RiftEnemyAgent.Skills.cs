using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using MultiFPS.Gameplay.RiftCombat;
namespace MultiFPS.Gameplay.RiftEnemies {
public sealed partial class RiftEnemyAgent {
 [SyncVar]public double SkillEnds;[SyncVar]public double SkillReady;
 bool skillCasting;Health skillTarget;Vector3 skillPoint,skillFacing;double nextWander,fleeUntil,wildSense;Vector3 fleeFrom;
 public bool IsSkillCasting=>skillCasting;
 [Server]public bool ServerTrySkill(Health target){
  if(Dead||skillCasting||attacking||NetworkTime.time<staggerUntil||Definition.Skill==RiftEnemySkill.None||NetworkTime.time<SkillReady||!Valid(target)||!AttackAllowed(target)||(target.transform.position-transform.position).sqrMagnitude>Definition.SkillRange*Definition.SkillRange||!Visible(target))return false;
  skillCasting=true;skillTarget=target;skillPoint=target.transform.position;skillFacing=target.transform.position-transform.position;skillFacing.y=0;if(skillFacing.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(skillFacing);skillFacing=transform.forward;SkillEnds=NetworkTime.time+Definition.SkillWindup;SkillReady=SkillEnds+Definition.SkillCooldown;Stop();Emit(5);RpcSkill(Definition.Skill==RiftEnemySkill.Slam?skillPoint:transform.position,Definition.SkillRadius,Definition.SkillWindup,Definition.SkillColor);return true;
 }
 void TickSkill(double now){Stop();if(!Valid(skillTarget)){skillCasting=false;SkillEnds=0;return;}if(now<SkillEnds)return;skillCasting=false;SkillEnds=0;int damage=Mathf.RoundToInt(LevelDamage*Definition.SkillMultiplier);
  if(Definition.Skill==RiftEnemySkill.EmberBolt){Projectile(skillTarget,damage,Definition.SkillColor);return;}
  if(Definition.Skill==RiftEnemySkill.Drain){if((skillTarget.transform.position-transform.position).sqrMagnitude<Definition.SkillRadius*Definition.SkillRadius&&Visible(skillTarget)){int before=skillTarget.CurrentHealth;CombatController.Hit(Health,skillTarget,damage);Health.ServerHeal(Mathf.CeilToInt(Mathf.Max(0,before-skillTarget.CurrentHealth)*.65f),Health.DNID);}return;}
  Vector3 origin=Definition.Skill==RiftEnemySkill.Slam?skillPoint:transform.position;
  foreach(var target in MultiFPS.Gameplay.Health.All){if(!target||target.Team!=0||target.CurrentHealth<=0)continue;var delta=target.transform.position-origin;delta.y=0;if(delta.sqrMagnitude>Definition.SkillRadius*Definition.SkillRadius)continue;if(Definition.Skill==RiftEnemySkill.Cleave&&Vector3.Angle(skillFacing,delta)>70)continue;if(Visible(target))CombatController.Hit(Health,target,damage);}
 }
 void BasicHit(Health target){if(!AttackAllowed(target))return;if(Definition.Ranged)Projectile(target,LevelDamage,Definition.SkillColor);else CombatController.Hit(Health,target,LevelDamage);}
 void Projectile(Health target,int damage,Color color){if(!AttackAllowed(target))return;var start=transform.position+Vector3.up*1.2f;var end=target.transform.position+Vector3.up;float seconds=Mathf.Clamp(Vector3.Distance(start,end)/12,.12f,2);RpcProjectile(start,end,seconds,color);StartCoroutine(Travel(target,end,seconds,damage));}
 IEnumerator Travel(Health target,Vector3 end,float seconds,int damage){yield return new WaitForSeconds(seconds);if(!Dead&&Valid(target)&&AttackAllowed(target)&&(target.transform.position+Vector3.up-end).sqrMagnitude<4&&Visible(target))CombatController.Hit(Health,target,damage);}
 [ClientRpc]void RpcSkill(Vector3 point,float radius,float seconds,Color color)=>RiftEnemySkillFX.Ring(point,radius,seconds,color);
 [ClientRpc]void RpcProjectile(Vector3 from,Vector3 to,float seconds,Color color)=>RiftEnemySkillFX.Projectile(from,to,seconds,color);
 void Wander(double now){
  if(Definition.Disposition==RiftEnemyDisposition.Timid&&now>=wildSense){wildSense=now+.4;foreach(var player in GameManager.Players)if(player&&!player.BOT&&player.MyCharacter&&(player.MyCharacter.transform.position-transform.position).sqrMagnitude<49){fleeFrom=player.MyCharacter.transform.position;fleeUntil=now+4;break;}}
  if(now<nextWander)return;nextWander=now+(now<fleeUntil?1:Random.Range(4f,8f));Vector3 point;
  if(now<fleeUntil){var direction=transform.position-fleeFrom;direction.y=0;point=transform.position+direction.normalized*8;}else{var offset=Random.insideUnitCircle*Definition.WanderRadius;point=Home+new Vector3(offset.x,0,offset.y);}
  if((point-Home).sqrMagnitude>EffectiveLeash*EffectiveLeash)point=Home;
  if(NavMesh.SamplePosition(point,out var sample,5,NavMesh.AllAreas)&&(!GameManager.Gamemode||!GameManager.Gamemode.IsProtectedPosition(sample.position)))Move(sample.position,now);
 }
}
}
