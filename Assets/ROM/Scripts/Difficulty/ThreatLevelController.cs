using Mirror;
using UnityEngine;
using MultiFPS;

namespace ROM.Difficulty
{
    /// <summary>
    /// Dueño del Threat Level activo de la incursión. El servidor decide, los
    /// clientes solo leen.
    ///
    /// Sigue el idioma de red del kit: no usa SyncVar, sincroniza con RPCs
    /// explícitos y cubre el late-join por OnNewPlayerConnected, igual que
    /// CharacterItemManager.
    ///
    /// Va en el mismo GameObject que ROOMMANAGER, al lado del Gamemode.
    /// </summary>
    public class ThreatLevelController : DNNetworkBehaviour
    {
        public static ThreatLevelController Instance { private set; get; }

        [SerializeField] ThreatLevelCatalog _catalog;

        [Tooltip("Loguea en consola cada cambio de nivel activo y de qué lado se aplicó. Sirve para verificar el sync; se puede apagar cuando el sistema esté estable.")]
        [SerializeField] bool _logChanges = true;

        [Tooltip("Nivel con el que arranca el servidor. Provisional: cuando exista la selección de incursión en el hub, el nivel va a llegar desde ahí.")]
        [SerializeField] ThreatLevel _startingLevel = ThreatLevel.I;

        public ThreatLevel ActiveLevel { private set; get; } = ThreatLevel.None;

        /// <summary>
        /// Definición del nivel activo, o null si todavía no se resolvió o el
        /// catálogo está incompleto. Quien la consuma tiene que chequear null:
        /// en un cliente recién conectado puede no haber llegado el RPC todavía.
        /// </summary>
        public ThreatLevelDefinition Active { private set; get; }

        public delegate void ThreatLevelChanged(ThreatLevel level, ThreatLevelDefinition definition);

        /// <summary>
        /// Se dispara en servidor y en cliente cada vez que el nivel activo cambia.
        /// Para UI y para sistemas que quieran reaccionar sin consultar por frame.
        /// </summary>
        public ThreatLevelChanged OnActiveLevelChanged { set; get; }

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

        public override void OnStartServer()
        {
            ApplyLevel(_startingLevel);
        }

        /// <summary>
        /// Cambia el nivel activo. Solo servidor.
        /// </summary>
        [Server]
        public void Server_SetLevel(ThreatLevel level)
        {
            if (level == ActiveLevel) return;

            if (ApplyLevel(level))
                RpcSetLevel(level);
        }

        [ClientRpc]
        void RpcSetLevel(ThreatLevel level)
        {
            if (isServer) return; //el servidor ya lo aplicó cuando lo decidió

            ApplyLevel(level);
        }

        #region late join
        //un cliente que entra tarde necesita saber el nivel que ya está en curso
        protected override void OnNewPlayerConnected(NetworkConnection conn)
        {
            TargetRpcUpdateForLatePlayer(conn, ActiveLevel);
        }

        [TargetRpc]
        void TargetRpcUpdateForLatePlayer(NetworkConnection conn, ThreatLevel level)
        {
            if (isServer) return;

            ApplyLevel(level);
        }
        #endregion

        /// <summary>
        /// Resuelve el nivel contra el catálogo y avisa a los suscriptores.
        /// Devuelve false y loguea si no se pudo resolver, sin tocar el estado
        /// actual: es mejor quedarse en el nivel anterior que pasar a uno nulo.
        /// </summary>
        bool ApplyLevel(ThreatLevel level)
        {
            if (!_catalog)
            {
                Debug.LogError($"[ROM] {nameof(ThreatLevelController)} no tiene catálogo asignado, el Threat Level no se puede resolver.", this);
                return false;
            }

            ThreatLevelDefinition definition = _catalog.Get(level);

            if (!definition)
            {
                Debug.LogError($"[ROM] El catálogo \"{_catalog.name}\" no tiene definición para {level}.", this);
                return false;
            }

            ActiveLevel = level;
            Active = definition;

            if (_logChanges)
            {
                string side = isServer ? (isClient ? "host" : "servidor") : "cliente";

                Debug.Log($"[ROM] Threat Level activo en {side}: {definition.DisplayName} " +
                          $"(bleed-out {definition.BleedOutSeconds}s, revive {definition.ReviveHoldSeconds}s " +
                          $"al {definition.ReviveHealthPercent}%, revividas {DescribeBudget(definition)})", this);
            }

            OnActiveLevelChanged?.Invoke(ActiveLevel, Active);

            return true;
        }

        static string DescribeBudget(ThreatLevelDefinition definition)
        {
            switch (definition.BudgetMode)
            {
                case ReviveBudgetMode.Unlimited:
                    return "ilimitadas";
                case ReviveBudgetMode.SquadPool:
                    return $"pool de escuadra de {definition.SquadReviveBudget}";
                case ReviveBudgetMode.PerPlayer:
                    return $"{definition.RevivesPerPlayer} por jugador (modo todavía no implementado)";
                default:
                    return "modo desconocido";
            }
        }
    }
}
