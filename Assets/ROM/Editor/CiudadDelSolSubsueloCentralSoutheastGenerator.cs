using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace ROM.EditorTools
{
 public static class CiudadDelSolSubsueloCentralSoutheastGenerator
 {
  const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity",Dungeon="Dungeon_Ciudad_del_Sol_Generated",Route="06-07 -";
  const string Root="Subsuelo_Central_Sureste_Etapa02_Preview",Menu="ROM/Dungeon Ciudad del Sol/Subsuelo - Etapa 2 Central y sureste/";
  const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/";
  static readonly Dictionary<string,string> Paths=new Dictionary<string,string>{
   {"Base",ET+"Pits and Platforms/Platform_Base_01.prefab"},
   {"Vault",ET+"Bases/Ceiling/Base_Ceiling_3m_01.prefab"},
   {"Beam",ET+"Bases/Ceiling/Ceiling_Beam_01.prefab"},
   {"Capital",ET+"Bases/Ceiling/Column_Ceiling_02.prefab"},
   {"Post",ET+"Columns/Column_Stackable_01.prefab"},
   {"Wall",ET+"Bases/Thin/Base_Wall_02.prefab"},
   {"Arch",ET+"Bases/Arch_Wide_01.prefab"},
   {"DecorArch",ET+"Bases/Thin/Entrance_Arch_01.prefab"},
   {"Trim",ET+"Railings and Decors/Bottom_Decor_01.prefab"}
  };
  sealed class Asset{public GameObject Prefab;public Bounds Bounds;}
  sealed class Source{public Scene Scene;public Transform Root,Route,Umbral,Roof,Access;public string FileHash;public bool Dirty;public GameObject[] OriginalRoots;
   public readonly List<Action> Checks=new List<Action>();
   public void Verify(bool integrating=false){foreach(var c in Checks)c();if((!integrating&&Scene.isDirty!=Dirty)||HashScene()!=FileHash)throw new InvalidOperationException("La escena original fue alterada.");}
  }
  static bool Ready=>!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode;
  static string HashScene(){using(var s=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(s.ComputeHash(System.IO.File.ReadAllBytes(Target)));}
        static Source Inspect()
        {
            var scene = SceneManager.GetActiveScene();
            if(StageUtility.GetCurrentStage() != StageUtility.GetMainStage() || scene.path != Target)
                throw new InvalidOperationException("Activa Dungeon_Isla_del_Sol en el escenario principal.");
            var roots = scene.GetRootGameObjects().Where(g => g.name == Dungeon).ToArray();
            if(roots.Length != 1) throw new InvalidOperationException("Base ausente o duplicada. No se regenerara.");
            var root = roots[0].transform;
            if(root.position.sqrMagnitude > .0001f || Quaternion.Angle(root.rotation,Quaternion.identity) > .01f ||
                (root.lossyScale-Vector3.one).sqrMagnitude > .0001f)
                throw new InvalidOperationException("Transformacion de la base inesperada.");
            string[] routes = {"01-02 -","02-03 -","03-04 -","03-05 -","04-05 -","03-06 -","05-07 -","06-07 -","01-06 -"};
            foreach(var prefix in routes)
                if(root.Cast<Transform>().Count(t => t.name.StartsWith(prefix,StringComparison.Ordinal)) != 1)
                    throw new InvalidOperationException("Recorrido ausente o duplicado: " + prefix);
            string[] approved = {"01 - Entrada Portico Solar/PorticoSolar_Architecture","02 - Jardines Dorados/JardinesDorados_Architecture",
                "03 - Plaza del Eclipse/PlazaDelEclipse_Architecture","04 - Archivos Solares/ArchivosSolares_Architecture",
                "05 - Claustro del Eclipse/ClaustroDelEclipse_Architecture","06 - Santuario Profano/SantuarioProfano_Architecture",
                "07 - El Sol Corrompido/ElSolCorrompido_Architecture"};
            foreach(var path in approved) if(!root.Find(path)) throw new InvalidOperationException("Falta arquitectura aprobada: " + path);
            var source = new Source {Scene=scene, Root=root, Route=root.Cast<Transform>().Single(t=>t.name.StartsWith(Route,StringComparison.Ordinal)), FileHash=HashScene(), Dirty=scene.isDirty, OriginalRoots=scene.GetRootGameObjects()};
            if(!source.Route) throw new InvalidOperationException("No se encontro el puente inferior original.");
            // Compare every existing component, transform and child list after creating the temporary review.
            foreach(var originalRoot in scene.GetRootGameObjects())
                foreach(var t in originalRoot.GetComponentsInChildren<Transform>(true))
                {
                    var original = t; var parent=t.parent; var position=t.localPosition; var rotation=t.localRotation; var scale=t.localScale;
                    var children=t.Cast<Transform>().Select(c=>c.GetInstanceID()).ToArray();
                    var components=t.GetComponents<Component>().Select(c=>c?c.GetInstanceID():0).ToArray();
                    string name=t.name; bool active=t.gameObject.activeSelf; int sibling=t.GetSiblingIndex();
                    source.Checks.Add(() =>
                    {
                        if(!original || original.parent!=parent || original.localPosition!=position || original.localRotation!=rotation ||
                            original.localScale!=scale || original.name!=name || original.gameObject.activeSelf!=active || original.GetSiblingIndex()!=sibling ||
                            !children.SequenceEqual(original.Cast<Transform>().Select(c=>c.GetInstanceID())) ||
                            !components.SequenceEqual(original.GetComponents<Component>().Select(c=>c?c.GetInstanceID():0)))
                            throw new InvalidOperationException("Objeto original alterado: " + name);
                    });
                    foreach(var c in t.GetComponents<Component>())
                    {
                        if(!c || c is Transform) continue;
                        var component=c; string json=EditorJsonUtility.ToJson(c);
                        source.Checks.Add(() => { if(!component || EditorJsonUtility.ToJson(component)!=json) throw new InvalidOperationException("Componente original alterado: " + name); });
                    }
                }
            var u=scene.GetRootGameObjects().Where(g=>g.name=="Umbral_De_Las_Profundidades").ToArray();
            if(u.Length!=1||!u[0].transform.Find("__ROM_Umbral_639_Approved_v1"))throw new InvalidOperationException("Falta el Umbral integrado aprobado.");
            if(u[0].GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=639)throw new InvalidOperationException("El Umbral no conserva 639 prefabs.");
            source.Umbral=u[0].transform;
            var roof=scene.GetRootGameObjects().Where(g=>g.name=="Subsuelo_Techo_Etapa01").ToArray();
            if(roof.Length!=1||!roof[0].transform.Find("__ROM_TechoSubsuelo_574_Huecos8_Approved_v1")||roof[0].GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=574)throw new InvalidOperationException("El techo aprobado no conserva sus 574 prefabs.");
            if(scene.GetRootGameObjects().Count(g=>g.name=="Salida_Meridional_Generated")!=1)throw new InvalidOperationException("Salida Meridional ausente o duplicada.");
            source.Roof=roof[0].transform;
            var access=scene.GetRootGameObjects().Where(g=>g.name=="Subsuelo_Acceso_Etapa01").ToArray();
            if(access.Length!=1||!access[0].transform.Find("__ROM_AccesoSubsuelo_129_Approved_v1")||access[0].GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=129)throw new InvalidOperationException("Acceso aprobado ausente o distinto de 129 prefabs.");
            source.Access=access[0].transform;return source;
        }

        static Dictionary<string,Asset> LoadAssets()
        {
            var result = new Dictionary<string,Asset>();
            foreach(var path in Paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path.Value);
                if(!prefab) throw new InvalidOperationException("Falta prefab: " + path.Value);
                var normalize=Matrix4x4.TRS(Vector3.zero,prefab.transform.localRotation,prefab.transform.localScale)*prefab.transform.worldToLocalMatrix;
                Bounds bounds=default; bool found=false;
                foreach(var f in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r=f.GetComponent<MeshRenderer>();
                    if(!r || !r.enabled || !f.sharedMesh || r.name.EndsWith("_Shadow",StringComparison.OrdinalIgnoreCase)) continue;
                    bool active=true;
                    for(var t=f.transform;t;t=t.parent) { if(!t.gameObject.activeSelf) active=false; if(t==prefab.transform) break; }
                    if(!active) continue;
                    var b=TransformBounds(f.sharedMesh.bounds,normalize*f.transform.localToWorldMatrix);
                    if(found) bounds.Encapsulate(b); else { bounds=b; found=true; }
                }
                if(!found) throw new InvalidOperationException("Prefab sin malla: " + path.Value);
                result[path.Key]=new Asset{Prefab=prefab,Bounds=bounds};
            }
            if((result["Base"].Bounds.size-new Vector3(6,2,6)).sqrMagnitude>.01f)
                throw new InvalidOperationException("El modulo de piedra ya no mide 6 x 2 x 6.");
            return result;
        }
        static Bounds TransformBounds(Bounds b,Matrix4x4 matrix)
        {
            var result=new Bounds(matrix.MultiplyPoint3x4(b.center),Vector3.zero);
            for(int i=0;i<8;i++) result.Encapsulate(matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,
                new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
            return result;
        }
        static void DisableImportedBehaviour(GameObject root)
        {
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true);
            var fallback=renderers.SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m);
            foreach(var r in renderers)
            {
                if(r.name.EndsWith("_Shadow",StringComparison.OrdinalIgnoreCase)) r.enabled=false;
                if(fallback&&r.sharedMaterials.Any(m=>!m)) r.sharedMaterials=r.sharedMaterials.Select(m=>m?m:fallback).ToArray();
                if(fallback&&r.sharedMaterials.Length==0) r.sharedMaterials=new[]{fallback};
            }
            foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true)) if(b) b.enabled=false;
            foreach(var a in root.GetComponentsInChildren<AudioSource>(true)) a.enabled=false;
            foreach(var l in root.GetComponentsInChildren<Light>(true)) l.enabled=false;
            foreach(var c in root.GetComponentsInChildren<Camera>(true)) c.enabled=false;
            foreach(var p in root.GetComponentsInChildren<ParticleSystem>(true)) { var m=p.main; m.playOnAwake=false; p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
            foreach(var b in root.GetComponentsInChildren<Rigidbody>(true)) { b.isKinematic=true; b.useGravity=false; }
        }




  sealed class Piece{public string Key,Group;public GameObject Object;public Bounds Bounds;}
  sealed class Plan
  {
   public Source Source;public Transform Root,Context;public GameObject OmittedRail;
   public readonly List<Piece> Pieces=new List<Piece>();public readonly List<Mesh> Meshes=new List<Mesh>();
   public readonly HashSet<Vector2Int> Cells=new HashSet<Vector2Int>();public readonly List<Vector3[]> Walks=new List<Vector3[]>();
   public Collider[] Original;public Bounds[] OldFloor;public int Samples,Fitted,ReusedFloor;public float MinHeadroom=float.PositiveInfinity;
  }
  static bool InSector(float x,float z)=>(x>=-42&&x<102&&z>=-48&&z<72)||(x>=102&&x<198&&z>=-132&&z<30)||(x>=138&&x<204&&z>=30&&z<114);
  static bool Atrium(float x,float z)=>(x>=144&&x<204&&z>=30&&z<96)||(x>=150&&x<174&&z>=24&&z<30);
  static bool InLowRoof(float x,float z)=>InSector(x,z)&&!Atrium(x,z);
  static bool Reserved(float x,float z,float radius=0)
  {
   return (Mathf.Abs(z-99)<2.5f+radius&&x>=156-radius&&x<=183+radius)||
    (Mathf.Abs(x-180)<2.5f+radius&&z>=-93-radius&&z<=102+radius)||
    (Mathf.Abs(z+90)<2.5f+radius&&x>=117-radius&&x<=183+radius)||
    (Mathf.Abs(z-12)<2.5f+radius&&x>=15-radius&&x<=183+radius);
  }
  [MenuItem(Menu+"1. Previsualizar sector (copia temporal) %#&c")]
  public static void Preview()
  {
   if(!Ready)return;try{Close();if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())throw new InvalidOperationException("Cerra la otra copia temporal desde Scenes.");var stage=ScriptableObject.CreateInstance<CentralPreview>();stage.Source=Inspect();stage.Assets=LoadAssets();StageUtility.GoToStage(stage,true);}catch(Exception e){Debug.LogException(e);}
  }
  [MenuItem(Menu+"2. Vista cenital y enlace con Etapa 1 %#&s")]
  public static void Top()=>View(new Vector3(84,-8,-9),Quaternion.Euler(82,0,0),151,0);
  [MenuItem(Menu+"3. Vista interior desde Y-16 %#&w")]
  public static void Interior()=>View(new Vector3(180,-14,-60),Quaternion.Euler(0,0,0),18,1);
  [MenuItem(Menu+"4. Vista exterior de muros y cubierta %#&z")]
  public static void Exterior()=>View(new Vector3(145,-10,-91),Quaternion.Euler(15,-35,0),82,2);
  [MenuItem(Menu+"5. Vista general con Ciudad del Sol %#&a")]
  public static void General()=>View(new Vector3(48,4,12),Quaternion.Euler(24,-35,0),232,3);
  static void View(Vector3 pivot,Quaternion rotation,float size,int mode)
  {
   var s=StageUtility.GetCurrentStage() as CentralPreview;if(!s||s.Plan==null)return;
   foreach(Transform root in s.Plan.Context)
   {
    bool show=mode==3||root.name=="Subsuelo_Techo_Etapa01"||root.name=="Subsuelo_Acceso_Etapa01"||(mode==1&&root.name==Dungeon);
    if(show)SceneVisibilityManager.instance.Show(root.gameObject,true);else SceneVisibilityManager.instance.Hide(root.gameObject,true);
   }
   if(mode==1)foreach(Transform area in s.Plan.Context.Find(Dungeon))foreach(Transform g in area)
    if(!g.name.Contains("Soportes")&&!g.name.StartsWith("Column_01",StringComparison.Ordinal))SceneVisibilityManager.instance.Hide(g.gameObject,true);
   foreach(Transform group in s.Plan.Root)
   {
    bool hide=mode==0&&(group.name.StartsWith("02 -")||group.name.StartsWith("03 -")||group.name.StartsWith("07 -"));
    if(hide)SceneVisibilityManager.instance.Hide(group.gameObject,true);else SceneVisibilityManager.instance.Show(group.gameObject,true);
   }
   if(mode==0)foreach(Transform group in s.Plan.Context.Find("Subsuelo_Techo_Etapa01"))if(!group.name.StartsWith("05 -"))SceneVisibilityManager.instance.Hide(group.gameObject,true);
   var v=SceneView.lastActiveSceneView;if(v){v.sceneLighting=false;v.sceneViewState.showImageEffects=false;v.orthographic=mode==0;v.LookAtDirect(pivot,rotation,size);v.Repaint();}
  }
  [MenuItem(Menu+"6. Validar sector y conexion")]
  public static void Validate(){try{var s=StageUtility.GetCurrentStage() as CentralPreview;if(!s||s.Plan==null)throw new InvalidOperationException("Abri la copia temporal.");ValidatePlan(s.Plan);}catch(Exception e){Debug.LogException(e);}}
  [MenuItem(Menu+"7. Cerrar previsualizacion sin guardar")]
  public static void Close(){if(Ready&&StageUtility.GetCurrentStage() is CentralPreview)StageUtility.GoToMainStage();}
  struct Vertex{public Vector3 Position,Normal;public Vector2 UV;public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{Position=Vector3.Lerp(a.Position,b.Position,t),Normal=Vector3.Lerp(a.Normal,b.Normal,t).normalized,UV=Vector2.Lerp(a.UV,b.UV,t)};}
  static List<Vertex> Clip(List<Vertex> polygon,int axis,float value,bool less)
  {
   var result=new List<Vertex>();if(polygon.Count==0)return result;
   Vertex previous=polygon[polygon.Count-1];float d0=(previous.Position[axis]-value)*(less?1:-1);bool in0=d0<=0;
   foreach(var current in polygon){float d1=(current.Position[axis]-value)*(less?1:-1);bool in1=d1<=0;
    if(in0!=in1)result.Add(Vertex.Lerp(previous,current,d0/(d0-d1)));
    if(in1)result.Add(current);previous=current;d0=d1;in0=in1;}
   return result;
  }
  static IEnumerable<List<Vertex>> Subtract(List<Vertex> polygon,Bounds box)
  {
   var remaining=polygon;
   for(int axis=0;axis<3;axis++)
   {
    var outside=Clip(remaining,axis,box.min[axis],true);if(outside.Count>=3)yield return outside;
    remaining=Clip(remaining,axis,box.min[axis],false);if(remaining.Count<3)yield break;
    outside=Clip(remaining,axis,box.max[axis],false);if(outside.Count>=3)yield return outside;
    remaining=Clip(remaining,axis,box.max[axis],true);if(remaining.Count<3)yield break;
   }
  }
  static void Fit(Plan plan,GameObject piece,Bounds allowed,Bounds[] obstacles)
  {
   foreach(var collider in piece.GetComponentsInChildren<Collider>())collider.enabled=false;
   foreach(var filter in piece.GetComponentsInChildren<MeshFilter>())
   {
    var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled||!filter.sharedMesh)continue;
    var source=filter.sharedMesh;var vertices=source.vertices;var normals=source.normals;var uv=source.uv;
    var output=new List<Vertex>();var indices=new List<int>[source.subMeshCount];
    for(int sub=0;sub<source.subMeshCount;sub++)
    {
     indices[sub]=new List<int>();var tris=source.GetTriangles(sub);
     for(int t=0;t<tris.Length;t+=3)
     {
      var poly=new List<Vertex>();for(int j=0;j<3;j++){int i=tris[t+j];poly.Add(new Vertex{Position=filter.transform.TransformPoint(vertices[i]),Normal=filter.transform.TransformDirection(normals.Length==vertices.Length?normals[i]:Vector3.up),UV=uv.Length==vertices.Length?uv[i]:Vector2.zero});}
      for(int axis=0;axis<3;axis++){poly=Clip(poly,axis,allowed.min[axis],false);poly=Clip(poly,axis,allowed.max[axis],true);}
      var parts=new List<List<Vertex>>();if(poly.Count>=3)parts.Add(poly);
      foreach(var obstacle in obstacles){var next=new List<List<Vertex>>();foreach(var part in parts)next.AddRange(Subtract(part,obstacle));parts=next;if(parts.Count==0)break;}
      foreach(var part in parts){int start=output.Count;output.AddRange(part);for(int j=1;j<part.Count-1;j++)indices[sub].AddRange(new[]{start,start+j,start+j+1});}
     }
    }
    var mesh=new Mesh{name=source.name+"_Preview_Fitted",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,hideFlags=HideFlags.DontSave};
    mesh.SetVertices(output.Select(v=>filter.transform.InverseTransformPoint(v.Position)).ToList());
    mesh.SetNormals(output.Select(v=>filter.transform.InverseTransformDirection(v.Normal).normalized).ToList());mesh.SetUVs(0,output.Select(v=>v.UV).ToList());
    mesh.subMeshCount=indices.Length;for(int sub=0;sub<indices.Length;sub++)mesh.SetTriangles(indices[sub],sub);mesh.RecalculateBounds();
    filter.sharedMesh=mesh;plan.Meshes.Add(mesh);if(output.Count>0)filter.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
   }
  }

  static IEnumerable<Rect> Minus(Rect r,Bounds b)
  {
   float x0=Mathf.Max(r.xMin,b.min.x),x1=Mathf.Min(r.xMax,b.max.x),z0=Mathf.Max(r.yMin,b.min.z),z1=Mathf.Min(r.yMax,b.max.z);
   if(x1-x0<.005f||z1-z0<.005f){yield return r;yield break;}
   if(x0-r.xMin>.005f)yield return Rect.MinMaxRect(r.xMin,r.yMin,x0,r.yMax);
   if(r.xMax-x1>.005f)yield return Rect.MinMaxRect(x1,r.yMin,r.xMax,r.yMax);
   if(z0-r.yMin>.005f)yield return Rect.MinMaxRect(x0,r.yMin,x1,z0);
   if(r.yMax-z1>.005f)yield return Rect.MinMaxRect(x0,z1,x1,r.yMax);
  }
  static Plan Build(Source source,Transform context,Scene scene,Dictionary<string,Asset> assets,GameObject omitted,bool undo=false)
  {
   var root=new GameObject(Root);SceneManager.MoveGameObjectToScene(root,scene);if(undo)Undo.RegisterCreatedObjectUndo(root,"Integrar subsuelo Etapa 2 aprobado");
   var plan=new Plan{Root=root.transform,Context=context,Source=source,OmittedRail=omitted};
   plan.Original=(context?context.GetComponentsInChildren<Collider>():source.OriginalRoots.SelectMany(g=>g.GetComponentsInChildren<Collider>()).ToArray()).Where(c=>c.enabled&&!c.isTrigger).ToArray();
   plan.OldFloor=plan.Original.Where(c=>c is BoxCollider&&c.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)&&Mathf.Abs(c.bounds.max.y+16)<.02f).Select(c=>c.bounds).ToArray();
   if(plan.OldFloor.Length<16)throw new InvalidOperationException("No se encontro el piso inferior aprobado.");
   var groups=new Dictionary<string,Transform>();var unique=new HashSet<string>();
   Transform Group(string name){if(!groups.TryGetValue(name,out var t)){var g=new GameObject(name);g.transform.SetParent(root.transform,false);groups[name]=t=g.transform;}return t;}
   GameObject P(string group,string key,float x,float z,float y,float yaw=0,bool top=false,Vector3? sizeScale=null,Bounds? allowed=null)
   {
    if(plan.Pieces.Count>=3600)throw new InvalidOperationException("Presupuesto de 3600 prefabs superado; no se ampliara automaticamente.");
    var a=assets[key];var q=Quaternion.Euler(0,yaw,0);var scale=sizeScale??Vector3.one;var b=TransformBounds(a.Bounds,Matrix4x4.TRS(Vector3.zero,q,scale));
    var pivot=new Vector3(x-b.center.x,y-(top?b.max.y:b.min.y),z-b.center.z);b.center+=pivot;
    string id=key+"|"+pivot.ToString("F4")+"|"+yaw+"|"+scale.ToString("F4");if(!unique.Add(id))throw new InvalidOperationException("Pieza duplicada: "+id);
    var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,scene);go.transform.SetParent(Group(group),true);go.transform.SetPositionAndRotation(pivot,q*a.Prefab.transform.localRotation);go.transform.localScale=Vector3.Scale(a.Prefab.transform.localScale,scale);DisableImportedBehaviour(go);go.isStatic=true;
    // Only copies of new modules are fitted. Meshes are never combined or written back to prefab/model assets.
    var cuts=plan.Original.Where(c=>c.bounds.Intersects(b)&&Mathf.Min(c.bounds.max.x,b.max.x)-Mathf.Max(c.bounds.min.x,b.min.x)>.015f&&Mathf.Min(c.bounds.max.y,b.max.y)-Mathf.Max(c.bounds.min.y,b.min.y)>.015f&&Mathf.Min(c.bounds.max.z,b.max.z)-Mathf.Max(c.bounds.min.z,b.min.z)>.015f).Select(c=>{var box=c.bounds;box.Expand(.012f);return box;}).ToArray();
    bool fit=allowed.HasValue||cuts.Length>0;
    if(fit){Fit(plan,go,allowed??b,cuts);plan.Fitted++;}
    else if(key=="Vault"||!go.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))
    {
     foreach(var c in go.GetComponentsInChildren<Collider>())c.enabled=false;
     foreach(var f in go.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled)f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
    }
    if(!go.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger&&(!(c is MeshCollider mesh)||mesh.sharedMesh&&mesh.sharedMesh.vertexCount>0)))
    {Object.DestroyImmediate(go);throw new InvalidOperationException("Modulo completamente ocupado: "+key+" "+b);}
    plan.Pieces.Add(new Piece{Key=key,Group=group,Object=go,Bounds=b});return go;
   }
   // Exactly three adjoining work areas, not closed rooms: central 144x120, southeast 96x162, atrium apron 66x84.
   for(int x=-39;x<204;x+=6)for(int z=-129;z<114;z+=6)if(InSector(x,z))plan.Cells.Add(new Vector2Int(x,z));
   foreach(var cell in plan.Cells.OrderBy(c=>c.x).ThenBy(c=>c.y))
   {
    var rect=new Rect(cell.x-3,cell.y-3,6,6);var parts=new List<Rect>{rect};
    foreach(var old in plan.OldFloor.Where(b=>b.min.x<rect.xMax&&b.max.x>rect.xMin&&b.min.z<rect.yMax&&b.max.z>rect.yMin))parts=parts.SelectMany(r=>Minus(r,old)).ToList();
    if(parts.Count==0)plan.ReusedFloor++;
    foreach(var part in parts)P("01 - Piso continuo Y-16","Base",part.center.x,part.center.y,-16,0,true,new Vector3(part.width/6,1,part.height/6));
    if(!Atrium(cell.x,cell.y))P("02 - Bovedas generales Y-4","Vault",cell.x,cell.y,-4,0,true);
    else if(cell.x>=144&&cell.x<204&&cell.y==93)P("02 - Enlace elevado norte del atrio","Vault",cell.x,cell.y,15.38f,0,true);
   }
   float underside=-4-assets["Vault"].Bounds.size.y,beamBottom=underside-assets["Beam"].Bounds.size.y;
   for(int x=147;x<204;x+=6)P("03 - Viga de enlace alto del atrio","Beam",x,96,15.38f-assets["Vault"].Bounds.size.y,90,true);
   foreach(var cell in plan.Cells.Where(c=>InLowRoof(c.x,c.y)&&((c.x+39)/6)%2==0))P("03 - Vigas longitudinales principales","Beam",cell.x-3,cell.y,underside,0,true);
   for(int x=-30;x<204;x+=12)for(int z=-120;z<114;z+=12)
   {
    if(!InLowRoof(x,z)||!InSector(x-1,z-1)||!InSector(x+1,z+1)||Reserved(x,z,1.3f))continue;
    var b=new Bounds(new Vector3(x,beamBottom-assets["Capital"].Bounds.size.y/2,z),assets["Capital"].Bounds.size);
    if(plan.Original.Any(c=>c.bounds.Intersects(b)&&c.bounds.max.y>-15.9f))continue;
    P("04 - Columnas monumentales del piso","Capital",x,z,beamBottom,0,true);
   }
   // Exterior only: the southern face and lower eastern face. All future-stage boundaries stay open.
   for(int x=105;x<198;x+=6){P("05 - Fachada exterior sur","Wall",x,-132,-16,90);P("06 - Cornisas exteriores","Trim",x,-132.775f,-5.5f,90);}
   for(int z=-129;z<-60;z+=6){P("05 - Fachada exterior sureste","Wall",198,z,-16);P("06 - Cornisas exteriores","Trim",198.775f,z,-5.5f);}
   for(int x=108;x<198;x+=12)P("05 - Arcadas monumentales del cerramiento","Arch",x,-128.175f,-16,90);
   for(int z=-126;z<-60;z+=12)P("05 - Arcadas monumentales del cerramiento","Arch",194.175f,z,-16);
   // Exterior plinths and pilasters are architectural projections, not interior partitions.
   for(int x=105;x<198;x+=6)
   {
    P("05 - Basamentos exteriores sur","Base",x,-133.5f,-16,0,true,new Vector3(1,1,.5f));
    P("05 - Coronacion continua de fachadas","Beam",x,-132,underside,90,true);
   }
   for(int z=-129;z<-60;z+=6)
   {
    P("05 - Basamentos exteriores este","Base",199.5f,z,-16,0,true,new Vector3(.5f,1,1));
    P("05 - Coronacion continua de fachadas","Beam",198,z,underside,0,true);
   }
   for(int x=108;x<198;x+=12)P("05 - Contrafuertes con capitel exteriores","Capital",x,-132.8f,beamBottom,0,true);
   for(int z=-126;z<-60;z+=12)P("05 - Contrafuertes con capitel exteriores","Capital",198.8f,z,beamBottom,0,true);
   for(int x=114;x<192;x+=12)P("05 - Arcos ornamentales exteriores","DecorArch",x,-132.8f,-16,90);
   for(int z=-120;z<-66;z+=12)P("05 - Arcos ornamentales exteriores","DecorArch",198.8f,z,-16);
   // The raised atrium retains its eight original sockets; the low roof never passes below them.
   for(int z=33;z<96;z+=6)foreach(float x in new[]{144f,204f})foreach(float y in new[]{-4f,4f})P("07 - Encuentro vertical de cubiertas","Wall",x,z,y);
   for(int x=147;x<204;x+=6)foreach(float y in new[]{-4f,4f})
   {
    P("07 - Encuentro vertical norte","Wall",x,96,y,90);
    if(x<150||x>=174)P("07 - Encuentro vertical sur - cripta abierta","Wall",x,30,y,90);
   }
   void Shaft(float x,float z,float top,float bottom,string group)
   {
    if(Reserved(x,z,1))return;
    for(float y=top;y>bottom+.02f;y-=4)
    {
     float min=Mathf.Max(bottom-2,y-4);
     var allowed=new Bounds(new Vector3(x,(min+y)/2,z),new Vector3(2,y-min,2));
     P(group,"Post",x,z,y,0,true,null,allowed);
    }
   }
   var oldRoof=context?context.Find("Subsuelo_Techo_Etapa01"):source.Roof;
   foreach(var c in oldRoof.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&c.bounds.size.y>8))
   {
    var b=c.bounds;float x=b.center.x,z=b.center.z;if(!InSector(x-1,z-1)||!InSector(x+1,z+1))continue;
    if(plan.Original.Any(p=>p.name.StartsWith("Column_Stackable_01",StringComparison.Ordinal)&&Mathf.Abs(p.bounds.center.x-x)<.05f&&Mathf.Abs(p.bounds.center.z-z)<.05f&&p.bounds.min.y<-15.9f))continue;
    Shaft(x,z,b.min.y,-16,"08 - Prolongaciones independientes del atrio");
   }
   var upper=(context?context.Find(Dungeon):source.Root).GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&c.name.StartsWith("Column_01",StringComparison.Ordinal)&&AnimationUtility.CalculateTransformPath(c.transform,context).Contains("Soportes"));
   foreach(var list in upper.GroupBy(c=>new Vector2Int(Mathf.RoundToInt(c.bounds.center.x*100),Mathf.RoundToInt(c.bounds.center.z*100))))
   {
    var b=list.OrderBy(c=>c.bounds.min.y).First().bounds;float x=b.center.x,z=b.center.z;
    if(!InSector(x,z)||Atrium(x,z))continue;
    if(b.min.y>-4)Shaft(x,z,b.min.y,-4,"09 - Enlaces con apoyos superiores");
    else if(b.min.y>-15.9f)Shaft(x,z,b.min.y,-16,"09 - Enlaces con apoyos superiores");
   }
   plan.Walks.Add(new[]{new Vector3(162,-16,99),new Vector3(180,-16,99)});
   plan.Walks.Add(new[]{new Vector3(180,-16,99),new Vector3(180,-16,-90)});
   plan.Walks.Add(new[]{new Vector3(180,-16,-90),new Vector3(120,-16,-90)});
   plan.Walks.Add(new[]{new Vector3(18,-16,12),new Vector3(180,-16,12)});
   Physics.SyncTransforms();return plan;
  }
  static void ValidatePlan(Plan plan,bool integrating=false)
  {
   Physics.SyncTransforms();if(!plan.OmittedRail||plan.OmittedRail.activeSelf)throw new InvalidOperationException("La abertura temporal autorizada no esta disponible.");
   if(plan.Cells.Count!=1066)throw new InvalidOperationException("Perimetro inesperado: "+plan.Cells.Count);
   if(plan.Pieces.Select(p=>p.Key+"|"+p.Object.transform.position.ToString("F4")+"|"+p.Object.transform.rotation.ToString("F4")).Distinct().Count()!=plan.Pieces.Count)throw new InvalidOperationException("Duplicados.");
   var created=plan.Root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();var all=plan.Original.Concat(created).ToArray();
   var floor=all.Where(c=>Mathf.Abs(c.bounds.max.y+16)<.025f).ToArray();var hits=new Collider[256];var physics=plan.Root.gameObject.scene.GetPhysicsScene();plan.Samples=0;plan.MinHeadroom=float.PositiveInfinity;
   void Ground(Vector3 p){if(!floor.Any(c=>c.bounds.min.x<=p.x&&c.bounds.max.x>=p.x&&c.bounds.min.z<=p.z&&c.bounds.max.z>=p.z&&c.Raycast(new Ray(p+Vector3.up*.7f,Vector3.down),out var h,.75f)&&h.normal.y>.5f&&Mathf.Abs(h.point.y+16)<.025f))throw new InvalidOperationException("Piso sin continuidad: "+p);}
   foreach(var path in plan.Walks)
   {
    var d=path[1]-path[0];var side=Vector3.Cross(Vector3.up,d.normalized);int steps=Mathf.Max(1,Mathf.CeilToInt(d.magnitude/.4f));
    for(int i=0;i<=steps;i++)for(int lane=0;lane<4;lane++)
    {
     var p=Vector3.Lerp(path[0],path[1],i/(float)steps)+side*((lane-1.5f)*.8f);Ground(p);
     int count=physics.OverlapCapsule(p+Vector3.up*.75f,p+Vector3.up*1.7f,.3f,hits,~0,QueryTriggerInteraction.Ignore);
     if(count>0)throw new InvalidOperationException("Acceso bloqueado en "+p+" / "+hits[0].name);plan.Samples++;
     if(physics.Raycast(p+Vector3.up*2.05f,Vector3.up,out var head,60,~0,QueryTriggerInteraction.Ignore))plan.MinHeadroom=Mathf.Min(plan.MinHeadroom,head.point.y-p.y);
    }
   }
   foreach(var c in plan.Cells){Ground(new Vector3(c.x,-16,c.y));foreach(var offset in new[]{new Vector3(2.9f,0,0),new Vector3(-2.9f,0,0),new Vector3(0,0,2.9f),new Vector3(0,0,-2.9f)})Ground(new Vector3(c.x,-16,c.y)+offset); }
   foreach(var p in plan.Pieces)
   {
    if(!PrefabUtility.GetCorrespondingObjectFromSource(p.Object))throw new InvalidOperationException("Referencia de prefab ausente.");
    if(p.Object.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new InvalidOperationException("Script faltante.");
    foreach(var r in p.Object.GetComponentsInChildren<MeshRenderer>())if(r.enabled&&(r.sharedMaterials.Length==0||r.sharedMaterials.Any(m=>!m||!m.shader)))throw new InvalidOperationException("Material faltante.");
    foreach(var collider in p.Object.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))
    {
     if(collider is MeshCollider mesh&&!mesh.sharedMesh)throw new InvalidOperationException("Collider sin malla.");
     foreach(var old in plan.Original.Where(o=>o is BoxCollider&&o.bounds.Intersects(collider.bounds)))
      if(Physics.ComputePenetration(collider,collider.transform.position,collider.transform.rotation,old,old.transform.position,old.transform.rotation,out var direction,out float depth)&&depth>.035f)throw new InvalidOperationException("Interferencia original: "+p.Key+" / "+old.name+" "+depth+" "+p.Bounds);
    }
   }
   foreach(var pillar in plan.Pieces.Where(p=>p.Key=="Capital"))
   {
    var rs=pillar.Object.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
    if(rs.Length==0||rs.Min(r=>r.bounds.min.y)>-15.965f)throw new InvalidOperationException("Columna sin contacto con piso: "+pillar.Object.transform.position);
   }
   Debug.Log("ETAPA2_DESGLOSE: "+string.Join(", ",plan.Pieces.GroupBy(p=>p.Key).Select(g=>g.Key+"="+g.Count())));
   var pending=new HashSet<Vector2Int>(plan.Cells);var queue=new Queue<Vector2Int>();queue.Enqueue(pending.First());pending.Remove(queue.Peek());
   while(queue.Count>0){var c=queue.Dequeue();foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})if(pending.Remove(c+d*6))queue.Enqueue(c+d*6);}
   if(pending.Count>0)throw new InvalidOperationException("Sectores desconectados.");
   if(plan.Root.GetComponentsInChildren<Light>().Any()||plan.Root.GetComponentsInChildren<ParticleSystem>().Any()||plan.Root.GetComponentsInChildren<UnityEngine.Rendering.Volume>().Any())throw new InvalidOperationException("Efectos no permitidos.");
   if(plan.MinHeadroom<2.1f)throw new InvalidOperationException("Altura libre insuficiente.");
   plan.Source.Verify(integrating);long triangles=plan.Root.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled).Sum(f=>Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(s=>(long)f.sharedMesh.GetIndexCount(s)/3));
   Debug.Log("SUBSUELO_ETAPA2_VALIDACION_OK: "+plan.Pieces.Count+" prefabs; 38376m2, envolvente X-42..204 Z-132..114; central144x120, sureste96x162, atrio66x84. PisoY-16, cubiertaY-4; atrio574 y ocho huecos conservados, ampliacion alta norte6m. "+plan.Samples+" muestras de cuatro carriles; altura libre medida "+plan.MinHeadroom.ToString("F2")+"m. Renderers="+plan.Root.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled)+", colliders="+created.Length+", triangulos="+triangles+", encajes temporales="+plan.Fitted+", celdas existentes reutilizadas="+plan.ReusedFloor+". Baranda omitida SOLO en copia autorizada; 129 originales intactos. Fronteras futuras abiertas; sin combinar mallas ni editar assets. Sin guardar original.",plan.Root);
  }

  const string IntegratedRoot="Subsuelo_Central_Sureste_Etapa02",Signature="__ROM_SubsueloEtapa2_3084_Approved_v1";
  static string MeshDigest(Mesh mesh)
  {
   using(var stream=new System.IO.MemoryStream())
   {
    using(var writer=new System.IO.BinaryWriter(stream,System.Text.Encoding.UTF8,true))
    {
     var vertices=mesh.vertices;writer.Write(vertices.Length);foreach(var p in vertices){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}
     var normals=mesh.normals;writer.Write(normals.Length);foreach(var p in normals){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}
     var uv=mesh.uv;writer.Write(uv.Length);foreach(var p in uv){writer.Write(p.x);writer.Write(p.y);}
     var tangents=mesh.tangents;writer.Write(tangents.Length);foreach(var p in tangents){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);writer.Write(p.w);}
     writer.Write(mesh.subMeshCount);for(int s=0;s<mesh.subMeshCount;s++){var indices=mesh.GetIndices(s);writer.Write((int)mesh.GetTopology(s));writer.Write(indices.Length);foreach(int index in indices)writer.Write(index);}
    }
    using(var sha=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(stream.ToArray()));
   }
  }
  static GameObject FindGate(Transform access,bool includeInactive=false)
  {
   var candidates=access.GetComponentsInChildren<Collider>(includeInactive).Where(c=>c.enabled&&!c.isTrigger&&c is BoxCollider).Where(c=>
   {
    var box=(BoxCollider)c;var b=TransformBounds(new Bounds(box.center,box.size),box.transform.localToWorldMatrix);
    return Mathf.Abs(b.center.x-167.47f)<.1f&&Mathf.Abs(b.center.z-99)<.1f&&Mathf.Abs(b.min.y+16)<.1f&&Mathf.Abs(b.size.z-6)<.05f;
   }).ToArray();
   if(candidates.Length!=1)throw new InvalidOperationException("El unico tramo de baranda autorizado no es identificable.");return candidates[0].gameObject;
  }
  static void EnsureAssetFolder(string path)
  {
   if(AssetDatabase.IsValidFolder(path))return;string parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');EnsureAssetFolder(parent);
   if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path))))throw new InvalidOperationException("No se pudo crear "+path);
  }
  [MenuItem(Menu+"8. Integrar Etapa 2 aprobada y guardar tras validar %#&f")]
  public static void IntegrateApproved()
  {
   if(!Ready)return;int undo=-1;bool saved=false;string meshFolder=null;Source source=null;
   try
   {
    var preview=StageUtility.GetCurrentStage() as CentralPreview;
    string approved=preview&&preview.Plan!=null?Layout(preview.Plan.Root):null;
    Close();var initial=Inspect();
    if(initial.Dirty)throw new InvalidOperationException("La original tiene cambios previos sin guardar; no se guardaran con esta etapa.");
    var existing=initial.OriginalRoots.Where(g=>g.name==IntegratedRoot).ToArray();
    if(existing.Length>0)
    {
     if(existing.Length!=1||!existing[0].transform.Find(Signature)||existing[0].GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=3084||FindGate(initial.Access,true).activeSelf)
      throw new InvalidOperationException("Etapa 2 existente ambigua o distinta de la aprobada.");
     Debug.Log("SUBSUELO_ETAPA2_YA_INTEGRADA: 3084 prefabs, sin duplicados ni cambios.",existing[0]);return;
    }
    if(approved==null)throw new InvalidOperationException("Abri primero la copia temporal aprobada para comparar la distribucion y la geometria.");
    if(initial.OriginalRoots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name==Root||t.name=="Contexto original - solo copias"||t.name.StartsWith("TEMP_CiudadDelSol",StringComparison.Ordinal)))throw new InvalidOperationException("Objetos temporales inesperados en la original.");
    var gate=FindGate(initial.Access);string gatePath=AnimationUtility.CalculateTransformPath(gate.transform,initial.Access);
    string backup="Temp/ROM_Backups/Dungeon_Isla_del_Sol_before_SubsueloEtapa2_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));System.IO.File.Copy(Target,backup,false);
    Undo.IncrementCurrentGroup();undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Integrar subsuelo Etapa 2 aprobado");
    // Sole authorised existing-object change: retain the railing as an inactive, recoverable prefab instance.
    Undo.RecordObject(gate,"Abrir paso de seis metros desde Etapa 1");gate.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(gate);
    source=Inspect(); // Capture preservation checks after the one explicitly authorised change.
    var plan=Build(source,null,source.Scene,LoadAssets(),gate,true);
    if(plan.Pieces.Count!=3084||plan.Root.GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=3084)throw new InvalidOperationException("Se requieren exactamente 3084 prefabs.");
    string generated=Layout(plan.Root);if(generated!=approved)throw new InvalidOperationException("La geometria o distribucion difiere de la previsualizacion aprobada: "+approved+" / "+generated);
    ValidatePlan(plan,true);
    EnsureAssetFolder("Assets/ROM/Generated");meshFolder=AssetDatabase.GenerateUniqueAssetPath("Assets/ROM/Generated/SubsueloEtapa02");EnsureAssetFolder(meshFolder);
    AssetDatabase.StartAssetEditing();
    try
    {
     for(int i=0;i<plan.Meshes.Count;i++)
     {
      var mesh=plan.Meshes[i];string name="Encaje_"+i.ToString("D3");string digest=MeshDigest(mesh);mesh.hideFlags=HideFlags.None;mesh.name=name;
      AssetDatabase.CreateAsset(mesh,meshFolder+"/Encaje_"+i.ToString("D3")+".asset");mesh.name=name;EditorUtility.SetDirty(mesh);
      if(MeshDigest(mesh)!=digest)throw new InvalidOperationException("La persistencia cambio un encaje aprobado.");
     }
    }
    finally{AssetDatabase.StopAssetEditing();}
    foreach(var mesh in plan.Meshes){if(!mesh||!AssetDatabase.Contains(mesh))throw new InvalidOperationException("Encaje sin asset persistente.");AssetDatabase.SaveAssetIfDirty(mesh);}
    foreach(var c in plan.Root.GetComponentsInChildren<Component>(true))if(c&&PrefabUtility.IsPartOfPrefabInstance(c)&&PrefabUtility.GetCorrespondingObjectFromSource(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    if(Layout(plan.Root)!=generated)throw new InvalidOperationException("Persistir las mallas altero la geometria aprobada.");
    plan.Root.name=IntegratedRoot;var signature=new GameObject(Signature);signature.transform.SetParent(plan.Root,false);signature.hideFlags=HideFlags.HideInHierarchy;
    if(!initial.OriginalRoots.Select(g=>g.GetInstanceID()).SequenceEqual(source.Scene.GetRootGameObjects().Where(g=>g!=plan.Root.gameObject).Select(g=>g.GetInstanceID())))throw new InvalidOperationException("Raices originales alteradas.");
    source.Verify(true);
    if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage()||source.Scene.path!=Target)throw new InvalidOperationException("Contexto de guardado incorrecto.");
    EditorSceneManager.MarkSceneDirty(source.Scene);if(!EditorSceneManager.SaveScene(source.Scene,Target,false))throw new InvalidOperationException("Fallo el guardado de la original.");
    saved=true;Undo.CollapseUndoOperations(undo);
    if(source.Scene.isDirty||source.Scene.GetRootGameObjects().Count(g=>g.name==IntegratedRoot)!=1||Layout(plan.Root)!=generated)throw new InvalidOperationException("Comprobacion posterior al guardado fallida.");
    Debug.Log("SUBSUELO_ETAPA2_INTEGRADA_GUARDADA: 3084 prefabs; distribucion y geometria="+generated+"; piso Y-16, cubierta Y-4, limites futuros abiertos; "+plan.Samples+" muestras y altura libre "+plan.MinHeadroom.ToString("F2")+"m. Unico objeto existente modificado: Subsuelo_Acceso_Etapa01/"+gatePath+" (baranda6m desactivada, no eliminada). "+plan.Meshes.Count+" mallas de encaje persistidas SIN combinar ni modificar prefabs originales en "+meshFolder+". Atrio574 y ocho huecos, Umbral639, siete zonas, caminos y Salida Meridional intactos. Backup: "+backup+". Escena guardada: "+Target,plan.Root);Selection.activeGameObject=plan.Root.gameObject;
   }
   catch(Exception e)
   {
    if(!saved&&undo>=0)Undo.RevertAllDownToGroup(undo);
    if(!saved&&meshFolder!=null&&AssetDatabase.IsValidFolder(meshFolder))AssetDatabase.DeleteAsset(meshFolder);
    Debug.LogError("SUBSUELO_ETAPA2_INTEGRACION_DETENIDA: "+e.Message);Debug.LogException(e);
   }
  }
  static string Layout(Transform root)
  {
   var rows=new List<string>();var meshHashes=new Dictionary<Mesh,string>();var culture=System.Globalization.CultureInfo.InvariantCulture;
   string V(Vector3 v)=>v.ToString("F4",culture);
   foreach(var t in root.GetComponentsInChildren<Transform>(true))
   {
    if(t.name==Signature)continue;
    string row=(t==root?"":AnimationUtility.CalculateTransformPath(t,root))+"|"+V(t.localPosition)+"|"+t.localRotation.ToString("F4",culture)+"|"+V(t.localScale)+"|"+t.gameObject.activeSelf;
    if(PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))row+="|prefab:"+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
    foreach(var r in t.GetComponents<Renderer>()){row+="|renderer:"+r.enabled;foreach(var m in r.sharedMaterials)row+="|material:"+AssetDatabase.GetAssetPath(m);}
    foreach(var c in t.GetComponents<Collider>())
    {
     row+="|collider:"+c.GetType().Name+"|"+c.enabled+"|"+c.isTrigger;
     if(c is BoxCollider box)row+="|"+V(box.center)+"|"+V(box.size);
     if(c is MeshCollider mesh)row+="|"+(mesh.sharedMesh?MeshDigest(mesh.sharedMesh):"null")+"|"+mesh.convex;
    }
    foreach(var f in t.GetComponents<MeshFilter>())
    {if(!f.sharedMesh)throw new InvalidOperationException("Malla ausente.");if(!meshHashes.TryGetValue(f.sharedMesh,out var digest))meshHashes[f.sharedMesh]=digest=MeshDigest(f.sharedMesh);row+="|mesh-content:"+digest;}
    rows.Add(row);
   }
   rows.Sort(StringComparer.Ordinal);return Hash128.Compute(string.Join("\n",rows)).ToString();
  }


  [MenuItem(Menu+"9. Corregir nombres de mallas guardadas - sin reintegrar %#&i")]
  public static void NormalizeSavedMeshNames()
  {
   if(!Ready)return;
   try
   {
    var scene=SceneManager.GetActiveScene();
    if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage()||scene.path!=Target||scene.isDirty)throw new InvalidOperationException("La original debe estar abierta y guardada; no se guardaran cambios ajenos.");
    var roots=scene.GetRootGameObjects().Where(g=>g.name==IntegratedRoot).ToArray();
    if(roots.Length!=1||!roots[0].transform.Find(Signature)||roots[0].GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=3084)throw new InvalidOperationException("Etapa 2 guardada ausente o ambigua.");
    const string folder="Assets/ROM/Generated/SubsueloEtapa02";
    var paths=AssetDatabase.FindAssets("t:Mesh",new[]{folder}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
    if(paths.Length!=287)throw new InvalidOperationException("Se esperaban 287 mallas guardadas.");
    string sceneHash=HashScene(),layout=Layout(roots[0].transform);
    var data=paths.ToDictionary(p=>p,p=>MeshDigest(AssetDatabase.LoadAssetAtPath<Mesh>(p)));
    var guids=paths.ToDictionary(p=>p,p=>AssetDatabase.AssetPathToGUID(p));int renamed=0;
    AssetDatabase.StartAssetEditing();
    try
    {
     foreach(var path in paths)
     {
      var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);string expected=System.IO.Path.GetFileNameWithoutExtension(path);
      if(mesh.name==expected)continue;
      mesh.name=expected;EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);renamed++;
     }
    }
    finally{AssetDatabase.StopAssetEditing();}
    foreach(var path in paths)
    {
     var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
     if(!mesh||mesh.name!=System.IO.Path.GetFileNameWithoutExtension(path)||MeshDigest(mesh)!=data[path]||AssetDatabase.AssetPathToGUID(path)!=guids[path])throw new InvalidOperationException("Cambio inesperado en malla: "+path);
    }
    if(HashScene()!=sceneHash||scene.isDirty||Layout(roots[0].transform)!=layout)throw new InvalidOperationException("La escena o geometria fue alterada durante la normalizacion.");
    Debug.Log("SUBSUELO_ETAPA2_NOMBRES_CORREGIDOS: "+renamed+" nombres normalizados; 287 mallas con nombre y archivo coincidentes, datos geometricos y GUID identicos. 3084 prefabs guardados; escena sin cambios ni nuevo guardado. No se ejecuto el generador ni la integracion.");
   }
   catch(Exception e){Debug.LogError("SUBSUELO_ETAPA2_NOMBRES_DETENIDO: "+e.Message);Debug.LogException(e);}
  }
  sealed class CentralPreview:PreviewSceneStage
  {
   public Source Source;public Dictionary<string,Asset> Assets;public Plan Plan;
   protected override GUIContent CreateHeaderContent()=>new GUIContent("Subsuelo Etapa 2 - Central y sureste - COPIA TEMPORAL");
   protected override bool OnOpenStage()
   {
    if(Source==null||Assets==null||!base.OnOpenStage())return false;
    try
    {
     Scene temporary=scene;temporary.name="TEMP_CiudadDelSol_Subsuelo_Etapa2";
     var context=new GameObject("Contexto original - solo copias");SceneManager.MoveGameObjectToScene(context,scene);context.SetActive(false);
     foreach(var original in Source.OriginalRoots.Where(g=>g.name==Dungeon||g.name=="Umbral_De_Las_Profundidades"||g.name=="Salida_Meridional_Generated"||g.name=="Subsuelo_Techo_Etapa01"||g.name=="Subsuelo_Acceso_Etapa01"))
     {var copy=Object.Instantiate(original,context.transform);copy.name=original.name;copy.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);copy.transform.localScale=original.transform.lossyScale;}
     DisableImportedBehaviour(context);context.SetActive(true);
     var access=context.transform.Find("Subsuelo_Acceso_Etapa01");var gate=access.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&Mathf.Abs(c.bounds.center.x-167.47f)<.1f&&Mathf.Abs(c.bounds.center.z-99)<.1f&&Mathf.Abs(c.bounds.min.y+16)<.1f).ToArray();
     if(gate.Length!=1)throw new InvalidOperationException("Tramo de baranda autorizado ambiguo.");var omitted=gate[0].gameObject;omitted.SetActive(false);
     foreach(string path in new[]{"03 - Plaza del Eclipse","06 - Santuario Profano"})
     {var zone=context.transform.Find(Dungeon+"/"+path);var boxes=zone.GetComponentsInChildren<BoxCollider>().Where(c=>c.enabled&&c.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)).ToArray();var b=boxes[0].bounds;foreach(var c in boxes.Skip(1))b.Encapsulate(c.bounds);Debug.Log("ETAPA2_INSPECCION_REAL "+path+" "+b);}
     Plan=Build(Source,context.transform,scene,Assets,omitted);ValidatePlan(Plan);
     EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this){Top();Selection.activeGameObject=null;}};
     Debug.Log("SUBSUELO_ETAPA2_PREVIEW_LISTA: cuatro vistas en menu2..5. Integracion exclusivamente mediante el comando explicito 8.");
    }
    catch(Exception e){Debug.LogException(e);if(Plan!=null)EditorApplication.delayCall+=()=>{if(this)Top();};}return true;
   }
   protected override void OnCloseStage(){try{if(Source!=null)Source.Verify();}finally{base.OnCloseStage();if(Plan!=null)foreach(var mesh in Plan.Meshes)if(mesh)Object.DestroyImmediate(mesh);}}
  }
 }
}
