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
    public static class CiudadDelSolCentroOrientalPreviewGenerator
    {
        const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        
        const string Menu="ROM/Dungeon Ciudad del Sol/Ampliacion vertical - Centro Oriental/";
        const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string Report="Temp/ROM_Reviews/CentroOriental_01";
        static readonly Rect Lower=Rect.MinMaxRect(78,30,132,96);
        static readonly string[] Protected={"Dungeon_Ciudad_del_Sol_Generated","Umbral_De_Las_Profundidades","Subsuelo_Techo_Etapa01","Subsuelo_Acceso_Etapa01","Salida_Meridional_Generated"};
        static readonly Dictionary<string,string> Paths=new Dictionary<string,string>{
            {"Floor",ET+"Pits and Platforms/Platform_Base_01.prefab"},
            {"Stair",ET+"Staircases/Staircase_01.prefab"},
            {"Post",ET+"Columns/Column_Stackable_01.prefab"},
            {"Wall",ET+"Bases/Thin/Base_Wall_02.prefab"},
            {"Beam",ET+"Bases/Ceiling/Ceiling_Beam_01.prefab"},
            {"Rail",ET+"Railings and Decors/Railing_01.prefab"},
            {"StairRail",ET+"Railings and Decors/Railing_Staircase_01.prefab"}, {"Arch",ET+"Bases/Arch_Wide_01.prefab"}, {"Chamfer",ET+"Bases/Thin/Base_Wall_02.prefab"}};
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

        static Scene Source(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())throw new InvalidOperationException("Cerrar otras copias temporales; original abierto fuera de Play.");var s=SceneManager.GetActiveScene();if(s.path!=Target)throw new InvalidOperationException("Abrir Dungeon_Isla_del_Sol original.");return s;}
        static bool Gallery(GameObject g){var b=Visible(g);return b.min.x>=113.98f&&b.max.x<=126.02f;}
        static GameObject[] Floors(IEnumerable<GameObject> roots)=>roots.Where(g=>g.name.StartsWith("Subsuelo_",StringComparison.Ordinal)).SelectMany(g=>g.GetComponentsInChildren<Transform>()).Where(t=>t.parent&&t.parent.name.StartsWith("01 - Piso continuo",StringComparison.Ordinal)&&t.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)&&Overlap(Lower,Visible(t.gameObject))).Select(t=>t.gameObject).ToArray();
        static GameObject[] Capitals(IEnumerable<GameObject> roots)=>roots.Where(g=>g.name.StartsWith("Subsuelo_",StringComparison.Ordinal)).SelectMany(g=>g.GetComponentsInChildren<Transform>()).Where(t=>t.name.StartsWith("Column_Ceiling_02",StringComparison.Ordinal)&&t.position.x>=77.99f&&t.position.x<=132.01f&&t.position.z>=29.99f&&t.position.z<=96.01f).Select(t=>t.gameObject).ToArray();
        static void Audit(Scene s,Dictionary<string,Asset> assets)
        {
            var roots=s.GetRootGameObjects();foreach(var n in Protected)if(roots.Count(g=>g.name==n)!=1)throw new InvalidOperationException("Raiz protegida ausente/duplicada: "+n);
            var floors=Floors(roots);if(floors.Length!=99||floors.Count(Gallery)!=22)throw new InvalidOperationException("Inventario distinto: se esperan99 pisos,22 G04.");
            foreach(var f in floors){var b=Visible(f);if(!Inside(Lower,b)||Mathf.Abs(b.max.y+16)>.02f||(b.size-new Vector3(6,2,6)).sqrMagnitude>.01f||f.GetComponent<MeshFilter>().sharedMesh!=assets["Floor"].Prefab.GetComponent<MeshFilter>().sharedMesh)throw new InvalidOperationException("Piso especial/no autorizado: "+f.transform.position);}
            if(Capitals(roots).Length!=22)throw new InvalidOperationException("Se esperan22 cabeceras conservadas.");
            var cavity=new Bounds(new Vector3(105,-27.02f,63),new Vector3(53.96f,17.96f,65.96f));
            foreach(var root in roots)foreach(var c in root.GetComponentsInChildren<Collider>())if(c.enabled&&!c.isTrigger&&c.bounds.Intersects(cavity))throw new InvalidOperationException("INTERFERENCIA_EXISTENTE_PROTEGIDA: "+root.name+"/"+c.name+" "+c.bounds+". Detener antes de generar; no se altera ese objeto.");
            Debug.Log("CENTRO_ORIENTAL_INSPECCION_OK:54x66=3564m2;77 pisos trasladables,22 G04;22 cabeceras; reserva V3 X-24..60 Z-24..60 y atrio X138..204 fuera de huella. Assets: "+string.Join(";",assets.Select(a=>a.Key+"="+a.Value.Bounds.size)));
        }
        sealed class Guard
        {
            readonly Scene source;readonly bool dirty;readonly string hash;readonly GameObject[] roots;readonly Dictionary<Object,string> state=new Dictionary<Object,string>();
            public Guard(Scene s){source=s;dirty=s.isDirty;hash=Hash();roots=s.GetRootGameObjects();foreach(var g in roots)foreach(var t in g.GetComponentsInChildren<Transform>(true)){state[t.gameObject]=EditorJsonUtility.ToJson(t.gameObject);foreach(var c in t.GetComponents<Component>())if(c)state[c]=EditorJsonUtility.ToJson(c);}}
            public void Verify(){if(!source.IsValid()||source.isDirty!=dirty||Hash()!=hash||!roots.SequenceEqual(source.GetRootGameObjects()))throw new InvalidOperationException("El original cambio; no guardar ni integrar.");foreach(var e in state)if(!e.Key||EditorJsonUtility.ToJson(e.Key)!=e.Value)throw new InvalidOperationException("Componente original alterado: "+(e.Key?e.Key.name:"destruido"));Debug.Log("CENTRO_ORIENTAL_ORIGINAL_INTACTO: archivo, jerarquia, componentes y estado dirty sin cambios; V3 no integrada.");}
        }
        static void Quiet(GameObject go){foreach(var c in go.GetComponentsInChildren<MonoBehaviour>(true))if(c)c.enabled=false;foreach(var c in go.GetComponentsInChildren<Animation>(true))c.enabled=false;foreach(var c in go.GetComponentsInChildren<Animator>(true))c.enabled=false;foreach(var c in go.GetComponentsInChildren<Light>(true))c.enabled=false;foreach(var c in go.GetComponentsInChildren<Camera>(true))c.enabled=false;foreach(var c in go.GetComponentsInChildren<AudioSource>(true)){c.playOnAwake=false;c.enabled=false;}foreach(var c in go.GetComponentsInChildren<ParticleSystem>(true))c.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}

        [MenuItem(Menu+"1. Inspeccionar sin generar")] public static void Inspect(){try{Audit(Source(),Load());}catch(Exception e){Debug.LogException(e);}}
        [MenuItem(Menu+"2. Crear previsualizacion temporal")] public static void Preview(){try{if(StageUtility.GetCurrentStage() is Sector old&&old.Complete){old.View(0);return;}var s=Source();var a=Load();Audit(s,a);var p=ScriptableObject.CreateInstance<Sector>();p.Source=s;p.Original=new Guard(s);p.Assets=a;StageUtility.GoToStage(p,true);}catch(Exception e){Debug.LogException(e);}}
        [MenuItem(Menu+"3. Vista general seccionada")] public static void General(){if(StageUtility.GetCurrentStage() is Sector s&&s.Complete)s.View(0);}
        [MenuItem(Menu+"4. Vista desde Y-34")] public static void Bottom(){if(StageUtility.GetCurrentStage() is Sector s&&s.Complete)s.View(1);}
        [MenuItem(Menu+"5. Vista desde galeria G04")] public static void GalleryView(){if(StageUtility.GetCurrentStage() is Sector s&&s.Complete)s.View(2);}
        [MenuItem(Menu+"6. Validar estructura temporal")] public static void Validate(){try{if(StageUtility.GetCurrentStage() is Sector s&&s.Complete)s.Validate();}catch(Exception e){Debug.LogException(e);}}
        [MenuItem(Menu+"9. Cerrar sin guardar")] public static void Close(){if(StageUtility.GetCurrentStage() is Sector)StageUtility.GoToMainStage();}
        sealed class Piece{public string Key;public GameObject Go;public Bounds Bounds;public bool Reused;}
        sealed class Walk{public Vector3 A,B;public bool Stair;}
        sealed class Chain{public GameObject Receiver;public readonly List<GameObject> Parts=new List<GameObject>();}
        sealed class Sector:PreviewSceneStage
        {
            // Joint preview adapter: individual generation keeps its original scene and geometry.
            internal Scene JointScene;
            Scene WorkScene=>JointScene.IsValid()?JointScene:scene;
            public Scene Source;public Guard Original;public Dictionary<string,Asset> Assets;public bool Complete;
            Transform context,generated;string preserved;int samples;float minClear;
            readonly List<Piece> pieces=new List<Piece>();readonly List<Walk> walks=new List<Walk>();readonly List<Chain> chains=new List<Chain>();readonly List<GameObject> moved=new List<GameObject>();readonly List<Renderer> section=new List<Renderer>();readonly List<GameObject> arches=new List<GameObject>();readonly Dictionary<string,Transform> groups=new Dictionary<string,Transform>();
            protected override GUIContent CreateHeaderContent()=>new GUIContent("Centro Oriental - COPIA TEMPORAL - NO INTEGRAR");
            protected override bool OnOpenStage()
            {
                if(!base.OnOpenStage())return false;
                try
                {
                    var previewScene=scene;previewScene.name="TEMP_CentroOriental_Y34";var holder=new GameObject("Contexto original copiado");SceneManager.MoveGameObjectToScene(holder,WorkScene);context=holder.transform;holder.SetActive(false);
                    foreach(var root in Source.GetRootGameObjects().Where(g=>Protected.Contains(g.name)||g.name.StartsWith("Subsuelo_",StringComparison.Ordinal))){var copy=Object.Instantiate(root,context);copy.name=root.name;copy.transform.SetPositionAndRotation(root.transform.position,root.transform.rotation);copy.transform.localScale=root.transform.lossyScale;}
                    Quiet(holder);holder.SetActive(true);Physics.SyncTransforms();var gen=new GameObject("Subsuelo_CentroOriental_Temporal");SceneManager.MoveGameObjectToScene(gen,WorkScene);generated=gen.transform;
                    var floors=Floors(context.Cast<Transform>().Select(t=>t.gameObject));moved.AddRange(floors.Where(g=>!Gallery(g)));preserved=Signature();Build(floors);Physics.SyncTransforms();Validate();Complete=true;
                    foreach(Transform root in context)foreach(var r in root.GetComponentsInChildren<Renderer>()){if(Protected.Contains(root.name)&&root.name!="Subsuelo_Acceso_Etapa01"&&root.name!="Subsuelo_Techo_Etapa01"||!Overlap(Rect.MinMaxRect(66,18,144,114),r.bounds))SceneVisibilityManager.instance.Hide(r.gameObject,false);else if(r.bounds.min.y>-9)section.Add(r);}
                    foreach(var p in pieces.Where(p=>p.Key=="Wall"&&(p.Bounds.center.x>132||p.Bounds.center.z<30)))section.AddRange(p.Go.GetComponentsInChildren<Renderer>());
                    EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this){Selection.activeGameObject=null;View(0);}};
                    Debug.Log("CENTRO_ORIENTAL_LISTO: solo copia temporal.3 general,4 piso,5 galeria,6 validar,9 cerrar. No Play Mode ni integracion.");
                }
                catch(Exception e){Debug.LogException(e);Debug.LogError("CENTRO_ORIENTAL_NO_VALIDADO: no integrar. Original intacto; copia descartada.");EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this)StageUtility.GoToMainStage();};}
                return true;
            }
            string Signature(){var excluded=new HashSet<GameObject>(moved);return string.Join("\n",context.GetComponentsInChildren<Renderer>(true).Where(r=>!excluded.Contains(r.gameObject)).Select(r=>{var f=r.GetComponent<MeshFilter>();return r.GetInstanceID()+"|"+r.enabled+"|"+r.transform.localToWorldMatrix.ToString("F5")+"|"+(f&&f.sharedMesh?f.sharedMesh.GetInstanceID():0)+"|"+string.Join(",",r.sharedMaterials.Select(m=>m?m.GetInstanceID():0));}));}
            Transform Group(string name){if(!groups.TryGetValue(name,out var g)){var go=new GameObject(name);go.transform.SetParent(generated,false);groups[name]=g=go.transform;}return g;}
            GameObject P(string group,string key,float x,float z,float y,float yaw=0,bool top=true,Vector3? scale=null)
            {
                var a=Assets[key];var sc=scale??Vector3.one;var q=Quaternion.Euler(0,yaw,0);var b=TransformBounds(a.Bounds,Matrix4x4.TRS(Vector3.zero,q,sc));var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,WorkScene);go.transform.SetParent(Group(group),true);go.transform.SetPositionAndRotation(new Vector3(x-b.center.x,y-(top?b.max.y:b.min.y),z-b.center.z),q*a.Prefab.transform.localRotation);go.transform.localScale=Vector3.Scale(a.Prefab.transform.localScale,sc);Quiet(go);go.isStatic=true;pieces.Add(new Piece{Key=key,Go=go,Bounds=Visible(go)});return go;
            }
            void Walk(Vector3 a,Vector3 b,bool stair=false)=>walks.Add(new Walk{A=a,B=b,Stair=stair});
            void Rail(float x,float z,float y,float yaw=0,float length=6)=>P("07 - Protecciones","Rail",x,z,y+1.25f,yaw,true,new Vector3(1,1,length/Assets["Rail"].Bounds.size.z));
            void Shaft(float x,float z,float top,GameObject receiver,string group)
            {
                var chain=new Chain{Receiver=receiver};float h=Assets["Post"].Bounds.size.y;for(float y=-34;y<top-.01f;y+=h)chain.Parts.Add(P(group,"Post",x,z,y,0,false,new Vector3(1,Mathf.Min(h,top-y)/h,1)));chains.Add(chain);
            }
            void Beam(Vector2 a,Vector2 b,float top,string group)
            {
                var d=b-a;float yaw=Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg;for(float o=0;o<d.magnitude-.01f;o+=6){float n=Mathf.Min(6,d.magnitude-o);var c=a+d.normalized*(o+n*.5f);P(group,"Beam",c.x,c.y,top,yaw,true,new Vector3(1,1,n/Assets["Beam"].Bounds.size.z));}
            }
            void Flight(Vector3 start,Vector3 direction,int count)
            {
                var p=start;float yaw=direction.x<-.5f?180:direction.x>.5f?0:direction.z>0?270:90;
                for(int n=0;n<count;n++)
                {
                    var next=p+direction*6+Vector3.down*2;var c=(p+next)*.5f;P("03 - Descenso en nueve tramos","Stair",c.x,c.z,next.y,yaw,false,new Vector3(1,.5f,1));
                    var right=Vector3.Cross(Vector3.up,direction);float railYaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
                    foreach(float sign in new[]{-1f,1f})
                    {
                        var a=Assets["StairRail"];var q=Quaternion.Euler(0,railYaw,0);var sc=new Vector3(1,.5f,1);var b=TransformBounds(a.Bounds,Matrix4x4.TRS(Vector3.zero,q,sc));var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,WorkScene);go.transform.SetParent(Group("07 - Protecciones"),true);var pos=p+right*sign*2.55f;pos.y=p.y+.75f-b.max.y;go.transform.SetPositionAndRotation(pos,q*a.Prefab.transform.localRotation);go.transform.localScale=Vector3.Scale(a.Prefab.transform.localScale,sc);Quiet(go);pieces.Add(new Piece{Key="StairRail",Go=go,Bounds=Visible(go)});
                    }
                    Walk(p,next,true);p=next;
                }
            }
            void Build(GameObject[] floors)
            {
                foreach(var g in floors.Where(g=>!Gallery(g))){g.transform.SetParent(Group("01 - 77 pisos reutilizados Y-34"),true);g.transform.position+=Vector3.down*18;pieces.Add(new Piece{Key="Floor",Go=g,Bounds=Visible(g),Reused=true});}
                foreach(var g in floors.Where(Gallery)){var b=Visible(g);P("02 - Continuidad bajo G04","Floor",b.center.x,b.center.z,-34);}
                var terrace=new List<GameObject>();foreach(float x in new[]{81f,87f})foreach(float z in new[]{87f,93f})terrace.Add(P("04 - Terraza Y-24","Floor",x,z,-24));
                var landing=P("04 - Descanso Y-30","Floor",81,63,-30);
                P("05 - Basamento bajo ultimo descenso","Floor",81,57,-32);
                Flight(new Vector3(114,-16,90),Vector3.left,4);
                Walk(new Vector3(90,-24,90),new Vector3(81,-24,90));Walk(new Vector3(81,-24,90),new Vector3(81,-24,84));
                Flight(new Vector3(81,-24,84),Vector3.back,3);Walk(new Vector3(81,-30,66),new Vector3(81,-30,60));Flight(new Vector3(81,-30,60),Vector3.back,2);
                Walk(new Vector3(120,-16,108),new Vector3(120,-16,90));Walk(new Vector3(120,-16,90),new Vector3(114,-16,90));Walk(new Vector3(120,-16,30),new Vector3(120,-16,108));
                Walk(new Vector3(81,-34,48),new Vector3(81,-34,39));Walk(new Vector3(81,-34,39),new Vector3(99,-34,39));Walk(new Vector3(99,-34,39),new Vector3(99,-34,45));
                Walk(new Vector3(96,-34,45),new Vector3(102,-34,45));Walk(new Vector3(102,-34,45),new Vector3(102,-34,75));Walk(new Vector3(102,-34,75),new Vector3(96,-34,75));Walk(new Vector3(96,-34,75),new Vector3(96,-34,45));
                Walk(new Vector3(99,-34,75),new Vector3(99,-34,93));Walk(new Vector3(99,-34,39),new Vector3(123,-34,39));Walk(new Vector3(123,-34,33),new Vector3(123,-34,51));
                foreach(var capital in Capitals(context.Cast<Transform>().Select(t=>t.gameObject))){var b=Visible(capital);Shaft(capital.transform.position.x,capital.transform.position.z,b.min.y,capital,"05 - Continuidad de 22 apoyos originales");}
                foreach(float x in new[]{114f,126f})
                {
                    Beam(new Vector2(x,30),new Vector2(x,96),-18,"05 - Vigas bajo G04");
                    foreach(float z in new[]{36f,60f,84f}){var receiver=pieces.Where(p=>p.Key=="Beam"&&Mathf.Abs(p.Bounds.center.x-x)<.1f).OrderBy(p=>Mathf.Abs(p.Bounds.center.z-z)).First();Shaft(x,z,receiver.Bounds.min.y,receiver.Go,"05 - Apoyos de borde G04");}
                }
                foreach(var f in terrace){var b=Visible(f);Shaft(b.center.x,b.center.z,b.min.y,f,"05 - Cuatro apoyos de terraza");}
                Shaft(81,63,Visible(landing).min.y,landing,"05 - Apoyo corto descanso");
                // Lower perimeter is retaining structure, not a partition into rooms. Future joins stay closed.
                void Wall(float x,float z,float yaw){P("06 - Contencion y encuentros cerrados","Wall",x,z,-36,yaw,false);P("06 - Contencion y encuentros cerrados","Wall",x,z,-28,yaw,false);P("06 - Contencion y encuentros cerrados","Wall",x,z,-20,yaw,false,new Vector3(1,2/Assets["Wall"].Bounds.size.y,1));}
                foreach(float z in Enumerable.Range(0,11).Select(i=>33+i*6f)){Wall(77.6f,z,0);Wall(132.4f,z,0);if(Mathf.Abs(z-87)>1&&Mathf.Abs(z-93)>1)Rail(77.45f,z,-16);Rail(132.55f,z,-16);}
                foreach(float x in Enumerable.Range(0,9).Select(i=>81+i*6f)){Wall(x,29.6f,90);Wall(x,96.4f,90);if(x<114||x>126){Rail(x,29.45f,-16,90);Rail(x,96.55f,-16,90);}}
                // Upper G04 remains a through-gallery: opening only toward the new staircase at Z90.
                foreach(float z in Enumerable.Range(0,11).Select(i=>33+i*6f)){if(z<87)Rail(114.55f,z,-16);Rail(125.45f,z,-16);}
                Rail(114.55f,85.5f,-16,0,3);Rail(114.55f,94.5f,-16,0,3);
                foreach(float x in new[]{81f,87f})Rail(x,95.45f,-24,90);foreach(float z in new[]{87f,93f})Rail(78.55f,z,-24);Rail(87,84.55f,-24,90);Rail(89.45f,85.5f,-24,0,3);Rail(89.45f,94.5f,-24,0,3);Rail(78.55f,63,-30);Rail(83.45f,63,-30);
                // Four native stone chamfers define the ceremonial floor without closing its broad entries.
                foreach(var ends in new[]{new[]{new Vector2(108,42),new Vector2(114,48)},new[]{new Vector2(114,72),new Vector2(108,78)},new[]{new Vector2(90,78),new Vector2(84,72)},new[]{new Vector2(84,48),new Vector2(90,42)}}){var a=ends[0];var b=ends[1];var d=b-a;var c=(a+b)*.5f;P("08 - Cuatro chaflanes estructurales","Chamfer",c.x,c.y,-34,Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg,false,new Vector3(1,1.2f/Assets["Wall"].Bounds.size.y,d.magnitude/Assets["Wall"].Bounds.size.z));}
                // Arches are used at native width only if the real mesh spans the retained 12m support axes.
                var arch=Assets["Arch"].Bounds;float width=Mathf.Max(arch.size.x,arch.size.z);
                if(Mathf.Abs(width-12)<1.3f){float yaw=arch.size.x>arch.size.z?90:0;foreach(float x in new[]{90f,108f}){var g=P("09 - Arcadas de borde","Arch",x,54,-22,yaw);arches.Add(g);}}
                else Debug.Log("CENTRO_ORIENTAL_ARCOS: no se fuerza Arch_Wide de ancho"+width+" a12m; se conservan las bovedas y arcadas originales.");
            }
            public void View(int mode)
            {
                foreach(var r in section)if(r){if(mode==0)SceneVisibilityManager.instance.Hide(r.gameObject,false);else SceneVisibilityManager.instance.Show(r.gameObject,false);}
                var eye=mode==0?new Vector3(158,-14,12):mode==1?new Vector3(99,-32.2f,40):new Vector3(120,-14.2f,81);var target=mode==0?new Vector3(104,-25,64):mode==1?new Vector3(96,-22,72):new Vector3(96,-28,66);var q=Quaternion.LookRotation(target-eye);var v=SceneView.lastActiveSceneView;if(!v)return;v.sceneLighting=false;v.sceneViewState.showImageEffects=false;v.cameraSettings.fieldOfView=mode==0?60:75;v.orthographic=false;v.LookAtDirect(eye+q*Vector3.forward*30,q,30);v.pivot=eye+q*Vector3.forward*v.cameraDistance;v.Repaint();
            }

            public void Validate()
            {
                Physics.SyncTransforms();Original.Verify();if(Signature()!=preserved)throw new InvalidOperationException("Se altero geometria copiada fuera de77 pisos autorizados.");
                var all=context.GetComponentsInChildren<Collider>().Concat(generated.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy).Distinct().ToArray();var near=all.Where(c=>Overlap(Rect.MinMaxRect(72,24,138,114),c.bounds)).Select(c=>(c,b:c.bounds)).ToArray();var allowed=new HashSet<Collider>(all);var hits=new Collider[256];var physics=WorkScene.GetPhysicsScene();samples=0;minClear=float.PositiveInfinity;
                foreach(var w in walks){var side=Vector3.Cross(Vector3.up,w.B-w.A).normalized;int count=Mathf.CeilToInt(Vector3.Distance(w.A,w.B)/.4f);for(int n=0;n<=count;n++)foreach(float lane in new[]{-1.2f,-.4f,.4f,1.2f})
                {
                    var p=Vector3.Lerp(w.A,w.B,n/(float)count)+side*lane;var ground=new List<RaycastHit>();foreach(var t in near.Where(t=>t.b.min.x<=p.x&&t.b.max.x>=p.x&&t.b.min.z<=p.z&&t.b.max.z>=p.z))if(t.c.Raycast(new Ray(p+Vector3.up*1.15f,Vector3.down),out var hit,2.4f)&&hit.normal.y>.5f)ground.Add(hit);
                    if(ground.Count==0)throw new InvalidOperationException("SIN_SUELO: "+p);var g=ground.OrderBy(h=>Mathf.Abs(h.point.y-p.y)).First();if(Mathf.Abs(g.point.y-p.y)>(w.Stair?.55f:.08f))throw new InvalidOperationException("DESNIVEL: "+p+" / "+g.point);
                    var foot=g.point;int overlaps=physics.OverlapCapsule(foot+Vector3.up*.69f,foot+Vector3.up*1.68f,.32f,hits,~0,QueryTriggerInteraction.Ignore);if(overlaps==hits.Length)throw new InvalidOperationException("Consulta saturada");for(int k=0;k<overlaps;k++)if(allowed.Contains(hits[k]))throw new InvalidOperationException("PASO_BLOQUEADO: "+foot+" / "+hits[k].name+" / "+hits[k].transform.position);
                    foreach(var offset in new[]{Vector3.zero,Vector3.right*.32f,Vector3.left*.32f,Vector3.forward*.32f,Vector3.back*.32f}){var ray=new Ray(foot+offset+Vector3.up*.69f,Vector3.up);foreach(var t in near.Where(t=>t.b.min.x<=ray.origin.x&&t.b.max.x>=ray.origin.x&&t.b.min.z<=ray.origin.z&&t.b.max.z>=ray.origin.z))if(t.c.Raycast(ray,out var h,60)){float clear=h.point.y-foot.y;minClear=Mathf.Min(minClear,clear);if(clear<2.1f)throw new InvalidOperationException("ALTURA_LIBRE: "+foot+" / "+t.c.name+" / "+clear);}}samples++;
                }}
                var lower=pieces.Where(p=>p.Key=="Floor"&&Mathf.Abs(p.Bounds.max.y+34)<.02f).ToArray();if(lower.Length!=99||pieces.Count(p=>p.Reused)!=77||pieces.Count(p=>p.Key=="Stair")!=9)throw new InvalidOperationException("Conteos incorrectos.");
                foreach(var f in lower)foreach(float dx in new[]{-2.9f,0,2.9f})foreach(float dz in new[]{-2.9f,0,2.9f}){var p=f.Bounds.center+new Vector3(dx,0,dz);if(!f.Go.GetComponent<Collider>().Raycast(new Ray(new Vector3(p.x,-32.9f,p.z),Vector3.down),out var h,1.2f)||Mathf.Abs(h.point.y+34)>.025f)throw new InvalidOperationException("PISO_DISCONTINUO: "+p);}
                if(pieces.GroupBy(p=>p.Key+"|"+p.Go.transform.position.ToString("F3")+"|"+p.Go.transform.rotation.ToString("F3")+"|"+p.Go.transform.localScale.ToString("F3")).Any(g=>g.Count()>1))throw new InvalidOperationException("Pieza duplicada");
                foreach(var p in pieces)if(!p.Go.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))throw new InvalidOperationException("Sin collider: "+p.Go.name);
                var cache=new Dictionary<GameObject,Surface>();Surface S(GameObject g){if(!cache.TryGetValue(g,out var v)){v=new Surface(g);cache[g]=v;}return v;}
                foreach(var chain in chains){if(!lower.Any(f=>MeshTouch(S(chain.Parts[0]),S(f.Go))))throw new InvalidOperationException("APOYO_SIN_BASE: "+chain.Parts[0].transform.position);for(int i=1;i<chain.Parts.Count;i++)if(!MeshTouch(S(chain.Parts[i-1]),S(chain.Parts[i])))throw new InvalidOperationException("JUNTA_APOYO: "+chain.Parts[i].transform.position);if(!MeshTouch(S(chain.Parts.Last()),S(chain.Receiver)))throw new InvalidOperationException("APOYO_SIN_RECEPTOR: "+chain.Parts.Last().transform.position);}
                foreach(var arch in arches)if(chains.Count(c=>c.Parts.Any(p=>Visible(p).Intersects(Visible(arch))&&MeshTouch(S(p),S(arch))))<2)throw new InvalidOperationException("ARCO_SIN_DOS_APOYOS: "+arch.transform.position);
                var gallery=Floors(context.Cast<Transform>().Select(t=>t.gameObject)).Where(Gallery).ToArray();if(gallery.Length!=22)throw new InvalidOperationException("G04 alterada.");
                foreach(var b in pieces.Where(p=>p.Key=="Beam"))if(!gallery.Any(f=>Visible(f).Intersects(b.Bounds)&&MeshTouch(S(b.Go),S(f))))throw new InvalidOperationException("VIGA_SIN_GALERIA: "+b.Go.transform.position);
                var walls=pieces.Where(p=>p.Key=="Wall").Select(p=>p.Go.GetComponent<Collider>()).ToArray();foreach(float y in new[]{-33.5f,-25f,-18.2f}){foreach(float z in new[]{33f,45f,57f,69f,81f,93f})foreach(float x in new[]{78f,132f}){var d=x<100?Vector3.left:Vector3.right;var p=new Vector3(x,y,z)-d;if(!walls.Any(c=>c.Raycast(new Ray(p,d),out var h,2)))throw new InvalidOperationException("CONTENCION_ABIERTA: "+p);}foreach(float x in new[]{81f,93f,105f,117f,129f})foreach(float z in new[]{30f,96f}){var d=z<60?Vector3.back:Vector3.forward;var p=new Vector3(x,y,z)-d;if(!walls.Any(c=>c.Raycast(new Ray(p,d),out var h,2)))throw new InvalidOperationException("CONTENCION_ABIERTA: "+p);}}
                int triangles=0;foreach(var f in generated.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh)for(int n=0;n<f.sharedMesh.subMeshCount;n++)triangles+=(int)f.sharedMesh.GetIndexCount(n)/3;
                string report="CENTRO_ORIENTAL_VALIDACION_OK: X78..132 Z30..96;3564m2; pisoY-34;22 pisos G04 Y-16 intactos;77 pisos reutilizados,22 inferiores nuevos;terrazas Y-24/Y-30;nueve tramos;cabeceras22 intactas;apoyos adicionales="+chains.Count+" ejes;modulos="+pieces.Count+";nuevos="+pieces.Count(p=>!p.Reused)+";renderers="+generated.GetComponentsInChildren<Renderer>().Length+";colliders="+generated.GetComponentsInChildren<Collider>().Length+";triangulos="+triangles+";muestras="+samples+";alturaLibreMin="+minClear.ToString("F2")+";encuentros de pilas y vigas comprobados sobre triangulos6cm;contencion cerrada N/S/O;sin integracion, Play ni cambios de original. Validacion estatica, no certificacion de CharacterController runtime.";
                Directory.CreateDirectory(Report);File.WriteAllText(Report+"/Validacion.txt",report+"\n"+string.Join("\n",pieces.Select(p=>p.Key+"|"+p.Go.transform.parent.name+"|"+p.Go.transform.position.ToString("F4")+"|"+(p.Reused?"REUTILIZADO":"NUEVO"))));Debug.Log(report);Original.Verify();
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

