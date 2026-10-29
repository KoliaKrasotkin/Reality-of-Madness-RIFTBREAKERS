using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Rendering;
namespace ROM.Tutorial.Crilium.Editor {
// Visual-only regional dressing for Crelium following the illustrated Crelium map: Vitral and Saara Woods at the start,
// the river and Gibahar/Quahar to the east, Old Sanctuary, Ashara Dunes and Eshkar Ruins to the west, the Great Desert
// and pyramid to the north, and both seas beyond. Everything lives under one root without colliders: walkable areas,
// hidden limits, lessons, respawns and NavMesh are untouched.
public static class CriliumMapEvolved {
 const string RootName="Crelium map regions - editable";const string Art="Assets/ROM/Tutorial/Crilium/Art/";
 static string Output=>Path.GetFullPath("Logs/CreliumMapEvolved");
 static readonly string[] Sources={"Assets/Standard Assets/Sun_Temple","Assets/Standard Assets/Eternal Temple","Assets/LeartesStudios/The_Grand_Egyptian_Temple","Assets/LeartesStudios/PirateTavern","Assets/LeartesStudios/The_Rally_Point","Assets/ROM/Levels/RiftSetDressing"};
 static readonly Dictionary<string,GameObject> cache=new Dictionary<string,GameObject>();
 static System.Random rng;static Rect[] areas;static Transform root;static int placed;
 static float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
 static T Pick<T>(params T[] items)=>items[rng.Next(items.Length)];
 static Bounds Bounds(Transform t){var rs=t.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)&&r.bounds.size.sqrMagnitude>0).ToArray();if(rs.Length==0)return new Bounds(t.position,Vector3.zero);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static GameObject Prefab(string name){if(cache.TryGetValue(name,out var hit))return hit;var path=AssetDatabase.FindAssets(name+" t:Prefab",Sources).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p)==name);if(path==null)throw new Exception("Missing existing prefab "+name);return cache[name]=AssetDatabase.LoadAssetAtPath<GameObject>(path);}
 // Distance from a ground point to the nearest walkable area; negative inside.
 static float Clearance(Vector2 p){float best=float.MaxValue;foreach(var a in areas){float dx=Mathf.Max(a.xMin-p.x,0,p.x-a.xMax),dz=Mathf.Max(a.yMin-p.y,0,p.y-a.yMax);float d=dx>0||dz>0?Mathf.Sqrt(dx*dx+dz*dz):-Mathf.Min(Mathf.Min(p.x-a.xMin,a.xMax-p.x),Mathf.Min(p.y-a.yMin,a.yMax-p.y));best=Mathf.Min(best,d);}return best;}
 static Transform Group(string name,Transform parent=null){var go=new GameObject(name);go.transform.SetParent(parent?parent:root,false);return go.transform;}
 // Instantiates an existing prefab, fits it to a height, seats it on the ground and strips every collider.
 static GameObject Put(string prefab,Transform parent,Vector3 at,float height,float yaw=0,float sink=0){var go=(GameObject)PrefabUtility.InstantiatePrefab(Prefab(prefab));go.transform.SetParent(parent,false);go.transform.position=at;go.transform.rotation=Quaternion.Euler(0,yaw,0);var b=Bounds(go.transform);if(b.size.y>.01f)go.transform.localScale*=height/b.size.y;b=Bounds(go.transform);go.transform.position+=Vector3.up*(at.y-b.min.y-sink);foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);placed++;return go;}
 static GameObject Shape(PrimitiveType type,string name,Transform parent,Vector3 at,Vector3 scale,Material m,float yaw=0){var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.position=at;go.transform.rotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=m;placed++;return go;}
 static Material Mat(string name,Func<Material> make,Action<Material> tune){var path=Art+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=make();AssetDatabase.CreateAsset(m,path);}tune(m);EditorUtility.SetDirty(m);return m;}
 static Material Existing(string name,string folder)=>AssetDatabase.FindAssets(name+" t:Material",new[]{folder}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>Path.GetFileNameWithoutExtension(p)==name).Select(AssetDatabase.LoadAssetAtPath<Material>).First();
 static void Tint(Material m,Color c){if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);}
 static Material Water(string name,Color c,Color emission){return Mat(name,()=>new Material(Shader.Find("Universal Render Pipeline/Lit")),m=>{Tint(m,c);m.SetFloat("_Smoothness",.92f);m.SetFloat("_Metallic",.05f);if(emission.maxColorComponent>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission);}});}
 // Scatters prefabs inside a rectangle while keeping clear of the route.
 static void Scatter(Transform parent,Rect rect,int count,float minClear,Func<Vector2,bool> accept,Action<Vector3> put){int tries=0;for(int n=0;n<count&&tries<count*30;tries++){var p=new Vector2(R(rect.xMin,rect.xMax),R(rect.yMin,rect.yMax));if(Clearance(p)<minClear||accept!=null&&!accept(p))continue;put(new Vector3(p.x,0,p.y));n++;}}
 static void Tree(Transform parent,Vector3 at){var kind=Pick("Fol_TallTree_A","Fol_TallTree_B","Tree_01","Tree_02","Fol_Pine_A","Fol_Pine_B","Fol_TallTree_A");float h=kind.StartsWith("Fol_Pine")?R(12,17):kind.StartsWith("Tree_")?R(15,22):R(13,19);Put(kind,parent,at,h,R(0,360),.2f);}
 // River from the Sea of Embers in the south-east up past Gibahar into the northern dunes.
 static readonly Vector2[] river={new Vector2(300,-470),new Vector2(190,-330),new Vector2(118,-210),new Vector2(96,-120),new Vector2(88,-55),new Vector2(100,0),new Vector2(92,60),new Vector2(106,120),new Vector2(118,180),new Vector2(132,250),new Vector2(160,360),new Vector2(150,470)};
 const string MeshFolder="Assets/ROM/Tutorial/Crilium/Dressing/Meshes/MapRegions/";
 static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material m){mesh.name=name;mesh.RecalculateNormals();mesh.RecalculateBounds();var path=MeshFolder+name.Replace(' ','_')+".asset";AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(mesh,path);var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;placed++;return go;}
 // Flat patch with an irregular, wind-worn outline instead of a square plane. UVs are world-space (uvScale metres per tile).
 static GameObject Blob(string name,Transform parent,Vector3 center,float rx,float rz,Material m,int seed,float uvScale=10,float wobble=.28f){var rnd=new System.Random(seed);float p1=(float)rnd.NextDouble()*6.3f,p2=(float)rnd.NextDouble()*6.3f,p3=(float)rnd.NextDouble()*6.3f;const int n=72;var v=new Vector3[n+1];var uv=new Vector2[n+1];var tris=new int[n*3];v[0]=center;
  for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;float k=1+wobble*(.55f*Mathf.Sin(3*a+p1)+.3f*Mathf.Sin(7*a+p2)+.15f*Mathf.Sin(13*a+p3));v[i+1]=center+new Vector3(Mathf.Cos(a)*rx*k,0,Mathf.Sin(a)*rz*k);}
  for(int i=0;i<=n;i++)uv[i]=new Vector2(v[i].x,v[i].z)/uvScale;for(int i=0;i<n;i++){tris[i*3]=0;tris[i*3+1]=(i+1)%n+1;tris[i*3+2]=i+1;}
  return MeshObject(name,parent,new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt16,vertices=v,uv=uv,triangles=tris},m);}
 static Vector2 CatmullRom(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float t)=>.5f*(2*b+(c-a)*t+(2*a-5*b+4*c-d)*t*t+(3*b-a-3*c+d)*t*t*t);
 // Continuous smooth strip along a polyline; width breathes along its length so it never reads as straight tiles.
 static GameObject Ribbon(string name,Transform parent,Vector2[] line,float width,float y,Material m,float uvScale=12){var pts=new List<Vector2>();for(int i=0;i<line.Length-1;i++){var a=line[Mathf.Max(0,i-1)];var b=line[i];var c=line[i+1];var d=line[Mathf.Min(line.Length-1,i+2)];for(int s=0;s<12;s++)pts.Add(CatmullRom(a,b,c,d,s/12f));}pts.Add(line[line.Length-1]);
  var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();float along=0;for(int i=0;i<pts.Count;i++){var dir=(pts[Mathf.Min(i+1,pts.Count-1)]-pts[Mathf.Max(i-1,0)]).normalized;var side=new Vector2(dir.y,-dir.x);if(i>0)along+=Vector2.Distance(pts[i],pts[i-1]);float w=width*(.5f+.12f*Mathf.Sin(along*.045f)+.06f*Mathf.Sin(along*.13f));
   foreach(var sign in new[]{-1,1}){var p=pts[i]+side*w*sign;v.Add(new Vector3(p.x,y,p.y));uv.Add(p/uvScale);}if(i>0){int k=v.Count-4;tris.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}}
  return MeshObject(name,parent,new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,vertices=v.ToArray(),uv=uv.ToArray(),triangles=tris.ToArray()},m);}
 // Keeps distant scenery from dropping its last LOD: the map is read from far away.
 static void KeepVisibleFar(){foreach(var lg in root.GetComponentsInChildren<LODGroup>(true)){var lods=lg.GetLODs();if(lods.Length==0)continue;lods[lods.Length-1].screenRelativeTransitionHeight=Mathf.Min(lods[lods.Length-1].screenRelativeTransitionHeight,.002f);for(int i=lods.Length-2;i>=0;i--)lods[i].screenRelativeTransitionHeight=Mathf.Max(lods[i].screenRelativeTransitionHeight,lods[i+1].screenRelativeTransitionHeight+.001f);lg.SetLODs(lods);}}
 static float RiverDistance(Vector2 p){float best=float.MaxValue;for(int i=0;i<river.Length-1;i++){var a=river[i];var ab=river[i+1]-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);best=Mathf.Min(best,Vector2.Distance(p,a+ab*t));}return best;}

 [MenuItem("ROM/Crilium/Apply Crelium map regions (visual only)")]
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before visual edit");Directory.CreateDirectory(Output);
  var scene=EditorSceneManager.OpenScene(CriliumSession.ScenePath,OpenSceneMode.Single);if(scene.name!="Crilium")throw new Exception("Wrong scene");
  var limits=UnityEngine.Object.FindFirstObjectByType<CriliumBounds>();if(!limits||limits.Areas==null||limits.Areas.Length==0)throw new Exception("Crelium limits missing");areas=limits.Areas;
  var physicsBefore=PhysicsSignature();
  var old=GameObject.Find(RootName);if(old)UnityEngine.Object.DestroyImmediate(old);root=new GameObject(RootName).transform;rng=new System.Random(20261029);placed=0;

  var sand=AssetDatabase.LoadAssetAtPath<Material>(Art+"CreliumOchreGround.mat");if(!sand)throw new Exception("Crelium sand material missing");
  var grass=Mat("CreliumSaaraGround",()=>new Material(sand),m=>{Tint(m,new Color(.52f,.66f,.36f));if(m.HasProperty("_BaseMap"))m.SetTextureScale("_BaseMap",Vector2.one);});
  var dryGrass=Mat("CreliumDryGround",()=>new Material(sand),m=>{Tint(m,new Color(.78f,.8f,.55f));if(m.HasProperty("_BaseMap"))m.SetTextureScale("_BaseMap",Vector2.one);});
  var duneSand=Mat("CreliumDuneSand",()=>new Material(sand),m=>{Tint(m,new Color(1,.95f,.82f));if(m.HasProperty("_BaseMap"))m.SetTextureScale("_BaseMap",new Vector2(12,12));});
  var riverWater=Water("CreliumRiverWater",new Color(.18f,.42f,.47f,1),Color.clear);var solace=Water("CreliumSeaOfSolace",new Color(.13f,.33f,.36f,1),Color.clear);var embers=Water("CreliumSeaOfEmbers",new Color(.2f,.1f,.07f,1),new Color(.32f,.07f,.015f));


  // Continuous desert ground below the route (foundations stay on top at y0). Green only where water is: past the river and at the Old Sanctuary.
  // The sand reaches far into the fog so its edge is never seen; patches use irregular outlines instead of square planes.
  AssetDatabase.DeleteAsset(MeshFolder.TrimEnd('/'));Directory.CreateDirectory(MeshFolder);AssetDatabase.Refresh();
  var ground=Group("Ground - visual only");Shape(PrimitiveType.Plane,"Crelium continental sand",ground,new Vector3(0,-.15f,300),new Vector3(600,1,600),sand).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
  Blob("Saara Woods ground",ground,new Vector3(190,-.08f,40),95,210,grass,11);Blob("Saara Woods ground north",ground,new Vector3(200,-.075f,250),70,90,grass,12);
  Ribbon("Saara river banks",ground,river,46,-.11f,dryGrass,10);
  Blob("Old Sanctuary spring ground",ground,new Vector3(-72,-.07f,44),26,30,dryGrass,13);

  // Vitral: the desert camp where Nertör starts. Tents ring the arrival, a communal fire sits behind the spawn,
  // supplies and a training corner flank it, and torches lead the way north. Small props inside the walkable square
  // keep to its sides; nothing is placed on the walking line x≈0.
  var vitral=Group("Vitral desert camp");
  var tents=Group("Tents",vitral);
  // Same red cloth tents as the existing arrival camp; canvas tarps shade the corners.
  (string,Vector3,float)[] tentSpots={("SM_TradingTent_A",new Vector3(-32,0,-11),4.8f),("SM_TradingTent_B",new Vector3(-31,0,5),5f),("SM_TradingTent_B",new Vector3(-31,0,18),4.6f),("SM_TradingTent_B",new Vector3(31,0,-12),5f),("SM_TradingTent_A",new Vector3(32,0,5),4.8f),("SM_TradingTent_B",new Vector3(31,0,18),4.6f),("SM_TradingTent_B",new Vector3(-15,0,-32),5f),("SM_TradingTent_A",new Vector3(0,0,-33),4.8f),("SM_TradingTent_B",new Vector3(15,0,-32),5f),("SM_Canopy_C",new Vector3(-30,0,-31),2.6f),("SM_Canopy_A",new Vector3(30,0,-31),2.4f)};
  foreach(var t in tentSpots)Put(t.Item1,tents,t.Item2,t.Item3,Face(t.Item2,new Vector3(0,0,-4))).name="Camp tent - "+t.Item1;
  // Palisade of fence and flag lines behind the tents, open to the north where the route leaves the camp.
  var palisade=Group("Palisade and banners",vitral);
  Line("SM_Fencing_A",palisade,new Vector3(-40,0,26),new Vector3(-40,0,-38),1.7f);Line("SM_Fencing_A",palisade,new Vector3(-40,0,-40),new Vector3(40,0,-40),1.7f);Line("SM_Fencing_A",palisade,new Vector3(40,0,-38),new Vector3(40,0,26),1.7f);
  foreach(var p in new[]{new Vector3(-40,0,28),new Vector3(40,0,28),new Vector3(-40,0,-40),new Vector3(40,0,-40),new Vector3(-6,0,-40),new Vector3(6,0,-40)})Put("SM_FlagPole",palisade,p,7.5f,0);
  Put("SM_FlagsRope_A",palisade,new Vector3(-31,0,-22),4.2f,90);Put("SM_FlagsRope_A",palisade,new Vector3(31,0,-22),4.2f,90);Put("SM_FlagsRope_A",palisade,new Vector3(0,0,-27),4.2f,0);
  // Communal fire south of the spawn: logs, benches, rugs and a cooking pot.
  var hearth=Group("Communal fire",vitral);var fire=new Vector3(0,0,-14);Fx("FX_Bonfire_A",hearth,fire,2.4f);Fx("FX_Smoke_01",hearth,fire+Vector3.up*2,3);
  for(int k=0;k<9;k++){float a=k*Mathf.PI*2/9;Put(Pick("Prop_Stone_Small_A","Prop_Stone_Small_B"),hearth,fire+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*1.9f,.45f,R(0,360));}
  for(int k=0;k<5;k++){float a=k*Mathf.PI*2/5+.3f;var p=fire+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*3.8f;Put(k%2==0?"Prop_Roughbench_A":"Prop_Roughbench_B",hearth,p,.8f,Face(p,fire)+90);Put(Pick("Prop_Rug_C","Prop_Rug_F","Prop_Rug_I"),hearth,p+(p-fire).normalized*1.6f,.05f,Face(p,fire));}
  Put("Prop_KitchenPot_C",hearth,fire+new Vector3(1.2f,0,-1.1f),.7f);Put("Prop_Firewood_A",hearth,fire,1.1f,40).name="Fire logs under the bonfire FX";Put("Prop_WoodPile_B",hearth,fire+new Vector3(-6,0,2.5f),.9f,40);Put("Prop_FoodBasket_B",hearth,fire+new Vector3(5.6f,0,-1.5f),.7f);Put("Prop_KitchenJug_A",hearth,fire+new Vector3(-4.6f,0,-3),.5f);
  // Small trading spots either side of the walking line, between the existing arrival tents and the fire.
  var market=Group("Camp market",vitral);foreach(var spot in new[]{new Vector3(-11,0,-9),new Vector3(-12,0,-3.5f),new Vector3(11,0,-9),new Vector3(12,0,-2.5f)}){float yaw=spot.x<0?90:-90;Put(Pick("Prop_Rug_D","Prop_Rug_G","Prop_Rug_J"),market,spot,.05f,yaw);
   Put(Pick("Prop_ClayPot_Grouped_B","Prop_ClayPot_Grouped_C","SM_PotterySet2_polySurface8"),market,spot+new Vector3(spot.x<0?-1:1,0,.6f),.9f,R(0,360));Put(Pick("Prop_Basket_A","Prop_FoodBasket_C","SM_Jug"),market,spot+new Vector3(spot.x<0?.6f:-.6f,0,-.8f),.6f,R(0,360));Put("Prop_Crate_B",market,spot+new Vector3(spot.x<0?-2.2f:2.2f,0,-1),.8f,R(0,360));}
  foreach(var p in new[]{new Vector3(-14,0,-14),new Vector3(14,0,-15),new Vector3(-26,0,-24),new Vector3(26,0,-25)})Fx("P_Dust",market,p+Vector3.up*.3f,1);
  // Supplies on the east side, training corner on the west side.
  var supplies=Group("Supplies",vitral);Put("SM_Canopy_B",supplies,new Vector3(20.5f,0,-14),2.6f,90);(string,Vector3,float)[] goods={("SM_BarrelsInNet_A",new Vector3(19,0,-21),2f),("Prop_BarrelCrates_A",new Vector3(22,0,-16),1.7f),("Prop_GrainSack_B",new Vector3(20,0,-11),.8f),("Prop_GrainSack_C",new Vector3(21.5f,0,-9.5f),.8f),("Prop_ClayPot_Grouped_A",new Vector3(22,0,-6),1.1f),("SM_JugBIG",new Vector3(19.5f,0,-4),1.2f),("Prop_Crate_A",new Vector3(22.5f,0,-21.5f),1f),("Prop_Crate_B",new Vector3(22,0,-19.6f),.9f),("SM_Bag",new Vector3(19.5f,0,-18),.6f)};
  foreach(var g in goods)Put(g.Item1,supplies,g.Item2,g.Item3,R(0,360));
  var training=Group("Training corner",vitral);Put("SM_EgyptianWeapon2_polySurface45",training,new Vector3(-21,0,-20),2.2f,90);Put("SM_EgyptianWeapon2_polySurface50",training,new Vector3(-21,0,-17.5f),2.2f,90);Put("Prop_StoneBench_A",training,new Vector3(-21.5f,0,-12),.7f,90);Put("Prop_WoodPile_A",training,new Vector3(-21,0,-7),1.2f,90);Put("Prop_Stonestash_B",training,new Vector3(-20,0,-22.5f),.9f,0);
  // Torches either side of the walking line, guiding north towards the first arena.
  var torches=Group("Path torches",vitral);foreach(var z in new[]{16f,30f,44f})foreach(var x in new[]{-7f,7f})Put("Torch_01",torches,new Vector3(x+(z>40?20:0),0,z),2.8f);
  // Oasis west of the camp: the first sign of life, palms around a small pool.
  var oasis=Group("Vitral oasis",vitral);var pool=new Vector3(-58,0,-4);Blob("Oasis pool",oasis,pool+Vector3.up*.02f,10.5f,7.5f,riverWater,21,10,.18f);Blob("Oasis damp ground",oasis,pool+Vector3.up*-.05f,19,14,dryGrass,22);
  for(int k=0;k<9;k++){float a=k*Mathf.PI*2/9+R(-.2f,.2f);var p=pool+new Vector3(Mathf.Cos(a)*R(13,16),0,Mathf.Sin(a)*R(9.5f,12));Put("PiratePalm",oasis,p,R(10,14),R(0,360));}
  for(int k=0;k<10;k++){float a=R(0,Mathf.PI*2);var p=pool+new Vector3(Mathf.Cos(a)*R(11,15),0,Mathf.Sin(a)*R(8,11));Put(Pick("Fol_Bush_A_Grouped","Bush_02","Grass_03","Grass_06"),oasis,p,R(.8f,1.6f),R(0,360));}
  foreach(var p in new[]{new Vector3(-36,0,-26),new Vector3(36,0,-24),new Vector3(-42,0,12),new Vector3(44,0,-6)})Put("PiratePalm",vitral,p,R(9,11),R(0,360)).name="Camp palm";

  // River with palm-lined banks; the Saara Woods only start on the far, green side.
  var water=Group("Saara river");Ribbon("Saara river water",water,river,15,.02f,riverWater);
  for(int i=0;i<RiverPoints().Length-1;i++){var a=RiverPoints()[i];var b=RiverPoints()[i+1];var dir=b-a;var side=new Vector2(dir.y,-dir.x).normalized;for(int k=0;k<4;k++){var p=Vector2.Lerp(a,b,(k+.5f)/4)+side*(k%2==0?11:-11)*R(1,1.3f);Put(k%2==0?"PiratePalm":Pick("Prop_Stone_Grouped_A","Prop_Stone_Grouped_C","Fol_Bush_A_Grouped"),water,new Vector3(p.x,0,p.y),k%2==0?R(11,15):R(1.2f,2.2f),R(0,360));}}
  var saara=Group("Saara Woods");Scatter(saara,new Rect(112,-120,130,330),95,6,p=>RiverDistance(p)>14&&(p-new Vector2(150,95)).magnitude>16,at=>Tree(saara,at));
  Scatter(saara,new Rect(112,-120,130,330),45,4,p=>RiverDistance(p)>11,at=>Put(Pick("Fol_Bush_A_Grouped","Fol_BushFlower_A_Grouped","Fol_BushFlower_E_Grouped","Bush_01","Bush_03"),saara,at,R(1.2f,2.4f),R(0,360)));

  // Gibahar: a woodland camp across the river, mirroring Vitral at a smaller scale.
  var gibahar=Group("Gibahar Forest camp");var g0=new Vector3(150,0,95);Fx("FX_Bonfire_A",gibahar,g0,2f);Fx("FX_Smoke_01",gibahar,g0+Vector3.up*2,2.5f);
  for(int k=0;k<4;k++){float a=k*Mathf.PI*.5f+.8f;var p=g0+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*9;Put(k%2==0?"SM_TradingTent_A":"SM_Canopy_B",gibahar,p,4.2f,Face(p,g0));}
  Put("SM_BarrelsInNet_B",gibahar,g0+new Vector3(4,0,-5),1.8f);Put("Prop_WoodPile_B",gibahar,g0+new Vector3(-5,0,3),1.1f,30);Put("SM_FlagPole",gibahar,g0+new Vector3(0,0,11),7,0);

  // Quahar Outpost: rocky rise with a walled lookout east of the refuge.
  var quahar=Group("Quahar Outpost");Put("Cliff_Formation_03",quahar,new Vector3(92,0,198),22,-30,1);Put("Cliff_Formation_07",quahar,new Vector3(76,0,222),16,40,1);Put("Rock_Facing_B",quahar,new Vector3(84,0,172),9,-70,.5f);
  Put("Building_Preset_05",quahar,new Vector3(84,0,190),14,-110);Put("Wall_Big_01",quahar,new Vector3(74,0,180),7,-20);Put("Wall_Big_02",quahar,new Vector3(70,0,196),7,-80);Put("Gates_01",quahar,new Vector3(72,0,166),8,-40);Put("Torch_01",quahar,new Vector3(68,0,170),2.6f);Put("Torch_01",quahar,new Vector3(76,0,162),2.6f);Put("Barrels_04",quahar,new Vector3(80,0,170),1.4f,20);

  // Old Sanctuary: grassy ruins and its spire on the west flank of Vitral.
  var sanctuary=Group("Old Sanctuary");Spire(Put("Cliff_Formation_11",sanctuary,new Vector3(-78,0,46),44,15,1),"Old Sanctuary spire");Put("Building_Ruins_01",sanctuary,new Vector3(-55,0,30),11,90);Put("Base_Arch_Broken_01",sanctuary,new Vector3(-48,0,52),9,100);Put("Column_Group_02",sanctuary,new Vector3(-58,0,64),8,10);Put("Column_Broken_01",sanctuary,new Vector3(-42,0,40),4,0);Put("Pedestal_03",sanctuary,new Vector3(-44,0,24),2,45);Put("Lantern_03",sanctuary,new Vector3(-40,0,30),3,90);
  for(int k=0;k<8;k++)Put(Pick("Ivy_01","Ivy_03","Bush_02","Grass_04","Grass_07"),sanctuary,new Vector3(R(-70,-42),0,R(20,70)),R(.8f,2),R(0,360));
  foreach(var p in new[]{new Vector3(-66,0,34),new Vector3(-86,0,58),new Vector3(-62,0,60),new Vector3(-90,0,30)})Tree(sanctuary,p);

  // Ashara Dunes: wind-swept sand and a colonnaded gate to the north-west.
  var ashara=Group("Ashara Dunes");for(int k=0;k<9;k++){var p=new Vector3(R(-230,-70),R(-4,-2.5f),R(95,260));if(Clearance(new Vector2(p.x,p.z))<45)continue;Shape(PrimitiveType.Sphere,"Ashara dune "+k,ashara,p,new Vector3(R(45,90),R(7,11),R(30,55)),duneSand,R(0,180)).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
  Put("SM_Gate",ashara,new Vector3(-72,0,120),12,90).name="Ashara gate";foreach(var z in new[]{104f,112f,128f,136f})Put(z<120?"SM_PillarFlower2":"SM_PillarFlower2_Broken_1",ashara,new Vector3(-64,0,z),10,90);Put("SM_Shackal_Low",ashara,new Vector3(-58,0,118),3,90,.4f);Put("SM_Shackal_Low",ashara,new Vector3(-58,0,124),3,90,.4f);

  // Eshkar Ruins: collapsed buildings west of the sentinel arena.
  var eshkar=Group("Eshkar Ruins");Put("Building_Ruins_04",eshkar,new Vector3(-44,0,178),10,90);Put("Building_Ruins_07",eshkar,new Vector3(-50,0,200),9,70);Put("Broken_Wall_03",eshkar,new Vector3(-34,0,160),5,90);Put("Broken_Wall_06",eshkar,new Vector3(-36,0,214),5,60);Put("Column_Broken_02",eshkar,new Vector3(-31,0,188),5,0);Put("SM_PillarFlower2_Broken_2",eshkar,new Vector3(-38,0,226),8,20);Put("SM_EgyptianStatue_Low_BIG",eshkar,new Vector3(-58,0,186),12,80);Put("SM_BrockenShapes",eshkar,new Vector3(-40,0,196),2,0);
  for(int k=0;k<10;k++)Put(Pick("Bricks_01","Bricks_03","Bricks_05","SM_Stone_02","SM_Stone_04"),eshkar,new Vector3(R(-62,-30),0,R(158,232)),R(.5f,1.4f),R(0,360));

  // The Great Desert: dunes and a processional avenue of statues leading the eye to the pyramid.
  var desert=Group("The Great Desert");Put("SM_MainGateBrocken",desert,new Vector3(-100,0,262),20,25).name="Great Desert broken pylon";Put("SM_GranitePillar",desert,new Vector3(-30,0,224),16,0);Put("SM_GranitePillar",desert,new Vector3(30,0,224),16,0);
  for(int k=0;k<10;k++){float z=300+k*75;foreach(var side in new[]{-1,1}){var at=new Vector3(-30+side*R(34,42),0,z+R(-6,6));if(k%3==2)Put(Pick("SM_PillarFlower2_Broken_1","SM_PillarFlower2_Broken_2"),desert,at,R(14,19),R(0,360));else Put(Pick("SM_Pharaon1_Low","SM_Pharaon2_Low","SM_Shackal_Low","SM_LadyStatue_Low"),desert,at,R(15,21),side<0?90:-90,.6f);}}
  Put("SM_Mastaba",desert,new Vector3(-180,0,560),28,30);Put("SM_Mastaba",desert,new Vector3(160,0,720),24,-20);
  for(int k=0;k<34;k++){var p=new Vector3(R(-520,520),R(-5,-3),R(240,1150));if(Mathf.Abs(p.x+30)<70||Clearance(new Vector2(p.x,p.z))<45)continue;Shape(PrimitiveType.Sphere,"Great Desert dune "+k,desert,p,new Vector3(R(60,150),R(9,16),R(40,90)),duneSand,R(0,180)).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}

  // Mountain chains and the two seas from the map.
  var far=Group("Mountains and seas - distant");(Vector3,float,float)[] ridges={(new Vector3(-420,0,-80),110,80),(new Vector3(-360,0,360),90,95),(new Vector3(420,0,240),120,-80),(new Vector3(330,0,-200),80,-120),(new Vector3(-40,0,-330),95,180),(new Vector3(-360,0,900),130,110),(new Vector3(380,0,1000),140,-100)};
  foreach(var r in ridges)Put("Bgr_Mountains",far,r.Item1,r.Item2*1.4f,r.Item3,2);
  // Seas are large irregular bodies that run out into the fog, with a damp shore band so the coast never reads as a cut edge.
  Blob("Sea of Solace shore",far,new Vector3(-1050,.35f,-800),860,760,dryGrass,31,14,.22f);Blob("Sea of Solace",far,new Vector3(-1050,.6f,-800),800,700,solace,31,14,.22f);
  Blob("Sea of Embers shore",far,new Vector3(1080,.35f,-860),860,740,dryGrass,32,14,.22f);Blob("Sea of Embers",far,new Vector3(1080,.6f,-860),800,680,embers,32,14,.22f);
  var azras=Group("Azras Spire",far);Spire(Put("Cliff_Formation_11",azras,new Vector3(-520,.6f,-430),95,0,2),"Azras Spire");Put("Cliff_Formation_05",azras,new Vector3(-490,.6f,-395),30,60,1);Put("Rock_Boulder_B",azras,new Vector3(-550,.6f,-400),14,0,1);Put("Rock_Boulder_C",azras,new Vector3(-500,.6f,-465),11,0,1);
  for(int k=0;k<14;k++){var p=new Vector3(k<7?R(-380,-260):R(240,360),0,R(-300,-80));Put(Pick("Rock_Boulder_A","Rock_Boulder_B","Rock_Facing_A","Rock_Facing_C"),far,p,R(10,22),R(0,360),.6f);}

  BoundaryHints();KeepVisibleFar();
  foreach(var c in root.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
  // Scenery never moves: let Unity batch it. Particle FX roots stay dynamic.
  foreach(var t in root.GetComponentsInChildren<Transform>(true))if(!t.GetComponentInParent<ParticleSystem>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);
  if(PhysicsSignature()!=physicsBefore)throw new Exception("Physical scene changed; refusing to save");
  RecaptureAtlas();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  var renderers=root.GetComponentsInChildren<Renderer>(true).Length;File.WriteAllText(Path.Combine(Output,"applied.txt"),$"Placed {placed} visual objects ({renderers} renderers) under '{RootName}'. Colliders under root: {root.GetComponentsInChildren<Collider>(true).Length}. Physics signature unchanged.\n");
  Debug.Log($"CRELIUM MAP REGIONS APPLIED: {placed} objects, {renderers} renderers");
 }
 static Vector2[] RiverPoints()=>river;
 // Narrows a fitted rock formation into a tall needle, as drawn for the Old Sanctuary and Azras Spire.
 static void Spire(GameObject go,string name){go.name=name;var s=go.transform.localScale;go.transform.localScale=new Vector3(s.x*.4f,s.y,s.z*.4f);}
 // Particle prefabs have no mesh bounds to fit; they are placed and scaled directly.
 static GameObject Fx(string prefab,Transform parent,Vector3 at,float scale){var go=(GameObject)PrefabUtility.InstantiatePrefab(Prefab(prefab));go.transform.SetParent(parent,false);go.transform.position=at;go.transform.localScale*=scale;var mesh=Bounds(go.transform);if(mesh.size.sqrMagnitude>0)go.transform.position+=new Vector3(at.x-mesh.center.x,0,at.z-mesh.center.z);foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);foreach(var ps in go.GetComponentsInChildren<ParticleSystem>(true)){var main=ps.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;}placed++;return go;}
 static float Face(Vector3 from,Vector3 to)=>Mathf.Atan2(to.x-from.x,to.z-from.z)*Mathf.Rad2Deg;
 // Repeats a fence-like prefab end to end along a segment, using its fitted footprint as the step.
 static void Line(string prefab,Transform parent,Vector3 a,Vector3 b,float height){float yaw=Face(a,b)+90;var probe=Put(prefab,parent,a,height,yaw);var size=Bounds(probe.transform).size;float step=Mathf.Max(1,Mathf.Max(size.x,size.z)*.95f);UnityEngine.Object.DestroyImmediate(probe);placed--;
  var dir=(b-a).normalized;float len=Vector3.Distance(a,b);for(float s=step*.5f;s<len;s+=step)Put(prefab,parent,a+dir*s,height,yaw);}
 // Low rocks/shrubs just outside the hidden limit walls so the edge of the playable path is readable.
 static void BoundaryHints(){var hints=Group("Route edge hints");var walls=GameObject.Find("Crelium hidden level limits").GetComponentsInChildren<BoxCollider>();foreach(var w in walls){var c=w.transform.position;bool alongX=w.size.x>w.size.z;float len=alongX?w.size.x:w.size.z;var axis=alongX?Vector3.right:Vector3.forward;var normal=alongX?Vector3.forward:Vector3.right;
   var probe=new Vector2(c.x+normal.x*2,c.z+normal.z*2);if(Clearance(probe)<0)normal=-normal;
   for(float s=-len*.5f+2;s<len*.5f-1;s+=R(3.5f,5.5f)){var p=c+axis*s+normal*R(1.6f,3.2f);p.y=0;if(Clearance(new Vector2(p.x,p.z))<1.2f)continue;bool forest=p.x>100;Put(forest?Pick("Fol_Bush_A_Grouped","Fol_BushFlower_A_Grouped","Bush_01","Bush_03","Prop_Stone_Grouped_B"):Pick("Prop_Stone_Grouped_A","Prop_Stone_Grouped_D","Prop_Stone_Med_A","Grass_04_Dry","Rocks_Decal_02"),hints,p,forest?R(1.2f,2.2f):R(.7f,1.6f),R(0,360));}}}
 // Same literal top-down capture the content builder uses, so map and minimap show the new regions.
 static void RecaptureAtlas(){const string Data="Assets/ROM/Tutorial/Crilium/Resources/Crilium/";var capture=new GameObject("Crilium atlas capture").AddComponent<Camera>();capture.orthographic=true;capture.orthographicSize=125;capture.aspect=1;capture.transform.position=new Vector3(20,250,100);capture.transform.rotation=Quaternion.Euler(90,0,0);capture.clearFlags=CameraClearFlags.SolidColor;capture.backgroundColor=new Color(.035f,.05f,.075f);capture.nearClipPlane=.1f;capture.farClipPlane=500;
  var rt=new RenderTexture(1024,1024,24);capture.targetTexture=rt;capture.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1024,1024,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1024,1024),0,0);texture.Apply();File.WriteAllBytes(Data+"CriliumAtlas.png",texture.EncodeToPNG());RenderTexture.active=previous;capture.targetTexture=null;UnityEngine.Object.DestroyImmediate(capture.gameObject);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(Data+"CriliumAtlas.png");}
 static string PhysicsSignature(){var s=new StringBuilder();foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>!root||!c.transform.IsChildOf(root)).Select(c=>c.GetType().Name+c.bounds.center.ToString("F3")+c.bounds.size.ToString("F3")+c.isTrigger).OrderBy(x=>x,StringComparer.Ordinal))s.AppendLine(c);return s.ToString();}

 public static void Capture(){Directory.CreateDirectory(Output);EditorSceneManager.OpenScene(CriliumSession.ScenePath,OpenSceneMode.Single);
  var bounds=UnityEngine.Object.FindFirstObjectByType<CriliumBounds>();if(!bounds.Contains(new Vector3(0,1,0))||bounds.Contains(new Vector3(-30,1,540)))throw new Exception("Bounds validation failed");
  var regions=GameObject.Find(RootName);if(!regions)throw new Exception("Regions missing");if(regions.GetComponentsInChildren<Collider>(true).Length>0)throw new Exception("Regions must not collide");
  Photo("01-campamento-desde-spawn.png",new Vector3(2,3.2f,6),new Vector3(0,1.5f,-26),64);Photo("01b-campamento-hacia-norte.png",new Vector3(0,3.2f,-24),new Vector3(0,3,25),64);Photo("02-oasis.png",new Vector3(-22,3.5f,4),new Vector3(-58,3,-4),64);Photo("03-rio-y-gibahar.png",new Vector3(60,6,80),new Vector3(130,4,95),62);
  Photo("04-quahar-desde-refugio.png",new Vector3(30,4,140),new Vector3(85,8,190),62);Photo("05-eshkar-desde-arena.png",new Vector3(10,4,175),new Vector3(-45,5,195),62);Photo("06-final-hacia-piramide.png",new Vector3(0,4,170),new Vector3(-20,40,800),60);
  Photo("07-vista-general.png",new Vector3(160,140,-170),new Vector3(10,0,120),58);Photo("08-vista-general-oeste.png",new Vector3(-220,120,40),new Vector3(10,0,150),58);
  Photo("10-lejos-mar-solace.png",new Vector3(-20,22,-10),new Vector3(-450,0,-380),62);Photo("11-lejos-rio-saara.png",new Vector3(40,22,30),new Vector3(220,0,70),62);Photo("12-lejos-sur-rio.png",new Vector3(60,20,-30),new Vector3(160,0,-300),62);
  File.Copy("Assets/ROM/Tutorial/Crilium/Resources/Crilium/CriliumAtlas.png",Path.Combine(Output,"09-atlas-mapa.png"),true);Debug.Log("CRELIUM MAP REGION CAPTURES SAVED");}
 static void Photo(string file,Vector3 position,Vector3 target,float fov){var go=new GameObject("Temporary Crelium photo camera");var cam=go.AddComponent<Camera>();cam.transform.position=position;cam.transform.LookAt(target);cam.fieldOfView=fov;cam.clearFlags=CameraClearFlags.Skybox;cam.nearClipPlane=.1f;cam.farClipPlane=2500;var rt=new RenderTexture(1600,900,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,file),texture.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
 public static void ApplyAndCapture(){Apply();Capture();}
}
}
