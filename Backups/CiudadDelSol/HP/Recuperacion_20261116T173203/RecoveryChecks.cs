using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using Mirror;using MultiFPS.Gameplay;using MultiFPS.Gameplay.Gamemodes;
namespace UnityEngine {public class Object{public static implicit operator bool(Object o)=>o!=null;}public class ScriptableObject:Object{}public class Material:Object{}public class CreateAssetMenuAttribute:Attribute{public string menuName;}public static class Resources{public static object Loaded;public static T Load<T>(string p) where T:class=>Loaded as T;}public static class Mathf{public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static int RoundToInt(float n)=>(int)Math.Round(n,MidpointRounding.ToEven);}public static class Time{public static float time;}public static class Debug{public static void LogException(Exception e){throw e;}} public struct Scene{public string path;} public class GameObject{public Scene scene;} }
namespace Mirror {public class ServerAttribute:Attribute{}public static class NetworkServer{public static bool active=true;}public static class NetworkTime{public static double time;}}
namespace MultiFPS.Gameplay {public class RiftClassDefinition:UnityEngine.Object{} public class Info{public bool Warrior=true;public int CharacterSkinID=1;}public class Character:UnityEngine.Object{public Health Health;public GameObject gameObject=new GameObject();}public class Health:UnityEngine.Object{public int CurrentHealth,MaxHealth=200;bool _dead;public int RpcCalls,LastRpcCurrent,LastRpcAmount;public Health ClientSnapshot;public static event Action<Health,Health,int> ServerHealed;public bool Dead{set=>_dead=value;}public int ServerHeal(int amount,Health healer)=>ApplyServerHeal(amount,0,healer);void RpcHealthAdded(int current,int added,byte id){RpcCalls++;LastRpcCurrent=current;LastRpcAmount=added;if(ClientSnapshot!=null)ClientSnapshot.CurrentHealth=current;}
int ApplyServerHeal(int healthToAdd,byte healerID,Health healer)
        {
            if(!NetworkServer.active||healthToAdd<=0||CurrentHealth<=0||_dead||CurrentHealth>=MaxHealth)return 0;
            int effective=(int)System.Math.Min((long)healthToAdd,(long)MaxHealth-CurrentHealth);
            CurrentHealth+=effective;RpcHealthAdded(CurrentHealth,effective,healerID);
            if(healer)try{ServerHealed?.Invoke(this,healer,effective);}catch(Exception e){Debug.LogException(e);}
            return effective;
        }
} public static class GameManager{public static Gamemode Gamemode;public static List<PlayerInstance> Players=new List<PlayerInstance>();} public class PlayerInstance:UnityEngine.Object {public bool isServer=true,BOT;public Character MyCharacter;public Info PlayerInfo=new Info();public int ClassId;public double LastCombatAt;float _nextRecovery,_originalNextRecovery;public float Fury=42;public int Experience=1337;public RiftClassDefinition ClassDefinition=>GameManager.Gamemode&&GameManager.Gamemode.ClassRules?GameManager.Gamemode.ClassRules.Find(ClassId):null;
[Server] public void ServerTickClass(){
            if(!isServer||BOT||!MyCharacter||MyCharacter.Health.CurrentHealth<=0||Time.time<_nextRecovery)return;
            float delay,fraction;
            if(ClassDefinition){
                var rules=GameManager.Gamemode.ClassRules;delay=rules.RecoveryDelay;fraction=rules.RecoveryPerSecond;
            }else{
                var raid=GameManager.Gamemode as Gamemodes.Raid;
                if(!raid||!raid.TryGetCityHealthRecovery(this,out delay,out fraction))return;
            }
            _nextRecovery=Time.time+1;
            if(NetworkTime.time-LastCombatAt>=delay)RiftClassCombat.Heal(this,MyCharacter.Health,fraction);
        }
[Server] public void OriginalTick(){
            if(BOT||!MyCharacter||!ClassDefinition||MyCharacter.Health.CurrentHealth<=0||Time.time<_originalNextRecovery)return;
            _originalNextRecovery=Time.time+1;var rules=GameManager.Gamemode.ClassRules;
            if(NetworkTime.time-LastCombatAt>=rules.RecoveryDelay)RiftClassCombat.Heal(this,MyCharacter.Health,rules.RecoveryPerSecond);
        }
}public static class RiftClassCombat{public static bool Active=>GameManager.Gamemode&&GameManager.Gamemode.ClassRules;public static PlayerInstance Owner(Health health)=>!health?null:GameManager.Players.FirstOrDefault(p=>p&&!p.BOT&&p.MyCharacter&&p.MyCharacter.Health==health);
public static int Heal(PlayerInstance healer,Health target,float fraction){
            if(!target||target.CurrentHealth<=0||target.CurrentHealth>=target.MaxHealth)return 0;
            var owner=Owner(target);float bonus=healer&&healer.ClassId==0&&owner&&owner.ClassId==1?1.25f:1;
            return target.ServerHeal(Mathf.RoundToInt(target.MaxHealth*fraction*bonus),healer.MyCharacter.Health);
        }
} }
namespace MultiFPS.Gameplay.Gamemodes {public class Gamemode:UnityEngine.Object{public virtual RiftClassRules ClassRules=>null;}public class WithRules:Gamemode{public RiftClassRules Rules;public override RiftClassRules ClassRules=>Rules;}public class Raid:Gamemode{
public bool TryGetCityHealthRecovery(PlayerInstance player, out float delay, out float fraction)
        {
            delay = fraction = 0;
            if (!player || !player.MyCharacter || !player.PlayerInfo.Warrior || player.PlayerInfo.CharacterSkinID != 1 ||
                player.MyCharacter.gameObject.scene.path != "Assets/Scenes/Dungeon_Isla_del_Sol.unity") return false;
            var rules = RiftClassRules.Load();
            if (!rules) return false;
            delay = rules.RecoveryDelay;
            fraction = rules.RecoveryPerSecond;
            return true;
        }
}}
public static class Checks {static int passed;const string City="Assets/Scenes/Dungeon_Isla_del_Sol.unity";static void Eq<T>(T a,T b,string name){if(!EqualityComparer<T>.Default.Equals(a,b))throw new Exception(name+": "+a+" != "+b);passed++;Console.WriteLine("PASS "+name);}static void Clock(double n){Time.time=(float)n;NetworkTime.time=n;}static PlayerInstance Player(int hp=40,int max=200){var p=new PlayerInstance{MyCharacter=new Character{Health=new Health{CurrentHealth=hp,MaxHealth=max}}};p.MyCharacter.gameObject.scene=new Scene{path=City};GameManager.Players.Add(p);return p;}static void Reset(){Clock(0);NetworkServer.active=true;GameManager.Players.Clear();GameManager.Gamemode=new Raid();}static void Tick(PlayerInstance p,double t){Clock(t);p.ServerTickClass();}public static int Main(){var rules=new RiftClassRules{Classes=new[]{new RiftClassDefinition()}};Resources.Loaded=rules;Reset();var p=Player();var raid=(Raid)GameManager.Gamemode;Eq(raid.ClassRules,null,"Raid ClassRules remains null");Eq(RiftClassCombat.Active,false,"legacy combat stays disabled");Eq(p.ClassDefinition,null,"no legacy class bound to Warrior");Eq(raid.TryGetCityHealthRecovery(p,out var delay,out var fraction),true,"recovery-only policy accepts City Warrior");Eq(delay,7f,"original delay");Eq(fraction,.08f,"original fraction");for(int i=0;i<7;i++)Tick(p,i);Eq(p.MyCharacter.Health.CurrentHealth,40,"no healing before seven seconds");p.MyCharacter.Health.ClientSnapshot=new Health{CurrentHealth=40};Tick(p,7);Eq(p.MyCharacter.Health.CurrentHealth,56,"eight percent at seven seconds");Eq(p.MyCharacter.Health.LastRpcAmount,16,"native heal publishes effective amount");Eq(p.MyCharacter.Health.ClientSnapshot.CurrentHealth,56,"heal callback carries updated HP (RPC double)");Tick(p,7.8);Eq(p.MyCharacter.Health.CurrentHealth,56,"no second heal within one second");Tick(p,8);Eq(p.MyCharacter.Health.CurrentHealth,72,"next one-second tick");p.LastCombatAt=8.2;for(int i=9;i<=15;i++)Tick(p,i);Eq(p.MyCharacter.Health.CurrentHealth,72,"new combat restarts delay");Tick(p,16);Eq(p.MyCharacter.Health.CurrentHealth,88,"healing resumes after new delay");Eq(p.Fury,42f,"recovery does not write Fury");Eq(p.Experience,1337,"recovery does not write XP");
Reset();p=Player(195);Tick(p,10);Eq(p.MyCharacter.Health.CurrentHealth,200,"healing capped at maximum");Eq(p.MyCharacter.Health.LastRpcAmount,5,"RPC reports five effective HP");Tick(p,11);Eq(p.MyCharacter.Health.RpcCalls,1,"full health sends no repeated heal");
Reset();p=Player(0);Tick(p,20);Eq(p.MyCharacter.Health.CurrentHealth,0,"zero-health player not healed");Eq(p.MyCharacter.Health.RpcCalls,0,"no dead-player RPC");p=Player();p.MyCharacter.Health.Dead=true;Tick(p,20);Eq(p.MyCharacter.Health.CurrentHealth,40,"Health dead guard remains authoritative");
Reset();p=Player();p.isServer=false;Tick(p,20);Eq(p.MyCharacter.Health.CurrentHealth,40,"client tick cannot heal");p.isServer=true;NetworkServer.active=false;Tick(p,22);Eq(p.MyCharacter.Health.CurrentHealth,40,"Health rejects inactive server");
Reset();p=Player();p.BOT=true;Tick(p,20);Eq(p.MyCharacter.Health.CurrentHealth,40,"bots unchanged");p=Player();p.MyCharacter=null;Tick(p,20);Eq(p.MyCharacter,null,"absent character safely ignored");
Reset();p=Player();p.MyCharacter.gameObject.scene=new Scene{path="Assets/OtherRaid.unity"};Tick(p,20);Eq(p.MyCharacter.Health.CurrentHealth,40,"other Raid scenes unchanged");Reset();p=Player();GameManager.Gamemode=new Gamemode();Tick(p,20);Eq(p.MyCharacter.Health.CurrentHealth,40,"other modes without class rules unchanged");Reset();p=Player();GameManager.Gamemode=null;Tick(p,20);Eq(p.MyCharacter.Health.CurrentHealth,40,"missing mode safe");
Reset();p=Player();p.PlayerInfo.Warrior=false;Tick(p,20);Eq(p.MyCharacter.Health.CurrentHealth,40,"non-Warrior excluded");p=Player();p.PlayerInfo.CharacterSkinID=2;Tick(p,20);Eq(p.MyCharacter.Health.CurrentHealth,40,"other character skins excluded");
Reset();var group=new[]{Player(40,100),Player(40,200),Player(40,300),Player(40,400)};foreach(var member in group)Tick(member,7);Eq(string.Join(",",group.Select(x=>x.MyCharacter.Health.CurrentHealth)),"48,56,64,72","four players use their own maximum health");
Reset();p=Player();Tick(p,100);Eq(p.MyCharacter.Health.CurrentHealth,56,"no large catch-up heal after long pause");p.MyCharacter=new Character{Health=new Health{CurrentHealth=200,MaxHealth=200}};p.MyCharacter.gameObject.scene=new Scene{path=City};Tick(p,101);Eq(p.MyCharacter.Health.CurrentHealth,200,"respawn full HP unchanged");p.MyCharacter.Health.CurrentHealth=100;p.LastCombatAt=101;Tick(p,107);Eq(p.MyCharacter.Health.CurrentHealth,100,"post-respawn damage starts new wait");Tick(p,108);Eq(p.MyCharacter.Health.CurrentHealth,116,"recovery works on replacement actor");
Reset();GameManager.Gamemode=new WithRules{Rules=rules};var current=Player(25);var original=Player(25);for(int i=0;i<=30;i++){Clock(i);if(i==4||i==17){current.LastCombatAt=original.LastCombatAt=i;current.MyCharacter.Health.CurrentHealth-=5;original.MyCharacter.Health.CurrentHealth-=5;}current.ServerTickClass();original.OriginalTick();if(current.MyCharacter.Health.CurrentHealth!=original.MyCharacter.Health.CurrentHealth||current.MyCharacter.Health.RpcCalls!=original.MyCharacter.Health.RpcCalls)throw new Exception("Legacy behavior mismatch at "+i);}Eq(current.MyCharacter.Health.CurrentHealth,original.MyCharacter.Health.CurrentHealth,"El Rift matches original tick across combat interruptions");Eq(current.MyCharacter.Health.RpcCalls,original.MyCharacter.Health.RpcCalls,"El Rift heal event count unchanged");
Reset();GameManager.Gamemode=new WithRules{Rules=rules};p=Player();p.ClassId=99;Tick(p,20);Eq(p.MyCharacter.Health.CurrentHealth,40,"invalid legacy class remains excluded");
Console.WriteLine("RESULT "+passed+" PASS; extracted production tick/policy/heal bodies + actual RiftClassRules; Unity and Mirror replaced by test doubles.");return 0;}}
