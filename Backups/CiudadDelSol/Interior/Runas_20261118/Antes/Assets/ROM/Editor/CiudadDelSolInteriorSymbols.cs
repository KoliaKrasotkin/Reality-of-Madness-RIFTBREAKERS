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
