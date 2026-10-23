using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace ROM.UI.Sprint01 {
// Cache thumbnails of existing item models. Rendering runs briefly, not once per slot every frame.
public sealed class RiftItemArt:MonoBehaviour {
 static readonly Dictionary<GameObject,Texture2D> cache=new Dictionary<GameObject,Texture2D>();RiftModelView view;GameObject model;int frames;
 public static void Place(Transform parent,GameObject model,float x,float y,float size){if(!model)return;var rect=RiftWidgets.Rect(parent,"Existing item model",x,y,size,size);if(cache.TryGetValue(model,out var texture)&&texture){var image=rect.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;return;}var art=rect.gameObject.AddComponent<RiftItemArt>();art.model=model;art.view=RiftModelView.Create(rect,0,0,size,size,null,false);art.view.ShowItem(model);art.view.UseInspectionRendering();art.view.SetThumbnailResolution(192);art.view.GetComponent<RawImage>().raycastTarget=false;}
 void LateUpdate(){if(!view||++frames<4)return;var previous=RenderTexture.active;var rt=view.PreviewTexture;RenderTexture.active=rt;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();RenderTexture.active=previous;if(cache.TryGetValue(model,out var old)&&old)Destroy(texture);else cache[model]=texture;var image=gameObject.AddComponent<RawImage>();image.texture=cache[model];image.raycastTarget=false;Destroy(view.gameObject);view=null;enabled=false;}
}
}
