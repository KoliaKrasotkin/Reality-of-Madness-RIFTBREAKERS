using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ROM.EditorTools {
// Independent, disposable visual trial. Never invokes any approved architecture/void generator.
// No save, integration, network, runtime-gameplay or original-asset mutation entry points.
public static class PlazaOccidentalDimensionalPreviewGenerator {
 const string Menu="ROM/Dungeon Ciudad del Sol/Grieta occidental - prueba temporal/";
 const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
 const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/";
 static readonly Bounds Region=new Bounds(new Vector3(-45,4,12),new Vector3(70,12,84));
 static readonly Bounds ContextRegion=new Bounds(new Vector3(-34,0,12),new Vector3(162,52,160));
 static readonly Dictionary<string,string> Paths=new Dictionary<string,string>{
  {"Aerial","Assets/ROM/Art/RiftCrystals/Prefabs/CrystalAerial.prefab"},
  {"Ground","Assets/ROM/Art/RiftCrystals/Prefabs/CrystalGround.prefab"},
  {"Column1",ET+"Damaged/Column_Broken_01.prefab"},{"Column2",ET+"Damaged/Column_Broken_02.prefab"},
  {"Wall",ET+"Damaged/Broken_Wall_04.prefab"},{"Ruin",ET+"Damaged/Building_Ruins_02.prefab"},
  {"Bricks1",ET+"Damaged/Bricks_01.prefab"},{"Bricks3",ET+"Damaged/Bricks_03.prefab"},
  {"Rock",ET+"Cliffs/Cliff_Formation_03.prefab"},
  {"Rock2",ET+"Cliffs/Cliff_Formation_07.prefab"},
  {"Arch",ET+"Bases/Base_Arch_Tall_Broken_01.prefab"},
  {"Arch2",ET+"Bases/Thin/Arch_06_Broken_01.prefab"},
  {"Wall2",ET+"Damaged/Broken_Wall_08.prefab"},
  {"Ruin2",ET+"Damaged/Building_Ruins_07.prefab"},
  {"Motes","Assets/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectile VFX loop/Projectile 15 pink crystal.prefab"},
  {"Mist",ET+"VFX/Fog_Big_FX.prefab"}
 };
 sealed class MeshData {public Vector3[] Vertices;public int[] Indices;}
 sealed class Shape {public Bounds Bounds;public Vector3[] Vertices;public int[] Indices;public string Name;}
 sealed class Asset {public GameObject Prefab;public Bounds Bounds;}
 static readonly Dictionary<Mesh,MeshData> Meshes=new Dictionary<Mesh,MeshData>();
 static string Hash(string path){using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return Convert.ToBase64String(sha.ComputeHash(stream));}
 static string Signature(GameObject[] roots)=>string.Join("\n",roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).SelectMany(t=>t.GetComponents<Component>()).Where(c=>c).Select(c=>c.GetInstanceID()+"|"+EditorJsonUtility.ToJson(c)));
 static Bounds WorldBounds(Bounds b,Matrix4x4 m){var result=new Bounds(m.MultiplyPoint3x4(b.center),Vector3.zero);for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)result.Encapsulate(m.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));return result;}
 static Bounds Visible(GameObject g){bool first=true;Bounds b=default;foreach(var f in g.GetComponentsInChildren<MeshFilter>()){if(!f.sharedMesh||!f.GetComponent<Renderer>()||!f.GetComponent<Renderer>().enabled)continue;var v=WorldBounds(f.sharedMesh.bounds,f.transform.localToWorldMatrix);if(first){b=v;first=false;}else b.Encapsulate(v);}return b;}
 static MeshData Read(Mesh mesh){if(Meshes.TryGetValue(mesh,out var result))return result;
  using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh)){
   var md=data[0];using(var vertices=new NativeArray<Vector3>(md.vertexCount,Allocator.Temp)){
    md.GetVertices(vertices);var indices=new List<int>();
    for(int s=0;s<md.subMeshCount;s++){var sub=md.GetSubMesh(s);if(sub.topology!=MeshTopology.Triangles)continue;using(var ids=new NativeArray<int>(sub.indexCount,Allocator.Temp)){md.GetIndices(ids,s,true);indices.AddRange(ids.ToArray());}}
    result=new MeshData{Vertices=vertices.ToArray(),Indices=indices.ToArray()};Meshes[mesh]=result;return result;
   }
  }
 }
 static Shape ShapeOf(Mesh mesh,Matrix4x4 m,string name){var data=Read(mesh);return new Shape{Bounds=WorldBounds(mesh.bounds,m),Vertices=data.Vertices.Select(m.MultiplyPoint3x4).ToArray(),Indices=data.Indices,Name=name};}
 static bool RayTriangle(Ray ray,Vector3 a,Vector3 b,Vector3 c,out float distance){distance=0;var e=b-a;var f=c-a;var h=Vector3.Cross(ray.direction,f);float det=Vector3.Dot(e,h);if(Mathf.Abs(det)<.000001f)return false;float inv=1/det;var s=ray.origin-a;float u=Vector3.Dot(s,h)*inv;if(u<0||u>1)return false;var q=Vector3.Cross(s,e);float v=Vector3.Dot(ray.direction,q)*inv;if(v<0||u+v>1)return false;distance=Vector3.Dot(f,q)*inv;return distance>.0001f;}
 static bool Separated(Vector3 axis,Vector3 a,Vector3 b,Vector3 c,Vector3 ext){if(axis.sqrMagnitude<.00000001f)return false;float p=Vector3.Dot(a,axis),q=Vector3.Dot(b,axis),r=Vector3.Dot(c,axis);float radius=ext.x*Mathf.Abs(axis.x)+ext.y*Mathf.Abs(axis.y)+ext.z*Mathf.Abs(axis.z);return Mathf.Min(p,Mathf.Min(q,r))>radius||Mathf.Max(p,Mathf.Max(q,r))< -radius;}
 static bool TriangleBox(Vector3 a,Vector3 b,Vector3 c,Bounds box){a-=box.center;b-=box.center;c-=box.center;var ext=box.extents;
  var axes=new[]{Vector3.right,Vector3.up,Vector3.forward};foreach(var axis in axes)if(Separated(axis,a,b,c,ext))return false;
  var edges=new[]{b-a,c-b,a-c};if(Separated(Vector3.Cross(edges[0],edges[1]),a,b,c,ext))return false;
  foreach(var edge in edges)foreach(var axis in axes)if(Separated(Vector3.Cross(edge,axis),a,b,c,ext))return false;return true;
 }
 static bool Intersects(Shape shape,Bounds box){if(!shape.Bounds.Intersects(box))return false;var ids=shape.Indices;var vs=shape.Vertices;
  for(int i=0;i<ids.Length;i+=3)if(TriangleBox(vs[ids[i]],vs[ids[i+1]],vs[ids[i+2]],box))return true;
  // Catch a candidate contained wholly inside a closed original support.
  if(shape.Bounds.Contains(box.center)){var ray=new Ray(box.center,new Vector3(1,.01731f,.01357f).normalized);var hits=new List<float>();for(int i=0;i<ids.Length;i+=3)if(RayTriangle(ray,vs[ids[i]],vs[ids[i+1]],vs[ids[i+2]],out float d)&&!hits.Any(h=>Mathf.Abs(h-d)<.001f))hits.Add(d);if(hits.Count%2==1)return true;}return false;
 }
 sealed class Inspection {
  public Scene Source;public GameObject[] Roots;public string Hash,State;public bool Dirty;
  public readonly List<Shape> Geometry=new List<Shape>();public readonly List<Bounds> Solids=new List<Bounds>(),Walks=new List<Bounds>();public readonly List<Vector3> FreeCells=new List<Vector3>();
  public readonly Dictionary<string,string> Assets=new Dictionary<string,string>();public Dictionary<string,Asset> Prototypes;
  public void Verify(){if(Hash!=PlazaOccidentalDimensionalPreviewGenerator.Hash(Target)||Dirty!=Source.isDirty||!Roots.SequenceEqual(Source.GetRootGameObjects())||State!=Signature(Roots))throw new InvalidOperationException("Cambio en escena original; detener, no guardar.");foreach(var item in Assets)if(PlazaOccidentalDimensionalPreviewGenerator.Hash(item.Key)!=item.Value)throw new InvalidOperationException("Recurso original alterado: "+item.Key);}
  public bool Free(Bounds bounds,float margin,out string reason){var b=bounds;b.Expand(margin*2);
   if(!Region.Contains(b.min)||!Region.Contains(b.max)){reason="fuera del volumen";return false;}
   if(Walks.Any(w=>w.Intersects(b))){reason="reserva de circulacion";return false;}
   if(Solids.Any(w=>w.Intersects(b))){reason="collider original";return false;}
   var shape=Geometry.FirstOrDefault(g=>Intersects(g,b));if(shape!=null){reason="malla "+shape.Name;return false;}reason=null;return true;
  }
 }
 static Inspection Inspect(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())throw new InvalidOperationException("Original abierto fuera de Play; cerrar otras previsualizaciones desde sus menus.");
  var source=SceneManager.GetSceneByPath(Target);if(!source.IsValid()||!source.isLoaded)throw new InvalidOperationException("Abrir la escena original; no se carga ni sustituye automaticamente.");
  Meshes.Clear();var result=new Inspection{Source=source,Roots=source.GetRootGameObjects(),Hash=Hash(Target),Dirty=source.isDirty,Prototypes=new Dictionary<string,Asset>()};result.State=Signature(result.Roots);
  foreach(var pair in Paths){var p=AssetDatabase.LoadAssetAtPath<GameObject>(pair.Value);if(!p)throw new InvalidOperationException("Falta: "+pair.Value);result.Assets[pair.Value]=Hash(pair.Value);
   if(pair.Key=="Motes"||pair.Key=="Mist"){result.Prototypes[pair.Key]=new Asset{Prefab=p};continue;}
   var norm=Matrix4x4.TRS(Vector3.zero,p.transform.localRotation,p.transform.localScale)*p.transform.worldToLocalMatrix;Bounds bounds=default;bool first=true;
   foreach(var f in p.GetComponentsInChildren<MeshFilter>(true)){if(!f.sharedMesh||!f.GetComponent<Renderer>()||!f.GetComponent<Renderer>().enabled)continue;var b=WorldBounds(f.sharedMesh.bounds,norm*f.transform.localToWorldMatrix);if(first){bounds=b;first=false;}else bounds.Encapsulate(b);}
   if(first)throw new InvalidOperationException("Sin malla: "+pair.Key);result.Prototypes[pair.Key]=new Asset{Prefab=p,Bounds=bounds};
  }
  var relevant=Region;relevant.Expand(10);var seen=new HashSet<string>();
  foreach(var root in result.Roots){foreach(var f in root.GetComponentsInChildren<MeshFilter>()){
   if(!f.sharedMesh||!f.GetComponent<Renderer>()||!f.GetComponent<Renderer>().enabled)continue;var b=WorldBounds(f.sharedMesh.bounds,f.transform.localToWorldMatrix);if(!b.Intersects(relevant))continue;
   string id=f.sharedMesh.GetInstanceID()+"|"+f.transform.localToWorldMatrix.ToString("F5");if(seen.Add(id))result.Geometry.Add(ShapeOf(f.sharedMesh,f.transform.localToWorldMatrix,root.name+"/"+f.name));
   if(f.name.StartsWith("Platform_")||f.name.StartsWith("Plaza_")||f.name.IndexOf("Staircase",StringComparison.OrdinalIgnoreCase)>=0||f.name.StartsWith("Road_")||(b.max.y>=0&&b.size.x>=3&&b.size.z>=3&&b.size.y<1.5f)){
    var min=b.min;var max=b.max;min.y=f.name.IndexOf("Staircase",StringComparison.OrdinalIgnoreCase)>=0?b.min.y-.35f:b.max.y-.35f;max.y+=4.5f;var walk=new Bounds();walk.SetMinMax(min-new Vector3(.6f,0,.6f),max+new Vector3(.6f,0,.6f));result.Walks.Add(walk);
   }
  }
  foreach(var c in root.GetComponentsInChildren<Collider>())if(c.enabled&&!c.isTrigger&&c.bounds.Intersects(relevant)){
   if(c is MeshCollider mc&&mc.sharedMesh){string id=mc.sharedMesh.GetInstanceID()+"|"+mc.transform.localToWorldMatrix.ToString("F5");if(seen.Add(id))result.Geometry.Add(ShapeOf(mc.sharedMesh,mc.transform.localToWorldMatrix,root.name+"/"+mc.name+" collider"));}
   else result.Solids.Add(c.bounds);
  }}
  for(float x=Region.min.x+3;x<=Region.max.x-3;x+=3)for(float z=Region.min.z+3;z<=Region.max.z-3;z+=3)for(float y=0;y<=8;y+=2)if(result.Free(new Bounds(new Vector3(x,y,z),new Vector3(2,1,2)),.25f,out _))result.FreeCells.Add(new Vector3(x,y,z));
  if(result.FreeCells.Count<20)throw new InvalidOperationException("Volumen libre insuficiente: "+result.FreeCells.Count+" celdas. No generar sin revisar apoyos.");
  result.Verify();Debug.Log("GRIETA_OCCIDENTAL_INSPECCION_OK: caja X-80..-10 Z-30..54 Y-2..10; "+result.FreeCells.Count+" celdas candidatas 2x1x2m (no sumar como volumen continuo); "+result.Geometry.Count+" mallas originales contrastadas por triangulos, "+result.Solids.Count+" colliders adicionales, "+result.Walks.Count+" reservas. Atrio, Umbral y conexiones profundas fuera de huella.\n"+string.Join("; ",result.Prototypes.Where(p=>p.Value.Prefab&&p.Key!="Mist"&&p.Key!="Motes").Select(p=>p.Key+"="+p.Value.Bounds.size.ToString("F2"))));return result;
 }
 [MenuItem(Menu+"1. Inspeccionar volumen sin generar")]
 public static void Audit(){try{Inspect();}catch(Exception e){Debug.LogException(e);}}
 [MenuItem(Menu+"2. Crear previsualizacion independiente")]
 public static void Preview(){try{if(StageUtility.GetCurrentStage() is RiftStage old&&old.Complete){old.View(1);return;}var stage=ScriptableObject.CreateInstance<RiftStage>();stage.Input=Inspect();StageUtility.GoToStage(stage,true);}catch(Exception e){Debug.LogException(e);}}
 [MenuItem(Menu+"3. Vista desde arriba")]
 public static void Above(){if(StageUtility.GetCurrentStage() is RiftStage stage&&stage.Complete)stage.View(0);}
 [MenuItem(Menu+"4. Vista lateral")]
 public static void Side(){if(StageUtility.GetCurrentStage() is RiftStage stage&&stage.Complete)stage.View(1);}
 [MenuItem(Menu+"5. Vista baja desde enlace de Jardines")]
 public static void Lower(){if(StageUtility.GetCurrentStage() is RiftStage stage&&stage.Complete)stage.View(2);}
 [MenuItem(Menu+"6. Validar composicion temporal")]
 public static void Validate(){try{if(StageUtility.GetCurrentStage() is RiftStage stage&&stage.Complete)stage.Validate();}catch(Exception e){Debug.LogException(e);}}
 [MenuItem(Menu+"9. Cerrar sin guardar")]
 public static void Close(){if(StageUtility.GetCurrentStage() is RiftStage)StageUtility.GoToMainStage();}
 sealed class Piece {public string Key;public GameObject Go;public Bounds Bounds;public Piece Support;}
 sealed class RiftStage:PreviewSceneStage {
  internal Inspection Input;internal bool Complete;
  Transform context,generated;string copied;Texture2D softMask;readonly List<Piece> pieces=new List<Piece>();readonly List<Material> owned=new List<Material>();readonly Dictionary<Material,Material> crystalMaterials=new Dictionary<Material,Material>();
  readonly List<ParticleSystem> systems=new List<ParticleSystem>();readonly List<Bounds> effectEnvelopes=new List<Bounds>();readonly List<Light> lights=new List<Light>();
  readonly Dictionary<string,int> rejects=new Dictionary<string,int>();readonly System.Random random=new System.Random(149227);double lastTick;int contactTests,placementTests;
  protected override GUIContent CreateHeaderContent()=>new GUIContent("Grieta occidental · SOLO PREVIEW · NO INTEGRAR");
  static bool ContextRelevant(GameObject go){var b=Visible(go);return b.size.sqrMagnitude>.001f&&b.Intersects(ContextRegion);}
  protected override bool OnOpenStage(){if(Input==null||!base.OnOpenStage())return false;try{
   var temp=scene;temp.name="TEMP_GrietaOccidental_Dimensional";var holder=new GameObject("Contexto original copiado - sin cambios");SceneManager.MoveGameObjectToScene(holder,scene);context=holder.transform;holder.SetActive(false);
   foreach(var root in Input.Roots){if(root.name=="Dungeon_Ciudad_del_Sol_Generated"){
    var parent=new GameObject(root.name);parent.transform.SetParent(context,false);foreach(Transform child in root.transform)if(ContextRelevant(child.gameObject))Copy(child.gameObject,parent.transform);
   }else if(ContextRelevant(root))Copy(root,context);}
   foreach(var b in context.GetComponentsInChildren<MonoBehaviour>(true))if(b)b.enabled=false;foreach(var l in context.GetComponentsInChildren<Light>(true))l.enabled=false;foreach(var c in context.GetComponentsInChildren<Camera>(true))c.enabled=false;foreach(var a in context.GetComponentsInChildren<AudioSource>(true)){a.playOnAwake=false;a.enabled=false;}foreach(var ps in context.GetComponentsInChildren<ParticleSystem>(true))ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   holder.SetActive(true);copied=Signature(new[]{holder});var stage=new GameObject("GrietaOccidental_Dimensional_Temporal");SceneManager.MoveGameObjectToScene(stage,scene);generated=stage.transform;
   Compose();Effects();Sun();Physics.SyncTransforms();Validate();Complete=true;lastTick=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;
   EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this){Selection.activeGameObject=null;View(2);}};
   Debug.Log("GRIETA_OCCIDENTAL_LISTA: opciones3 arriba,4 lateral,5 enlace inferior real Y4,6 validar,9 cerrar. Original intacto; no guardar ni integrar.");return true;
  }catch(Exception e){Debug.LogException(e);Debug.LogError("GRIETA_OCCIDENTAL_NO_VALIDADA: no integrar. Original intacto; copia descartada.");base.OnCloseStage();return false;}}
  void Copy(GameObject original,Transform parent){var copy=Object.Instantiate(original,parent);copy.name=original.name;copy.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);copy.transform.localScale=original.transform.lossyScale;}
  bool Reject(string reason){rejects[reason]=rejects.TryGetValue(reason,out int n)?n+1:1;return false;}
  bool Available(Bounds b,Piece support=null){placementTests++;if(!Input.Free(b,.22f,out var reason))return Reject(reason);
   foreach(var piece in pieces){if(piece==support)continue;var other=piece.Bounds;other.Expand(.13f);if(other.Intersects(b))return Reject("otra pieza nueva");}return true;
  }
  Material CrystalMaterial(Material source){if(!source||!source.shader||!source.shader.isSupported)throw new InvalidOperationException("Material de cristal incompatible.");if(crystalMaterials.TryGetValue(source,out var value))return value;
   var copy=new Material(source){name=source.name+" - GRIETA TEMPORAL",hideFlags=HideFlags.HideAndDontSave};owned.Add(copy);crystalMaterials[source]=copy;
   if(copy.HasProperty("_EmissionColor")){bool heart=source.name.IndexOf("Heart",StringComparison.OrdinalIgnoreCase)>=0;copy.SetColor("_EmissionColor",heart?new Color(.8f,.15f,3.2f,1):new Color(.25f,.04f,1.15f,1));}
   if(copy.HasProperty("_RimColor"))copy.SetColor("_RimColor",new Color(.45f,.12f,1.7f,1));if(copy.HasProperty("_PulseDepth"))copy.SetFloat("_PulseDepth",.025f);return copy;
  }
  Piece Place(string key,Vector3 center,Quaternion rotation,float scale,Piece support=null,bool alignBottom=false){var asset=Input.Prototypes[key];var q=rotation*asset.Prefab.transform.localRotation;var b=WorldBounds(asset.Bounds,Matrix4x4.TRS(Vector3.zero,rotation,Vector3.one*scale));
   var pivot=center-b.center;if(alignBottom)pivot.y=center.y-b.min.y;b.center+=pivot;if(!Available(b,support))return null;
   var go=(GameObject)PrefabUtility.InstantiatePrefab(asset.Prefab,scene);go.transform.SetParent(generated,true);go.transform.SetPositionAndRotation(pivot,q);go.transform.localScale=asset.Prefab.transform.localScale*scale;go.name=pieces.Count.ToString("D2")+" - "+key;
   foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;foreach(var m in go.GetComponentsInChildren<MonoBehaviour>(true))if(m)m.enabled=false;
   foreach(var a in go.GetComponentsInChildren<Animator>(true))a.enabled=false;foreach(var a in go.GetComponentsInChildren<Animation>(true))a.enabled=false;foreach(var l in go.GetComponentsInChildren<Light>(true))l.enabled=false;
   foreach(var r in go.GetComponentsInChildren<MeshRenderer>(true)){if(key=="Ground"||key=="Aerial"){r.sharedMaterials=r.sharedMaterials.Select(CrystalMaterial).ToArray();r.shadowCastingMode=ShadowCastingMode.Off;}else r.shadowCastingMode=ShadowCastingMode.On;}
   var piece=new Piece{Key=key,Go=go,Bounds=Visible(go),Support=support};pieces.Add(piece);return piece;
  }
  static readonly Vector3[] Foci={new Vector3(-59,2,39),new Vector3(-39,4,38),new Vector3(-61,2,15),new Vector3(-39,4,14),new Vector3(-55,2,-12),new Vector3(-28,3,-17)};
  Vector3 Candidate(){var anchor=Foci[random.Next(Foci.Length)];return Candidate(anchor,100);}
  Vector3 Candidate(Vector3 anchor,int near){var ordered=Input.FreeCells.OrderBy(v=>(v-anchor).sqrMagnitude).Take(near).ToArray();var p=ordered[random.Next(ordered.Length)];return p+new Vector3((float)random.NextDouble()-.5f,((float)random.NextDouble()-.5f)*.6f,(float)random.NextDouble()-.5f);}
  long Triangles(GameObject go)=>go.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled).Sum(f=>Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(s=>(long)f.sharedMesh.GetIndexCount(s)/3));
  bool Budget(string key){var p=Input.Prototypes[key].Prefab;return pieces.Sum(v=>v.Go.GetComponentsInChildren<Renderer>().Length)+p.GetComponentsInChildren<Renderer>(true).Length<=1150&&pieces.Sum(v=>Triangles(v.Go))+Triangles(p)<=450000;}
  Piece Fit(string key,Vector3 near,float size,bool tilted=true,int attempts=60){if(!Budget(key))return null;var a=Input.Prototypes[key];float scale=size/Mathf.Max(a.Bounds.size.x,Mathf.Max(a.Bounds.size.y,a.Bounds.size.z));if(scale>2.5f)return null;
   for(int n=0;n<attempts;n++){var q=Quaternion.Euler(tilted?random.Next(-30,31):0,random.Next(360),tilted?random.Next(-25,26):0);var p=Place(key,Candidate(near,160),q,scale);if(p!=null)return p;}return null;
  }
  bool SupportHit(Piece support,float x,float z,out Vector3 point){point=default;float best=float.PositiveInfinity;var ray=new Ray(new Vector3(x,Region.max.y+5,z),Vector3.down);
   foreach(var f in support.Go.GetComponentsInChildren<MeshFilter>()){if(!f.sharedMesh||!f.GetComponent<Renderer>()||!f.GetComponent<Renderer>().enabled)continue;var shape=ShapeOf(f.sharedMesh,f.transform.localToWorldMatrix,f.name);for(int i=0;i<shape.Indices.Length;i+=3){var ids=shape.Indices;var vs=shape.Vertices;var a=vs[ids[i]];var b=vs[ids[i+1]];var c=vs[ids[i+2]];if(Vector3.Cross(b-a,c-a).normalized.y<.45f)continue;if(RayTriangle(ray,a,b,c,out float d)&&d<best){best=d;point=ray.GetPoint(d);}}}contactTests++;return best<float.PositiveInfinity;
  }
  void Compose(){
   // Compose by hierarchy of scale and local coverage, not an arbitrary target count.
   foreach(var focus in Foci){foreach(var key in new[]{"Rock","Arch","Wall2","Rock2","Arch2","Ruin2"})Fit(key,focus,5.5f+(float)random.NextDouble()*2.8f,key.StartsWith("Arch")?false:true);
    Fit("Aerial",focus,5.5f,false);Fit("Ground",focus,4.5f,false);
   }
   var supports=pieces.Where(p=>p.Key.StartsWith("Rock")||p.Key.StartsWith("Ruin")).OrderByDescending(p=>p.Bounds.size.x*p.Bounds.size.z).ToArray();
   foreach(var support in supports)for(int n=0;n<12;n++){
    if(!Budget("Ground"))break;float x=support.Bounds.center.x+(float)(random.NextDouble()-.5)*support.Bounds.size.x*.7f,z=support.Bounds.center.z+(float)(random.NextDouble()-.5)*support.Bounds.size.z*.7f;
    if(!SupportHit(support,x,z,out var hit))continue;var a=Input.Prototypes["Ground"];float scale=(1.6f+(float)random.NextDouble()*2.6f)/Mathf.Max(.001f,a.Bounds.size.y);if(scale>2.5f)continue;
    if(Place("Ground",hit+Vector3.up*.025f,Quaternion.Euler(0,random.Next(360),0),scale,support,true)!=null)break;
   }
   // Secondary rubble accumulates around the six unequal concentrations, leaving negative space between them.
   foreach(var focus in Foci)for(int n=0;n<18;n++){var keys=new[]{"Column1","Column2","Wall","Ruin","Bricks1","Bricks3","Rock"};Fit(keys[random.Next(keys.Length)],focus,1.8f+(float)random.NextDouble()*2.8f,true,12);}
   // Add secondary crystal groups only while the shared renderer/triangle budget permits it.
   foreach(var focus in Foci)for(int n=0;n<4;n++)Fit(n%2==0?"Ground":"Aerial",focus,2.2f+(float)random.NextDouble()*1.6f,n%2!=0,24);
   if(pieces.Count(p=>p.Bounds.size.magnitude>5)<8||pieces.Count(p=>p.Key=="Ground"||p.Key=="Aerial")<8)throw new InvalidOperationException("No cabe una composicion monumental segura. Revisar volumen, no reducir silenciosamente a fragmentos dispersos.");
  }
  Vector3 EffectAnchor(Vector3 near,Vector3 size){foreach(var cell in Input.FreeCells.OrderBy(c=>(c-near).sqrMagnitude))if(Input.Free(new Bounds(cell,size),.1f,out _)&&!effectEnvelopes.Any(b=>Vector3.Distance(b.center,cell)<3))return cell;throw new InvalidOperationException("No hay espacio libre para emisor "+size);}
  Material Unlit(Material original,Color tint){var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");if(!shader||!shader.isSupported)throw new InvalidOperationException("Falta shader URP de particulas.");
   if(!softMask){softMask=new Texture2D(64,64,TextureFormat.RGBA32,false,true){name="Mascara suave - SOLO PREVIEW",hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};var pixels=new Color[64*64];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=new Vector2((x-31.5f)/30,(y-31.5f)/30).magnitude;pixels[y*64+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),2));}softMask.SetPixels(pixels);softMask.Apply(false,true);}
   var m=new Material(shader){name="Energia violeta suave - SOLO PREVIEW",hideFlags=HideFlags.HideAndDontSave};owned.Add(m);m.SetColor("_BaseColor",tint);m.SetTexture("_BaseMap",softMask);m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;m.SetOverrideTag("RenderType","Transparent");return m;}
  ParticleSystem Ambient(string key,Vector3 center,bool mist){var go=(GameObject)PrefabUtility.InstantiatePrefab(Input.Prototypes[key].Prefab,scene);go.transform.SetParent(generated,true);go.transform.position=center;go.transform.rotation=Quaternion.identity;go.transform.localScale=Vector3.one;go.name=mist?"Niebla tenue URP":"Motas dimensionales URP";
   foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))if(b)b.enabled=false;foreach(var l in go.GetComponentsInChildren<Light>(true))l.enabled=false;
   var all=go.GetComponentsInChildren<ParticleSystem>(true);if(all.Length!=1)throw new InvalidOperationException("El emisor ya no es sencillo: "+key);
   var ps=all[0];ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(mist?31007:31009);
   ps.transform.position=center;ps.transform.rotation=Quaternion.identity;ps.transform.localScale=Vector3.one;
   var main=ps.main;main.playOnAwake=false;main.loop=true;main.startDelay=0;main.startSize3D=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=mist?10:40;main.startLifetime=mist?6:3.5f;main.startSpeed=0;main.gravityModifier=0;main.startSize=mist?new ParticleSystem.MinMaxCurve(1.6f,2.6f):new ParticleSystem.MinMaxCurve(.16f,.28f);main.startColor=Color.white;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
   var emission=ps.emission;emission.enabled=true;emission.SetBursts(Array.Empty<ParticleSystem.Burst>());emission.rateOverTime=mist?1.4f:10;
   var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.position=mist?Vector3.zero:new Vector3(0,-.7f,0);shape.rotation=Vector3.zero;shape.scale=mist?new Vector3(2,.15f,2):new Vector3(1.3f,.6f,1.3f);
   var velocity=ps.velocityOverLifetime;velocity.enabled=false;var force=ps.forceOverLifetime;force.enabled=false;var noise=ps.noise;noise.enabled=false;var trails=ps.trails;trails.enabled=false;var collision=ps.collision;collision.enabled=false;var trigger=ps.trigger;trigger.enabled=false;var light=ps.lights;light.enabled=false;var sub=ps.subEmitters;sub.enabled=false;
   var sizeLife=ps.sizeOverLifetime;sizeLife.enabled=false;var sizeSpeed=ps.sizeBySpeed;sizeSpeed.enabled=false;var colorSpeed=ps.colorBySpeed;colorSpeed.enabled=false;var rotation=ps.rotationOverLifetime;rotation.enabled=false;var rotationSpeed=ps.rotationBySpeed;rotationSpeed.enabled=false;var forces=ps.externalForces;forces.enabled=false;var inherit=ps.inheritVelocity;inherit.enabled=false;var sheet=ps.textureSheetAnimation;sheet.enabled=false;
   // Explicit world-space ascent; speed and inherited prefab modules cannot spray particles into routes.
   velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=0;velocity.y=mist?.025f:.38f;velocity.z=0;
   var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.8f,.42f,1),0),new GradientColorKey(new Color(.52f,.22f,1),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(mist?.32f:.85f,.2f),new GradientAlphaKey(mist?.22f:.6f,.65f),new GradientAlphaKey(0,1)});color.color=gradient;
   var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=Unlit(r.sharedMaterial,mist?new Color(.65f,.32f,1,.35f):new Color(1.8f,.8f,3,1));r.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Normal,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.renderMode=ParticleSystemRenderMode.Billboard;r.minParticleSize=0;r.maxParticleSize=mist?.12f:.035f;
   systems.Add(ps);ps.Simulate(5,true,true);return ps;
  }
  void Halo(Vector3 center,float size){var go=new GameObject("Halo discreto de cristal");go.transform.SetParent(generated,false);go.transform.position=center;var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.playOnAwake=false;main.loop=true;main.startLifetime=1000;main.maxParticles=1;main.startSpeed=0;main.startSize=size;var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
   var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/ROM/Art/RiftCrystals/Materials/SkyRadianceHalo.mat");if(!original||!original.shader.isSupported)throw new InvalidOperationException("Halo URP no disponible.");var material=new Material(original){name="Halo - SOLO PREVIEW",hideFlags=HideFlags.HideAndDontSave};material.SetColor("_Tint",new Color(1.2f,.32f,3,.85f));material.SetFloat("_Gain",1.1f);owned.Add(material);var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=material;r.renderMode=ParticleSystemRenderMode.Billboard;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.maxParticleSize=.16f;ps.Emit(1);systems.Add(ps);
  }
  void Effects(){
   foreach(var focus in Foci){var envelope=Vector3.one*3.5f;var anchor=EffectAnchor(focus,envelope);effectEnvelopes.Add(new Bounds(anchor,envelope));Halo(anchor,3.2f);}
   for(int i=0;i<4;i++){var size=new Vector3(6,4,6);var anchor=EffectAnchor(Foci[i]+new Vector3(2,-1,2),size);effectEnvelopes.Add(new Bounds(anchor,size));Ambient("Mist",anchor,true);}
   for(int i=0;i<4;i++){var size=Vector3.one*4.5f;var anchor=EffectAnchor(Foci[i+1]+new Vector3(-3,1,-2),size);effectEnvelopes.Add(new Bounds(anchor,size));Ambient("Motes",anchor,false);}
   // Broad fill plus local accents, bounded to eight shadowless lights (never one light per crystal).
   for(int i=0;i<8;i++){var near=i<6?Foci[i]:new Vector3(-47,3,i==6?34:-9);var anchor=EffectAnchor(near,Vector3.one*.4f);var go=new GameObject("Luz violeta sin sombras "+(i+1));go.transform.SetParent(generated,false);go.transform.position=anchor;var l=go.AddComponent<Light>();l.type=LightType.Point;l.color=i%3==1?new Color(.55f,.22f,1):new Color(.4f,.14f,1);l.intensity=i<6?38:24;l.range=i<6?16:23;l.shadows=LightShadows.None;l.renderMode=LightRenderMode.Auto;lights.Add(l);}
  }
  void Sun(){var original=Input.Roots.SelectMany(r=>r.GetComponents<Light>()).FirstOrDefault(l=>l.type==LightType.Directional);if(original){var go=Object.Instantiate(original.gameObject);SceneManager.MoveGameObjectToScene(go,scene);go.name="Luz de inspeccion atenuada - SOLO PREVIEW";var l=go.GetComponent<Light>();l.shadows=LightShadows.None;l.intensity=Mathf.Clamp(original.intensity*.32f,.15f,.6f);l.color=Color.Lerp(original.color,new Color(.7f,.6f,1),.35f);}}
  internal void Validate(){Input.Verify();if(copied!=Signature(new[]{context.gameObject}))throw new InvalidOperationException("Contexto original copiado alterado.");
   foreach(var p in pieces){if(!Input.Free(p.Bounds,.2f,out var reason))throw new InvalidOperationException("Interferencia: "+p.Go.name+" "+reason);if(p.Go.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))throw new InvalidOperationException("Collider nuevo innecesario.");if(p.Go.GetComponentsInChildren<Component>(true).Any(c=>c&&c.GetType().FullName=="Mirror.NetworkIdentity"))throw new InvalidOperationException("NetworkIdentity no permitido.");
    if(p.Support!=null&&!SupportHit(p.Support,p.Bounds.center.x,p.Bounds.center.z,out _))throw new InvalidOperationException("Cristal sin contacto real con soporte: "+p.Go.name);
   }
   for(int i=0;i<pieces.Count;i++)for(int j=0;j<i;j++)if(pieces[i].Key==pieces[j].Key&&Vector3.Distance(pieces[i].Go.transform.position,pieces[j].Go.transform.position)<.01f)throw new InvalidOperationException("Duplicado exacto.");
   foreach(var box in effectEnvelopes)if(!Input.Free(box,.08f,out var reason))throw new InvalidOperationException("Envolvente VFX obstruida: "+reason);
   for(int i=0;i<systems.Count;i++){
    var ps=systems[i];var renderer=ps.GetComponent<ParticleSystemRenderer>();if(!renderer.sharedMaterial||!renderer.sharedMaterial.shader.isSupported||ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader))throw new InvalidOperationException("Shader VFX incompatible: "+ps.name);
    var particles=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(particles);if(count==0)throw new InvalidOperationException("Emisor sin particulas visibles: "+ps.name);
    for(int n=0;n<count;n++){var position=ps.main.simulationSpace==ParticleSystemSimulationSpace.World?particles[n].position:ps.transform.TransformPoint(particles[n].position);var bounds=new Bounds(position,Vector3.one*particles[n].GetCurrentSize(ps));if(!effectEnvelopes[i].Contains(bounds.min)||!effectEnvelopes[i].Contains(bounds.max))throw new InvalidOperationException("Particula fuera del volumen reservado: "+ps.name);}
   }
   if(systems.Count>18||systems.Sum(p=>p.main.maxParticles)>240||lights.Count>8||lights.Any(l=>l.shadows!=LightShadows.None)||generated.GetComponentsInChildren<Renderer>().Length>1200||pieces.Sum(p=>Triangles(p.Go))>450000)throw new InvalidOperationException("Presupuesto de composicion incorrecto.");
   long triangles=pieces.Sum(p=>p.Go.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled).Sum(f=>Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(s=>(long)f.sharedMesh.GetIndexCount(s)/3)));
   var actual=new Bounds(pieces[0].Bounds.center,Vector3.zero);foreach(var p in pieces)actual.Encapsulate(p.Bounds);
   int renderers=generated.GetComponentsInChildren<Renderer>().Length,maxParticles=systems.Sum(p=>p.main.maxParticles);string report="GRIETA_OCCIDENTAL_VALIDADA: "+pieces.Count+" piezas estaticas; "+renderers+" renderizadores incluidos VFX; "+triangles+" triangulos estaticos estimados; "+systems.Count+" emisores (6 halos,4 nieblas,4 motas), maximo "+maxParticles+" particulas; "+lights.Count+" luces violetas sin sombras; 0 colliders activos y 0 NetworkIdentity nuevos.\n"+string.Join("; ",pieces.GroupBy(p=>p.Key).Select(g=>g.Key+"="+g.Count()))+"\nHuella real min="+actual.min.ToString("F2")+" max="+actual.max.ToString("F2")+"; formaciones >5m="+pieces.Count(p=>p.Bounds.size.magnitude>5)+"\n"+placementTests+" intentos contrastados con triangulos originales, "+contactTests+" consultas de contacto. Celdas libres="+Input.FreeCells.Count+"; reservas="+Input.Walks.Count+". Escena, componentes originales y prefabs intactos. Sin Bloom global ni efectos antiguos.\nBajo cubierta Y-4 el interior cerrado tiene oclusion real: vista baja desde enlace exterior Jardines; no se oculta cubierta. Coste CPU/GPU/FPS pendiente de medir en juego. Riesgos: solapamiento de transparencias y limite URP de luces adicionales por objeto.\nRechazos: "+string.Join(";",rejects.Select(p=>p.Key+"="+p.Value));
   report+="\nGameObjects de composicion (incluye hijos de prefabs)="+generated.GetComponentsInChildren<Transform>(true).Length+"; materiales independientes="+owned.Count+"; particulas vivas al validar="+systems.Sum(p=>p.particleCount)+". Una luz direccional adicional de inspeccion atenuada, exclusiva de la copia; luz solar original intacta.";
   Directory.CreateDirectory("Temp/ROM_Reviews/GrietaOccidental");File.WriteAllText("Temp/ROM_Reviews/GrietaOccidental/Informe.txt",report);Debug.Log(report);
  }
  void Tick(){if(!this||!Complete||StageUtility.GetCurrentStage()!=this)return;double now=EditorApplication.timeSinceStartup;float dt=Mathf.Min(.05f,(float)(now-lastTick));lastTick=now;foreach(var ps in systems)if(ps){if(ps.main.maxParticles==1&&ps.particleCount==0)ps.Emit(1);ps.Simulate(dt,true,false);}SceneView.RepaintAll();}
  bool Sight(Vector3 eye,Vector3 target){var delta=target-eye;float length=delta.magnitude;var ray=new Ray(eye,delta.normalized);
   foreach(var box in Input.Solids)if(box.IntersectRay(ray,out float hit)&&hit<length)return false;
   foreach(var shape in Input.Geometry){if(!shape.Bounds.IntersectRay(ray,out float hit)||hit>length)continue;for(int i=0;i<shape.Indices.Length;i+=3)if(RayTriangle(ray,shape.Vertices[shape.Indices[i]],shape.Vertices[shape.Indices[i+1]],shape.Vertices[shape.Indices[i+2]],out float d)&&d<length-.15f)return false;}return true;
  }
  void LowerCamera(Vector3 bias,out Vector3 eye,out Vector3 target){eye=Vector3.zero;target=Vector3.zero;float best=-1;
   var targets=pieces.Where(p=>p.Key=="Ground"||p.Key=="Aerial"||p.Key.StartsWith("Arch")).OrderByDescending(p=>p.Bounds.size.magnitude).Take(32).Select(p=>p.Bounds.center).ToArray();
   foreach(var cell in Input.FreeCells.Where(v=>v.y<=4&&v.y>=0).OrderBy(v=>(v-bias).sqrMagnitude).Take(180)){
    if(!Input.Free(new Bounds(cell,Vector3.one*.6f),.1f,out _)||pieces.Any(p=>Vector3.Distance(p.Bounds.ClosestPoint(cell),cell)<2)||effectEnvelopes.Any(b=>b.Contains(cell)))continue;
    var visible=targets.Where(p=>Vector3.Distance(p,cell)>5&&Vector3.Distance(p,cell)<40&&Sight(cell,p)).ToArray();
    for(int a=0;a<8;a++){var direction=Quaternion.Euler(0,a*45,0)*Vector3.forward;var group=visible.Where(p=>Vector3.Dot((p-cell).normalized,direction)>.72f).ToArray();float score=group.Length-(cell-bias).magnitude*.008f;if(group.Length<3||score<=best)continue;best=score;eye=cell;target=group.Aggregate(Vector3.zero,(sum,p)=>sum+p)/group.Length;}
   }
   if(best<0)throw new InvalidOperationException("Sin encuadre inferior libre hacia las formaciones; no ocultar arquitectura.");Debug.Log("GRIETA_VISTA_BAJA: ojo="+eye.ToString("F2")+" objetivo="+target.ToString("F2")+"; lineas de vista contrastadas con geometria original.");
  }
  internal void View(int index){var view=SceneView.lastActiveSceneView;if(!view)view=EditorWindow.GetWindow<SceneView>();var pivot=new Vector3(-45,3,12);Quaternion rotation;float size;
   if(index==0){rotation=Quaternion.Euler(47,65,0);size=49;}
   else {LowerCamera(index==1?new Vector3(-28,2,-18):new Vector3(-47,2,24),out var eye,out pivot);rotation=Quaternion.LookRotation(pivot-eye);size=Vector3.Distance(eye,pivot)*Mathf.Sin(30*Mathf.Deg2Rad);}
   Selection.activeGameObject=null;view.sceneLighting=true;view.in2DMode=false;view.isRotationLocked=false;view.LookAtDirect(pivot,rotation,size);view.orthographic=false;view.Focus();view.Repaint();
  }
  protected override void OnCloseStage(){EditorApplication.update-=Tick;foreach(var m in owned)if(m)Object.DestroyImmediate(m);owned.Clear();if(softMask)Object.DestroyImmediate(softMask);base.OnCloseStage();}
 }
}
}
