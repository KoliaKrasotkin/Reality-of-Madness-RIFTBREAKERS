using UnityEngine;
using Mirror;

namespace MultiFPS.Gameplay.Gamemodes
{
    public class CheckpointZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkServer.active)
                return;

            CharacterInstance character =
                other.transform.root.GetComponentInChildren<CharacterInstance>();

            if (character == null)
                return;

            PlayerInstance[] players = FindObjectsByType<PlayerInstance>(FindObjectsSortMode.None);
            foreach (PlayerInstance player in players)
            {
                if (player == null || player.BOT)
                    continue;

                if (player.MyCharacter == character)
                {
                    Raid raid = GameManager.Gamemode as Raid;

                    if (raid == null)
                    {
                        Debug.LogError("Raid: no se encontró el modo Raid activo.");
                        return;
                    }

                    raid.RegisterCheckpoint(player, transform);

                    Debug.Log("Raid: jugador alcanzó y registró un checkpoint.");
                    return;
                }
            }
        }
    }
}