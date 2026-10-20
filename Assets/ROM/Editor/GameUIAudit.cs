using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using MultiFPS.UI;
namespace ROM.EditorTools { public static class GameUIAudit {
public static void Run(){EditorSceneManager.OpenScene("Assets/ROM/Scenes/MainMenu.unity");var manager=Object.FindFirstObjectByType<ClientInterfaceManager>(FindObjectsInactive.Include);var report=new List<string>();var refs=new HashSet<string>();var so=new SerializedObject(manager);var it=so.GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue is GameObject go){var path=AssetDatabase.GetAssetPath(go);if(!string.IsNullOrEmpty(path)&&path.Contains("UI")){refs.Add(path);report.Add(it.propertyPath+"="+path);}}
foreach(var path in refs){report.Add("PREFAB "+path);var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);foreach(var t in root.GetComponentsInChildren<RectTransform>(true)){string name=string.Join("/",Parents(t).Reverse());if(t.GetComponent<Graphic>()||t.GetComponent<Selectable>()||t.GetComponents<MonoBehaviour>().Any(c=>c&&c.GetType().Namespace!=null&&c.GetType().Namespace.StartsWith("MultiFPS")))report.Add(name+" rect="+t.anchoredPosition+" size="+t.sizeDelta+" ["+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name))+"] "+(t.GetComponent<Text>()?t.GetComponent<Text>().text:""));}}
report.Add("EMOTES");foreach(var path in AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/IDS ASSETS"}).Select(AssetDatabase.GUIDToAssetPath).Distinct())foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")&&(c.name.ToLower().Contains("salut")||c.name.ToLower().Contains("waving")||c.name.ToLower().Contains("flex"))))report.Add(clip.name+" human="+clip.humanMotion+" path="+path);
File.WriteAllLines("Documentation/game-ui-audit.txt",report);EditorApplication.Exit(0);}
static IEnumerable<string> Parents(Transform t){while(t){yield return t.name;t=t.parent;}}
}}
