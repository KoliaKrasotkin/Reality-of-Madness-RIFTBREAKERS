using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace ROM.UI.Gameplay {
// Local presentation only. A network party roster must exist before other actors can be shown.
public sealed class RiftMapPlayerLocator:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler {
 RiftFieldUI map;RectTransform[] rings;RectTransform tooltip;Text caption;float started=-10;bool hovered,located;string displayName="Vos";int level;
 public float PulseDuration=>2.1f;public bool PulseActive=>located&&Time.unscaledTime-started<PulseDuration;
 public static RiftMapPlayerLocator Install(RiftFieldUI owner){var p=owner.PlayerMarker.gameObject.AddComponent<RiftMapPlayerLocator>();p.Initialize(owner);return p;}
 void Initialize(RiftFieldUI owner){map=owner;map.PlayerMarker.sizeDelta=new Vector2(30,38);var arrow=GetComponent<RiftHUDGraphic>();arrow.color=new Color(1,.98f,.87f);arrow.raycastTarget=true;var outline=gameObject.AddComponent<Outline>();outline.effectColor=new Color(.025f,.01f,.07f,1);outline.effectDistance=new Vector2(2.5f,-2.5f);
  rings=new RectTransform[2];for(int i=0;i<2;i++){var ring=new GameObject("Local player sonar "+i,typeof(RectTransform)).GetComponent<RectTransform>();ring.SetParent(map.Content,false);ring.anchorMin=ring.anchorMax=ring.pivot=new Vector2(.5f,.5f);var g=ring.gameObject.AddComponent<RiftHUDGraphic>();g.Shape=HUDShape.Ring;g.Thickness=2;g.raycastTarget=false;rings[i]=ring;ring.SetSiblingIndex(map.PlayerMarker.GetSiblingIndex());}
  tooltip=new GameObject("Local player map tooltip",typeof(RectTransform)).GetComponent<RectTransform>();tooltip.SetParent(map.Viewport,false);tooltip.anchorMin=tooltip.anchorMax=tooltip.pivot=new Vector2(.5f,.5f);tooltip.sizeDelta=new Vector2(240,76);var surface=tooltip.gameObject.AddComponent<RiftMapSurface>();surface.Fill=new Color(.025f,.028f,.05f,.97f);surface.Border=new Color(.65f,.48f,1);surface.raycastTarget=false;var label=new GameObject("Player identity",typeof(RectTransform)).GetComponent<RectTransform>();label.SetParent(tooltip,false);label.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;label.sizeDelta=new Vector2(-28,-18);caption=label.gameObject.AddComponent<Text>();caption.font=Resources.Load<Font>("RiftInventoryStatic");if(!caption.font)caption.font=map.Theme.BodyFont;caption.fontSize=17;caption.alignment=TextAnchor.MiddleLeft;caption.color=new Color(.97f,.95f,1);caption.raycastTarget=false;tooltip.gameObject.SetActive(false);
 }
 public bool ShowLocal(Vector3 position,float yaw,string name,int actualLevel){located=map.TryPosition(position,out var point);map.PlayerMarker.gameObject.SetActive(located);if(!located){HidePresentation();return false;}displayName=string.IsNullOrWhiteSpace(name)?"Vos":name;level=actualLevel;map.PlayerMarker.anchoredPosition=point;var heading=map.Heading(position,yaw);if(heading.sqrMagnitude>0)map.PlayerMarker.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(heading.x,heading.y)*Mathf.Rad2Deg);return true;}
 public void Pulse(){if(!located)return;started=Time.unscaledTime;}
 public void Center(){if(!located)return;map.Content.anchoredPosition=-map.PlayerMarker.anchoredPosition*map.Content.localScale.x;Pulse();}
 void Update(){if(!map||!map.MapOpen){HidePresentation();return;}var p=ClientFrontend.ClientPlayerInstance;var actor=p?p.MyCharacter:null;if(actor){var combat=p.GetComponent<AbilityController>();ShowLocal(actor.transform.position,actor.Input.LookY,actor.Health.CharacterName,combat?combat.Level:0);}DrawPulse(Time.unscaledTime-started);if(hovered&&located){tooltip.gameObject.SetActive(true);caption.text="Vos · "+displayName+(level>0?"\nNivel "+level:"");Vector2 point;RectTransformUtility.ScreenPointToLocalPointInRectangle(map.Viewport,RectTransformUtility.WorldToScreenPoint(null,map.PlayerMarker.position),null,out point);var bounds=map.Viewport.rect;point+=new Vector2(140,55);point.x=Mathf.Clamp(point.x,bounds.xMin+128,bounds.xMax-128);point.y=Mathf.Clamp(point.y,bounds.yMin+46,bounds.yMax-46);tooltip.anchoredPosition=point;tooltip.SetAsLastSibling();}else tooltip.gameObject.SetActive(false);}
 public void DrawPulse(float age){if(rings==null)return;for(int i=0;i<rings.Length;i++){float t=(age-i*.3f)/1.8f;bool on=located&&map.MapOpen&&t>=0&&t<=1;rings[i].gameObject.SetActive(on);if(!on)continue;rings[i].anchoredPosition=map.PlayerMarker.anchoredPosition;rings[i].sizeDelta=Vector2.one*Mathf.Lerp(30,145,t);rings[i].localScale=Vector3.one/map.Content.localScale.x;var g=rings[i].GetComponent<RiftHUDGraphic>();g.color=new Color(.74f,.61f,1,(1-t)*.75f);}}
 void HidePresentation(){hovered=false;if(tooltip)tooltip.gameObject.SetActive(false);if(rings!=null)foreach(var r in rings)if(r)r.gameObject.SetActive(false);}
 public void OnPointerEnter(PointerEventData e){hovered=true;}public void OnPointerExit(PointerEventData e){hovered=false;}
 void OnDisable(){HidePresentation();}
}
}
