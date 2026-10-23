using Mirror;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public sealed partial class AbilityController {
 public static event System.Action<string> ActionNotice;double nextNotice;
 public void LocalNotice(string message){ActionNotice?.Invoke(message);}
 [Server]bool RejectAction(string reason){if(connectionToClient!=null&&NetworkTime.time>=nextNotice){nextNotice=NetworkTime.time+.7;TargetActionNotice(connectionToClient,reason);}return false;}
 [TargetRpc]void TargetActionNotice(NetworkConnectionToClient connection,string reason){ActionNotice?.Invoke(reason);}

 [Server]public void ReportIncomingThreat(Health attacker){if(!Actor||!attacker||attacker==Actor.Health||attacker.Team==Actor.Health.Team||attacker.CurrentHealth<=0||attacker.netId==0||connectionToClient==null)return;TargetIncomingThreat(connectionToClient,attacker.netId);}
 [TargetRpc]void TargetIncomingThreat(NetworkConnectionToClient connection,uint attackerId){if(!NetworkClient.spawned.TryGetValue(attackerId,out var identity))return;var attacker=identity.GetComponent<Health>();RiftMMOController.Instance?.TryTargetAttacker(attacker);}
 public int AbilityRequiredLevel(int id){var a=Definition(id);return a?RiftAbilityUnlocks.Required(a.Id):1;}
 public string AbilityUnavailableReason(int id){var a=Definition(id);if(!a||!a.Implemented)return "Todavía no disponible";if(Level<AbilityRequiredLevel(id))return "Se desbloquea en nivel "+AbilityRequiredLevel(id);return "No disponible en esta especialización";}
}
}
