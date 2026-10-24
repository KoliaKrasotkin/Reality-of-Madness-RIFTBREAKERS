using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using Mirror;using MultiFPS.Gameplay;
namespace MultiFPS {
[RequireComponent(typeof(PlayerInstance))]public class ChatBehaviour:NetworkBehaviour {
 PlayerInstance player;readonly RiftChatRateGate rate=new RiftChatRateGate();double partyReady,noticeReady;
 public static ChatBehaviour _instance{get;private set;}public bool ChatWriting{get;private set;}
 public static event Action<RiftChatLine> Received;public static event Action<byte> Notice;public static event Action PartyChanged;
 public static uint ClientLeader,InviteFrom,InviteToken;public static uint[] ClientMembers=Array.Empty<uint>();public static string[] ClientNames=Array.Empty<string>();public static string InviteName="";public static double InviteExpires;
 static readonly Dictionary<GameObject,double> npcNext=new Dictionary<GameObject,double>();static double npcPrune;
 public static void ResetServerSession(){npcNext.Clear();npcPrune=0;RiftSessionParty.ResetSession();}
 public PlayerInstance Player=>player;public bool Ready=>player&&!player.BOT&&connectionToClient!=null&&connectionToClient.isReady;public bool Alive=>Ready&&player.MyCharacter&&player.MyCharacter.Health&&player.MyCharacter.Health.CurrentHealth>0;
 void Awake(){player=GetComponent<PlayerInstance>();}public override void OnStartLocalPlayer(){_instance=this;ClearClient();}public override void OnStopLocalPlayer(){if(_instance==this){SetTyping(false);_instance=null;ClearClient();}}
 public override void OnStopServer(){RiftSessionParty.Remove(netId);}
 void Update(){if(isServer)RiftSessionParty.Tick();}
 public void SetTyping(bool value){ChatWriting=value;}
 static void ClearClient(){ClientLeader=InviteFrom=InviteToken=0;ClientMembers=Array.Empty<uint>();ClientNames=Array.Empty<string>();InviteName="";InviteExpires=0;PartyChanged?.Invoke();}
 [Server]public void RpcHandleChatClientMessage(string message){if(player&&player.BOT&&player.MyCharacter)ServerNpcLine(player.MyCharacter.gameObject,"bot:"+netId,player.MyCharacter.Health.CharacterName,message);}
 [Command(channel=0)]public void CmdRelayClientMessage(string message){ServerSend(0,message);}
 [Command(channel=0)]public void CmdSendChannel(byte channel,string message){ServerSend(channel,message);}
 [Server]public bool ServerSend(byte channel,string message){var t=RiftSessionChatTuning.Load;if(!Alive||message==null||message.Length>t.Characters)return false;if(channel>1)return false;var plain=RiftSessionChatTuning.Plain(message,t.Characters);if(plain.Length==0)return false;if(!rate.Take(NetworkTime.time,t.Burst,t.MessagesPerSecond)){Status(1);return false;}if(channel==1&&!RiftSessionParty.HasGroup(netId)){Status(2);return false;}var source=player.MyCharacter;var name=RiftSessionChatTuning.Plain(player.PlayerInfo.Username,24);if(string.IsNullOrEmpty(name))name="Jugador";foreach(var target in ServerRecipients(this,channel))target.Deliver(source.netId,"",name,plain,source.transform.position,channel);return true;}
 public static ChatBehaviour[] ServerRecipients(ChatBehaviour source,byte channel){if(!NetworkServer.active||!source||!source.Alive)return Array.Empty<ChatBehaviour>();return NetworkServer.spawned.Values.Select(n=>n?n.GetComponent<ChatBehaviour>():null).Where(t=>t&&t.Alive&&t.player.MyCharacter.gameObject.scene==source.player.MyCharacter.gameObject.scene&&(channel==0?Vector3.SqrMagnitude(t.player.MyCharacter.transform.position-source.player.MyCharacter.transform.position)<=RiftSessionChatTuning.Load.Radius*RiftSessionChatTuning.Load.Radius:channel==1&&RiftSessionParty.Together(source.netId,t.netId))).ToArray();}
 [Server]public void Deliver(uint actor,string npc,string name,string text,Vector3 point,byte channel){if(Ready)TargetLine(connectionToClient,actor,npc,name,text,point,channel);}
 [TargetRpc(channel=0)]void TargetLine(NetworkConnectionToClient connection,uint actor,string npc,string name,string text,Vector3 point,byte channel){Received?.Invoke(new RiftChatLine(actor,npc,name,text,point,channel));}
 [Server]public void Status(byte code){if(Ready&&NetworkTime.time>=noticeReady){noticeReady=NetworkTime.time+1;TargetNotice(connectionToClient,code);}}
 [TargetRpc(channel=0)]void TargetNotice(NetworkConnectionToClient connection,byte code){Notice?.Invoke(code);}
 public static bool ServerNpcLine(GameObject npc,string id,string name,string text){if(!NetworkServer.active||!npc||!npc.activeInHierarchy||string.IsNullOrWhiteSpace(id))return false;var t=RiftSessionChatTuning.Load;double now=NetworkTime.time;if(npcNext.TryGetValue(npc,out var ready)&&now<ready)return false;var plain=RiftSessionChatTuning.Plain(text,t.Characters);if(plain.Length==0)return false;if(now>=npcPrune){npcPrune=now+30;foreach(var key in npcNext.Keys.Where(k=>!k||npcNext[k]+60<now).ToArray())npcNext.Remove(key);}if(npcNext.Count>=256&&!npcNext.ContainsKey(npc))return false;npcNext[npc]=now+t.NpcDelay;foreach(var target in NetworkServer.spawned.Values.Select(n=>n?n.GetComponent<ChatBehaviour>():null).Where(c=>c&&c.Alive&&c.player.MyCharacter.gameObject.scene==npc.scene&&Vector3.SqrMagnitude(c.player.MyCharacter.transform.position-npc.transform.position)<=t.Radius*t.Radius))target.Deliver(0,RiftSessionChatTuning.Plain(id,64),RiftSessionChatTuning.Plain(name,32),plain,npc.transform.position,2);return true;}
 [Command(channel=0)]public void CmdInviteParty(uint other){if(NetworkTime.time<partyReady)return;partyReady=NetworkTime.time+.5;RiftSessionParty.Invite(this,other);}
 [Command(channel=0)]public void CmdAcceptParty(uint from,uint token){RiftSessionParty.Accept(this,from,token);}
 [Command(channel=0)]public void CmdDeclineParty(uint token){RiftSessionParty.Decline(this,token);}
 [Command(channel=0)]public void CmdLeaveParty(){RiftSessionParty.Remove(netId);}
 [Server]public void PushParty(uint leader,uint[] members,string[] names){if(Ready)TargetParty(connectionToClient,leader,members,names);}
 [TargetRpc(channel=0)]void TargetParty(NetworkConnectionToClient connection,uint leader,uint[] members,string[] names){ClientLeader=leader;ClientMembers=members??Array.Empty<uint>();ClientNames=names??Array.Empty<string>();PartyChanged?.Invoke();}
 [Server]public void PushInvite(uint from,uint token,string name,double until){if(Ready)TargetInvite(connectionToClient,from,token,name,until);}
 [TargetRpc(channel=0)]void TargetInvite(NetworkConnectionToClient connection,uint from,uint token,string name,double until){InviteFrom=from;InviteToken=token;InviteName=name;InviteExpires=until;PartyChanged?.Invoke();}
}
}
