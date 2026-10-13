using Mirror;
using UnityEngine;
using MultiFPS;
using MultiFPS.Gameplay;
using ROM.Difficulty;

namespace ROM.Gameplay
{
    /// <summary>
    /// Turns running out of health into being *downed* instead of dead.
    ///
    /// The kit has no concept of a downed character, but it already has a full
    /// death and resurrect pipeline. So rather than fighting it, this component
    /// reinterprets it: what MultiFPS calls death is what ROM calls downed, and
    /// real death — bleeding out — is the new concept layered on top. That buys
    /// the ragdoll, the dropped weapon and the loss of control for free, since
    /// all of those already happen on Health.Server_OnHealthDepleted.
    ///
    /// Server authoritative. Clients are told the state so they can show it, and
    /// never decide it.
    ///
    /// Goes on the character prefab, next to CharacterInstance and Health.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class DownedState : DNNetworkBehaviour
    {
        [Tooltip("Loguea en consola las transiciones y de qué lado se aplicaron. Sirve para verificar el sync; se puede apagar cuando el sistema esté estable.")]
        [SerializeField] bool _logChanges = true;

        Health _health;

        /// <summary>
        /// Whether this component manages this character at all. Only real players
        /// get a downed state: enemies and filler bots have to keep dying, both
        /// because the GDD gives only Redheads a downed state and because the Raid
        /// wave counter listens to the same depleted event to count kills.
        /// Resolved once on the server, never per frame.
        /// </summary>
        public bool Managed { private set; get; }

        public bool IsDowned { private set; get; }

        /// <summary>True once the character bled out. It does not come back.</summary>
        public bool IsOut { private set; get; }

        /// <summary>Full bleed-out window of the current downed state, in seconds.</summary>
        public float BleedOutDuration { private set; get; }

        /// <summary>
        /// Seconds left before bleeding out, on both sides: the server counts down
        /// authoritatively and each client runs the same clock for its own UI.
        /// Zero when not downed.
        /// </summary>
        public float BleedOutRemaining { private set; get; }

        public delegate void DownedStateChanged(DownedState state);

        /// <summary>Raised on server and clients when the character goes down.</summary>
        public DownedStateChanged OnWentDown { set; get; }

        /// <summary>Raised on server and clients when the character is back up.</summary>
        public DownedStateChanged OnGotUp { set; get; }

        /// <summary>Raised on server and clients when the bleed-out timer ran out.</summary>
        public DownedStateChanged OnBledOut { set; get; }

        void Awake()
        {
            //straight off the GameObject, not through CharacterInstance.Health:
            //that property is assigned in the Awake of CharacterInstance, and Unity
            //does not guarantee Awake order between components of the same object,
            //so reading it here would be null intermittently.
            _health = GetComponent<Health>();
        }

        #region server
        public override void OnStartServer()
        {
            //a character spawned for a real player carries the connection of its
            //owner; one spawned for a bot does not. CharacterInstance.BOT looks
            //like the right check, but the kit never assigns it: it is always false.
            Managed = connectionToClient != null;

            if (!Managed) return;

            _health.Server_OnHealthDepleted += Server_OnHealthDepleted;
            _health.Server_Resurrect += Server_OnResurrect;
        }

        public override void OnStopServer()
        {
            if (!Managed) return;

            _health.Server_OnHealthDepleted -= Server_OnHealthDepleted;
            _health.Server_Resurrect -= Server_OnResurrect;
        }

        /// <summary>
        /// Someone brought the character back without going through Server_Revive —
        /// today that is the respawn of PlayerInstance, which is still active in the
        /// gamemodes of the kit. Without this the character would be walking around
        /// while this component still believed it was downed, and would bleed it out
        /// when the timer ran out.
        ///
        /// A revive started here clears the state before resurrecting, so this is a
        /// no-op in that case.
        /// </summary>
        void Server_OnResurrect(int health)
        {
            if (!IsDowned) return;

            ClearDowned();

            Log("de pie por una resurrección externa");
            OnGotUp?.Invoke(this);

            RpcGotUp();
        }

        void Server_OnHealthDepleted(CharacterPart part, AttackType attackType, Health attacker, int attackForce)
        {
            if (IsDowned || IsOut) return;

            ThreatLevelDefinition threat = ActiveThreatLevel();

            if (threat == null)
            {
                //no difficulty settings, no made-up numbers: the normal death of the
                //kit stays in place rather than guessing a bleed-out window
                Debug.LogError($"[ROM] {name} se quedó sin vida pero no hay Threat Level activo, " +
                               $"así que no entra en estado caído y muere como en el kit base.", this);
                return;
            }

            BleedOutDuration = threat.BleedOutSeconds;
            BleedOutRemaining = BleedOutDuration;
            IsDowned = true;

            Log("caído");
            OnWentDown?.Invoke(this);

            RpcWentDown(BleedOutDuration);
        }

        void Update()
        {
            if (!IsDowned) return;

            //both sides run the clock: the server to decide, each client for its UI
            BleedOutRemaining -= Time.deltaTime;

            if (BleedOutRemaining > 0f) return;

            BleedOutRemaining = 0f;

            if (!isServer) return; //only the server turns the timer into a decision

            Server_BleedOut();
        }

        /// <summary>
        /// Brings a downed character back up with the health the active Threat Level
        /// dictates. Server only. Returns false when the character was not downed,
        /// so callers do not have to check the state themselves.
        /// </summary>
        [Server]
        public bool Server_Revive()
        {
            if (!IsDowned || IsOut) return false;

            ThreatLevelDefinition threat = ActiveThreatLevel();

            //a revive with no difficulty settings would have to invent how much
            //health to give back, so it is refused instead
            if (threat == null)
            {
                Debug.LogError($"[ROM] No se puede revivir a {name}: no hay Threat Level activo " +
                               $"que defina con cuánta vida vuelve.", this);
                return false;
            }

            ClearDowned();

            Log("de pie");
            OnGotUp?.Invoke(this);

            RpcGotUp();

            //does the actual healing and fires the whole resurrect chain of the kit,
            //which puts the ragdoll back and restores control
            _health.ServerResurrect(threat.GetReviveHealth(_health.MaxHealth));

            return true;
        }

        [Server]
        void Server_BleedOut()
        {
            ClearDowned();
            IsOut = true;

            Log("desangrado, fuera de la incursión");
            OnBledOut?.Invoke(this);

            RpcBledOut();
        }
        #endregion

        #region client sync
        [ClientRpc]
        void RpcWentDown(float bleedOutSeconds)
        {
            if (isServer) return;

            BleedOutDuration = bleedOutSeconds;
            BleedOutRemaining = bleedOutSeconds;
            IsDowned = true;

            Log("caído");
            OnWentDown?.Invoke(this);
        }

        [ClientRpc]
        void RpcGotUp()
        {
            if (isServer) return;

            ClearDowned();

            Log("de pie");
            OnGotUp?.Invoke(this);
        }

        [ClientRpc]
        void RpcBledOut()
        {
            if (isServer) return;

            ClearDowned();
            IsOut = true;

            Log("desangrado, fuera de la incursión");
            OnBledOut?.Invoke(this);
        }

        //someone joining mid-incursion has to see who is already down, and with the
        //time actually left rather than a fresh window
        protected override void OnNewPlayerConnected(NetworkConnection conn)
        {
            if (!IsDowned && !IsOut) return;

            TargetRpcUpdateForLatePlayer(conn, IsDowned, IsOut, BleedOutDuration, BleedOutRemaining);
        }

        [TargetRpc]
        void TargetRpcUpdateForLatePlayer(NetworkConnection conn, bool isDowned, bool isOut, float duration, float remaining)
        {
            if (isServer) return;

            IsDowned = isDowned;
            IsOut = isOut;
            BleedOutDuration = duration;
            BleedOutRemaining = remaining;

            Log(isOut ? "desangrado, fuera de la incursión" : "caído");

            if (isOut)
                OnBledOut?.Invoke(this);
            else if (isDowned)
                OnWentDown?.Invoke(this);
        }
        #endregion

        void ClearDowned()
        {
            IsDowned = false;
            BleedOutDuration = 0f;
            BleedOutRemaining = 0f;
        }

        static ThreatLevelDefinition ActiveThreatLevel()
        {
            return ThreatLevelController.Instance != null
                ? ThreatLevelController.Instance.Active
                : null;
        }

        void Log(string state)
        {
            if (!_logChanges) return;

            string side = isServer ? (isClient ? "host" : "servidor") : "cliente";

            Debug.Log($"[ROM] {_health.CharacterName} {state} (en {side}" +
                      (IsDowned ? $", {BleedOutRemaining:0.#}s de {BleedOutDuration:0.#}s" : "") + ")", this);
        }
    }
}
