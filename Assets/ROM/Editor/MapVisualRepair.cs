using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace ROM.EditorTools {
public static class MapVisualRepair {
const string Folder="Documentation/map-visual-review";
public static void Audit(){try{
EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);Directory.CreateDirectory(Folder);var lines=new List<string>();
foreach(string name in new[]{"Bosque Fracturado - Ciudad del Sol","Bosque Fracturado - Distrito urbano"}){
var root=GameObject.Find(name);if(!root)throw new Exception("Missing zone "+name);
var renderers=root.GetComponentsInChildren<MeshRenderer>(true);lines.Add("ZONE "+name+" renderers="+renderers.Length+" bounds="+BoundsOf(renderers));
foreach(var r in renderers){var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh||r.sharedMaterials.Any(m=>!m)||!r.enabled||!r.gameObject.activeInHierarchy||r.forceRenderingOff||r.shadowCastingMode==ShadowCastingMode.ShadowsOnly)lines.Add("RENDER "+PathOf(r.transform)+" active="+r.gameObject.activeInHierarchy+" enabled="+r.enabled+" off="+r.forceRenderingOff+" shadow="+r.shadowCastingMode+" mesh="+(f&&f.sharedMesh?f.sharedMesh.name:"MISSING"));}
foreach(var mat in renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct()){
lines.Add("MAT "+AssetDatabase.GetAssetPath(mat)+" shader="+(mat.shader?mat.shader.name:"MISSING")+" supported="+(mat.shader&&mat.shader.isSupported)+" queue="+mat.renderQueue+" surface="+Float(mat,"_Surface")+" clip="+Float(mat,"_AlphaClip")+" cull="+Float(mat,"_Cull")+" color="+(mat.HasProperty("_BaseColor")?mat.GetColor("_BaseColor").ToString():"none"));
var props=new SerializedObject(mat).FindProperty("m_SavedProperties.m_TexEnvs");for(int i=0;i<props.arraySize;i++){var p=props.GetArrayElementAtIndex(i);var tex=p.FindPropertyRelative("second.m_Texture").objectReferenceValue;if(tex)lines.Add(" TEX "+p.FindPropertyRelative("first").stringValue+"="+AssetDatabase.GetAssetPath(tex));}
}
foreach(var c in root.GetComponentsInChildren<Collider>(true)){if(c.isTrigger||!c.enabled)continue;var r=c.GetComponent<Renderer>();if(!r||!r.enabled||!r.gameObject.activeInHierarchy)lines.Add("HIDDEN COLLIDER "+PathOf(c.transform)+" "+c.bounds);}
foreach(var f in root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/ROM/")))lines.Add("GENERATED "+PathOf(f.transform)+" path="+AssetDatabase.GetAssetPath(f.sharedMesh)+" pos="+f.transform.position+" scale="+f.transform.lossyScale+" bounds="+f.GetComponent<Renderer>().bounds);
}
File.WriteAllLines(Folder+"/audit.txt",lines);
Capture("sun-overview-before",new Vector3(500,210,650),new Vector3(820,30,370));Capture("sun-ground-before",new Vector3(775,27,425),new Vector3(860,42,320));
Capture("town-overview-before",new Vector3(-110,145,-660),new Vector3(100,13,-470));Capture("town-street-before",new Vector3(100,11,-590),new Vector3(115,16,-440));EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
public static void Details(){try{
EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var report=new List<string>();var root=GameObject.Find("Bosque Fracturado - Ciudad del Sol");
foreach(var f in root.GetComponentsInChildren<MeshFilter>(true)){
 if(f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).Contains("SunTerrain")){
 var m=f.sharedMesh;var v=m.vertices;var t=m.triangles;int up=0,down=0;for(int i=0;i<t.Length;i+=3){var n=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);if(n.y>.001f)up++;if(n.y<-.001f)down++;}report.Add("TERRAIN "+m.name+" up="+up+" down="+down+" normalsDown="+m.normals.Count(n=>n.y<0)+" count="+m.vertexCount+" heights "+v.Min(a=>a.y)+".."+v.Max(a=>a.y));
 }
 var r=f.GetComponent<MeshRenderer>();if(r&&r.enabled&&r.sharedMaterials.Any(m=>m&&m.name.Contains("HLP")))report.Add("HELPER "+PathOf(f.transform)+" "+r.bounds+" "+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null")));
}
var terrain=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/ROM/Levels/SunTempleURP/RecoveredTerrain.asset");report.Add("RECOVERED TERRAIN "+(terrain?terrain.size.ToString():"none"));
File.WriteAllLines(Folder+"/details.txt",report);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
public static void Repair(){try{
var scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var report=new List<string>();
var town=GameObject.Find("Bosque Fracturado - Distrito urbano");var sun=GameObject.Find("Bosque Fracturado - Ciudad del Sol");
foreach(var m in town.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct()){
 string path=AssetDatabase.GetAssetPath(m);if(!Path.GetFileName(path).StartsWith("Converted_"))continue;
 string id=Path.GetFileNameWithoutExtension(path).Substring(10);string sourcePath=AssetDatabase.GUIDToAssetPath(id);
 if(!sourcePath.Contains("PostApocalypticTown"))continue;
 string name=Path.GetFileNameWithoutExtension(sourcePath);
 if(System.Text.RegularExpressions.Regex.IsMatch(name,"(?i)leaf|leaves|foliage|grass|fence|grate|chain|wiremesh|decal|glass"))continue;
 if(m.GetFloat("_AlphaClip")==0)continue;
 m.SetFloat("_AlphaClip",0);m.DisableKeyword("_ALPHATEST_ON");m.SetFloat("_Surface",0);m.SetFloat("_ZWrite",1);m.SetFloat("_SrcBlend",1);m.SetFloat("_DstBlend",0);m.renderQueue=2000;m.SetOverrideTag("RenderType","Opaque");EditorUtility.SetDirty(m);report.Add("OPAQUE "+name+" => "+path);
}
var models=AssetDatabase.FindAssets("t:Model",new[]{"Assets/Standard Assets/Sun_Temple/Content/Meshes"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
foreach(var f in sun.GetComponentsInChildren<MeshFilter>(true).Where(f=>!f.sharedMesh)){
 string name=System.Text.RegularExpressions.Regex.Replace(f.name,@" \(\d+\)$","");if(name=="Bld_Common_Windowframe_Closed")name+="_A";
 var path=models.FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p)==name);var mesh=path==null?null:AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();
 if(mesh){f.sharedMesh=mesh;EditorUtility.SetDirty(f);foreach(var c in f.GetComponents<MeshCollider>())if(!c.sharedMesh){c.sharedMesh=mesh;EditorUtility.SetDirty(c);}report.Add("MESH "+PathOf(f.transform)+" => "+path);}else report.Add("UNRESOLVED "+PathOf(f.transform));
}
const string mats="Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/Materials/", tex="Assets/Standard Assets/Sun_Temple/Content/Textures/";
foreach(var pair in new[]{new[]{"Fol_Bark1","fol_bark1.tif"},new[]{"Fol_Bark2","fol_bark2.tif"},new[]{"Fol_Bark3","fol_bark3.tif"},new[]{"Dec_Ornaments_A","dec_ornaments.tif"}}){
 var m=AssetDatabase.LoadAssetAtPath<Material>(mats+pair[0]+".mat");var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(tex+pair[1]);if(!m||!texture)throw new Exception("Missing texture "+pair[1]);m.SetTexture("_BaseMap",texture);if(pair[0].StartsWith("Dec_")){m.SetFloat("_AlphaClip",1);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}EditorUtility.SetDirty(m);report.Add("TEXTURE "+pair[0]);
}
foreach(var r in sun.GetComponentsInChildren<MeshRenderer>(true))if(r.sharedMaterials.Any(m=>m&&m.name=="HLP_Red")){r.enabled=false;EditorUtility.SetDirty(r);}
Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
report.Add("Remaining missing meshes: "+sun.GetComponentsInChildren<MeshFilter>(true).Count(f=>!f.sharedMesh));File.WriteAllLines(Folder+"/repairs.txt",report);
Capture("sun-ground-after",new Vector3(775,27,425),new Vector3(860,42,320));Capture("town-street-after",new Vector3(100,11,-590),new Vector3(115,16,-440));
EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
public static void ProbeGeometry(){try{
EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var town=GameObject.Find("Bosque Fracturado - Distrito urbano");var log=new List<string>();
foreach(var g in town.GetComponentsInChildren<LODGroup>()){g.ForceLOD(0);var levels=g.GetLODs();log.Add("LOD "+PathOf(g.transform)+" enabled="+g.enabled+" levels="+levels.Length);foreach(var rr in levels[0].renderers){var f=rr.GetComponent<MeshFilter>();log.Add(" MESH "+rr.name+" "+(f&&f.sharedMesh?f.sharedMesh.name:"NULL")+" "+rr.bounds);}}
Capture("town-forced-lod0",new Vector3(100,11,-590),new Vector3(115,16,-440));
foreach(var m in AssetDatabase.FindAssets("t:Model",new[]{"Assets/Standard Assets/Sun_Temple/Content/Meshes"}).Select(AssetDatabase.GUIDToAssetPath).SelectMany(p=>AssetDatabase.LoadAllAssetsAtPath(p)).OfType<Mesh>().Where(m=>m.name.Contains("Book")))log.Add("BOOK "+m.name+" "+AssetDatabase.GetAssetPath(m));
File.WriteAllLines(Folder+"/geometry.txt",log);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
public static void InspectLOD(){try{
EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var log=new List<string>();
foreach(var rootName in new[]{"Bosque Fracturado - Distrito urbano","Bosque Fracturado - Ciudad del Sol"}){
var root=GameObject.Find(rootName);foreach(var g in root.GetComponentsInChildren<LODGroup>()){
 var lods=g.GetLODs();var first=new HashSet<Renderer>(lods[0].renderers);foreach(var r in lods.SelectMany(l=>l.renderers).Where(r=>r))r.enabled=first.Contains(r);
}
foreach(var f in root.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh&&f.GetComponent<Renderer>() is Renderer r&&r.enabled&&(r.sharedMaterials.Any(m=>m&&m.name.Contains("Ornaments"))||f.name.Contains("Wall")||f.sharedMesh.name!=System.Text.RegularExpressions.Regex.Replace(f.name,@" \(\d+\)$","")))log.Add(PathOf(f.transform)+" MESH="+f.sharedMesh.name+" BOUNDS="+r.bounds);
}
Capture("town-single-lod",new Vector3(100,11,-590),new Vector3(115,16,-440));Capture("sun-single-lod",new Vector3(775,27,425),new Vector3(860,42,320));File.WriteAllLines(Folder+"/mesh-names.txt",log);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
public static void Isolate(){try{
EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var town=GameObject.Find("Bosque Fracturado - Distrito urbano");var log=new List<string>();
foreach(var g in town.GetComponentsInChildren<LODGroup>()){var lods=g.GetLODs();var first=new HashSet<Renderer>(lods[0].renderers);foreach(var r in lods.SelectMany(l=>l.renderers).Where(r=>r))r.enabled=first.Contains(r);}
var shop=town.transform.Find("Barrio comercial - 0");Capture("shop-materials",new Vector3(76,17,-599),new Vector3(56,14,-575));
var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetFloat("_Cull",0);
foreach(var r in town.GetComponentsInChildren<MeshRenderer>()){var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;if(r.enabled)log.Add(r.name+" submeshes="+f.sharedMesh.subMeshCount+" materials="+r.sharedMaterials.Length+" tris="+f.sharedMesh.triangles.Length);r.sharedMaterials=r.sharedMaterials.Select(m=>mat).ToArray();}
Capture("shop-solid",new Vector3(76,17,-599),new Vector3(56,14,-575));File.WriteAllLines(Folder+"/submeshes.txt",log);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
public static void Finish(){try{
var scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var log=new List<string>();var sun=GameObject.Find("Bosque Fracturado - Ciudad del Sol");var town=GameObject.Find("Bosque Fracturado - Distrito urbano");
// Match collision to the detailed visible shell, not the vendor's full bounding box.
int disabled=0,precise=0;
foreach(var g in town.GetComponentsInChildren<LODGroup>()){
 var first=g.GetLODs()[0].renderers.Where(r=>r).Select(r=>r.GetComponent<MeshFilter>()).Where(f=>f&&f.sharedMesh).ToArray();
 foreach(var c in g.GetComponentsInChildren<Collider>())if(!c.isTrigger){c.enabled=false;EditorUtility.SetDirty(c);disabled++;}
 foreach(var f in first){var c=f.GetComponent<MeshCollider>();if(!c)c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;c.enabled=true;EditorUtility.SetDirty(c);precise++;}
}
log.Add("Town: "+disabled+" coarse/redundant colliders replaced by "+precise+" LOD0 mesh colliders.");
// Some legacy open-book variants are no longer shipped. Use the supplied closed book.
foreach(var f in sun.GetComponentsInChildren<MeshFilter>(true).Where(f=>!f.sharedMesh&&f.name=="Prop_Book_Open_A")){f.sharedMesh=AssetDatabase.LoadAllAssetsAtPath("Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/Prop_Book_A.fbx").OfType<Mesh>().First();EditorUtility.SetDirty(f);}
// Broken legacy ornament projection produced atlas-sized rectangles. Hide just those
// cosmetic planes; they have no collision and can be replaced with URP decals later.
int decals=0;foreach(var r in sun.GetComponentsInChildren<MeshRenderer>(true))if(r.sharedMaterials.Any(m=>m&&m.name=="Dec_Ornaments_A")){r.enabled=false;EditorUtility.SetDirty(r);decals++;}log.Add("Legacy ornament planes hidden: "+decals);
var ground=sun.GetComponentsInChildren<MeshCollider>().First(c=>c.sharedMesh&&AssetDatabase.GetAssetPath(c.sharedMesh).EndsWith("/SunTerrain.asset"));
var authored=sun.transform.Find("Sun Temple - composicion original");
AddFoundationSkirts(sun.transform,authored,ground,log);
DressSunIsland(sun.transform,authored,ground,log);
Physics.SyncTransforms();
var surface=UnityEngine.Object.FindFirstObjectByType<Unity.AI.Navigation.NavMeshSurface>();var saved=surface.navMeshData;surface.BuildNavMesh();if(saved&&AssetDatabase.Contains(saved)){EditorUtility.CopySerialized(surface.navMeshData,saved);surface.RemoveData();surface.navMeshData=saved;surface.AddData();EditorUtility.SetDirty(saved);}EditorUtility.SetDirty(surface);
foreach(var checkpoint in UnityEngine.Object.FindObjectsByType<ROM.Horde.RiftCheckpoint>(FindObjectsSortMode.None)){
 bool ok=UnityEngine.AI.NavMesh.SamplePosition(checkpoint.transform.position,out var hit,12,UnityEngine.AI.NavMesh.AllAreas);log.Add("CHECKPOINT "+checkpoint.name+" nav="+ok+" sample="+hit.position);if(!ok)throw new Exception("Checkpoint missing nav: "+checkpoint.name);
}
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllLines(Folder+"/final-validation.txt",log);
Capture("sun-overview-final",new Vector3(500,210,650),new Vector3(820,30,370));Capture("sun-ground-final",new Vector3(775,27,425),new Vector3(860,42,320));Capture("town-close-final",new Vector3(76,17,-599),new Vector3(56,14,-575));EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
static float GroundHeight(MeshCollider ground,Vector3 p){return ground.Raycast(new Ray(new Vector3(p.x,180,p.z),Vector3.down),out var hit,240)?hit.point.y:8;}
static void AddFoundationSkirts(Transform sun,Transform authored,MeshCollider ground,List<string> log){
 if(sun.Find("Cimientos reparados")){log.Add("Foundation skirts already present");return;}
 var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();var seen=new HashSet<string>();
 foreach(var f in authored.GetComponentsInChildren<MeshFilter>()){
 var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled||!f.sharedMesh||!f.name.StartsWith("Bld_")||System.Text.RegularExpressions.Regex.IsMatch(f.name,"(?i)LOD[_ ]?[1-9]|deco|roof|int_|window|door|dome|column|beam|trim")||r.bounds.size.y<4)continue;
 var mesh=f.sharedMesh;var vv=mesh.vertices;var tt=mesh.triangles;var world=vv.Select(v=>f.transform.TransformPoint(v)).ToArray();var low=new Dictionary<Vector2,float>();
 foreach(var v in world){var key=new Vector2(Mathf.Round(v.x*20),Mathf.Round(v.z*20));if(!low.ContainsKey(key)||v.y<low[key])low[key]=v.y;}
 for(int i=0;i<tt.Length;i+=3)for(int e=0;e<3;e++){
 var a=world[tt[i+e]];var b=world[tt[i+(e+1)%3]];if(Mathf.Max(a.y,b.y)>r.bounds.min.y+Mathf.Min(5,r.bounds.size.y*.25f)||Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z))<.15f||Vector3.Distance(a,b)>60)continue;
 if(a.y>low[new Vector2(Mathf.Round(a.x*20),Mathf.Round(a.z*20))]+.05f||b.y>low[new Vector2(Mathf.Round(b.x*20),Mathf.Round(b.z*20))]+.05f)continue;
 float ay=GroundHeight(ground,a)-.3f,by=GroundHeight(ground,b)-.3f;if(a.y<=ay+.3f&&b.y<=by+.3f)continue;
 string ka=(Vector3Int.RoundToInt(a*20)).ToString(),kb=(Vector3Int.RoundToInt(b*20)).ToString();string key=string.CompareOrdinal(ka,kb)<0?ka+kb:kb+ka;if(!seen.Add(key))continue;
 int n=vertices.Count;vertices.AddRange(new[]{a,b,new Vector3(a.x,Mathf.Min(ay,a.y-.1f),a.z),new Vector3(b.x,Mathf.Min(by,b.y-.1f),b.z)});float width=Vector3.Distance(a,b)/4;uv.AddRange(new[]{new Vector2(0,a.y/4),new Vector2(width,b.y/4),new Vector2(0,ay/4),new Vector2(width,by/4)});triangles.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});
 }
 }
 if(vertices.Count==0){log.Add("No foundation gaps detected");return;}
 var result=new Mesh{name="Sun Temple foundation skirts",indexFormat=IndexFormat.UInt32};result.SetVertices(vertices);result.SetUVs(0,uv);result.SetTriangles(triangles,0);result.RecalculateNormals();result.RecalculateBounds();const string path="Assets/ROM/Levels/SunTempleURP/FoundationSkirts.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(result,saved);result=saved;EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(result,path);
 var go=new GameObject("Cimientos reparados",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(sun,true);go.GetComponent<MeshFilter>().sharedMesh=result;go.GetComponent<MeshCollider>().sharedMesh=result;go.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/Materials/Bricks_Plain_Blue.mat");go.isStatic=true;log.Add("Foundation skirts: "+seen.Count+" edges");
}
static void DressSunIsland(Transform sun,Transform authored,MeshCollider ground,List<string> log){
 if(sun.Find("Jardines exteriores")){log.Add("Gardens already present");return;}
 var parent=new GameObject("Jardines exteriores").transform;parent.SetParent(sun,false);var random=new System.Random(102026);var used=new List<Vector3>();
 var buildings=authored.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Bld_")&&r.bounds.size.y>2).Select(r=>r.bounds).ToArray();
 var bridges=sun.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.Contains("Puente")||r.name.Contains("Paseo")||r.name.Contains("Plaza")||r.transform.parent&&r.transform.parent.name.Contains("Paseo")).Select(r=>r.bounds).ToArray();
 var checkpoints=UnityEngine.Object.FindObjectsByType<ROM.Horde.RiftCheckpoint>(FindObjectsSortMode.None).Select(c=>c.transform.position).ToArray();
 const string natural="Assets/Standard Assets/Sun_Temple/Prefabs/Natural/";int count=0;
 for(int i=0;i<1800&&count<210;i++){
 double angle=random.NextDouble()*Math.PI*2;float radius=125+(float)random.NextDouble()*125;var p=new Vector3(820+Mathf.Sin((float)angle)*radius,0,370+Mathf.Cos((float)angle)*radius);p.y=GroundHeight(ground,p);
 if(used.Any(q=>Vector3.Distance(q,p)<10)||checkpoints.Any(q=>Vector2.Distance(new Vector2(q.x,q.z),new Vector2(p.x,p.z))<30)||buildings.Any(b=>p.x>b.min.x-10&&p.x<b.max.x+10&&p.z>b.min.z-10&&p.z<b.max.z+10)||bridges.Any(b=>p.x>b.min.x-18&&p.x<b.max.x+18&&p.z>b.min.z-18&&p.z<b.max.z+18))continue;
 string file=count%5==0?"Rock_Boulder_B.prefab":count%3==0?"Fol_Bush_A.prefab":count%2==0?"Fol_TallTree_A.prefab":"Fol_TallTree_B.prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(natural+file);if(!prefab)throw new Exception("Missing "+file);var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.name="Jardin - "+Path.GetFileNameWithoutExtension(file);go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
 var rr=go.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh&&!r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("HLP")).ToArray();if(rr.Length==0){UnityEngine.Object.DestroyImmediate(go);continue;}var b=BoundsOf(rr);float height=file.StartsWith("Rock")?3+(float)random.NextDouble()*4:file.StartsWith("Fol_Bush")?2:12+(float)random.NextDouble()*8;float scale=height/Mathf.Max(1,b.size.y);go.transform.localScale*=scale;go.transform.position=p-new Vector3(b.center.x,b.min.y,b.center.z)*scale;
 foreach(var t in go.GetComponentsInChildren<Transform>()){t.gameObject.layer=0;t.gameObject.isStatic=true;}foreach(var script in go.GetComponentsInChildren<MonoBehaviour>())if(script)UnityEngine.Object.DestroyImmediate(script);
 if(!file.StartsWith("Fol_Bush")){var col=go.AddComponent<CapsuleCollider>();col.center=go.transform.InverseTransformPoint(p+Vector3.up*(file.StartsWith("Rock")?height*.45f:height*.2f));col.height=(file.StartsWith("Rock")?height*.9f:height*.4f)/go.transform.lossyScale.y;col.radius=(file.StartsWith("Rock")?height*.35f:.5f)/go.transform.lossyScale.x;}
 used.Add(p);count++;
 }
 log.Add("Exterior planted groups: "+count);
}
public static void Validate(){try{
var scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var log=new List<string>();
foreach(var f in GameObject.Find("Bosque Fracturado - Ciudad del Sol").GetComponentsInChildren<MeshFilter>(true).Where(f=>!f.sharedMesh&&f.name=="Prop_Book_Open_A")){f.sharedMesh=AssetDatabase.LoadAllAssetsAtPath("Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/Prop_Book_A.fbx").OfType<Mesh>().First();EditorUtility.SetDirty(f);}
var sunRoot=GameObject.Find("Bosque Fracturado - Ciudad del Sol");var terrainCollider=sunRoot.GetComponentsInChildren<MeshCollider>().First(c=>c.sharedMesh&&AssetDatabase.GetAssetPath(c.sharedMesh).EndsWith("/SunTerrain.asset"));
var checkpointsForRepair=UnityEngine.Object.FindObjectsByType<ROM.Horde.RiftCheckpoint>(FindObjectsSortMode.None);var sunCheckpoint=checkpointsForRepair.First(c=>c.Title=="Ciudad del Sol");var camp=checkpointsForRepair.First(c=>c.StartingCamp);UnityEngine.AI.NavMesh.SamplePosition(camp.Respawn.position,out var campNav,12,UnityEngine.AI.NavMesh.AllAreas);
// Keep the crystal on the broad arrival plaza, connected to the bridge, rather
// than an isolated old-demo terrace beneath the reconstructed hillside.
var arrival=new Vector3(626,8.2f,229);if(!UnityEngine.AI.NavMesh.SamplePosition(arrival,out var arrivalNav,10,UnityEngine.AI.NavMesh.AllAreas))throw new Exception("Sun arrival plaza has no navmesh");var arrivalPath=new UnityEngine.AI.NavMeshPath();if(!UnityEngine.AI.NavMesh.CalculatePath(campNav.position,arrivalNav.position,UnityEngine.AI.NavMesh.AllAreas,arrivalPath)||arrivalPath.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)throw new Exception("Sun arrival plaza disconnected");
var position=arrivalNav.position+new Vector3(0,.2f,3);sunCheckpoint.transform.position=position;sunCheckpoint.Respawn.position=arrivalNav.position+Vector3.up*.2f;EditorUtility.SetDirty(sunCheckpoint.transform);EditorUtility.SetDirty(sunCheckpoint.Respawn);
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
foreach(string name in new[]{"Bosque Fracturado - Ciudad del Sol","Bosque Fracturado - Distrito urbano"}){
var root=GameObject.Find(name);
foreach(var mat in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.sharedMaterials).Where(m=>m&&(!m.shader||!m.shader.isSupported)).Distinct()){
log.Add("UNSUPPORTED "+AssetDatabase.GetAssetPath(mat)+" shader="+(mat.shader?mat.shader.name:"null"));
}
int missing=root.GetComponentsInChildren<MeshFilter>(true).Count(f=>!f.sharedMesh);int bad=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.sharedMaterials).Count(m=>!m||!m.shader||!m.shader.isSupported);log.Add(name+" missingMeshes="+missing+" missingUnsupportedMaterials="+bad);if(missing>0||bad>0){File.WriteAllLines(Folder+"/asset-and-route-validation.txt",log);throw new Exception("Unresolved assets "+name);}
}
var checkpoints=UnityEngine.Object.FindObjectsByType<ROM.Horde.RiftCheckpoint>(FindObjectsSortMode.None);var start=checkpoints.First(c=>c.StartingCamp);UnityEngine.AI.NavMesh.SamplePosition(start.Respawn.position,out var origin,12,UnityEngine.AI.NavMesh.AllAreas);
foreach(var c in checkpoints){var p=c.Respawn?c.Respawn.position:c.transform.position;bool found=UnityEngine.AI.NavMesh.SamplePosition(p,out var hit,12,UnityEngine.AI.NavMesh.AllAreas);var path=new UnityEngine.AI.NavMeshPath();bool connected=found&&UnityEngine.AI.NavMesh.CalculatePath(origin.position,hit.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;bool ground=Physics.Raycast(p+Vector3.up*3,Vector3.down,out var floor,18,~0,QueryTriggerInteraction.Ignore);log.Add("CHECKPOINT "+c.Title+" position="+p+" sampled="+hit.position+" connected="+connected+" floor="+ground+" floorY="+floor.point.y);}
File.WriteAllLines(Folder+"/asset-and-route-validation.txt",log);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
static float Float(Material m,string key)=>m.HasProperty(key)?m.GetFloat(key):-1;
static Bounds BoundsOf(Renderer[] rr){var b=new Bounds(rr[0].bounds.center,Vector3.zero);foreach(var r in rr.Where(r=>r.enabled))b.Encapsulate(r.bounds);return b;}
static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
public static void Capture(string name,Vector3 position,Vector3 look){
var go=new GameObject("Visual review",typeof(Camera));var camera=go.GetComponent<Camera>();camera.transform.position=position;camera.transform.LookAt(look);camera.farClipPlane=2500;camera.nearClipPlane=.2f;camera.fieldOfView=65;camera.useOcclusionCulling=false;camera.clearFlags=CameraClearFlags.Skybox;var rt=new RenderTexture(1600,1000,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
}
}}
