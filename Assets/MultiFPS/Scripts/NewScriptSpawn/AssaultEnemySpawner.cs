using UnityEngine;
using Mirror;

namespace MultiFPS.Gameplay.Gamemodes
{
    public class AssaultEnemySpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private GameObject enemyPrefab;

        private int nextSpawnPointIndex = 0;
        public void SpawnEnemy(GameObject enemyPrefab)
        {

            if (!NetworkServer.active || enemyPrefab == null)
                return;

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("Asalto: no hay puntos de aparición asignados.");
                return;
            }

            Transform spawnPoint = spawnPoints[nextSpawnPointIndex];

            nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Length;
            GameObject enemy = Instantiate(
                enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            NetworkIdentity networkIdentity = enemy.GetComponent<NetworkIdentity>();

            if (networkIdentity == null)
            {
                Debug.LogError(
                    "Asalto: el prefab no tiene NetworkIdentity: " + enemyPrefab.name
                );

                Destroy(enemy);
                return;
            }

            NetworkServer.Spawn(enemy);
            Health enemyHealth = enemy.GetComponent<Health>();

            if (enemyHealth != null)
            {
                AssaultWaveManager waveManager =
                    FindFirstObjectByType<AssaultWaveManager>();

                if (waveManager != null)
                    waveManager.RegisterEnemy(enemyHealth);
                else
                    Debug.LogError("Asalto: no se encontró AssaultWaveManager.");
            }
            else
            {
                Debug.LogError("Asalto: el enemigo no tiene Health en el objeto raíz.");
            }

            Debug.Log("Asalto: enemigo creado: " + enemyPrefab.name);


        }
        [ContextMenu("Prueba/Generar un enemigo")]
        private void TestSpawnEnemy()
        {
            SpawnEnemy(enemyPrefab);
        }
    }
}