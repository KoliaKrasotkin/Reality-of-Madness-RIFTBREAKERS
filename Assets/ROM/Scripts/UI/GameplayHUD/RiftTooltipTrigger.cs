using UnityEngine;
using UnityEngine.EventSystems;
namespace ROM.UI.Gameplay {
public sealed class RiftTooltipTrigger:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler {
 public int Ability=-1;public string Title,Description,ActionId;bool hovered,focused,pointerFocus;
 void Show(){if(Ability>=0)RiftAbilityTooltip.ShowAbility(gameObject,Ability);else RiftAbilityTooltip.ShowText(gameObject,Title,Description,ActionId);}
 public void OnPointerEnter(PointerEventData e){hovered=true;pointerFocus=true;Show();}
 public void OnPointerExit(PointerEventData e){hovered=false;if(!focused||pointerFocus)RiftAbilityTooltip.Hide(gameObject);}
 public void OnSelect(BaseEventData e){focused=true;pointerFocus=hovered||e is PointerEventData;Show();}
 public void OnDeselect(BaseEventData e){focused=false;if(!hovered)RiftAbilityTooltip.Hide(gameObject);}
 void OnDisable(){hovered=focused=pointerFocus=false;RiftAbilityTooltip.Hide(gameObject);}
}}