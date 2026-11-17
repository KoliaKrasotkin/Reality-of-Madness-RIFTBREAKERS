// Production method bodies are inserted mechanically by the accompanying extractor.
// Unity objects and Mirror transport/RPC emission are doubles, not a live Host.
using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static implicit operator bool(Object o) => !ReferenceEquals(o,null) && !o.Destroyed;
        public static bool operator ==(Object a,Object b) => (!a && !b) || ReferenceEquals(a,b);
        public static bool operator !=(Object a,Object b) => !(a==b);
        public override bool Equals(object o) => ReferenceEquals(this,o);
        public override int GetHashCode() => base.GetHashCode();
    }
    public struct Color { public static Color red, cyan; }
    public static class ColorUtility { public static string ToHtmlStringRGBA(Color c)=>"FF0000FF"; }
    public static class Debug { public static int Warnings, Errors; public static void LogWarning(object s){Warnings++;} public static void LogError(object s){Errors++;} }
}
namespace Mirror
{
    public static class NetworkServer { public static bool active; }
    public class NetworkIdentity : UnityEngine.Object { public uint netId; public bool isServer; }
    public class NetworkConnection { }
    public class NetworkConnectionToClient : NetworkConnection { }
}
public class DNNetworkBehaviour : UnityEngine.Object
{
    public NetworkIdentity netIdentity = new NetworkIdentity{isServer=true,netId=17};
    public bool isServer => netIdentity.isServer;
    public uint netId => netIdentity.netId;
    public byte DNID {get;private set;}
    public string name="object";
    public void SetDNID(byte id){DNID=id;}
    public T GetComponent<T>() where T:class => this as T;
    public T[] GetComponents<T>() where T:class => new[]{this as T};
}
public static class Wire
{
    public static int Invalid, Sent;
    public static void Send(DNNetworkBehaviour sender)
    {
        if(!NetworkServer.active){Invalid++;return;}
        if(!sender.isServer){Debug.Warnings++;return;}
        Sent++;
    }
}
public struct RpcRegisterDNSyncObjectMsg {public uint MirrorNetID; public byte PoolID,DNID; public ushort StateHash;}
public struct RpcDeregisterDNSyncObjectMsg {public uint MirrorNetID; public byte PoolID,DNID; public ushort StateHash;}
public class LobbySynchronizer : DNNetworkBehaviour
{
    public int Deregisters, Registers;
    public RpcDeregisterDNSyncObjectMsg Last;
    public void ClientDeregisterObj(RpcDeregisterDNSyncObjectMsg msg){Deregisters++;Last=msg;Wire.Send(this);}
    public void ClientRegisterMsg(RpcRegisterDNSyncObjectMsg msg){Registers++;Wire.Send(this);}
}
public class PoolBase
{
    protected string _poolName="test";
    protected byte _poolID=7,_maxSize=4;
    protected NetworkIdentity[] _netIDs=new NetworkIdentity[4];
    public ushort PoolStateHash {get;protected set;}
    public byte RegisteredObjs {get;protected set;}
    public virtual void ServerRegisterDNSyncObj(DNNetworkBehaviour obj){}
    public virtual void ServerDeregisterDNSyncObj(DNNetworkBehaviour obj){}
}
public class Pool<T> : PoolBase where T:class
{
    LobbySynchronizer _synchronizer;
    public T[] Obj=new T[4];
    public Pool(LobbySynchronizer sync){_synchronizer=sync;}
    public NetworkIdentity At(int i)=>_netIDs[i];
    public void SetHash(ushort hash){PoolStateHash=hash;}
    public override void ServerRegisterDNSyncObj(DNNetworkBehaviour behaviour)
        {
            if (!_synchronizer.isServer) return;

            PoolStateHash++;
            if (PoolStateHash == ushort.MaxValue)
                PoolStateHash = 0;

            RegisteredObjs++;

            for (byte i = 0; i < _maxSize; i++)
            {
                if (_netIDs[i] != null) continue;

                _netIDs.SetValue(behaviour.netIdentity, i);
                Obj.SetValue(behaviour.GetComponent<T>(), i);

                DNNetworkBehaviour[] dnBeh = behaviour.GetComponents<DNNetworkBehaviour>();

                for (int j = 0; j < dnBeh.Length; j++)
                {
                    if (dnBeh[j].GetType() == typeof(T))
                        dnBeh[j].SetDNID(i);
                }

                _synchronizer.ClientRegisterMsg(new RpcRegisterDNSyncObjectMsg
                {
                    MirrorNetID = behaviour.netId,
                    PoolID = _poolID,
                    DNID = i,
                    StateHash = PoolStateHash
                });
                return;
            }

            Debug.LogError($"Limit of registered synchronized object reached for pool: {_poolName}");
        }
    public override void ServerDeregisterDNSyncObj(DNNetworkBehaviour netObj)
        {
            if (!_synchronizer || !_synchronizer.netIdentity || !_synchronizer.isServer || !netObj) return;

            if (netObj.DNID == byte.MaxValue) return; //it was never registered, so dont try to deregister
            byte id = netObj.DNID;
            if (id >= _netIDs.Length || ReferenceEquals(_netIDs[id], null) || !ReferenceEquals(netObj.netIdentity, _netIDs[id])) {
                Debug.LogWarning($"Server tried to deregister obj that was not registered: {netObj.name}");
                return;
            }

            uint mirrorId = netObj.netId;

            PoolStateHash++;
            if (PoolStateHash == ushort.MaxValue)
                PoolStateHash = 0;

            RegisteredObjs--;

            _netIDs[id] = null;
            Obj[id] = null;
            netObj.SetDNID(byte.MaxValue);

            // Mirror keeps isServer after shutdown. Local cleanup is still needed,
            // but an RPC is only valid while the server is actually active.
            if (!NetworkServer.active) return;

            _synchronizer.ClientDeregisterObj(new RpcDeregisterDNSyncObjectMsg
            {
                MirrorNetID = mirrorId,
                PoolID = _poolID,
                DNID = id,
                StateHash = PoolStateHash
            });
        }
}
public class DNNetworkManager : UnityEngine.Object
{
    public static DNNetworkManager Instance;
    public Action<NetworkConnection> OnNewPlayerConnected;
}
public class Gamemode : DNNetworkBehaviour
{
    public class Team {public List<PlayerInstance> PlayerInstances=new List<PlayerInstance>();}
    public List<Team> _teams=new List<Team>{new Team(),new Team()};
    public int Chats,RemovedEvents,Checks,Relays;
    public Action RemovedHook;
    public void Server_WriteToChat(string msg){Chats++;Wire.Send(this);}
    protected void CheckTeamStates(){Checks++;Wire.Send(this);}
    protected void OnPlayerRemovedFromTeam(PlayerInstance player,int team){RemovedEvents++;RemovedHook?.Invoke();Wire.Send(this);}
    public void Relay_ClientDisconnected(NetworkConnection conn,NetworkIdentity identity){Relays++;Wire.Send(this);}
    protected void RemovePlayerFromTeam(PlayerInstance player) 
        {
            if (player.Team != -1)
            {
                _teams[player.Team].PlayerInstances.Remove(player);
                OnPlayerRemovedFromTeam(player, player.Team);
            }
            else
                Debug.LogError("MultiFPS: TRYING TO REMOVE PLAYER FROM TEAM WHO DOES NOT BELONG TO ANY TEAM");
        }
    public void Server_OnPlayerInstanceRemoved(PlayerInstance player)
        {
            if (!netIdentity || !isServer) return;

            // OnDestroy may run after Mirror has stopped the server, while
            // isServer remains true. Remove local references without callbacks/RPCs.
            if (!NetworkServer.active)
            {
                foreach (var team in _teams) team.PlayerInstances.Remove(player);
                return;
            }

            //if player disconnects and was in certain team, then remove him from it
            if (player.Team != -1)
                RemovePlayerFromTeam(player);

            //say on chat that someone left the game
            Server_WriteToChat($"<color=#{ColorUtility.ToHtmlStringRGBA(Color.red)}>" + player.PlayerInfo.Username + " left the game</color>");

            //check if game can still run after player disconnected
            CheckTeamStates();
        }
}
public class Raid : Gamemode
{
    public int CheckpointCalls;
    public void UnregisterCheckpoint(PlayerInstance player){if(!NetworkServer.active || player==null)return;CheckpointCalls++;}
}
public static class GameManager
{
    public static List<PlayerInstance> Players=new List<PlayerInstance>();
    public static Dictionary<int,PlayerInstance> PlayersByConnID=new Dictionary<int,PlayerInstance>();
    public static Gamemode Gamemode;
    public static int Removed;
    public static void RemovePlayerInstance(PlayerInstance player){Removed++;Players.Remove(player);}
    public static void RemoveConnectedPlayer(int connID,PlayerInstance expected=null)
        {
            if(PlayersByConnID.TryGetValue(connID,out var current)&&(ReferenceEquals(expected,null)||ReferenceEquals(current,expected)))PlayersByConnID.Remove(connID);
        }
}
public class PlayerInstance : DNNetworkBehaviour
{
    public struct Info {public string Username;}
    public Info PlayerInfo=new Info{Username="Warrior"};
    public int Team=0;
    bool _disconnected, _registeredPlayerInstance, _subbedToNetworkManager;
    int _connectionToClient=-1;
    public NetworkConnectionToClient connectionToClient=new NetworkConnectionToClient();
    void UpdateDataForLatePlayer(NetworkConnection conn){}
    public void Prepare(int connection=4,bool registered=true)
    {
        _connectionToClient=connection;_registeredPlayerInstance=registered;
        _subbedToNetworkManager=true;DNNetworkManager.Instance.OnNewPlayerConnected+=UpdateDataForLatePlayer;
        GameManager.Players.Add(this);
        if(connection!=-1)GameManager.PlayersByConnID[connection]=this;
        GameManager.Gamemode?._teams[Team].PlayerInstances.Add(this);
    }
    public bool Cleared => _disconnected&&!_registeredPlayerInstance&&!_subbedToNetworkManager&&_connectionToClient==-1;
    public void DestroyNow(){OnDestroy();}
    public void Disconnect()
        {
            if (_disconnected) return;
            _disconnected = true;

            GameManager.RemovePlayerInstance(this);

            if (_subbedToNetworkManager)
            {
                _subbedToNetworkManager = false;
                if (DNNetworkManager.Instance)
                    DNNetworkManager.Instance.OnNewPlayerConnected -= UpdateDataForLatePlayer;
            }

            var gamemode = GameManager.Gamemode;
            bool serverContext = gamemode && gamemode.netIdentity && gamemode.isServer;

            if (_registeredPlayerInstance)
            {
                _registeredPlayerInstance = false;
                if (serverContext) gamemode.Server_OnPlayerInstanceRemoved(this);
            }
            
            if (_connectionToClient != -1)
            {
                int connectionId = _connectionToClient;
                _connectionToClient = -1;
                if (NetworkServer.active && serverContext)
                    gamemode.Relay_ClientDisconnected(connectionToClient, this.netIdentity);
                GameManager.RemoveConnectedPlayer(connectionId,this);
            }
            if (gamemode is Raid raid)
            {
                raid.UnregisterCheckpoint(this);
            }

        }
    private void OnDestroy()
        {
            Disconnect();
        }
}
static class Checks
{
    static int count;
    static void Assert(bool value,string message){count++;if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
    static void Reset(bool active=true)
    {
        NetworkServer.active=active;Wire.Invalid=Wire.Sent=0;Debug.Warnings=Debug.Errors=0;
        GameManager.Players.Clear();GameManager.PlayersByConnID.Clear();GameManager.Removed=0;
        GameManager.Gamemode=new Raid();DNNetworkManager.Instance=new DNNetworkManager();
    }
    public static int Main()
    {
        try
        {
            Reset();var sync=new LobbySynchronizer();var pool=new Pool<DNNetworkBehaviour>(sync);var actor=new DNNetworkBehaviour();
            pool.ServerRegisterDNSyncObj(actor);byte id=actor.DNID;
            Assert(pool.RegisteredObjs==1&&sync.Registers==1,"active register unchanged");
            pool.ServerDeregisterDNSyncObj(actor);
            Assert(pool.RegisteredObjs==0&&pool.At(id)==null&&pool.Obj[id]==null,"active deregister clears pool");
            Assert(sync.Deregisters==1&&Wire.Invalid==0,"active deregister still sends RPC");
            Assert(sync.Last.DNID==id&&sync.Last.MirrorNetID==17&&sync.Last.StateHash==2&&sync.Last.PoolID==7,"active wire fields retain old slot, netId and hash");
            Assert(actor.DNID==byte.MaxValue,"released slot invalidated");
            pool.ServerDeregisterDNSyncObj(actor);
            Assert(pool.RegisteredObjs==0&&sync.Deregisters==1&&pool.PoolStateHash==2,"second deregister is idempotent");
            pool.ServerRegisterDNSyncObj(actor);
            Assert(pool.RegisteredObjs==1&&actor.DNID==id,"instance re-registration receives valid slot");
            pool.SetHash(ushort.MaxValue-1);pool.ServerDeregisterDNSyncObj(actor);
            Assert(pool.PoolStateHash==0&&sync.Last.StateHash==0,"original hash wrap preserved");
            Reset();sync=new LobbySynchronizer();pool=new Pool<DNNetworkBehaviour>(sync);actor=new DNNetworkBehaviour();pool.ServerRegisterDNSyncObj(actor);
            NetworkServer.active=false;int sent=Wire.Sent;pool.ServerDeregisterDNSyncObj(actor);
            Assert(sync.isServer&&!NetworkServer.active,"Mirror sticky isServer model reproduced");
            Assert(pool.RegisteredObjs==0&&pool.Obj[0]==null&&actor.DNID==255,"stopped server still clears local pool");
            Assert(Wire.Sent==sent&&sync.Deregisters==0&&Wire.Invalid==0,"stopped server emits no deregistration RPC");
            pool.ServerDeregisterDNSyncObj(actor);
            Assert(pool.RegisteredObjs==0&&Debug.Warnings==0,"stopped duplicate does not underflow or warn");
            Reset();sync=new LobbySynchronizer();pool=new Pool<DNNetworkBehaviour>(sync);actor=new DNNetworkBehaviour();pool.ServerRegisterDNSyncObj(actor);
            var stranger=new DNNetworkBehaviour();stranger.SetDNID(actor.DNID);pool.ServerDeregisterDNSyncObj(stranger);
            Assert(pool.RegisteredObjs==1&&sync.Deregisters==0&&Debug.Warnings==1,"foreign identity still warns without removing owner");
            stranger.SetDNID(9);pool.ServerDeregisterDNSyncObj(stranger);
            Assert(pool.RegisteredObjs==1&&Debug.Warnings==2,"invalid index warns instead of corrupting array");
            actor.netIdentity.Destroyed=true;stranger.netIdentity.Destroyed=true;stranger.SetDNID(actor.DNID);pool.ServerDeregisterDNSyncObj(stranger);
            Assert(pool.RegisteredObjs==1&&Debug.Warnings==3,"fake-null distinct identities cannot clear each other");
            pool.ServerDeregisterDNSyncObj(actor);
            Assert(pool.RegisteredObjs==0&&sync.Deregisters==1,"own destroyed identity can release its recorded slot");
            Reset();sync=new LobbySynchronizer();pool=new Pool<DNNetworkBehaviour>(sync);actor=new DNNetworkBehaviour();pool.ServerRegisterDNSyncObj(actor);
            sync.netIdentity.isServer=false;pool.ServerDeregisterDNSyncObj(actor);
            Assert(pool.RegisteredObjs==1&&sync.Deregisters==0,"client context cannot deregister server pool");
            sync.netIdentity=null;pool.ServerDeregisterDNSyncObj(actor);
            Assert(sync.Deregisters==0,"missing synchronizer network context is safe");
            sync.Destroyed=true;pool.ServerDeregisterDNSyncObj(actor);pool.ServerDeregisterDNSyncObj(null);
            Assert(sync.Deregisters==0,"destroyed synchronizer and absent actor are safe");

            Reset();var player=new PlayerInstance();player.Prepare();var mode=GameManager.Gamemode;
            player.Disconnect();
            Assert(mode.Chats==1&&mode.RemovedEvents==1&&mode.Checks==1&&mode.Relays==1,"active player removal preserves all callbacks");
            Assert(GameManager.Players.Count==0&&GameManager.PlayersByConnID.Count==0&&mode._teams[0].PlayerInstances.Count==0,"active player references removed");
            Assert(player.Cleared&&DNNetworkManager.Instance.OnNewPlayerConnected==null,"active flags and subscription cleared");
            sent=Wire.Sent;NetworkServer.active=false;player.DestroyNow();player.Disconnect();
            Assert(GameManager.Removed==1&&mode.Chats==1&&Wire.Sent==sent&&Wire.Invalid==0,"Disconnect followed by OnDestroy cleans once");

            Reset();player=new PlayerInstance();player.Prepare();mode=GameManager.Gamemode;NetworkServer.active=false;player.DestroyNow();
            Assert(GameManager.Players.Count==0&&GameManager.PlayersByConnID.Count==0&&mode._teams[0].PlayerInstances.Count==0,"late OnDestroy clears local references after shutdown");
            Assert(mode.Chats==0&&mode.RemovedEvents==0&&mode.Checks==0&&mode.Relays==0&&Wire.Invalid==0,"late OnDestroy sends no RPC or match callback");
            Assert(player.Cleared&&GameManager.Removed==1,"late destruction is idempotent");
            player.DestroyNow();Assert(GameManager.Removed==1,"second destruction has no repeated cleanup");

            Reset();player=new PlayerInstance();player.Prepare();mode=GameManager.Gamemode;mode.RemovedHook=player.Disconnect;player.Disconnect();
            Assert(GameManager.Removed==1&&mode.Chats==1&&mode.Relays==1,"reentrant removal cannot deregister twice");
            Reset();player=new PlayerInstance();player.Prepare();GameManager.Gamemode=null;DNNetworkManager.Instance=null;player.DestroyNow();
            Assert(player.Cleared&&GameManager.PlayersByConnID.Count==0&&Wire.Invalid==0,"missing managers do not block local cleanup");
            Reset();player=new PlayerInstance();player.Prepare();mode=GameManager.Gamemode;mode.netIdentity=null;player.DestroyNow();
            Assert(player.Cleared&&mode.Chats==0&&mode.Relays==0,"lost gamemode identity sends no callbacks");
            Reset();player=new PlayerInstance();player.Prepare();var replacement=new PlayerInstance();GameManager.PlayersByConnID[4]=replacement;player.DestroyNow();
            Assert(ReferenceEquals(GameManager.PlayersByConnID[4],replacement),"reused connection keeps replacement player");
            Reset();player=new PlayerInstance();player.Prepare(-1,false);mode=GameManager.Gamemode;player.DestroyNow();
            Assert(mode.Chats==0&&mode.Relays==0&&GameManager.Players.Count==0,"client or unregistered player performs local cleanup only");
            Reset();var party=new PlayerInstance[4];for(int i=0;i<4;i++){party[i]=new PlayerInstance();party[i].Prepare(i);}
            mode=GameManager.Gamemode;party[1].Disconnect();
            Assert(GameManager.Players.Count==3&&GameManager.PlayersByConnID.Count==3&&mode._teams[0].PlayerInstances.Count==3,"four-player model keeps three connected players");
            Assert(mode.Chats==1&&mode.Checks==1&&mode.Relays==1,"four-player removal synchronizes exactly once");
            NetworkServer.active=false;for(int i=0;i<4;i++)party[i].DestroyNow();
            Assert(GameManager.Players.Count==0&&GameManager.PlayersByConnID.Count==0&&mode._teams[0].PlayerInstances.Count==0,"four-player shutdown clears all remaining references");
            Assert(Wire.Invalid==0&&mode.Chats==1&&mode.Relays==1,"four-player shutdown produces no late RPC");
            Console.WriteLine("RESULT "+count+" PASS; extracted production methods, Unity/Mirror lifecycle and RPC transport simulated.");return 0;
        }
        catch(Exception exception){Console.WriteLine("FAIL "+exception);return 1;}
    }
}
