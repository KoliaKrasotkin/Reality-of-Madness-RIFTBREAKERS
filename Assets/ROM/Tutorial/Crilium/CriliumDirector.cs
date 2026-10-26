using System;using System.Collections;using System.Collections.Generic;using System.Linq;
using Mirror;using MultiFPS;using MultiFPS.Gameplay;using MultiFPS.Gameplay.RiftCombat;using MultiFPS.Gameplay.RiftEnemies;using MultiFPS.UI;
using ROM.Progression;using ROM.Horde;using ROM.UI.Gameplay;using ROM.UI.Sprint01;using UnityEngine;
namespace ROM.Tutorial.Crilium {
public sealed partial class CriliumDirector:MonoBehaviour {
 public CriliumDefinition Definition;public Transform Arrival,ExitPortal;public float PortalRadius=4;
 public int LessonIndex{get;private set;}public CriliumLesson Current=>Definition&&Definition.Lessons!=null&&LessonIndex<Definition.Lessons.Length?Definition.Lessons[LessonIndex].Lesson:CriliumLesson.Movement;
 public bool Completed=>Current==CriliumLesson.Portal;public Vector3 Goal=>Definition&&Definition.Lessons.Length>LessonIndex?Definition.Lessons[LessonIndex].Goal:Arrival.position;
 public int ConnectedAutoAttacks{get;private set;}public int CleaveVictims{get;private set;}public bool GeneratedFury{get;private set;}public bool SpentFury{get;private set;}
 readonly List<RiftEnemyAgent> enemies=new List<RiftEnemyAgent>();readonly HashSet<int> finalAbilities=new HashSet<int>();
 PlayerInstance player;AbilityController combat;RiftJournalPlayer journal;CharacterInstance actor;RiftFieldUI field;RiftHub hub;
 Vector3 lastPosition,lastSafe;float traveled,startYaw,startPitch,startDistance;double enteredAt,successAt=-1;bool started,leaving,restoring,addsSpawned,standUsed,roarUsed,inspectedItem,inspectedAbility,inspectedTalent,journalOpened,mapOpened;double leapReady,standReady,roarReady;uint leapSequenceAtEntry;
 RiftGroundLoot lessonDrop;GameObject waypoint;bool introTalked;bool retryAfterDeath;float nextHud;
 void OnEnable(){AbilityController.OffensiveActionConnected+=Connected;CombatController.Damaged+=Damaged;}
 void OnDisable(){AbilityController.OffensiveActionConnected-=Connected;CombatController.Damaged-=Damaged;}
 void Start(){if(!Definition)Definition=CriliumDefinition.Load;BuildPresentation();lastSafe=Arrival?Arrival.position:Vector3.up*2;}
 void Update(){
  if(!NetworkServer.active||leaving||!Definition)return;
  player=GameManager.Players.FirstOrDefault(p=>p&&!p.BOT&&p.MyCharacter);if(!player)return;combat=player.GetComponent<AbilityController>();journal=player.GetComponent<RiftJournalPlayer>();if(!combat||!combat.Prototype||!journal||journal.State==null)return;
  if(actor!=player.MyCharacter){actor=player.MyCharacter;lastPosition=actor.transform.position;}
  if(actor.Health.CurrentHealth<=0){retryAfterDeath=true;return;}
  if(retryAfterDeath){actor.ServerTeleport(lastSafe,0);retryAfterDeath=false;RestartLesson(false);}
  if(!started){started=true;field=UnityEngine.Object.FindFirstObjectByType<RiftFieldUI>();if(!field){var pf=Resources.Load<GameObject>("RiftFieldUI");if(pf)field=Instantiate(pf).GetComponent<RiftFieldUI>();}hub=(UIPauseMenu._instance?UIPauseMenu._instance.pauseMenuOverlay.GetComponentInChildren<RiftHub>(true):null);var saved=CriliumSession.Resume;if(saved!=null)try{var state=JsonUtility.FromJson<JournalState>(saved.Journal);if(state!=null&&state.CharacterId==journal.State.CharacterId&&state.Quests.Count<=1&&state.Quests.All(q=>q.Id==Definition.QuestId)&&state.LootAxes.Count<=1){JsonUtility.FromJsonOverwrite(saved.Journal,journal.State);if(journal.State.Quest(Definition.QuestId)!=null)RiftJournalView.Tracked=Definition.QuestId;LessonIndex=Mathf.Clamp(saved.Lesson,0,Definition.Lessons.Length-1);var accepted=journal.State.Quest(Definition.QuestId);int combatStart=Array.FindIndex(Definition.Lessons,l=>l.Lesson==CriliumLesson.AutoAttack);if(accepted!=null&&accepted.Status!=QuestStatus.TurnedIn&&combatStart>=0&&LessonIndex<combatStart)LessonIndex=combatStart;var resumeFall=GetComponent<CriliumRiftFall>();if(saved.FragmentFallen&&resumeFall)resumeFall.RestoreCompletedWorld();var position=LessonIndex==0?Arrival.position:Definition.Lessons[LessonIndex].Goal+new Vector3(0,0,-5);if(UnityEngine.AI.NavMesh.SamplePosition(position,out var resumeHit,8,UnityEngine.AI.NavMesh.AllAreas)){lastSafe=resumeHit.position+Vector3.up*.3f;actor.ServerTeleport(lastSafe,0);lastPosition=lastSafe;}}}catch{ShowNotice("No se pudo recuperar el punto del prólogo; se inicia sin tocar tu progreso normal.");}journal.ServerGrantExperience(0);EnterLesson();}
  var fall=GetComponent<CriliumRiftFall>();if(fall&&fall.Playing)return;var pos=actor.transform.position;float delta=Vector3.Distance(pos,lastPosition);if(delta<2&&actor.Input.Movement.sqrMagnitude>.01f)traveled+=delta;lastPosition=pos;
  if(pos.y<-14||Mathf.Abs(pos.x)>100||pos.z<-40||pos.z>230){actor.ServerTeleport(lastSafe,0);lastPosition=lastSafe;ShowNotice("Volvés al último espacio seguro.");}
  bool near=Vector3.Distance(new Vector3(pos.x,0,pos.z),Goal)<Definition.Lessons[LessonIndex].ArrivalRadius;
  if(near&&successAt<0)lastSafe=new Vector3(Goal.x,2,Goal.z-5);
  if(Time.unscaledTime>=nextHud){nextHud=Time.unscaledTime+.1f;ObserveScreens();RefreshPresentation();}
  bool done=false;switch(Current){
   case CriliumLesson.Movement:done=near&&traveled>=8;break;
   case CriliumLesson.Camera:var m=RiftMMOController.Instance;if(m){bool turned=Mathf.Abs(Mathf.DeltaAngle(startYaw,m.CameraYaw))>8||Mathf.Abs(startPitch-m.CameraPitch)>5;bool zoomed=Mathf.Abs(startDistance-m.CameraDistance)>.4f;cameraTurned|=turned;cameraZoomed|=zoomed;done=near&&cameraTurned&&cameraZoomed;}break;
   case CriliumLesson.Introduction:var arrival=UnityEngine.Object.FindFirstObjectByType<CriliumArrival>();introTalked|=arrival&&RiftVillageUI.Instance&&RiftVillageUI.Instance.TalkingTo==arrival.Guide;done=introTalked&&journal.State.Quest(Definition.QuestId)?.Status==QuestStatus.Active&&(!RiftVillageUI.Instance||!RiftVillageUI.Instance.TalkingTo);if(done)RiftJournalView.Tracked=Definition.QuestId;break;
   case CriliumLesson.AutoAttack:done=near&&ConnectedAutoAttacks>=2;break;
   case CriliumLesson.Fury:GeneratedFury|=combat.Resource>=25;done=near&&GeneratedFury&&SpentFury;break;
   case CriliumLesson.Cleave:done=near&&CleaveVictims>=3;break;
   case CriliumLesson.Mobility:done=combat.GroundLeapSequence!=leapSequenceAtEntry&&NetworkTime.time>=combat.CastEnd;break;
   case CriliumLesson.Defence:standUsed|=combat.Ready.Count>6&&combat.Ready[6]>standReady;roarUsed|=combat.Ready.Count>3&&combat.Ready[3]>roarReady;done=near&&standUsed&&roarUsed;break;
   case CriliumLesson.Loot:if(journal.State.LootAxes.Count>lootBefore){journal.ServerRecord(RiftProgressEvent.Collect,"crilium-axe",1,0);done=true;}break;
   case CriliumLesson.Equipment:done=near&&inspectedItem&&!string.IsNullOrEmpty(journal.State.EquippedLootAxe)&&ScreensClosed;break;
   case CriliumLesson.Spellbook:done=near&&inspectedAbility&&ScreensClosed;break;
   case CriliumLesson.Talents:done=near&&inspectedTalent&&ScreensClosed;break;
   case CriliumLesson.Journal:done=near&&journalOpened&&mapOpened&&field&&!field.MapOpen&&RiftJournalView.Tracked==Definition.QuestId&&ScreensClosed;break;
   case CriliumLesson.Map:done=near&&mapOpened&&field&&!field.MapOpen;break;
   case CriliumLesson.Final:var boss=enemies.FirstOrDefault(e=>e&&e.Definition.IsBoss);if(boss&&!addsSpawned&&boss.Health.CurrentHealth<boss.Health.MaxHealth*.6f){addsSpawned=true;for(int n=0;n<3;n++)Spawn(Definition.Enemy,Goal+new Vector3((n-1)*3,0,-9));}if(finalDefeated&&fall&&!fall.HasFallen)fall.TryPlay();done=finalDefeated&&(!fall||fall.HasFallen&&!fall.Playing);break;
   case CriliumLesson.Portal:if((!fall||fall.HasFallen)&&near&&Vector3.Distance(pos,ExitPortal.position)<PortalRadius&&NetworkTime.time-enteredAt>2)StartCoroutine(LeaveThroughRift());break;
  }
  if(done&&successAt<0){successAt=NetworkTime.time;ShowNotice("Bien. Seguís dominando tu poder.");}
  if(successAt>=0&&NetworkTime.time-successAt>1.1&&LessonIndex<Definition.Lessons.Length-1){LessonIndex++;EnterLesson();}
 }
 bool ScreensClosed=>!UIPauseMenu._instance||!UIPauseMenu._instance.InterfaceShowing;
 bool cameraTurned,cameraZoomed,finalDefeated;Vector3 mobilityStart;int lootBefore;
 void EnterLesson(){
  enteredAt=NetworkTime.time;successAt=-1;traveled=0;ConnectedAutoAttacks=CleaveVictims=0;GeneratedFury=SpentFury=standUsed=roarUsed=false;inspectedItem=inspectedAbility=inspectedTalent=journalOpened=mapOpened=false;cameraTurned=cameraZoomed=finalDefeated=addsSpawned=false;finalAbilities.Clear();ClearEnemies();
  var m=RiftMMOController.Instance;if(m){startYaw=m.CameraYaw;startPitch=m.CameraPitch;startDistance=m.CameraDistance;}
  leapSequenceAtEntry=combat.GroundLeapSequence;mobilityStart=actor.transform.position;leapReady=combat.Ready.Count>4?combat.Ready[4]:0;standReady=combat.Ready.Count>6?combat.Ready[6]:0;roarReady=combat.Ready.Count>3?combat.Ready[3]:0;
  if(Current==CriliumLesson.AutoAttack||Current==CriliumLesson.Fury)SpawnTarget(Goal+Vector3.right*3+Vector3.forward*2);
  if(Current==CriliumLesson.Cleave){for(int i=0;i<4;i++)SpawnTarget(Goal+new Vector3((i-1.5f)*.8f,0,2.5f));}
  if(Current==CriliumLesson.Defence){combat.ServerCriliumCheckpoint(Mathf.Max(combat.Resource,65),false);if(Current==CriliumLesson.Defence)Spawn(Definition.Enemy,Goal+Vector3.forward*4);}
  if(Current==CriliumLesson.Loot){lootBefore=0;if(lessonDrop)NetworkServer.Destroy(lessonDrop.gameObject);if(journal.State.LootAxes.Count==0)lessonDrop=RiftGroundLoot.ServerCreate(player,Goal+Vector3.right*2,0,20);actor.Health.ServerHeal(actor.Health.MaxHealth,actor.Health);}
  if(Current==CriliumLesson.Final){combat.ServerCriliumCheckpoint(65,false);actor.Health.ServerHeal(actor.Health.MaxHealth,actor.Health);Spawn(Definition.Sentinel,Goal+Vector3.forward*3);}
  if(Current==CriliumLesson.Portal){var q=Definition.Journal.Quest(Definition.QuestId);journal.State.TurnIn(q);journal.ServerGrantExperience(0);}
  if(waypoint)waypoint.transform.position=Goal+Vector3.up*.12f;CloseHelp();RefreshPresentation();CriliumSession.SaveCheckpoint(LessonIndex,journal.State,GetComponent<CriliumRiftFall>()&&GetComponent<CriliumRiftFall>().HasFallen);RiftJournalView.RefreshTransientPresentation();
 }
 RiftEnemyAgent Spawn(RiftEnemyDefinition definition,Vector3 position){if(!definition||!definition.Prefab)return null;if(!UnityEngine.AI.NavMesh.SamplePosition(position,out var hit,8,UnityEngine.AI.NavMesh.AllAreas)){ShowNotice("No se encontró suelo accesible para este encuentro.");return null;}var enemy=Instantiate(definition.Prefab,hit.position,Quaternion.identity);enemy.Definition=definition;enemy.ConfigureSpawn(1,32,"Crilium");enemy.ServerKilled+=Killed;enemies.Add(enemy);NetworkServer.Spawn(enemy.gameObject);enemy.Health.RegisterNativeSpawn();return enemy;}
 void SpawnTarget(Vector3 p){var enemy=Spawn(Definition.Target,p);if(enemy)enemy.Health.ServerMakeTrainingTarget();}
 void ClearEnemies(){foreach(var e in enemies.ToArray())if(e){e.ServerKilled-=Killed;e.Despawn();}enemies.Clear();}
 void Killed(RiftEnemyAgent victim,Health killer){if(!actor||killer!=actor.Health)return;if(victim.Definition.IsBoss&&Current==CriliumLesson.Final){finalDefeated=true;journal.ServerRecord(RiftProgressEvent.Kill,"crilium-sentinel",1,0);}}
 void Connected(AbilityController owner,string token){if(owner!=combat||!actor)return;int slot=owner.Casting;if(Current==CriliumLesson.AutoAttack&&slot<0)ConnectedAutoAttacks++;if(Current==CriliumLesson.Fury&&slot>0){var a=owner.Definition(slot);if(a&&a.Cost>0)SpentFury=true;}if(Current==CriliumLesson.Cleave&&slot==7)CleaveVictims=Mathf.Max(CleaveVictims,enemies.Count(e=>e&&e.Health.TrainingLastHit>=NetworkTime.time-.08));if(Current==CriliumLesson.Final&&slot>0)finalAbilities.Add(slot);RiftJournalView.RefreshTransientPresentation();}
 void Damaged(Health victim,Health attacker,int amount){if(Current==CriliumLesson.Final&&actor&&victim==actor.Health&&combat.DefensiveUntil>NetworkTime.time)standUsed=true;}
 public void RetryCurrentLesson()=>RestartLesson(true);
 void RestartLesson(bool teleport){if(!NetworkServer.active||!actor||actor.Health.CurrentHealth<=0||leaving)return;if(teleport)actor.ServerTeleport(lastSafe,0);combat.ServerCriliumCheckpoint(Current==CriliumLesson.Fury?0:65,true);actor.Health.ServerSetVitals(actor.Health.MaxHealth,actor.Health.MaxHealth);EnterLesson();}
 public void SkipToRift(){if(NetworkServer.active&&!leaving)StartCoroutine(LeaveThroughRift());}
 public void ExitToMenu(){if(leaving)return;leaving=true;ClearEnemies();if(NetworkServer.active&&NetworkClient.active)DNNetworkManager.Instance.StopHost();}
 IEnumerator LeaveThroughRift(){leaving=true;ClearEnemies();if(message)message.text="EL RIFT TE ARREBATA TU PODER\nLo recuperarás al otro lado.";if(waypoint)waypoint.SetActive(false);yield return new WaitForSecondsRealtime(2);
  foreach(var j in UnityEngine.Object.FindObjectsByType<RiftJournalPlayer>(FindObjectsSortMode.None))j.ResetCriliumJournal();RiftJournalView.ClearTransientView();CriliumSession.BeginCrossing();DNNetworkManager.Instance.ServerChangeScene(CriliumSession.RiftScene);
 }
 void OnDestroy(){if(NetworkServer.active)ClearEnemies();if(traceMaterial)Destroy(traceMaterial);}
}
}
