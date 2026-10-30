using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ROM.EditorTools
{
    // Preview only. No integration, asset mutation, scene save, mesh combining or gameplay entry points.
    public static class CiudadDelSolVerticalPilotPreviewGenerator
    {
        const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string East="Subsuelo_Ala_Este_Cierre_Etapa05";
        const string Menu="ROM/Dungeon Ciudad del Sol/Ampliacion vertical - piloto/";
        const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string Report="Temp/ROM_Reviews/PilotoVertical_01";
        static readonly Rect Lower=Rect.MinMaxRect(222,66,240,96),Open=Rect.MinMaxRect(222,66,234,96);
        static readonly string[] Protected={"Dungeon_Ciudad_del_Sol_Generated","Umbral_De_Las_Profundidades","Subsuelo_Techo_Etapa01","Subsuelo_Acceso_Etapa01","Salida_Meridional_Generated"};
        static readonly Dictionary<string,string> Paths=new Dictionary<string,string>{
            {"Floor",ET+"Pits and Platforms/Platform_Base_01.prefab"},
            {"Stair",ET+"Staircases/Staircase_01.prefab"},
            {"Post",ET+"Columns/Column_Stackable_01.prefab"},
            {"Wall",ET+"Bases/Thin/Base_Wall_02.prefab"},
            {"Beam",ET+"Bases/Ceiling/Ceiling_Beam_01.prefab"},
            {"Rail",ET+"Railings and Decors/Railing_01.prefab"},
            {"StairRail",ET+"Railings and Decors/Railing_Staircase_01.prefab"}};
        sealed class Asset{public GameObject Prefab;public Bounds Bounds;}
        static string Hash(){using(var h=SHA256.Create())return Convert.ToBase64String(h.ComputeHash(File.ReadAllBytes(Target)));}
        static bool Overlap(Rect r,Bounds b)=>b.min.x<r.xMax-.01f&&b.max.x>r.xMin+.01f&&b.min.z<r.yMax-.01f&&b.max.z>r.yMin+.01f;
        static bool Inside(Rect r,Bounds b)=>b.min.x>=r.xMin-.02f&&b.max.x<=r.xMax+.02f&&b.min.z>=r.yMin-.02f&&b.max.z<=r.yMax+.02f;
        static Bounds TransformBounds(Bounds b,Matrix4x4 m){bool first=true;Bounds result=default;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var p=m.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);}return result;}
        static Bounds Visible(GameObject g){var rs=g.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(rs.Length==0)throw new InvalidOperationException("Sin geometria: "+g.name);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
        static Dictionary<string,Asset> Load()
        {
            var result=new Dictionary<string,Asset>();foreach(var pair in Paths){var p=AssetDatabase.LoadAssetAtPath<GameObject>(pair.Value);if(!p)throw new InvalidOperationException("Falta prefab: "+pair.Value);var norm=Matrix4x4.TRS(Vector3.zero,p.transform.localRotation,p.transform.localScale)*p.transform.worldToLocalMatrix;bool first=true;Bounds b=default;
                foreach(var f in p.GetComponentsInChildren<MeshFilter>(true)){if(!f.sharedMesh||!f.GetComponent<Renderer>()||!f.GetComponent<Renderer>().enabled)continue;var bb=TransformBounds(f.sharedMesh.bounds,norm*f.transform.localToWorldMatrix);if(first){b=bb;first=false;}else b.Encapsulate(bb);}if(first)throw new InvalidOperationException("Prefab sin malla: "+pair.Value);result[pair.Key]=new Asset{Prefab=p,Bounds=b};}
            if((result["Floor"].Bounds.size-new Vector3(6,2,6)).sqrMagnitude>.01f)throw new InvalidOperationException("Modulo de piso distinto del inventario aprobado.");return result;
        }
        static Scene Source(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())throw new InvalidOperationException("Cerrar otras previsualizaciones desde Scenes; original abierto fuera de Play.");var s=SceneManager.GetActiveScene();if(s.path!=Target)throw new InvalidOperationException("Abrir la escena original Dungeon_Isla_del_Sol.");return s;}
        static GameObject[] Floors(Transform context){var group=context.Find(East+"/01 - Piso continuo Y-16");if(!group)throw new InvalidOperationException("Falta el grupo de piso oriental.");return group.Cast<Transform>().Where(t=>t.gameObject.activeInHierarchy&&t.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)).Select(t=>t.gameObject).ToArray();}
        static void Audit(Scene s,Dictionary<string,Asset> assets)
        {
            var roots=s.GetRootGameObjects();var east=roots.SingleOrDefault(g=>g.name==East);if(!east)throw new InvalidOperationException("Falta Etapa5 integrada.");foreach(var name in Protected)if(roots.Count(g=>g.name==name)!=1)throw new InvalidOperationException("Raiz protegida ambigua: "+name);
            var group=east.transform.Find("01 - Piso continuo Y-16");var selected=group.Cast<Transform>().Select(t=>t.gameObject).Where(g=>Overlap(Open,Visible(g))).ToArray();if(selected.Length!=10)throw new InvalidOperationException("La seleccion autorizada debe tener10 pisos, hay "+selected.Length);
            foreach(var g in selected){var b=Visible(g);if(!Inside(Open,b)||Mathf.Abs(b.max.y+16)>.02f||(b.size-new Vector3(6,2,6)).sqrMagnitude>.01f||g.GetComponent<MeshFilter>().sharedMesh!=assets["Floor"].Prefab.GetComponent<MeshFilter>().sharedMesh)throw new InvalidOperationException("Piso distinto del inventario: "+g.transform.position);}
            var volume=new Bounds(new Vector3(231,-25.005f,81),new Vector3(17.96f,13.97f,29.96f));
            foreach(var g in roots.Where(g=>g.name!=East))foreach(var c in g.GetComponentsInChildren<Collider>())if(c.enabled&&!c.isTrigger&&c.bounds.Intersects(volume))throw new InvalidOperationException("INTERFERENCIA_PROTEGIDA: "+g.name+"/"+c.name+" "+c.bounds+". Detener y consultar antes de generar.");
            var upper=east.transform.Find("04 - Columnas monumentales").GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&Overlap(Open,r.bounds)).ToArray();
            if(upper.Count(r=>Mathf.Abs(r.bounds.center.x-228)<.05f&&(Mathf.Abs(r.bounds.center.z-72)<.05f||Mathf.Abs(r.bounds.center.z-84)<.05f))!=2)throw new InvalidOperationException("Los dos apoyos de cubierta no coinciden con el inventario.");
            Debug.Log("VERTICAL_PILOTO_INSPECCION_OK: abrir X222..234 Z66..96=360m2,10 modulos nativos autorizados; piso inferior X222..240=540m2; conservar5 pisos de galeria oriental. Protegidos, atrio574/8, Umbral639, acceso129 y reservaV3 fuera del volumen. No se modifico nada.");
        }
        sealed class Guard
        {
            readonly Scene source;readonly bool dirty;readonly string hash;readonly GameObject[] roots;readonly Dictionary<Object,string> state=new Dictionary<Object,string>();
            public Guard(Scene s){source=s;dirty=s.isDirty;hash=Hash();roots=s.GetRootGameObjects();foreach(var g in roots)foreach(var t in g.GetComponentsInChildren<Transform>(true)){state[t.gameObject]=EditorJsonUtility.ToJson(t.gameObject);foreach(var c in t.GetComponents<Component>())if(c)state[c]=EditorJsonUtility.ToJson(c);}}
            public void Verify(){if(!source.IsValid()||source.isDirty!=dirty||Hash()!=hash||!roots.SequenceEqual(source.GetRootGameObjects()))throw new InvalidOperationException("El original cambio; no guardar ni integrar.");foreach(var e in state)if(!e.Key||EditorJsonUtility.ToJson(e.Key)!=e.Value)throw new InvalidOperationException("Componente original alterado: "+(e.Key?e.Key.name:"destruido"));Debug.Log("VERTICAL_PILOTO_ORIGINAL_INTACTO: archivo, jerarquia, componentes y estado dirty sin cambios; V3 no integrada.");}
        }
        static void Quiet(GameObject go){foreach(var c in go.GetComponentsInChildren<MonoBehaviour>(true))if(c)c.enabled=false;foreach(var c in go.GetComponentsInChildren<Animation>(true))c.enabled=false;foreach(var c in go.GetComponentsInChildren<Animator>(true))c.enabled=false;foreach(var c in go.GetComponentsInChildren<Light>(true))c.enabled=false;foreach(var c in go.GetComponentsInChildren<Camera>(true))c.enabled=false;foreach(var c in go.GetComponentsInChildren<AudioSource>(true)){c.playOnAwake=false;c.enabled=false;}foreach(var c in go.GetComponentsInChildren<ParticleSystem>(true))c.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        [MenuItem(Menu+"1. Inspeccionar sector sin generar")]
        public static void Inspect(){try{Audit(Source(),Load());}catch(Exception e){Debug.LogException(e);}}
        [MenuItem(Menu+"2. Crear previsualizacion temporal")]
        public static void Preview(){try{if(StageUtility.GetCurrentStage() is Pilot existing&&existing.Complete){existing.View(false);return;}var s=Source();var assets=Load();Audit(s,assets);var p=ScriptableObject.CreateInstance<Pilot>();p.Source=s;p.Original=new Guard(s);p.Assets=assets;StageUtility.GoToStage(p,true);}catch(Exception e){Debug.LogException(e);}}
        [MenuItem(Menu+"3. Vista general seccionada")]
        public static void General(){if(StageUtility.GetCurrentStage() is Pilot p&&p.Complete)p.View(true);}
        [MenuItem(Menu+"4. Vista desde Y-30")]
        public static void Bottom(){if(StageUtility.GetCurrentStage() is Pilot p&&p.Complete)p.View(false);}
        [MenuItem(Menu+"5. Validar copia temporal")]
        public static void Validate(){try{if(StageUtility.GetCurrentStage() is Pilot p&&p.Complete)p.Validate();}catch(Exception e){Debug.LogException(e);}}
        [MenuItem(Menu+"9. Cerrar sin guardar")]
        public static void Close(){if(StageUtility.GetCurrentStage() is Pilot)StageUtility.GoToMainStage();}
        sealed class Piece{public string Key;public GameObject Go;public Bounds Bounds;public bool Reused;}
        sealed class Walk{public Vector3 A,B;public bool Stair;}
        sealed class Chain{public readonly List<GameObject> Parts=new List<GameObject>();public GameObject Receiver;public string Name;}
        sealed class Pilot:PreviewSceneStage
        {
            public Scene Source;public Guard Original;public Dictionary<string,Asset> Assets;public bool Complete;
            Transform context,generated;readonly Dictionary<string,Transform> groups=new Dictionary<string,Transform>();readonly List<Piece> pieces=new List<Piece>();readonly List<Walk> walks=new List<Walk>();readonly List<Chain> chains=new List<Chain>();readonly List<GameObject> moved=new List<GameObject>();readonly List<Renderer> section=new List<Renderer>();string preserved;int samples;float minHeadroom;
            protected override GUIContent CreateHeaderContent()=>new GUIContent("Piloto vertical - COPIA TEMPORAL - NO INTEGRAR");
            protected override bool OnOpenStage()
            {
                if(!base.OnOpenStage())return false;
                try{var s=scene;s.name="TEMP_CiudadDelSol_PilotoVertical01";var holder=new GameObject("Contexto - solo copias originales");SceneManager.MoveGameObjectToScene(holder,scene);context=holder.transform;holder.SetActive(false);
                    foreach(var g in Source.GetRootGameObjects().Where(g=>Protected.Contains(g.name)||g.name.StartsWith("Subsuelo_",StringComparison.Ordinal))){var copy=Object.Instantiate(g,context);copy.name=g.name;copy.transform.SetPositionAndRotation(g.transform.position,g.transform.rotation);copy.transform.localScale=g.transform.lossyScale;}
                    Quiet(holder);holder.SetActive(true);Physics.SyncTransforms();var root=new GameObject("Subsuelo_PilotoVertical01_Temporal");SceneManager.MoveGameObjectToScene(root,scene);generated=root.transform;
                    var select=Floors(context).Where(g=>Overlap(Open,Visible(g))).ToArray();moved.AddRange(select);preserved=Signature();Build(select);Physics.SyncTransforms();Validate();Complete=true;
                    foreach(Transform r in context){if(Protected.Contains(r.name)&&r.name!="Subsuelo_Acceso_Etapa01"&&r.name!="Subsuelo_Techo_Etapa01")SceneVisibilityManager.instance.Hide(r.gameObject,true);else foreach(var renderer in r.GetComponentsInChildren<Renderer>()){if(!Overlap(Rect.MinMaxRect(138,54,246,120),renderer.bounds))SceneVisibilityManager.instance.Hide(renderer.gameObject,false);else if(renderer.bounds.min.y> -9||renderer.bounds.min.x>239.5f&&Overlap(Lower,renderer.bounds))section.Add(renderer);}}
                    foreach(var p in pieces.Where(p=>p.Key=="Wall"&&p.Bounds.center.x>240))section.AddRange(p.Go.GetComponentsInChildren<Renderer>());
                    EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this){Selection.activeGameObject=null;View(false);}};Debug.Log("VERTICAL_PILOTO_LISTO: solo copia temporal; menu3 general,4 inferior,5 validar,9 cerrar sin guardar.");
                }catch(Exception e){Debug.LogException(e);Debug.LogError("VERTICAL_PILOTO_NO_VALIDADO: no integrar. Copia descartada; original conservado.");EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this)StageUtility.GoToMainStage();};}return true;
            }
            string Signature()
            {
                var exclude=new HashSet<GameObject>(moved);
                return string.Join("\n",context.GetComponentsInChildren<Renderer>(true).Where(r=>!exclude.Contains(r.gameObject)).Select(r=>
                {
                    var filter=r.GetComponent<MeshFilter>();var mesh=filter?filter.sharedMesh:null;
                    return r.GetInstanceID()+"|"+r.enabled+"|"+r.gameObject.activeInHierarchy+"|"+r.transform.position.ToString("F5")+"|"+r.transform.rotation.ToString("F5")+"|"+r.transform.lossyScale.ToString("F5")+"|"+string.Join(",",r.sharedMaterials.Select(m=>m?m.GetInstanceID():0))+"|"+(mesh?mesh.GetInstanceID():0);
                }));
            }
            Transform Group(string name){if(!groups.TryGetValue(name,out var t)){var g=new GameObject(name);g.transform.SetParent(generated,false);groups[name]=t=g.transform;}return t;}
            GameObject P(string group,string key,float x,float z,float y,float yaw=0,bool top=true,Vector3? scale=null)
            {
                var a=Assets[key];var sc=scale??Vector3.one;var q=Quaternion.Euler(0,yaw,0);var b=TransformBounds(a.Bounds,Matrix4x4.TRS(Vector3.zero,q,sc));var pivot=new Vector3(x-b.center.x,y-(top?b.max.y:b.min.y),z-b.center.z);var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,scene);go.transform.SetParent(Group(group),true);go.transform.SetPositionAndRotation(pivot,q*a.Prefab.transform.localRotation);go.transform.localScale=Vector3.Scale(a.Prefab.transform.localScale,sc);Quiet(go);go.isStatic=true;pieces.Add(new Piece{Key=key,Go=go,Bounds=Visible(go)});return go;
            }
            void Walk(Vector3 a,Vector3 b,bool stair=false)=>walks.Add(new Walk{A=a,B=b,Stair=stair});
            void Shaft(float x,float z,float top,GameObject receiver,string name)
            {
                var c=new Chain{Name=name,Receiver=receiver};float h=Assets["Post"].Bounds.size.y;for(float y=-30;y<top-.01f;y+=h)c.Parts.Add(P(name,"Post",x,z,y,0,false,new Vector3(1,Mathf.Min(h,top-y)/h,1)));chains.Add(c);
            }
            void BeamLine(Vector2 a,Vector2 b,string group)
            {
                var d=b-a;var dir=d.normalized;float yaw=Mathf.Atan2(dir.x,dir.y)*Mathf.Rad2Deg;for(float o=0;o<d.magnitude-.01f;o+=6){float length=Mathf.Min(6,d.magnitude-o);var c=a+dir*(o+length/2);P(group,"Beam",c.x,c.y,-18,yaw,true,new Vector3(1,1,length/Assets["Beam"].Bounds.size.z));}
            }
            void Rail(float x,float z,float y,float yaw=0)=>P("07 - Protecciones de bordes","Rail",x,z,y+1.25f,yaw);
            void Flight(Vector3 start,Vector3 direction,int count)
            {
                var p=start;float yaw=direction.z>0?270:90;
                for(int i=0;i<count;i++){var next=p+direction*6+Vector3.down*2;P("03 - Siete tramos de escalera","Stair",p.x,(p.z+next.z)/2,next.y,yaw,false,new Vector3(1,.5f,1));
                    foreach(float sign in new[]{-1f,1f}){var a=Assets["StairRail"];var q=Quaternion.Euler(0,direction.z>0?0:180,0);var sc=new Vector3(1,.5f,1);var bb=TransformBounds(a.Bounds,Matrix4x4.TRS(Vector3.zero,q,sc));var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,scene);go.transform.SetParent(Group("07 - Barandas de escalinata"),true);go.transform.SetPositionAndRotation(new Vector3(p.x+sign*2.55f,p.y+.75f-bb.max.y,p.z),q*a.Prefab.transform.localRotation);go.transform.localScale=Vector3.Scale(a.Prefab.transform.localScale,sc);Quiet(go);go.isStatic=true;pieces.Add(new Piece{Key="StairRail",Go=go,Bounds=Visible(go)});}
                    Walk(p,next,true);p=next;}
            }
            void Build(GameObject[] selected)
            {
                foreach(var g in selected){g.transform.SetParent(Group("01 - Diez pisos reutilizados Y-30"),true);g.transform.position+=Vector3.down*14;pieces.Add(new Piece{Key="Floor",Go=g,Bounds=Visible(g),Reused=true});}
                for(int z=69;z<=93;z+=6)P("02 - Piso bajo galeria conservada","Floor",237,z,-30);
                var landing=new[]{P("04 - Descanso de giro Y-24","Floor",225,69,-24),P("04 - Descanso de giro Y-24","Floor",231,69,-24)};
                Flight(new Vector3(225,-16,96),Vector3.back,4);Walk(new Vector3(225,-24,72),new Vector3(225,-24,69));Walk(new Vector3(225,-24,69),new Vector3(231,-24,69));Walk(new Vector3(231,-24,69),new Vector3(231,-24,72));Flight(new Vector3(231,-24,72),Vector3.forward,3);
                Walk(new Vector3(156,-16,99),new Vector3(216,-16,99));Walk(new Vector3(216,-16,99),new Vector3(225,-16,99));Walk(new Vector3(225,-16,99),new Vector3(225,-16,96));
                // The approved upper through-route is X216. The eastern strip carries existing arcade feet;
                // it is preserved structurally, without claiming a new unobstructed route through those feet.
                Walk(new Vector3(216,-16,60),new Vector3(216,-16,120));
                // Closed lower loop around the gallery piers, avoiding the low underside of the return flight.
                Walk(new Vector3(231,-30,90),new Vector3(231,-30,93));Walk(new Vector3(231,-30,93),new Vector3(237,-30,93));Walk(new Vector3(237,-30,93),new Vector3(225,-30,93));Walk(new Vector3(225,-30,93),new Vector3(225,-30,75));Walk(new Vector3(225,-30,75),new Vector3(231,-30,75));Walk(new Vector3(231,-30,75),new Vector3(231,-30,72));Walk(new Vector3(231,-30,72),new Vector3(237,-30,72));Walk(new Vector3(237,-30,72),new Vector3(237,-30,93));
                var capitals=context.Find(East+"/04 - Columnas monumentales").GetComponentsInChildren<Renderer>();foreach(float z in new[]{72f,84f}){var c=capitals.Single(r=>Mathf.Abs(r.bounds.center.x-228)<.05f&&Mathf.Abs(r.bounds.center.z-z)<.05f);Shaft(228,z,c.bounds.min.y,c.gameObject,"05 - Prolongacion de dos apoyos de cubierta");}
                Shaft(225,69,-26,landing[0],"05 - Apoyos cortos del descanso");Shaft(231,69,-26,landing[1],"05 - Apoyos cortos del descanso");
                BeamLine(new Vector2(234,66),new Vector2(234,96),"05 - Viga de galeria oriental");
                foreach(float z in new[]{75f,87f}){var beam=pieces.Single(p=>p.Key=="Beam"&&Mathf.Abs(p.Bounds.center.z-z)<.03f);Shaft(234,z,beam.Bounds.min.y,beam.Go,"05 - Dos apoyos de galeria oriental");}
                BeamLine(new Vector2(222,66),new Vector2(222,96),"06 - Encuentros bajo suelo superior");BeamLine(new Vector2(240,66),new Vector2(240,96),"06 - Encuentros bajo suelo superior");BeamLine(new Vector2(222,66),new Vector2(240,66),"06 - Encuentros bajo suelo superior");BeamLine(new Vector2(222,96),new Vector2(240,96),"06 - Encuentros bajo suelo superior");
                void Wall(float x,float z,float yaw){P("06 - Cerramiento inferior y contencion","Wall",x,z,-32,yaw,false);P("06 - Cerramiento inferior y contencion","Wall",x,z,-24,yaw,false,new Vector3(1,6/Assets["Wall"].Bounds.size.y,1));}
                foreach(float z in new[]{69f,75f,81f,87f,93f}){Wall(221.6f,z,0);Wall(240.4f,z,0);Rail(221.45f,z,-16);Rail(234.55f,z,-16);}
                foreach(float x in new[]{225f,231f,237f}){Wall(x,65.6f,90);Wall(x,96.4f,90);Rail(x,65.45f,-16,90);if(x!=225)Rail(x,96.55f,-16,90);}
                Rail(225,66.55f,-24,90);Rail(231,66.55f,-24,90);Rail(222.55f,69,-24);Rail(233.45f,69,-24);
            }
            public void View(bool general)
            {
                foreach(var r in section)if(r){if(general)SceneVisibilityManager.instance.Hide(r.gameObject,false);else SceneVisibilityManager.instance.Show(r.gameObject,false);}
                var eye=general?new Vector3(253,-22,106):new Vector3(237,-28.2f,93);var target=general?new Vector3(230,-23,81):new Vector3(225,-21,78);var q=Quaternion.LookRotation(target-eye);var v=SceneView.lastActiveSceneView;if(!v)return;v.sceneLighting=false;v.sceneViewState.showImageEffects=false;v.cameraSettings.fieldOfView=general?65:75;v.orthographic=false;v.LookAtDirect(eye+q*Vector3.forward*20,q,20);v.pivot=eye+q*Vector3.forward*v.cameraDistance;v.Repaint();
            }
            public void Validate()
            {
                Physics.SyncTransforms();if(Signature()!=preserved)throw new InvalidOperationException("Cambio no autorizado en geometria copiada fuera de los10 pisos.");Original.Verify();
                var all=context.GetComponentsInChildren<Collider>().Concat(generated.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy).Distinct().ToArray();var lookup=all.Where(c=>Overlap(Rect.MinMaxRect(144,54,246,126),c.bounds)).Select(c=>(c,b:c.bounds)).ToArray();var allowed=new HashSet<Collider>(all);var hits=new Collider[256];var physics=scene.GetPhysicsScene();samples=0;minHeadroom=float.PositiveInfinity;
                foreach(var w in walks){var side=Vector3.Cross(Vector3.up,w.B-w.A).normalized;int count=Mathf.CeilToInt(Vector3.Distance(w.A,w.B)/.4f);for(int n=0;n<=count;n++)foreach(float lane in new[]{-1.2f,-.4f,.4f,1.2f}){var p=Vector3.Lerp(w.A,w.B,n/(float)count)+side*lane;var ground=new List<RaycastHit>();foreach(var t in lookup.Where(t=>t.b.min.x<=p.x&&t.b.max.x>=p.x&&t.b.min.z<=p.z&&t.b.max.z>=p.z))if(t.c.Raycast(new Ray(p+Vector3.up*1.15f,Vector3.down),out var h,2.4f)&&h.normal.y>.5f)ground.Add(h);if(ground.Count==0)throw new InvalidOperationException("Recorrido sin suelo: "+p);var g=ground.OrderBy(h=>Mathf.Abs(h.point.y-p.y)).First();if(Mathf.Abs(g.point.y-p.y)>(w.Stair?.55f:.08f))throw new InvalidOperationException("Desnivel inesperado: "+p+" / "+g.point);var foot=g.point;int overlaps=physics.OverlapCapsule(foot+Vector3.up*.65f,foot+Vector3.up*1.75f,.3f,hits,~0,QueryTriggerInteraction.Ignore);if(overlaps==hits.Length)throw new InvalidOperationException("Consulta saturada.");for(int k=0;k<overlaps;k++)if(allowed.Contains(hits[k]))throw new InvalidOperationException("Paso bloqueado: "+foot+" / "+hits[k].name);
                    foreach(var offset in new[]{Vector3.zero,Vector3.right*.3f,Vector3.left*.3f,Vector3.forward*.3f,Vector3.back*.3f}){var ray=new Ray(foot+offset+Vector3.up*.65f,Vector3.up);foreach(var t in lookup.Where(t=>t.b.min.x<=ray.origin.x&&t.b.max.x>=ray.origin.x&&t.b.min.z<=ray.origin.z&&t.b.max.z>=ray.origin.z))if(t.c.Raycast(ray,out var h,50)){float clear=h.point.y-foot.y;minHeadroom=Mathf.Min(minHeadroom,clear);if(clear<2.1f)throw new InvalidOperationException("Altura libre insuficiente: "+foot+" / "+t.c.name+" / "+clear);}}samples++;}}
                var lower=pieces.Where(p=>p.Key=="Floor"&&Mathf.Abs(p.Bounds.max.y+30)<.02f).ToArray();if(lower.Length!=15||pieces.Count(p=>p.Reused)!=10||pieces.Count(p=>p.Key=="Stair")!=7)throw new InvalidOperationException("Conteos de estructura incorrectos.");foreach(var tile in lower)foreach(float dx in new[]{-2.9f,0,2.9f})foreach(float dz in new[]{-2.9f,0,2.9f}){var p=tile.Bounds.center+new Vector3(dx,0,dz);if(!tile.Go.GetComponent<Collider>().Raycast(new Ray(new Vector3(p.x,-28.9f,p.z),Vector3.down),out var h,1.2f)||Mathf.Abs(h.point.y+30)>.025f)throw new InvalidOperationException("Piso inferior discontinuo: "+p);}
                if(pieces.GroupBy(p=>p.Key+"|"+p.Go.transform.position.ToString("F3")+"|"+p.Go.transform.rotation.ToString("F3")+"|"+p.Go.transform.localScale.ToString("F3")).Any(g=>g.Count()>1))throw new InvalidOperationException("Duplicados en la prueba.");foreach(var p in pieces)if(!p.Go.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))throw new InvalidOperationException("Sin collider: "+p.Go.name);
                ValidateStructure();ValidateWalls();int triangles=0;foreach(var f in generated.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh)for(int s=0;s<f.sharedMesh.subMeshCount;s++)triangles+=(int)f.sharedMesh.GetIndexCount(s)/3;
                var report="VERTICAL_PILOTO_VALIDACION_OK: abertura X222..234 Z66..96=360m2; piso inferior18x30=540m2 Y-30; cubierta Y-4 y galeria6m Y-16 conservadas;7 tramos de6x2m, descenso14m, pendiente18.43grados; descanso12x6 Y-24;10 pisos reutilizados por traslado SOLO copia; nuevos="+pieces.Count(p=>!p.Reused)+"; desactivados=0; total modulos="+pieces.Count+"; renderers="+generated.GetComponentsInChildren<MeshRenderer>().Length+"; colliders="+generated.GetComponentsInChildren<Collider>().Length+"; triangulos="+triangles+"; muestras4carriles="+samples+"; altura libre minima local="+minHeadroom.ToString("F2")+"; desglose="+string.Join("; ",pieces.GroupBy(p=>p.Key).Select(g=>g.Key+":"+g.Count()))+". Sin V3, habitaciones, agua, decoracion, iluminacion, gameplay ni guardado.";
                Directory.CreateDirectory(Report);File.WriteAllText(Report+"/Validacion.txt",report+"\n"+string.Join("\n",pieces.Select(p=>p.Key+"|"+p.Go.transform.parent.name+"|"+p.Go.transform.position.ToString("F5")+"|"+p.Go.transform.localScale.ToString("F5")+"|"+(p.Reused?"REUTILIZADO":"NUEVO"))));Debug.Log(report);Original.Verify();
            }
            void ValidateWalls()
            {
                var walls=pieces.Where(p=>p.Key=="Wall").SelectMany(p=>p.Go.GetComponentsInChildren<Collider>()).ToArray();foreach(float y in new[]{-29.8f,-25f,-21f,-18.2f}){foreach(float z in new[]{69f,75f,81f,87f,93f})foreach(float x in new[]{222f,240f}){var dir=x<230?Vector3.left:Vector3.right;var origin=new Vector3(x,y,z)-dir;if(!walls.Any(c=>c.Raycast(new Ray(origin,dir),out var h,2)))throw new InvalidOperationException("Abertura en cerramiento X: "+origin);}foreach(float x in new[]{225f,231f,237f})foreach(float z in new[]{66f,96f}){var dir=z<80?Vector3.back:Vector3.forward;var origin=new Vector3(x,y,z)-dir;if(!walls.Any(c=>c.Raycast(new Ray(origin,dir),out var h,2)))throw new InvalidOperationException("Abertura en cerramiento Z: "+origin);}}Debug.Log("VERTICAL_PILOTO_CERRAMIENTO_OK:64 sondas a cuatro alturas; sin salida al vacio por las paredes inferiores.");
            }
            void ValidateStructure()
            {
                var cache=new Dictionary<GameObject,Surface>();Surface S(GameObject g){if(!cache.TryGetValue(g,out var s)){s=new Surface(g);cache.Add(g,s);}return s;}
                var lower=pieces.Where(p=>p.Key=="Floor"&&Mathf.Abs(p.Bounds.max.y+30)<.02f).ToArray();foreach(var c in chains){if(!lower.Any(f=>MeshTouch(S(c.Parts[0]),S(f.Go))))throw new InvalidOperationException("Pila sin contacto real con piso: "+c.Name);for(int n=1;n<c.Parts.Count;n++)if(!MeshTouch(S(c.Parts[n-1]),S(c.Parts[n])))throw new InvalidOperationException("Pila con junta abierta: "+c.Name);if(!MeshTouch(S(c.Parts.Last()),S(c.Receiver)))throw new InvalidOperationException("Pila sin receptor: "+c.Name);}
                var floors=Floors(context);foreach(var p in pieces.Where(p=>p.Key=="Beam"))if(!floors.Any(f=>Visible(f).Intersects(p.Bounds)&&MeshTouch(S(p.Go),S(f)))&&!pieces.Any(q=>q.Key=="Wall"&&q.Bounds.Intersects(p.Bounds)&&MeshTouch(S(p.Go),S(q.Go))))throw new InvalidOperationException("Viga sin encuentro real con suelo o contencion: "+p.Go.transform.position);
                Debug.Log("VERTICAL_PILOTO_APOYOS_MALLAS_OK:6 ejes (2 prolongaciones de cubierta,2 de borde de galeria,2 cortos de descanso), sin retirar columnas; juntas y receptores verificados sobre triangulos, tolerancia6cm. Vigas superiores enlazadas con suelos conservados o contencion.");
            }
            sealed class Surface
            {
                public readonly List<Vector3[]> Triangles=new List<Vector3[]>();
                public Surface(GameObject go)
                {
                    foreach(var f in go.GetComponentsInChildren<MeshFilter>())
                    {
                        var r=f.GetComponent<Renderer>();if(!r||!r.enabled||!f.sharedMesh)continue;
                        var vertices=f.sharedMesh.vertices.Select(f.transform.TransformPoint).ToArray();var indices=f.sharedMesh.triangles;
                        for(int i=0;i<indices.Length;i+=3)Triangles.Add(new[]{vertices[indices[i]],vertices[indices[i+1]],vertices[indices[i+2]]});
                    }
                    if(Triangles.Count==0)throw new InvalidOperationException("Sin triangulos para verificar encuentro: "+go.name);
                }
            }
            static Vector3 ClosestTriangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
            {
                var ab=b-a;var ac=c-a;var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);if(d1<=0&&d2<=0)return a;
                var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return b;
                float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return a+ab*(d1/(d1-d3));
                var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return c;
                float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return a+ac*(d2/(d2-d6));
                float va=d3*d6-d5*d4;if(va<=0&&d4-d3>=0&&d5-d6>=0)return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));
                float sum=va+vb+vc;if(Mathf.Abs(sum)<1e-12f)return a;
                return a+ab*(vb/sum)+ac*(vc/sum);
            }
            static bool SegmentTriangle(Vector3 a,Vector3 b,Vector3[] t)
            {
                var d=b-a;var e=t[1]-t[0];var f=t[2]-t[0];var h=Vector3.Cross(d,f);float det=Vector3.Dot(e,h);if(Mathf.Abs(det)<1e-8f)return false;
                var s=a-t[0];float u=Vector3.Dot(s,h)/det;if(u<0||u>1)return false;var q=Vector3.Cross(s,e);float v=Vector3.Dot(d,q)/det;if(v<0||u+v>1)return false;
                float distance=Vector3.Dot(f,q)/det;return distance>=0&&distance<=1;
            }
            static float SegmentGap(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                var u=b-a;var v=d-c;var w=a-c;float aa=Vector3.Dot(u,u),bb=Vector3.Dot(u,v),cc=Vector3.Dot(v,v),dd=Vector3.Dot(u,w),ee=Vector3.Dot(v,w);
                if(aa<1e-10f||cc<1e-10f)return Mathf.Min((a-c).sqrMagnitude,(b-d).sqrMagnitude);
                float den=aa*cc-bb*bb;float s=den>1e-10f?Mathf.Clamp01((bb*ee-cc*dd)/den):0;float t=(bb*s+ee)/cc;
                if(t<0){t=0;s=Mathf.Clamp01(-dd/aa);}else if(t>1){t=1;s=Mathf.Clamp01((bb-dd)/aa);}
                return (a+u*s-c-v*t).sqrMagnitude;
            }
            static bool MeshTouch(Surface a,Surface b,float tolerance=.06f)
            {
                float sq=tolerance*tolerance;
                foreach(var x in a.Triangles)foreach(var y in b.Triangles)
                {
                    var bx=new Bounds(x[0],Vector3.zero);bx.Encapsulate(x[1]);bx.Encapsulate(x[2]);bx.Expand(tolerance*2);
                    var by=new Bounds(y[0],Vector3.zero);by.Encapsulate(y[1]);by.Encapsulate(y[2]);if(!bx.Intersects(by))continue;
                    for(int i=0;i<3;i++)
                    {
                        if(SegmentTriangle(x[i],x[(i+1)%3],y)||SegmentTriangle(y[i],y[(i+1)%3],x))return true;
                        if((x[i]-ClosestTriangle(x[i],y[0],y[1],y[2])).sqrMagnitude<=sq||(y[i]-ClosestTriangle(y[i],x[0],x[1],x[2])).sqrMagnitude<=sq)return true;
                        for(int j=0;j<3;j++)if(SegmentGap(x[i],x[(i+1)%3],y[j],y[(j+1)%3])<=sq)return true;
                    }
                }
                return false;
            }

            protected override void OnCloseStage(){try{Original?.Verify();}finally{base.OnCloseStage();}}
        }
    }
}

// Pilot revision: renderer-safe signature and grounded stair rails.
