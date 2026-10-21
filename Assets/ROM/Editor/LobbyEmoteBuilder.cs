using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
public static class LobbyEmoteBuilder {
const string Root="Assets/ROM/UI/Emotes";
public static void Build(){Directory.CreateDirectory(Root);AssetDatabase.Refresh();
var salute=Mixamo("Salute","Assets/IDS ASSETS/Characters/Anrak/TEMP_Anims/Salute.fbx");
var flex=Mixamo("Celebrate","Assets/IDS ASSETS/Characters/TEMP_Mixamo/DN/Dinork_S01_Mesh@Mutant Flexing Muscles.fbx");
var clips=ImportedAnimationAudit.Roots.SelectMany(r=>AssetDatabase.FindAssets("t:AnimationClip",new[]{r})).Select(AssetDatabase.GUIDToAssetPath).Distinct().SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")&&c.humanMotion).Distinct().ToArray();
AnimationClip Find(string name)=>clips.Single(c=>c.name==name);
var catalog=AssetDatabase.LoadAssetAtPath<FrontEndCatalog>("Assets/ROM/UI/FrontEnd/FrontEndCatalog.asset");
catalog.Emotes=new[]{new LobbyEmote{Name="SALUDO",Clip=salute,Seconds=salute.length+.2f},new LobbyEmote{Name="CELEBRAR",Clip=flex,Seconds=flex.length+.2f},new LobbyEmote{Name="MANOS ARRIBA",Clip=Find("SCR_Hands_Up_Hostage_Loop_IP"),Seconds=8},new LobbyEmote{Name="ZOMBI",Clip=Find("Zombie_Reach_1_Loop_IPC"),Seconds=7},new LobbyEmote{Name="SAMURÁI",Clip=Find("Anim_Samurai_Idle_v3"),Seconds=7},new LobbyEmote{Name="SUPLICAR",Clip=Find("SCR_Beg_For_Life_Standing_Loop_IP"),Seconds=7}};
string path=Root+"/LobbyEmotes.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
if(!controller){controller=AnimatorController.CreateAnimatorControllerAtPath(path);controller.AddParameter("playEmote",AnimatorControllerParameterType.Trigger);controller.AddParameter("stopEmote",AnimatorControllerParameterType.Trigger);var sm=controller.layers[0].stateMachine;var idle=sm.AddState("Idle");var emote=sm.AddState("Emote");idle.motion=Find("MOB1_Stand_Relaxed_Idle_v2_IPC");emote.motion=salute;sm.defaultState=idle;var play=idle.AddTransition(emote);play.hasExitTime=false;play.duration=.18f;play.hasFixedDuration=true;play.AddCondition(AnimatorConditionMode.If,0,"playEmote");var stop=emote.AddTransition(idle);stop.hasExitTime=false;stop.duration=.18f;stop.hasFixedDuration=true;stop.AddCondition(AnimatorConditionMode.If,0,"stopEmote");}
catalog.EmoteController=controller;catalog.EmotePlaceholder=salute;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();}
static AnimationClip Mixamo(string name,string source){string dest=Root+"/"+name+".fbx";if(!File.Exists(dest))AssetDatabase.CopyAsset(source,dest);var importer=(ModelImporter)AssetImporter.GetAtPath(dest);
{var root=AssetDatabase.LoadAssetAtPath<GameObject>(dest);var bones=root.GetComponentsInChildren<Transform>(true);var humans=new List<HumanBone>();
string[,] mapping={{"Hips","Hips"},{"Spine","Spine"},{"Chest","Spine1"},{"UpperChest","Spine2"},{"Neck","Neck"},{"Head","Head"},{"LeftShoulder","LeftShoulder"},{"RightShoulder","RightShoulder"},{"LeftUpperArm","LeftArm"},{"RightUpperArm","RightArm"},{"LeftLowerArm","LeftForeArm"},{"RightLowerArm","RightForeArm"},{"LeftHand","LeftHand"},{"RightHand","RightHand"},{"LeftUpperLeg","LeftUpLeg"},{"RightUpperLeg","RightUpLeg"},{"LeftLowerLeg","LeftLeg"},{"RightLowerLeg","RightLeg"},{"LeftFoot","LeftFoot"},{"RightFoot","RightFoot"},{"LeftToes","LeftToeBase"},{"RightToes","RightToeBase"}};
for(int i=0;i<mapping.GetLength(0);i++){var bone=bones.FirstOrDefault(b=>b.name=="mixamorig:"+mapping[i,1])??bones.FirstOrDefault(b=>b.name.EndsWith(":"+mapping[i,1]))??bones.FirstOrDefault(b=>b.name==mapping[i,1]);if(bone)humans.Add(new HumanBone{humanName=mapping[i,0],boneName=bone.name,limit=new HumanLimit{useDefaultValues=true}});}
importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.humanDescription=new HumanDescription{human=humans.ToArray(),skeleton=bones.Select(b=>new SkeletonBone{name=b.name,position=b.localPosition,rotation=b.localRotation,scale=b.localScale}).ToArray(),upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.02f,legStretch=.02f};importer.SaveAndReimport();}
var clip=AssetDatabase.LoadAllAssetsAtPath(dest).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));if(!clip||!clip.humanMotion)throw new Exception("Invalid emote "+name);return clip;
}
}}
