using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ROM.EditorTools { public static class LizardmanAudit {
public static void Run(){var p="Assets/Novozhilov3D/Lizardman_Berserker/Base_Mesh/Lizardman_Berserker.fbx";var m=(ModelImporter)AssetImporter.GetAtPath(p);var go=AssetDatabase.LoadAssetAtPath<GameObject>(p);var a=go.GetComponent<Animator>();File.WriteAllLines("Documentation/lizardman-audit.txt",new[]{"Avatar "+a.avatar.isValid+" human "+a.avatar.isHuman}.Concat(m.defaultClipAnimations.Select(c=>$"TAKE {c.name} {c.firstFrame}..{c.lastFrame}")).Concat(AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Select(c=>$"CLIP {c.name} {c.length} loop {c.isLooping}")));EditorApplication.Exit(0);}
}}
