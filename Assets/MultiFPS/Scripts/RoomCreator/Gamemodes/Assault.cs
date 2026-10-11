using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace MultiFPS.Gameplay.Gamemodes
{
    [AddComponentMenu("MultiFPS/gamemodes/Assault")]
    public class Assault : Gamemode 
    {
        public AssaultWaveManager WaveManager { get; private set; }
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


            Indicator = Gamemodes.Assault;
            FFA = false;
            FriendyFire = false;
            LetPlayersSpawnOnTheirOwn = true;

            WaveManager = GetComponent<AssaultWaveManager>();

            if (WaveManager == null)
            {
                Debug.LogError("Asalto: falta AssaultWaveManager en el objeto del modo Assault.");
            }
        }

        public override void PlayerSpawnCharacterRequest(PlayerInstance playerInstance)
        {
            if (playerInstance.BOT)
            {
                AssaultEnemySpawner enemySpawner =
                    FindFirstObjectByType<AssaultEnemySpawner>();

                if (enemySpawner == null)
                {
                    Debug.LogError("Asalto: no se encontró AssaultEnemySpawner.");
                    return;
                }

                Transform enemySpawnPoint = enemySpawner.GetNextEnemySpawnPoint();

                if (enemySpawnPoint == null)
                {
                    Debug.LogError("Asalto: no se pudo obtener un punto para el bot.");
                    return;
                }

                playerInstance.Server_SpawnCharacter(enemySpawnPoint);
                return;
            }
            if (selectedPlayerSpawnZone == null)
            {
                if (playerSpawnZones == null || playerSpawnZones.Length == 0)
                {
                    Debug.LogError("Asalto: no hay zonas de aparición asignadas.");

                    base.PlayerSpawnCharacterRequest(playerInstance);
                    return;
                }

                selectedPlayerSpawnZone =
                    playerSpawnZones[Random.Range(0, playerSpawnZones.Length)];

                nextPlayerSpawnPointIndex = 0;

                Debug.Log("Asalto: zona inicial seleccionada: "
                    + selectedPlayerSpawnZone.name);
            }

            if (selectedPlayerSpawnZone == null ||
                selectedPlayerSpawnZone.childCount == 0)
            {
                Debug.LogError("Asalto: la zona seleccionada no tiene puntos.");
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
                Debug.LogError("Asalto: falta asignar el prefab del objetivo.");
                return;
            }

            if (objectiveSpawnPoints == null ||
                objectiveSpawnPoints.Length == 0)
            {
                Debug.LogError("Asalto: no hay puntos para el objetivo.");
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
                Debug.LogError("Asalto: todos los puntos del objetivo son nulos.");
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
                Debug.LogError(
                    "Asalto: el prefab del objetivo necesita NetworkIdentity en la raíz."
                );

                Destroy(objective);
                return;
            }

            NetworkServer.Spawn(objective);
            objectiveSpawned = true;

            Debug.Log(
                "Asalto: objetivo generado en " + selectedPoint.name
            );
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnObjective();

            if (WaveManager == null)
            {
                Debug.LogError("Asalto: no se puede iniciar, falta AssaultWaveManager.");
                return;
            }

            Debug.Log("Asalto: servidor iniciado y administrador de oleadas encontrado.");
            WaveManager.StartAssault();
        }


    }


}
