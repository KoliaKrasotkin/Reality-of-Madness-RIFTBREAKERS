using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
[DefaultExecutionOrder(12500)]public sealed class CombatEquipment:MonoBehaviour {
 CharacterInstance actor;GameObject model;WeaponDefinition equipped;Transform socket;Renderer[] hidden;Item item;IDSGameplayPose pose;
 void Awake(){actor=GetComponent<CharacterInstance>();}
 void LateUpdate(){if(!actor||actor.GetComponent<WarriorAvatar>())return;var player=AbilityController.For(actor.Health);var enemy=GetComponent<RiftEnemyCombat>();WeaponDefinition desired=player&&player.Prototype?player.Specialization.Weapon:enemy&&enemy.ActiveCombat&&!enemy.Definition.Caster?enemy.MeleeWeapon:null;
  if(!desired){Clear();if(enemy&&enemy.ActiveCombat)HideHeld();return;}if(!pose)pose=actor.CharacterParent?actor.CharacterParent.GetComponentInChildren<IDSGameplayPose>(true):null;if(!pose||!pose.Target||!pose.Target.isHuman)return;
  var hand=pose.Target.GetBoneTransform(desired.Socket);if(equipped!=desired||socket!=hand){Clear();socket=hand;if(!socket)return;equipped=desired;model=Instantiate(desired.Model,socket);model.name="ROM equipped "+desired.Id;foreach(var c in model.GetComponentsInChildren<Collider>())c.enabled=false;model.transform.localPosition=desired.Position;model.transform.localRotation=Quaternion.Euler(desired.Rotation);model.transform.localScale=desired.Scale;}
  HideHeld();
 }
 void HideHeld(){var held=actor.CharacterItemManager.CurrentlyUsedItem;if(item!=held){Restore();item=held;if(held&&held.PresentationModel)hidden=held.PresentationModel.GetComponentsInChildren<Renderer>(true);}if(hidden!=null)foreach(var r in hidden)if(r)r.enabled=false;
 }
 void Restore(){if(hidden!=null)foreach(var r in hidden)if(r)r.enabled=true;hidden=null;item=null;}
 void Clear(){Restore();if(model)Destroy(model);model=null;equipped=null;socket=null;}
 void OnDisable()=>Clear();
}}
