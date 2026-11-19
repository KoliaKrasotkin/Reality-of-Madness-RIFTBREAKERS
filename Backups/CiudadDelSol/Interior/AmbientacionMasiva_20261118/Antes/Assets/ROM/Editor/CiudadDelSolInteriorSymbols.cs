using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
 public static partial class CiudadDelSolInteriorStageTwo
 {
  const string SymbolAssets="Assets/ROM/Art/CiudadDelSol/Interior/Simbolos";
  const string SymbolWork="Backups/CiudadDelSol/Interior/Simbolos_20261118";
  static readonly string[] SymbolNames={"Rojo_Advertencia","Verde_Investigacion","Violeta_Corrupcion","Azul_Contencion"};
  [Serializable] sealed class SymbolReceipt
  {
   public int recolored,newSymbols,persistedSymbols;public bool collidersUnchanged,navUnchanged,saved,persisted,bidirectional,materialsEmissive,cleanAfterReload;
   public string sceneBefore,sceneAfter,nav,unity;
  }
  static Mesh SymbolMesh(string name,List<Vector2[]> paths)
  {
   var vertices=new List<Vector3>();var indices=new[]{new List<int>(),new List<int>()};
   for(int pass=0;pass<2;pass++)foreach(var line in paths)for(int i=1;i<line.Length;i++)
   {
    var d=(line[i]-line[i-1]).normalized;var side=new Vector2(-d.y,d.x)*(pass==0?.055f:.028f);int n=vertices.Count;float z=pass==0?0:-.004f;
    foreach(var p in new[]{line[i-1]+side,line[i-1]-side,line[i]-side,line[i]+side})vertices.Add(new Vector3(p.x,p.y,z));
    indices[pass].AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
   }
   var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.subMeshCount=2;for(int i=0;i<2;i++)mesh.SetTriangles(indices[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }
  static Material SymbolMaterial(string name,Color color,float emission)
  {
   var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)throw new InvalidOperationException("URP Lit no disponible.");
   var m=new Material(shader){name=name,globalIlluminationFlags=emission>0?MaterialGlobalIlluminationFlags.BakedEmissive:MaterialGlobalIlluminationFlags.None};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",0);
   m.SetColor("_EmissionColor",color*emission);if(emission>0)m.EnableKeyword("_EMISSION");m.SetFloat("_SpecularHighlights",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
   AssetDatabase.CreateAsset(m,SymbolAssets+"/"+name+".mat");return m;
  }
  static int ExistingSymbolColor(string name,bool a03)
  {
   if(a03)
   {
    if(name.StartsWith("01_Sol_")||name.StartsWith("02_Sol_")||name.StartsWith("03_Sol_")||name=="04_Nucleo_solar"||name.StartsWith("05_Rayo_")||name=="08_Glifo_antiguo_tres")return 3;
    if(name=="06_Glifo_antiguo_uno")return 1;if(name=="07_Glifo_antiguo_dos")return 0;
    if(name=="12_Filamento_violeta"||name=="13_Ramificacion_fractura")return 2;
   }
   else
   {
    if(name.StartsWith("01_Sello_")||name.StartsWith("02_Sello_")||name.StartsWith("03_Sello_")||name=="23_Limite_de_contencion")return 3;
    if(name=="05_Nucleo_ceremonial"||name=="25c_Residuo_violeta_del_muro"||name=="26_Fractura_proyectada_en_carcasa")return 2;
    if(name.StartsWith("06_Escala_radial_"))return int.Parse(name.Substring("06_Escala_radial_".Length))%4==0?0:1;
    if(name.StartsWith("21_Diagrama_")||name.StartsWith("22_Diagrama_")||name=="24_Eje_de_medicion"||name.StartsWith("25_Escala_mural_"))return 1;
   }
   return -1;
  }
  static void ApplyInteriorSymbols(Scene scene)
  {
   if(scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||Directory.Exists(SymbolAssets))throw new InvalidOperationException("Se necesita la escena guardada; la biblioteca no se sobrescribe ni duplica.");
   if(!File.Exists(SymbolWork+"/Antes_SHA256.json"))throw new InvalidOperationException("Falta respaldo.");
   var receipt=new SymbolReceipt{sceneBefore=Hash(City),nav=Hash(Nav),unity=Application.unityVersion};string physics=A03Physics(scene),digest=TriangleDigest4();
   var a03=scene.GetRootGameObjects().Single(g=>g.name==A03Root);var r21=scene.GetRootGameObjects().Single(g=>g.name==R21Root);
   Directory.CreateDirectory(SymbolAssets);AssetDatabase.Refresh();
   var colors=new[]{new Color(1,.025f,.012f),new Color(.035f,.9f,.16f),new Color(.62f,.05f,1),new Color(.035f,.32f,1)};
   var materials=SymbolNames.Select((n,i)=>SymbolMaterial(n,colors[i],1.4f)).ToArray();var outline=SymbolMaterial("Contorno_oscuro",new Color(.008f,.01f,.014f),0);
   var paths=new[]{
    new List<Vector2[]>{new[]{new Vector2(0,.46f),new Vector2(-.43f,-.38f),new Vector2(.43f,-.38f),new Vector2(0,.46f)},new[]{new Vector2(0,.20f),new Vector2(0,-.10f)},new[]{new Vector2(-.035f,-.25f),new Vector2(.035f,-.25f)}},
    new List<Vector2[]>{new[]{new Vector2(0,.45f),new Vector2(.40f,0),new Vector2(0,-.45f),new Vector2(-.40f,0),new Vector2(0,.45f)},new[]{new Vector2(-.24f,.12f),new Vector2(.24f,.12f)},new[]{new Vector2(-.24f,-.12f),new Vector2(.24f,-.12f)}},
    new List<Vector2[]>{A03Arc(Vector2.zero,.34f,35,310,32),A03Arc(Vector2.zero,.46f,95,255,24),new[]{new Vector2(-.08f,.47f),new Vector2(.07f,.16f),new Vector2(-.10f,-.08f),new Vector2(.12f,-.47f)}},
    new List<Vector2[]>{new[]{new Vector2(-.18f,.45f),new Vector2(-.43f,.45f),new Vector2(-.43f,-.45f),new Vector2(-.18f,-.45f)},new[]{new Vector2(.18f,.45f),new Vector2(.43f,.45f),new Vector2(.43f,-.45f),new Vector2(.18f,-.45f)},new[]{new Vector2(0,.27f),new Vector2(.22f,0),new Vector2(0,-.27f),new Vector2(-.22f,0),new Vector2(0,.27f)}}};
   var meshes=paths.Select((p,i)=>SymbolMesh(SymbolNames[i],p)).ToArray();
   for(int i=0;i<4;i++)
   {
    AssetDatabase.CreateAsset(meshes[i],SymbolAssets+"/"+SymbolNames[i]+".asset");
    var temp=new GameObject(SymbolNames[i],typeof(MeshFilter),typeof(MeshRenderer));temp.GetComponent<MeshFilter>().sharedMesh=meshes[i];temp.GetComponent<MeshRenderer>().sharedMaterials=new[]{outline,materials[i]};temp.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    PrefabUtility.SaveAsPrefabAsset(temp,SymbolAssets+"/"+SymbolNames[i]+".prefab");UnityEngine.Object.DestroyImmediate(temp);
   }
   foreach(var root in new[]{a03,r21})foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
   {
    int color=ExistingSymbolColor(renderer.name,root==a03);if(color<0)continue;
    renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>materials[color]).ToArray();receipt.recolored++;
   }
   A03LoadFaces(A03Arch(scene));
   AddProjectedSymbol("Simbolo_Rojo_Advertencia",meshes[0],a03.transform,outline,materials[0],v=>A03Surface(new Vector2(161.1f+v.x*.9f,-11.5f+v.y*.9f))+Vector3.forward*v.z);
   AddProjectedSymbol("Simbolo_Verde_Investigacion",meshes[1],a03.transform,outline,materials[1],v=>A03Surface(new Vector2(151.1f+v.x*.9f,-10.8f+v.y*.9f))+Vector3.forward*v.z);
   AddProjectedSymbol("Simbolo_Azul_Contencion",meshes[3],r21.transform,outline,materials[3],v=>R21WallPoint(scene,new Vector2(112+v.x*1.4f,-3.35f+v.y*1.4f))+Vector3.forward*v.z);
   r21FloorConfig=ConstructionConfig4(scene);
   AddProjectedSymbol("Simbolo_Violeta_Corrupcion",meshes[2],r21.transform,outline,materials[2],v=>R21FloorPoint(V(117+v.x*2.6f,-16,132+v.y*2.6f)) - Vector3.up*v.z);
   receipt.newSymbols=4;Physics.SyncTransforms();receipt.collidersUnchanged=physics==A03Physics(scene);receipt.navUnchanged=receipt.nav==Hash(Nav)&&digest==TriangleDigest4();
   receipt.bidirectional=SymbolRouteCheck();
   if(!receipt.collidersUnchanged||!receipt.navUnchanged||!receipt.bidirectional)throw new InvalidOperationException("No se guarda: cambió la seguridad de navegación.");
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("No se guardó la escena.");receipt.saved=true;receipt.sceneAfter=Hash(City);
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);scene=SceneManager.GetSceneByPath(City);
   int count=scene.GetRootGameObjects().Where(g=>g.name==A03Root||g.name==R21Root).SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Count(r=>r.name.StartsWith("Simbolo_",StringComparison.Ordinal));
   bool emission=materials.All(m=>m&&m.IsKeywordEnabled("_EMISSION")&&m.GetColor("_EmissionColor").maxColorComponent>1);
   receipt.persisted=!scene.isDirty&&count==4&&receipt.sceneAfter==Hash(City)&&receipt.nav==Hash(Nav)&&digest==TriangleDigest4()&&emission&&SymbolRouteCheck();
   File.WriteAllText(SymbolWork+"/Resultado.json",JsonUtility.ToJson(receipt,true));
   if(!receipt.persisted)throw new InvalidOperationException("No se acreditó persistencia tras recarga.");
   Debug.Log("CIUDAD_SIMBOLOS: "+receipt.recolored+" símbolos existentes recoloreados; 4 nuevos; escena guardada y recargada; colliders y NavMesh intactos. Sin cambios de Bloom.");
  }
  static void FinishInteriorSymbols(Scene scene)
  {
   var receipt=JsonUtility.FromJson<SymbolReceipt>(File.ReadAllText(SymbolWork+"/Resultado.json"));
   if(scene.isDirty||Hash(City)!=receipt.sceneAfter)throw new InvalidOperationException("No se descartan cambios posteriores al guardado de símbolos.");
   foreach(string name in SymbolNames)
   {
    var mat=AssetDatabase.LoadAssetAtPath<Material>(SymbolAssets+"/"+name+".mat");mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;mat.EnableKeyword("_EMISSION");EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
    AssetDatabase.ImportAsset(SymbolAssets+"/"+name+".mat",ImportAssetOptions.ForceSynchronousImport);
   }
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);scene=SceneManager.GetSceneByPath(City);
   receipt.persistedSymbols=scene.GetRootGameObjects().Where(g=>g.name==A03Root||g.name==R21Root).SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Count(r=>r.name.StartsWith("Simbolo_",StringComparison.Ordinal));
   receipt.materialsEmissive=SymbolNames.All(name=>{var m=AssetDatabase.LoadAssetAtPath<Material>(SymbolAssets+"/"+name+".mat");return m.IsKeywordEnabled("_EMISSION")&&m.GetColor("_EmissionColor").maxColorComponent>1;});
   receipt.cleanAfterReload=!scene.isDirty;receipt.bidirectional=SymbolRouteCheck();receipt.navUnchanged=Hash(Nav)==receipt.nav;
   receipt.persisted=receipt.persistedSymbols==4&&receipt.materialsEmissive&&receipt.cleanAfterReload&&receipt.bidirectional&&receipt.navUnchanged&&Hash(City)==receipt.sceneAfter;
   File.WriteAllText(SymbolWork+"/Resultado.json",JsonUtility.ToJson(receipt,true));if(!receipt.persisted)throw new InvalidOperationException("No se acreditó persistencia: "+JsonUtility.ToJson(receipt));
   Debug.Log("CIUDAD_SIMBOLOS_FINAL: 50 existentes actualizados, 4 símbolos nuevos; emisión, guardado y recarga verificados. Sin cambios de Bloom ni navegación.");
  }
  static void AddProjectedSymbol(string name,Mesh source,Transform parent,Material outline,Material color,Func<Vector3,Vector3> project)
  {
   var mesh=UnityEngine.Object.Instantiate(source);mesh.name=name;mesh.vertices=source.vertices.Select(project).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,SymbolAssets+"/"+name+"_Proyectado.asset");
   var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterials=new[]{outline,color};g.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  }
  static bool SymbolRouteCheck()
  {
   var a=V(156,-16,99);var b=V(117,-16,123);var f=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
   if(!NavMesh.SamplePosition(a,out var aa,.35f,f)||!NavMesh.SamplePosition(b,out var bb,.35f,f))return false;
   var path=new NavMeshPath();return NavMesh.CalculatePath(aa.position,bb.position,f,path)&&path.status==NavMeshPathStatus.PathComplete&&NavMesh.CalculatePath(bb.position,aa.position,f,path)&&path.status==NavMeshPathStatus.PathComplete;
  }

  const string Batch02Work="Backups/CiudadDelSol/Interior/Lote02_20261118";
  const string Batch02Root="CiudadDelSol_Ambientacion_Lote02";
  [Serializable] sealed class SymbolRoomReceipt { public string id;public int symbols;public string colors;public Vector3[] positions; }
  [Serializable] sealed class SymbolBatchReceipt
  {
   public bool saved,persisted,collidersUnchanged,navUnchanged,localNavigation;
   public string sceneBefore,sceneAfter,nav;public List<SymbolRoomReceipt> rooms=new List<SymbolRoomReceipt>();
  }
  sealed class SymbolRoom
  {
   public string id,parent,prefix;public Vector3 center;public float south,north;
   public int[] colors;public float[] stations;
  }
  static bool TryRoomGlyph(Collider[] walls,Vector3 origin,Vector3 direction,float size,Mesh source,out Vector3[] vertices,out RaycastHit support)
  {
   vertices=null;support=default;float closest=100;
   foreach(var c in walls)if(c.Raycast(new Ray(origin,direction),out var hit,20)&&hit.distance<closest&&Mathf.Abs(hit.normal.y)<.15f){support=hit;closest=hit.distance;}
   if(!support.collider)return false;var contact=support;var rotation=Quaternion.LookRotation(-contact.normal,Vector3.up);var result=new List<Vector3>();
   foreach(var v in source.vertices)
   {
    var p=contact.point+rotation*new Vector3(v.x*size,v.y*size,0);var ray=new Ray(p+contact.normal*2,-contact.normal);
    if(!contact.collider.Raycast(ray,out var h,4)||Vector3.Dot(h.normal,contact.normal)<.75f)return false;
    result.Add(h.point+contact.normal*(.009f-v.z));
   }
   vertices=result.ToArray();return true;
  }
  static bool Batch02LocalPaths(SymbolRoom[] rooms)
  {
   var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
   foreach(var room in rooms.Skip(1))foreach(bool reverse in new[]{false,true})
   {
    var from=reverse?room.center:rooms[0].center;var to=reverse?rooms[0].center:room.center;
    if(!NavMesh.SamplePosition(from,out var a,.35f,filter)||!NavMesh.SamplePosition(to,out var b,.35f,filter)||Mathf.Abs(a.position.y-from.y)>.35f||Mathf.Abs(b.position.y-to.y)>.35f)return false;
    var path=new NavMeshPath();if(!NavMesh.CalculatePath(a.position,b.position,filter,path)||path.status!=NavMeshPathStatus.PathComplete)return false;
    var cfg=ConstructionConfig4(SceneManager.GetSceneByPath(City));Station previous=null;
    for(int leg=1;leg<path.corners.Length;leg++)
    {
     var p=path.corners[leg-1];var q=path.corners[leg];int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(p,q)/.15f));
     for(int j=leg==1?0:1;j<=n;j++)
     {
      var st=Probe(Vector3.Lerp(p,q,(float)j/n),cfg,false,.35f);if(!st.Clear||!st.nav)return false;
      if(previous!=null&&!Sweep(previous,st,cfg,false,out _,out _))return false;previous=st;
     }
    }
   }
   return true;
  }
  static void ApplySymbolsBatch02(Scene scene)
  {
   if(scene.isDirty||scene.GetRootGameObjects().Any(g=>g.name==Batch02Root))throw new InvalidOperationException("Escena sin guardar o lote ya existente; no duplicar ni descartar cambios.");
   if(!File.Exists(Batch02Work+"/Antes_SHA256.json"))throw new InvalidOperationException("Falta respaldo.");
   var rooms=new[]{
    new SymbolRoom{id="R23",parent="Subsuelo_34_Naves_Sin_Reticula",prefix="Nave 114/156 ",center=V(123,-16,165),south=156,north=174,colors=new[]{3,1},stations=new[]{159f,171f}},
    new SymbolRoom{id="G11",parent="Subsuelo_Rediseno_16_Sectores",prefix="E6 Entrada occidental al ala del Sol",center=V(105,-16,165),south=150,north=180,colors=new[]{3,1},stations=new[]{153f,177f}},
    new SymbolRoom{id="R26",parent="Subsuelo_34_Naves_Sin_Reticula",prefix="Nave 132/120 ",center=V(141,-16,159),south=120,north=198,colors=new[]{0,2,2},stations=new[]{159f,123f,195f}}};
   var receipt=new SymbolBatchReceipt{sceneBefore=Hash(City),nav=Hash(Nav)};string physics=A03Physics(scene),digest=TriangleDigest4();
   if(!Batch02LocalPaths(rooms))throw new InvalidOperationException("La navegación local base no está acreditada. No se ambienta sobre un fallo previo.");
   var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   string applied=SymbolAssets+"/Aplicados_Lote02";Directory.CreateDirectory(applied);AssetDatabase.Refresh();
   var batch=new GameObject(Batch02Root);SceneManager.MoveGameObjectToScene(batch,scene);
   foreach(var room in rooms)
   {
    var architecture=transforms.Single(t=>t.parent&&t.parent.name==room.parent&&t.name.StartsWith(room.prefix,StringComparison.Ordinal));
    var walls=architecture.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger).ToArray();
    var group=new GameObject(room.id);group.transform.SetParent(batch.transform,false);var positions=new List<Vector3>();var contacts=new List<RaycastHit>();
    for(int i=0;i<room.colors.Length;i++)
    {
     int color=room.colors[i];var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SymbolAssets+"/"+SymbolNames[color]+".prefab");if(!prefab)throw new InvalidOperationException("Falta prefab común.");
     var source=prefab.GetComponent<MeshFilter>().sharedMesh;Vector3[] vertices=null;RaycastHit support=default;bool fitted=false;
     // Prefer eye-level pilasters. Only if their full footprint does not fit,
     // use an existing end-arch/gable; no new surface is built.
     foreach(var candidate in new[]{(p:V(room.center.x,-13.3f,room.stations[i]),d:Vector3.right,s:.9f),(p:V(room.center.x,-13.3f,room.stations[i]),d:Vector3.left,s:.9f),
      (p:V(room.center.x+(i-1)*2.5f,-5.4f,i%2==0?room.south+3:room.north-3),d:i%2==0?Vector3.back:Vector3.forward,s:1.8f)})
     {
      if(!TryRoomGlyph(walls,candidate.p,candidate.d,candidate.s,source,out vertices,out support))continue;
      if(positions.Any(p=>Vector3.Distance(p,support.point)<1.5f))continue;fitted=true;break;
     }
     if(!fitted)throw new InvalidOperationException(room.id+" sin superficie completa para símbolo "+i+"; no se fuerza.");
     var mesh=UnityEngine.Object.Instantiate(source);mesh.name=room.id+"_"+SymbolNames[color]+"_"+i;mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,applied+"/"+mesh.name+".asset");
     var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);instance.transform.SetParent(group.transform,false);instance.name=mesh.name;instance.GetComponent<MeshFilter>().sharedMesh=mesh;
     PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<MeshFilter>());PrefabUtility.RecordPrefabInstancePropertyModifications(instance);positions.Add(support.point);contacts.Add(support);
    }
    // One short-range unshadowed light per space; no fixtures or global changes.
    var anchor=contacts.OrderBy(c=>Mathf.Abs(c.point.z-room.center.z)).First();var lightObject=new GameObject("Luz_local_"+room.id,typeof(Light));lightObject.transform.SetParent(group.transform,false);
    lightObject.transform.position=anchor.point+anchor.normal*.25f+Vector3.up*1.2f;
    var target=anchor.point+anchor.normal*4;target.y=room.center.y+.3f;lightObject.transform.rotation=Quaternion.LookRotation(target-lightObject.transform.position);
    var light=lightObject.GetComponent<Light>();light.type=LightType.Spot;light.range=room.id=="G11"?9:10;light.spotAngle=85;light.intensity=4;light.shadows=LightShadows.None;light.bounceIntensity=0;
    light.color=room.id=="G11"?new Color(.45f,.65f,.85f):new Color(1,.69f,.4f);
    receipt.rooms.Add(new SymbolRoomReceipt{id=room.id,symbols=positions.Count,colors=string.Join(",",room.colors.Select(c=>SymbolNames[c]).Distinct()),positions=positions.ToArray()});
   }
   Physics.SyncTransforms();receipt.collidersUnchanged=physics==A03Physics(scene);receipt.navUnchanged=receipt.nav==Hash(Nav)&&digest==TriangleDigest4();receipt.localNavigation=Batch02LocalPaths(rooms);
   if(!receipt.collidersUnchanged||!receipt.navUnchanged||!receipt.localNavigation||batch.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("No se guarda: comprobación local o integridad fallida.");
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("No se guardó el lote.");receipt.saved=true;receipt.sceneAfter=Hash(City);
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);scene=SceneManager.GetSceneByPath(City);var loaded=scene.GetRootGameObjects().Single(g=>g.name==Batch02Root);
   receipt.persisted=!scene.isDirty&&loaded.GetComponentsInChildren<MeshRenderer>(true).Length==7&&loaded.GetComponentsInChildren<Light>(true).Length==3&&loaded.GetComponentsInChildren<Collider>(true).Length==0&&Hash(City)==receipt.sceneAfter&&Hash(Nav)==receipt.nav&&TriangleDigest4()==digest&&Batch02LocalPaths(rooms);
   File.WriteAllText(Batch02Work+"/Resultado.json",JsonUtility.ToJson(receipt,true));if(!receipt.persisted)throw new InvalidOperationException("No se acreditó persistencia del lote.");
   Debug.Log("CIUDAD_LOTE02: R23=2, G11=2, R26=3 símbolos; tres luces locales. Guardado, recarga y navegación local satisfactorios.");
  }
 }
}
