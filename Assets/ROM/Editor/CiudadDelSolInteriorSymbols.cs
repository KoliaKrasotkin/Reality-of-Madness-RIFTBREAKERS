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

  // One resumable visual-only workflow for the remaining documented spaces.
  const string MassWork="Backups/CiudadDelSol/Interior/AmbientacionMasiva_20261118";
  const string MassRoot="CiudadDelSol_Ambientacion_Interior";
  [Serializable] sealed class MassBounds { public Vector3 center,size; }
  [Serializable] sealed class MassRoom
  { public string id,sector,hierarchy;public Vector3 center,seed;public float width,depth;public Vector3[] sites;public MassBounds bounds; }
  [Serializable] sealed class MassPlan { public MassRoom[] rooms;public string[] preserved,reserved; }
  [Serializable] sealed class MassResult
  { public string id,status,reason;public int symbols,lights,fog;public Vector3[] positions;public bool localNavigation; }
  [Serializable] sealed class MassReceipt
  {
   public string sceneBefore,sceneAfter,nav,digest;public int batches;
   public bool saved,persisted,physicsUnchanged,navUnchanged;public List<MassResult> rooms=new List<MassResult>();
  }
  static bool MassLocalNav(Vector3 a,Vector3 b)
  {
   var f=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
   if(!NavMesh.SamplePosition(a,out var aa,.35f,f)||!NavMesh.SamplePosition(b,out var bb,.35f,f)||Mathf.Abs(aa.position.y-a.y)>.35f||Mathf.Abs(bb.position.y-b.y)>.35f)return false;
   var p=new NavMeshPath();return NavMesh.CalculatePath(aa.position,bb.position,f,p)&&p.status==NavMeshPathStatus.PathComplete&&NavMesh.CalculatePath(bb.position,aa.position,f,p)&&p.status==NavMeshPathStatus.PathComplete;
  }
  static bool MassFloorGlyph(Config cfg,Vector3 point,float size,Mesh mesh,out Vector3[] vertices,out RaycastHit support)
  {
   vertices=null;support=default;var p=Probe(point,cfg,false,.35f);if(!p.Clear||!p.nav||p.slope>1||p.navEdge<size*.7f+.6f)return false;
   var result=new Vector3[mesh.vertexCount];var vv=mesh.vertices;
   for(int i=0;i<vv.Length;i++)
   {
    Vector3 q=p.floor+V(vv[i].x*size,0,vv[i].y*size);
    if(!cfg.physics.Raycast(q+Vector3.up*.25f,Vector3.down,out var h,.5f,cfg.mask,QueryTriggerInteraction.Ignore)||h.normal.y<.99f||Mathf.Abs(h.point.y-p.floor.y)>.03f)return false;
    if(i==0)support=h;result[i]=h.point+Vector3.up*(.009f-vv[i].z);
   }
   vertices=result;return true;
  }
  static int[] MassColors(MassRoom r)
  {
   if(r.center.y<=-30)return new[]{2,0,3,2};
   if(r.sector=="S05")return new[]{1,3,1};
   if(r.sector=="S08")return new[]{0,2,3,2};
   if(r.sector=="S04")return new[]{3,2,0};
   if(r.sector=="S06")return new[]{1,2,1};
   if(r.sector=="S01"||r.sector=="S02")return new[]{3,1,3};
   return new[]{3,0,2};
  }
  static Material MassFogMaterial()
  {
   string folder=SymbolAssets+"/Atmosfera";Directory.CreateDirectory(folder);
   var existing=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Niebla_violeta_tenue.mat");if(existing)return existing;
   var shader=Shader.Find("Universal Render Pipeline/Unlit");if(!shader)throw new InvalidOperationException("Falta URP Unlit.");
   var texture=new Texture2D(64,64,TextureFormat.RGBA32,false,true){name="Niebla_suave",wrapMode=TextureWrapMode.Clamp};
   for(int y=0;y<64;y++)for(int x=0;x<64;x++)
   {
    float dx=(x-31.5f)/31.5f,dy=(y-31.5f)/31.5f;
    float alpha=Mathf.Pow(Mathf.Clamp01(1-dx*dx-dy*dy),2)*(.7f+.3f*Mathf.PerlinNoise(x*.12f,y*.12f));
    texture.SetPixel(x,y,new Color(1,1,1,alpha));
   }
   texture.Apply(false,false);AssetDatabase.CreateAsset(texture,folder+"/Niebla_suave.asset");
   var mat=new Material(shader){name="Niebla_violeta_tenue",renderQueue=3000};
   mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",0);mat.SetFloat("_SrcBlend",5);mat.SetFloat("_DstBlend",10);mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",0);
   mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetOverrideTag("RenderType","Transparent");mat.SetTexture("_BaseMap",texture);mat.SetColor("_BaseColor",new Color(.33f,.07f,.52f,.055f));
   AssetDatabase.CreateAsset(mat,folder+"/Niebla_violeta_tenue.mat");return mat;
  }
  static int MassFog(MassRoom room,Transform group,List<Station> safe,Config cfg)
  {
   if(room.center.y>-30)return 0;int made=0;
   foreach(var st in safe.Where(t=>t.slope<1&&t.navEdge>2.4f))
   {
    // Two tiny, static soft patches at ankle height, wholly supported by one level.
    // No particles, gameplay callbacks, lights or global fog; at most six quads per space.
    bool supported=true;foreach(float x in new[]{-1.4f,0,1.4f})foreach(float z in new[]{-1.4f,0,1.4f})
    {
     var q=st.floor+V(x,0,z);
     if(!cfg.physics.Raycast(q+Vector3.up*.25f,Vector3.down,out var hit,.5f,cfg.mask,QueryTriggerInteraction.Ignore)||hit.normal.y<.99f||Mathf.Abs(hit.point.y-st.floor.y)>.03f)supported=false;
    }
    if(!supported)continue;
    var mat=MassFogMaterial();
    for(int layer=0;layer<3;layer++)
    {
     var g=new GameObject("Niebla_baja_"+made+"_"+layer,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(group,false);g.transform.position=st.floor+Vector3.up*(.07f+layer*.07f);
     // Shared horizontal quad; alpha fades to zero well before its supported boundary.
     string path=SymbolAssets+"/Atmosfera/Plano_niebla.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
     if(!mesh){mesh=new Mesh{name="Plano_niebla"};mesh.vertices=new[]{V(-1.4f,0,-1.4f),V(-1.4f,0,1.4f),V(1.4f,0,1.4f),V(1.4f,0,-1.4f)};mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);}
     g.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    made++;if(made==2)break;
   }
   return made*3;
  }
  static void ApplySymbolsMass(Scene scene)
  {
   if(scene.isDirty||!File.Exists(MassWork+"/Antes_SHA256.json"))throw new InvalidOperationException("Se necesita escena guardada y respaldo verificado.");
   var plan=JsonUtility.FromJson<MassPlan>(File.ReadAllText(MassWork+"/Espacios.json"));
   string resultPath=MassWork+"/Resultado.json";
   var receipt=File.Exists(resultPath)?JsonUtility.FromJson<MassReceipt>(File.ReadAllText(resultPath)):new MassReceipt{sceneBefore=Hash(City),nav=Hash(Nav),digest=TriangleDigest4()};
   if(Hash(Nav)!=receipt.nav||TriangleDigest4()!=receipt.digest)throw new InvalidOperationException("La navegación cambió; no se sobrescribe.");
   var pending=plan.rooms.Where(r=>!receipt.rooms.Any(t=>t.id==r.id)).Take(8).ToArray();if(pending.Length==0)return;
   string physics=A03Physics(scene);var cfg=ConstructionConfig4(scene);
   var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   var lookup=all.GroupBy(t=>Hierarchy(t)).ToDictionary(g=>g.Key,g=>g.First());
   var root=scene.GetRootGameObjects().SingleOrDefault(g=>g.name==MassRoot);
   if(!root){root=new GameObject(MassRoot);SceneManager.MoveGameObjectToScene(root,scene);}
   string applied=SymbolAssets+"/Aplicados_Interior";Directory.CreateDirectory(applied);AssetDatabase.Refresh();
   foreach(var room in pending)
   {
    var rr=new MassResult{id=room.id,status="pendiente"};GameObject group=null;
    try
    {
     if(root.transform.Find(room.id))throw new InvalidOperationException("Grupo existente sin recepción; no duplicar.");
     // Match exact original hierarchy, allowing the legacy replacement character only in dimensions.
     Transform architecture;
     if(!lookup.TryGetValue(room.hierarchy,out architecture))
      architecture=all.SingleOrDefault(t=>Hierarchy(t).Replace('×','?').Replace('\uFFFD','?')==room.hierarchy.Replace('×','?').Replace('\uFFFD','?'));
     if(!architecture||!architecture.gameObject.activeInHierarchy)throw new InvalidOperationException("Arquitectura física ausente/inactiva.");
     var safe=new List<Station>();
     foreach(var site in new[]{room.seed}.Concat(room.sites))
     {
      var st=Probe(site,cfg,false,.35f);if(!st.Clear||!st.nav||Mathf.Abs(st.floor.y-room.center.y)>.35f)continue;
      if(safe.Count==0||MassLocalNav(safe[0].floor,st.floor))safe.Add(st);
     }
     if(safe.Count==0)throw new InvalidOperationException("Sin muestra local con apoyo, cápsula y NavMesh del nivel correcto.");
     rr.localNavigation=MassLocalNav(safe[0].floor,safe[safe.Count-1].floor);if(!rr.localNavigation)throw new InvalidOperationException("Navegación local incompleta.");
     group=new GameObject(room.id);group.transform.SetParent(root.transform,false);
     var walls=architecture.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger).ToArray();
     // Adjacent existing supporting floor/architecture is only read, never modified.
     var positions=new List<Vector3>();int[] colors=MassColors(room);int desired=room.width*room.depth>1000?5:room.depth>40||room.width>40?4:2;
     for(int i=0;i<desired;i++)
     {
      int color=colors[i%colors.Length];var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SymbolAssets+"/"+SymbolNames[color]+".prefab");
      if(!prefab)throw new InvalidOperationException("Falta símbolo común.");var source=prefab.GetComponent<MeshFilter>().sharedMesh;
      Vector3[] vertices=null;RaycastHit contact=default;bool fit=false;
      foreach(var st in safe.OrderBy(t=>Mathf.Abs(t.floor.z-(room.center.z+(i-(desired-1)*.5f)*6))))
      {
       foreach(float height in new[]{2.5f,3.8f,7.5f,10.5f})
       {
        foreach(var d in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
        {
         var origin=st.floor+Vector3.up*height;
         if(!TryRoomGlyph(walls,origin,d,height>5?1.7f:.95f,source,out vertices,out contact)||positions.Any(p=>Vector3.Distance(p,contact.point)<2.5f))continue;
         if(cfg.physics.Raycast(origin,d,out var front,20,cfg.mask,QueryTriggerInteraction.Ignore)&&Vector3.Distance(front.point,contact.point)>.03f)continue;
         fit=true;break;
        }
        if(fit)break;
       }
       if(fit)break;
      }
      if(!fit)foreach(var st in safe)
      {
       if(positions.Any(p=>Vector3.Distance(p,st.floor)<3))continue;
       if(MassFloorGlyph(cfg,st.floor,1.6f,source,out vertices,out contact)){fit=true;break;}
      }
      if(!fit)continue;
      var mesh=UnityEngine.Object.Instantiate(source);mesh.name=room.id+"_"+SymbolNames[color]+"_"+i;mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
      string path=applied+"/"+mesh.name+".asset";if(File.Exists(path))path=AssetDatabase.GenerateUniqueAssetPath(path);AssetDatabase.CreateAsset(mesh,path);
      var glyph=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);glyph.name=mesh.name;glyph.transform.SetParent(group.transform,false);glyph.GetComponent<MeshFilter>().sharedMesh=mesh;
      PrefabUtility.RecordPrefabInstancePropertyModifications(glyph.GetComponent<MeshFilter>());PrefabUtility.RecordPrefabInstancePropertyModifications(glyph);
      positions.Add(contact.point);
     }
     if(positions.Count==0)throw new InvalidOperationException("No existe huella completa para proyectar símbolos.");
     var anchor=safe.OrderBy(t=>Vector3.Distance(t.floor,room.center)).First();float lampHeight=6;
     if(cfg.physics.Raycast(anchor.floor+Vector3.up*.3f,Vector3.up,out var ceiling,8,cfg.mask,QueryTriggerInteraction.Ignore))lampHeight=Mathf.Min(lampHeight,ceiling.distance-.4f);
     if(lampHeight<2.5f)throw new InvalidOperationException("Altura local insuficiente para iluminación segura.");
     var lg=new GameObject("Resplandor_violeta_"+room.id,typeof(Light));lg.transform.SetParent(group.transform,false);lg.transform.position=anchor.floor+Vector3.up*lampHeight;lg.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);
     var light=lg.GetComponent<Light>();light.type=LightType.Spot;light.range=lampHeight+.65f;light.spotAngle=125;light.innerSpotAngle=55;light.intensity=room.center.y<=-30?6:4.5f;
     light.color=new Color(.51f,.075f,1);light.shadows=LightShadows.None;light.bounceIntensity=0;light.renderMode=LightRenderMode.Auto;
     rr.symbols=positions.Count;rr.positions=positions.ToArray();rr.lights=1;rr.fog=MassFog(room,group.transform,safe,cfg);rr.status="ambientado";
    }
    catch(Exception e)
    {
     if(group)UnityEngine.Object.DestroyImmediate(group);rr.status="omitido";rr.reason=e.Message;rr.symbols=rr.lights=rr.fog=0;
    }
    receipt.rooms.Add(rr);
   }
   Physics.SyncTransforms();receipt.physicsUnchanged=physics==A03Physics(scene);receipt.navUnchanged=Hash(Nav)==receipt.nav&&TriangleDigest4()==receipt.digest;
   if(!receipt.physicsUnchanged||!receipt.navUnchanged||root.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("No se guarda: cambió física o navegación.");
   EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new IOException("No se guardó el lote.");
   receipt.saved=true;receipt.sceneAfter=Hash(City);receipt.batches++;
   receipt.persisted=false;File.WriteAllText(resultPath,JsonUtility.ToJson(receipt,true));
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);scene=SceneManager.GetSceneByPath(City);root=scene.GetRootGameObjects().Single(g=>g.name==MassRoot);
   var expected=receipt.rooms.Where(r=>r.status=="ambientado").ToArray();
   receipt.persisted=!scene.isDirty&&Hash(City)==receipt.sceneAfter&&Hash(Nav)==receipt.nav&&TriangleDigest4()==receipt.digest&&root.GetComponentsInChildren<Collider>(true).Length==0;
   foreach(var rr in expected)
   {
    var g=root.transform.Find(rr.id);receipt.persisted&=g&&g.GetComponentsInChildren<Light>(true).Length==rr.lights&&g.GetComponentsInChildren<MeshFilter>(true).Length==rr.symbols+rr.fog;
    if(g)foreach(var mr in g.GetComponentsInChildren<MeshRenderer>(true))receipt.persisted&=mr.sharedMaterials.All(m=>m&&m.shader&&m.shader.isSupported);
   }
   File.WriteAllText(resultPath,JsonUtility.ToJson(receipt,true));
   if(!receipt.persisted)throw new InvalidOperationException("Persistencia del lote no acreditada.");
   Debug.Log("CIUDAD_AMBIENTACION: lote "+receipt.batches+" guardado y recargado; "+expected.Length+" espacios, "+expected.Sum(r=>r.symbols)+" símbolos, "+expected.Sum(r=>r.lights)+" luces; física/NavMesh intactos.");
   if(plan.rooms.Any(r=>!receipt.rooms.Any(t=>t.id==r.id)))File.WriteAllText(R21Work+"/Accion.pending","{\"action\":\"symbols-mass\"}");
  }

  const string RuneWork="Backups/CiudadDelSol/Interior/Runas_20261118";
  const string RuneAssets=SymbolAssets+"/Runas";
  const string RuneRoot="CiudadDelSol_EscriturasDelQuiebre";
  static readonly string[] RuneColors={"Violeta","Celeste","Rojo","Azul","Verde"};
  [Serializable] sealed class RuneRoomResult
  {
   public string id,status,reason;public int[] inscriptions=new int[5];public int characters,lights,fogPatches;
   public bool localNavigation;public List<Vector3> contacts=new List<Vector3>();
  }
  [Serializable] sealed class RuneReceipt
  {
   public string sceneBefore,sceneAfter,nav,digest,bloom;public int batches;
   public bool saved,persisted,physicsUnchanged,navUnchanged;
   public List<RuneRoomResult> rooms=new List<RuneRoomResult>();
  }
  static Mesh RuneWriting(int seed,bool vertical,int lines,out int characters)
  {
   // A fictitious ornamental alphabet, deliberately without translations or historical claims.
   var segments=new[]{
    new[]{new Vector2(-.20f,-.42f),new Vector2(-.20f,.42f)},new[]{new Vector2(.20f,-.42f),new Vector2(.20f,.42f)},
    new[]{new Vector2(-.20f,.42f),new Vector2(.20f,.24f)},new[]{new Vector2(-.20f,.24f),new Vector2(.20f,.42f)},
    new[]{new Vector2(-.20f,.05f),new Vector2(.20f,.05f)},new[]{new Vector2(-.20f,-.15f),new Vector2(.20f,.05f)},
    new[]{new Vector2(-.20f,-.42f),new Vector2(.20f,-.24f)},new[]{new Vector2(-.20f,-.24f),new Vector2(.20f,-.42f)},
    new[]{new Vector2(0,.42f),new Vector2(0,-.42f)},new[]{new Vector2(-.28f,.31f),new Vector2(.28f,-.31f)}};
   int[] alphabet={0x035,0x192,0x069,0x10C,0x282,0x141,0x0A6,0x314,0x04B,0x1A8,0x263,0x0D1,0x186,0x215,0x142,0x08D,0x329,0x052};
   var rng=new System.Random(seed);var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();characters=0;
   float scale=.27f+(seed%4)*.025f;int rows=vertical?1:lines;
   for(int row=0;row<rows;row++)
   {
    int count=vertical?6+rng.Next(3):8+rng.Next(7);float advance=scale*.72f;
    for(int letter=0;letter<count;letter++)
    {
     int mask=alphabet[rng.Next(alphabet.Length)];float gap=(letter/4)*scale*.27f;
     var origin=vertical?new Vector2(0,(count*.5f-letter)*scale*1.12f):new Vector2((letter-count*.5f)*advance+gap,-row*scale*1.4f);
     for(int s=0;s<segments.Length;s++)if((mask&(1<<s))!=0)
     {
      var a=origin+segments[s][0]*scale;var b=origin+segments[s][1]*scale;var d=(b-a).normalized;var side=new Vector2(-d.y,d.x)*scale*.15f;int n=vertices.Count;
      vertices.Add(new Vector3(a.x-side.x,a.y-side.y,0));vertices.Add(new Vector3(a.x+side.x,a.y+side.y,0));vertices.Add(new Vector3(b.x+side.x,b.y+side.y,0));vertices.Add(new Vector3(b.x-side.x,b.y-side.y,0));
      uv.Add(new Vector2(-1,0));uv.Add(new Vector2(1,0));uv.Add(new Vector2(1,1));uv.Add(new Vector2(-1,1));triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
     }
     characters++;
    }
   }
   var mesh=new Mesh{name="Escritura_"+seed};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }
  static Material RuneMaterial(int color,int energy)
  {
   string path=RuneAssets+"/"+RuneColors[color]+"_"+energy+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
   var shader=AssetDatabase.LoadAssetAtPath<Shader>(RuneAssets+"/EscrituraEnergetica.shader");if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Shader rúnico no compilado.");
   var colors=new[]{new Color(.61f,.045f,1),new Color(.03f,.79f,1),new Color(1,.025f,.012f),new Color(.035f,.25f,1),new Color(.04f,1,.16f)};
   m=new Material(shader){name=RuneColors[color]+"_"+energy};m.SetColor("_Color",colors[color]);m.SetFloat("_Energy",energy);AssetDatabase.CreateAsset(m,path);return m;
  }
  static int RuneColorAt(int index)
  {
   int x=index%20;if(x<10)return 0;if(x<15)return 1;if(x<17)return 2;if(x<19)return 3;return 4;
  }
  static int RuneStableSeed(string id,int i)
  { int n=17;foreach(char c in id)n=n*31+c;return (n*31+i)&0x7FFFFFFF; }
  static void RuneRetirePrevious(Scene scene,string id)
  {
   foreach(var root in scene.GetRootGameObjects())
   {
    if(root.name==MassRoot||root.name==Batch02Root)
    {
     var old=root.transform.Find(id);if(!old)continue;
     foreach(var m in old.GetComponentsInChildren<MeshRenderer>(true)){m.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(m);}
     foreach(var l in old.GetComponentsInChildren<Light>(true))l.enabled=false;
    }
    else if((id=="A03"&&root.name==A03Root)||(id=="R21"&&root.name==R21Root))
    {
     foreach(var m in root.GetComponentsInChildren<MeshRenderer>(true))
      if(ExistingSymbolColor(m.name,id=="A03")>=0||m.name.StartsWith("Simbolo_",StringComparison.Ordinal)){m.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(m);}
    }
   }
  }
  static int RuneRoomLights(MassRoom room,Transform group,List<Station> safe)
  {
   int wanted=room.width*room.depth>=2400?3:room.width*room.depth>=900||Mathf.Max(room.width,room.depth)>=60?2:1;
   var chosen=new List<Vector3>();int count=0;var cfg=ConstructionConfig4(SceneManager.GetSceneByPath(City));
   foreach(var st in safe.OrderBy(t=>Vector3.Distance(t.floor,room.center)))
   {
    if(chosen.Any(p=>Vector3.Distance(p,st.floor)<9))continue;
    float height=9;
    if(cfg.physics.Raycast(st.floor+Vector3.up*.3f,Vector3.up,out var top,12,cfg.mask,QueryTriggerInteraction.Ignore))height=Mathf.Min(height,top.distance-.5f);
    if(height<1.6f)continue;
    var g=new GameObject("Luz_"+(count==2?"celeste":"violeta")+"_"+count,typeof(Light));g.transform.SetParent(group,false);g.transform.position=st.floor+Vector3.up*height;g.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);
    var light=g.GetComponent<Light>();light.type=LightType.Spot;light.range=height+1.3f;light.spotAngle=135;light.innerSpotAngle=65;light.shadows=LightShadows.None;light.bounceIntensity=0;light.renderMode=LightRenderMode.Auto;
    light.color=count==2?new Color(.035f,.72f,1):new Color(.56f,.065f,1);light.intensity=room.center.y<=-30?38:28;
    chosen.Add(st.floor);count++;if(count>=wanted)break;
   }
   return count;
  }
  static int RuneRoomFog(MassRoom room,Transform group,List<Station> safe,Config cfg)
  {
   bool special=room.id=="R30"||room.id=="M04";if(room.center.y>-30&&!special)return 0;
   int max=room.id=="D02"?12:room.id=="D01"?9:special?5:3;var used=new List<Vector3>();var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
   Action<Vector3,Vector3,Vector3,Vector3> quad=(a,b,c,d)=>{int n=v.Count;v.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});};
   foreach(var st in safe.Where(p=>p.slope<1&&p.navEdge>3.1f))
   {
    if(used.Any(p=>Vector3.Distance(p,st.floor)<4.5f))continue;float radius=Mathf.Min(2.3f,st.navEdge-1);
    bool flat=true;foreach(float x in new[]{-radius,-radius*.5f,0,radius*.5f,radius})foreach(float z in new[]{-radius,-radius*.5f,0,radius*.5f,radius})
     if(!cfg.physics.Raycast(st.floor+V(x,.25f,z),Vector3.down,out var h,.5f,cfg.mask,QueryTriggerInteraction.Ignore)||h.normal.y<.99f||Mathf.Abs(h.point.y-st.floor.y)>.03f)flat=false;
    if(!flat)continue;var p=st.floor;
    quad(p+V(-radius,.10f,-radius),p+V(-radius,.10f,radius),p+V(radius,.10f,radius),p+V(radius,.10f,-radius));
    quad(p+V(-radius,.27f,-radius),p+V(-radius,.27f,radius),p+V(radius,.27f,radius),p+V(radius,.27f,-radius));
    quad(p+V(-radius,.02f,0),p+V(-radius,.52f,0),p+V(radius,.52f,0),p+V(radius,.02f,0));
    quad(p+V(0,.02f,-radius),p+V(0,.52f,-radius),p+V(0,.52f,radius),p+V(0,.02f,radius));
    used.Add(p);if(used.Count==max)break;
   }
   if(used.Count==0)return 0;var mesh=new Mesh{name=room.id+"_Niebla_local"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,RuneAssets+"/"+mesh.name+".asset");
   string path=RuneAssets+"/Niebla_profunda.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!mat){mat=new Material(MassFogMaterial()){name="Niebla_profunda"};mat.SetColor("_BaseColor",new Color(.43f,.08f,.68f,.095f));AssetDatabase.CreateAsset(mat,path);}
   var fog=new GameObject("Niebla_local_baja",typeof(MeshFilter),typeof(MeshRenderer));fog.transform.SetParent(group,false);fog.GetComponent<MeshFilter>().sharedMesh=mesh;var mr=fog.GetComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mr.receiveShadows=false;return used.Count;
  }
  static string RuneBloomState(Scene scene)
  {
   var result=new List<string>();var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;result.Add("pipeline="+(pipeline?pipeline.name:"null"));
   foreach(var root in scene.GetRootGameObjects())foreach(var volume in root.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true))
   {
    var profile=volume.sharedProfile;if(profile&&profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))result.Add(Hierarchy(volume.transform)+":active="+(volume.enabled&&volume.gameObject.activeInHierarchy)+",intensity="+bloom.intensity.value+",threshold="+bloom.threshold.value);
   }
   foreach(var root in scene.GetRootGameObjects())foreach(var cam in root.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(true))result.Add("camera="+Hierarchy(cam.transform)+",post="+cam.renderPostProcessing);
   return string.Join(";",result)+"; global settings unchanged; local shader supplies halo independently";
  }
  static void ApplyInteriorRunes(Scene scene)
  {
   if(scene.isDirty||!File.Exists(RuneWork+"/Antes_SHA256.json"))throw new InvalidOperationException("Se necesita escena guardada y respaldo verificable.");
   var plan=JsonUtility.FromJson<MassPlan>(File.ReadAllText(RuneWork+"/Espacios.json"));string path=RuneWork+"/Resultado.json";
   var receipt=File.Exists(path)?JsonUtility.FromJson<RuneReceipt>(File.ReadAllText(path)):new RuneReceipt{sceneBefore=Hash(City),nav=Hash(Nav),digest=TriangleDigest4(),bloom=RuneBloomState(scene)};
   if(Hash(Nav)!=receipt.nav||TriangleDigest4()!=receipt.digest)throw new InvalidOperationException("La navegación cambió.");
   var pending=plan.rooms.Where(r=>!receipt.rooms.Any(x=>x.id==r.id)).Take(8).ToArray();if(pending.Length==0)return;
   string physics=A03Physics(scene);var cfg=ConstructionConfig4(scene);var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   var allColliders=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger).ToArray();
   var root=scene.GetRootGameObjects().SingleOrDefault(g=>g.name==RuneRoot);if(!root){root=new GameObject(RuneRoot);SceneManager.MoveGameObjectToScene(root,scene);}
   int colorIndex=receipt.rooms.Sum(r=>r.inscriptions.Sum());
   foreach(var room in pending)
   {
    var rr=new RuneRoomResult{id=room.id,status="pendiente"};GameObject group=null;
    try
    {
     if(root.transform.Find(room.id))throw new InvalidOperationException("Grupo existente; no duplicar.");
     var architecture=transforms.SingleOrDefault(t=>Hierarchy(t).Replace('×','?').Replace('\uFFFD','?')==room.hierarchy.Replace('×','?').Replace('\uFFFD','?'));
     if(!architecture||!architecture.gameObject.activeInHierarchy)throw new InvalidOperationException("Arquitectura ausente/inactiva.");
     var safe=new List<Station>();foreach(var site in new[]{room.seed}.Concat(room.sites))
     {
      var st=Probe(site,cfg,false,.35f);if(st.Clear&&st.nav&&Mathf.Abs(st.floor.y-room.center.y)<.35f&&(safe.Count==0||MassLocalNav(safe[0].floor,st.floor)))safe.Add(st);
     }
     if(safe.Count==0)throw new InvalidOperationException("Sin muestra física/NavMesh segura.");rr.localNavigation=MassLocalNav(safe[0].floor,safe[safe.Count-1].floor);
     if(!rr.localNavigation)throw new InvalidOperationException("Navegación local incompleta.");
     group=new GameObject(room.id);group.transform.SetParent(root.transform,false);
     var volume=new Bounds(room.center+Vector3.up*8,V(room.width+1.5f,16,room.depth+1.5f));
     var walls=architecture.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger).Concat(allColliders.Where(c=>c.bounds.Intersects(volume))).Distinct().ToArray();
     int desired=room.id=="R30"||room.id=="M04"||room.id=="D02"?14:room.width*room.depth>1800?10:Mathf.Max(room.width,room.depth)>42?8:5;
     var occupied=new List<Bounds>();
     for(int i=0;i<desired;i++)
     {
      bool fit=false;Vector3[] projected=null;RaycastHit contact=default;Mesh source=null;int chars=0;
      foreach(bool vertical in new[]{i%5==4,true})
      {
       source=RuneWriting(RuneStableSeed(room.id,i),vertical,1+i%3,out chars);
       foreach(var st in safe.OrderBy(p=>Mathf.Abs(p.floor.z-(room.center.z+(i-(desired-1)*.5f)*3.2f))))
       {
        float headroom=15;if(cfg.physics.Raycast(st.floor+Vector3.up*.3f,Vector3.up,out var ceiling,16,cfg.mask,QueryTriggerInteraction.Ignore))headroom=ceiling.distance+.1f;
        foreach(float height in new[]{2.4f,3.6f,1.25f,6f,8.5f,10.5f,12.5f,14f})
        {
         if(height+source.bounds.max.y>headroom)continue;
         foreach(var d in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
         {
          var origin=st.floor+Vector3.up*height;
          if(!TryRoomGlyph(walls,origin,d,1,source,out projected,out contact))continue;
          if(cfg.physics.Raycast(origin,d,out var front,20,cfg.mask,QueryTriggerInteraction.Ignore)&&Vector3.Distance(front.point,contact.point)>.03f)continue;
          var bounds=new Bounds(projected[0],Vector3.zero);foreach(var p in projected)bounds.Encapsulate(p);bounds.Expand(.22f);
          if(occupied.Any(b=>b.Intersects(bounds)))continue;occupied.Add(bounds);fit=true;break;
         }
         if(fit)break;
        }
        if(fit)break;
       }
       if(fit)break;UnityEngine.Object.DestroyImmediate(source);source=null;
      }
      if(!fit)continue;
      source.name=room.id+"_Inscripcion_"+i;source.vertices=projected;source.RecalculateNormals();source.RecalculateBounds();string meshPath=RuneAssets+"/"+source.name+".asset";if(File.Exists(meshPath))meshPath=AssetDatabase.GenerateUniqueAssetPath(meshPath);AssetDatabase.CreateAsset(source,meshPath);
      int color=RuneColorAt(colorIndex++);int energy=color<2?(i%4==0?11:i%4==1?5:8):(i%3==0?4:6);
      var inscription=new GameObject(source.name+"_"+RuneColors[color],typeof(MeshFilter),typeof(MeshRenderer));inscription.transform.SetParent(group.transform,false);inscription.GetComponent<MeshFilter>().sharedMesh=source;
      var renderer=inscription.GetComponent<MeshRenderer>();renderer.sharedMaterial=RuneMaterial(color,energy);renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
      rr.inscriptions[color]++;rr.characters+=chars;rr.contacts.Add(contact.point);
     }
     if(rr.inscriptions.Sum()==0)throw new InvalidOperationException("No hay superficie vertical completa para escritura; se conserva ambientación previa.");
     rr.lights=RuneRoomLights(room,group.transform,safe);rr.fogPatches=RuneRoomFog(room,group.transform,safe,cfg);RuneRetirePrevious(scene,room.id);rr.status="actualizado";
    }
    catch(Exception e)
    {
     if(group)UnityEngine.Object.DestroyImmediate(group);rr.status="omitido";rr.reason=e.Message;rr.inscriptions=new int[5];rr.characters=rr.lights=rr.fogPatches=0;
    }
    receipt.rooms.Add(rr);
   }
   Physics.SyncTransforms();receipt.physicsUnchanged=physics==A03Physics(scene);receipt.navUnchanged=Hash(Nav)==receipt.nav&&TriangleDigest4()==receipt.digest;
   if(!receipt.physicsUnchanged||!receipt.navUnchanged||root.GetComponentsInChildren<Collider>(true).Length>0)throw new InvalidOperationException("No se guarda: física/navegación alteradas.");
   EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new IOException("No se guardó el lote.");
   receipt.batches++;receipt.saved=true;receipt.persisted=false;receipt.sceneAfter=Hash(City);File.WriteAllText(path,JsonUtility.ToJson(receipt,true));
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);scene=SceneManager.GetSceneByPath(City);root=scene.GetRootGameObjects().Single(g=>g.name==RuneRoot);
   receipt.persisted=!scene.isDirty&&Hash(City)==receipt.sceneAfter&&Hash(Nav)==receipt.nav&&TriangleDigest4()==receipt.digest&&root.GetComponentsInChildren<Collider>(true).Length==0;
   foreach(var r in receipt.rooms.Where(x=>x.status=="actualizado"))
   {
    var g=root.transform.Find(r.id);receipt.persisted&=g&&g.GetComponentsInChildren<Light>(true).Length==r.lights&&g.GetComponentsInChildren<MeshRenderer>(true).Length==r.inscriptions.Sum()+(r.fogPatches>0?1:0);
    if(g)foreach(var m in g.GetComponentsInChildren<MeshRenderer>(true))receipt.persisted&=m.sharedMaterials.All(a=>a&&a.shader&&a.shader.isSupported&&!ShaderUtil.ShaderHasError(a.shader));
   }
   File.WriteAllText(path,JsonUtility.ToJson(receipt,true));if(!receipt.persisted)throw new InvalidOperationException("No se acreditó persistencia de runas.");
   Debug.Log("CIUDAD_RUNAS: lote "+receipt.batches+" guardado/recargado, "+receipt.rooms.Count(r=>r.status=="actualizado")+" espacios; sin cambios de física/NavMesh.");
   if(plan.rooms.Any(r=>!receipt.rooms.Any(x=>x.id==r.id)))File.WriteAllText(R21Work+"/Accion.pending","{\"action\":\"runes\"}");
  }
  static void FinishInteriorRunes(Scene scene)
  {
   if(scene.isDirty)throw new InvalidOperationException("No se descartan cambios sin guardar.");
   string path=RuneWork+"/Resultado.json";var receipt=JsonUtility.FromJson<RuneReceipt>(File.ReadAllText(path));
   string physics=A03Physics(scene);foreach(var r in receipt.rooms.Where(x=>x.status=="actualizado"))RuneRetirePrevious(scene,r.id);
   if(physics!=A03Physics(scene)||Hash(Nav)!=receipt.nav)throw new InvalidOperationException("Integridad alterada.");
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("No se guardó la sustitución de emblemas.");receipt.sceneAfter=Hash(City);
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);scene=SceneManager.GetSceneByPath(City);int oldVisible=0;
   foreach(var root in scene.GetRootGameObjects())
   {
    if(root.name==MassRoot||root.name==Batch02Root)
     foreach(var room in receipt.rooms.Where(x=>x.status=="actualizado"))
     {var g=root.transform.Find(room.id);if(g)oldVisible+=g.GetComponentsInChildren<MeshRenderer>(true).Count(m=>m.enabled);}
    if(root.name==A03Root||root.name==R21Root)
     oldVisible+=root.GetComponentsInChildren<MeshRenderer>(true).Count(m=>m.enabled&&(ExistingSymbolColor(m.name,root.name==A03Root)>=0||m.name.StartsWith("Simbolo_",StringComparison.Ordinal)));
   }
   receipt.persisted=receipt.persisted&&oldVisible==0&&!scene.isDirty&&Hash(Nav)==receipt.nav&&TriangleDigest4()==receipt.digest;
   File.WriteAllText(path,JsonUtility.ToJson(receipt,true));File.WriteAllText(RuneWork+"/Sustitucion.json","{\"oldSymbolsVisibleAfterReload\":"+oldVisible+",\"verified\":"+receipt.persisted.ToString().ToLowerInvariant()+"}");
   if(!receipt.persisted)throw new InvalidOperationException("No se verificó la retirada persistente de emblemas anteriores.");
   Debug.Log("CIUDAD_RUNAS_FINAL: sustitución persistente verificada; ningún emblema anterior visible, sin modificaciones de colliders/NavMesh.");
  }

  const string AtmosWork="Backups/CiudadDelSol/Interior/Atmosfera_20261118";
  const string AtmosAssets="Assets/ROM/Art/CiudadDelSol/Interior/Atmosfera";
  const string UniformWork="Backups/CiudadDelSol/Interior/Unificacion_20261119";
  const string UniformAssets=AtmosAssets+"/Unificada";
  const string BalanceWork="Backups/CiudadDelSol/Interior/BalanceVisual_20261119";
  static void BalanceInteriorVisual(Scene scene)
  {
   if(scene.isDirty||!File.Exists(BalanceWork+"/Antes_SHA256.json"))throw new InvalidOperationException("Se necesita respaldo y escena guardada.");
   if(scene.GetRootGameObjects().Any(g=>g.GetComponent<ROM.Presentation.CiudadDelSolWarriorInteriorVisual>()))throw new InvalidOperationException("El ajuste ya está instalado; no se acumulan intensidades ni componentes.");
   var plan=JsonUtility.FromJson<MassPlan>(File.ReadAllText(UniformWork+"/Espacios.json"));var root=scene.GetRootGameObjects().Single(g=>g.name==RuneRoot);
   string nav=Hash(Nav),digest=TriangleDigest4(),physics=A03Physics(scene);int walls=0,floors=0,cyan=0;
   var changes=new System.Text.StringBuilder();
   foreach(var room in plan.rooms)
   {
    var group=root.transform.Find(room.id);if(!group)throw new InvalidOperationException("Falta espacio "+room.id);
    foreach(var light in group.GetComponentsInChildren<Light>(true))
    {
     float before=light.intensity,rangeBefore=light.range;bool down=Vector3.Dot(light.transform.forward,Vector3.down)>.7f;bool cool=light.color.g>light.color.r;
     light.intensity*=down?1.08f:cool?1.12f:1.18f;
     float cap=room.center.y>=0?room.center.y+10-light.transform.position.y:-.3f-light.transform.position.y;
     if(!down)light.range=Mathf.Min(light.range*1.03f,cap);
     if(down)floors++;else walls++;if(cool)cyan++;
     changes.AppendLine(room.id+"|"+light.name+"|intensidad="+before+"→"+light.intensity+"|alcance="+rangeBefore+"→"+light.range);
    }
   }
   if(walls+floors!=78)throw new InvalidOperationException("La cantidad de luces difiere de las 78 aprobadas.");
   var holder=new GameObject("CiudadDelSol_IntegracionVisualWarrior");SceneManager.MoveGameObjectToScene(holder,scene);
   var visual=holder.AddComponent<ROM.Presentation.CiudadDelSolWarriorInteriorVisual>();
   float minX=plan.rooms.Min(r=>r.center.x-r.width*.5f),maxX=plan.rooms.Max(r=>r.center.x+r.width*.5f),minZ=plan.rooms.Min(r=>r.center.z-r.depth*.5f),maxZ=plan.rooms.Max(r=>r.center.z+r.depth*.5f);
   var footprint=new Bounds(V((minX+maxX)*.5f,-20,(minZ+maxZ)*.5f),V(maxX-minX,80,maxZ-minZ));
   var entries=plan.rooms.Where(r=>r.id=="A01"||r.id=="A02").Select(r=>new Bounds(r.center+Vector3.up,V(r.width,3,r.depth))).ToArray();
   // Verified D03 footprint also contains B01–B04; their playable floors are Y=-24..-38.
   visual.Configure(footprint,entries,new Bounds(V(18,-30,18),V(84,20,84)));
   if(visual.EvaluateAt(V(30,-1.903f,-206))!=0||visual.EvaluateAt(V(18,-38,18))!=0||visual.EvaluateAt(V(18,-24,18))!=0||visual.EvaluateAt(V(105,-34,63))<.99f)throw new InvalidOperationException("Fallo de ámbito visual exterior/interior/protegido.");
   if(physics!=A03Physics(scene)||nav!=Hash(Nav)||digest!=TriangleDigest4())throw new InvalidOperationException("Física/NavMesh alterados.");
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("No se guardó el ajuste.");string saved=Hash(City);
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);scene=SceneManager.GetSceneByPath(City);
   bool persisted=!scene.isDirty&&Hash(City)==saved&&Hash(Nav)==nav&&TriangleDigest4()==digest&&scene.GetRootGameObjects().Count(g=>g.GetComponent<ROM.Presentation.CiudadDelSolWarriorInteriorVisual>())==1;
   File.WriteAllText(BalanceWork+"/Luces.txt",changes.ToString());File.WriteAllText(BalanceWork+"/Resultado.json","{\"lights\":78,\"wallLights\":"+walls+",\"floorLights\":"+floors+",\"cyan\":"+cyan+",\"persisted\":"+persisted.ToString().ToLowerInvariant()+",\"sceneAfter\":\""+saved+"\",\"nav\":\""+nav+"\"}");
   if(!persisted)throw new InvalidOperationException("No se acreditó persistencia.");
  }
  [Serializable] sealed class UniformResult
  { public string id,status,reason;public int renderers,fogPatches;public bool originalFogPreserved; }
  [Serializable] sealed class UniformReceipt
  {
   public string sceneBefore,sceneAfter,nav,digest;public int batches,legacyHeightGateCandidates;
   public bool persisted,physicsUnchanged,navUnchanged;public List<UniformResult> rooms=new List<UniformResult>();
  }
  static Material UniformStone(Material source,bool upper)
  {
   if(!source)return source;string originalPath=AssetDatabase.GetAssetPath(source);
   if(originalPath.StartsWith(UniformAssets+"/",StringComparison.Ordinal))return source;
   if(source.shader.name=="ROM/CiudadDelSol/PiedraInterior")
   {
    var m=System.Text.RegularExpressions.Regex.Match(Path.GetFileNameWithoutExtension(originalPath),"^Piedra_([0-9a-f]{32})_Y");
    if(!m.Success)throw new InvalidOperationException("No se identifica material original: "+originalPath);
    source=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value));
   }
   if(!source||(source.shader.name!="Universal Render Pipeline/Simple Lit"&&source.shader.name!="Universal Render Pipeline/Lit"))return source;
   if(source.HasProperty("_EmissionColor")&&source.GetColor("_EmissionColor").maxColorComponent>.5f)return source;
   string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));string path=UniformAssets+"/Piedra_"+guid+(upper?"_Acceso":"_Interior")+".mat";
   var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat)return mat;
   mat=new Material(source){name="Piedra_uniforme_"+source.name};mat.shader=AssetDatabase.LoadAssetAtPath<Shader>(AtmosAssets+"/PiedraInterior.shader");
   mat.SetTexture("_InteriorGrime",AssetDatabase.LoadAssetAtPath<Texture2D>(AtmosAssets+"/Deterioro.png"));mat.SetFloat("_InteriorFloor",-16);mat.SetFloat("_InteriorMaxY",upper?12:-.05f);mat.SetFloat("_InteriorStrength",1);
   AssetDatabase.CreateAsset(mat,path);return mat;
  }
  static void ApplyUniformInterior(Scene scene)
  {
   if(scene.isDirty||!File.Exists(UniformWork+"/Antes_SHA256.json"))throw new InvalidOperationException("Se necesita respaldo verificado y escena guardada.");
   var shader=AssetDatabase.LoadAssetAtPath<Shader>(AtmosAssets+"/PiedraInterior.shader");if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Shader sin compilar.");
   Directory.CreateDirectory(UniformAssets);AssetDatabase.Refresh();
   var plan=JsonUtility.FromJson<MassPlan>(File.ReadAllText(UniformWork+"/Espacios.json"));string resultPath=UniformWork+"/Resultado.json";
   var receipt=File.Exists(resultPath)?JsonUtility.FromJson<UniformReceipt>(File.ReadAllText(resultPath)):new UniformReceipt{sceneBefore=Hash(City),nav=Hash(Nav),digest=TriangleDigest4()};
   var pending=plan.rooms.Where(r=>!receipt.rooms.Any(x=>x.id==r.id)).Take(8).ToArray();if(pending.Length==0)return;
   if(Hash(Nav)!=receipt.nav||TriangleDigest4()!=receipt.digest)throw new InvalidOperationException("Navegación modificada externamente.");
   string physics=A03Physics(scene);var cfg=ConstructionConfig4(scene);
   var renderers=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(m=>m.enabled&&!AtmosProtected(m.transform)).Select(m=>(renderer:m,bounds:m.bounds)).ToArray();
   if(receipt.batches==0)
    receipt.legacyHeightGateCandidates=renderers.Count(x=>x.renderer.sharedMaterials.Any(m=>m&&m.shader.name=="ROM/CiudadDelSol/PiedraInterior"&&x.bounds.max.y>m.GetFloat("_InteriorFloor")+7));
   var runeRoot=scene.GetRootGameObjects().Single(g=>g.name==RuneRoot);
   foreach(var room in pending)
   {
    var rr=new UniformResult{id=room.id,status="omitido"};
    try
    {
     float top=room.center.y>=0?12:Mathf.Min(-.01f,Mathf.Max(room.bounds.center.y+room.bounds.size.y*.5f,room.center.y+(room.center.y<=-30?32:18)));
     var envelope=new Bounds(V(room.center.x,(room.center.y-2.2f+top)*.5f,room.center.z),V(room.width+1.2f,top-room.center.y+2.2f,room.depth+1.2f));
     foreach(var candidate in renderers.Where(x=>x.bounds.Intersects(envelope)))
     {
      var old=candidate.renderer.sharedMaterials;bool whollyUnderground=candidate.bounds.max.y<0;
      var replacement=old.Select(m=>m&&m.shader.name=="Universal Render Pipeline/Lit"&&!whollyUnderground?m:UniformStone(m,room.center.y>=0)).ToArray();
      if(old.SequenceEqual(replacement))continue;candidate.renderer.sharedMaterials=replacement;PrefabUtility.RecordPrefabInstancePropertyModifications(candidate.renderer);rr.renderers++;
     }
     var group=runeRoot.transform.Find(room.id);if(!group)throw new InvalidOperationException("No existe el espacio ambientado.");
     var fog=group.Find("Niebla_local_baja");var fogRenderer=fog?fog.GetComponent<MeshRenderer>():null;
     if(fogRenderer&&fogRenderer.sharedMaterial&&fogRenderer.sharedMaterial.shader.name=="ROM/CiudadDelSol/NieblaBaja")
     {
      rr.originalFogPreserved=true;var old=JsonUtility.FromJson<AtmosReceipt>(File.ReadAllText(AtmosWork+"/Resultado.json"));rr.fogPatches=old.rooms.Single(r=>r.id==room.id).fogPatches;
     }
     else rr.fogPatches=AtmosFog(room,group,cfg,true);
     rr.status="actualizado";if(rr.fogPatches==0)rr.reason="Sin huella plana suficiente para niebla; no se cubren escaleras ni bordes.";
    }
    catch(Exception e){rr.reason=e.Message;}
    receipt.rooms.Add(rr);
   }
   Physics.SyncTransforms();receipt.physicsUnchanged=physics==A03Physics(scene);receipt.navUnchanged=Hash(Nav)==receipt.nav&&TriangleDigest4()==receipt.digest;
   if(!receipt.physicsUnchanged||!receipt.navUnchanged)throw new InvalidOperationException("No se guarda: física/NavMesh alterados.");
   EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new IOException("No se guardó el lote.");
   receipt.batches++;receipt.sceneAfter=Hash(City);receipt.persisted=false;File.WriteAllText(resultPath,JsonUtility.ToJson(receipt,true));
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);scene=SceneManager.GetSceneByPath(City);
   receipt.persisted=!scene.isDirty&&Hash(City)==receipt.sceneAfter&&Hash(Nav)==receipt.nav&&TriangleDigest4()==receipt.digest;
   File.WriteAllText(resultPath,JsonUtility.ToJson(receipt,true));if(!receipt.persisted)throw new InvalidOperationException("Persistencia no acreditada.");
   Debug.Log("CIUDAD_UNIFICADA: "+receipt.rooms.Count(x=>x.status=="actualizado")+" espacios guardados/recargados; runas y navegación conservadas.");
   if(plan.rooms.Any(r=>!receipt.rooms.Any(x=>x.id==r.id)))File.WriteAllText(R21Work+"/Accion.pending","{\"action\":\"uniform\"}");
  }
  static void FinishUniformConnections(Scene scene)
  {
   if(scene.isDirty)throw new InvalidOperationException("Se conserva cualquier cambio no guardado.");
   string path=UniformWork+"/Resultado.json";var receipt=JsonUtility.FromJson<UniformReceipt>(File.ReadAllText(path));string physics=A03Physics(scene);var cfg=ConstructionConfig4(scene);var audit=new System.Text.StringBuilder();int updated=0;
   // Probe the actual gold strip seen north of G05, including the spaces between room rectangles.
   foreach(float z in new[]{132f,144f,156f,168f,180f,192f})
    if(cfg.physics.Raycast(V(75,-15.8f,z),Vector3.down,out var hit,.5f,cfg.mask,QueryTriggerInteraction.Ignore))
    {var m=hit.collider.GetComponent<MeshRenderer>();audit.AppendLine("ANTES G05 x75 z"+z+"|"+Hierarchy(hit.collider.transform)+"|"+(m?string.Join(",",m.sharedMaterials.Where(a=>a).Select(a=>a.shader.name)):"sin Renderer"));}
   foreach(var root in scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Subsuelo_",StringComparison.Ordinal)||g.name=="Nucleo_Monumental_Conjunto"))
    foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(m=>m.enabled&&!AtmosProtected(m.transform)))
    {
     var original=renderer.sharedMaterials;bool upper=root.name=="Subsuelo_Acceso_Etapa01"&&renderer.bounds.max.y>0;
     var replacement=original.Select(m=>m&&m.shader.name=="Universal Render Pipeline/Lit"&&renderer.bounds.max.y>=0?m:UniformStone(m,upper)).ToArray();
     if(original.SequenceEqual(replacement))continue;renderer.sharedMaterials=replacement;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);updated++;
    }
   if(physics!=A03Physics(scene)||Hash(Nav)!=receipt.nav||TriangleDigest4()!=receipt.digest)throw new InvalidOperationException("Física o NavMesh alterados.");
   EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new IOException("No se guardó continuidad visual.");receipt.sceneAfter=Hash(City);
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);scene=SceneManager.GetSceneByPath(City);receipt.persisted=!scene.isDirty&&Hash(City)==receipt.sceneAfter&&Hash(Nav)==receipt.nav&&TriangleDigest4()==receipt.digest;
   File.WriteAllText(path,JsonUtility.ToJson(receipt,true));audit.AppendLine("Piezas adicionales de enlace/techo="+updated);File.WriteAllText(UniformWork+"/Continuidad.txt",audit.ToString());
   if(!receipt.persisted)throw new InvalidOperationException("No se acreditó persistencia final.");
  }
  [Serializable] sealed class AtmosRoomResult
  { public string id,status,reason;public int renderers,materials,lights,fogPatches;public bool localNavigation; }
  [Serializable] sealed class AtmosReceipt
  {
   public string sceneBefore,sceneAfter,nav,digest;public int batches;
   public bool saved,persisted,physicsUnchanged,navUnchanged;
   public List<AtmosRoomResult> rooms=new List<AtmosRoomResult>();
  }
  static bool AtmosProtected(Transform t)=>Hierarchy(t).Contains("03 - El Sol Invertido V3")||Hierarchy(t).StartsWith(RuneRoot+"/",StringComparison.Ordinal);
  static Material AtmosStone(Material source,float floor)
  {
   if(!source||(source.shader.name!="Universal Render Pipeline/Simple Lit"&&source.shader.name!="Universal Render Pipeline/Lit"))return source;
   if(source.HasProperty("_EmissionColor")&&source.GetColor("_EmissionColor").maxColorComponent>.5f)return source;
   string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));if(guid.Length==0)throw new InvalidOperationException("Material fuente sin asset.");
   string path=AtmosAssets+"/Piedra_"+guid+"_Y"+floor.ToString("0",System.Globalization.CultureInfo.InvariantCulture)+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat)return mat;
   var shader=AssetDatabase.LoadAssetAtPath<Shader>(AtmosAssets+"/PiedraInterior.shader");
   if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Shader interior con errores.");
   mat=new Material(source){name="Interior_"+source.name+"_Y"+floor};mat.shader=shader;
   mat.SetTexture("_InteriorGrime",AssetDatabase.LoadAssetAtPath<Texture2D>(AtmosAssets+"/Deterioro.png"));mat.SetFloat("_InteriorFloor",floor);mat.SetFloat("_InteriorMaxY",floor>=0?floor+8:-.05f);
   AssetDatabase.CreateAsset(mat,path);return mat;
  }
  static void AtmosLights(MassRoom room,Scene scene,Config cfg)
  {
   var root=scene.GetRootGameObjects().Single(g=>g.name==RuneRoot).transform.Find(room.id);if(!root)return;
   int i=0;foreach(var light in root.GetComponentsInChildren<Light>(true))
   {
    // Lift light off the ground and wash the real vault/pilasters rather than only the floor.
    var old=light.transform.position;var st=Probe(V(old.x,room.center.y,old.z),cfg,false,.35f);if(!st.Clear||!st.nav)continue;
    var origin=st.floor+Vector3.up*3.2f;var target=origin+Vector3.up*6;
    float distance=float.MaxValue;
    if(i%2==0)foreach(var dir in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
    {
     if(cfg.physics.Raycast(origin+Vector3.up*1.5f,dir,out var wall,14,cfg.mask,QueryTriggerInteraction.Ignore)&&Mathf.Abs(wall.normal.y)<.3f&&wall.distance<distance)
     {target=wall.point+wall.normal*.2f;distance=wall.distance;}
    }
    else target=st.floor+Vector3.up*.5f;
    if(i%2==0&&cfg.physics.Raycast(origin,Vector3.up,out var roof,14,cfg.mask,QueryTriggerInteraction.Ignore)&&distance==float.MaxValue)target=roof.point-Vector3.up*.2f;
    float range=Mathf.Clamp(Vector3.Distance(origin,target)+1.3f,7,14);
    if(room.center.y<0)range=Mathf.Min(range,-.3f-origin.y);else range=Mathf.Min(range,room.center.y+10-origin.y);
    if(range<3)continue;
    light.transform.position=origin;light.transform.rotation=Quaternion.LookRotation(target-origin);
    light.range=range;light.spotAngle=i%2==0?105:125;light.innerSpotAngle=42;light.intensity=room.center.y<=-30?62:48;light.shadows=LightShadows.None;
    light.color=i==2?new Color(.07f,.64f,1):new Color(.54f,.07f,1);i++;
   }
   // Only these fixture lights belong exclusively to the allowed rooms.
   foreach(var g in scene.GetRootGameObjects().Where(g=>(room.id=="A03"&&g.name==A03Root)||(room.id=="R21"&&g.name==R21Root)))
    foreach(var l in g.GetComponentsInChildren<Light>(true))if(l.color.r>l.color.b){l.intensity=Mathf.Min(l.intensity,.65f);PrefabUtility.RecordPrefabInstancePropertyModifications(l);}
  }
  static int AtmosFog(MassRoom room,Transform group,Config cfg,bool uniform=false)
  {
   bool special=room.id=="R30"||room.id=="M04";if(!uniform&&room.center.y>-30&&!special)return 0;
   string path=(uniform?UniformAssets:AtmosAssets)+"/Niebla_gris_violeta"+(room.center.y<=-30?"_profunda":room.center.y<=-24?"_media":"")+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!mat){var shader=AssetDatabase.LoadAssetAtPath<Shader>(AtmosAssets+"/NieblaBaja.shader");if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Shader de niebla con errores.");mat=new Material(shader){name="Niebla_gris_violeta"};mat.SetColor("_Color",new Color(.40f,.29f,.48f,room.center.y<=-30?.34f:room.center.y<=-24?.30f:.24f));AssetDatabase.CreateAsset(mat,path);}
   var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();var used=new List<Vector3>();
   Action<Vector3,Vector3,Vector3,Vector3> quad=(a,b,c,d)=>{int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});};
   foreach(var p in new[]{room.seed}.Concat(room.sites))
   {
    var st=Probe(p,cfg,false,.35f);if(!st.Clear||!st.nav||st.slope>1||st.navEdge<(uniform?1.8f:3.7f)||used.Any(q=>Vector3.Distance(q,st.floor)<(uniform?3:4)))continue;
    // Smaller mist footprints fit narrow safe floors without approaching their NavMesh edges.
    float radius=Mathf.Min(3,st.navEdge-(uniform?.9f:1.2f));bool flat=true;
    foreach(float x in new[]{-radius,-radius/2,0,radius/2,radius})foreach(float z in new[]{-radius,-radius/2,0,radius/2,radius})
     if(!cfg.physics.Raycast(st.floor+V(x,.25f,z),Vector3.down,out var h,.5f,cfg.mask,QueryTriggerInteraction.Ignore)||h.normal.y<.99f||Mathf.Abs(h.point.y-st.floor.y)>.03f)flat=false;
    if(!flat)continue;var q0=st.floor;
    float height=uniform?(room.center.y<=-30?.85f:room.center.y<=-24?.65f:.5f):.85f;
    foreach(float y in new[]{height*.16f,height*.5f})quad(q0+V(-radius,y,-radius),q0+V(-radius,y,radius),q0+V(radius,y,radius),q0+V(radius,y,-radius));
    quad(q0+V(-radius,.02f,0),q0+V(-radius,height,0),q0+V(radius,height,0),q0+V(radius,.02f,0));
    quad(q0+V(0,.02f,-radius),q0+V(0,height,-radius),q0+V(0,height,radius),q0+V(0,.02f,radius));used.Add(q0);
    int limit=uniform?(room.center.y<=-30?32:room.center.y<=-24?18:Mathf.Max(room.width,room.depth)>40?12:8):(special?12:32);
    if(used.Count==limit)break;
   }
   if(used.Count==0)return 0;var mesh=new Mesh{name=room.id+(uniform?"_Niebla_uniforme":"_Niebla_ampliada")};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,(uniform?UniformAssets:AtmosAssets)+"/"+mesh.name+".asset");
   var fog=group.Find("Niebla_local_baja");if(!fog){var g=new GameObject("Niebla_local_baja",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(group,false);fog=g.transform;}
   fog.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=fog.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;return used.Count;
  }
  static void ApplyInteriorAtmosphere(Scene scene)
  {
   if(scene.isDirty||!File.Exists(AtmosWork+"/Antes_SHA256.json"))throw new InvalidOperationException("Se necesita respaldo y escena guardada.");
   var shader=AssetDatabase.LoadAssetAtPath<Shader>(AtmosAssets+"/PiedraInterior.shader");var fogShader=AssetDatabase.LoadAssetAtPath<Shader>(AtmosAssets+"/NieblaBaja.shader");
   if(!shader||!fogShader)throw new InvalidOperationException("Faltan shaders locales.");
   if(ShaderUtil.ShaderHasError(shader)||ShaderUtil.ShaderHasError(fogShader))throw new InvalidOperationException(string.Join(";",new[]{shader,fogShader}.SelectMany(x=>ShaderUtil.GetShaderMessages(x)).Select(x=>x.message+" línea "+x.line)));
   var textureImporter=AssetImporter.GetAtPath(AtmosAssets+"/Deterioro.png") as TextureImporter;
   if(textureImporter&&textureImporter.sRGBTexture){textureImporter.sRGBTexture=false;textureImporter.wrapMode=TextureWrapMode.Repeat;textureImporter.SaveAndReimport();}
   string path=AtmosWork+"/Resultado.json";var plan=JsonUtility.FromJson<MassPlan>(File.ReadAllText(AtmosWork+"/Espacios.json"));
   var receipt=File.Exists(path)?JsonUtility.FromJson<AtmosReceipt>(File.ReadAllText(path)):new AtmosReceipt{sceneBefore=Hash(City),nav=Hash(Nav),digest=TriangleDigest4()};
   var pending=plan.rooms.Where(r=>!receipt.rooms.Any(x=>x.id==r.id)).Take(8).ToArray();if(pending.Length==0)return;
   string physics=A03Physics(scene);var cfg=ConstructionConfig4(scene);var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   var roots=plan.rooms.Select(r=>(room:r,root:transforms.SingleOrDefault(t=>Hierarchy(t).Replace('×','?').Replace('\uFFFD','?')==r.hierarchy.Replace('×','?').Replace('\uFFFD','?')))).Where(p=>p.root).ToArray();
   var renderers=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(m=>m.enabled&&!AtmosProtected(m.transform)).Select(m=>(renderer:m,bounds:m.bounds)).ToArray();
   var runeRoot=scene.GetRootGameObjects().Single(g=>g.name==RuneRoot);
   foreach(var room in pending)
   {
    var rr=new AtmosRoomResult{id=room.id,status="omitido"};
    try
    {
     var architecture=roots.Single(x=>x.room.id==room.id).root;
     rr.localNavigation=MassLocalNav(room.seed,room.seed);if(!rr.localNavigation){var st=room.sites.FirstOrDefault(p=>MassLocalNav(p,p));rr.localNavigation=st!=Vector3.zero;}
     if(!rr.localNavigation)throw new InvalidOperationException("No se acredita muestra del nivel.");
     var owned=architecture.GetComponentsInChildren<MeshRenderer>(true).Where(m=>m.enabled&&!AtmosProtected(m.transform)&&!roots.Any(o=>o.room.id!=room.id&&o.root.IsChildOf(architecture)&&m.transform.IsChildOf(o.root)));
     var floors=renderers.Where(x=>x.bounds.max.y>=room.center.y-2.2f&&x.bounds.max.y<=room.center.y+.15f&&Mathf.Abs(x.bounds.center.x-room.center.x)<=room.width*.5f&&Mathf.Abs(x.bounds.center.z-room.center.z)<=room.depth*.5f).Select(x=>x.renderer);
     foreach(var m in owned.Concat(floors).Distinct())
     {
      var old=m.sharedMaterials;var changed=old.Select(mat=>AtmosStone(mat,room.center.y)).ToArray();if(old.SequenceEqual(changed))continue;
      m.sharedMaterials=changed;PrefabUtility.RecordPrefabInstancePropertyModifications(m);rr.renderers++;rr.materials+=old.Zip(changed,(a,b)=>a!=b?1:0).Sum();
     }
     AtmosLights(room,scene,cfg);var group=runeRoot.transform.Find(room.id);rr.lights=group.GetComponentsInChildren<Light>(true).Length;rr.fogPatches=AtmosFog(room,group,cfg);rr.status="actualizado";
    }
    catch(Exception e){rr.reason=e.Message;}
    receipt.rooms.Add(rr);
   }
   Physics.SyncTransforms();receipt.physicsUnchanged=physics==A03Physics(scene);receipt.navUnchanged=Hash(Nav)==receipt.nav&&TriangleDigest4()==receipt.digest;
   if(!receipt.physicsUnchanged||!receipt.navUnchanged)throw new InvalidOperationException("No se guarda: física/NavMesh alterados.");
   EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new IOException("No se guardó el lote.");
   receipt.saved=true;receipt.persisted=false;receipt.sceneAfter=Hash(City);receipt.batches++;File.WriteAllText(path,JsonUtility.ToJson(receipt,true));
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);scene=SceneManager.GetSceneByPath(City);
   receipt.persisted=!scene.isDirty&&Hash(City)==receipt.sceneAfter&&Hash(Nav)==receipt.nav&&TriangleDigest4()==receipt.digest;
   File.WriteAllText(path,JsonUtility.ToJson(receipt,true));if(!receipt.persisted)throw new InvalidOperationException("No se acreditó persistencia.");
   Debug.Log("CIUDAD_ATMOSFERA: "+receipt.rooms.Count(x=>x.status=="actualizado")+" espacios guardados/recargados; sin cambios de física/NavMesh.");
   if(plan.rooms.Any(r=>!receipt.rooms.Any(x=>x.id==r.id)))File.WriteAllText(R21Work+"/Accion.pending","{\"action\":\"atmosphere\"}");
  }
  static void BeginAtmospherePlay(Scene scene)
  {
   if(scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Se requiere escena guardada y Edit Mode.");
   SessionState.SetBool("ROM.Atmosphere.Play",true);SessionState.SetFloat("ROM.Atmosphere.Start",(float)EditorApplication.timeSinceStartup);EditorApplication.EnterPlaymode();
  }
  static void FinishInteriorAtmosphere(Scene scene)
  {
   if(scene.isDirty)throw new InvalidOperationException("No se descartan cambios pendientes.");
   var plan=JsonUtility.FromJson<MassPlan>(File.ReadAllText(AtmosWork+"/Espacios.json"));var cfg=ConstructionConfig4(scene);string physics=A03Physics(scene),nav=Hash(Nav);var text=new System.Text.StringBuilder();
   int changed=0;var candidates=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(m=>m.enabled&&!AtmosProtected(m.transform)).Select(m=>(renderer:m,bounds:m.bounds)).ToArray();
   foreach(var room in plan.rooms)
   {
    // Entrance/platform roots alone do not own every surrounding arch or wall.
    // Include nearby original architecture, while retaining the protected hierarchy exclusion.
    foreach(var candidate in candidates.Where(x=>Mathf.Abs(x.bounds.center.x-room.center.x)<=room.width*.5f+1&&Mathf.Abs(x.bounds.center.z-room.center.z)<=room.depth*.5f+1&&x.bounds.max.y>=room.center.y-.1f&&x.bounds.min.y<=room.center.y+6&&x.bounds.center.y<=room.center.y+12))
    {
     var original=candidate.renderer.sharedMaterials;var replacement=original.Select(m=>AtmosStone(m,room.center.y)).ToArray();
     if(!original.SequenceEqual(replacement)){candidate.renderer.sharedMaterials=replacement;PrefabUtility.RecordPrefabInstancePropertyModifications(candidate.renderer);changed++;}
    }
    RaycastHit floor=default;bool found=false;
    foreach(var p in new[]{room.seed}.Concat(room.sites))if(cfg.physics.Raycast(p+Vector3.up*.15f,Vector3.down,out floor,.45f,cfg.mask,QueryTriggerInteraction.Ignore)&&floor.normal.y>.99f){found=true;break;}
    if(!found){text.AppendLine(room.id+"|sin apoyo local identificado");continue;}
    var renderer=floor.collider.GetComponent<MeshRenderer>();
    if(renderer&&!AtmosProtected(renderer.transform)&&renderer.bounds.max.y<=room.center.y+.15f)
    {
     var original=renderer.sharedMaterials;var replacement=original.Select(m=>AtmosStone(m,room.center.y)).ToArray();
     if(!original.SequenceEqual(replacement)){renderer.sharedMaterials=replacement;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);changed++;}
    }
    text.AppendLine(room.id+"|"+Hierarchy(floor.collider.transform)+"|"+(renderer?string.Join(",",renderer.sharedMaterials.Where(m=>m).Select(m=>m.shader.name)):"collider sin Renderer propio"));
   }
   if(physics!=A03Physics(scene)||nav!=Hash(Nav))throw new InvalidOperationException("Física/NavMesh alterados.");
   EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new IOException("No se guardaron los apoyos.");
   var receipt=JsonUtility.FromJson<AtmosReceipt>(File.ReadAllText(AtmosWork+"/Resultado.json"));receipt.sceneAfter=Hash(City);
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);scene=SceneManager.GetSceneByPath(City);receipt.persisted=!scene.isDirty&&Hash(City)==receipt.sceneAfter&&Hash(Nav)==receipt.nav&&TriangleDigest4()==receipt.digest;
   File.WriteAllText(AtmosWork+"/Resultado.json",JsonUtility.ToJson(receipt,true));File.WriteAllText(AtmosWork+"/Apoyos.txt",text.ToString()+"Apoyos adicionales actualizados="+changed);
   if(!receipt.persisted)throw new InvalidOperationException("Persistencia final no acreditada.");
  }
  [Serializable] sealed class AtmosPixels
  { public float luminance,yellowFraction,violetFraction; }
  static AtmosPixels AtmosMeasure(Camera camera,Vector3 eye,Vector3 target)
  {
   var position=camera.transform.position;var rotation=camera.transform.rotation;var oldActive=RenderTexture.active;
   var rt=RenderTexture.GetTemporary(320,180,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Texture2D texture=null;
   try
   {
    camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));
    var request=new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt};
    if(!UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("La cámara no admite la lectura numérica solicitada.");
    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,request);RenderTexture.active=rt;
    texture=new Texture2D(320,180,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,320,180),0,0);texture.Apply();
    var values=texture.GetPixels32();float lum=0;int yellow=0,violet=0;
    foreach(var c in values){lum+=(.2126f*c.r+.7152f*c.g+.0722f*c.b)/255;if(c.r>c.b*1.2f&&c.g>c.b*1.1f&&c.r>50)yellow++;if(c.b>c.g*1.22f&&c.r>c.g*1.1f&&c.b>45)violet++;}
    return new AtmosPixels{luminance=lum/values.Length,yellowFraction=(float)yellow/values.Length,violetFraction=(float)violet/values.Length};
   }
   finally{camera.transform.SetPositionAndRotation(position,rotation);RenderTexture.active=oldActive;if(texture)UnityEngine.Object.DestroyImmediate(texture);RenderTexture.ReleaseTemporary(rt);}
  }
  static void AtmosPlayMeasurements(Camera camera)
  {
   var scene=SceneManager.GetSceneByPath(City);var plan=JsonUtility.FromJson<MassPlan>(File.ReadAllText(AtmosWork+"/Espacios.json"));
   var renderers=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(m=>m.sharedMaterials.Any(a=>a&&a.shader.name=="ROM/CiudadDelSol/PiedraInterior")).ToArray();
   var originals=renderers.Select(m=>m.sharedMaterials).ToArray();var clones=new Dictionary<Material,Material>();var text=new System.Text.StringBuilder();
   try
   {
    foreach(var mat in originals.SelectMany(a=>a).Where(m=>m&&m.shader.name=="ROM/CiudadDelSol/PiedraInterior").Distinct()){var clone=new Material(mat);clone.SetFloat("_InteriorStrength",0);clones.Add(mat,clone);}
    foreach(string id in new[]{"A03","G05","R30","M04","D02"})
    {
     var room=plan.rooms.Single(r=>r.id==id);var eye=room.seed+Vector3.up*1.7f;var direction=room.depth>=room.width?Vector3.forward:Vector3.right;var target=eye+direction*12+Vector3.up*.2f;
     var current=AtmosMeasure(camera,eye,target);
     for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=originals[i].Select(m=>m&&clones.TryGetValue(m,out var clone)?clone:m).ToArray();
     AtmosPixels baseline;
     try{baseline=AtmosMeasure(camera,eye,target);}finally{for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=originals[i];}
     text.AppendLine(id+"|tratamiento="+JsonUtility.ToJson(current)+"|sombreado_base_mismas_luces="+JsonUtility.ToJson(baseline));
    }
    text.AppendLine("Lectura de píxeles en memoria desde la cámara jugable real; sin imágenes guardadas. Comparación aislada del sombreado, no una prueba artística ni un benchmark FPS.");
    var pipeline=UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
    text.AppendLine("frame="+Time.frameCount+"; drawCalls="+UnityStats.drawCalls+"; batches="+UnityStats.batches+"; deltaTime="+Time.unscaledDeltaTime+"; quality="+QualitySettings.names[QualitySettings.GetQualityLevel()]+"; pipeline="+(pipeline?pipeline.name:"null")+"; additional="+(pipeline?pipeline.additionalLightsRenderingMode.ToString():"none"));
    File.WriteAllText(AtmosWork+"/Play_mediciones.txt",text.ToString());
   }
   finally{for(int i=0;i<renderers.Length;i++)if(renderers[i])renderers[i].sharedMaterials=originals[i];foreach(var m in clones.Values)UnityEngine.Object.DestroyImmediate(m);}
  }
  [InitializeOnLoadMethod] static void InstallAtmospherePlay(){EditorApplication.update-=AtmospherePlayTick;EditorApplication.update+=AtmospherePlayTick;}
  static bool uniformReviewStarted,uniformReviewReport;
  static double uniformReviewSettle;
  static Vector3 uniformReviewOrigin;
  static float uniformReviewYaw,uniformReviewPitch,uniformReviewDistance,uniformReviewActorYaw;
  static readonly string[] UniformReviewRooms={"D01","G05","R30","M04","D02","A03","B06"};
  static void UniformReviewTick(MultiFPS.Gameplay.CharacterInstance actor,Camera camera)
  {
   var input=MultiFPS.Gameplay.RiftMMOController.Instance;if(!input||input.Actor!=actor)return;
   string requestPath=UniformWork+"/Vista.pending";R21Request request=null;
   if(File.Exists(requestPath)){request=JsonUtility.FromJson<R21Request>(File.ReadAllText(requestPath));File.Move(requestPath,requestPath+".consumed."+DateTime.UtcNow.Ticks);}
   var visual=SceneManager.GetSceneByPath(City).GetRootGameObjects().Select(g=>g.GetComponent<ROM.Presentation.CiudadDelSolWarriorInteriorVisual>()).FirstOrDefault(v=>v);
   if(request!=null&&(request.action=="original"||request.action=="balanced"))
   {
    if(!visual)throw new InvalidOperationException("Falta la presentación local del Warrior.");
    visual.PreviewOriginal(request.action=="original");uniformReviewSettle=EditorApplication.timeSinceStartup+1.5;uniformReviewReport=true;request=null;
   }
   if(request!=null&&request.action=="exterior"&&uniformReviewStarted)
   {
    actor.ServerTeleport(uniformReviewOrigin,uniformReviewActorYaw);input.CameraYaw=uniformReviewYaw;input.CameraPitch=uniformReviewPitch;input.CameraDistance=uniformReviewDistance;
    SessionState.SetString("ROM.Uniform.Room","EXTERIOR");uniformReviewSettle=EditorApplication.timeSinceStartup+2;uniformReviewReport=true;request=null;
   }
   if(!uniformReviewStarted)
   {
    uniformReviewStarted=true;uniformReviewOrigin=actor.transform.position;uniformReviewActorYaw=actor.transform.eulerAngles.y;
    uniformReviewYaw=input.CameraYaw;uniformReviewPitch=input.CameraPitch;uniformReviewDistance=input.CameraDistance;
    request=request??new R21Request{action="view",view=0};
   }
   if(request!=null&&request.action=="end")
   {
    actor.ServerTeleport(uniformReviewOrigin,uniformReviewActorYaw);input.CameraYaw=uniformReviewYaw;input.CameraPitch=uniformReviewPitch;input.CameraDistance=uniformReviewDistance;
    SessionState.SetBool("ROM.Uniform.Review",false);SessionState.SetBool("ROM.Atmosphere.Play",false);uniformReviewStarted=false;EditorApplication.ExitPlaymode();return;
   }
   if(request!=null)
   {
    string id=UniformReviewRooms[Mathf.Clamp(request.view,0,UniformReviewRooms.Length-1)];var plan=JsonUtility.FromJson<MassPlan>(File.ReadAllText(UniformWork+"/Espacios.json"));var room=plan.rooms.Single(r=>r.id==id);var cfg=ConstructionConfig4(SceneManager.GetSceneByPath(City));Station chosen=null;
    foreach(var p in new[]{room.seed}.Concat(room.sites)){var st=Probe(p,cfg,false,.35f);if(st.Clear&&st.nav){chosen=st;break;}}
    if(chosen==null)throw new InvalidOperationException("Sin posición segura para revisar "+id);
    // Temporary visual-test setup through the existing authoritative API; not a route/walking test.
    float yaw=room.depth>=room.width?0:90;actor.ServerTeleport(chosen.floor+Vector3.up*.03f,yaw);input.CameraYaw=yaw;input.CameraPitch=12;input.CameraDistance=5.5f;
    SessionState.SetString("ROM.Uniform.Room",id);uniformReviewSettle=EditorApplication.timeSinceStartup+2;uniformReviewReport=true;
    var gameType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");if(gameType!=null)EditorWindow.GetWindow(gameType).Focus();
   }
   if(uniformReviewReport&&EditorApplication.timeSinceStartup>uniformReviewSettle)
   {
    uniformReviewReport=false;File.WriteAllText(UniformWork+"/RevisionPlay.txt","Sala="+SessionState.GetString("ROM.Uniform.Room","")+"; frame="+Time.frameCount+"; Host="+Mirror.NetworkServer.active+"; Warrior="+actor.name+"; posición="+actor.transform.position+"; cámara real="+camera.name+" "+camera.transform.position+"; HP="+actor.Health.CurrentHealth+"; drawCalls="+UnityStats.drawCalls+"; batches="+UnityStats.batches+"; deltaTime="+Time.unscaledDeltaTime+"\nReposición temporal mediante ServerTeleport original; no acredita recorrido ni cuatro jugadores. Sin archivos de imagen.\n");
    if(visual)File.AppendAllText(BalanceWork+"/Warrior_Play.txt",SessionState.GetString("ROM.Uniform.Room","")+"|"+visual.Describe()+"\n");
   }
  }
  static void AtmospherePlayTick()
  {
   if(!SessionState.GetBool("ROM.Atmosphere.Play",false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
   float elapsed=(float)EditorApplication.timeSinceStartup-SessionState.GetFloat("ROM.Atmosphere.Start",0);
   if(elapsed>(SessionState.GetBool("ROM.Uniform.Review",false)?300:120)){SessionState.SetBool("ROM.Atmosphere.Play",false);SessionState.SetBool("ROM.Uniform.Review",false);File.WriteAllText(AtmosWork+"/Play.txt","Tiempo agotado; no se acredita inspección visual.");if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
   if(!EditorApplication.isPlaying||Time.frameCount<30)return;
   var actor=MultiFPS.UI.ClientFrontend.OwnedCharacterInstance;var gameplay=MultiFPS.GameplayCamera._instance;
   if(!actor||!gameplay||!Mirror.NetworkServer.active)return;
   var camera=gameplay.GetComponent<Camera>();if(!camera)return;
   if(SessionState.GetBool("ROM.Uniform.Review",false)){UniformReviewTick(actor,camera);return;}
   File.WriteAllText(AtmosWork+"/Play.txt","Play real: frame="+Time.frameCount+"; server="+Mirror.NetworkServer.active+"; Warrior="+actor.name+"; camera="+camera.name+"; rendering="+camera.enabled+"; NO se acredita aún composición artística.\n");
   SessionState.SetBool("ROM.Atmosphere.Play",false);
   try{AtmosPlayMeasurements(camera);}catch(Exception e){File.AppendAllText(AtmosWork+"/Play.txt","Medición pendiente: "+e+"\n");}
   finally{EditorApplication.ExitPlaymode();}
  }
  static void InspectInteriorAtmosphere(Scene scene)
  {
   var plan=JsonUtility.FromJson<MassPlan>(File.ReadAllText(AtmosWork+"/Espacios.json"));
   var lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Where(l=>l.enabled&&l.gameObject.activeInHierarchy).ToArray();
   var text=new System.Text.StringBuilder();text.AppendLine("AmbientMode="+RenderSettings.ambientMode+"; ambientIntensity="+RenderSettings.ambientIntensity+"; sky="+RenderSettings.ambientSkyColor+"; equator="+RenderSettings.ambientEquatorColor+"; ground="+RenderSettings.ambientGroundColor+"; skybox="+(RenderSettings.skybox?AssetDatabase.GetAssetPath(RenderSettings.skybox):"none"));
   text.AppendLine(RuneBloomState(scene));
   foreach(var l in lights.Where(l=>l.type==LightType.Directional||l.color.r>l.color.b||l.transform.position.y<10))text.AppendLine("LIGHT "+Hierarchy(l.transform)+"|"+l.type+"|pos="+l.transform.position+"|color="+l.color+"|intensity="+l.intensity+"|range="+l.range+"|shadows="+l.shadows+"|mask="+l.cullingMask);
   var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   foreach(var room in plan.rooms.Where(r=>new[]{"G05","R30","M04","D01","D02","A03","R21"}.Contains(r.id)))
   {
    var root=all.SingleOrDefault(t=>Hierarchy(t).Replace('×','?').Replace('\uFFFD','?')==room.hierarchy.Replace('×','?').Replace('\uFFFD','?'));if(!root)continue;
    foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))foreach(var mat in renderer.sharedMaterials.Where(m=>m))
     text.AppendLine("MAT "+room.id+"|"+Hierarchy(renderer.transform)+"|bounds="+renderer.bounds+"|"+AssetDatabase.GetAssetPath(mat)+"|shader="+mat.shader.name+"|color="+(mat.HasProperty("_BaseColor")?mat.GetColor("_BaseColor").ToString():"none")+"|emission="+(mat.HasProperty("_EmissionColor")?mat.GetColor("_EmissionColor").ToString():"none"));
   }
   File.WriteAllText(AtmosWork+"/Fuentes.txt",text.ToString());
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
