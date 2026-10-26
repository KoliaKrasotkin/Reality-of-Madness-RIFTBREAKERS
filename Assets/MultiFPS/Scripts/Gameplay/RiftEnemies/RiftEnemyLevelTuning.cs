using System;using UnityEngine;using MultiFPS.Gameplay.RiftCombat;
namespace MultiFPS.Gameplay.RiftEnemies {
[CreateAssetMenu(menuName="ROM/Enemies/Level scaling")]
public sealed class RiftEnemyLevelTuning:ScriptableObject {
 [Range(0,.5f)]public float HealthPerLevel=.08f,DamagePerLevel=.06f;
 [Header("Enemy above player level only")][Range(1,3)]public float OverlevelDamageGrowth=1.65f;[Range(0,10)]public int OverlevelGrace=2;
 [Range(0,2)]public float ExperiencePerLevel=.35f,GoldPerLevel=.12f;
 public int MaximumHealth=100000,MaximumDamage=10000,MaximumExperience=1000000,MaximumGold=1000000;
 public int ClampLevel(int value)=>Mathf.Clamp(value,1,RiftProgressionCaps.Load.WorldCap);
 static double Rate(float value,float max)=>float.IsNaN(value)||float.IsInfinity(value)?0:Math.Max(0,Math.Min(max,value));
 int Scale(int basis,int level,float rate,int maximum,bool exponential){if(basis<=0)return 0;double factor=exponential?Math.Pow(1+Rate(rate,.5f),ClampLevel(level)-1):1+(ClampLevel(level)-1)*Rate(rate,2);return (int)Math.Max(1,Math.Min(Math.Max(1,maximum),Math.Round(basis*factor,MidpointRounding.AwayFromZero)));}
 public int Health(int basis,int level)=>Scale(Math.Max(1,basis),level,HealthPerLevel,Mathf.Clamp(MaximumHealth,1,100000),true);
 public int Damage(int basis,int level)=>Scale(basis,level,DamagePerLevel,Mathf.Clamp(MaximumDamage,1,10000),true);
 public int DamageAgainstPlayer(int incoming,int enemyLevel,int playerLevel){if(incoming<=0)return incoming;int gap=Mathf.Max(0,ClampLevel(enemyLevel)-Mathf.Max(1,playerLevel)-Mathf.Clamp(OverlevelGrace,0,10));double growth=double.IsNaN(OverlevelDamageGrowth)||double.IsInfinity(OverlevelDamageGrowth)?1:Math.Max(1,Math.Min(3,OverlevelDamageGrowth));if(gap==0||growth<=1)return incoming;double amount=incoming*Math.Pow(growth,gap);return (int)Math.Max(1,Math.Min(Mathf.Clamp(MaximumDamage,1,10000),Math.Round(amount,MidpointRounding.AwayFromZero)));}
 public int Experience(int basis,int level)=>Scale(basis,level,ExperiencePerLevel,Mathf.Clamp(MaximumExperience,1,1000000),false);
 public int Gold(int basis,int level)=>Scale(basis,level,GoldPerLevel,Mathf.Clamp(MaximumGold,1,1000000),false);
 static RiftEnemyLevelTuning cached;public static RiftEnemyLevelTuning Load{get{if(!cached)cached=Resources.Load<RiftEnemyLevelTuning>("RiftEnemyLevelTuning");if(!cached)cached=CreateInstance<RiftEnemyLevelTuning>();return cached;}}
}}
