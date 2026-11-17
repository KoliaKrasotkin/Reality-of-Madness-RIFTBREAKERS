using UnityEngine;
using UnityEngine.UI;
using MultiFPS.Gameplay;
namespace ROM.UI.Gameplay {
 public sealed class RiftHUDMinimap:MonoBehaviour {
  public RawImage Map;public RectTransform Arrow;public RiftHUDTheme Theme;
  RiftMinimapAtlas atlas;bool loaded;UnityEngine.UI.Text floorLabel;
  // Kept for existing diagnostics. Gameplay no longer creates a render texture/camera.
  public RenderTexture Texture=>null;
  public void Refresh(CharacterInstance actor,bool force=false){
   if(!actor||!Map||!Theme)return;
   if(Arrow)Arrow.localRotation=Quaternion.Euler(0,0,-actor.Input.LookY);
   if(!loaded){loaded=true;atlas=ROM.Tutorial.Crilium.CriliumSession.Active&&ROM.Tutorial.Crilium.CriliumDefinition.Load?ROM.Tutorial.Crilium.CriliumDefinition.Load.Atlas:Resources.Load<RiftMinimapAtlas>("RiftMinimapAtlas");if(atlas)Map.texture=atlas.Map;}
   bool exterior=RiftCiudadDelSolExteriorMap.IsLocalContext(actor);
   var local=actor.transform.position;
   bool city=!exterior&&RiftCiudadDelSolCartography.Sample(actor.transform.position,out local);
   var selected=exterior?RiftCiudadDelSolExteriorMap.Load():city?RiftCiudadDelSolCartography.Atlas(RiftCiudadDelSolCartography.AutomaticFloor):atlas;
   if(selected&&selected.Map){Map.texture=selected.Map;Map.uvRect=selected.View(city?local:actor.transform.position,Theme.MinimapRange);}
   else if(exterior)Map.texture=null;
   if(city&&!floorLabel){var rect=new GameObject("Ciudad del Sol floor",typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(transform,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,-12);rect.sizeDelta=new Vector2(235,30);floorLabel=rect.gameObject.AddComponent<UnityEngine.UI.Text>();floorLabel.font=Theme.BodyFont;floorLabel.fontSize=12;floorLabel.alignment=TextAnchor.MiddleCenter;floorLabel.color=Theme.Text;floorLabel.raycastTarget=false;var shadow=rect.gameObject.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=Color.black;shadow.effectDistance=new Vector2(1,-1);}
   if(floorLabel){floorLabel.gameObject.SetActive(city);floorLabel.text=city?RiftCiudadDelSolCartography.CurrentName:"";}
  }
 }
}
