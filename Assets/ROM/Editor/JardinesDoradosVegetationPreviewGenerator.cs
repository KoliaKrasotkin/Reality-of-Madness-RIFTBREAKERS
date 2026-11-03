using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ROM.EditorTools {
// A disposable preview stage only. No integration, original-scene mutation or asset saving API.
public static class JardinesDoradosVegetationPreviewGenerator {
 const string Menu="ROM/Dungeon Ciudad del Sol/Jardines Dorados/Vegetacion exterior/";
 const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
 const string PondMaterialPath="Assets/ROM/Art/CiudadDelSol/Materials/JardinesDorados_Estanque_Agua_URP.mat";
 const string PondSourceMaterial="Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/Materials/Dec_Puddle_A.mat";
 const string PondPrefab="Assets/Standard Assets/Sun_Temple/Prefabs/Decals/Dec_Puddle_A.prefab";
 const string PondNormal="Assets/Standard Assets/Sun_Temple/Content/Textures/dec_puddle_N.tif";
 const string Zone="02 - Jardines Dorados";
 const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/Flora/";
 const string ST="Assets/Standard Assets/Sun_Temple/Prefabs/Natural/";
 static readonly Vector3 Center=new Vector3(-102,4,-18);
 static readonly Dictionary<string,string> Paths=new Dictionary<string,string>{
  {"Palm","Assets/ROM/Levels/RiftSetDressing/Prefabs/PiratePalm.prefab"},
  {"Bush1",ET+"Bush_01.prefab"},{"Bush2",ET+"Bush_02.prefab"},{"Bush3",ET+"Bush_03.prefab"},
  {"Grass1",ET+"Grass_01.prefab"},{"Grass3",ET+"Grass_03.prefab"},{"Grass5",ET+"Grass_05.prefab"},
  {"Dry",ET+"Grass_01_Dry.prefab"},
  {"FlowersA",ST+"Fol_BushFlower_A.prefab"},{"FlowersC",ST+"Fol_BushFlower_C.prefab"},
  {"FlowersE",ST+"Fol_BushFlower_E.prefab"},
  {"Ivy1",ET+"Ivy_01.prefab"},{"Ivy3",ET+"Ivy_03.prefab"},{"Ivy5",ET+"Ivy_05.prefab"}
 };
 sealed class Asset {public GameObject Prefab;public Bounds Bounds;}
 static Bounds TransformBounds(Bounds b,Matrix4x4 matrix){var result=new Bounds(matrix.MultiplyPoint3x4(b.center),Vector3.zero);for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)result.Encapsulate(matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));return result;}
 static Bounds Visible(GameObject g){var meshes=g.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled).ToArray();if(meshes.Length==0)throw new InvalidOperationException("Sin malla visible: "+g.name);var b=TransformBounds(meshes[0].sharedMesh.bounds,meshes[0].transform.localToWorldMatrix);foreach(var f in meshes.Skip(1))b.Encapsulate(TransformBounds(f.sharedMesh.bounds,f.transform.localToWorldMatrix));return b;}
 static string Hash(string path){using(var sha=SHA256.Create())using(var file=File.OpenRead(path))return Convert.ToBase64String(sha.ComputeHash(file));}
 static string Snapshot(Transform t){return string.Join("\n",t.GetComponentsInChildren<Transform>(true).SelectMany(x=>x.GetComponents<Component>()).Where(c=>c).Select(c=>c.GetInstanceID()+"|"+EditorJsonUtility.ToJson(c)));}
 static bool Foliage(Transform t){for(;t;t=t.parent)if(new[]{"Tree_","Fol_","Bush_","Ivy_","Grass_","PiratePalm"}.Any(n=>t.name.StartsWith(n,StringComparison.Ordinal)))return true;return false;}
 static bool OverlapXZ(Bounds a,Bounds b,float margin=0)=>a.min.x<b.max.x+margin&&a.max.x>b.min.x-margin&&a.min.z<b.max.z+margin&&a.max.z>b.min.z-margin;
 static bool Inside(Bounds b)=>b.min.x>=Center.x-46.8f&&b.max.x<=Center.x+46.8f&&b.min.z>=Center.z-46.8f&&b.max.z<=Center.z+46.8f;
 static Dictionary<string,Asset> Load(){var result=new Dictionary<string,Asset>();foreach(var item in Paths){var p=AssetDatabase.LoadAssetAtPath<GameObject>(item.Value);if(!p)throw new InvalidOperationException("Falta prefab: "+item.Value);var norm=Matrix4x4.TRS(Vector3.zero,p.transform.localRotation,p.transform.localScale)*p.transform.worldToLocalMatrix;Bounds b=default;bool first=true;foreach(var f in p.GetComponentsInChildren<MeshFilter>(true)){if(!f.sharedMesh||!f.GetComponent<Renderer>()||!f.GetComponent<Renderer>().enabled)continue;var next=TransformBounds(f.sharedMesh.bounds,norm*f.transform.localToWorldMatrix);if(first){b=next;first=false;}else b.Encapsulate(next);}if(first)throw new InvalidOperationException("Prefab vacio: "+item.Key);result.Add(item.Key,new Asset{Prefab=p,Bounds=b});}return result;}

 [MenuItem(Menu+"1. Previsualizar vegetacion temporal")]
 public static void Preview(){try{
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new InvalidOperationException("Fuera de Play y esperar compilacion/importacion.");
  if(StageUtility.GetCurrentStage() is GardenStage existing&&existing.Complete){existing.View(0);return;}
  if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())throw new InvalidOperationException("Cerrar primero la otra previsualizacion desde su propio menu.");
  var scene=SceneManager.GetSceneByPath(Target);if(!scene.IsValid()||!scene.isLoaded)throw new InvalidOperationException("Abrir Dungeon_Isla_del_Sol en el Editor; no se cambia de escena automaticamente.");
  var root=scene.GetRootGameObjects().Single(g=>g.name=="Dungeon_Ciudad_del_Sol_Generated").transform;
  var garden=root.Find(Zone);if(!garden||Vector3.Distance(garden.position,Center)>.01f||!garden.Find("JardinesDorados_Architecture"))throw new InvalidOperationException("No se reconoce el jardin aprobado.");
  var routes=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("01-02 -")||t.name.StartsWith("02-03 -")).ToArray();
  if(routes.Length!=2)throw new InvalidOperationException("Se esperan los dos recorridos originales del jardin.");
  var assets=Load();Debug.Log("JARDINES_FLORA_ASSETS: "+string.Join("; ",assets.Select(a=>a.Key+" "+a.Value.Bounds.size.ToString("F2"))));
  var stage=ScriptableObject.CreateInstance<GardenStage>();stage.Source=scene;stage.OriginalGarden=garden;stage.Routes=routes;stage.Assets=assets;StageUtility.GoToStage(stage,true);
 }catch(Exception e){Debug.LogException(e);}}
 [MenuItem(Menu+"2. Vista general")]
 public static void General(){if(StageUtility.GetCurrentStage() is GardenStage g&&g.Complete)g.View(0);}
 [MenuItem(Menu+"3. Vista cercana patios recuperados")]
 public static void Courts(){if(StageUtility.GetCurrentStage() is GardenStage g&&g.Complete)g.View(1);}
 [MenuItem(Menu+"4. Vista cercana recorridos y estanque")]
 public static void PathsView(){if(StageUtility.GetCurrentStage() is GardenStage g&&g.Complete)g.View(2);}
 [MenuItem(Menu+"5. Vista cenital y validar")]
 public static void ValidateView(){if(StageUtility.GetCurrentStage() is GardenStage g&&g.Complete){g.Validate();g.View(3);}}
 [MenuItem(Menu+"6. Vista cercana del agua corregida")]
 public static void PondView(){if(StageUtility.GetCurrentStage() is GardenStage g&&g.Complete){g.Validate();g.View(4);}}
 [MenuItem(Menu+"9. Cerrar sin guardar")]
 public static void Close(){if(StageUtility.GetCurrentStage() is GardenStage)StageUtility.GoToMainStage();}

 sealed class Plant {public string Key;public GameObject Go;public Bounds Bounds;public bool Hanging;}
 sealed class GardenStage:PreviewSceneStage {
  internal Scene Source;internal Transform OriginalGarden;internal Transform[] Routes;internal Dictionary<string,Asset> Assets;internal bool Complete;
  Transform context,garden,generated;string originalHash,originalState,copiedState;bool originalDirty;GameObject[] originalRoots;
  readonly List<Plant> plants=new List<Plant>();readonly List<Bounds> reserves=new List<Bounds>();readonly List<Bounds> obstacles=new List<Bounds>();readonly List<Collider> floors=new List<Collider>();
  readonly Dictionary<Material,Material> materials=new Dictionary<Material,Material>();readonly Dictionary<string,int> rejected=new Dictionary<string,int>();
  readonly System.Random random=new System.Random(73921);
  Camera camera;Transform rig;
  MeshRenderer pond;Material[] originalPondMaterials;Material water;Matrix4x4 pondMatrix;Mesh pondMesh;
  string sourcePondHash,prefabPondHash;
  protected override GUIContent CreateHeaderContent()=>new GUIContent("Jardines Dorados · VEGETACION TEMPORAL · NO INTEGRAR");
  static GameObject Copy(Transform original,Transform parent){var g=Object.Instantiate(original.gameObject,parent);g.name=original.name;g.transform.SetPositionAndRotation(original.position,original.rotation);g.transform.localScale=original.lossyScale;return g;}
  protected override bool OnOpenStage(){if(!OriginalGarden||Assets==null||!base.OnOpenStage())return false;try{
   originalHash=Hash(Target);originalState=Snapshot(OriginalGarden);originalDirty=Source.isDirty;originalRoots=Source.GetRootGameObjects();var previewScene=scene;previewScene.name="TEMP_JardinesDorados_Vegetacion";
   var holder=new GameObject("Contexto aprobado - copia intacta");SceneManager.MoveGameObjectToScene(holder,scene);context=holder.transform;holder.SetActive(false);
   garden=Copy(OriginalGarden,context).transform;foreach(var route in Routes)Copy(route,context);
   foreach(var b in context.GetComponentsInChildren<MonoBehaviour>(true))if(b)b.enabled=false;
   foreach(var a in context.GetComponentsInChildren<AudioSource>(true)){a.playOnAwake=false;a.enabled=false;}
   foreach(var p in context.GetComponentsInChildren<ParticleSystem>(true))p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   holder.SetActive(true);Physics.SyncTransforms();copiedState=Snapshot(context);
   var stage=new GameObject("JardinesDorados_Vegetacion_Temporal");SceneManager.MoveGameObjectToScene(stage,scene);generated=stage.transform;
   PrepareReservations();Compose();AddRig();ApplyPond();Physics.SyncTransforms();Validate();Complete=true;
   EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this){Selection.activeGameObject=null;View(4);}};
   Debug.Log("JARDINES_FLORA_LISTA: "+plants.Count+" instancias nuevas. Opciones2 general,3 patios,4 recorridos,5 cenital/validar,9 cerrar. No integrar ni guardar.");return true;
  }catch(Exception e){Debug.LogException(e);Debug.LogError("JARDINES_FLORA_NO_VALIDADA: copia descartada; original no modificado.");base.OnCloseStage();return false;}}
  void PrepareReservations(){
   var field=typeof(JardinesDoradosArchitectureGenerator).GetField("Reservations",BindingFlags.Static|BindingFlags.NonPublic);
   if(field==null)throw new InvalidOperationException("No se encuentran las reservas aprobadas.");reserves.AddRange((Bounds[])field.GetValue(null));
   foreach(var t in context.GetComponentsInChildren<Transform>())if(t.name=="Galeria lateral elevada +4"||t.name.StartsWith("Staircase_",StringComparison.Ordinal)){
    var b=Visible(t.gameObject);b.Expand(new Vector3(2.4f,9,2.4f));reserves.Add(b);
   }
   foreach(var route in context.Cast<Transform>().Where(t=>t!=garden)){var b=Visible(route.gameObject);b.Expand(new Vector3(3,10,3));reserves.Add(b);}
   foreach(var c in garden.GetComponentsInChildren<Collider>())if(c.enabled&&!c.isTrigger){var b=c.bounds;if(b.size.sqrMagnitude<.001f)continue;if(b.max.y<=4.08f&&b.max.y>=3.98f)floors.Add(c);}
   foreach(var r in garden.GetComponentsInChildren<MeshRenderer>())if(r.enabled&&!Foliage(r.transform)){
    var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;var b=TransformBounds(f.sharedMesh.bounds,f.transform.localToWorldMatrix);
    if(b.max.y>4.3f&&b.min.y<16)obstacles.Add(b);
   }
   if(floors.Count<200)throw new InvalidOperationException("No se reconoce el piso existente Y4.");
  }
  bool Ground(float x,float z){var ray=new Ray(new Vector3(x,4.8f,z),Vector3.down);return floors.Any(c=>c.Raycast(ray,out var hit,1)&&Mathf.Abs(hit.point.y-4)<.09f);}
  bool Reject(string reason){rejected[reason]=rejected.TryGetValue(reason,out var n)?n+1:1;return false;}
  bool Accept(Bounds b,bool hanging,Bounds[] parts=null){
   if(!Inside(b))return Reject("perimetro");
   if(reserves.Any(r=>OverlapXZ(r,b,.35f)))return Reject("reserva transitable");
   if(!hanging){
    foreach(var p in new[]{b.center,new Vector3(b.min.x,4,b.min.z),new Vector3(b.max.x,4,b.max.z),new Vector3(b.min.x,4,b.max.z),new Vector3(b.max.x,4,b.min.z)})if(!Ground(p.x,p.z))return Reject("sin suelo continuo");
    if(obstacles.Any(o=>parts==null?o.Intersects(b):parts.Any(part=>o.Intersects(part))))return Reject("arquitectura existente");
   }
   if(plants.Any(p=>Vector2.Distance(new Vector2(p.Bounds.center.x,p.Bounds.center.z),new Vector2(b.center.x,b.center.z))<(hanging?.9f:Mathf.Min(1.6f,.35f+Mathf.Min(b.size.x,b.size.z)*.23f))))return Reject("separacion");
   return true;
  }
  bool Put(string key,float x,float z,float yaw,float scale=1,Bounds? hangingBounds=null){
   var a=Assets[key];var q=Quaternion.Euler(0,yaw,0);var sc=Vector3.one*scale;
   var b=TransformBounds(a.Bounds,Matrix4x4.TRS(Vector3.zero,q,sc));bool hanging=hangingBounds.HasValue;
   var position=hanging?hangingBounds.Value.center-b.center:new Vector3(Center.x+x-b.center.x,4-b.min.y+.015f,Center.z+z-b.center.z);b.center+=position;
   Bounds[] parts=null;
   if(key=="Palm"){
    var norm=Matrix4x4.TRS(position,q*a.Prefab.transform.localRotation,a.Prefab.transform.localScale*scale)*a.Prefab.transform.worldToLocalMatrix;
    parts=a.Prefab.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh).Select(f=>TransformBounds(f.sharedMesh.bounds,norm*f.transform.localToWorldMatrix)).ToArray();
   }
   if(!Accept(b,hanging,parts))return false;
   var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,scene);go.transform.SetParent(generated,true);go.transform.SetPositionAndRotation(position,q*a.Prefab.transform.localRotation);go.transform.localScale=a.Prefab.transform.localScale*scale;go.name=key+" - "+(plants.Count+1).ToString("D2");
   foreach(var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))if(behaviour)behaviour.enabled=false;
   foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;
   if(key=="Palm"){
    // Only a low trunk capsule; never a canopy collider. Original prefab remains untouched.
    var mesh=go.GetComponentsInChildren<MeshFilter>().FirstOrDefault(f=>f.sharedMesh&&(f.name.IndexOf("Trunk",StringComparison.OrdinalIgnoreCase)>=0||f.sharedMesh.name.IndexOf("Trunk",StringComparison.OrdinalIgnoreCase)>=0));
    if(!mesh)throw new InvalidOperationException("No se identifica la malla del tronco de la palmera.");
    var solid=TransformBounds(mesh.sharedMesh.bounds,mesh.transform.localToWorldMatrix);
    var trunk=go.AddComponent<CapsuleCollider>();trunk.direction=1;trunk.radius=Mathf.Clamp(Mathf.Max(solid.size.x,solid.size.z)*.5f,.25f,.7f);trunk.height=Mathf.Min(4.2f,solid.size.y);trunk.center=go.transform.InverseTransformPoint(new Vector3(solid.center.x,solid.min.y+trunk.height*.5f,solid.center.z));
   }
   foreach(var r in go.GetComponentsInChildren<MeshRenderer>(true)){
    r.shadowCastingMode=key.StartsWith("Grass")||key=="Dry"||key.StartsWith("Flowers")?ShadowCastingMode.Off:ShadowCastingMode.On;
    r.receiveShadows=true;r.sharedMaterials=r.sharedMaterials.Select(FoliageMaterial).ToArray();
   }
   PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
   plants.Add(new Plant{Key=key,Go=go,Bounds=Visible(go),Hanging=hanging});return true;
  }
  Material FoliageMaterial(Material original){
   if(!original||!original.shader||!original.shader.isSupported)throw new InvalidOperationException("Material/shader no disponible: "+(original?original.name:"null"));
   bool leaves=new[]{"Leaves","Grass","Branch","BushFlower","BushFlowers"}.Any(s=>original.name.IndexOf(s,StringComparison.OrdinalIgnoreCase)>=0);
   if(!leaves)return original;if(materials.TryGetValue(original,out var copy))return copy;
   copy=new Material(original){name=original.name+" - SOLO PREVIEW",hideFlags=HideFlags.HideAndDontSave};
   if(copy.HasProperty("_Cull"))copy.SetFloat("_Cull",0);
   if(copy.HasProperty("_AlphaClip")){
    var texture=copy.HasProperty("_BaseMap")?copy.GetTexture("_BaseMap"):copy.mainTexture;
    var importer=texture?AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter:null;
    if(importer&&importer.DoesSourceTextureHaveAlpha()){copy.SetFloat("_AlphaClip",1);copy.EnableKeyword("_ALPHATEST_ON");if(copy.HasProperty("_Cutoff"))copy.SetFloat("_Cutoff",.4f);}
   }
   copy.enableInstancing=true;materials.Add(original,copy);return copy;
  }
  void Scatter(string[] keys,int desired){int count=0;for(int attempt=0;attempt<1600&&count<desired;attempt++){
   // Unequal patches, jitter and sparse transition strips, never a repeating lattice.
   var patches=new[]{new Vector3(-35,-35,8),new Vector3(-35,36,8),new Vector3(38,-35,7),new Vector3(7,-39,8),new Vector3(-8,38,11),new Vector3(-33,0,6)};
   var patch=patches[random.Next(patches.Length)];double angle=random.NextDouble()*Math.PI*2;float radius=Mathf.Sqrt((float)random.NextDouble())*patch.z;
   string key=keys[random.Next(keys.Length)];if(Put(key,patch.x+Mathf.Cos((float)angle)*radius,patch.y+Mathf.Sin((float)angle)*radius,(float)random.NextDouble()*360,.9f+(float)random.NextDouble()*.2f))count++;
  }}
  void Ivy(){
   var walls=garden.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&!Foliage(c.transform)&&c.name.IndexOf("Wall",StringComparison.OrdinalIgnoreCase)>=0&&c.bounds.size.y>3).OrderBy(c=>c.bounds.center.x).ThenBy(c=>c.bounds.center.z).ToArray();
   foreach(var wall in walls){if(plants.Count(p=>p.Hanging)>=8)break;var wb=wall.bounds;bool thinX=wb.size.x<wb.size.z;
    foreach(float shift in new[]{-.27f,.23f}){
     if(plants.Count(p=>p.Hanging)>=8)break;string key=new[]{"Ivy1","Ivy3","Ivy5"}[random.Next(3)];var own=Assets[key].Bounds;bool nativeThinX=own.size.x<own.size.z;float yaw=thinX==nativeThinX?0:90;
     var b=TransformBounds(own,Matrix4x4.Rotate(Quaternion.Euler(0,yaw,0)));var outside=new Vector3(thinX?wb.max.x+2:wb.center.x+wb.size.x*shift,Mathf.Clamp(wb.max.y-1.5f,6,10),thinX?wb.center.z+wb.size.z*shift:wb.max.z+2);var direction=thinX?Vector3.left:Vector3.back;
     if(!wall.Raycast(new Ray(outside,direction),out var hit,5))continue;
     var center=hit.point-direction*(thinX?b.extents.x+.025f:b.extents.z+.025f);center.y=Mathf.Max(4.2f+b.extents.y,hit.point.y);
     b.center=center;Put(key,0,0,yaw,1,b);
    }
   }
  }
  void Compose(){
   foreach(var p in new[]{new Vector2(2,37),new Vector2(32,-38),new Vector2(-30,-34),new Vector2(-23,39),new Vector2(-32,0)})if(plants.Count(x=>x.Key=="Palm")<2)Put("Palm",p.x,p.y,0);
   Scatter(new[]{"Bush1","Bush2","Bush3"},16);Scatter(new[]{"FlowersA","FlowersC","FlowersE"},16);
   Scatter(new[]{"Grass1","Grass3","Grass5","Dry"},40);Ivy();
   if(plants.Count<60||plants.Count>100)throw new InvalidOperationException("Densidad fuera de60-100: "+plants.Count+". "+string.Join(";",rejected.Select(p=>p.Key+"="+p.Value)));
  }
  void ApplyPond(){
   sourcePondHash=Hash(PondSourceMaterial);prefabPondHash=Hash(PondPrefab);
   pond=garden.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Dec_Puddle_A");
   pondMatrix=pond.transform.localToWorldMatrix;pondMesh=pond.GetComponent<MeshFilter>().sharedMesh;
   originalPondMaterials=pond.sharedMaterials;
   water=AssetDatabase.LoadAssetAtPath<Material>(PondMaterialPath);
   if(!water){
    var shader=Shader.Find("Universal Render Pipeline/Lit");var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(PondNormal);
    if(!shader||!shader.isSupported||!normal)throw new InvalidOperationException("No se encuentra URP/Lit o la normal original del charco.");
    water=new Material(shader){name="JardinesDorados_Estanque_Agua_URP"};
    water.SetColor("_BaseColor",new Color(.035f,.115f,.125f,.92f));
    water.SetFloat("_Metallic",0);water.SetFloat("_Smoothness",.84f);
    water.SetTexture("_BumpMap",normal);water.SetTextureScale("_BumpMap",new Vector2(1,2));water.SetFloat("_BumpScale",.18f);water.EnableKeyword("_NORMALMAP");
    water.SetFloat("_AlphaClip",0);water.DisableKeyword("_ALPHATEST_ON");water.SetFloat("_AlphaToMask",0);
    water.SetFloat("_Surface",1);water.SetFloat("_Blend",0);water.SetFloat("_Cull",0);water.SetFloat("_ZWrite",0);
    water.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);water.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
    water.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);water.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
    water.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");water.DisableKeyword("_ALPHAPREMULTIPLY_ON");water.DisableKeyword("_ALPHAMODULATE_ON");
    water.SetFloat("_EnvironmentReflections",1);water.SetFloat("_SpecularHighlights",1);water.SetColor("_EmissionColor",Color.black);water.DisableKeyword("_EMISSION");
    water.SetOverrideTag("RenderType","Transparent");water.renderQueue=(int)RenderQueue.Transparent;water.SetShaderPassEnabled("ShadowCaster",false);
    string folder="Assets";foreach(string part in "ROM/Art/CiudadDelSol/Materials".Split('/')){if(!AssetDatabase.IsValidFolder(folder+"/"+part))AssetDatabase.CreateFolder(folder,part);folder+="/"+part;}
    AssetDatabase.CreateAsset(water,PondMaterialPath);AssetDatabase.SaveAssetIfDirty(water);
   }
   if(!water.shader||water.shader.name!="Universal Render Pipeline/Lit"||!water.shader.isSupported||water.GetTexture("_BumpMap")!=AssetDatabase.LoadAssetAtPath<Texture2D>(PondNormal))throw new InvalidOperationException("Material independiente del estanque no valido.");
   // Water's dielectric reflectance is about 2%; avoid a plastic-looking 4% highlight.
   // This asset belongs exclusively to this preview pass, never to the source prefab.
   water.SetFloat("_WorkflowMode",0);water.EnableKeyword("_SPECULAR_SETUP");
   water.SetColor("_SpecColor",new Color(.02f,.025f,.025f,1));water.SetFloat("_Smoothness",.78f);
   EditorUtility.SetDirty(water);AssetDatabase.SaveAssetIfDirty(water);
   pond.sharedMaterial=water;
   Debug.Log("JARDINES_AGUA_CORREGIDA: "+PondMaterialPath+"; URP/Lit, azul petroleo verdoso, alfa0.92, normal0.18, suavidad0.78, reflectancia2-2.5%, metal0; sin simulacion, camaras de reflejo ni geometria nueva. Solo renderer de copia temporal.");
  }
  string ContextSnapshot(){
   if(!pond)return Snapshot(context);
   var current=pond.sharedMaterials;
   try{pond.sharedMaterials=originalPondMaterials;return Snapshot(context);}
   finally{pond.sharedMaterials=current;}
  }
  void AddRig(){var go=new GameObject("Inspeccion temporal - no integrar");SceneManager.MoveGameObjectToScene(go,scene);rig=go.transform;
   var cam=new GameObject("Camara de revision");cam.transform.SetParent(rig,false);camera=cam.AddComponent<Camera>();camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=400;camera.backgroundColor=new Color(.09f,.11f,.14f);camera.clearFlags=CameraClearFlags.SolidColor;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.enabled=false;
   for(int i=0;i<2;i++){var light=new GameObject("Luz neutra de inspeccion");light.transform.SetParent(rig,false);light.transform.rotation=Quaternion.Euler(i==0?48:35,i==0?-35:140,0);var l=light.AddComponent<Light>();l.type=LightType.Directional;l.color=i==0?new Color(1,.94f,.83f):new Color(.78f,.87f,1);l.intensity=i==0?1.3f:.55f;l.shadows=i==0?LightShadows.Soft:LightShadows.None;}
  }
  internal void Validate(){
   Physics.SyncTransforms();if(Hash(Target)!=originalHash||Source.isDirty!=originalDirty||!Source.GetRootGameObjects().SequenceEqual(originalRoots)||Snapshot(OriginalGarden)!=originalState)throw new InvalidOperationException("El original cambio durante la revision; no guardar ni integrar.");
   // New vegetation is a child of the copy; exclude it while checking the untouched approved context.
   string copied=ContextSnapshot();
   if(copied!=copiedState)throw new InvalidOperationException("La arquitectura o vegetacion aprobada copiada cambio.");
   if(pond&&(pond.sharedMaterial!=water||pond.transform.localToWorldMatrix!=pondMatrix||pond.GetComponent<MeshFilter>().sharedMesh!=pondMesh||Hash(PondSourceMaterial)!=sourcePondHash||Hash(PondPrefab)!=prefabPondHash))throw new InvalidOperationException("Cambio no autorizado en estanque, geometria o recursos originales.");
   if(plants.Count<60||plants.Count>100||plants.Select(p=>p.Go.transform.position.ToString("F3")).Distinct().Count()!=plants.Count)throw new InvalidOperationException("Cantidad/duplicado de nuevas plantas.");
   foreach(var p in plants){if(reserves.Any(b=>OverlapXZ(p.Bounds,b,.32f))||!Inside(p.Bounds))throw new InvalidOperationException("Planta invade reserva: "+p.Go.name);if(p.Key!="Palm"&&p.Go.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))throw new InvalidOperationException("Collider innecesario: "+p.Go.name);}
   foreach(var p in plants.Where(p=>p.Key=="Palm"))foreach(var c in p.Go.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))if(reserves.Any(b=>OverlapXZ(b,c.bounds,.6f))||obstacles.Any(b=>b.Intersects(c.bounds))||garden.GetComponentsInChildren<Collider>().Any(other=>other.enabled&&!other.isTrigger&&other.bounds.max.y>4.3f&&other.bounds.Intersects(c.bounds)))throw new InvalidOperationException("Tronco obstruido: "+p.Go.name);
   int renderers=plants.Sum(p=>p.Go.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled));long triangles=plants.Sum(p=>p.Go.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled).Sum(f=>(long)Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(s=>(long)f.sharedMesh.GetIndexCount(s))/3));
   string report="JARDINES_FLORA_VALIDADA: "+plants.Count+" instancias; "+renderers+" renderizadores; "+plants.Sum(p=>p.Go.GetComponentsInChildren<Collider>().Count(c=>c.enabled))+" colliders nuevos; "+triangles+" triangulos estimados (sin descontar culling).\n"+string.Join("; ",plants.GroupBy(p=>p.Key).Select(g=>g.Key+"="+g.Count()))+"\nMateriales URP revisados; clones de follaje solo en memoria. Reservas, suelo bajo plantas, arquitectura y archivo original conservados. Comprobaciones estaticas; FPS y recorrido manual pendientes.\nDescartes de candidatos: "+string.Join("; ",rejected.Select(p=>p.Key+"="+p.Value));
   Directory.CreateDirectory("Temp/ROM_Reviews/JardinesVegetacion");File.WriteAllText("Temp/ROM_Reviews/JardinesVegetacion/Informe.txt",report);Debug.Log(report);
  }
  internal void View(int index){var view=SceneView.lastActiveSceneView;if(!view)view=EditorWindow.GetWindow<SceneView>();
   var pivots=new[]{Center+Vector3.up*5,Center+new Vector3(-25,4,-24),Center+new Vector3(7,3,0),Center,Center+new Vector3(3,.5f,0)};var rotations=new[]{Quaternion.Euler(52,-30,0),Quaternion.Euler(27,-24,0),Quaternion.Euler(35,-42,0),Quaternion.Euler(90,0,0),Quaternion.Euler(38,-35,0)};var sizes=new[]{67f,29f,35f,61f,17f};
   view.sceneLighting=true;view.LookAtDirect(pivots[index],rotations[index],sizes[index]);view.orthographic=index==3;view.Repaint();camera.transform.rotation=rotations[index];camera.transform.position=pivots[index]-camera.transform.forward*140;camera.orthographicSize=sizes[index]*.72f;
  }
  protected override void OnCloseStage(){foreach(var m in materials.Values)if(m)Object.DestroyImmediate(m);materials.Clear();base.OnCloseStage();}
 }
}
}
