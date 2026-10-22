using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftEnemies;
using MultiFPS.UI;
using ROM.Horde;
using ROM.Progression;
using ROM.UI.Gameplay;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
public static partial class RiftBagReview {
 static RiftJournalCatalog originalCatalog,reviewCatalog;static RiftQuestDefinition collectionQuest,abandonQuest;static RiftQuestCollectible[] fruit;static GameObject blocker;static RiftEnemyAgent reviewEnemy;static RiftEnemyDefinition reviewEnemyDefinition;static int killXP;static Material[] originalFruitMaterials;static string oldTracked;static Vector3[] fruitPositions;
 static RiftQuestCollectible Fruit(Vector3 position,string suffix){var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Explicit placeholder fruit "+suffix;go.SetActive(false);go.transform.position=position;go.transform.localScale=Vector3.one*.65f;var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.65f,.08f,.12f);go.GetComponent<Renderer>().sharedMaterial=material;var item=go.AddComponent<RiftQuestCollectible>();item.StableId=id+suffix;item.ItemKey="review-fruit";item.DisplayName="Fruto de prueba";go.SetActive(true);return item;}
 static void QuestTick(PlayerInstance player,CharacterInstance actor,RiftGameplayHUD hud,RiftHub hub){
  var journal=player.GetComponent<RiftJournalPlayer>();if(!journal||journal.State==null)return;var guidance=hud.GetComponent<RiftQuestGuidance>();if(!guidance)return;var iria=RiftVillageNpc.Active.First(n=>n.Id=="iria");
  if(step==100){
   oldTracked=RiftJournalView.Tracked;originalCatalog=RiftJournalCatalog.Load;reviewCatalog=ScriptableObject.Instantiate(originalCatalog);
   collectionQuest=ScriptableObject.CreateInstance<RiftQuestDefinition>();collectionQuest.Id="review-collect";collectionQuest.Title="Provisiones del Rift (prueba)";collectionQuest.Description="Fixture aislado de recolección y combate.";collectionQuest.Giver=collectionQuest.TurnIn="iria";collectionQuest.Objectives=new[]{new JournalObjective{Event=RiftProgressEvent.Collect,Filter="review-fruit",Label="Recoger frutos de prueba",Required=3},new JournalObjective{Event=RiftProgressEvent.Kill,Filter="review-demon",Label="Derrotar objetivo de prueba",Required=1}};
   abandonQuest=ScriptableObject.Instantiate(collectionQuest);abandonQuest.Id="review-abandon";abandonQuest.Title="Abandono (prueba)";
   reviewCatalog.Quests=originalCatalog.Quests.Concat(new[]{collectionQuest,abandonQuest}).ToArray();typeof(RiftJournalCatalog).GetField("cached",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).SetValue(null,reviewCatalog);journal.Catalog=reviewCatalog;journal.State.Quests.Clear();journal.ServerGrantExperience(0);
   actor.ServerTeleport(iria.transform.position-Vector3.forward*2,0);fruit=new[]{Fruit(actor.transform.position+Vector3.forward+Vector3.right, "a"),Fruit(actor.transform.position+Vector3.right*2,"b"),Fruit(actor.transform.position-Vector3.forward+Vector3.right,"c")};originalFruitMaterials=fruit[0].GetComponent<Renderer>().sharedMaterials;
   Check(!journal.ServerCollect(fruit[0].StableId,out _),"Server rejects collect without an active quest");Check(!journal.ServerCollect("unknown-source",out _),"Server rejects unknown scene object id");Check(guidance.HighlightCount==0,"No quest means no local quest highlight");Check(journal.ServerAccept(collectionQuest.Id,"iria"),"Existing server accept starts controlled collect+kill quest");RiftJournalView.Tracked=collectionQuest.Id;Advance();return;
  }
  if(step==101){
   Check(guidance.HighlightCount>=3,"Active collect quest highlights actual source meshes");Check(fruit[0].GetComponent<Renderer>().sharedMaterials.SequenceEqual(originalFruitMaterials),"Highlight does not modify original shared materials");Check(guidance.Destination&&guidance.Destination.GetComponent<RiftQuestCollectible>(),"Tracked mission resolves a real collect location");Check(Resources.Load<Shader>("RiftQuestHighlight").isSupported,"URP quest rim shader supported");
   Capture("rift-quest-collect",1920,1080);var other=new JournalState{CharacterId="other"};Check(!fruit[0].Eligible(other,reviewCatalog),"Another character without quest is ineligible for local glow/collect");
   var old=fruit[0].transform.position;fruit[0].transform.position=actor.transform.position+Vector3.forward*20;Check(!journal.ServerCollect(fruit[0].StableId,out _),"Server rejects collect beyond interaction range");fruit[0].transform.position=old;
   var duplicate=Fruit(old,"a");Check(!journal.ServerCollect(fruit[0].StableId,out _),"Server rejects duplicate configured source ids");UnityEngine.Object.Destroy(duplicate.gameObject);
   actor.Health.ServerSetVitals(actor.Health.MaxHealth,0);Check(!journal.ServerCollect(fruit[0].StableId,out _),"Server rejects collect while dead");actor.Health.ServerSetVitals(actor.Health.MaxHealth,actor.Health.MaxHealth);fruitPositions=fruit.Select(f=>f.transform.position).ToArray();for(int i=0;i<fruit.Length;i++)fruit[i].transform.position=actor.transform.position-Vector3.forward*20+Vector3.right*i;Advance();return;
  }
  if(step==102){
   Check(guidance.Offscreen,"Objective behind the camera uses a direction arrow");Capture("rift-quest-direction",1920,1080);for(int i=0;i<fruit.Length;i++)fruit[i].transform.position=fruitPositions[i];
   blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="Quest LOS validation blocker";blocker.transform.position=(actor.transform.position+Vector3.up+fruit[0].transform.position+Vector3.up*.15f)*.5f;blocker.transform.localScale=new Vector3(.6f,2,.6f);int mask=GameManager.environmentLayer;for(int layer=0;layer<32;layer++)if((mask&(1<<layer))!=0){blocker.layer=layer;break;}Physics.SyncTransforms();Check(!journal.ServerCollect(fruit[0].StableId,out _),"Server rejects collect through an obstacle");UnityEngine.Object.Destroy(blocker);Advance();return;
  }
  if(step==103){
   Check(journal.ServerCollect(fruit[0].StableId,out _),"Server collects a nearby eligible source");Check(journal.State.Quest(collectionQuest.Id).Progress[0]==1,"One source grants one item of quest progress");Check(!journal.ServerCollect(fruit[0].StableId,out _),"Repeated collect cannot duplicate credit");var loaded=JsonUtility.FromJson<JournalState>(JsonUtility.ToJson(journal.State));Check(!loaded.CanCollect(reviewCatalog,fruit[0].StableId,fruit[0].ItemKey),"Collection receipt survives serialization/reload");
   var other=new JournalState{CharacterId="other"};other.Accept(collectionQuest);other.RecordCollection(reviewCatalog,fruit[0].StableId,fruit[0].ItemKey);Check(other.Quest(collectionQuest.Id).Progress[0]==1&&fruit[0].gameObject.activeInHierarchy,"Same source stays available to another eligible character");Advance();return;
  }
  if(step==104){Check(!fruit[0].Eligible(RiftJournalView.Read(),reviewCatalog),"Collected source loses local eligibility after snapshot");InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();RiftMMOController.Instance.SelectWorld(fruit[1].GetComponent<RiftWorldTarget>());InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.F));Advance();return;}
  if(step==105){InputSystem.QueueStateEvent(keyboard,new KeyboardState());Check(!RiftVillageUI.Instance.TalkingTo,"F on a selected quest item does not open a nearby NPC");Check(journal.State.Quest(collectionQuest.Id).Progress[0]==2,"World interaction dispatches real Mirror collect command");Check(journal.ServerCollect(fruit[2].StableId,out _),"Third distinct source completes collect objective");Advance();return;}
  if(step==106){
   Check(guidance.HighlightCount==0,"Completed collect objective clears source highlights");
   var definition=UnityEngine.Object.FindObjectsByType<RiftEnemyPopulation>(FindObjectsSortMode.None).SelectMany(p=>p.Zones).Where(z=>z&&z.Enemies!=null).SelectMany(z=>z.Enemies).Select(e=>e.Enemy).First(e=>e&&!e.IsWildlife);
   reviewEnemyDefinition=ScriptableObject.Instantiate(definition);reviewEnemyDefinition.Id="review-demon";reviewEnemyDefinition.DisplayName="Objetivo de prueba";
   var outside=UnityEngine.Object.FindObjectsByType<RiftEnemySpawnZone>(FindObjectsSortMode.None).Where(z=>z&&z.SpawnPoints!=null).SelectMany(z=>z.SpawnPoints).First(t=>t&&!ROM.Horde.Horde.Instance.IsSafe(t.position));Check(NavMesh.SamplePosition(outside.position,out var nav,15,NavMesh.AllAreas),"Controlled kill fixture has a valid navmesh position");actor.ServerTeleport(nav.position-Vector3.forward*3,0);reviewEnemy=UnityEngine.Object.Instantiate(definition.Prefab,nav.position,Quaternion.identity);reviewEnemy.Definition=reviewEnemyDefinition;NetworkServer.Spawn(reviewEnemy.gameObject);reviewEnemy.GetComponent<NavMeshAgent>().enabled=false;reviewEnemy.enabled=false;reviewEnemy.ServerKilled+=(enemy,killer)=>ROM.Horde.Horde.Instance.ServerNativeEnemyReward(enemy.Definition,killer,null,false);killXP=journal.State.Experience;Advance();return;
  }
  if(step==107){
   Check(RiftQuestGuidance.EnemyMatches(reviewEnemy.Health,collectionQuest.Objectives[1],actor),"Typed kill objective matches the actual enemy definition");Check(guidance.HighlightCount>0,"Active kill objective highlights an actual enemy");Check(guidance.Destination==reviewEnemy.transform,"Tracker moves from collected objects to kill target");Capture("rift-quest-enemy",1920,1080);
   reviewEnemy.Health.Server_ChangeHealthStateRaw(999999,0,AttackType.hitscan,actor.Health,0);Advance();return;
  }
  if(step==108){
   Check(journal.State.Quest(collectionQuest.Id).Status==QuestStatus.Ready,"Authoritative enemy death completes active typed quest");Check(journal.State.Experience>killXP,"Normal enemy experience still granted");Check(guidance.HighlightCount==0,"Dead target and ready quest remove local rim overlays");Check(guidance.Destination==iria.transform,"Ready quest guides back to actual turn-in NPC");Capture("rift-quest-turnin",1920,1080);actor.ServerTeleport(iria.transform.position-Vector3.forward*2,0);Check(journal.ServerTurnIn(collectionQuest.Id,"iria"),"Existing server turn-in completes controlled quest");Advance();return;
  }
  if(step==109){Check(journal.State.Quest(collectionQuest.Id).Status==QuestStatus.TurnedIn,"Completed quest remains persisted");Check(!journal.ServerCollect(fruit[0].StableId,out _),"Completed quest cannot collect extra items");Check(journal.ServerAccept(abandonQuest.Id,"iria"),"Second controlled quest accepted for abandon cleanup");RiftJournalView.Tracked=abandonQuest.Id;Advance();return;}
  if(step==110){Check(guidance.HighlightCount>=3,"New eligible quest can use the same persistent sources");fruit[0].gameObject.SetActive(false);Advance();return;}
  if(step==111){Check(guidance.HighlightCount==2,"Disabling a quest source removes its local highlight");PlayerPrefs.SetString("ROM.Character.ActiveId",id+"other");Advance();return;}
  if(step==112){Check(guidance.HighlightCount==0&&!guidance.Destination,"Changing local character clears old quest markers and glow");PlayerPrefs.SetString("ROM.Character.ActiveId",id);journal.CmdAbandon(abandonQuest.Id);Advance();return;}
  if(step==113){Check(!journal.State.Quests.Any(q=>q.Id==abandonQuest.Id),"Existing Mirror abandon command removes quest");Check(guidance.HighlightCount==0&&!guidance.Destination,"Abandon clears markers and local highlights");Finish(true);}
 }
 static void CleanupQuests(){if(!reviewCatalog)return;typeof(RiftJournalCatalog).GetField("cached",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).SetValue(null,originalCatalog);PlayerPrefs.SetString("ROM.Journal.Track."+id,oldTracked??"");foreach(var q in new[]{collectionQuest,abandonQuest})if(q)PlayerPrefs.DeleteKey("ROM.Journal.Reward.v1."+id+"."+q.Id);PlayerPrefs.DeleteKey("ROM.Journal.Cache.v1."+id+"other");if(fruit!=null)foreach(var item in fruit)if(item){var mat=item.GetComponent<Renderer>().sharedMaterial;UnityEngine.Object.Destroy(item.gameObject);UnityEngine.Object.Destroy(mat);}if(blocker)UnityEngine.Object.Destroy(blocker);if(reviewEnemy)NetworkServer.Destroy(reviewEnemy.gameObject);if(reviewEnemyDefinition)UnityEngine.Object.Destroy(reviewEnemyDefinition);UnityEngine.Object.Destroy(reviewCatalog);UnityEngine.Object.Destroy(collectionQuest);UnityEngine.Object.Destroy(abandonQuest);}
}
}
