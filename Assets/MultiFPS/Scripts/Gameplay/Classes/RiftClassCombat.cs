using System.Linq;
using Mirror;
using UnityEngine;
namespace MultiFPS.Gameplay {
    // All health, cooldown, targeting and damage decisions run on the server.
    public static class RiftClassCombat {
        public static PlayerInstance Owner(Health health)=>!health?null:GameManager.Players.FirstOrDefault(p=>p&&!p.BOT&&p.MyCharacter&&p.MyCharacter.Health==health);
        public static bool Active=>GameManager.Gamemode&&GameManager.Gamemode.ClassRules;
        public static bool SupportNear(PlayerInstance player)=>Active&&player&&player.MyCharacter&&GameManager.Players.Any(p=>p&&!p.BOT&&p!=player&&p.ClassId==0&&p.MyCharacter&&p.MyCharacter.Health.CurrentHealth>0&&Vector3.Distance(p.MyCharacter.transform.position,player.MyCharacter.transform.position)<GameManager.Gamemode.ClassRules.AllyRadius);
        public static bool CanTarget(Health target,Vector3 from){
            if(!target||target.CurrentHealth<=0)return false;
            var p=Owner(target);return !p||p.ClassId!=3||p.HiddenUntil<=NetworkTime.time||Vector3.Distance(target.transform.position,from)<5;
        }
        public static float TargetPriority(Health target){var p=Owner(target);return p&&p.ClassId==1&&p.EmpoweredUntil>NetworkTime.time?.22f:1;}
        public static int Resolve(Health victim,Health attacker,int damage){
            if(!Active||!attacker||!victim||attacker==victim||attacker.Team==victim.Team||damage<=0)return damage;
            float amount=damage*(victim.CombatTransfer?1:attacker.CombatDamageMultiplier);
            var dealer=victim.CombatTransfer||RiftCombat.AbilityController.For(attacker)?.Prototype==true?null:Owner(attacker);var receiver=RiftCombat.AbilityController.For(victim)?.Prototype==true?null:Owner(victim);double now=NetworkTime.time;
            if(dealer){
                var def=GameManager.Gamemode.ClassRules.Find(dealer.ClassId);if(def)amount*=def.WeaponDamage;
                if(dealer.ClassId==2&&dealer.EmpoweredUntil>now)amount*=1.35f;
                if(dealer.ClassId==3&&dealer.HiddenUntil>now){amount*=1.6f;dealer.HiddenUntil=0;}
                if(victim.VulnerableUntil>now)amount*=1.2f;
                if(victim.WeakenedUntil>now&&dealer.ClassId==0)amount*=1.1f;
                dealer.LastCombatAt=now;
            }
            if(receiver){
                receiver.LastCombatAt=now;var def=GameManager.Gamemode.ClassRules.Find(receiver.ClassId);if(def)amount*=1-def.Armor;
                if(receiver.ClassId==1&&receiver.EmpoweredUntil>now)amount*=.5f;
                if(receiver.ClassId==2&&receiver.EmpoweredUntil>now)amount*=.8f;
                if(!victim.CombatTransfer&&attacker.WeakenedUntil>now)amount*=.8f;
                // With a nearby paladin a guarding Bastion can take part of an ally's hit.
                // Without support he keeps his own sustain; enemy damage never rises by role.
                if(receiver.ClassId!=1){
                    var tank=GameManager.Players.FirstOrDefault(p=>p&&!p.BOT&&p.ClassId==1&&p.EmpoweredUntil>now&&p.MyCharacter&&p.MyCharacter.Health.CurrentHealth>0&&Vector3.Distance(p.MyCharacter.transform.position,victim.transform.position)<18&&SupportNear(p));
                    if(tank){int redirected=Mathf.FloorToInt(amount*.25f);amount-=redirected;var shield=tank.MyCharacter.Health;shield.CombatTransfer=true;try{shield.Server_ChangeHealthStateRaw(redirected,0,AttackType.hitscan,attacker,0);}finally{shield.CombatTransfer=false;}}
                }
            }
            return Mathf.Max(1,Mathf.RoundToInt(amount));
        }
        public static int Heal(PlayerInstance healer,Health target,float fraction){
            if(!target||target.CurrentHealth<=0||target.CurrentHealth>=target.MaxHealth)return 0;
            var owner=Owner(target);float bonus=healer&&healer.ClassId==0&&owner&&owner.ClassId==1?1.25f:1;
            return target.ServerHeal(Mathf.RoundToInt(target.MaxHealth*fraction*bonus),healer.MyCharacter.Health.DNID);
        }
    }
}
