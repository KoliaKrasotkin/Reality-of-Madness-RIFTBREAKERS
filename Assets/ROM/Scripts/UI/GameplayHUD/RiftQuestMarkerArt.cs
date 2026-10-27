using System.Linq;
using UnityEngine;
using ROM.Progression;
namespace ROM.UI.Gameplay {
 public enum RiftQuestMarkerState { None, Available, InProgress, TurnIn, Tracked }
 // Presentation only. Quest ownership, acceptance and payout remain in the journal.
 public static class RiftQuestMarkerArt {
  public const string Root="RiftQuestMarkers/";
  public static RiftQuestMarkerState ForNpc(string id,JournalState state,RiftJournalCatalog catalog){
   if(state==null||!catalog)return RiftQuestMarkerState.None;
   if(catalog.Quests.Any(q=>q&&q.TurnIn==id&&state.Quest(q.Id)?.Status==QuestStatus.Ready))return RiftQuestMarkerState.TurnIn;
   if(catalog.Quests.Any(q=>q&&q.Giver==id&&state.Available(q)))return RiftQuestMarkerState.Available;
   if(catalog.Quests.Any(q=>q&&(q.Giver==id||q.TurnIn==id)&&state.Quest(q.Id)?.Status==QuestStatus.Active))return RiftQuestMarkerState.InProgress;
   return RiftQuestMarkerState.None;
  }
  public static Sprite Icon(RiftQuestMarkerState state)=>state==RiftQuestMarkerState.None?null:Resources.Load<Sprite>(Root+state);
  public static GameObject Model(RiftQuestMarkerState state)=>state==RiftQuestMarkerState.None?null:Resources.Load<GameObject>(Root+"World/"+state);
  public static RiftQuestMarkerState State(RiftQuestPinKind kind)=>kind==RiftQuestPinKind.TurnIn?RiftQuestMarkerState.TurnIn:kind==RiftQuestPinKind.Objective?RiftQuestMarkerState.Tracked:kind==RiftQuestPinKind.InProgress?RiftQuestMarkerState.InProgress:RiftQuestMarkerState.Available;
 }
 public sealed class RiftQuestWorldMarker:MonoBehaviour {
  public RiftQuestMarkerState State{get;private set;}GameObject model;
  public void SetState(RiftQuestMarkerState value){if(State==value)return;State=value;if(model){model.SetActive(false);Destroy(model);}model=null;var prefab=RiftQuestMarkerArt.Model(value);if(prefab){model=Instantiate(prefab,transform,false);model.name="Quest art - "+value;}}
  void LateUpdate(){var camera=MultiFPS.GameplayCamera._instance?MultiFPS.GameplayCamera._instance.transform:Camera.main?Camera.main.transform:null;if(camera)transform.rotation=camera.rotation;}
 }
}
