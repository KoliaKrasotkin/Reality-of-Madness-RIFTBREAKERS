using System;
using System.IO;
using System.Linq;
using MultiFPS;
using MultiFPS.Gameplay;
using UnityEditor;
using UnityEngine;

namespace ROM.UI.EditorTools
{
    public static class FrontEndAudit
    {
        public static void FromCommandLine()
        {
            var report="FRONTEND ASSETS\n";
            foreach(var guid in AssetDatabase.FindAssets("t:SkinContainer")){
                var skin=AssetDatabase.LoadAssetAtPath<SkinContainer>(AssetDatabase.GUIDToAssetPath(guid));
                var renderer=skin.Renderer;var root=renderer?renderer.transform.root.gameObject:null;
                report+=$"SKIN {skin.SkinName} asset={AssetDatabase.GetAssetPath(skin)} model={AssetDatabase.GetAssetPath(root)} mesh={skin.Mesh?.name} bounds={renderer?.bounds}\n";
                if(root)foreach(var a in root.GetComponentsInChildren<Animator>(true))report+=$"  Animator={a.name} avatar={a.avatar?.name} human={(a.avatar&&a.avatar.isHuman)} controller={AssetDatabase.GetAssetPath(a.runtimeAnimatorController)}\n";
                if(renderer)report+="  Bones: "+string.Join(",",renderer.bones.Where(b=>b).Select(b=>b.name))+"\n";
            }
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/MultiFPS"})){
                string path=AssetDatabase.GUIDToAssetPath(guid);var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(go&&go.GetComponent<Item>())report+=$"ITEM {path} renderers={go.GetComponentsInChildren<Renderer>(true).Length}\n";
            }
            var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MultiFPS/Art/Animations/Player/Soldier_EmptyHanded.controller");
            foreach(var clip in controller.animationClips.Distinct())report+=$"CLIP {clip.name} {AssetDatabase.GetAssetPath(clip)} human={clip.humanMotion} length={clip.length}\n";
            var character=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MultiFPS/Levels/Prefabs/Gameplay/MultiFPS_Character.prefab");
            foreach(var a in character.GetComponentsInChildren<Animator>(true))report+=$"GAMEPLAY ANIMATOR {a.name} avatar={a.avatar?.name} human={(a.avatar&&a.avatar.isHuman)}\n";
            foreach(var path in new[]{"White Knight/White_Knight_Rig.fbx","Dinork/Dinork_Skin_01_Rig.fbx","Redhead/Red_Head_Rig.fbx","PurpleHead/Purple_Head_rig.fbx","BlackPanter/Black_Panther_rig.fbx"}){
                var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/IDS ASSETS/Characters/"+path);if(!model)continue;
                report+=$"CANONICAL {path} scale={model.transform.localScale}\n";
                foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))report+=$"  {renderer.name} mesh={renderer.sharedMesh.name} vertices={renderer.sharedMesh.vertexCount} bounds={renderer.bounds} bones={renderer.bones.Length}\n";
                foreach(var bone in model.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="hips"||t.name=="upperLeg.L"||t.name=="head"||t.name=="Armature"))report+=$"  BONE {bone.name} pos={bone.position} scale={bone.lossyScale}\n";
            }
            File.WriteAllText("Documentation/frontend-assets-audit.txt",report);Debug.Log(report);EditorApplication.Exit(0);
        }
    }
}
