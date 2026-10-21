using System;
using System.Collections.Generic;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.Gamemodes;
using MultiFPS.UI;
using UnityEngine;

namespace ROM.Horde
{
    public enum HordeState : byte
    {
        WaitingForPlayers,
        Intermission,
        Wave,
        GameOver,
    }

    /// <summary>
    /// Avisos grandes que el HUD muestra en el centro de la pantalla.
    /// </summary>
    public enum HordeBanner : byte
    {
        Welcome,
        WaveStart,
        WaveCleared,
        GameOver,
        RunRestart,
    }

    /// <summary>
    /// Estado de la horda que ven los clientes. El servidor lo arma, lo compara con
    /// el último que mandó y solo lo reenvía si cambió.
    /// </summary>
    public struct HordeSnapshot : IEquatable<HordeSnapshot>
    {
        public HordeState State;
        public int Wave;
        public int EnemiesRemaining;
        public int Lives;
        public HordeModifier Modifier;
        public bool BossWave;
        public int ActivePortals;
        public int BestWave;

        /// <summary>
        /// Momento (en NetworkTime.time) en que termina la cuenta regresiva del
        /// estado actual. Los clientes calculan el tiempo restante localmente.
        /// </summary>
        public double CountdownEnd;

        public bool Equals(HordeSnapshot other)
        {
            return State == other.State && Wave == other.Wave && EnemiesRemaining == other.EnemiesRemaining
                && Lives == other.Lives && Modifier == other.Modifier && BossWave == other.BossWave
                && ActivePortals == other.ActivePortals && BestWave == other.BestWave
                && CountdownEnd.Equals(other.CountdownEnd);
        }
    }

    /// <summary>
    /// Modo Horda: los jugadores defienden una plataforma circular mientras la
    /// grieta escupe oleadas de enemigos desde portales en todo el borde.
    ///
    /// - Oleadas infinitas con dificultad progresiva (ver HordeDifficulty).
    /// - Los jugadores arrancan solo con pistola, cuchillo y granada; el arsenal
    ///   pesado está tirado en el centro (HordeWeaponCache).
    /// - Reservas compartidas: reaparecer durante una oleada consume una reserva.
    ///   Sin reservas, el caído espera al final de la oleada. Si caen todos sin
    ///   reservas, la partida termina y se reinicia desde la oleada 1.
    /// - Al superar una oleada: +1 reserva, los caídos vuelven, los vivos se curan
    ///   y recargan, y el arsenal se repone.
    /// - Élites, Colosos cada 5 oleadas y anomalías aleatorias que cambian las reglas.
    ///
    /// Se registra con el indicador Raid para que el menú lo ofrezca y para que el
    /// kit no rellene la partida con bots propios; no hereda de Raid a propósito,
    /// así la IA usa su comportamiento normal (ir a buscar al enemigo más cercano
    /// sin límite de detección), que es lo que una horda necesita.
    ///
    /// Va en ROOMMANAGER, como cualquier gamemode del kit. Sincroniza con RPCs y
    /// late-join por OnNewPlayerConnected, igual que ThreatLevelController.
    /// </summary>
    [AddComponentMenu("ROM/Gamemodes/Horde")]
    public partial class Horde : Gamemode
    {
        public const int PlayerTeam = 0;
        public const int EnemyTeam = 1;

        public static Horde Instance { private set; get; }

        [Header("Escena")]
        [SerializeField] HordeRiftPortal[] _portals;
        [SerializeField] HordeWeaponCache _weaponCache;

        [Header("Dificultad")]
        [SerializeField] HordeDifficulty _difficulty = new HordeDifficulty();

        [Header("Ritmo")]
        [Tooltip("Segundos entre que entra el primer jugador y arranca la oleada 1.")]
        [SerializeField] float _firstWaveDelay = 15f;
        [Tooltip("Descanso entre oleadas.")]
        [SerializeField] float _intermission = 12f;
        [Tooltip("Segundos que dura la pantalla de derrota antes de reiniciar.")]
        [SerializeField] float _gameOverDuration = 10f;
        [Tooltip("Segundos que un enemigo muerto queda tirado antes de que el pool lo reutilice.")]
        [SerializeField] float _corpseTime = 4f;

        [Header("Reservas")]
        [SerializeField] int _startingLives = 3;
        [SerializeField] int _livesPerWave = 1;
        [SerializeField] int _maxLives = 6;

        [Header("Protección al aparecer")]
        [Tooltip("Segundos en los que un jugador recién aparecido no recibe daño de la horda ni es elegido como objetivo.")]
        [SerializeField] float _spawnProtectionSeconds = 4f;

        [Header("HUD")]
        [Tooltip("Instancia del prefab HordeHUD que está en la escena.")]
        [SerializeField] HordeHUD _hud;
        [SerializeField] string _mapDisplayName = "El Rift";

        [Header("Equipo")]
        [Tooltip("Equipo con el que aparecen los jugadores, un elemento por slot del personaje. Slots vacíos quedan sin arma.")]
        [SerializeField] Item[] _playerStartingItems;
        [Tooltip("Armas que usan los enemigos durante la anomalía Artillería.")]
        [SerializeField] Item[] _artilleryWeapons;

        [Header("Enemigos")]
        [Tooltip("Equipo de los enemigos que vienen a cuchillo: solo el cuchillo, en el primer slot.")]
        [SerializeField] Item _meleeWeapon;
        [Tooltip("Distancia a la que se acercan los enemigos de cuchillo antes de tirar el golpe.")]
        [SerializeField] float _meleeEngageDistance = 1.4f;
        [SerializeField] string _meleeName = "Desgarrador";
        [SerializeField] string _enemyName = "Engendro";
        [SerializeField] string _eliteName = "Élite de la Grieta";
        [SerializeField] string _bossName = "COLOSO";

        /// <summary>
        /// Último estado conocido. En servidor es el último enviado; en cliente,
        /// el último recibido.
        /// </summary>
        public HordeSnapshot Snapshot { private set; get; }

        public delegate void SnapshotChanged(HordeSnapshot snapshot);

        /// <summary>
        /// Se dispara en servidor y en cliente cada vez que llega un estado nuevo.
        /// </summary>
        public SnapshotChanged OnSnapshotChanged { set; get; }

        public delegate void BannerEvent(HordeBanner banner, int wave, HordeModifier modifier, int bosses);

        /// <summary>
        /// Solo cliente: aviso grande de inicio/fin de oleada, derrota, etc.
        /// </summary>
        public BannerEvent OnBanner { set; get; }

        public delegate void SpawnProtectionEvent(CharacterInstance character, float seconds);

        /// <summary>
        /// Solo cliente: un personaje de la escuadra quedó protegido por unos segundos.
        /// </summary>
        public SpawnProtectionEvent OnSpawnProtection { set; get; }

        public string MapDisplayName => _mapDisplayName;

        #region estado de servidor
        HordeState _state = HordeState.WaitingForPlayers;
        int _wave;
        int _lives;
        int _bestWave;
        int _activePortals;
        double _stateEndsAt;

        HordeWavePlan _plan;
        int _toSpawn;
        int _bossesToSpawn;
        float _nextSpawnTime;

        readonly List<PlayerInstance> _bots = new List<PlayerInstance>();
        readonly Dictionary<PlayerInstance, float> _botDiedAt = new Dictionary<PlayerInstance, float>();
        readonly Dictionary<Health, EnemyKind> _aliveEnemies = new Dictionary<Health, EnemyKind>();
        readonly Dictionary<Health, PlayerInstance> _botByHealth = new Dictionary<Health, PlayerInstance>();
        readonly List<Health> _deadEnemiesBuffer = new List<Health>();

        //jugadores que ya aparecieron en esta corrida: su próxima aparición durante una oleada cuesta reserva
        readonly HashSet<PlayerInstance> _humansSpawned = new HashSet<PlayerInstance>();

        //personajes con protección de aparición y hasta cuándo dura
        readonly Dictionary<Health, float> _protectedUntil = new Dictionary<Health, float>();
        readonly List<Health> _expiredProtections = new List<Health>();

        bool _snapshotSent;
        #endregion

        GameObject[] _playerStartingItemObjects;
        GameObject[] _meleeLoadout;
        int[] _portalOrder;

        //BotAI no expone la distancia que mantiene con su objetivo; para los de cuchillo la achicamos por reflexión
        static readonly System.Reflection.FieldInfo BotDistanceField =
            typeof(BotAI).GetField("DistanceFromTarget", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        enum EnemyKind : byte
        {
            Normal,
            Melee,
            Elite,
            Boss,
        }

        protected override void Awake()
        {
            base.Awake();

            if (!Instance)
                Instance = this;

            Indicator = Gamemodes.Raid;
            FFA = false;
            FriendyFire = false;
            LetPlayersSpawnOnTheirOwn = true;

            _playerStartingItemObjects = ToGameObjects(_playerStartingItems);
            _meleeLoadout = _meleeWeapon ? new[] { _meleeWeapon.gameObject } : null;
            _portalOrder = BuildPortalOrder(_portals != null ? _portals.Length : 0);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (_hud)
                _hud.Bind(this);
            else
                Debug.LogWarning("[ROM] Horde no tiene HUD asignado; agregá el prefab HordeHUD a la escena y asignalo.", this);
        }

        public override void SetupGamemode(RoomProperties roomProperties)
        {
            //la horda maneja sus propios bots; si quedara activado el relleno del kit,
            //al entrar un jugador nuevo el kit destruiría un enemigo para hacerle lugar
            if (NetworkServer.active)
                roomProperties.P_FillEmptySlotsWithBots = false;

            base.SetupGamemode(roomProperties);
        }

        void Update()
        {
            if (!isServer)
                return;

            if (ExplorationMode) { TickExploration(); return; }

            switch (_state)
            {
                case HordeState.Intermission:
                    if (NetworkTime.time >= _stateEndsAt)
                        StartWave(_wave + 1);
                    break;

                case HordeState.Wave:
                    TickWave();
                    break;

                case HordeState.GameOver:
                    if (NetworkTime.time >= _stateEndsAt)
                        RestartRun();
                    break;
            }

            TickSpawnProtection();
            PushSnapshotIfChanged();
        }

        #region flujo de la partida
        void EnterIntermission(float seconds)
        {
            _state = HordeState.Intermission;
            _stateEndsAt = NetworkTime.time + seconds;
        }

        void StartWave(int wave)
        {
            _wave = wave;

            HordeModifier modifier = _difficulty.RollModifier(wave);
            _plan = _difficulty.Build(wave, CountHumans(), _portals != null ? _portals.Length : 0, modifier);

            _toSpawn = _plan.EnemyCount;
            _bossesToSpawn = _plan.BossCount;
            _nextSpawnTime = Time.time + 1.5f;
            _activePortals = _plan.ActivePortals;

            _state = HordeState.Wave;
            _stateEndsAt = 0;

            RpcBanner(HordeBanner.WaveStart, wave, _plan.Modifier, _plan.BossCount);
        }

        void TickWave()
        {
            PruneDestroyedEnemies();

            bool pending = _toSpawn > 0 || _bossesToSpawn > 0;

            if (pending && _aliveEnemies.Count < _plan.MaxAlive && Time.time >= _nextSpawnTime)
            {
                //el Coloso entra cuando ya salió la mitad de la oleada, para que llegue con escolta
                bool spawnBoss = _bossesToSpawn > 0 && (_toSpawn <= _plan.EnemyCount / 2 || _toSpawn == 0);

                EnemyKind kind = spawnBoss ? EnemyKind.Boss : RollEnemyKind();

                if (SpawnEnemy(kind))
                {
                    if (spawnBoss) _bossesToSpawn--;
                    else _toSpawn--;

                    _nextSpawnTime = Time.time + _plan.SpawnInterval;
                }
                else
                {
                    //no había bot libre, se reintenta en un rato
                    _nextSpawnTime = Time.time + 0.5f;
                }
            }

            if (_toSpawn <= 0 && _bossesToSpawn <= 0 && _aliveEnemies.Count == 0)
                WaveCleared();
        }

        void WaveCleared()
        {
            _bestWave = Mathf.Max(_bestWave, _wave);
            _lives = Mathf.Min(_maxLives, _lives + _livesPerWave);

            RpcBanner(HordeBanner.WaveCleared, _wave, HordeModifier.None, 0);

            RewardSurvivors();

            if (_weaponCache)
                _weaponCache.Server_Restock();

            EnterIntermission(_intermission);
        }

        void GameOver()
        {
            _state = HordeState.GameOver;
            _stateEndsAt = NetworkTime.time + _gameOverDuration;

            int survived = Mathf.Max(0, _wave - 1);
            _bestWave = Mathf.Max(_bestWave, survived);

            RpcBanner(HordeBanner.GameOver, survived, HordeModifier.None, 0);
        }

        void RestartRun()
        {
            DespawnAllEnemies();

            _wave = 0;
            _lives = _startingLives;
            _activePortals = 0;
            _humansSpawned.Clear();

            ResetPlayersStats();

            //primero salir de GameOver: en ese estado TrySpawnHuman no deja aparecer a nadie
            EnterIntermission(_firstWaveDelay);

            foreach (PlayerInstance human in Humans())
                TrySpawnHuman(human, true);

            if (_weaponCache)
                _weaponCache.Server_Restock();

            RpcBanner(HordeBanner.RunRestart, 0, HordeModifier.None, 0);
        }

        void ResetToWaiting()
        {
            DespawnAllEnemies();

            _state = HordeState.WaitingForPlayers;
            _wave = 0;
            _activePortals = 0;
            _stateEndsAt = 0;
            _humansSpawned.Clear();
        }

        protected override void CheckTeamStates()
        {
            base.CheckTeamStates();

            if (!isServer)
                return;

            if (ExplorationMode) return;

            int humans = CountHumans();

            if (humans > 0 && _state == HordeState.WaitingForPlayers)
            {
                _wave = 0;
                _lives = _startingLives;

                EnterIntermission(_firstWaveDelay);
            }
            else if (humans == 0 && _state != HordeState.WaitingForPlayers)
            {
                ResetToWaiting();
            }
        }
        #endregion

        #region jugadores
        public override void Server_OnPlayerInstanceAdded(PlayerInstance player)
        {
            if (!isServer || player.BOT)
                return; //los bots los maneja la horda: se unen al equipo enemigo y aparecen cuando hacen falta

            if (player.Team == -1)
                AssignPlayerToTeam(player, PlayerTeam);
        }

        protected override int PlayerRequestToJoinTeamPermission(PlayerInstance player, int requestedTeam)
        {
            int allowedTeam = player.BOT ? EnemyTeam : PlayerTeam;
            return requestedTeam == allowedTeam ? 0 : -1;
        }

        protected override void OnPlayerAddedToTeam(PlayerInstance player, int team)
        {
            if (!player.BOT)
                TrySpawnHuman(player, true);
        }

        protected override void OnPlayerRemovedFromTeam(PlayerInstance player, int team)
        {
            _humansSpawned.Remove(player);
        }

        public override void PlayerSpawnCharacterRequest(PlayerInstance playerInstance)
        {
            //los bots de la horda piden reaparecer solos al terminar su cooldown;
            //se ignora, el que decide cuándo y dónde vuelven es TickWave
            if (playerInstance == null || playerInstance.BOT)
                return;

            TrySpawnHuman(playerInstance, false);
        }

        void TrySpawnHuman(PlayerInstance player, bool free)
        {
            if (player == null || player.BOT || player.Team != PlayerTeam)
                return;

            if (_state == HordeState.GameOver)
                return;

            if (player.MyCharacter && player.MyCharacter.Health.CurrentHealth > 0)
                return;

            bool costsLife = !ExplorationMode && !free && _state == HordeState.Wave && _humansSpawned.Contains(player);

            if (costsLife)
            {
                if (_lives <= 0)
                {
                    CheckGameOver();
                    return;
                }

                _lives--;
            }

            player.SetItemsOnSpawn(_playerStartingItemObjects);

            //con enemigos vivos, el punto más lejos de ellos; sin enemigos, rotamos para no apilar a la escuadra en el mismo lugar
            Transform spawnPoint = !defaultSpawnPoints ? null
                : _aliveEnemies.Count > 0 ? defaultSpawnPoints.GetBestSpawnPoint(PlayerTeam)
                : defaultSpawnPoints.GetNextSpawnPoint();
            if (ExplorationMode) spawnPoint = ExplorationSpawn(player, spawnPoint);
            player.Server_SpawnCharacter(spawnPoint ? spawnPoint : transform);

            _humansSpawned.Add(player);

            if (player.MyCharacter)
                StartSpawnProtection(player.MyCharacter);
        }

        #region protección al aparecer
        /// <summary>
        /// Health no tiene un modo invulnerable y no lo tocamos. Usamos la regla de
        /// fuego amigo que ya trae: mientras dura la protección, del lado del servidor
        /// el personaje figura en el equipo de la horda. Así el daño de los enemigos
        /// cuenta como fuego amigo y se descarta, y la IA no lo elige como objetivo.
        /// Los clientes no ven el cambio de equipo, solo el aviso en el HUD.
        /// </summary>
        void StartSpawnProtection(CharacterInstance character)
        {
            if (_spawnProtectionSeconds <= 0f)
                return;

            if (!ExplorationMode) character.Health.Team = EnemyTeam;
            _protectedUntil[character.Health] = Time.time + _spawnProtectionSeconds;

            RpcSpawnProtection(character.netIdentity, _spawnProtectionSeconds);
        }

        void TickSpawnProtection()
        {
            if (_protectedUntil.Count == 0)
                return;

            _expiredProtections.Clear();

            foreach (KeyValuePair<Health, float> entry in _protectedUntil)
            {
                if (entry.Key == null || Time.time >= entry.Value)
                    _expiredProtections.Add(entry.Key);
            }

            foreach (Health health in _expiredProtections)
            {
                _protectedUntil.Remove(health);

                if (health)
                    health.Team = PlayerTeam;
            }
        }

        [ClientRpc]
        void RpcSpawnProtection(NetworkIdentity character, float seconds)
        {
            CharacterInstance instance = character ? character.GetComponent<CharacterInstance>() : null;

            if (instance)
                OnSpawnProtection?.Invoke(instance, seconds);
        }
        #endregion

        void RewardSurvivors()
        {
            foreach (PlayerInstance human in Humans())
            {
                CharacterInstance character = human.MyCharacter;

                if (!character || character.Health.CurrentHealth <= 0)
                {
                    TrySpawnHuman(human, true);
                    continue;
                }

                character.Health.ServerHeal(character.Health.MaxHealth, character.DNID);

                foreach (Slot slot in character.CharacterItemManager.Slots)
                {
                    if (slot.Item)
                        slot.Item.AddAmmo(slot.Item.MaxAmmoSupply);
                }
            }
        }

        void CheckGameOver()
        {
            if (_state != HordeState.Wave)
                return;

            if (_lives <= 0 && CountHumans() > 0 && CountAliveHumans() == 0)
                GameOver();
        }

        IEnumerable<PlayerInstance> Humans()
        {
            List<PlayerInstance> team = _teams[PlayerTeam].PlayerInstances;

            //copia: TrySpawnHuman puede disparar callbacks que toquen la lista
            foreach (PlayerInstance player in team.ToArray())
            {
                if (player && !player.BOT)
                    yield return player;
            }
        }

        int CountHumans()
        {
            int count = 0;
            foreach (PlayerInstance player in _teams[PlayerTeam].PlayerInstances)
                if (player && !player.BOT) count++;
            return count;
        }

        int CountAliveHumans()
        {
            int count = 0;
            foreach (PlayerInstance player in _teams[PlayerTeam].PlayerInstances)
                if (player && !player.BOT && player.MyCharacter && player.MyCharacter.Health.CurrentHealth > 0) count++;
            return count;
        }
        #endregion

        #region enemigos
        public override void Server_OnPlayerKilled(Health victim, Health killer)
        {
            base.Server_OnPlayerKilled(victim, killer);

            if (!isServer || victim == null)
                return;

            if (_botByHealth.TryGetValue(victim, out PlayerInstance bot))
            {
                _botByHealth.Remove(victim);

                if (bot)
                    _botDiedAt[bot] = Time.time;

                if (_aliveEnemies.TryGetValue(victim, out EnemyKind kind))
                {
                    if (ExplorationMode) ExplorationEnemyKilled(victim, killer, kind);
                    _aliveEnemies.Remove(victim);

                    //murió solo (trabado, caída al vacío): no cuenta como baja, vuelve a la cola
                    bool selfInflicted = killer == null || killer == victim;

                    if (selfInflicted && _state == HordeState.Wave)
                    {
                        if (kind == EnemyKind.Boss) _bossesToSpawn++;
                        else _toSpawn++;
                    }
                }

                return;
            }

            //cayó un jugador (no se mira victim.Team: si estaba protegido figura en el otro equipo)
            if (_protectedUntil.Remove(victim))
                victim.Team = PlayerTeam;

            if (!ExplorationMode) CheckGameOver();
        }

        EnemyKind RollEnemyKind()
        {
            if (UnityEngine.Random.value < _plan.EliteChance)
                return EnemyKind.Elite;

            if (_meleeLoadout != null && UnityEngine.Random.value < _plan.MeleeChance)
                return EnemyKind.Melee;

            return EnemyKind.Normal;
        }

        bool SpawnEnemy(EnemyKind kind, Transform encounterSpawn = null)
        {
            using var measurement=new RiftFrameDiagnostics.Sample(6);
            PlayerInstance bot = GetIdleBot();
            if (!bot)
                return false;

            int portalIndex = encounterSpawn ? -1 : PickPortal();
            Transform spawnPoint = encounterSpawn ? encounterSpawn : portalIndex >= 0 ? _portals[portalIndex].GetRandomSpawnPoint() : transform;

            bot.PlayerInfo.Username = EnemyName(kind);
            bot.PlayerInfo.CharacterSkinID = PickSkin(kind);
            bot.PlayerInfo.Lodout = null; //el kit sortea un lodout nuevo en cada aparición
            bot.AlwaysReinstantiateOnRespawn = true; //personaje nuevo cada vez, así el equipo forzado se aplica siempre

            GameObject[] loadout = kind == EnemyKind.Melee
                ? _meleeLoadout
                : _plan.Modifier == HordeModifier.Artilleria ? PickArtilleryLoadout() : null;

            bot.SetItemsOnSpawn(loadout);
            bot.Server_SpawnCharacter(spawnPoint);

            CharacterInstance character = bot.MyCharacter;
            if (!character)
                return false;

            float healthMultiplier = _plan.HealthMultiplier;
            if (kind == EnemyKind.Elite) healthMultiplier *= _difficulty.EliteHealthMultiplier;
            if (kind == EnemyKind.Boss) healthMultiplier *= _difficulty.BossHealthMultiplier;

            //la vida es autoridad del servidor; los clientes reciben el valor real con el primer daño
            Health health = character.Health;
            int hp = Mathf.Max(1, Mathf.RoundToInt(health.MaxHealth * healthMultiplier));
            health.ServerSetVitals(hp,hp);

            BotAI ai = character.GetComponent<BotAI>();
            if (ai)
            {
                if (kind == EnemyKind.Melee)
                {
                    //ritmo de cuchillazos; el cooldown del arma limita igual
                    ai.fireTime = 1f;
                    ai.waitForFireTime = 0.35f;

                    //sin esto el bot se queda a 34 m "manteniendo distancia" y nunca llega a pegar
                    BotDistanceField?.SetValue(ai, _meleeEngageDistance);
                }
                else
                {
                    ai.fireTime = _plan.BotFireTime;
                    ai.waitForFireTime = _plan.BotWaitForFireTime;
                }
            }

            _aliveEnemies[health] = kind;
            _botByHealth[health] = bot;

            if (portalIndex >= 0)
                RpcPortalPulse((byte)portalIndex);

            return true;
        }

        PlayerInstance GetIdleBot()
        {
            for (int i = _bots.Count - 1; i >= 0; i--)
            {
                PlayerInstance bot = _bots[i];

                if (!bot)
                {
                    _bots.RemoveAt(i);
                    continue;
                }

                if (!bot.MyCharacter)
                    return bot;

                if (bot.MyCharacter.Health.CurrentHealth > 0)
                    continue;

                //dejamos el cuerpo tirado un rato antes de reciclarlo
                if (!_botDiedAt.TryGetValue(bot, out float diedAt) || Time.time - diedAt >= _corpseTime)
                    return bot;
            }

            //tope del pool: los vivos más un margen para los cadáveres que todavía se ven
            if (_bots.Count >= _difficulty.MaxAliveCap + 6)
                return null;

            PlayerInstance newBot = SpawnBot(EnemyTeam);
            if (newBot)
                _bots.Add(newBot);

            return newBot;
        }

        void DespawnAllEnemies()
        {
            foreach (PlayerInstance bot in _bots)
            {
                if (bot)
                    bot.DespawnCharacterIfExist();
            }

            _aliveEnemies.Clear();
            _botByHealth.Clear();
            _botDiedAt.Clear();
            _toSpawn = 0;
            _bossesToSpawn = 0;
        }

        void PruneDestroyedEnemies()
        {
            _deadEnemiesBuffer.Clear();

            foreach (KeyValuePair<Health, EnemyKind> enemy in _aliveEnemies)
            {
                if (enemy.Key == null)
                    _deadEnemiesBuffer.Add(enemy.Key);
            }

            foreach (Health destroyed in _deadEnemiesBuffer)
            {
                _aliveEnemies.Remove(destroyed);
                _botByHealth.Remove(destroyed);
            }
        }

        int PickPortal()
        {
            if (_portals == null || _portals.Length == 0)
                return -1;

            int active = Mathf.Clamp(_activePortals, 1, _portalOrder.Length);

            //preferimos portales sin jugadores encima, para no aparecer en la cara de nadie
            int start = UnityEngine.Random.Range(0, active);
            for (int i = 0; i < active; i++)
            {
                int index = _portalOrder[(start + i) % active];

                if (_portals[index] && !HumanNear(_portals[index].transform.position, 10f))
                    return index;
            }

            return _portalOrder[start];
        }

        bool HumanNear(Vector3 position, float radius)
        {
            float sqrRadius = radius * radius;

            foreach (PlayerInstance player in _teams[PlayerTeam].PlayerInstances)
            {
                if (!player || !player.MyCharacter || player.MyCharacter.Health.CurrentHealth <= 0)
                    continue;

                if ((player.MyCharacter.transform.position - position).sqrMagnitude < sqrRadius)
                    return true;
            }

            return false;
        }

        int PickSkin(EnemyKind kind)
        {
            ClientInterfaceManager ui = ClientInterfaceManager.Instance;
            int skins = ui && ui.characterSkins != null ? ui.characterSkins.Length : 0;

            if (skins <= 0)
                return 0;

            //el Coloso siempre usa la última apariencia, así se lo reconoce de lejos
            return kind == EnemyKind.Boss ? skins - 1 : UnityEngine.Random.Range(0, skins);
        }

        string EnemyName(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Boss: return _bossName;
                case EnemyKind.Elite: return _eliteName;
                case EnemyKind.Melee: return _meleeName;
                default: return _enemyName;
            }
        }

        GameObject[] PickArtilleryLoadout()
        {
            if (_artilleryWeapons == null || _artilleryWeapons.Length == 0)
                return null;

            Item weapon = _artilleryWeapons[UnityEngine.Random.Range(0, _artilleryWeapons.Length)];
            return weapon ? new[] { weapon.gameObject } : null;
        }
        #endregion

        #region sincronización
        HordeSnapshot BuildSnapshot()
        {
            bool inWave = _state == HordeState.Wave;

            return new HordeSnapshot
            {
                State = _state,
                Wave = _wave,
                EnemiesRemaining = inWave ? _toSpawn + _bossesToSpawn + _aliveEnemies.Count : 0,
                Lives = _lives,
                Modifier = inWave ? _plan.Modifier : HordeModifier.None,
                BossWave = inWave && _plan.IsBossWave,
                ActivePortals = _activePortals,
                BestWave = _bestWave,
                CountdownEnd = _stateEndsAt,
            };
        }

        void PushSnapshotIfChanged()
        {
            HordeSnapshot snapshot = BuildSnapshot();

            if (_snapshotSent && snapshot.Equals(Snapshot))
                return;

            _snapshotSent = true;

            ApplySnapshot(snapshot);
            RpcSnapshot(snapshot);
        }

        [ClientRpc]
        void RpcSnapshot(HordeSnapshot snapshot)
        {
            if (isServer) return; //el servidor ya lo aplicó al armarlo

            ApplySnapshot(snapshot);
        }

        [ClientRpc]
        void RpcBanner(HordeBanner banner, int wave, HordeModifier modifier, int bosses)
        {
            OnBanner?.Invoke(banner, wave, modifier, bosses);
        }

        [ClientRpc]
        void RpcPortalPulse(byte portalIndex)
        {
            if (_portals != null && portalIndex < _portals.Length && _portals[portalIndex])
                _portals[portalIndex].Pulse();
        }

        //un cliente que entra tarde necesita el estado de la oleada en curso
        protected override void OnNewPlayerConnected(NetworkConnection conn)
        {
            base.OnNewPlayerConnected(conn);

            if (_snapshotSent)
                TargetRpcSnapshot(conn, Snapshot);
        }

        [TargetRpc]
        void TargetRpcSnapshot(NetworkConnection conn, HordeSnapshot snapshot)
        {
            if (isServer) return;

            ApplySnapshot(snapshot);
        }

        void ApplySnapshot(HordeSnapshot snapshot)
        {
            Snapshot = snapshot;

            if (_portals != null)
            {
                for (int i = 0; i < _portalOrder.Length; i++)
                {
                    HordeRiftPortal portal = _portals[_portalOrder[i]];
                    if (portal)
                        portal.SetOpen(i < snapshot.ActivePortals);
                }
            }

            OnSnapshotChanged?.Invoke(snapshot);
        }
        #endregion

        #region helpers
        static GameObject[] ToGameObjects(Item[] items)
        {
            if (items == null || items.Length == 0)
                return null;

            GameObject[] result = new GameObject[items.Length];
            for (int i = 0; i < items.Length; i++)
                result[i] = items[i] ? items[i].gameObject : null;

            return result;
        }

        /// <summary>
        /// Orden en que se abren los portales: primero uno sí y uno no (así desde la
        /// oleada 1 llegan de lados opuestos) y después los intermedios.
        /// </summary>
        static int[] BuildPortalOrder(int count)
        {
            int[] order = new int[count];
            int k = 0;

            for (int i = 0; i < count; i += 2) order[k++] = i;
            for (int i = 1; i < count; i += 2) order[k++] = i;

            return order;
        }
        #endregion
    }
}
