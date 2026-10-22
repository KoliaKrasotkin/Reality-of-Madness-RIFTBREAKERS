using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 string abilityTab="Skills";
 void TalentTree(Transform body){var player=ClientFrontend.ClientPlayerInstance;var combat=player?player.GetComponent<AbilityController>():null;if(!player||!combat||!combat.Prototype){RiftWidgets.Label(body,"Los talentos están disponibles para Warrior en juego.",0,30,676,100,24);return;}body.gameObject.AddComponent<RiftTalentTreeView>().Build(player,combat,this);}
}
}
