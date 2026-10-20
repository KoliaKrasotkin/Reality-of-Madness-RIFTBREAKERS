using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using MultiFPS;
using MultiFPS.Gameplay;
namespace ROM.UI.Gameplay
{
    public sealed class RiftHUDMinimap:MonoBehaviour
    {
        public RawImage Map;
        public RectTransform Arrow;
        public RiftHUDTheme Theme;
        Camera mapCamera;RenderTexture texture;float next;
        public RenderTexture Texture=>texture;
        public void Refresh(CharacterInstance actor,bool force=false)
        {
            if(!actor||!Map||!Theme)return;
            if(Arrow)Arrow.localRotation=Quaternion.Euler(0,0,-actor.Input.LookY);
            if(!force&&Time.unscaledTime<next)return;next=Time.unscaledTime+1/Mathf.Max(1,Theme.MinimapRefreshRate);
            if(!mapCamera){var go=new GameObject("Rift HUD - minimap camera");go.hideFlags=HideFlags.DontSave;mapCamera=go.AddComponent<Camera>();mapCamera.enabled=false;mapCamera.orthographic=true;mapCamera.orthographicSize=Theme.MinimapRange;mapCamera.clearFlags=CameraClearFlags.SolidColor;mapCamera.backgroundColor=new Color(.035f,.045f,.06f);mapCamera.cullingMask=GameManager.environmentLayer;mapCamera.nearClipPlane=.5f;mapCamera.farClipPlane=180;mapCamera.allowHDR=false;mapCamera.allowMSAA=false;var data=mapCamera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;texture=new RenderTexture(Theme.MinimapResolution,Theme.MinimapResolution,16,RenderTextureFormat.ARGB32){name="Rift minimap 256"};texture.Create();mapCamera.targetTexture=texture;Map.texture=texture;}
            mapCamera.transform.SetPositionAndRotation(actor.transform.position+Vector3.up*70,Quaternion.Euler(90,0,0));mapCamera.Render();
        }
        void OnDestroy(){if(mapCamera){mapCamera.targetTexture=null;Destroy(mapCamera.gameObject);}if(texture){texture.Release();Destroy(texture);}}
    }
}
