using System.Linq;using Mirror;using MultiFPS;using MultiFPS.Gameplay;using ROM.Horde;using UnityEngine;
namespace ROM.Progression {
public sealed partial class RiftJournalPlayer {
 [Command]public void CmdReturnToCrystal(){
  var actor=Player?Player.MyCharacter:null;if(!actor||!actor.Health||actor.Health.CurrentHealth<=0){Report("El rescate está disponible cuando estás vivo.");return;}
  var crystals=FindObjectsByType<RiftCheckpoint>(FindObjectsSortMode.None).Where(c=>c&&c.isActiveAndEnabled&&c.gameObject.scene==actor.gameObject.scene).OrderBy(c=>(c.transform.position-actor.transform.position).sqrMagnitude);
  foreach(var crystal in crystals){var anchor=crystal.Respawn?crystal.Respawn:crystal.transform;if(!TryCrystalRescuePosition(actor.GetComponent<CharacterController>(),anchor.position,GameManager.environmentLayer,out var point))continue;actor.ServerTeleport(point,anchor.eulerAngles.y);Report("Regresaste al cristal: "+crystal.Title);return;}
  Report("No hay un lugar libre y seguro junto a un cristal de esta zona.");
 }
 // Server chooses the anchor and validates the whole standing capsule; no client position is accepted.
 public static bool TryCrystalRescuePosition(CharacterController body,Vector3 anchor,int groundMask,out Vector3 point){
  point=anchor;if(!body||groundMask==0)return false;
  var scale=body.transform.lossyScale;float radius=Mathf.Max(.15f,body.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z)));
  float height=Mathf.Max(radius*2,body.height*Mathf.Abs(scale.y));var center=Vector3.Scale(body.center,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)));
  float pad=Mathf.Max(.035f,body.skinWidth);var physics=body.gameObject.scene.GetPhysicsScene();var hits=new Collider[48];
  for(int ring=0;ring<=8;ring++){int count=ring==0?1:16;float distance=ring==0?0:radius+.5f+ring*.75f;
   for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/count;var trial=anchor+new Vector3(Mathf.Cos(angle)*distance,0,Mathf.Sin(angle)*distance);
    if(!physics.Raycast(trial+Vector3.up*6,Vector3.down,out var ground,14,groundMask,QueryTriggerInteraction.Ignore)||ground.point.y>anchor.y+.75f||ground.point.y<anchor.y-4||ground.normal.y<Mathf.Cos(Mathf.Min(45,body.slopeLimit)*Mathf.Deg2Rad))continue;
    var root=ground.point+Vector3.up*(height*.5f-center.y+pad*2+.01f);var middle=root+center;var low=middle-Vector3.up*(height*.5f-radius);var high=middle+Vector3.up*(height*.5f-radius);
    int overlaps=physics.OverlapCapsule(low,high,radius+pad,hits,~0,QueryTriggerInteraction.Ignore);if(overlaps==hits.Length)continue;bool blocked=false;
    for(int n=0;n<overlaps;n++)if(hits[n]&&hits[n]!=body&&!hits[n].transform.IsChildOf(body.transform)&&!body.transform.IsChildOf(hits[n].transform)){blocked=true;break;}if(blocked)continue;
    bool supported=true;for(int side=0;side<4;side++){var offset=(side==0?Vector3.right:side==1?Vector3.left:side==2?Vector3.forward:Vector3.back)*radius*.7f;if(!physics.Raycast(ground.point+offset+Vector3.up*.4f,Vector3.down,out var support,1,groundMask,QueryTriggerInteraction.Ignore)||Mathf.Abs(support.point.y-ground.point.y)>.25f){supported=false;break;}}
    if(supported){point=root;return true;}
   }
  }return false;
 }
}
}
