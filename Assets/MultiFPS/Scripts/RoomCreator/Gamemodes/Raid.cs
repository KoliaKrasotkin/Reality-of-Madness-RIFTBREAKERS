
using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace MultiFPS.Gameplay.Gamemodes
{
    [AddComponentMenu("MultiFPS/gamemodes/Raid")]
    public class Raid : Gamemode
    {
        [SerializeField] private PlayerSpawn playerSpawn;
        [SerializeField] private SpawnpointsContainer spawnpointsContainer;

        private readonly Dictionary<PlayerInstance, Transform> playerCheckpoints =
            new Dictionary<PlayerInstance, Transform>();

        protected override void Awake()
        {
            base.Awake();

            Indicator = Gamemodes.Raid;
            FFA = false;
            FriendyFire = false;
            LetPlayersSpawnOnTheirOwn = true;
        }

        public void RegisterCheckpoint(PlayerInstance player, Transform checkpoint)
        {
            if (!NetworkServer.active || player == null || checkpoint == null)
                return;

            playerCheckpoints[player] = checkpoint;

            Debug.Log("Raid: checkpoint registrado para un jugador.");
        }

        public override void PlayerSpawnCharacterRequest(PlayerInstance playerInstance)
        {
            if (playerInstance == null)
                return;

            if (playerCheckpoints.TryGetValue(playerInstance, out Transform checkpoint)
                && checkpoint != null)
            {
                playerInstance.Server_SpawnCharacter(checkpoint);
                Debug.Log("Raid: jugador reaparece en su último checkpoint.");
                return;
            }

            if (spawnpointsContainer != null)
            {
                Transform spawnPoint = spawnpointsContainer.GetNextSpawnPoint();

                if (spawnPoint != null)
                {
                    playerInstance.Server_SpawnCharacter(spawnPoint);
                    return;
                }
            }

            if (playerSpawn == null)
            {
                Debug.LogError("Raid: no hay puntos de aparición asignados.");
                return;
            }

            playerInstance.Server_SpawnCharacter(playerSpawn.transform);
        }


        public void UnregisterCheckpoint(PlayerInstance player)
        {
            if (!NetworkServer.active || player == null)
                return;

            if (playerCheckpoints.Remove(player))
            {
                Debug.Log("Raid: checkpoint eliminado para un jugador desconectado.");
            }
        }


        public override void OnStartServer()
        {
            base.OnStartServer();
        }
    }
}
