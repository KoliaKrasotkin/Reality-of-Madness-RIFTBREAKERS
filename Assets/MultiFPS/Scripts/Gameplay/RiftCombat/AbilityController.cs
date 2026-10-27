using System.Collections.Generic;

using Mirror;

using UnityEngine;

namespace MultiFPS.Gameplay.RiftCombat {

[DisallowMultipleComponent]

public sealed partial class AbilityController:NetworkBehaviour {

 public const string Preference="ROM.Combat.Nertor";[UnityEngine.Serialization.FormerlySerializedAs("Species")][SerializeField]SpeciesDefinition defaultSpecies;
 public bool IsPaladin=>Player&&Player.PlayerInfo.CharacterSkinID==PaladinPrototypeSettings.Skin;
 public SpeciesDefinition SpeciesForSkin(int skin)=>skin==PaladinPrototypeSettings.Skin&&PaladinPrototypeSettings.Available?PaladinPrototypeSettings.Load.Species:defaultSpecies;
 public SpeciesDefinition Species{get=>SpeciesForSkin(Player?Player.PlayerInfo.CharacterSkinID:1);set=>defaultSpecies=value;}

 [SyncVar]public bool Prototype; [SyncVar]public int SpecIndex;[SyncVar]public float Resource;

 [SyncVar]public double CastEnd,GlobalReady,BuffUntil;[SyncVar]public float BuffReduction,OffensiveBonus;[SyncVar]public int Casting=-1;

 public readonly SyncList<double> Ready=new SyncList<double>();

 public PlayerInstance Player {get;private set;}

 public CharacterInstance Actor=>Player?Player.MyCharacter:null;

 public CombatClassDefinition Class=>Species&&Species.Classes.Length>0?Species.Classes[0]:null;

 public SpecializationDefinition Specialization=>Class&&Class.Specializations.Length>0?Class.Specializations[Mathf.Clamp(SpecIndex,0,Class.Specializations.Length-1)]:null;

 public static readonly List<AbilityController> Players=new List<AbilityController>();

 public static AbilityController For(Health h){foreach(var c in Players)if(c&&c.Actor&&c.Actor.Health==h)return c;return null;}

 int strikeTicks;uint attackTarget;AbilityDefinition pending;double hitAt;bool applied;Vector3 facing;CharacterInstance configured; double nextSelect;int queued=-1;float queueUntil;

 void Awake(){Player=GetComponent<PlayerInstance>();Players.Add(this);}

 void OnDestroy(){Players.Remove(this);DisposeGroundAim();}

 public override void OnStartServer(){for(int i=0;i<8;i++)Ready.Add(0);for(int i=0;i<9;i++)Talents.Add(0);}

 [SyncVar]public uint TargetNetId;

 public Health CombatTarget{get{var registry=isServer?NetworkServer.spawned:NetworkClient.spawned;return TargetNetId!=0&&registry.TryGetValue(TargetNetId,out var identity)?identity.GetComponent<Health>():null;}}

 [Command]public void CmdSetTarget(uint id){ServerSetTarget(id);}

 [Server]public bool ServerSetTarget(uint id){if(id==0){TargetNetId=0;return true;}if(!Actor||!NetworkServer.spawned.TryGetValue(id,out var identity))return false;var health=identity.GetComponent<Health>();if(!health||!health.gameObject.activeInHierarchy||health.gameObject.scene!=Actor.gameObject.scene||health.CurrentHealth<=0||Vector3.Distance(Actor.transform.position,health.transform.position)>100)return false;TargetNetId=id;return true;}

 CharacterInstance localSelected;

 public override void OnStartLocalPlayer(){}

 [Command]public void CmdSelect(int specialization){ServerSelect(specialization);}

 [Server]public bool ServerSelect(int specialization){

  if(!Player.BOT&&specialization<0)return false;

  if(Player.BOT||!Class||specialization < -1||specialization>=Class.Specializations.Length||NetworkTime.time<nextSelect)return false;

  if(Actor&&(Actor.Health.CurrentHealth<=0||!GameManager.Gamemode.IsProtectedPosition(Actor.transform.position)))return false;

  if(specialization>=0&&!Species.AllowsSkin(Player.PlayerInfo.CharacterSkinID))return false;

  nextSelect=NetworkTime.time+.5;Prototype=specialization>=0;SpecIndex=Mathf.Max(0,specialization);Resource=0;Cancel();BuffUntil=0;configured=null;

  if(Actor){if(Prototype)Configure();else Player.ServerConfigureClass();}return true;

 }

 void Configure(){if(!Actor||!Specialization)return;Resource=0;Actor.Health.ServerSetVitals(ProgressionHealth(Specialization.Health),ProgressionHealth(Specialization.Health));configured=Actor;}

 public AbilityDefinition Definition(int slot)=>Class?(slot==0?Class.BasicAttack:slot>0&&slot<=Class.Abilities.Length?Class.Abilities[slot-1]:null):null;

 public int AbilityIndex(string id){if(string.IsNullOrEmpty(id)||!Class)return 0;for(int i=0;i<Class.Abilities.Length;i++)if(Class.Abilities[i]&&Class.Abilities[i].Id==id)return i+1;return 0;}

 public bool IsAbilityLearned(int id){var a=Definition(id);return Prototype&&id>0&&a&&a.Implemented&&Specialization&&a.Allows(Specialization.Id)&&RiftAbilityUnlocks.Available(a.Id,Level);}

 [Command]public void CmdUseAbility(string abilityId){int id=AbilityIndex(abilityId);if(id>0)ServerUse(id);}

 [Command]public void CmdUse(int slot){if(slot>0)ServerUse(slot);}

 [SyncVar]public double PortalReady;

 [Command]public void CmdPortal(int id){ServerPortal(id);}

 [Server]public bool ServerPortal(int id){if(!Actor||Actor.Health.CurrentHealth<=0||NetworkTime.time<PortalReady||Casting>=0)return false;foreach(var portal in RiftPortal.All)if(portal&&portal.Definition&&portal.Definition.Id==id&&portal.Destination&&Vector3.Distance(Actor.transform.position,portal.transform.position)<=portal.Definition.InteractRange){PortalReady=NetworkTime.time+portal.Definition.Cooldown;Actor.ServerTeleport(portal.Destination.position,portal.Destination.eulerAngles.y);return true;}return false;}

 [Server]public bool ServerUse(int slot){

  var ability=Definition(slot);double now=NetworkTime.time;

  if(!IsAbilityLearned(slot))return RejectAction(AbilityUnavailableReason(slot));

  if(!Actor||Actor.Health.CurrentHealth<=0||slot<1||slot>=Ready.Count)return false;

  if(now<CastEnd||now<GlobalReady||now<Ready[slot])return RejectAction("Habilidad en recarga");

  if(slot==4&&(!hasGroundLeap||!TryGroundLeapLanding(Actor.GetComponent<CharacterController>(),Actor.transform.position,leapDestination,GameManager.environmentLayer,out _)))return RejectAction("Elegí un destino de salto válido");

  float cost=AbilityCost(slot,ability);if(Resource<cost)return RejectAction("Furia insuficiente");

  if(!GameManager.Gamemode.CanUseClassAbilities(Actor.transform.position))return RejectAction("No podés usar esta habilidad aquí");

  if(slot==0){var target=CombatTarget;if(!target||target==Actor.Health||target.Team==Actor.Health.Team||target.CurrentHealth<=0)return false;var delta=target.transform.position-Actor.transform.position;delta.y=0;var forward=Quaternion.Euler(0,Actor.Input.LookY,0)*Vector3.forward;if(delta.magnitude>ability.Radius||Vector3.Angle(forward,delta)>ability.Arc*.5f||!CombatController.Visible(Actor,target))return false;}

  if(ability.RequiresPrimaryTarget&&!ValidAreaVictim(CombatTarget,Actor.transform.position,Quaternion.Euler(0,Actor.Input.LookY,0)*Vector3.forward,slot,ability))return RejectAction("Seleccioná un enemigo válido.");

  if(slot!=4&&ability.Shape!=AbilityShape.Self&&ability.Damage>0&&QueueWeaponDraw(slot))return true;

  Resource=Mathf.Max(0,Resource-cost);Ready[slot]=now+AbilityCooldown(slot,ability);GlobalReady=now+ability.Recovery;

  castWeaponRoll=ability.Shape!=AbilityShape.Self&&ability.Damage>0?RollWeaponDamage():0;castWearToken=ability.Shape!=AbilityShape.Self&&ability.Damage>0?BeginWearAction():null;castWearConnected=false;attackTarget=TargetNetId;Casting=slot;pending=ability;strikeTicks=0;CastEnd=now+ability.CastDuration;hitAt=now+ability.HitTime;applied=false;

  facing=Quaternion.Euler(0,Actor.Input.LookY,0)*Vector3.forward;Player.LastCombatAt=now;

  RpcPresentation(slot,Actor.transform.position,facing);return true;

 }

 [Server]public void Cancel(){CancelWeaponDraw();hasGroundLeap=false;Casting=-1;pending=null;CastEnd=0;}

 void Update(){

  TickAutoAttack();

  if(isLocalPlayer&&Actor&&localSelected!=Actor){localSelected=Actor;CmdSelect(PlayerPrefs.GetInt(Preference,0)-1);}

  if(isServer&&Prototype){

   if(!Species.AllowsSkin(Player.PlayerInfo.CharacterSkinID)){Prototype=false;Cancel();return;}

   if(!Actor||Actor.Health.CurrentHealth<=0){if(Class&&Class.Resource==CombatResourceKind.Rage)Resource=0;TargetNetId=0;Cancel();configured=null;return;}if(TargetNetId!=0&&(!CombatTarget||CombatTarget.CurrentHealth<=0||Vector3.Distance(Actor.transform.position,CombatTarget.transform.position)>110))TargetNetId=0;if(configured!=Actor)Configure();TickRageDecay(Time.deltaTime);

   while(pending&&!applied&&NetworkTime.time>=hitAt){Apply(pending);strikeTicks++;if(Casting==5&&strikeTicks<4)hitAt+=.4;else applied=true;}if(pending&&NetworkTime.time>=CastEnd)Cancel();

  }

  if(isLocalPlayer&&Prototype){if(queued>=0){if(Time.unscaledTime>queueUntil)queued=-1;else if(NetworkTime.time>=CastEnd&&NetworkTime.time>=GlobalReady){CmdUse(queued);queued=-1;}}}

 }

 public void Request(int slot){if(!isLocalPlayer||slot<1||slot>=Ready.Count)return;if(!IsAbilityLearned(slot)){LocalNotice(AbilityUnavailableReason(slot));return;}if(NetworkTime.time<Ready[slot]||NetworkTime.time<GlobalReady){LocalNotice("Habilidad en recarga");return;}var ability=Definition(slot);if(slot==4){BeginGroundAim();return;}if(ability&&ability.Shape!=AbilityShape.Self)RiftMMOController.Instance?.TryAutoTarget(ability,this);if(ability&&ability.Implemented&&ability.Allows(Specialization.Id)&&ability.Shape!=AbilityShape.Self&&Resource>=AbilityCost(slot,ability))RiftMMOController.Instance?.StartAutoAttack();if(NetworkTime.time<CastEnd&&CastEnd-NetworkTime.time<=.18){queued=slot;queueUntil=Time.unscaledTime+.18f;}else CmdUseAbility(ability.Id);}


 [Server]void TickRageDecay(float elapsed){if(!Class||Class.Resource!=CombatResourceKind.Rage)return;Resource=Mathf.Clamp(Resource,0,Class.MaximumResource);if(Casting>=0||autoPending||NetworkTime.time-Player.LastCombatAt<Class.RageDecayDelay)return;Resource=Mathf.Max(0,Resource-Mathf.Max(0,Class.RageDecayPerSecond)*Mathf.Max(0,elapsed));}
 bool CanTaunt(Health target,Vector3 origin,AbilityDefinition ability)=>Actor&&target&&target!=Actor.Health&&target.gameObject.activeInHierarchy&&target.CurrentHealth>0&&target.Team!=Actor.Health.Team&&target.gameObject.scene==Actor.gameObject.scene&&(target.transform.position-origin).sqrMagnitude<=ability.Radius*ability.Radius&&CombatController.Visible(Actor,target);
 bool ValidAreaVictim(Health victim,Vector3 origin,Vector3 forward,int slot,AbilityDefinition ability){
  if(!Actor||!victim||victim==Actor.Health||!victim.gameObject.activeInHierarchy||victim.CurrentHealth<=0||victim.Team==Actor.Health.Team||victim.gameObject.scene!=Actor.gameObject.scene)return false;
  var delta=victim.transform.position-origin;delta.y=0;if(delta.magnitude>AbilityRadius(slot,ability)||(ability.Shape==AbilityShape.Arc&&Vector3.Angle(forward,delta)>AbilityArc(slot,ability)*.5f))return false;
  return (!GameManager.Gamemode||!GameManager.Gamemode.BlockDamage(victim,Actor.Health))&&CombatController.Visible(Actor,victim);
 }
 static float AreaDistanceSquared(Health victim,Vector3 origin){var delta=victim.transform.position-origin;delta.y=0;return delta.sqrMagnitude;}
 List<Health> SelectAreaVictims(int slot,AbilityDefinition ability,Vector3 origin,Vector3 forward){
  var selected=new List<Health>();if(ability.RequiresPrimaryTarget){Health primary=null;if(attackTarget!=0&&NetworkServer.spawned.TryGetValue(attackTarget,out var identity))primary=identity.GetComponent<Health>();if(!ValidAreaVictim(primary,origin,forward,slot,ability))return selected;selected.Add(primary);}
  var candidates=new List<Health>();var seen=new HashSet<Health>(selected);foreach(var victim in Health.All)if(victim&&seen.Add(victim)&&ValidAreaVictim(victim,origin,forward,slot,ability))candidates.Add(victim);
  candidates.Sort((a,b)=>{int order=AreaDistanceSquared(a,origin).CompareTo(AreaDistanceSquared(b,origin));return order!=0?order:a.netId.CompareTo(b.netId);});int limit=ability.MaximumTargets>0?ability.MaximumTargets:int.MaxValue;foreach(var candidate in candidates){if(selected.Count>=limit)break;selected.Add(candidate);}return selected;
 }

 // Find a supported, empty capsule on the approach side; never teleport into a target.
 // No controller/cast state is disabled here: failure leaves the actor at its origin.
 public static bool TryLeapLanding(CharacterController body,Transform target,Vector3 origin,Vector3 forward,int groundMask,out Vector3 landing)=>FindLeapLanding(body,target,origin,target?target.position:origin+forward*8,groundMask,out landing);
 static bool FindLeapLanding(CharacterController body,Transform target,Vector3 origin,Vector3 destination,int groundMask,out Vector3 landing){
  landing=origin;if(!body)return false;
  var scale=body.transform.lossyScale;float radius=body.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
  float height=Mathf.Max(radius*2,body.height*Mathf.Abs(scale.y));
  Vector3 center=body.transform.TransformVector(body.center);float bottom=center.y-height*.5f;
  var delta=destination-origin;delta.y=0;float distance=delta.magnitude;if(distance<.05f)return false;
  Vector3 direction=delta/distance;float travel=Mathf.Min(12,distance);
  if(target){
   // AABB support includes large and offset solid colliders, rather than an assumed enemy radius.
   float near=distance;
   foreach(var collider in target.GetComponentsInChildren<Collider>())if(collider.enabled&&!collider.isTrigger){
    var bounds=collider.bounds;float extent=Mathf.Abs(direction.x)*bounds.extents.x+Mathf.Abs(direction.z)*bounds.extents.z;
    near=Mathf.Min(near,Vector3.Dot(bounds.center-origin,direction)-extent);
   }
   travel=Mathf.Min(travel,near-radius-.2f);
  }
  Physics.SyncTransforms();
  for(float step=travel;step>=.25f;step-=.3f){
   var candidate=origin+direction*step;
   // Ground is sampled around the feet too: do not balance the capsule on a ledge.
   if(!Physics.Raycast(candidate+Vector3.up*4,Vector3.down,out var ground,12,groundMask,QueryTriggerInteraction.Ignore)||Vector3.Angle(ground.normal,Vector3.up)>body.slopeLimit)continue;
   if(ground.collider.GetComponentInParent<Health>()||ground.collider.GetComponentInParent<CharacterController>())continue;
   candidate.y=ground.point.y-bottom+.12f;
   bool supported=true;
   foreach(var offset in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back}){
    if(!Physics.Raycast(candidate+offset*radius*.8f+Vector3.up*(bottom+.5f),Vector3.down,out var support,1,groundMask,QueryTriggerInteraction.Ignore)||Mathf.Abs(support.point.y-ground.point.y)>.3f||Vector3.Angle(support.normal,Vector3.up)>body.slopeLimit){supported=false;break;}
   }
   if(!supported)continue;
   Vector3 low=candidate+center+Vector3.up*(-height*.5f+radius),high=candidate+center+Vector3.up*(height*.5f-radius);
   bool blocked=false;
   foreach(var collider in Physics.OverlapCapsule(low,high,radius+.05f,~0,QueryTriggerInteraction.Ignore))if(!collider.transform.IsChildOf(body.transform)){blocked=true;break;}
   if(blocked)continue;
   // Prevent crossing walls/hidden level bounds. Ignore only the actor's own colliders.
   Vector3 fromLow=origin+center+Vector3.up*(-height*.5f+radius),fromHigh=origin+center+Vector3.up*(height*.5f-radius);
   var move=candidate-origin;float length=move.magnitude;
   foreach(var hit in Physics.CapsuleCastAll(fromLow,fromHigh,radius,move.normalized,length,~0,QueryTriggerInteraction.Ignore))if(!hit.collider.transform.IsChildOf(body.transform)&&hit.distance>.02f){blocked=true;break;}
   if(blocked)continue;
   landing=candidate;return true;
  }
  return false;
 }

 [Server]void Apply(AbilityDefinition ability){

  if(!Actor||Actor.Health.CurrentHealth<=0)return;int hits=0;var origin=Actor.transform.position;

  if(Casting==4&&hasGroundLeap&&TryGroundLeapLanding(Actor.GetComponent<CharacterController>(),origin,leapDestination,GameManager.environmentLayer,out var landing)){LastGroundLeapOrigin=origin;Actor.ServerTeleport(landing,Actor.Input.LookY);origin=Actor.transform.position;LastGroundLeapLanding=origin;GroundLeapSequence++;}

  if(ability.Shape==AbilityShape.Self&&Casting==6){DefensiveUntil=NetworkTime.time+ability.BuffDuration;RpcImpact(Casting,origin,facing);return;}

  if(ability.Shape==AbilityShape.Self){

   BuffUntil=NetworkTime.time+ability.BuffDuration;BuffReduction=Specialization.Id=="guardian"?ability.DamageReduction:ability.DamageReduction*.5f;OffensiveBonus=Specialization.Id=="ravager"?ability.OffensiveBonus:0;

   var taunted=new HashSet<Health>();foreach(var enemy in RiftEnemyCombat.Active)if(enemy&&CanTaunt(enemy.GetComponent<Health>(),origin,ability)&&taunted.Add(enemy.GetComponent<Health>()))enemy.Taunt(Actor.Health,ability.BuffDuration,Specialization.Id=="guardian");

   foreach(var enemy in RiftEnemies.RiftEnemyAgent.All)if(enemy&&CanTaunt(enemy.Health,origin,ability)&&taunted.Add(enemy.Health))enemy.Taunt(Actor.Health,ability.BuffDuration,Specialization.Id=="guardian");

  }else{

   // Registry contains each Health once: one damage event per target per strike.

   IEnumerable<Health> victims=ability.MaximumTargets>0||ability.RequiresPrimaryTarget?SelectAreaVictims(Casting,ability,origin,facing):Health.All;
   foreach(var victim in victims){if(!victim)continue;

    if(victim==Actor.Health||victim.Team==Actor.Health.Team||victim.CurrentHealth<=0||(Casting==0&&victim.netId!=attackTarget))continue;

    Vector3 delta=victim.transform.position-origin;delta.y=0;

    if(delta.magnitude>AbilityRadius(Casting,ability)||(ability.Shape==AbilityShape.Arc&&Vector3.Angle(facing,delta)>AbilityArc(Casting,ability)*.5f)||!CombatController.Visible(Actor,victim))continue;

    float buff=BuffUntil>NetworkTime.time?1+OffensiveBonus:1;

    int damage=Mathf.RoundToInt(AbilityDamageBasis(ability,castWeaponRoll)*Specialization.DamageMultiplier*buff*(Casting==2?1+Rank(7)*.08f:Casting==7?1+Rank(2)*.05f:1));

    if(CombatController.Hit(Actor.Health,victim,damage)){hits++;var enemy=victim.GetComponent<RiftEnemyCombat>();if(enemy){if(ability.ThreatMultiplier>1)enemy.AddThreat(Actor.Health,damage*Specialization.ThreatMultiplier*(ability.ThreatMultiplier-1));enemy.Stagger(ability.Stagger+(Casting==2?Rank(7)*.1f:0));}var npc=victim.GetComponent<RiftEnemies.RiftEnemyAgent>();if(npc){if(ability.ThreatMultiplier>1)npc.AddThreat(Actor.Health,damage*Specialization.ThreatMultiplier*(ability.ThreatMultiplier-1));npc.Stagger(ability.Stagger+(Casting==2?Rank(7)*.1f:0));}}

   }

  }

  if(hits>0){if(!castWearConnected){castWearConnected=true;ConnectWearAction(castWearToken);}Resource=Mathf.Min(Class.MaximumResource,Resource+Gain(ability.ResourceGain));}

  RpcImpact(Casting,origin,facing);

 }

 [ClientRpc]void RpcPresentation(int slot,Vector3 origin,Vector3 forward){var a=Definition(slot);if(!a)return;if(a.CastVfx)CombatVfxPool.Spawn(a.CastVfx,origin,Quaternion.LookRotation(forward),3);if(Actor&&a.Animation){var native=Actor.GetComponent<WarriorAvatar>();if(native){native.PresentAttack(slot,a);return;}var pose=Actor.CharacterParent?Actor.CharacterParent.GetComponentInChildren<IDSGameplayPose>(true):null;if(pose){var motion=pose.gameObject.GetComponent<CombatMotion>();if(!motion)motion=pose.gameObject.AddComponent<CombatMotion>();motion.Play(pose.Target,a.Animation,a.CastDuration);}}}

 [ClientRpc]void RpcImpact(int slot,Vector3 origin,Vector3 forward){var a=Definition(slot);if(a&&a.ImpactVfx)CombatVfxPool.Spawn(a.ImpactVfx,origin+Vector3.up*.1f,Quaternion.LookRotation(forward),3);}

}}

