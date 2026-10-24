using System.Collections.Generic;
using System.Linq;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.Gameplay.RiftEnemies;
using MultiFPS.UI;
using ROM.Horde;
using ROM.Progression;
using ROM.UI.Sprint01;
using UnityEngine;
using UnityEngine.UI;
namespace ROM.UI.Gameplay {
// All presentation belongs to the local journal. Source renderers/materials are never changed.
public sealed class RiftQuestGuidance:MonoBehaviour {
 public Transform Destination {get;private set;}public string DestinationLabel {get;private set;}
 public bool Offscreen {get;private set;}public int HighlightCount=>glows.Count;
 RiftJournalHUD journal;RiftGameplayHUD hud;Text direction,caption;RectTransform marker;RiftHUDGraphic arrow;Material material;float next;
 RiftEnemySpawnZone[] zones;RiftCheckpoint[] checkpoints;readonly Dictionary<Renderer,Renderer> glows=new Dictionary<Renderer,Renderer>();readonly HashSet<Renderer> wanted=new HashSet<Renderer>();
 static readonly Color Gold=new Color(.95f,.76f,.32f);
 public static QuestRecord Tracked(JournalState state){var id=RiftJournalView.Tracked;if(id=="-")return null;var q=state.Quest(id);return q!=null&&q.Status!=QuestStatus.TurnedIn?q:state.Quests.FirstOrDefault(r=>r.Status!=QuestStatus.TurnedIn);}
 void Awake(){
  journal=GetComponent<RiftJournalHUD>();hud=GetComponent<RiftGameplayHUD>();if(!hud)hud=GetComponentInParent<RiftGameplayHUD>();if(!journal||!hud)return;
  direction=RiftWidgets.Label(journal.Tracker,"",0,153,285,68,14,Gold);direction.name="Quest direction";direction.font=journal.Objectives.font;journal.Tracker.sizeDelta=new Vector2(journal.Tracker.sizeDelta.x,230);
  marker=RiftWidgets.Rect(hud.Design,"Quest destination marker",0,0,420,80);marker.anchorMin=marker.anchorMax=marker.pivot=new Vector2(.5f,.5f);
  arrow=RiftWidgets.Rect(marker,"Quest direction arrow",190,0,32,32).gameObject.AddComponent<RiftHUDGraphic>();arrow.Shape=HUDShape.Diamond;arrow.color=Gold;arrow.Border=Gold;arrow.raycastTarget=false;
  caption=RiftWidgets.Label(marker,"",0,40,420,40,18,Gold);caption.alignment=TextAnchor.MiddleCenter;caption.font=journal.Objectives.font;var shadow=caption.gameObject.AddComponent<Shadow>();shadow.effectColor=Color.black;shadow.effectDistance=new Vector2(1,-1);marker.gameObject.SetActive(false);
  var shader=Resources.Load<Shader>("RiftQuestHighlight");if(shader)material=new Material(shader){name="Local quest rim",hideFlags=HideFlags.HideAndDontSave};
  zones=FindObjectsByType<RiftEnemySpawnZone>(FindObjectsSortMode.None);checkpoints=FindObjectsByType<RiftCheckpoint>(FindObjectsSortMode.None);
 }
 void LateUpdate(){
  var player=ClientFrontend.ClientPlayerInstance;var actor=player?player.MyCharacter:null;
  if(!hud||!actor||actor.Health.CurrentHealth<=0||ClientFrontend.Pause||ClientFrontend.Hub){if(marker)marker.gameObject.SetActive(false);ClearGlows();return;}
  var state=RiftJournalView.Read();var catalog=RiftJournalCatalog.Load;if(!catalog)return;
  if(Time.unscaledTime>=next){next=Time.unscaledTime+.25f;Resolve(state,catalog,actor);}
  foreach(var pair in glows)if(pair.Value)pair.Value.enabled=pair.Key&&pair.Key.enabled&&pair.Key.gameObject.activeInHierarchy;
  DrawMarker(actor);
  if(RiftControls.Pressed("Interact")&&!RiftInterfaceCursor.Typing&&UICursorManager.GamePlayInput()&&RiftMMOController.Instance&&RiftMMOController.Instance.SelectedWorld){var item=RiftMMOController.Instance.SelectedWorld.GetComponent<RiftQuestCollectible>();if(item)item.GetComponent<RiftWorldTarget>().Interact();}
 }
 bool Incomplete(QuestRecord q,RiftQuestDefinition def,int i)=>q.Status==QuestStatus.Active&&q.Progress!=null&&i<q.Progress.Length&&q.Progress[i]<def.Objectives[i].Required;
 public static bool EnemyMatches(Health health,JournalObjective objective,CharacterInstance actor){
  if(!health||!actor||health==actor.Health||health.TrainingTarget||health.CurrentHealth<=0||health.Team==actor.Health.Team||!health.gameObject.activeInHierarchy)return false;
  var native=health.GetComponent<RiftEnemyAgent>();if(native){if(!native.Definition||native.Dead)return false;return objective.Matches(objective.Event,native.Definition.Id,native.QuestEncounter)&&(objective.Event==RiftProgressEvent.Kill&&!native.Definition.IsWildlife||objective.Event==RiftProgressEvent.MiniBoss&&native.Definition.IsBoss);}
  var legacy=health.GetComponent<RiftEnemyCombat>();return legacy&&legacy.ActiveCombat&&(objective.Event==RiftProgressEvent.Kill&&objective.Matches(RiftProgressEvent.Kill,"enemy")||objective.Event==RiftProgressEvent.MiniBoss&&legacy.ArchetypeIndex==2&&objective.Matches(RiftProgressEvent.MiniBoss,"boss"));
 }
 void Resolve(JournalState state,RiftJournalCatalog catalog,CharacterInstance actor){
  wanted.Clear();
  foreach(var q in state.Quests){var def=catalog.Quest(q.Id);if(!def)continue;for(int i=0;i<def.Objectives.Length;i++){if(!Incomplete(q,def,i))continue;var o=def.Objectives[i];if(o.Event==RiftProgressEvent.Collect){foreach(var item in RiftQuestCollectible.Active)if(item&&item.Eligible(state,catalog)&&o.Matches(RiftProgressEvent.Collect,item.ItemKey)&&!q.Seen.Contains("collect:"+i+":"+item.StableId))Want(item.transform);}
   else if(o.Event==RiftProgressEvent.Kill||o.Event==RiftProgressEvent.MiniBoss)foreach(var h in Health.All)if(EnemyMatches(h,o,actor))Want(h.transform);
  }}
  foreach(var source in glows.Keys.ToArray())if(!source||!wanted.Contains(source)){if(glows[source])Destroy(glows[source].gameObject);glows.Remove(source);}
  if(material)foreach(var source in wanted)if(source&&!glows.ContainsKey(source)&&glows.Count<64){var overlay=Overlay(source);if(overlay)glows.Add(source,overlay);}
  Destination=null;DestinationLabel="";var tracked=Tracked(state);var definition=tracked!=null?catalog.Quest(tracked.Id):null;if(!definition){if(direction)direction.text="";return;}
  if(tracked.Status==QuestStatus.Ready){ChooseNpc(definition.TurnIn,actor);DestinationLabel=Destination?"Entregar: "+RiftHub.NpcName(definition.TurnIn):"";}
  else for(int i=0;i<definition.Objectives.Length;i++){if(!Incomplete(tracked,definition,i))continue;FindObjective(definition.Objectives[i],tracked,i,state,catalog,actor);if(Destination)break;}
  if(direction)direction.text=Destination?DestinationLabel+"\n"+Vector3.Distance(actor.transform.position,Destination.position).ToString("0")+" m · seguí la marca dorada":"Sin ubicación disponible para este objetivo";
 }
 void Choose(Transform candidate,string label,CharacterInstance actor){if(candidate&&candidate.gameObject.activeInHierarchy&&(!Destination||(candidate.position-actor.transform.position).sqrMagnitude<(Destination.position-actor.transform.position).sqrMagnitude)){Destination=candidate;DestinationLabel=label;}}
 void ChooseNpc(string id,CharacterInstance actor){foreach(var npc in RiftVillageNpc.Active)if(npc&&npc.Id==id)Choose(npc.transform,npc.DisplayName,actor);}
 void FindObjective(JournalObjective o,QuestRecord q,int index,JournalState state,RiftJournalCatalog catalog,CharacterInstance actor){
  if(o.Event==RiftProgressEvent.Talk){foreach(var npc in RiftVillageNpc.Active)if(npc&&o.Matches(o.Event,npc.Id)&&(!o.Unique||!q.Seen.Contains(index+":"+npc.Id)))Choose(npc.transform,npc.DisplayName,actor);}
  else if(o.Event==RiftProgressEvent.Collect){foreach(var item in RiftQuestCollectible.Active)if(item&&item.Eligible(state,catalog)&&o.Matches(o.Event,item.ItemKey)&&!q.Seen.Contains("collect:"+index+":"+item.StableId))Choose(item.transform,item.DisplayName,actor);}
  else if(o.Event==RiftProgressEvent.TrainingHit&&o.Matches(o.Event,"dummy")){foreach(var h in Health.All)if(h&&h.TrainingTarget)Choose(h.transform,"Entrenamiento",actor);}
  else if(o.Event==RiftProgressEvent.PotionBought){foreach(var npc in RiftVillageNpc.Active)if(npc&&npc.Trades)Choose(npc.transform,npc.DisplayName,actor);}
  else if(o.Event==RiftProgressEvent.Checkpoint){foreach(var crystal in checkpoints)if(crystal&&!crystal.StartingCamp&&o.Matches(o.Event,crystal.Title)&&(!o.Unique||!q.Seen.Contains(index+":"+crystal.Title)))Choose(crystal.transform,crystal.Title,actor);}
  else if(o.Event==RiftProgressEvent.Encounter){var world=ROM.Horde.Horde.Instance;if(world&&world.Encounters!=null)foreach(var encounter in world.Encounters)if(encounter&&!encounter.Cleared&&o.Matches(o.Event,encounter.Title))Choose(encounter.transform,encounter.Title,actor);}
  else if(o.Event==RiftProgressEvent.Kill||o.Event==RiftProgressEvent.MiniBoss){foreach(var h in Health.All)if(EnemyMatches(h,o,actor))Choose(h.transform,h.CharacterName,actor);
   if(!Destination)foreach(var zone in zones)if(zone&&!zone.Cleared&&zone.Enemies!=null&&zone.Enemies.Any(e=>e.Enemy&&o.Matches(o.Event,e.Enemy.Id,zone.Encounter?zone.Encounter.Title:zone.Title)&&(o.Event==RiftProgressEvent.Kill&&!e.Enemy.IsWildlife||o.Event==RiftProgressEvent.MiniBoss&&e.Enemy.IsBoss)))Choose(zone.transform,"Zona: "+zone.Title,actor);
   if(!Destination&&o.Event==RiftProgressEvent.MiniBoss&&o.Matches(o.Event,"boss")){var world=ROM.Horde.Horde.Instance;if(world&&world.Encounters!=null)foreach(var encounter in world.Encounters)if(encounter&&encounter.MiniBoss&&!encounter.Cleared)Choose(encounter.transform,encounter.Title,actor);}
  }
 }
 void Want(Transform target){foreach(var r in target.GetComponentsInChildren<Renderer>())if(r&&r.enabled&&r.name!="Quest rim (local)"&&(r is MeshRenderer||r is SkinnedMeshRenderer))wanted.Add(r);}
 Renderer Overlay(Renderer source){
  var skin=source as SkinnedMeshRenderer;var filter=source.GetComponent<MeshFilter>();if(!skin&&(!filter||!filter.sharedMesh))return null;
  var go=new GameObject("Quest rim (local)");go.transform.SetParent(source.transform,false);go.layer=source.gameObject.layer;Renderer result;
  if(skin){var copy=go.AddComponent<SkinnedMeshRenderer>();copy.sharedMesh=skin.sharedMesh;copy.bones=skin.bones;copy.rootBone=skin.rootBone;copy.localBounds=skin.localBounds;result=copy;}else{go.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;result=go.AddComponent<MeshRenderer>();}
  int count=skin?skin.sharedMesh.subMeshCount:filter.sharedMesh.subMeshCount;result.sharedMaterials=Enumerable.Repeat(material,Mathf.Max(1,count)).ToArray();result.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;result.receiveShadows=false;return result;
 }
 void DrawMarker(CharacterInstance actor){
  if(!marker)return;var camera=GameplayCamera._instance?GameplayCamera._instance.GetComponent<Camera>():Camera.main;bool show=Destination&&camera&&journal.Tracker.gameObject.activeSelf;marker.gameObject.SetActive(show);if(!show)return;
  var point=camera.WorldToScreenPoint(Destination.position+Vector3.up*1.8f);Offscreen=point.z<=0||point.x<100||point.x>Screen.width-100||point.y<100||point.y>Screen.height-100;
  Vector2 screen=point;if(Offscreen){var delta=screen-new Vector2(Screen.width*.5f,Screen.height*.5f);if(point.z<0)delta=-delta;if(delta.sqrMagnitude<1)delta=Vector2.down;screen=new Vector2(Screen.width*.5f,Screen.height*.78f);arrow.Shape=HUDShape.Arrow;arrow.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-90);}else{arrow.Shape=HUDShape.Diamond;arrow.rectTransform.localRotation=Quaternion.identity;}
  arrow.SetVerticesDirty();RectTransformUtility.ScreenPointToLocalPointInRectangle(hud.Design,screen,null,out var local);marker.anchoredPosition=local;caption.text=DestinationLabel+" · "+Vector3.Distance(actor.transform.position,Destination.position).ToString("0")+" m";
 }
 void ClearGlows(){foreach(var pair in glows)if(pair.Value)Destroy(pair.Value.gameObject);glows.Clear();}
 void OnDisable(){ClearGlows();if(marker)marker.gameObject.SetActive(false);}
 void OnDestroy(){ClearGlows();if(marker)Destroy(marker.gameObject);if(direction)Destroy(direction.gameObject);if(material)Destroy(material);}
}
}
