using System.Linq;
using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
using ROM.Progression;
using UnityEngine;
using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 string abilityTab="Skills";
 void TalentTree(Transform body){var player=ClientFrontend.ClientPlayerInstance;var c=player?player.GetComponent<AbilityController>():null;if(InGame&&c&&!c.Prototype){W.Label(body,"La primera rama de talentos está disponible para Warrior.",20,80,1600,80,26,W.Gold);return;}var state=RiftJournalView.Read(InGame?null:Roster.Active.Id);var t=WarriorProgressionDefinition.Load;var ranks=state.TalentRanks??new int[9];int level=t.Level(state.Experience),points=Mathf.Max(0,level-1-ranks.Sum());W.Label(body,"NIVEL "+level+" · "+points+" PUNTOS DISPONIBLES",20,65,1500,40,27,W.Gold);W.Label(body,"Cada nivel otorga un punto. Completá el talento anterior para avanzar. Cambios en refugio o fuera de combate.",20,110,1700,35,19,W.Muted);for(int col=0;col<3;col++){W.Label(body,t.Talents[col*3].Branch.ToUpperInvariant(),20+col*575,160,550,35,25,W.Gold);for(int row=0;row<3;row++){int index=col*3+row;var node=t.Talents[index];int rank=index<ranks.Length?ranks[index]:0;bool available=InGame&&c&&c.Prototype&&c.CanChangeBuild&&points>0&&level>=node.RequiredLevel&&rank<node.MaximumRank&&(node.Parent<0||ranks[node.Parent]>=t.Talents[node.Parent].MaximumRank);var button=W.Button(body,node.Name+"  "+rank+"/"+node.MaximumRank,20+col*575,205+row*130,545,46,()=>player.GetComponent<RiftJournalPlayer>().CmdTalent(index),available);button.interactable=available;W.Label(body,node.Description+"\nNivel "+node.RequiredLevel+(node.Parent>=0?" · Requiere "+t.Talents[node.Parent].Name+" 3/3":""),25+col*575,255+row*130,535,72,17,W.Muted);}}
 if(InGame)W.Button(body,"REINICIAR TALENTOS · GRATIS",20,620,545,48,()=>Dialog("Reiniciar talentos","Se devolverán todos los puntos gastados.",()=>player.GetComponent<RiftJournalPlayer>().CmdResetTalents()));}
}
}
