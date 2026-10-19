using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ROM.Horde.EditorTools
{
    public static partial class FracturedForestBuilder
    {
        public static void FinalizeSprintFromCommandLine()
        {
            try{
                ROM.UI.EditorTools.SprintUIBuilder.Build();
                BuildNextIsland();
                EditorSceneManager.OpenScene(ScenePath);Physics.SyncTransforms();
                VerifyPath(SanctuaryCenter);VerifyPath(SunCourt);foreach(var c in SatelliteCenters)VerifyPath(c);VerifyPath(UrbanCenter);
                CaptureEclipse("Documentation/eclipse-overview.png",SanctuaryCenter+new Vector3(-300,300,-330),SanctuaryCenter+Vector3.up*8);
                CaptureEclipse("Documentation/eclipse-court.png",SanctuaryCenter+new Vector3(-20,5,-72),SanctuaryCenter+new Vector3(0,10,60));
                CaptureEclipse("Documentation/sun-ground-repaired.png",SunCenter+new Vector3(-280,200,280),SunCenter+Vector3.up*20);
                ROM.UI.EditorTools.SprintUIProbe.FromCommandLine();
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void CaptureEclipse(string file,Vector3 position,Vector3 lookAt)
        {
            var go=new GameObject("Sprint QA Camera",typeof(Camera));var camera=go.GetComponent<Camera>();camera.transform.position=position;camera.transform.LookAt(lookAt);
            camera.fieldOfView=60;camera.nearClipPlane=.3f;camera.farClipPlane=3000;camera.clearFlags=CameraClearFlags.Skybox;camera.backgroundColor=new Color(.12f,.17f,.23f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            float fog=RenderSettings.fogDensity;RenderSettings.fogDensity=.0007f;
            var lods=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);foreach(var lod in lods)lod.ForceLOD(0);
            var target=new RenderTexture(1600,1000,24);camera.targetTexture=target;camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();File.WriteAllBytes(file,texture.EncodeToPNG());RenderTexture.active=previous;
            camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);
            foreach(var lod in lods)lod.ForceLOD(-1);RenderSettings.fogDensity=fog;
        }
    }
}
