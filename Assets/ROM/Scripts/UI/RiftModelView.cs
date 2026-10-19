using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ROM.UI.Sprint01
{
    // Off-world render stages have no gameplay scripts, network identities, physics or audio.
    // They render to a UI texture and never load a gameplay scene in the background.
    public sealed class RiftModelView : MonoBehaviour, IDragHandler, IScrollHandler
    {
        public const int PreviewLayer=30;
        GameObject world,subject;
        Camera view;
        RenderTexture texture;
        RawImage image;
        bool lobby;
        static int nextWorld;
        float yaw=0,zoom=1;
        Bounds bounds;
        public bool Ready=>view&&subject&&texture&&texture.IsCreated();
        public void SetDisplay(bool visible){if(image)image.enabled=visible;}
        public static RiftModelView Create(Transform parent,float x,float y,float w,float h,FrontEndCatalog catalog,bool isLobby)
        {
            var rect=RiftWidgets.Rect(parent,"ModelView",x,y,w,h);var raw=rect.gameObject.AddComponent<RawImage>();raw.color=Color.white;raw.raycastTarget=!isLobby;
            var instance=rect.gameObject.AddComponent<RiftModelView>();instance.image=raw;instance.lobby=isLobby;instance.Setup(catalog,(int)w,(int)h);return instance;
        }
        void Setup(FrontEndCatalog catalog,int width,int height)
        {
            world=new GameObject("ROM Isolated Preview");world.transform.position=new Vector3(nextWorld++*250,-5000,0);
            if(lobby&&catalog&&catalog.StagePrefab){var stage=Instantiate(catalog.StagePrefab,world.transform);stage.transform.localPosition=Vector3.zero;}
            var cameraObject=new GameObject("Preview Camera");cameraObject.transform.SetParent(world.transform,false);view=cameraObject.AddComponent<Camera>();
            view.cullingMask=1<<PreviewLayer;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.035f,.045f,.07f);view.nearClipPlane=.03f;view.farClipPlane=100;view.allowHDR=true;view.fieldOfView=38;
            var data=view.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.volumeLayerMask=(1<<PreviewLayer)|1;
            texture=new RenderTexture(Mathf.Clamp(width,320,1920),Mathf.Clamp(height,320,1080),24,RenderTextureFormat.ARGBHalf);texture.Create();view.targetTexture=texture;image.texture=texture;
            AddLight("Key",new Vector3(-3,5,-4),new Color(1,.92f,.83f),40,LightType.Spot);
            AddLight("Rim",new Vector3(3,4,3),new Color(.55f,.63f,1),28,LightType.Spot);
            AddLight("Warm fill",new Vector3(3,3,-4),new Color(.72f,.81f,1),15,LightType.Spot);
            AddLight("Studio daylight",new Vector3(-3,5,-4),new Color(.78f,.84f,1),.6f,LightType.Directional);
            SetLayer(world);
        }
        void AddLight(string name,Vector3 position,Color color,float intensity,LightType type)
        {
            var go=new GameObject(name);go.transform.SetParent(world.transform,false);go.transform.localPosition=position;go.transform.LookAt(world.transform.position+Vector3.up);
            var light=go.AddComponent<Light>();light.type=type;light.color=color;light.intensity=intensity;light.range=25;light.spotAngle=65;light.shadowBias=.015f;light.shadowNormalBias=.15f;light.cullingMask=1<<PreviewLayer;light.shadows=LightShadows.Soft;
        }
        public void Show(PreviewModel model)
        {
            if(subject)DestroyImmediate(subject);if(model==null||!model.Prefab){image.enabled=false;return;}
            image.enabled=true;subject=new GameObject(model.Name);subject.transform.SetParent(world.transform,false);Instantiate(model.Prefab,subject.transform);subject.transform.localPosition=Vector3.zero;subject.transform.localRotation=Quaternion.identity;
            bounds=WorldBounds(subject);float target=model.Category=="Personaje"?2.15f:2.1f;
            subject.transform.localScale*=target/Mathf.Max(.001f,model.Category=="Personaje"?bounds.size.y:Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z));
            bounds=WorldBounds(subject);subject.transform.position+=world.transform.position-bounds.center+Vector3.up*(model.Category=="Personaje"?bounds.extents.y:1.25f);
            yaw=model.Category=="Personaje"?180:130;zoom=1;SetLayer(subject);UpdateCamera();
        }
        public void ShowParty(FrontEndCatalog catalog,ISocialService social)
        {
            for(int i=world.transform.childCount-1;i>=0;i--){var t=world.transform.GetChild(i);if(t.name.StartsWith("Party Avatar"))DestroyImmediate(t.gameObject);}
            var characters=catalog.Characters;if(characters.Length==0)return;
            var hero=characters[Mathf.Clamp(social.Self.Skin,0,characters.Length-1)];Show(hero);
            int slot=0;
            foreach(var member in social.Party){
                if(member.ProfileId==social.Self.Id)continue;var profile=social.Find(member.ProfileId);if(profile==null)continue;
                var model=characters[Mathf.Clamp(profile.Skin,0,characters.Length-1)];var go=new GameObject("Party Avatar "+slot);go.transform.SetParent(world.transform,false);Instantiate(model.Prefab,go.transform);
                var box=WorldBounds(go);go.transform.localScale*=2.05f/Mathf.Max(.001f,box.size.y);box=WorldBounds(go);
                Vector3 offset=new Vector3(slot==0?-2.3f:slot==1?2.3f:4.3f,box.extents.y,slot==2?1.8f:1);
                go.transform.position+=world.transform.position+offset-box.center;go.transform.localRotation=Quaternion.Euler(0,180,0);SetLayer(go);slot++;
            }
        }
        void UpdateCamera()
        {
            if(!view)return;
            if(lobby){view.transform.localPosition=new Vector3(0,1.5f,-7.4f);view.transform.LookAt(world.transform.position+new Vector3(.1f,1.2f,0));view.fieldOfView=34;}
            else{view.transform.localPosition=new Vector3(0,1.4f,-4.8f*zoom);view.transform.LookAt(world.transform.position+Vector3.up*1.2f);view.fieldOfView=36;}
            if(subject)subject.transform.localRotation=Quaternion.Euler(0,yaw,0);
        }
        public void OnDrag(PointerEventData e){if(lobby)return;yaw-=e.delta.x*.4f;UpdateCamera();}
        public void OnScroll(PointerEventData e){if(lobby)return;zoom=Mathf.Clamp(zoom-e.scrollDelta.y*.08f,.65f,1.5f);UpdateCamera();}
        public void Turn(float degrees){yaw+=degrees;UpdateCamera();}
        static Bounds WorldBounds(GameObject go)
        {
            var renderers=go.GetComponentsInChildren<Renderer>(true);var b=new Bounds(go.transform.position,Vector3.zero);bool first=true;
            foreach(var r in renderers){if(!r.enabled)continue;if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}return b;
        }
        static void SetLayer(GameObject go){foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=PreviewLayer;}
        void OnDestroy(){if(view)view.targetTexture=null;if(texture){texture.Release();Destroy(texture);}if(world)Destroy(world);}
    }
}
