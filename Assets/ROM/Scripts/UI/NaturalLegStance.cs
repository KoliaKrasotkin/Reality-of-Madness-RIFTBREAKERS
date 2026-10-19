using System;
using UnityEngine;
using MultiFPS;
using MultiFPS.Gameplay;

namespace ROM.UI.Sprint01
{
    // Constrain excessive abduction in the existing humanoid motion. Does not scale
    // bones or alter weights. Crouching/jumping and root motion stay with the kit.
    [DefaultExecutionOrder(10000)]
    public sealed class NaturalLegStance : MonoBehaviour
    {
        public Animator Animator;
        public CharacterInstance Character;
        [Range(.05f,.7f)] public float MaximumAbduction=.18f;
        public bool RelaxedPreviewIdle;
        HumanPoseHandler handler;
        HumanPose pose;
        int left=-1,right=-1;
        void LateUpdate()
        {
            if(!Animator||!Animator.isHuman||!Animator.avatar||!Animator.isActiveAndEnabled)return;
            if(Character&&(Character.FPP||Character.IsCrouching||!Character.isGrounded))return;
            if(handler==null){handler=new HumanPoseHandler(Animator.avatar,Animator.transform);left=Array.IndexOf(HumanTrait.MuscleName,"Left Upper Leg In-Out");right=Array.IndexOf(HumanTrait.MuscleName,"Right Upper Leg In-Out");}
            handler.GetHumanPose(ref pose);
            if(left>=0)pose.muscles[left]=Mathf.Clamp(pose.muscles[left],-MaximumAbduction,MaximumAbduction);
            if(right>=0)pose.muscles[right]=Mathf.Clamp(pose.muscles[right],-MaximumAbduction,MaximumAbduction);
            handler.SetHumanPose(ref pose);
            if(RelaxedPreviewIdle){RelaxArm(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,-1);RelaxArm(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,1);}
        }
        void RelaxArm(HumanBodyBones upperBone,HumanBodyBones lowerBone,HumanBodyBones handBone,float side)
        {
            var upper=Animator.GetBoneTransform(upperBone);var lower=Animator.GetBoneTransform(lowerBone);var hand=Animator.GetBoneTransform(handBone);if(!upper||!lower||!hand)return;
            var down=-Animator.transform.up;var outward=Animator.transform.right*side*.12f;var forward=Animator.transform.forward*.08f;
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,(down+outward+forward).normalized)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,(down+forward*.5f).normalized)*lower.rotation;
        }
        void OnDisable(){handler?.Dispose();handler=null;}
        public void ApplyNow(){LateUpdate();}
    }
}
