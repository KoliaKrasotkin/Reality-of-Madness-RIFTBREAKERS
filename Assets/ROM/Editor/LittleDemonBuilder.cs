using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftEnemies;
using ROM.Horde;
namespace ROM.EditorTools {
public static class LittleDemonBuilder {
 public const string Root="Assets/ROM/Enemies/LittleDemon",Package="Assets/SculptiqStudios/DualForm-LittleDemon";
 public const string PrefabPath=Root+"/LittleDemon_Enemy.prefab";
 static AnimationClip Clip(string name,bool loop){string path=Root+"/Animations/"+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip){clip=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationClip>(Package+"/Animations/"+name+".anim"));if(!clip)throw new Exception("Missing imported animation "+name);clip.name=name;AssetDatabase.CreateAsset(clip,path);}var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);return clip;}
 static AnimatorController Controller(){string path=Root+"/LittleDemon_Enemy.controller";var existing=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(existing)return existing;var c=AnimatorController.CreateAnimatorControllerAtPath(path);c.AddParameter("Speed",AnimatorControllerParameterType.Float);var sm=c.layers[0].stateMachine;var idle=sm.AddState("Idle");idle.motion=Clip("Idle01",true);sm.defaultState=idle;var walk=sm.AddState("Walk");walk.motion=Clip("Walk01",true);walk.speed=1.15f;var moving=idle.AddTransition(walk);moving.hasExitTime=false;moving.duration=.15f;moving.AddCondition(AnimatorConditionMode.Greater,.1f,"Speed");var stopped=walk.AddTransition(idle);stopped.hasExitTime=false;stopped.duration=.15f;stopped.AddCondition(AnimatorConditionMode.Less,.1f,"Speed");foreach(string name in new[]{"Attack01","Attack02","Attack03","Hit","Death"}){var state=sm.AddState(name);state.motion=Clip(name=="Hit"?"Hit01":name=="Death"?"Death01":name,false);if(name!="Death"){var exit=state.AddTransition(idle);exit.hasExitTime=true;exit.exitTime=.95f;exit.duration=.12f;}}EditorUtility.SetDirty(c);return c;}
 [MenuItem("ROM/Enemies/Build Little Demon and spawn zones")]
 public static void Build(){
  Directory.CreateDirectory(Root+"/Animations");Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory("Documentation/little-demon");AssetDatabase.Refresh();var report=new List<string>();var controller=Controller();
  var definition=AssetDatabase.LoadAssetAtPath<RiftEnemyDefinition>(Root+"/LittleDemon.asset");if(!definition){definition=ScriptableObject.CreateInstance<RiftEnemyDefinition>();AssetDatabase.CreateAsset(definition,Root+"/LittleDemon.asset");}
  var root=new GameObject("Little Demon - independent enemy");root.AddComponent<NetworkIdentity>();var health=root.AddComponent<Health>();health.CharacterName="Diablillo";health.Team=1;health.MaxHealth=health.CurrentHealth=definition.Health;
  var navigation=root.AddComponent<NavMeshAgent>();navigation.enabled=false;navigation.radius=.3f;navigation.height=1.3f;navigation.acceleration=14;navigation.angularSpeed=540;navigation.obstacleAvoidanceType=ObstacleAvoidanceType.LowQualityObstacleAvoidance;
  var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Package+"/Prefabs/LittleDemon.prefab"));visual.transform.SetParent(root.transform,false);visual.name="Native Little Demon visual";
  foreach(var behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(behaviour);
  var animator=visual.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
  if(!animator.avatar||!animator.avatar.isValid)throw new Exception("Little Demon avatar is invalid");
  Clip("Idle01",true).SampleAnimation(visual,0);
  var renderers=visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);float scale=1.35f/Mathf.Max(.1f,bounds.size.y);visual.transform.localScale*=scale;visual.transform.localPosition-=Vector3.up*bounds.min.y*scale;
  var materials=new Dictionary<Material,Material>();int vertices=0;
  foreach(var renderer in renderers){renderer.updateWhenOffscreen=false;vertices+=renderer.sharedMesh?renderer.sharedMesh.vertexCount:0;var array=renderer.sharedMaterials;for(int i=0;i<array.Length;i++){var source=array[i];if(!source)continue;if(!materials.TryGetValue(source,out var material)){material=LittleDemonMaterials.Variant(source);materials[source]=material;}array[i]=material;}renderer.sharedMaterials=array;}
  var capsule=root.AddComponent<CapsuleCollider>();capsule.height=1.35f;capsule.radius=.3f;capsule.center=Vector3.up*.675f;health.centerPosition=Vector3.up*.85f;
  var hit=new GameObject("Body hitbox");hit.transform.SetParent(root.transform,false);var hitCollider=hit.AddComponent<CapsuleCollider>();hitCollider.height=1.25f;hitCollider.radius=.31f;hitCollider.center=Vector3.up*.65f;hitCollider.isTrigger=true;var box=hit.AddComponent<HitBox>();box._health=health;box.part=CharacterPart.body;
  var agent=root.AddComponent<RiftEnemyAgent>();agent.Definition=definition;agent.Animator=animator;agent.Body=capsule;var sync=root.AddComponent<NetworkTransformReliable>();sync.target=root.transform;sync.syncDirection=SyncDirection.ServerToClient;sync.syncScale=false;
  var prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);definition.Prefab=prefab.GetComponent<RiftEnemyAgent>();LittleDemonMaterials.Preview(prefab);EditorUtility.SetDirty(definition);UnityEngine.Object.DestroyImmediate(root);
  if(prefab.GetComponent<CharacterInstance>()||prefab.GetComponent<PlayerInstance>()||prefab.GetComponent<BotAI>()||prefab.GetComponentInChildren<CharacterAnimator>())throw new Exception("Template enemy component leaked into native prefab");
  report.Add("PASS standalone Mirror prefab: no PlayerInstance, CharacterInstance, BotAI, CharacterAnimator or template skeleton");report.Add("Native renderers="+renderers.Length+" vertices="+vertices+" material variants="+materials.Count);report.Add("Animations: Idle01, Walk01, Attack01/02/03, Hit01, Death01; original package unchanged");
  foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/MultiFPS/Levels","Assets/ROM"}).Select(AssetDatabase.GUIDToAssetPath)){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset||!asset.GetComponent<NetworkManager>())continue;var contents=PrefabUtility.LoadPrefabContents(path);try{var manager=contents.GetComponent<NetworkManager>();if(!manager.spawnPrefabs.Contains(prefab)){manager.spawnPrefabs.Add(prefab);PrefabUtility.SaveAsPrefabAsset(contents,path);}}finally{PrefabUtility.UnloadPrefabContents(contents);}}
  foreach(string path in new[]{ProjectSceneCleanup.Menu,ProjectSceneCleanup.Rift}){var scene=EditorSceneManager.OpenScene(path);foreach(var manager in UnityEngine.Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(!manager.spawnPrefabs.Contains(prefab)){manager.spawnPrefabs.Add(prefab);EditorUtility.SetDirty(manager);}
   if(path==ProjectSceneCleanup.Rift){var world=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();var population=world.GetComponent<RiftEnemyPopulation>();if(!population)population=world.gameObject.AddComponent<RiftEnemyPopulation>();var zones=new List<RiftEnemySpawnZone>();foreach(var encounter in world.Encounters){if(!encounter||encounter.MiniBoss)continue;encounter.NativeSpawn=true;var zone=encounter.GetComponent<RiftEnemySpawnZone>();if(!zone)zone=encounter.gameObject.AddComponent<RiftEnemySpawnZone>();zone.Title=encounter.Title;zone.SpawnPoints=encounter.SpawnPoints;zone.ActivationRadius=encounter.ActivationRadius;zone.Encounter=encounter;if(zone.Enemies==null||zone.Enemies.Length==0)zone.Enemies=new[]{new RiftEnemySpawnChoice{Enemy=definition,Weight=1}};zones.Add(zone);EditorUtility.SetDirty(zone);EditorUtility.SetDirty(encounter);}population.Zones=zones.ToArray();EditorUtility.SetDirty(population);report.Add("Native population zones="+zones.Count+" global live cap="+population.MaximumAlive+"; miniboss encounters retain their existing prefab/AI");}
   EditorSceneManager.SaveScene(scene);
  }
  AssetDatabase.SaveAssets();File.WriteAllLines("Documentation/little-demon/build-validation.txt",report);
 }
 public static void Run(){try{Build();RiftJournalProbe.RunNativeEnemies();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
}
