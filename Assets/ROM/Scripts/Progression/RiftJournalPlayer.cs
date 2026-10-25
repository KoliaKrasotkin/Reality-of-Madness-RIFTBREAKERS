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
    public sealed partial class RiftJournalPlayer:NetworkBehaviour
    {
        public RiftJournalCatalog Catalog;public JournalState State {get;private set;}
        public static string StorageOverride;
        PlayerInstance Player=>GetComponent<PlayerInstance>();string path;double nextCommand;
        public override void OnStartServer(){RiftWorldEvents.TrainingHit+=TrainingHit;Health.ServerDied+=DurabilityDeath;MultiFPS.Gameplay.RiftCombat.AbilityController.OffensiveActionStarted+=CaptureWearAction;MultiFPS.Gameplay.RiftCombat.AbilityController.OffensiveActionConnected+=ConnectedWearAction;}
        public override void OnStopServer(){MultiFPS.Gameplay.RiftCombat.AbilityController.OffensiveActionStarted-=CaptureWearAction;MultiFPS.Gameplay.RiftCombat.AbilityController.OffensiveActionConnected-=ConnectedWearAction;wearActions.Clear();Health.ServerDied-=DurabilityDeath;RiftWorldEvents.TrainingHit-=TrainingHit;ServerCancelTrade();}
        void Update(){if(isServer){TradeTick();if(State==null&&Ensure())Push("");}}
        bool Ensure(){if(State!=null){
            if(State.EnsureTalentRanks(MultiFPS.Gameplay.RiftCombat.WarriorProgressionDefinition.Load)){if(!Persist())return false;ApplyProgression();}
            // Resolve durable wallet operations before another action reads its balance or journal.
            string pendingRoot=Path.GetDirectoryName(path);
            if(Directory.Exists(pendingRoot)&&Directory.GetFiles(pendingRoot,"vendor-*.pending").Length>0){
                try{RiftVendorPersistence.Recover(pendingRoot);State=JsonUtility.FromJson<JournalState>(File.ReadAllText(path));ApplyProgression();}
                catch(Exception e){Debug.LogWarning("Pending reward recovery: "+e.Message);return false;}
            }
            return State!=null;
        }if(!isServer||!Player||Player.BOT||string.IsNullOrEmpty(Player.PlayerInfo.Username))return false;if(!Catalog)Catalog=RiftJournalCatalog.Load;if(!Catalog)return false;string id=string.IsNullOrEmpty(Player.PlayerInfo.CharacterId)?Player.PlayerInfo.Username:Player.PlayerInfo.CharacterId;var root=string.IsNullOrEmpty(StorageOverride)?Path.Combine(Application.persistentDataPath,"RiftJournals"):StorageOverride;Directory.CreateDirectory(root);RiftTradePersistence.Recover(root);RiftVendorPersistence.Recover(root);string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(id))).Replace("-","");path=Path.Combine(root,hash+".json");try{State=File.Exists(path)?JsonUtility.FromJson<JournalState>(File.ReadAllText(path)):null;}catch(Exception e){Debug.LogWarning("Journal load: "+e.Message);}if(State==null)State=new JournalState{CharacterId=id};State.CharacterId=id;bool repaired=State.EnsureDurability();repaired|=State.EnsureTalentRanks(MultiFPS.Gameplay.RiftCombat.WarriorProgressionDefinition.Load);if(repaired)Persist();ApplyProgression();return true;}
        bool Persist(){var measured=RiftFrameDiagnostics.Begin();try{State.EnsureDurability();State.EnsureTalentRanks(MultiFPS.Gameplay.RiftCombat.WarriorProgressionDefinition.Load);RiftTradePersistence.Recover(Path.GetDirectoryName(path));RiftVendorPersistence.Recover(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(State));FlushExperienceFeedback();return true;}catch(Exception e){pendingExperienceFeedback=0;Debug.LogWarning("Journal save: "+e.Message);return false;}finally{RiftFrameDiagnostics.End(2,measured);}}
        [TargetRpc]void TargetSnapshot(NetworkConnectionToClient connection,string json,string notice){RiftJournalView.ApplyServerSnapshot(json,notice);}
        [TargetRpc]void TargetNotice(NetworkConnectionToClient connection,string notice){RiftJournalView.Notify(notice);ROM.UI.Sprint01.RiftVillageUI.Notify(notice);}
        void Push(string notice){ApplyProgression();if(connectionToClient!=null)TargetSnapshot(connectionToClient,JsonUtility.ToJson(State),notice);}
        public void Report(string notice){if(connectionToClient!=null)TargetNotice(connectionToClient,notice);}
        bool Near(string npcId,out RiftVillageNpc npc){npc=UnityEngine.Object.FindObjectsByType<RiftVillageNpc>(FindObjectsSortMode.None).FirstOrDefault(n=>n.Id==npcId);return npc&&Player.MyCharacter&&Player.MyCharacter.Health.CurrentHealth>0&&Vector3.Distance(npc.transform.position,Player.MyCharacter.transform.position)<=npc.InteractionRadius+.15f;}
        [Command]public void CmdTalk(string npcId){ServerTalk(npcId);}
        [Server]public bool ServerTalk(string npcId){if(!Ensure()||!Near(npcId,out var speakingNpc))return false;ServerRecord(RiftProgressEvent.Talk,npcId);speakingNpc.ServerSpeak("greeting");return true;}
        [Command]public void CmdAccept(string questId,string npcId){if(!ServerAccept(questId,npcId))Report("No podés aceptar esta misión aquí.");}
        [Server]public bool ServerAccept(string questId,string npcId){if(!Ensure()||!Near(npcId,out _)||NetworkTime.time<nextCommand)return false;var definition=Catalog.Quest(questId);if(!definition||definition.Giver!=npcId||State.Quests.Count(q=>q.Status!=QuestStatus.TurnedIn)>=8)return false;string previous=JsonUtility.ToJson(State);if(!State.Accept(definition))return false;nextCommand=NetworkTime.time+.15;if(!Persist()){State=JsonUtility.FromJson<JournalState>(previous);return false;}Push("Misión aceptada: "+definition.Title);return true;}
        [Command]public void CmdTurnIn(string questId,string npcId){if(!ServerTurnIn(questId,npcId))Report("Completá los objetivos y acercate al NPC de entrega.");}
        [Server]public bool ServerTurnIn(string questId,string npcId){
            if(!Ensure()||!Near(npcId,out _)||NetworkTime.time<nextCommand)return false;
            var definition=Catalog.Quest(questId);if(!definition||definition.TurnIn!=npcId||Trading)return false;
            // Common-gold wallet is Host-local. Never credit a remote participant's local preferences.
            if(connectionToClient!=NetworkServer.localConnection||Player.PlayerInfo.CharacterId!=ROM.UI.Sprint01.RiftCharacterWallet.Id)return false;
            int gold=ROM.UI.Sprint01.RiftExplorationProgress.Gold;if(definition.Gold<0||gold>1000000000-definition.Gold)return false;
            string previous=JsonUtility.ToJson(State);if(!State.TurnIn(definition))return false;
            nextCommand=NetworkTime.time+.15;
            var record=State.Quest(questId);record.RewardLedgerVersion=1;record.GoldRewardCommitted=definition.Gold;
            var notices=State.Record(Catalog,RiftProgressEvent.QuestTurnedIn,definition.Id);
            AwardExperience(definition.RewardExperience,notices);
            try{
                // Existing write-ahead operation commits the absolute wallet plus journal (including XP and receipt).
                // Recovery replays assignments, never increments: interruptions cannot pay twice.
                RiftVendorPersistence.Commit(path,JsonUtility.ToJson(State),State.CharacterId,gold+definition.Gold,
                    ROM.UI.Sprint01.RiftVillageSupplies.Count(0),ROM.UI.Sprint01.RiftVillageSupplies.Count(1));
            }catch(Exception e){State=JsonUtility.FromJson<JournalState>(previous);pendingExperienceFeedback=0;Debug.LogWarning("Quest reward not committed: "+e.Message);return false;}
            FlushExperienceFeedback();
            Push("Misión completada: "+definition.Title+(notices.Count>0?"\n"+string.Join("\n",notices):""));return true;
        }
        [Command]public void CmdAbandon(string questId){if(!Ensure())return;var record=State.Quest(questId);if(record==null||record.Status==QuestStatus.TurnedIn)return;State.Quests.Remove(record);Persist();Push("Misión abandonada.");}
        [Server]public void ServerRecord(RiftProgressEvent kind,string key,int amount=1,int experienceOverride=-1,string encounter=""){if(!Ensure())return;bool fresh=!State.UniqueKeys.Contains(((int)kind)+":"+(key??""));var notices=State.Record(Catalog,kind,key,amount,encounter);if(experienceOverride>=0)AwardExperience(experienceOverride,notices);else AwardProgressEvent(kind,fresh,notices);Persist();Push(string.Join("\n",notices));}
        void TrainingHit(Health attacker){if(isServer&&Player&&Player.MyCharacter&&Player.MyCharacter.Health==attacker)ServerRecord(RiftProgressEvent.TrainingHit,"dummy");}
        public static void Record(PlayerInstance player,RiftProgressEvent kind,string key,string encounter=""){if(player){var journal=player.GetComponent<RiftJournalPlayer>();if(journal)journal.ServerRecord(kind,key,1,-1,encounter);}}
    }
}
