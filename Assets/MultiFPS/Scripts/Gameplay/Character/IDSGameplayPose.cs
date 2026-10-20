using UnityEngine;
namespace MultiFPS.Gameplay
{
    // Render on the character's own humanoid avatar. The invisible kit rig remains
    // responsible for network state, hitboxes and existing weapon anchors.
    [DefaultExecutionOrder(11000)]
    public sealed class IDSGameplayPose : MonoBehaviour
    {
        public Animator Source;
        public Animator Target;
        public CharacterInstance Character;
        public bool UnarmedPreview;
        Item visualItem;
        bool initialized;
        float soleToAnkle;
        AnimatorControllerParameter[] parameters;
        RuntimeAnimatorController controller;
        CharacterAnimationSet animationSet;
        CharacterMotor motor;
        readonly System.Collections.Generic.Dictionary<int,bool> triggers = new System.Collections.Generic.Dictionary<int,bool>();
        readonly System.Collections.Generic.List<int> states = new System.Collections.Generic.List<int>();
        public void SetVisualTrigger(int hash, bool value) { triggers[hash] = value; }
        public void PlayVisualState(int hash) { states.Add(hash); }
        void Initialize()
        {
            if(initialized)return;
            var renderers=Target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            float bottom=float.PositiveInfinity;
            // Renderer.bounds includes the imported animation envelope; use the
            // actual bind mesh to measure the soles, not that inflated envelope.
            foreach(var r in renderers){
                var baked=new Mesh();r.BakeMesh(baked);
                foreach(var vertex in baked.vertices)bottom=Mathf.Min(bottom,r.transform.TransformPoint(vertex).y);
                DestroyImmediate(baked);
            }
            float ankle=Mathf.Min(Target.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,Target.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
            soleToAnkle=float.IsInfinity(bottom)?.1f:Mathf.Clamp(ankle-bottom,.02f,.3f);
            Target.applyRootMotion=false;Target.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animationSet=Target.GetComponent<CharacterAnimationSet>();
            motor=Character?Character.GetComponent<CharacterMotor>():null;
            initialized=true;
        }
        void Synchronize()
        {
            if(visualItem)visualItem.ResetPresentationPose();
            if(!Source||!Target||!Source.isHuman||!Target.isHuman||!Source.runtimeAnimatorController)return;
            Initialize();
            Target.transform.SetPositionAndRotation(Source.transform.position,Source.transform.rotation);
            if(controller!=Source.runtimeAnimatorController){
                controller=Source.runtimeAnimatorController;Target.runtimeAnimatorController=animationSet?animationSet.Resolve(controller):controller;
                parameters=Source.parameters;
                for(int layer=0;layer<Source.layerCount;layer++){
                    var state=Source.GetCurrentAnimatorStateInfo(layer);
                    if(state.fullPathHash!=0)Target.Play(state.fullPathHash,layer,state.normalizedTime);
                }
            }
            foreach(var p in parameters){
                switch(p.type){
                    case AnimatorControllerParameterType.Float:Target.SetFloat(p.nameHash,Source.GetFloat(p.nameHash));break;
                    case AnimatorControllerParameterType.Int:Target.SetInteger(p.nameHash,Source.GetInteger(p.nameHash));break;
                    case AnimatorControllerParameterType.Bool:Target.SetBool(p.nameHash,Source.GetBool(p.nameHash));break;
                    case AnimatorControllerParameterType.Trigger:break;
                }
            }
            foreach(var trigger in triggers){if(trigger.Value)Target.SetTrigger(trigger.Key);else Target.ResetTrigger(trigger.Key);}
            triggers.Clear();
            foreach(var state in states)Target.Play(state);
            states.Clear();
            for(int layer=1;layer<Source.layerCount;layer++)Target.SetLayerWeight(layer,Source.GetLayerWeight(layer));
            if(animationSet&&animationSet.HasOwnLocomotion&&motor){
                // Motus jog loops cover both 4.2 m/s movement and sprint. Match cadence
                // to displacement instead of reusing the old kit's six-metre stride.
                float stride=Character.IsCrouching?.6f:3.2f;
                Target.SetFloat("locomotionRate",Mathf.Clamp(motor.PlanarVelocity.magnitude/stride,.65f,2.4f),.1f,Time.deltaTime);
            }
            Target.speed=Source.speed;
        }
        void FinishPose()
        {
            if(!initialized||!Target)return;
            if(!Character||Character.isGrounded){
                float ankle=Mathf.Min(Target.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,Target.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
                Target.transform.position+=Vector3.up*(Source.transform.position.y+soleToAnkle-ankle);
            }
            if((!animationSet||!animationSet.HasOwnLocomotion)&&(UnarmedPreview||(Character&&Character.CharacterItemManager!=null&&!Character.CharacterItemManager.CurrentlyUsedItem))){
                SwingArm(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightFoot,-1);
                SwingArm(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot,1);
            }
        }
        void SwingArm(HumanBodyBones upperBone,HumanBodyBones lowerBone,HumanBodyBones handBone,HumanBodyBones oppositeFoot,float side)
        {
            var upper=Target.GetBoneTransform(upperBone);var lower=Target.GetBoneTransform(lowerBone);var hand=Target.GetBoneTransform(handBone);
            var hip=Target.GetBoneTransform(HumanBodyBones.Hips);var foot=Target.GetBoneTransform(oppositeFoot);
            float swing=Mathf.Clamp(Vector3.Dot(foot.position-hip.position,Target.transform.forward)*1.3f,-.6f,.6f);
            Vector3 direction=(-Target.transform.up+Target.transform.right*side*.1f+Target.transform.forward*swing).normalized;
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,direction)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,(direction+Target.transform.forward*.22f).normalized)*lower.rotation;
        }
        void AttachWeaponVisual()
        {
            var item=Character&&!Character.FPP&&Character.CharacterItemManager!=null?Character.CharacterItemManager.CurrentlyUsedItem:null;
            if(visualItem!=item){if(visualItem)visualItem.ResetPresentationPose();visualItem=item;}
            if(!item||!item.PresentationModel)return;
            var sourceHand=Source.GetBoneTransform(HumanBodyBones.RightHand);var nativeHand=Target.GetBoneTransform(HumanBodyBones.RightHand);
            var grip=Character.CharacterItemManager.ItemTarget;if(!grip||!sourceHand||!nativeHand)return;
            var model=item.PresentationModel;
            Quaternion originalRotation=grip.rotation*Quaternion.Euler(Character.CharacterItemManager.ItemRotationCorrector);
            Vector3 originalPosition=grip.position+originalRotation*Vector3.Scale(model.localPosition,item.transform.lossyScale);
            Quaternion alignment=nativeHand.rotation*Quaternion.Inverse(sourceHand.rotation);
            model.SetPositionAndRotation(nativeHand.position+alignment*(originalPosition-sourceHand.position),alignment*originalRotation*model.localRotation);
        }
        void OnDisable(){if(visualItem)visualItem.ResetPresentationPose();visualItem=null;}
        void Update(){Synchronize();}
        void LateUpdate(){FinishPose();AttachWeaponVisual();}
        public void ApplyNow(){Synchronize();if(!Application.isPlaying&&Target)Target.Update(.025f);FinishPose();}
    }
}
