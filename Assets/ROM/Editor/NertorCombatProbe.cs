using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.UI;
using ROM.Horde;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
[InitializeOnLoad]public static class NertorCombatProbe {
 const string Key="ROM.NertorProbe";static int step,before,hp;static double began,next;static List<string> report;static AbilityController c;static Health dummy;static RiftEnemyCombat melee,caster;
 static NertorCombatProbe(){EditorApplication.playModeStateChanged+=Changed;}
 public static void Run(){NertorSliceBuilder.Build();SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);EditorApplication.isPlaying=true;}
 static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){step=0;began=EditorApplication.timeSinceStartup;next=began+3;report=new List<string>();SessionState.SetInt(Key+"Preference",PlayerPrefs.GetInt(AbilityController.Preference,0));SessionState.SetInt(Key+"Skin",UserSettings.CharacterSkinID);EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);PlayerPrefs.SetInt(AbilityController.Preference,SessionState.GetInt(Key+"Preference",0));UserSettings.CharacterSkinID=SessionState.GetInt(Key+"Skin",0);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}}
 static void Check(bool value,string message){if(!value)throw new Exception(message);report.Add("PASS "+message);}
 static void Tick(){try{
  double now=EditorApplication.timeSinceStartup;if(now-began>210)throw new Exception("Timeout step="+step);if(now<next)return;
  if(step==0){var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();if(!hub||!hub.Stage||!hub.Stage.Ready)return;Check(hub.CombatSpecies&&hub.CombatSpecies.Classes[0].Specializations.Length==2,"Main menu exposes SO-backed specializations");UserSettings.CharacterSkinID=1;PlayerPrefs.SetInt(AbilityController.Preference,2);hub.Creator.BotsToggle.isOn=false;hub.Creator.HostGameButton.onClick.Invoke();Advance(16);return;}
  var p=ClientFrontend.ClientPlayerInstance;var actor=ClientFrontend.OwnedCharacterInstance;if(!p||!actor)return;c=p.GetComponent<AbilityController>();
  switch(step){
  case 1:Check(c&&c.Prototype,"Nertor selected in real host flow");Check(!actor.FPP&&c.Specialization.Id=="ravager","Ravager in third person");Check(c.Ready.Count==7,"Synchronized cooldown slots");dummy=GameManager.Players.Where(x=>x&&x.MyCharacter&&x.MyCharacter.Health.TrainingTarget).Select(x=>x.MyCharacter.Health).FirstOrDefault();Check(dummy,"Training dummy available");actor.ServerTeleport(dummy.transform.position-Vector3.forward*2.5f,0);Advance(1);break;
  case 2:before=dummy.TrainingTotalDamage;Check(!c.ServerUse(2),"Insufficient Rage rejected");c.CmdUse(0);Advance(1.2);break;
  case 3:Check(dummy.TrainingTotalDamage>before,"Basic melee through Mirror command");Check(Mathf.Approximately(c.Resource,12),"Basic Rage generated once");before=dummy.TrainingTotalDamage;c.CmdUse(1);Advance(.1);break;
  case 4:Check(!c.ServerUse(1),"Duplicate action rejected");Advance(1.1);break;
  case 5:Check(dummy.TrainingTotalDamage>before&&Mathf.Approximately(c.Resource,30),"Cleaving Fury damage and Rage");before=dummy.TrainingTotalDamage;c.CmdUse(2);Advance(1.2);break;
  case 6:Check(dummy.TrainingTotalDamage>before&&Mathf.Approximately(c.Resource,5),"Earthbreaker damage and cost");c.CmdUse(0);Advance(1.1);break;
  case 7:c.CmdUse(3);Advance(1.1);break;
  case 8:var hud=MultiFPS.UI.HUD.UICharacter._instance;if(hud){var legacy=hud.transform.Find("GameplayHUD/Equipment");report.Add("Legacy equipment HUD active="+(legacy&&legacy.gameObject.activeInHierarchy));}Check(c.BuffUntil>NetworkTime.time&&c.OffensiveBonus>0&&Mathf.Approximately(c.Resource,2),"Ravager roar buff and cost");Check(!c.ServerUse(4)&&!c.ServerUse(99),"Unavailable and invalid skills rejected");Check(!c.ServerPortal(999),"Invalid portal rejected");Capture("training");actor.ServerTeleport(RiftPortal.All.First(x=>x.Definition.Id==0).transform.position,0);Advance(1);break;
  case 9:var portal=RiftPortal.All.First(x=>x.Definition.Id==0);Check(c.ServerPortal(0),"Validated portal use");Check(Vector3.Distance(actor.transform.position,portal.Destination.position)<1,"Actual teleport arrival");Check(Physics.Raycast(portal.Destination.position+Vector3.up*.5f,Vector3.down,2,GameManager.environmentLayer),"Portal arrival has collision floor");Check(!c.ServerPortal(0),"Repeated portal rejected");Capture("portal-arrival");var mode=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();var e=mode.Encounters.First(x=>x&&x.SpawnPoints.Length>=4&&!x.MiniBoss);actor.ServerTeleport(e.SpawnPoints[0].position+Vector3.forward*8,180);actor.Health.ServerSetVitals(10000,10000);Advance(9);break;
  case 10:melee=RiftEnemyCombat.Active.FirstOrDefault(x=>x&&x.ActiveCombat&&!x.Definition.Caster&&Vector3.Distance(x.transform.position,actor.transform.position)<140);caster=RiftEnemyCombat.Active.FirstOrDefault(x=>x&&x.ActiveCombat&&x.Definition.Caster&&Vector3.Distance(x.transform.position,actor.transform.position)<140);Check(melee&&caster,"Melee and caster encounter composition");Check(!melee.GetComponent<BotAI>().enabled,"Legacy gun AI disabled for new encounters");melee.Actor.Health.ServerSetVitals(1000,1000);caster.Actor.Health.ServerSetVitals(1000,1000);actor.ServerTeleport(melee.transform.position-Vector3.forward*2,0);hp=melee.Actor.Health.CurrentHealth;c.CmdUse(0);Advance(1.1);break;
  case 11:Check(melee.Actor.Health.CurrentHealth<hp,"Melee damages live enemy");Check(melee.Target==actor.Health,"Damage generates threat");Check(!c.ServerSelect(0),"Cannot respec in hostile area");actor.ServerTeleport(caster.transform.position+Vector3.forward*10,180);hp=actor.Health.CurrentHealth;Advance(8);break;
  case 12:Check(actor.Health.CurrentHealth<hp,"Caster authoritative attack deals damage");Capture("encounter");var heart=UnityEngine.Object.FindFirstObjectByType<FracturedHeart>();Check(heart&&heart.WorldLight&&heart.Crystal,"Fractured Heart visible source configured");heart.Transition(1);Check(heart.Intensity==1,"Energy transition API");actor.ServerTeleport(RiftPortal.All.First(x=>x.Definition.Id==0).transform.position,0);Advance(1);break;
  case 13:c.Cancel();Check(c.ServerSelect(0),"Guardian selection in refuge");Check(c.Specialization.Id=="guardian"&&actor.Health.MaxHealth==220,"Guardian profile");actor.ServerTeleport(dummy.transform.position-Vector3.forward*2.5f,0);c.Resource=40;c.Ready[3]=0;c.CmdUse(3);Advance(1.1);break;
  case 14:Check(c.BuffUntil>NetworkTime.time&&c.BuffReduction>.3f,"Guardian defensive buff");Check(CombatController.Resolve(actor.Health,melee.Actor.Health,100)==48,"Guardian armor plus defensive mitigation");Check(c.ServerSelect(-1),"Can restore previous class");Check(!c.Prototype,"Existing classes preserved");p.PlayerInfo.CharacterSkinID=0;Check(!c.ServerSelect(1),"Species skin restriction enforced on server");p.PlayerInfo.CharacterSkinID=1;Finish(true);break;
  }
 }catch(Exception e){Debug.LogException(e);report.Add(e.ToString());Finish(false);}}
 static void Advance(double delay){step++;next=EditorApplication.timeSinceStartup+delay;}
 static void Capture(string name){typeof(GameUIProbe).GetMethod("CaptureGame",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"Documentation/combat-slice/"+name+".png"});}
 static void Finish(bool ok){File.WriteAllLines("Documentation/combat-slice/play-validation.txt",report);SessionState.SetInt(Key+"Result",ok?0:1);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
}}
