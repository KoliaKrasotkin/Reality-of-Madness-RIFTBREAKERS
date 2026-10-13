
using UnityEngine;
using Mirror;

namespace MultiFPS.Gameplay.Gamemodes
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject enemyPrefab;

        public void SpawnEnemy()
        {
            Debug.Log("EnemySpawner: se solicitó generar el enemigo.");
            SpawnEnemy(enemyPrefab);
        }

        public void SpawnEnemy(GameObject prefab)
        {
            if (!NetworkServer.active)
                return;

            if (spawnPoint == null)
            {
                Debug.LogError("EnemySpawner: falta asignar el punto de aparición.", this);
                return;
            }

            if (prefab == null)
            {
                Debug.LogError("EnemySpawner: falta asignar el prefab del enemigo.", this);
                return;
            }

            if (prefab.GetComponent<NetworkIdentity>() == null)
            {
                Debug.LogError(
                    $"EnemySpawner: {prefab.name} necesita NetworkIdentity en la raíz.",
                    this
                );
                return;
            }

            GameObject enemy = Instantiate(
                prefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            NetworkServer.Spawn(enemy);
        }
    }
}
