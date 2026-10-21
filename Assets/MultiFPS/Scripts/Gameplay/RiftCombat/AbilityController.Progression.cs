using Mirror;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public sealed partial class AbilityController {
 [SyncVar]public int Experience;[SyncVar]public bool SwordAndShield;[SyncVar]public double DefensiveUntil;
 public readonly SyncList<int> Talents=new SyncList<int>();
 public WarriorProgressionDefinition Progression=>WarriorProgressionDefinition.Load;
 public int Level=>Progression.Level(Experience);public int Rank(int i)=>Prototype&&i>=0&&i<Talents.Count?Talents[i]:0;
 public int Spent {get{int count=0;foreach(var rank in Talents)count+=rank;return count;}}public int TalentPoints=>Mathf.Max(0,Level-1-Spent);
 public float PowerMultiplier=>(1+(Level-1)*Progression.DamagePerLevel)*(1+Rank(0)*.03f);
 public float CriticalChance=>Mathf.Clamp01(AutoProfile.CriticalChance+Rank(2)*.01f);
 public float ExtraArmor=>Mathf.Clamp01(Rank(3)*.02f+(Prototype&&SwordAndShield?.12f+Rank(5)*.03f:0));
 public float WeaponMultiplier=>Prototype&&SwordAndShield?.85f:Specialization&&Specialization.Weapon?Specialization.Weapon.DamageMultiplier:1;
 public int ProgressionHealth(int basis)=>Mathf.Max(1,Mathf.RoundToInt(basis*(1+(Level-1)*Progression.HealthPerLevel)*(1+Rank(4)*.05f)*(Prototype&&SwordAndShield?1.15f:1)));
 public float AbilityCooldown(int slot,AbilityDefinition a)=>Mathf.Max(.1f,a.Cooldown*(1-Rank(8)*.03f)*(slot==6&&SwordAndShield?1-Rank(5)*.05f:1));
 public float AbilityRadius(int slot,AbilityDefinition a)=>a.Radius+((slot==1||slot==7)?Rank(1)*.3f:0);
 public float AbilityArc(int slot,AbilityDefinition a)=>a.Arc+((slot==1||slot==7)?Rank(1)*15:0);
 public float Gain(float amount)=>amount*(1+Rank(6)*.1f);
 public bool CanChangeBuild=>Actor&&Actor.Health.CurrentHealth>0&&Casting<0&&(GameManager.Gamemode.IsProtectedPosition(Actor.transform.position)||NetworkTime.time-Player.LastCombatAt>8);
 [Server]public void ServerProgression(int experience,int[] ranks,bool swordShield){Experience=Mathf.Clamp(experience,0,Progression.MaximumExperience);while(Talents.Count<Progression.Talents.Length)Talents.Add(0);for(int i=0;i<Talents.Count;i++)Talents[i]=ranks!=null&&i<ranks.Length?Mathf.Clamp(ranks[i],0,Progression.Talents[i].MaximumRank):0;SwordAndShield=swordShield;if(Actor){int max=ProgressionHealth(Prototype&&Specialization?Specialization.Health:Player.ClassDefinition?Player.ClassDefinition.Health:Actor.Health.MaxHealth);if(max!=Actor.Health.MaxHealth)Actor.Health.ServerSetVitals(max,Mathf.Clamp(Mathf.RoundToInt(Actor.Health.CurrentHealth*(float)max/Mathf.Max(1,Actor.Health.MaxHealth)),1,max));}}
}
}
