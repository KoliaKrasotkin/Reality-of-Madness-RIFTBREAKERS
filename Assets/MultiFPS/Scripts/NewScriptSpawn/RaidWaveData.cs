using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

[MovedFrom(true, sourceClassName: "AssaultWaveData")]
[CreateAssetMenu(fileName = "RaidWaveData", menuName = "Raid/Wave Data")]
public class RaidWaveData : ScriptableObject
{
    public float timeBeforeWave = 5f;
    public float spawnInterval = 1f;

    public List<RaidEnemyData> enemies = new List<RaidEnemyData>();
}

[Serializable]
[MovedFrom(true, sourceClassName: "AssaultEnemyData")]
public class RaidEnemyData
{
    public GameObject enemyPrefab;
    public int amount = 1;
}