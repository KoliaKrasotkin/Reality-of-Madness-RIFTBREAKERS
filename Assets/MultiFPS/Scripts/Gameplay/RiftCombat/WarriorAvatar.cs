using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MultiFPS.Gameplay.RiftCombat
{
    // CharacterInstance remains the spawn/health/input adapter. Action poses run
    // through the Animator before the weapon grip rig evaluates.
    [DefaultExecutionOrder(150)]
    public sealed partial class WarriorAvatar : NetworkBehaviour
    {
        public Animator Animator;
        public GameObject EquippedWeapon;
        public WeaponDefinition Weapon;
        [SerializeField] private bool useWarriorEquipment = true;
        public float WalkSpeed=2.8f, SprintSpeed=5.7f, Acceleration=22, Braking=30, Gravity=23, JumpHeight=1.2f;
        public Vector3 Velocity {get;private set;}
        public bool Grounded {get;private set;}
        [SyncVar(hook=nameof(OnWeaponDrawnChanged))]
        [SerializeField] private bool weaponDrawn=true;
        [SyncVar] private double weaponReadyAt;
        public bool WeaponDrawn => weaponDrawn;
        CharacterSkinController Skin => GetComponent<CharacterSkinController>();
        public bool HasActiveSkin => Skin && Skin.IsActive;
        public bool CanUseWeapon => (!HasActiveSkin && GetComponent<PaladinPrototypePresentation>())||weaponDrawn&&!IsRolling&&NetworkTime.time>=weaponReadyAt;
        CharacterInstance actor;
        CharacterController capsule;
        Vector3 planar,previous;
        float vertical, jumpRequested=-100, lastGround=-100, presentationUntil, presentationStarted;
        int activeAttackSlot=-1;
        
        bool falling;float fallPeak;
        void Awake(){if(Animator&&!Animator.GetComponent<RiftFootstepEvents>())Animator.gameObject.AddComponent<RiftFootstepEvents>();actor=GetComponent<CharacterInstance>();capsule=GetComponent<CharacterController>();previous=transform.position;ApplyWeaponDrawn();}
        public override void OnStartClient(){base.OnStartClient();ApplyWeaponDrawn();}

        /// <summary>Call on the server or on this character's owning client. Also works offline.</summary>
        public void SetWeaponDrawn(bool drawn)
        {
            if(!NetworkServer.active&&!NetworkClient.active){if(IsRolling){queuedWeaponState=drawn?1:0;return;}PrepareWeaponChange(drawn);weaponDrawn=drawn;ApplyWeaponDrawn();}
            else if(isServer)SetWeaponDrawnOnServer(drawn);
            else if(isOwned)CmdSetWeaponDrawn(drawn);
        }
        [Command] private void CmdSetWeaponDrawn(bool drawn)=>SetWeaponDrawnOnServer(drawn);
        [Server] private void SetWeaponDrawnOnServer(bool drawn)
        {
            var combatState=actor&&actor.Health?AbilityController.For(actor.Health):null;
            if(combatState&&combatState.Prototype)drawn=combatState.ServerWeaponCombatActive;
            if(IsRolling){queuedWeaponState=drawn?1:0;return;}
            if(weaponDrawn==drawn)return;
            PrepareWeaponChange(drawn);
            weaponDrawn=drawn;
            if(!drawn&&actor&&actor.Health){var combat=AbilityController.For(actor.Health);if(combat)combat.Cancel();}
            ApplyWeaponDrawn();
        }
        private void PrepareWeaponChange(bool drawn)
        {
            if(!drawn||weaponDrawn)return;
            float duration=0f;
            if(Animator&&Animator.runtimeAnimatorController)
                foreach(var clip in Animator.runtimeAnimatorController.animationClips)
                    if(clip.name=="2Hand-Sword-Unsheath-Back-Unarmed") {duration=clip.length;break;}
            weaponReadyAt=NetworkTime.time+duration+.24f;
        }
        private void OnWeaponDrawnChanged(bool previousValue,bool newValue)=>ApplyWeaponDrawn();
        private void ApplyWeaponDrawn()
        {
            if(GetComponent<PaladinPrototypePresentation>()&&!HasActiveSkin)return;
            if(Animator)Animator.SetBool("WeaponDrawn",weaponDrawn);
            if(!weaponDrawn){activeAttackSlot=-1;if(Animator)Animator.SetBool("DefensiveHold",false);}
        }
        void Start(){
            if(useWarriorEquipment&&!GetComponent<PaladinPrototypePresentation>()&&!GetComponent<RiftWarriorEquipment>())gameObject.AddComponent<RiftWarriorEquipment>();
            if(actor.PlayerRecoil)actor.PlayerRecoil.enabled=false;
            foreach(var hit in actor.CharacterParent.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(capsule,hit,true);
        }
        public void SetSkin(int skin){ if(Skin)Skin.SelectSkinId(skin); }
        public void ReadInput(){if(isOwned&&actor)RiftMMOController.Ensure(gameObject).ReadCharacter(actor);}
        public void RequestJump(){if(!IsRolling)jumpRequested=Time.time;}
        void FixedUpdate()
        {
            if(isServer){ServerFallDamage();var combatState=actor&&actor.Health?AbilityController.For(actor.Health):null;if(combatState&&combatState.Prototype)SetWeaponDrawnOnServer(combatState.ServerWeaponCombatActive);}
            if(!isOwned||!actor||actor.Health.CurrentHealth<=0||!capsule.enabled)return;
            float dt=Time.fixedDeltaTime;
            actor.PrepareCharacterToLerp();
            Grounded=capsule.isGrounded||Physics.SphereCast(transform.position+Vector3.up*.35f,.24f,Vector3.down,out _, .23f,GameManager.environmentLayer,QueryTriggerInteraction.Ignore);
            if(Grounded){lastGround=Time.time;if(vertical<0)vertical=-2;}
            bool sprint=actor.ReadActionKeyCode(ActionCodes.Sprint)&&actor.Input.Movement.y>=0;
            Vector2 input=actor.Block?Vector2.zero:actor.Input.Movement;
            var combat=AbilityController.For(actor.Health);
            Vector3 desired=Quaternion.Euler(0,actor.Input.LookY,0)*new Vector3(input.x,0,input.y)*(sprint?SprintSpeed:WalkSpeed)*(combat&&combat.Casting==5?.55f:1);
            planar=Vector3.MoveTowards(planar,desired,(input.sqrMagnitude>.01f?Acceleration:Braking)*dt);
            if(IsRolling){actor.Input.LookY=RollFacing;planar=RollStepVelocity(dt);}
            if(!IsRolling&&Time.time-jumpRequested<.12f&&Time.time-lastGround<.1f){vertical=Mathf.Sqrt(2*Gravity*JumpHeight);jumpRequested=lastGround=-100;Grounded=false;}
            vertical=Mathf.Max(-55,vertical-Gravity*dt);
            var flags=capsule.Move((planar+Vector3.up*vertical)*dt);
            if((flags&CollisionFlags.Above)!=0&&vertical>0)vertical=0;
            Velocity=new Vector3(capsule.velocity.x,0,capsule.velocity.z);
            actor.isGrounded=Grounded;actor.IsRunning=sprint;actor.IsCrouching=false;
            actor.SetCurrentPositionTargetToLerp(transform.position);
            // The existing deathzone remains authoritative; ordinary long falls remain damaging.
        }
        void LateUpdate()
        {
            if(GetComponent<PaladinPrototypePresentation>()&&!HasActiveSkin)return;
            UpdateRollPresentation();
            if(!Animator||!actor||!actor.Health)return;
            bool alive=actor.Health.CurrentHealth>0;
            Animator.SetBool("Dead",!alive);
            Vector3 measured=isOwned?Velocity:(transform.position-previous)/Mathf.Max(.001f,Time.deltaTime);previous=transform.position;
            if(measured.magnitude>15)measured=Vector3.zero;
            Vector3 local=Quaternion.Inverse(Quaternion.Euler(0,actor.Input.LookY,0))*measured;
            Animator.SetFloat("MoveX",local.x/SprintSpeed,.12f,Time.deltaTime);
            Animator.SetFloat("MoveY",local.z/SprintSpeed,.12f,Time.deltaTime);
            bool grounded=isOwned?Grounded:Physics.Raycast(transform.position+Vector3.up*.2f,Vector3.down,.4f,GameManager.environmentLayer);
            Animator.SetBool("Grounded",grounded);
            var equipmentOwner=AbilityController.For(actor.Health);if(EquippedWeapon)EquippedWeapon.SetActive(alive&&(!equipmentOwner||!equipmentOwner.SwordAndShield));
            Animator.SetBool("DefensiveHold",!IsRolling&&weaponDrawn&&alive&&((activeAttackSlot==6&&Time.time<presentationUntil)||(equipmentOwner&&equipmentOwner.DefensiveUntil>NetworkTime.time)));
            // The rig must evaluate after the action pose; no special may alter bones in LateUpdate.
            if(alive&&activeAttackSlot>=0&&equipmentOwner&&Time.time>presentationStarted+.15f&&Time.time<presentationUntil-.04f&&equipmentOwner.Casting!=activeAttackSlot)
            {
                Animator.CrossFadeInFixedTime("Locomotion",.09f,0,0);
                activeAttackSlot=-1;
            }
            if(!alive||Time.time>=presentationUntil)activeAttackSlot=-1;
        }
        void ServerFallDamage(){
            if(!actor||!actor.Health||actor.Health.CurrentHealth<=0){falling=false;return;}
            bool floor=Physics.Raycast(transform.position+Vector3.up*.2f,Vector3.down,.5f,GameManager.environmentLayer,QueryTriggerInteraction.Ignore);
            if(!floor){if(!falling){falling=true;fallPeak=transform.position.y;}fallPeak=Mathf.Max(fallPeak,transform.position.y);}
            else if(falling){float distance=fallPeak-transform.position.y;falling=false;if(distance>5.5f)actor.Health.Server_ChangeHealthStateRaw(Mathf.RoundToInt(distance*distance*.5f),0,AttackType.falldamage,actor.Health,0);}
        }
        public bool PresentAutoAttack(AbilityDefinition ability)
        {
            if(HasActiveSkin){bool played=PresentAttack(0,ability);if(played)activeAttackSlot=-1;return played;}
            var paladin=GetComponent<PaladinPrototypePresentation>();if(paladin)return paladin.Attack();
            if(!PresentAttack(0,ability))return false;
            // Auto swings have their own server timer, not an AbilityController.Casting slot.
            // Let the Animator finish Attack0 rather than cancelling it as an interrupted cast.
            activeAttackSlot=-1;
            return true;
        }
        public bool PresentAttack(int slot,AbilityDefinition ability)
        {
            if(HasActiveSkin)
            {
                if(!CanUseWeapon||!ability||!Skin.TryPlayAction(ability.Id,ability.CastDuration))return false;
                var motion=GetComponent<WarriorSpecialMotion>();if(motion)motion.Stop();
                activeAttackSlot=slot;presentationStarted=Time.time;presentationUntil=Time.time+ability.CastDuration;
                return true;
            }
            if(!Animator||!ability||!CanUseWeapon)return false;
            string state="Base Layer.Attack"+slot;
            if(slot<0||!ability.Animation||!Animator.HasState(0,Animator.StringToHash(state)))return false;
            var special=GetComponent<WarriorSpecialMotion>();
            if(special)special.Stop();
            var equipment=GetComponent<CharacterWeaponPresentation>();
            var clip=equipment?equipment.ResolveClip(ability.Animation):ability.Animation;
            Animator.SetFloat("AttackRate",clip.length/Mathf.Max(.1f,ability.CastDuration));
            if(slot==6)Animator.SetBool("DefensiveHold",true);
            Animator.CrossFadeInFixedTime(state,.09f,0,0);
            activeAttackSlot=slot;presentationStarted=Time.time;
            presentationUntil=Time.time+ability.CastDuration;return true;
        }
    }
}
