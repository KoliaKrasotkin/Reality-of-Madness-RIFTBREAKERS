using MultiFPS;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace ROM.UI.Gameplay {
public static class RiftAbilityDrag {
 public static GameObject Owner {get;private set;}public static int Ability {get;private set;}public static int FromSlot {get;private set;}static Image ghost;
 public static void Begin(GameObject owner,int ability,int from,Sprite icon,PointerEventData e){Cancel();var p=ClientFrontend.ClientPlayerInstance;var c=p?p.GetComponent<AbilityController>():null;if(!c||!c.IsAbilityLearned(ability))return;Owner=owner;Ability=ability;FromSlot=from;RiftHotbar.Dragging=true;e.eligibleForClick=false;RiftAbilityTooltip.HideAll();if(icon){var go=new GameObject("Dragged ability",typeof(RectTransform),typeof(Image));go.transform.SetParent(owner.GetComponentInParent<Canvas>().transform,false);ghost=go.GetComponent<Image>();ghost.sprite=icon;ghost.raycastTarget=false;ghost.color=new Color(1,1,1,.8f);ghost.rectTransform.sizeDelta=new Vector2(66,66);Move(e);}}
 public static void Move(PointerEventData e){e.eligibleForClick=false;if(ghost)ghost.rectTransform.position=e.position;}
 public static bool Drop(PointerEventData e,int slot){if(!Owner||e.pointerDrag!=Owner)return false;e.eligibleForClick=false;return RiftHotbar.Assign(Ability,slot);}
 public static void Cancel(){Owner=null;Ability=0;FromSlot=-1;RiftHotbar.Dragging=false;if(ghost)Object.Destroy(ghost.gameObject);ghost=null;}
}
public sealed class RiftHotbarSlot:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IDropHandler,ISelectHandler,IDeselectHandler,IPointerClickHandler {
 bool focused,hovered;public int Slot;
 void Show(){int id=RiftHotbar.Resolve(Slot);if(id>0)RiftAbilityTooltip.ShowAbility(gameObject,id);else RiftAbilityTooltip.ShowText(gameObject,"Ranura vacía","Asigná una habilidad desde el libro. La tecla pertenece a esta ranura.","Ability"+Slot);}
 public void OnPointerEnter(PointerEventData e){hovered=true;if(!RiftHotbar.Dragging)Show();}
 public void OnPointerExit(PointerEventData e){hovered=false;if(!focused)RiftAbilityTooltip.Hide(gameObject);}
 public void OnSelect(BaseEventData e){focused=true;if(!RiftHotbar.Dragging)Show();}
 public void OnDeselect(BaseEventData e){focused=false;if(!hovered)RiftAbilityTooltip.Hide(gameObject);}
 public void OnBeginDrag(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;var p=ClientFrontend.ClientPlayerInstance;var c=p?p.GetComponent<AbilityController>():null;int ability=RiftHotbar.Resolve(Slot);var definition=c?c.Definition(ability):null;RiftAbilityDrag.Begin(gameObject,ability,Slot,definition?definition.Icon:null,e);}
 public void OnDrag(PointerEventData e){if(RiftAbilityDrag.Owner==gameObject)RiftAbilityDrag.Move(e);}
 public void OnDrop(PointerEventData e){RiftAbilityDrag.Drop(e,Slot);}
 public void OnEndDrag(PointerEventData e){e.eligibleForClick=false;if(RiftAbilityDrag.Owner==gameObject)RiftAbilityDrag.Cancel();}
 public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Right){RiftHotbar.Clear(Slot);RiftAbilityTooltip.HideAll();}}
 void LateUpdate(){if(RiftAbilityDrag.Owner==gameObject){var canvas=GetComponentInParent<Canvas>();if(!canvas||!canvas.enabled)RiftAbilityDrag.Cancel();}}
 void OnApplicationFocus(bool focus){if(!focus&&RiftAbilityDrag.Owner==gameObject)RiftAbilityDrag.Cancel();}
 void OnDisable(){focused=hovered=false;if(RiftAbilityDrag.Owner==gameObject)RiftAbilityDrag.Cancel();RiftAbilityTooltip.Hide(gameObject);}
}
public sealed class RiftSpellDrag:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler {
 public int Ability;public Sprite Icon;
 public void OnBeginDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)RiftAbilityDrag.Begin(gameObject,Ability,-1,Icon,e);}
 public void OnDrag(PointerEventData e){if(RiftAbilityDrag.Owner==gameObject)RiftAbilityDrag.Move(e);}
 public void OnEndDrag(PointerEventData e){e.eligibleForClick=false;if(RiftAbilityDrag.Owner==gameObject)RiftAbilityDrag.Cancel();}
 void OnDisable(){if(RiftAbilityDrag.Owner==gameObject)RiftAbilityDrag.Cancel();}
}
public sealed class RiftSpellRemoveTarget:MonoBehaviour,IDropHandler {
 public void OnDrop(PointerEventData e){if(RiftAbilityDrag.Owner&&e.pointerDrag==RiftAbilityDrag.Owner&&RiftAbilityDrag.FromSlot>=0){RiftHotbar.Clear(RiftAbilityDrag.FromSlot);e.eligibleForClick=false;}}
}
}
