using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MultiFPS.UI;
using MultiFPS.UI.HUD;
using ROM.UI.Sprint01;
using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.EditorTools {
public static class GameUIUpgrade {
static Font body,buttons;static RiftHub source;
public static void Build(){
source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ROM/UI/Sprint01/ROM_RiftHub.prefab").GetComponent<RiftHub>();body=source.InterfaceFont;buttons=source.ButtonFont;
W.Body=W.Title=body;W.ButtonFont=buttons;LobbyEmoteBuilder.Build();
var report=new List<string>();
var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/MultiFPS/Levels/Prefabs/UI","Assets/ROM/UI"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.Contains("/Models/")&&!p.Contains("/FrontEnd/")).Distinct().ToArray();
foreach(string path in paths){var root=PrefabUtility.LoadPrefabContents(path);if(!root.GetComponentInChildren<Graphic>(true)&&!root.GetComponentInChildren<Canvas>(true)){PrefabUtility.UnloadPrefabContents(root);continue;}
Style(root);if(root.GetComponent<UICharacter>())Hud(root);if(root.GetComponent<UIPauseMenu>())Pause(root);if(root.GetComponent<ChatUI>())Chat(root);if(root.GetComponent<ScoreBoard>())Score(root);
PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);report.Add(path);
}
foreach(string guid in AssetDatabase.FindAssets("t:ROMUITheme")){var theme=AssetDatabase.LoadAssetAtPath<ROM.UI.ROMUITheme>(AssetDatabase.GUIDToAssetPath(guid));theme.Accent=W.Red;theme.AccentDark=new Color(.3f,.015f,.05f);theme.Panel=W.Ink;theme.Text=W.White;theme.Muted=W.Muted;theme.BodyFont=body;theme.TitleFont=buttons;theme.Logo=source.Logo;EditorUtility.SetDirty(theme);}
foreach(string guid in AssetDatabase.FindAssets("t:HordeHUDTheme")){var theme=AssetDatabase.LoadAssetAtPath<ROM.Horde.HordeHUDTheme>(AssetDatabase.GUIDToAssetPath(guid));theme.Accent=W.Red;theme.Shade=W.Ink;theme.Text=W.White;theme.Muted=W.Muted;theme.TitleFont=buttons;theme.BodyFont=body;theme.Logo=source.Logo;EditorUtility.SetDirty(theme);}
AssetDatabase.SaveAssets();File.WriteAllLines("Documentation/game-ui-upgrade.txt",report);
}
static void Style(GameObject root){
foreach(var canvas in root.GetComponentsInChildren<Canvas>(true))if(canvas.isRootCanvas&&canvas.renderMode==RenderMode.ScreenSpaceOverlay){var scaler=canvas.GetComponent<CanvasScaler>();if(scaler){scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;}}
foreach(var text in root.GetComponentsInChildren<Text>(true)){text.font=body;text.fontSize=Mathf.Clamp(text.fontSize,16,34);text.color=W.White;
foreach(var shadow in text.GetComponents<Shadow>())UnityEngine.Object.DestroyImmediate(shadow);}
foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)){text.color=W.White;text.fontSize=Mathf.Clamp(text.fontSize,16,32);}
foreach(var image in root.GetComponentsInChildren<Image>(true)){
foreach(var shadow in image.GetComponents<Shadow>())if(!image.name.ToLowerInvariant().Contains("crosshair"))UnityEngine.Object.DestroyImmediate(shadow);
string n=image.name.ToLowerInvariant();if(n=="logo"&&source.Logo)image.sprite=source.Logo;if(image.GetComponent<Button>())continue;
if((n.Contains("accent")||n=="bar"||n=="fill")&&image.color.a>.01f)image.color=W.Red;
if(image.type==Image.Type.Filled){if(image.color.a>.01f)image.color=W.Red;continue;}
if((n.Contains("background")||n.Contains("panel")||n=="overlay"||n=="legend")&&image.rectTransform.sizeDelta.x>=100&&image.rectTransform.sizeDelta.y>=40&&image.color.a>.01f){image.color=W.Ink;Frame(image.rectTransform);}}
foreach(var button in root.GetComponentsInChildren<Button>(true)){
if(button is RiftStyledButton)continue;var graphic=button.transform.Find("RiftButtonFrame");RiftButtonGraphic frame;
if(!graphic){var rect=W.Rect(button.transform,"RiftButtonFrame",0,0,1,1);Stretch(rect);rect.SetAsFirstSibling();rect.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;frame=rect.gameObject.AddComponent<RiftButtonGraphic>();}else frame=graphic.GetComponent<RiftButtonGraphic>();
frame.Ornament=false;frame.color=Color.white;frame.raycastTarget=true;var image=button.GetComponent<Image>();if(image)image.color=Color.clear;button.targetGraphic=frame;button.transition=Selectable.Transition.None;
var skin=button.GetComponent<RiftLegacyButton>()??button.gameObject.AddComponent<RiftLegacyButton>();skin.Frame=frame;
foreach(var text in button.GetComponentsInChildren<Text>(true)){text.font=buttons;text.fontSize=Mathf.Clamp(text.fontSize,16,26);text.color=W.White;}}
foreach(var dropdown in root.GetComponentsInChildren<Dropdown>(true)){if(dropdown.targetGraphic)dropdown.targetGraphic.color=W.Ink;if(dropdown.captionText){dropdown.captionText.color=W.White;dropdown.captionText.fontSize=18;dropdown.captionText.enabled=true;dropdown.captionText.gameObject.SetActive(true);var label=dropdown.captionText.rectTransform;label.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;label.offsetMin=new Vector2(12,2);label.offsetMax=new Vector2(-34,-2);Frame((RectTransform)dropdown.transform);}if(dropdown.template){var bg=dropdown.template.GetComponent<Image>();if(bg)bg.color=W.Ink;}}
foreach(var input in root.GetComponentsInChildren<InputField>(true))if(input.targetGraphic)input.targetGraphic.color=W.Ink;
}
static void Frame(RectTransform parent){var rect=parent.Find("RiftFrame") as RectTransform;if(!rect){rect=W.Panel(parent,"RiftFrame",0,0,parent.sizeDelta.x,parent.sizeDelta.y);rect.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;}Stretch(rect);rect.SetAsFirstSibling();rect.GetComponent<Image>().color=Color.clear;
foreach(Transform child in rect){var edge=(RectTransform)child;if(child.name=="Top"||child.name=="Bottom"){float y=child.name=="Top"?1:0;edge.anchorMin=new Vector2(0,y);edge.anchorMax=new Vector2(1,y);edge.pivot=new Vector2(.5f,y);edge.anchoredPosition=Vector2.zero;edge.sizeDelta=new Vector2(0,1);}else if(child.name=="Left"||child.name=="Right"){float x=child.name=="Left"?0:1;edge.anchorMin=new Vector2(x,0);edge.anchorMax=new Vector2(x,1);edge.pivot=new Vector2(x,.5f);edge.anchoredPosition=Vector2.zero;edge.sizeDelta=new Vector2(1,0);}else if(child.name=="Accent"){bool first=child.GetSiblingIndex()==4;Rect(edge,first?new Vector2(0,1):new Vector2(1,0),first?new Vector2(0,1):new Vector2(1,0),0,0,60,3);}}}
static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
static void Rect(RectTransform r,Vector2 anchor,Vector2 pivot,float x,float y,float width,float height){r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(width,height);r.localScale=Vector3.one;}
static void Child(Transform parent,string path,float x,float y,float w,float h,int font=0){var t=parent.Find(path) as RectTransform;if(!t)return;foreach(var fitter in t.GetComponents<ContentSizeFitter>())UnityEngine.Object.DestroyImmediate(fitter);Rect(t,new Vector2(0,1),new Vector2(0,1),x,-y,w,h);var text=t.GetComponent<Text>();if(text&&font>0)text.fontSize=font;}
static void Hud(GameObject root){
var hud=root.transform.Find("GameplayHUD");var health=hud.Find("Health") as RectTransform;Rect(health,Vector2.zero,Vector2.zero,26,24,240,106);var healthImage=health.GetComponent<Image>()??health.gameObject.AddComponent<Image>();healthImage.color=W.Ink;healthImage.raycastTarget=false;Frame(health);
Child(health,"health",16,33,200,48,40);Child(health,"healthBa_background",16,88,208,5);Child(health,"healthBar",16,88,208,5);health.Find("healthBar").GetComponent<Image>().color=W.Red;
if(!health.Find("VitalLabel")){var label=W.Label(health,"VITALIDAD",16,8,200,25,14,W.Gold);label.name="VitalLabel";}
var ammo=hud.Find("Ammo") as RectTransform;foreach(var layout in ammo.GetComponents<LayoutGroup>())UnityEngine.Object.DestroyImmediate(layout);foreach(var fitter in ammo.GetComponents<ContentSizeFitter>())UnityEngine.Object.DestroyImmediate(fitter);
Rect(ammo,new Vector2(1,0),new Vector2(1,0),-26,24,250,106);ammo.GetComponent<Image>().color=W.Ink;Frame(ammo);Child(ammo,"ammo",16,34,115,53,42);Child(ammo,"ammoSupply",140,51,94,34,22);
var weapon=ammo.Find("WeaponLabel")?ammo.Find("WeaponLabel").GetComponent<Text>():W.Label(ammo,"ARMA",16,8,220,25,14,W.Gold);weapon.name="WeaponLabel";
var status=hud.Find("ReloadStatus")?hud.Find("ReloadStatus").GetComponent<Text>():W.Label(hud,"",0,0,350,32,19,W.Gold);status.name="ReloadStatus";Rect(status.rectTransform,new Vector2(.5f,0),new Vector2(.5f,0),0,80,350,32);status.alignment=TextAnchor.MiddleCenter;
var details=root.GetComponent<RiftHudDetails>()??root.AddComponent<RiftHudDetails>();details.WeaponName=weapon;details.Status=status;
var equipment=hud.Find("Equipment") as RectTransform;Rect(equipment,new Vector2(1,0),new Vector2(1,0),-26,145,250,280);var group=equipment.GetComponent<VerticalLayoutGroup>();group.spacing=7;group.childAlignment=TextAnchor.LowerRight;group.childControlHeight=group.childControlWidth=false;group.childForceExpandHeight=group.childForceExpandWidth=false;group.childScaleHeight=false;
var element=equipment.GetComponentInChildren<HudInventoryElement>(true);var itemRect=(RectTransform)element.transform;itemRect.sizeDelta=new Vector2(250,54);itemRect.localScale=Vector3.one;element.EmptySlotSize=new Vector2(250,54);
Child(element.transform,"ItemImage",48,4,90,44);element.transform.Find("ItemImage").GetComponent<Image>().preserveAspect=true;Child(element.transform,"ItemName",142,8,102,38,14);Child(element.transform,"Text (Legacy)",10,8,32,38,18);
var so=new SerializedObject(element);so.FindProperty("_itemName").objectReferenceValue=element.transform.Find("ItemName").GetComponent<Text>();so.FindProperty("_inUseColor").colorValue=W.White;so.FindProperty("_notInUseColor").colorValue=W.Muted;so.FindProperty("_inUsebackGroundColor").colorValue=new Color(.28f,.025f,.055f,.95f);so.FindProperty("_notInUsebackGroundColor").colorValue=W.Ink;so.ApplyModifiedPropertiesWithoutUndo();Frame(itemRect);
}
static void Pause(GameObject root){
var pause=root.GetComponent<UIPauseMenu>();var existing=root.GetComponent<RiftPauseAdapter>();if(existing){Arsenal(existing.Hub.ArsenalPanel);return;}
var so=new SerializedObject(pause);var disconnect=so.FindProperty("_uiBtnDisconnect").objectReferenceValue as Button;
var legacy=pause.pauseMenuOverlay;legacy.SetActive(false);
var rect=W.Rect(root.transform,"RiftPauseScreen",0,0,1,1);Stretch(rect);var hub=rect.gameObject.AddComponent<RiftHub>();hub.InGame=true;hub.InterfaceFont=body;hub.ButtonFont=buttons;hub.Theme=source.Theme;hub.Logo=source.Logo;hub.Catalog=source.Catalog;
pause.pauseMenuOverlay=rect.gameObject;rect.gameObject.SetActive(false);var adapter=root.AddComponent<RiftPauseAdapter>();adapter.Pause=pause;adapter.Hub=hub;adapter.Disconnect=disconnect;var arsenal=legacy.transform.Find("LodoutPanel") as RectTransform;hub.ArsenalPanel=arsenal;adapter.ArsenalCancel=arsenal.Find("Cancel").GetComponent<Button>();Arsenal(arsenal);arsenal.gameObject.SetActive(false);var oldSettings=root.GetComponent<UIUserSettings>();if(oldSettings)oldSettings.enabled=false;root.GetComponent<Canvas>().sortingOrder=100;
}
static void Arsenal(RectTransform panel){if(!panel)return;panel.GetComponent<Image>().color=W.Ink;Child(panel,"header",28,20,644,48,26);panel.Find("header").GetComponent<Text>().text="ARSENAL";if(!panel.Find("ArsenalHint")){var text=W.Label(panel,"Se aplica en tu próxima aparición.",28,78,644,35,18,W.Muted);text.name="ArsenalHint";}var slots=panel.Find("Slots") as RectTransform;Rect(slots,new Vector2(.5f,.5f),new Vector2(.5f,.5f),0,-70,575,223);var cancel=panel.Find("Cancel") as RectTransform;Rect(cancel,new Vector2(1,0),new Vector2(1,0),-24,24,190,48);cancel.GetComponentInChildren<Text>().text="CERRAR";}
static void Chat(GameObject root){var chat=root.GetComponent<ChatUI>();var log=chat._chatText;Stretch((RectTransform)log.transform.parent);log.fontSize=18;log.enableAutoSizing=false;log.alignment=TextAlignmentOptions.BottomLeft;Rect(log.rectTransform,Vector2.zero,Vector2.zero,40,190,522,192);var so=new SerializedObject(chat);var input=so.FindProperty("_inputField").objectReferenceValue as InputField;Rect((RectTransform)input.transform,Vector2.zero,Vector2.zero,26,138,550,42);var rect=root.transform.Find("RiftChatFrame") as RectTransform;if(!rect){rect=W.Panel(root.transform,"RiftChatFrame",0,0,550,225);rect.SetAsFirstSibling();}Rect(rect,Vector2.zero,Vector2.zero,26,178,550,225);var group=rect.GetComponent<CanvasGroup>()??rect.gameObject.AddComponent<CanvasGroup>();var skin=root.GetComponent<RiftChatFrame>()??root.AddComponent<RiftChatFrame>();skin.Log=log;skin.Frame=group;}
static void Score(GameObject root){foreach(var image in root.GetComponentsInChildren<Image>(true))if(image.name.Contains("deaths_background"))image.color=Color.clear;var so=new SerializedObject(root.GetComponent<ScoreBoard>());var overlay=(GameObject)so.FindProperty("_scoreBoard").objectReferenceValue;var rect=(RectTransform)overlay.transform;Rect(rect,new Vector2(.5f,.5f),new Vector2(.5f,.5f),0,0,1100,500);Frame(rect);foreach(var row in root.GetComponentsInChildren<UIScoreBoardPlayerElement>(true)){var data=new SerializedObject(row);data.FindProperty("_localPlayerColor").colorValue=new Color(.22f,.025f,.05f,.96f);data.FindProperty("_rowColor").colorValue=W.Ink;data.FindProperty("_textColor").colorValue=W.White;data.ApplyModifiedPropertiesWithoutUndo();}
var legend=rect.Find("Legend");if(legend){var icon=legend.Find("latency");if(icon)icon.gameObject.SetActive(false);if(!legend.Find("RiftPing")){var ping=W.Label(legend,"PING",0,0,72,44,18);ping.name="RiftPing";Rect(ping.rectTransform,new Vector2(1,.5f),new Vector2(1,.5f),-20,0,72,44);}}var grid=overlay.GetComponent<GridLayoutGroup>();grid.cellSize=new Vector2(1068,52);grid.padding=new RectOffset(16,16,90,16);grid.spacing=new Vector2(0,6);if(!rect.Find("RiftScoreTitle")){var title=W.Label(rect,"ESCUADRA / EL RIFT",28,18,1044,48,28,W.Gold);title.name="RiftScoreTitle";title.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;}foreach(var text in overlay.GetComponentsInChildren<Text>(true)){if(text.name=="username"){Rect(text.rectTransform,new Vector2(0,.5f),new Vector2(0,.5f),28,0,600,44);text.fontSize=22;}int offset=text.name=="kills"?260:text.name=="deaths"?180:text.name=="assists"?100:text.name=="latency"?20:-1;if(offset>=0){Rect(text.rectTransform,new Vector2(1,.5f),new Vector2(1,.5f),-offset,0,72,44);text.fontSize=18;}}}
public static void Run(){try{Build();GameUIProbe.Gameplay();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
