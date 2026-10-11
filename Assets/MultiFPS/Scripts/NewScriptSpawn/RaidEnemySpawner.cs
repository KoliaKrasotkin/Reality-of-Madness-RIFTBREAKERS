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
                Debug.LogError(
                    "Asalto: no se encontró la zona inicial del jugador.");
                return false;
            }

            if (spawnPoints == null || spawnPoints.Length < 4)
            {
                Debug.LogError(
                    "Asalto: se necesitan al menos 4 puntos candidatos.");
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
                Debug.LogError(
                    "Asalto: no hay 4 puntos suficientemente alejados " +
                    "de la zona del jugador.");
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

            Debug.LogError(
                "Asalto: no se encontraron 4 puntos que respeten " +
                "las distancias configuradas.");

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

            if (!SelectEnemySpawnPoints())
                return;

            Raid raid = FindFirstObjectByType<Raid>();

            if (raid == null)
            {
                Debug.LogError("Asalto: no se encontró el modo Assault.");
                return;
            }

            PlayerInstance bot = raid.SpawnBot(1);

            if (bot == null)
            {
                Debug.LogError("Asalto: no se pudo crear el bot enemigo.");
                return;
            }

            Debug.Log("Asalto: bot enemigo solicitado por AssaultEnemySpawner.");
        }
        [ContextMenu("Prueba/Generar un enemigo")]
        private void TestSpawnEnemy()
        {
            SpawnEnemy(enemyPrefab);
        }
    }
}