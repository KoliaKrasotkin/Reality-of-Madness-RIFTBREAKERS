using MultiFPS;
using MultiFPS.UI;
using UnityEngine;
namespace MultiFPS.Gameplay {
    [DefaultExecutionOrder(200)]
    public sealed class RiftThirdPersonCamera : MonoBehaviour {
        public float Distance=3.6f;
        void LateUpdate(){
            var character=ClientFrontend.ObservedCharacterInstance;if(!character||!character.FPPCameraTarget)return;
            var camera=GetComponent<Camera>();camera.nearClipPlane=.08f;
            if(GameplayCamera._instance.FPPCamera)GameplayCamera._instance.FPPCamera.enabled=false;
            Quaternion rotation=character.FPPCameraTarget.rotation;
            Vector3 pivot=character.transform.position+Vector3.up*(character.IsCrouching?1.2f:1.7f);
            Vector3 offset=rotation*new Vector3(.6f,.25f,-Distance);
            float length=offset.magnitude;
            if(Physics.SphereCast(pivot,.22f,offset.normalized,out var hit,length,GameManager.environmentLayer,QueryTriggerInteraction.Ignore))length=Mathf.Max(.35f,hit.distance-.12f);
            var position=pivot+offset.normalized*length;
            transform.SetPositionAndRotation(position,rotation);
        }
    }
}
