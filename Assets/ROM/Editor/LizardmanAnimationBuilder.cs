using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MultiFPS.Gameplay;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace ROM.EditorTools {
public static class LizardmanAnimationBuilder {
const string Root="Assets/ROM/Characters/Lizardman";
static AnimationClip[] imported;
static AnimationClip Clip(string name){var c=imported.SingleOrDefault(c=>c.name==name);if(!c)throw new Exception("Missing imported humanoid clip "+name);return c;}
public static void Build(){
PrepareSamurai();
imported=ImportedAnimationAudit.Roots.SelectMany(r=>AssetDatabase.FindAssets("t:AnimationClip",new[]{r})).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("/Animation")).Distinct().SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")&&c.humanMotion).Distinct().ToArray();
var baseController=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/MultiFPS/Art/Animations/Player/Soldier_EmptyHanded.controller");
var originals=baseController.animationClips.Distinct().ToArray();
var report=new List<string>{"Only ids-character-1 (Lizardman / former Dinork). Imported humanoid library: "+imported.Length};
var root=PrefabUtility.LoadPrefabContents("Assets/ROM/UI/FrontEnd/Models/ids-character-1.prefab");
var set=root.GetComponent<CharacterAnimationSet>()??root.AddComponent<CharacterAnimationSet>();
set.Library=imported;set.HasOwnLocomotion=true;
set.Bindings=Bindings("MOB1",originals,report);set.PistolBindings=Bindings("W1",originals,report);set.RifleBindings=Bindings("W2",originals,report);
string previewPath=Root+"/LizardmanIdle.controller";var idle=AssetDatabase.LoadAssetAtPath<AnimatorController>(previewPath);
if(!idle){idle=AnimatorController.CreateAnimatorControllerAtPath(previewPath);idle.layers[0].stateMachine.AddState("Relaxed idle");}
idle.layers[0].stateMachine.states[0].state.motion=Clip("MOB1_Stand_Relaxed_Idle_v2_IPC");EditorUtility.SetDirty(idle);root.GetComponent<Animator>().runtimeAnimatorController=idle;
PrefabUtility.SaveAsPrefabAsset(root,"Assets/ROM/UI/FrontEnd/Models/ids-character-1.prefab");PrefabUtility.UnloadPrefabContents(root);AssetDatabase.SaveAssets();
File.WriteAllLines("Documentation/lizardman-animation-bindings.txt",report);
File.WriteAllText("Documentation/lizardman-integration.txt","Slot 1: Lizardman Berserker replaces Dinork. Native humanoid avatar, URP materials and exclusive imported animation set. See lizardman-animation-bindings.txt.\n");
ImportedAnimationAudit.Audit();
}
static CharacterAnimationSet.Binding[] Bindings(string family,AnimationClip[] originals,List<string> report){
string idle=family=="MOB1"?"MOB1_Stand_Relaxed_Idle_v2_IPC":family=="W1"?"W1_Stand_Aim_Idle_IPC":"W2_Stand_Aim_Idle_v2_IPC";
string jog=family=="MOB1"?"MOB1_Jog_F_Loop_IPC":family+"_Jog_Aim_F_Loop_IPC";
string crouch=family=="MOB1"?"MOB1_Crouch_Idle_V2_IPC":family=="W1"?"W1_Crouch_Aim_Idle_IPC":"W2_Crouch_Aim_Idle_v2_IPC";
string crouchWalk=family=="MOB1"?"MOB1_CrouchWalk_F_Loop_IPC":family+"_CrouchWalk_Aim_F_Loop_IPC";
var bindings=new List<CharacterAnimationSet.Binding>();
foreach(var original in originals){string n=original.name.ToLowerInvariant();string replacement=null;
// At the existing 4.2 m/s normal movement speed, a jog is the matching gait.
if(n.Contains("walk_forward")||n.Contains("walk_fl")||n.Contains("walk_fr")||n.Contains("emptyhanded_run"))replacement=jog;
else if(n.Contains("armed_crouch_forward")||n.Contains("armed_crouch_fl")||n.Contains("armed_crouch_fr"))replacement=crouchWalk;
else if(n.Contains("armed_crouch_idle"))replacement=crouch;
else if(n.EndsWith("emptyhanded")||n.EndsWith("emptyhanded_90")||n.EndsWith("emptyhanded_m90"))replacement=idle;
else if(n.Contains("emptyhanded_inair"))replacement=family=="MOB1"?"MOB1_Jog_F_Jump_RU_Air_IPC":family+"_Stand_Aim_Jump_Air_IPC";
else if(n.Contains("emptyhanded_turnleft"))replacement=family=="MOB1"?"MOB1_Stand_Rlx_Turn_In_Place_L_Loop_IPC":family+"_Stand_Aim_Turn_In_Place_L_Loop_IPC";
else if(n.Contains("crouch_turnleft"))replacement="MOB1_Crouch_Rlx_Turn_In_Place_L_Loop_IPC";
if(replacement!=null){var clip=Clip(replacement);bindings.Add(new CharacterAnimationSet.Binding{Original=original,Replacement=clip});report.Add(family+": "+original.name+" -> "+clip.name);}
}
if(bindings.Count<8)throw new Exception("Incomplete locomotion bindings for "+family+": "+bindings.Count);
return bindings.ToArray();
}
static void PrepareSamurai(){
string path="Assets/Raise Creation/Samurai Animation Pack/Models/Skeleton.fbx";
var importer=(ModelImporter)AssetImporter.GetAtPath(path);var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
if(importer.animationType!=ModelImporterAnimationType.Human){
var transforms=model.GetComponentsInChildren<Transform>(true);var human=new List<HumanBone>();
string[,] mapping={{"Hips","pelvis"},{"Spine","spine_01"},{"Chest","spine_03"},{"UpperChest","spine_05"},{"Neck","neck_01"},{"Head","head"},{"LeftShoulder","clavicle_l"},{"RightShoulder","clavicle_r"},{"LeftUpperArm","upperarm_l"},{"RightUpperArm","upperarm_r"},{"LeftLowerArm","lowerarm_l"},{"RightLowerArm","lowerarm_r"},{"LeftHand","hand_l"},{"RightHand","hand_r"},{"LeftUpperLeg","thigh_l"},{"RightUpperLeg","thigh_r"},{"LeftLowerLeg","calf_l"},{"RightLowerLeg","calf_r"},{"LeftFoot","foot_l"},{"RightFoot","foot_r"},{"LeftToes","ball_l"},{"RightToes","ball_r"}};
for(int i=0;i<mapping.GetLength(0);i++)if(transforms.Any(t=>t.name==mapping[i,1]))human.Add(new HumanBone{humanName=mapping[i,0],boneName=mapping[i,1],limit=new HumanLimit{useDefaultValues=true}});
foreach(string side in new[]{"Left","Right"})foreach(string finger in new[]{"Thumb","Index","Middle","Ring","Little"})for(int segment=1;segment<=3;segment++){
string bone=(finger=="Little"?"pinky":finger.ToLowerInvariant())+"_0"+segment+"_"+(side=="Left"?"l":"r");
if(transforms.Any(t=>t.name==bone))human.Add(new HumanBone{humanName=side+" "+finger+" "+new[]{"Proximal","Intermediate","Distal"}[segment-1],boneName=bone,limit=new HumanLimit{useDefaultValues=true}});
}
importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
importer.humanDescription=new HumanDescription{human=human.ToArray(),skeleton=transforms.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.02f,legStretch=.02f};importer.SaveAndReimport();}
var avatar=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Animator>().avatar;if(!avatar||!avatar.isHuman||!avatar.isValid)throw new Exception("Samurai humanoid conversion failed");
foreach(var p in AssetDatabase.FindAssets("t:Model",new[]{"Assets/Raise Creation/Samurai Animation Pack/Animations"}).Select(AssetDatabase.GUIDToAssetPath)){
var mi=(ModelImporter)AssetImporter.GetAtPath(p);if(mi.animationType==ModelImporterAnimationType.Human)continue;mi.animationType=ModelImporterAnimationType.Human;mi.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;mi.sourceAvatar=avatar;mi.SaveAndReimport();}
}
public static void Run(){try{Build();ArmedMotionReview.Lizardman();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
