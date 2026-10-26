using System.Collections.Generic;using Mirror;using MultiFPS;using MultiFPS.Gameplay;using UnityEngine;
namespace ROM.Tutorial.Crilium {
// Collision walls are scene objects; this is a server fallback for teleport-based movement.
[DefaultExecutionOrder(-150)]public sealed class CriliumBounds:MonoBehaviour {
 public Rect[] Areas;public Vector3 Arrival;public float Inset=.75f;readonly Dictionary<uint,Vector3> lastValid=new Dictionary<uint,Vector3>();
 public bool Contains(Vector3 p){if(Areas==null||p.y<-3||p.y>35)return false;foreach(var area in Areas){var r=new Rect(area.x+Inset,area.y+Inset,area.width-Inset*2,area.height-Inset*2);if(r.Contains(new Vector2(p.x,p.z)))return true;}return false;}
 public int Recoveries{get;private set;}
 void LateUpdate(){if(!NetworkServer.active||!CriliumSession.Active)return;foreach(var player in GameManager.Players){if(!player||player.BOT||!player.MyCharacter||player.MyCharacter.Health.CurrentHealth<=0)continue;var actor=player.MyCharacter;var p=actor.transform.position;if(Contains(p)){lastValid[player.netId]=p;continue;}var safe=lastValid.TryGetValue(player.netId,out var prior)&&Contains(prior)?prior:Arrival;actor.ServerTeleport(safe+Vector3.up*.08f,actor.Input.LookY);var combat=player.GetComponent<MultiFPS.Gameplay.RiftCombat.AbilityController>();if(combat)combat.Cancel();Recoveries++;}}
 }
}
