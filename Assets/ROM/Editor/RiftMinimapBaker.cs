using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ROM.Horde;
using ROM.UI.Gameplay;
namespace ROM.EditorTools {
public static class RiftMinimapBaker {
 const string Folder="Assets/ROM/UI/GameplayHUD/Resources";
 [MenuItem("ROM/UI/Bake Rift minimap")]
 public static void Build(){
  var setup=EditorSceneManager.GetSceneManagerSetup();
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  EditorSceneManager.OpenScene("Assets/ROM/Levels/map_rom_horde.unity");
  bool fog=RenderSettings.fog;var lods=Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);var heart=Object.FindFirstObjectByType<FracturedHeart>();bool heartActive=heart&&heart.gameObject.activeSelf;GameObject bakeLight=null;
  var hidden=new System.Collections.Generic.Dictionary<Renderer,bool>();GameObject go=null;RenderTexture target=null;Texture2D image=null;var previous=RenderTexture.active;
  try {
   foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(renderer is SkinnedMeshRenderer||renderer is ParticleSystemRenderer||renderer.GetComponent<TextMesh>()||renderer.GetComponentInParent<ROM.Horde.RiftVillageNpc>()){hidden[renderer]=renderer.forceRenderingOff;renderer.forceRenderingOff=true;}RenderSettings.fog=false;foreach(var lod in lods)lod.ForceLOD(0);if(heart)heart.gameObject.SetActive(false);bakeLight=new GameObject("Temporary map lighting");var light=bakeLight.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;bakeLight.transform.rotation=Quaternion.Euler(75,20,0);
   var points=Object.FindObjectsByType<RiftCheckpoint>(FindObjectsSortMode.None).Select(p=>p.transform.position).Concat(Object.FindObjectsByType<RiftEncounter>(FindObjectsSortMode.None).Select(p=>p.transform.position)).ToArray();
   if(points.Length==0)throw new System.Exception("No Rift checkpoints found");
   float minX=points.Min(p=>p.x)-220,maxX=points.Max(p=>p.x)+220,minZ=points.Min(p=>p.z)-220,maxZ=points.Max(p=>p.z)+220;
   float size=Mathf.Max(maxX-minX,maxZ-minZ);Vector2 center=new Vector2((minX+maxX)*.5f,(minZ+maxZ)*.5f);
   go=new GameObject("Temporary minimap bake camera");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=size*.5f;camera.nearClipPlane=1;camera.farClipPlane=1800;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.06f);camera.cullingMask=MultiFPS.GameManager.environmentLayer;camera.useOcclusionCulling=false;camera.allowHDR=false;camera.allowMSAA=false;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;camera.transform.SetPositionAndRotation(new Vector3(center.x,1200,center.y),Quaternion.Euler(90,0,0));
   target=new RenderTexture(4096,4096,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;camera.Render();RenderTexture.active=target;image=new Texture2D(4096,4096,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,4096,4096),0,0);image.Apply();Directory.CreateDirectory(Folder);string path=Folder+"/RiftMinimap.png";File.WriteAllBytes(path,image.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
   var atlas=AssetDatabase.LoadAssetAtPath<RiftMinimapAtlas>(Folder+"/RiftMinimapAtlas.asset");if(!atlas){atlas=ScriptableObject.CreateInstance<RiftMinimapAtlas>();AssetDatabase.CreateAsset(atlas,Folder+"/RiftMinimapAtlas.asset");}atlas.Map=AssetDatabase.LoadAssetAtPath<Texture2D>(path);atlas.Center=center;atlas.WorldSize=Vector2.one*size;EditorUtility.SetDirty(atlas);AssetDatabase.SaveAssetIfDirty(atlas);Debug.Log("Minimap baked: "+center+" extent "+size);
  }finally{foreach(var pair in hidden)if(pair.Key)pair.Key.forceRenderingOff=pair.Value;RenderSettings.fog=fog;foreach(var lod in lods)if(lod)lod.ForceLOD(-1);if(heart)heart.gameObject.SetActive(heartActive);if(bakeLight)Object.DestroyImmediate(bakeLight);RenderTexture.active=previous;if(go)Object.DestroyImmediate(go);if(target){target.Release();Object.DestroyImmediate(target);}if(image)Object.DestroyImmediate(image);if(setup.Any(x=>x.isLoaded&&x.isActive&&!string.IsNullOrEmpty(x.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
 }
}}
