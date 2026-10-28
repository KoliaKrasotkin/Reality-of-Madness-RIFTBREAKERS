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
 public static class CiudadDelSolSubsueloNorthernWingGenerator
 {
  const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity",Dungeon="Dungeon_Ciudad_del_Sol_Generated",Route="06-07 -";
  const string Root="Subsuelo_Ala_Norte_Etapa04_Preview",Menu="ROM/Dungeon Ciudad del Sol/Subsuelo - Etapa 4 Ala norte/";
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
            source.Access=access[0].transform;
            var stage2=scene.GetRootGameObjects().Where(g=>g.name=="Subsuelo_Central_Sureste_Etapa02").ToArray();
            if(stage2.Length!=1||!stage2[0].transform.Find("__ROM_SubsueloEtapa2_3084_Approved_v1")||stage2[0].GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=3084)
                throw new InvalidOperationException("Etapa 2 integrada ausente o distinta de 3084 prefabs.");
            var stage3=scene.GetRootGameObjects().Where(g=>g.name=="Subsuelo_Ala_Occidental_Etapa03").ToArray();
            if(stage3.Length!=1||!stage3[0].transform.Find("__ROM_SubsueloEtapa3_2821_Approved_v1")||stage3[0].GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=2821)
             throw new InvalidOperationException("La Etapa 3 integrada no conserva sus 2821 prefabs.");
            if(scene.isDirty)throw new InvalidOperationException("Guarda tus cambios manuales antes de abrir esta copia; la herramienta no guarda la escena.");
            return source;
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
  // Stepped footprint: southern cross-wing and a narrower crown beneath the Claustro.
  static bool InSector(float x,float z)=>(x>=-66&&x<102&&z>=72&&z<144)||(x>=-36&&x<72&&z>=144&&z<192);
  static bool Atrium(float x,float z)=>false;
  static bool InLowRoof(float x,float z)=>InSector(x,z);
  static bool Reserved(float x,float z,float radius=0)
  {
   return (Mathf.Abs(x-24)<2.5f+radius&&z>=60-radius&&z<=183+radius)||
    (Mathf.Abs(z-108)<2.5f+radius&&x>=-117-radius&&x<=93+radius);
  }
  [MenuItem(Menu+"1. Previsualizar ala norte (copia temporal) %&F1")]
  public static void Preview()
  {
   if(!Ready)return;try{Close();if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())throw new InvalidOperationException("Cerra la otra copia temporal desde Scenes.");var stage=ScriptableObject.CreateInstance<NorthernPreview>();stage.Source=Inspect();stage.Assets=LoadAssets();StageUtility.GoToStage(stage,true);}catch(Exception e){Debug.LogException(e);}
  }
  [MenuItem(Menu+"2. Vista cenital del ala norte %&F2")]
  public static void Top()=>View(new Vector3(18,-8,132),Quaternion.Euler(90,0,0),96,0);
  [MenuItem(Menu+"3. Vista interior desde Y-16 %&F3")]
  public static void Interior()=>View(new Vector3(24,-14,126),Quaternion.Euler(0,0,0),24,1);
  [MenuItem(Menu+"4. Vista exterior de muros y cubierta %&F4")]
  public static void Exterior()=>View(new Vector3(18,-10,190),Quaternion.Euler(12,165,0),58,2);
  [MenuItem(Menu+"5. Vista conexiones con Etapas 2 y 3 %&F5")]
  public static void Connection()=>View(new Vector3(-24,-10,84),Quaternion.Euler(65,0,0),105,4);
  [MenuItem(Menu+"6. Vista general hasta Etapa 4 %&F6")]
  public static void General()=>View(new Vector3(24,-2,30),Quaternion.Euler(18,-35,0),245,3);
  static void View(Vector3 pivot,Quaternion rotation,float size,int mode)
  {
   var s=StageUtility.GetCurrentStage() as NorthernPreview;if(!s||s.Plan==null)return;
   SceneVisibilityManager.instance.Show(s.Plan.Context.gameObject,true);
   foreach(Transform root in s.Plan.Context)
   {
    bool show=mode==3||root.name.StartsWith("Subsuelo_",StringComparison.Ordinal);
    if(show)SceneVisibilityManager.instance.Show(root.gameObject,true);else SceneVisibilityManager.instance.Hide(root.gameObject,true);
   }
   foreach(Transform group in s.Plan.Root)
   {
    bool hide=(mode==0||mode==4)&&(group.name.StartsWith("02 -")||group.name.StartsWith("03 -"));
    if(hide)SceneVisibilityManager.instance.Hide(group.gameObject,true);else SceneVisibilityManager.instance.Show(group.gameObject,true);
   }
   if(mode==0||mode==4)
   {
    foreach(string name in new[]{"Subsuelo_Central_Sureste_Etapa02","Subsuelo_Ala_Occidental_Etapa03"})
    foreach(Transform group in s.Plan.Context.Find(name))if(!group.name.StartsWith("01 -"))SceneVisibilityManager.instance.Hide(group.gameObject,true);
    SceneVisibilityManager.instance.Hide(s.Plan.Context.Find("Subsuelo_Techo_Etapa01").gameObject,true);
   }
   var v=SceneView.lastActiveSceneView;if(v){v.sceneLighting=false;v.sceneViewState.showImageEffects=false;v.orthographic=mode==0;v.LookAtDirect(pivot,rotation,size);v.Repaint();}
  }
  [MenuItem(Menu+"7. Validar continuidad colliders y apoyos")]
  public static void Validate(){try{var s=StageUtility.GetCurrentStage() as NorthernPreview;if(!s||s.Plan==null)throw new InvalidOperationException("Abri la copia temporal.");ValidatePlan(s.Plan);}catch(Exception e){Debug.LogException(e);}}
  [MenuItem(Menu+"8. Cerrar previsualizacion sin guardar %&F8")]
  public static void Close(){if(Ready&&StageUtility.GetCurrentStage() is NorthernPreview)StageUtility.GoToMainStage();}
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

  static Plan Build(Source source,Transform context,Scene scene,Dictionary<string,Asset> assets,GameObject omitted)
  {
   var root=new GameObject(Root);SceneManager.MoveGameObjectToScene(root,scene);
   var plan=new Plan{Root=root.transform,Context=context,Source=source,OmittedRail=omitted};
   plan.Original=(context?context.GetComponentsInChildren<Collider>():source.OriginalRoots.SelectMany(g=>g.GetComponentsInChildren<Collider>()).ToArray()).Where(c=>c.enabled&&!c.isTrigger).ToArray();
   plan.OldFloor=plan.Original.Where(c=>c is BoxCollider&&c.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)&&Mathf.Abs(c.bounds.max.y+16)<.02f).Select(c=>c.bounds).ToArray();
   if(plan.OldFloor.Length<16)throw new InvalidOperationException("No se encontro el piso inferior aprobado.");
   var groups=new Dictionary<string,Transform>();var unique=new Dictionary<string,GameObject>();
   Transform Group(string name){if(!groups.TryGetValue(name,out var t)){var g=new GameObject(name);g.transform.SetParent(root.transform,false);groups[name]=t=g.transform;}return t;}
   GameObject P(string group,string key,float x,float z,float y,float yaw=0,bool top=false,Vector3? sizeScale=null,Bounds? allowed=null)
   {
    if(plan.Pieces.Count>=1800)throw new InvalidOperationException("Presupuesto de 1800 prefabs superado; no se ampliara automaticamente.");
    var a=assets[key];var q=Quaternion.Euler(0,yaw,0);var scale=sizeScale??Vector3.one;var b=TransformBounds(a.Bounds,Matrix4x4.TRS(Vector3.zero,q,scale));
    var pivot=new Vector3(x-b.center.x,y-(top?b.max.y:b.min.y),z-b.center.z);b.center+=pivot;
    string id=key+"|"+pivot.ToString("F4")+"|"+yaw+"|"+scale.ToString("F4");if(unique.TryGetValue(id,out var shared))return shared;
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
    unique.Add(id,go);plan.Pieces.Add(new Piece{Key=key,Group=group,Object=go,Bounds=b});return go;
   }
   // Inspect the copied original platforms, then generate only the western footprint.
   for(int x=-63;x<102;x+=6)for(int z=75;z<192;z+=6)if(InSector(x,z))plan.Cells.Add(new Vector2Int(x,z));
   foreach(var cell in plan.Cells.OrderBy(c=>c.x).ThenBy(c=>c.y))
   {
    P("01 - Piso continuo Y-16","Base",cell.x,cell.y,-16,0,true);
    P("02 - Bovedas generales Y-4","Vault",cell.x,cell.y,-4,0,true);
   }
   float underside=-4-assets["Vault"].Bounds.size.y,beamBottom=underside-assets["Beam"].Bounds.size.y;
   foreach(var cell in plan.Cells.Where(c=>((c.x+63)/6)%2==0))
    P("03 - Vigas principales","Beam",cell.x-3,cell.y,underside,0,true);
   for(int x=-54;x<102;x+=12)for(int z=84;z<192;z+=12)
   {
    if(!InSector(x-1,z-1)||!InSector(x+1,z+1)||Reserved(x,z,1.3f))continue;
    var b=new Bounds(new Vector3(x,beamBottom-assets["Capital"].Bounds.size.y/2,z),assets["Capital"].Bounds.size);
    if(plan.Original.Any(c=>c.bounds.Intersects(b)&&c.bounds.max.y>-15.9f))continue;
    P("04 - Columnas monumentales","Capital",x,z,beamBottom,0,true);
   }
   // North crown, western upper return and the northwest shoulder only.
   // South and southwest join saved floors; ALL east-facing boundaries stay open for Stage 5.
   void Facade(float x,float z,bool northFace,int index)
   {
    float yaw=northFace?90:0;
    float ox=northFace?0:-1,oz=northFace?1:0;
    P("05 - Muros exteriores del ala norte","Wall",x,z,-16,yaw);
    bool corner=!northFace&&index==0;
    P("05 - Basamentos de contrafuertes","Base",x+ox*1.5f,corner?z+1.5f:z+oz*1.5f,-16,0,true,northFace?new Vector3(1,1,.5f):new Vector3(.5f,1,corner?.5f:1));
    P("06 - Cornisas y encuentros","Trim",x+ox*.775f,z+oz*.775f,-5.5f,yaw);
    P("06 - Coronacion de muros","Beam",x,z,underside,yaw,true);
    if(index%2==0)
    {
     P("05 - Contrafuertes perimetrales","Capital",x+ox*.8f,z+oz*.8f,beamBottom,0,true);
     float ax=x-ox*3.825f,az=z-oz*3.825f,ex=northFace?6:3.825f,ez=northFace?3.825f:6;
     if(InSector(ax-ex+.02f,az-ez+.02f)&&InSector(ax+ex-.02f,az+ez-.02f))
      P("05 - Arcadas monumentales","Arch",ax,az,-16,northFace?90:0);
    }
   }
   for(int x=-33,i=0;x<72;x+=6,i++)Facade(x,192,true,i);
   for(int z=147,i=0;z<192;z+=6,i++)Facade(-36,z,false,i);
   for(int x=-63,i=0;x<-36;x+=6,i++)Facade(x,144,true,i);
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
   var upper=(context?context.Find(Dungeon):source.Root).GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&c.name.StartsWith("Column_01",StringComparison.Ordinal)&&AnimationUtility.CalculateTransformPath(c.transform,context).Contains("Soportes"));
   foreach(var list in upper.GroupBy(c=>new Vector2Int(Mathf.RoundToInt(c.bounds.center.x*100),Mathf.RoundToInt(c.bounds.center.z*100))))
   {
    var b=list.OrderBy(c=>c.bounds.min.y).First().bounds;float x=b.center.x,z=b.center.z;
    if(!InSector(x,z)||Atrium(x,z))continue;
    if(b.min.y>-4)Shaft(x,z,b.min.y,-4,"09 - Enlaces con apoyos superiores");
    else if(b.min.y>-15.9f)Shaft(x,z,b.min.y,-16,"09 - Enlaces con apoyos superiores");
   }
   plan.Walks.Add(new[]{new Vector3(24,-16,60),new Vector3(24,-16,180)});
   plan.Walks.Add(new[]{new Vector3(-114,-16,108),new Vector3(90,-16,108)});
   // Sample the full seams with four player lanes, not only a single decorative connection.
   for(int x=-39;x<102;x+=6)plan.Walks.Add(new[]{new Vector3(x,-16,66),new Vector3(x,-16,78)});
   for(int z=75;z<144;z+=6)plan.Walks.Add(new[]{new Vector3(-72,-16,z),new Vector3(-60,-16,z)});
   Physics.SyncTransforms();return plan;
  }
  static void ValidatePlan(Plan plan,bool integrating=false)
  {
   Physics.SyncTransforms();
   if(plan.Cells.Count!=480)throw new InvalidOperationException("Perimetro inesperado: "+plan.Cells.Count);
   if(plan.Pieces.Select(p=>p.Key+"|"+p.Object.transform.position.ToString("F4")+"|"+p.Object.transform.rotation.ToString("F4")).Distinct().Count()!=plan.Pieces.Count)throw new InvalidOperationException("Duplicados.");
   var bases=plan.Pieces.Where(p=>p.Key=="Base").ToArray();
   for(int i=0;i<bases.Length;i++)for(int j=i+1;j<bases.Length;j++)
    if(Mathf.Min(bases[i].Bounds.max.x,bases[j].Bounds.max.x)-Mathf.Max(bases[i].Bounds.min.x,bases[j].Bounds.min.x)>.02f&&Mathf.Min(bases[i].Bounds.max.z,bases[j].Bounds.max.z)-Mathf.Max(bases[i].Bounds.min.z,bases[j].Bounds.min.z)>.02f)throw new InvalidOperationException("Losas nuevas superpuestas.");
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
    Ground(new Vector3(pillar.Bounds.center.x,-16,pillar.Bounds.center.z));
   }
   Debug.Log("ETAPA4_DESGLOSE: "+string.Join(", ",plan.Pieces.GroupBy(p=>p.Key).Select(g=>g.Key+"="+g.Count())));
   var pending=new HashSet<Vector2Int>(plan.Cells);var queue=new Queue<Vector2Int>();queue.Enqueue(pending.First());pending.Remove(queue.Peek());
   while(queue.Count>0){var c=queue.Dequeue();foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})if(pending.Remove(c+d*6))queue.Enqueue(c+d*6);}
   if(pending.Count>0)throw new InvalidOperationException("Sectores desconectados.");
   if(plan.Root.GetComponentsInChildren<Light>().Any()||plan.Root.GetComponentsInChildren<ParticleSystem>().Any()||plan.Root.GetComponentsInChildren<UnityEngine.Rendering.Volume>().Any())throw new InvalidOperationException("Efectos no permitidos.");
   if(plan.MinHeadroom<2.1f)throw new InvalidOperationException("Altura libre insuficiente.");
   plan.Source.Verify(integrating);long triangles=plan.Root.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled).Sum(f=>Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(s=>(long)f.sharedMesh.GetIndexCount(s)/3));
   Debug.Log("SUBSUELO_ETAPA4_VALIDACION_OK: "+plan.Pieces.Count+" prefabs; 17280m2 de piso principal +549m2 de basamentos, contorno X-66..102 Z72..192 (168x120m), cuerpo168x72 y corona108x48. Proyecciones de basamentos hasta Z195. PisoY-16, cubiertaY-4; conexion sur con Etapa2 X-42..102 Z72 (144m), conexion oeste con Etapa3 X-66 Z72..144 (72m); este abierto para Etapa5. "+plan.Samples+" muestras de cuatro carriles; altura libre medida "+plan.MinHeadroom.ToString("F2")+"m. Renderers="+plan.Root.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled)+", colliders="+created.Length+", triangulos="+triangles+", encajes temporales="+plan.Fitted+". Originales, Etapas1/2/3, atrio574/ocho huecos y Umbral639 intactos. Solo copia temporal, sin combinar ni guardar assets.",plan.Root);


  }

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
  static void EnsureAssetFolder(string path)
  {
   if(AssetDatabase.IsValidFolder(path))return;string parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');EnsureAssetFolder(parent);
   if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path))))throw new InvalidOperationException("No se pudo crear "+path);
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



  const string IntegratedRoot="Subsuelo_Ala_Norte_Etapa04",Signature="__ROM_SubsueloEtapa4_1477_Approved_v1";
  [MenuItem(Menu+"9. Integrar Etapa 4 aprobada y guardar tras validar %&F7")]
  public static void IntegrateApproved()
  {
   if(!Ready)return;
   int undo=-1;bool saved=false;Plan plan=null;Source source=null;string meshFolder=null;
   try
   {
    var preview=StageUtility.GetCurrentStage() as NorthernPreview;
    if(preview&&preview.Plan!=null)ValidatePlan(preview.Plan);
    string approved=preview&&preview.Plan!=null?Layout(preview.Plan.Root):null;
    Close();source=Inspect();
    var existing=source.OriginalRoots.Where(g=>g.name==IntegratedRoot).ToArray();
    if(existing.Length>0)
    {
     if(existing.Length!=1||!existing[0].transform.Find(Signature)||existing[0].GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=1477)
      throw new InvalidOperationException("Etapa 4 existente ambigua; no se duplicara.");
     ValidatePersistent(existing[0].transform,Layout(existing[0].transform));
     Debug.Log("SUBSUELO_ETAPA4_YA_INTEGRADA: 1477 prefabs; sin duplicados ni cambios.");return;
    }
    if(approved==null)throw new InvalidOperationException("Abri primero la previsualizacion aprobada para comparar geometria y distribucion.");
    if(source.OriginalRoots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name==Root||t.name=="Contexto original - solo copias"||t.name.StartsWith("TEMP_CiudadDelSol",StringComparison.Ordinal)))
     throw new InvalidOperationException("Objetos temporales inesperados en la original.");
    string backup="Temp/ROM_Backups/Dungeon_Isla_del_Sol_before_SubsueloEtapa4_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));System.IO.File.Copy(Target,backup,false);
    if(!System.IO.File.ReadAllBytes(Target).SequenceEqual(System.IO.File.ReadAllBytes(backup)))throw new InvalidOperationException("El respaldo no coincide.");
    Undo.IncrementCurrentGroup();undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Integrar ala norte aprobada");
    plan=Build(source,null,source.Scene,LoadAssets(),null);
    Undo.RegisterCreatedObjectUndo(plan.Root.gameObject,"Integrar 1477 prefabs aprobados");
    if(plan.Pieces.Count!=1477||plan.Root.GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=1477)
     throw new InvalidOperationException("Se requieren exactamente 1477 prefabs.");
    string generated=Layout(plan.Root);
    if(generated!=approved)throw new InvalidOperationException("La distribucion o geometria difiere de la copia aprobada: "+approved+" / "+generated);
    ValidatePlan(plan,true);
    EnsureAssetFolder("Assets/ROM/Generated");meshFolder=AssetDatabase.GenerateUniqueAssetPath("Assets/ROM/Generated/SubsueloEtapa04");EnsureAssetFolder(meshFolder);
    AssetDatabase.StartAssetEditing();
    try
    {
     for(int i=0;i<plan.Meshes.Count;i++)
     {
      var mesh=plan.Meshes[i];string name="Encaje_"+i.ToString("D3"),digest=MeshDigest(mesh);
      mesh.hideFlags=HideFlags.None;mesh.name=name;AssetDatabase.CreateAsset(mesh,meshFolder+"/"+name+".asset");EditorUtility.SetDirty(mesh);
      if(MeshDigest(mesh)!=digest)throw new InvalidOperationException("Persistir altero la malla aprobada.");
     }
    }
    finally{AssetDatabase.StopAssetEditing();}
    foreach(var mesh in plan.Meshes)
    {
     if(!mesh||!AssetDatabase.Contains(mesh))throw new InvalidOperationException("Malla sin asset persistente.");
     AssetDatabase.SaveAssetIfDirty(mesh);
     if(mesh.name!=System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mesh)))throw new InvalidOperationException("Nombre de malla inconsistente.");
    }
    foreach(var c in plan.Root.GetComponentsInChildren<Component>(true))
     if(c&&PrefabUtility.IsPartOfPrefabInstance(c)&&PrefabUtility.GetCorrespondingObjectFromSource(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    if(Layout(plan.Root)!=generated)throw new InvalidOperationException("La persistencia altero la geometria.");
    plan.Root.name=IntegratedRoot;
    var signature=new GameObject(Signature);signature.transform.SetParent(plan.Root,false);signature.hideFlags=HideFlags.HideInHierarchy;
    ValidatePersistent(plan.Root,generated);
    source.Verify(true);
    if(!source.OriginalRoots.Select(g=>g.GetInstanceID()).SequenceEqual(source.Scene.GetRootGameObjects().Where(g=>g!=plan.Root.gameObject).Select(g=>g.GetInstanceID())))
     throw new InvalidOperationException("Raices anteriores alteradas.");
    if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage()||source.Scene.path!=Target)throw new InvalidOperationException("Contexto de guardado incorrecto.");
    EditorSceneManager.MarkSceneDirty(source.Scene);
    if(!EditorSceneManager.SaveScene(source.Scene,Target,false))throw new InvalidOperationException("Fallo el guardado.");
    saved=true;Undo.CollapseUndoOperations(undo);
    // Deserialize the saved scene independently to verify actual persisted references, not only live objects.
    var reopened=EditorSceneManager.OpenPreviewScene(Target);
    try
    {
     var roots=reopened.GetRootGameObjects().Where(g=>g.name==IntegratedRoot).ToArray();
     if(roots.Length!=1)throw new InvalidOperationException("Raiz duplicada o ausente al leer el archivo guardado.");
     ValidatePersistent(roots[0].transform,generated);
    }
    finally{EditorSceneManager.ClosePreviewScene(reopened);}
    foreach(var check in source.Checks)check();
    if(source.Scene.isDirty||Layout(plan.Root)!=generated)throw new InvalidOperationException("La comprobacion posterior al guardado fallo.");
    Debug.Log("SUBSUELO_ETAPA4_INTEGRADA_GUARDADA: 1477 prefabs; hash="+generated+"; pisoY-16 cubiertaY-4; enlace144m con Etapa2 y 72m con Etapa3 abiertos; este abierto para Etapa5. "+plan.Samples+" muestras, altura libre "+plan.MinHeadroom.ToString("F2")+"m. "+plan.Meshes.Count+" mallas persistidas en "+meshFolder+". Relectura del archivo guardado correcta, referencias y colliders completos, cero duplicados. Todos los objetos anteriores intactos. Backup: "+backup+". Escena: "+Target,plan.Root);
    Selection.activeGameObject=plan.Root.gameObject;
   }
   catch(Exception e)
   {
    if(!saved)
    {
     if(undo>=0)Undo.RevertAllDownToGroup(undo);
     if(plan!=null&&plan.Root)Object.DestroyImmediate(plan.Root.gameObject);
     if(meshFolder!=null&&AssetDatabase.IsValidFolder(meshFolder))AssetDatabase.DeleteAsset(meshFolder);
    }
    Debug.LogError("SUBSUELO_ETAPA4_INTEGRACION_DETENIDA: "+e.Message+(saved?" El guardado ya se realizo; revisar validacion de persistencia.":" No se guardo la escena."));
    Debug.LogException(e);
   }
  }
  static void ValidatePersistent(Transform root,string expected)
  {
   if(!root.Find(Signature)||root.GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=1477||Layout(root)!=expected)
    throw new InvalidOperationException("Conteo, firma o geometria persistida incorrectos.");
   foreach(var t in root.GetComponentsInChildren<Transform>(true))
   {
    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Script perdido.");
    foreach(var f in t.GetComponents<MeshFilter>())if(!f.sharedMesh||!AssetDatabase.Contains(f.sharedMesh))throw new InvalidOperationException("Malla no persistente.");
    foreach(var c in t.GetComponents<MeshCollider>())if(c.enabled&&(!c.sharedMesh||!AssetDatabase.Contains(c.sharedMesh)))throw new InvalidOperationException("Collider sin malla persistente.");
    foreach(var r in t.GetComponents<Renderer>())if(r.enabled&&r.sharedMaterials.Any(m=>!m||!m.shader))throw new InvalidOperationException("Material perdido.");
   }
  }


  sealed class NorthernPreview:PreviewSceneStage
  {
   public Source Source;public Dictionary<string,Asset> Assets;public Plan Plan;
   protected override GUIContent CreateHeaderContent()=>new GUIContent("Subsuelo Etapa 4 - Ala norte - COPIA TEMPORAL");
   protected override bool OnOpenStage()
   {
    if(Source==null||Assets==null||!base.OnOpenStage())return false;
    try
    {
     Scene temporary=scene;temporary.name="TEMP_CiudadDelSol_Subsuelo_Etapa4";
     var context=new GameObject("Contexto original - solo copias");SceneManager.MoveGameObjectToScene(context,scene);context.SetActive(false);
     foreach(var original in Source.OriginalRoots.Where(g=>g.name==Dungeon||g.name=="Umbral_De_Las_Profundidades"||g.name=="Salida_Meridional_Generated"||g.name=="Subsuelo_Techo_Etapa01"||g.name=="Subsuelo_Acceso_Etapa01"||g.name=="Subsuelo_Central_Sureste_Etapa02"||g.name=="Subsuelo_Ala_Occidental_Etapa03"))
     {var copy=Object.Instantiate(original,context.transform);copy.name=original.name;copy.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);copy.transform.localScale=original.transform.lossyScale;}
     DisableImportedBehaviour(context);context.SetActive(true);
     var zone=context.transform.Find(Dungeon+"/05 - Claustro del Eclipse");
     var boxes=zone.GetComponentsInChildren<BoxCollider>().Where(c=>c.enabled&&c.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)).ToArray();
     if(boxes.Length==0)throw new InvalidOperationException("No se encontro la plataforma real del Claustro.");
     var bounds=boxes[0].bounds;foreach(var c in boxes.Skip(1))bounds.Encapsulate(c.bounds);
     Debug.Log("ETAPA4_INSPECCION_CLAUSTRO_REAL: "+bounds);
     if(Mathf.Abs(bounds.min.x+36)>.1f||Mathf.Abs(bounds.max.x-72)>.1f||Mathf.Abs(bounds.min.z-96)>.1f||Mathf.Abs(bounds.max.z-192)>.1f)
      throw new InvalidOperationException("Los limites reales del Claustro difieren del trazado previsto.");
     foreach(string name in new[]{"Subsuelo_Central_Sureste_Etapa02","Subsuelo_Ala_Occidental_Etapa03"})
     {
      var floor=context.transform.Find(name+"/01 - Piso continuo Y-16");
      var cs=floor.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&Mathf.Abs(c.bounds.max.y+16)<.025f).ToArray();
      var b=cs[0].bounds;foreach(var c in cs.Skip(1))b.Encapsulate(c.bounds);
      Debug.Log("ETAPA4_INSPECCION_BORDE_EXISTENTE: "+name+" "+b);
      void Seam(float x,float z)
      {
       var p=new Vector3(x,-15.3f,z);
       if(!cs.Any(c=>c.Raycast(new Ray(p,Vector3.down),out var hit,.75f)&&Mathf.Abs(hit.point.y+16)<.025f))
        throw new InvalidOperationException("Falta piso existente en el borde de conexion: "+p);
      }
      if(name.Contains("Central"))for(int x=-39;x<102;x+=6)Seam(x,71.9f);
      else for(int z=75;z<144;z+=6)Seam(-66.1f,z);
     }
     Plan=Build(Source,context.transform,scene,Assets,null);ValidatePlan(Plan);
     EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this){Top();Selection.activeGameObject=null;}};
     Debug.Log("SUBSUELO_ETAPA4_PREVIEW_LISTA: cinco vistas en menu 2..6; solo previsualizacion temporal. Integracion aprobada exclusivamente mediante comando explicito 9.");
    }
    catch(Exception e){Debug.LogException(e);if(Plan!=null)EditorApplication.delayCall+=()=>{if(this)Top();};}return true;
   }
   protected override void OnCloseStage(){try{if(Source!=null)Source.Verify();}finally{base.OnCloseStage();if(Plan!=null)foreach(var mesh in Plan.Meshes)if(mesh)Object.DestroyImmediate(mesh);}}
  }
 }
}
