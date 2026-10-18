using Mirror;
using UnityEngine;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.Gamemodes;

namespace ROM.Gameplay
{
    /// <summary>
    /// Co-op incursion: one squad, no respawn.
    ///
    /// This is the gamemode the downed state needs in order to mean anything. The
    /// kit is built around respawning — a cooldown and then you are back, or you
    /// take over one of your own bots — and all of that is an escape hatch out of
    /// being downed. Here a Redhead enters the incursion once, and the only way
    /// back up is a teammate picking them up.
    ///
    /// The GDD asks for composition over separate base code, so the type of
    /// incursion (raid, recovery, investigation) is meant to become data on this
    /// one gamemode rather than three subclasses of it.
    ///
    /// Goes on the ROOMMANAGER object, like every gamemode of the kit.
    /// </summary>
    [AddComponentMenu("ROM/gamemodes/Incursion")]
    public class IncursionGameMode : Gamemode
    {
        /// <summary>
        /// The squad is a single team. The base class assigns team 0 to anyone who
        /// arrives without a team, so this is the one it lands on.
        /// </summary>
        public const int SquadTeam = 0;

        [Tooltip("Loguea en consola las entradas a la incursión y los pedidos de reaparición denegados. Sirve para verificar el comportamiento; se puede apagar cuando el sistema esté estable.")]
        [SerializeField] bool _logChanges = true;

        protected override void Awake()
        {
            base.Awake();

            Indicator = Gamemodes.Incursion;

            //one squad against the dimension, not players against each other
            FFA = false;
            FriendyFire = false;

            DenyRespawn();
        }

        /// <summary>
        /// The base class turns respawning back on when the match starts waiting for
        /// players, so it has to be denied again afterwards.
        /// </summary>
        protected override void MatchEvent_StartWaitingForPlayers()
        {
            base.MatchEvent_StartWaitingForPlayers();
            DenyRespawn();
        }

        /// <summary>
        /// Same during warmup.
        /// </summary>
        protected override void MatchEvent_StartWarmup()
        {
            base.MatchEvent_StartWarmup();
            DenyRespawn();
        }

        /// <summary>
        /// The squad fights whatever the incursion spawns, not PvP filler bots.
        /// </summary>
        protected override bool AllowBotFilling { get { return false; } }

        public override void Server_OnPlayerInstanceAdded(PlayerInstance player)
        {
            //the base assigns a team to whoever arrives without one and then asks
            //for a spawn, which is refused here, so the squad is put in by hand
            base.Server_OnPlayerInstanceAdded(player);

            if (!isServer) return;

            Server_EnterIncursion(player);
        }

        /// <summary>
        /// Refuses every respawn request, which is the whole point of the mode.
        ///
        /// With respawning denied this should not be reached, so it logs instead of
        /// silently doing nothing: being called means something is asking for a
        /// respawn through a path that was not expected, and that is worth seeing.
        /// </summary>
        public override void PlayerSpawnCharacterRequest(PlayerInstance playerInstance)
        {
            if (!_logChanges || playerInstance == null) return;

            Debug.Log($"[ROM] Pedido de reaparición denegado para " +
                      $"{playerInstance.PlayerInfo.Username}: en una incursión no se reaparece.", this);
        }

        /// <summary>
        /// Puts a player into the incursion. Covers both the squad at the start and
        /// anyone connecting while it is already running: there is no difference
        /// between the two yet, because entering late as a spectator is a decision
        /// that has not been made.
        /// </summary>
        [Server]
        void Server_EnterIncursion(PlayerInstance player)
        {
            if (player == null) return;

            //enemies are not players: whatever spawns them owns their lifecycle
            if (player.BOT) return;

            if (defaultSpawnPoints == null)
            {
                Debug.LogError($"[ROM] {nameof(IncursionGameMode)} no tiene spawnpoints asignados, " +
                               $"así que nadie puede entrar a la incursión.", this);
                return;
            }

            player.Server_SpawnCharacter(defaultSpawnPoints.GetNextSpawnPoint());

            if (_logChanges)
                Debug.Log($"[ROM] {player.PlayerInfo.Username} entró a la incursión.", this);
        }

        void DenyRespawn()
        {
            //running out of health means downed, and getting back up means a
            //teammate picked you up
            LetPlayersSpawnOnTheirOwn = false;

            //taking over one of your own bots on death is the other way the kit
            //hands control back, and it would skip the downed state entirely
            LetPlayersTakeControlOverBots = false;
        }
    }
}
