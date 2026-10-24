using System;using System.Linq;using Mirror;using UnityEngine;using MultiFPS.Gameplay;
namespace ROM.Progression {
public sealed partial class JournalState {public string LastDurabilityDeathLife="";}
public sealed partial class RiftJournalPlayer {
 bool PersistDurabilityAtomic(){try{string root=System.IO.Path.GetDirectoryName(path);RiftTradePersistence.Recover(root);RiftVendorPersistence.Recover(root);string temp=path+".death-tmp";System.IO.File.WriteAllText(temp,JsonUtility.ToJson(State));if(System.IO.File.Exists(path))System.IO.File.Replace(temp,path,null);else System.IO.File.Move(temp,path);return true;}catch(Exception error){Debug.LogWarning("Death durability save: "+error.Message);return false;}}
 void DurabilityDeath(Health health,string life){ServerApplyDeathDurability(health,life);}
 [Server]public bool ServerApplyDeathDurability(Health health,string life){if(!health||!Player||!Player.MyCharacter||Player.MyCharacter.Health!=health||health.CurrentHealth>0||string.IsNullOrEmpty(life)||health.ServerLifeToken!=life||!Ensure()||State.LastDurabilityDeathLife==life||!Combat||!Combat.Prototype)return false;string previous=JsonUtility.ToJson(State);State.EnsureDurability();State.LastDurabilityDeathLife=life;wearActions.Clear();ServerCancelTrade();var tuning=RiftRepairTuning.Load;int lost=0;
 if(tuning&&!Combat.SwordAndShield){var item=State.LootAxes.FirstOrDefault(x=>x.Id==State.EquippedLootAxe&&x.Level<=Combat.Level);if(item!=null){int before=item.Durability;item.Durability=Mathf.Max(0,before-tuning.DeathLoss(item.MaximumDurability));lost=before-item.Durability;}else{int before=State.StarterAxeDurability;State.StarterAxeDurability=Mathf.Max(0,before-tuning.DeathLoss(State.StarterAxeMaximumDurability));lost=before-State.StarterAxeDurability;}}
 if(!PersistDurabilityAtomic()){State=JsonUtility.FromJson<JournalState>(previous);Report("No se pudo guardar el desgaste de muerte.");return false;}
 // Update only weapon bonus at zero, never vitals or animation.
 ApplyLootEquipment();
 if(connectionToClient!=null)TargetSnapshot(connectionToClient,JsonUtility.ToJson(State),lost>0?"Muerte · -"+lost+" durabilidad del hacha equipada":"");return true;}
}
}
