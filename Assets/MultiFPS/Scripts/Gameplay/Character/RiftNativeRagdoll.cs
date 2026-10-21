using System.Collections.Generic;
using UnityEngine;
namespace MultiFPS.Gameplay {
// Keep the complete native skeleton/bindposes. Only the existing physical proxy is networked.
[DefaultExecutionOrder(12000)]
public sealed class RiftNativeRagdoll:MonoBehaviour {
 struct Binding {public Transform Native,Proxy;public Quaternion Offset;}
 readonly List<Binding> bindings=new List<Binding>();Transform nativeHips,proxyHips,nativeHead;Vector3 hipsOffset;public Animator Visual {get;private set;}
 public int BindingCount=>bindings.Count;
 public bool Initialize(Animator source,Animator native,RagDoll ragdoll){
  using var measurement=new RiftFrameDiagnostics.Sample(8);
  if(!source||!native||!source.isHuman||!native.isHuman||!native.avatar||!native.avatar.isValid)return false;
  var proxyByName=new Dictionary<string,Transform>();foreach(var t in ragdoll.GetComponentsInChildren<Transform>(true))if(!proxyByName.ContainsKey(t.name))proxyByName[t.name]=t;
  var sourceHips=source.GetBoneTransform(HumanBodyBones.Hips);if(!sourceHips||!proxyByName.TryGetValue(sourceHips.name,out proxyHips))return false;
  // Begin physics at the actual live template pose rather than the prefab T-pose.
  foreach(var live in source.GetComponentsInChildren<Transform>(true))if(proxyByName.TryGetValue(live.name,out var proxy)&&proxy!=ragdoll.transform)proxy.SetPositionAndRotation(live.position,live.rotation);
  var model=Instantiate(native.gameObject,ragdoll.transform);model.name="Native corpse visual";model.transform.SetPositionAndRotation(native.transform.position,native.transform.rotation);model.transform.localScale=Vector3.Scale(native.transform.lossyScale,new Vector3(1/Mathf.Max(.0001f,ragdoll.transform.lossyScale.x),1/Mathf.Max(.0001f,ragdoll.transform.lossyScale.y),1/Mathf.Max(.0001f,ragdoll.transform.lossyScale.z)));
  // Cloned gameplay/animation components must never drive a corpse or retain live Character references.
  foreach(var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true)){behaviour.enabled=false;Destroy(behaviour);}
  foreach(var collider in model.GetComponentsInChildren<Collider>(true))collider.enabled=false;
  foreach(var body in model.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.useGravity=false;}
  Visual=model.GetComponent<Animator>();Visual.enabled=false;model.SetActive(true);foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))renderer.enabled=true;
  GameTools.SetLayerRecursively(model,(int)GameLayers.ragdoll);
  nativeHips=Visual.GetBoneTransform(HumanBodyBones.Hips);nativeHead=Visual.GetBoneTransform(HumanBodyBones.Head);hipsOffset=Quaternion.Inverse(proxyHips.rotation)*(nativeHips.position-proxyHips.position);
  for(int i=0;i<(int)HumanBodyBones.LastBone;i++){var role=(HumanBodyBones)i;var live=source.GetBoneTransform(role);var target=Visual.GetBoneTransform(role);if(live&&target&&proxyByName.TryGetValue(live.name,out var proxy))bindings.Add(new Binding{Native=target,Proxy=proxy,Offset=Quaternion.Inverse(live.rotation)*target.rotation});}
  Follow();return bindings.Count>=10;
 }
 void LateUpdate()=>Follow();
 void Follow(){if(!nativeHips||!proxyHips)return;nativeHips.position=proxyHips.position+proxyHips.rotation*hipsOffset;foreach(var b in bindings)if(b.Native&&b.Proxy)b.Native.rotation=b.Proxy.rotation*b.Offset;}
 public void HideHead(){if(nativeHead)nativeHead.localScale=Vector3.zero;}
}}
