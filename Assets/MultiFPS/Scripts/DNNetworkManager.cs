using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using MultiFPS.Gameplay;
using System;

namespace MultiFPS
{
    public class DNNetworkManager : NetworkManager
    {
        public static DNNetworkManager Instance;
        //this event exist to send late players data about gamemode and equipment of other players.
        public delegate void NewPlayerJoinedTheGame(NetworkConnectionToClient conn);
        public NewPlayerJoinedTheGame OnNewPlayerConnected { get; set; }

        public delegate void PlayerDisconnected(NetworkConnectionToClient conn);
        public PlayerDisconnected OnPlayerDisconnected { get; set; }

        //Toggle Mirror's GUI for hosting game and connecting 
        bool _guiSet;

        public Action<string, ushort> Action_ConnectToTheGame;
        public Action<ushort> Action_SetNetworkManagerPort { get; set; }
        

        public override void Awake()
        {
            base.Awake();
            var mage=Gameplay.RiftCombat.MagePrototypeSettings.Load;if(mage&&mage.PlayerPrefab&&!spawnPrefabs.Contains(mage.PlayerPrefab))spawnPrefabs.Add(mage.PlayerPrefab);
            var paladin=Gameplay.RiftCombat.PaladinPrototypeSettings.Load;if(paladin&&paladin.PlayerPrefab&&!spawnPrefabs.Contains(paladin.PlayerPrefab))spawnPrefabs.Add(paladin.PlayerPrefab);
            Debug.Log("MultiFPS | desNetware.net | discord.gg/REnFR3wXNY");

            headlessStartMode = HeadlessStartOptions.DoNothing;

            if (Instance)
            {
                Debug.LogError("Fatal error, two instances of DNNetworkManager");
                Destroy(Instance.gameObject);
            }

            Instance = this;

            offlineScene = SceneManager.GetActiveScene().path;
        }

        #region server setup
        public override void OnStartServer()
        {
            ChatBehaviour.ResetServerSession();
            NetworkServer.RegisterHandler<ClientSendPositionMessage>(OnReceivedPlayerPositionMessage);
            NetworkServer.RegisterHandler<ClientSendInputMessage>(OnReceivedPlayerInputMessage);
        }
        public override void OnStopServer()
        {
            ChatBehaviour.ResetServerSession();
            NetworkServer.UnregisterHandler<ClientSendPositionMessage>();
            NetworkServer.UnregisterHandler<ClientSendInputMessage>();
        }
        #endregion

        #region client setup
        public override void OnClientConnect()
        {
            base.OnClientConnect();
            NetworkClient.RegisterHandler<CharactersInputMessage>(OnClientReceivedCharactersInputs);
            NetworkClient.RegisterHandler<CharactersPositionMessage>(OnClientReceivedCharactersPositions);
        }
        public override void OnClientDisconnect()
        {
            NetworkClient.UnregisterHandler<CharactersInputMessage>();
            NetworkClient.UnregisterHandler<CharactersPositionMessage>();
        }
        #endregion

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if(!RiftSessionParty.CanAdmit(MultiFPS.Gameplay.Gamemodes.RoomSetup.Properties.P_Gamemode,NetworkServer.connections.Values.Count(c=>c!=null&&c.identity))){conn.Disconnect();return;}
            base.OnServerAddPlayer(conn);
            OnNewPlayerConnected?.Invoke(conn);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            OnPlayerDisconnected?.Invoke(conn);
            base.OnServerDisconnect(conn);
        }

        public override void Update()
        {
            base.Update();

#if UNITY_EDITOR

            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                _guiSet = !_guiSet;

                GetComponent<NetworkManagerHUD>().enabled = _guiSet;
                Cursor.visible = _guiSet;
                if (_guiSet)
                {
                    Cursor.lockState = CursorLockMode.Confined;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
#endif
        }

        public void ConnectToTheGame()
        {
            StartClient();
        }

        public void SetAddressAndPort(string address, string port)
        {
            if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(port)) return;

            networkAddress = address;

            ushort uport = (ushort)System.Convert.ToInt32(port);

            Action_SetNetworkManagerPort?.Invoke(uport);
        }

        #region gameplay synchronization handlers
        void OnReceivedPlayerPositionMessage(NetworkConnectionToClient conn, ClientSendPositionMessage pos)
        {
            if(conn==null||!conn.isReady||NetworkServer.isLoadingScene)return;
            var player=GameManager.GetPlayerByConnID(conn.connectionId);
            if(!player||player.connectionToClient!=conn||conn.identity!=player.netIdentity)return;
            CharacterInstance characterInstance=player.CurrentlyControlledCharacter;

            if (!characterInstance) return;
            if (characterInstance.Health.CurrentHealth <= 0) return;

            characterInstance.PrepareCharacterToLerp();
            characterInstance.transform.position = pos.Position;
            characterInstance.SetCurrentPositionTargetToLerp(pos.Position);
            characterInstance.DnTransform.ReceivePositionFromClient(pos.Position); //cache position to dntransform so it can be sent to other clients on next tick
        }
        void OnReceivedPlayerInputMessage(NetworkConnectionToClient conn, ClientSendInputMessage pos)
        {
            if(conn==null||!conn.isReady||NetworkServer.isLoadingScene)return;
            var player=GameManager.GetPlayerByConnID(conn.connectionId);
            if(!player||player.connectionToClient!=conn||conn.identity!=player.netIdentity)return;
            var actor=player.CurrentlyControlledCharacter;
            if(actor&&actor.Health&&actor.Health.CurrentHealth>0)actor.ReadAndApplyInputFromMessage(pos);
        }

        void OnClientReceivedCharactersInputs(CharactersInputMessage msg)
        {
            if (!GameSync.Singleton) return;

            GameSync.Singleton.ClientUpdateCharactersInputs(msg.Inputs, msg.StateID);
        }
        void OnClientReceivedCharactersPositions(CharactersPositionMessage msg)
        {
            if (!GameSync.Singleton) return;

            GameSync.Singleton.ClientUpdateCharacterPositions(msg.Positions, msg.StateID);
        }
        #endregion
    }

    public struct CharactersPositionMessage : NetworkMessage
    {
        public ushort StateID;
        public CharacterPositionMsg[] Positions;
    }
    public struct CharacterPositionMsg
    {
        public byte CharacterID;
        public Vector3 Position;

        public CharacterPositionMsg(CharacterInstance character)
        {
            CharacterID = character.DNID;
            Position = character.transform.position;
        }
    }
    public struct CharactersInputMessage : NetworkMessage
    {
        public ushort StateID;
        public CharacterInputMessage[] Inputs;
    }

    public struct ClientSendInputMessage : NetworkMessage
    {
        public byte Movement;
        public sbyte LookX;
        public short LookY;
        public byte ActionCodes; //sprint, fire1, fire2, and 5 free inputs to utilize   
    }
    public struct ClientSendPositionMessage : NetworkMessage
    {
        public Vector3 Position;
    }
    public struct HealthBatchMsg
    {
        public byte DNID;
        public DamageAction[] DmgAction;

        public HealthBatchMsg(byte dnid, List<DamageAction> dmgs)
        {
            DNID = dnid;
            DmgAction = dmgs.ToArray();
        }
    }
}