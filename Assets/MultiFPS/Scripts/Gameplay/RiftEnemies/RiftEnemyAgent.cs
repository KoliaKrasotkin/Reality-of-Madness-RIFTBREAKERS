using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using MultiFPS.Gameplay.RiftCombat;
namespace MultiFPS.Gameplay.RiftEnemies {
// Standalone server NPC: no PlayerInstance, CharacterInstance, BotAI or template rig.
[RequireComponent(typeof(NetworkIdentity),typeof(Health),typeof(NavMeshAgent))]
public sealed partial class RiftEnemyAgent:NetworkBehaviour {
 public RiftEnemyDefinition Definition;public Animator Animator;public CapsuleCollider Body;
 [SyncVar(hook=nameof(LifeChanged))]public int Life;
 [SyncVar]public int MaximumLife;
 [SyncVar(hook=nameof(DeathChanged))]public bool Dead;
 [SyncVar(hook=nameof(AnimationChanged))]int animationEvent;
 public static readonly List<RiftEnemyAgent> All=new List<RiftEnemyAgent>();
 public event Action<RiftEnemyAgent,Health> ServerKilled;
 public Health Health {get;private set;}public Health Target {get;private set;}
 public Vector3 Home {get;private set;}public bool Attacking=>attacking;public double StaggerUntil=>staggerUntil;
 NavMeshAgent navigation;readonly Dictionary<Health,float> threat=new Dictionary<Health,float>();
 double nextPlan,nextPath,readyAt,hitAt,staggerUntil,forcedUntil;Health forced,castTarget;bool attacking,returning;Vector3 previous;int sequence;
 public override void OnStartServer(){
  Home=transform.position;MaximumLife=Definition.Health;Life=MaximumLife;Health.MaxHealth=MaximumLife;Health.CurrentHealth=Life;Health.CharacterName=Definition.DisplayName;Health.Team=Definition.Team;
  navigation.enabled=true;navigation.speed=Definition.MoveSpeed;navigation.stoppingDistance=.35f;navigation.avoidancePriority=20+(int)(netId%60);
  nextPlan=NetworkTime.time+(netId%7)*.04;Health.Server_OnHealthDepleted+=Killed;
 }
 public override void OnStopServer(){Health.Server_OnHealthDepleted-=Killed;}
 void Awake(){Health=GetComponent<Health>();navigation=GetComponent<NavMeshAgent>();navigation.enabled=false;previous=transform.position;}
 void OnEnable(){All.Add(this);CombatController.Damaged+=Damaged;}
 void OnDisable(){All.Remove(this);CombatController.Damaged-=Damaged;}
 public override void OnStartClient(){LifeChanged(Life,Life);if(Dead)Play("Death");}
 void LifeChanged(int old,int value){if(Health){Health.CurrentHealth=value;Health.MaxHealth=MaximumLife;Health.Team=Definition.Team;if(Definition)Health.CharacterName=Definition.DisplayName;}}
 void DeathChanged(bool old,bool dead){if(dead){if(Body)Body.enabled=false;Play("Death");}}
 void AnimationChanged(int old,int value){int type=value%10;Play(type==5?"Skill":type==1?"Hit":type==2?"Attack01":type==3?"Attack02":"Attack03");}
 void Play(string state){if(Animator&&Animator.isActiveAndEnabled)Animator.CrossFadeInFixedTime(state,.08f);}
 void Emit(int type){animationEvent=++sequence*10+type;}
 void Update(){
  if(isServer)Tick();
  if(isClient&&Animator&&!Dead){float speed=(transform.position-previous).magnitude/Mathf.Max(.001f,Time.deltaTime);Animator.SetFloat("Speed",speed,.12f,Time.deltaTime);}previous=transform.position;
 }
 bool Valid(Health h)=>h&&h.CurrentHealth>0&&h.Team!=Health.Team&&RiftClassCombat.CanTarget(h,transform.position)&&(!GameManager.Gamemode||!GameManager.Gamemode.IsProtectedPosition(h.transform.position))&&(h.transform.position-Home).sqrMagnitude<Definition.Leash*Definition.Leash;
 bool Visible(Health target){var origin=transform.position+Vector3.up*.8f;var delta=target.transform.position+Vector3.up-origin;return !Physics.Raycast(origin,delta.normalized,delta.magnitude,GameManager.environmentLayer,QueryTriggerInteraction.Ignore);}
 void Damaged(Health victim,Health attacker,int amount){if(!isServer||Dead||victim!=Health||!attacker||attacker.Team==Health.Team)return;var owner=AbilityController.For(attacker);AddThreat(attacker,amount*(owner&&owner.Prototype&&owner.Specialization?owner.Specialization.ThreatMultiplier:1));if(Definition.Disposition==RiftEnemyDisposition.Timid){fleeFrom=attacker.transform.position;fleeUntil=NetworkTime.time+6;}if(!attacking&&NetworkTime.time>=staggerUntil)Emit(1);}
 [Server]public void AddThreat(Health source,float amount){if(source&&amount>0){threat.TryGetValue(source,out float value);threat[source]=value+amount;}}
 [Server]public void Taunt(Health source,float seconds,bool force){AddThreat(source,100);if(force){forced=source;forcedUntil=NetworkTime.time+seconds;}}
 [Server]public void Stagger(float seconds){if(Dead||seconds<=0)return;staggerUntil=NetworkTime.time+Mathf.Min(2,seconds);attacking=false;skillCasting=false;SkillEnds=0;Stop();Emit(1);}
 void Stop(){if(navigation.enabled&&navigation.isOnNavMesh)navigation.isStopped=true;}
 void Tick(){
  if(Dead||!Definition)return;Life=Health.CurrentHealth;MaximumLife=Health.MaxHealth;
  if(!navigation.enabled||!navigation.isOnNavMesh)return;double now=NetworkTime.time;
  if(now<staggerUntil){Stop();return;}
  if((transform.position-Home).sqrMagnitude>Definition.Leash*Definition.Leash){Target=null;threat.Clear();returning=true;attacking=false;}
  if(returning){if((transform.position-Home).sqrMagnitude<2){returning=false;Stop();Health.ServerHeal(MaximumLife,Health.DNID);Life=Health.CurrentHealth;}else Move(Home,now);return;}
  if(skillCasting){TickSkill(now);return;}
  if(attacking){Stop();if(!Valid(castTarget)){attacking=false;return;}if(now>=hitAt){attacking=false;if((transform.position-castTarget.transform.position).sqrMagnitude<=Mathf.Pow(Definition.AttackRange+.35f,2)&&Visible(castTarget))BasicHit(castTarget);}return;}
  if(now>=nextPlan){nextPlan=now+.3;Target=ChooseTarget();}
  if(!Valid(Target)){Target=null;if(Definition.IsWildlife){Wander(now);return;}if((transform.position-Home).sqrMagnitude>4){returning=true;Move(Home,now);}else Stop();return;}
  Vector3 delta=Target.transform.position-transform.position;delta.y=0;
  if(ServerTrySkill(Target))return;
  if(Definition.Ranged&&delta.sqrMagnitude<16){Move(transform.position-delta.normalized*4,now);return;}
  if(delta.sqrMagnitude>Definition.AttackRange*Definition.AttackRange){float angle=netId*137.5f;var offset=Quaternion.Euler(0,angle,0)*Vector3.forward*.65f;Move(Target.transform.position+offset,now);return;}
  Stop();if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),540*Time.deltaTime);
  if(now<readyAt||!Visible(Target))return;int attackers=0;foreach(var enemy in All)if(enemy&&enemy!=this&&enemy.attacking&&enemy.castTarget==Target)attackers++;
  if(attackers>=2)return;attacking=true;castTarget=Target;hitAt=now+Definition.Windup;readyAt=hitAt+Definition.Cooldown;Emit(2+(sequence%3));
 }
 Health ChooseTarget(){if(Definition.Disposition==RiftEnemyDisposition.Timid)return null;if(forcedUntil>NetworkTime.time&&Valid(forced))return forced;Health best=null;float score=float.NegativeInfinity;foreach(var player in GameManager.Players){if(!player||player.BOT||!player.MyCharacter)continue;var h=player.MyCharacter.Health;if(!Valid(h))continue;float distance=(h.transform.position-transform.position).magnitude;threat.TryGetValue(h,out float value);if(value<=0&&(Definition.Disposition==RiftEnemyDisposition.Defensive||distance>Definition.Detection))continue;float candidate=value+10/(1+distance);if(candidate>score){score=candidate;best=h;}}return best;}
 void Move(Vector3 point,double now){navigation.isStopped=false;if(now<nextPath)return;nextPath=now+.4+(netId%5)*.02;if(NavMesh.SamplePosition(point,out var sample,3,NavMesh.AllAreas))navigation.SetDestination(sample.position);}
 void Killed(CharacterPart part,AttackType type,Health killer,int force){if(Dead)return;Dead=true;Life=0;attacking=false;skillCasting=false;SkillEnds=0;Target=null;Stop();navigation.enabled=false;if(Body)Body.enabled=false;ServerKilled?.Invoke(this,killer);Invoke(nameof(RemoveCorpse),Definition.CorpseSeconds);}
 void RemoveCorpse(){if(isServer)NetworkServer.Destroy(gameObject);}
 [Server]public void Despawn(){ServerKilled=null;NetworkServer.Destroy(gameObject);}
}
}
