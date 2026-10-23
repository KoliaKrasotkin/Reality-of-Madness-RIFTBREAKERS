using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;
using UnityEngine.UI;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 string abilityTab="Skills";
 void BuildTalentBackdrop(RectTransform panel){
  var texture=Resources.Load<Texture2D>("RiftMenu/RiftBackground");if(!texture)return;
  var clip=RiftWidgets.Rect(panel,"Talent Rift background",0,0,panel.rect.width,panel.rect.height);clip.gameObject.AddComponent<RectMask2D>();clip.SetAsFirstSibling();
  var image=RiftWidgets.Rect(clip,"Approved Rift landscape",0,0,panel.rect.width,panel.rect.height).gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
  image.rectTransform.anchorMin=image.rectTransform.anchorMax=image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.anchoredPosition=Vector2.zero;
  var fit=image.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=(float)texture.width/texture.height;
  // Darken only the dedicated backdrop. Character, nodes and text render above it.
  RiftWidgets.Image(clip,"Backdrop readability shade",0,0,panel.rect.width,panel.rect.height,new Color(0,0,0,.62f));
 }

 void TalentTree(Transform body){var player=ClientFrontend.ClientPlayerInstance;var combat=player?player.GetComponent<AbilityController>():null;if(!player||!combat||!combat.Prototype){RiftWidgets.Label(body,"Los talentos están disponibles para Warrior en juego.",0,30,676,100,24);return;}body.gameObject.AddComponent<RiftTalentTreeView>().Build(player,combat,this);}
}
}
