using System;
using UnityEngine;
using MultiFPS.Gameplay.RiftEnemies;
namespace ROM.Horde {
[Serializable]public sealed class RiftEnemySpawnChoice {public RiftEnemyDefinition Enemy;[Min(.01f)]public float Weight=1;}
public sealed class RiftEnemySpawnZone:MonoBehaviour {
 public string Title,RegionTitle;[Range(1,60)]public int MinimumLevel=1,MaximumLevel=1;[Min(0)]public float LeashOverride;
 public int RollLevel(){int min=RiftEnemyLevelTuning.Load.ClampLevel(MinimumLevel),max=RiftEnemyLevelTuning.Load.ClampLevel(Mathf.Max(MinimumLevel,MaximumLevel));return UnityEngine.Random.Range(min,max+1);}
 public string LevelRange=>"Nv. "+Mathf.Min(MinimumLevel,MaximumLevel)+(MinimumLevel==MaximumLevel?"":"-"+Mathf.Max(MinimumLevel,MaximumLevel));
 public Transform[] SpawnPoints;public RiftEnemySpawnChoice[] Enemies;
 public RiftEncounter Encounter;public float ActivationRadius=135,RetireRadius=280,RespawnSeconds=90;
 [NonSerialized]public int Spawned,Defeated;[NonSerialized]public double RespawnAt;
 public int Count=>SpawnPoints==null?0:SpawnPoints.Length;
 public bool Cleared=>Count>0&&Defeated>=Count;
 public RiftEnemyDefinition Choose(){float total=0;if(Enemies==null)return null;foreach(var entry in Enemies)if(entry.Enemy&&entry.Enemy.Prefab)total+=Mathf.Max(0,entry.Weight);if(total<=0)return null;float pick=UnityEngine.Random.value*total;foreach(var entry in Enemies){if(!entry.Enemy||!entry.Enemy.Prefab||entry.Weight<=0)continue;pick-=Mathf.Max(0,entry.Weight);if(pick<=0)return entry.Enemy;}return null;}
}
}
