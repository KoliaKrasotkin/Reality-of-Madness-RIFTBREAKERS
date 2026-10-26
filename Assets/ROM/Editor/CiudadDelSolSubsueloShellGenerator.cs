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
 public static class CiudadDelSolSubsueloShellGenerator
 {
  const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity",Dungeon="Dungeon_Ciudad_del_Sol_Generated",Route="06-07 -";
  const string Root="Subsuelo_Piso_Perimetro_Preview",Menu="ROM/Dungeon Ciudad del Sol/Piso y paredes del subsuelo/";
  const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/";
  static readonly Dictionary<string,string> Paths=new Dictionary<string,string>{
   {"Base",ET+"Pits and Platforms/Platform_Base_01.prefab"},
   {"Wall",ET+"Bases/Thin/Base_Wall_02.prefab"},
   {"Trim",ET+"Railings and Decors/Bottom_Decor_01.prefab"}
  };
  sealed class Asset{public GameObject Prefab;public Bounds Bounds;}
  sealed class Source{public Scene Scene;public Transform Root,Route,Umbral,Roof;public string FileHash;public bool Dirty;public GameObject[] OriginalRoots;
   public readonly List<Action> Checks=new List<Action>();
   public void Verify(){foreach(var c in Checks)c();if(Scene.isDirty!=Dirty||HashScene()!=FileHash)throw new InvalidOperationException("La escena original fue alterada.");}
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
            source.Roof=roof[0].transform;return source;
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


  sealed class Piece{public string Key;public GameObject Object;public Bounds Bounds;}
  sealed class Plan{public Source Source;public Transform Root,Context;public readonly List<Piece> Pieces=new List<Piece>();public readonly List<Mesh> Meshes=new List<Mesh>();public int Samples;}
  [MenuItem(Menu+"1. Previsualizar piso y perimetro (copia temporal) %#&b")]
  public static void Preview(){if(!Ready)return;try{Close();var p=ScriptableObject.CreateInstance<ShellPreview>();p.Source=Inspect();p.Assets=LoadAssets();StageUtility.GoToStage(p,true);}catch(Exception e){Debug.LogException(e);}}
  [MenuItem(Menu+"2. Vista del interior completo %#&n")]
  public static void Interior()=>View(new Vector3(174,5,60),Quaternion.Euler(48,-25,0),34,0);
  [MenuItem(Menu+"3. Vista de conexion con la cripta %#&o")]
  public static void Crypt()=>View(new Vector3(162,6.5f,30),Quaternion.Euler(-2,0,0),10,1);
  [MenuItem(Menu+"4. Vista exterior del perimetro %#&v")]
  public static void Exterior()=>View(new Vector3(174,8,60),Quaternion.Euler(14,-35,0),38,2);
  static void View(Vector3 pivot,Quaternion rotation,float size,int mode)
  {
   var s=StageUtility.GetCurrentStage() as ShellPreview;if(!s||s.Plan==null)return;
   foreach(Transform root in s.Plan.Context)
   {
    bool show=root.name=="Subsuelo_Techo_Etapa01"||root.name=="Umbral_De_Las_Profundidades";
    if(show)SceneVisibilityManager.instance.Show(root.gameObject,true);else SceneVisibilityManager.instance.Hide(root.gameObject,true);
   }
   var umbral=s.Plan.Context.Find("Umbral_De_Las_Profundidades");
   if(mode!=1)foreach(Transform g in umbral)
   {
    if(g.name!="02 - Espesor y contrafuertes")SceneVisibilityManager.instance.Hide(g.gameObject,true);
    else foreach(Transform p in g)if(!p.name.StartsWith("SM_GranitePillar",StringComparison.Ordinal))SceneVisibilityManager.instance.Hide(p.gameObject,true);
   }
   var roof=s.Plan.Context.Find("Subsuelo_Techo_Etapa01");
   if(mode==0)foreach(Transform g in roof)if(g.name.StartsWith("01 -")||g.name.StartsWith("02 -"))SceneVisibilityManager.instance.Hide(g.gameObject,true);
   var view=SceneView.lastActiveSceneView;if(view){view.sceneLighting=false;view.sceneViewState.showImageEffects=false;view.orthographic=false;view.LookAtDirect(pivot,rotation,size);view.Repaint();}
  }
  [MenuItem(Menu+"5. Validar piso, altura y acceso")]
  public static void Validate(){try{var p=StageUtility.GetCurrentStage() as ShellPreview;if(!p||p.Plan==null)throw new InvalidOperationException("Abri la copia temporal.");ValidatePlan(p.Plan);}catch(Exception e){Debug.LogException(e);}}
  [MenuItem(Menu+"6. Cerrar previsualizacion sin guardar")]
  public static void Close(){if(Ready&&StageUtility.GetCurrentStage() is ShellPreview)StageUtility.GoToMainStage();}

  // Mesh fitting affects only these temporary instances. Source prefab meshes and original colliders remain untouched.
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
  static Plan Build(Source source,Transform context,Scene scene,Dictionary<string,Asset> assets)
  {
   var root=new GameObject(Root);SceneManager.MoveGameObjectToScene(root,scene);var plan=new Plan{Source=source,Context=context,Root=root.transform};
   var originals=context.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
   var groups=new Dictionary<string,Transform>();var unique=new HashSet<string>();
   void P(string key,string group,float x,float z,float bottom,float yaw=0,Bounds? limit=null)
   {
    var a=assets[key];var q=Quaternion.Euler(0,yaw,0);var b=TransformBounds(a.Bounds,Matrix4x4.Rotate(q));var position=new Vector3(x-b.center.x,bottom-b.min.y,z-b.center.z);b.center+=position;
    if(!unique.Add(key+position.ToString("F4")+yaw))throw new InvalidOperationException("Duplicado.");
    if(!groups.TryGetValue(group,out var parent)){var g=new GameObject(group);g.transform.SetParent(root.transform,false);groups[group]=parent=g.transform;}
    var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,scene);go.transform.SetParent(parent,true);go.transform.SetPositionAndRotation(position,q*a.Prefab.transform.localRotation);go.transform.localScale=a.Prefab.transform.localScale;DisableImportedBehaviour(go);go.isStatic=true;
    if(key!="Base")
    {
     var clip=limit??b;var cuts=originals.Where(c=>c.bounds.Intersects(b)).Select(c=>{var box=c.bounds;box.Expand(.012f);return box;}).ToArray();Fit(plan,go,clip,cuts);
    }
    if(!go.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))throw new InvalidOperationException("Modulo sin geometria solida: "+key+" "+position);
    plan.Pieces.Add(new Piece{Key=key,Object=go,Bounds=b});
   }
   for(int x=0;x<10;x++)for(int z=0;z<10;z++)P("Base","01 - Piso continuo de piedra",147+x*6,33+z*6,2);
   // Native 8m wall modules fit between the existing capitals and below the vaulted ribs. Cuts use only copied collider envelopes.
   for(int i=0;i<10;i++)
   {
    float along=147+i*6;
    foreach(float x in new[]{144f,204f})
    {
     var allowed=new Bounds(new Vector3(x,7.678f,60),new Vector3(2,7.356f,59.2f));
     P("Wall","02 - Muros exteriores oeste y este",x,33+i*6,4,0,allowed);
     P("Trim","05 - Molduras exteriores de piedra",x==144?143.225f:204.775f,33+i*6,10.4f);
    }
    foreach(float z in new[]{30f,90f})
    {
     if(z==30&&(i==2||i==3))continue; // Real 12m crypt opening, not a decorative doorway.
     var allowed=new Bounds(new Vector3(174,7.678f,z),new Vector3(60,7.356f,2));
     P("Wall",z==30?"03 - Fachada de acceso abierto":"04 - Muro exterior norte",along,z,4,90,allowed);
     P("Trim","05 - Molduras exteriores de piedra",along,z==30?29.225f:90.775f,10.4f,90);
    }
   }
   Physics.SyncTransforms();return plan;
  }
  static void ValidatePlan(Plan plan)
  {
   Physics.SyncTransforms();var physics=plan.Root.gameObject.scene.GetPhysicsScene();var hits=new Collider[256];
   var originals=plan.Context.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
   var all=originals.Concat(plan.Root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger)).ToArray();
   if(plan.Pieces.Select(p=>p.Key+"|"+p.Object.transform.position.ToString("F4")+"|"+p.Object.transform.rotation.ToString("F4")).Distinct().Count()!=plan.Pieces.Count)throw new InvalidOperationException("Prefabs duplicados.");
   plan.Samples=0;
   if(plan.Pieces.Count!=176||plan.Pieces.Count(p=>p.Key=="Base")!=100)throw new InvalidOperationException("Cantidad de modulos inesperada.");
   foreach(var piece in plan.Pieces)
   {
    if(!PrefabUtility.GetCorrespondingObjectFromSource(piece.Object))throw new InvalidOperationException("Se perdio referencia al prefab.");
    foreach(var c in piece.Object.GetComponentsInChildren<MeshCollider>())if(c.enabled&&(!c.sharedMesh||c.sharedMesh.vertexCount==0))throw new InvalidOperationException("MeshCollider vacio.");
    foreach(var r in piece.Object.GetComponentsInChildren<MeshRenderer>())if(r.enabled&&r.sharedMaterials.Any(m=>!m||!m.shader))throw new InvalidOperationException("Material ausente.");
    if(piece.Object.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new InvalidOperationException("Script faltante.");
    if(piece.Key!="Base")foreach(var c in piece.Object.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))foreach(var old in originals.Where(o=>o is BoxCollider&&o.bounds.Intersects(c.bounds)))
     if(Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,old,old.transform.position,old.transform.rotation,out var wallDirection,out float wallDepth)&&wallDepth>.025f)throw new InvalidOperationException("Muro interfiere con original: "+old.name+" "+wallDepth);
    if(piece.Key=="Base")foreach(var c in piece.Object.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))foreach(var old in originals)
    {
     if(!c.bounds.Intersects(old.bounds))continue;
     if(Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,old,old.transform.position,old.transform.rotation,out var direction,out float depth)&&depth>.035f)
     {
      bool foundation=(old.name.StartsWith("SM_GranitePillar",StringComparison.Ordinal)||old.name.StartsWith("Column_Ceiling_02",StringComparison.Ordinal))&&old.bounds.min.y<4&&old.bounds.max.y>4;
      if(!foundation)throw new InvalidOperationException("Piso interfiere con original: "+old.name+" "+old.bounds);
     }
    }
   }
   var floors=all.Where(c=>Mathf.Abs(c.bounds.max.y-4)<.03f).ToArray();
   void Floor(Vector3 p){if(!floors.Any(c=>c.bounds.min.x<=p.x&&c.bounds.max.x>=p.x&&c.bounds.min.z<=p.z&&c.bounds.max.z>=p.z&&c.Raycast(new Ray(p+Vector3.up*.6f,Vector3.down),out var hit,.65f)&&hit.normal.y>.5f&&Mathf.Abs(hit.point.y-4)<.025f))throw new InvalidOperationException("Hueco o desnivel: "+p);}
   void Clear(Vector3 a,Vector3 b)
   {
    var d=b-a;var side=Vector3.Cross(Vector3.up,d.normalized);int steps=Mathf.CeilToInt(d.magnitude/.25f);
    for(int lane=0;lane<4;lane++)for(int i=0;i<=steps;i++)
    {
     var p=Vector3.Lerp(a,b,i/(float)steps)+side*(lane-1.5f);Floor(p);
     int count=physics.OverlapCapsule(p+Vector3.up*.75f,p+Vector3.up*1.7f,.3f,hits,~0,QueryTriggerInteraction.Ignore);
     if(count>0)throw new InvalidOperationException("Circulacion bloqueada: "+p+" / "+hits[0].name);plan.Samples++;
    }
   }
   Clear(new Vector3(162,4,6.2f),new Vector3(162,4,29.5f));Clear(new Vector3(162,4,29.5f),new Vector3(165,4,36));Clear(new Vector3(165,4,36),new Vector3(165,4,87));
   Clear(new Vector3(148,4,48),new Vector3(200,4,48));Clear(new Vector3(148,4,84),new Vector3(200,4,84));
   for(float x=144.25f;x<204;x+=.5f)for(float z=30.25f;z<90;z+=.5f)Floor(new Vector3(x,4,z));
   if(plan.Root.GetComponentsInChildren<Light>().Any()||plan.Root.GetComponentsInChildren<ParticleSystem>().Any())throw new InvalidOperationException("Efectos no permitidos.");
   plan.Source.Verify();Debug.Log("SUBSUELO_PISO_VALIDACION_OK: 176 prefabs (100 pisos, 38 muros, 38 molduras). 60x60m X144..204 Z30..90, superficie Y4, base Y2. Muros hasta Y11.356, bajo vigas existentes Y11.357; molduras exteriores Y10.4..11.08; intrados minimo existente Y9.41. Entrada central 12m X156..168; ruta de cuatro jugadores y "+plan.Samples+" muestras de capsula comprobadas; piso continuo comprobado cada 0.5m. Encajes de columnas sobre cimentacion intencionales; muros recortados solo en copias para respetar soportes existentes. Techo 574 y ocho huecos, Umbral639, zonas, caminos y salida intactos. Sin guardar original.",plan.Root);
  }
  sealed class ShellPreview:PreviewSceneStage
  {
   public Source Source;public Dictionary<string,Asset> Assets;public Plan Plan;
   protected override GUIContent CreateHeaderContent()=>new GUIContent("Piso y paredes del subsuelo - copia temporal");
   protected override bool OnOpenStage()
   {
    if(Source==null||Assets==null||!base.OnOpenStage())return false;
    try
    {
     Scene temporary=scene;temporary.name="TEMP_CiudadDelSol_Piso_Perimetro";var context=new GameObject("Contexto original - solo copias");SceneManager.MoveGameObjectToScene(context,scene);context.SetActive(false);
     foreach(var original in Source.OriginalRoots.Where(g=>g.name==Dungeon||g.name=="Umbral_De_Las_Profundidades"||g.name=="Salida_Meridional_Generated"||g.name=="Subsuelo_Techo_Etapa01"))
     {var copy=Object.Instantiate(original,context.transform);copy.name=original.name;copy.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);copy.transform.localScale=original.transform.lossyScale;}
     DisableImportedBehaviour(context);context.SetActive(true);Plan=Build(Source,context.transform,scene,Assets);ValidatePlan(Plan);
     EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this){Interior();Selection.activeGameObject=null;}};
     Debug.Log("SUBSUELO_PISO_PREVIEW_LISTA: vistas 2 interior en corte, 3 cripta, 4 exterior. No hay opcion de integrar hasta aprobacion.");
    }
    catch(Exception e){Debug.LogException(e);}return true;
   }
   protected override void OnCloseStage(){try{if(Source!=null)Source.Verify();}finally{base.OnCloseStage();if(Plan!=null)foreach(var mesh in Plan.Meshes)if(mesh)Object.DestroyImmediate(mesh);}}
  }
 }
}
