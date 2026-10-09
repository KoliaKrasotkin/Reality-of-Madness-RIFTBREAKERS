using UnityEngine;

namespace MultiFPS.Gameplay.Gamemodes
{
    [AddComponentMenu("MultiFPS/gamemodes/Assault")]
    public class Assault : Gamemode 
    {
        public AssaultWaveManager WaveManager { get; private set; }
        protected override void Awake()
        {
           base.Awake();


            Indicator = Gamemodes.Assault;
            FFA = false;
            FriendyFire = false;
            LetPlayersSpawnOnTheirOwn = true;

         WaveManager = GetComponent<AssaultWaveManager>();
        }

       
    }


}
