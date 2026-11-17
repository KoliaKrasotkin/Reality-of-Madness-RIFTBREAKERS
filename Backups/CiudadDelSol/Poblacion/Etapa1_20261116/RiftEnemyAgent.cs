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
 [SyncVar]public int Level=1;[SyncVar]public string QuestEncounter="";
 int requestedLevel=1;float requestedLeash;
 public int LevelHealth=>RiftEnemyLevelTuning.Load.Health(Definition.Health,Level);
 public int LevelDamage=>RiftEnemyLevelTuning.Load.Damage(Definition.Damage,Level);
 public float EffectiveLeash=>requestedLeash>0?Mathf.Clamp(requestedLeash,4,120):Definition.Leash;
 public void ConfigureSpawn(int level,float leash=0,string encounter="",bool cityRaidPursuit=false){ServerAttackTargetGate=null;ResetCityRaidPursuit();requestedCityRaidPursuit=cityRaidPursuit;QuestEncounter=encounter??"";requestedLevel=RiftEnemyLevelTuning.Load.ClampLevel(level);requestedLeash=leash;}
 [SyncVar(hook=nameof(LifeChanged))]public int Life;
 [SyncVar]public int MaximumLife;
 [SyncVar(hook=nameof(DeathChanged))]public bool Dead;
 [SyncVar(hook=nameof(AnimationChanged))]int animationEvent;
 public static readonly List<RiftEnemyAgent> All=new List<RiftEnemyAgent>();
 public event Action<RiftEnemyAgent,Health> ServerKilled;
  public Health[] LastKillParticipants{get;private set;}=Array.Empty<Health>();
 public Action<RiftEnemyAgent> ReleaseToPool;
 // Optional server-instance validation. Null preserves the native behavior in every existing mode.
 // A trial may reject an attack without changing target acquisition or pursuit.
 public Func<Health,bool> ServerAttackTargetGate {get;set;}
 bool AttackAllowed(Health target)=>ServerAttackTargetGate==null||ServerAttackTargetGate(target);
 public Health Health {get;private set;}public Health Target {get;private set;}
 public Vector3 Home {get;private set;}public bool Attacking=>attacking;public double StaggerUntil=>staggerUntil;
 NavMeshAgent navigation;readonly Dictionary<Health,float> threat=new Dictionary<Health,float>();double lastTargetSwitch;
 double nextPlan,nextPath,readyAt,hitAt,staggerUntil,forcedUntil;Health forced,castTarget;bool attacking,returning;Vector3 previous;int sequence;
 public override void OnStartServer(){ServerAttackTargetGate=null;
  CancelInvoke();StopAllCoroutines();LastKillParticipants=Array.Empty<Health>();Dead=false;Body.enabled=true;Health.ResetNativeSpawn();ResetAggroState();attacking=returning=skillCasting=false;Target=forced=castTarget=skillTarget=null;SkillEnds=SkillReady=readyAt=hitAt=staggerUntil=forcedUntil=nextPath=nextPlan=nextWander=fleeUntil=wildSense=0;previous=transform.position;if(Animator){Animator.Rebind();Animator.Play("Idle",0,0);}
  Home=transform.position;Level=RiftEnemyLevelTuning.Load.ClampLevel(requestedLevel);MaximumLife=LevelHealth;Life=MaximumLife;Health.MaxHealth=MaximumLife;Health.CurrentHealth=Life;Health.CharacterName=Definition.DisplayName;Health.Team=Definition.Team;
  navigation.enabled=true;navigation.autoRepath=false;navigation.speed=Definition.MoveSpeed;navigation.stoppingDistance=.35f;navigation.avoidancePriority=20+(int)(netId%60);
  StartCityRaidPursuit();
  nextPlan=NetworkTime.time+(netId%7)*.04;Health.Server_OnHealthDepleted+=Killed;
 }
 public override void OnStopServer(){ServerAttackTargetGate=null;Health.Server_OnHealthDepleted-=Killed;ResetAggroState();ResetCityRaidPursuit();}
 void Awake(){Health=GetComponent<Health>();navigation=GetComponent<NavMeshAgent>();navigation.enabled=false;previous=transform.position;}
 void OnEnable(){All.Add(this);CombatController.Damaged+=Damaged;}
 void OnDisable(){ServerAttackTargetGate=null;All.Remove(this);CombatController.Damaged-=Damaged;ResetAggroState();ResetCityRaidPursuit();}
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
 bool Valid(Health h)=>h&&RiftThreatRules.ActivePlayer(h)&&h.CurrentHealth>0&&h.Team!=Health.Team&&RiftClassCombat.CanTarget(h,transform.position)&&(!GameManager.Gamemode||!GameManager.Gamemode.IsProtectedPosition(h.transform.position))&&WithinHomeLeash(h.transform.position)&&(!CityRaidPursuitActive||h.gameObject.scene==gameObject.scene);
 bool Visible(Health target){var origin=transform.position+Vector3.up*.8f;var delta=target.transform.position+Vector3.up-origin;return !Physics.Raycast(origin,delta.normalized,delta.magnitude,GameManager.environmentLayer,QueryTriggerInteraction.Ignore);}
 void Damaged(Health victim,Health attacker,int amount){if(!isServer||Dead||victim!=Health||!attacker||attacker.Team==Health.Team)return;var owner=AbilityController.For(attacker);AddThreat(attacker,amount*(owner&&owner.Prototype&&owner.Specialization?owner.Specialization.ThreatMultiplier:1));if(Definition.Disposition==RiftEnemyDisposition.Timid){fleeFrom=attacker.transform.position;fleeUntil=NetworkTime.time+6;}if(!attacking&&NetworkTime.time>=staggerUntil)Emit(1);}
 [Server]public void AddThreat(Health source,float amount){if(Tracked(source)&&amount>0&&!float.IsNaN(amount)&&!float.IsInfinity(amount)){threat.TryGetValue(source,out float value);threat[source]=RiftThreatRules.Accumulate(value,amount);}}
 [Server]public void Taunt(Health source,float seconds,bool force){if(Definition.Disposition==RiftEnemyDisposition.Timid||!Valid(source)||!Visible(source))return;AddThreat(source,100);if(force){forced=source;forcedUntil=NetworkTime.time+seconds;}}
 [Server]public void Stagger(float seconds){if(Dead||seconds<=0)return;staggerUntil=NetworkTime.time+Mathf.Min(2,seconds);attacking=false;skillCasting=false;SkillEnds=0;Stop();Emit(1);}
 void Stop(){if(navigation.enabled&&navigation.isOnNavMesh)navigation.isStopped=true;}
 void Tick(){
  if(Dead||!Definition)return;PruneThreat();Life=Health.CurrentHealth;MaximumLife=Health.MaxHealth;
  if(!navigation.enabled||!navigation.isOnNavMesh)return;double now=NetworkTime.time;
  if(CityRaidPursuitActive&&navigation.hasPath&&!navigation.pathPending&&navigation.pathStatus!=NavMeshPathStatus.PathComplete){Stop();navigation.ResetPath();}
  if(navigation.hasPath&&NearProtected(transform.position+navigation.velocity*.25f)){Stop();ResetAggroState();returning=true;attacking=skillCasting=false;SkillEnds=0;nextPath=0;}
  if(now<staggerUntil){Stop();return;}
  if(!CityRaidPursuitActive&&(transform.position-Home).sqrMagnitude>EffectiveLeash*EffectiveLeash){ResetAggroState();returning=true;attacking=false;}
  if(returning){if((transform.position-Home).sqrMagnitude<2){returning=false;ResetAggroState();Stop();Health.ServerHeal(MaximumLife,Health.DNID);Life=Health.CurrentHealth;}else Move(Home,now);return;}
  if(skillCasting){TickSkill(now);return;}
  if(attacking){Stop();if(!Valid(castTarget)){attacking=false;return;}if(now>=hitAt){attacking=false;if((transform.position-castTarget.transform.position).sqrMagnitude<=Mathf.Pow(Definition.AttackRange+.35f,2)&&Visible(castTarget))BasicHit(castTarget);}return;}
  if(now>=nextPlan){nextPlan=now+.3;Target=ChooseTarget();}
  if(!Valid(Target)){Target=null;if(Definition.IsWildlife){Wander(now);return;}if((transform.position-Home).sqrMagnitude>4){returning=true;Move(Home,now);}else Stop();return;}
  Vector3 delta=Target.transform.position-transform.position;delta.y=0;
  if(ServerTrySkill(Target))return;
  if(Definition.Ranged&&delta.sqrMagnitude<16){Move(transform.position-delta.normalized*4,now);return;}
  if(delta.sqrMagnitude>Definition.AttackRange*Definition.AttackRange||(CityRaidPursuitActive&&(Mathf.Abs(Target.transform.position.y-transform.position.y)>.36f||!Visible(Target)))){float angle=netId*137.5f;var offset=Quaternion.Euler(0,angle,0)*Vector3.forward*.65f;Move(Target.transform.position+offset,now,true);return;}
  Stop();if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),540*Time.deltaTime);
  if(now<readyAt||!Visible(Target)||!AttackAllowed(Target))return;int attackers=0;foreach(var enemy in All)if(enemy&&enemy!=this&&enemy.attacking&&enemy.castTarget==Target)attackers++;
  if(attackers>=2)return;attacking=true;castTarget=Target;hitAt=now+Definition.Windup;readyAt=hitAt+Definition.Cooldown;Emit(2+(sequence%3));
 }
 bool Tracked(Health h)=>RiftThreatRules.ActivePlayer(h)&&h.Team!=Health.Team&&h.gameObject.scene==gameObject.scene&&(!GameManager.Gamemode||!GameManager.Gamemode.IsProtectedPosition(h.transform.position))&&WithinHomeLeash(h.transform.position);
 void ResetAggroState(){threat.Clear();Target=forced=castTarget=skillTarget=null;forcedUntil=lastTargetSwitch=0;}
 void PruneThreat(){foreach(var source in new List<Health>(threat.Keys))if(!Tracked(source))threat.Remove(source);if(forced&&!Tracked(forced)){forced=null;forcedUntil=0;}if(Target&&!Tracked(Target)){Target=null;lastTargetSwitch=0;nextPlan=0;attacking=skillCasting=false;SkillEnds=0;}}
 public bool CanReceiveHealingThreat(Health receiver,Health healer)=>isServer&&isActiveAndEnabled&&!Dead&&Definition&&Definition.Disposition!=RiftEnemyDisposition.Timid&&Tracked(receiver)&&Tracked(healer)&&(Target==receiver||threat.ContainsKey(receiver));
 public float DetectionFor(Health player)=>RiftAggroTuning.Load.Detection(Definition.Detection,Level,RiftThreatRules.PlayerLevel(player));
 Health ChooseTarget(){PruneThreat();if(Definition.Disposition==RiftEnemyDisposition.Timid)return null;if(forcedUntil>NetworkTime.time&&Valid(forced)){if(Target!=forced)lastTargetSwitch=NetworkTime.time;return forced;}Health best=null;float score=float.NegativeInfinity,currentScore=float.NegativeInfinity;foreach(var player in GameManager.Players){if(!player||player.BOT||!player.MyCharacter)continue;var h=player.MyCharacter.Health;if(!Valid(h))continue;float distance=(h.transform.position-transform.position).magnitude;threat.TryGetValue(h,out float value);bool known=h==Target||value>0;if(!known&&(Definition.Disposition==RiftEnemyDisposition.Defensive||distance>DetectionFor(h)))continue;if(h!=Target&&!Visible(h))continue;float candidate=value+10/(1+distance);if(h==Target)currentScore=candidate;if(candidate>score){score=candidate;best=h;}}if(Target&&Valid(Target)&&best&&best!=Target&&!RiftAggroTuning.Load.ShouldSwitch(currentScore,score,lastTargetSwitch,NetworkTime.time))return Target;if(best!=Target)lastTargetSwitch=NetworkTime.time;return best;}
 NavMeshPath boundaryPath;
 bool NearProtected(Vector3 p){var g=GameManager.Gamemode;if(!g)return false;float margin=navigation.radius+1.5f;return g.IsProtectedPosition(p)||g.IsProtectedPosition(p+Vector3.right*margin)||g.IsProtectedPosition(p-Vector3.right*margin)||g.IsProtectedPosition(p+Vector3.forward*margin)||g.IsProtectedPosition(p-Vector3.forward*margin);}
 public bool PathAvoidsProtected(Vector3 destination){if(!navigation.enabled||!navigation.isOnNavMesh||NearProtected(destination))return false;if(boundaryPath==null)boundaryPath=new NavMeshPath();if(!navigation.CalculatePath(destination,boundaryPath)||boundaryPath.status!=NavMeshPathStatus.PathComplete)return false;var corners=boundaryPath.corners;for(int i=1;i<corners.Length;i++){int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(corners[i-1],corners[i])));for(int n=1;n<=steps;n++)if(NearProtected(Vector3.Lerp(corners[i-1],corners[i],(float)n/steps)))return false;}return true;}
 void Move(Vector3 point,double now,bool chasing=false){if(now<nextPath)return;if(CityRaidPursuitActive){MoveCityRaid(point,now,chasing);return;}nextPath=now+.4+(netId%5)*.02;if(NavMesh.SamplePosition(point,out var sample,3,NavMesh.AllAreas)&&PathAvoidsProtected(sample.position)){navigation.isStopped=false;navigation.SetPath(boundaryPath);}else{Stop();ResetAggroState();returning=true;attacking=false;skillCasting=false;SkillEnds=0;}}
 void Killed(CharacterPart part,AttackType type,Health killer,int force){if(Dead)return;LastKillParticipants=new List<Health>(threat.Keys).ToArray();ResetAggroState();Dead=true;Life=0;attacking=false;skillCasting=false;SkillEnds=0;Target=null;Stop();navigation.enabled=false;if(Body)Body.enabled=false;ServerKilled?.Invoke(this,killer);Invoke(nameof(RemoveCorpse),Definition.CorpseSeconds);}
 void RemoveCorpse(){if(isServer)Despawn();}
 [Server]public void Despawn(){ServerKilled=null;CancelInvoke();StopAllCoroutines();if(ReleaseToPool!=null)ReleaseToPool(this);else NetworkServer.Destroy(gameObject);}
}
}
