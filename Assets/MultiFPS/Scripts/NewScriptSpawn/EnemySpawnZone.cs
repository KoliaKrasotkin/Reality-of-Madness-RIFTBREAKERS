using UnityEngine;
using Mirror;

namespace MultiFPS.Gameplay.Gamemodes
{
    public class EnemySpawnZone : MonoBehaviour
    {
        [SerializeField] private EnemySpawner enemySpawner;
        private bool enemySpawned;

        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkServer.active || enemySpawned)
                return;

            Transform characterRoot = other.transform.root;

            CharacterInstance character =
                characterRoot.GetComponentInChildren<CharacterInstance>();

            if (character == null)
                return;

            if (enemySpawner == null)
            {
                Debug.LogError(
                    "EnemySpawnZone: falta asignar EnemySpawner.",
                    this
                );
                return;
            }

            enemySpawned = true;
            enemySpawner.SpawnEnemy();
        }
    }
}
