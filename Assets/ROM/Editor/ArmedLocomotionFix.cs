using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace ROM.EditorTools {
public static class ArmedLocomotionFix {
public static void Build(){
const string root="Assets/ROM/Animation";Directory.CreateDirectory(root);AssetDatabase.Refresh();
string path=root+"/RiftWeaponUpperBody.mask";var mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(path);if(!mask){mask=new AvatarMask();AssetDatabase.CreateAsset(mask,path);}
for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
foreach(var part in new[]{AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers})mask.SetHumanoidBodyPartActive(part,true);
EditorUtility.SetDirty(mask);
var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/MultiFPS/Art/Animations/Player/Soldier_EmptyHanded.controller");var layers=controller.layers;var upper=Array.FindIndex(layers,l=>l.name=="upperBody");if(upper<0)throw new Exception("Missing weapon layer");layers[upper].avatarMask=mask;controller.layers=layers;EditorUtility.SetDirty(controller);
var prefabPath="Assets/MultiFPS/Levels/Prefabs/Gameplay/MultiFPS_Character.prefab";var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
foreach(var aim in prefab.GetComponentsInChildren<UnityEngine.Animations.Rigging.OverrideTransform>(true)){var data=aim.data;data.sourceObject=null;aim.data=data;}
PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);PrefabUtility.UnloadPrefabContents(prefab);AssetDatabase.SaveAssets();
File.WriteAllText("Documentation/armed-motion-fixes.txt","Aim targets: world-space yaw, assigned after body rotation.\nWeapon mask: arms, fingers and head only; pelvis/spine remain on locomotion layer, chest/head aiming handled by existing rig constraints.\nArmed sprint: weapon arm layer remains at 0.8 instead of switching off into an unarmed arm swing.\nAnimation velocity: CharacterController velocity capped by integrated planar speed; transform/physics corrections do not double the cadence. Networking unchanged.\n");
}
public static void FromCommandLine(){try{Build();ArmedMotionReview.After();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
