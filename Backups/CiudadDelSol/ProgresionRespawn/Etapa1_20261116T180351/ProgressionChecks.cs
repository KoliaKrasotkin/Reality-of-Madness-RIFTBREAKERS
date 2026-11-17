using Object=UnityEngine.Object;
using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.AI;using Mirror;using MultiFPS;using MultiFPS.Gameplay;using MultiFPS.Gameplay.Gamemodes;using MultiFPS.Gameplay.RiftCombat;using MultiFPS.Gameplay.RiftEnemies;using ROM.Progression;using ROM.CiudadDelSol;
namespace UnityEngine {public class Object {public static implicit operator bool(Object o)=>o!=null;} public class ScriptableObject:Object{public static T CreateInstance<T>()where T:new()=>new T();}public class Material:Object{}public class CreateAssetMenuAttribute:Attribute{public string menuName;}public class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}}public class MinAttribute:Attribute{public MinAttribute(float a){}}public class HeaderAttribute:Attribute{public HeaderAttribute(string a){}}public static class Resources{public static object Rules;public static T Load<T>(string n)where T:class=>Rules as T;}public struct Color{public Color(float r,float g,float b){}}public static class Mathf{public static int Clamp(int n,int a,int b)=>Math.Min(b,Math.Max(a,n));public static int Min(int a,int b)=>Math.Min(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static int RoundToInt(float n)=>(int)Math.Round(n,MidpointRounding.ToEven);}public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static float Distance(Vector3 a,Vector3 b)=>0;}public struct Quaternion{public static Quaternion Euler(float a,float b,float c)=>new Quaternion();}public class Transform{public Vector3 position;}public class Scene{public string path;}public class GameObject:Object{public Scene scene;public RiftEnemyAgent Enemy;}public static class Debug{public static List<string> Logs=new List<string>();public static void Log(string s,object c){Logs.Add(s);}public static void LogWarning(string s,object c){Logs.Add(s);}public static void LogError(string s,object c){Logs.Add(s);}public static void LogException(Exception e,object c){Logs.Add(e.Message);}}}
namespace UnityEngine.AI{public class NavMeshAgent{public float speed=2.4f;public bool isOnNavMesh=true;}}
namespace Mirror{public static class NetworkTime{public static double time;}public static class NetworkServer{public static bool active=true,FailSpawn;static uint sequence;public static int Spawns;public static void Spawn(GameObject g){if(FailSpawn)throw new Exception("Simulated network spawn failure");var e=g.Enemy;e.netId=++sequence;Spawns++;e.isServer=true;e.Level=e.RequestedLevel;e.Health.ServerSetVitals(e.LevelHealth,e.LevelHealth);e.Navigation.speed=e.Definition.MoveSpeed;} }}
namespace MultiFPS{public class PlayerInstance:Object{public bool BOT;public Character MyCharacter;public RiftJournalPlayer Journal=new RiftJournalPlayer();public T GetComponent<T>()where T:class=>Journal as T;}public class Character:Object{public Transform transform=new Transform();public Health Health=new Health();public GameObject gameObject=new GameObject();}public static class GameManager{public static List<PlayerInstance> Players=new List<PlayerInstance>();}}
namespace MultiFPS.Gameplay{public class Health:Object{public Transform transform=new Transform();public int CurrentHealth=100,MaxHealth=100;public float CombatDamageMultiplier=1;public GameObject gameObject=new GameObject();public void ServerSetVitals(int max,int now){MaxHealth=max;CurrentHealth=now;}public void RegisterNativeSpawn(){}}public class RiftClassDefinition:Object{}}
namespace MultiFPS.Gameplay.Gamemodes{public class Raid{}}
namespace MultiFPS.Gameplay.RiftCombat{public class RiftLootTableDefinition:Object{}public class RiftProgressionCaps{public int WorldCap=60;public static RiftProgressionCaps Load=new RiftProgressionCaps();}public static class RiftClassCombat{public static PlayerInstance Owner(Health health)=>!health?null:GameManager.Players.FirstOrDefault(p=>p&&!p.BOT&&p.MyCharacter!=null&&p.MyCharacter.Health==health);}}
namespace MultiFPS.Gameplay.RiftEnemies{public class RiftEnemyAgent:Object{public RiftEnemyDefinition Definition;public Health Health=new Health();public NavMeshAgent Navigation=new NavMeshAgent();public Transform transform=new Transform();public GameObject gameObject;public string name,QuestEncounter;public int Level=1,RequestedLevel=1;public uint netId;public bool isServer,CityRaidPursuitActive=true;public Action<RiftEnemyAgent> ReleaseToPool;public Func<Health,bool> ServerTargetGate,ServerAttackTargetGate;public event Action<RiftEnemyAgent,Health> ServerKilled;public int LevelHealth=>RiftEnemyLevelTuning.Load.Health(Definition.Health,Level);public int LevelDamage=>RiftEnemyLevelTuning.Load.Damage(Definition.Damage,Level);public RiftEnemyAgent(){gameObject=new GameObject{Enemy=this};}public void ConfigureSpawn(int level,float leash=0,string encounter="",bool cityRaidPursuit=false){RequestedLevel=RiftEnemyLevelTuning.Load.ClampLevel(level);QuestEncounter=encounter;CityRaidPursuitActive=cityRaidPursuit;}public T GetComponent<T>()where T:class=>Navigation as T;public void Kill(Health killer){Health.CurrentHealth=0;ServerKilled?.Invoke(this,killer);}public void StopAllCoroutines(){}public void Despawn(){}}}
namespace ROM.Progression{public enum RiftProgressEvent{Kill,MiniBoss}public class RiftJournalPlayer:Object{public int Experience;public int Records;public void ServerRecord(RiftProgressEvent kind,string key,int amount=1,int experienceOverride=-1,string encounter=""){Experience+=experienceOverride;Records++;}}}
namespace ROM.CiudadDelSol{public static class CiudadDelSolSpawnSafety{public static bool Recovery(Vector3 p,CiudadDelSolPopulationDefinition c)=>false;public static bool AttackOnSameFloor(RiftEnemyAgent e,Health h)=>true;}}
public class Trial {const int NormalMaximumLevel=3;const float NormalSpeedPerLevel=.01f;
public sealed class Post
        {
            internal CiudadEnemyPost Definition;
            // Session-only state of this stable post; never written to the authored definition or a netId.
            internal int NextSpawnLevel = 1;
            internal int SpawnLevel => Definition.Rank == CiudadEnemyRank.Normal ? NextSpawnLevel : Definition.Level;
            internal Vector3 Reference;
            internal RiftEnemyAgent Occupant;
            internal readonly CiudadDelSolRespawnSchedule Schedule = new CiudadDelSolRespawnSchedule();
            internal double RetryAt;
            internal string LastReason;
            internal bool Approved;
            internal Vector3[] Patrol = Array.Empty<Vector3>();
        }
readonly List<Post> posts=new List<Post>();readonly Dictionary<RiftEnemyAgent,Post> owners=new Dictionary<RiftEnemyAgent,Post>();CiudadDelSolPopulationDefinition configuration=new CiudadDelSolPopulationDefinition{GlobalMaximum=29};bool shuttingDown,faulted,initialized=true;public bool Faulted=>faulted;public int OccupiedCount=>posts.Count(p=>p.Occupant);public GameObject gameObject=new GameObject{scene=new Scene{path=CiudadDelSolPopulationDefinition.ScenePath}};public Post Add(string id,CiudadEnemyRank rank=CiudadEnemyRank.Normal,int authoredLevel=1,bool respawn=true){var d=new RiftEnemyDefinition();d.Prefab=new RiftEnemyAgent{Definition=d};var p=new Post{Definition=new CiudadEnemyPost{Id=id,Rank=rank,Level=authoredLevel,Respawn=respawn,Enemy=d}};posts.Add(p);return p;}public RiftEnemyAgent Appear(Post p){Spawn(p,new Vector3());return p.Occupant;}public void Repeat(RiftEnemyAgent e,Health k)=>Killed(e,k);public void ReapplyScale(Post p)=>Scale(p,p.Occupant);public void RemoveCorpse(Post p){p.Occupant=null;}public void Disappear(Post p){p.Occupant=null;p.Schedule.Lost(NetworkTime.time,p.Definition.RespawnSeconds,p.Definition.AutoRespawn);}public int Posts=>posts.Count;static void Require(bool ok,string why){if(!ok)throw new Exception(why);}void InstallPatrol(Post p){}void SceneManagementMove(GameObject g){g.scene=gameObject.scene;g.Enemy.Health.gameObject.scene=g.scene;}RiftEnemyAgent Instantiate(RiftEnemyAgent prefab,Vector3 p,Quaternion r)=>new RiftEnemyAgent{Definition=prefab.Definition};void Destroy(Object o){}
void Attach(Post post, RiftEnemyAgent enemy)
        {
            post.Occupant = enemy; post.Schedule.Spawned(); post.LastReason = null;
            owners.Add(enemy, post);
            enemy.ServerKilled += Killed;
            enemy.ServerTargetGate = target => target && target.gameObject.scene == gameObject.scene &&
                !CiudadDelSolSpawnSafety.Recovery(target.transform.position, configuration);
            enemy.ServerAttackTargetGate = target => enemy.ServerTargetGate != null && enemy.ServerTargetGate(target) &&
                CiudadDelSolSpawnSafety.AttackOnSameFloor(enemy, target);
            InstallPatrol(post);
        }
void Killed(RiftEnemyAgent enemy, Health killer)
        {
            if (!NetworkServer.active || shuttingDown || !owners.TryGetValue(enemy, out var post) ||
                post.Occupant != enemy ||
                post.Schedule.Phase != CiudadDelSolRespawnSchedule.State.Occupied) return;
            try
            {
                post.Schedule.Lost(NetworkTime.time, post.Definition.RespawnSeconds, post.Definition.AutoRespawn);
                if (post.Definition.Rank == CiudadEnemyRank.Normal && post.Definition.AutoRespawn)
                    post.NextSpawnLevel = Mathf.Min(NormalMaximumLevel, enemy.Level + 1);
                // Match native kill XP without depending on Horde or sharing XP as party quest credit.
                // Lost consumes this life before dispatch, so duplicate death callbacks cannot award twice.
                var player = RiftClassCombat.Owner(killer);
                var journal = player ? player.GetComponent<RiftJournalPlayer>() : null;
                if (journal)
                {
                    var definition = enemy.Definition;
                    int experience = RiftEnemyLevelTuning.Load.Experience(definition.Experience, enemy.Level);
                    journal.ServerRecord(RiftProgressEvent.Kill, definition.Id, 1, experience, enemy.QuestEncounter);
                    if (definition.IsBoss)
                        journal.ServerRecord(RiftProgressEvent.MiniBoss, definition.Id, 1, 0, enemy.QuestEncounter);
                }
                Debug.Log($"CIUDAD_POBLACION MUERTE {post.Definition.Id}; nivel={enemy.Level}, siguiente={post.SpawnLevel}; " +
                    (post.Definition.AutoRespawn ? $"respawn elegible en {post.Definition.RespawnSeconds:F0}s, sujeto a seguridad y presupuesto." : "sin respawn automático."), this);
            }
            catch (Exception error) { faulted = true; Debug.LogException(error, this); }
        }
void Spawn(Post post, Vector3 position)
        {
            RiftEnemyAgent enemy = null;
            try
            {
                enemy = Instantiate(post.Definition.Enemy.Prefab, position, Quaternion.Euler(0, post.Definition.Yaw, 0));
                SceneManagementMove(enemy.gameObject);
                enemy.name = "Ciudad del Sol · " + post.Definition.Id;
                enemy.ConfigureSpawn(post.SpawnLevel, encounter: post.Definition.EncounterId, cityRaidPursuit: true);
                NetworkServer.Spawn(enemy.gameObject);
                Require(enemy.isServer && enemy.netId != 0 && enemy.CityRaidPursuitActive && enemy.GetComponent<NavMeshAgent>().isOnNavMesh,
                    "Mirror o NavMesh rechazaron el respawn.");
                enemy.Health.RegisterNativeSpawn();
                Scale(post, enemy);
                Attach(post, enemy);
                Debug.Log($"CIUDAD_POBLACION APARICION {post.Definition.Id}, netId={enemy.netId}, " +
                    $"nivel={enemy.Level}, posición={position:F3}, vida={enemy.Health.MaxHealth}, " +
                    $"velocidad={enemy.GetComponent<NavMeshAgent>().speed:F3}, ocupados={OccupiedCount}/{configuration.GlobalMaximum}.", this);
            }
            catch (Exception error)
            {
                faulted = true; // Fail closed: no endless instantiate/destroy loop.
                if (enemy) { if (enemy.netId != 0) enemy.Despawn(); else Destroy(enemy.gameObject); }
                Debug.LogError("CIUDAD_POBLACION ERROR: apariciones suspendidas, sin reintentos: " + error.Message, this);
            }
        }
void Scale(Post post, RiftEnemyAgent enemy)
        {
            if (post.Definition.Rank == CiudadEnemyRank.Normal)
                enemy.GetComponent<NavMeshAgent>().speed = enemy.Definition.MoveSpeed * (1 + NormalSpeedPerLevel * (enemy.Level - 1));
            int participants = GameManager.Players.Count(p => p && !p.BOT && p.MyCharacter &&
                p.MyCharacter.gameObject.scene == gameObject.scene && p.MyCharacter.Health.CurrentHealth > 0 &&
                Vector3.Distance(p.MyCharacter.transform.position, post.Reference) <= post.Definition.ActivationRadius);
            var rules = configuration.ScaleForHumanPlayers ? RiftClassRules.Load() : null;
            int health = Mathf.Clamp(Mathf.RoundToInt(enemy.LevelHealth * post.Definition.HealthMultiplier *
                (rules ? rules.HealthScale(Mathf.Clamp(participants, 1, 4)) : 1)), 1, 100000);
            enemy.Health.ServerSetVitals(health, health);
            enemy.Health.CombatDamageMultiplier = post.Definition.DamageMultiplier * (rules ? rules.DamageScale(Mathf.Clamp(participants, 1, 4)) : 1);
        }
public void Shutdown()
        {
            if (shuttingDown) return;
            shuttingDown = true;
            foreach (var enemy in owners.Keys.ToArray()) if (enemy)
            {
                enemy.ServerKilled -= Killed;
                enemy.ServerTargetGate = null; enemy.ServerAttackTargetGate = null;
                enemy.StopAllCoroutines();
                if (NetworkServer.active && enemy.netId != 0) enemy.Despawn(); else Destroy(enemy.gameObject);
            }
            owners.Clear(); posts.Clear(); initialized = false;
            if (configuration) Destroy(configuration);
        }
}
public static class Checks{static int passed;static void Eq<T>(T a,T b,string label){if(!EqualityComparer<T>.Default.Equals(a,b))throw new Exception(label+": "+a+" != "+b);passed++;Console.WriteLine("PASS "+label);}static void Near(float a,float b,string l){if(Math.Abs(a-b)>.0001f)throw new Exception(l+": "+a+" != "+b);passed++;Console.WriteLine("PASS "+l);}static PlayerInstance Player(Trial t){var p=new PlayerInstance{MyCharacter=new Character()};p.MyCharacter.gameObject.scene=t.gameObject.scene;GameManager.Players.Add(p);return p;}static void Clear(){GameManager.Players.Clear();NetworkServer.active=true;NetworkServer.FailSpawn=false;NetworkTime.time=0;}public static int Main(){try{return Run();}catch(Exception e){Console.WriteLine("FAIL "+e.Message);return 1;}}static int Run(){Resources.Rules=new RiftClassRules();Clear();var t=new Trial();var p=Player(t);var post=t.Add("stable-demon");post.Definition.Enemy.Health=70;post.Definition.Enemy.Damage=5;post.Definition.Enemy.Experience=20;var e=t.Appear(post);Eq(e.Level,1,"normal first spawn level one");Eq(e.Health.MaxHealth,70,"native level-one HP");Eq(e.LevelDamage,5,"native level-one damage");Near(e.Navigation.speed,2.4f,"base speed");uint firstId=e.netId;e.Kill(p.MyCharacter.Health);Eq(post.NextSpawnLevel,2,"death reserves level two");Eq(p.Journal.Experience,20,"first kill awards level-one XP");Eq(post.Schedule.ReadyAt,90d,"native 90-second respawn retained");t.Repeat(e,p.MyCharacter.Health);Eq(post.NextSpawnLevel,2,"duplicate callback no extra level");Eq(p.Journal.Experience,20,"duplicate callback no extra XP");Eq(post.Schedule.Due(89.99),false,"no early respawn");t.RemoveCorpse(post);NetworkTime.time=90;e=t.Appear(post);Eq(e.Level,2,"same post respawns at two");Eq(e.netId!=firstId,true,"progression survives new netId");Eq(e.Health.MaxHealth,76,"HP from native level-two formula");Eq(e.LevelDamage,5,"native integer damage rounding retained");Near(e.Navigation.speed,2.424f,"level-two linear speed");t.ReapplyScale(post);Near(e.Navigation.speed,2.424f,"reapplication does not compound speed");Eq(e.Health.MaxHealth,76,"reapplication does not compound HP");e.Kill(p.MyCharacter.Health);Eq(post.NextSpawnLevel,3,"second death reserves level three");Eq(p.Journal.Experience,47,"second kill awards 27 XP");t.RemoveCorpse(post);NetworkTime.time=180;e=t.Appear(post);Eq(e.Level,3,"third life level three");Eq(e.Health.MaxHealth,82,"HP from native level-three formula");Eq(e.LevelDamage,6,"native level-three damage");Near(e.Navigation.speed,2.448f,"level-three speed");e.Kill(p.MyCharacter.Health);Eq(post.NextSpawnLevel,3,"third death stays capped at three");Eq(p.Journal.Experience,81,"third kill awards 34 XP");t.RemoveCorpse(post);NetworkTime.time=270;var capped=t.Appear(post);Eq(capped.Level,3,"fourth life capped at three");t.Repeat(e,p.MyCharacter.Health);Eq(p.Journal.Experience,81,"old corpse callback after replacement ignored");Eq(post.NextSpawnLevel,3,"old corpse cannot advance replacement");Eq(post.Definition.Level,1,"authored post level not mutated");Eq(post.Definition.Enemy.Health,70,"enemy definition HP not mutated");Near(post.Definition.Enemy.MoveSpeed,2.4f,"enemy definition speed unchanged");
var lost=t.Add("removed");t.Appear(lost);t.Disappear(lost);Eq(lost.NextSpawnLevel,1,"disappearance without death keeps level");t.Appear(lost);Eq(lost.Occupant.Level,1,"replacement after disappearance stays level one");var failed=t.Add("spawn-error");failed.NextSpawnLevel=2;NetworkServer.FailSpawn=true;t.Appear(failed);Eq(failed.NextSpawnLevel,2,"failed spawn does not advance level");Eq(failed.Schedule.Phase,CiudadDelSolRespawnSchedule.State.Initial,"failed spawn consumes no life");Eq(t.Faulted,true,"spawn failure still fails closed");NetworkServer.FailSpawn=false;
Clear();t=new Trial();p=Player(t);var elite=t.Add("elite",CiudadEnemyRank.Elite,2);elite.Definition.HealthMultiplier=2;elite.Definition.DamageMultiplier=1.15f;var ee=t.Appear(elite);Eq(ee.Level,2,"elite keeps authored level");Eq(ee.Health.MaxHealth,152,"elite HP multiplier unchanged");Near(ee.Health.CombatDamageMultiplier,1.15f,"elite damage multiplier unchanged");Near(ee.Navigation.speed,2.4f,"elite speed unchanged");ee.Kill(p.MyCharacter.Health);Eq(elite.NextSpawnLevel,1,"elite does not progress");t.RemoveCorpse(elite);ee=t.Appear(elite);Eq(ee.Level,2,"elite respawn keeps level");var mini=t.Add("regent",CiudadEnemyRank.MiniBoss,1,false);var me=t.Appear(mini);me.Kill(p.MyCharacter.Health);Eq(mini.Schedule.Phase,CiudadDelSolRespawnSchedule.State.Defeated,"miniboss remains defeated");Eq(mini.Schedule.Due(99999),false,"miniboss no auto-respawn");
Clear();t=new Trial();var humans=new[]{Player(t),Player(t),Player(t),Player(t)};post=t.Add("coop");e=t.Appear(post);Eq(e.Health.MaxHealth,Mathf.RoundToInt(70*RiftClassRules.Load().HealthScale(4)),"four-player HP scaling applied once");e.Kill(humans[2].MyCharacter.Health);Eq(post.NextSpawnLevel,2,"four-player kill advances once");Eq(string.Join(",",humans.Select(h=>h.Journal.Experience)),"0,0,20,0","XP goes only to original killer rule");foreach(var h in humans)t.Repeat(e,h.MyCharacter.Health);Eq(post.NextSpawnLevel,2,"multi-client duplicate callbacks advance once");Eq(humans.Sum(h=>h.Journal.Experience),20,"multi-client duplicates cannot duplicate XP");t.RemoveCorpse(post);NetworkTime.time=90;e=t.Appear(post);Eq(e.Level,2,"coop respawn receives server level");Eq(e.Health.MaxHealth,Mathf.RoundToInt(76*RiftClassRules.Load().HealthScale(4)),"native level and coop HP not double-scaled");Near(e.Health.CombatDamageMultiplier,RiftClassRules.Load().DamageScale(4),"existing coop damage assignment untouched");NetworkServer.active=false;t.Repeat(e,humans[0].MyCharacter.Health);Eq(post.NextSpawnLevel,2,"client cannot progress post");Eq(humans[0].Journal.Experience,0,"client cannot award XP");NetworkServer.active=true;var isolated=t.Add("independent");Eq(t.Appear(isolated).Level,1,"another stable post starts independently");
var unknown=new RiftEnemyAgent();t.Repeat(unknown,humans[0].MyCharacter.Health);Eq(humans[0].Journal.Experience,0,"unowned enemy cannot reward");t.Shutdown();Eq(t.Posts,0,"shutdown clears session progression");t=new Trial();var reset=t.Add("coop");Eq(t.Appear(reset).Level,1,"new incursion resets same Id to one");for(int i=1;i<24;i++)Eq(t.Appear(t.Add("normal-"+i)).Level,1,"normal post "+i+" begins at one");
Clear();t=new Trial();p=Player(t);post=t.Add("actual-demon");post.Definition.Enemy.Health=90;e=t.Appear(post);Eq(e.Health.MaxHealth,90,"current Diablillo asset HP level one");e.Kill(p.MyCharacter.Health);t.RemoveCorpse(post);NetworkTime.time=90;e=t.Appear(post);Eq(e.Health.MaxHealth,97,"current Diablillo asset HP level two");e.Kill(p.MyCharacter.Health);t.RemoveCorpse(post);NetworkTime.time=180;e=t.Appear(post);Eq(e.Health.MaxHealth,105,"current Diablillo asset HP level three");
Console.WriteLine("RESULT "+passed+" PASS; production Post, Attach, Killed, Spawn, Scale, Shutdown and native tuning/schedule; Unity/Mirror lifecycle simulated.");return 0;}}
