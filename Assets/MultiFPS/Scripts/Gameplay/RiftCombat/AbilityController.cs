using System.Collections.Generic;
using Mirror;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
[DisallowMultipleComponent]
public sealed class AbilityController:NetworkBehaviour {
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
 AbilityDefinition pending;double hitAt;bool applied;Vector3 facing;CharacterInstance configured; double nextSelect;int queued=-1;float queueUntil;
 void Awake(){Player=GetComponent<PlayerInstance>();Players.Add(this);}
 void OnDestroy(){Players.Remove(this);}
 public override void OnStartServer(){for(int i=0;i<7;i++)Ready.Add(0);}
 CharacterInstance localSelected;
 public override void OnStartLocalPlayer(){}
 [Command]public void CmdSelect(int specialization){ServerSelect(specialization);}
 [Server]public bool ServerSelect(int specialization){
  if(Player.BOT||!Class||specialization < -1||specialization>=Class.Specializations.Length||NetworkTime.time<nextSelect)return false;
  if(Actor&&(Actor.Health.CurrentHealth<=0||!GameManager.Gamemode.IsProtectedPosition(Actor.transform.position)))return false;
  if(specialization>=0&&!Species.AllowsSkin(Player.PlayerInfo.CharacterSkinID))return false;
  nextSelect=NetworkTime.time+.5;Prototype=specialization>=0;SpecIndex=Mathf.Max(0,specialization);Resource=0;Cancel();BuffUntil=0;configured=null;
  if(Actor){if(Prototype)Configure();else Player.ServerConfigureClass();}return true;
 }
 void Configure(){if(!Actor||!Specialization)return;Resource=0;Actor.Health.ServerSetVitals(Specialization.Health,Specialization.Health);configured=Actor;}
 public AbilityDefinition Definition(int slot)=>Class?(slot==0?Class.BasicAttack:slot>0&&slot<=Class.Abilities.Length?Class.Abilities[slot-1]:null):null;
 [Command]public void CmdUse(int slot){ServerUse(slot);}
 [SyncVar]public double PortalReady;
 [Command]public void CmdPortal(int id){ServerPortal(id);}
 [Server]public bool ServerPortal(int id){if(!Actor||Actor.Health.CurrentHealth<=0||NetworkTime.time<PortalReady||Casting>=0)return false;foreach(var portal in RiftPortal.All)if(portal&&portal.Definition&&portal.Definition.Id==id&&portal.Destination&&Vector3.Distance(Actor.transform.position,portal.transform.position)<=portal.Definition.InteractRange){PortalReady=NetworkTime.time+portal.Definition.Cooldown;Actor.ServerTeleport(portal.Destination.position,portal.Destination.eulerAngles.y);return true;}return false;}
 [Server]public bool ServerUse(int slot){
  var ability=Definition(slot);double now=NetworkTime.time;
  if(!Prototype||!Actor||Actor.Health.CurrentHealth<=0||!ability||!ability.Implemented||!ability.Allows(Specialization.Id)||slot<0||slot>=Ready.Count||now<CastEnd||now<GlobalReady||now<Ready[slot]||Resource<ability.Cost||!GameManager.Gamemode.CanUseClassAbilities(Actor.transform.position))return false;
  Resource=Mathf.Max(0,Resource-ability.Cost);Ready[slot]=now+ability.Cooldown;GlobalReady=now+ability.Recovery;
  Casting=slot;pending=ability;CastEnd=now+ability.CastDuration;hitAt=now+ability.HitTime;applied=false;
  facing=Quaternion.Euler(0,Actor.Input.LookY,0)*Vector3.forward;Player.LastCombatAt=now;
  RpcPresentation(slot,Actor.transform.position,facing);return true;
 }
 [Server]public void Cancel(){Casting=-1;pending=null;CastEnd=0;}
 void Update(){
  if(isLocalPlayer&&Actor&&localSelected!=Actor){localSelected=Actor;CmdSelect(PlayerPrefs.GetInt(Preference,0)-1);}
  if(isServer&&Prototype){
   if(!Species.AllowsSkin(Player.PlayerInfo.CharacterSkinID)){Prototype=false;Cancel();return;}
   if(!Actor||Actor.Health.CurrentHealth<=0){Cancel();configured=null;return;}if(configured!=Actor)Configure();
   if(pending&&!applied&&NetworkTime.time>=hitAt){applied=true;Apply(pending);}if(pending&&NetworkTime.time>=CastEnd)Cancel();
  }
  if(isLocalPlayer&&Prototype){if(queued>=0){if(Time.unscaledTime>queueUntil)queued=-1;else if(NetworkTime.time>=CastEnd&&NetworkTime.time>=GlobalReady){CmdUse(queued);queued=-1;}}}
 }
 public void Request(int slot){if(!isLocalPlayer||!Prototype||slot<0||slot>=Ready.Count||NetworkTime.time<Ready[slot]||NetworkTime.time<GlobalReady)return;if(NetworkTime.time<CastEnd&&CastEnd-NetworkTime.time<=.18){queued=slot;queueUntil=Time.unscaledTime+.18f;}else CmdUse(slot);}
 [Server]void Apply(AbilityDefinition ability){
  if(!Actor||Actor.Health.CurrentHealth<=0)return;int hits=0;var origin=Actor.transform.position;
  if(ability.Shape==AbilityShape.Self){
   BuffUntil=NetworkTime.time+ability.BuffDuration;BuffReduction=Specialization.Id=="guardian"?ability.DamageReduction:ability.DamageReduction*.5f;OffensiveBonus=Specialization.Id=="ravager"?ability.OffensiveBonus:0;
   foreach(var enemy in RiftEnemyCombat.Active)if(enemy&&Vector3.Distance(enemy.transform.position,origin)<=ability.Radius)enemy.Taunt(Actor.Health,ability.BuffDuration,Specialization.Id=="guardian");
  }else{
   // Registry contains each Health once: one damage event per target per strike.
   foreach(var p in GameManager.Players){if(!p||!p.MyCharacter)continue;var victim=p.MyCharacter.Health;
    if(victim==Actor.Health||victim.Team==Actor.Health.Team||victim.CurrentHealth<=0)continue;
    Vector3 delta=victim.transform.position-origin;delta.y=0;
    if(delta.magnitude>ability.Radius||(ability.Shape==AbilityShape.Arc&&Vector3.Angle(facing,delta)>ability.Arc*.5f)||!CombatController.Visible(Actor,victim))continue;
    float buff=BuffUntil>NetworkTime.time?1+OffensiveBonus:1;
    int damage=Mathf.RoundToInt(ability.Damage*Specialization.DamageMultiplier*(Specialization.Weapon?Specialization.Weapon.DamageMultiplier:1)*buff);
    if(CombatController.Hit(Actor.Health,victim,damage)){hits++;var enemy=victim.GetComponent<RiftEnemyCombat>();if(enemy){if(ability.ThreatMultiplier>1)enemy.AddThreat(Actor.Health,damage*Specialization.ThreatMultiplier*(ability.ThreatMultiplier-1));enemy.Stagger(ability.Stagger);}}
   }
  }
  if(hits>0)Resource=Mathf.Min(Class.MaximumResource,Resource+ability.ResourceGain);
  RpcImpact(Casting,origin,facing);
 }
 [ClientRpc]void RpcPresentation(int slot,Vector3 origin,Vector3 forward){var a=Definition(slot);if(!a)return;if(a.CastVfx)CombatVfxPool.Spawn(a.CastVfx,origin,Quaternion.LookRotation(forward),3);if(Actor&&a.Animation){var native=Actor.GetComponent<WarriorAvatar>();if(native){native.PresentAttack(slot,a);return;}var pose=Actor.CharacterParent?Actor.CharacterParent.GetComponentInChildren<IDSGameplayPose>(true):null;if(pose){var motion=pose.gameObject.GetComponent<CombatMotion>();if(!motion)motion=pose.gameObject.AddComponent<CombatMotion>();motion.Play(pose.Target,a.Animation,a.CastDuration);}}}
 [ClientRpc]void RpcImpact(int slot,Vector3 origin,Vector3 forward){var a=Definition(slot);if(a&&a.ImpactVfx)CombatVfxPool.Spawn(a.ImpactVfx,origin+Vector3.up*.1f,Quaternion.LookRotation(forward),3);}
}}
