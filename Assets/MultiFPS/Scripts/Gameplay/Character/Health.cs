using UnityEngine;
using Mirror;

using MultiFPS.UI.HUD;
using MultiFPS;
using MultiFPS.UI;
using System;

namespace MultiFPS.Gameplay
{
    public sealed class Health : DNNetworkBehaviour
    {
        public static readonly System.Collections.Generic.List<Health> All=new System.Collections.Generic.List<Health>();
        protected override void OnEnable(){base.OnEnable();All.Add(this);}
        protected override void OnDisable(){All.Remove(this);base.OnDisable();}
        [Header("Health")]
        /// <summary>
        /// character name, used for example for killfeed
        /// </summary>
        public string CharacterName = "DEFAULT";
        public int CurrentHealth = 100;
        [SyncVar] public int MaxHealth = 100;
        [SyncVar(hook=nameof(TrainingChanged))] public bool TrainingTarget;
        [SyncVar] public int TrainingLastDamage,TrainingTotalDamage;
        [SyncVar] public double TrainingLastHit;
        public float CombatDamageMultiplier=1;
        public bool CombatTransfer;
        public double VulnerableUntil,WeakenedUntil;

        public int Team = -1;

        [Header("Element 1 -Body, 2 -Head, another ones can be custom")]
        [SerializeField] float[] damageMultipliers = { 1f, 2, 0.5f };


        //server callbacks
        public delegate void SHealthDepleted(CharacterPart hittedPartID, AttackType attackType, Health killer, int attackForce);
        public SHealthDepleted Server_OnHealthDepleted { set; get; } //when this object is killed on server



        public delegate void SOnDamaged(int currentHealth, CharacterPart damagedPart, AttackType attackType, Health attacker, int attackForce);
        public SOnDamaged Server_OnDamaged { set; get; } //when this object is killed on server



        //client callbacks
        public delegate void ClientHealthStateChanged(int currentHealth, CharacterPart damagedPart, AttackType attackType, byte attackerID);
        public ClientHealthStateChanged Client_OnHealthStateChanged { set; get; }



        public delegate void ClientHealthAdded(int currentHealth, int addedHealth, byte healerID);
        public ClientHealthAdded Client_OnHealthAdded { set; get; }



        public delegate void HealthDepleted(CharacterPart damagedPart, byte attackerID);
        public HealthDepleted Client_OnHealthDepleted { set; get; } //when this object is killed on client



        public delegate void KillConfirmation(CharacterPart damagedPart, Health victimID);
        public HealthDepleted Client_KillConfirmation { set; get; } //called when this object kill something
        public KilledCharacter Client_OnEarnedAssist { set; get; }


        public delegate void ClientDamageDealt(int currentHealth, int takenDamage, CharacterPart damagedPart, AttackType attackType, byte victimID);
        public ClientDamageDealt Client_OnDamageDealt { set; get; }


        public delegate void ResurrectEvent(int health);
        public ResurrectEvent Server_Resurrect { set; get; }
        public ResurrectEvent Client_Resurrect { set; get; }

        #region assists
        public delegate void KilledCharacter(Health victimID);
        public KilledCharacter Server_OnEarnedAssist;
        #endregion


        bool _dead = false;
        bool _clientDead = false;

        public Vector3 centerPosition = new Vector3(0, 1.5f, 0);

        #region register/deregister health object

        Transform _targetPositionTracker;

        public DamageMemory[] _damageMemories;
        int _lastDamageMemory = 0;

        bool nativeRegistered;
        public void ReleaseNativeRegistration(){if(nativeRegistered&&GameSync.Singleton){GameSync.Singleton.Healths.ServerDeregisterDNSyncObj(this);nativeRegistered=false;}}
        public void RegisterNativeSpawn(){if(_targetPositionTracker&&isServer&&GameSync.Singleton&&!nativeRegistered){GameSync.Singleton.Healths.ServerRegisterDNSyncObj(this);nativeRegistered=true;}}
        public void ResetNativeSpawn(){_dead=_clientDead=false;CombatDamageMultiplier=1;CombatTransfer=false;VulnerableUntil=WeakenedUntil=0;_lastDamageMemory=0;if(_damageMemories!=null)System.Array.Clear(_damageMemories,0,_damageMemories.Length);}
        void Start()
        {
            _damageMemories = new DamageMemory[6];

            gameObject.layer = (int)GameLayers.character;
            GameSync.Singleton.Healths.ServerRegisterDNSyncObj(this);nativeRegistered=isServer;

            //update 1.1: moved here from CharacterInstance
            //CustomSceneManager.RegisterCharacter(this); //we have to register spawned characters in order to let bot "see" them, and select nearest enemies from that register

            _targetPositionTracker = new GameObject("TargetTracker").transform;
            _targetPositionTracker.transform.SetParent(transform);
            _targetPositionTracker.transform.localPosition = centerPosition;
            _targetPositionTracker.transform.localRotation = Quaternion.identity;
        }
        void OnDestroy()
        {
            if(GameSync.Singleton&&nativeRegistered)GameSync.Singleton.Healths.ServerDeregisterDNSyncObj(this);

            //update 1.1: moved here from CharacterInstance
            //CustomSceneManager.DeRegisterCharacter(this);
        }
        #endregion

        //process taking damage server side
        public void Server_ChangeHealthState(int damage, CharacterPart damagedPart, AttackType attackType, Health attacker, int attackForce)
        {
            if (GameManager.Gamemode && GameManager.Gamemode.BlockDamage(this, attacker)) return;
            int finalDamage = RiftClassCombat.Resolve(this,attacker,Mathf.FloorToInt(damage * damageMultipliers[(int)damagedPart]));

            finalDamage = RiftCombat.CombatController.Resolve(this,attacker,finalDamage,out bool critical);
            if(TrainingTarget){RecordTrainingHit(finalDamage,damagedPart,attackType,attacker,attackForce,critical);return;}
            if (!(!GameManager.Gamemode.FriendyFire && attacker != this && attacker.Team == Team)) //avoid friendly fire
                CurrentHealth -= finalDamage;

            RiftCombat.CombatController.Feedback(attacker,this,Mathf.Min(finalDamage,Mathf.Max(0,CurrentHealth+finalDamage)),critical);
            RememberDamage(attacker, (short)finalDamage);

            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, 100000);
            RpcHealthStateChanged(CurrentHealth, finalDamage, damagedPart, attackType, attacker.DNID);

            Server_OnDamaged?.Invoke(CurrentHealth, damagedPart, attackType, attacker, attackForce);

            if (CurrentHealth <= 0 && !_dead)
            {
                RpcNotifyGamemode(damagedPart, attackType, attacker, attackForce, EvaluateAssist(attacker));
                Server_OnHealthDepleted?.Invoke(damagedPart, attackType, attacker, attackForce);
                _dead = true;
            }
        }

        //process taking damage server side
        public void Server_ChangeHealthStateRaw(int damage, CharacterPart damagedPart, AttackType attackType, Health attacker, int attackForce)
        {
            if (GameManager.Gamemode && GameManager.Gamemode.BlockDamage(this, attacker)) return;
            damage = RiftClassCombat.Resolve(this,attacker,damage);
            damage = RiftCombat.CombatController.Resolve(this,attacker,damage,out bool critical);
            if(TrainingTarget){RecordTrainingHit(damage,damagedPart,attackType,attacker,attackForce,critical);return;}
            if (!(!GameManager.Gamemode.FriendyFire && attacker != this && attacker.Team == Team)) //avoid friendly fir
                CurrentHealth -= damage;

            RiftCombat.CombatController.Feedback(attacker,this,Mathf.Min(damage,Mathf.Max(0,CurrentHealth+damage)),critical);
            RememberDamage(attacker, (short)damage);
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, 100000);

            if (CurrentHealth <= 0 && !_dead)
            {
                RpcNotifyGamemode(damagedPart, attackType, attacker, attackForce, EvaluateAssist(attacker));
                Server_OnHealthDepleted?.Invoke(damagedPart, attackType, attacker, attackForce);
                _dead = true;
            }

            RpcHealthStateChanged(CurrentHealth, damage, damagedPart, attackType, attacker.DNID);
        }

        void RememberDamage(Health attackerToRemember, short receivedDamage)
        {
            int currentID = DoWeRememberHim(attackerToRemember);

            if (currentID > -1)
            {
                _damageMemories[currentID].DamageDealt += (ushort)receivedDamage;
                _lastDamageMemory = currentID + 1;
            }
            else
            {
                if (_lastDamageMemory >= _damageMemories.Length)
                    _lastDamageMemory = 0;

                _damageMemories[_lastDamageMemory].DamageDealt = (ushort)receivedDamage;
                _damageMemories[_lastDamageMemory].PIID = attackerToRemember;

                _lastDamageMemory++;
            }

            int DoWeRememberHim(Health attacker)
            {
                for (int i = 0; i < _damageMemories.Length; i++)
                {
                    DamageMemory mem = _damageMemories[i];
                    if (mem.DamageDealt > 0 && mem.PIID == attacker) return i;
                }
                return -1;
            }
        }

        [ClientRpc]
        void RpcHealthStateChanged(int currentHealth, int takenDamage, CharacterPart hittedPart, AttackType attackType, byte attackerID)
        {
            if (!isServer) 
                CurrentHealth = currentHealth;

            Health attacker = GameSync.Singleton.Healths.GetObj(attackerID);

            if (!attacker) return;

            if (!_clientDead)
            {
                Client_OnHealthStateChanged?.Invoke(currentHealth, hittedPart, attackType, attacker.DNID);

                attacker.Client_OnDamageDealt?.Invoke(currentHealth, takenDamage, hittedPart, attackType, DNID);

                if (CurrentHealth <= 0)
                {
                    Client_OnHealthDepleted?.Invoke(hittedPart, attacker.DNID);


                    if (attacker)
                        attacker.Client_KillConfirmation?.Invoke(hittedPart, DNID);

                    _clientDead = true;
                }
            }
        }

        [ClientRpc]
        void RpcHealthAdded(int currentHealth, int addedHealth, byte healerID)
        {
            if (!isServer)
                CurrentHealth = currentHealth;

            Client_OnHealthAdded?.Invoke(currentHealth, addedHealth, healerID);
        }
        [ClientRpc]
        void RpcNotifyGamemode(CharacterPart hittedPart, AttackType attackType, Health attacker, int attackForce, Health assist)
        {
            GameManager.Gamemode.Client_PlayerKilledByPlayer?.Invoke(this, hittedPart, attackType, attacker, assist); //killfeed listens to this event
        }
        Health EvaluateAssist(Health killer)
        {
            int highestDmg = 0;
            Health id = null;

            DamageMemory mem;

            for (byte i = 0; i < _damageMemories.Length; i++)
            {
                mem = _damageMemories[i];
                if (mem.PIID != killer && mem.DamageDealt > highestDmg)
                {
                    highestDmg = mem.DamageDealt;
                    id = mem.PIID;
                }
            }

            if (id && highestDmg > Mathf.FloorToInt(MaxHealth * 0.5f))
            {
                id.Server_OnEarnedAssist?.Invoke(this);
                id.RpcOnAssistEarned(this);
                return id;
            }

            return null;
        }
        [ClientRpc]
        void RpcOnAssistEarned(Health victim)
        {
            Client_OnEarnedAssist?.Invoke(victim);
        }

        public int CountDamage(byte hittedPart, int damage)
        {
            return Mathf.FloorToInt(damage * damageMultipliers[hittedPart]);
        }

        //for AI to know where to aim at
        public Vector3 GetPositionToAttack()
        {
            return _targetPositionTracker.position;
        }

        [Server] public void ServerMakeTrainingTarget(){TrainingTarget=true;ServerSetVitals(100000,100000);TrainingChanged(false,true);}
        void TrainingChanged(bool before,bool after){if(after&&!GetComponent<TrainingTargetDisplay>())gameObject.AddComponent<TrainingTargetDisplay>();}
        void RecordTrainingHit(int damage,CharacterPart part,AttackType type,Health attacker,int force,bool critical){
            if(!attacker||attacker==this||attacker.Team==Team||damage<=0)return;
            if(NetworkTime.time-TrainingLastHit>10)TrainingTotalDamage=0;
            TrainingLastDamage=damage;TrainingTotalDamage=Mathf.Min(100000000,TrainingTotalDamage+damage);TrainingLastHit=NetworkTime.time;RiftWorldEvents.ReportTrainingHit(attacker);
            RiftCombat.CombatController.Feedback(attacker,this,damage,critical);
            RpcHealthStateChanged(CurrentHealth,damage,part,type,attacker.DNID);
        }

        [Server] public void ServerSetVitals(int max,int current){MaxHealth=Mathf.Max(1,max);CurrentHealth=Mathf.Clamp(current,1,MaxHealth);RpcSetVitals(MaxHealth,CurrentHealth);}
        [ClientRpc] void RpcSetVitals(int max,int current){MaxHealth=max;CurrentHealth=current;Client_OnHealthAdded?.Invoke(current,0,DNID);}

        //execute only on server, then server will update health state for every client
        public int ServerHeal(int healthToAdd, byte healerID) 
        {
            if (CurrentHealth == MaxHealth) return 0;

            int neededHealth = MaxHealth - CurrentHealth;

            if (neededHealth > healthToAdd)
                neededHealth = healthToAdd;

            CurrentHealth += neededHealth;

            RpcHealthAdded(CurrentHealth, neededHealth, healerID);

            return neededHealth;
        }

        public void ServerResurrect()
        {
            //prepare client for resurrection
            RpcClientResurrect();

            //resurrect
            _dead = false;
            CurrentHealth = MaxHealth;
            Server_Resurrect?.Invoke(MaxHealth);
        }

        [ClientRpc]
        public void RpcClientResurrect()
        {
            _clientDead = false;
            CurrentHealth = MaxHealth;
            Client_Resurrect?.Invoke(CurrentHealth);
        }
    }

    [System.Serializable]
    public struct DamageMemory
    {
        public Health PIID;
        public int DamageDealt;
    }

    //damage message sent through network
    public struct DamageAction
    {
        public short CurrentHealth;
        public short DamageDealt;
        public byte HittedPart;
        public byte AttackType;
        public byte AttackForce;
        public byte Attacker;

        public DamageAction(short currentHealth, short damageDealt, byte hittedPart, AttackType attackType, byte attackForce, byte attacker)
        {
            CurrentHealth = currentHealth;
            DamageDealt = damageDealt;
            HittedPart = hittedPart;
            AttackType = (byte)attackType;
            AttackForce = attackForce;
            Attacker = attacker;
        }
    }

    public enum CharacterPart : byte
    {
        body,
        head,
        legs,
    }
}