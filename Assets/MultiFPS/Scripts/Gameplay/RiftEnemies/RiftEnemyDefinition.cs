using UnityEngine;
namespace MultiFPS.Gameplay.RiftEnemies {
public enum RiftEnemySkill {None,EmberBolt,Cleave,Drain,Slam,VoidNova}
public enum RiftEnemyDisposition {Hostile,Defensive,Timid}
[CreateAssetMenu(menuName="ROM/Enemies/Enemy definition")]
public sealed class RiftEnemyDefinition:ScriptableObject {
 public string Id="little-demon",DisplayName="Diablillo",Role="Enemigo común";
 public RiftEnemyAgent Prefab;
 [Min(1)]public int Health=70,Damage=5,Experience=20,Gold=8;
 [Min(.1f)]public float MoveSpeed=2.4f,Detection=22,AttackRange=1.65f,Windup=.55f,Cooldown=1.8f,Leash=36,CorpseSeconds=5;
 public float WalkAnimationSpeed=1.15f;
 public int Team=1;public bool IsWildlife,IsBoss,Ranged;
 public RiftEnemyDisposition Disposition;public float WanderRadius=12;
 public RiftEnemySkill Skill;public string SkillName;
 public float SkillRange=8,SkillRadius=3.5f,SkillWindup=1,SkillCooldown=7,SkillMultiplier=1.8f;
 public Color SkillColor=new Color(1,.2f,.05f);
}}
