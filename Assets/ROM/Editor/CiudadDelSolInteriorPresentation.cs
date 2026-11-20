using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
    const string CloseWork="Backups/CiudadDelSol/Maestro/CierreDiez_20261119_232925";
    static bool RoomsOnly=>SessionState.GetBool(LookKey+".rooms",false);
    public static void ReviewCustodian(){
      SessionState.SetBool(LookKey+".custodian",true);
      SessionState.SetBool(LookKey+".rooms",true);
      ReviewPresentation();
    }
    public static void FinishTenRooms(){
      var scene=EditorSceneManager.OpenScene(City);
      if(Hash(City)!=Hash(CloseWork+"/Dungeon_Isla_del_Sol.unity"))throw new IOException("Estado distinto del respaldo del cierre.");
      var rooms=scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Dependencia ")).OrderBy(g=>g.name).ToArray();
      if(rooms.Length!=10)throw new InvalidOperationException("Inventario incompleto");
      var metal=new Material(Shader.Find("Universal Render Pipeline/Lit"));
      metal.SetColor("_BaseColor",new Color(.49f,.32f,.42f));metal.SetFloat("_Metallic",.48f);metal.SetFloat("_Smoothness",.44f);
      metal.EnableKeyword("_EMISSION");metal.SetColor("_EmissionColor",new Color(.26f,.12f,.24f));
      AssetDatabase.CreateAsset(metal,ArtFolder+"/Electro_envejecido.mat");
      for(int i=0;i<rooms.Length;i++){
        var room=rooms[i].transform;
        var writing=room.Find("Inscripción violeta");
        var inlay=new GameObject("Incrustación mural - "+DepNames[i]);inlay.transform.SetParent(room,false);
        inlay.transform.SetPositionAndRotation(writing.position+room.forward*.008f,room.rotation);
        var mesh=RoomInlay(i);AssetDatabase.CreateAsset(mesh,ArtFolder+"/Incrustacion_"+(i+1).ToString("00")+".asset");
        inlay.AddComponent<MeshFilter>().sharedMesh=mesh;var r=inlay.AddComponent<MeshRenderer>();r.sharedMaterial=metal;r.shadowCastingMode=ShadowCastingMode.Off;
        var lamp=room.Find("Luz del recinto");if(i==0)lamp.position=writing.position+room.forward*1.9f+Vector3.up*.48f;
      }
      AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
      if(!EditorSceneManager.SaveScene(scene))throw new IOException(City);
      scene=EditorSceneManager.OpenScene(City);
      foreach(var room in scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Dependencia "))){
        var inlay=room.transform.Find("Incrustación mural - "+room.name.Substring("Dependencia 00 - ".Length));
        if(!inlay||!inlay.GetComponent<MeshFilter>().sharedMesh||inlay.GetComponentsInChildren<Collider>().Length!=0||
           room.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterials.Any(m=>!m||!m.shader.isSupported||ShaderUtil.ShaderHasError(m.shader))))
          throw new InvalidOperationException(room.name);
      }
      SessionState.SetBool(LookKey+".rooms",true);ReviewPresentation();
    }
    static Mesh RoomInlay(int id){
      var vertices=new List<Vector3>();var triangles=new List<int>();float x=0,y=0;
      void Stroke(float a,float b,float c,float d){
        var start=new Vector3(x+a,y+b,0);var end=new Vector3(x+c,y+d,0);
        var side=Vector3.Cross(Vector3.forward,(end-start).normalized)*.025f;
        int i=vertices.Count;vertices.AddRange(new[]{start-side,end-side,end+side,start+side});
        triangles.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
      }
      void Arc(float rx,float ry,float first,float last){
        for(int n=0;n<24;n++){float a=Mathf.Lerp(first,last,n/24f)*Mathf.Deg2Rad,b=Mathf.Lerp(first,last,(n+1)/24f)*Mathf.Deg2Rad;
          Stroke(rx*Mathf.Cos(a),ry*Mathf.Sin(a),rx*Mathf.Cos(b),ry*Mathf.Sin(b));}
      }
      foreach(int flank in id==0?new[]{0}:new[]{-1,1}){
        x=flank*3.05f;y=id==0?.85f:.06f;
        switch(id){
          case 0: Arc(.55f,.35f,0,180);Stroke(-.63f,0,.63f,0);Stroke(0,.4f,0,.62f);break;
          case 1: for(int k=0;k<3;k++){float h=k*.32f-.36f;Stroke(-.74f,h,.74f-k*.17f,h);Stroke(-.74f,h,-.74f,h+.19f);}break;
          case 2: Arc(.84f,.62f,0,360);Arc(.37f,.62f,0,360);Stroke(-1,0,1,0);break;
          case 3: Stroke(0,-.6f,0,.58f);for(int k=0;k<3;k++)Stroke(-.72f,k*.3f-.32f,.72f,k*.3f-.32f);break;
          case 4: Arc(.56f,.58f,0,180);Stroke(-.56f,0,-.56f,-.56f);Stroke(.56f,0,.56f,-.56f);Stroke(-.65f,-.62f,.65f,-.62f);break;
          case 5: Arc(.67f,.52f,180,360);Stroke(-.64f,.05f,.64f,.05f);Stroke(-.26f,.16f,0,.76f);Stroke(0,.76f,.26f,.16f);break;
          case 6: Arc(.72f,.72f,32,326);Arc(.47f,.47f,148,464);Stroke(-.91f,0,-.31f,0);break;
          case 7: for(int k=1;k<=3;k++)Arc(k*.31f,k*.25f,flank<0?-70:110,flank<0?70:250);break;
          case 8: Stroke(-.86f,0,0,.43f);Stroke(0,.43f,.86f,0);Stroke(.86f,0,0,-.43f);Stroke(0,-.43f,-.86f,0);Arc(.16f,.16f,0,360);break;
          case 9: Stroke(-.88f,-.36f,0,-.36f);Stroke(0,-.36f,.5f,.4f);Stroke(.5f,.4f,.9f,.4f);Stroke(0,-.36f,.82f,-.36f);break;
        }
      }
      var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
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
        if(RoomsOnly&&lookIndex==0)lookIndex=1;
        if(!positioned){
          var scene=SceneManager.GetSceneByPath(City);var cfg=ConstructionConfig4(scene);
          Vector3 point=actor.transform.position;float yaw=0;
          if(lookIndex>=1 && lookIndex<=10){
            var root=scene.GetRootGameObjects().Single(g=>g.name.StartsWith("Dependencia "+lookIndex.ToString("00")+" -")).transform;
            point=root.TransformPoint(V(0,0,3));yaw=root.eulerAngles.y+180;
            if(lookIndex==1){point=V(-145,-16,75);yaw=Quaternion.LookRotation(root.Find("Inscripción violeta").position-point).eulerAngles.y;}
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
        File.AppendAllText((RoomsOnly?CloseWork:LookWork)+"/Vistas.txt",$"{lookIndex}|{Time.frameCount}|{actor.transform.position}|{camera.name}\n");
        positioned=false;
        bool custodian=SessionState.GetBool(LookKey+".custodian",false);
        if(++lookIndex<(custodian?2:RoomsOnly?11:13))return;
        sheet.Apply();File.WriteAllBytes(custodian?"Library/CiudadDelSolCustodio.jpg":"Library/CiudadDelSolRevision.jpg",sheet.EncodeToJPG(94));
        SessionState.SetBool(LookKey,false);EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
      }catch(Exception e){File.WriteAllText(LookWork+"/ErrorVista.txt",e.ToString());SessionState.SetBool(LookKey,false);EditorApplication.Exit(1);}
    }
 }
}
