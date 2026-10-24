using System;
using System.Collections.Generic;
using System.Linq;
using MultiFPS;
using UnityEngine;
using UnityEngine.UI;
using ROM.Progression;
using ROM.UI.Gameplay;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 readonly Dictionary<Text,(string label,string action)> boundLabels=new Dictionary<Text,(string,string)>();
 RiftQuestDefinition[] visibleQuests=Array.Empty<RiftQuestDefinition>();RectTransform journalScroll;
 Button BoundButton(Transform parent,string label,string action,float x,float y,float w,float h,Action clicked){var b=RiftWidgets.Button(parent,label+" ["+RiftControls.ActionLabel(action)+"]",x,y,w,h,clicked);var text=b.GetComponentInChildren<Text>();text.fontSize=h<45?17:20;text.resizeTextForBestFit=true;text.resizeTextMinSize=14;text.resizeTextMaxSize=text.fontSize;boundLabels[text]=(label,action);var tip=b.gameObject.AddComponent<RiftTooltipTrigger>();tip.Title=label;tip.Description="Clic para activar. El acceso se puede cambiar en Ajustes > Controles.";tip.ActionId=action;return b;}
 void RefreshBoundLabels(){foreach(var entry in boundLabels.ToArray()){if(!entry.Key){boundLabels.Remove(entry.Key);continue;}entry.Key.text=entry.Value.label+" ["+RiftControls.ActionLabel(entry.Value.action)+"]";}}
 void HandleInterfaceKeys(){
  if(!InGame){if(modal&&!RiftControls.Rebinding&&RiftControls.EscapeConsumedFrame!=Time.frameCount&&(RiftControls.Pressed("Pause")||(UnityEngine.InputSystem.Keyboard.current!=null&&UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame))){DismissModal();RiftControls.EscapeConsumedFrame=Time.frameCount;}return;}
  if(modal||RiftControls.Rebinding||RiftControls.TextEntryFocused||RiftControls.InterfaceConsumedFrame==Time.frameCount)return;
  if(Page==HubPage.Missions&&HandleJournalKeys())return;
  var actions=new[]{"Inventory","Character","Equipment","Abilities","Talents","Journal","Achievements","Friends","Party","Settings"};
  foreach(var action in actions)if(RiftControls.Pressed(action)){var page=action=="Inventory"?HubPage.Inventory:action=="Character"||action=="Equipment"?HubPage.Character:action=="Abilities"||action=="Talents"?HubPage.Abilities:action=="Journal"?HubPage.Missions:action=="Achievements"?HubPage.Progression:action=="Friends"||action=="Party"?HubPage.Social:HubPage.Settings;TogglePanel(action,page);return;}
 }
 public void TogglePanel(string action,HubPage page){
  if(page==HubPage.Character)page=HubPage.Inventory;
  RiftControls.InterfaceConsumedFrame=RiftInterfaceCursor.InventoryConsumedFrame=Time.frameCount;
  bool same=Page==page&&(action!="Abilities"||abilityTab=="Skills")&&(action!="Talents"||abilityTab=="Talents");
  if(same){ResumeRequested?.Invoke();return;}
  if(action=="Abilities")ShowSkills();else if(action=="Talents")ShowTalents();else if(action=="Settings")ShowGameSettings();else if(action=="Party")ShowPartyChat();else ShowGameplayPage(page);
 }
 public bool TryCloseTopLayer(){
  if(RiftInventoryPaperdoll.DragView){RiftInventoryPaperdoll.CancelDrag();return true;}
  if(RiftHotbar.Dragging){RiftAbilityDrag.Cancel();return true;}
  if(modal){DismissModal();return true;}
  if(modernMoreMenu&&modernMoreMenu.gameObject.activeSelf){modernMoreMenu.gameObject.SetActive(false);return true;}
  foreach(var dropdown in GetComponentsInChildren<Dropdown>()){var list=dropdown.transform.Find("Dropdown List");if(list&&list.gameObject.activeInHierarchy){dropdown.Hide();return true;}}
  if(InGame&&(Page==HubPage.Help||Page==HubPage.News)){Navigate(HubPage.Home);return true;}
  if(ArsenalPanel&&ArsenalPanel.gameObject.activeSelf){ShowGameSettings();return true;}
  return false;
 }
 void DismissModal(){if(!modal)return;DestroyImmediate(modal.gameObject);var cancelled=modalCancel;modalCancel=null;cancelled?.Invoke();}
 void SelectJournal(int direction){if(visibleQuests.Length==0)return;int index=Array.FindIndex(visibleQuests,q=>q.Id==journalSelection);index=(Mathf.Max(0,index)+direction+visibleQuests.Length)%visibleQuests.Length;journalSelection=visibleQuests[index].Id;Navigate(HubPage.Missions);if(journalScroll){var scroll=journalScroll.GetComponentInParent<ScrollRect>();if(scroll)scroll.verticalNormalizedPosition=visibleQuests.Length<=1?1:1-(float)index/(visibleQuests.Length-1);}}
 void ToggleJournalTracking(){var selected=visibleQuests.FirstOrDefault(q=>q.Id==journalSelection);var record=selected?JournalState.Quest(selected.Id):null;if(record==null||record.Status==QuestStatus.TurnedIn){Notify("Aceptá primero una misión activa para seguirla.");return;}RiftJournalView.Tracked=RiftJournalView.Tracked==selected.Id?"-":selected.Id;Navigate(HubPage.Missions);}
 bool HandleJournalKeys(){if(RiftControls.Pressed("JournalPrevious")){SelectJournal(-1);return true;}if(RiftControls.Pressed("JournalNext")){SelectJournal(1);return true;}if(RiftControls.Pressed("JournalTrack")){ToggleJournalTracking();return true;}return false;}
}
}
