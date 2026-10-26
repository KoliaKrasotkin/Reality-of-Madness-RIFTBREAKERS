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
 public static class CiudadDelSolSubsueloAccessGenerator
 {
  const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity",Dungeon="Dungeon_Ciudad_del_Sol_Generated",Route="06-07 -";
  const string Root="Subsuelo_Acceso_Etapa01_Preview",Menu="ROM/Dungeon Ciudad del Sol/Acceso al subsuelo - Etapa 1/";
  const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/";
  static readonly Dictionary<string,string> Paths=new Dictionary<string,string>{
   {"Base",ET+"Pits and Platforms/Platform_Base_01.prefab"},
   {"Stair",ET+"Staircases/Staircase_01.prefab"},
   {"StairRail",ET+"Railings and Decors/Railing_Staircase_01.prefab"},
   {"Rail",ET+"Railings and Decors/Railing_01.prefab"},
   {"Column",ET+"Columns/Column_Stackable_01.prefab"},
   {"Arch",ET+"Bases/Arch_Wide_01.prefab"}
  };
  sealed class Asset{public GameObject Prefab;public Bounds Bounds;}
  sealed class Source{public Scene Scene;public Transform Root,Route,Umbral,Roof;public string FileHash;public bool Dirty;public GameObject[] OriginalRoots;
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



  sealed class Piece{public string Key,Group;public GameObject Object;public Bounds Bounds;public Vector3 Scale;}
  sealed class Walk{public Vector3 A,B;public bool Stair;}
  sealed class Plan{public Source Source;public Transform Root,Context;public readonly List<Piece> Pieces=new List<Piece>();public readonly List<Walk> Walks=new List<Walk>();public int Samples;public float MinClearance=float.PositiveInfinity;}
  [MenuItem(Menu+"1. Previsualizar acceso (copia temporal) %#&e")]
  public static void Preview()
  {
   if(!Ready)return;
   try
   {
    if(StageUtility.GetCurrentStage() is AccessPreview)Close();
    if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())throw new InvalidOperationException("Cerra primero la previsualizacion anterior desde Scenes. No se modificara ni copiara su geometria.");
    var stage=ScriptableObject.CreateInstance<AccessPreview>();stage.Source=Inspect();stage.Assets=LoadAssets();StageUtility.GoToStage(stage,true);
   }
   catch(Exception e){Debug.LogException(e);}
  }
  [MenuItem(Menu+"2. Vista desde arriba %#&q")]
  public static void Above()=>View(new Vector3(156,-5,70),Quaternion.Euler(48,-35,0),46,0);
  [MenuItem(Menu+"3. Vista desde la cripta %#&r")]
  public static void Crypt()=>View(new Vector3(158,4.5f,33),Quaternion.Euler(10,-12,0),11,1);
  [MenuItem(Menu+"4. Vista desde el nivel inferior %#&x")]
  public static void Below()=>View(new Vector3(156,-8,76),Quaternion.Euler(-10,180,0),19,2);
  static void View(Vector3 pivot,Quaternion rotation,float size,int mode)
  {
   var s=StageUtility.GetCurrentStage() as AccessPreview;if(!s||s.Plan==null)return;
   foreach(Transform r in s.Plan.Context)
   {
    if(r.name=="Subsuelo_Techo_Etapa01"||r.name=="Umbral_De_Las_Profundidades")SceneVisibilityManager.instance.Show(r.gameObject,true);
    else SceneVisibilityManager.instance.Hide(r.gameObject,true);
   }
   var umbral=s.Plan.Context.Find("Umbral_De_Las_Profundidades");
   if(mode!=1)foreach(Transform g in umbral)
   {
    bool keep=g.name.StartsWith("06 -",StringComparison.Ordinal)||g.name=="02 - Espesor y contrafuertes";
    if(!keep)SceneVisibilityManager.instance.Hide(g.gameObject,true);
    else if(g.name=="02 - Espesor y contrafuertes")foreach(Transform p in g)if(!p.name.StartsWith("SM_GranitePillar",StringComparison.Ordinal))SceneVisibilityManager.instance.Hide(p.gameObject,true);
   }
   if(mode==0)foreach(Transform g in s.Plan.Context.Find("Subsuelo_Techo_Etapa01"))
    if(!g.name.StartsWith("05 -",StringComparison.Ordinal))SceneVisibilityManager.instance.Hide(g.gameObject,true);
   var v=SceneView.lastActiveSceneView;if(v){v.sceneLighting=false;v.sceneViewState.showImageEffects=false;v.orthographic=false;v.LookAtDirect(pivot,rotation,size);v.Repaint();}
  }
  [MenuItem(Menu+"5. Validar pendientes, colliders y accesos")]
  public static void Validate(){try{var s=StageUtility.GetCurrentStage() as AccessPreview;if(!s||s.Plan==null)throw new InvalidOperationException("Abri la copia temporal.");ValidatePlan(s.Plan);}catch(Exception e){Debug.LogException(e);}}
  [MenuItem(Menu+"6. Cerrar previsualizacion sin guardar")]
  public static void Close(){if(Ready&&StageUtility.GetCurrentStage() is AccessPreview)StageUtility.GoToMainStage();}

  static Plan Build(Source source,Transform context,Scene scene,Dictionary<string,Asset> assets,bool undo=false)
  {
   Debug.Log("ACCESO_ASSETS: Stair="+assets["Stair"].Bounds+" Column="+assets["Column"].Bounds);
   if(Mathf.Abs(assets["Stair"].Bounds.max.y-4)>.05f||Mathf.Abs(assets["Stair"].Bounds.size.z-6)>.05f)throw new InvalidOperationException("Dimensiones nativas inesperadas: "+assets["Stair"].Bounds);
   var root=new GameObject(Root);SceneManager.MoveGameObjectToScene(root,scene);if(undo)Undo.RegisterCreatedObjectUndo(root,"Integrar acceso al subsuelo aprobado");var plan=new Plan{Source=source,Root=root.transform,Context=context};
   var groups=new Dictionary<string,Transform>();var unique=new HashSet<string>();
   Transform Group(string name){if(!groups.TryGetValue(name,out var t)){var g=new GameObject(name);g.transform.SetParent(root.transform,false);groups[name]=t=g.transform;}return t;}
   GameObject P(string group,string key,float x,float z,float y,float yaw=0,bool top=false,float length=6)
   {
    var a=assets[key];var q=Quaternion.Euler(0,yaw,0);var scale=Vector3.one;
    if(key=="Rail")scale.z=length/6; // Only terminal railing lengths fit the landing edges; stair, floor and column scales stay native.
    var b=TransformBounds(a.Bounds,Matrix4x4.TRS(Vector3.zero,q,scale));var pivot=new Vector3(x-b.center.x,y-(top?b.max.y:b.min.y),z-b.center.z);b.center+=pivot;
    string id=key+"|"+pivot.ToString("F4")+"|"+yaw+"|"+length;if(!unique.Add(id))throw new InvalidOperationException("Duplicado: "+id);
    var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,scene);go.transform.SetParent(Group(group),true);go.transform.SetPositionAndRotation(pivot,q*a.Prefab.transform.localRotation);go.transform.localScale=Vector3.Scale(a.Prefab.transform.localScale,scale);DisableImportedBehaviour(go);go.isStatic=true;
    if(!go.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))foreach(var f in go.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled)f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
    plan.Pieces.Add(new Piece{Key=key,Group=group,Object=go,Bounds=b,Scale=go.transform.localScale});return go;
   }
   var floors=new HashSet<Vector3>();
   void Floor(string group,float x,float z,float y){if(floors.Add(new Vector3(x,y,z)))P(group,"Base",x,z,y,0,true);}
   void Walk(Vector3 a,Vector3 b,bool stair=false)=>plan.Walks.Add(new Walk{A=a,B=b,Stair=stair});
   void Rail(string group,float x,float z,float y,float yaw=0,float length=6)=>P(group,"Rail",x,z,y,yaw,false,length);
   // The real crypt ends at Z30, at Y4. No approved floor is duplicated.
   foreach(float x in new[]{147f,153f,159f,165f,171f})
   {Floor("01 - Atrio de enlace Y4",x,33,4);Floor("06 - Cimentacion de apoyos",x,33,-16);}
   Walk(new Vector3(162,4,7),new Vector3(162,4,33.45f));
   Walk(new Vector3(162,4,33.45f),new Vector3(150,4,33.45f));Walk(new Vector3(150,4,33.45f),new Vector3(150,4,36));
   Rail("04 - Pretiles del enlace",146.45f,33,4);
   foreach(float x in new[]{159f,165f,171f})Rail("04 - Pretiles del enlace",x,35.47f,4,90);
   Rail("04 - Pretiles del enlace",154.5f,35.47f,4,90,3);
   // Five genuine native stair flights: 4m rise / 6m run, with 6m horizontal intermediate landings.
   for(int i=0;i<5;i++)
   {
    float highZ=36+i*12,highY=4-i*4,lowZ=highZ+6,lowY=highY-4;
    P("02 - Cinco tramos descendentes","Stair",150,highZ+3,lowY,270);
    foreach(float sign in new[]{-1f,1f})
    {
     var a=assets["StairRail"];var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,scene);go.transform.SetParent(Group("04 - Barandas inclinadas"),true);
     go.transform.SetPositionAndRotation(new Vector3(150+sign*3.55f,highY+1.5f-a.Bounds.max.y,highZ),a.Prefab.transform.localRotation);go.transform.localScale=a.Prefab.transform.localScale;DisableImportedBehaviour(go);go.isStatic=true;
     var b=TransformBounds(a.Bounds,Matrix4x4.Translate(go.transform.position));plan.Pieces.Add(new Piece{Key="StairRail",Group="04 - Barandas inclinadas",Object=go,Bounds=b,Scale=go.transform.localScale});
    }
    Walk(new Vector3(150,highY,highZ),new Vector3(150,lowY,lowZ),true);
    if(i<4)
    {
     Floor("03 - Descansos intermedios",150,lowZ+3,lowY);Floor("06 - Cimentacion de apoyos",147,lowZ+3,-16);Floor("06 - Cimentacion de apoyos",153,lowZ+3,-16);
     foreach(float x in new[]{146.45f,153.55f})Rail("04 - Pretiles de descansos",x,lowZ+3,lowY);
     Walk(new Vector3(150,lowY,lowZ),new Vector3(150,lowY,lowZ+6));
    }
   }
   // A first 24x24 lower sector, not the complete basement or its perimeter.
   foreach(float x in new[]{147f,153f,159f,165f})foreach(float z in new[]{93f,99f,105f,111f})Floor("05 - Primer piso inferior Y-16",x,z,-16);
   foreach(float z in new[]{93f,99f,105f,111f}){Rail("04 - Protecciones del piso inferior",144.53f,z,-16);Rail("04 - Protecciones del piso inferior",167.47f,z,-16);}
   foreach(float x in new[]{147f,153f,159f,165f})Rail("04 - Protecciones del piso inferior",x,113.47f,-16,90);
   Rail("04 - Protecciones del piso inferior",145.5f,90.53f,-16,90,3);
   foreach(float x in new[]{156f,162f,166.5f})Rail("04 - Protecciones del piso inferior",x,90.53f,-16,90,x==166.5f?3:6);
   // Separate load-bearing columns below landing edges, outside the 6m stair width.
   for(int i=0;i<5;i++)
   {
    float z=33+i*12,y=4-i*4;
    foreach(float x in new[]{146.1f,153.9f})for(float top=y-2;top>-16.01f;top-=4)P("07 - Pilas de apoyo de descansos","Column",x,z,top,0,true);
   }
   // Extend only three neighbouring approved roof capitals with additional, independent shafts.
   var roof=context?context.Find("Subsuelo_Techo_Etapa01"):source.Roof;
   foreach(var point in new[]{new Vector2(156,31),new Vector2(168,31),new Vector2(156,90)})
   {
    // Cloned scene objects may not retain prefab links; the native collider identifies the capital independently.
    var candidates=roof.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&Mathf.Abs(c.bounds.center.x-point.x)<.1f&&Mathf.Abs(c.bounds.center.z-point.y)<.1f&&c.bounds.size.y>8).ToArray();
    if(candidates.Length!=1)throw new InvalidOperationException("Capitel aprobado ambiguo en "+point);
    float bottom=candidates[0].bounds.min.y;
    for(float top=bottom;top>-16.01f;top-=4)P("08 - Encuentros con columnas del atrio","Column",point.x,point.y,top,0,true);
    if(point.y==90)Floor("06 - Cimentacion de apoyos",156,87,-16);
   }
   P("09 - Arco del descanso inferior","Arch",156,108,-16,90);
   Walk(new Vector3(150,-16,90),new Vector3(150,-16,99));Walk(new Vector3(150,-16,99),new Vector3(156,-16,99));Walk(new Vector3(156,-16,99),new Vector3(156,-16,111));
   Physics.SyncTransforms();return plan;
  }
  static void ValidatePlan(Plan plan,bool integrating=false)
  {
   Physics.SyncTransforms();var physics=plan.Root.gameObject.scene.GetPhysicsScene();var hits=new Collider[256];
   var original=(plan.Context?plan.Context.GetComponentsInChildren<Collider>():plan.Source.OriginalRoots.SelectMany(g=>g.GetComponentsInChildren<Collider>()).ToArray()).Where(c=>c.enabled&&!c.isTrigger).ToArray();
   var created=plan.Root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();var all=original.Concat(created).ToArray();plan.Samples=0;plan.MinClearance=float.PositiveInfinity;
   if(plan.Pieces.Count(p=>p.Key=="Stair")!=5||plan.Pieces.Count(p=>p.Group=="03 - Descansos intermedios")!=4||plan.Pieces.Count(p=>p.Group=="05 - Primer piso inferior Y-16")!=16)throw new InvalidOperationException("Composicion distinta del acceso previsto.");
   if(plan.Pieces.Select(p=>p.Key+"|"+p.Object.transform.position.ToString("F4")+"|"+p.Object.transform.rotation.ToString("F4")).Distinct().Count()!=plan.Pieces.Count)throw new InvalidOperationException("Prefabs duplicados.");
   foreach(var p in plan.Pieces)
   {
    var sourcePrefab=PrefabUtility.GetCorrespondingObjectFromSource(p.Object);
    if(!sourcePrefab||(p.Object.transform.localScale-p.Scale).sqrMagnitude>.0001f||(p.Key!="Rail"&&(p.Object.transform.localScale-sourcePrefab.transform.localScale).sqrMagnitude>.0001f))throw new InvalidOperationException("Prefab o escala inesperada.");
    if(!p.Object.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))throw new InvalidOperationException("Collider ausente: "+p.Key);
    foreach(var c in p.Object.GetComponentsInChildren<MeshCollider>())if(c.enabled&&!c.sharedMesh)throw new InvalidOperationException("MeshCollider sin malla.");
    foreach(var r in p.Object.GetComponentsInChildren<MeshRenderer>())if(r.enabled&&(r.sharedMaterials.Length==0||r.sharedMaterials.Any(m=>!m||!m.shader)))throw new InvalidOperationException("Material ausente.");
    if(p.Object.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new InvalidOperationException("Script ausente.");
    foreach(var c in p.Object.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))foreach(var old in original.Where(o=>o.bounds.Intersects(c.bounds)))
    {
     bool supported=c is BoxCollider||(c is MeshCollider m&&m.convex)||old is BoxCollider||(old is MeshCollider n&&n.convex);
     if(supported&&Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,old,old.transform.position,old.transform.rotation,out var direction,out float depth)&&depth>.035f)
     {
      bool foundation=p.Key=="Base"&&old.name.StartsWith("Column_Ceiling_02",StringComparison.Ordinal)&&c.bounds.min.y<old.bounds.min.y&&c.bounds.max.y>old.bounds.min.y;
      if(!foundation)throw new InvalidOperationException("Interferencia con original: "+p.Key+" "+c.bounds+" / "+old.name+" "+old.bounds+" profundidad "+depth);
     }
    }
   }
   float Ground(Vector3 p,bool stair)
   {
    float highest=float.NegativeInfinity;
    foreach(var c in all)
    {
     var b=c.bounds;if(p.x<b.min.x-.01f||p.x>b.max.x+.01f||p.z<b.min.z-.01f||p.z>b.max.z+.01f||b.max.y<p.y-.7f||b.min.y>p.y+.7f)continue;
     if(c.Raycast(new Ray(p+Vector3.up*.8f,Vector3.down),out var hit,1.6f)&&hit.normal.y>.5f)highest=Mathf.Max(highest,hit.point.y);
    }
    bool seam=plan.Pieces.Any(piece=>piece.Key=="Stair"&&p.x>=piece.Bounds.min.x&&p.x<=piece.Bounds.max.x&&p.z>=piece.Bounds.min.z-.25f&&p.z<=piece.Bounds.max.z+.25f);
    if(float.IsNegativeInfinity(highest)||Mathf.Abs(highest-p.y)>(stair?.65f:seam?.3f:.05f))throw new InvalidOperationException("Hueco o altura incorrecta: "+p+" suelo="+highest);
    return highest;
   }
   foreach(var path in plan.Walks)
   {
    var d=path.B-path.A;var flat=d;flat.y=0;float slope=Mathf.Atan2(Mathf.Abs(d.y),flat.magnitude)*Mathf.Rad2Deg;
    if(path.Stair&&Mathf.Abs(slope-33.69007f)>.05f)throw new InvalidOperationException("Pendiente incorrecta.");
    var side=Vector3.Cross(Vector3.up,flat.normalized);int steps=Mathf.Max(1,Mathf.CeilToInt(flat.magnitude/.2f));var previous=new float[4];
    for(int i=0;i<=steps;i++)for(int lane=0;lane<4;lane++)
    {
     var p=Vector3.Lerp(path.A,path.B,i/(float)steps)+side*((lane-1.5f)*.8f);float y=Ground(p,path.Stair);
     if(i>0&&Mathf.Abs(y-previous[lane])>.55f)throw new InvalidOperationException("Escalon no transitable en "+p);previous[lane]=y;
     var feet=new Vector3(p.x,y,p.z);int count=physics.OverlapCapsule(feet+Vector3.up*.75f,feet+Vector3.up*1.7f,.3f,hits,~0,QueryTriggerInteraction.Ignore);
     if(count==hits.Length)throw new InvalidOperationException("Consulta de colisiones saturada.");
     if(count>0)throw new InvalidOperationException("Paso bloqueado: "+feet+" / "+hits[0].name);
     float ceiling=float.PositiveInfinity;
     foreach(var c in all){var b=c.bounds;if(p.x<b.min.x||p.x>b.max.x||p.z<b.min.z||p.z>b.max.z||b.max.y<y+2.05f)continue;
      if(c.Raycast(new Ray(feet+Vector3.up*2.05f,Vector3.up),out var hit,80))ceiling=Mathf.Min(ceiling,hit.point.y-y);}
     if(!float.IsPositiveInfinity(ceiling))plan.MinClearance=Mathf.Min(plan.MinClearance,ceiling);
     plan.Samples++;
    }
   }
   foreach(var p in plan.Pieces.Where(p=>p.Group=="05 - Primer piso inferior Y-16"))for(float x=p.Bounds.min.x+.25f;x<p.Bounds.max.x;x+=.5f)for(float z=p.Bounds.min.z+.25f;z<p.Bounds.max.z;z+=.5f)Ground(new Vector3(x,-16,z),false);
   foreach(var column in plan.Pieces.Where(p=>p.Key=="Column"))
    if(!plan.Pieces.Any(p=>p!=column&&(p.Key=="Column"||p.Key=="Base")&&p.Bounds.max.y>=column.Bounds.min.y-.025f&&p.Bounds.min.y<=column.Bounds.min.y+.025f&&p.Bounds.min.x<=column.Bounds.center.x&&p.Bounds.max.x>=column.Bounds.center.x&&p.Bounds.min.z<=column.Bounds.center.z&&p.Bounds.max.z>=column.Bounds.center.z))throw new InvalidOperationException("Apoyo sin cimentacion: "+column.Bounds);
   if(plan.MinClearance<2.1f)throw new InvalidOperationException("Altura libre insuficiente.");
   if(plan.Root.GetComponentsInChildren<Light>().Any()||plan.Root.GetComponentsInChildren<ParticleSystem>().Any()||plan.Root.GetComponentsInChildren<UnityEngine.Rendering.Volume>().Any())throw new InvalidOperationException("Efectos no permitidos.");
   plan.Source.Verify(integrating);Debug.Log("SUBSUELO_ACCESO_VALIDACION_OK: "+plan.Pieces.Count+" prefabs; enlace Y4 X144..174 Z30..36; escalera 6m de ancho, cinco tramos 6x4m, cuatro descansos 6x6m, 33.69 grados; descenso 20m hasta Y-16; piso inicial 24x24m X144..168 Z90..114. "+plan.Samples+" muestras de cuatro carriles; altura libre minima medida "+plan.MinClearance.ToString("F2")+"m. Colliders, continuidad, apoyos y duplicados comprobados. Techo574, ocho huecos, Umbral639 y mapa original intactos. Solo previsualizacion, sin guardado.",plan.Root);
  }

  const string IntegratedRoot="Subsuelo_Acceso_Etapa01",Signature="__ROM_AccesoSubsuelo_129_Approved_v1";
  [MenuItem(Menu+"7. Integrar acceso aprobado y guardar tras validar %#&d")]
  public static void IntegrateApproved()
  {
   if(!Ready)return;
   Source source=null;int undo=-1;bool saved=false;
   try
   {
    var preview=StageUtility.GetCurrentStage() as AccessPreview;
    string approved=preview&&preview.Plan!=null?Layout(preview.Plan.Root):null;
    Close();source=Inspect();
    if(source.Dirty)throw new InvalidOperationException("La original tiene cambios previos sin guardar; no se guardaran con esta etapa.");
    var existing=source.OriginalRoots.Where(g=>g.name==IntegratedRoot).ToArray();
    if(existing.Length>0)
    {
     if(existing.Length!=1||!existing[0].transform.Find(Signature)||existing[0].GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=129)
      throw new InvalidOperationException("Acceso existente ambiguo o distinto de la version aprobada; no se sobrescribira.");
     source.Verify();Debug.Log("SUBSUELO_ACCESO_YA_INTEGRADO: 129 prefabs, sin duplicacion ni cambios.",existing[0]);return;
    }
    if(approved==null)throw new InvalidOperationException("Abri la copia temporal aprobada antes de integrar para comparar su distribucion.");
    if(source.OriginalRoots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name==Root||t.name=="Contexto original - solo copias"||t.name.StartsWith("TEMP_CiudadDelSol",StringComparison.Ordinal)))
     throw new InvalidOperationException("Objetos temporales inesperados en la original.");
    string backup="Temp/ROM_Backups/Dungeon_Isla_del_Sol_before_AccesoSubsuelo_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));System.IO.File.Copy(Target,backup,false);
    Undo.IncrementCurrentGroup();undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Integrar acceso al subsuelo aprobado");
    var plan=Build(source,null,source.Scene,LoadAssets(),true);
    if(plan.Pieces.Count!=129||plan.Root.GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=129)
     throw new InvalidOperationException("La version aprobada requiere exactamente 129 prefabs.");
    string generated=Layout(plan.Root);
    if(generated!=approved)throw new InvalidOperationException("La distribucion no coincide con la copia aprobada: "+approved+" / "+generated);
    ValidatePlan(plan,true);
    foreach(var c in plan.Root.GetComponentsInChildren<Component>(true))if(c&&PrefabUtility.IsPartOfPrefabInstance(c)&&PrefabUtility.GetCorrespondingObjectFromSource(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    if(Layout(plan.Root)!=generated)throw new InvalidOperationException("Los overrides alteraron la distribucion.");
    plan.Root.name=IntegratedRoot;
    var signature=new GameObject(Signature);signature.transform.SetParent(plan.Root,false);signature.hideFlags=HideFlags.HideInHierarchy;
    if(!source.OriginalRoots.Select(g=>g.GetInstanceID()).SequenceEqual(source.Scene.GetRootGameObjects().Where(g=>g!=plan.Root.gameObject).Select(g=>g.GetInstanceID())))throw new InvalidOperationException("Raices originales alteradas.");
    source.Verify(true);
    if(source.Scene.path!=Target||StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())throw new InvalidOperationException("Contexto de guardado incorrecto.");
    EditorSceneManager.MarkSceneDirty(source.Scene);
    if(!EditorSceneManager.SaveScene(source.Scene,Target,false))throw new InvalidOperationException("Fallo al guardar la original.");
    saved=true;Undo.CollapseUndoOperations(undo);
    if(source.Scene.isDirty||source.Scene.GetRootGameObjects().Count(g=>g.name==IntegratedRoot)!=1||Layout(plan.Root)!=generated)throw new InvalidOperationException("Fallo la comprobacion posterior al guardado.");
    Debug.Log("SUBSUELO_ACCESO_INTEGRADO_GUARDADO: 129 prefabs; cinco tramos y cuatro descansos de Y4 a Y-16; piso24x24, arco y soportes aprobados. Distribucion="+generated+"; "+plan.Samples+" muestras, altura libre minima "+plan.MinClearance.ToString("F2")+"m. Techo574, ocho huecos, Umbral639, siete zonas, nueve caminos y Salida Meridional intactos. Backup: "+backup+". Guardado en "+Target,plan.Root);Selection.activeGameObject=plan.Root.gameObject;
   }
   catch(Exception e)
   {
    if(!saved&&undo>=0)Undo.RevertAllDownToGroup(undo);
    if(!saved&&source!=null){try{source.Verify(true);}catch(Exception preservation){Debug.LogException(preservation);}}
    Debug.LogError("SUBSUELO_ACCESO_INTEGRACION_DETENIDA: "+e.Message);Debug.LogException(e);
   }
  }
  static string Layout(Transform root)
  {
   var rows=new List<string>();var culture=System.Globalization.CultureInfo.InvariantCulture;
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
     if(c is MeshCollider mesh)row+="|"+(mesh.sharedMesh?mesh.sharedMesh.name:"null")+"|"+mesh.convex;
    }
    rows.Add(row);
   }
   rows.Sort(StringComparer.Ordinal);return Hash128.Compute(string.Join("\n",rows)).ToString();
  }

  sealed class AccessPreview:PreviewSceneStage
  {
   public Source Source;public Dictionary<string,Asset> Assets;public Plan Plan;
   protected override GUIContent CreateHeaderContent()=>new GUIContent("Acceso al subsuelo - Etapa 1 - SOLO COPIA TEMPORAL");
   protected override bool OnOpenStage()
   {
    if(Source==null||Assets==null||!base.OnOpenStage())return false;
    try
    {
     Scene temporary=scene;temporary.name="TEMP_CiudadDelSol_Acceso_Subsuelo_Etapa1";
     var context=new GameObject("Contexto original - solo copias");SceneManager.MoveGameObjectToScene(context,scene);context.SetActive(false);
     foreach(var original in Source.OriginalRoots.Where(g=>g.name==Dungeon||g.name=="Umbral_De_Las_Profundidades"||g.name=="Salida_Meridional_Generated"||g.name=="Subsuelo_Techo_Etapa01"))
     {var copy=Object.Instantiate(original,context.transform);copy.name=original.name;copy.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);copy.transform.localScale=original.transform.lossyScale;}
     DisableImportedBehaviour(context);context.SetActive(true);Plan=Build(Source,context.transform,scene,Assets);ValidatePlan(Plan);
     EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this){Above();Selection.activeGameObject=null;}};
     Debug.Log("SUBSUELO_ACCESO_PREVIEW_LISTA: menu vistas 2 arriba, 3 cripta, 4 nivel inferior. La integracion requiere el comando explicito 7.");
    }
    catch(Exception e){Debug.LogException(e);}return true;
   }
   protected override void OnCloseStage(){try{if(Source!=null)Source.Verify();}finally{base.OnCloseStage();}}
  }
 }
}
