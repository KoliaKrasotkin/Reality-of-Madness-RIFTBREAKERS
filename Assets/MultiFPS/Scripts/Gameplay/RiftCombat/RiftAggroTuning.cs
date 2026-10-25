using System;using System.Linq;using UnityEngine;using Mirror;using MultiFPS.Gameplay.RiftEnemies;
namespace MultiFPS.Gameplay.RiftCombat {
[CreateAssetMenu(menuName="ROM/Enemies/Aggro and threat")]
public sealed class RiftAggroTuning:ScriptableObject {
 [Min(0)]public float MetersPerLevel=.4f,MinimumDetection=2,MaximumDetection=32;
 [Min(1)]public float SwitchMultiplier=1.15f;[Min(0)]public float SwitchMargin=3,SwitchHoldSeconds=1,HealingThreatMultiplier=.5f;
 static float Safe(float value,float fallback)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:value;
 public float Detection(float basis,int enemyLevel,int playerLevel){float min=Mathf.Max(.1f,Safe(MinimumDetection,2)),max=Mathf.Max(min,Safe(MaximumDetection,32));int difference=Mathf.Clamp(enemyLevel,1,60)-Mathf.Clamp(playerLevel,1,60);return Mathf.Clamp(Mathf.Max(0,Safe(basis,min))+difference*Mathf.Clamp(Safe(MetersPerLevel,.4f),0,2),min,max);}
 public bool ShouldSwitch(float currentScore,float candidateScore,double since,double now)=>now-since>=Mathf.Clamp(Safe(SwitchHoldSeconds,1),0,10)&&candidateScore>currentScore*Mathf.Clamp(Safe(SwitchMultiplier,1.15f),1,3)+Mathf.Max(0,Safe(SwitchMargin,3));
 public float HealingShare(int amount,int count)=>Mathf.Max(0,amount)*Mathf.Clamp(Safe(HealingThreatMultiplier,.5f),0,5)/Mathf.Max(1,count);
 static RiftAggroTuning cached;public static RiftAggroTuning Load{get{if(!cached)cached=Resources.Load<RiftAggroTuning>("RiftAggroTuning");if(!cached)cached=CreateInstance<RiftAggroTuning>();return cached;}}
}
public static class RiftThreatRules {
 public static bool ActivePlayer(Health h){if(!NetworkServer.active||!h||h.CurrentHealth<=0)return false;var owner=RiftClassCombat.Owner(h);return owner&&!owner.BOT&&owner.connectionToClient!=null&&owner.connectionToClient.isReady;}
 public static int PlayerLevel(Health h){var c=AbilityController.For(h);return c?c.Level:1;}
 public static float Accumulate(float old,float value){if(float.IsNaN(value)||float.IsInfinity(value)||value<=0)return old;return (float)Math.Min(1000000000,Math.Max(0,float.IsNaN(old)||float.IsInfinity(old)?0:old)+(double)value);}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Install(){Health.ServerHealed-=Healing;Health.ServerHealed+=Healing;}
 static void Healing(Health receiver,Health healer,int effective){if(!NetworkServer.active||effective<=0||!ActivePlayer(receiver)||!ActivePlayer(healer)||receiver.Team!=healer.Team)return;var native=RiftEnemyAgent.All.Where(e=>e&&e.CanReceiveHealingThreat(receiver,healer)).ToArray();var legacy=RiftEnemyCombat.Active.Where(e=>e&&e.CanReceiveHealingThreat(receiver,healer)).ToArray();int count=native.Length+legacy.Length;if(count==0)return;var c=AbilityController.For(healer);float role=c&&c.Prototype&&c.Specialization?c.Specialization.ThreatMultiplier:1;float share=RiftAggroTuning.Load.HealingShare(effective,count)*role;foreach(var e in native)e.AddThreat(healer,share);foreach(var e in legacy)e.AddThreat(healer,share);}
}
}
