using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
namespace MultiFPS.Gameplay.RiftCombat {
public sealed class RiftEnemyCombat:NetworkBehaviour {
 public EnemyArchetypeDefinition[] Archetypes;public WeaponDefinition MeleeWeapon;
 [SyncVar]public bool ActiveCombat;[SyncVar]public int ArchetypeIndex;[SyncVar]public double CastEnds;[SyncVar]public uint TargetId;
 public static readonly List<RiftEnemyCombat> Active=new List<RiftEnemyCombat>();
 public EnemyArchetypeDefinition Definition=>Archetypes!=null&&Archetypes.Length>0?Archetypes[Mathf.Clamp(ArchetypeIndex,0,Archetypes.Length-1)]:null;
 public CharacterInstance Actor {get;private set;}
 public Health[] QuestParticipants=>new List<Health>(threat.Keys).ToArray();
  public Health Target;readonly Dictionary<Health,float> threat=new Dictionary<Health,float>();NavMeshPath path;Vector3 destination,aimPoint;Health castTarget,forced;double forcedUntil,staggerUntil,nextAttack,nextPlan;bool firing;double lastTargetSwitch;int corner;double nextPath;Vector3[] corners=new Vector3[0];
 void Awake(){Actor=GetComponent<CharacterInstance>();path=new NavMeshPath();Active.Add(this);}
 void OnEnable(){CombatController.Damaged+=OnDamaged;}
 void OnDisable(){CombatController.Damaged-=OnDamaged;ResetAggro();}
 void OnDestroy(){Active.Remove(this);}
 [Server]public void Configure(int index){if(Actor.Health.TrainingTarget)return;ArchetypeIndex=Mathf.Clamp(index,0,Archetypes.Length-1);ActiveCombat=true;var old=GetComponent<BotAI>();if(old)old.enabled=false;}
 void OnDamaged(Health victim,Health attacker,int damage){if(victim==Actor.Health&&attacker&&attacker.Team!=victim.Team){var owner=AbilityController.For(attacker);AddThreat(attacker,damage*(owner&&owner.Prototype?owner.Specialization.ThreatMultiplier:1));}}
 [Server]public void AddThreat(Health source,float amount){if(!Tracked(source)||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return;threat.TryGetValue(source,out float old);threat[source]=RiftThreatRules.Accumulate(old,amount);}
 [Server]public void Taunt(Health source,float seconds,bool force){if(!ActiveCombat||!Valid(source)||!CombatController.Visible(Actor,source))return;AddThreat(source,force?100:20);if(force){forced=source;forcedUntil=NetworkTime.time+seconds;}}
 [Server]public void Stagger(float seconds){if(seconds<=0)return;staggerUntil=NetworkTime.time+Mathf.Min(seconds,2);firing=false;CastEnds=0;nextAttack=NetworkTime.time+1;}
 bool Valid(Health h){if(!h||!RiftThreatRules.ActivePlayer(h)||h.CurrentHealth<=0||h.Team==Actor.Health.Team||!RiftClassCombat.CanTarget(h,transform.position)||GameManager.Gamemode.IsProtectedPosition(h.transform.position))return false;var territory=GetComponent<RiftEnemyTerritory>();return !territory||territory.CanTarget(h);}
 void FixedUpdate(){
  if(!isServer||!ActiveCombat||!Definition)return;
  var legacy=GetComponent<BotAI>();if(legacy)legacy.enabled=false;
  Actor.Input.ActionCodes=0;Actor.Input.Movement=Vector2.zero;
  if(Actor.Health.CurrentHealth<=0){ResetAggro();firing=false;CastEnds=0;return;}PruneThreat();
  double now=NetworkTime.time;if(now<staggerUntil)return;
  var territory=GetComponent<RiftEnemyTerritory>();
  if(territory&&Vector3.Distance(transform.position,territory.Home)>territory.Leash){ResetAggro();Move(territory.Home,now);return;}
  if(firing){if(!Valid(castTarget)){firing=false;CastEnds=0;return;}if(now>=CastEnds){firing=false;CastEnds=0;FinishAttack();}return;}
  if(now>=nextPlan){Target=Select();nextPlan=now+.35;}
  if(!Valid(Target)){TargetId=0;var home=GetComponent<RiftEnemyTerritory>();if(home&&Vector3.Distance(transform.position,home.Home)>1)Move(home.Home,now);else ResetAggro();return;}TargetId=Target.netId;
  var delta=Target.transform.position-transform.position;Actor.Input.LookY=Quaternion.LookRotation(new Vector3(delta.x,0,delta.z)).eulerAngles.y;Actor.Input.LookX=0;
  float distance=delta.magnitude;
  bool inRange=distance<Definition.Range&&CombatController.Visible(Actor,Target);
  if(inRange&&now>=nextAttack){int attacking=0;foreach(var other in Active)if(other&&other!=this&&other.firing&&other.castTarget==Target)attacking++;if(attacking<2){firing=true;castTarget=Target;aimPoint=Target.transform.position;CastEnds=now+Definition.Windup;nextAttack=CastEnds+Definition.Cooldown;RpcCast(aimPoint);}}
  if(!inRange){Vector3 offset=Quaternion.Euler(0,(netId*137)%360,0)*Vector3.forward*(Definition.Caster?Definition.PreferredDistance:1.5f);Move(Target.transform.position+offset,now);}
  else if(Definition.Caster&&distance<5){Move(transform.position-delta.normalized*4,now);}
 }
 bool Tracked(Health h)=>RiftThreatRules.ActivePlayer(h)&&h.Team!=Actor.Health.Team&&h.gameObject.scene==gameObject.scene&&(!GameManager.Gamemode||!GameManager.Gamemode.IsProtectedPosition(h.transform.position))&&(!GetComponent<RiftEnemyTerritory>()||GetComponent<RiftEnemyTerritory>().CanTarget(h));
 void ResetAggro(){threat.Clear();Target=forced=castTarget=null;TargetId=0;forcedUntil=lastTargetSwitch=0;}
 void PruneThreat(){foreach(var source in new List<Health>(threat.Keys))if(!Tracked(source))threat.Remove(source);if(forced&&!Tracked(forced)){forced=null;forcedUntil=0;}if(Target&&!Tracked(Target)){Target=null;TargetId=0;nextPlan=0;firing=false;CastEnds=0;}}
 public bool CanReceiveHealingThreat(Health receiver,Health healer)=>isServer&&isActiveAndEnabled&&ActiveCombat&&Actor&&Actor.Health.CurrentHealth>0&&Tracked(receiver)&&Tracked(healer)&&(Target==receiver||threat.ContainsKey(receiver));
 Health Select(){PruneThreat();Health best=null;float score=float.NegativeInfinity,currentScore=float.NegativeInfinity;if(forcedUntil>NetworkTime.time&&Valid(forced)){if(Target!=forced)lastTargetSwitch=NetworkTime.time;return forced;}foreach(var p in GameManager.Players){if(!p||p.BOT||!p.MyCharacter)continue;var h=p.MyCharacter.Health;if(!Valid(h))continue;float distance=Vector3.Distance(transform.position,h.transform.position);threat.TryGetValue(h,out float value);bool known=h==Target||value>0;float radius=RiftAggroTuning.Load.Detection(Definition.Detection,1,RiftThreatRules.PlayerLevel(h));if(!known&&distance>radius)continue;if(h!=Target&&!CombatController.Visible(Actor,h))continue;float candidate=value+10/(1+distance);if(h==Target)currentScore=candidate;if(candidate>score){score=candidate;best=h;}}if(Target&&Valid(Target)&&best&&best!=Target&&!RiftAggroTuning.Load.ShouldSwitch(currentScore,score,lastTargetSwitch,NetworkTime.time))return Target;if(best!=Target)lastTargetSwitch=NetworkTime.time;return best;}
 void Move(Vector3 point,double now){
  if(now>=nextPath&&((point-destination).sqrMagnitude>4||corners.Length==0)){nextPath=now+.5;if(!NavMesh.SamplePosition(point,out var sample,3,NavMesh.AllAreas))return;NavMesh.CalculatePath(transform.position,sample.position,NavMesh.AllAreas,path);destination=point;corner=1;corners=path.corners;corners=path.corners;}
  if(corner>=corners.Length)return;Vector3 delta=corners[corner]-transform.position;delta.y=0;if(delta.magnitude<.6f){corner++;return;}
  Vector3 direction=delta.normalized;foreach(var other in Active)if(other&&other!=this&&other.ActiveCombat){Vector3 separation=transform.position-other.transform.position;separation.y=0;if(separation.sqrMagnitude>.01f&&separation.sqrMagnitude<2)direction+=separation.normalized*.7f;}
  direction.Normalize();Actor.Input.LookY=Quaternion.LookRotation(direction).eulerAngles.y;Actor.Input.Movement=Vector2.up;var motor=GetComponent<CharacterMotor>();motor.WalkSpeed=Definition.MoveSpeed;motor.RunSpeed=Definition.MoveSpeed;
 }
 void FinishAttack(){if(!Valid(castTarget))return;if(Definition.Caster){var start=transform.position+Vector3.up*1.3f;var end=castTarget.transform.position+Vector3.up;RpcProjectile(start,end);StartCoroutine(Travel(castTarget,start,end));}
  else if(Vector3.Distance(transform.position,castTarget.transform.position)<=Definition.Range+.4f&&CombatController.Visible(Actor,castTarget))CombatController.Hit(Actor.Health,castTarget,Mathf.RoundToInt(Definition.Damage));}
 System.Collections.IEnumerator Travel(Health target,Vector3 start,Vector3 end){yield return new WaitForSeconds(Vector3.Distance(start,end)/14);if(Actor.Health.CurrentHealth<=0||!Valid(target)||Vector3.Distance(target.transform.position+Vector3.up,end)>2||!CombatController.Visible(Actor,target))yield break;CombatController.Hit(Actor.Health,target,Mathf.RoundToInt(Definition.Damage));}
 [ClientRpc]void RpcCast(Vector3 point){var a=Definition?Definition.Attack:null;if(!a)return;if(a.CastVfx)CombatVfxPool.Spawn(a.CastVfx,transform.position,Quaternion.identity,Definition.Windup);if(a.ImpactVfx)CombatVfxPool.Spawn(a.ImpactVfx,Definition.Caster?point:transform.position,Quaternion.identity,Definition.Windup);if(a.Animation){var pose=Actor.CharacterParent?Actor.CharacterParent.GetComponentInChildren<IDSGameplayPose>(true):null;if(pose){var motion=pose.gameObject.GetComponent<CombatMotion>();if(!motion)motion=pose.gameObject.AddComponent<CombatMotion>();motion.Play(pose.Target,a.Animation,Definition.Windup+.2f);}}}
 [ClientRpc]void RpcProjectile(Vector3 start,Vector3 end){var a=Definition?Definition.Attack:null;if(a&&a.ImpactVfx)CombatVfxPool.Spawn(a.ImpactVfx,end,Quaternion.identity,1);CombatProjectile.Show(start,end);}
}}
