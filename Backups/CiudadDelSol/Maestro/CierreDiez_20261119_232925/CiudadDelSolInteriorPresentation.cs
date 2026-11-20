using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace ROM.EditorTools {
 public static partial class CiudadDelSolInteriorStageTwo {
    const string LookWork="Backups/CiudadDelSol/Maestro/Presentacion_20261119_182052";
    const string LookKey="ROM.Dependencies.VisualReview";
    static int lookIndex,lookFrame;
    static bool positioned;
    static Texture2D sheet;
    const string ArtFolder="Assets/ROM/Art/CiudadDelSol/Interior/Presentacion";
    static Material PresentationMaterial(Material source,string label,Vector2 tiles,Color tint,bool bounce=false){
      string path=ArtFolder+"/"+label+".mat";
      var m=AssetDatabase.LoadAssetAtPath<Material>(path);
      if(m)return m;
      m=new Material(source);
      if(m.HasProperty("_BaseMap"))m.SetTextureScale("_BaseMap",tiles);
      if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",m.GetColor("_BaseColor")*tint);
      if(bounce&&m.HasProperty("_EmissionMap")){
        m.SetTexture("_EmissionMap",m.GetTexture("_BaseMap"));m.SetColor("_EmissionColor",new Color(.046f,.031f,.072f));m.EnableKeyword("_EMISSION");
      }
      AssetDatabase.CreateAsset(m,path);return m;
    }
    public static void PolishPresentation(){
      var scene=EditorSceneManager.OpenScene(City);
      if(Hash(City)!=Hash(LookWork+"/Dungeon_Isla_del_Sol.unity"))throw new IOException("La escena difiere del respaldo.");
      if(!AssetDatabase.IsValidFolder(ArtFolder))AssetDatabase.CreateFolder("Assets/ROM/Art/CiudadDelSol/Interior","Presentacion");
      var rooms=scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Dependencia ")).OrderBy(g=>g.name).ToArray();
      if(rooms.Length!=10)throw new InvalidOperationException("Inventario de recintos");
      Color[] hues={new Color(.67f,.38f,.88f),new Color(.49f,.43f,.92f),new Color(.33f,.56f,.9f),new Color(.58f,.43f,.82f),new Color(.60f,.23f,.71f),new Color(.70f,.38f,.65f),new Color(.31f,.44f,.92f),new Color(.55f,.25f,.91f),new Color(.68f,.47f,.76f),new Color(.42f,.48f,.86f)};
      for(int i=0;i<rooms.Length;i++){
        var t=rooms[i].transform;
        var light=t.Find("Luz del recinto").GetComponent<Light>();
        light.color=hues[i];light.intensity=3.3f;light.range=11;
        light.transform.localPosition=i==0?V(-4,2.6f,-2):V(i%2==0?-2.1f:2.1f,2.65f,-3.2f);
        foreach(Transform part in t){
          var r=part.GetComponent<MeshRenderer>();if(!r)continue;
          if(part.name=="Pilastra de remate"||part.name=="Cornisa solar"){
            bool cornice=part.name=="Cornisa solar";
            r.sharedMaterial=PresentationMaterial(r.sharedMaterial,cornice?"Friso":"Jamba",cornice?new Vector2(6,.4f):new Vector2(.55f,2.2f),new Color(.48f,.42f,.51f));
          }else if(part.name=="Muro de fondo"||part.name=="Ala lateral"){
            bool rear=part.name=="Muro de fondo";r.sharedMaterial=PresentationMaterial(r.sharedMaterial,rear?"Silleria":"Retorno",rear?new Vector2(5,1.4f):new Vector2(2,1.3f),Color.white);
          }
        }
      }
      var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
      foreach(var cliff in all.Where(t=>t.name.StartsWith("Acantilado estratificado "))){
        foreach(var r in cliff.GetComponentsInChildren<MeshRenderer>()){
          var mats=r.sharedMaterials;
          for(int i=0;i<mats.Length;i++)if(AssetDatabase.GetAssetPath(mats[i]).StartsWith("Assets/ROM/CiudadDelSolMasaFlotante/"))
            mats[i]=PresentationMaterial(mats[i],"Talud_"+mats[i].name,mats[i].GetTextureScale("_BaseMap"),Color.white,true);
          r.sharedMaterials=mats;
        }
      }
      int fragments=0;
      var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Standard Assets/Eternal Temple/Prefabs/Damaged/Column_Broken_01.prefab");
      var model=source.GetComponentsInChildren<MeshFilter>().First(f=>f.sharedMesh&&f.GetComponent<MeshRenderer>());
      foreach(var group in all.Where(t=>t.name.StartsWith("Fragmento ")&&t.childCount==1&&t.Find("Estratos de roca")).OrderBy(t=>t.name)){
        var rock=group.GetComponentInChildren<MeshFilter>();if(!rock||!rock.sharedMesh)continue;
        var b=rock.GetComponent<MeshRenderer>().bounds;if(Mathf.Min(b.size.x,b.size.z)<4)continue;
        // Raycast only the island render mesh. The temporary query collider is removed before serialization.
        var probe=rock.gameObject.AddComponent<MeshCollider>();
        Vector3 seat;
        try{probe.sharedMesh=rock.sharedMesh;Physics.SyncTransforms();
          if(!probe.Raycast(new Ray(b.center+Vector3.up*(b.extents.y+2),Vector3.down),out var hit,b.size.y+4)||hit.normal.y<.65f)continue;
          seat=hit.point;
        }finally{Object.DestroyImmediate(probe);}
        if(UnityEngine.AI.NavMesh.SamplePosition(seat,out _,8,UnityEngine.AI.NavMesh.AllAreas))continue;
        var ruin=new GameObject("Fuste solar fracturado");ruin.transform.SetParent(group,false);
        ruin.transform.rotation=Quaternion.Euler(8,fragments*103,fragments%2==0?12:-16);
        ruin.AddComponent<MeshFilter>().sharedMesh=model.sharedMesh;
        var renderer=ruin.AddComponent<MeshRenderer>();renderer.sharedMaterials=model.GetComponent<MeshRenderer>().sharedMaterials;
        ruin.transform.localScale*=Mathf.Min(3.5f,b.size.x*.48f)/renderer.bounds.size.y;
        var rb=renderer.bounds;ruin.transform.position+=seat-new Vector3(rb.center.x,rb.min.y+.35f,rb.center.z);
        if(++fragments==3)break;
      }
      AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
      if(!EditorSceneManager.SaveScene(scene))throw new IOException(City);
      scene=EditorSceneManager.OpenScene(City);ReceiveDependencies(scene);
      File.WriteAllText(LookWork+"/Guardado.txt","Dependencias="+rooms.Length+" fragmentos="+fragments);
      ReviewPresentation();
    }
    public static void ReviewPresentation(){
      EditorSceneManager.OpenScene(City);
      if(!SessionState.GetBool(MultiFPS.Preloader.Preloader.CityDevelopmentEnabledKey,false)) EditorApplication.ExecuteMenuItem("ROM/Preparar Ciudad del Sol para pruebas");
      if(!SessionState.GetBool(MultiFPS.Preloader.Preloader.CityDevelopmentEnabledKey,false))throw new InvalidOperationException("Warrior no disponible.");
      SessionState.SetBool(LookKey,true);
      SessionState.SetFloat(LookKey+".time",(float)EditorApplication.timeSinceStartup);
      EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void InstallLookReview(){EditorApplication.update-=TickLook;EditorApplication.update+=TickLook;}
    static void TickLook(){
      if(!SessionState.GetBool(LookKey,false))return;
      try{
        if(EditorApplication.timeSinceStartup-SessionState.GetFloat(LookKey+".time",0)>360)throw new TimeoutException("No se completó la revisión con el Host.");
        if(!EditorApplication.isPlaying||Time.frameCount<15)return;
        var actor=MultiFPS.UI.ClientFrontend.OwnedCharacterInstance;
        var ctrl=MultiFPS.Gameplay.RiftMMOController.Instance;
        var cam=MultiFPS.GameplayCamera._instance;
        if(!actor||!ctrl||!cam||ctrl.Actor!=actor||!Mirror.NetworkServer.active)return;
        var camera=cam.GetComponent<Camera>();
        if(!positioned){
          var scene=SceneManager.GetSceneByPath(City);var cfg=ConstructionConfig4(scene);
          Vector3 point=actor.transform.position;float yaw=0;
          if(lookIndex>=1 && lookIndex<=10){
            var root=scene.GetRootGameObjects().Single(g=>g.name.StartsWith("Dependencia "+lookIndex.ToString("00")+" -")).transform;
            point=root.TransformPoint(V(0,0,3));yaw=root.eulerAngles.y+180;
            if(lookIndex==1){point=V(-140,-16,63);yaw=270;}
          }else if(lookIndex==11){point=V(105,-34,63);yaw=70;}
          else if(lookIndex==12){point=V(30,-2,-170);yaw=245;}
          if(lookIndex>0){
            var st=new[]{Vector3.zero,Vector3.forward,Vector3.right,Vector3.left,Vector3.back}.Select(offset=>Probe(point+offset,cfg,false,.45f)).FirstOrDefault(s=>s.Clear&&s.nav);
            if(st==null)throw new InvalidOperationException("Sin apoyo en vista "+lookIndex+" "+point);
            actor.ServerTeleport(st.floor+Vector3.up*.03f,yaw);
          }
          ctrl.CameraYaw=yaw;ctrl.CameraPitch=12;ctrl.CameraDistance=4.5f;
          positioned=true;lookFrame=Time.frameCount;return;
        }
        if(Time.frameCount-lookFrame<8)return;
        const int w=480,h=270;
        if(!sheet)sheet=new Texture2D(w*3,h*5,TextureFormat.RGB24,false);
        var rt=RenderTexture.GetTemporary(w,h,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var previous=RenderTexture.active;
        try{
          var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};
          RenderPipeline.SubmitRenderRequest(camera,request);RenderTexture.active=rt;
          sheet.ReadPixels(new Rect(0,0,w,h),(lookIndex%3)*w,(4-lookIndex/3)*h,false);
        }finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
        File.AppendAllText(LookWork+"/Vistas.txt",$"{lookIndex}|{Time.frameCount}|{actor.transform.position}|{camera.name}\n");
        positioned=false;
        if(++lookIndex<13)return;
        sheet.Apply();File.WriteAllBytes("Library/CiudadDelSolRevision.jpg",sheet.EncodeToJPG(94));
        SessionState.SetBool(LookKey,false);EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
      }catch(Exception e){File.WriteAllText(LookWork+"/ErrorVista.txt",e.ToString());SessionState.SetBool(LookKey,false);EditorApplication.Exit(1);}
    }
 }
}
