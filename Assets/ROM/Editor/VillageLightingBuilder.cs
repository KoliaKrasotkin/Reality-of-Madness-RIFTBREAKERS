using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ROM.EditorTools {
public static class VillageLightingBuilder {
public static void Probe(){try{
var scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var log=new List<string>();
foreach(var root in scene.GetRootGameObjects())log.Add("ROOT "+root.name+" pos="+root.transform.position);
foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.bounds.center.magnitude<60&&r.bounds.size.magnitude>2).Take(100))log.Add("NEAR "+r.transform.parent?.name+"/"+r.name+" "+r.bounds);
foreach(var file in new[]{"Bld_FishingShack_A","Bld_Square_A","Bld_Medium_A","Bld_Storage_A"}){
var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Standard Assets/Sun_Temple/Prefabs/Buildings_Prefabbed/"+file+".prefab");if(!p){log.Add("MISSING "+file);continue;}var go=UnityEngine.Object.Instantiate(p);go.transform.position=Vector3.zero;var rr=go.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh&&!r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("HLP")).ToArray();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);log.Add("PREFAB "+file+" "+b);UnityEngine.Object.DestroyImmediate(go);
}
Directory.CreateDirectory("Documentation/village-night");File.WriteAllLines("Documentation/village-night/probe.txt",log);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}

const string AssetsRoot="Assets/ROM/Rendering/VillageNight";
static Transform village;
static readonly List<Vector3> lamps=new List<Vector3>();
public static void Build(){try{
Directory.CreateDirectory(AssetsRoot);Directory.CreateDirectory("Documentation/village-night");AssetDatabase.Refresh();
const string playerPrefab="Assets/MultiFPS/Levels/Prefabs/Gameplay/MultiFPS_PlayerInstance.prefab";var playerRoot=PrefabUtility.LoadPrefabContents(playerPrefab);try{if(!playerRoot.GetComponent<ROM.Horde.RiftVillagePlayer>())playerRoot.AddComponent<ROM.Horde.RiftVillagePlayer>();PrefabUtility.SaveAsPrefabAsset(playerRoot,playerPrefab);}finally{PrefabUtility.UnloadPrefabContents(playerRoot);}
var scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var existing=GameObject.Find("Refugio - Aldea");if(existing)UnityEngine.Object.DestroyImmediate(existing);var weatherOld=GameObject.Find("El Rift - Noche y bruma");if(weatherOld)UnityEngine.Object.DestroyImmediate(weatherOld);
village=new GameObject("Refugio - Aldea").transform;lamps.Clear();
var level=GameObject.Find("Level");
foreach(var t in level.GetComponentsInChildren<Transform>(true)){
 if(t.name=="Cover"||t.name.StartsWith("FloorTrim_")||t.name.StartsWith("Rim_")||t.name=="ArsenalDaisTrim"||t.name.StartsWith("BridgeTrim_")||t.name.StartsWith("Frame")||t.name=="Spinner"||t.name=="PadTrim"||t.name=="PortalLight")t.gameObject.SetActive(false);
}
var centerLight=GameObject.Find("Center Light");if(centerLight)centerLight.SetActive(false);
foreach(var t in GameObject.Find("Bosque Fracturado - Etapa 1").GetComponentsInChildren<Transform>(true))if((t.name=="Bosque - arbol"||t.name=="Sotobosque"||t.name=="Cristal dimensional"||t.name=="Ruinas olvidadas")&&new Vector2(t.position.x,t.position.z).magnitude<74)t.gameObject.SetActive(false);
Physics.SyncTransforms();
var paving=new Material(Shader.Find("ROM/World Terrain"));paving.name="Aldea - pavimento de piedra";var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/Materials/Pavement_A.mat");paving.SetTexture("_BaseMap",source.GetTexture("_BaseMap"));paving.SetTexture("_RockMap",source.GetTexture("_BaseMap"));paving.SetColor("_BaseColor",new Color(.66f,.65f,.61f));paving.SetFloat("_WorldScale",.22f);var oldPaving=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+"/VillagePaving.mat");if(oldPaving){EditorUtility.CopySerialized(paving,oldPaving);UnityEngine.Object.DestroyImmediate(paving);paving=oldPaving;}else AssetDatabase.CreateAsset(paving,AssetsRoot+"/VillagePaving.mat");
foreach(var r in level.GetComponentsInChildren<MeshRenderer>())if(r.name=="Floor"||r.name=="ArsenalDais"||r.name.StartsWith("Bridge_")||r.name=="Pad")r.sharedMaterial=paving;
// Houses surround a broad, clear north-south street and the training court.
var positions=Enumerable.Range(0,8).Select(i=>{float angle=(22.5f+i*45)*Mathf.Deg2Rad;return new Vector3(Mathf.Sin(angle)*43,0,Mathf.Cos(angle)*43);}).ToArray();
for(int i=0;i<positions.Length;i++){
 var p=positions[i];p.y=Ground(p);string file=i==2||i==5?"Bld_Square_A":i%2==0?"Bld_FishingShack_A":"Bld_FishingShack_B";
 var house=Place("Buildings_Prefabbed/"+file,p,i==2||i==5?10:7.5f,Quaternion.LookRotation(-p).eulerAngles.y,"Vivienda "+(i+1));
 var frontage=p-p.normalized*9;frontage.y=Ground(frontage);Lantern(frontage);
 Place("Props/Prop_BarrelCrates_A",p+p.normalized*9+Vector3.right*3,1.3f,i*71,"Suministros de vivienda");
}
foreach(var p in new[]{new Vector3(-16,0,-26),new Vector3(16,0,-26)}){Place("Props/Prop_Table_C",p,1,0,"Puesto del mercado");Place("Props/Prop_Crate_A",p+Vector3.right*3,1,15,"Mercader - provisiones");}
foreach(var p in new[]{new Vector3(-14,0,3),new Vector3(16,0,27)})Place("Props/Prop_Roughbench_A",p,1,90,"Banco de la plaza");
Place("Props/Prop_WoodPile_A",new Vector3(-40,0,-24),1.8f,90,"Reserva de leña");
Place("Props/Prop_Firewood_A",new Vector3(-13,0,-10),.6f,0,"Hogar de la plaza");WarmLight(new Vector3(-13,1.2f,-10),16,16,"Luz del hogar");
foreach(var p in new[]{new Vector3(-10,0,-5),new Vector3(12,0,-14),new Vector3(-10,0,25),new Vector3(10,0,50),new Vector3(-10,0,-65)})Lantern(p);
BuildNpcs();
var weather=new GameObject("El Rift - Noche y bruma");var atmosphere=weather.AddComponent<ROM.Horde.RiftNightAtmosphere>();atmosphere.Moon=RenderSettings.sun;atmosphere.Moon.transform.rotation=Quaternion.Euler(32,-32,0);atmosphere.Apply();EditorUtility.SetDirty(RenderSettings.skybox);
var profile=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/ROM/Rendering/RiftCinematic.asset");if(profile&&profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var grade)){grade.contrast.Override(6);grade.postExposure.Override(atmosphere.BaseExposure);EditorUtility.SetDirty(profile);}
var shader=Shader.Find("ROM/Drifting Mist");if(!shader||!shader.isSupported)throw new Exception("Mist shader unsupported");var mist=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+"/RiftMist.mat");if(!mist){mist=new Material(shader);AssetDatabase.CreateAsset(mist,AssetsRoot+"/RiftMist.mat");}mist.SetColor("_Color",new Color(.13f,.16f,.22f,.22f));mist.SetFloat("_Softness",5);EditorUtility.SetDirty(mist);
var centers=new[]{Vector3.zero,new Vector3(-310,8,0),new Vector3(310,8,0),new Vector3(0,8,450),new Vector3(100,8,-470),new Vector3(820,8,370),new Vector3(-620,8,430)};float[] radii={105,145,145,170,175,265,215};
for(int i=0;i<centers.Length;i++)Mist(weather.transform,centers[i],radii[i],mist,i);
// Checkpoints provide cool navigation beacons; house lights stay warm.
foreach(var checkpoint in UnityEngine.Object.FindObjectsByType<ROM.Horde.RiftCheckpoint>(FindObjectsSortMode.None))foreach(var light in checkpoint.GetComponentsInChildren<Light>()){light.intensity=6;light.range=20;light.shadows=LightShadows.None;EditorUtility.SetDirty(light);}
Physics.SyncTransforms();var surface=UnityEngine.Object.FindFirstObjectByType<Unity.AI.Navigation.NavMeshSurface>();var saved=surface.navMeshData;surface.BuildNavMesh();if(saved&&AssetDatabase.Contains(saved)){EditorUtility.CopySerialized(surface.navMeshData,saved);surface.RemoveData();surface.navMeshData=saved;surface.AddData();EditorUtility.SetDirty(saved);}EditorUtility.SetDirty(surface);
Validate(log:new List<string>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
foreach(var ps in weather.GetComponentsInChildren<ParticleSystem>())ps.Simulate(70,true,true);
MapVisualRepair.Capture("../village-night/plaza",new Vector3(0,2.8f,-23),new Vector3(-3,4,18));MapVisualRepair.Capture("../village-night/village-overview",new Vector3(-85,55,-95),new Vector3(0,3,0));MapVisualRepair.Capture("../village-night/distance-fog",new Vector3(0,4,80),new Vector3(0,12,400));
Debug.Log("[VillageNight] PASS: village, lighting, smoke and routes saved.");EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
static float Ground(Vector3 p){var hits=Physics.RaycastAll(p+Vector3.up*100,Vector3.down,180,~0,QueryTriggerInteraction.Ignore);foreach(var hit in hits.OrderByDescending(h=>h.point.y)){var f=hit.collider.GetComponent<MeshFilter>();string asset=f&&f.sharedMesh?AssetDatabase.GetAssetPath(f.sharedMesh):"";if(hit.collider.name=="Floor"||asset.Contains("ForestIsland")||asset.Contains("Satellite_")||asset.Contains("ExtraIsland"))return hit.point.y;}return 0;}
static GameObject Place(string asset,Vector3 point,float height,float yaw,string label){
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Standard Assets/Sun_Temple/Prefabs/"+asset+".prefab");if(!prefab)throw new Exception("Missing village prefab "+asset);var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,village);go.name=label;go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.Euler(0,yaw,0);
foreach(var f in go.GetComponentsInChildren<MeshFilter>(true)){var r=f.GetComponent<MeshRenderer>();if(!r)continue;if(f.sharedMesh&&f.sharedMesh.name.StartsWith("HLP")||f.name.Contains("CollisionBox")||f.name.StartsWith("Dec_Ornament")){r.enabled=false;continue;}if(!f.sharedMesh){string name=System.Text.RegularExpressions.Regex.Replace(f.name,@" \(\d+\)$","");if(name=="Prop_Book_Open_A")name="Prop_Book_A";if(name=="Bld_Common_Windowframe_Closed")name+="_A";var path="Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/"+name+".fbx";f.sharedMesh=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();if(!f.sharedMesh)r.enabled=false;}}
var rr=go.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();if(rr.Length==0)throw new Exception("No renderers "+asset);var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);float scale=height/Mathf.Max(.1f,b.size.y);go.transform.localScale*=scale;point.y=Ground(point);go.transform.position=point-new Vector3(b.center.x,b.min.y,b.center.z)*scale;
foreach(var c in go.GetComponentsInChildren<Collider>())c.enabled=false;
var lodRenderers=new HashSet<Renderer>(go.GetComponentsInChildren<LODGroup>().SelectMany(g=>g.GetLODs().Skip(1)).SelectMany(l=>l.renderers));
foreach(var r in rr)if(!lodRenderers.Contains(r)&&r.GetComponent<MeshFilter>() is MeshFilter f&&f.sharedMesh){var c=r.GetComponent<MeshCollider>();if(!c)c=r.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;c.enabled=true;}
foreach(var t in go.GetComponentsInChildren<Transform>()){t.gameObject.layer=0;t.gameObject.isStatic=true;}foreach(var mb in go.GetComponentsInChildren<MonoBehaviour>())if(mb)UnityEngine.Object.DestroyImmediate(mb);foreach(var light in go.GetComponentsInChildren<Light>())UnityEngine.Object.DestroyImmediate(light);return go;
}
static void BuildNpcs(){
var parent=new GameObject("Habitantes del refugio").transform;parent.SetParent(village);
AddNpc(parent,"alma","Alma","Boticaria",new Vector3(-14,0,-23),0,true,"Bienvenido al refugio. Mis pociones se pagan con el oro que ganás en las islas. ¿Necesitás provisiones?","La poción de vida recupera el 40% de tu salud máxima; la mayor, el 75%. Podés beber con H. No se gasta si ya tenés toda la vida. Entre dosis hay cinco segundos de recarga.");
AddNpc(parent,"bram","Bram","Intendente",new Vector3(14,0,-23),1,true,"Los caminos se vuelven peligrosos al salir de la aldea. Comprá provisiones antes de cruzar los puentes. También recompro tus pociones sobrantes.","Los enemigos y guardianes entregan oro del juego. Las pociones que lleves quedan guardadas para tu próxima visita. Explorá cada isla y activá su cristal antes de adentrarte más.");
AddNpc(parent,"iria","Iria","Exploradora",new Vector3(-17,0,12),2,false,"La bruma esconde los fragmentos del Rift. Los puentes son nuestras rutas entre islas. Puedo orientarte antes de que salgas.","Este pueblo es una zona segura. Afuera encontrarás patrullas y guardianes. Los cristales registran tu checkpoint y permiten reaparecer allí. Probá tus habilidades con el muñeco de la plaza antes de partir.");
var ui=new GameObject("Dialogos y comercio",typeof(RectTransform),typeof(ROM.UI.Sprint01.RiftVillageUI));ui.transform.SetParent(parent,false);
}
static void AddNpc(Transform parent,string id,string name,string role,Vector3 p,int model,bool trades,string greeting,string advice){
p.y=Ground(p);var npc=new GameObject(name+" - "+role).AddComponent<ROM.Horde.RiftVillageNpc>();npc.transform.SetParent(parent);npc.transform.position=p;npc.Id=id;npc.DisplayName=name;npc.Role=role;npc.Trades=trades;npc.Greeting=greeting;npc.Advice=advice;
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ROM/UI/FrontEnd/Models/ids-character-"+model+".prefab");if(!prefab)throw new Exception("Missing IDS NPC model");var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab,npc.transform);body.name="Modelo IDS";body.transform.position=Vector3.zero;body.transform.rotation=Quaternion.LookRotation(new Vector3(-p.x,0,-p.z));var rr=body.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);float scale=1.95f/Mathf.Max(.1f,b.size.y);body.transform.localScale*=scale;body.transform.position=p-new Vector3(b.center.x,b.min.y,b.center.z)*scale;
foreach(var c in body.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);foreach(var script in body.GetComponentsInChildren<MonoBehaviour>())if(script)UnityEngine.Object.DestroyImmediate(script);
foreach(var animator in body.GetComponentsInChildren<Animator>()){animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/ROM/UI/FrontEnd/Models/IDS_LobbyIdle.controller");animator.applyRootMotion=false;}
var collider=npc.gameObject.AddComponent<CapsuleCollider>();collider.height=1.9f;collider.radius=.4f;collider.center=Vector3.up*.95f;
var label=new GameObject("Nombre",typeof(TextMesh)).GetComponent<TextMesh>();label.transform.SetParent(npc.transform);label.transform.localPosition=Vector3.up*2.35f;label.text=name+" · "+role;label.characterSize=.055f;label.fontSize=48;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.88f,.77f,.52f);label.gameObject.AddComponent<ROM.Horde.RiftCheckpointLabel>();
}
static void Lantern(Vector3 p){p.y=Ground(p);var lamp=Place("Props/Prop_Lamp_B_Lit",p+Vector3.right*.5f,.55f,0,"Farol de la aldea");lamp.transform.position+=Vector3.up*2.05f;
var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/Materials/Wood_Glazed_Tiling_Rotten.mat");
foreach(bool arm in new[]{false,true}){var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name=arm?"Brazo del farol":"Poste del farol";post.transform.SetParent(village);post.transform.position=p+(arm?new Vector3(.28f,2.7f,0):Vector3.up*1.4f);post.transform.localScale=arm?new Vector3(.85f,.13f,.16f):new Vector3(.18f,2.8f,.18f);post.GetComponent<Renderer>().sharedMaterial=wood;post.isStatic=true;}
WarmLight(p+new Vector3(.5f,2.4f,0),13,17,"Luz cálida de calle");lamps.Add(p);}
static void WarmLight(Vector3 p,float strength,float radius,string name){var l=new GameObject(name,typeof(Light)).GetComponent<Light>();l.transform.SetParent(village);l.transform.position=p;l.type=LightType.Point;l.color=new Color(1,.62f,.29f);l.intensity=strength;l.range=radius;l.shadows=LightShadows.None;}
static void Mist(Transform parent,Vector3 center,float radius,Material mat,int seed){
var go=new GameObject("Bruma de borde "+seed,typeof(ParticleSystem));go.transform.SetParent(parent);go.transform.position=center+Vector3.down*3;go.transform.rotation=Quaternion.Euler(90,0,0);var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(9120+seed);var main=ps.main;main.duration=80;main.loop=true;main.prewarm=true;main.startLifetime=new ParticleSystem.MinMaxCurve(65,80);main.startSpeed=.08f;main.startSize=new ParticleSystem.MinMaxCurve(55,100);main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.maxParticles=32;main.simulationSpace=ParticleSystemSimulationSpace.World;main.cullingMode=ParticleSystemCullingMode.PauseAndCatchup;var emission=ps.emission;emission.rateOverTime=.35f;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=radius;shape.radiusThickness=.12f;var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.6f,1,1.2f));var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.2f),new GradientAlphaKey(1,.75f),new GradientAlphaKey(0,1)});color.color=gradient;var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=mat;r.renderMode=ParticleSystemRenderMode.Billboard;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
}
static void Validate(List<string> log){
var mode=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();var spawns=UnityEngine.Object.FindFirstObjectByType<MultiFPS.Gameplay.Gamemodes.SpawnpointsContainer>();
foreach(var spawn in spawns.Spawnpoints){if(Physics.CheckCapsule(spawn.position+Vector3.up*.5f,spawn.position+Vector3.up*1.7f,.3f,MultiFPS.GameManager.environmentLayer,QueryTriggerInteraction.Ignore))throw new Exception("Blocked spawn "+spawn.position);}
if(!mode.IsSafe(mode.TrainingSpot.position))throw new Exception("Training left safe zone");if(Physics.CheckCapsule(mode.TrainingSpot.position+Vector3.up*.5f,mode.TrainingSpot.position+Vector3.up*1.7f,.3f,MultiFPS.GameManager.environmentLayer,QueryTriggerInteraction.Ignore))throw new Exception("Training spot blocked");
UnityEngine.AI.NavMesh.SamplePosition(spawns.Spawnpoints[0].position,out var from,5,UnityEngine.AI.NavMesh.AllAreas);
foreach(var checkpoint in mode.Checkpoints){var path=new UnityEngine.AI.NavMeshPath();bool found=UnityEngine.AI.NavMesh.SamplePosition(checkpoint.Respawn.position,out var hit,5,UnityEngine.AI.NavMesh.AllAreas);bool connected=found&&UnityEngine.AI.NavMesh.CalculatePath(from.position,hit.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;log.Add(checkpoint.Title+" connected="+connected);if(!connected)throw new Exception("Village blocks route to "+checkpoint.Title);}
int bad=village.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).SelectMany(r=>r.sharedMaterials).Count(m=>!m||!m.shader||!m.shader.isSupported);if(bad>0)throw new Exception("Village has incompatible materials: "+bad);
log.Add("Spawnpoints and training dummy unobstructed; safe zone retained.");log.Add("8 houses, 2 market tables, street lanterns; 7 edge mist systems, maximum 224 particles total.");File.WriteAllLines("Documentation/village-night/validation.txt",log);
}
}}
