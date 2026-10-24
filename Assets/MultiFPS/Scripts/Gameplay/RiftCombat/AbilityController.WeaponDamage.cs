using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public sealed partial class AbilityController {
 float castWeaponRoll,autoWeaponRoll;
 public WeaponDefinition EquippedDamageWeapon=>Prototype&&SwordAndShield?Progression.Sword:Specialization?Specialization.Weapon:null;
 public float EquippedWeaponPower=>Prototype&&SwordAndShield?1:LootWeaponMultiplier;
 public float WeaponDamageMinimum=>EquippedDamageWeapon?EquippedDamageWeapon.BaseDamageMinimum*EquippedWeaponPower:AutoProfile.Damage;
 public float WeaponDamageMaximum=>EquippedDamageWeapon?EquippedDamageWeapon.BaseDamageMaximum*EquippedWeaponPower:AutoProfile.Damage;
 public float AutoInterval=>Prototype&&EquippedDamageWeapon?EquippedDamageWeapon.SwingDuration:AutoProfile.Interval;
 public float WeaponDamagePerSecond=>(WeaponDamageMinimum+WeaponDamageMaximum)*.5f/Mathf.Max(.2f,AutoInterval);
 public string EquippedWeaponSummary=>EquippedDamageWeapon?EquippedDamageWeapon.DamageSummary(EquippedWeaponPower):"Daño: "+AutoProfile.Damage+"\nVelocidad: "+AutoInterval.ToString("0.##")+" s";
 // One server roll is captured at action acceptance and reused for every victim/tick.
 float RollWeaponDamage()=>Mathf.Lerp(WeaponDamageMinimum,WeaponDamageMaximum,Random.value);
 public float AbilityDamageBasis(AbilityDefinition a,float weaponRoll)=>a.UsesWeaponDamage?Mathf.Max(0,weaponRoll*Mathf.Max(0,a.WeaponCoefficient)+a.WeaponBonus):a.Damage*WeaponMultiplier;
 public string AbilityDamageSummary(AbilityDefinition a){if(!a||a.Shape==AbilityShape.Self)return "Sin daño directo";return a.UsesWeaponDamage?"Daño: "+AbilityDamageBasis(a,WeaponDamageMinimum).ToString("0.##")+"–"+AbilityDamageBasis(a,WeaponDamageMaximum).ToString("0.##")+" por impacto\nArma ×"+a.WeaponCoefficient.ToString("0.##")+" + "+a.WeaponBonus.ToString("0.##")+" (antes de talentos, crítico y defensa)":"Daño base: "+a.Damage.ToString("0.##");}
}
}
