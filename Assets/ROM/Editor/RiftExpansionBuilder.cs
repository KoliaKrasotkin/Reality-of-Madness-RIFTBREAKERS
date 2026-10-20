using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.AI;
using Unity.AI.Navigation;
namespace ROM.EditorTools {
public static class RiftExpansionBuilder {
const string Rally="Assets/LeartesStudios/The_Rally_Point/Art/Prefabs/";
const string Pirate="Assets/LeartesStudios/PirateTavern/Art/Prefabs/";
const string Output="Assets/ROM/Levels/RiftSetDressing";
const string Docs="Documentation/rally-pirate-expansion";
public static void Probe(){try{
Directory.CreateDirectory(Docs);EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var lines=new List<string>();
foreach(var root in new[]{Rally,Pirate})foreach(var name in (root==Rally?new[]{"SM_Shop_01","SM_Shop_02","SM_Restaurant_01","SM_Fake_Shop_8x5m","SM_Museum_Entry","SM_Monument_Wall_4m","SM_Residential_Wall_4x6m","SM_Floor_5x5m","SM_Double_Door_Frame_01","SM_Door_Frame_01","SM_Museum_Pillar_01","SM_Fake_Tower_01","SM_Stair_01","SM_Ammo_Crate_01","SM_Concrete_Fence_01"}:new[]{"SM_Cliff_A","SM_Rock_A","SM_WoodenFloor_A","SM_WoodenFloor_B","SM_TradingTent_A","SM_Beam_A","SM_Arch_Beams_A","SM_Roof_Main_A","SM_Wall_3x3_A","SM_Barrel","SM_OilLamp","SM_PalmTrunk_A","SM_Palm_Branches_Cylinder_013","SM_BarrelsInNet_A","SM_Net_A"})){
var p=AssetDatabase.LoadAssetAtPath<GameObject>(root+name+".prefab");if(!p){lines.Add("MISSING "+name);continue;}var go=UnityEngine.Object.Instantiate(p);go.transform.position=Vector3.zero;var rr=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!(r is ParticleSystemRenderer)).ToArray();if(rr.Length==0){lines.Add("EMPTY "+name);UnityEngine.Object.DestroyImmediate(go);continue;}var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);lines.Add("PREFAB "+name+" bounds="+b+" nativeScale="+go.transform.localScale+" lods="+go.GetComponentsInChildren<LODGroup>().Length);
foreach(var m in rr.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct()) {lines.Add(" MAT "+AssetDatabase.GetAssetPath(m)+" shader="+m.shader.name+" supported="+m.shader.isSupported);var props=new SerializedObject(m).FindProperty("m_SavedProperties.m_TexEnvs");for(int i=0;i<props.arraySize;i++){var a=props.GetArrayElementAtIndex(i);var tex=a.FindPropertyRelative("second.m_Texture").objectReferenceValue;if(tex)lines.Add("  TEX "+a.FindPropertyRelative("first").stringValue+" "+AssetDatabase.GetAssetPath(tex));}}
UnityEngine.Object.DestroyImmediate(go);
}
foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/ROM/")&&(AssetDatabase.GetAssetPath(f.sharedMesh).Contains("Satellite_")||AssetDatabase.GetAssetPath(f.sharedMesh).Contains("ExtraIsland_"))))lines.Add("GROUND "+f.name+" "+f.transform.position+" "+AssetDatabase.GetAssetPath(f.sharedMesh));
File.WriteAllLines(Docs+"/source-assets.txt",lines);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}

static Transform root;static readonly List<Vector3> destinations=new List<Vector3>();static readonly Dictionary<string,int> usage=new Dictionary<string,int>();static readonly List<string> report=new List<string>();
static Material stone,wood;static ROM.Horde.Horde mode;
static readonly Vector3 WestRoom=new Vector3(-265,2,-88),EclipseRoom=new Vector3(-525,2,375);
[MenuItem("ROM/El Rift/Encontrar playa del puerto")]
public static void FocusBeach(){Focus(new Vector3(140,5,440),110);}
[MenuItem("ROM/El Rift/Encontrar archivo semienterrado")]
public static void FocusArchive(){Focus(WestRoom,50);}
static void Focus(Vector3 p,float size){if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.Frame(new Bounds(p,Vector3.one*size),false);}
public static void Build(){try{
Directory.CreateDirectory(Output+"/Meshes");Directory.CreateDirectory(Output+"/Materials");Directory.CreateDirectory(Docs);AssetDatabase.Refresh();
var scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);if(GameObject.Find("El Rift - Rally y costa pirata"))throw new Exception("La expansión ya existe. Editar los grupos guardados en lugar de duplicar el contenido.");
root=new GameObject("El Rift - Rally y costa pirata").transform;usage.Clear();destinations.Clear();report.Clear();mode=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();
stone=Surface("Piedra del archivo",RallyMaterial("M_Stone_Wall_02"));wood=Surface("Madera portuaria",AssetDatabase.LoadAssetAtPath<Material>("Assets/LeartesStudios/PirateTavern/Art/Materials/M_WoodTilingMaterial_NoPainting.mat"));
// Existing island meshes only. Their footprint and bridges remain in place.
SculptGround("Isla de las ruinas",155,WestRoom,false);
SculptGround("Santuario del Eclipse - isla de 440 metros",220,EclipseRoom,false);
SculptGround("Isla del bosque antiguo",185,Vector3.zero,true);
BuriedArchive(WestRoom,"Archivo hundido de las ruinas");BuriedArchive(EclipseRoom,"Refugio de excavación del Eclipse");
Shop(new Vector3(55,8,-335),90,"Distrito - taller de suministros");Shop(new Vector3(145,8,-335),-90,"Distrito - local recuperado");
Shop(new Vector3(260,8,-70),0,"Cantera - puesto de exploradores");
Pavilion(new Vector3(360,8,-105));Beach();
// Small pirate cargo details at the existing village market, away from its NPCs.
var villageDetail=Group("Cargamento del refugio",new Vector3(22,0,-28));Put(Pirate,"SM_Barrel",villageDetail.position,1.0f,0,villageDetail);Put(Pirate,"SM_Crate_A",villageDetail.position+Vector3.right*1.4f,.9f,12,villageDetail);
Physics.SyncTransforms();var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();var oldNav=surface.navMeshData;surface.BuildNavMesh();if(oldNav&&AssetDatabase.Contains(oldNav)){EditorUtility.CopySerialized(surface.navMeshData,oldNav);surface.RemoveData();surface.navMeshData=oldNav;surface.AddData();EditorUtility.SetDirty(oldNav);}EditorUtility.SetDirty(surface);
Validate();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllLines(Docs+"/validation.txt",report);File.WriteAllLines(Docs+"/asset-usage.txt",usage.OrderBy(x=>x.Key).Select(x=>x.Value+" x "+x.Key));
MapVisualRepair.Capture("../rally-pirate-expansion/beach-overview",new Vector3(220,75,330),new Vector3(135,4,450));MapVisualRepair.Capture("../rally-pirate-expansion/beach-ground",new Vector3(110,9.5f,417),new Vector3(168,3,440));MapVisualRepair.Capture("../rally-pirate-expansion/archive-entry",WestRoom+new Vector3(14,10,-32),WestRoom+new Vector3(0,2,4));MapVisualRepair.Capture("../rally-pirate-expansion/archive-interior",WestRoom+new Vector3(0,2,1),WestRoom+new Vector3(0,2,14));MapVisualRepair.Capture("../rally-pirate-expansion/urban-buildings",new Vector3(100,28,-250),new Vector3(100,12,-335));
Debug.Log("[RallyPirate] PASS: existing islands enriched; two navigable buried interiors and coastal harbor saved.");EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);File.WriteAllLines(Docs+"/validation-failed.txt",report.Concat(new[]{e.ToString()}));EditorApplication.Exit(1);}}
static Transform Group(string name,Vector3 pos){var g=new GameObject(name).transform;g.SetParent(root);g.position=pos;return g;}
static Bounds RenderBounds(GameObject g){var rr=g.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh).ToArray();if(rr.Length==0)throw new Exception("No meshes "+g.name);var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
static GameObject Put(string package,string name,Vector3 p,float height,float yaw,Transform parent,bool collide=true){
string path=package+name+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)throw new Exception("Missing prefab "+path);usage[path]=usage.TryGetValue(path,out var count)?count+1:1;
var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);g.name=name;g.transform.position=Vector3.zero;g.transform.rotation=Quaternion.Euler(0,yaw,0);var b=RenderBounds(g);float scale=height/Mathf.Max(.05f,b.size.y);g.transform.localScale*=scale;g.transform.position=p-new Vector3(b.center.x,b.min.y,b.center.z)*scale;
foreach(var mb in g.GetComponentsInChildren<MonoBehaviour>())if(mb)UnityEngine.Object.DestroyImmediate(mb);foreach(var light in g.GetComponentsInChildren<Light>())UnityEngine.Object.DestroyImmediate(light);foreach(var c in g.GetComponentsInChildren<Collider>())c.enabled=false;
var lower=new HashSet<Renderer>(g.GetComponentsInChildren<LODGroup>().SelectMany(l=>l.GetLODs().Skip(1)).SelectMany(l=>l.renderers));
foreach(var r in g.GetComponentsInChildren<MeshRenderer>()){
 r.lightmapIndex=-1;r.lightmapScaleOffset=new Vector4(1,1,0,0);foreach(var mat in r.sharedMaterials)if(!mat||!mat.shader||!mat.shader.isSupported)throw new Exception("Incompatible material "+path);
 var f=r.GetComponent<MeshFilter>();if(collide&&!lower.Contains(r)&&f&&f.sharedMesh){var c=r.GetComponent<MeshCollider>();if(!c)c=r.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;c.enabled=true;}
}
foreach(var t in g.GetComponentsInChildren<Transform>()){t.gameObject.layer=0;t.gameObject.isStatic=true;}return g;
}
static GameObject Fit(string package,string name,Vector3 center,Vector3 size,float yaw,Transform parent,bool collide=true){
var g=Put(package,name,Vector3.zero,1,0,parent,collide);var b=RenderBounds(g);var originalScale=g.transform.localScale;g.transform.localScale=Vector3.Scale(originalScale,new Vector3(size.x/Mathf.Max(.03f,b.size.x),size.y/Mathf.Max(.03f,b.size.y),size.z/Mathf.Max(.03f,b.size.z)));b=RenderBounds(g);g.transform.position-=b.center;g.transform.RotateAround(Vector3.zero,Vector3.up,yaw);g.transform.position+=center;return g;
}
static Material RallyMaterial(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/LeartesStudios/The_Rally_Point/Art/Materials/"+name+".mat");
static Material Surface(string name,Material source){
var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetFloat("_Cull",0);m.SetFloat("_Smoothness",.12f);m.SetColor("_BaseColor",Color.white);
if(source){CopyTexture(source,m,"_BaseMap",new[]{"_BaseMap","_Base","_BaseColorMap","_MainTex","_Albedo"});CopyTexture(source,m,"_BumpMap",new[]{"_BumpMap","_Normal","_NormalMap"});if(m.GetTexture("_BumpMap"))m.EnableKeyword("_NORMALMAP");}string path=Output+"/Materials/"+name+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old){EditorUtility.CopySerialized(m,old);UnityEngine.Object.DestroyImmediate(m);m=old;EditorUtility.SetDirty(m);}else AssetDatabase.CreateAsset(m,path);return m;
}
static void CopyTexture(Material source,Material target,string key,string[] aliases){var props=new SerializedObject(source).FindProperty("m_SavedProperties.m_TexEnvs");foreach(string alias in aliases)for(int i=0;i<props.arraySize;i++){var prop=props.GetArrayElementAtIndex(i);if(prop.FindPropertyRelative("first").stringValue!=alias)continue;var value=prop.FindPropertyRelative("second");var tex=value.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;if(tex){target.SetTexture(key,tex);return;}}}
static GameObject Box(string name,Vector3 center,Vector3 size,Material mat,Transform parent,bool collision=true){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.position=center;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collision)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;return g;}
static void Lamp(Vector3 p,Transform parent,bool cool=false){Put(Pirate,"SM_OilLamp",p,.48f,0,parent,false);var light=new GameObject("Luz de orientación",typeof(Light)).GetComponent<Light>();light.transform.SetParent(parent);light.transform.position=p+Vector3.up*.5f;light.type=LightType.Point;light.color=cool?new Color(.45f,.7f,1):new Color(1,.66f,.35f);light.range=15;light.intensity=cool?20:12;light.shadows=LightShadows.None;}
static void ClearPlot(Vector3 p,Vector2 half,float margin=3){
foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).ToArray()){
 if(!t||t.IsChildOf(root))continue;string n=t.name.ToLowerInvariant();if(!n.Contains("arbol")&&!n.Contains("tree")&&!n.Contains("afloramiento")&&!n.Contains("cobertura")&&!n.Contains("sotobosque")&&!n.StartsWith("jardin -")&&!n.Contains("ruina de exploracion"))continue;
 var b=t.GetComponent<Renderer>()?t.GetComponent<Renderer>().bounds:new Bounds(t.position,Vector3.zero);if(Mathf.Abs(b.center.x-p.x)>half.x+margin||Mathf.Abs(b.center.z-p.z)>half.y+margin)continue;
 var instance=PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);var target=instance?instance:t.gameObject;target.SetActive(false);
}
}
static float SculptY(Vector3 p,Vector3 room,bool beach){
if(beach){float across=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(48,78,Mathf.Abs(p.z-450)));float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(95,165,p.x));return 8-6.8f*t*across;}
float x=Mathf.Abs(p.x-room.x),z=p.z-room.z;if(z>16.5f)return 8;float sides=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.6f,7,x));float descent=Mathf.Clamp01(Mathf.InverseLerp(-26,-1,z));return 8-6*sides*descent;
}
static void SculptGround(string name,float radius,Vector3 room,bool beach){
var go=GameObject.Find(name);if(!go)throw new Exception("Missing island "+name);var f=go.GetComponent<MeshFilter>();var original=f.sharedMesh;
var v=new List<Vector3>{Vector3.zero};var uv=new List<Vector2>{Vector2.zero};const int rings=128,segments=256;
for(int r=1;r<=rings;r++)for(int s=0;s<segments;s++){float a=s*Mathf.PI*2/segments,rad=radius*r/rings*(1+.06f*Mathf.Sin(a*5));var pos=new Vector3(Mathf.Sin(a)*rad,0,Mathf.Cos(a)*rad);var w=go.transform.TransformPoint(pos);pos.y=SculptY(w,room,beach)-go.transform.position.y;v.Add(pos);uv.Add(new Vector2(pos.x,pos.z)/8);}
var triangles=new List<int>();Action<int,int,int> add=(a,b,c)=>{if(!beach&&SubtractRoomTriangle(a,b,c,v,uv,triangles,room-go.transform.position))return;triangles.Add(a);triangles.Add(b);triangles.Add(c);};
for(int s=0;s<segments;s++)add(0,1+s,1+(s+1)%segments);for(int r=1;r<rings;r++)for(int s=0;s<segments;s++){int a=1+(r-1)*segments+s,b=1+(r-1)*segments+(s+1)%segments,c=a+segments,d=b+segments;add(a,c,b);add(b,c,d);}
var mesh=new Mesh{name=name+" - relieve intervenido",indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();string path=Output+"/Meshes/"+(beach?"Coast":name.Contains("Eclipse")?"EclipseArchive":"WestArchive")+".asset";mesh=SaveMesh(mesh,path);f.sharedMesh=mesh;go.GetComponent<MeshCollider>().sharedMesh=mesh;EditorUtility.SetDirty(f);EditorUtility.SetDirty(go.GetComponent<MeshCollider>());
if(beach){var m=new Material(Shader.Find("ROM/Coastal Ground"));m.name="Arena y bosque del puerto";var soil=AssetDatabase.LoadAssetAtPath<Material>("Assets/ROM/Levels/FracturedForest/Materials/ForestSoil.mat");m.SetTexture("_BaseMap",soil.GetTexture("_BaseMap"));m.SetTexture("_RockMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HIVEMIND/PostApocalypticTown/URP/Art/Textures/LandscapeMaterial/T_Canyon_A.png"));m.SetTexture("_SandMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LeartesStudios/PirateTavern/Art/Textures/T_Sand_Wavy_v2_BC.PNG"));m.SetColor("_BaseColor",new Color(.68f,.72f,.57f));AssetDatabase.CreateAsset(m,Output+"/Materials/CoastalGround.mat");go.GetComponent<Renderer>().sharedMaterial=m;}
report.Add("Existing island sculpted: "+name+"; source retained "+AssetDatabase.GetAssetPath(original));
}
static void BuriedArchive(Vector3 p,string name){
ClearPlot(p+new Vector3(0,0,-5),new Vector2(14,30));var g=Group(name,p);
Box("Suelo del recinto",p+new Vector3(0,-.16f,8),new Vector3(15.6f,.3f,16.6f),stone,g);
Box("Techo bajo el terreno",new Vector3(p.x,7.9f,p.z+8),new Vector3(17,.5f,18),stone,g);
Box("Muro izquierdo",p+new Vector3(-7.7f,2.9f,8),new Vector3(.35f,6,16),stone,g);Box("Muro derecho",p+new Vector3(7.7f,2.9f,8),new Vector3(.35f,6,16),stone,g);Box("Muro posterior",p+new Vector3(0,2.9f,16),new Vector3(15.7f,6,.35f),stone,g);
Box("Frente izquierdo",p+new Vector3(-5.3f,2.9f,0),new Vector3(5,6,.35f),stone,g);Box("Frente derecho",p+new Vector3(5.3f,2.9f,0),new Vector3(5,6,.35f),stone,g);Box("Dintel del acceso",p+new Vector3(0,4.8f,0),new Vector3(6,2.2f,.4f),stone,g);
Fit(Rally,"SM_Double_Door_Frame_01",p+new Vector3(0,1.7f,-.25f),new Vector3(4.8f,3.4f,.25f),0,g);
// Vendor modules make the interior architectural rather than an empty primitive box.
for(int side=-1;side<=1;side+=2)for(int row=0;row<4;row++)Put(Rally,"SM_Museum_Pillar_01",p+new Vector3(side*7.1f,0,2+row*4),5.5f,side<0?90:-90,g);
for(int x=-1;x<=1;x++)for(int z=0;z<3;z++)Fit(Rally,"SM_Floor_5x5m",p+new Vector3(x*5,.04f,3+z*5),new Vector3(5,.03f,5),0,g);
Put(Rally,"SM_Foldable_table_01",p+new Vector3(-4,0,12),.9f,0,g);for(int i=0;i<4;i++)Put(Rally,"SM_Ammo_Crate_01",p+new Vector3(4,0,9+i*1.3f),.65f,90,g);Put(Rally,"SM_Antenna_01",new Vector3(p.x+5,8.2f,p.z+11),4,0,g);
Lamp(p+new Vector3(-5,1,3),g);Lamp(p+new Vector3(5,1,13),g,true);Lamp(p+new Vector3(-3.5f,1,-2),g);
destinations.Add(p+new Vector3(0,.2f,8));report.Add("Walkable buried room: "+name+" interior="+destinations.Last());
}
static void Shop(Vector3 p,float yaw,string name){
ClearPlot(p,new Vector2(14,12));var g=Group(name,p);var q=Quaternion.Euler(0,yaw,0);
Fit(Rally,"SM_Fake_Shop_8x5m",p+Vector3.up*2.5f,new Vector3(8,5,5),yaw,g);
Fit(Rally,"SM_Shop_01",p+q*new Vector3(0,2.5f,2.35f),new Vector3(8,5,.8f),yaw,g);
for(int i=-1;i<=1;i+=2)Fit(Rally,"SM_Residential_Wall_W_3x3m",p+q*new Vector3(i*2,6.5f,2.48f),new Vector3(4,3,.2f),yaw,g);
Box("Cuerpo superior",p+Vector3.up*6.5f,new Vector3(7.9f,3,4.9f),stone,g).transform.rotation=q;
Box("Cubierta",p+Vector3.up*8.1f,new Vector3(8.6f,.3f,5.6f),stone,g).transform.rotation=q;
Fit(Rally,"SM_Awning_Metallic_01",p+q*new Vector3(0,4.6f,3.3f),new Vector3(8.8f,.6f,2.2f),yaw,g);
Put(Rally,"SM_Chair_Foldable_01",p+q*new Vector3(-3,0,5),1,yaw+30,g);Put(Rally,"SM_Cardbox_Crate_01",p+q*new Vector3(3.2f,0,4),.8f,yaw,g);Lamp(p+q*new Vector3(-3,1.2f,2.9f),g);
destinations.Add(p+q*new Vector3(0,.2f,6));
}
static void Pavilion(Vector3 p){
ClearPlot(p,new Vector2(15,13));var g=Group("Cantera - puesto arqueológico",p);Fit(Rally,"SM_Floor_5x5m",p,new Vector3(20,.06f,15),0,g);
for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Put(Rally,"SM_Museum_Pillar_01",p+new Vector3(x*8,0,z*5),6,0,g);
Box("Tejado del puesto",p+Vector3.up*6,new Vector3(19,.4f,14),stone,g);Put(Rally,"SM_Foldable_table_01",p+new Vector3(-4,0,0),1,0,g);Put(Rally,"SM_Projector_Packed_01",p+new Vector3(-4,1,0),.4f,0,g);Put(Rally,"SM_Ammo_Crate_01",p+new Vector3(5,0,3),.7f,0,g);Lamp(p+new Vector3(-7,1,4),g,true);destinations.Add(p+new Vector3(0,.2f,0));
}
static void Beach(){
var g=Group("Bosque antiguo - playa y puerto",new Vector3(140,0,450));ClearPlot(new Vector3(140,8,450),new Vector2(50,78),2);
foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).ToArray())if(t&&!t.IsChildOf(root)){
 if(t.name.Contains("sendero")&&t.TryGetComponent<Renderer>(out var rr)&&rr.bounds.size.x>100&&Mathf.Abs(rr.bounds.center.z-450)<3&&rr.bounds.center.x>40&&rr.bounds.center.x<140)t.gameObject.SetActive(false);
 if(t.name.Contains("Acantilado rocoso")&&t.position.x>135&&t.position.x<215&&Mathf.Abs(t.position.z-450)<73){var instance=PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);if(instance)instance.SetActive(false);else t.gameObject.SetActive(false);}
}
foreach(var p in new[]{new Vector3(125,-12,376),new Vector3(125,-12,524)})Put(Pirate,"SM_Cliff_A",p,29,p.z<450?0:180,g);
for(int i=0;i<12;i++){float z=390+i*11;float x=104+Mathf.Sin(i*1.5f)*12;float y=SculptY(new Vector3(x,0,z),Vector3.zero,true);Put(Pirate,"SM_Cliff_B",new Vector3(x,y-3,z),7,i*71,g);}
// Dock lies on the existing shoreline. No boat or extra island is spawned.
for(int ix=0;ix<9;ix++)for(int iz=0;iz<2;iz++)Fit(Pirate,ix%2==0?"SM_WoodenFloor_A":"SM_WoodenFloor_B",new Vector3(146+ix*3,2.5f,416+iz*3),new Vector3(3,.35f,3),0,g);
for(int ix=0;ix<5;ix++)for(int side=0;side<2;side++){var p=new Vector3(145+ix*6,0,414+side*7);Put(Pirate,"SM_Beam_A",p,4,0,g);if(ix%2==0)Lamp(new Vector3(p.x,4,p.z),g);}
Put(Pirate,"SM_TradingTent_A",new Vector3(126,SculptY(new Vector3(126,0,405),Vector3.zero,true),405),3.5f,0,g);
for(int i=0;i<9;i++){var p=new Vector3(123+i%3*2.2f,0,422+i/3*3);p.y=SculptY(p,Vector3.zero,true);Put(Pirate,i%2==0?"SM_Barrel":"SM_Crate_A",p,i%2==0?1.1f:.9f,i*17,g);}
Put(Pirate,"SM_BarrelsInNet_A",new Vector3(140,2.5f,414),2.2f,0,g);Put(Pirate,"SM_Net_A",new Vector3(151,4,420),2.6f,0,g,false);
for(int i=0;i<6;i++){var p=new Vector3(110+i%2*10,0,390+i*23);p.y=SculptY(p,Vector3.zero,true);Put(Pirate,"SM_PalmTrunk_A",p,6,0,g);for(int leaf=0;leaf<7;leaf++)Put(Pirate,"SM_Palm_Branches_Cylinder_013",p+Vector3.up*5.5f,1.3f,leaf*360f/7,g,false);}
BuildWater(g);destinations.Add(new Vector3(164,2.9f,417));destinations.Add(new Vector3(120,SculptY(new Vector3(120,0,450),Vector3.zero,true)+.2f,450));report.Add("Beach and empty harbor on east shore of the existing northern forest island.");
}
static void BuildWater(Transform parent){
var ground=GameObject.Find("Isla del bosque antiguo").GetComponent<MeshFilter>();var vv=ground.sharedMesh.vertices;var original=ground.sharedMesh.triangles;var v=new List<Vector3>();var t=new List<int>();
for(int i=0;i<original.Length;i+=3){Vector3 a=ground.transform.TransformPoint(vv[original[i]]),b=ground.transform.TransformPoint(vv[original[i+1]]),c=ground.transform.TransformPoint(vv[original[i+2]]);if(Mathf.Min(a.y,Mathf.Min(b.y,c.y))>1.9f)continue;int n=v.Count;a.y=b.y=c.y=1.85f;v.AddRange(new[]{a,b,c});t.AddRange(new[]{n,n+1,n+2});}
var mesh=new Mesh{name="Bahía dentro de la isla",indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Output+"/Meshes/HarborWater.asset");var go=new GameObject("Agua de la bahía",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,true);go.transform.position=Vector3.zero;go.GetComponent<MeshFilter>().sharedMesh=mesh;var mat=new Material(Shader.Find("ROM/Harbor Water"));AssetDatabase.CreateAsset(mat,Output+"/Materials/HarborWater.mat");go.GetComponent<MeshRenderer>().sharedMaterial=mat;
}
static void Validate(){
Physics.SyncTransforms();var spawn=UnityEngine.Object.FindFirstObjectByType<MultiFPS.Gameplay.Gamemodes.SpawnpointsContainer>().Spawnpoints[0].position;if(!NavMesh.SamplePosition(spawn,out var origin,5,NavMesh.AllAreas))throw new Exception("Spawn has no NavMesh");
foreach(var p in mode.Checkpoints.Select(c=>c.Respawn.position).Concat(destinations)){var path=new NavMeshPath();bool sample=NavMesh.SamplePosition(p,out var hit,3,NavMesh.AllAreas);bool connected=sample&&NavMesh.CalculatePath(origin.position,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;report.Add("NAV "+p+" => "+hit.position+" connected="+connected);if(!connected)throw new Exception("Disconnected new destination "+p);}
foreach(var p in new[]{WestRoom,EclipseRoom})for(int i=0;i<=6;i++){var test=p+new Vector3(0,.15f,-25+i*5);if(test.z>p.z)test.y=p.y+.15f;else test.y=SculptY(test,p,false)+.15f;if(Physics.CheckCapsule(test+Vector3.up*.45f,test+Vector3.up*1.7f,.3f,MultiFPS.GameManager.environmentLayer,QueryTriggerInteraction.Ignore))throw new Exception("Bunker approach blocked "+test);}
if(mode.Checkpoints.Length!=7||mode.Encounters.Length!=18)throw new Exception("Gameplay zones altered");
foreach(var point in UnityEngine.Object.FindFirstObjectByType<MultiFPS.Gameplay.Gamemodes.SpawnpointsContainer>().Spawnpoints)if(Physics.CheckCapsule(point.position+Vector3.up*.5f,point.position+Vector3.up*1.7f,.3f,MultiFPS.GameManager.environmentLayer,QueryTriggerInteraction.Ignore))throw new Exception("Initial spawn blocked");
report.Add("PASS: same seven checkpoints and eighteen encounters, all paths connected; initial spawns and underground ramps clear.");
}

static Mesh SaveMesh(Mesh mesh,string path){var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!old){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}
static List<Vector3> Clip(List<Vector3> input,int axis,float value,bool below){var result=new List<Vector3>();if(input.Count==0)return result;var previous=input[input.Count-1];bool prevInside=below?previous[axis]<=value:previous[axis]>=value;foreach(var current in input){bool inside=below?current[axis]<=value:current[axis]>=value;if(inside!=prevInside){float t=(value-previous[axis])/(current[axis]-previous[axis]);result.Add(Vector3.Lerp(previous,current,t));}if(inside)result.Add(current);previous=current;prevInside=inside;}return result;}
static bool SubtractRoomTriangle(int a,int b,int c,List<Vector3> v,List<Vector2> uv,List<int> triangles,Vector3 room){
float minX=room.x-7.7f,maxX=room.x+7.7f,minZ=room.z+.2f,maxZ=room.z+16.2f;var original=new List<Vector3>{v[a],v[b],v[c]};if(original.Max(p=>p.x)<minX||original.Min(p=>p.x)>maxX||original.Max(p=>p.z)<minZ||original.Min(p=>p.z)>maxZ)return false;
var middle=Clip(Clip(original,0,minX,false),0,maxX,true);foreach(var polygon in new[]{Clip(original,0,minX,true),Clip(original,0,maxX,false),Clip(middle,2,minZ,true),Clip(middle,2,maxZ,false)}){if(polygon.Count<3)continue;int index=v.Count;foreach(var point in polygon){v.Add(point);uv.Add(new Vector2(point.x,point.z)/8);}for(int i=1;i<polygon.Count-1;i++){triangles.Add(index);triangles.Add(index+i);triangles.Add(index+i+1);}}return true;
}
public static void Refine(){try{
var scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);root=GameObject.Find("El Rift - Rally y costa pirata").transform;mode=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();report.Clear();destinations.Clear();
SculptGround("Isla de las ruinas",155,WestRoom,false);SculptGround("Santuario del Eclipse - isla de 440 metros",220,EclipseRoom,false);
var existingStone=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/Piedra del archivo.mat");var source=RallyMaterial("M_Stone_Wall_02");var material=new Material(Shader.Find("ROM/World Terrain"));CopyTexture(source,material,"_BaseMap",new[]{"_Base","_BaseMap"});material.SetTexture("_RockMap",material.GetTexture("_BaseMap"));material.SetFloat("_WorldScale",.3f);material.SetColor("_BaseColor",new Color(.68f,.7f,.73f));EditorUtility.CopySerialized(material,existingStone);EditorUtility.SetDirty(existingStone);UnityEngine.Object.DestroyImmediate(material);
foreach(var r in root.GetComponentsInChildren<MeshRenderer>())if(r.name=="Techo bajo el terreno")r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/ROM/Levels/FracturedForest/Materials/ForestSoil.mat");
var leaves=Surface("Hojas de palmera URP",AssetDatabase.LoadAssetAtPath<Material>("Assets/LeartesStudios/PirateTavern/Art/Materials/M_PalmOldBranch.mat"));leaves.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LeartesStudios/PirateTavern/Art/Textures/T_Coconut_Branch_BC.PNG"));leaves.SetColor("_BaseColor",new Color(.62f,.75f,.43f));leaves.SetFloat("_AlphaClip",1);leaves.SetFloat("_Cutoff",.4f);leaves.EnableKeyword("_ALPHATEST_ON");leaves.SetOverrideTag("RenderType","TransparentCutout");leaves.renderQueue=2450;EditorUtility.SetDirty(leaves);
foreach(var r in root.GetComponentsInChildren<MeshRenderer>())if(r.name.Contains("Palm_Branches"))r.sharedMaterials=r.sharedMaterials.Select(m=>leaves).ToArray();
var coast=root.Find("Bosque antiguo - playa y puerto");if(!coast.Find("Cornisa de la bahía")){
var cornice=new GameObject("Cornisa de la bahía").transform;cornice.SetParent(coast,false);cornice.position=Vector3.zero;
for(int i=0;i<10;i++){float z=-65+i*14.4f;float angle=Mathf.Atan2(175,z);float r=185*(1+.06f*Mathf.Sin(angle*5));float x=Mathf.Sqrt(Mathf.Max(0,r*r-z*z))-4;Put(Pirate,"SM_Cliff_B",new Vector3(x,-10,z+450),13,i*17,cornice);}
}
Physics.SyncTransforms();var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();var nav=surface.navMeshData;surface.BuildNavMesh();EditorUtility.CopySerialized(surface.navMeshData,nav);surface.RemoveData();surface.navMeshData=nav;surface.AddData();EditorUtility.SetDirty(nav);EditorUtility.SetDirty(surface);
destinations.AddRange(new[]{WestRoom+new Vector3(0,.2f,8),EclipseRoom+new Vector3(0,.2f,8),new Vector3(61,8.2f,-335),new Vector3(139,8.2f,-335),new Vector3(260,8.2f,-64),new Vector3(360,8.2f,-105),new Vector3(164,2.9f,417),new Vector3(120,6.22f,450)});Validate();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllLines(Docs+"/validation.txt",report);
MapVisualRepair.Capture("../rally-pirate-expansion/beach-overview",new Vector3(220,75,330),new Vector3(135,4,450));MapVisualRepair.Capture("../rally-pirate-expansion/beach-ground",new Vector3(110,9.5f,417),new Vector3(168,3,440));MapVisualRepair.Capture("../rally-pirate-expansion/archive-entry",WestRoom+new Vector3(14,10,-32),WestRoom+new Vector3(0,2,4));MapVisualRepair.Capture("../rally-pirate-expansion/archive-interior",WestRoom+new Vector3(0,2,1),WestRoom+new Vector3(0,2,14));MapVisualRepair.Capture("../rally-pirate-expansion/urban-buildings",new Vector3(100,28,-250),new Vector3(100,12,-335));EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}

public static void InspectInteriors(){try{
EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var log=new List<string>();Physics.SyncTransforms();foreach(var room in new[]{WestRoom,EclipseRoom}){
for(int x=-5;x<=5;x+=2)for(int z=2;z<=14;z+=2){var p=room+new Vector3(x,0,z);var hits=Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.7f,.3f,MultiFPS.GameManager.environmentLayer,QueryTriggerInteraction.Ignore);foreach(var hit in hits)log.Add("BLOCK "+p+" "+hit.name+" root="+hit.transform.root.name);}
foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.transform.IsChildOf(GameObject.Find("El Rift - Rally y costa pirata").transform)&&r.bounds.Intersects(new Bounds(room+new Vector3(0,1.5f,8),new Vector3(14,3,16)))))log.Add("LEGACY RENDER INSIDE "+room+" "+r.name+" "+r.bounds);
}
File.WriteAllLines(Docs+"/interior-clearance.txt",log);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}

static void CutSocle(string name,Vector3 room){
var go=GameObject.Find(name);var f=go.GetComponent<MeshFilter>();var source=f.sharedMesh;var v=source.vertices.Select(p=>go.transform.TransformPoint(p)).ToList();var uv=source.uv.ToList();if(uv.Count!=v.Count)uv=v.Select(p=>new Vector2(p.x,p.z)/8).ToList();var result=new List<int>();var input=source.triangles;
for(int i=0;i<input.Length;i+=3)if(!SubtractRoomTriangle(input[i],input[i+1],input[i+2],v,uv,result,room)){result.Add(input[i]);result.Add(input[i+1]);result.Add(input[i+2]);}
var mesh=new Mesh{name=name+" - cavidad interior",indexFormat=IndexFormat.UInt32};mesh.SetVertices(v.Select(p=>go.transform.InverseTransformPoint(p)).ToList());mesh.SetUVs(0,uv);mesh.SetTriangles(result,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=SaveMesh(mesh,Output+"/Meshes/"+(name.Contains("Eclipse")?"EclipseSocle":"WestSocle")+".asset");f.sharedMesh=mesh;foreach(var c in go.GetComponents<Collider>())c.enabled=false;var collider=go.GetComponent<MeshCollider>();if(!collider)collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.enabled=true;EditorUtility.SetDirty(f);EditorUtility.SetDirty(collider);
}
public static void FinalizeDetails(){try{
var scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);root=GameObject.Find("El Rift - Rally y costa pirata").transform;mode=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();report.Clear();destinations.Clear();
CutSocle("Isla de las ruinas - socle",WestRoom);CutSocle("Santuario del Eclipse - isla de 440 metros - socle",EclipseRoom);
var coast=root.Find("Bosque antiguo - playa y puerto");var old=coast.Find("Cornisa de la bahía");if(old)old.gameObject.SetActive(false);
if(!coast.Find("Cornisa de roca maciza")){var group=new GameObject("Cornisa de roca maciza").transform;group.SetParent(coast);for(int i=0;i<6;i++){float z=-65+i*26,angle=Mathf.Atan2(175,z),r=185*(1+.06f*Mathf.Sin(angle*5));float x=Mathf.Sqrt(Mathf.Max(0,r*r-z*z))+10;Put(Pirate,"SM_Cliff_A",new Vector3(x,-21,z+450),23,90+i*7,group);}}
foreach(var room in new[]{WestRoom,EclipseRoom}){var group=root.Find(room==WestRoom?"Archivo hundido de las ruinas":"Refugio de excavación del Eclipse");if(!group.Find("Apoyos de faroles")){var tables=new GameObject("Apoyos de faroles").transform;tables.SetParent(group);foreach(var p in new[]{room+new Vector3(-5,0,3),room+new Vector3(5,0,13)})Put(Rally,"SM_Cardbox_Crate_01",p,1,0,tables);}}
Physics.SyncTransforms();var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();var nav=surface.navMeshData;surface.BuildNavMesh();EditorUtility.CopySerialized(surface.navMeshData,nav);surface.RemoveData();surface.navMeshData=nav;surface.AddData();EditorUtility.SetDirty(nav);EditorUtility.SetDirty(surface);
destinations.AddRange(new[]{WestRoom+new Vector3(0,.2f,8),EclipseRoom+new Vector3(0,.2f,8),new Vector3(61,8.2f,-335),new Vector3(139,8.2f,-335),new Vector3(260,8.2f,-64),new Vector3(360,8.2f,-105),new Vector3(164,2.9f,417),new Vector3(120,6.22f,450)});Validate();
foreach(var room in new[]{WestRoom,EclipseRoom})for(int x=-5;x<=5;x+=2)for(int z=2;z<=14;z+=2){var p=room+new Vector3(x,0,z);foreach(var hit in Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.7f,.3f,MultiFPS.GameManager.environmentLayer,QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(root))throw new Exception("Legacy mesh inside room: "+hit.name+" at "+p);}
report.Add("PASS: original island undersides carved to clear both interiors; no legacy collider inside the room grids.");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllLines(Docs+"/validation.txt",report);
MapVisualRepair.Capture("../rally-pirate-expansion/beach-overview",new Vector3(220,75,330),new Vector3(135,4,450));MapVisualRepair.Capture("../rally-pirate-expansion/archive-interior",WestRoom+new Vector3(0,2,1),WestRoom+new Vector3(0,2,14));EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}

public static void FinishPalms(){try{
var scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);root=GameObject.Find("El Rift - Rally y costa pirata").transform;
var source=EditorSceneManager.OpenScene("Assets/LeartesStudios/PirateTavern/Scene/URP_PirateTavern.unity",OpenSceneMode.Additive);
var forest=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="CoconutPalm");
var trunk=forest.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.Contains("PalmTrunk")).OrderBy(r=>r.bounds.min.y).First();var origin=new Vector3(trunk.bounds.center.x,trunk.bounds.min.y,trunk.bounds.center.z);
var parts=forest.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh&&Vector2.Distance(new Vector2(r.bounds.center.x,r.bounds.center.z),new Vector2(origin.x,origin.z))<4.5f&&r.bounds.min.y>=origin.y-.25f&&r.bounds.max.y<origin.y+18).ToArray();if(parts.Length<10)throw new Exception("Cannot extract an authored palm cluster");
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);var proto=new GameObject("Palmera del paquete - combinada");var byMaterial=new Dictionary<Material,List<CombineInstance>>();
foreach(var part in parts){var mesh=part.GetComponent<MeshFilter>().sharedMesh;for(int sub=0;sub<mesh.subMeshCount;sub++){var m=part.sharedMaterials[Mathf.Min(sub,part.sharedMaterials.Length-1)];if(!byMaterial.TryGetValue(m,out var list)){list=new List<CombineInstance>();byMaterial[m]=list;}list.Add(new CombineInstance{mesh=mesh,subMeshIndex=sub,transform=Matrix4x4.Translate(-origin)*part.localToWorldMatrix});}}
foreach(var pair in byMaterial){var mesh=new Mesh{name="Palmera - "+pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh=SaveMesh(mesh,Output+"/Meshes/Palm_"+pair.Key.name+".asset");var go=new GameObject(pair.Key.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(proto.transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var m=pair.Key;if(m.name.Contains("Branch")||m.name.Contains("Leaf")){var converted=Surface("Palmera_"+m.name,m);converted.SetFloat("_AlphaClip",1);converted.SetFloat("_Cutoff",.25f);converted.SetColor("_BaseColor",Color.white);converted.EnableKeyword("_ALPHATEST_ON");converted.renderQueue=2450;EditorUtility.SetDirty(converted);m=converted;}go.GetComponent<MeshRenderer>().sharedMaterial=m;}
EditorSceneManager.CloseScene(source,true);var bounds=RenderBounds(proto);Directory.CreateDirectory(Output+"/Prefabs");AssetDatabase.Refresh();var prefab=PrefabUtility.SaveAsPrefabAsset(proto,Output+"/Prefabs/PiratePalm.prefab");UnityEngine.Object.DestroyImmediate(proto);
var coast=root.Find("Bosque antiguo - playa y puerto");foreach(var t in coast.GetComponentsInChildren<Transform>().Where(t=>t.name=="SM_PalmTrunk_A"||t.name=="SM_Palm_Branches_Cylinder_013").ToArray())t.gameObject.SetActive(false);
for(int i=0;i<6;i++){var p=new Vector3(110+i%2*10,0,390+i*23);p.y=SculptY(p,Vector3.zero,true);var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,coast);go.name="Palmera costera "+i;float scale=8/Mathf.Max(1,bounds.size.y);go.transform.localScale=Vector3.one*scale;go.transform.position=p-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;var collider=go.AddComponent<CapsuleCollider>();collider.center=new Vector3(bounds.center.x,bounds.min.y+bounds.size.y*.35f,bounds.center.z);collider.height=bounds.size.y*.7f;collider.radius=.4f/scale;foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;}
Physics.SyncTransforms();var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();var nav=surface.navMeshData;surface.BuildNavMesh();EditorUtility.CopySerialized(surface.navMeshData,nav);surface.RemoveData();surface.navMeshData=nav;surface.AddData();EditorUtility.SetDirty(nav);EditorUtility.SetDirty(surface);
mode=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();report.Clear();destinations.Clear();destinations.Add(new Vector3(164,2.9f,417));destinations.Add(WestRoom+new Vector3(0,.2f,8));destinations.Add(EclipseRoom+new Vector3(0,.2f,8));Validate();report.Add("Authored palm cluster baked: "+parts.Length+" mesh renderers into "+byMaterial.Count+" material groups; six coastal instances.");File.WriteAllLines(Docs+"/palm-validation.txt",report);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
MapVisualRepair.Capture("../rally-pirate-expansion/beach-overview",new Vector3(220,75,330),new Vector3(135,4,450));MapVisualRepair.Capture("../rally-pirate-expansion/beach-ground",new Vector3(110,9.5f,417),new Vector3(168,3,440));EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
