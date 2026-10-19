using Mirror;
using UnityEngine;
using MultiFPS;
using MultiFPS.Gameplay;
using ROM.Difficulty;

namespace ROM.Gameplay
{
    /// <summary>
    /// Hold-to-revive a downed teammate.
    ///
    /// The kit has no generic interaction system — only pickup triggers on items —
    /// so the whole hold, the target search and the validation are built here.
    ///
    /// Split of responsibilities: the owning client decides what it is *aiming at*
    /// and when it is holding the key, and asks. The server decides whether that is
    /// allowed, runs the only clock that counts, and can cancel at any moment. A
    /// client that lies about its target or its distance gets refused, because every
    /// condition is re-checked on the server every frame.
    ///
    /// Goes on the character prefab, next to Health and DownedState.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(DownedState))]
    public class ReviveInteraction : DNNetworkBehaviour
    {
        [Header("Alcance")]
        [Tooltip("Distancia máxima para revivir a un compañero caído, en metros.")]
        [SerializeField] float _range = 2.5f;

        [Tooltip("Qué tan de frente hay que estar mirando al caído. 1 es exactamente de frente, 0 es cualquier dirección dentro de 90 grados. 0.35 equivale a un cono de unos 70 grados.")]
        [Range(-1f, 1f)]
        [SerializeField] float _facingDot = 0.35f;

        [Tooltip("Margen que se le suma al alcance al validar en el servidor, para que un paso o la latencia no cancelen un revive en curso.")]
        [SerializeField] float _rangeTolerance = 1.25f;

        [Tooltip("Loguea en consola el inicio, la cancelación y el final de cada revive. Sirve para verificar el comportamiento; se puede apagar cuando el sistema esté estable.")]
        [SerializeField] bool _logChanges = true;

        Health _health;
        DownedState _downed;
        CharacterInstance _character;

        //owner only: this component reads input on its own instead of patching
        //PlayerGameplayInput, so no file of the kit has to change
        MultiFPSInputs _input;

        /// <summary>
        /// Downed teammate the local player could revive right now, or null. For the
        /// prompt on screen. Only meaningful on the owning client.
        /// </summary>
        public DownedState Candidate { private set; get; }

        //owner only: what we have already asked the server for, so the command is
        //not resent every frame
        DownedState _requested;

        //server only
        DownedState _target;
        float _holdSeconds;
        float _elapsed;

        void Awake()
        {
            _health = GetComponent<Health>();
            _downed = GetComponent<DownedState>();
            _character = GetComponent<CharacterInstance>();
        }

        public override void OnStartAuthority()
        {
            _input = new MultiFPSInputs();
            _input.Enable();
        }

        public override void OnStopAuthority()
        {
            if (_input == null) return;

            _input.Disable();
            _input = null;
        }

        void OnDisable()
        {
            //leaving a revive marked as in progress on a target we are no longer
            //working on would freeze its progress bar forever
            if (isServer)
                Server_Cancel("el que revivía se desactivó");
        }

        void Update()
        {
            //on a host both run, which is correct: it is a server and a client at once
            if (isServer) ServerTick();
            if (isOwned) OwnerTick();
        }

        #region owner
        void OwnerTick()
        {
            if (_input == null) return;

            //a downed player cannot pick anyone else up. Self-revive is a separate
            //Threat Level setting and is not implemented yet.
            if (_downed.IsDowned || _downed.IsOut)
            {
                Candidate = null;
                StopRequestIfAny();
                return;
            }

            Candidate = FindCandidate();

            //IsPressed and not ReadValue<float>(): Interaction is declared as a
            //Button action, and a Button completes its interaction when pressed, so
            //reading its value does not reliably report that it is still held. The
            //actions the kit does read by value — Sprint, Crouch, Fire1Hold — are all
            //declared as Value for exactly that reason. IsPressed asks the control
            //itself, so it works whatever the action type is.
            bool holding = _input.Player.Interaction.IsPressed();

            if (!holding || Candidate == null)
            {
                StopRequestIfAny();
                return;
            }

            if (_requested == Candidate) return; //already asked for this one

            _requested = Candidate;
            CmdStartRevive(Candidate.netIdentity);
        }

        void StopRequestIfAny()
        {
            if (_requested == null) return;

            _requested = null;
            CmdStopRevive();
        }

        /// <summary>
        /// Nearest downed teammate within range and roughly in front. Runs on the
        /// owning client only, to drive the prompt: the server never trusts it.
        ///
        /// Walks the character registry the kit already keeps instead of querying
        /// physics. A sphere query would depend on which layer carries a collider,
        /// and the hitboxes of a character are on a different layer than the
        /// character itself — a detail easy to get wrong and hard to notice, because
        /// the failure mode is silently never finding anyone. The registry holds at
        /// most 32 entries, so walking it is cheaper than the query anyway.
        /// </summary>
        DownedState FindCandidate()
        {
            if (GameSync.Singleton == null) return null;

            CharacterInstance[] characters = GameSync.Singleton.Characters.Obj;

            DownedState best = null;
            float bestSqrDistance = float.MaxValue;
            float rangeSqr = _range * _range;

            for (int i = 0; i < characters.Length; i++)
            {
                CharacterInstance other = characters[i];

                if (other == null || other == _character) continue;

                //distance first: it is a subtraction, and it keeps the component
                //lookup to the handful of characters actually nearby
                float sqrDistance = (other.transform.position - transform.position).sqrMagnitude;

                if (sqrDistance > rangeSqr || sqrDistance >= bestSqrDistance) continue;

                DownedState downed = other.GetComponent<DownedState>();

                if (!IsRevivable(downed)) continue;

                bestSqrDistance = sqrDistance;
                best = downed;
            }

            return best;
        }
        #endregion

        #region server
        [Command]
        void CmdStartRevive(NetworkIdentity targetIdentity)
        {
            if (targetIdentity == null) return;

            DownedState target = targetIdentity.GetComponent<DownedState>();

            //the client asked, the server decides
            if (!ServerCanRevive(target))
            {
                //logged here and not through Server_Cancel, which returns early when
                //there is no revive in progress — exactly the case of a request that
                //was refused on arrival, and the one worth seeing
                if (_logChanges)
                {
                    Debug.Log($"[ROM] Pedido de revive rechazado: objetivo " +
                              $"{(target == null ? "inexistente" : target.name)}, " +
                              $"caído={(target != null && target.IsDowned)}, " +
                              $"mismo equipo={(target != null && target.GetComponent<Health>() != null && target.GetComponent<Health>().Team == _health.Team)}, " +
                              $"distancia={(target == null ? -1f : Vector3.Distance(transform.position, target.transform.position)):0.##}m " +
                              $"(alcance {_range * _rangeTolerance:0.##}m)", this);
                }

                Server_Cancel(null);
                return;
            }

            ThreatLevelDefinition threat = ActiveThreatLevel();

            if (threat == null)
            {
                Debug.LogError($"[ROM] No se puede revivir: no hay Threat Level activo que " +
                               $"defina cuánto hay que mantener la interacción.", this);
                return;
            }

            if (_target == target) return;

            Server_Cancel(null); //drop any previous target without logging noise

            _target = target;
            _holdSeconds = threat.ReviveHoldSeconds;
            _elapsed = 0f;

            _target.Server_SetBeingRevived(true, _holdSeconds);

            Log($"empezó a revivir a {_target.name} ({_holdSeconds:0.#}s)");
        }

        [Command]
        void CmdStopRevive()
        {
            Server_Cancel("soltó la interacción");
        }

        void ServerTick()
        {
            if (_target == null) return;

            //re-validated every frame: the target can bleed out, get revived by
            //someone else, or either of the two can walk away mid-hold
            if (!ServerCanRevive(_target))
            {
                Server_Cancel("se dejaron de cumplir las condiciones");
                return;
            }

            _elapsed += Time.deltaTime;

            if (_elapsed < _holdSeconds) return;

            Server_Complete();
        }

        [Server]
        void Server_Complete()
        {
            DownedState target = _target;

            //cleared first so the revive of the target does not come back into a
            //component that still thinks it is working
            _target = null;
            _elapsed = 0f;

            Log($"terminó de revivir a {target.name}");

            //Server_Revive clears the downed state, and with it the being-revived
            //state, so there is nothing to undo here
            target.Server_Revive();
        }

        [Server]
        void Server_Cancel(string reason)
        {
            if (_target == null) return;

            DownedState target = _target;

            _target = null;
            _elapsed = 0f;

            target.Server_SetBeingRevived(false, 0f);

            if (reason != null)
                Log($"dejó de revivir a {target.name}: {reason}");
        }

        /// <summary>
        /// Every condition a revive needs, checked on the server. Also used by the
        /// client to decide what to offer, so the prompt and the rule cannot drift
        /// apart: the difference is that here it is authoritative.
        /// </summary>
        bool ServerCanRevive(DownedState target)
        {
            if (!IsRevivable(target)) return false;

            //whoever revives has to be on their feet
            if (_downed.IsDowned || _downed.IsOut) return false;
            if (_health.CurrentHealth <= 0) return false;

            //a little extra room on the server so a step or some latency does not
            //cancel a hold the player is doing right
            float range = _range * _rangeTolerance;

            return Vector3.Distance(transform.position, target.transform.position) <= range;
        }

        /// <summary>
        /// Whether this target is something worth offering a revive on at all:
        /// a downed teammate that is not this character and has not bled out.
        /// </summary>
        bool IsRevivable(DownedState target)
        {
            if (target == null) return false;
            if (target == _downed) return false;
            if (!target.IsDowned || target.IsOut) return false;

            Health targetHealth = target.GetComponent<Health>();

            if (targetHealth == null) return false;

            //only squadmates, and never a character with no team assigned yet
            if (targetHealth.Team < 0 || targetHealth.Team != _health.Team) return false;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.0001f) return true; //on top of each other

            return Vector3.Dot(transform.forward, toTarget.normalized) >= _facingDot;
        }
        #endregion

        static ThreatLevelDefinition ActiveThreatLevel()
        {
            return ThreatLevelController.Instance != null
                ? ThreatLevelController.Instance.Active
                : null;
        }

        void Log(string what)
        {
            if (!_logChanges) return;

            string side = isServer ? (isClient ? "host" : "servidor") : "cliente";

            Debug.Log($"[ROM] {_health.CharacterName} {what} (en {side})", this);
        }
    }
}
