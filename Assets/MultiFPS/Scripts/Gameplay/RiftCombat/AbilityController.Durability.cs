using System;using Mirror;using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public sealed partial class AbilityController {
 public static event Action<AbilityController,string> OffensiveActionStarted,OffensiveActionConnected;
 long wearActionSequence;string castWearToken,autoWearToken;bool castWearConnected;
 string BeginWearAction(){if(!NetworkServer.active||!Prototype||!Actor)return null;string token=Actor.Health.ServerLifeToken+":"+(++wearActionSequence);try{OffensiveActionStarted?.Invoke(this,token);}catch(Exception error){Debug.LogException(error);}return token;}
 void ConnectWearAction(string token){if(!NetworkServer.active||string.IsNullOrEmpty(token))return;try{OffensiveActionConnected?.Invoke(this,token);}catch(Exception error){Debug.LogException(error);}}
}
}
