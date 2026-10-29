using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ROM.Progression;
using ROM.UI.Gameplay;
using MultiFPS.Gameplay;

namespace ROM.EditorTools
{
    public static class RiftAchievementFlashReview
    {
        static string Output => Path.GetFullPath("Logs/AchievementFlashReview");
        static readonly List<string> checks = new List<string>();
        static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        static void Call(object target, string name, params object[] args) => target.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(m=>m.Name==name&&m.GetParameters().Length==args.Length&&m.GetParameters().Select((p,i)=>p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(target, args);
        static void Check(bool value, string message) { if (!value) throw new Exception(message); checks.Add("PASS " + message); File.WriteAllLines(Path.Combine(Output,"progress.txt"),checks); }

        [MenuItem("ROM/FX/Probar destello de logro (sin progreso)")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Termina Play o la compilacion antes de esta previsualizacion");
            Directory.CreateDirectory(Output);
            Scene preview = default;
            RenderTexture target = null;
            RiftAchievementFlashSettings copy = null;
            var previousTarget = RenderTexture.active;
            try
            {
                var catalog = RiftJournalCatalog.Load;
                Check(catalog && catalog.Achievements.Length > 0, "Actual journal catalog loaded");
                var definition = catalog.Achievements.First(a => a.Criterion.Event == RiftProgressEvent.TrainingHit);
                var state = new JournalState { CharacterId = "AchievementFlashQA-memory-only" };
                var notices = state.Record(catalog, definition.Criterion.Event, definition.Criterion.Filter ?? "dummy", definition.Criterion.Required);
                Check(state.Achievements.Any(a => a.Id == definition.Id), "Actual JournalState.Record unlocks the catalog achievement in memory");
                string unchanged = JsonUtility.ToJson(state);

                preview = EditorSceneManager.NewPreviewScene();
                var prefab = Resources.Load<GameObject>("RiftGameplayHUD");
                Check(prefab, "Production HUD prefab loaded");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
                var hud = instance.GetComponent<RiftGameplayHUD>();
                var alerts = instance.GetComponent<RiftCombatAlerts>();
                if (!alerts) alerts = instance.AddComponent<RiftCombatAlerts>();
                if (Field(alerts, "panel") == null) Call(alerts, "Awake");
                var panel = (RectTransform)Field(alerts, "panel");
                var group = (CanvasGroup)Field(alerts, "group");
                var flash = (RiftAchievementFlash)Field(alerts, "achievementFlash");
                Check(panel && flash, "Production alert Awake installs the flash behind the labels");
                Check(flash.transform.GetSiblingIndex() == 0 && !flash.raycastTarget && !group.blocksRaycasts, "Flash is behind text and does not intercept input");
                var settings = Resources.Load<RiftAchievementFlashSettings>("RiftAchievementFlash");
                Check(settings, "Editable Resources settings asset resolves");
                copy = UnityEngine.Object.Instantiate(settings);flash.Settings = copy;

                RiftCombatAlerts.EnqueueJournalNotice(alerts.Queue, string.Join("\n", notices), 0f);
                alerts.Queue.Tick(0f);
                Check(alerts.Queue.Current != null && alerts.Queue.Current.Key.StartsWith("achievement:"), "Real journal unlock notice routes to the achievement alert");
                Check(alerts.Queue.Current.Title == "Logro desbloqueado" && alerts.Queue.Current.Body == definition.Title, "Achievement heading and title stay in separate text lanes");
                RiftCombatAlerts.EnqueueJournalNotice(alerts.Queue, string.Join("\n", notices), .5f);
                Check(alerts.Queue.Pending == 0, "Duplicate unlock notice does not enqueue a second flash");
                group.alpha = 1f;
                Call(alerts, "UpdateAchievementFlash", copy.StartDelay + .1f);
                Check(flash.Playing, "Visible achievement triggers production one-shot flash");
                flash.SampleAt(.3f);
                Call(alerts, "UpdateAchievementFlash", copy.StartDelay + .2f);
                Check(!flash.Playing && Mathf.Abs(flash.Progress - .3f) < .001f, "Same displayed alert does not retrigger the flash");

                var queue = new RiftAlertQueue();
                RiftCombatAlerts.EnqueueJournalNotice(queue, "Misión completada: prueba\nLogro desbloqueado: Uno\nLogro desbloqueado: Dos", 0f);
                queue.Tick(0);Check(queue.Current.Body == "Uno" && queue.Pending == 2, "Multiple unlocks remain separate ahead of ordinary quest feedback");
                queue.Tick(4.1f);Check(queue.Current.Body == "Dos", "Second achievement survives the queue");
                queue.Tick(8.2f);Check(queue.Current.Key.StartsWith("quest:"), "Ordinary quest feedback is retained without achievement tagging");
                Check(JsonUtility.ToJson(state) == unchanged, "Presentation does not mutate the unlocked journal state");

                foreach (Transform child in hud.Design) if (child != panel) child.gameObject.SetActive(false);
                hud.Design.anchorMin = hud.Design.anchorMax = hud.Design.pivot = new Vector2(.5f, .5f);
                hud.Design.anchoredPosition = Vector2.zero;hud.Design.sizeDelta = new Vector2(1920, 1080);hud.Design.localScale = Vector3.one;
                var cameraObject = new GameObject("Achievement flash inspection camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, preview);
                var camera = cameraObject.GetComponent<Camera>();camera.scene = preview;camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;camera.backgroundColor = new Color(.025f, .035f, .055f);
                camera.orthographic = true;camera.orthographicSize = 540;camera.nearClipPlane = .1f;camera.farClipPlane = 20;
                camera.transform.position = new Vector3(0, 0, -10);
                var canvas = instance.GetComponent<Canvas>();canvas.renderMode = RenderMode.ScreenSpaceCamera;canvas.worldCamera = camera;canvas.planeDistance = 1;
                var scaler = instance.GetComponent<CanvasScaler>();if (scaler) { scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution = new Vector2(1920,1080); }
                ((Text)Field(alerts,"title")).text = alerts.Queue.Current.Title;
                ((Text)Field(alerts,"body")).text = alerts.Queue.Current.Body;
                group.alpha = 1f;
                target = new RenderTexture(3840,2160,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture = target;
                foreach (float phase in new[] {0f, .08f, .2f, .4f, .7f, 1f})
                {
                    flash.SampleAt(phase);flash.Rebuild(CanvasUpdate.PreRender);Canvas.ForceUpdateCanvases();if(phase>0f&&phase<1f){using(var vh=new VertexHelper()){Call(flash,"OnPopulateMesh",vh);Check(vh.currentVertCount>0,"Active phase "+phase+" builds flash geometry");}}{var mesh=flash.canvasRenderer.GetMesh();File.AppendAllText(Path.Combine(Output,"geometry.txt"),"phase="+phase+" progress="+flash.Progress+" active="+flash.isActiveAndEnabled+" cull="+flash.canvasRenderer.cull+" alpha="+flash.canvasRenderer.GetAlpha()+" rect="+flash.rectTransform.rect+" pos="+flash.rectTransform.position+" mesh="+(mesh?mesh.vertexCount:0)+" material="+flash.material.name+"\n");}camera.Render();camera.Render();
                    RenderTexture.active = target;
                    var pixels = new Texture2D(3840,2160,TextureFormat.RGB24,false);
                    pixels.ReadPixels(new Rect(0,0,3840,2160),0,0);pixels.Apply();
                    File.WriteAllBytes(Path.Combine(Output,"flash-"+Mathf.RoundToInt(phase*100).ToString("D3")+".png"),pixels.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(pixels);
                }
                copy.Enabled=false;flash.SampleAt(.2f);Canvas.ForceUpdateCanvases();
                using(var vh=new VertexHelper()) { Call(flash,"OnPopulateMesh",vh);Check(vh.currentVertCount==0,"Disabled setting produces no flash geometry"); }
                copy.Enabled=true;flash.Stop();
                using(var vh=new VertexHelper()) { Call(flash,"OnPopulateMesh",vh);Check(vh.currentVertCount==0,"Finished flash leaves no residual geometry"); }
                checks.Add("SCOPE Actual catalog + JournalState.Record + production alert construction/routing/flash in a preview scene. No live Host/RPC unlock, combat, or player save was invoked.");
                File.WriteAllLines(Path.Combine(Output,"PASS.txt"),new[]{DateTime.UtcNow.ToString("o")}.Concat(checks));
                Debug.Log("Achievement flash review PASS: "+checks.Count+" checks/notes");
            }
            catch(Exception e) { File.WriteAllText(Path.Combine(Output,"FAIL.txt"),DateTime.UtcNow.ToString("o")+"\n"+e);Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);throw; }
            finally
            {
                RenderTexture.active=previousTarget;
                if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}
                if(copy)UnityEngine.Object.DestroyImmediate(copy);
                if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
