using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using UnityEngine.Rendering;
namespace ROM.EditorTools {
 public static class RiftQuestMarkerImporter {
  const string Art="Assets/ROM/Art/QuestMarkers";const string Res="Assets/ROM/Resources/RiftQuestMarkers";
  [MenuItem("ROM/Art/Reimport quest marker prefabs")]public static void Run(){
   Directory.CreateDirectory(Res+"/World");Directory.CreateDirectory(Art+"/Materials");AssetDatabase.Refresh();
   foreach(var name in new[]{"Available","InProgress","TurnIn","Tracked"}){
    string png=Res+"/"+name+".png";var texture=(TextureImporter)AssetImporter.GetAtPath(png);texture.textureType=TextureImporterType.Sprite;texture.spriteImportMode=SpriteImportMode.Single;texture.alphaIsTransparency=true;texture.mipmapEnabled=false;texture.textureCompression=TextureImporterCompression.Uncompressed;texture.filterMode=FilterMode.Bilinear;texture.maxTextureSize=512;texture.spritePixelsPerUnit=256;texture.SaveAndReimport();
    var importer=(ModelImporter)AssetImporter.GetAtPath(Art+"/Models/"+name+".fbx");importer.importAnimation=false;importer.addCollider=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
    var source=AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath);var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name=name;go.transform.localRotation=Quaternion.Euler(0,180,0);
    try{
     foreach(var renderer in go.GetComponentsInChildren<Renderer>()){
      renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
      renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>Convert(m,name)).ToArray();
     }
     var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("Missing marker mesh: "+name);
     var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);
     // Normalize imported FBX axes/units into an editable 0.8 m-high billboard prefab.
     var root=new GameObject(name);go.transform.SetParent(root.transform,false);float scale=.8f/Mathf.Max(.001f,b.size.y);go.transform.localScale*=scale;go.transform.localPosition-=b.center*scale;
     PrefabUtility.SaveAsPrefabAsset(root,Res+"/World/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
    }finally{if(go)UnityEngine.Object.DestroyImmediate(go);}
   }AssetDatabase.SaveAssets();AssetDatabase.Refresh();
  }
  static Material Convert(Material source,string state){string id=source?source.name:"Default";string safe=string.Concat(id.Select(c=>char.IsLetterOrDigit(c)?c:'_'));string path=Art+"/Materials/"+state+"_"+safe+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
   m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=state+" "+id;bool rim=id.IndexOf("Dark",StringComparison.OrdinalIgnoreCase)>=0;Color c=rim?new Color(.075f,.05f,.035f):state=="InProgress"?new Color(.67f,.74f,.82f):state=="Tracked"?new Color(.5f,.055f,1):state=="TurnIn"?new Color(1,.62f,.055f):new Color(1,.42f,.015f);
   int facet=0;var last=id.LastOrDefault();if(char.IsDigit(last))facet=last-'0';c*=rim?1:Mathf.Lerp(.75f,1.12f,facet/3f);c.a=1;m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",rim?.8f:.45f);m.SetFloat("_Smoothness",.5f);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*(rim?0:state=="InProgress"?.12f:.4f));AssetDatabase.CreateAsset(m,path);return m;
  }
 }
}
