using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
 // Presentation adapter only: the existing native motor and server autoattack own movement/damage.
 [DefaultExecutionOrder(180)] public sealed class PaladinPrototypePresentation:MonoBehaviour {
  public Animator Animator;public WeaponDefinition Weapon;public Vector3 GripPosition,GripRotation;public Vector3 GripScale=Vector3.one;
  CharacterInstance actor;WarriorAvatar motor;Vector3 previous;GameObject sword;
  void Awake(){actor=GetComponent<CharacterInstance>();motor=GetComponent<WarriorAvatar>();previous=transform.position;}
  void Start(){if(!Animator||!Animator.isHuman||!Weapon||!Weapon.Model)return;var hand=Animator.GetBoneTransform(HumanBodyBones.RightHand);if(!hand)return;sword=Instantiate(Weapon.Model,hand);sword.name="Paladin prototype sword";sword.transform.localPosition=GripPosition;sword.transform.localEulerAngles=GripRotation;sword.transform.localScale=GripScale;foreach(var c in sword.GetComponentsInChildren<Collider>(true))c.enabled=false;}
  public bool Attack(){if(!Animator||!actor||actor.Health.CurrentHealth<=0)return false;Animator.SetTrigger("Attack");return true;}
  void LateUpdate(){if(!Animator||!actor||!actor.Health)return;bool alive=actor.Health.CurrentHealth>0;var velocity=motor&&motor.isOwned?motor.Velocity:(transform.position-previous)/Mathf.Max(.001f,Time.deltaTime);previous=transform.position;Animator.speed=alive?1:0;Animator.SetFloat("Speed",alive?Mathf.Min(8,velocity.magnitude):0,.15f,Time.deltaTime);if(sword)sword.SetActive(alive);}
 }
}
