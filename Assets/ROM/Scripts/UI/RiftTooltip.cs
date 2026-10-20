using UnityEngine;
using UnityEngine.EventSystems;

namespace ROM.UI.Sprint01
{
    public sealed class RiftTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public string Message;
        public void OnPointerEnter(PointerEventData data)=>Show();
        public void OnPointerExit(PointerEventData data)=>Hide();
        public void OnSelect(BaseEventData data)=>Show();
        public void OnDeselect(BaseEventData data)=>Hide();
        void Show(){var hub=GetComponentInParent<RiftHub>();if(hub)hub.SetHint(Message);}
        void Hide(){var hub=GetComponentInParent<RiftHub>();if(hub)hub.SetHint("");}
    }
}
