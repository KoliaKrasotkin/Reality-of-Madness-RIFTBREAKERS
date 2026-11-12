using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using MultiFPS.Gameplay.RiftCombat;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 [Header("Persistent front-end presentation (MainMenu only)")]
 public RiftFrontEndView FrontEnd;
 bool BuildAuthoredFrontEnd(){
  if(InGame||!FrontEnd)return false;
  Initialize();boundLabels.Clear();nav.Clear();productView=null;Design=FrontEnd.Design;content=FrontEnd.LegacyContent;toast=FrontEnd.Toast;
  FrontEnd.Bind(this);Navigate(HubPage.Home);Fit();return true;
 }
 bool NavigateAuthoredHome(HubPage page){
  if(InGame||!FrontEnd)return false;
  FrontEnd.HideScreens();content.gameObject.SetActive(true);
  if(page!=HubPage.Home)return false;
  Page=page;Clear(content);FrontEnd.Bind(this);
  if(homePanel=="CreateCharacter")FrontEnd.ShowCreation(creationPaladin,creationMage);
  else if(homePanel=="Characters"||homePanel=="Class")FrontEnd.ShowSelection(Roster.Characters,Roster.Active,!string.IsNullOrEmpty(Roster.LastDeletedId)&&!Roster.Characters.Any(c=>c.Id==Roster.LastDeletedId),c=>ClassName(c.ClassKey));
  else if(string.IsNullOrEmpty(homePanel)||homePanel=="Menu"){
   FrontEnd.ShowHome(Roster.Active);
   if(homePanel=="Menu"){
    var drawer=Drawer("MenuDrawer","MÁS OPCIONES",0,530,650);string[] titles={"LOGROS","TIENDA","CASA DE SUBASTAS","AMIGOS"};HubPage[] pages={HubPage.Progression,HubPage.Marketplace,HubPage.Auction,HubPage.Social};
    for(int i=0;i<titles.Length;i++){var target=pages[i];RiftWidgets.Button(drawer,titles[i],24,88+i*72,482,58,()=>{shopTab=target==HubPage.Auction?"Subastas":"Marketplace";Navigate(target);});}
   }
  }else return false;
  var first=FrontEnd.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.IsInteractable());if(first&&EventSystem.current)EventSystem.current.SetSelectedGameObject(first.gameObject);
  ROM.UI.Gameplay.RiftAbilityTooltip.HideAll();return true;
 }
 public void FrontEndAction(string action){StartCoroutine(DeferMainMenuAction(()=>{
  switch(action){
   case "Play":Deploy();break;case "Home":homePanel="";Navigate(HubPage.Home);break;
   case "Characters":homePanel="Characters";Navigate(HubPage.Home);break;
   case "Create":creationPaladin=false;creationMage=false;homePanel="CreateCharacter";Navigate(HubPage.Home);break;
   case "Warrior":creationPaladin=false;creationMage=false;Navigate(HubPage.Home);break;case "Mage":creationMage=true;creationPaladin=false;Navigate(HubPage.Home);break;case "Paladin":creationMage=false;creationPaladin=true;Navigate(HubPage.Home);break;
   case "Settings":Navigate(HubPage.Settings);break;case "Exit":MenuExit();break;case "Connect":Connection();break;case "More":ToggleHomePanel("Menu");break;
   case "Crelium":if(RequireWarrior()&&Roster.Active.ClassKey=="warrior")ROM.Tutorial.Crilium.CriliumSession.Enter(roster);break;
   case "Delete":ConfirmDeleteCharacter();break;case "Undo":if(Roster.RestoreDeleted(Roster.LastDeletedId)){ApplyActiveCharacter();SocialChanged();}break;
   case "Continue":if(!RequireWarrior())return;if(Roster.Active.ClassKey!="warrior"||Roster.Active.CreliumCompleted)Deploy();else ROM.Tutorial.Crilium.CriliumSession.Enter(roster);break;
   case "Confirm":bool mage=creationMage&&MagePrototypeSettings.Available;bool paladin=!mage&&creationPaladin&&PaladinPrototypeSettings.Available;int skin=mage?MagePrototypeSettings.Skin:paladin?PaladinPrototypeSettings.Skin:1;string error=Roster.Create(RiftSkinIdentity.Name(skin),mage?MagePrototypeSettings.ClassKey:paladin?PaladinPrototypeSettings.ClassKey:"warrior",skin);if(error!=null){Notify(error);return;}ApplyActiveCharacter();homePanel="Characters";SocialChanged();Notify("Personaje creado: "+Roster.Active.DisplayName);break;
  }
 }));}
 public void FrontEndSelect(string id){StartCoroutine(DeferMainMenuAction(()=>{if(Roster.Select(id)){ApplyActiveCharacter();SocialChanged();}}));}
 public void FrontEndEquipment(RiftModelView preview,RiftPlayerCharacter record){ApplyCharacterPreviewEquipment(preview,record);}
}
}
