using Mirror;
using UnityEngine;
using MultiFPS;
using ROM.Difficulty;

namespace ROM.Gameplay
{
    /// <summary>
    /// How many revives the squad has left.
    ///
    /// This is what makes difficulty felt rather than configured: the Threat Level
    /// decides whether revives are unlimited, a pool the squad shares, or a quota
    /// per player, and how many. Whoever consumes a revive asks two questions and
    /// never learns which mode is active, so adding or changing a mode does not
    /// touch them.
    ///
    /// Server authoritative. Clients are told what is left so they can show it.
    ///
    /// Goes on the ROOMMANAGER object, next to the gamemode.
    /// </summary>
    [DisallowMultipleComponent]
    public class ReviveBudget : DNNetworkBehaviour
    {
        public static ReviveBudget Instance { private set; get; }

        [Tooltip("Loguea en consola cada consumo y cada negativa. Sirve para verificar el comportamiento; se puede apagar cuando el sistema esté estable.")]
        [SerializeField] bool _logChanges = true;

        public ReviveBudgetMode Mode { private set; get; } = ReviveBudgetMode.Unlimited;

        /// <summary>
        /// Revives the squad has left. Meaningless when the mode is Unlimited, where
        /// <see cref="IsUnlimited"/> answers instead.
        /// </summary>
        public int Remaining { private set; get; }

        public bool IsUnlimited { get { return Mode == ReviveBudgetMode.Unlimited; } }

        /// <summary>True when there are revives left, in whatever mode is active.</summary>
        public bool HasAny { get { return IsUnlimited || Remaining > 0; } }

        public delegate void BudgetChanged(ReviveBudget budget);

        /// <summary>Raised on server and clients whenever what is left changes.</summary>
        public BudgetChanged OnChanged { set; get; }

        void Awake()
        {
            if (!Instance)
                Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #region server
        public override void OnStartServer()
        {
            ThreatLevelController threat = ThreatLevelController.Instance;

            if (threat == null)
            {
                Debug.LogError($"[ROM] {nameof(ReviveBudget)} no encuentra el controlador de " +
                               $"Threat Level, así que no puede saber cuántas revividas hay.", this);
                return;
            }

            threat.OnActiveLevelChanged += Server_OnThreatLevelChanged;

            //also read it right away: the Threat Level may already have been resolved
            //before this component got to subscribe, and depending on which of the
            //two runs first is how a system ends up silently uninitialised
            if (threat.Active != null)
                Server_Apply(threat.Active);
        }

        public override void OnStopServer()
        {
            if (ThreatLevelController.Instance != null)
                ThreatLevelController.Instance.OnActiveLevelChanged -= Server_OnThreatLevelChanged;
        }

        void Server_OnThreatLevelChanged(ThreatLevel level, ThreatLevelDefinition definition)
        {
            Server_Apply(definition);
        }

        [Server]
        void Server_Apply(ThreatLevelDefinition definition)
        {
            if (definition == null) return;

            Mode = definition.BudgetMode;

            switch (Mode)
            {
                case ReviveBudgetMode.Unlimited:
                    Remaining = 0;
                    break;

                case ReviveBudgetMode.SquadPool:
                    Remaining = definition.SquadReviveBudget;
                    break;

                case ReviveBudgetMode.PerPlayer:
                    //failing loudly instead of quietly behaving like another mode: a
                    //difficulty configured with a mode that does not exist yet would
                    //otherwise look like it was working
                    Debug.LogError($"[ROM] {definition.DisplayName} pide revividas por jugador, " +
                                   $"que todavía no está implementado. Se usa el pool de escuadra " +
                                   $"con {definition.SquadReviveBudget} como aproximación.", this);
                    Mode = ReviveBudgetMode.SquadPool;
                    Remaining = definition.SquadReviveBudget;
                    break;
            }

            Log("presupuesto inicial");
            RpcSetBudget(Mode, Remaining);
            OnChanged?.Invoke(this);
        }

        /// <summary>
        /// Takes one revive if there is one to take. Server only.
        ///
        /// This is the atomic moment of the whole budget: it has to be called when a
        /// revive actually completes and not when someone starts holding, or two
        /// players reviving at once could both finish off a single remaining revive.
        /// </summary>
        [Server]
        public bool Server_TryConsume()
        {
            if (IsUnlimited) return true;

            if (Remaining <= 0)
            {
                if (_logChanges)
                    Debug.Log($"[ROM] Revive negado: la escuadra se quedó sin revividas.", this);

                return false;
            }

            Remaining--;

            Log("revivida consumida");
            RpcSetBudget(Mode, Remaining);
            OnChanged?.Invoke(this);

            return true;
        }
        #endregion

        #region client sync
        [ClientRpc]
        void RpcSetBudget(ReviveBudgetMode mode, int remaining)
        {
            if (isServer) return;

            Mode = mode;
            Remaining = remaining;

            OnChanged?.Invoke(this);
        }

        //someone joining mid-incursion has to see what the squad has left
        protected override void OnNewPlayerConnected(NetworkConnection conn)
        {
            TargetRpcUpdateForLatePlayer(conn, Mode, Remaining);
        }

        [TargetRpc]
        void TargetRpcUpdateForLatePlayer(NetworkConnection conn, ReviveBudgetMode mode, int remaining)
        {
            if (isServer) return;

            Mode = mode;
            Remaining = remaining;

            OnChanged?.Invoke(this);
        }
        #endregion

        void Log(string what)
        {
            if (!_logChanges) return;

            Debug.Log($"[ROM] {what}: " +
                      (IsUnlimited ? "revividas ilimitadas" : $"quedan {Remaining} revividas de escuadra"), this);
        }
    }
}
