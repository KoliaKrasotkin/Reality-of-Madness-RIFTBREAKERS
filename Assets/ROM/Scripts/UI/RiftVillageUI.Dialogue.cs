using System.Collections.Generic;using System.Linq;using UnityEngine;using UnityEngine.UI;using ROM.Horde;using ROM.UI.Gameplay;using MultiFPS;using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.UI.Sprint01 {public sealed partial class RiftVillageUI {
 const float DialogueWidth=1160,DialogueHeight=158;string dialogueQuest;
 public RectTransform DialoguePanel=>dialog;
 public bool CinematicDialogue=>TalkingTo&&dialog&&dialog.gameObject.activeSelf&&!vendorWindow;
 struct HudState {public float Alpha;public bool Blocks,Interactable;}
 readonly Dictionary<CanvasGroup,HudState> dialogueHidden=new Dictionary<CanvasGroup,HudState>();RiftGameplayHUD dialogueHud;
 void HideForDialogue(Transform target){if(!target)return;var group=target.GetComponent<CanvasGroup>();if(!group)group=target.gameObject.AddComponent<CanvasGroup>();if(!dialogueHidden.ContainsKey(group))dialogueHidden.Add(group,new HudState{Alpha=group.alpha,Blocks=group.blocksRaycasts,Interactable=group.interactable});group.alpha=0;group.blocksRaycasts=group.interactable=false;}
 void RefreshDialogueHUD(){if(!CinematicDialogue){RestoreDialogueHUD();return;}if(!dialogueHud)dialogueHud=FindFirstObjectByType<RiftGameplayHUD>();if(!dialogueHud)return;
  foreach(string name in new[]{"Circular minimap","Tracked quest","Integrated action dock","Gameplay shortcuts","General menu shortcuts","Detached lower right micromenu","Always accessible inventory","Screen bottom experience","XP reading background","Session proximity chat","Authorized chat bubbles","Quest destination marker"})HideForDialogue(dialogueHud.Design.Find(name));
  if(dialogueHud.ExperienceLabel)HideForDialogue(dialogueHud.ExperienceLabel.transform);if(dialogueHud.HoverPanel)HideForDialogue(dialogueHud.HoverPanel);
  foreach(var marker in dialogueHud.GetComponentsInChildren<RiftHUDTargetMarker>(true))HideForDialogue(marker.transform);
 }
 void RestoreDialogueHUD(){foreach(var entry in dialogueHidden)if(entry.Key){entry.Key.alpha=entry.Value.Alpha;entry.Key.blocksRaycasts=entry.Value.Blocks;entry.Key.interactable=entry.Value.Interactable;}dialogueHidden.Clear();}
 void LayoutDialogue(){if(!dialog||!dialog.gameObject.activeSelf)return;dialog.localScale=Vector3.one;dialog.anchorMin=dialog.anchorMax=dialog.pivot=new Vector2(.5f,0);dialog.anchoredPosition=new Vector2(0,8);var safe=Screen.safeArea;var bounds=RiftHUDPanelDock.ScreenBounds(dialog);float scale=Mathf.Min(1,(safe.width-32)/Mathf.Max(1,bounds.width));float ceiling=safe.yMax;
  if(dialogueHud){var player=dialogueHud.Design.Find("Player vitals") as RectTransform;if(player)ceiling=Mathf.Min(ceiling,RiftHUDPanelDock.ScreenBounds(player).yMin-8);var target=dialogueHud.TargetPanel;if(target&&target.gameObject.activeInHierarchy)ceiling=Mathf.Min(ceiling,RiftHUDPanelDock.ScreenBounds(target).yMin-8);}
  scale=Mathf.Min(scale,Mathf.Max(1,ceiling-safe.yMin-8)/Mathf.Max(1,bounds.height));dialog.localScale=Vector3.one*scale;bounds=RiftHUDPanelDock.ScreenBounds(dialog);RiftHUDPanelDock.MoveScreenCenter(dialog,new Vector2(safe.center.x,safe.yMin+8+bounds.height*.5f));
 }
 void ClearDialogueContent(){foreach(string name in new[]{"Dialogue content","Dialogue quest title","Dialogue actions"}){var old=dialog.Find(name);if(old)DestroyImmediate(old.gameObject);}}
 Button DialogueAction(RectTransform actions,string text,float x,float y,float width,System.Action action){var button=W.Button(actions,text,x,y,width,28,action);button.GetComponentInChildren<Text>().fontSize=15;return button;}
 RectTransform DialogueActions()=>W.Rect(dialog,"Dialogue actions",870,46,270,104);
 void DialogueBody(string text){var content=W.ScrollArea(dialog,"Dialogue content",20,48,820,80,80);speech=W.Label(content,text,0,0,804,80,17);speech.alignment=TextAnchor.UpperLeft;float height=Mathf.Max(80,speech.preferredHeight+6);speech.rectTransform.sizeDelta=new Vector2(804,height);content.sizeDelta=new Vector2(820,height);}
 void ShowDialogueGreeting(RiftVillageNpc npc,bool advice=false){if(!dialog||TalkingTo!=npc)return;dialogueQuest=null;ClearDialogueContent();DialogueBody(advice?npc.Advice:npc.Greeting);var actions=DialogueActions();DialogueAction(actions,"MISIONES",0,0,270,()=>ShowQuests(npc));DialogueAction(actions,"HABLAR DEL REFUGIO",0,34,270,()=>ShowDialogueGreeting(npc,true));if(npc.Trades)DialogueAction(actions,"COMERCIAR",0,68,270,()=>ShowGoods(npc));RiftPanelChrome.Apply(dialog);}
 void ShowDialogueQuest(RiftVillageNpc npc,ROM.Progression.RiftQuestDefinition[] definitions,int index){
  var q=definitions[index];dialogueQuest=q.Id;var state=ROM.Progression.RiftJournalView.Read();var record=state.Quest(q.Id);ClearDialogueContent();
  var title=W.Label(dialog,q.Title+"  ·  "+q.RewardExperience+" XP / "+q.Gold+" oro",20,44,820,24,17,W.White,true);title.name="Dialogue quest title";
  var content=W.ScrollArea(dialog,"Dialogue content",20,74,820,54,54);var details=W.Label(content,record==null?q.Description:ROM.Progression.RiftJournalView.Objectives(q,record),0,0,804,54,16);details.alignment=TextAnchor.UpperLeft;float height=Mathf.Max(54,details.preferredHeight+6);details.rectTransform.sizeDelta=new Vector2(804,height);content.sizeDelta=new Vector2(820,height);
  var actions=DialogueActions();var player=MultiFPS.UI.ClientFrontend.ClientPlayerInstance;var journal=player?player.GetComponent<ROM.Progression.RiftJournalPlayer>():null;
  if(journal&&record==null)DialogueAction(actions,"ACEPTAR MISIÓN",0,0,270,()=>journal.CmdAccept(q.Id,npc.Id));else if(journal&&record.Status==ROM.Progression.QuestStatus.Ready)DialogueAction(actions,"ENTREGAR MISIÓN",0,0,270,()=>journal.CmdTurnIn(q.Id,npc.Id));else W.Label(actions,"EN PROGRESO",0,0,270,28,15,W.Muted);
  var prev=DialogueAction(actions,"‹",0,34,50,()=>ShowDialogueQuest(npc,definitions,(index+definitions.Length-1)%definitions.Length));var next=DialogueAction(actions,"›",220,34,50,()=>ShowDialogueQuest(npc,definitions,(index+1)%definitions.Length));prev.interactable=next.interactable=definitions.Length>1;W.Label(actions,(index+1)+" / "+definitions.Length,56,34,158,28,14,W.Muted).alignment=TextAnchor.MiddleCenter;
  DialogueAction(actions,"VOLVER",0,68,128,()=>ShowDialogueGreeting(npc));DialogueAction(actions,"DIARIO",142,68,128,()=>{Close();var hud=FindFirstObjectByType<RiftGameplayHUD>();if(hud){hud.Open(HubPage.Missions);var pause=MultiFPS.UI.UIPauseMenu._instance;var hub=pause?pause.pauseMenuOverlay.GetComponentInChildren<RiftHub>():null;if(hub)hub.ShowQuest(q.Id);}});RiftPanelChrome.Apply(dialog);
 }
}}
