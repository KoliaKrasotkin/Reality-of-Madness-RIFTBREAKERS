using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ROM.UI.Sprint01;
namespace ROM.UI.EditorTools {
public static class CharacterPresentationUpgrade {
const string Root="Assets/ROM/UI/FrontEnd/CharacterPresentation";
public static void Build(){
Directory.CreateDirectory(Root);Directory.CreateDirectory(Root+"/Maps");AssetDatabase.Refresh();
var catalog=AssetDatabase.LoadAssetAtPath<FrontEndCatalog>("Assets/ROM/UI/FrontEnd/FrontEndCatalog.asset");var report=new List<string>();
foreach(var model in catalog.Characters){if(model.Name=="Lizardman Berserker")continue;var prefab=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(model.Prefab));
foreach(var r in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){
var old=mats[i];string basePath=old&&old.HasProperty("_BaseMap")?AssetDatabase.GetAssetPath(old.GetTexture("_BaseMap")):"";
string prefix="",albedo="",normal="",packed="",metal="",rough="",ao="";
if(model.SkinIndex==0){prefix="Assets/IDS ASSETS/Characters/BlackPanter/Textures/T_BlackPanther";albedo=prefix+"_BC.tga";normal=prefix+"_N.tga";metal=prefix+"_M.tga";rough=prefix+"_R.tga";}
if(model.SkinIndex==4){prefix="Assets/IDS ASSETS/Characters/White Knight/Textures/T_WhiteNight";albedo=prefix+"_BC.tga";normal=prefix+"_N.tga";metal=prefix+"_M.tga";rough=prefix+"_R.tga";}
if(model.SkinIndex==3){prefix="Assets/IDS ASSETS/Characters/Redhead/Textures/RedHead_MAT";albedo=prefix+"_Base_color.png";normal=prefix+"_Normal_OpenGL.png";packed=prefix+"_MetallicSmoothness.png";ao=prefix+"_Mixed_AO.png";}
if(model.SkinIndex==2){string part=(basePath+" "+old.name).IndexOf("armor",StringComparison.OrdinalIgnoreCase)>=0?"Armor":"Body";prefix="Assets/IDS ASSETS/Characters/PurpleHead/Textures/"+part+"/PH_"+part;albedo=prefix+"_AlbedoTransparency.png";normal=prefix+"_Normal.png";packed=prefix+"_MetallicSmoothness.png";ao=prefix+"_AO.png";}
if(model.SkinIndex==1){bool body=(basePath+" "+old.name).IndexOf("body",StringComparison.OrdinalIgnoreCase)>=0;prefix="Assets/IDS ASSETS/Characters/Dinork/Textures/"+(body?"Dinork_Body":"Dinork_S01_Mesh_Dinork_S01_MAT");albedo=prefix+(body?"_Base Color.png":"_AlbedoTransparency.png");normal=prefix+"_Normal.png";if(body)rough=prefix+"_Roughness.png";else packed=prefix+"_MetallicSmoothness.png";}
string path=Root+"/"+model.Key+"-"+i+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
mat.SetTexture("_BumpMap",CopyMap(normal,true));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.8f);
var mask=string.IsNullOrEmpty(packed)?Pack(metal,rough,model.Key+"-"+i):CopyMap(packed,false);
mat.SetTexture("_MetallicGlossMap",mask);mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Metallic",1);mat.SetFloat("_Smoothness",model.SkinIndex==1&&i==0?.4f:.8f);mat.SetFloat("_SmoothnessTextureChannel",0);
if(!string.IsNullOrEmpty(ao)){mat.SetTexture("_OcclusionMap",CopyMap(ao,false));mat.SetFloat("_OcclusionStrength",.7f);}
mat.enableInstancing=true;EditorUtility.SetDirty(mat);mats[i]=mat;
if(!mat.GetTexture("_BaseMap")||!mat.GetTexture("_BumpMap")||!mask)throw new Exception("Missing PBR maps for "+model.Name+" "+i+" base="+basePath);
report.Add(model.Name+" / "+i+" original texture set="+prefix+"; albedo, normal, metallic/smoothness assigned.");
}r.sharedMaterials=mats;
}
PrefabUtility.SaveAsPrefabAsset(prefab,AssetDatabase.GetAssetPath(model.Prefab));PrefabUtility.UnloadPrefabContents(prefab);
}
AssetDatabase.SaveAssets();File.WriteAllLines("Documentation/character-pbr-validation.txt",report);
}
static Texture2D CopyMap(string source,bool normal){if(string.IsNullOrEmpty(source)||!File.Exists(source))return null;string dest=Root+"/Maps/"+Path.GetFileName(source);if(!File.Exists(dest))File.Copy(source,dest);AssetDatabase.ImportAsset(dest);var importer=(TextureImporter)AssetImporter.GetAtPath(dest);importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=false;importer.maxTextureSize=2048;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(dest);}
static Texture2D Read(string path){if(string.IsNullOrEmpty(path))return null;var tex=CopyMap(path,false);if(!tex)return null;var rt=RenderTexture.GetTemporary(tex.width,tex.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);Graphics.Blit(tex,rt);var prev=RenderTexture.active;RenderTexture.active=rt;var read=new Texture2D(tex.width,tex.height,TextureFormat.RGBA32,false,true);read.ReadPixels(new Rect(0,0,tex.width,tex.height),0,0);read.Apply();RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);return read;}
static Texture2D Pack(string metalPath,string roughPath,string key){var metal=Read(metalPath);var rough=Read(roughPath);if(!rough)throw new Exception("Missing roughness "+roughPath);int width=rough.width,height=rough.height;var pixels=rough.GetPixels();for(int j=0;j<pixels.Length;j++){float m=metal?metal.GetPixelBilinear((j%width)/(float)width,(j/width)/(float)height).r:0;pixels[j]=new Color(m,0,0,1-pixels[j].r);}var result=new Texture2D(width,height,TextureFormat.RGBA32,false,true);result.SetPixels(pixels);result.Apply();string path=Root+"/Maps/"+key+"-MetallicSmoothness.png";File.WriteAllBytes(path,result.EncodeToPNG());UnityEngine.Object.DestroyImmediate(result);if(metal)UnityEngine.Object.DestroyImmediate(metal);UnityEngine.Object.DestroyImmediate(rough);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
public static void FromCommandLine(){try{Build();ButtonStyleProbe.FromCommandLine();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
