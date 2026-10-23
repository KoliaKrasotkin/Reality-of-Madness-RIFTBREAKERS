using System;
using System.IO;
using MultiFPS;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ROM.UI.Sprint01
{
    [Serializable] public class VideoPreferences
    {
        public int Quality=2,Width=1920,Height=1080,WindowMode=1,FrameLimit=120,AA=2;
        public bool VSync=true,Bloom=true,MotionBlur=false;
        public float RenderScale=1,Exposure=0,Volume=.8f,Sensitivity=1,Fov=90;
        public VideoPreferences Copy()=>JsonUtility.FromJson<VideoPreferences>(JsonUtility.ToJson(this));
    }
    public sealed class RiftVideoSettings : MonoBehaviour
    {
        public static RiftVideoSettings Instance {get;private set;}
        public VideoPreferences Current {get;private set;}
        public bool AwaitingConfirmation=>rollback!=null;
        public float SecondsRemaining=>Mathf.Max(0,deadline-Time.realtimeSinceStartup);
        public event Action Changed;
        VideoPreferences rollback;
        float deadline;
        UniversalRenderPipelineAsset runtimePipeline;
        RenderPipelineAsset sourcePipeline;
        Volume userVolume;
        string PathName=>Path.Combine(Application.persistentDataPath,"rom-video.json");
        public static RiftVideoSettings Ensure()
        {
            if(Instance)return Instance;
            return new GameObject("ROM Video Preferences").AddComponent<RiftVideoSettings>();
        }
        void Awake()
        {
            if(Instance&&Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);
            Current=new VideoPreferences{Width=Screen.currentResolution.width>0?Screen.currentResolution.width:Screen.width,Height=Screen.currentResolution.height>0?Screen.currentResolution.height:Screen.height,Quality=Application.isMobilePlatform?1:2};
            if(File.Exists(PathName))try{Current=JsonUtility.FromJson<VideoPreferences>(File.ReadAllText(PathName))??Current;}catch(Exception e){Debug.LogWarning(e.Message);}
            Clamp(Current);Apply(Current,true);SceneManager.sceneLoaded+=OnScene;RenderPipelineManager.beginCameraRendering+=OnCamera;
        }
        void OnScene(Scene scene,LoadSceneMode mode){ApplyEffects();}
        void OnCamera(ScriptableRenderContext context,Camera camera)
        {
            if(!camera||camera.cameraType!=CameraType.Game||camera.GetComponent<RiftInspectionCamera>())return;
            if(camera.TryGetComponent<UniversalAdditionalCameraData>(out var data)){
                data.renderPostProcessing=true;data.antialiasing=Current.AA==0?AntialiasingMode.None:Current.AA==1?AntialiasingMode.FastApproximateAntialiasing:AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality=AntialiasingQuality.High;
            }
        }
        public void Preview(VideoPreferences preferences)
        {
            if(AwaitingConfirmation)Revert();rollback=Current.Copy();Current=preferences.Copy();Clamp(Current);Apply(Current,true);deadline=Time.realtimeSinceStartup+15;Changed?.Invoke();
        }
        public void Confirm(){rollback=null;File.WriteAllText(PathName,JsonUtility.ToJson(Current,true));Changed?.Invoke();}
        public void Revert(){if(rollback==null)return;Current=rollback;rollback=null;Apply(Current,true);Changed?.Invoke();}
        void Update(){if(AwaitingConfirmation&&Time.realtimeSinceStartup>=deadline)Revert();}
        static void Clamp(VideoPreferences p)
        {
            p.Quality=Mathf.Clamp(p.Quality,0,QualitySettings.names.Length-1);p.Width=Mathf.Clamp(p.Width,640,7680);p.Height=Mathf.Clamp(p.Height,360,4320);
            p.WindowMode=Mathf.Clamp(p.WindowMode,0,2);p.FrameLimit=Mathf.Clamp(p.FrameLimit,30,240);p.AA=Mathf.Clamp(p.AA,0,2);p.RenderScale=Mathf.Clamp(p.RenderScale,.5f,1.5f);p.Exposure=Mathf.Clamp(p.Exposure,-1,1);p.Volume=Mathf.Clamp01(p.Volume);p.Sensitivity=Mathf.Clamp(p.Sensitivity,.1f,5);p.Fov=Mathf.Clamp(p.Fov,60,110);
        }
        void Apply(VideoPreferences p,bool display)
        {
            QualitySettings.SetQualityLevel(p.Quality,true);
            var pipeline=QualitySettings.renderPipeline;
            if(pipeline==runtimePipeline)pipeline=sourcePipeline;
            if(pipeline is UniversalRenderPipelineAsset urp){
                var previous=runtimePipeline;sourcePipeline=pipeline;runtimePipeline=Instantiate(urp);runtimePipeline.name="ROM Runtime Quality";runtimePipeline.hideFlags=HideFlags.DontSave;
                runtimePipeline.renderScale=p.RenderScale;QualitySettings.renderPipeline=runtimePipeline;if(previous)Destroy(previous);
            }
            QualitySettings.vSyncCount=p.VSync?1:0;Application.targetFrameRate=p.FrameLimit;AudioListener.volume=p.Volume;UserSettings.MouseSensitivity=p.Sensitivity;UserSettings.FieldOfView=p.Fov;
            if(display&&!Application.isBatchMode)Screen.SetResolution(p.Width,p.Height,p.WindowMode==0?FullScreenMode.ExclusiveFullScreen:p.WindowMode==1?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
            ApplyEffects();
        }
        void ApplyEffects()
        {
            if(!userVolume){var go=new GameObject("ROM User Effects");DontDestroyOnLoad(go);userVolume=go.AddComponent<Volume>();userVolume.isGlobal=true;userVolume.priority=100;userVolume.profile=ScriptableObject.CreateInstance<VolumeProfile>();}
            var profile=userVolume.profile;
            if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>();bloom.intensity.Override(Current.Bloom?.22f:0);bloom.threshold.Override(1.2f);
            if(!profile.TryGet<MotionBlur>(out var motion))motion=profile.Add<MotionBlur>();motion.intensity.Override(Current.MotionBlur?.12f:0);
            if(!profile.TryGet<ColorAdjustments>(out var color))color=profile.Add<ColorAdjustments>();var night=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.RiftNightAtmosphere>();color.postExposure.Override(Current.Exposure+(night?night.BaseExposure:0));
        }
        void OnDestroy()
        {
            if(Instance!=this)return;SceneManager.sceneLoaded-=OnScene;RenderPipelineManager.beginCameraRendering-=OnCamera;
            if(QualitySettings.renderPipeline==runtimePipeline)QualitySettings.renderPipeline=sourcePipeline;
            if(runtimePipeline)Destroy(runtimePipeline);if(userVolume){Destroy(userVolume.profile);Destroy(userVolume.gameObject);}Instance=null;
        }
    }
}
