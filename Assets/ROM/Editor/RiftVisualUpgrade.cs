using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ROM.Horde.EditorTools
{
    public static partial class FracturedForestBuilder
    {
        const string VisualRoot="Assets/ROM/Rendering";
        public static void VisualUpgradeFromCommandLine()
        {
            try{UpgradeRiftVisuals();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void VisualPreviewFromCommandLine()
        {
            CaptureRiftReview();EditorApplication.Exit(0);
        }
        public static void CaptureRiftReview()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var mode=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();if(mode){var serialized=new SerializedObject(mode);serialized.FindProperty("_mapDisplayName").stringValue="El Rift";serialized.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());}
            CaptureEclipse("Documentation/el-rift-visual-overview.png",SanctuaryCenter+new Vector3(-220,140,-260),SanctuaryCenter+Vector3.up*12);
            CaptureEclipse("Documentation/el-rift-visual-ground.png",SanctuaryCenter+new Vector3(-18,3,-68),SanctuaryCenter+new Vector3(0,10,60));
        }
        [MenuItem("ROM/El Rift/Aplicar iluminación y postprocesado")]
        public static void UpgradeRiftVisuals()
        {
            Directory.CreateDirectory(VisualRoot);AssetDatabase.Refresh();ConfigureQuality();
            var scene=EditorSceneManager.OpenScene(ScenePath);
            const string group="El Rift - Direccion de arte";
            var old=GameObject.Find(group);if(old)UnityEngine.Object.DestroyImmediate(old);root=new GameObject(group).transform;
            var profile=EnsureCinematicProfile();var volume=root.gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=10;volume.sharedProfile=profile;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.28f,.35f,.43f);RenderSettings.fogDensity=.00165f;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.4f,.49f,.61f);RenderSettings.ambientEquatorColor=new Color(.22f,.27f,.3f);RenderSettings.ambientGroundColor=new Color(.085f,.09f,.11f);RenderSettings.reflectionIntensity=.6f;
            string skyPath=VisualRoot+"/RiftSky.mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);if(!sky){sky=new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(sky,skyPath);}
            sky.SetColor("_SkyTint",new Color(.42f,.48f,.58f));sky.SetColor("_GroundColor",new Color(.07f,.08f,.12f));sky.SetFloat("_AtmosphereThickness",1.25f);sky.SetFloat("_Exposure",.85f);RenderSettings.skybox=sky;EditorUtility.SetDirty(sky);
            var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).ToArray();
            var sun=lights.FirstOrDefault();if(!sun)sun=new GameObject("Rift Sun").AddComponent<Light>();
            foreach(var light in lights)light.enabled=light==sun;
            sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(38,-32,0);sun.color=new Color(1,.89f,.73f);sun.intensity=1.8f;sun.shadows=LightShadows.Soft;sun.shadowBias=.035f;sun.shadowNormalBias=.25f;RenderSettings.sun=sun;
            int count=ReplacePrimitiveCliffs();
            Physics.SyncTransforms();ClearBridgeRockIntersections();
            var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();surface.BuildNavMesh();
            string navPath=Root+"/NavMesh-RiftVisuals.asset";var nav=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if(nav){EditorUtility.CopySerialized(surface.navMeshData,nav);surface.RemoveData();surface.navMeshData=nav;surface.AddData();EditorUtility.SetDirty(nav);}else AssetDatabase.CreateAsset(surface.navMeshData,navPath);EditorUtility.SetDirty(surface);
            VerifyPath(SanctuaryCenter);VerifyPath(SunCourt);VerifyPath(UrbanCenter);foreach(var center in SatelliteCenters)VerifyPath(center);ValidateNavigation();
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            CaptureEclipse("Documentation/el-rift-visual-overview.png",SanctuaryCenter+new Vector3(-220,140,-260),SanctuaryCenter+Vector3.up*12);
            CaptureEclipse("Documentation/el-rift-visual-ground.png",SanctuaryCenter+new Vector3(-18,3,-68),SanctuaryCenter+new Vector3(0,10,60));
            File.WriteAllText("Documentation/el-rift-visual-validation.txt",$"{count} primitive cliff meshes replaced by authored rocks.\nACES / Bloom / color grading / vignette / SSAO (high profiles) / soft shadows / atmospheric sky and fog.\nNavMesh routes: original arena, three regions, urban, Eclipse and Sun court PASS.\nNo claim of mobile performance or final AAA art.\n");
        }
        static int ReplacePrimitiveCliffs()
        {
            var candidates=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.name.ToLowerInvariant().Contains("acantilado")&&f.sharedMesh&&f.sharedMesh.name=="Sphere").ToArray();
            var decks=UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name.Contains("suelo continuo")).ToArray();int count=0;
            foreach(var old in candidates){
                var box=old.GetComponent<Renderer>().bounds;bool nearBridge=decks.Any(d=>d.bounds.SqrDistance(new Vector3(box.center.x,d.bounds.center.y,box.center.z))<16*16);
                if(nearBridge)continue;
                var position=new Vector3(box.center.x,box.min.y,box.center.z);float height=Mathf.Clamp(box.size.y,25,55);
                var cliff=Place(Temple+"Cliffs/Cliff_01.prefab",position,height,(count*137)%360,"Acantilado rocoso de El Rift");
                foreach(var filter in cliff.GetComponentsInChildren<MeshFilter>()){
                    filter.name="Acantilado rocoso de El Rift";
                    foreach(var collider in filter.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(collider);
                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
                }
                foreach(var renderer in cliff.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)if(material&&AssetDatabase.GetAssetPath(material).StartsWith("Assets/ROM/"))material.enableInstancing=true;
                UnityEngine.Object.DestroyImmediate(old.gameObject);count++;
            }
            return count;
        }
        public static VolumeProfile EnsureCinematicProfile()
        {
            string path=VisualRoot+"/RiftCinematic.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
            if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.ACES);
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.intensity.Override(.25f);bloom.threshold.Override(1.2f);bloom.scatter.Override(.65f);
            if(!profile.TryGet<ColorAdjustments>(out var color)){color=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(color,profile);}color.contrast.Override(12);color.saturation.Override(-6);
            if(!profile.TryGet<Vignette>(out var vignette)){vignette=profile.Add<Vignette>();AssetDatabase.AddObjectToAsset(vignette,profile);}vignette.intensity.Override(.17f);vignette.smoothness.Override(.55f);
            EditorUtility.SetDirty(profile);return profile;
        }
        static void ConfigureQuality()
        {
            var template=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/MultiFPS/Rendering/URPA.asset");
            string[] labels={"Bajo","Medio","Alto","Ultra"};var qualityObject=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);var levels=qualityObject.FindProperty("m_QualitySettings");
            for(int i=0;i<levels.arraySize;i++){
                string path=VisualRoot+"/RiftQuality_"+i+".asset";var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if(!pipeline){pipeline=UnityEngine.Object.Instantiate(template);pipeline.name="Rift "+labels[i];AssetDatabase.CreateAsset(pipeline,path);}
                pipeline.supportsHDR=true;pipeline.supportsCameraDepthTexture=true;pipeline.msaaSampleCount=i<2?2:4;pipeline.renderScale=i==0?.8f:1;pipeline.shadowDistance=new[]{45f,75f,110f,150f}[i];pipeline.shadowCascadeCount=i<2?2:4;pipeline.shadowDepthBias=.5f;pipeline.shadowNormalBias=.3f;
                var so=new SerializedObject(pipeline);so.FindProperty("m_MainLightShadowmapResolution").intValue=i<2?2048:4096;
                if(i>=2){
                    string rendererPath=VisualRoot+"/RiftRenderer_"+i+".asset";var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                    if(!renderer){renderer=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/MultiFPS/Rendering/New Universal Render Pipeline Asset_Renderer.asset"));AssetDatabase.CreateAsset(renderer,rendererPath);}
                    if(!renderer.rendererFeatures.Any(f=>f is ScreenSpaceAmbientOcclusion)){
                        var ao=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();ao.name="Rift Ambient Occlusion";AssetDatabase.AddObjectToAsset(ao,renderer);renderer.rendererFeatures.Add(ao);
                        var settings=new SerializedObject(ao);settings.FindProperty("m_Settings.Intensity").floatValue=.65f;settings.FindProperty("m_Settings.Radius").floatValue=.3f;settings.FindProperty("m_Settings.Downsample").boolValue=true;settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(renderer);
                    }
                    so.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;
                }
                so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
                var level=levels.GetArrayElementAtIndex(i);level.FindPropertyRelative("name").stringValue=labels[i];level.FindPropertyRelative("customRenderPipeline").objectReferenceValue=pipeline;level.FindPropertyRelative("skinWeights").intValue=4;
                level.FindPropertyRelative("globalTextureMipmapLimit").intValue=i==0?1:0;level.FindPropertyRelative("lodBias").floatValue=i<2?1.5f:3;level.FindPropertyRelative("excludedTargetPlatforms").ClearArray();
            }
            qualityObject.FindProperty("m_CurrentQuality").intValue=2;qualityObject.ApplyModifiedPropertiesWithoutUndo();QualitySettings.SetQualityLevel(2,true);AssetDatabase.SaveAssets();
        }
    }
}
