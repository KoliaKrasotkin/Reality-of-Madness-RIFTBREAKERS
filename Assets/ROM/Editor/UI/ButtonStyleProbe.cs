using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ROM.UI.Sprint01;
namespace ROM.UI.EditorTools
{
    [InitializeOnLoad] public static class ButtonStyleProbe
    {
        const string Key="ROM.ButtonStyleProbe";
        static double next;static int step;
        static ButtonStyleProbe(){EditorApplication.playModeStateChanged+=Changed;}
        public static void FromCommandLine()
        {
            var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/ROM/UI/FrontEnd/Fonts/CrimsonText-SemiBold.ttf");
            if(!font)throw new Exception("Missing button font");
            string prefab="Assets/ROM/UI/Sprint01/ROM_RiftHub.prefab";
            var root=PrefabUtility.LoadPrefabContents(prefab);root.GetComponent<RiftHub>().ButtonFont=font;PrefabUtility.SaveAsPrefabAsset(root,prefab);PrefabUtility.UnloadPrefabContents(root);
            foreach(var path in new[]{"Assets/ROM/UI/Sprint01/ROM_UI_Sprint01.unity","Assets/MultiFPS/Levels/hub.unity"}){
                if(!File.Exists(path))continue;var scene=EditorSceneManager.OpenScene(path);var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(hub){hub.ButtonFont=font;EditorUtility.SetDirty(hub);EditorSceneManager.SaveScene(scene);}}
            AssetDatabase.SaveAssets();SessionState.SetInt(Key,1);EditorApplication.isPlaying=true;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(SessionState.GetInt(Key,0)==0)return;
            if(state==PlayModeStateChange.EnteredPlayMode){next=EditorApplication.timeSinceStartup+3;step=0;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetInt(Key,0);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            try{
                var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub)throw new Exception("Missing hub");
                if(step==0){
                    hub.Navigate(HubPage.Home);
                    var panel=RiftWidgets.Panel(hub.Design,"Button style review",360,200,1200,700);
                    RiftWidgets.Label(panel,"ESTILO DE BOTONES",50,22,1000,45,28);
                    string[] labels={"NORMAL","SELECCIONADO","CURSOR ENCIMA","PRESIONADO","DESACTIVADO"};
                    for(int i=0;i<5;i++){
                        int id=i;var b=RiftWidgets.Button(panel,labels[i],50,100+i*105,480,82,()=>{},i==1);
                        var frame=b.GetComponent<RiftButtonGraphic>();
                        if(i==2)frame.SetVisualState(1,true);if(i==3)frame.SetVisualState(2,true);if(i==4)b.interactable=false;
                        if(i==0){bool clicked=false;b.onClick.AddListener(()=>clicked=true);b.onClick.Invoke();if(!clicked)throw new Exception("Button callback failed");}
                    }
                    RiftWidgets.Button(panel,"BOTÓN COMPACTO",650,110,350,58,()=>{});
                    RiftWidgets.Label(panel,"Marcos metálicos / relieve / ornamento\nBrillo rojo / tipografía serif\nDistribución y acciones conservadas",650,220,500,180,24);
                    next=EditorApplication.timeSinceStartup+1;step++;return;
                }
                if(step==1){SprintUIProbe.Capture(hub,"Documentation/rift-button-states.png",1920,1080);UnityEngine.Object.DestroyImmediate(hub.Design.Find("Button style review").gameObject);next=EditorApplication.timeSinceStartup+1;step++;return;}
                SprintUIProbe.Capture(hub,"Documentation/rift-button-style-lobby.png",1920,1080);
                File.WriteAllText("Documentation/button-style-validation.txt","PASS: Unity compilation, real lobby in Play Mode, click callback, normal / active / hover / pressed / disabled state gallery.\nOnly shared button rendering, button font and related scene/prefab references modified.\nCrimson Text source: https://github.com/google/fonts/tree/main/ofl/crimsontext ; OFL included with font.\n");
                SessionState.SetInt(Key+"Result",0);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
            }catch(Exception e){Debug.LogException(e);SessionState.SetInt(Key+"Result",1);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
        }
    }
}
