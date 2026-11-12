using Mirror;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public sealed partial class AbilityController {

 sealed class FaithSnapshot {public float Value;public double[] Ready;public double GlobalReady;}
 // Server process memory only. Stable existing character IDs preserve resource on reconnect.
 static readonly System.Collections.Generic.Dictionary<string,FaithSnapshot> faithSessions=new System.Collections.Generic.Dictionary<string,FaithSnapshot>();
 bool faithInitialized;double lastFaithTick;string faithIdentity;
 void ConfigurePaladinFaith(){
  string id=Player.PlayerInfo.CharacterId;if(string.IsNullOrEmpty(id))id=netId.ToString();
  if(!faithInitialized||faithIdentity!=id){faithIdentity=id;faithInitialized=true;if(faithSessions.TryGetValue(id,out var saved)){Resource=Mathf.Clamp(saved.Value,0,Class.MaximumResource);if(saved.Ready!=null)for(int i=0;i<Ready.Count&&i<saved.Ready.Length;i++)Ready[i]=System.Math.Max(Ready[i],saved.Ready[i]);GlobalReady=System.Math.Max(GlobalReady,saved.GlobalReady);}else Resource=Mathf.Clamp(PaladinPrototypeSettings.Load.InitialFaith,0,Class.MaximumResource);}
  lastFaithTick=NetworkTime.time;StorePaladinFaith();
 }
 void StorePaladinFaith(){if(!faithInitialized||string.IsNullOrEmpty(faithIdentity))return;faithSessions[faithIdentity]=new FaithSnapshot{Value=Mathf.Clamp(Resource,0,Class?Class.MaximumResource:100),Ready=System.Linq.Enumerable.ToArray(Ready),GlobalReady=GlobalReady};}
 void ServerPaladinDeath(){Resource=0;lastFaithTick=NetworkTime.time;StorePaladinFaith();}
 void RegenerateFaith(){double now=NetworkTime.time;float elapsed=(float)System.Math.Max(0,now-lastFaithTick);lastFaithTick=now;Resource=Mathf.Clamp(Resource+elapsed*Mathf.Max(0,PaladinPrototypeSettings.Load.FaithRegenerationPerSecond),0,Class?Class.MaximumResource:100);StorePaladinFaith();}
 AbilityDefinition paladinPending;uint paladinVictim;bool paladinApplied;double paladinHitAt;Health paladinCaster;string paladinLife;float paladinWeaponRoll;
 public string AbilityRangeSummary(int slot,AbilityDefinition a)=>a&&a.PaladinEffect==PaladinAbilityEffect.Prayer?"Uso propio; sin objetivo":"Alcance/área: "+(IsPaladin?a.Range:AbilityRadius(slot,a)).ToString("0.0")+" m";
 public string ResourceName=>IsMage?"Maná":IsPaladin?"Fe":"Furia";
 public string ClassLabel=>IsMage?"Mago":IsPaladin?"Paladín":"Warrior";
 public bool IsPaladinSupport(AbilityDefinition a)=>a&&(a.PaladinEffect==PaladinAbilityEffect.HealingLight||a.PaladinEffect==PaladinAbilityEffect.RadiantShield);
 public string PaladinEffectSummary(AbilityDefinition a){if(!a)return "";if(a.PaladinEffect==PaladinAbilityEffect.HealingLight)return "Cura: "+(a.HealthFraction*100).ToString("0.#")+"% de vida máxima del objetivo";if(a.PaladinEffect==PaladinAbilityEffect.RadiantShield)return "Absorbe: "+(a.HealthFraction*100).ToString("0.#")+"% de vida máxima durante "+a.BuffDuration.ToString("0.#")+" s; no acumula";if(a.PaladinEffect==PaladinAbilityEffect.Prayer)return "Restaura toda la Fe; recarga "+a.Cooldown.ToString("0.#")+" s";return "Consume "+a.Cost.ToString("0.#")+" Fe; sin generación por impacto";}
 void RequestPaladin(int slot,AbilityDefinition a){
  if(!a)return;
  if(a.PaladinEffect==PaladinAbilityEffect.SacredStrike)RiftMMOController.Instance?.TryAutoTarget(a,this);
  CmdUseAbility(a.Id);
 }
 bool PaladinTarget(uint id,AbilityDefinition a,out Health target){
  target=null;if(!Actor||!Actor.Health||Actor.Health.CurrentHealth<=0)return false;
  if(id==0){if(!IsPaladinSupport(a))return false;target=Actor.Health;}
  else if(NetworkServer.spawned.TryGetValue(id,out var identity))target=identity.GetComponent<Health>();
  if(!target||!target.gameObject.activeInHierarchy||target.CurrentHealth<=0||target.gameObject.scene!=Actor.gameObject.scene)return false;
  bool friendly=target==Actor.Health||target.Team==Actor.Health.Team;
  if(IsPaladinSupport(a)!=friendly)return false;
  if(Vector3.Distance(Actor.transform.position,target.transform.position)>a.Range)return false;
  if(target!=Actor.Health&&!CombatController.Visible(Actor,target))return false;
  if(!IsPaladinSupport(a)){var delta=target.transform.position-Actor.transform.position;delta.y=0;if(Vector3.Angle(Quaternion.Euler(0,Actor.Input.LookY,0)*Vector3.forward,delta)>a.Arc*.5f)return false;if(GameManager.Gamemode&&GameManager.Gamemode.BlockDamage(target,Actor.Health))return false;}
  return true;
 }
 [Server]bool ServerUsePaladin(int slot,AbilityDefinition a,double now){
  if(!IsPaladin||!a||a.PaladinEffect==PaladinAbilityEffect.None||!Class||Class.Resource!=CombatResourceKind.Faith)return RejectAction("Habilidad de Paladín no disponible");
  if(!GameManager.Gamemode||!GameManager.Gamemode.CanUseClassAbilities(Actor.transform.position))return RejectAction("No podés usar esta habilidad aquí");
  if(a.PaladinEffect==PaladinAbilityEffect.Prayer){if(Resource>=Class.MaximumResource)return RejectAction("Ya tenés toda la Fe");Resource=Class.MaximumResource;Ready[slot]=now+AbilityCooldown(slot,a);GlobalReady=now+a.Recovery;StorePaladinFaith();RpcPaladinImpact(slot,Actor.netId);return true;}
  if(!PaladinTarget(TargetNetId,a,out var target))return RejectAction(IsPaladinSupport(a)?"Seleccioná un aliado vivo y visible dentro del alcance, o quitá el objetivo para usarla en vos.":"Seleccioná un enemigo visible dentro del alcance.");
  if(a.PaladinEffect==PaladinAbilityEffect.HealingLight&&target.CurrentHealth>=target.MaxHealth)return RejectAction("El objetivo ya tiene toda la vida");
  if(a.PaladinEffect==PaladinAbilityEffect.RadiantShield&&target.RadiantShieldRemaining>0)return RejectAction("El objetivo ya tiene Defensa radiante");
  float cost=AbilityCost(slot,a);if(Resource<cost)return RejectAction("Fe insuficiente");
  if(a.PaladinEffect==PaladinAbilityEffect.SacredStrike&&QueueWeaponDraw(slot))return true;
  Resource=Mathf.Clamp(Resource-cost,0,Class.MaximumResource);Ready[slot]=now+AbilityCooldown(slot,a);GlobalReady=now+a.Recovery;
  Casting=slot;CastEnd=now+a.CastDuration;paladinHitAt=now+a.HitTime;paladinPending=a;paladinVictim=target.netId;paladinApplied=false;paladinCaster=Actor.Health;paladinLife=paladinCaster.ServerLifeToken;paladinWeaponRoll=a.PaladinEffect==PaladinAbilityEffect.SacredStrike?RollWeaponDamage():0;
  StorePaladinFaith();Player.LastCombatAt=now;RpcPresentation(slot,Actor.transform.position,Quaternion.Euler(0,Actor.Input.LookY,0)*Vector3.forward);return true;
 }
 [Server]void TickPaladinCast(){
  RegenerateFaith();
  if(!paladinPending)return;
  if(!Actor||Actor.Health!=paladinCaster||Actor.Health.CurrentHealth<=0||paladinCaster.ServerLifeToken!=paladinLife){Cancel();return;}
  if(!paladinApplied&&NetworkTime.time>=paladinHitAt){
   paladinApplied=true;var a=paladinPending;
   if(PaladinTarget(paladinVictim,a,out var target)){
    bool success=false;
    if(a.PaladinEffect==PaladinAbilityEffect.SacredStrike){success=CombatController.Hit(Actor.Health,target,Mathf.RoundToInt(AbilityDamageBasis(a,paladinWeaponRoll)*Specialization.DamageMultiplier));}
    else if(a.PaladinEffect==PaladinAbilityEffect.HealingLight)success=target.ServerHeal(Mathf.Max(1,Mathf.RoundToInt(target.MaxHealth*a.HealthFraction)),Actor.Health)>0;
    else if(a.PaladinEffect==PaladinAbilityEffect.RadiantShield)success=target.ServerGrantRadiantShield(Mathf.Max(1,Mathf.RoundToInt(target.MaxHealth*a.HealthFraction)),a.BuffDuration);
    if(success)RpcPaladinImpact(Casting,target.netId);else RejectAction("El objetivo ya no admite el efecto");
   }else RejectAction("El objetivo dejó de ser válido; lanzamiento cancelado");
  }
  if(NetworkTime.time>=CastEnd)Cancel();
 }
 void CancelPaladinCast(){paladinPending=null;paladinCaster=null;paladinLife=null;paladinVictim=0;}
 [ClientRpc]void RpcPaladinImpact(int slot,uint id){if(!NetworkClient.spawned.TryGetValue(id,out var identity))return;var a=Definition(slot);if(!a)return;PaladinEffectPulse.Show(identity.transform,a.Color);}
}
// Local non-interactive feedback; never deals damage or grants resource.
public sealed class PaladinEffectPulse:MonoBehaviour {
 float until;LineRenderer line;
 public static void Show(Transform target,Color color){var go=new GameObject("Paladin effect pulse");go.transform.SetParent(target,false);go.transform.localPosition=Vector3.up*.12f;var p=go.AddComponent<PaladinEffectPulse>();p.until=Time.time+.65f;p.line=go.AddComponent<LineRenderer>();p.line.useWorldSpace=false;p.line.loop=true;p.line.positionCount=32;p.line.widthMultiplier=.055f;var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");if(!shader)shader=Shader.Find("Sprites/Default");p.line.material=new Material(shader);p.line.startColor=p.line.endColor=color;p.line.material.color=color;for(int i=0;i<32;i++){float angle=i*Mathf.PI*2/32;p.line.SetPosition(i,new Vector3(Mathf.Cos(angle),.2f,Mathf.Sin(angle))*.65f);}}
 void Update(){if(Time.time>=until)Destroy(gameObject);}
 void OnDestroy(){if(line&&line.sharedMaterial)Destroy(line.sharedMaterial);}
}
}
