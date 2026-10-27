using System.Collections.Generic;using UnityEngine;
namespace ROM.Tutorial.Crilium {
// Actual scene-space strands and particles. No part of the monument is drawn in the skybox.
[ExecuteAlways]public sealed class CriliumBloodBeacon:MonoBehaviour {
 public LineRenderer[] Streams;public LineRenderer[] Rings;public float Height=370,Radius=9,Speed=.7f;public ParticleSystem Sparks;
 readonly Dictionary<Camera,Vector2> oldCameras=new Dictionary<Camera,Vector2>();
 void Update(){Evaluate(Application.isPlaying?Time.time:Time.realtimeSinceStartup);if(Application.isPlaying)foreach(var cam in Camera.allCameras){if(!cam||!cam.GetComponent<MultiFPS.Gameplay.RiftThirdPersonCamera>())continue;if(!oldCameras.ContainsKey(cam))oldCameras[cam]=new Vector2(cam.farClipPlane,(float)cam.clearFlags);cam.farClipPlane=Mathf.Max(cam.farClipPlane,1500);cam.clearFlags=CameraClearFlags.Skybox;}}
 public void Evaluate(float seconds){if(Streams!=null)for(int s=0;s<Streams.Length;s++){var line=Streams[s];if(!line)continue;int count=line.positionCount;for(int i=0;i<count;i++){float f=(float)i/(count-1),angle=f*12-seconds*Speed+s*Mathf.PI*.5f;float r=(s==0?1.2f:Radius)*(0.25f+f*.75f);line.SetPosition(i,new Vector3(Mathf.Cos(angle)*r,Height*f,Mathf.Sin(angle)*r));}line.widthMultiplier=(s==0?4.2f:1.5f)*(1+.13f*Mathf.Sin(seconds*1.8f+s));}
  if(Rings!=null)for(int r=0;r<Rings.Length;r++){var ring=Rings[r];if(!ring)continue;for(int n=0;n<ring.positionCount;n++){float a=n*Mathf.PI*2/(ring.positionCount-1)+seconds*(r==0?.23f:-.3f);float radius=(r==0?8:13)*(1+.06f*Mathf.Sin(seconds*1.4f));ring.SetPosition(n,new Vector3(Mathf.Cos(a)*radius,r*10+Mathf.Sin(a*3+seconds)*1.1f,Mathf.Sin(a)*radius));}}
 }
 ROM.UI.Gameplay.RiftGameplayHUD hud;void LateUpdate(){if(!Application.isPlaying||!CriliumSession.Active)return;if(!hud)hud=Object.FindFirstObjectByType<ROM.UI.Gameplay.RiftGameplayHUD>();if(hud){hud.Zone.text="CRELIUM";hud.MapZone.text="Crelium";}}
 void OnDisable(){foreach(var pair in oldCameras)if(pair.Key){pair.Key.farClipPlane=pair.Value.x;pair.Key.clearFlags=(CameraClearFlags)(int)pair.Value.y;}oldCameras.Clear();}
}
}
