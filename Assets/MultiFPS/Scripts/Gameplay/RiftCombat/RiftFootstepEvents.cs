using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
// Receives the native pack's presentation events on the Animator object.
// Footsteps do not drive movement, damage or networking. Surface audio can subscribe later.
public sealed class RiftFootstepEvents:MonoBehaviour {
 public void FootL(){}
 public void FootR(){}
}
}
