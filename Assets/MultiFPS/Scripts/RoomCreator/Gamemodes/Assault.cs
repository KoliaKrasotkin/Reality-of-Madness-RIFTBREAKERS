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

            if (WaveManager == null)
            {
                Debug.LogError("Asalto: falta AssaultWaveManager en el objeto del modo Assault.");
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

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
