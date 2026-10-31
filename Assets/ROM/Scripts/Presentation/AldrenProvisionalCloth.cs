using System;
using System.Collections.Generic;
using UnityEngine;
namespace ROM.Presentation
{
    // Explicitly provisional visual only. No save IDs, character movement, attacks or Animator controller edits.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(220)]
    public sealed class AldrenProvisionalCloth:MonoBehaviour
    {
        public SkinnedMeshRenderer Target;
        public Mesh OriginalMesh;
        public AldrenProvisionalClothData Data;
        [Tooltip("Disable this component before Play to retain the original imported visual.")]public bool UseProvisionalVisual=true;
        [Tooltip("V7 with the seam-pinned Cloth experiment accepted provisionally. Spikes, wrinkles and garment intersections remain.")]public bool UseCloth=true;
        public bool ClothReady{get;private set;}
        public string LastFailure{get;private set;}
        public SkinnedMeshRenderer GarmentRenderer=>garment;
        SkinnedMeshRenderer garment;Cloth cloth;GameObject garmentObject;
        readonly List<CapsuleCollider> capsules=new List<CapsuleCollider>();
        readonly List<Transform> first=new List<Transform>(),second=new List<Transform>();
        readonly List<float> radii=new List<float>();
        SkinQuality originalQuality;bool originalOffscreen,initialized;
        void Awake(){if(Application.isPlaying)Apply();}
        void OnEnable(){if(Application.isPlaying)Apply();}
        void Apply()
        {
            if(initialized||!UseProvisionalVisual)return;
            if(!Target||!Data||!Data.FullMesh){LastFailure="Missing provisional visual references";Debug.LogWarning("Aldren provisional: "+LastFailure,this);return;}
            originalQuality=Target.quality;originalOffscreen=Target.updateWhenOffscreen;initialized=true;
            if(!OriginalMesh)OriginalMesh=Target.sharedMesh;
            Target.sharedMesh=Data.FullMesh;Target.quality=SkinQuality.Bone4;
            if(!UseCloth)return;
            try
            {
                if(!Data.BodyMesh||!Data.GarmentMesh||Data.BoneNames==null||Target.bones.Length!=Data.BoneNames.Length)throw new InvalidOperationException("Mesh/rig reference mismatch");
                for(int i=0;i<Target.bones.Length;i++)if(!Target.bones[i]||Target.bones[i].name!=Data.BoneNames[i])throw new InvalidOperationException("Unexpected bone order at "+i);
                garmentObject=new GameObject("Aldren V7 seam-pinned cloth - PROVISIONAL");garmentObject.layer=Target.gameObject.layer;var tr=garmentObject.transform;tr.SetParent(Target.transform.parent,false);tr.localPosition=Target.transform.localPosition;tr.localRotation=Target.transform.localRotation;tr.localScale=Target.transform.localScale;
                garment=garmentObject.AddComponent<SkinnedMeshRenderer>();garment.sharedMesh=Data.GarmentMesh;garment.bones=Target.bones;garment.rootBone=Target.rootBone;garment.sharedMaterials=Target.sharedMaterials;garment.quality=SkinQuality.Bone4;garment.updateWhenOffscreen=true;garment.shadowCastingMode=Target.shadowCastingMode;garment.receiveShadows=Target.receiveShadows;garment.localBounds=Target.localBounds;
                cloth=garmentObject.AddComponent<Cloth>();cloth.useGravity=false;cloth.damping=Data.Damping;cloth.stretchingStiffness=Data.StretchingStiffness;cloth.bendingStiffness=Data.BendingStiffness;cloth.clothSolverFrequency=Data.SolverFrequency;cloth.friction=Data.Friction;cloth.useTethers=true;cloth.enableContinuousCollision=true;cloth.worldVelocityScale=Data.WorldVelocityScale;cloth.worldAccelerationScale=Data.WorldAccelerationScale;
                var raw=cloth.vertices;var coeff=cloth.coefficients;
                if(Data.ParticleRestPositions==null||Data.ParticleMaxDistances==null||raw.Length!=Data.ParticleRestPositions.Length||coeff.Length!=raw.Length||raw.Length!=Data.ParticleMaxDistances.Length)throw new InvalidOperationException("Cloth particle topology differs from the baked data");
                float bestError=float.PositiveInfinity;
                for(int mode=0;mode<3;mode++){float error=0;for(int i=0;i<raw.Length;i++){var p=mode==0?raw[i]:mode==1?tr.InverseTransformPoint(raw[i]):tr.InverseTransformPoint(garment.rootBone.TransformPoint(raw[i]));error=Mathf.Max(error,Vector3.Distance(p,Data.ParticleRestPositions[i]));}bestError=Mathf.Min(bestError,error);}
                if(bestError>.001f)throw new InvalidOperationException("Cloth particle order/rest mapping differs: "+bestError+" m");
                for(int i=0;i<coeff.Length;i++){coeff[i].maxDistance=Data.ParticleMaxDistances[i];coeff[i].collisionSphereDistance=0;}cloth.coefficients=coeff;
                foreach(var side in new[]{"Left","Right"}){AddCapsule(side+"UpperLeg",side+"LowerLeg",Data.ThighRadius);AddCapsule(side+"LowerLeg",side+"Foot",Data.CalfRadius);}UpdateCapsules();cloth.capsuleColliders=capsules.ToArray();cloth.useGravity=true;
                Target.sharedMesh=Data.BodyMesh;Target.updateWhenOffscreen=true;ClothReady=true;
            }
            catch(Exception ex){LastFailure=ex.Message;ReleaseCloth();Target.sharedMesh=Data.FullMesh;Debug.LogWarning("Aldren provisional Cloth did not initialize; using V7 skin only. "+LastFailure,this);}
        }
        Transform Bone(string name){foreach(var bone in Target.bones)if(bone.name==name)return bone;throw new InvalidOperationException("Missing cloth collision bone "+name);}
        void AddCapsule(string a,string b,float radius)
        {
            var go=new GameObject("Aldren cloth-only trigger "+a);go.layer=2;go.transform.SetParent(transform,false);var c=go.AddComponent<CapsuleCollider>();c.direction=1;c.isTrigger=true;c.radius=radius;first.Add(Bone(a));second.Add(Bone(b));radii.Add(radius);capsules.Add(c);
        }
        void UpdateCapsules(){for(int i=0;i<capsules.Count;i++){var c=capsules[i];if(!c||!first[i]||!second[i])continue;var delta=second[i].position-first[i].position;c.transform.position=(first[i].position+second[i].position)*.5f;c.transform.rotation=Quaternion.FromToRotation(Vector3.up,delta);c.center=Vector3.zero;c.radius=radii[i];c.height=delta.magnitude/Mathf.Max(.0001f,Mathf.Abs(c.transform.lossyScale.y))+2*radii[i];}}
        void LateUpdate(){if(!initialized)return;UpdateCapsules();if(garment&&Target)garment.enabled=Target.enabled;}
        void ReleaseCloth(){ClothReady=false;if(garmentObject)Destroy(garmentObject);garmentObject=null;garment=null;cloth=null;foreach(var c in capsules)if(c)Destroy(c.gameObject);capsules.Clear();first.Clear();second.Clear();radii.Clear();}
        void OnDisable(){if(!Application.isPlaying||!initialized)return;ReleaseCloth();if(Target){Target.sharedMesh=OriginalMesh;Target.quality=originalQuality;Target.updateWhenOffscreen=originalOffscreen;}initialized=false;}
    }
}
