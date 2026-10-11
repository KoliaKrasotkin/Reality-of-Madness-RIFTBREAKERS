using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace MultiFPS.Gameplay.Gamemodes
{
    public class RaidEnemySpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private GameObject enemyPrefab;

        private int nextSpawnPointIndex = 0;

        private Transform[] selectedSpawnPoints;

        [SerializeField] private float minDistanceFromPlayer = 15f;
        [SerializeField] private float minDistanceBetweenEnemyPoints = 8f;

        private bool SelectEnemySpawnPoints()
        {
            if (selectedSpawnPoints != null &&
                selectedSpawnPoints.Length == 4)
                return true;

            Raid raid = FindFirstObjectByType<Raid>();

            if (raid == null ||
                raid.SelectedPlayerSpawnZone == null)
            {
               
                return false;
            }

            if (spawnPoints == null || spawnPoints.Length < 4)
            {
               
                return false;
            }

            Transform playerZone = raid.SelectedPlayerSpawnZone;
            List<Transform> candidates = new List<Transform>();

            foreach (Transform point in spawnPoints)
            {
                if (point == null)
                    continue;

                bool tooCloseToPlayer = false;

                for (int i = 0; i < playerZone.childCount; i++)
                {
                    Transform playerPoint = playerZone.GetChild(i);

                    if (Vector3.Distance(
                        point.position, playerPoint.position)
                        < minDistanceFromPlayer)
                    {
                        tooCloseToPlayer = true;
                        break;
                    }
                }

                if (!tooCloseToPlayer)
                    candidates.Add(point);
            }

            if (candidates.Count < 4)
            {
                
                return false;
            }

            for (int attempt = 0; attempt < 100; attempt++)
            {
                List<Transform> shuffled = new List<Transform>(candidates);

                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);

                    Transform temp = shuffled[i];
                    shuffled[i] = shuffled[j];
                    shuffled[j] = temp;
                }

                List<Transform> selected = new List<Transform>();

                foreach (Transform point in shuffled)
                {
                    bool tooCloseToAnotherEnemy = false;

                    foreach (Transform chosen in selected)
                    {
                        if (Vector3.Distance(
                            point.position, chosen.position)
                            < minDistanceBetweenEnemyPoints)
                        {
                            tooCloseToAnotherEnemy = true;
                            break;
                        }
                    }

                    if (!tooCloseToAnotherEnemy)
                        selected.Add(point);

                    if (selected.Count == 4)
                        break;
                }

                if (selected.Count == 4)
                {

                    selectedSpawnPoints = selected.ToArray();
                    nextSpawnPointIndex = 0;

                    return true;
                }
            }

           
            return false;
        }
        public Transform GetNextEnemySpawnPoint()
        {
            if ((selectedSpawnPoints == null ||
                 selectedSpawnPoints.Length != 4) &&
                !SelectEnemySpawnPoints())
            {
                return null;
            }

            Transform spawnPoint = selectedSpawnPoints[nextSpawnPointIndex];

            nextSpawnPointIndex =
                (nextSpawnPointIndex + 1) % selectedSpawnPoints.Length;

            return spawnPoint;
        }
        public void SpawnEnemy(GameObject enemyPrefab)
        {
            if (!NetworkServer.active)
                return;

            if (enemyPrefab == null)
            {
                Debug.LogError("Raid: falta asignar el prefab de la oleada.");
                return;
            }

            if (enemyPrefab.GetComponent<NetworkIdentity>() == null)
            {
                Debug.LogError(
                    $"Raid: {enemyPrefab.name} necesita NetworkIdentity en la raíz."
                );
                return;
            }

            Health prefabHealth = enemyPrefab.GetComponentInChildren<Health>(true);

            if (prefabHealth == null)
            {
                Debug.LogError(
                    $"Raid: {enemyPrefab.name} no contiene un componente Health."
                );
                return;
            }

            RaidWaveManager waveManager =
                FindFirstObjectByType<RaidWaveManager>();

            if (waveManager == null)
            {
                Debug.LogError("Raid: no se encontró RaidWaveManager.");
                return;
            }

            Transform spawnPoint = GetNextEnemySpawnPoint();

            if (spawnPoint == null)
                return;

            GameObject enemy = Instantiate(
                enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            NetworkServer.Spawn(enemy);

            Health enemyHealth = enemy.GetComponentInChildren<Health>(true);

            if (enemyHealth == null)
            {
                NetworkServer.Destroy(enemy);
                return;
            }

            waveManager.RegisterEnemy(enemyHealth);
        }


        
        [ContextMenu("Prueba/Generar un enemigo")]
        private void TestSpawnEnemy()
        {
            SpawnEnemy(enemyPrefab);
        }
    }
}