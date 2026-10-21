using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftEnemies;
namespace ROM.Horde {
// Own population budget and prefab selection; never creates a template bot/player.
public sealed class RiftEnemyPopulation:MonoBehaviour {
 public RiftEnemySpawnZone[] Zones;[Range(1,64)]public int MaximumAlive=24;public float SpawnInterval=.5f;
 readonly Dictionary<RiftEnemyAgent,RiftEnemySpawnZone> alive=new Dictionary<RiftEnemyAgent,RiftEnemySpawnZone>();readonly List<RiftEnemyAgent> retired=new List<RiftEnemyAgent>();
 double nextTick;public int AliveCount=>alive.Count;
 float PlayerDistance(Vector3 point){float best=float.PositiveInfinity;foreach(var p in GameManager.Players)if(p&&!p.BOT&&p.MyCharacter&&p.MyCharacter.Health.CurrentHealth>0)best=Mathf.Min(best,(p.MyCharacter.transform.position-point).sqrMagnitude);return Mathf.Sqrt(best);}
 void Update(){if(!NetworkServer.active||!Horde.Instance||!Horde.Instance.ExplorationMode||NetworkTime.time<nextTick)return;nextTick=NetworkTime.time+Mathf.Max(.2f,SpawnInterval);Tick();}
 void Tick(){
  retired.Clear();foreach(var pair in alive){var enemy=pair.Key;if(!enemy||PlayerDistance(pair.Value.transform.position)>Mathf.Max(pair.Value.RetireRadius,pair.Value.ActivationRadius+10))retired.Add(enemy);else if(enemy.Health.CurrentHealth>0){var rules=Horde.Instance.ClassRules;int count=pair.Value.Encounter?pair.Value.Encounter.Participants.Count:1;int max=Mathf.RoundToInt(enemy.Definition.Health*(rules?rules.HealthScale(Mathf.Clamp(count,1,4)):1));if(max!=enemy.Health.MaxHealth)enemy.Health.ServerSetVitals(max,Mathf.Max(1,Mathf.RoundToInt((float)enemy.Health.CurrentHealth/enemy.Health.MaxHealth*max)));enemy.Health.CombatDamageMultiplier=rules?rules.DamageScale(Mathf.Clamp(count,1,4)):1;}}
  foreach(var enemy in retired){if(!alive.TryGetValue(enemy,out var zone))continue;alive.Remove(enemy);zone.Spawned=Mathf.Max(zone.Defeated,zone.Spawned-1);if(zone.Encounter)zone.Encounter.Spawned=zone.Spawned;if(enemy){enemy.ServerKilled-=Killed;enemy.Despawn();}}
  if(Zones==null)return;
  foreach(var zone in Zones){if(!zone||zone.Count==0)continue;if(zone.Cleared&&zone.RespawnAt>0&&NetworkTime.time>=zone.RespawnAt){zone.Spawned=zone.Defeated=0;zone.RespawnAt=0;if(zone.Encounter){zone.Encounter.Spawned=zone.Encounter.Defeated=0;zone.Encounter.Participants.Clear();}}}
  if(alive.Count>=MaximumAlive)return;RiftEnemySpawnZone chosen=null;float closest=float.PositiveInfinity;
  foreach(var zone in Zones){if(!zone||zone.Cleared||zone.Spawned>=zone.Count)continue;float distance=PlayerDistance(zone.transform.position);if(distance<=zone.ActivationRadius&&distance<closest){chosen=zone;closest=distance;}}
  if(chosen)ServerSpawn(chosen);
 }
 public RiftEnemyAgent ServerSpawn(RiftEnemySpawnZone zone){
  if(!NetworkServer.active||!zone||zone.Cleared||zone.Spawned>=zone.Count||alive.Count>=MaximumAlive)return null;var definition=zone.Choose();if(!definition||!definition.Prefab)return null;var point=zone.SpawnPoints[zone.Spawned];if(!point||Horde.Instance.IsSafe(point.position)||!NavMesh.SamplePosition(point.position,out var nav,12,NavMesh.AllAreas)||Horde.Instance.IsSafe(nav.position))return null;
  var enemy=Instantiate(definition.Prefab,nav.position,point.rotation);enemy.Definition=definition;enemy.ServerKilled+=Killed;alive.Add(enemy,zone);NetworkServer.Spawn(enemy.gameObject);zone.Spawned++;if(zone.Encounter)zone.Encounter.Spawned=zone.Spawned;return enemy;
 }
 void Killed(RiftEnemyAgent enemy,Health killer){if(!alive.TryGetValue(enemy,out var zone))return;alive.Remove(enemy);enemy.ServerKilled-=Killed;
  if(!killer||killer==enemy.Health||killer.Team!=Horde.PlayerTeam){zone.Spawned=Mathf.Max(zone.Defeated,zone.Spawned-1);if(zone.Encounter)zone.Encounter.Spawned=zone.Spawned;return;}
  zone.Defeated++;if(zone.Encounter)zone.Encounter.Defeated=zone.Defeated;if(zone.Cleared)zone.RespawnAt=NetworkTime.time+Mathf.Max(10,zone.RespawnSeconds);Horde.Instance.ServerNativeEnemyReward(enemy.Definition,killer,zone.Encounter,zone.Cleared);
 }
 void OnDestroy(){foreach(var pair in alive)if(pair.Key)pair.Key.ServerKilled-=Killed;alive.Clear();}
}
}
