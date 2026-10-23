using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ROM.UI.Gameplay;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 void ModernFriends(Transform body){
  var contacts=RiftSkillStyle.Panel(body,"Local contacts",0,0,580,650,.65f);
  RiftWidgets.Label(contacts,"CONTACTOS",24,18,530,40,24,RiftSkillStyle.White);
  RiftWidgets.Label(contacts,"Guardados en esta PC · sin conexión online",24,60,530,32,16,RiftSkillStyle.Muted);
  var input=RiftWidgets.Input(contacts,"Nombre del contacto local",24,112,350,v=>friendDraft=v);input.text=friendDraft;
  RiftSkillStyle.Button(contacts,"AGREGAR",390,112,166,48,()=>{var value=friendDraft;friendDraft="";Notify(Social.AddFriend(value));});
  RiftSkillStyle.Button(contacts,"CONVERSACIÓN DE GRUPO LOCAL",24,178,532,52,()=>{peer="party";Navigate(HubPage.Social);},peer=="party");
  var invitations=Social.Invitations.ToArray();int rows=Social.Friends.Count+invitations.Length;
  var list=RiftWidgets.ScrollArea(contacts,"Local contacts list",24,250,532,328,Mathf.Max(328,rows*70));int y=0;
  foreach(var f in Social.Friends){var contact=f;RiftSkillStyle.Button(list,contact.Name,0,y,332,56,()=>{peer=contact.Id;Navigate(HubPage.Social);},peer==contact.Id);RiftSkillStyle.Button(list,"INVITAR",344,y,164,56,()=>Notify(Social.Invite(contact.Id)));y+=70;}
  foreach(var invitation in invitations){var invite=invitation;var name=Social.Find(invite.From)?.Name??"Contacto local";RiftSkillStyle.Button(list,"ACEPTAR: "+name,0,y,508,56,()=>Notify(Social.AcceptInvite(invite.From)));y+=70;}
  if(rows==0)RiftWidgets.Label(list,"Todavía no hay contactos locales.",0,12,508,70,18,RiftSkillStyle.Muted);
  RiftWidgets.Label(contacts,"Los mensajes no se envían por internet.",24,603,532,28,16,RiftSkillStyle.Muted);
  var chat=RiftSkillStyle.Panel(body,"Local conversation",604,0,1168,650,.65f);
  string title=peer=="party"?"GRUPO LOCAL":Social.Find(peer)?.Name??"CONTACTO LOCAL";
  RiftWidgets.Label(chat,title,28,18,1108,44,26,RiftSkillStyle.White);
  RiftWidgets.Label(chat,"Perfil local: "+Social.Self.Name,28,64,1108,28,16,RiftSkillStyle.Muted);
  string transcript=string.Join("\n\n",Social.Conversation(peer).Select(m=>(Social.Find(m.From)?.Name??"Jugador")+": "+m.Text));
  var history=RiftWidgets.ScrollArea(chat,"Local message history",28,116,1112,398,398,true);
  var log=RiftWidgets.Label(history,string.IsNullOrEmpty(transcript)?"Sin mensajes en esta conversación local.":transcript,0,0,1084,398,21,RiftSkillStyle.White);log.alignment=TextAnchor.UpperLeft;log.rectTransform.sizeDelta=new Vector2(1084,Mathf.Max(398,log.preferredHeight+24));history.sizeDelta=log.rectTransform.sizeDelta;
  var draft=RiftWidgets.Input(chat,"Escribí un mensaje local",28,550,884,v=>chatDraft=v);draft.characterLimit=500;draft.text=chatDraft;
  RiftSkillStyle.Button(chat,"ENVIAR",932,550,208,48,()=>{var value=chatDraft;chatDraft="";Notify(Social.Send(peer,value));});
  RiftWidgets.Label(chat,"Solo se conserva en este equipo. No es un chat multiplayer.",28,610,1112,26,16,RiftSkillStyle.Muted);
  RiftPanelChrome.Apply(body);
 }
}
}
