using System.Collections;
using UnityEngine;
using Mirror;
using MultiFPS.Gameplay;
using System.Collections.Generic;

public class RaidWaveManager : NetworkBehaviour
{
    [SerializeField] private RaidWaveData[] waves;
    private MultiFPS.Gameplay.Gamemodes.RaidEnemySpawner enemySpawner;

    private int enemiesAlive = 0;
    public int EnemiesAlive => enemiesAlive;
    private int currentWaveIndex = 0;
    private bool isSpawningWave = false;

    private readonly HashSet<Health> registeredEnemies = new HashSet<Health>();
    private readonly HashSet<Health> defeatedEnemies = new HashSet<Health>();

    public override void OnStartServer()
    {
        base.OnStartServer();

        enemySpawner = FindFirstObjectByType<
            MultiFPS.Gameplay.Gamemodes.RaidEnemySpawner>();
        Debug.Log("Asalto: spawner encontrado = " + (enemySpawner != null));

        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<
                MultiFPS.Gameplay.Gamemodes.RaidEnemySpawner>();
        }

        if (enemySpawner == null)
        {
            Debug.LogError("Asalto: no se encontró ningún spawner en la escena.");
            return;
        }
    }

    public void RegisterEnemy(Health enemyHealth)
    {
        if (!isServer || enemyHealth == null)
            return;

        // Cada personaje se registra una sola vez.
        if (!registeredEnemies.Add(enemyHealth))
            return;

        enemyHealth.Server_OnHealthDepleted +=
            (part, attackType, killer, attackForce) =>
                OnEnemyDeath(enemyHealth);

        enemiesAlive++;

        Debug.Log("Asalto: enemigos vivos = " + enemiesAlive);
    }

    public void StartRaid()
    {
        if (!isServer)
            return;

        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("Asalto: no hay oleadas configuradas.");
            return;
        }

        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<
                MultiFPS.Gameplay.Gamemodes.RaidEnemySpawner>();
        }

        if (enemySpawner == null)
        {
            Debug.LogError("Asalto: no hay un spawner disponible.");
            return;
        }

        Debug.Log("Asalto: sistema de oleadas iniciado.");

        StartWave(0);
    }

    private void StartWave(int waveIndex)
    {
        if (!isServer || waveIndex < 0 || waveIndex >= waves.Length)
            return;

        currentWaveIndex = waveIndex;
        isSpawningWave = true;

        Debug.Log("Asalto: iniciando oleada " + (currentWaveIndex + 1));

        StartCoroutine(SpawnWaveCoroutine(waves[currentWaveIndex]));
    }

    private void StartNextWave()
    {
        int nextWaveIndex = currentWaveIndex + 1;

        if (nextWaveIndex >= waves.Length)
        {
            Debug.Log("Asalto: todas las oleadas fueron completadas.");
            return;
        }

        StartWave(nextWaveIndex);
    }

    private IEnumerator SpawnWaveCoroutine(RaidWaveData wave)
    {
        yield return new WaitForSeconds(wave.timeBeforeWave);
        Debug.Log("Asalto: terminó la espera de la oleada " + wave.name);
        Debug.Log("Asalto: grupos configurados = " +
         (wave.enemies == null ? "NULL" : wave.enemies.Count.ToString()));

        foreach (RaidEnemyData enemyData in wave.enemies)
        {
            Debug.Log("Asalto: oleada " + wave.name
    + " | prefab = " + (enemyData.enemyPrefab != null
        ? enemyData.enemyPrefab.name
        : "NULL")
    + " | cantidad = " + enemyData.amount);
            for (int i = 0; i < enemyData.amount; i++)
            {

                enemySpawner.SpawnEnemy(enemyData.enemyPrefab);

                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }

        Debug.Log("Asalto: oleada generada.");

        isSpawningWave = false;

        if (enemiesAlive == 0)
        {
            StartNextWave();
        }
    }

    private void OnEnemyDeath(Health enemyHealth)
    {
        if (!isServer || enemyHealth == null)
            return;

        // Impide descontar varias veces la misma muerte.
        if (!defeatedEnemies.Add(enemyHealth))
            return;

        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);

        Debug.Log("Asalto: enemigo eliminado. Vivos = " + enemiesAlive);
        if (enemiesAlive == 0 && !isSpawningWave)
        {
            StartNextWave();
        }

        if (enemiesAlive == 0)
        {
            Debug.Log("Asalto: todos los enemigos de la oleada fueron eliminados.");
        }
    }
}