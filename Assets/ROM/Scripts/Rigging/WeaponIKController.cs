using UnityEngine;
using UnityEngine.Animations.Rigging;

// Inspector entry point. The constraint evaluates the grip in the animation stream.
[DisallowMultipleComponent]
public class WeaponIKController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public Animator Animator => animator != null ? animator : GetComponentInChildren<Animator>(true);
    public RigBuilder RigBuilder => Animator != null ? Animator.GetComponent<RigBuilder>() : null;
    public AnimatedWeaponGripConstraint GripConstraint => RigBuilder != null
        ? RigBuilder.GetComponentInChildren<AnimatedWeaponGripConstraint>(true)
        : null;
}
