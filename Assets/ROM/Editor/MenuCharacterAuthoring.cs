using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ROM.UI.Sprint01;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROM.EditorTools
{
    // Explicit, non-destructive authoring migration. Never runs at startup.
    public static class MenuCharacterAuthoring
    {
        public const string Folder = "Assets/ROM/UI/FrontEnd/Characters";
        const string CatalogPath = "Assets/ROM/UI/FrontEnd/FrontEndCatalog.asset";
        static readonly Dictionary<UnityEngine.Object,UnityEngine.Object> copies = new();

        [MenuItem("Rift/UI/Menu Characters/Select Catalog")]
        public static void SelectCatalog() => Selection.activeObject = AssetDatabase.LoadAssetAtPath<FrontEndCatalog>(CatalogPath);

        [MenuItem("Rift/UI/Menu Characters/Create Missing Presentation Prefabs")]
        public static void CreateMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            copies.Clear();
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<FrontEndCatalog>(CatalogPath);
            foreach (int skin in new[] {1,2,3})
            {
                var model = catalog.Characters.Single(x => x.SkinIndex == skin);
                string name = skin == 1 ? "WarriorMenu" : skin == 2 ? "PaladinMenu" : "MageMenu";
                string path = Folder + "/" + name + ".prefab";
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing) { model.Prefab = existing; continue; }
                var holder = new GameObject("Menu character authoring");
                try
                {
                    // Use the exact visual conversion already used by the menu.
                    typeof(RiftModelView).GetMethod("InstantiatePreview", BindingFlags.Static|BindingFlags.NonPublic)
                        .Invoke(null, new object[] {model.Prefab, holder.transform});
                    var visual = holder.transform.GetChild(0).gameObject;
                    if (PrefabUtility.IsPartOfPrefabInstance(visual)) PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    visual.name = name;
                    foreach (var t in visual.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    foreach (var script in visual.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
                    foreach (var c in visual.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
                    foreach (var b in visual.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(b);
                    foreach (var a in visual.GetComponentsInChildren<AudioSource>(true)) UnityEngine.Object.DestroyImmediate(a);
                    if (visual.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                        throw new InvalidOperationException(name + " still contains behaviour scripts; refusing unsafe copy.");
                    foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m ? CopyAsset(m, name) : null).ToArray();
                    foreach (var animator in visual.GetComponentsInChildren<Animator>(true))
                    {
                        animator.runtimeAnimatorController = CopyController(animator.runtimeAnimatorController, name);
                        animator.applyRootMotion = false;
                    }
                    model.Prefab = PrefabUtility.SaveAsPrefabAsset(visual, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(holder); }
            }
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            Validate();
        }
        static T CopyAsset<T>(T source, string prefix) where T : UnityEngine.Object
        {
            if (!source) return null;
            if (copies.TryGetValue(source, out var copy)) return (T)copy;
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string extension = source is Material ? ".mat" : Path.GetExtension(sourcePath);
            string target = AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + prefix + "_" + source.name.Replace('/', '_') + extension);
            if (source is Material) AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(source), target);
            else if (!AssetDatabase.CopyAsset(sourcePath, target)) throw new IOException("Cannot copy " + sourcePath);
            var result = AssetDatabase.LoadAssetAtPath<T>(target); copies[source] = result; return result;
        }
        static RuntimeAnimatorController CopyController(RuntimeAnimatorController source, string prefix)
        {
            if (!source) return null;
            var copy = CopyAsset(source, prefix);
            if (copy is AnimatorOverrideController overrides)
            {
                overrides.runtimeAnimatorController = CopyController(((AnimatorOverrideController)source).runtimeAnimatorController, prefix);
                EditorUtility.SetDirty(overrides);
            }
            return copy;
        }
        [MenuItem("Rift/UI/Menu Characters/Validate Presentation Prefabs")]
        public static void Validate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FrontEndCatalog>(CatalogPath);
            var results = new List<string>();
            foreach (int skin in new[] {1,2,3})
            {
                var model = catalog.Characters.Single(x => x.SkinIndex == skin);
                var prefab = model.Prefab;
                if (!AssetDatabase.GetAssetPath(prefab).StartsWith(Folder + "/")) throw new Exception("Preview is not isolated: " + skin);
                if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Any(script => !(script is MenuHandPose))) throw new Exception("Preview has gameplay scripts: " + skin);
                if (prefab.GetComponentsInChildren<Renderer>(true).Length == 0) throw new Exception("Missing renderers: " + skin);
                foreach (var a in prefab.GetComponentsInChildren<Animator>(true))
                    if (a.runtimeAnimatorController && !AssetDatabase.GetAssetPath(a.runtimeAnimatorController).StartsWith(Folder + "/")) throw new Exception("Shared controller: " + skin);
                foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                        if (m && !AssetDatabase.GetAssetPath(m).StartsWith(Folder + "/")) throw new Exception("Shared material: " + skin);
                results.Add("PASS skin " + skin + ": isolated prefab, controllers and materials; renderers present; no gameplay scripts");
            }
            Debug.Log(string.Join("\n", results));
        }
        public static void BuildBatch()
        {
            try { CreateMissing(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}

