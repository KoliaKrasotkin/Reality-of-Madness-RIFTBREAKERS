using System;
using UnityEngine;

namespace ROM.Presentation {
 // A weapon grip is a local presentation correction, evaluated after Humanoid retargeting.
 // It never moves the wrist, weapon, body, or gameplay colliders.
 [DefaultExecutionOrder(220)]
 public sealed class AldrenWeaponGripPose : MonoBehaviour {
  [Serializable] public struct FingerJoint {
   public Transform Joint;
   public Quaternion LocalRotation;
  }
  [Tooltip("Finger joint rotations for this model: fitted sword grip on the right and relaxed fingers on the left. Edit in prefab; body animations remain unchanged.")]
  public FingerJoint[] Joints=Array.Empty<FingerJoint>();
  [Range(0,1),Tooltip("Zero leaves the source animation's finger pose; one uses the fitted weapon grip.")]
  public float GripWeight=1;
  void LateUpdate(){ApplyPose();}
  public void ApplyPose(){if(GripWeight<=0)return;foreach(var pose in Joints)if(pose.Joint)pose.Joint.localRotation=Quaternion.Slerp(pose.Joint.localRotation,pose.LocalRotation,GripWeight);}
 }
}
