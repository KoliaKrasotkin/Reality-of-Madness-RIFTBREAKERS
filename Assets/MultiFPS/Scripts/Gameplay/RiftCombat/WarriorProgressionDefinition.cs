using System;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
[Serializable]public sealed class WarriorTalent {
 public string Id,Name,Description,Branch;public int Parent=-1,RequiredLevel=2,MaximumRank=3;
}
[CreateAssetMenu(menuName="ROM/Combat/Warrior progression")]
public sealed class WarriorProgressionDefinition:ScriptableObject {
 public int MaximumLevel=20,KillExperience=35,BossExperience=150,EncounterExperience=75,CrystalExperience=60,QuestExperience=250;
 public float DamagePerLevel=.02f,HealthPerLevel=.08f;
 public WeaponDefinition Sword;public GameObject Shield;public Vector3 ShieldPosition=new Vector3(0,-.12f,0),ShieldRotation=new Vector3(0,0,90);public Vector3 ShieldScale=Vector3.one;
 public WarriorTalent[] Talents={
 new WarriorTalent{Id="arms",Name="Disciplina de armas",Description="+3% de daño por rango.",Branch="Armas"},
 new WarriorTalent{Id="wide",Name="Hoja amplia",Description="Barrido y Furia: +15° de arco y +0,3 m por rango.",Branch="Armas",Parent=0,RequiredLevel=5},
 new WarriorTalent{Id="precision",Name="Instinto ejecutor",Description="+1% de crítico y +5% de daño de Barrido por rango.",Branch="Armas",Parent=1,RequiredLevel=10},
 new WarriorTalent{Id="iron",Name="Piel de hierro",Description="+2% de reducción de daño por rango.",Branch="Protección"},
 new WarriorTalent{Id="vitality",Name="Tenacidad",Description="+5% de vida máxima por rango.",Branch="Protección",Parent=3,RequiredLevel=5},
 new WarriorTalent{Id="shield",Name="Maestría del escudo",Description="Con escudo: +3% de reducción y -5% de recarga de Última resistencia por rango.",Branch="Protección",Parent=4,RequiredLevel=10},
 new WarriorTalent{Id="rage",Name="Corazón de furia",Description="+10% de generación de Furia por rango.",Branch="Furia"},
 new WarriorTalent{Id="earth",Name="Golpe sísmico",Description="Rompetierra: +8% de daño y +0,1 s de stagger por rango.",Branch="Furia",Parent=6,RequiredLevel=5},
 new WarriorTalent{Id="rhythm",Name="Ritmo de batalla",Description="-3% de recarga de habilidades por rango.",Branch="Furia",Parent=7,RequiredLevel=10}};
 static WarriorProgressionDefinition cached;public static WarriorProgressionDefinition Load {get{if(!cached)cached=Resources.Load<WarriorProgressionDefinition>("WarriorProgression");if(!cached)cached=CreateInstance<WarriorProgressionDefinition>();return cached;}}
 public int Required(int level)=>Mathf.Max(1,100+50*(level-1)+25*(level-1)*(level-1));
 public int Level(int total){int level=1;while(level<MaximumLevel&&total>=Required(level)){total-=Required(level);level++;}return level;}
 public int InLevel(int total){int level=1;while(level<MaximumLevel&&total>=Required(level)){total-=Required(level);level++;}return level>=MaximumLevel?0:total;}
 public int MaximumExperience {get{int sum=0;for(int i=1;i<MaximumLevel;i++)sum+=Required(i);return sum;}}
}}
