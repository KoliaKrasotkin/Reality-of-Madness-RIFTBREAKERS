using System;
using System.Collections.Generic;
using MultiFPS.Gameplay;
using MultiFPS.UI;
using Mirror;
using UnityEngine;
namespace ROM.Progression {
// Sources stay alive on the server; visibility and pickup receipts belong to each character.
public sealed class RiftQuestCollectible:MonoBehaviour,IRiftLocalPickup {
 public static readonly List<RiftQuestCollectible> Active=new List<RiftQuestCollectible>();
 public string StableId,ItemKey,DisplayName="Suministro de misión";[Range(1,5)]public float Range=3.5f;
 [Range(.25f,2)]public float AutoPickupRadius=1.2f;
 Renderer[] visuals;bool[] originalHidden;float nextAuto;
 public bool LocalVisible {get;private set;}
 void Reset(){StableId=Guid.NewGuid().ToString("N");}
 void OnEnable(){if(!Active.Contains(this))Active.Add(this);visuals=GetComponentsInChildren<Renderer>(true);originalHidden=new bool[visuals.Length];for(int i=0;i<visuals.Length;i++)originalHidden[i]=visuals[i].forceRenderingOff;RefreshLocalVisibility();}
 void OnDisable(){Active.Remove(this);if(visuals!=null)for(int i=0;i<visuals.Length;i++)if(visuals[i])visuals[i].forceRenderingOff=originalHidden[i];}
 public void RefreshLocalVisibility(){var player=ClientFrontend.ClientPlayerInstance;var actor=player?player.MyCharacter:null;var catalog=RiftJournalCatalog.Load;LocalVisible=NetworkClient.active&&actor&&actor.Health&&actor.Health.CurrentHealth>0&&catalog&&Eligible(RiftJournalView.Read(),catalog);if(visuals!=null)for(int i=0;i<visuals.Length;i++)if(visuals[i])visuals[i].forceRenderingOff=originalHidden[i]||!LocalVisible;}
 void LateUpdate(){if(!Application.isPlaying)return;RefreshLocalVisibility();if(!LocalVisible||Time.unscaledTime<nextAuto)return;var actor=ClientFrontend.ClientPlayerInstance.MyCharacter;if(Vector3.Distance(actor.transform.position,transform.position)<=Mathf.Min(Range,AutoPickupRadius)){nextAuto=Time.unscaledTime+.5f;RequestPickup(true);}}
 public void RequestPickup(bool automatic=false){if(!LocalVisible)return;var player=ClientFrontend.ClientPlayerInstance;if(player)player.GetComponent<RiftJournalPlayer>()?.CmdCollect(StableId,automatic);}
 public bool Eligible(JournalState state,RiftJournalCatalog catalog)=>state!=null&&catalog&&!string.IsNullOrEmpty(StableId)&&!string.IsNullOrEmpty(ItemKey)&&state.CanCollect(catalog,StableId,ItemKey);
}
}
