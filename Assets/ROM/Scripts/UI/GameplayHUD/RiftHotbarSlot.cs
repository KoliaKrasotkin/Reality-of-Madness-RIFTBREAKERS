using MultiFPS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace ROM.UI.Gameplay {
public sealed class RiftHotbarSlot:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IDropHandler {
 public int Slot;static RiftHotbarSlot source;static Image ghost;
 public void OnPointerEnter(PointerEventData e){if(!RiftHotbar.Dragging)RiftAbilityTooltip.ShowAbility(gameObject,RiftHotbar.Resolve(Slot));}
 public void OnPointerExit(PointerEventData e)=>RiftAbilityTooltip.Hide(gameObject);
 public void OnBeginDrag(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;End();source=this;RiftHotbar.Dragging=true;e.eligibleForClick=false;RiftAbilityTooltip.HideAll();var image=transform.Find("Ability icon")?.GetComponent<Image>();if(image&&image.sprite){var go=new GameObject("Dragged ability",typeof(RectTransform),typeof(Image));go.transform.SetParent(GetComponentInParent<Canvas>().transform,false);ghost=go.GetComponent<Image>();ghost.sprite=image.sprite;ghost.raycastTarget=false;ghost.color=new Color(1,1,1,.8f);ghost.rectTransform.sizeDelta=new Vector2(61,62);ghost.rectTransform.position=e.position;}}
 public void OnDrag(PointerEventData e){e.eligibleForClick=false;if(source==this&&ghost)ghost.rectTransform.position=e.position;}
 public void OnDrop(PointerEventData e){if(source&&e.pointerDrag==source.gameObject){RiftHotbar.Swap(source.Slot,Slot);e.eligibleForClick=false;}}
 public void OnEndDrag(PointerEventData e){e.eligibleForClick=false;if(source==this)End();}
 void LateUpdate(){if(source==this){var canvas=GetComponentInParent<Canvas>();if(!canvas||!canvas.enabled)End();}}
 void OnApplicationFocus(bool focus){if(!focus&&source==this)End();}
 void OnDisable(){if(source==this)End();RiftAbilityTooltip.Hide(gameObject);}
 static void End(){source=null;RiftHotbar.Dragging=false;if(ghost)Destroy(ghost.gameObject);ghost=null;}
}}
