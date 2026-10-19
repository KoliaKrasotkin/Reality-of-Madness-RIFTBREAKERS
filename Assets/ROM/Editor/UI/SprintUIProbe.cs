using System;
using System.IO;
using System.Linq;
using ROM.UI.Sprint01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ROM.UI.EditorTools
{
    [InitializeOnLoad]
    public static class SprintUIProbe
    {
        const string Key="ROM.SprintUIProbe";
        static double started;
        static bool ran;
        static SprintUIProbe(){EditorApplication.playModeStateChanged+=Changed;}
        public static void FromCommandLine()
        {
            SessionState.SetInt(Key,1);EditorSceneManager.OpenScene("Assets/ROM/UI/Sprint01/ROM_UI_Sprint01.unity");EditorApplication.isPlaying=true;
        }
        public static void BuildAndCheckFromCommandLine(){SprintUIBuilder.Build();FromCommandLine();}
        public static void StartupFromCommandLine(){SessionState.SetInt(Key,2);EditorSceneManager.OpenScene("Assets/MultiFPS/Levels/hub.unity");EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state)
        {
            if(SessionState.GetInt(Key,0)==0)return;
            if(state==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;ran=false;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode){EditorSceneManager.OpenScene("Assets/MultiFPS/Levels/hub.unity");EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}
        }
        static void Tick()
        {
            if(ran||EditorApplication.timeSinceStartup-started<2)return;ran=true;
            try{
                var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub)throw new Exception("Missing RiftHub");
                if(SessionState.GetInt(Key,0)==2){CheckStartup(hub);SessionState.SetInt(Key+"Result",0);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;return;}
                var report="PLAY MODE UI VALIDATION\n";
                foreach(HubPage page in Enum.GetValues(typeof(HubPage))){hub.Navigate(page);Canvas.ForceUpdateCanvases();if(hub.Page!=page)throw new Exception("Navigation failed "+page);report+=page+": PASS / "+hub.GetComponentsInChildren<Button>().Length+" controls\n";}
                // Exercise actual button listeners, item modal, ready, mock purchase and
                // boundary that forbids any mutation of displayed currency balances.
                hub.Navigate(HubPage.Missions);Find(hub,"SELECCIONAR").onClick.Invoke();Assert(hub.Page==HubPage.Loadout,"Mission -> Loadout");
                Find(hub,"CONTINUAR A LA ESCUADRA").onClick.Invoke();Assert(hub.Page==HubPage.Lobby,"Loadout -> Lobby");
                Find(hub,"ESTOY LISTO").onClick.Invoke();Assert(hub.Services.Party.LocalReady,"Ready");
                var gold=hub.Services.Balances.AncientGold;var crystals=hub.Services.Balances.RiftCrystals;
                hub.Navigate(HubPage.Marketplace);Find(hub,"VER COSMÉTICO").onClick.Invoke();Find(hub,"CONFIRMAR").onClick.Invoke();
                Assert(hub.Services.Marketplace.Products.Any(p=>p.Owned),"Mock ownership");Assert(gold==hub.Services.Balances.AncientGold&&crystals==hub.Services.Balances.RiftCrystals,"Mock balances isolated");
                hub.Services.Rewards.PreviewClaim();Assert(hub.Services.Rewards.State==RewardState.PendingValidation,"Reward validation state");
                hub.Build();hub.Navigate(HubPage.Home);Capture(hub,"Documentation/sprint01-hub.png",1920,1080);
                hub.Navigate(HubPage.Missions);Capture(hub,"Documentation/sprint01-missions.png",1920,1080);
                hub.Navigate(HubPage.Marketplace);Capture(hub,"Documentation/sprint01-marketplace.png",1920,1080);
                hub.Navigate(HubPage.Home);Capture(hub,"Documentation/sprint01-mobile.png",1280,720);
                File.WriteAllText("Documentation/sprint01-ui-validation.txt",report+"Button flow / Ready / modal / mock ownership / unchanged balances / pending reward: PASS\nCaptures: desktop 1920x1080, landscape 1280x720.\nReal host and remote clients not launched by this probe.\n");
                SessionState.SetInt(Key+"Result",0);
            }catch(Exception e){Debug.LogException(e);SessionState.SetInt(Key+"Result",1);}
            EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
        }
        static Button Find(RiftHub hub,string name)=>hub.GetComponentsInChildren<Button>().Last(b=>b.name==name);
        static void CheckStartup(RiftHub hub)
        {
            Assert(hub.Creator&&hub.LegacyMenu,"Startup bindings");
            Assert(hub.Creator.Maps.Any(m=>m&&m.Scene.Contains("map_rom_horde")),"Horde registration");
            Find(hub,"CONEXIÓN / PERFIL").onClick.Invoke();Assert(hub.LegacyMenu.activeSelf&&!hub.gameObject.activeSelf,"Legacy bridge open");
            hub.LegacyMenu.transform.Find("SprintReturn").GetComponent<Button>().onClick.Invoke();Assert(hub.gameObject.activeSelf&&!hub.LegacyMenu.activeSelf,"Legacy bridge return");
            hub.Navigate(HubPage.Missions);Find(hub,"SELECCIONAR").onClick.Invoke();Find(hub,"CONTINUAR A LA ESCUADRA").onClick.Invoke();Find(hub,"ESTOY LISTO").onClick.Invoke();
            Assert(Find(hub,"DESPLEGAR COMO HOST").interactable,"Deployment enabled in startup hub");
            Find(hub,"DESPLEGAR COMO HOST").onClick.Invoke();Assert(hub.Creator.Maps[hub.Creator.MapselectionDropdown.value].Scene.Contains("map_rom_horde"),"Horde selected");
            Assert(hub.GetComponentsInChildren<Button>().Any(b=>b.name=="CONFIRMAR"),"Deployment confirmation");
            Find(hub,"CANCELAR").onClick.Invoke();
            File.WriteAllText("Documentation/sprint01-startup-validation.txt","PLAY MODE STARTUP PASS\nExisting RoomCreator / legacy open and return / 4-slot visual lobby / ready / real Horde selection / deployment confirmation.\nConfirmation cancelled before hosting. No network session started.\n");
        }
        static void Assert(bool condition,string name){if(!condition)throw new Exception(name);}
        public static void Capture(RiftHub hub,string file,int width,int height)
        {
            var canvas=hub.GetComponent<Canvas>();var go=new GameObject("CaptureCamera");var camera=go.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;camera.cullingMask=1<<5;
            var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            foreach(var t in hub.GetComponentsInChildren<Transform>())t.gameObject.layer=5;
            Canvas.ForceUpdateCanvases();var fit=hub.Design;fit.anchorMin=fit.anchorMax=fit.pivot=new Vector2(.5f,.5f);fit.anchoredPosition=Vector2.zero;fit.localScale=Vector3.one*Mathf.Min(((RectTransform)hub.transform).rect.width/1920f,((RectTransform)hub.transform).rect.height/1080f);
            Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(file,texture.EncodeToPNG());RenderTexture.active=old;
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
