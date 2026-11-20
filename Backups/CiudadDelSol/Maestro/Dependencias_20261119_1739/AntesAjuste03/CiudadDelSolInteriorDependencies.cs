using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
 public static partial class CiudadDelSolInteriorStageTwo
 {
    const string DependencyWork="Backups/CiudadDelSol/Maestro/Dependencias_20261119_1739";
    public static void ResumeDependencyBatch(){
      var scene=EditorSceneManager.OpenScene(City);
      BuildDependencies(scene);
      ReceiveDependencies(SceneManager.GetSceneByPath(City));
    }
    const string TempleProps="Assets/Standard Assets/Eternal Temple/Prefabs/Props/";
    const string SunProps="Assets/Standard Assets/Sun_Temple/Prefabs/Props/";
    static readonly string[][] Furnishings={
      new[]{"Furniture/Bench_01","@Prop_Bed_B","Chest_01"},
      new[]{"@Prop_BookCase_C","Furniture/Table_03","Scrolls_04"},
      new[]{"Furniture/Table_05","Furniture/Cupboard_02","Jugs_04"},
      new[]{"Furniture/Stone_Altar_03","Furniture/Table_02","Scrolls_01"},
      new[]{"#Sepulcro","Furniture/Stone_Altar_02","Candels_04"},
      new[]{"#Sepulcro","Furniture/Stone_Altar_04","Jugs_02"},
      new[]{"Furniture/Stone_Altar_01","Furniture/Cupboard_06","Mana_Potion_01"},
      new[]{"Furniture/Stone_Altar_04","Chest_01","Candels_03"},
      new[]{"@Prop_Bed_A","Furniture/Cupboard_Broken_01","Baskets_01"},
      new[]{"@Prop_BookCase_E","Furniture/Table_04","Scrolls_02"}};
    static readonly string[] DepNames={"Estancia del custodio","Archivo de las tablillas","Gabinete de observación","Sala de cotejo","Cripta de los nichos","Cripta de las lámparas","Cámara de contención","Cámara del eco","Estancia de los vigías","Archivo de tránsito"};
    static readonly Vector3[] DepAnchors={V(-144,-16,69),V(105,-34,63),V(105,-34,63),V(105,-34,63),V(99,-34,-9),V(99,-34,-9),V(99,-34,-9),V(99,-34,-9),V(-129,-16,42),V(-105,-16,90)};
    static readonly List<Vector3> DepProtected=new List<Vector3>();
    static void ReadDepRoutes()
    {
       DepProtected.Clear();
       void Add(Vector3 a,Vector3 b){int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)));for(int i=0;i<=n;i++)DepProtected.Add(Vector3.Lerp(a,b,i/(float)n));}
       foreach(var plan in ChainsSeven().Concat(Plans()).Concat(StageThreeLandingPlans()).Concat(StageFourPlans()).Concat(StageFivePlans()))
         for(int j=1;j<plan.points.Length;j++)Add(plan.points[j-1],plan.points[j]);
       foreach(string line in File.ReadLines("Docs/CiudadDelSol/Interior/Plan_20261117/Conexiones.csv")){
         var matches=Regex.Matches(line,@"\((-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?)\)");
         Vector3? prev=null;
         foreach(Match m in matches){var c=System.Globalization.CultureInfo.InvariantCulture;var p=V(float.Parse(m.Groups[1].Value,c),float.Parse(m.Groups[2].Value,c),float.Parse(m.Groups[3].Value,c));if(prev.HasValue)Add(prev.Value,p);prev=p;}
       }
    }
    static bool DepWalk(Vector3 a,Vector3 b,Config cfg)
    {
      Station prev=null;int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)*2));
      for(int j=0;j<=n;j++){var s=Probe(Vector3.Lerp(a,b,j/(float)n),cfg,false,.12f);
        if(!s.Clear||!s.nav || prev!=null && (!DirectSweep(prev,s,cfg,out _)||!DirectSweep(s,prev,cfg,out _)||NavMesh.Raycast(prev.navPosition,s.navPosition,out _,NavMesh.AllAreas)||NavMesh.Raycast(s.navPosition,prev.navPosition,out _,NavMesh.AllAreas)))return false;
        prev=s;}
      return true;
    }
    static bool DepPath(Vector3 a,Vector3 b){
      var f=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
      if(!NavMesh.SamplePosition(a,out var x,.45f,f)||!NavMesh.SamplePosition(b,out var y,.45f,f))return false;
      var p=new NavMeshPath();return NavMesh.CalculatePath(x.position,y.position,f,p)&&p.status==NavMeshPathStatus.PathComplete;
    }
    static void BuildDependencies(Scene scene){
      if(scene.isDirty||Application.isPlaying||!File.Exists(DependencyWork+"/Dungeon_Isla_del_Sol.unity"))throw new InvalidOperationException("Se requiere escena limpia y respaldo.");
      var cfg=ConstructionConfig4(scene);ReadDepRoutes();Physics.SyncTransforms();
      var navHash=Hash(Nav);
      for(int i=0;i<10;i++){
        string name="Dependencia "+(i+1).ToString("00")+" - "+DepNames[i];
        if(scene.GetRootGameObjects().Any(g=>g.name==name))continue;
        bool built=false;string why="sin candidato";
        var primary=DepAnchors[i];
        var occupied=scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Dependencia ")).Select(g=>g.transform.position).ToArray();
        var alternatives=new[]{primary,V(99,-34,-9),V(105,-34,63),V(-129,-16,42),V(-105,-16,90),V(-117,-16,-117),V(171,-16,-39),V(-69,-16,-99),V(189,-16,162),V(21,-16,153)};
        foreach(var anchor in (i==0?new[]{primary}:alternatives.Distinct().ToArray())){
          var sites=new List<Vector3>();
          if(i==0)sites.Add(anchor);
          else for(int u=-30;u<=30;u+=3)for(int v=-33;v<=33;v+=3)if(Mathf.Max(Mathf.Abs(u),Mathf.Abs(v))>=9)sites.Add(anchor+V(u,0,v));
          foreach(var site in sites.OrderBy(p=>Mathf.Abs(Vector3.Distance(anchor,p)-18))){
            if(occupied.Any(p=>Mathf.Abs(p.y-site.y)<6 && Mathf.Abs(p.x-site.x)<17 && Mathf.Abs(p.z-site.z)<17))continue;
            if(site.x<70&&site.x>-38&&site.z<74&&site.z>-38)continue;
            foreach(int yaw in i==0?new[]{90}:new[]{0,90,180,270}){
              if(TryDependency(scene,cfg,i,site,yaw,name,out why)){built=true;break;}
            }
            if(built)break;
          }
          if(built)break;
        }
        if(!built){File.AppendAllText(DependencyWork+"/Estado.txt",name+" pendiente: "+why+"\n");continue;}
        if(Hash(Nav)!=navHash)throw new InvalidOperationException("Cambio de navegación original.");
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Guardado fallido.");
        scene=EditorSceneManager.OpenScene(City);
        if(!scene.GetRootGameObjects().Any(g=>g.name==name))throw new IOException("Falta dependencia tras la recarga.");
        cfg=ConstructionConfig4(scene);
      }
      scene=EditorSceneManager.OpenScene(City);
      File.WriteAllLines(DependencyWork+"/Recarga.txt",scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Dependencia ")).Select(g=>g.name+" | "+g.transform.position+" | renderers="+g.GetComponentsInChildren<MeshRenderer>().Length+" | luces="+g.GetComponentsInChildren<Light>().Length));
    }
    static void ReceiveDependencies(Scene scene)
    {
      if(scene.isDirty||Application.isPlaying)throw new InvalidOperationException("Recepción en Edit Mode y escena guardada.");
      var cfg=ConstructionConfig4(scene);
      var lines=new List<string>();
      foreach(var room in scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Dependencia ")).OrderBy(g=>g.name)){
        var t=room.transform;bool first=room.name.StartsWith("Dependencia 01");
        bool walk=true;
        if(first){
          walk=DepWalk(V(-134,-16,63),V(-146,-16,63),cfg)&&DepWalk(V(-134,-16,75),V(-146,-16,75),cfg)&&DepWalk(V(-140,-16,63),V(-140,-16,75),cfg);
        }else{
          foreach(float x in new[]{-2.4f,-.8f,.8f,2.4f})walk &= DepWalk(t.TransformPoint(V(x,0,8.5f)),t.TransformPoint(V(x,0,-4.5f)),cfg);
          walk &= DepWalk(t.TransformPoint(V(-5,0,1)),t.TransformPoint(V(5,0,1)),cfg);
        }
        var entry=first?V(-134,-16,63):t.TransformPoint(V(0,0,9.5f));
        bool global=DepPath(V(156,-16,99),entry)&&DepPath(entry,V(156,-16,99));
        bool fittings=true;
        foreach(Transform prop in t){
          if(!PrefabUtility.IsAnyPrefabInstanceRoot(prop.gameObject))continue;
          var meshes=prop.GetComponentsInChildren<MeshRenderer>();var b=meshes[0].bounds;foreach(var m in meshes)b.Encapsulate(m.bounds);
          var local=t.InverseTransformPoint(b.center);
          fittings &= Mathf.Abs(local.x)<9.1f && Mathf.Abs(local.z)<6.6f && Mathf.Abs(b.min.y-t.position.y)<.15f;
        }
        bool materials=room.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m&&m.shader&&m.shader.isSupported));
        var light=room.GetComponentInChildren<Light>();var script=t.Find("Inscripción violeta");
        lines.Add($"{room.name}|rutas={walk}|A03={global}|muebles={fittings}|materiales={materials}|luz={light&&light.enabled&&light.intensity>0}|runa={script&&script.GetComponent<MeshFilter>().sharedMesh}");
      }
      File.WriteAllLines(DependencyWork+"/Recepcion.txt",lines);
      var report=InspectSelected(ChainsSeven(),DependencyWork+"/Regresion","Recorridos principales después de las dependencias",false,true);
      File.WriteAllLines(DependencyWork+"/Regresion.txt",report.connections.Select(c=>c.plan.id+"="+c.technicalIndividual+" ida="+c.forward.complete+" vuelta="+c.reverse.complete));
    }
    static bool TryDependency(Scene scene,Config cfg,int id,Vector3 site,int yaw,string name,out string why){
      why="espacio insuficiente";var rot=Quaternion.Euler(0,yaw,0);
      Vector3 Pnt(float x,float z)=>site+rot*V(x,0,z);
      bool r09=id==0;float half=r09?9:7,dep=r09?6:6.5f;
      Vector3 access=Pnt(0,dep+3),core=Pnt(0,1);
      if(!r09){
        // Exclude footprints along documented routes before probing or instantiating anything.
        foreach(var p in DepProtected){
          if(Mathf.Abs(p.y-site.y)>2 || Mathf.Abs(p.x-site.x)>12 || Mathf.Abs(p.z-site.z)>12)continue;
          var q=Quaternion.Inverse(rot)*(p-site);
          if(q.z < -dep+7.8f && q.z > -dep-3 && (Mathf.Abs(q.x)>half-3.3f && Mathf.Abs(q.x)<half+3 || Mathf.Abs(q.x)<half+3 && q.z < -dep+3.6f))return false;
        }
        if(!Physics.Raycast(site+Vector3.up*.4f,Vector3.down,.65f,cfg.mask)||Physics.CheckBox(site+Vector3.up*2, new Vector3(half-.4f,1.85f,dep-.4f),rot,cfg.mask,QueryTriggerInteraction.Ignore))return false;
        if(!Physics.Raycast(site+Vector3.up*2,Vector3.up,20,cfg.mask,QueryTriggerInteraction.Ignore))return false;
        foreach(float x in new[]{-half,0,half})foreach(float z in new[]{-dep,0,dep})if(!Probe(Pnt(x,z),cfg,false,.12f).Clear)return false;
      }
      var entrance=r09?V(-134,-16,63):access;
      if(!DepPath(V(156,-16,99),entrance)||!DepPath(entrance,V(156,-16,99)))return false;
      bool Routes(){
        if(r09)return DepWalk(V(-134,-16,63),V(-146,-16,63),cfg)&&DepWalk(V(-134,-16,75),V(-146,-16,75),cfg)&&DepWalk(V(-140,-16,63),V(-140,-16,75),cfg);
        foreach(float lane in new[]{-2.4f,-.8f,.8f,2.4f})if(!DepWalk(Pnt(lane,dep+2),Pnt(lane,-dep+2),cfg))return false;
        return DepWalk(Pnt(-half+2,1),Pnt(half-2,1),cfg);
      }
      var stone=AssetDatabase.LoadAssetAtPath<Material>(R21Assets+"/R21_Piedra_sombría.mat");
      var trim=AssetDatabase.LoadAssetAtPath<Material>(R21Assets+"/R21_Grabado_solar.mat");
      if(!stone||!trim)throw new IOException("Material arquitectónico ausente.");
      var root=new GameObject(name);root.transform.SetPositionAndRotation(site,rot);
      SceneManager.MoveGameObjectToScene(root,scene);
      bool success=false;
      try{
        void ClearBox(Vector3 local,Vector3 size){
          var pos=root.transform.TransformPoint(local);
          if(Physics.CheckBox(pos,Vector3.Max(size*.5f-Vector3.one*.025f,Vector3.one*.01f),rot,cfg.mask,QueryTriggerInteraction.Ignore))throw new InvalidOperationException("interferencia física");
          if(DepProtected.Any(p=>Mathf.Abs(p.y-site.y)<2 && new Bounds(local,size+V(5,0,5)).SqrDistance(new Vector3(root.transform.InverseTransformPoint(p).x,local.y,root.transform.InverseTransformPoint(p).z))<.001f))throw new InvalidOperationException("corredor reservado");
        }
        GameObject Block(string label,Vector3 local,Vector3 size,Material mat,bool carve=true){
          ClearBox(local,size);var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=label;g.transform.SetParent(root.transform,false);g.transform.localPosition=local;g.transform.localScale=size;g.GetComponent<MeshRenderer>().sharedMaterial=mat;
          if(carve){var o=g.AddComponent<NavMeshObstacle>();o.shape=NavMeshObstacleShape.Box;o.center=Vector3.zero;o.size=Vector3.one;o.carving=true;}
          return g;
        }
        if(!r09){
          Block("Muro de fondo",V(0,1.8f,-dep),V(half*2,3.6f,.38f),stone);
          Block("Cornisa solar",V(0,3.72f,-dep),V(half*2,.24f,.54f),trim,false);
          foreach(float side in new[]{-1f,1f}){
            Block("Ala lateral",V(side*(half-.2f),1.48f,-dep+2.5f),V(.40f,2.96f,4.58f),stone);
            Block("Pilastra de remate",V(side*(half-.2f),1.62f,-dep+5.14f),V(.68f,3.24f,.70f),trim);
          }
        }
        void Furnish(string asset,float x,float z,float maxW,float maxH,float maxD){
          if(asset.StartsWith("#")){
            Block("Cista de piedra",V(x,.48f,z),V(maxW*.88f,.96f,maxD*.85f),stone);
            Block("Losa funeraria",V(x,1.05f,z),V(maxW,.18f,maxD),trim,false);return;
          }
          string path=asset.StartsWith("@")?SunProps+asset.Substring(1):TempleProps+asset;
          var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path+".prefab");if(!prefab)throw new IOException(path);
          var inst=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);inst.transform.SetParent(root.transform,false);
          var meshes=inst.GetComponentsInChildren<MeshRenderer>();if(meshes.Length==0)throw new InvalidOperationException(asset);
          Bounds B(){var b=meshes[0].bounds;foreach(var m in meshes)b.Encapsulate(m.bounds);return b;}
          var b=B();bool swap=yaw%180!=0;float w=swap?b.size.z:b.size.x,d=swap?b.size.x:b.size.z;
          inst.transform.localScale*=Mathf.Min(1f,maxW/Mathf.Max(.01f,w),maxH/Mathf.Max(.01f,b.size.y),maxD/Mathf.Max(.01f,d));
          b=B();var l=V(x,b.size.y*.5f,z);inst.SetActive(false);Physics.SyncTransforms();ClearBox(l,V(swap?b.size.z:b.size.x,b.size.y,swap?b.size.x:b.size.z));
          if(!Physics.Raycast(Pnt(x,z)+Vector3.up*.35f,Vector3.down,out var hit,.55f,cfg.mask,QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y-site.y)>.12f)throw new InvalidOperationException("sin apoyo");
          inst.transform.position+=hit.point-new Vector3(b.center.x,b.min.y,b.center.z);inst.SetActive(true);
          var box=new GameObject("Volumen de mobiliario");box.transform.SetParent(root.transform,false);box.transform.localPosition=l;
          var c=box.AddComponent<BoxCollider>();c.size=V(swap?b.size.z:b.size.x,b.size.y,swap?b.size.x:b.size.z);
          var o=box.AddComponent<NavMeshObstacle>();o.shape=NavMeshObstacleShape.Box;o.size=c.size;o.carving=true;
        }
        float z0=r09?-4.1f:-dep+1.35f;
        Furnish(Furnishings[id][0],r09?-6:-4.7f,z0,3,2.9f,1.6f);
        Furnish(Furnishings[id][1],r09?6:4.7f,z0,3.4f,2.8f,1.8f);
        Furnish(Furnishings[id][2],r09?4:5.7f,z0+1.8f,1.15f,1.4f,1.15f);
        Vector3 mark=Pnt(r09?-6:0,r09?-5.58f:-dep+.20f)+Vector3.up*2.05f;
        if(r09){if(!Physics.Raycast(mark+root.transform.forward*1.5f,-root.transform.forward,out var h,3,cfg.mask,QueryTriggerInteraction.Ignore))throw new InvalidOperationException("muro del relicario");mark=h.point+root.transform.forward*.015f;}
        var writing=new GameObject("Inscripción violeta");writing.transform.SetParent(root.transform,false);writing.transform.position=mark+root.transform.forward*.018f;
        var mf=writing.AddComponent<MeshFilter>();mf.sharedMesh=RuneWriting(1700+id,false,2+id%2,out _);writing.AddComponent<MeshRenderer>().sharedMaterial=RuneMaterial(0,5);
        var glow=new GameObject("Luz del recinto");glow.transform.SetParent(root.transform,false);glow.transform.localPosition=V(0,3, -2);
        var light=glow.AddComponent<Light>();light.type=LightType.Point;light.shadows=LightShadows.None;light.color=new Color(.48f,.14f,.86f);light.range=10;light.intensity=2.1f;
        Physics.SyncTransforms();if(!Routes())throw new InvalidOperationException("acceso posterior obstruido");
        foreach(var rr in root.GetComponentsInChildren<MeshRenderer>())if(rr.sharedMaterials.Any(m=>!m||!m.shader||!m.shader.isSupported))throw new InvalidOperationException("material incompatible");
        string meshpath=AssetDatabase.GenerateUniqueAssetPath(RuneAssets+"/Dependencia_"+id+".asset");
        AssetDatabase.CreateAsset(mf.sharedMesh,meshpath);
        EditorSceneManager.MarkSceneDirty(scene);success=true;
        File.AppendAllText(DependencyWork+"/Estado.txt",$"{name} | centro={site} | yaw={yaw} | {half*2} x {dep*2} | entrada={entrance} | física/rutas/materiales OK\n");
        return true;
      }catch(InvalidOperationException ex){why=ex.Message;return false;}
      finally{if(!success){Object.DestroyImmediate(root);Physics.SyncTransforms();}}
    }
 }
}
