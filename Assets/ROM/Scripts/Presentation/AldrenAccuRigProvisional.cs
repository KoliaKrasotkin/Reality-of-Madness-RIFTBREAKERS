using System;
using System.Collections.Generic;
using UnityEngine;
using MultiFPS.Gameplay.RiftCombat;

namespace ROM.Presentation {
    // Provisional, reversible visual only: the source Animator and gameplay rig remain authoritative.
    [DisallowMultipleComponent, DefaultExecutionOrder(800)]
    public sealed class AldrenAccuRigProvisional : MonoBehaviour {
        [Tooltip("Turn off before Play to use the existing paladin visual and cloth.")] public bool UseAccuRig = true;
        public GameObject AccuRigModel;
        public Material BodyMaterial;
        public Material HandMaterial;
        public bool Ready { get; private set; }
        public string LastFailure { get; private set; }
        Animator source, target;
        Transform sourceHand, targetHand, prototypeSword;
        PaladinPrototypePresentation prototype;
        float nextPrototypeLookup;
        GameObject instance;
        HumanPoseHandler sourcePose, targetPose;
        HumanPose pose;
        AnimatorCullingMode previousCulling;
        bool cullingChanged;
        readonly List<SkinnedMeshRenderer> hidden = new List<SkinnedMeshRenderer>();
        readonly List<bool> states = new List<bool>();

        void Start() { if (UseAccuRig) Apply(); }
        void Apply() {
            try {
                source=GetComponent<Animator>();
                if(!source || !source.avatar || !source.avatar.isValid || !source.avatar.isHuman || !AccuRigModel || !BodyMaterial || !HandMaterial) throw new InvalidOperationException("Missing Humanoid or provisional references");
                instance=Instantiate(AccuRigModel,transform,false);instance.name="Aldren AccuRIG body without cape - PROVISIONAL";
                target=instance.GetComponent<Animator>();
                if(!target || !target.avatar || !target.avatar.isValid || !target.avatar.isHuman) throw new InvalidOperationException("AccuRIG Avatar is not a valid Humanoid");
                target.enabled=false;
                var newSkin=instance.GetComponentInChildren<SkinnedMeshRenderer>();if(!newSkin)throw new InvalidOperationException("AccuRIG skin missing");
                var materials=newSkin.sharedMaterials;
                for(int i=0;i<materials.Length;i++){var name=materials[i]?materials[i].name.ToLowerInvariant():"";materials[i]=name.Contains("hand")?HandMaterial:BodyMaterial;}
                newSkin.sharedMaterials=materials;newSkin.updateWhenOffscreen=true;instance.layer=gameObject.layer;foreach(var t in instance.GetComponentsInChildren<Transform>(true))t.gameObject.layer=gameObject.layer;
                previousCulling=source.cullingMode;source.cullingMode=AnimatorCullingMode.AlwaysAnimate;cullingChanged=true;
                sourcePose=new HumanPoseHandler(source.avatar,source.transform);targetPose=new HumanPoseHandler(target.avatar,target.transform);
                sourcePose.GetHumanPose(ref pose);targetPose.SetHumanPose(ref pose);
                sourceHand=source.GetBoneTransform(HumanBodyBones.RightHand);targetHand=target.GetBoneTransform(HumanBodyBones.RightHand);
                foreach(var skin in source.GetComponentsInChildren<SkinnedMeshRenderer>(true)){if(skin.transform.IsChildOf(instance.transform))continue;hidden.Add(skin);states.Add(skin.enabled);skin.enabled=false;}
                if(hidden.Count==0)throw new InvalidOperationException("Original skin not found");
                Ready=true;
            }catch(Exception e){LastFailure=e.Message;Restore();Debug.LogWarning("Aldren AccuRIG provisional unavailable: "+LastFailure,this);}
        }
        void LateUpdate(){if(!Ready)return;try{sourcePose.GetHumanPose(ref pose);targetPose.SetHumanPose(ref pose);AlignPrototypeSword();}catch(Exception e){LastFailure=e.Message;Restore();Debug.LogWarning("Aldren AccuRIG provisional disabled: "+LastFailure,this);}}
        void AlignPrototypeSword(){
            if(!sourceHand||!targetHand)return;
            if(!prototype&&Time.unscaledTime>=nextPrototypeLookup){nextPrototypeLookup=Time.unscaledTime+1f;foreach(var candidate in FindObjectsByType<PaladinPrototypePresentation>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(candidate.Animator==source){prototype=candidate;break;}}
            if(!prototype)return;
            if(!prototypeSword||prototypeSword.parent!=sourceHand)prototypeSword=sourceHand.Find("Paladin prototype sword");
            if(!prototypeSword)return;
            // PaladinPrototypePresentation creates this visual-only model with disabled colliders.
            // Keep the existing authored wrist orientation and offset, translating only by the
            // difference between the source and retargeted wrists after the Humanoid pose copy.
            prototypeSword.localPosition=prototype.GripPosition;
            prototypeSword.position+=targetHand.position-sourceHand.position;
        }
        void Restore(){Ready=false;if(prototypeSword&&prototype)prototypeSword.localPosition=prototype.GripPosition;prototypeSword=null;prototype=null;sourceHand=null;targetHand=null;for(int i=0;i<hidden.Count;i++)if(hidden[i])hidden[i].enabled=states[i];hidden.Clear();states.Clear();if(cullingChanged&&source)source.cullingMode=previousCulling;cullingChanged=false;if(sourcePose!=null)sourcePose.Dispose();if(targetPose!=null)targetPose.Dispose();sourcePose=null;targetPose=null;if(instance)Destroy(instance);instance=null;}
        void OnDisable(){if(Ready || instance)Restore();}
    }
}
