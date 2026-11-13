using System;
using UnityEngine;
using ROM.UI.Sprint01;
namespace ROM.Progression {
// This redeems authoritative remote quest receipts into the existing trusted local wallet.
// A saved absolute assignment is replayed after interruption; never repeat an increment.
public static class RiftQuestRewardBridge {
 const string Pending="ROM.QuestReward.Pending.v1";
 [Serializable]public sealed class Payment {public string Character,Receipt;public int Gold;}
 public static string Receipt(string character,string quest)=>"ROM.QuestReward.Receipt.v1."+character+"."+quest;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
 public static void Recover(){
  if(!PlayerPrefs.HasKey(Pending))return;
  var payment=JsonUtility.FromJson<Payment>(PlayerPrefs.GetString(Pending));
  if(payment==null||string.IsNullOrEmpty(payment.Character)||string.IsNullOrEmpty(payment.Receipt)||payment.Gold<0)throw new InvalidOperationException("Invalid pending quest reward");
  PlayerPrefs.SetInt("ROM.CharacterWallet.v1."+payment.Character+".Gold",payment.Gold);
  PlayerPrefs.SetInt(payment.Receipt,1);PlayerPrefs.Save();PlayerPrefs.DeleteKey(Pending);PlayerPrefs.Save();
 }
 public static void Apply(JournalState state){
  Recover();if(state==null||string.IsNullOrEmpty(state.CharacterId)||state.CharacterId!=RiftCharacterWallet.Id||state.Quests==null)return;
  foreach(var quest in state.Quests){
   // Version 1 and legacy records were already paid by the host; no retroactive payout.
   if(quest.Status!=QuestStatus.TurnedIn||quest.RewardLedgerVersion!=2||quest.GoldRewardCommitted<0)continue;
   string receipt=Receipt(state.CharacterId,quest.Id);if(PlayerPrefs.GetInt(receipt,0)==1)continue;
   int gold=PlayerPrefs.GetInt(RiftCharacterWallet.GoldKey,0);
   if((long)gold+quest.GoldRewardCommitted>int.MaxValue)continue;
   var payment=new Payment{Character=state.CharacterId,Receipt=receipt,Gold=gold+quest.GoldRewardCommitted};
   PlayerPrefs.SetString(Pending,JsonUtility.ToJson(payment));PlayerPrefs.Save();Recover();
  }
 }
}
}
