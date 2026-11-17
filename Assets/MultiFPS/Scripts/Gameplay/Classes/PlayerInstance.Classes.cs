using System.Linq;
using Mirror;
using UnityEngine;
namespace MultiFPS.Gameplay {
    public partial class PlayerInstance {
        public const string ClassPreference="ROM.Class";
        [SyncVar] public int ClassId;
        [SyncVar] public double FirstReadyAt,SecondReadyAt,EmpoweredUntil,HiddenUntil;
        public double LastCombatAt;
        float _nextRecovery;
        public RiftClassDefinition ClassDefinition=>GameManager.Gamemode&&GameManager.Gamemode.ClassRules?GameManager.Gamemode.ClassRules.Find(ClassId):null;
        [Server] public void ServerConfigureClass(){
            if(BOT||!MyCharacter||!ClassDefinition)return;
            int max=GetComponent<RiftCombat.AbilityController>()?.ProgressionHealth(ClassDefinition.Health)??ClassDefinition.Health;MyCharacter.Health.ServerSetVitals(max,max);
            EmpoweredUntil=HiddenUntil=0;LastCombatAt=NetworkTime.time;
        }
        [Command] public void CmdChooseClass(int id){if(ServerChooseClass(id))TargetClassChoice(connectionToClient,id);}
        [Server] public bool ServerChooseClass(int id){
            if(!BOT)return false; // Only the native Warrior is playable; legacy definitions remain for compatibility.
            var rules=GameManager.Gamemode?GameManager.Gamemode.ClassRules:null;
            if(BOT||!rules||!rules.Find(id)||id==ClassId)return false;
            if(MyCharacter&&(!GameManager.Gamemode.IsProtectedPosition(MyCharacter.transform.position)||MyCharacter.Health.CurrentHealth<=0))return false;
            ClassId=id;PlayerInfo.ClassId=id;ServerConfigureClass();return true;
        }
        [TargetRpc] void TargetClassChoice(NetworkConnectionToClient connection,int id){PlayerPrefs.SetInt(ClassPreference,id);PlayerPrefs.Save();}
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
        [Command] public void CmdUseClassAbility(byte slot){ServerUseClassAbility(slot);}
        [Server] public bool ServerUseClassAbility(byte slot){
            var combat=GetComponent<RiftCombat.AbilityController>();if(PlayerInfo.Warrior||combat&&combat.Prototype)return false;
            var def=ClassDefinition;double now=NetworkTime.time;
            if(BOT||!MyCharacter||!def||MyCharacter.Health.CurrentHealth<=0||slot>1||!GameManager.Gamemode.CanUseClassAbilities(MyCharacter.transform.position))return false;
            if((slot==0?FirstReadyAt:SecondReadyAt)>now)return false;
            var center=MyCharacter.transform.position;Health target=null;Vector3 effect=center;float radius=10;
            if((ClassId==0&&slot==1)||(ClassId==2&&slot==0)||(ClassId==3&&slot==0)){
                target=AimEnemy(ClassId==3?65:45);if(!target)return false;effect=target.transform.position;
            }
            if(slot==0)FirstReadyAt=now+def.FirstCooldown;else SecondReadyAt=now+def.SecondCooldown;
            LastCombatAt=now;
            switch((RiftClassId)ClassId){
                case RiftClassId.Paladin:
                    if(slot==0){HealAllies(center,16,.35f);HurtArea(center,10,def.FirstPower);radius=16;}
                    else{target.WeakenedUntil=now+8;Hurt(target,def.SecondPower);HealAllies(effect,12,.12f);radius=3;}
                    break;
                case RiftClassId.Bastion:
                    if(slot==0){EmpoweredUntil=now+6;if(!RiftClassCombat.SupportNear(this))RiftClassCombat.Heal(this,MyCharacter.Health,.25f);radius=18;}
                    else{foreach(var enemy in Enemies(center,10)){enemy.WeakenedUntil=now+4;Hurt(enemy,def.SecondPower);}radius=10;}
                    break;
                case RiftClassId.BattleMage:
                    if(slot==0){HurtArea(effect,7,def.FirstPower);radius=7;}
                    else{EmpoweredUntil=now+8;RiftClassCombat.Heal(this,MyCharacter.Health,.15f);radius=3;}
                    break;
                case RiftClassId.VeilHunter:
                    if(slot==0){target.VulnerableUntil=now+10;Hurt(target,def.FirstPower);radius=2;}
                    else{HiddenUntil=now+6;RiftClassCombat.Heal(this,MyCharacter.Health,.2f);radius=5;}
                    break;
            }
            RpcClassEffect(center+Vector3.up, effect+Vector3.up*.2f,radius,ClassId);return true;
        }
        Health AimEnemy(float range){
            var combat=GetComponent<RiftCombat.AbilityController>();var target=combat?combat.CombatTarget:null;
            if(!target||target.CurrentHealth<=0||target.Team==MyCharacter.Health.Team||!Visible(target))return null;
            var delta=target.transform.position-MyCharacter.transform.position;delta.y=0;
            var forward=Quaternion.Euler(0,MyCharacter.Input.LookY,0)*Vector3.forward;
            return delta.magnitude<=range&&Vector3.Angle(forward,delta)<=90?target:null;
        }
        Health[] Enemies(Vector3 center,float radius)=>Health.All.Where(h=>h&&h.Team!=MyCharacter.Health.Team&&h.CurrentHealth>0&&Vector3.Distance(h.transform.position,center)<radius&&Visible(h)).ToArray();
        bool Visible(Health target){Vector3 origin=MyCharacter.transform.position+Vector3.up*1.3f;Vector3 delta=target.transform.position+Vector3.up-origin;return !MyCharacter.gameObject.scene.GetPhysicsScene().Raycast(origin,delta.normalized,out _,delta.magnitude,GameManager.environmentLayer,QueryTriggerInteraction.Ignore);}
        void Hurt(Health target,float amount){if(target&&target.CurrentHealth>0)target.Server_ChangeHealthStateRaw(Mathf.RoundToInt(amount),0,AttackType.explosion,MyCharacter.Health,0);}
        void HurtArea(Vector3 center,float radius,float amount){foreach(var enemy in Enemies(center,radius))Hurt(enemy,amount);}
        void HealAllies(Vector3 center,float radius,float fraction){foreach(var p in GameManager.Players)if(p&&!p.BOT&&p.MyCharacter&&Vector3.Distance(p.MyCharacter.transform.position,center)<radius)RiftClassCombat.Heal(this,p.MyCharacter.Health,fraction);}
        [ClientRpc] void RpcClassEffect(Vector3 origin,Vector3 point,float radius,int id){RiftAbilityEffect.Show(origin,point,radius,id);}
    }
}
