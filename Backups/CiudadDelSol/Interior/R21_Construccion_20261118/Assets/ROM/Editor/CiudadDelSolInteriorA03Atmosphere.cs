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
  const string A03Work="Backups/CiudadDelSol/Interior/A03_Construccion_20261118";
  const string A03Assets="Assets/ROM/Art/CiudadDelSol/Interior/A03";
  const string A03Root="CiudadDelSol_A03_UmbralDelSilencio";
  [Serializable] sealed class A03Request {public string action; public int view;}
  [Serializable] sealed class A03Object {public string path,type;public Vector3 center,size;public string[] materials;}
  [Serializable] sealed class A03Face {public Vector3 requested,point,normal;public bool found;public string collider;}
  [Serializable] sealed class A03Inspection {public string scene,nav,digest;public bool dirty;public List<A03Object> objects=new List<A03Object>();public List<A03Face> faces=new List<A03Face>();}
  static double a03Next;
  [InitializeOnLoadMethod] static void A03Install(){EditorApplication.update-=A03Poll;EditorApplication.update+=A03Poll;}
  static void A03Poll()
  {
   if(EditorApplication.timeSinceStartup<a03Next||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   a03Next=EditorApplication.timeSinceStartup+1;
   string file=A03Work+"/Accion.pending";if(!File.Exists(file))return;
   var text=File.ReadAllText(file);File.Move(file,file+".consumed."+DateTime.UtcNow.Ticks);
   try
   {
    var r=JsonUtility.FromJson<A03Request>(text);var s=SceneManager.GetSceneByPath(City);
    if(!s.isLoaded)throw new InvalidOperationException("La escena original debe estar abierta; no se sustituye automáticamente.");
    if(r.action=="inspect")A03Inspect(s);
    else if(r.action=="view")A03View(r.view);
    else if(r.action=="build")A03Build(s);
    else if(r.action=="validate")A03Validate(s,false);
    else if(r.action=="save")A03Validate(s,true);
    else if(r.action=="reload")A03Reload(s);
    else if(r.action=="finish")A03Finish(s);
    else if(r.action=="opaque")A03Opaque(s);
    else if(r.action=="lamp")A03LampMaterial(s);
    else if(r.action=="routes")
    {
     var routes=RunAutomaticRoutes(A03Work+"/Regresion_rutas",s.isDirty);
     if(routes.Count!=8||routes.Any(x=>!x.navigable))throw new InvalidOperationException("Alguna de las ocho rutas automáticas no conserva la navegación.");
    }
    else throw new InvalidOperationException("Acción desconocida: "+r.action);
    File.WriteAllText(A03Work+"/Ultima_accion.json",JsonUtility.ToJson(r,true));
   }
   catch(Exception e){File.WriteAllText(A03Work+"/Error_"+DateTime.UtcNow.Ticks+".txt",e.ToString());Debug.LogException(e);}
  }
  static Transform A03Find(Scene s,string path)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>Hierarchy(t)==path);
  static Transform A03Arch(Scene s)=>A03Find(s,"Subsuelo_Acceso_Etapa01/09 - Arco del descanso inferior");
  static void A03Inspect(Scene s)
  {
   Physics.SyncTransforms();var r=new A03Inspection{scene=Hash(City),nav=Hash(Nav),digest=TriangleDigest4(),dirty=s.isDirty};
   foreach(var root in s.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<Renderer>(true))
   {
    var b=c.bounds;if(b.max.y<-19||b.min.y>-4||b.max.x<143||b.min.x>169||b.max.z<89||b.min.z>115)continue;
    r.objects.Add(new A03Object{path=Hierarchy(c.transform),type=c.GetType().Name,center=b.center,size=b.size,materials=c.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray()});
   }
   var arch=A03Arch(s).GetComponentsInChildren<Collider>();
   foreach(float x in new[]{150f,151f,152f,156f,160f,161f,162f})foreach(float y in new[]{-14.7f,-13.8f,-13.2f,-11f,-9f})
   {
    var q=V(x,y,100);var f=new A03Face{requested=q};float d=100;
    foreach(var c in arch)if(c.Raycast(new Ray(q,Vector3.forward),out var h,16)&&h.distance<d){d=h.distance;f.found=true;f.point=h.point;f.normal=h.normal;f.collider=Hierarchy(c.transform);}
    r.faces.Add(f);
   }
   var ray=new Ray(V(165,-12.8f,102.8f),Vector3.up);
   if(s.GetPhysicsScene().Raycast(ray.origin,ray.direction,out var roof,20,~0,QueryTriggerInteraction.Ignore))r.faces.Add(new A03Face{found=true,requested=ray.origin,point=roof.point,normal=roof.normal,collider=Hierarchy(roof.collider.transform)});
   File.WriteAllText(A03Work+"/Inspeccion.json",JsonUtility.ToJson(r,true));A03View(0);
  }
  static void A03View(int view)
  {
   var eyes=new[]{V(158,-12,93),V(156,-14.3f,99),V(163,-12.5f,101),V(156,-13.3f,113),V(156,-5,88)};
   var aims=new[]{V(156,-12.8f,108),V(156,-12.7f,108),V(161,-13.5f,107),V(162,-14.3f,99),V(156,-13,104)};
   int i=Mathf.Clamp(view,0,eyes.Length-1);var sv=SceneView.lastActiveSceneView??EditorWindow.GetWindow<SceneView>();
   sv.sceneLighting=true;sv.drawGizmos=false;sv.cameraSettings.fieldOfView=60;
   sv.LookAtDirect(aims[i],Quaternion.LookRotation(aims[i]-eyes[i],Vector3.up),Vector3.Distance(eyes[i],aims[i])*.5f);sv.Repaint();sv.Focus();
  }

  [Serializable] sealed class A03Proof
  {
   public string sceneBefore,sceneAfter,nav,digest,physicsBefore,physicsAfter,error;
   public bool saved,reloaded,physicsUnchanged,navUnchanged,individualClear,baselineIndividualClear;
   public int floorRenderers,archRenderers,railRenderers,newColliders,newBehaviours;
   public List<A03Object> additions=new List<A03Object>();
   public List<Station> before=new List<Station>(),after=new List<Station>();
   public List<string> sweepFailures=new List<string>();
  }
  static A03Proof a03Proof;
  static GameObject a03Root;
  static List<(Vector3 a,Vector3 b,Vector3 c)> a03Triangles;
  static Material a03Bronze,a03Glyph,a03Violet,a03Soot;

  static string A03Physics(Scene s)
  {
   // Same-session fingerprint: includes every existing collider, mesh and transform.
   // No slow GlobalObjectId enumeration over the monumental scene.
   var rows=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).OrderBy(c=>c.GetInstanceID()).Select(c=>c.GetInstanceID()+"|"+EditorJsonUtility.ToJson(c)+"|"+c.transform.localToWorldMatrix.ToString("R")+"|"+c.gameObject.activeInHierarchy+"|"+c.gameObject.layer);
   using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",rows)))).Replace("-","");
  }
  static Plan A03Route()=>P("A03-R21 preservación","A03","R21",V(156,-16,99),V(174,-16,99),V(174,-16,117),V(120,-16,117),V(117,-16,123));
  static List<Station> A03Stations(Scene s,List<string> failures)
  {
   var cfg=ConstructionConfig4(s);var p=A03Route();var stations=new List<Station>();
   foreach(bool reverse in new[]{false,true})
   {
    var pts=reverse?p.points.Reverse().ToArray():p.points;Station previous=null;
    for(int leg=1;leg<pts.Length;leg++)
    {
     int n=Mathf.CeilToInt(Vector3.Distance(pts[leg-1],pts[leg])/.15f);
     for(int j=leg==1?0:1;j<=n;j++)
     {
      var q=Vector3.Lerp(pts[leg-1],pts[leg],(float)j/n);var st=Probe(q,cfg,false,.03f);stations.Add(st);
      if(previous!=null&&!Sweep(previous,st,cfg,false,out var problem,out _))failures.Add((reverse?"vuelta ":"ida ")+JsonUtility.ToJson(problem));previous=st;
     }
    }
   }
   return stations;
  }
  static Material A03Material(string name,Material source,Color color,bool textured=true,float emission=0)
  {
   string path=A03Assets+"/"+name+".mat";
   if(AssetDatabase.LoadAssetAtPath<Material>(path))throw new InvalidOperationException("No se sobrescribe material existente: "+path);
   var m=source&&textured?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;
   if(!textured){m.SetTexture("_BaseMap",null);m.SetTexture("_BumpMap",null);m.SetTexture("_SpecGlossMap",null);m.SetTexture("_EmissionMap",null);m.DisableKeyword("_NORMALMAP");m.DisableKeyword("_SPECGLOSSMAP");}
   m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);m.SetFloat("_Smoothness",.12f);
   m.SetColor("_EmissionColor",color*emission);if(emission>0)m.EnableKeyword("_EMISSION");else m.DisableKeyword("_EMISSION");
   AssetDatabase.CreateAsset(m,path);return m;
  }
  static void A03Assign(Transform t,Material m)
  {
   foreach(var r in t.GetComponentsInChildren<MeshRenderer>(true))
   {
    r.sharedMaterials=r.sharedMaterials.Select(_=>m).ToArray();
    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
   }
  }
  static void A03LoadFaces(Transform arch)
  {
   a03Triangles=new List<(Vector3,Vector3,Vector3)>();
   foreach(var f in arch.GetComponentsInChildren<MeshFilter>(true))
   {
    if(!f.sharedMesh)continue;var v=f.sharedMesh.vertices;var t=f.sharedMesh.triangles;
    for(int i=0;i<t.Length;i+=3)a03Triangles.Add((f.transform.TransformPoint(v[t[i]]),f.transform.TransformPoint(v[t[i+1]]),f.transform.TransformPoint(v[t[i+2]])));
   }
  }
  static Vector3 A03Surface(Vector2 p,float offset=.009f)
  {
   // Intersect the real render triangles as well as the physical arch: the relief
   // projects onto visible stone, including the moulding, not a collider's flat box.
   var o=V(p.x,p.y,100);float nearest=float.MaxValue;
   foreach(var t in a03Triangles)
   {
    var e1=t.b-t.a;var e2=t.c-t.a;var h=Vector3.Cross(Vector3.forward,e2);float det=Vector3.Dot(e1,h);if(Mathf.Abs(det)<1e-7f)continue;
    float inv=1/det;var s=o-t.a;float u=inv*Vector3.Dot(s,h);if(u<0||u>1)continue;var q=Vector3.Cross(s,e1);float v=inv*Vector3.Dot(Vector3.forward,q);if(v<0||u+v>1)continue;
    float d=inv*Vector3.Dot(e2,q);if(d>0&&d<nearest)nearest=d;
   }
   if(nearest==float.MaxValue||nearest>14)throw new InvalidOperationException("Relieve sin piedra visible de apoyo: "+p);
   return o+Vector3.forward*(nearest-offset);
  }
  static void A03Ribbon(string name,Vector2[] line,float width,Material material)
  {
   var verts=new List<Vector3>();var tris=new List<int>();
   for(int i=1;i<line.Length;i++)
   {
    var d=(line[i]-line[i-1]).normalized;var side=new Vector2(-d.y,d.x)*width*.5f;int n=verts.Count;
    verts.Add(A03Surface(line[i-1]+side));verts.Add(A03Surface(line[i-1]-side));verts.Add(A03Surface(line[i]-side));verts.Add(A03Surface(line[i]+side));
    tris.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
   }
   var mesh=new Mesh{name=name};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
   AssetDatabase.CreateAsset(mesh,A03Assets+"/"+name+".asset");var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(a03Root.transform,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  }
  static Vector2[] A03Arc(Vector2 c,float radius,float start,float end,int segments=24)=>Enumerable.Range(0,segments+1).Select(i=>{float a=Mathf.Lerp(start,end,(float)i/segments)*Mathf.Deg2Rad;return c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}).ToArray();
  static void A03Light(string name,Vector3 p,Vector3 aim,Color color,float intensity,float range,float angle,bool shadows)
  {
   var g=new GameObject(name,typeof(Light));g.transform.SetParent(a03Root.transform,false);g.transform.position=p;g.transform.rotation=Quaternion.LookRotation(aim-p);
   var l=g.GetComponent<Light>();l.type=LightType.Spot;l.color=color;l.intensity=intensity;l.range=range;l.spotAngle=angle;l.innerSpotAngle=angle*.55f;l.shadows=shadows?LightShadows.Soft:LightShadows.None;l.shadowStrength=.75f;l.renderMode=LightRenderMode.ForcePixel;l.bounceIntensity=0;
  }
  static void A03Build(Scene s)
  {
   if(s.isDirty||s.GetRootGameObjects().Any(g=>g.name==A03Root))throw new InvalidOperationException("La escena debe estar limpia y A03 no debe existir ya.");
   var original=JsonUtility.FromJson<A03Inspection>(File.ReadAllText(A03Work+"/Inspeccion.json"));
   if(Hash(City)!=original.scene||Hash(Nav)!=original.nav)throw new InvalidOperationException("Escena o navegación cambiaron desde la medición.");
   if(!File.Exists(A03Work+"/Antes_SHA256.json"))throw new InvalidOperationException("Falta respaldo SHA-256.");
   a03Proof=new A03Proof{sceneBefore=Hash(City),nav=Hash(Nav),digest=TriangleDigest4(),physicsBefore=A03Physics(s)};
   var fails=new List<string>();a03Proof.before=A03Stations(s,fails);a03Proof.baselineIndividualClear=a03Proof.before.All(x=>x.Clear&&x.nav)&&fails.Count==0;
   File.WriteAllText(A03Work+"/Base_fisica.json",JsonUtility.ToJson(a03Proof,true));
   if(!a03Proof.baselineIndividualClear)throw new InvalidOperationException("La ruta base no pasó; no se construye sobre una regresión previa.");
   Directory.CreateDirectory(A03Assets);AssetDatabase.Refresh();
   var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Eternal Temple/Source_FBX/Materials/Atlas_Architecture_01.mat");
   var stone=A03Material("A03_Piedra_oscurecida",source,new Color(.38f,.37f,.35f));
   var floor=A03Material("A03_Pavimento_antiguo",source,new Color(.54f,.50f,.44f));
   var rail=A03Material("A03_Piedra_barandas",source,new Color(.45f,.42f,.37f));
   a03Bronze=A03Material("A03_Bronce_envejecido",source,new Color(.29f,.23f,.15f),false);
   a03Glyph=A03Material("A03_Relieve_solar",source,new Color(.51f,.43f,.29f),false);
   a03Soot=A03Material("A03_Incision_oscura",source,new Color(.10f,.085f,.075f),false);
   a03Violet=A03Material("A03_Fractura_violeta",source,new Color(.24f,.12f,.36f),false,.9f);
   var arch=A03Arch(s);var floorRoot=A03Find(s,"Subsuelo_Acceso_Etapa01/05 - Primer piso inferior Y-16");var railRoot=A03Find(s,"Subsuelo_Acceso_Etapa01/04 - Protecciones del piso inferior");
   a03Proof.floorRenderers=floorRoot.GetComponentsInChildren<MeshRenderer>(true).Length;a03Proof.archRenderers=arch.GetComponentsInChildren<MeshRenderer>(true).Length;a03Proof.railRenderers=railRoot.GetComponentsInChildren<MeshRenderer>(true).Length;
   A03Assign(floorRoot,floor);A03Assign(arch,stone);A03Assign(railRoot,rail);
   a03Root=new GameObject(A03Root);SceneManager.MoveGameObjectToScene(a03Root,s);A03LoadFaces(arch);
   // An incomplete sun, worn radial strokes and a small unique glyph group.
   var center=new Vector2(156,-9.03f);
   A03Ribbon("01_Sol_erosionado_arco_a",A03Arc(center,.58f,12,139),.036f,a03Glyph);
   A03Ribbon("02_Sol_erosionado_arco_b",A03Arc(center,.58f,154,276),.036f,a03Glyph);
   A03Ribbon("03_Sol_erosionado_arco_c",A03Arc(center,.58f,290,344),.028f,a03Glyph);
   A03Ribbon("04_Nucleo_solar",A03Arc(center,.22f,0,360),.025f,a03Glyph);
   for(int i=0;i<10;i++){float a=(i*36+10)*Mathf.Deg2Rad;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));A03Ribbon("05_Rayo_"+i,new[]{center+d*.71f,center+d*(i%3==0?.86f:.95f)},.027f,a03Glyph);}
   A03Ribbon("06_Glifo_antiguo_uno",new[]{new Vector2(150.9f,-12.1f),new Vector2(151.1f,-11.9f),new Vector2(151.3f,-12.1f),new Vector2(151.1f,-12.3f),new Vector2(150.9f,-12.1f)},.026f,a03Glyph);
   A03Ribbon("07_Glifo_antiguo_dos",new[]{new Vector2(150.88f,-12.6f),new Vector2(151.1f,-12.42f),new Vector2(151.1f,-12.85f)},.023f,a03Glyph);
   A03Ribbon("08_Glifo_antiguo_tres",A03Arc(new Vector2(151.1f,-13.15f),.17f,30,300,18),.025f,a03Glyph);
   foreach(float x in new[]{151.05f,161.1f})
   {
    string id=x<156?"oeste":"este";
    A03Ribbon("09_Anclaje_"+id,new[]{new Vector2(x-.12f,-14.1f),new Vector2(x+.12f,-14.1f),new Vector2(x+.12f,-14.34f),new Vector2(x-.12f,-14.34f),new Vector2(x-.12f,-14.1f)},.055f,a03Bronze);
    A03Ribbon("10_Argolla_rota_"+id,A03Arc(new Vector2(x,-14.4f),.16f,35,315,22),.048f,a03Bronze);
   }
   var crack=new[]{new Vector2(161.38f,-12.5f),new Vector2(161.31f,-12.68f),new Vector2(161.44f,-12.85f),new Vector2(161.34f,-13.02f),new Vector2(161.40f,-13.19f),new Vector2(161.28f,-13.4f)};
   A03Ribbon("11_Fractura_piedra",crack,.050f,a03Soot);A03Ribbon("12_Filamento_violeta",crack,.012f,a03Violet);
   A03Ribbon("13_Ramificacion_fractura",new[]{crack[2],new Vector2(161.62f,-12.92f),new Vector2(161.69f,-13.07f)},.008f,a03Violet);
   A03Lantern(s,source);
   A03Light("Luz_calida_arco",V(154,-8.25f,101.8f),V(156,-11.3f,106),new Color(1,.69f,.35f),4,12,100,false);
   EditorSceneManager.MarkSceneDirty(s);AssetDatabase.SaveAssets();A03Validate(s,false);A03View(0);
  }
  static void A03Lantern(Scene s,Material source)
  {
   var p=V(165,-11.9f,102.8f);
   if(!s.GetPhysicsScene().Raycast(p,Vector3.up,out var roof,12,~0,QueryTriggerInteraction.Ignore)||roof.normal.y>-.5f)throw new InvalidOperationException("No hay techo sólido para la suspensión.");
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Standard Assets/Eternal Temple/Prefabs/Lanterns/Lantern_Small_03_Unlit.prefab");
   // Copy only its existing visual mesh; no prefab scripts, physics or original edits.
   var mf=prefab.GetComponentsInChildren<MeshFilter>(true).First();var visual=new GameObject("14_Luminaria_original_visual",typeof(MeshFilter),typeof(MeshRenderer));visual.transform.SetParent(a03Root.transform,false);visual.transform.position=p;visual.transform.localScale=Vector3.one*.8f;visual.GetComponent<MeshFilter>().sharedMesh=mf.sharedMesh;
   var mr=visual.GetComponent<MeshRenderer>();mr.sharedMaterials=mf.GetComponent<MeshRenderer>().sharedMaterials.Select(_=>a03Bronze).ToArray();
   float top=mr.bounds.max.y;float bottom=mr.bounds.min.y;if(bottom<-13)throw new InvalidOperationException("Luminaria invadiría la altura libre protegida.");
   var chain=new GameObject("15_Suspension_al_techo",typeof(MeshFilter),typeof(MeshRenderer));chain.transform.SetParent(a03Root.transform,false);
   // A narrow rectangular metal suspension supported at the measured roof underside.
   float h=roof.point.y-top;var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=primitive.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(primitive);
   chain.GetComponent<MeshFilter>().sharedMesh=cube;chain.GetComponent<MeshRenderer>().sharedMaterial=a03Bronze;chain.transform.position=V(p.x,top+h*.5f,p.z);chain.transform.localScale=V(.025f,h,.025f);
   A03Light("Luz_guia_calida",V(p.x,bottom-.03f,p.z),V(163,-16,100),new Color(1,.73f,.43f),2.2f,9,110,false);
  }
  static void A03Validate(Scene s,bool save)
  {
   if(a03Proof==null&&File.Exists(A03Work+"/Validacion.json"))a03Proof=JsonUtility.FromJson<A03Proof>(File.ReadAllText(A03Work+"/Validacion.json"));
   if(a03Proof==null)throw new InvalidOperationException("Falta la captura base de esta sesión; no se guarda automáticamente.");
   a03Proof.sweepFailures.Clear();
   Physics.SyncTransforms();a03Proof.after=A03Stations(s,a03Proof.sweepFailures);a03Proof.physicsAfter=A03Physics(s);a03Proof.physicsUnchanged=a03Proof.physicsBefore==a03Proof.physicsAfter;
   a03Proof.navUnchanged=Hash(Nav)==a03Proof.nav&&TriangleDigest4()==a03Proof.digest;
   a03Proof.individualClear=a03Proof.after.All(x=>x.Clear&&x.nav)&&a03Proof.sweepFailures.Count==0;
   var root=s.GetRootGameObjects().Single(g=>g.name==A03Root);a03Proof.newColliders=root.GetComponentsInChildren<Collider>(true).Length;
   a03Proof.newBehaviours=root.GetComponentsInChildren<MonoBehaviour>(true).Length;
   a03Proof.additions=root.GetComponentsInChildren<Renderer>(true).Select(r=>new A03Object{path=Hierarchy(r.transform),type=r.GetType().Name,center=r.bounds.center,size=r.bounds.size,materials=r.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray()}).ToList();
   bool okay=a03Proof.baselineIndividualClear&&a03Proof.individualClear&&a03Proof.navUnchanged&&a03Proof.physicsUnchanged&&a03Proof.newColliders==0&&a03Proof.newBehaviours==0;
   File.WriteAllText(A03Work+"/Validacion.json",JsonUtility.ToJson(a03Proof,true));
   if(!okay)throw new InvalidOperationException("A03 no supera la preservación física/NavMesh. No se guarda.");
   if(save)
   {
    // Explicit separate action after actual visual inspection. Never builds/bakes NavMesh.
    if(!File.Exists(A03Work+"/Revision_visual_aprobada.txt"))throw new InvalidOperationException("Falta revisión visual documentada.");
    if(!EditorSceneManager.SaveScene(s))throw new IOException("SaveScene falló.");a03Proof.saved=true;a03Proof.sceneAfter=Hash(City);
    File.WriteAllText(A03Work+"/Validacion.json",JsonUtility.ToJson(a03Proof,true));
   }
  }
  static void A03Reload(Scene s)
  {
   var proof=JsonUtility.FromJson<A03Proof>(File.ReadAllText(A03Work+"/Validacion.json"));if(!proof.saved||s.isDirty||Hash(City)!=proof.sceneAfter)throw new InvalidOperationException("La escena debe coincidir con el guardado validado.");
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);s=SceneManager.GetSceneByPath(City);var failures=new List<string>();var stations=A03Stations(s,failures);
   proof.reloaded=Hash(Nav)==proof.nav&&TriangleDigest4()==proof.digest&&stations.All(x=>x.Clear&&x.nav)&&failures.Count==0&&!s.isDirty&&s.GetRootGameObjects().Count(g=>g.name==A03Root)==1;
   File.WriteAllText(A03Work+"/Validacion_recarga.json",JsonUtility.ToJson(proof,true));if(!proof.reloaded)throw new InvalidOperationException("Falló verificación tras recarga.");A03View(0);
  }
  static void A03Finish(Scene s)
  {
   a03Root=s.GetRootGameObjects().Single(g=>g.name==A03Root);
   if(a03Root.transform.Find("16_Nucleo_ambar_luminaria"))throw new InvalidOperationException("El acabado ya existe; no se duplica.");
   var glyph=AssetDatabase.LoadAssetAtPath<Material>(A03Assets+"/A03_Relieve_solar.mat");glyph.SetColor("_BaseColor",new Color(.25f,.205f,.14f));glyph.SetColor("_Color",new Color(.25f,.205f,.14f));EditorUtility.SetDirty(glyph);
   foreach(string name in new[]{"12_Filamento_violeta","13_Ramificacion_fractura"})
   {
    var mesh=a03Root.transform.Find(name).GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;
    for(int i=0;i<vertices.Length;i++)vertices[i]-=Vector3.forward*.004f;
    mesh.vertices=vertices;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
   }
   var bronze=AssetDatabase.LoadAssetAtPath<Material>(A03Assets+"/A03_Bronce_envejecido.mat");
   foreach(float x in new[]{151.05f,161.1f})
   {
    string id=x<156?"oeste":"este";var old=a03Root.transform.Find("10_Argolla_rota_"+id);old.gameObject.SetActive(false);
    var center=V(x,-14.4f,104.2f);var vertices=new List<Vector3>();var triangles=new List<int>();int major=36,minor=8;
    for(int i=0;i<=major;i++)for(int j=0;j<=minor;j++)
    {
     float a=Mathf.Lerp(35,315,(float)i/major)*Mathf.Deg2Rad,b=j*Mathf.PI*2/minor;float r=.15f+.025f*Mathf.Cos(b);
     vertices.Add(center+V(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(b)*.025f));
     if(i<major&&j<minor){int n=i*(minor+1)+j;triangles.AddRange(new[]{n,n+minor+1,n+1,n+1,n+minor+1,n+minor+2});}
    }
    // Fit the back of the ring to actual visible stone at its suspension point.
    A03LoadFaces(A03Arch(s));
    for(int i=0;i<vertices.Count;i++){var v=vertices[i];v.z=A03Surface(new Vector2(v.x,v.y)).z-.04f+(v.z-center.z);vertices[i]=v;}
    var mesh=new Mesh{name="Anclaje_volumetrico_"+id};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,A03Assets+"/"+mesh.name+".asset");
    var g=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(a03Root.transform,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=bronze;
   }
   var glow=A03Material("A03_Ambar_luminaria",null,new Color(.65f,.28f,.06f),false,1.2f);
   var bulb=GameObject.CreatePrimitive(PrimitiveType.Sphere);UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());bulb.name="16_Nucleo_ambar_luminaria";bulb.transform.SetParent(a03Root.transform,false);bulb.transform.position=V(165,-11.68f,102.8f);bulb.transform.localScale=V(.11f,.19f,.11f);bulb.GetComponent<MeshRenderer>().sharedMaterial=glow;
   EditorSceneManager.MarkSceneDirty(s);AssetDatabase.SaveAssets();A03Validate(s,false);A03View(0);
  }
  static void A03Opaque(Scene s)
  {
   // Plain relief/metal must not inherit the atlas' cutout and alpha-to-coverage.
   // Keep the GUIDs and all original shared materials untouched.
   foreach(string n in new[]{"A03_Bronce_envejecido","A03_Relieve_solar","A03_Incision_oscura","A03_Fractura_violeta"})
   {
    var m=AssetDatabase.LoadAssetAtPath<Material>(A03Assets+"/"+n+".mat");var color=m.GetColor("_BaseColor");color.a=1;var emission=m.GetColor("_EmissionColor");
    var clean=new Material(Shader.Find("Universal Render Pipeline/Lit"));clean.name=n;clean.SetColor("_BaseColor",color);clean.SetFloat("_Smoothness",.15f);clean.SetColor("_EmissionColor",emission);if(n=="A03_Fractura_violeta")clean.EnableKeyword("_EMISSION");
    EditorUtility.CopySerialized(clean,m);UnityEngine.Object.DestroyImmediate(clean);EditorUtility.SetDirty(m);
   }
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s);A03Validate(s,false);A03View(2);
  }
  static void A03LampMaterial(Scene s)
  {
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Standard Assets/Eternal Temple/Prefabs/Lanterns/Lantern_Small_03_Unlit.prefab");var originals=prefab.GetComponentsInChildren<MeshRenderer>(true).First().sharedMaterials;
   var local=originals.Select((m,i)=>A03Material("A03_Luminaria_atlas_"+i,m,new Color(.65f,.55f,.4f),true)).ToArray();
   s.GetRootGameObjects().Single(g=>g.name==A03Root).transform.Find("14_Luminaria_original_visual").GetComponent<MeshRenderer>().sharedMaterials=local;
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s);A03Validate(s,false);A03View(3);
  }
 }
}
