using System.Linq;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;
using UnityEngine.UI;
namespace ROM.UI.Sprint01 {
// Presentation snapshot of the current actor, never a second gameplay character.
public sealed class RiftWarriorShowcase:MonoBehaviour {
 PlayerInstance player;RiftModelView view;Text nameLabel,equipment;CharacterInstance lastActor;string lastGear;float next;
 public RiftModelView Preview=>view;
 public static RiftWarriorShowcase Create(Transform parent,PlayerInstance player){if(!player||!player.MyCharacter)return null;var root=RiftWidgets.Rect(parent,"Warrior inspection showcase",22,110,430,780);var component=root.gameObject.AddComponent<RiftWarriorShowcase>();component.player=player;component.view=RiftModelView.Create(root,0,105,430,650,null,false);var plate=RiftSkillStyle.Panel(root,"Identity backing",0,0,430,96,.35f);component.nameLabel=RiftWidgets.Label(plate,"",16,2,398,45,36,RiftSkillStyle.White);component.equipment=RiftWidgets.Label(plate,"",16,52,398,36,18,RiftSkillStyle.Muted);component.Refresh();return component;}
 void Update(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.25f;Refresh();}
 void Refresh(){if(!player||!player.MyCharacter||!view)return;var actor=player.MyCharacter;var c=player.GetComponent<AbilityController>();var gear=string.Join("|",actor.CharacterParent.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Select(r=>r.GetInstanceID().ToString()));if(lastActor!=actor||lastGear!=gear){view.ShowLiveCharacter(actor);lastActor=actor;lastGear=gear;}nameLabel.text=actor.Health.CharacterName;equipment.text="Warrior · Nivel "+(c?c.Level:1)+" · "+(c&&c.SwordAndShield?"Espada y escudo":"Hacha a dos manos");}
 void OnDisable(){if(view)view.SetRenderActive(false);}
 void OnEnable(){if(view)view.SetRenderActive(true);}
}
}
