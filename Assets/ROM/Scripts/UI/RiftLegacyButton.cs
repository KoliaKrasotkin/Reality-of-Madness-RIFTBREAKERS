using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MultiFPS.UI;
namespace ROM.UI.Sprint01 {
public sealed class RiftLegacyButton : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler {
public RiftButtonGraphic Frame;Button source;int state;bool last;
void Awake(){source=GetComponent<Button>();UpdateFrame();}
void OnEnable(){if(source)UpdateFrame();}
void LateUpdate(){if(source&&last!=source.interactable)UpdateFrame();}
void UpdateFrame(){if(!Frame||!source)return;last=source.interactable;Frame.SetVisualState(state,last);}
public void OnPointerEnter(PointerEventData e){state=1;UpdateFrame();}public void OnPointerExit(PointerEventData e){state=0;UpdateFrame();}
public void OnPointerDown(PointerEventData e){state=2;UpdateFrame();}public void OnPointerUp(PointerEventData e){state=1;UpdateFrame();}
public void OnSelect(BaseEventData e){state=1;UpdateFrame();}public void OnDeselect(BaseEventData e){state=0;UpdateFrame();}
}
}
