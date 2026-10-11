using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace MultiFPS.Gameplay.Gamemodes
{
    [AddComponentMenu("MultiFPS/gamemodes/Raid")]
    public class Raid : Gamemode 
    {
        public RaidWaveManager WaveManager { get; private set; }
        [SerializeField] private Transform[] playerSpawnZones;

        private Transform selectedPlayerSpawnZone;
        private int nextPlayerSpawnPointIndex = 0;
      
        [SerializeField] private GameObject objectivePrefab;
        [SerializeField] private Transform[] objectiveSpawnPoints;
        
        private bool objectiveSpawned;

        public Transform SelectedPlayerSpawnZone => selectedPlayerSpawnZone;
        protected override void Awake()
        {
           base.Awake();


            Indicator = Gamemodes.Raid;
            FFA = false;
            FriendyFire = false;
            LetPlayersSpawnOnTheirOwn = true;

            WaveManager = GetComponent<RaidWaveManager>();
                       
        }

        public override void PlayerSpawnCharacterRequest(PlayerInstance playerInstance)
        {
            if (playerInstance.BOT)
            {
                RaidEnemySpawner enemySpawner =
                    FindFirstObjectByType<RaidEnemySpawner>();

                if (enemySpawner == null)
                {
                    return;
                }

                Transform enemySpawnPoint = enemySpawner.GetNextEnemySpawnPoint();

                if (enemySpawnPoint == null)
                {
                    return;
                }

                playerInstance.Server_SpawnCharacter(enemySpawnPoint);
                return;
            }
            if (selectedPlayerSpawnZone == null)
            {
                if (playerSpawnZones == null || playerSpawnZones.Length == 0)
                {

                    base.PlayerSpawnCharacterRequest(playerInstance);
                    return;
                }

                selectedPlayerSpawnZone =
                    playerSpawnZones[Random.Range(0, playerSpawnZones.Length)];

                nextPlayerSpawnPointIndex = 0;

            }

            if (selectedPlayerSpawnZone == null ||
                selectedPlayerSpawnZone.childCount == 0)
            {
                base.PlayerSpawnCharacterRequest(playerInstance);
                return;
            }

            Transform spawnPoint = selectedPlayerSpawnZone.GetChild(
                nextPlayerSpawnPointIndex % selectedPlayerSpawnZone.childCount);

            nextPlayerSpawnPointIndex++;
          

            playerInstance.Server_SpawnCharacter(spawnPoint);
        }

        private void SpawnObjective()
        {
            if (!isServer || objectiveSpawned)
                return;

            if (objectivePrefab == null)
            {
                return;
            }

            if (objectiveSpawnPoints == null ||
                objectiveSpawnPoints.Length == 0)
            {
                return;
            }

            List<Transform> validPoints = new List<Transform>();

            foreach (Transform point in objectiveSpawnPoints)
            {
                if (point != null)
                    validPoints.Add(point);
            }

            if (validPoints.Count == 0)
            {
                return;
            }

            Transform selectedPoint =
                validPoints[Random.Range(0, validPoints.Count)];

            GameObject objective = Instantiate(
                objectivePrefab,
                selectedPoint.position,
                selectedPoint.rotation
            );

            if (objective.GetComponent<NetworkIdentity>() == null)
            {
                

                Destroy(objective);
                return;
            }

            NetworkServer.Spawn(objective);
            objectiveSpawned = true;

            
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnObjective();

            if (WaveManager == null)
            {
                
                return;
            }

            
            WaveManager.StartRaid();
        }


    }


}
