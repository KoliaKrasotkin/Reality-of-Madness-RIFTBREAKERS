using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public enum AbilityShape { Arc, Circle, Self, Ground, Projectile }
public enum CombatResourceKind { Rage, Mana, Energy }
public enum CombatDamageKind { Physical, Arcane }
[CreateAssetMenu(menuName="ROM/Combat/Ability")]
public sealed class AbilityDefinition:ScriptableObject {
 public string Id,DisplayName; [TextArea] public string Description;
 public Sprite Icon; public AbilityShape Shape; public CombatDamageKind DamageKind;
 public float Damage=25,Range=3,Radius=3,Arc=120,Cooldown=1,Cost,ResourceGain,CastDuration=.6f,HitTime=.28f,Recovery=.15f,Stagger,ThreatMultiplier=1;
 public bool Implemented=true,Interruptible=true,AllowsMovement=true;
 public AnimationClip Animation; public GameObject CastVfx,ImpactVfx; public AudioClip Audio;
 public Color Color=new Color(.7f,.2f,1); public float BuffDuration,DamageReduction,OffensiveBonus;
 public string[] AllowedSpecializations;
 public bool Allows(string id)=>AllowedSpecializations==null||AllowedSpecializations.Length==0||System.Array.IndexOf(AllowedSpecializations,id)>=0;
 void OnValidate(){Cooldown=Mathf.Max(.1f,Cooldown);CastDuration=Mathf.Max(.1f,CastDuration);HitTime=Mathf.Clamp(HitTime,0,CastDuration);Cost=Mathf.Max(0,Cost);}
}}
