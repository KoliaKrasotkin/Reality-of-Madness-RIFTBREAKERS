using UnityEngine;
namespace ROM.UI.Gameplay {
[CreateAssetMenu(menuName="ROM/UI/Shared presentation settings")]
public sealed class RiftPresentationSettings:ScriptableObject {
 [Header("Shared style")][Tooltip("Optional shared text font; falls back to the HUD theme.")]public Font BodyFont;
 public Color Panel=new Color(.032f,.036f,.058f,.9f),Border=new Color(.46f,.36f,.65f,.9f),TextColor=new Color(.94f,.93f,.98f),Accent=new Color(.6f,.36f,.9f,.7f);
 [Header("Authored prefabs")][Tooltip("Keep positions/fonts authored in the HUD prefab instead of applying runtime modern layout.")]public bool KeepAuthoredHUDLayout;
 [Tooltip("Keep an authored map panel instead of generating the modern frame.")]public bool KeepAuthoredMapLayout;
 [Header("Map")][HideInInspector]public float MinimumMapZoom=1; // Legacy serialized value; the illustrated map always allows its full-bleed overview.
 [Range(1,4)]public float TutorialMinimumMapZoom=1,MaximumMapZoom=4;
 [Tooltip("Tutorial atlas inset only. The illustrated Rift map fills its viewport edge-to-edge.")][UnityEngine.Serialization.FormerlySerializedAs("MapOverviewPadding")][Range(0,80)]public float TutorialMapOverviewPadding=24;
 [Tooltip("One combined island name/range card, kept readable independently of map zoom.")]public Vector2 MapRegionLabelSize=new Vector2(156,44);
 [Range(11,20)]public int MapRegionLabelFontSize=13;
 [Range(10,24)]public int MapLabelFontSize=14;
 public Vector2 MapLabelSize=new Vector2(150,24);public bool HideMinimapWithWorldMap=true;
 public Vector2 MapAnchorMin=new Vector2(.04f,.06f),MapAnchorMax=new Vector2(.96f,.94f);
 [Header("Shared HUD docking")]
 public Vector2 TrackerOffset=new Vector2(-24,-304),TargetOffset=new Vector2(-54,-350),ActionDockOffset=new Vector2(0,196);
 [Header("Tutorial overlay")]
 public Vector2 TutorialPanelSize=new Vector2(800,180),TutorialPanelOffset=new Vector2(0,-36),TutorialInstructionSize=new Vector2(752,94),TutorialInstructionOffset=new Vector2(0,-55),TutorialButtonSize=new Vector2(136,34);
 [Range(14,32)]public int TutorialInstructionFontSize=21,TutorialMinimumFontSize=17;
 public Vector2[] TutorialButtons={new Vector2(-320,-152),new Vector2(-172,-152),new Vector2(-24,-152),new Vector2(124,-152),new Vector2(272,-152)};
 public Vector2 TutorialButtonPosition(int index){if(TutorialButtons!=null&&index>=0&&index<TutorialButtons.Length)return TutorialButtons[index];return new Vector2(-320+148*index,-152);}
 static RiftPresentationSettings cached;
 public static RiftPresentationSettings Load {get{if(!cached){cached=Resources.Load<RiftPresentationSettings>("UI/RiftPresentationSettings");if(!cached){cached=CreateInstance<RiftPresentationSettings>();cached.hideFlags=HideFlags.HideAndDontSave;}}return cached;}}
 void OnValidate(){MinimumMapZoom=Mathf.Clamp(MinimumMapZoom,1,4);TutorialMinimumMapZoom=Mathf.Clamp(TutorialMinimumMapZoom,1,4);MaximumMapZoom=Mathf.Clamp(MaximumMapZoom,Mathf.Max(MinimumMapZoom,TutorialMinimumMapZoom),4);TutorialMinimumFontSize=Mathf.Min(TutorialMinimumFontSize,TutorialInstructionFontSize);TutorialPanelSize=Vector2.Max(TutorialPanelSize,new Vector2(400,140));}
}}
