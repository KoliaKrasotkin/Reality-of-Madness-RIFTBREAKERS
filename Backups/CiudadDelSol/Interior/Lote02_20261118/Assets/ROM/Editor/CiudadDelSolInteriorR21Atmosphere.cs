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
  const string R21Work="Backups/CiudadDelSol/Interior/R21_Construccion_20261118";
  const string R21Assets="Assets/ROM/Art/CiudadDelSol/Interior/R21";
  const string R21Root="CiudadDelSol_R21_SalonDeLasOfrendas";
  [Serializable] sealed class R21Request { public string action;public int view; }
  [Serializable] sealed class R21Inspection
  {
   public bool dirty;public string scene,nav,digest;
   public List<A03Object> objects=new List<A03Object>();public List<Station> samples=new List<Station>();
   public List<A03Face> supports=new List<A03Face>();
   public List<string> prefabs=new List<string>();
  }
  static double r21Next;
  [InitializeOnLoadMethod] static void R21Install(){EditorApplication.update-=R21Poll;EditorApplication.update+=R21Poll;}
  static void R21Poll()
  {
   if(EditorApplication.timeSinceStartup<r21Next||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;r21Next=EditorApplication.timeSinceStartup+1;
   string file=R21Work+"/Accion.pending";if(!File.Exists(file))return;
   string text=File.ReadAllText(file);File.Move(file,file+".consumed."+DateTime.UtcNow.Ticks);
   try
   {
    var r=JsonUtility.FromJson<R21Request>(text);var s=SceneManager.GetSceneByPath(City);if(!s.isLoaded)throw new InvalidOperationException("Se necesita la escena original abierta; no se abre otro Editor.");
    if(r.action=="inspect")R21Inspect(s);else if(r.action=="view")R21View(r.view);
    else if(r.action=="build")R21Build(s);
    else if(r.action=="validate")R21Validate(s,false);
    else if(r.action=="save")R21Validate(s,true);
    else if(r.action=="reload")R21Reload(s);
    else if(r.action=="regressions")R21Regressions(s);
    else if(r.action=="symbols")ApplyInteriorSymbols(s);
    else if(r.action=="symbols-finish")FinishInteriorSymbols(s);
    else if(r.action=="details")R21Details(s);
    else if(r.action=="polish")R21Polish(s);
    else if(r.action=="stone")R21StoneFinish(s);
    else if(r.action=="tone")
    {
     var mat=AssetDatabase.LoadAssetAtPath<Material>(R21Assets+"/R21_Piedra_sombría.mat");var color=new Color(.17f,.185f,.175f,mat.GetColor("_BaseColor").a);mat.SetColor("_BaseColor",color);mat.SetColor("_Color",color);
     EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s);R21Validate(s,false);R21View(0);
    }
    else if(r.action=="bed")
    {
     r21Root=s.GetRootGameObjects().Single(g=>g.name==R21Root);if(r21Root.transform.Find("31_Placa_de_apoyo_de_carcasa"))throw new InvalidOperationException("La placa ya existe.");
     var bounds=r21Root.transform.Find("16_Carcasa_pétrea_original").GetComponent<Renderer>().bounds;
     var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name="31_Placa_de_apoyo_de_carcasa";plate.transform.SetParent(r21Root.transform,false);
     plate.transform.position=V(bounds.center.x,bounds.min.y-.05f,bounds.center.z);plate.transform.localScale=V(bounds.size.x+.2f,.1f,bounds.size.z+.2f);plate.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(R21Assets+"/R21_Metal_oxidado.mat");
     Physics.SyncTransforms();if(Mathf.Abs(plate.GetComponent<Renderer>().bounds.max.y-bounds.min.y)>.0001f)throw new InvalidOperationException("La carcasa no apoya sobre la placa.");
     File.WriteAllText(R21Work+"/Contacto_carcasa.txt","Cara superior de placa="+plate.GetComponent<Renderer>().bounds.max.y.ToString("R")+"; cara inferior visible de carcasa="+bounds.min.y.ToString("R"));
     EditorSceneManager.MarkSceneDirty(s);R21Validate(s,false);R21View(6);
    }
    else throw new InvalidOperationException("Acción no disponible: "+r.action);
    File.WriteAllText(R21Work+"/Ultima_accion.json",JsonUtility.ToJson(r,true));
   }
   catch(Exception e){File.WriteAllText(R21Work+"/Error_"+DateTime.UtcNow.Ticks+".txt",e.ToString());Debug.LogException(e);}
  }
  static Transform R21Nave(Scene s)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.parent&&t.parent.name=="Subsuelo_34_Naves_Sin_Reticula"&&t.name.StartsWith("Nave 102/102 ",StringComparison.Ordinal));
  static void R21Inspect(Scene s)
  {
   if(s.isDirty)throw new InvalidOperationException("No se modifica ni descarta una escena sucia.");Physics.SyncTransforms();
   var cfg=ConstructionConfig4(s);var r=new R21Inspection{scene=Hash(City),nav=Hash(Nav),digest=TriangleDigest4(),dirty=s.isDirty};
   foreach(var g in s.GetRootGameObjects())foreach(var m in g.GetComponentsInChildren<Renderer>(true))
   {
    var b=m.bounds;if(b.min.x>132||b.max.x<102||b.min.z>144||b.max.z<102||b.min.y>3||b.max.y<-18)continue;
    r.objects.Add(new A03Object{path=Hierarchy(m.transform),type=m.GetType().Name,center=b.center,size=b.size,materials=m.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray()});
   }
   for(int z=103;z<144;z+=2)for(int x=103;x<132;x+=2)r.samples.Add(Probe(V(x,-16,z),cfg,false,.03f));
   foreach(float x in new[]{114f,117f,120f})foreach(float z in new[]{129f,132f,135f})
   {
    var p=V(x,-13,z);if(cfg.physics.Raycast(p,Vector3.up,out var h,20,cfg.mask,QueryTriggerInteraction.Ignore))r.supports.Add(new A03Face{found=true,requested=p,point=h.point,normal=h.normal,collider=Hierarchy(h.collider.transform)});
   }
   foreach(int side in new[]{-1,1})foreach(float z in new[]{108f,114f,120f,126f,132f,138f})foreach(float y in new[]{-14f,-12f,-10f,-8f})
   {
    var p=V(117,y,z);float near=100;A03Face face=null;
    foreach(var c in R21Nave(s).GetComponentsInChildren<Collider>())if(c.Raycast(new Ray(p,Vector3.right*side),out var h,16)&&h.distance<near){near=h.distance;face=new A03Face{found=true,requested=p,point=h.point,normal=h.normal,collider=Hierarchy(c.transform)};}
    if(face!=null)r.supports.Add(face);
   }
   string[] names={"Stone_Altar_01","Stone_Altar_02","Stone_Altar_03","Stone_Altar_04","Bottom_Decor_Rounded_Big_01","Lantern_Small_03_Unlit"};
   foreach(string n in names)
   {
    string guid=AssetDatabase.FindAssets(n+" t:Prefab",new[]{"Assets/Standard Assets/Eternal Temple/Prefabs"}).FirstOrDefault();if(guid==null)continue;string path=AssetDatabase.GUIDToAssetPath(guid);var obj=AssetDatabase.LoadAssetAtPath<GameObject>(path);r.prefabs.Add(path);
    foreach(var mf in obj.GetComponentsInChildren<MeshFilter>(true))if(mf.sharedMesh)r.objects.Add(new A03Object{path=path+"/"+mf.name,type="PrefabMesh",center=mf.sharedMesh.bounds.center,size=mf.sharedMesh.bounds.size,materials=mf.GetComponent<Renderer>().sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray()});
   }
   File.WriteAllText(R21Work+"/Inspeccion.json",JsonUtility.ToJson(r,true));R21View(0);
  }
  static void R21View(int view)
  {
   var eyes=new[]{V(127,-13.6f,108),V(119,-14.3f,117),V(106,-13.8f,136),V(129,-12.7f,140),V(111,-12.5f,124),V(120,-5.5f,137),V(120,-8.8f,127.5f)};
   var aims=new[]{V(117,-10.7f,131),V(117,-10.8f,132),V(117,-11.7f,131),V(116,-12.3f,127),V(117,-10.3f,132),V(117,-3.4f,143.5f),V(117.1f,-8.9f,132)};
   int i=Mathf.Clamp(view,0,eyes.Length-1);Selection.activeObject=null;var sv=SceneView.lastActiveSceneView??EditorWindow.GetWindow<SceneView>();sv.sceneLighting=true;sv.drawGizmos=false;sv.cameraSettings.fieldOfView=65;
   sv.LookAtDirect(aims[i],Quaternion.LookRotation(aims[i]-eyes[i],Vector3.up),Vector3.Distance(eyes[i],aims[i])*Mathf.Sin(32.5f*Mathf.Deg2Rad));sv.Repaint();sv.Focus();
  }

  [Serializable] sealed class R21Proof
  {
   public string sceneBefore,sceneAfter,nav,digest,originalPhysics,a03Before,a03After,error;
   public bool saved,reloaded,originalCollidersPreserved,a03Preserved,navPreserved,gridPreserved,localRoutesClear,regressions,automaticRoutes;
   public int floorRenderers,newColliders;public float minimumNewColliderHeight=100,minimumMeasuredHeadroom=100;
   public List<Station> before=new List<Station>(),after=new List<Station>();
   public List<A03Object> additions=new List<A03Object>();public List<A03Face> mounts=new List<A03Face>();
   public List<Width> widths=new List<Width>();public List<NativeRoute> localNative=new List<NativeRoute>();
   public List<string> problems=new List<string>();
  }
  static R21Proof r21Proof;
  static GameObject r21Root;
  static Material r21Stone,r21Floor,r21Metal,r21Gold,r21Dark,r21Violet,r21Amber;
  static Config r21FloorConfig;
  static MeshCollider[] r21WallColliders;
  static string R21Physics(Scene s)
  {
   var rows=s.GetRootGameObjects().Where(g=>g.name!=R21Root).SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).OrderBy(c=>c.GetInstanceID()).Select(c=>c.GetInstanceID()+"|"+EditorJsonUtility.ToJson(c)+"|"+c.transform.localToWorldMatrix.ToString("R")+"|"+c.gameObject.activeInHierarchy+"|"+c.gameObject.layer);
   return R21Digest(string.Join("\n",rows));
  }
  static string R21Digest(string text)
  {
   using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-","");
  }
  static string R21A03(Scene s)
  {
   var root=s.GetRootGameObjects().Single(g=>g.name==A03Root);
   return R21Digest(string.Join("\n",root.GetComponentsInChildren<Component>(true).Select(c=>c.GetInstanceID()+"|"+EditorJsonUtility.ToJson(c))));
  }
  static Material R21Mat(string name,Color color,Material source=null,float emission=0)
  {
   string path=R21Assets+"/"+name+".mat";if(AssetDatabase.LoadAssetAtPath<Material>(path))throw new InvalidOperationException("No sobrescribir material existente: "+path);
   var m=source?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;
   if(source&&m.HasProperty("_BaseColor"))color.a=m.GetColor("_BaseColor").a;
   m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);
   if(!source)m.SetFloat("_Smoothness",.16f);
   m.SetColor("_EmissionColor",emission>0?new Color(color.r,color.g,color.b,1)*emission:Color.black);
   if(emission>0)m.EnableKeyword("_EMISSION");else m.DisableKeyword("_EMISSION");
   AssetDatabase.CreateAsset(m,path);return m;
  }
  static GameObject R21Mesh(string name,List<Vector3> vertices,List<int> triangles,Material mat,bool solid)
  {
   var mesh=new Mesh{name=name};if(vertices.Count>65000)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
   mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,R21Assets+"/"+name+".asset");
   var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(r21Root.transform,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=mat;
   if(solid)g.AddComponent<MeshCollider>().sharedMesh=mesh;
   return g;
  }
  static Vector3 R21FloorPoint(Vector3 p,float offset=.006f)
  {
   var cfg=r21FloorConfig??(r21FloorConfig=ConstructionConfig4(SceneManager.GetSceneByPath(City)));p.y=-16;
   if(!Ground(p,.03f,cfg,out var h)||Vector3.Angle(h.normal,Vector3.up)>2)throw new InvalidOperationException("Grabado sin suelo compatible: "+p);
   return h.point+Vector3.up*offset;
  }
  static void R21FloorLine(string name,Vector3[] points,float width,Material mat)
  {
   var vs=new List<Vector3>();var ts=new List<int>();
   for(int i=1;i<points.Length;i++)
   {
    var side=Vector3.Cross(Vector3.up,(points[i]-points[i-1]).normalized)*width*.5f;int n=vs.Count;
    vs.Add(R21FloorPoint(points[i-1]+side));vs.Add(R21FloorPoint(points[i-1]-side));vs.Add(R21FloorPoint(points[i]-side));vs.Add(R21FloorPoint(points[i]+side));ts.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
   }
   R21Mesh(name,vs,ts,mat,false);
  }
  static Vector3[] R21Circle(float radius,float start,float end,int n=96)=>Enumerable.Range(0,n+1).Select(i=>{float a=Mathf.Lerp(start,end,(float)i/n)*Mathf.Deg2Rad;return V(117+Mathf.Cos(a)*radius,-16,132+Mathf.Sin(a)*radius);}).ToArray();
  static GameObject R21Ring(string name,Vector3 center,Quaternion orientation,float radius,float thickness,float start,float end,Material mat)
  {
   var vs=new List<Vector3>();var ts=new List<int>();int major=96,minor=10;
   for(int i=0;i<=major;i++)for(int j=0;j<=minor;j++)
   {
    float a=Mathf.Lerp(start,end,(float)i/major)*Mathf.Deg2Rad,b=j*Mathf.PI*2/minor;
    vs.Add(center+orientation*V(Mathf.Cos(a)*(radius+thickness*Mathf.Cos(b)),Mathf.Sin(a)*(radius+thickness*Mathf.Cos(b)),thickness*Mathf.Sin(b)));
    if(i<major&&j<minor){int k=i*(minor+1)+j;ts.AddRange(new[]{k,k+minor+1,k+1,k+1,k+minor+1,k+minor+2});}
   }
   return R21Mesh(name,vs,ts,mat,true);
  }
  static GameObject R21Beam(string name,Vector3 a,Vector3 b,float width,Material mat,bool solid=true)
  {
   var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(r21Root.transform,false);g.transform.position=(a+b)*.5f;g.transform.rotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);g.transform.localScale=V(width,Vector3.Distance(a,b),width);g.GetComponent<Renderer>().sharedMaterial=mat;
   if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
  }
  static RaycastHit R21Roof(Scene s,Vector3 p)
  {
   int n=s.GetPhysicsScene().Raycast(p,Vector3.up,Hits,30,~0,QueryTriggerInteraction.Ignore);float nearest=100;RaycastHit best=default;
   if(n==Hits.Length)throw new InvalidOperationException("Consulta de techo saturada.");
   for(int i=0;i<n;i++)if(!Hits[i].collider.transform.IsChildOf(r21Root.transform)&&Hits[i].distance<nearest){best=Hits[i];nearest=best.distance;}
   if(!best.collider||best.normal.y>-.5f)throw new InvalidOperationException("Sin techo firme para suspensión: "+p);return best;
  }
  static void R21Light(string name,Vector3 p,Vector3 target,Color color,float power,float range,float angle)
  {
   var g=new GameObject(name,typeof(Light));g.transform.SetParent(r21Root.transform,false);g.transform.position=p;g.transform.rotation=Quaternion.LookRotation(target-p);
   var l=g.GetComponent<Light>();l.type=LightType.Spot;l.color=color;l.intensity=power;l.range=range;l.spotAngle=angle;l.innerSpotAngle=angle*.5f;l.shadows=LightShadows.None;l.bounceIntensity=0;
  }
  static void R21Build(Scene s)
  {
   if(s.isDirty||s.GetRootGameObjects().Any(g=>g.name==R21Root))throw new InvalidOperationException("Escena sucia o R21 ya construido; no duplicar.");
   var old=JsonUtility.FromJson<R21Inspection>(File.ReadAllText(R21Work+"/Inspeccion.json"));if(old.scene!=Hash(City)||old.nav!=Hash(Nav)||!File.Exists(R21Work+"/Antes_SHA256.json"))throw new InvalidOperationException("Respaldo/medición no coincide.");
   r21Proof=new R21Proof{sceneBefore=Hash(City),nav=Hash(Nav),digest=TriangleDigest4(),originalPhysics=R21Physics(s),a03Before=R21A03(s),before=old.samples};
   File.WriteAllText(R21Work+"/Base.json",JsonUtility.ToJson(r21Proof,true));Directory.CreateDirectory(R21Assets);AssetDatabase.Refresh();
   var stoneSource=AssetDatabase.LoadAssetAtPath<Material>("Assets/ROM/CiudadDelSolSalasMonumentales/PiedraOpaca.mat");var floorSource=AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Eternal Temple/Source_FBX/Materials/Atlas_Architecture_01.mat");
   r21Stone=R21Mat("R21_Piedra_sombría",new Color(.25f,.265f,.255f),stoneSource);r21Floor=R21Mat("R21_Pavimento_ceremonial",new Color(.44f,.415f,.36f),floorSource);
   r21Metal=R21Mat("R21_Metal_oxidado",new Color(.25f,.20f,.14f));r21Gold=R21Mat("R21_Grabado_solar",new Color(.38f,.31f,.20f));r21Dark=R21Mat("R21_Incisiones",new Color(.075f,.07f,.06f));r21Violet=R21Mat("R21_Fractura_dimensional",new Color(.22f,.115f,.31f),null,.7f);r21Amber=R21Mat("R21_Ambar_residual",new Color(.48f,.22f,.055f),null,1.2f);
   foreach(var r in R21Nave(s).GetComponentsInChildren<MeshRenderer>(true)){r.sharedMaterials=r.sharedMaterials.Select(_=>r21Stone).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
   foreach(var root in s.GetRootGameObjects())foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
   {
    var b=r.bounds;if(!r.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)||Mathf.Abs(b.max.y+16)>.03f||b.min.x<101.99f||b.max.x>132.01f||b.min.z<101.99f||b.max.z>144.01f)continue;
    r.sharedMaterials=r.sharedMaterials.Select(_=>r21Floor).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);r21Proof.floorRenderers++;
   }
   if(r21Proof.floorRenderers!=35)throw new InvalidOperationException("La selección de suelo no coincide con 35 módulos completos de R21.");
   r21Root=new GameObject(R21Root);SceneManager.MoveGameObjectToScene(r21Root,s);
   var modifier=r21Root.AddComponent<Unity.AI.Navigation.NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;
   // The seal is a flush visual inlay over existing supported stone, never a new floor.
   R21FloorLine("01_Sello_exterior_a",R21Circle(6.5f,8,130),.075f,r21Gold);R21FloorLine("02_Sello_exterior_b",R21Circle(6.5f,145,256),.075f,r21Gold);R21FloorLine("03_Sello_exterior_c",R21Circle(6.5f,275,352),.055f,r21Gold);
   R21FloorLine("04_Registro_concentrico",R21Circle(5.85f,0,360),.035f,r21Dark);R21FloorLine("05_Nucleo_ceremonial",R21Circle(2.1f,0,360),.09f,r21Gold);
   for(int i=0;i<16;i++)
   {
    float a=(i*22.5f+5)*Mathf.Deg2Rad;var dir=V(Mathf.Cos(a),0,Mathf.Sin(a));
    R21FloorLine("06_Escala_radial_"+i,new[]{V(117,-16,132)+dir*5.65f,V(117,-16,132)+dir*(i%4==0?4.6f:5.25f)},i%4==0?.11f:.045f,r21Gold);
   }
   R21FloorLine("07_Cicatriz_ritual",new[]{V(111,-16,132),V(112.4f,-16,131.5f),V(113,-16,130.8f),V(114.2f,-16,130.6f)},.06f,r21Dark);
   R21FloorLine("08_Marca_arrastre",new[]{V(108,-16,128),V(110.5f,-16,130.5f),V(111.5f,-16,130.7f)},.04f,r21Dark);
   var center=V(117,-8,132);
   R21Ring("09_Orbita_solar_principal",center,Quaternion.Euler(0,0,12),3.8f,.13f,0,360,r21Metal);
   R21Ring("10_Orbita_transversal",center,Quaternion.Euler(0,72,-18),3.15f,.095f,0,360,r21Gold);
   R21Ring("11_Anillo_contencion_roto",center,Quaternion.Euler(72,18,8),2.55f,.11f,18,312,r21Metal);
   var corners=new[]{V(114,-10.28f,129),V(120,-10.28f,129),V(120,-10.28f,135),V(114,-10.28f,135)};
   for(int i=0;i<4;i++)
   {
    R21Beam("12_Cuna_estructural_"+i,corners[i],corners[(i+1)%4],.18f,r21Metal);
    var roof=R21Roof(s,corners[i]+Vector3.up*.1f);r21Proof.mounts.Add(new A03Face{found=true,requested=corners[i],point=roof.point,normal=roof.normal,collider=Hierarchy(roof.collider.transform)});
    R21Beam("13_Suspension_al_techo_"+i,corners[i],roof.point,.065f,r21Metal);
   }
   R21Beam("14_Apoyo_interior_oeste",V(115.5f,-10.28f,129),V(115.5f,-10.28f,135),.16f,r21Metal);R21Beam("15_Apoyo_interior_este",V(118.5f,-10.28f,129),V(118.5f,-10.28f,135),.16f,r21Metal);
   // Reuse the existing altar's carved stone as the instrument housing, held by the cradle.
   var altar=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Standard Assets/Eternal Temple/Prefabs/Props/Furniture/Stone_Altar_04.prefab");var mf=altar.GetComponentsInChildren<MeshFilter>(true).First();
   var housing=new GameObject("16_Carcasa_pétrea_original",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));housing.transform.SetParent(r21Root.transform,false);housing.transform.localScale=Vector3.one*1.8f;
   var bounds=mf.sharedMesh.bounds;housing.transform.position=V(117,-10.19f+bounds.size.y*.9f,132)-bounds.center*1.8f;housing.GetComponent<MeshFilter>().sharedMesh=mf.sharedMesh;housing.GetComponent<MeshCollider>().sharedMesh=mf.sharedMesh;
   housing.GetComponent<MeshRenderer>().sharedMaterials=mf.GetComponent<Renderer>().sharedMaterials.Select((m,i)=>R21Mat("R21_Carcasa_"+i,new Color(.36f,.33f,.28f),m)).ToArray();
   R21Beam("17_Registro_fisurado",V(119.15f,-7.5f,131.8f),V(119.32f,-6.95f,132.02f),.02f,r21Violet,false);
   R21Beam("18_Ramal_fisura",V(119.21f,-7.3f,131.89f),V(119.57f,-7.12f,131.9f),.012f,r21Violet,false);
   // Warm sources are held by the device; no light or global setting changes elsewhere.
   R21Beam("19_Franja_ambar_a",V(115.55f,-10.18f,130.1f),V(115.55f,-10.18f,131.1f),.045f,r21Amber,false);
   R21Beam("20_Franja_ambar_b",V(118.45f,-10.18f,132.9f),V(118.45f,-10.18f,133.9f),.045f,r21Amber,false);
   R21Light("Luz_cálida_ceremonial",V(117,-10.5f,132),V(117,-16,132),new Color(1,.69f,.36f),4,14,115);
   R21Light("Luz_rasante_occidental",V(104,-9.8f,125),V(106,-14,131),new Color(1,.62f,.30f),2.5f,12,75);
   R21Light("Luz_residual_acceso",V(126,-9.5f,111),V(121,-16,118),new Color(1,.73f,.45f),2,13,100);
   EditorSceneManager.MarkSceneDirty(s);AssetDatabase.SaveAssets();R21Validate(s,false);R21View(0);
  }
  static List<Station> R21Grid(Scene s)
  {
   var cfg=ConstructionConfig4(s);var result=new List<Station>();for(int z=103;z<144;z+=2)for(int x=103;x<132;x+=2)result.Add(Probe(V(x,-16,z),cfg,false,.03f));return result;
  }
  static void R21Validate(Scene s,bool save)
  {
   if(r21Proof==null)r21Proof=JsonUtility.FromJson<R21Proof>(File.ReadAllText(File.Exists(R21Work+"/Validacion.json")?R21Work+"/Validacion.json":R21Work+"/Base.json"));Physics.SyncTransforms();r21Proof.problems.Clear();
   r21Proof.originalCollidersPreserved=R21Physics(s)==r21Proof.originalPhysics;r21Proof.a03After=R21A03(s);r21Proof.a03Preserved=r21Proof.a03After==r21Proof.a03Before;r21Proof.navPreserved=Hash(Nav)==r21Proof.nav&&TriangleDigest4()==r21Proof.digest;
   r21Proof.after=R21Grid(s);r21Proof.gridPreserved=r21Proof.before.Count==r21Proof.after.Count;
   for(int i=0;i<r21Proof.after.Count&&r21Proof.gridPreserved;i++)
   {
    var a=r21Proof.before[i];var b=r21Proof.after[i];if(a.Clear&&!b.Clear||a.nav&&!b.nav)r21Proof.gridPreserved=false;
   }
   var root=s.GetRootGameObjects().Single(g=>g.name==R21Root);var newCols=root.GetComponentsInChildren<Collider>(true);r21Proof.newColliders=newCols.Length;r21Proof.minimumNewColliderHeight=newCols.Min(c=>c.bounds.min.y+16);
   var cfg=ConstructionConfig4(s);var floor=V(117,-16,132);if(cfg.physics.Raycast(floor+Vector3.up*.1f,Vector3.up,out var roof,30,cfg.mask,QueryTriggerInteraction.Ignore))r21Proof.minimumMeasuredHeadroom=roof.point.y+16;
   var points=new[]{V(117,-16,123),V(117,-16,141),V(105,-16,123),V(129,-16,123),V(105,-16,139),V(129,-16,139)};r21Proof.localRoutesClear=true;r21Proof.widths.Clear();r21Proof.localNative.Clear();
   foreach(var p in points)foreach(bool reverse in new[]{false,true})
   {
    var plan=P("R21 eje local","Centro","Acceso",reverse?p:floor,reverse?floor:p);var guide=InspectTrace("R21 suelo y cápsula",plan.points,plan,cfg,true);var route=Native(plan.points[0],plan.points[1],plan,cfg,"R21",guide.clear&&guide.missingNav==0,false);
    r21Proof.widths.AddRange(guide.widths);r21Proof.localNative.Add(route);
    if(!guide.clear||guide.missingNav!=0||!route.complete||!route.physical){r21Proof.localRoutesClear=false;r21Proof.problems.Add(JsonUtility.ToJson(plan));}
   }
   r21Proof.additions=root.GetComponentsInChildren<Renderer>(true).Select(r=>new A03Object{path=Hierarchy(r.transform),type=r.GetType().Name,center=r.bounds.center,size=r.bounds.size,materials=r.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray()}).ToList();
   File.WriteAllText(R21Work+"/Validacion.json",JsonUtility.ToJson(r21Proof,true));
   if(!r21Proof.originalCollidersPreserved||!r21Proof.a03Preserved||!r21Proof.navPreserved||!r21Proof.gridPreserved||!r21Proof.localRoutesClear||r21Proof.minimumNewColliderHeight<3.2f)throw new InvalidOperationException("La intervención no conserva la seguridad o integridad. No se guarda.");
   if(save)
   {
    if(!File.Exists(R21Work+"/Revision_visual.txt")||!r21Proof.regressions||!r21Proof.automaticRoutes)throw new InvalidOperationException("Falta revisión visual o regresiones reales.");
    if(!EditorSceneManager.SaveScene(s))throw new IOException("Falló SaveScene.");r21Proof.saved=true;r21Proof.sceneAfter=Hash(City);File.WriteAllText(R21Work+"/Validacion.json",JsonUtility.ToJson(r21Proof,true));
   }
  }
  static void R21Regressions(Scene s)
  {
   if(r21Proof==null)r21Proof=JsonUtility.FromJson<R21Proof>(File.ReadAllText(R21Work+"/Validacion.json"));
   r21Proof.regressions=AutoRegressions(R21Work+"/Regresiones_Etapas1a7");var routes=RunAutomaticRoutes(R21Work+"/Regresion_rutas",s.isDirty);r21Proof.automaticRoutes=routes.Count==8&&routes.All(r=>r.navigable);
   File.WriteAllText(R21Work+"/Validacion.json",JsonUtility.ToJson(r21Proof,true));if(!r21Proof.regressions||!r21Proof.automaticRoutes)throw new InvalidOperationException("Alguna regresión no pasó; se conserva el resultado para diagnóstico.");
  }
  static void R21Reload(Scene s)
  {
   var proof=JsonUtility.FromJson<R21Proof>(File.ReadAllText(R21Work+"/Validacion.json"));if(s.isDirty||!proof.saved||Hash(City)!=proof.sceneAfter)throw new InvalidOperationException("No se descarta ni sustituye una escena con cambios.");
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);s=SceneManager.GetSceneByPath(City);var grid=R21Grid(s);
   proof.reloaded=Hash(Nav)==proof.nav&&TriangleDigest4()==proof.digest&&!s.isDirty&&s.GetRootGameObjects().Count(g=>g.name==R21Root)==1&&grid.Count==proof.after.Count&&grid.Where((q,i)=>proof.after[i].Clear&&!q.Clear||proof.after[i].nav&&!q.nav).Count()==0;
   File.WriteAllText(R21Work+"/Validacion_recarga.json",JsonUtility.ToJson(proof,true));if(!proof.reloaded)throw new InvalidOperationException("Falló recepción tras recarga.");R21View(0);
  }
  static Vector3 R21WallPoint(Scene s,Vector2 p)
  {
   RaycastHit best=default;float near=100;var ray=new Ray(V(p.x,p.y,140),Vector3.forward);
   foreach(var c in r21WallColliders??(r21WallColliders=R21Nave(s).GetComponentsInChildren<MeshCollider>()))
   {
    if(c.GetComponent<MeshFilter>().sharedMesh!=c.sharedMesh)throw new InvalidOperationException("El soporte físico y visual del friso no coincide.");
    if(c.Raycast(ray,out var h,5)&&h.distance<near&&h.normal.z<-.7f){best=h;near=h.distance;}
   }
   if(!best.collider)throw new InvalidOperationException("No hay piedra real para el friso: "+p);return best.point+best.normal*.009f;
  }
  static void R21WallLine(Scene s,string name,Vector2[] points,float width,Material mat)
  {
   var vs=new List<Vector3>();var ts=new List<int>();
   for(int i=1;i<points.Length;i++)
   {
    var delta=(points[i]-points[i-1]).normalized;var side=new Vector2(-delta.y,delta.x)*width*.5f;int n=vs.Count;
    vs.Add(R21WallPoint(s,points[i-1]+side));vs.Add(R21WallPoint(s,points[i-1]-side));vs.Add(R21WallPoint(s,points[i]-side));vs.Add(R21WallPoint(s,points[i]+side));ts.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
   }
   if(mat.name=="R21_Fractura_dimensional")for(int i=0;i<vs.Count;i++)vs[i]-=Vector3.forward*.004f;
   R21Mesh(name,vs,ts,mat,false);
  }
  static void R21Details(Scene s)
  {
   r21Root=s.GetRootGameObjects().Single(g=>g.name==R21Root);
   if(r21Root.transform.Find("21_Diagrama_solar_norte_a"))throw new InvalidOperationException("No duplicar el acabado.");
   r21Metal=AssetDatabase.LoadAssetAtPath<Material>(R21Assets+"/R21_Metal_oxidado.mat");r21Gold=AssetDatabase.LoadAssetAtPath<Material>(R21Assets+"/R21_Grabado_solar.mat");r21Violet=AssetDatabase.LoadAssetAtPath<Material>(R21Assets+"/R21_Fractura_dimensional.mat");
   var c=new Vector2(117,-3.35f);R21WallLine(s,"21_Diagrama_solar_norte_a",A03Arc(c,2.15f,15,168,50),.065f,r21Gold);
   R21WallLine(s,"22_Diagrama_solar_norte_b",A03Arc(c,2.15f,196,348,50),.065f,r21Gold);
   R21WallLine(s,"23_Limite_de_contencion",A03Arc(c,1.65f,0,360,80),.032f,r21Gold);
   R21WallLine(s,"24_Eje_de_medicion",new[]{new Vector2(114.35f,-3.35f),new Vector2(119.65f,-3.35f)},.04f,r21Gold);
   for(int i=0;i<5;i++){float x=115.8f+i*.6f;R21WallLine(s,"25_Escala_mural_"+i,new[]{new Vector2(x,-5.9f),new Vector2(x,-5.55f)},.035f,r21Gold);}
   var scar=new[]{new Vector2(121.5f,-5.6f),new Vector2(121.34f,-5.1f),new Vector2(121.52f,-4.7f),new Vector2(121.28f,-4.25f),new Vector2(121.40f,-3.85f)};
   R21WallLine(s,"25b_Fractura_oscura_del_muro",scar,.055f,AssetDatabase.LoadAssetAtPath<Material>(R21Assets+"/R21_Incisiones.mat"));
   R21WallLine(s,"25c_Residuo_violeta_del_muro",scar,.012f,r21Violet);
   // Replace the first free-space accent with a crack projected onto its real housing.
   foreach(string name in new[]{"17_Registro_fisurado","18_Ramal_fisura"}){var old=r21Root.transform.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);}
   var housing=r21Root.transform.Find("16_Carcasa_pétrea_original").GetComponent<MeshCollider>();
   var line=new[]{new Vector2(117.5f,-9.3f),new Vector2(117.58f,-9.14f),new Vector2(117.46f,-8.99f),new Vector2(117.62f,-8.82f)};
   var vertices=new List<Vector3>();var triangles=new List<int>();
   for(int i=1;i<line.Length;i++)
   {
    var d=(line[i]-line[i-1]).normalized;var side=new Vector2(-d.y,d.x)*.009f;int n=vertices.Count;
    foreach(var p in new[]{line[i-1]+side,line[i-1]-side,line[i]-side,line[i]+side})
    {
     if(!housing.Raycast(new Ray(V(p.x,p.y,128),Vector3.forward),out var hit,5))throw new InvalidOperationException("Fractura sin carcasa de apoyo.");vertices.Add(hit.point+hit.normal*.012f);
    }
    triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
   }
   R21Mesh("26_Fractura_proyectada_en_carcasa",vertices,triangles,r21Violet,false);
   var fixture=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Standard Assets/Eternal Temple/Prefabs/Lanterns/Lantern_Small_03_Unlit.prefab").GetComponentsInChildren<MeshFilter>(true).First();
   var lamp=new GameObject("27_Luminaria_del_mecanismo",typeof(MeshFilter),typeof(MeshRenderer));lamp.transform.SetParent(r21Root.transform,false);lamp.transform.position=V(114,-9.25f,129);lamp.transform.localScale=Vector3.one*.7f;lamp.GetComponent<MeshFilter>().sharedMesh=fixture.sharedMesh;
   lamp.GetComponent<MeshRenderer>().sharedMaterials=fixture.GetComponent<Renderer>().sharedMaterials.Select((m,i)=>R21Mat("R21_Luminaria_"+i,new Color(.55f,.44f,.29f),m)).ToArray();
   R21Light("Luz_rasante_del_instrumento",V(114,-8.9f,129),V(117,-8.7f,132),new Color(1,.63f,.28f),18,10,80);
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s);R21Validate(s,false);R21View(1);
  }
  static void R21Polish(Scene s)
  {
   r21Root=s.GetRootGameObjects().Single(g=>g.name==R21Root);
   string backup=R21Work+"/Antes_patina";if(Directory.Exists(backup))throw new InvalidOperationException("No sobrescribir el punto de revisión.");Directory.CreateDirectory(backup);
   foreach(string file in Directory.GetFiles(R21Assets))File.Copy(file,backup+"/"+Path.GetFileName(file));
   var metal=AssetDatabase.LoadAssetAtPath<Material>(R21Assets+"/R21_Metal_oxidado.mat");var glyph=AssetDatabase.LoadAssetAtPath<Material>(R21Assets+"/R21_Grabado_solar.mat");
   metal.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HIVEMIND/PostApocalypticTown/URP/Art/Textures/Metal/T_Rust_A.png"));
   metal.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HIVEMIND/PostApocalypticTown/URP/Art/Textures/Metal/T_Metal_N.png"));metal.EnableKeyword("_NORMALMAP");metal.SetFloat("_BumpScale",.3f);metal.SetColor("_BaseColor",new Color(.78f,.72f,.61f));metal.SetFloat("_Smoothness",.12f);
   glyph.SetColor("_BaseColor",new Color(.29f,.245f,.17f));
   foreach(var m in new[]{metal,glyph}){m.SetFloat("_SpecularHighlights",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");m.SetFloat("_EnvironmentReflections",0);m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");EditorUtility.SetDirty(m);}
   foreach(var mf in r21Root.GetComponentsInChildren<MeshFilter>(true))
   {
    var renderer=mf.GetComponent<MeshRenderer>();if(!renderer||!mf.GetComponent<Collider>())continue;
    if(renderer.sharedMaterial==glyph)renderer.sharedMaterial=metal;
    if(renderer.sharedMaterial!=metal)continue;
    var mesh=mf.sharedMesh;string path=AssetDatabase.GetAssetPath(mesh);
    if(!path.StartsWith(R21Assets+"/",StringComparison.Ordinal))continue; // Never change imported/prefab meshes.
    mesh.uv=mesh.vertices.Select(v=>new Vector2(v.x*.28f+v.z*.12f,v.y*.28f+v.z*.18f)).ToArray();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
   }
   // Visible mechanical bearings: no concentric element is left suspended in space.
   var center=V(117,-8,132);
   var outer=Quaternion.Euler(0,0,12);var middle=Quaternion.Euler(0,72,-18);var inner=Quaternion.Euler(72,18,8);
   foreach(int side in new[]{-1,1})
   {
    R21Beam("28_Eje_orbita_exterior_"+side,center,center+outer*Vector3.right*(3.8f*side),.10f,metal);
    R21Beam("29_Eje_orbita_transversal_"+side,center,center+middle*Vector3.right*(3.15f*side),.085f,metal);
   }
   foreach(float angle in new[]{90f,225f}){float a=angle*Mathf.Deg2Rad;R21Beam("30_Apoyo_anillo_fracturado_"+angle,center,center+inner*V(Mathf.Cos(a)*2.55f,Mathf.Sin(a)*2.55f,0),.075f,metal);}
   var west=r21Root.transform.Find("14_Apoyo_interior_oeste");var east=r21Root.transform.Find("15_Apoyo_interior_este");
   var wp=west.position;wp.x=116.45f;west.position=wp;var ep=east.position;ep.x=117.55f;east.position=ep;
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s);R21Validate(s,false);R21View(1);
  }
  static void R21StoneFinish(Scene s)
  {
   var mat=AssetDatabase.LoadAssetAtPath<Material>(R21Assets+"/R21_Piedra_sombría.mat");
   File.Copy(R21Assets+"/R21_Piedra_sombría.mat",R21Work+"/Piedra_antes_remate.mat",false);
   const string textures="Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Textures/";
   mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"T_StoneWallNEW_Damage_BaseColor.png"));
   mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"T_StoneWallNEW_Damage_Normal.png"));
   mat.SetTextureScale("_BaseMap",Vector2.one*.5f);mat.SetTextureScale("_BumpMap",Vector2.one*.5f);mat.SetTexture("_SpecGlossMap",null);mat.DisableKeyword("_SPECGLOSSMAP");mat.EnableKeyword("_NORMALMAP");
   mat.SetFloat("_SpecSource",1);mat.SetFloat("_BumpScale",.65f);var color=new Color(.17f,.185f,.175f,mat.GetColor("_BaseColor").a);mat.SetColor("_BaseColor",color);mat.SetColor("_Color",color);
   EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s);R21Validate(s,false);R21View(5);
  }
 }
}
