using System.Collections.Generic;

using Mirror;

using UnityEngine;

namespace MultiFPS.Gameplay.RiftCombat {

[DisallowMultipleComponent]

public sealed partial class AbilityController:NetworkBehaviour {

 public const string Preference="ROM.Combat.Nertor";public SpeciesDefinition Species;

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

 void OnDestroy(){Players.Remove(this);}

 public override void OnStartServer(){for(int i=0;i<8;i++)Ready.Add(0);for(int i=0;i<9;i++)Talents.Add(0);}

 [SyncVar]public uint TargetNetId;

 public Health CombatTarget{get{var registry=isServer?NetworkServer.spawned:NetworkClient.spawned;return TargetNetId!=0&&registry.TryGetValue(TargetNetId,out var identity)?identity.GetComponent<Health>():null;}}

 [Command]public void CmdSetTarget(uint id){ServerSetTarget(id);}

 [Server]public bool ServerSetTarget(uint id){if(id==0){TargetNetId=0;return true;}if(!Actor||!NetworkServer.spawned.TryGetValue(id,out var identity))return false;var health=identity.GetComponent<Health>();if(!health||!health.gameObject.activeInHierarchy||health.gameObject.scene!=Actor.gameObject.scene||health.CurrentHealth<=0||Vector3.Distance(Actor.transform.position,health.transform.position)>60)return false;TargetNetId=id;return true;}

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

  if(Resource<ability.Cost)return RejectAction("Furia insuficiente");

  if(!GameManager.Gamemode.CanUseClassAbilities(Actor.transform.position))return RejectAction("No podés usar esta habilidad aquí");

  if(slot==0){var target=CombatTarget;if(!target||target==Actor.Health||target.Team==Actor.Health.Team||target.CurrentHealth<=0)return false;var delta=target.transform.position-Actor.transform.position;delta.y=0;var forward=Quaternion.Euler(0,Actor.Input.LookY,0)*Vector3.forward;if(delta.magnitude>ability.Radius||Vector3.Angle(forward,delta)>ability.Arc*.5f||!CombatController.Visible(Actor,target))return false;}

  Resource=Mathf.Max(0,Resource-ability.Cost);Ready[slot]=now+AbilityCooldown(slot,ability);GlobalReady=now+ability.Recovery;

  attackTarget=TargetNetId;Casting=slot;pending=ability;strikeTicks=0;CastEnd=now+ability.CastDuration;hitAt=now+ability.HitTime;applied=false;

  facing=Quaternion.Euler(0,Actor.Input.LookY,0)*Vector3.forward;Player.LastCombatAt=now;

  RpcPresentation(slot,Actor.transform.position,facing);return true;

 }

 [Server]public void Cancel(){Casting=-1;pending=null;CastEnd=0;}

 void Update(){

  TickAutoAttack();

  if(isLocalPlayer&&Actor&&localSelected!=Actor){localSelected=Actor;CmdSelect(PlayerPrefs.GetInt(Preference,0)-1);}

  if(isServer&&Prototype){

   if(!Species.AllowsSkin(Player.PlayerInfo.CharacterSkinID)){Prototype=false;Cancel();return;}

   if(!Actor||Actor.Health.CurrentHealth<=0){TargetNetId=0;Cancel();configured=null;return;}if(TargetNetId!=0&&(!CombatTarget||CombatTarget.CurrentHealth<=0||Vector3.Distance(Actor.transform.position,CombatTarget.transform.position)>60))TargetNetId=0;if(configured!=Actor)Configure();

   while(pending&&!applied&&NetworkTime.time>=hitAt){Apply(pending);strikeTicks++;if(Casting==5&&strikeTicks<4)hitAt+=.4;else applied=true;}if(pending&&NetworkTime.time>=CastEnd)Cancel();

  }

  if(isLocalPlayer&&Prototype){if(queued>=0){if(Time.unscaledTime>queueUntil)queued=-1;else if(NetworkTime.time>=CastEnd&&NetworkTime.time>=GlobalReady){CmdUse(queued);queued=-1;}}}

 }

 public void Request(int slot){if(!isLocalPlayer||slot<1||slot>=Ready.Count)return;if(!IsAbilityLearned(slot)){LocalNotice(AbilityUnavailableReason(slot));return;}if(NetworkTime.time<Ready[slot]||NetworkTime.time<GlobalReady){LocalNotice("Habilidad en recarga");return;}var ability=Definition(slot);if(ability&&ability.Shape!=AbilityShape.Self)RiftMMOController.Instance?.TryAutoTarget(ability,this);if(ability&&ability.Implemented&&ability.Allows(Specialization.Id)&&ability.Shape!=AbilityShape.Self&&Resource>=ability.Cost)RiftMMOController.Instance?.StartAutoAttack();if(NetworkTime.time<CastEnd&&CastEnd-NetworkTime.time<=.18){queued=slot;queueUntil=Time.unscaledTime+.18f;}else CmdUseAbility(ability.Id);}

 [Server]void Apply(AbilityDefinition ability){

  if(!Actor||Actor.Health.CurrentHealth<=0)return;int hits=0;var origin=Actor.transform.position;

  if(Casting==4){var desired=CombatTarget?CombatTarget.transform.position:origin+facing*8;var delta=Vector3.ClampMagnitude(desired-origin,12);delta.y=0;if(Physics.Raycast(origin+delta+Vector3.up*8,Vector3.down,out var ground,18,GameManager.environmentLayer,QueryTriggerInteraction.Ignore)){Actor.ServerTeleport(ground.point+Vector3.up*.1f,Actor.Input.LookY);origin=Actor.transform.position;}}

  if(ability.Shape==AbilityShape.Self&&Casting==6){DefensiveUntil=NetworkTime.time+ability.BuffDuration;RpcImpact(Casting,origin,facing);return;}

  if(ability.Shape==AbilityShape.Self){

   BuffUntil=NetworkTime.time+ability.BuffDuration;BuffReduction=Specialization.Id=="guardian"?ability.DamageReduction:ability.DamageReduction*.5f;OffensiveBonus=Specialization.Id=="ravager"?ability.OffensiveBonus:0;

   foreach(var enemy in RiftEnemyCombat.Active)if(enemy&&Vector3.Distance(enemy.transform.position,origin)<=ability.Radius)enemy.Taunt(Actor.Health,ability.BuffDuration,Specialization.Id=="guardian");

   foreach(var enemy in RiftEnemies.RiftEnemyAgent.All)if(enemy&&(enemy.transform.position-origin).sqrMagnitude<=ability.Radius*ability.Radius)enemy.Taunt(Actor.Health,ability.BuffDuration,Specialization.Id=="guardian");

  }else{

   // Registry contains each Health once: one damage event per target per strike.

   foreach(var victim in Health.All){if(!victim)continue;

    if(victim==Actor.Health||victim.Team==Actor.Health.Team||victim.CurrentHealth<=0||(Casting==0&&victim.netId!=attackTarget))continue;

    Vector3 delta=victim.transform.position-origin;delta.y=0;

    if(delta.magnitude>AbilityRadius(Casting,ability)||(ability.Shape==AbilityShape.Arc&&Vector3.Angle(facing,delta)>AbilityArc(Casting,ability)*.5f)||!CombatController.Visible(Actor,victim))continue;

    float buff=BuffUntil>NetworkTime.time?1+OffensiveBonus:1;

    int damage=Mathf.RoundToInt(ability.Damage*Specialization.DamageMultiplier*WeaponMultiplier*buff*(Casting==2?1+Rank(7)*.08f:Casting==7?1+Rank(2)*.05f:1));

    if(CombatController.Hit(Actor.Health,victim,damage)){hits++;var enemy=victim.GetComponent<RiftEnemyCombat>();if(enemy){if(ability.ThreatMultiplier>1)enemy.AddThreat(Actor.Health,damage*Specialization.ThreatMultiplier*(ability.ThreatMultiplier-1));enemy.Stagger(ability.Stagger+(Casting==2?Rank(7)*.1f:0));}var npc=victim.GetComponent<RiftEnemies.RiftEnemyAgent>();if(npc){if(ability.ThreatMultiplier>1)npc.AddThreat(Actor.Health,damage*Specialization.ThreatMultiplier*(ability.ThreatMultiplier-1));npc.Stagger(ability.Stagger+(Casting==2?Rank(7)*.1f:0));}}

   }

  }

  if(hits>0)Resource=Mathf.Min(Class.MaximumResource,Resource+Gain(ability.ResourceGain));

  RpcImpact(Casting,origin,facing);

 }

 [ClientRpc]void RpcPresentation(int slot,Vector3 origin,Vector3 forward){var a=Definition(slot);if(!a)return;if(a.CastVfx)CombatVfxPool.Spawn(a.CastVfx,origin,Quaternion.LookRotation(forward),3);if(Actor&&a.Animation){var native=Actor.GetComponent<WarriorAvatar>();if(native){native.PresentAttack(slot,a);return;}var pose=Actor.CharacterParent?Actor.CharacterParent.GetComponentInChildren<IDSGameplayPose>(true):null;if(pose){var motion=pose.gameObject.GetComponent<CombatMotion>();if(!motion)motion=pose.gameObject.AddComponent<CombatMotion>();motion.Play(pose.Target,a.Animation,a.CastDuration);}}}

 [ClientRpc]void RpcImpact(int slot,Vector3 origin,Vector3 forward){var a=Definition(slot);if(a&&a.ImpactVfx)CombatVfxPool.Spawn(a.ImpactVfx,origin+Vector3.up*.1f,Quaternion.LookRotation(forward),3);}

}}

