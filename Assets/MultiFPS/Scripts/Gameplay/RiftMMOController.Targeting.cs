using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.UI;
using UnityEngine;
namespace MultiFPS.Gameplay {
 public enum RiftTargetDisposition {None,Self,Friendly,Hostile}
 public sealed partial class RiftMMOController {
  public RiftTargetDisposition Disposition=>SelectedWorld?RiftTargetDisposition.Friendly:!Selected?RiftTargetDisposition.None:Actor&&Selected==Actor.Health?RiftTargetDisposition.Self:Actor&&Selected.Team==Actor.Health.Team?RiftTargetDisposition.Friendly:RiftTargetDisposition.Hostile;
  public Health FindOffensiveTarget(float range,float arc=360){
   if(!Actor)return null;range=Mathf.Min(range,TargetRange);var camera=GameplayCamera._instance?GameplayCamera._instance.GetComponent<Camera>():null;var forward=Quaternion.Euler(0,Actor.Input.LookY,0)*Vector3.forward;Health best=null;int bestFront=2;float bestDistance=float.PositiveInfinity;
   foreach(var h in Health.All){if(!Valid(h,true)||h.gameObject.scene!=Actor.gameObject.scene||!CombatController.Visible(Actor,h))continue;var delta=h.transform.position-Actor.transform.position;float distance=delta.magnitude;if(distance>range)continue;delta.y=0;if(delta.sqrMagnitude>.001f&&Vector3.Angle(forward,delta)>arc*.5f)continue;if(camera){var point=camera.WorldToViewportPoint(h.transform.position+Vector3.up);if(point.z<=0||point.x<0||point.x>1||point.y<0||point.y>1)continue;}int front=Vector3.Dot(forward,delta)>=0?0:1;if(front<bestFront||front==bestFront&&(distance<bestDistance-.001f||Mathf.Abs(distance-bestDistance)<=.001f&&(!best||h.netId<best.netId))){best=h;bestFront=front;bestDistance=distance;}}
   return best;
  }
  public bool TryAutoTarget(AbilityDefinition ability,AbilityController combat){if(!ability||ability.Shape==AbilityShape.Self||!Actor||Valid(Selected,false)||ValidWorld(SelectedWorld))return false;float range=ability.Id=="warborn-leap"?12:combat?combat.AbilityRadius(combat.AbilityIndex(ability.Id),ability):ability.Radius;float arc=ability.Shape==AbilityShape.Arc&&combat?combat.AbilityArc(combat.AbilityIndex(ability.Id),ability):360;var target=FindOffensiveTarget(range,arc);if(!target)return false;Select(target);return true;}
  public bool TryTargetAttacker(Health attacker){if(!Actor||ClientFrontend.Hub||Actor.Health.CurrentHealth<=0||Valid(Selected,false)||ValidWorld(SelectedWorld)||!Valid(attacker,true)||attacker.gameObject.scene!=Actor.gameObject.scene||!CombatController.Visible(Actor,attacker))return false;Select(attacker);return true;}
 }
}
