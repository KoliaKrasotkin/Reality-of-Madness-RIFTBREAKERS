using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
// Receives the native pack's presentation events on the Animator object.
// These presentation events do not drive movement, damage or networking.
// Surface audio can subscribe later.
public sealed class RiftFootstepEvents:MonoBehaviour {
 // Native sword clips emit Hit on the Animator object. This is presentation
 // contact only: AbilityController's server windup remains the damage authority.
 public event System.Action AttackContact;
 public int HitCount {get;private set;}
 public float LastHitAt {get;private set;}
 public void Hit(){HitCount++;LastHitAt=Time.time;AttackContact?.Invoke();}
 public void FootL(){}
 public void FootR(){}
 // The pack emits Land when its landing animation starts; grounding is handled by the controller.
 public void Land(){}
 public void WeaponSwitch(){}
}
}
