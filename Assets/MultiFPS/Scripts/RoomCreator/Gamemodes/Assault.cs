using UnityEngine;

namespace MultiFPS.Gameplay.Gamemodes
{
    [AddComponentMenu("MultiFPS/gamemodes/Assault")]
    public class Assault : Gamemode 
    {
        protected override void Awake()
        {
           base.Awake();

            Indicator = Gamemodes.Assault;
            FFA = false;
            FriendyFire = false;
            LetPlayersSpawnOnTheirOwn = true;
        }

        /*public override void Server_OnPlayerInstanceAdded(PlayerInstance player)
        {
            if (!isServer) return;
            if (player.BOT)
                AssignPlayerToTeam(player, 1);
            else
                AssignPlayerToTeam(player, 0);
        }*/
    }


}
