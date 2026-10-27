using UnityEngine;
using UnityEngine.UI;
using MultiFPS.Gameplay;
namespace ROM.UI.Gameplay {
 public sealed class RiftHUDMinimap:MonoBehaviour {
  public RawImage Map;public RectTransform Arrow;public RiftHUDTheme Theme;
  RiftMinimapAtlas atlas;bool loaded;
  // Kept for existing diagnostics. Gameplay no longer creates a render texture/camera.
  public RenderTexture Texture=>null;
  public void Refresh(CharacterInstance actor,bool force=false){
   if(!actor||!Map||!Theme)return;
   if(Arrow)Arrow.localRotation=Quaternion.Euler(0,0,-actor.Input.LookY);
   if(!loaded){loaded=true;atlas=ROM.Tutorial.Crilium.CriliumSession.Active&&ROM.Tutorial.Crilium.CriliumDefinition.Load?ROM.Tutorial.Crilium.CriliumDefinition.Load.Atlas:Resources.Load<RiftMinimapAtlas>("RiftMinimapAtlas");if(atlas)Map.texture=atlas.Map;}
   if(atlas&&atlas.Map)Map.uvRect=atlas.View(actor.transform.position,Theme.MinimapRange);
  }
 }
}
