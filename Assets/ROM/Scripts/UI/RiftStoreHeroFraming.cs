using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
namespace ROM.UI.Sprint01 {
// Store-only framing of live visual bounds; never changes the shared preview or character asset.
[DefaultExecutionOrder(1800)]public sealed class RiftStoreHeroFraming:MonoBehaviour,IScrollHandler {
 RiftModelView view;Transform subject;Renderer[] renderers;float zoom=.8f;
 public void ResetZoom(float value){zoom=value;}
 public void OnScroll(PointerEventData e){zoom=Mathf.Clamp(zoom-e.scrollDelta.y*.08f,.65f,1.5f);}
 void LateUpdate(){if(!view)view=GetComponent<RiftModelView>();if(!view||!view.PreviewSubject||!view.PreviewCamera)return;if(subject!=view.PreviewSubject){subject=view.PreviewSubject;renderers=subject.GetComponentsInChildren<Renderer>().Where(r=>(r is MeshRenderer||r is SkinnedMeshRenderer)&&!r.GetComponent<TextMesh>()).ToArray();}if(renderers.Length==0)return;var bounds=renderers[0].bounds;foreach(var r in renderers)if(r&&r.enabled&&r.gameObject.activeInHierarchy)bounds.Encapsulate(r.bounds);var camera=view.PreviewCamera;float tangent=Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad);float fit=Mathf.Max(bounds.extents.y/tangent,bounds.extents.x/(tangent*camera.aspect));float distance=(fit*1.16f+bounds.extents.z)*zoom/.8f;camera.transform.position=bounds.center+Vector3.back*distance;camera.transform.LookAt(bounds.center,Vector3.up);}
}
}
