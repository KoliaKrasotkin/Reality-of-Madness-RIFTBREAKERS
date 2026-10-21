using UnityEngine;
using UnityEngine.AI;
using MultiFPS.Gameplay.Gamemodes;
using UnityEngine.Serialization;

namespace MultiFPS.Gameplay
{
    /// <summary>
    /// AI that controls bots
    /// </summary>
    public class BotAI : DNNetworkBehaviour
    {
        public bool Passive = false;
        public float RotationToTargetSpeed = 350f;
        public float fireTime = 1.55f;
        public float waitForFireTime = 0.75f;
        public bool SwitchItemsRandomly = false;

        [SerializeField] float DistanceFromTarget = 34f;
        [FormerlySerializedAs("assaultDetectionRange")]
        [SerializeField] private float raidDetectionRange = 20f;
        [FormerlySerializedAs("assaultPatrolRadius")]
        [SerializeField] private float raidPatrolRadius = 12f;
        [FormerlySerializedAs("assaultPatrolWaitTime")]
        [SerializeField] private float raidPatrolWaitTime = 1.5f;
        [FormerlySerializedAs("assaultPatrolArrivalDistance")]
        [SerializeField] private float raidPatrolArrivalDistance = 1.2f;

        private Vector3 raidPatrolDestination;
        private bool hasRaidPatrolDestination;
        private float nextRaidPatrolTime;

        Transform _transform;
        protected Health targetedEnemy;
        protected CharacterInstance _characterInstance;
        bool _pushTrigger = false;
        Quaternion mindRotation;

        #region navigation

        int _currentCorner;
        bool _isMoving;
        bool _rotateToDestination;
        int _pathCorners;

        #endregion

        float _focusEnemyTimer = 0;
        float _focusEnemyTime = 1.5f;

        bool _maintainPosition;
        Vector3 _positionToMaintain;
        float _maxAllowedDistanceFromPositionToMaintain;

        bool _enemyInSight;

        // Firing
        float _fireTime;
        float _stepAsideCooldownTimer;

        float _stuckTimer;
        float _takeRandomItemTimer;

        Gamemode _gamemode;
        GameSync _gameSync;

        #region drunkLook

        Vector2 _lerpedFuzzyLook;
        Vector2 _fuzzyLook;
        float _fuzzyLoookLerpSpeed = 1.8f;
        int _fuzzyLookUpdateTickInverval = 2;
        int _fuzzyLookUpdateCurrentTick = 0;

        #endregion

        PhysicsScene _physicsScene;

        Health _potentialNearestEnemy;
        Health _potentialEnemy;

        #region navigation

        bool isFollowingPath;
        Vector3 desiredVelocity;
        Vector3 directTravelTargetPoint;
        NavMeshPath _path;

        #endregion

        private void Awake()
        {
            _characterInstance = GetComponent<CharacterInstance>();
            _characterInstance.Server_SetAsBOT += SetAsBot;

            enabled = false;

            _path = new NavMeshPath();
            _transform = transform;
        }

        protected void Start()
        {
            _characterInstance.Health.Server_OnHealthDepleted += OnDeath;
            _characterInstance.Health.Server_Resurrect += ServerOnResurrect;

            if (isServer)
            {
                _gameSync = GameSync.Singleton;
                _gamemode = GameManager.Gamemode;
                _physicsScene = gameObject.scene.GetPhysicsScene();
            }
        }

        private void FixedUpdate()
        {
            if (isServer)
                AIUpdate(Time.fixedDeltaTime);
        }

        protected void OnDestroy()
        {
            if (_characterInstance)
            {
                _characterInstance.Health.Server_OnHealthDepleted -= OnDeath;
                _characterInstance.Server_SetAsBOT -= SetAsBot;
                _characterInstance = null;
            }

            targetedEnemy = null;
            _path = null;
        }

        void SetAsBot(bool _set)
        {
            enabled = _set;
        }

        public void ServerMantainPosition(Vector3 pos, float maxDistance = 0.5f)
        {
            _positionToMaintain = pos;
            _maintainPosition = true;
            _maxAllowedDistanceFromPositionToMaintain = maxDistance;
        }

        public void ServerForgetPositionToMaintain()
        {
            _maintainPosition = false;
        }

        void TakeRandomItem()
        {
            int randomSlot = Random.Range(
                0,
                _characterInstance.CharacterItemManager.Slots.Count
            );

            _characterInstance.CharacterItemManager.ServerCommandTakeItem(randomSlot);
        }

        void OnDeath(
            CharacterPart hittedPartID,
            AttackType attackType,
            Health killer,
            int attackForce
        )
        {
            ServerForgetPositionToMaintain();
        }

        void ServerOnResurrect(int health)
        {
            enabled = _characterInstance.BOT;
        }

        bool LocalPatrol => _gamemode is Raid || (_gamemode && _gamemode.UseLocalPatrol);

        void AIUpdate(float timestep)
        {
            if (_characterInstance.Health.CurrentHealth <= 0)
                return;

            if (SwitchItemsRandomly && Time.time > _takeRandomItemTimer)
            {
                _takeRandomItemTimer = Time.time + Random.Range(6f, 18f);
                TakeRandomItem();
            }

            if (Passive)
                return;

            // Commit sepuku if stuck for more than 5 seconds
            if (_stuckTimer > 5f)
            {
                _stuckTimer = 0f;

                _characterInstance.Health.Server_ChangeHealthState(
                    9999,
                    CharacterPart.body,
                    AttackType.falldamage,
                    _characterInstance.Health,
                    255
                );
            }

            var territory = GetComponent<RiftEnemyTerritory>();
            if (territory && Vector3.Distance(transform.position, territory.Home) > territory.Leash) {
                targetedEnemy=null;_pushTrigger=false;_characterInstance.Input.ActionCodes=0;
                SetTravelDestinationByNavMesh(territory.Home,true,true);UpdatePath();return;
            }
            if (targetedEnemy && (!RiftClassCombat.CanTarget(targetedEnemy,transform.position)||(territory && !territory.CanTarget(targetedEnemy)))) targetedEnemy=null;
            UpdatePath();

            _enemyInSight = EnemyInSight();

            #region fuzzy look swinging calculations

            if (_enemyInSight)
            {
                if (_fuzzyLookUpdateCurrentTick >= _fuzzyLookUpdateTickInverval)
                {
                    _fuzzyLook.x = Random.Range(-1, 6f);
                    _fuzzyLook.y = Random.Range(-12f, 12f);
                    _fuzzyLookUpdateCurrentTick = 0;
                }

                _fuzzyLookUpdateCurrentTick++;

                _focusEnemyTimer += timestep;
            }
            else
            {
                _focusEnemyTimer = 0;
                _fuzzyLookUpdateCurrentTick = _fuzzyLookUpdateTickInverval;
                _fuzzyLook = Vector2.zero;
            }

            #endregion

            _lerpedFuzzyLook = Vector2.Lerp(
                _lerpedFuzzyLook,
                _fuzzyLook,
                _fuzzyLoookLerpSpeed * timestep
            );

            if (_enemyInSight)
                _focusEnemyTimer += timestep;
            else
                _focusEnemyTimer = 0;

            // AI LOGIC
            if (_gamemode.ClassRules || !_enemyInSight ||
                (targetedEnemy && targetedEnemy.CurrentHealth <= 0))
            {
                targetedEnemy = GetClosestEnemy();
            }
            // En Asalto, abandonar objetivos que se alejan demasiado.
            if (LocalPatrol && targetedEnemy != null)
            {
                float targetDistance = Vector3.Distance(
                    _transform.position,
                    targetedEnemy.transform.position
                );

                if (targetDistance > raidDetectionRange)
                {
                    targetedEnemy = null;
                    _enemyInSight = false;
                    _focusEnemyTimer = 0f;
                }
            }

            // If there is someone to attack
            if (targetedEnemy)
            {
                float distanceFromEnemy = Vector3.Distance(
                    _transform.position,
                    targetedEnemy.transform.position
                );

                // Enemy in sight -> attack
                if (_enemyInSight &&
                    (_focusEnemyTime < _focusEnemyTimer || distanceFromEnemy < 5f))
                {
                    if (Time.time > _fireTime)
                    {
                        _fireTime = Time.time +
                            (_pushTrigger ? waitForFireTime : fireTime);

                        _pushTrigger = !_pushTrigger;
                    }

                    if (_pushTrigger)
                        _characterInstance.CharacterItemManager.Fire1();

                    _characterInstance.SetActionKeyCode(
                        ActionCodes.Sprint,
                        false
                    );

                    if (distanceFromEnemy < DistanceFromTarget)
                    {
                        Vector3 newFreeVector;

                        if (_stepAsideCooldownTimer < Time.time)
                        {
                            // Setting cooldown to prevent enemy from moving all the time
                            _stepAsideCooldownTimer += 2f;

                            Vector3[] increasingDistancePossibleDirections =
                            {
                                -transform.right,
                                transform.right,
                                -transform.forward
                            };

                            newFreeVector = FreeSpace(
                                increasingDistancePossibleDirections,
                                8f
                            );

                            if (Vector3.Distance(
                                newFreeVector,
                                transform.position
                            ) > 0.5f)
                            {
                                SetTravelDestinationByNavMesh(
                                    newFreeVector,
                                    false
                                );
                            }
                        }
                    }
                    else
                    {
                        SetTravelDestinationByNavMesh(
                            targetedEnemy.transform.position,
                            false
                        );
                    }

                    LookAt(
                        targetedEnemy.GetPositionToAttack(),
                        timestep
                    );
                }
                else
                {
                    _characterInstance.SetActionKeyCode(
                        ActionCodes.Sprint,
                        distanceFromEnemy > 2f
                    );

                    // Enemy is not in sight: do not shoot.
                    // Move toward the enemy's current position.
                    if (!_maintainPosition)
                    {
                        SetTravelDestinationByNavMesh(
                            targetedEnemy.transform.position,
                            true,
                            true
                        );
                    }
                    else
                    {
                        float distanceFromTarget = Vector3.Distance(
                            transform.position,
                            _positionToMaintain
                        );

                        if (distanceFromTarget >
                            _maxAllowedDistanceFromPositionToMaintain)
                        {
                            SetTravelDestinationByNavMesh(
                                _positionToMaintain,
                                true,
                                true
                            );
                        }
                    }
                }
            }
            else
            {
                if (_maintainPosition)
                {
                    float distanceFromTarget = Vector3.Distance(
                        transform.position,
                        _positionToMaintain
                    );

                    if (distanceFromTarget >
                        _maxAllowedDistanceFromPositionToMaintain)
                    {
                        SetTravelDestinationByNavMesh(
                            _positionToMaintain,
                            true,
                            true
                        );
                    }
                }
                else if (LocalPatrol)
                {
                    PatrolRaid();
                }
                else
                {
                    SetTravelDestinationByNavMesh(
                        transform.position,
                        false
                    );
                }
            }
        }

        void PatrolRaid()
        {
            _characterInstance.SetActionKeyCode(ActionCodes.Sprint, false);

            if (hasRaidPatrolDestination)
            {
                float distance = Vector3.Distance(
                    _transform.position,
                    raidPatrolDestination
                );

                if (distance <= raidPatrolArrivalDistance)
                {
                    hasRaidPatrolDestination = false;
                    nextRaidPatrolTime =
                        Time.time + raidPatrolWaitTime;

                    _characterInstance.Input.Movement = Vector2.zero;
                }
                else if (_isMoving)
                {
                    return;
                }
                else
                {
                    // Si no pudo completar el recorrido, buscar otro destino.
                    hasRaidPatrolDestination = false;
                    nextRaidPatrolTime = Time.time + 0.5f;
                }
            }

            if (Time.time < nextRaidPatrolTime)
            {
                _characterInstance.Input.Movement = Vector2.zero;
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                Vector3 offset =
                    Random.insideUnitSphere * raidPatrolRadius;

                offset.y = 0f;

                Vector3 candidatePosition =
                    (GetComponent<RiftEnemyTerritory>() is var home && home ? home.Home : _transform.position) + offset;

                if (!NavMesh.SamplePosition(
                    candidatePosition,
                    out NavMeshHit hit,
                    3f,
                    NavMesh.AllAreas))
                {
                    continue;
                }

                if (Vector3.Distance(
                    _transform.position,
                    hit.position) <
                    raidPatrolArrivalDistance * 2f)
                {
                    continue;
                }

                NavMeshPath patrolPath = new NavMeshPath();

                bool validPath = NavMesh.CalculatePath(
                    _transform.position,
                    hit.position,
                    NavMesh.AllAreas,
                    patrolPath
                );

                if (!validPath ||
                    patrolPath.status != NavMeshPathStatus.PathComplete ||
                    patrolPath.corners.Length <= 1)
                {
                    continue;
                }

                raidPatrolDestination = hit.position;
                hasRaidPatrolDestination = true;

                SetTravelDestinationByNavMesh(
                    raidPatrolDestination,
                    true,
                    true
                );

                return;
            }

            // Reintentar si no encuentra un destino transitable.
            nextRaidPatrolTime = Time.time + 1f;
            _characterInstance.Input.Movement = Vector2.zero;
        }
        protected Vector3 FreeSpace(Vector3[] direction, float range)
        {
            Vector3 bestVector = _transform.position;

            for (int i = 0; i < direction.Length; i++)
            {
                if (_physicsScene.Raycast(
                    _transform.position + _transform.up * 0.25f,
                    direction[i],
                    out RaycastHit freePoint,
                    range,
                    GameManager.environmentLayer
                ))
                {
                    if (
                        Vector3.Distance(
                            _transform.position,
                            freePoint.point
                        ) > Vector3.Distance(
                            _transform.position,
                            bestVector
                        )
                        && freePoint.collider.gameObject.layer != 13
                    )
                    {
                        bestVector = freePoint.point;
                    }
                }
                else
                {
                    float difference =
                        Vector3.Distance(
                            _transform.position,
                            _transform.position + direction[i] * range
                        )
                        - Vector3.Distance(
                            _transform.position,
                            bestVector
                        );

                    if (Mathf.Abs(difference) < 2f)
                    {
                        if (UnityEngine.Random.Range(0, 3) == 1)
                        {
                            bestVector =
                                _transform.position + direction[i] * range;
                        }
                    }
                    else if (difference > 0)
                    {
                        bestVector =
                            _transform.position + direction[i] * range;
                    }
                }
            }

            return bestVector;
        }

        // Makes bot look at given spot; rotation is not instant.
        protected void LookAt(Vector3 _lookTarget, float timestep)
        {
            _characterInstance.characterMind.position =
                _characterInstance.Health.GetPositionToAttack();

            Vector3 lookRotationVector =
                _lookTarget - _characterInstance.characterMind.position;

            if (lookRotationVector != Vector3.zero)
            {
                mindRotation = Quaternion.Lerp(
                    mindRotation,
                    Quaternion.LookRotation(lookRotationVector),
                    RotationToTargetSpeed * timestep
                );
            }

            _characterInstance.characterMind.rotation = mindRotation;

            _characterInstance.Input.LookY =
                mindRotation.eulerAngles.y + _lerpedFuzzyLook.y;

            // Look up/down correction
            float lookx = -mindRotation.eulerAngles.x;
            float fixedLookX = lookx < -90 ? lookx += 360 : lookx;

            _characterInstance.Input.LookX =
                -fixedLookX + _lerpedFuzzyLook.x;
        }

        protected void LookAtSharp(Vector3 _lookTarget)
        {
            _characterInstance.characterMind.position =
                _characterInstance.Health.GetPositionToAttack();

            _characterInstance.characterMind.LookAt(_lookTarget);
            mindRotation = _characterInstance.characterMind.rotation;

            _characterInstance.Input.LookY = mindRotation.eulerAngles.y;

            // Look up/down correction
            float lookx = -mindRotation.eulerAngles.x;
            float fixedLookX = lookx < -90 ? lookx += 360 : lookx;

            _characterInstance.Input.LookX = -fixedLookX;
        }

        /// <summary>
        /// Can we see the current enemy, or is there something between us?
        /// </summary>
        protected bool EnemyInSight()
        {
            if (!targetedEnemy)
                return false;

            Vector3 direction =
                targetedEnemy.GetPositionToAttack()
                - _characterInstance.Health.GetPositionToAttack();

            return !_physicsScene.Raycast(
                _characterInstance.Health.GetPositionToAttack(),
                direction,
                direction.magnitude,
                GameManager.environmentLayer
            );
        }

        #region navigation tools

        protected virtual void MovementTick()
        {
            if (isFollowingPath)
            {
                desiredVelocity = transform.InverseTransformDirection(
                    directTravelTargetPoint - transform.position
                ).normalized;
            }
            else
            {
                desiredVelocity = Vector3.zero;
            }
        }

        protected void SetTravelDestinationByNavMesh(
            Vector3 destinationPosition,
            bool rotateToDestination,
            bool overrideCurrentIfExistPath = true
        )
        {
            if (_isMoving && !overrideCurrentIfExistPath)
                return;

            _rotateToDestination = rotateToDestination;
            _isMoving = true;

            NavMesh.CalculatePath(
                transform.position,
                destinationPosition,
                NavMesh.AllAreas,
                _path
            );

            _currentCorner = 1;
            _pathCorners = _path.corners.Length;
        }

        void UpdatePath()
        {
            if (!_isMoving)
                return;

            if (_currentCorner < _pathCorners)
            {
                directTravelTargetPoint = _path.corners[_currentCorner];

                desiredVelocity = transform.InverseTransformDirection(
                    directTravelTargetPoint - transform.position
                ).normalized;

                _characterInstance.Input.Movement = new Vector2(
                    Mathf.Clamp(desiredVelocity.x, -1.0f, 1.0f),
                    Mathf.Clamp(desiredVelocity.z, -1.0f, 1.0f)
                );

                if (_rotateToDestination)
                {
                    LookAt(
                        _path.corners[_currentCorner]
                        + new Vector3(
                            0,
                            _characterInstance.characterMind.localPosition.y
                        ),
                        Time.fixedDeltaTime
                    );
                }

                float distanceFromCurrentCorner = Vector3.Distance(
                    transform.position,
                    _path.corners[_currentCorner]
                );

                if (distanceFromCurrentCorner <= 0.35f)
                    _currentCorner++;
            }
            else
            {
                _isMoving = false;
                _characterInstance.Input.Movement = new Vector2(0, 0);
            }
        }

        #endregion

        protected Health GetClosestEnemy()
        {
            _potentialNearestEnemy = null;
            _potentialEnemy = null;

            if (targetedEnemy && targetedEnemy.CurrentHealth <= 0)
                targetedEnemy = null;

            if (_gamemode.PeacefulBots)
            {
                targetedEnemy = null;
                return null;
            }

            float lastDistance = float.MaxValue;
            bool ffa = _gamemode.FFA;
            int myTeam = _characterInstance.Health.Team;
            Vector3 we = transform.position;

            for (
                int characterUnit = 0;
                characterUnit < _gameSync.Characters.Obj.Length;
                characterUnit++
            )
            {
                CharacterInstance characterInstance =
                    _gameSync.Characters.Obj[characterUnit];

                if (characterInstance == null)
                    continue;

                _potentialEnemy = characterInstance.Health;

                if (
                    (_potentialEnemy.Team != myTeam || ffa)
                    && _potentialEnemy != _characterInstance.Health
                    && _potentialEnemy.CurrentHealth > 0
                )
                {
                    float distance = Vector3.Distance(
                     we,
                    _potentialEnemy.transform.position
                    );

                    var territory=GetComponent<RiftEnemyTerritory>();
                    if(territory && !territory.CanTarget(_potentialEnemy)) continue;
                    bool raidMode = LocalPatrol;

                    if (raidMode && distance > raidDetectionRange)
                        continue;

                    if (!RiftClassCombat.CanTarget(_potentialEnemy,we))continue;
                    distance*=RiftClassCombat.TargetPriority(_potentialEnemy);
                    if (distance < lastDistance)
                    {
                        _potentialNearestEnemy = _potentialEnemy;
                        lastDistance = distance;
                    }
                }
            }

            return _potentialNearestEnemy;
        }
    }
}