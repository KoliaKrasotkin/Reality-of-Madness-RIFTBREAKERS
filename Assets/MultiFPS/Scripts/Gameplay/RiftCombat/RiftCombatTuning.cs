using System;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
[Serializable]public sealed class AutoAttackProfile {
 public string ClassKey,DisplayName;public int Damage=18;public float Interval=1.8f,Range=3.4f,Arc=100,Windup=.28f,ResourceGain=8;
 [Range(0,1)]public float CriticalChance=.15f;public float CriticalMultiplier=2;
}
[CreateAssetMenu(menuName="ROM/Combat/Auto attacks and criticals")]
public sealed class RiftCombatTuning:ScriptableObject {
 public AutoAttackProfile[] Profiles={new AutoAttackProfile{ClassKey="warrior",DisplayName="Golpe de arma"},new AutoAttackProfile{ClassKey="paladin",DisplayName="Golpe sagrado",Damage=16},new AutoAttackProfile{ClassKey="bastion",DisplayName="Golpe pesado",Damage=20,Interval=2.1f},new AutoAttackProfile{ClassKey="battlemage",DisplayName="Descarga arcana",Damage=15,Range=25,Interval=1.8f,ResourceGain=0},new AutoAttackProfile{ClassKey="veilhunter",DisplayName="Disparo certero",Damage=14,Range=30,Interval=1.5f,ResourceGain=0}};
 static RiftCombatTuning cached;public static RiftCombatTuning Load {get{if(!cached)cached=Resources.Load<RiftCombatTuning>("RiftCombatTuning");if(!cached)cached=CreateInstance<RiftCombatTuning>();return cached;}}
 public AutoAttackProfile Find(string key){foreach(var p in Profiles)if(p!=null&&p.ClassKey==key)return p;return Profiles[0];}
}}
