using UnityEngine;
using Mirror;

namespace MultiFPS.Gameplay.Gamemodes
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Configuracion")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject enemyPrefab;

        [Header("Skin")]
        [Tooltip("Indice dentro de ClientInterfaceManager.characterSkins (el array de SkinContainer). -1 = no cambiar la skin")]
        [SerializeField] private int skinID = -1;

        public void SpawnEnemy()
        {
            Debug.Log("EnemySpawner: se solicito generar el enemigo.");
            SpawnEnemy(enemyPrefab, skinID);
        }

        public void SpawnEnemy(GameObject prefab)
        {
            SpawnEnemy(prefab, skinID);
        }

        public void SpawnEnemy(GameObject prefab, int skin)
        {
            if (!NetworkServer.active)
                return;

            if (spawnPoint == null)
            {
                Debug.LogError("EnemySpawner: falta asignar el punto de aparicion.", this);
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
                    $"EnemySpawner: {prefab.name} necesita NetworkIdentity en la raiz.",
                    this
                );
                return;
            }

            GameObject enemy = Instantiate(
                prefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            // La skin se fija ANTES del Spawn para que viaje en el estado inicial a todos los clientes
            if (skin >= 0)
            {
                CharacterInstance character = enemy.GetComponentInChildren<CharacterInstance>();

                if (character != null)
                    character.Server_SetForcedSkin(skin);
                else
                    Debug.LogWarning(
                        $"EnemySpawner: {prefab.name} no tiene CharacterInstance, no se pudo aplicar la skin {skin}.",
                        this
                    );
            }

            NetworkServer.Spawn(enemy);
        }
    }
}