using System.Collections.Generic;
using MultiFPS.UI;
using ROM.UI.Sprint01;
using UnityEngine;
namespace ROM.UI.Gameplay {
// Repositions HUD roots, never inventory content. Restores original transforms on close.
[DefaultExecutionOrder(1400)]public sealed class RiftHUDPanelDock:MonoBehaviour {
 public enum Mode{None,Side,Wide,Map}
 sealed class Pose{public RectTransform Rect;public Vector2 Min,Max,Pivot,Position;public Vector3 Scale;public bool Hidden,WasActive;public void Restore(){if(!Rect)return;Rect.anchorMin=Min;Rect.anchorMax=Max;Rect.pivot=Pivot;Rect.anchoredPosition=Position;Rect.localScale=Scale;if(Hidden){Rect.gameObject.SetActive(WasActive);Hidden=false;}}}
 readonly Dictionary<string,Pose> poses=new Dictionary<string,Pose>();Mode current;CanvasGroup group;bool wasBlocks,wasInteractable;float wasAlpha;RiftFieldUI field;
 public Mode Current=>current;
 void Awake(){foreach(Transform child in transform)if(child is RectTransform r)poses[child.name]=new Pose{Rect=r,Min=r.anchorMin,Max=r.anchorMax,Pivot=r.pivot,Position=r.anchoredPosition,Scale=r.localScale};group=GetComponent<CanvasGroup>();if(!group)group=gameObject.AddComponent<CanvasGroup>();wasBlocks=group.blocksRaycasts;wasInteractable=group.interactable;wasAlpha=group.alpha;}
 void LateUpdate(){if(!field)field=FindFirstObjectByType<RiftFieldUI>();var mode=Mode.None;if(field&&field.MapOpen)mode=Mode.Map;else{RiftHub active=null;foreach(var h in FindObjectsByType<RiftHub>(FindObjectsSortMode.None))if(h.InGame&&h.isActiveAndEnabled){active=h;break;}if(active&&ClientFrontend.Pause)mode=(active.Page==HubPage.Abilities&&active.GetComponentInChildren<ROM.UI.Sprint01.RiftTalentTreeView>())||active.Page==HubPage.Progression||active.Page==HubPage.Settings||active.Page==HubPage.Social?Mode.Wide:Mode.Side;else if(RiftVillageUI.Instance&&RiftVillageUI.Instance.TalkingTo)mode=Mode.Side;}Refresh(mode);}
 void Place(string name,Vector2 anchor,Vector2 pos,float scale){if(!poses.TryGetValue(name,out var p)||!p.Rect)return;p.Rect.anchorMin=p.Rect.anchorMax=anchor;if(name=="Player vitals"||name=="Selected target")p.Rect.pivot=Vector2.up;p.Rect.anchoredPosition=pos;p.Rect.localScale=((name=="Player vitals"||name=="Selected target")?Vector3.one*RiftHUDPreferences.FrameScale:p.Scale)*scale;}
 void Hide(string name){if(!poses.TryGetValue(name,out var p)||!p.Rect)return;if(!p.Hidden){p.WasActive=p.Rect.gameObject.activeSelf;p.Hidden=true;}p.Rect.gameObject.SetActive(false);}
 public void Refresh(Mode mode){if(mode!=current){foreach(var p in poses.Values)p.Restore();current=mode;}group.alpha=wasAlpha;group.blocksRaycasts=mode==Mode.None&&wasBlocks;group.interactable=mode==Mode.None&&wasInteractable;if(mode==Mode.None)return;var root=(RectTransform)transform;float scale=mode==Mode.Side?Mathf.Min(1,(root.rect.width*.44f-48)/RiftHUDIntegratedBar.Width):Mathf.Min(1,(root.rect.width-48)/2120);scale=Mathf.Max(mode==Mode.Side?.42f:.55f,scale);var left=Vector2.zero;
  Place("Player vitals",left,new Vector2(24,(mode==Mode.Side?328:180)*scale),scale);
  Place("Integrated action dock",left,new Vector2(24+(RiftHUDIntegratedBar.Width*.5f+(mode==Mode.Side?0:630))*scale,RiftHUDIntegratedBar.Bottom*scale),scale);
  Place("Gameplay shortcuts",left,new Vector2(mode==Mode.Side?24+400*scale:24+1265*scale,mode==Mode.Side?270*scale:95*scale),scale);
  Hide("Always accessible inventory");Hide("Mouseover tooltip");Hide("General menu shortcuts");
  if(mode==Mode.Side){Place("Selected target",left,new Vector2(24,486*scale),scale);Place("Circular minimap",Vector2.up,new Vector2(24+400*scale,-24),scale);Place("Tracked quest",Vector2.up,new Vector2(24,-330*scale),scale);Place("Journal notification",Vector2.up,new Vector2(24,-640*scale),scale);}
  else{Place("Tracked quest",Vector2.zero,new Vector2(24+1810*scale,180*scale),scale*.72f);foreach(var n in new[]{"Circular minimap","Zone and checkpoint","Selected target","Journal notification"})Hide(n);}
 }
 void OnDisable(){foreach(var p in poses.Values)p.Restore();if(group){group.alpha=wasAlpha;group.blocksRaycasts=wasBlocks;group.interactable=wasInteractable;}current=Mode.None;}
}
}
