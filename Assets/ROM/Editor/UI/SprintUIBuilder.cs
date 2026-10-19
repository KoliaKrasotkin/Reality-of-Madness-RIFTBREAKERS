using System;
using System.IO;
using System.Linq;
using MultiFPS;
using ROM.UI.Sprint01;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROM.UI.EditorTools
{
    public static class SprintUIBuilder
    {
        const string Root="Assets/ROM/UI/Sprint01";
        [MenuItem("ROM/UI/Sprint 01/Construir e instalar HUB")]
        public static void Build()
        {
            ImportSprite(Root+"/Art/RiftCitadel.png");ImportSprite(Root+"/Art/ROMLogo.png");ImportSprite(Root+"/Art/ROMLogoMono.png");
            // Dedicated preview scene, independent of networking. The normal startup
            // remains the existing hub with the same RoomCreator and Mirror objects.
            var preview=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var hub=Create();new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var cam=new GameObject("PreviewCamera",typeof(Camera)).GetComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
            PrefabUtility.SaveAsPrefabAsset(hub.gameObject,Root+"/ROM_RiftHub.prefab");
            EditorSceneManager.SaveScene(preview,Root+"/ROM_UI_Sprint01.unity");
            var scene=EditorSceneManager.OpenScene("Assets/MultiFPS/Levels/hub.unity");
            foreach(var old in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RiftHub>(true)).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            hub=Create();hub.Creator=UnityEngine.Object.FindFirstObjectByType<RoomCreator>(FindObjectsInactive.Include);
            var legacy=UnityEngine.Object.FindFirstObjectByType<ROMMainMenu>(FindObjectsInactive.Include);
            if(legacy){hub.LegacyMenu=legacy.gameObject;legacy.gameObject.SetActive(false);
                var previous=legacy.transform.Find("SprintReturn");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var back=ROMUIFactory.Button(legacy.transform,ROMUITheme.Load(),"SprintReturn","VOLVER AL HUB",true,new Vector2(.5f,1),new Vector2(0,-20),new Vector2(300,48));
                UnityEventTools.AddPersistentListener(back.onClick,hub.ReturnFromLegacy);
            }
            if(!UnityEngine.Object.FindFirstObjectByType<EventSystem>())new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("[Sprint01] Preview and startup hub saved. Existing networking preserved.");
        }
        static void ImportSprite(string path)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)return;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=2048;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
        }
        static RiftHub Create()
        {
            var go=new GameObject("ROM_RiftHub",typeof(RectTransform));ROMUIFactory.Canvas(go,25,true);
            var scaler=go.GetComponent<CanvasScaler>();scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var hub=go.AddComponent<RiftHub>();hub.Theme=ROMUITheme.Load();hub.Background=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/RiftCitadel.png");hub.Logo=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/ROMLogo.png");
            hub.Build();return hub;
        }
        public static void FromCommandLine(){try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
