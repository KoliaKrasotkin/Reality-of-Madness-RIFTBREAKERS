using UnityEngine;
namespace ROM.Progression {
[CreateAssetMenu(menuName="ROM/Vendor/Repair tuning")]
public sealed class RiftRepairTuning:ScriptableObject {
 [Range(1,10000)]public int MaximumDurability=100;[Range(0,100000)]public int GoldPerPoint=1;
 [Tooltip("Points lost when the server action hit-wear roll succeeds. One roll per action, including multi-target/tick abilities.")][Range(0,100)]public int WearPerAction=1;
 [Range(0,1)]public float DeathLossFraction=.1f;
 [Tooltip("Chance per connected offensive action; never per victim or tick.")][Range(0,1)]public float HitWearChance=.1f;
 public bool AttackWearEnabled=true,SuspendBonusesAtZero=true;
 public static bool AutomaticWearEnabled=>Load&&Load.AttackWearEnabled;
 public bool ShouldLoseHit(float roll){if(float.IsNaN(roll)||float.IsInfinity(roll))return false;return Mathf.Clamp(roll,0,.99999994f)<Mathf.Clamp01(HitWearChance);}
 public int DeathLoss(int maximum)=>Mathf.CeilToInt(Mathf.Max(1,maximum)*Mathf.Clamp01(DeathLossFraction));
 public static RiftRepairTuning Load=>Resources.Load<RiftRepairTuning>("RiftRepairTuning");
 public int Maximum=>Mathf.Clamp(MaximumDurability,1,10000);
 public int Price(int points){long total=(long)Mathf.Max(0,points)*Mathf.Clamp(GoldPerPoint,0,100000);return (int)System.Math.Min(int.MaxValue,total);}
}
}
