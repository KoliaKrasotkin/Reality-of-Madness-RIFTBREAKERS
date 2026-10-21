using UnityEngine;
using UnityEngine.EventSystems;
namespace ROM.UI.Gameplay {
public sealed class RiftTooltipTrigger:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler {
 public int Ability=-1;public string Title,Description,ActionId;
 public void OnPointerEnter(PointerEventData e){if(Ability>=0)RiftAbilityTooltip.ShowAbility(gameObject,Ability);else RiftAbilityTooltip.ShowText(gameObject,Title,Description,ActionId);}
 public void OnPointerExit(PointerEventData e)=>RiftAbilityTooltip.Hide(gameObject);
 void OnDisable()=>RiftAbilityTooltip.Hide(gameObject);
}}
