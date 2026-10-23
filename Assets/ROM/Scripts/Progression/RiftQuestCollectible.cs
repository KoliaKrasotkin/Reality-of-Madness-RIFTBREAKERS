using System;
using System.Collections.Generic;
using MultiFPS.Gameplay;
using MultiFPS.UI;
using UnityEngine;
namespace ROM.Progression {
// A scene source is never globally consumed: receipts belong to each character's quest.
public sealed class RiftQuestCollectible:MonoBehaviour {
 public static readonly List<RiftQuestCollectible> Active=new List<RiftQuestCollectible>();
 public string StableId,ItemKey,DisplayName="Suministro de misión";[Range(1,5)]public float Range=3.5f;
 RiftWorldTarget world;
 void Reset(){StableId=Guid.NewGuid().ToString("N");}
 void OnEnable(){Active.Add(this);world=GetComponent<RiftWorldTarget>();if(!world)world=gameObject.AddComponent<RiftWorldTarget>();world.Id=StableId;world.DisplayName=DisplayName;world.Role="Objeto de misión";world.InteractionRange=Range;world.InteractRequested+=Interact;}
 void OnDisable(){Active.Remove(this);if(world)world.InteractRequested-=Interact;}
 void Interact(){var player=ClientFrontend.ClientPlayerInstance;if(player)player.GetComponent<RiftJournalPlayer>()?.CmdCollect(StableId);}
 public bool Eligible(JournalState state,RiftJournalCatalog catalog)=>!string.IsNullOrEmpty(StableId)&&!string.IsNullOrEmpty(ItemKey)&&state.CanCollect(catalog,StableId,ItemKey);
}
}
