using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ROM.UI.Gameplay;
namespace ROM.UI.Sprint01 {
// Opt-in chrome. Never called on inventory, shared fonts or the shared widget factories.
public static class RiftPanelChrome {
 public static void Apply(Transform root){if(!root)return;foreach(var image in root.GetComponentsInChildren<Image>(true)){if(!image.transform.Find("Top")||!image.transform.Find("Bottom"))continue;foreach(Transform child in image.transform)if(child.name=="Top"||child.name=="Bottom"||child.name=="Left"||child.name=="Right"||child.name=="Accent")child.gameObject.SetActive(false);image.color=Color.clear;Surface((RectTransform)image.transform,new Color(.032f,.036f,.058f,.9f));}
  foreach(var old in root.GetComponentsInChildren<RiftButtonGraphic>(true)){old.enabled=false;var button=old.GetComponent<Button>();if(!button)continue;var surface=Surface((RectTransform)old.transform,new Color(.09f,.075f,.14f,.7f));button.targetGraphic=surface;var state=old.GetComponent<RiftModernChromeButton>();if(!state)state=old.gameObject.AddComponent<RiftModernChromeButton>();state.Surface=surface;state.Button=button;foreach(var text in old.GetComponentsInChildren<Text>(true)){text.font=RiftWidgets.Body?RiftWidgets.Body:text.font;text.fontStyle=FontStyle.Normal;text.color=new Color(.94f,.93f,.98f);var rect=text.rectTransform;rect.anchoredPosition=new Vector2(18,rect.anchoredPosition.y);rect.sizeDelta=new Vector2(((RectTransform)old.transform).rect.width-36,rect.sizeDelta.y);}}
  foreach(var text in root.GetComponentsInChildren<Text>(true))if(text.color==RiftWidgets.Gold)text.color=new Color(.77f,.66f,.97f);
 }
 static RiftMapSurface Surface(RectTransform root,Color fill){var found=root.Find("Modern local chrome");if(found)return found.GetComponent<RiftMapSurface>();var rect=new GameObject("Modern local chrome",typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(root,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;rect.anchoredPosition=Vector2.zero;rect.SetAsFirstSibling();var surface=rect.gameObject.AddComponent<RiftMapSurface>();surface.Fill=fill;surface.Border=new Color(.43f,.4f,.55f,.85f);surface.Radius=8;surface.raycastTarget=false;return surface;}
}
public sealed class RiftModernChromeButton:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler {
 public RiftMapSurface Surface;public Button Button;bool hover,selected;Color last;
 void LateUpdate(){if(!Surface||!Button)return;var tint=!Button.interactable?new Color(.5f,.5f,.55f,.7f):hover||selected?new Color(.69f,.45f,1):new Color(.43f,.4f,.55f,.85f);if(tint==last)return;last=tint;Surface.Border=tint;Surface.Glow=hover||selected?4:0;Surface.SetVerticesDirty();}
 public void OnPointerEnter(PointerEventData e){hover=true;}public void OnPointerExit(PointerEventData e){hover=false;}public void OnSelect(BaseEventData e){selected=true;}public void OnDeselect(BaseEventData e){selected=false;}
}
}
