using System.Collections.Generic;
using System.Linq;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using ROM.UI.Sprint01;
using UnityEngine;
namespace ROM.Horde {
    public partial class Horde {
        [Header("Exploración de El Rift")]
        public bool ExplorationMode;
        public override bool UseLocalPatrol => ExplorationMode;
        public override bool IsProtectedPosition(Vector3 point) => IsSafe(point);
        public override bool BlockDamage(Health victim, Health attacker) => BlockExplorationDamage(victim,attacker);
        public RiftCheckpoint[] Checkpoints;
        public RiftEncounter[] Encounters;
        readonly Dictionary<PlayerInstance,RiftCheckpoint> _checkpoints=new Dictionary<PlayerInstance,RiftCheckpoint>();
        readonly Dictionary<Health,RiftEncounter> _encounterByEnemy=new Dictionary<Health,RiftEncounter>();
        float _nextExplorationTick;
        public string LocalCheckpoint {get;private set;}="Refugio inicial";
        public bool IsSafe(Vector3 position) => ExplorationMode && Checkpoints!=null && Checkpoints.Any(c=>c&&c.Contains(position));
        public bool BlockExplorationDamage(Health victim,Health attacker) {
            // Falling remains lethal, including a fall from a protected checkpoint.
            if(!ExplorationMode||!victim||!attacker||attacker==victim)return false;
            return (victim.Team==PlayerTeam && (IsSafe(victim.transform.position)||_protectedUntil.ContainsKey(victim)))
                || (attacker.Team==PlayerTeam && IsSafe(attacker.transform.position));
        }
        Transform ExplorationSpawn(PlayerInstance player,Transform fallback) {
            if(_checkpoints.TryGetValue(player,out var checkpoint)&&checkpoint)return checkpoint.Respawn;
            var start=Checkpoints?.FirstOrDefault(c=>c&&c.StartingCamp);
            if(start)_checkpoints[player]=start;
            // Keep the existing initial spawn locations; only revisits use a crystal.
            return fallback;
        }
        void TickExploration() {
            TickSpawnProtection();
            if(Time.time<_nextExplorationTick)return;_nextExplorationTick=Time.time+.4f;
            _plan=new HordeWavePlan{HealthMultiplier=1,MaxAlive=30,BotFireTime=.65f,BotWaitForFireTime=1.3f};
            foreach(var player in GameManager.Players.ToArray()) {
                if(!player||player.BOT||!player.MyCharacter||player.MyCharacter.Health.CurrentHealth<=0)continue;
                var character=player.MyCharacter;
                if(Checkpoints==null)continue;
                foreach(var checkpoint in Checkpoints) {
                    if(!checkpoint||Vector3.Distance(character.transform.position,checkpoint.transform.position)>9)continue;
                    if(!_checkpoints.TryGetValue(player,out var old)||old!=checkpoint){
                        _checkpoints[player]=checkpoint;
                        TargetExplorationNotice(player.connectionToClient,0,"Checkpoint: "+checkpoint.Title,checkpoint.Title);
                        character.Health.ServerHeal(character.Health.MaxHealth,character.Health.DNID);
                    }
                }
            }
            if(Encounters==null)return;
            // Retire remote patrols without rewarding them so unvisited islands never
            // become blocked by the active enemy budget. Cleared pockets stay cleared.
            foreach(var entry in _encounterByEnemy.ToArray()){
                if(!entry.Key)continue;
                bool nearby=GameManager.Players.Any(p=>p&&!p.BOT&&p.MyCharacter&&Vector3.Distance(p.MyCharacter.transform.position,entry.Value.transform.position)<300);
                if(nearby)continue;
                entry.Value.Spawned--;_encounterByEnemy.Remove(entry.Key);_aliveEnemies.Remove(entry.Key);_botByHealth.Remove(entry.Key);
                NetworkServer.Destroy(entry.Key.gameObject);
            }
            foreach(var encounter in Encounters) {
                if(!encounter||encounter.Cleared||encounter.Spawned>=encounter.Count)continue;
                bool near=GameManager.Players.Any(p=>p&&!p.BOT&&p.MyCharacter&&p.MyCharacter.Health.CurrentHealth>0&&Vector3.Distance(p.MyCharacter.transform.position,encounter.transform.position)<encounter.ActivationRadius);
                if(!near||_aliveEnemies.Count>=30)continue;
                var point=encounter.SpawnPoints[encounter.Spawned];
                if(!point||IsSafe(point.position))continue;
                var before=new HashSet<Health>(_aliveEnemies.Keys);
                var kind=encounter.MiniBoss&&encounter.Spawned==encounter.Count-1?EnemyKind.Boss:encounter.Spawned%3==0?EnemyKind.Normal:EnemyKind.Melee;
                if(SpawnEnemy(kind,point)){
                    var health=_aliveEnemies.Keys.First(h=>!before.Contains(h));
                    _encounterByEnemy[health]=encounter;
                    var territory=health.gameObject.AddComponent<RiftEnemyTerritory>();territory.Home=encounter.transform.position;
                    if(kind==EnemyKind.Boss)health.CharacterName="GUARDIÁN · "+encounter.Title;
                    encounter.Spawned++;
                }
            }
            PushSnapshotIfChanged();
        }
        void ExplorationEnemyKilled(Health victim,Health killer,EnemyKind kind) {
            if(!_encounterByEnemy.TryGetValue(victim,out var encounter))return;
            _encounterByEnemy.Remove(victim);
            if(!killer || killer==victim || killer.Team!=PlayerTeam){encounter.Spawned--;return;}
            encounter.Defeated++;
            var player=GameManager.Players.FirstOrDefault(p=>p&&!p.BOT&&p.MyCharacter&&p.MyCharacter.Health==killer);
            if(player)TargetExplorationNotice(player.connectionToClient,kind==EnemyKind.Boss?150:20,kind==EnemyKind.Boss?"Guardián derrotado · +150 oro local":"Enemigo derrotado · +20 oro local",null);
            if(encounter.Cleared)foreach(var human in GameManager.Players)
                if(human&&!human.BOT&&human.MyCharacter&&Vector3.Distance(human.MyCharacter.transform.position,encounter.transform.position)<180)
                    TargetExplorationNotice(human.connectionToClient,75,encounter.Title+" despejado · +75 oro local",null);
        }
        [TargetRpc] void TargetExplorationNotice(NetworkConnectionToClient connection,int gold,string message,string checkpoint) {
            if(!string.IsNullOrEmpty(checkpoint))LocalCheckpoint=checkpoint;
            RiftExplorationProgress.Receive(gold,message);
        }
    }
}
