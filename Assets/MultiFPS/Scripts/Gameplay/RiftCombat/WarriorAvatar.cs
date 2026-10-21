using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MultiFPS.Gameplay.RiftCombat
{
    // Clean player presentation/motor. CharacterInstance is only the existing
    // spawn/health/input-message adapter; no template bones, IK or Animator drive this rig.
    [DefaultExecutionOrder(150)]
    public sealed class WarriorAvatar : NetworkBehaviour
    {
        public Animator Animator;
        public GameObject EquippedWeapon;
        public WeaponDefinition Weapon;
        public float WalkSpeed=2.8f, SprintSpeed=5.7f, Acceleration=22, Braking=30, Gravity=23, JumpHeight=1.2f;
        public Vector3 Velocity {get;private set;}
        public bool Grounded {get;private set;}
        CharacterInstance actor;
        CharacterController capsule;
        Vector3 planar,previous;
        float vertical, jumpRequested=-100, lastGround=-100, presentationUntil;
        
        bool falling;float fallPeak;
        void Awake(){actor=GetComponent<CharacterInstance>();capsule=GetComponent<CharacterController>();previous=transform.position;}
        void Start(){
            if(!GetComponent<RiftWarriorEquipment>())gameObject.AddComponent<RiftWarriorEquipment>();
            if(actor.PlayerRecoil)actor.PlayerRecoil.enabled=false;
            foreach(var hit in actor.CharacterParent.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(capsule,hit,true);
        }
        public void SetSkin(int skin){ /* The first Warrior body is native Lizardman; no legacy retargeting. */ }
        public void ReadInput(){if(isOwned&&actor)RiftMMOController.Ensure(gameObject).ReadCharacter(actor);}
        public void RequestJump(){jumpRequested=Time.time;}
        void FixedUpdate()
        {
            if(isServer)ServerFallDamage();
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
            if(Time.time-jumpRequested<.12f&&Time.time-lastGround<.1f){vertical=Mathf.Sqrt(2*Gravity*JumpHeight);jumpRequested=lastGround=-100;Grounded=false;}
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
        }
        void ServerFallDamage(){
            if(!actor||!actor.Health||actor.Health.CurrentHealth<=0){falling=false;return;}
            bool floor=Physics.Raycast(transform.position+Vector3.up*.2f,Vector3.down,.5f,GameManager.environmentLayer,QueryTriggerInteraction.Ignore);
            if(!floor){if(!falling){falling=true;fallPeak=transform.position.y;}fallPeak=Mathf.Max(fallPeak,transform.position.y);}
            else if(falling){float distance=fallPeak-transform.position.y;falling=false;if(distance>5.5f)actor.Health.Server_ChangeHealthStateRaw(Mathf.RoundToInt(distance*distance*.5f),0,AttackType.falldamage,actor.Health,0);}
        }
        public bool PresentAttack(int slot,AbilityDefinition ability)
        {
            if(!Animator||!ability||!ability.Animation)return false;
            Animator.SetFloat("AttackRate",ability.Animation.length/Mathf.Max(.1f,ability.CastDuration));
            Animator.CrossFadeInFixedTime("Attack"+(slot>=4?(slot==5?1:slot==6?3:0):slot),.09f,0,0);
            presentationUntil=Time.time+ability.CastDuration;return true;
        }
    }
}
