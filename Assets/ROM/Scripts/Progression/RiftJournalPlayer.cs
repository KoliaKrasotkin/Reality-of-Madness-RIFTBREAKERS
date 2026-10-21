using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using ROM.Horde;
using UnityEngine;
namespace ROM.Progression
{
    public sealed class RiftJournalPlayer:NetworkBehaviour
    {
        public RiftJournalCatalog Catalog;public JournalState State {get;private set;}
        public static string StorageOverride;
        PlayerInstance Player=>GetComponent<PlayerInstance>();string path;double nextCommand;
        public override void OnStartServer(){RiftWorldEvents.TrainingHit+=TrainingHit;}
        public override void OnStopServer(){RiftWorldEvents.TrainingHit-=TrainingHit;}
        void Update(){if(isServer&&State==null&&Ensure())Push("");}
        bool Ensure(){if(State!=null)return true;if(!isServer||!Player||Player.BOT||string.IsNullOrEmpty(Player.PlayerInfo.Username))return false;if(!Catalog)Catalog=RiftJournalCatalog.Load;if(!Catalog)return false;string id=string.IsNullOrEmpty(Player.PlayerInfo.CharacterId)?Player.PlayerInfo.Username:Player.PlayerInfo.CharacterId;var root=string.IsNullOrEmpty(StorageOverride)?Path.Combine(Application.persistentDataPath,"RiftJournals"):StorageOverride;Directory.CreateDirectory(root);string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(id))).Replace("-","");path=Path.Combine(root,hash+".json");try{State=File.Exists(path)?JsonUtility.FromJson<JournalState>(File.ReadAllText(path)):null;}catch(Exception e){Debug.LogWarning("Journal load: "+e.Message);}if(State==null)State=new JournalState{CharacterId=id};State.CharacterId=id;return true;}
        bool Persist(){try{File.WriteAllText(path,JsonUtility.ToJson(State));return true;}catch(Exception e){Debug.LogWarning("Journal save: "+e.Message);return false;}}
        [TargetRpc]void TargetSnapshot(NetworkConnectionToClient connection,string json,string notice){RiftJournalView.ApplyServerSnapshot(json,notice);}
        [TargetRpc]void TargetNotice(NetworkConnectionToClient connection,string notice){RiftJournalView.Notify(notice);ROM.UI.Sprint01.RiftVillageUI.Notify(notice);}
        void Push(string notice){if(connectionToClient!=null)TargetSnapshot(connectionToClient,JsonUtility.ToJson(State),notice);}
        public void Report(string notice){if(connectionToClient!=null)TargetNotice(connectionToClient,notice);}
        bool Near(string npcId,out RiftVillageNpc npc){npc=UnityEngine.Object.FindObjectsByType<RiftVillageNpc>(FindObjectsSortMode.None).FirstOrDefault(n=>n.Id==npcId);return npc&&Player.MyCharacter&&Player.MyCharacter.Health.CurrentHealth>0&&Vector3.Distance(npc.transform.position,Player.MyCharacter.transform.position)<=npc.InteractionRadius+.15f;}
        [Command]public void CmdTalk(string npcId){ServerTalk(npcId);}
        [Server]public bool ServerTalk(string npcId){if(!Ensure()||!Near(npcId,out _))return false;ServerRecord(RiftProgressEvent.Talk,npcId);return true;}
        [Command]public void CmdAccept(string questId,string npcId){if(!ServerAccept(questId,npcId))Report("No podés aceptar esta misión aquí.");}
        [Server]public bool ServerAccept(string questId,string npcId){if(!Ensure()||!Near(npcId,out _)||NetworkTime.time<nextCommand)return false;var definition=Catalog.Quest(questId);if(!definition||definition.Giver!=npcId||State.Quests.Count(q=>q.Status!=QuestStatus.TurnedIn)>=8)return false;string previous=JsonUtility.ToJson(State);if(!State.Accept(definition))return false;nextCommand=NetworkTime.time+.15;if(!Persist()){State=JsonUtility.FromJson<JournalState>(previous);return false;}Push("Misión aceptada: "+definition.Title);return true;}
        [Command]public void CmdTurnIn(string questId,string npcId){if(!ServerTurnIn(questId,npcId))Report("Completá los objetivos y acercate al NPC de entrega.");}
        [Server]public bool ServerTurnIn(string questId,string npcId){if(!Ensure()||!Near(npcId,out _)||NetworkTime.time<nextCommand)return false;var definition=Catalog.Quest(questId);if(!definition||definition.TurnIn!=npcId)return false;string previous=JsonUtility.ToJson(State);if(!State.TurnIn(definition))return false;nextCommand=NetworkTime.time+.15;var notices=State.Record(Catalog,RiftProgressEvent.QuestTurnedIn,definition.Id);if(!Persist()){State=JsonUtility.FromJson<JournalState>(previous);return false;}Push("Misión completada: "+definition.Title+(notices.Count>0?"\n"+string.Join("\n",notices):""));return true;}
        [Command]public void CmdAbandon(string questId){if(!Ensure())return;var record=State.Quest(questId);if(record==null||record.Status==QuestStatus.TurnedIn)return;State.Quests.Remove(record);Persist();Push("Misión abandonada.");}
        [Server]public void ServerRecord(RiftProgressEvent kind,string key,int amount=1){if(!Ensure())return;var notices=State.Record(Catalog,kind,key,amount);Persist();Push(string.Join("\n",notices));}
        void TrainingHit(Health attacker){if(isServer&&Player&&Player.MyCharacter&&Player.MyCharacter.Health==attacker)ServerRecord(RiftProgressEvent.TrainingHit,"dummy");}
        public static void Record(PlayerInstance player,RiftProgressEvent kind,string key){if(player){var journal=player.GetComponent<RiftJournalPlayer>();if(journal)journal.ServerRecord(kind,key);}}
    }
}
