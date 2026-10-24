using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace ROM.UI.Sprint01 {
// Store-only cache. Never changes inventory thumbnail code, source prefabs or materials.
public sealed class RiftStoreThumbnail:MonoBehaviour {
 static readonly Dictionary<GameObject,Texture2D> cache=new Dictionary<GameObject,Texture2D>();RiftModelView view;GameObject source;int frames;
 public static void Place(Transform parent,PreviewModel model,float x,float y,float width,float height){var rect=RiftWidgets.Rect(parent,"Actual cosmetic thumbnail",x,y,width,height);if(cache.TryGetValue(model.Prefab,out var texture)&&texture){Image(rect,texture);return;}var owner=rect.gameObject.AddComponent<RiftStoreThumbnail>();owner.source=model.Prefab;owner.view=RiftModelView.Create(rect,0,0,width,height,null,false);owner.view.Show(model);owner.view.UseInspectionRendering();owner.view.SetInspectionZoom(.78f);var raw=owner.view.GetComponent<RawImage>();raw.raycastTarget=false;Fit(raw,owner.view.PreviewTexture);owner.view.gameObject.AddComponent<RiftStoreHeroFraming>().ResetZoom(.78f);}
 void LateUpdate(){if(!view||++frames<5)return;var rt=view.PreviewTexture;var previous=RenderTexture.active;Texture2D texture=null;try{RenderTexture.active=rt;texture=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();}finally{RenderTexture.active=previous;}if(cache.TryGetValue(source,out var existing)&&existing)Destroy(texture);else cache[source]=texture;Image(transform,cache[source]);Destroy(view.gameObject);view=null;enabled=false;}
 static void Fit(RawImage image,Texture texture){var r=image.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;var aspect=image.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=(float)texture.width/texture.height;}
 static void Image(Transform parent,Texture texture){var rect=RiftWidgets.Rect(parent,"Cosmetic thumbnail image",0,0,1,1);var image=rect.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;Fit(image,texture);}

}
}
