using UnityEngine;using UnityEngine.EventSystems;
namespace ROM.UI.Gameplay {
// Clicking the map reports real X/Z only. It does not teleport, move or create quests.
public sealed class RiftMapCoordinatePointer:MonoBehaviour,IPointerClickHandler,IInitializePotentialDragHandler,IBeginDragHandler,IDragHandler,IEndDragHandler {
 public RiftFieldUI Owner;bool dragged;
 UnityEngine.UI.ScrollRect Scroll=>Owner?Owner.Viewport.GetComponent<UnityEngine.UI.ScrollRect>():null;
 public void OnInitializePotentialDrag(PointerEventData data){dragged=false;if(Scroll)Scroll.OnInitializePotentialDrag(data);}
 public void OnBeginDrag(PointerEventData data){dragged=true;if(Scroll)Scroll.OnBeginDrag(data);}
 public void OnDrag(PointerEventData data){if(Scroll)Scroll.OnDrag(data);}
 public void OnEndDrag(PointerEventData data){if(Scroll)Scroll.OnEndDrag(data);}
 public void OnPointerClick(PointerEventData data){if(dragged||!Owner||!Owner.MapOpen||data.button!=PointerEventData.InputButton.Left)return;Vector2 local;if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,data.position,data.pressEventCamera,out local)&&Owner.TryWorldPosition(local,out var world))Owner.MapInfo.text="Ubicación del mapa\nX: "+world.x.ToString("0")+"   Z: "+world.z.ToString("0");}
 }
}
