using System.Collections.Generic;
using System.Linq;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using UnityEngine;
namespace ROM.Horde {
    public partial class Horde {
        public RiftClassRules ClassConfiguration;
        public override RiftClassRules ClassRules=>ExplorationMode?ClassConfiguration:null;
        readonly Dictionary<Health,int> _classEnemyBase=new Dictionary<Health,int>();
        void RefreshEncounterScaling(){
            if(!ClassRules||Encounters==null)return;
            foreach(var encounter in Encounters){
                if(!encounter||encounter.Cleared)continue;
                foreach(var p in GameManager.Players)if(p&&!p.BOT&&p.MyCharacter&&p.MyCharacter.Health.CurrentHealth>0&&Vector3.Distance(p.MyCharacter.transform.position,encounter.transform.position)<ClassRules.ParticipantRadius)encounter.Participants.Add(p.netId);
            }
            foreach(var entry in _encounterByEnemy){if(entry.Key&&entry.Key.CurrentHealth>0)ScaleEncounterEnemy(entry.Key,entry.Value);}
        }
        void ScaleEncounterEnemy(Health health,RiftEncounter encounter){
            if(!ClassRules)return;
            if(!_classEnemyBase.ContainsKey(health))_classEnemyBase[health]=health.MaxHealth;
            int count=Mathf.Clamp(encounter.Participants.Count,1,4);
            int max=Mathf.RoundToInt(_classEnemyBase[health]*ClassRules.HealthScale(count));
            if(max!=health.MaxHealth){float ratio=(float)health.CurrentHealth/health.MaxHealth;health.ServerSetVitals(max,Mathf.Max(1,Mathf.RoundToInt(max*ratio)));}
            health.CombatDamageMultiplier=ClassRules.DamageScale(count);
        }
    }
}
