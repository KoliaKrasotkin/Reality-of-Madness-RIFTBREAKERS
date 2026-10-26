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
 public static class CiudadDelSolSubsueloRoofGenerator
 {
  const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity",Dungeon="Dungeon_Ciudad_del_Sol_Generated",Route="06-07 -";
  const string Root="Subsuelo_Techo_Etapa01_Preview",Menu="ROM/Dungeon Ciudad del Sol/Techo del subsuelo/";
  const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/";
  static readonly Dictionary<string,string> Paths=new Dictionary<string,string>{
   {"Base",ET+"Pits and Platforms/Platform_Base_01.prefab"},
   {"Vault",ET+"Bases/Ceiling/Base_Ceiling_01.prefab"},
   {"Vault3",ET+"Bases/Ceiling/Base_Ceiling_3m_01.prefab"},
   {"Beam",ET+"Bases/Ceiling/Ceiling_Beam_01.prefab"},
   {"Beam2",ET+"Bases/Ceiling/Ceiling_Beam_02.prefab"},
   {"Capital",ET+"Bases/Ceiling/Column_Ceiling_01.prefab"},
   {"Capital2",ET+"Bases/Ceiling/Column_Ceiling_02.prefab"},
   {"Edge",ET+"Bases/Ceiling/Ceiling_Edge_01.prefab"},
   {"Arch",ET+"Arch Alley/Arch_Alley_Rounded_01.prefab"},
   {"Column",ET+"Columns/Column_Stackable_01.prefab"},
   {"Trim",ET+"Railings and Decors/Bottom_Decor_01.prefab"}
  };
  sealed class Asset{public GameObject Prefab;public Bounds Bounds;}
  sealed class Source{public Scene Scene;public Transform Root,Route,Umbral;public string FileHash;public bool Dirty;public GameObject[] OriginalRoots;
   public readonly List<Action> Checks=new List<Action>();
   public void Verify(bool integrating=false){foreach(var c in Checks)c();if((!integrating&&Scene.isDirty!=Dirty)||HashScene()!=FileHash)throw new InvalidOperationException("La escena original fue alterada.");}
  }
  static bool Ready=>!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode;
  static string HashScene(){using(var s=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(s.ComputeHash(System.IO.File.ReadAllBytes(Target)));}
  [MenuItem(Menu+"0. Inspeccionar espacio y prefabs")]
  public static void InspectOnly(){if(!Ready)return;try{var s=Inspect();var a=LoadAssets();Report(s,a);s.Verify();}catch(Exception e){Debug.LogException(e);}}
  static void Report(Source source,Dictionary<string,Asset> assets)
  {
   Physics.SyncTransforms();
   foreach(var a in assets)Debug.Log("TECHO_ASSET "+a.Key+" bounds="+a.Value.Bounds);
   foreach(Transform z in source.Root)if(z.name.Length>3&&char.IsDigit(z.name[0])&&z.name[2]==' ')
   {var rs=z.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);Debug.Log("TECHO_ZONA "+z.name+" "+b);}
   var region=new Bounds(new Vector3(174,12,60),new Vector3(72,10,60));
   foreach(var c in source.Umbral.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&c.bounds.Intersects(region)))Debug.Log("TECHO_EXISTENTE "+c.name+" "+c.bounds);
   Debug.Log("TECHO_INSPECCION: Umbral centro (162,20,42), intrados principal Y16; cripta Y4, cubierta min13.38 max15.38; boca futura X156..168 Z30. La extension debe conservar sus piers y mantener altura libre para el futuro subsuelo.");
  }
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
            source.Umbral=u[0].transform;return source;
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
  sealed class Plan{public Source Source;public Transform Root,Context;public readonly List<Piece> Pieces=new List<Piece>();public readonly HashSet<Vector2> Tiles=new HashSet<Vector2>();public int Slots;}
  [MenuItem(Menu+"1. Previsualizar techo (copia temporal) %#&t")]
  public static void Preview()
  {
   if(!Ready)return;try{Close();var stage=ScriptableObject.CreateInstance<RoofPreview>();stage.Source=Inspect();stage.Assets=LoadAssets();StageUtility.GoToStage(stage,true);}catch(Exception e){Debug.LogException(e);}
  }
  [MenuItem(Menu+"2. Vista del techo desde abajo %#&y")]
  public static void UnderView()=>SetView(new Vector3(174,11,62),Quaternion.Euler(-12,0,0),12);
  [MenuItem(Menu+"3. Vista de conexion con la cripta %#&h")]
  public static void CryptView()=>SetView(new Vector3(162,7.5f,30),Quaternion.Euler(-3,0,0),9);
  [MenuItem(Menu+"4. Vista de la extension completa %#&g")]
  public static void WholeView()=>SetView(new Vector3(174,10,60),Quaternion.Euler(-65,-30,0),46,true);
  static void SetView(Vector3 pivot,Quaternion rotation,float size,bool isolate=false)
  {
   var stage=StageUtility.GetCurrentStage() as RoofPreview;if(!stage)return;
   if(stage.Plan!=null&&stage.Plan.Context){if(isolate)SceneVisibilityManager.instance.Hide(stage.Plan.Context.gameObject,true);else SceneVisibilityManager.instance.Show(stage.Plan.Context.gameObject,true);}
   var view=SceneView.lastActiveSceneView;
   if(view){view.sceneLighting=false;view.sceneViewState.showImageEffects=false;view.orthographic=false;view.LookAtDirect(pivot,rotation,size);view.Repaint();}
  }
  [MenuItem(Menu+"5. Validar techo y acceso existente")]
  public static void Validate(){try{var stage=StageUtility.GetCurrentStage() as RoofPreview;if(!stage||stage.Plan==null)throw new InvalidOperationException("Abri primero la copia temporal.");ValidatePlan(stage.Plan);}catch(Exception e){Debug.LogException(e);}}
  [MenuItem(Menu+"6. Cerrar previsualizacion sin guardar")]
  public static void Close(){if(Ready&&StageUtility.GetCurrentStage() is RoofPreview)StageUtility.GoToMainStage();}

  static IEnumerable<T> Existing<T>(Source source,Transform context) where T:Component
   =>context?context.GetComponentsInChildren<T>():source.OriginalRoots.SelectMany(g=>g.GetComponentsInChildren<T>());
  const string IntegratedRoot="Subsuelo_Techo_Etapa01",Signature="__ROM_TechoSubsuelo_574_Huecos8_Approved_v1";
  [MenuItem(Menu+"7. Integrar techo aprobado y guardar tras validar")]
  public static void IntegrateApproved()
  {
   if(!Ready)return;
   int undo=-1;bool saved=false;Source source=null;
   try
   {
    var preview=StageUtility.GetCurrentStage() as RoofPreview;
    string approved=preview&&preview.PreviewRoot?Layout(preview.PreviewRoot):null;
    Close();source=Inspect();
    if(source.Dirty)throw new InvalidOperationException("La original tiene cambios previos sin guardar; no se guardaran con esta etapa.");
    var existing=source.OriginalRoots.Where(g=>g.name==IntegratedRoot).ToArray();
    if(existing.Length>0)
    {
     if(existing.Length!=1||!existing[0].transform.Find(Signature)||existing[0].GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=574)
      throw new InvalidOperationException("Techo existente ambiguo o distinto de la version aprobada. No se sobrescribira.");
     Debug.Log("TECHO_YA_INTEGRADO: una unica estructura de 574 prefabs; no se duplico ni modifico.",existing[0]);return;
    }
    if(source.OriginalRoots.Count(g=>g.name=="Salida_Meridional_Generated")!=1)throw new InvalidOperationException("Salida Meridional ausente o duplicada.");
    if(source.OriginalRoots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name==Root||t.name=="Contexto original - solo copias"||t.name.StartsWith("TEMP_CiudadDelSol",StringComparison.Ordinal)))
     throw new InvalidOperationException("Hay objetos temporales inesperados en la original.");
    string backup="Temp/ROM_Backups/Dungeon_Isla_del_Sol_before_TechoSubsuelo_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));System.IO.File.Copy(Target,backup,false);
    var assets=LoadAssets();
    Undo.IncrementCurrentGroup();undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Integrar techo del subsuelo aprobado");
    var plan=Build(source,null,source.Scene,assets,true);
    var counts=plan.Pieces.GroupBy(p=>p.Key).ToDictionary(g=>g.Key,g=>g.Count());
    var expected=new Dictionary<string,int>{{"Vault3",92},{"Vault",4},{"Beam",282},{"Capital2",36},{"Beam2",120},{"Trim",40}};
    if(plan.Pieces.Count!=574||plan.Slots!=8||plan.Tiles.Count!=92||counts.Count!=expected.Count||expected.Any(p=>!counts.TryGetValue(p.Key,out int n)||n!=p.Value))
     throw new InvalidOperationException("La estructura no coincide con los 574 prefabs y ocho huecos aprobados.");
    string generated=Layout(plan.Root);
    if(approved!=null&&generated!=approved)throw new InvalidOperationException("Diferencia respecto de la copia aprobada: preview="+approved+" generado="+generated);
    if(plan.Pieces.Select(p=>p.Key+"|"+p.Object.transform.position.ToString("F4")+"|"+p.Object.transform.rotation.ToString("F4")).Distinct().Count()!=574)
     throw new InvalidOperationException("Prefabs duplicados.");
    ValidatePlan(plan,true);
    foreach(var c in plan.Root.GetComponentsInChildren<Component>(true))
     if(c&&PrefabUtility.IsPartOfPrefabInstance(c)&&PrefabUtility.GetCorrespondingObjectFromSource(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    if(Layout(plan.Root)!=generated)throw new InvalidOperationException("Registrar overrides cambio la distribucion aprobada.");
    plan.Root.name=IntegratedRoot;var sign=new GameObject(Signature);sign.transform.SetParent(plan.Root,false);sign.hideFlags=HideFlags.HideInHierarchy;
    var before=source.OriginalRoots.Select(g=>g.GetInstanceID());
    if(!before.SequenceEqual(source.Scene.GetRootGameObjects().Where(g=>g!=plan.Root.gameObject).Select(g=>g.GetInstanceID())))throw new InvalidOperationException("Raices originales alteradas.");
    source.Verify(true);
    if(source.Scene.path!=Target||StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())throw new InvalidOperationException("Contexto de guardado incorrecto.");
    EditorSceneManager.MarkSceneDirty(source.Scene);
    if(!EditorSceneManager.SaveScene(source.Scene,Target,false))throw new InvalidOperationException("Fallo el guardado de la original.");
    saved=true;Undo.CollapseUndoOperations(undo);
    if(source.Scene.isDirty||source.Scene.GetRootGameObjects().Count(g=>g.name==IntegratedRoot)!=1)throw new InvalidOperationException("El guardado no supero la comprobacion final.");
    Debug.Log("TECHO_INTEGRADO_GUARDADO: 574 prefabs; ocho huecos cuadrados conservados; cubierta 60x60m y enlace 24x6m a Y15.38; distribucion="+generated+". Conexiones, colisiones, apoyos y duplicados validados. Umbral de 639 prefabs, siete zonas, nueve caminos y Salida Meridional intactos. Sin VFX ni gameplay. Guardado en "+Target+". Backup: "+backup,plan.Root);
    Selection.activeGameObject=plan.Root.gameObject;
   }
   catch(Exception e)
   {
    if(!saved&&undo>=0)Undo.RevertAllDownToGroup(undo);
    if(!saved&&source!=null){try{source.Verify(true);}catch(Exception preservation){Debug.LogException(preservation);}}
    Debug.LogError("TECHO_INTEGRACION_DETENIDA: "+e.Message);Debug.LogException(e);
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

  static Plan Build(Source source,Transform context,Scene scene,Dictionary<string,Asset> assets,bool undo=false)
  {
   const float top=15.38f;float underside=top-assets["Vault3"].Bounds.size.y;
   float beamBottom=underside-assets["Beam"].Bounds.size.y;
   var go=new GameObject(Root);SceneManager.MoveGameObjectToScene(go,scene);
   if(undo)Undo.RegisterCreatedObjectUndo(go,"Integrar techo del subsuelo aprobado");
   var plan=new Plan{Source=source,Context=context,Root=go.transform};var groups=new Dictionary<string,Transform>();var unique=new HashSet<string>();
   Transform Group(string name){if(!groups.TryGetValue(name,out var t)){var g=new GameObject(name);g.transform.SetParent(go.transform,false);groups[name]=t=g.transform;}return t;}
   void P(string group,string key,float x,float z,float y,float yaw=0)
   {
    var asset=assets[key];var q=Quaternion.Euler(0,yaw,0);var b=TransformBounds(asset.Bounds,Matrix4x4.Rotate(q));
    var position=new Vector3(x-b.center.x,y-b.max.y,z-b.center.z);b.center+=position;
    string id=key+"|"+position.ToString("F4")+"|"+yaw;
    if(!unique.Add(id))throw new InvalidOperationException("Pieza duplicada: "+id);
    var p=(GameObject)PrefabUtility.InstantiatePrefab(asset.Prefab,scene);p.transform.SetParent(Group(group),true);
    p.transform.SetPositionAndRotation(position,q*asset.Prefab.transform.localRotation);p.transform.localScale=asset.Prefab.transform.localScale;
    DisableImportedBehaviour(p);p.isStatic=true;
    if(key=="Vault3"||key=="Beam2")
    {
     foreach(var c in p.GetComponentsInChildren<Collider>())c.enabled=false;
     foreach(var f in p.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled)f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
    }
    if(!p.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))
     foreach(var f in p.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled)f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
    plan.Pieces.Add(new Piece{Key=key,Group=group,Object=p,Bounds=b});
   }
   var originals=Existing<Collider>(source,context).Where(c=>c.enabled&&!c.isTrigger).ToArray();
   // Respect the existing pier shafts with deliberate 6m structural sockets, not random gaps.
   for(int ix=0;ix<10;ix++)for(int iz=0;iz<10;iz++)
   {
    float x=147+ix*6,z=33+iz*6;
    var test=new Bounds(new Vector3(x,top-assets["Vault3"].Bounds.size.y/2,z),new Vector3(5.98f,assets["Vault3"].Bounds.size.y-.02f,5.98f));
    var hit=originals.Where(c=>c.bounds.Intersects(test)).ToArray();
    if(hit.Length>0)
    {
     if(hit.Any(c=>!c.name.StartsWith("SM_GranitePillar",StringComparison.Ordinal)))throw new InvalidOperationException("Obstaculo imprevisto en el techo: "+hit[0].name+" "+test);
     plan.Slots++;continue;
    }
    P("01 - Bovedas modulares continuas","Vault3",x,z,top);plan.Tiles.Add(new Vector2(x,z));
   }
   foreach(float x in new[]{153f,159f,165f,171f})P("02 - Enlace con la cubierta de la cripta","Vault",x,27,top);
   foreach(float x in new[]{150f,174f})P("02 - Molduras del enlace","Beam",x,27,top-assets["Vault"].Bounds.size.y);
   // A connected 6m beam lattice also frames every preserved pier socket.
   for(int ix=0;ix<=10;ix++)for(int iz=0;iz<10;iz++)P("03 - Reticula longitudinal de piedra","Beam",144+ix*6,33+iz*6,underside);
   for(int iz=0;iz<=10;iz++)for(int ix=0;ix<10;ix++)P("04 - Reticula transversal de piedra","Beam",147+ix*6,30+iz*6,underside,90);
   for(int ix=0;ix<=5;ix++)for(int iz=0;iz<=5;iz++)P("05 - Columnas con capitel monumental","Capital2",144+ix*12,iz==0&&(144+ix*12==156||144+ix*12==168)?31:30+iz*12,beamBottom);
   float half=assets["Beam2"].Bounds.size.x/2;
   for(int iz=0;iz<=5;iz++)for(int ix=0;ix<10;ix++)
   {
    float x=147+ix*6,z=30+iz*12;
    P("06 - Nervaduras y arcos de soporte","Beam2",x-half,z,beamBottom);
    P("06 - Nervaduras y arcos de soporte","Beam2",x+half,z,beamBottom,180);
    P("06 - Claves y vigas sobre los arcos","Beam",x,z,beamBottom,90);
   }
   for(int i=0;i<10;i++)
   {
    P("07 - Cornisas de piedra envejecida","Trim",143.75f,33+i*6,top);P("07 - Cornisas de piedra envejecida","Trim",204.25f,33+i*6,top);
    P("07 - Cornisas de piedra envejecida","Trim",147+i*6,29.75f,top,90);P("07 - Cornisas de piedra envejecida","Trim",147+i*6,90.25f,top,90);
   }
   Physics.SyncTransforms();return plan;
  }
  static void ValidatePlan(Plan plan,bool integrating=false)
  {
   Physics.SyncTransforms();var original=Existing<Collider>(plan.Source,plan.Context).Where(c=>c.enabled&&!c.isTrigger).ToArray();
   var created=plan.Root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();var physics=plan.Root.gameObject.scene.GetPhysicsScene();var hits=new Collider[128];
   foreach(var p in plan.Pieces)
   {
    var prefab=PrefabUtility.GetCorrespondingObjectFromSource(p.Object);
    if(!prefab||(p.Object.transform.localScale-prefab.transform.localScale).sqrMagnitude>.0001f)throw new InvalidOperationException("Prefab o escala alterada: "+p.Key);
    if(!p.Object.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))throw new InvalidOperationException("Collider ausente: "+p.Key);
    foreach(var t in p.Object.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Script faltante: "+t.name);
    foreach(var r in p.Object.GetComponentsInChildren<Renderer>())if(r.sharedMaterials.Length==0||r.sharedMaterials.Any(m=>!m||!m.shader))throw new InvalidOperationException("Material faltante: "+r.name);
   }
   foreach(var c in created)foreach(var old in original)
   {
    if(!c.bounds.Intersects(old.bounds))continue;
    // Every new beam/column has a native primitive collider; curved meshes are checked against the originals' primitives or convex colliders.
    bool supported=c is BoxCollider||(c is MeshCollider mesh&&mesh.convex)||old is BoxCollider||(old is MeshCollider other&&other.convex);
    if(!supported)
    {
     var a=c.bounds;var b=old.bounds;
     float overlap=Mathf.Min(Mathf.Min(a.max.x,b.max.x)-Mathf.Max(a.min.x,b.min.x),Mathf.Min(Mathf.Min(a.max.y,b.max.y)-Mathf.Max(a.min.y,b.min.y),Mathf.Min(a.max.z,b.max.z)-Mathf.Max(a.min.z,b.min.z)));
     if(overlap>.035f)throw new InvalidOperationException("Cruce de mallas estaticas que requiere separacion: "+c.name+" / "+old.name);
    }
    if(supported&&Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,old,old.transform.position,old.transform.rotation,out var direction,out float depth)&&depth>.035f)
     throw new InvalidOperationException("Interseccion con estructura aprobada: "+c.name+" / "+old.name+" profundidad "+depth+" en "+c.bounds.center);
   }
   // Preserve the real crypt and its walkable floor. No new floor or corridor is created.
   void Clear(Vector3 a,Vector3 b,bool floor)
   {
    var d=b-a;d.y=0;var side=Vector3.Cross(Vector3.up,d.normalized);int n=Mathf.CeilToInt(d.magnitude/.3f);
    for(int lane=0;lane<4;lane++)for(int i=0;i<=n;i++)
    {
     var p=Vector3.Lerp(a,b,i/(float)n)+side*(lane-1.5f);
     if(floor&&!original.Any(c=>c.Raycast(new Ray(p+Vector3.up,Vector3.down),out var hit,1.1f)&&hit.normal.y>.5f&&Mathf.Abs(hit.point.y-p.y)<.05f))throw new InvalidOperationException("Se perdio el suelo existente en "+p);
     int count=physics.OverlapCapsule(p+Vector3.up*.75f,p+Vector3.up*1.7f,.3f,hits,~0,QueryTriggerInteraction.Ignore);
     if(count>0)throw new InvalidOperationException("Acceso o reserva futura bloqueada: "+p+" / "+hits[0].name);
    }
   }
   Clear(new Vector3(162,4,6.2f),new Vector3(162,4,29.5f),true);
   Clear(new Vector3(162,4,29.5f),new Vector3(165,4,36),false);Clear(new Vector3(165,4,36),new Vector3(165,4,88),false);
   var oldRoofs=Existing<MeshRenderer>(plan.Source,plan.Context).Where(r=>r.enabled&&Mathf.Abs(r.bounds.max.y-15.38f)<.02f&&Mathf.Abs(r.bounds.max.z-24)<.02f).ToArray();
   foreach(var link in plan.Pieces.Where(p=>p.Key=="Vault"))
   {
    if(!oldRoofs.Any(r=>Mathf.Abs(r.bounds.max.z-link.Bounds.min.z)<.02f&&Mathf.Abs(r.bounds.center.x-link.Bounds.center.x)<.02f&&r.bounds.max.y>link.Bounds.min.y))throw new InvalidOperationException("Enlace sin contacto con la cubierta existente de la cripta.");
    if(!plan.Pieces.Any(p=>p.Key=="Vault3"&&Mathf.Abs(p.Bounds.min.z-link.Bounds.max.z)<.02f&&Mathf.Abs(p.Bounds.center.x-link.Bounds.center.x)<.02f))throw new InvalidOperationException("Enlace separado de la cubierta principal.");
   }
   // Every new roof tile must belong to one continuous edge-connected masonry network.
   var pending=new HashSet<Vector2>(plan.Tiles);var queue=new Queue<Vector2>();queue.Enqueue(pending.First());pending.Remove(queue.Peek());
   while(queue.Count>0){var c=queue.Dequeue();foreach(var d in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down}){var next=c+d*6;if(pending.Remove(next))queue.Enqueue(next);}}
   if(pending.Count>0)throw new InvalidOperationException("Hay sectores de cubierta desconectados.");
   foreach(var capital in plan.Pieces.Where(p=>p.Key=="Capital2"))if(!plan.Pieces.Any(p=>p.Key=="Beam"&&Mathf.Abs(p.Bounds.min.y-capital.Bounds.max.y)<.025f&&p.Bounds.min.x<capital.Bounds.max.x-.02f&&p.Bounds.max.x>capital.Bounds.min.x+.02f&&p.Bounds.min.z<capital.Bounds.max.z-.02f&&p.Bounds.max.z>capital.Bounds.min.z+.02f))throw new InvalidOperationException("Capitel sin contacto estructural: "+capital.Bounds);
   if(plan.Root.GetComponentsInChildren<Light>().Any(l=>l.enabled)||plan.Root.GetComponentsInChildren<ParticleSystem>().Any(p=>p.isPlaying))throw new InvalidOperationException("No se permiten luces ni VFX en esta etapa.");
   plan.Source.Verify(integrating);Debug.Log("TECHO_VALIDACION_OK: "+plan.Pieces.Count+" prefabs; cubierta continua 60x60m X144..204 Z30..90 y enlace 24x6m Z24..30. Coronacion Y15.38; intrados de panel Y12.14, nervaduras minY9.41 (altura minima 5.41 sobre futuro Y4); columnas nativas 8.68m. "+plan.Slots+" alojamientos para pilares originales; cripta, 639 prefabs del Umbral y mapa original intactos. Solo techo y soportes: sin suelos nuevos, paredes, corredores ni chambers. Reservas de volumen, no prueba de NavMesh.",plan.Root);
  }
  sealed class RoofPreview:PreviewSceneStage
  {
   public Source Source;public Dictionary<string,Asset> Assets;public Plan Plan;
   public Transform PreviewRoot=>scene.GetRootGameObjects().SingleOrDefault(g=>g.name==Root)?.transform;
   protected override GUIContent CreateHeaderContent()=>new GUIContent("Techo del subsuelo - copia temporal");
   protected override bool OnOpenStage()
   {
    if(Source==null||Assets==null||!base.OnOpenStage())return false;
    try
    {
     Scene temporary=scene;temporary.name="TEMP_CiudadDelSol_Techo_Subsuelo";
     var context=new GameObject("Contexto original - solo copias");SceneManager.MoveGameObjectToScene(context,scene);context.SetActive(false);
     foreach(var original in Source.Scene.GetRootGameObjects().Where(g=>g.name==Dungeon||g.name=="Umbral_De_Las_Profundidades"||g.name=="Salida_Meridional_Generated"))
     {var copy=Object.Instantiate(original,context.transform);copy.name=original.name;copy.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);copy.transform.localScale=original.transform.lossyScale;}
     DisableImportedBehaviour(context);context.SetActive(true);Plan=Build(Source,context.transform,scene,Assets);
     EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this){WholeView();Selection.activeGameObject=null;}};
     ValidatePlan(Plan);Debug.Log("TECHO_PREVIEW_LISTA: no se integro ni guardo la original. Vistas 2: intrados, 3: cripta, 4: extension completa.");return true;
    }
    catch(Exception e){Debug.LogException(e);return true;}
   }
   protected override void OnCloseStage(){try{if(Source!=null)Source.Verify();}finally{base.OnCloseStage();}}
  }

 }
}
