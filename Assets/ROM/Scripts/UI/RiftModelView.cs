using System;
using MultiFPS.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ROM.UI.Sprint01
{
    // Marker isolates inspection alpha from gameplay camera post-processing policy.
    public sealed class RiftInspectionCamera : MonoBehaviour {}
    // Off-world render stages have no gameplay scripts, network identities, physics or audio.
    // They render to a UI texture and never load a gameplay scene in the background.
    public sealed class RiftModelView : MonoBehaviour, IDragHandler, IScrollHandler
    {
        public const int PreviewLayer=30;
        GameObject world,subject;
        Camera view;
        RenderTexture texture;
        RawImage image;
        bool lobby,mainMenuPresentation;GameObject stageEnvironment;
        static int nextWorld;
        float yaw=0,zoom=1;
        Bounds bounds;
        FrontEndCatalog catalog;
        Animator emoteAnimator;
        RuntimeAnimatorController idleController;
        AnimatorOverrideController emoteController;
        float emoteUntil,restoreAt;
        public string CurrentEmoteName {get;private set;}
        public Camera PreviewCamera=>view;public RenderTexture PreviewTexture=>texture;
        public Transform PreviewSubject=>subject?subject.transform:null;
        public CharacterInstance SourceCharacter {get;private set;}
        public void SetRenderActive(bool active){if(view)view.enabled=active;}
        public Animator PreviewAnimator=>subject?subject.GetComponentInChildren<Animator>():null;
        public bool Ready=>view&&subject&&texture&&texture.IsCreated();
        public void SetDisplay(bool visible){if(image)image.enabled=visible;}
        public static RiftModelView Create(Transform parent,float x,float y,float w,float h,FrontEndCatalog catalog,bool isLobby)
        {
            var rect=RiftWidgets.Rect(parent,"ModelView",x,y,w,h);var raw=rect.gameObject.AddComponent<RawImage>();raw.color=Color.white;raw.raycastTarget=!isLobby;
            var instance=rect.gameObject.AddComponent<RiftModelView>();instance.image=raw;instance.lobby=isLobby;instance.Setup(catalog,(int)w,(int)h);return instance;
        }
        void Setup(FrontEndCatalog catalog,int width,int height)
        {
            this.catalog=catalog;
            world=new GameObject("ROM Isolated Preview");world.transform.position=new Vector3(nextWorld++*250,-5000,0);
            if(lobby&&catalog&&catalog.StagePrefab){stageEnvironment=Instantiate(catalog.StagePrefab,world.transform);stageEnvironment.transform.localPosition=Vector3.zero;}
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
            ResetEmote();
            if(subject)DestroyImmediate(subject);if(model==null||!model.Prefab){image.enabled=false;return;}
            image.enabled=true;subject=new GameObject(model.Name);subject.transform.SetParent(world.transform,false);Instantiate(model.Prefab,subject.transform);subject.transform.localPosition=Vector3.zero;subject.transform.localRotation=Quaternion.identity;
            bounds=WorldBounds(subject);float target=model.Category=="Personaje"?2.15f:2.1f;
            subject.transform.localScale*=target/Mathf.Max(.001f,model.Category=="Personaje"?bounds.size.y:Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z));
            bounds=WorldBounds(subject);subject.transform.position+=world.transform.position-bounds.center+Vector3.up*(model.Category=="Personaje"?bounds.extents.y:1.25f);
            yaw=model.Category=="Personaje"?180:130;zoom=1;SetLayer(subject);UpdateCamera();
        }
        public void ShowLiveCharacter(CharacterInstance actor)
        {
            ResetEmote();if(subject)DestroyImmediate(subject);SourceCharacter=actor;
            if(!actor||!actor.CharacterParent){image.enabled=false;return;}
            subject=new GameObject("Live character inspection");subject.SetActive(false);subject.transform.SetParent(world.transform,false);
            var copy=Instantiate(actor.CharacterParent.gameObject,subject.transform,false);copy.transform.localPosition=Vector3.zero;copy.transform.localRotation=Quaternion.identity;
            foreach(var script in copy.GetComponentsInChildren<MonoBehaviour>(true))DestroyImmediate(script);
            foreach(var source in copy.GetComponentsInChildren<AudioSource>(true))DestroyImmediate(source);
            foreach(var collider in copy.GetComponentsInChildren<Collider>(true))DestroyImmediate(collider);
            foreach(var body in copy.GetComponentsInChildren<Rigidbody>(true))DestroyImmediate(body);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true))DestroyImmediate(camera);
            foreach(var light in copy.GetComponentsInChildren<Light>(true))DestroyImmediate(light);
            SetLayer(subject);subject.SetActive(true);
            foreach(var animator in copy.GetComponentsInChildren<Animator>(true)){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.SetFloat("MoveX",0);animator.SetFloat("MoveY",0);animator.SetBool("Grounded",true);animator.SetBool("Dead",false);animator.Update(0);}
            bounds=WorldBounds(subject);subject.transform.localScale*=2.15f/Mathf.Max(.001f,bounds.size.y);bounds=WorldBounds(subject);subject.transform.position+=world.transform.position-bounds.center+Vector3.up*bounds.extents.y;
            UseInspectionRendering();
            yaw=140;zoom=.8f;image.enabled=true;UpdateCamera();
        }
        public void SetInspectionZoom(float value){zoom=Mathf.Clamp(value,.65f,1.5f);UpdateCamera();}
        public void ShowItem(GameObject model){Show(new PreviewModel{Name=model.name,Category="Equipo",Prefab=model});subject.transform.localRotation=Quaternion.identity;var box=WorldBounds(subject);var size=box.size;Vector3 longest=size.x>=size.y&&size.x>=size.z?Vector3.right:size.y>=size.z?Vector3.up:Vector3.forward;Vector3 shortest=size.x<=size.y&&size.x<=size.z?Vector3.right:size.y<=size.z?Vector3.up:Vector3.forward;if(shortest==longest)shortest=longest==Vector3.up?Vector3.forward:Vector3.up;float maximum=Mathf.Max(size.x,size.y,size.z),minimum=Mathf.Min(size.x,size.y,size.z),middle=size.x+size.y+size.z-maximum-minimum;bool elongated=maximum/Mathf.Max(.001f,middle)>=1.6f;var face=Quaternion.LookRotation(Vector3.back,Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(shortest,longest));subject.transform.localRotation=(elongated?Quaternion.AngleAxis(-30,Vector3.forward):Quaternion.identity)*face;box=WorldBounds(subject);subject.transform.position+=world.transform.position+Vector3.up*1.2f-box.center;box=WorldBounds(subject);view.orthographic=true;view.orthographicSize=Mathf.Max(box.extents.x,box.extents.y)*1.1f;view.transform.localPosition=new Vector3(0,1.2f,-4);view.transform.localRotation=Quaternion.identity;}
        public void UseInspectionRendering(){if(!view)return;if(!view.GetComponent<RiftInspectionCamera>())view.gameObject.AddComponent<RiftInspectionCamera>();view.backgroundColor=Color.clear;var data=view.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.antialiasing=AntialiasingMode.None;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;data.volumeLayerMask=1<<PreviewLayer;view.allowHDR=false;foreach(var light in world.GetComponentsInChildren<Light>())light.shadows=LightShadows.None;}
        public void SetThumbnailResolution(int size){if(!view)return;view.targetTexture=null;if(texture){texture.Release();Destroy(texture);}texture=new RenderTexture(size,size,24,RenderTextureFormat.ARGB32);texture.Create();view.targetTexture=texture;image.texture=texture;}
        public void ShowParty(FrontEndCatalog catalog,ISocialService social)
        {
            for(int i=world.transform.childCount-1;i>=0;i--){var t=world.transform.GetChild(i);if(t.name.StartsWith("Party Avatar"))DestroyImmediate(t.gameObject);}
            var characters=catalog.Characters;if(characters.Length==0)return;
            var hero=characters[Mathf.Clamp(social.Self.Skin,0,characters.Length-1)];if(mainMenuPresentation&&!MultiFPS.Gameplay.RiftCombat.RiftSkinIdentity.Playable(hero.SkinIndex)){foreach(var candidate in characters)if(MultiFPS.Gameplay.RiftCombat.RiftSkinIdentity.Playable(candidate.SkinIndex)){hero=candidate;break;}}Show(hero);
            int slot=0;
            foreach(var member in social.Party){
                if(member.ProfileId==social.Self.Id)continue;var profile=social.Find(member.ProfileId);if(profile==null)continue;
                var model=characters[Mathf.Clamp(profile.Skin,0,characters.Length-1)];var go=new GameObject("Party Avatar "+slot);go.transform.SetParent(world.transform,false);Instantiate(model.Prefab,go.transform);
                var box=WorldBounds(go);go.transform.localScale*=2.05f/Mathf.Max(.001f,box.size.y);box=WorldBounds(go);
                Vector3 offset=new Vector3(slot==0?-2.3f:slot==1?2.3f:4.3f,box.extents.y,slot==2?1.8f:1);
                go.transform.position+=world.transform.position+offset-box.center;go.transform.localRotation=Quaternion.Euler(0,180,0);SetLayer(go);if(mainMenuPresentation)go.SetActive(false);slot++;
            }
        }
        public void ConfigureMainMenuPresentation(){if(!lobby)return;mainMenuPresentation=true;if(stageEnvironment)stageEnvironment.SetActive(false);UseInspectionRendering();UpdateCamera();}
        void UpdateCamera()
        {
            if(!view)return;
            if(lobby){view.transform.localPosition=mainMenuPresentation?new Vector3(0,1.2f,-4.7f):new Vector3(0,1.5f,-7.4f);view.transform.LookAt(world.transform.position+(mainMenuPresentation?new Vector3(0,1.1f,0):new Vector3(.1f,1.2f,0)));view.fieldOfView=mainMenuPresentation?32:34;}
            else{view.transform.localPosition=new Vector3(0,1.4f,-4.8f*zoom);view.transform.LookAt(world.transform.position+Vector3.up*1.2f);view.fieldOfView=36;}
            if(subject)subject.transform.localRotation=Quaternion.Euler(0,yaw,0);
        }
        public void OnDrag(PointerEventData e){if(lobby)return;yaw-=e.delta.x*.4f;UpdateCamera();}
        public void OnScroll(PointerEventData e){if(lobby)return;zoom=Mathf.Clamp(zoom-e.scrollDelta.y*.08f,.65f,1.5f);UpdateCamera();}
        public void Turn(float degrees){yaw+=degrees;UpdateCamera();}
        public bool PlayEmote(LobbyEmote emote)
        {
            if(!lobby||!subject||!catalog||!catalog.EmoteController||emote==null||!emote.Clip)return false;
            if(emoteAnimator)RestoreIdle();
            emoteAnimator=subject.GetComponentInChildren<Animator>();if(!emoteAnimator)return false;
            idleController=emoteAnimator.runtimeAnimatorController;
            emoteController=new AnimatorOverrideController(catalog.EmoteController);
            emoteController[catalog.EmotePlaceholder.name]=emote.Clip;
            emoteAnimator.runtimeAnimatorController=emoteController;emoteAnimator.applyRootMotion=false;
            emoteAnimator.Update(0);emoteAnimator.SetTrigger("playEmote");
            CurrentEmoteName=emote.Name;emoteUntil=Time.unscaledTime+Mathf.Max(.5f,emote.Seconds);restoreAt=0;return true;
        }
        public void StopEmote(){if(!emoteAnimator)return;emoteAnimator.SetTrigger("stopEmote");CurrentEmoteName=null;emoteUntil=0;restoreAt=Time.unscaledTime+.25f;}
        void Update(){if(emoteUntil>0&&Time.unscaledTime>=emoteUntil)StopEmote();if(restoreAt>0&&Time.unscaledTime>=restoreAt)RestoreIdle();}
        void RestoreIdle(){if(emoteAnimator)emoteAnimator.runtimeAnimatorController=idleController;ResetEmote();}
        void ResetEmote(){if(emoteController)Destroy(emoteController);emoteController=null;emoteAnimator=null;idleController=null;CurrentEmoteName=null;emoteUntil=restoreAt=0;}
        static Bounds WorldBounds(GameObject go)
        {
            var renderers=go.GetComponentsInChildren<Renderer>(true);var b=new Bounds(go.transform.position,Vector3.zero);bool first=true;
            foreach(var r in renderers){if(!r.enabled||!r.gameObject.activeInHierarchy)continue;if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}return b;
        }
        static void SetLayer(GameObject go){foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=PreviewLayer;}
        void OnEnable(){if(view)view.enabled=true;}
        void OnDisable(){if(view)view.enabled=false;}
        void OnDestroy(){ResetEmote();if(view)view.targetTexture=null;if(texture){texture.Release();Destroy(texture);}if(world)Destroy(world);}
    }
}
