using UnityEngine;

using System;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "AssaultWaveData", menuName = "Assault/Wave Data")]
public class AssaultWaveData : ScriptableObject
{
    public float timeBeforeWave = 5f;
    public float spawnInterval = 1f;

    public List<AssaultEnemyData> enemies = new List<AssaultEnemyData>();
}

[Serializable]
public class AssaultEnemyData
{
    public GameObject enemyPrefab;
    public int amount = 1;
}