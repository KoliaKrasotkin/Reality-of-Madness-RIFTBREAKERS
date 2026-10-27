using UnityEngine;
namespace ROM.Horde {
[ExecuteAlways,DisallowMultipleComponent]
public sealed class RiftCrystalRadiance:MonoBehaviour {
 [Tooltip("World-space halo renderer. It adds no collider or UI raycast.")]public Renderer Halo;
 [Min(0)]public float HaloGain=.65f;
 [Range(0,.25f)]public float PulseDepth=.04f;
 [Min(.01f)]public float PulseSpeed=.6f;
 MaterialPropertyBlock block;public float CurrentGain{get;private set;}
 void LateUpdate(){if(!Halo)return;var camera=MultiFPS.GameplayCamera._instance?MultiFPS.GameplayCamera._instance.GetComponent<Camera>():Camera.main;if(camera)Halo.transform.rotation=camera.transform.rotation;CurrentGain=HaloGain*(1+PulseDepth*Mathf.Sin((Application.isPlaying?Time.time:Time.realtimeSinceStartup)*PulseSpeed));if(block==null)block=new MaterialPropertyBlock();Halo.GetPropertyBlock(block);block.SetFloat("_Gain",CurrentGain);Halo.SetPropertyBlock(block);}
}
}
