using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Mirror;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
public static class PlayableClassesValidation {
 static readonly List<string> results=new List<string>();
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);results.Add("PASS "+name);}
 public static void Run(){int code=0;string account="__playable_classes_validation_"+Guid.NewGuid().ToString("N");
 try {
 Check(PaladinPrototypeSettings.Available,"Paladin configured");Check(MagePrototypeSettings.Available,"Mage configured");
 var catalog=AssetDatabase.LoadAssetAtPath<FrontEndCatalog>("Assets/ROM/UI/FrontEnd/FrontEndCatalog.asset");
 foreach(int skin in new[]{1,2,3})Check(catalog.Characters.Count(x=>x.SkinIndex==skin)==1,"Unique preview skin "+skin);
 var mage=MagePrototypeSettings.Load;
 Check(mage.Species.AllowsSkin(3)&&!mage.Species.AllowsSkin(1)&&!mage.Species.AllowsSkin(2),"Mage species excludes other classes");
 Check(mage.Species.Classes.Length==1&&mage.Species.Classes[0].Abilities.Length==0,"Mage basic prototype does not inherit warrior abilities");
 Check(mage.AutoAttack.Range==25,"Mage ranged attack profile");
 uint previous=0;
 foreach(var prefab in new[]{PaladinPrototypeSettings.Load.PlayerPrefab,mage.PlayerPrefab}){
 Check(prefab.GetComponent<CharacterInstance>()&&prefab.GetComponent<WarriorAvatar>()&&prefab.GetComponent<CharacterController>(),prefab.name+" native locomotion components");
 var net=prefab.GetComponent<NetworkIdentity>();Check(net&&net.assetId!=0&&net.assetId!=previous,prefab.name+" unique network asset");previous=net.assetId;
 var skin=prefab.GetComponent<CharacterSkinController>();Check(skin&&skin.Skins.Length>0,prefab.name+" skin configuration");
 var slot=skin.Skins[skin.InitialIndex];Check(slot.Definition&&slot.ExistingAnimator&&slot.ExistingVisualRoot,prefab.name+" current model references");
 Check(slot.ExistingAnimator.avatar&&slot.ExistingAnimator.avatar.isValid&&slot.ExistingAnimator.avatar.isHuman,prefab.name+" valid humanoid avatar");
 Check(slot.Definition.Controller,prefab.name+" animation controller");
 Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab)==0,prefab.name+" root scripts valid");
 }
 var roster=new LocalCharacterRoster(account,"Test",1,"warrior");
 Check(roster.Create("ignored",PaladinPrototypeSettings.ClassKey,2)==null&&roster.Active.Available,"Create/select paladin");string paladin=roster.Active.Id;
 Check(roster.Create("ignored",MagePrototypeSettings.ClassKey,3)==null&&roster.Active.Available,"Create/select mage");string mageId=roster.Active.Id;
 Check(roster.Create("ignored",MagePrototypeSettings.ClassKey,1)!=null,"Reject mismatched mage skin");
 roster.SetSkin(2);Check(roster.Active.Skin==3,"Cannot convert mage into paladin");
 Check(roster.Select(paladin)&&roster.Select(mageId),"Repeated class selection");
 var reloaded=new LocalCharacterRoster(account,"Test",1,"warrior");Check(reloaded.Active.Id==mageId&&reloaded.Active.Available,"Mage identity survives reload");
 Check(!RiftSkinIdentity.Playable(0)&&!RiftSkinIdentity.Playable(4),"Legacy models remain unavailable");
 } catch(Exception e){code=1;results.Add("FAIL "+e);Debug.LogException(e);}
 finally {PlayerPrefs.DeleteKey("ROM.CharacterRoster.v1."+account);PlayerPrefs.Save();Directory.CreateDirectory("Documentation/playable-classes");File.WriteAllLines("Documentation/playable-classes/validation.txt",results);Debug.Log(string.Join("\n",results));EditorApplication.Exit(code);}
 }
}
}
