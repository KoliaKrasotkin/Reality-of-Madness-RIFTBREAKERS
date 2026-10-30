using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    // Deliberately has no integration or scene-save command. All surgery is on preview copies.
    public static class CiudadDelSolSolInvertidoPreviewGenerator
    {
        const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Menu="ROM/Dungeon Ciudad del Sol/El Sol Invertido - prueba temporal/";
        const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string Crown="Assets/Standard Assets/Sun_Temple/Prefabs/Modules_Temple/Bld_Temple_Deco_Sun_A.prefab";
        static readonly Rect Outer=Rect.MinMaxRect(-24,-24,60,60);
        static readonly Rect Opening=Rect.MinMaxRect(-18,-18,54,54);
        static string Hash(){using(var h=SHA256.Create())return Convert.ToBase64String(h.ComputeHash(File.ReadAllBytes(Target)));}
        static bool Inside(Rect r,Bounds b,float tolerance=.02f)=>b.min.x>=r.xMin-tolerance&&b.max.x<=r.xMax+tolerance&&b.min.z>=r.yMin-tolerance&&b.max.z<=r.yMax+tolerance;
        static bool Overlap(Rect r,Bounds b)=>b.max.x>r.xMin+.02f&&b.min.x<r.xMax-.02f&&b.max.z>r.yMin+.02f&&b.min.z<r.yMax-.02f;
        static bool IsStage(Transform t)=>t.name.StartsWith("Subsuelo_Central_Sureste_Etapa02",StringComparison.Ordinal)||t.name.StartsWith("Subsuelo_Ala_Occidental_Etapa03",StringComparison.Ordinal)||t.name.StartsWith("Subsuelo_Ala_Norte_Etapa04",StringComparison.Ordinal)||t.name.StartsWith("Subsuelo_Ala_Este_Cierre_Etapa05",StringComparison.Ordinal);
        static Scene Source()
        {
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())throw new InvalidOperationException("Activa la escena original fuera de Play y de otras previsualizaciones.");
            var s=SceneManager.GetActiveScene();if(s.path!=Target)throw new InvalidOperationException("La escena activa no es Dungeon_Isla_del_Sol.");
            return s;
        }
        [MenuItem(Menu+"1. Inspeccionar sin generar %&F9")]
        public static void Inspect(){try{Audit(Source());}catch(Exception e){Debug.LogException(e);}}
        static void Audit(Scene s)
        {
            var roots=s.GetRootGameObjects();var stages=roots.Where(g=>IsStage(g.transform)).ToArray();
            if(stages.Length!=4)throw new InvalidOperationException("No se encuentran las cuatro etapas de superficie integradas.");
            var floor=stages.SelectMany(g=>g.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger&&c.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)&&Mathf.Abs(c.bounds.max.y+16)<.025f&&Overlap(Outer,c.bounds)).ToArray();
            var opened=floor.Where(c=>Inside(Opening,c.bounds)).ToArray();
            if(opened.Length!=144)throw new InvalidOperationException("La abertura de 72x72 no coincide con 144 modulos completos: "+opened.Length);
            foreach(var c in floor.Where(c=>Overlap(Opening,c.bounds)))if(!Inside(Opening,c.bounds))throw new InvalidOperationException("Modulo de piso cruza el borde de apertura: "+c.name+" "+c.bounds);
            var supports=stages.SelectMany(g=>g.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger&&Overlap(Outer,c.bounds)&&c.bounds.min.y<-8.1f&&c.bounds.max.y>-15.9f&&!floor.Contains(c)).ToArray();
            foreach(var g in roots.Where(g=>!IsStage(g.transform)))
            foreach(var c in g.GetComponentsInChildren<Collider>())
                if(c.enabled&&!c.isTrigger&&Overlap(Outer,c.bounds)&&c.bounds.min.y<-17.99f)throw new InvalidOperationException("ESTRUCTURA PROTEGIDA invade la excavacion: "+g.name+"/"+c.name+" "+c.bounds);
            var roof=stages.SelectMany(g=>g.GetComponentsInChildren<Renderer>()).Where(r=>r.enabled&&Overlap(Outer,r.bounds)&&r.name.StartsWith("Base_Ceiling",StringComparison.Ordinal)).ToArray();
            Debug.Log("SOL_INVERTIDO_INSPECCION: viable para prueba aislada. Huella84x84 X-24..60 Z-24..60; abrir72x72 X-18..54 Z-18..54; piso a desactivar solo en copia="+opened.Length+"; conservar piso perimetral="+(floor.Length-opened.Length)+" colliders; soportes existentes en huella="+supports.Length+"; cubiertas conservadas="+roof.Length+". Apoyos="+string.Join("; ",supports.GroupBy(c=>c.name).Select(g=>g.Key+":"+g.Count()))+". Escena dirty="+s.isDirty+" (no se guardara). SHA="+Hash());
        }

        sealed class Asset { public GameObject Prefab; public Bounds Bounds; }
        static readonly Dictionary<string,string> Paths=new Dictionary<string,string>{
            {"Floor",ET+"Pits and Platforms/Platform_Base_01.prefab"},
            {"Stair",ET+"Staircases/Staircase_01.prefab"},
            {"Post",ET+"Columns/Column_Stackable_01.prefab"},
            {"Wall",ET+"Bases/Thin/Base_Wall_02.prefab"},
            {"Arch",ET+"Bases/Arch_Wide_01.prefab"},
            {"Rail",ET+"Railings and Decors/Railing_01.prefab"},
            {"Beam",ET+"Bases/Ceiling/Ceiling_Beam_01.prefab"},
            {"Bracket",ET+"Bases/Base_Arch_01.prefab"},
            {"TallBracket",ET+"Bases/Base_Arch_Tall_01.prefab"},
            {"Sun",Crown}
        };
        static Bounds TransformBounds(Bounds b,Matrix4x4 m)
        {
            bool first=true;Bounds result=default;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {var p=m.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);}return result;
        }
        static Dictionary<string,Asset> Load()
        {
            var result=new Dictionary<string,Asset>();
            foreach(var item in Paths)
            {
                var go=AssetDatabase.LoadAssetAtPath<GameObject>(item.Value);if(!go)throw new InvalidOperationException("Falta prefab: "+item.Value);
                var root=go.transform;var norm=Matrix4x4.TRS(Vector3.zero,root.localRotation,root.localScale)*root.worldToLocalMatrix;
                bool first=true;Bounds b=default;
                foreach(var f in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r=f.GetComponent<Renderer>();if(!f.sharedMesh||!r||!r.enabled)continue;
                    var bb=TransformBounds(f.sharedMesh.bounds,norm*f.transform.localToWorldMatrix);if(first){b=bb;first=false;}else b.Encapsulate(bb);
                }
                if(first)throw new InvalidOperationException("Prefab sin geometria medible: "+item.Value);
                result[item.Key]=new Asset{Prefab=go,Bounds=b};
            }
            Debug.Log("SOL_INVERTIDO_ASSETS: "+string.Join("; ",result.Select(k=>k.Key+"="+k.Value.Bounds)));
            return result;
        }
        sealed class Guard
        {
            readonly Scene scene;readonly bool dirty;readonly string hash;
            readonly GameObject[] roots;readonly Dictionary<Object,string> state=new Dictionary<Object,string>();
            public Guard(Scene s)
            {
                scene=s;dirty=s.isDirty;hash=Hash();roots=s.GetRootGameObjects();
                foreach(var g in roots)foreach(var t in g.GetComponentsInChildren<Transform>(true))
                {state[t.gameObject]=EditorJsonUtility.ToJson(t.gameObject);foreach(var c in t.GetComponents<Component>())if(c)state[c]=EditorJsonUtility.ToJson(c);}
            }
            public void Verify()
            {
                if(!scene.IsValid()||scene.isDirty!=dirty||Hash()!=hash||!roots.SequenceEqual(scene.GetRootGameObjects()))throw new InvalidOperationException("La escena original cambio durante la prueba.");
                foreach(var entry in state)if(!entry.Key||EditorJsonUtility.ToJson(entry.Key)!=entry.Value)throw new InvalidOperationException("Componente original alterado: "+(entry.Key?entry.Key.name:"destruido"));
                Debug.Log("SOL_INVERTIDO_ORIGINAL_INTACTO: componentes, jerarquia, estado dirty y archivo original conservados.");
            }
        }
        [MenuItem(Menu+"2. Crear previsualizacion aislada %&F10")]
        public static void Preview()=>Launch(false);
        [MenuItem(Menu+"10. Crear segunda variante - camara abierta")]
        public static void PreviewOpen()=>Launch(true);
        [MenuItem(Menu+"15. Inspeccionar modulos para V3 - sin generar")]
        public static void InspectThirdAssets(){try{Load();}catch(Exception e){Debug.LogException(e);}}
        [MenuItem(Menu+"16. Crear V3 - apoyos verificados")]
        public static void PreviewThird()=>Launch(true,true);
        [MenuItem(Menu+"17. Comparar V3 - fondo")]
        public static void ThirdBottom(){var p=StageUtility.GetCurrentStage() as SolPreview;if(!p||!p.Live||!p.HasThird)return;p.ShowThird();View(3);}
        [MenuItem(Menu+"18. Comparar V3 - general")]
        public static void ThirdGeneral(){var p=StageUtility.GetCurrentStage() as SolPreview;if(!p||!p.Live||!p.HasThird)return;p.ShowThird();View(4);}
        [MenuItem(Menu+"19. V3 estructural - cubierta visible")]
        public static void ThirdStructure()
        {
            var p=StageUtility.GetCurrentStage() as SolPreview;if(!p||!p.Live||!p.HasThird)return;
            p.ShowThird();View(3);p.ShowSection(false);
            var v=SceneView.lastActiveSceneView;v.cameraSettings.fieldOfView=80;var eye=new Vector3(0,-34,0);var target=new Vector3(18,-18,18);
            var q=Quaternion.LookRotation(target-eye);v.LookAtDirect(eye+q*Vector3.forward*20,q,20);v.pivot=eye+q*Vector3.forward*v.cameraDistance;v.Repaint();
        }
        static void Launch(bool open,bool third=false)
        {
            try
            {
                if(StageUtility.GetCurrentStage() is SolPreview current&&current.Live)
                {
                    if(third){current.MakeThirdVariant();current.ShowThird();}
                    else if(open){current.MakeOpenVariant();current.ShowReference(false);}
                    else if(current.HasReference)current.ShowReference(true);
                    General();return;
                }
                if(StageUtility.GetCurrentStage() is SolPreview)Close();
                var source=Source();Audit(source);var assets=Load();
                var stage=ScriptableObject.CreateInstance<SolPreview>();stage.Source=source;stage.Assets=assets;stage.Original=new Guard(source);stage.OpenVariant=open;stage.BuildThird=third;
                StageUtility.GoToStage(stage,true);
            }
            catch(Exception e){Debug.LogException(e);}
        }
        [MenuItem(Menu+"3. Vista acceso superior %&F11")] public static void Upper()=>View(0);
        [MenuItem(Menu+"4. Vista puente Y-24 %&F12")] public static void Bridge24()=>View(1);
        [MenuItem(Menu+"5. Vista puente Y-30")] public static void Bridge30()=>View(2);
        [MenuItem(Menu+"6. Vista fondo")] public static void Bottom()=>View(3);
        [MenuItem(Menu+"7. Vista general seccionada")] public static void General()=>View(4);
        [MenuItem(Menu+"8. Validar copia temporal")]
        public static void Validate(){try{var p=StageUtility.GetCurrentStage() as SolPreview;if(!p||!p.Complete)throw new InvalidOperationException("No hay una prueba completa.");int version=p.CurrentVersion;try{if(p.HasThird)p.ShowThird();else if(p.HasReference)p.ShowReference(false);p.ValidateGeometry();p.Original.Verify();}finally{if(version==1)p.ShowReference(true);else if(version==2)p.ShowReference(false);}}catch(Exception e){Debug.LogException(e);}}
        [MenuItem(Menu+"11. Comparar V1 - fondo %&1")] public static void V1Bottom()=>Compare(true,3);
        [MenuItem(Menu+"12. Comparar V2 - fondo %&2")] public static void V2Bottom()=>Compare(false,3);
        [MenuItem(Menu+"13. Comparar V1 - general %&3")] public static void V1General()=>Compare(true,4);
        [MenuItem(Menu+"14. Comparar V2 - general %&4")] public static void V2General()=>Compare(false,4);
        static void Compare(bool reference,int view){var p=StageUtility.GetCurrentStage() as SolPreview;if(!p||!p.Complete)return;if(!p.HasReference)p.MakeOpenVariant();p.ShowReference(reference);View(view);}
        [MenuItem(Menu+"9. Cerrar sin guardar")]
        public static void Close(){if(StageUtility.GetCurrentStage() is SolPreview)StageUtility.GoToMainStage();}
        static void View(int index)
        {
            var p=StageUtility.GetCurrentStage() as SolPreview;if(!p||!p.Complete)return;
            p.ShowSection(index==4);
            var camera=p.Views[index];var v=SceneView.lastActiveSceneView;if(!v)return;
            v.sceneLighting=false;v.sceneViewState.showImageEffects=false;v.orthographic=false;v.cameraSettings.fieldOfView=60;
            v.LookAtDirect(camera.transform.position+camera.transform.forward*20,camera.transform.rotation,20);
            v.pivot=camera.transform.position+camera.transform.forward*v.cameraDistance;v.Repaint();
        }
        static void Quiet(GameObject go)
        {
            foreach(var c in go.GetComponentsInChildren<MonoBehaviour>(true))if(c)c.enabled=false;
            foreach(var c in go.GetComponentsInChildren<Animation>(true))c.enabled=false;
            foreach(var c in go.GetComponentsInChildren<Animator>(true))c.enabled=false;
            foreach(var c in go.GetComponentsInChildren<Light>(true))c.enabled=false;
            foreach(var c in go.GetComponentsInChildren<Camera>(true))c.enabled=false;
            foreach(var c in go.GetComponentsInChildren<AudioSource>(true)){c.playOnAwake=false;c.enabled=false;}
            foreach(var c in go.GetComponentsInChildren<ParticleSystem>(true))c.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        sealed class Walk { public Vector3 A,B;public bool Stair; }
        sealed class Piece {public string Key;public GameObject Go;public Bounds Bounds;}
        sealed class SolPreview:PreviewSceneStage
        {
            // Joint preview adapter: individual generation keeps its original scene and geometry.
            internal Scene JointScene; bool jointReferenceReady,jointThirdReady;
            Scene WorkScene=>JointScene.IsValid()?JointScene:scene;
            public Scene Source;public Guard Original;public Dictionary<string,Asset> Assets;public bool Complete,OpenVariant,ViewingReference;
            public bool HasReference=>JointScene.IsValid()?jointReferenceReady:referenceRoot;
            public bool HasThird=>JointScene.IsValid()?jointThirdReady:secondGenerated;
            public bool BuildThird,ViewingSecond;
            public int CurrentVersion=>ViewingReference?1:ViewingSecond?2:HasThird?3:HasReference?2:1;
            public bool Live=>Complete&&context&&generated&&Original!=null&&Assets!=null&&pieces.Count>0;
            public Camera[] Views=new Camera[5];
            Transform context,generated;readonly List<Piece> pieces=new List<Piece>();readonly List<Walk> walks=new List<Walk>();
            readonly Dictionary<string,Transform> groups=new Dictionary<string,Transform>();
            readonly List<GameObject> omitted=new List<GameObject>();readonly List<Mesh> meshes=new List<Mesh>();
            readonly List<GameObject> roofViews=new List<GameObject>();
            Transform referenceRoot,referenceContext,referenceGenerated;
            Transform secondGenerated;
            readonly List<GameObject> secondWalls=new List<GameObject>();
            readonly List<ThirdPlan> thirdAccepted=new List<ThirdPlan>();
            readonly List<GameObject> referenceRoof=new List<GameObject>();
            readonly List<GameObject> referenceWalls=new List<GameObject>();
            readonly List<Vector2> retainedInterior=new List<Vector2>();
            readonly List<string> columnReport=new List<string>();
            int removedColumns,extendedColumns,walkSamples;float minHeadroom=float.PositiveInfinity;Bounds sunBounds;
            protected override GUIContent CreateHeaderContent()=>new GUIContent("El Sol Invertido - COPIA DESCARTABLE - no integrar");
            protected override bool OnOpenStage()
            {
                if(!base.OnOpenStage())return false;
                try
                {
                    var temporary=scene;temporary.name="TEMP_CiudadDelSol_SolInvertido";
                    var container=new GameObject("Contexto - exclusivamente copias");SceneManager.MoveGameObjectToScene(container,WorkScene);context=container.transform;container.SetActive(false);
                    foreach(var root in Source.GetRootGameObjects().Where(g=>g.name=="Dungeon_Ciudad_del_Sol_Generated"||g.name.StartsWith("Subsuelo_",StringComparison.Ordinal)||g.name=="Umbral_De_Las_Profundidades"||g.name=="Salida_Meridional_Generated"))
                    {
                        var copy=Object.Instantiate(root,context);copy.name=root.name;copy.transform.SetPositionAndRotation(root.transform.position,root.transform.rotation);copy.transform.localScale=root.transform.lossyScale;
                    }
                    Quiet(container);container.SetActive(true);Physics.SyncTransforms();
                    var newRoot=new GameObject("SolInvertido_CamaraCentral_Prueba");SceneManager.MoveGameObjectToScene(newRoot,WorkScene);generated=newRoot.transform;
                    OpenCopies();Build();Physics.SyncTransforms();ValidateGeometry();Original.Verify();
                    if(OpenVariant)MakeOpenVariant();if(BuildThird)MakeThirdVariant();CreateViews();Complete=true;
                    EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this){General();Selection.activeGameObject=null;}};
                    Debug.Log("SOL_INVERTIDO_PREVIEW_LISTA: solo copia temporal, sin guardar ni integrar. Menu ROM/Dungeon Ciudad del Sol/El Sol Invertido - prueba temporal/3..7.");
                }
                catch(Exception e)
                {
                    Debug.LogException(e);Debug.LogError("SOL_INVERTIDO_PRUEBA_NO_VALIDADA: no integrar. Cerrar desde el menu 9; original intacto.");
                    EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this)StageUtility.GoToMainStage();};
                }
                return true;
            }
            void Omit(GameObject go){if(!omitted.Contains(go)){go.SetActive(false);omitted.Add(go);}}
            static Bounds VisibleBounds(GameObject go)
            {
                var rs=go.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new InvalidOperationException("Sin renderer: "+go.name);
                var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
            }
            void Widen(GameObject go,float factor)
            {
                var before=VisibleBounds(go);go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(factor,1,factor));
                var after=VisibleBounds(go);var shift=before.center-after.center;shift.y=0;go.transform.position+=shift;
            }
            string FixedLayout()=>string.Join("\n",pieces.Where(p=>p.Key!="Post"&&p.Key!="Beam"&&!p.Go.transform.parent.name.StartsWith("V3 -",StringComparison.Ordinal)).Select(p=>p.Key+"|"+p.Go.transform.parent.name+"|"+p.Go.transform.position.ToString("F5")+"|"+p.Go.transform.rotation.ToString("F5")+"|"+p.Go.transform.localScale.ToString("F5")));
            static bool At(Vector3 p,Vector2 q)=>Mathf.Abs(p.x-q.x)<.06f&&Mathf.Abs(p.z-q.y)<.06f;
            void Inventory(string label)
            {
                columnReport.Add("\n"+label);
                foreach(Transform root in context)
                {
                    var list=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&Overlap(Outer,r.bounds)&&r.bounds.max.y> -38&&r.bounds.min.y< -8.1f&&(r.name.Contains("Column")||r.name.Contains("Pillar"))).ToArray();
                    foreach(var g in list.GroupBy(r=>r.name))columnReport.Add("CONTEXTO "+root.name+" / "+g.Key+": "+g.Count()+" renderers; "+(IsStage(root)?"etapa del subsuelo":"estructura protegida, oculta en las vistas de la camara"));
                }
                foreach(var g in pieces.Where(p=>p.Key=="Post").GroupBy(p=>p.Go.transform.parent.name))
                {
                    int shafts=g.Select(p=>new Vector2(Mathf.Round(p.Bounds.center.x*100)/100,Mathf.Round(p.Bounds.center.z*100)/100)).Distinct().Count();
                    columnReport.Add("NUEVO "+g.Key+": "+g.Count()+" modulos / "+shafts+" ejes; Column_Stackable_01");
                }
            }
            public void MakeOpenVariant()
            {
                if(HasReference)return;
                Original.Verify();Inventory("V1 - REFERENCIA SIN CAMBIOS");string layout=FixedLayout();
                if(!JointScene.IsValid())
                {
                var rr=new GameObject("V1 - Referencia original conservada - alternar desde menu");SceneManager.MoveGameObjectToScene(rr,WorkScene);referenceRoot=rr.transform;rr.SetActive(false);
                referenceContext=Object.Instantiate(context.gameObject,referenceRoot).transform;referenceContext.name=context.name;
                referenceGenerated=Object.Instantiate(generated.gameObject,referenceRoot).transform;referenceGenerated.name=generated.name;
                // Renderer visibility is editor state, so explicitly reproduce the V1 cutaway on its new copies.
                foreach(var item in roofViews){var t=referenceContext.Find(AnimationUtility.CalculateTransformPath(item.transform,context));if(t)referenceRoof.Add(t.gameObject);}
                foreach(var item in pieces.Where(p=>p.Key=="Wall"&&(p.Bounds.center.x>59||p.Bounds.center.z< -23)))
                {var t=referenceGenerated.Find(AnimationUtility.CalculateTransformPath(item.Go.transform,generated));if(t)referenceWalls.Add(t.gameObject);}
                }
                jointReferenceReady=JointScene.IsValid();
                var capitals=context.Cast<Transform>().Where(IsStage).SelectMany(t=>t.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger&&c.name.StartsWith("Column_Ceiling_02",StringComparison.Ordinal)&&Inside(Opening,new Bounds(new Vector3(c.bounds.center.x,0,c.bounds.center.z),Vector3.one*.1f))).ToList();
                if(capitals.Count!=17)throw new InvalidOperationException("V1 distinta de la referencia: se esperaban17 apoyos interiores, hay "+capitals.Count);
                var pool=new List<Collider>(capitals);
                foreach(var target in new[]{new Vector2(-6,-12),new Vector2(42,-12),new Vector2(42,48),new Vector2(-6,48)})
                {
                    var keep=pool.OrderBy(c=>Vector2.Distance(new Vector2(c.bounds.center.x,c.bounds.center.z),target)).First();pool.Remove(keep);
                    retainedInterior.Add(new Vector2(keep.bounds.center.x,keep.bounds.center.z));
                }
                foreach(var c in capitals)
                {
                    var center=c.bounds.center;var go=PrefabUtility.GetOutermostPrefabInstanceRoot(c.gameObject)??c.gameObject;
                    bool keep=retainedInterior.Any(q=>At(center,q));columnReport.Add((keep?"CONSERVAR Y ENSANCHAR ":"RETIRAR SOLO V2 ")+AnimationUtility.CalculateTransformPath(go.transform,context)+" centro="+center.ToString("F3"));
                    if(keep)Widen(go,1.6f);else Omit(go);
                }
                removedColumns+=capitals.Count-retainedInterior.Count;extendedColumns=retainedInterior.Count;
                int retiredPosts=0;
                var gallery=new[]{new Vector2(-15,-15),new Vector2(-15,51),new Vector2(51,51),new Vector2(51,-15),new Vector2(-15,39)};
                foreach(var piece in pieces.Where(p=>p.Key=="Post").ToArray())
                {
                    string group=piece.Go.transform.parent.name;var center=piece.Bounds.center;bool keep=true;
                    if(group=="05 - Prolongaciones de apoyos conservados")keep=retainedInterior.Any(q=>At(center,q));
                    else if(group=="05 - Apoyos de galerias Y-24")keep=gallery.Any(q=>At(center,q));
                    else if(group=="05 - Apoyos de galerias Y-30")keep=At(center,new Vector2(21,-3))||At(center,new Vector2(21,39));
                    if(!keep){Omit(piece.Go);pieces.Remove(piece);retiredPosts++;}
                    else if(group!="08 - Anclaje fijo a la cubierta"){Widen(piece.Go,1.6f);piece.Bounds=VisibleBounds(piece.Go);}
                }
                int before=pieces.Count;
                Shaft(51,39,-38,-26,"V2 - Pilar extremo oriental del puente Y-24");
                Shaft(45,45,-38,-32,"V2 - Base del apoyo del descanso conservado");
                foreach(var piece in pieces.Skip(before)){Widen(piece.Go,1.6f);piece.Bounds=VisibleBounds(piece.Go);}
                // Existing longitudinal roof beams remain; two transverse ties connect the four retained piers.
                BeamLine(retainedInterior[0],retainedInterior[1],-7.244f,"V2 - Vigas transversales de la cubierta");
                BeamLine(retainedInterior[3],retainedInterior[2],-7.244f,"V2 - Vigas transversales de la cubierta");
                foreach(var spec in new[]{new Vector3(-15,51,-26),new Vector3(-9,45,-32)})
                {
                    float min=spec.x,max=spec.y,top=spec.z;
                    BeamLine(new Vector2(min,min),new Vector2(max,min),top,"V2 - Vigas perimetrales de galerias");
                    BeamLine(new Vector2(max,min),new Vector2(max,max),top,"V2 - Vigas perimetrales de galerias");
                    BeamLine(new Vector2(max,max),new Vector2(min,max),top,"V2 - Vigas perimetrales de galerias");
                    if(Mathf.Abs(min+9)<.01f&&Mathf.Abs(top+32)<.01f)
                    {
                        // Approved correction only: the Y-30 west gallery has a stair opening at Z12..36.
                        // Keep both original end spans and frame the opening outside the six metre stair width.
                        BeamLine(new Vector2(min,max),new Vector2(min,36),top,"V2 - Vigas perimetrales de galerias");
                        BeamLine(new Vector2(min,12),new Vector2(min,min),top,"V2 - Vigas perimetrales de galerias");
                        foreach(float x in new[]{-12.55f,-5.45f})BeamLine(new Vector2(x,12),new Vector2(x,36),top,"V2 - Marco lateral del hueco de escalera");
                        foreach(float z in new[]{12f,36f})BeamLine(new Vector2(-12.55f,z),new Vector2(-5.45f,z),top,"V2 - Marco lateral del hueco de escalera");
                    }
                    else BeamLine(new Vector2(min,max),new Vector2(min,min),top,"V2 - Vigas perimetrales de galerias");
                }
                BeamLine(new Vector2(-15,39),new Vector2(51,39),-26,"V2 - Viga bajo puente Y-24");
                BeamLine(new Vector2(21,-9),new Vector2(21,45),-32,"V2 - Viga bajo puente Y-30");
                foreach(var corner in new[]{new Vector2(-15,-15),new Vector2(-15,51),new Vector2(51,51),new Vector2(51,-15)})
                {
                    var inner=corner+new Vector2(corner.x<18?6:-6,corner.y<18?6:-6);
                    BeamLine(corner,inner,-32,"V2 - Mensulas de transferencia al nivel Y-30");
                }
                if(FixedLayout()!=layout)throw new InvalidOperationException("Cambio no autorizado en estatua, pisos, puentes, escaleras, muros o arcos V1.");
                Inventory("V2 - CAMARA ABIERTA");
                columnReport.Add("Retirados respecto V1:13 capiteles interiores y "+retiredPosts+" modulos Post; agregados5 modulos para completar2 ejes necesarios. 4 pilares interiores principales: "+string.Join("; ",retainedInterior));
                columnReport.Add("La referencia V1 permanece como copia independiente inactiva en el mismo escenario temporal. Ninguna escena ni prefab original se guarda.");
                Directory.CreateDirectory("Temp/ROM_Reviews");File.WriteAllLines("Temp/ROM_Reviews/SolInvertido_V2_Columnas.txt",columnReport);
                Physics.SyncTransforms();ValidateGeometry();Original.Verify();
                Debug.Log("SOL_INVERTIDO_V2_LISTA: V1 conservada en memoria;13 capiteles y "+retiredPosts+" modulos Post retirados,5 Post agregados;4 pilares interiores principales retenidos="+string.Join("; ",retainedInterior)+"; Post activos="+pieces.Count(p=>p.Key=="Post")+". Estatua, profundidad, pisos, puentes, escaleras, muros y cuatro arcos originales de V1 sin cambios. Informe Temp/ROM_Reviews/SolInvertido_V2_Columnas.txt");
            }
            void BeamLine(Vector2 a,Vector2 b,float top,string group)
            {
                var d=b-a;float length=d.magnitude;var dir=d.normalized;float yaw=Mathf.Atan2(dir.x,dir.y)*Mathf.Rad2Deg;
                for(float offset=0;offset<length-.02f;offset+=6)
                {float segment=Mathf.Min(6,length-offset);var center=a+dir*(offset+segment/2);P(group,"Beam",center.x,center.y,top,yaw,true,new Vector3(1,1,segment/Assets["Beam"].Bounds.size.z));}
            }
            public void ShowReference(bool show)
            {
                if(JointScene.IsValid()){ViewingReference=false;ViewingSecond=false;context.gameObject.SetActive(true);generated.gameObject.SetActive(true);return;}
                if(!HasReference){ViewingReference=false;return;}
                ViewingReference=show;ViewingSecond=!show&&HasThird;context.gameObject.SetActive(!show);generated.gameObject.SetActive(!show&&!HasThird);referenceRoot.gameObject.SetActive(show);if(secondGenerated)secondGenerated.gameObject.SetActive(!show);
                if(show)
                {
                    foreach(Transform r in referenceContext)
                    {
                        if(!IsStage(r)){SceneVisibilityManager.instance.Hide(r.gameObject,true);continue;}
                        foreach(var renderer in r.GetComponentsInChildren<Renderer>())if(!Overlap(Outer,renderer.bounds))SceneVisibilityManager.instance.Hide(renderer.gameObject,false);
                    }
                }
                Physics.SyncTransforms();Debug.Log("SOL_INVERTIDO_COMPARACION: vista activa "+(show?"V1 referencia original":"V2 camara abierta"));
            }
            public void ShowThird()
            {
                if(JointScene.IsValid()){ViewingReference=false;ViewingSecond=false;context.gameObject.SetActive(true);generated.gameObject.SetActive(true);return;}
                if(!HasThird)return;ViewingReference=false;ViewingSecond=false;referenceRoot.gameObject.SetActive(false);secondGenerated.gameObject.SetActive(false);context.gameObject.SetActive(true);generated.gameObject.SetActive(true);Physics.SyncTransforms();Debug.Log("SOL_INVERTIDO_COMPARACION: vista activa V3 apoyos verificados");
            }
            sealed class ThirdPlacement
            {
                public string Key;public Vector3 Pivot,Scale;public Quaternion Rotation;public Bounds Bounds;
            }
            sealed class ThirdPlan
            {
                public Vector2 Target,Main;public string Label,Solution;public float Top;public List<ThirdPlacement> New=new List<ThirdPlacement>();public List<Piece> Retire=new List<Piece>();
            }
            Bounds NativeFoot(string key)
            {
                var a=Assets[key];var root=a.Prefab.transform;var m=Matrix4x4.TRS(Vector3.zero,root.localRotation,root.localScale)*root.worldToLocalMatrix;
                bool found=false;Bounds foot=default;
                foreach(var f in a.Prefab.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh&&f.GetComponent<Renderer>()?.enabled==true)
                foreach(var v in f.sharedMesh.vertices)
                {var p=(m*f.transform.localToWorldMatrix).MultiplyPoint3x4(v);if(p.y>a.Bounds.min.y+.12f)continue;if(!found){foot=new Bounds(p,Vector3.zero);found=true;}else foot.Encapsulate(p);}
                if(!found)throw new InvalidOperationException("No se pudo medir el pie del arco: "+key);return foot;
            }
            ThirdPlacement SpatialBeam(Vector3 a,Vector3 b)
            {
                var asset=Assets["Beam"];var rotation=Quaternion.FromToRotation(Vector3.forward,(b-a).normalized);var scale=new Vector3(1,1,Vector3.Distance(a,b)/asset.Bounds.size.z);
                var bounds=TransformBounds(asset.Bounds,Matrix4x4.TRS(Vector3.zero,rotation,scale));var pivot=(a+b)/2-bounds.center;bounds.center+=pivot;
                return new ThirdPlacement{Key="Beam",Pivot=pivot,Scale=scale,Rotation=rotation,Bounds=bounds};
            }
            void PlannedBeamLine(List<ThirdPlacement> list,Vector3 a,Vector3 b)
            {
                int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/6));for(int i=0;i<count;i++)list.Add(SpatialBeam(Vector3.Lerp(a,b,i/(float)count),Vector3.Lerp(a,b,(i+1)/(float)count)));
            }
            bool ClearThird(List<ThirdPlacement> list,out string reason)
            {
                foreach(var p in list)
                {
                    if(!Inside(Outer,p.Bounds,.03f)){reason="fuera de la huella84x84";return false;}
                    if(p.Bounds.Intersects(sunBounds)){reason="invade la reserva de la estatua";return false;}
                    foreach(var floor in pieces.Where(x=>x.Key=="Floor"&&x.Bounds.max.y> -37))
                    {
                        float y=floor.Bounds.max.y;var b=floor.Bounds;b.size=new Vector3(Mathf.Max(.1f,b.size.x-1.5f),2.1f,Mathf.Max(.1f,b.size.z-1.5f));b.center=new Vector3(b.center.x,y+1.2f,b.center.z);
                        if(p.Bounds.Intersects(b)){reason="invade la envolvente de circulacion del nivelY"+y.ToString("F0");return false;}
                    }
                    foreach(var w in walks)
                    {
                        int count=Mathf.CeilToInt(Vector3.Distance(w.A,w.B)/.6f);var side=Vector3.Cross(Vector3.up,w.B-w.A).normalized;
                        for(int i=0;i<=count;i++)foreach(float lane in new[]{-1.2f,0,1.2f})
                        {var foot=Vector3.Lerp(w.A,w.B,i/(float)count)+side*lane;var person=new Bounds(foot+Vector3.up*1.2f,new Vector3(.7f,1.8f,.7f));if(p.Bounds.Intersects(person)){reason="invade un recorrido existente";return false;}}
                    }
                }
                reason="envolventes libres";return true;
            }
            bool TryNativeArch(ThirdPlan plan,string key,List<ThirdPlacement> chords,out List<ThirdPlacement> result,out string reason)
            {
                result=null;var a=Assets[key];var foot=NativeFoot(key);var d=plan.Target-plan.Main;
                bool positive=Mathf.Abs(a.Bounds.max.z-foot.center.z)>=Mathf.Abs(a.Bounds.min.z-foot.center.z);
                float reach=positive?a.Bounds.max.z-foot.center.z:foot.center.z-a.Bounds.min.z;
                if(foot.size.x>2.4f||foot.size.z>2.4f){reason="pie nativo mayor que el apoyo existente: "+foot.size.ToString("F2");return false;}
                if(d.magnitude-reach>2){reason="alcance nativo"+reach.ToString("F2")+"m insuficiente para"+d.magnitude.ToString("F2")+"m sin prolongacion excesiva";return false;}
                var q=Quaternion.Euler(0,Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg+(positive?0:180),0);var bounds=TransformBounds(a.Bounds,Matrix4x4.Rotate(q));var worldFoot=q*foot.center;
                var pivot=new Vector3(plan.Main.x-worldFoot.x,plan.Top-Assets["Beam"].Bounds.size.y-bounds.max.y,plan.Main.y-worldFoot.z);bounds.center+=pivot;
                if(bounds.min.y< -37.9f){reason="el arco llegaria por debajo del piso profundo";return false;}
                var arc=new ThirdPlacement{Key=key,Pivot=pivot,Scale=Vector3.one,Rotation=q,Bounds=bounds};var trial=new List<ThirdPlacement>(chords){arc};
                if(!ClearThird(trial,out reason))return false;result=trial;return true;
            }
            public void MakeThirdVariant()
            {
                if(HasThird)return;if(!HasReference)MakeOpenVariant();ShowReference(false);Original.Verify();string unchanged=FixedLayout();
                var report=new List<string>{"V3 - comprobacion previa; V1/V2 no se modifican.","Bracket "+Assets["Bracket"].Bounds+" pie="+NativeFoot("Bracket"),"TallBracket "+Assets["TallBracket"].Bounds+" pie="+NativeFoot("TallBracket"),"Beam "+Assets["Beam"].Bounds};
                var targets=new[]{new Vector2(-15,-15),new Vector2(-15,51),new Vector2(51,51),new Vector2(51,-15),new Vector2(-15,39),new Vector2(51,39),new Vector2(45,-15),new Vector2(45,45)};
                foreach(var target in targets)
                {
                    bool upper=target==new Vector2(45,-15),middle=target==new Vector2(45,45);var main=retainedInterior.OrderBy(x=>Vector2.Distance(x,target)).First();
                    var plan=new ThirdPlan{Target=target,Main=main,Top=upper?-18:middle?-26:-26.78f,Label=target.ToString("F2")};
                    plan.Retire=pieces.Where(p=>p.Key=="Post"&&At(p.Bounds.center,target)&&(upper?p.Go.transform.parent.name=="05 - Apoyo del descanso superior":middle?p.Go.transform.parent.name=="05 - Apoyo del descanso intermedio"||p.Go.transform.parent.name=="V2 - Base del apoyo del descanso conservado":p.Go.transform.parent.name=="05 - Apoyos de galerias Y-24"||p.Go.transform.parent.name=="V2 - Pilar extremo oriental del puente Y-24")).ToList();
                    if(plan.Retire.Count==0)throw new InvalidOperationException("V2 no coincide: falta apoyo candidato "+target);
                    var d=(target-main).normalized;var normal=new Vector3(-d.y,0,d.x);var chord=new List<ThirdPlacement>();float height=Assets["Beam"].Bounds.size.y;
                    foreach(float offset in new[]{-.45f,0,.45f})PlannedBeamLine(chord,new Vector3(main.x,plan.Top-height/2,main.y)+normal*offset,new Vector3(target.x,plan.Top-height/2,target.y)+normal*offset);
                    bool accepted=false;
                    foreach(string key in new[]{"Bracket","TallBracket"})
                    {
                        if(TryNativeArch(plan,key,chord,out var trial,out var why)){plan.New=trial;plan.Solution=key+" nativo + vigas de reparto";accepted=true;break;}
                        report.Add(target+" "+key+" descartado en precomprobacion: "+why);
                    }
                    if(!accepted)
                    {
                        var trial=new List<ThirdPlacement>(chord);float anchor=Mathf.Max(-36,plan.Top-height-10);
                        foreach(float offset in new[]{-.45f,0,.45f})PlannedBeamLine(trial,new Vector3(main.x,anchor,main.y)+normal*offset,new Vector3(target.x,plan.Top-height,target.y)+normal*offset);
                        if(ClearThird(trial,out var why)){plan.New=trial;plan.Solution="vigas nativas en tres nervios diagonales y cordon superior, sin pilares nuevos";accepted=true;}
                        else report.Add(target+" vigas diagonales descartadas: "+why);
                    }
                    if(accepted){thirdAccepted.Add(plan);report.Add("VIABLE "+target+": retirar"+plan.Retire.Count+" Post; apoyo principal="+main+"; "+plan.Solution);}
                    else report.Add("PENDIENTE "+target+": conservar"+plan.Retire.Count+" Post para no interferir con niveles y rutas.");
                }
                Directory.CreateDirectory("Temp/ROM_Reviews");File.WriteAllLines("Temp/ROM_Reviews/SolInvertido_V3_Encaje.txt",report);Debug.Log("SOL_INVERTIDO_V3_PRECOMPROBACION:\n"+string.Join("\n",report));
                // Freeze the complete V2 before changing even one support in the working copy.
                if(!JointScene.IsValid())
                {
                var holder=new GameObject("V2 - Referencia conservada para comparar con V3");SceneManager.MoveGameObjectToScene(holder,WorkScene);holder.SetActive(false);
                secondGenerated=Object.Instantiate(generated.gameObject,holder.transform).transform;secondGenerated.name=generated.name;
                foreach(var p in pieces.Where(p=>p.Key=="Wall"&&(p.Bounds.center.x>59||p.Bounds.center.z< -23))){var t=secondGenerated.Find(AnimationUtility.CalculateTransformPath(p.Go.transform,generated));if(t)secondWalls.Add(t.gameObject);}
                // Keep the holder active; only its child is toggled when selecting V1/V2/V3.
                secondGenerated.gameObject.SetActive(false);holder.SetActive(true);
                }
                jointThirdReady=JointScene.IsValid();ShowThird();
                foreach(var plan in thirdAccepted)
                {
                    foreach(var old in plan.Retire){Omit(old.Go);pieces.Remove(old);}
                    foreach(var p in plan.New)
                    {
                        var a=Assets[p.Key];var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,WorkScene);go.transform.SetParent(Group("V3 - Transferencia "+plan.Label),true);go.transform.SetPositionAndRotation(p.Pivot,p.Rotation*a.Prefab.transform.localRotation);go.transform.localScale=Vector3.Scale(a.Prefab.transform.localScale,p.Scale);Quiet(go);go.isStatic=true;
                        pieces.Add(new Piece{Key=p.Key,Go=go,Bounds=p.Bounds});
                    }
                }
                CorrectRedundantCornerTies();
                if(FixedLayout()!=unchanged)throw new InvalidOperationException("V3 altero geometria protegida de V2.");
                Physics.SyncTransforms();ValidateGeometry();Original.Verify();Inventory("V3 - RESULTADO");
                Debug.Log("SOL_INVERTIDO_V3_LISTA: retirados"+thirdAccepted.Sum(p=>p.Retire.Count)+" modulos Post en"+thirdAccepted.Count+" ejes; Post conservados="+pieces.Count(p=>p.Key=="Post")+"; nuevos elementos="+thirdAccepted.Sum(p=>p.New.Count)+". V1 y V2 conservadas; estatua, niveles, escaleras, puentes y plataformas sin cambios. Pendientes="+string.Join("; ",targets.Where(t=>!thirdAccepted.Any(p=>p.Target==t)))+". No integrar.");
            }
            void ValidateThirdConnections()
            {
                bool Touch(Bounds a,Bounds b){a.Expand(.06f);return a.Intersects(b);}
                var surfaces=new Dictionary<GameObject,Surface>();
                Surface MeshOf(GameObject go){if(!surfaces.TryGetValue(go,out var s)){s=new Surface(go);surfaces.Add(go,s);}return s;}
                bool RealTouch(GameObject a,Bounds ba,GameObject b,Bounds bb)=>Touch(ba,bb)&&MeshTouch(MeshOf(a),MeshOf(b));
                var realAnchors=context.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.GetComponent<MeshFilter>()?.sharedMesh).Select(r=>(go:r.gameObject,b:r.bounds))
                    .Concat(pieces.Where(p=>p.Key=="Post").Select(p=>(go:p.Go,b:p.Bounds))).ToArray();
                var realReceivers=pieces.Where(p=>p.Key=="Floor"||p.Key=="Beam"&&!p.Go.transform.parent.name.StartsWith("V3 -",StringComparison.Ordinal)).ToArray();
                var anchors=context.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).Select(c=>c.bounds).Concat(pieces.Where(p=>p.Key=="Post").Select(p=>p.Bounds)).ToArray();
                var receivers=pieces.Where(p=>p.Key=="Floor"||p.Key=="Beam"&&!p.Go.transform.parent.name.StartsWith("V3 -",StringComparison.Ordinal)).Select(p=>p.Bounds).ToArray();
                foreach(var plan in thirdAccepted)
                {
                    var connected=new HashSet<ThirdPlacement>(plan.New.Where(p=>anchors.Any(a=>Touch(a,p.Bounds))));bool change;
                    do{change=false;foreach(var p in plan.New)if(!connected.Contains(p)&&connected.Any(q=>Touch(q.Bounds,p.Bounds))){connected.Add(p);change=true;}}while(change);
                    if(connected.Count!=plan.New.Count||!plan.New.Any(p=>receivers.Any(a=>Touch(a,p.Bounds))))throw new InvalidOperationException("V3 tiene un encuentro sin apoyo: "+plan.Target);
                    if(!ClearThird(plan.New,out var why))throw new InvalidOperationException("V3 invade circulacion: "+plan.Target+" / "+why);
                    var members=pieces.Where(p=>p.Go.transform.parent.name=="V3 - Transferencia "+plan.Label).ToArray();
                    var reached=new HashSet<Piece>(members.Where(p=>realAnchors.Any(a=>RealTouch(p.Go,p.Bounds,a.go,a.b))));
                    do{change=false;foreach(var p in members)if(!reached.Contains(p)&&reached.Any(q=>RealTouch(p.Go,p.Bounds,q.Go,q.Bounds))){reached.Add(p);change=true;}}while(change);
                    if(reached.Count!=members.Length||!members.Any(p=>realReceivers.Any(r=>RealTouch(p.Go,p.Bounds,r.Go,r.Bounds))))throw new InvalidOperationException("V3 encuentro de mallas sin continuidad real: "+plan.Target+" / "+reached.Count+" de "+members.Length);
                }
                Debug.Log("SOL_INVERTIDO_V3_ENCUENTROS_OK: "+thirdAccepted.Count+" conjuntos conectados a pilares existentes y receptores de galeria; comprobacion triangulo/triangulo de todas sus piezas, tolerancia6cm; sin miembros aislados. Revision visual adicional requerida.");
            }
            // Read-only triangle tests in world space: no collider approximations or mesh assets are created.
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
            void ValidateCornerMeshes()
            {
                foreach(var corner in new[]{new Vector2(-9,-9),new Vector2(-9,45),new Vector2(45,45),new Vector2(45,-9)})
                {
                    var floor=pieces.Single(p=>p.Key=="Floor"&&At(p.Bounds.center,corner)&&Mathf.Abs(p.Bounds.max.y+30)<.03f);
                    var beams=pieces.Where(p=>p.Key=="Beam"&&p.Go.transform.parent.name=="V2 - Vigas perimetrales de galerias"&&Mathf.Abs(p.Bounds.max.y+32)<.03f&&p.Bounds.SqrDistance(new Vector3(corner.x,-32.39f,corner.y))<.5f).ToArray();
                    if(beams.Length!=2)throw new InvalidOperationException("Esquina sin dos vigas perimetrales: "+corner+" / "+beams.Length);
                    var floorSurface=new Surface(floor.Go);var first=new Surface(beams[0].Go);var second=new Surface(beams[1].Go);
                    if(!MeshTouch(first,floorSurface)||!MeshTouch(second,floorSurface)||!MeshTouch(first,second))throw new InvalidOperationException("Encuentro real de mallas discontinuo en esquina: "+corner);
                    Debug.Log("SOL_INVERTIDO_V3_ESQUINA_MALLAS_OK: "+corner+"; ambas vigas contactan el piso Y-30 y entre si; prueba triangulo/triangulo, tolerancia6cm.");
                }
            }
            void CorrectRedundantCornerTies()
            {
                const string group="V2 - Mensulas de transferencia al nivel Y-30";
                var ties=pieces.Where(p=>p.Key=="Beam"&&p.Go.transform.parent.name==group).ToArray();
                if(ties.Length!=8)throw new InvalidOperationException("Se esperaban exactamente8 modulos en4 mensulas, hay "+ties.Length);
                // Each retired outer axis is already replaced in V3. These ties carry no separate floor tile:
                // the Y-30 corner tiles rest directly on the two retained perimeter beams, checked on mesh triangles.
                ValidateCornerMeshes();
                foreach(var corner in new[]{new Vector2(-15,-15),new Vector2(-15,51),new Vector2(51,51),new Vector2(51,-15)})
                    if(!thirdAccepted.Any(p=>p.Target==corner))throw new InvalidOperationException("No se sustituyo el eje exterior de la mensula: "+corner);
                foreach(var tie in ties){Omit(tie.Go);pieces.Remove(tie);}
                Debug.Log("SOL_INVERTIDO_V3_MENSULAS_CORREGIDAS: retiradas4 mensulas redundantes /8 Beam solamente en V3; esquinas sostenidas por vigas perimetrales existentes comprobadas con triangulos; ningun pilar ni recorrido cambiado;0 prefabs nuevos.");
            }
            void OpenCopies()
            {
                var columns=new List<Collider>();
                foreach(Transform stage in context)
                {
                    if(!IsStage(stage))continue;
                    foreach(var c in stage.GetComponentsInChildren<Collider>())
                    {
                        var b=c.bounds;if(!c.enabled||c.isTrigger||!Overlap(Outer,b))continue;
                        if(c.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)&&Mathf.Abs(b.max.y+16)<.025f&&Inside(Opening,b))Omit(PrefabUtility.GetOutermostPrefabInstanceRoot(c.gameObject)??c.gameObject);
                        else if(c.name.StartsWith("Column_Ceiling_02",StringComparison.Ordinal)&&b.min.y<-8.1f&&b.max.y>-15.9f)columns.Add(c);
                    }
                }
                if(omitted.Count!=144)throw new InvalidOperationException("La copia no abrio exactamente 144 modulos: "+omitted.Count);
                // A 36x36 central void frees the sculpture. Existing supports elsewhere remain and extend downward.
                foreach(var c in columns)
                {
                    var b=c.bounds;var center=b.center;
                    if(center.x>0&&center.x<36&&center.z>-1&&center.z<37){Omit(PrefabUtility.GetOutermostPrefabInstanceRoot(c.gameObject)??c.gameObject);removedColumns++;}
                    else if(Inside(Opening,new Bounds(new Vector3(center.x,0,center.z),Vector3.one*.1f)))
                    {Shaft(center.x,center.z,-38,b.min.y,"05 - Prolongaciones de apoyos conservados");extendedColumns++;}
                }
                foreach(Transform r in context)
                {
                    if(!IsStage(r)){SceneVisibilityManager.instance.Hide(r.gameObject,true);continue;}
                    foreach(var renderer in r.GetComponentsInChildren<Renderer>())
                    {
                        if(!Overlap(Outer,renderer.bounds))SceneVisibilityManager.instance.Hide(renderer.gameObject,false);
                        else if(renderer.bounds.min.y>-9f)roofViews.Add(renderer.gameObject);
                    }
                }
                Debug.Log("SOL_INVERTIDO_COPIAS: desactivar144 pisos, retirar"+removedColumns+" capiteles centrales solamente en copia; prolongar"+extendedColumns+" apoyos. 52 pisos perimetrales conservados.");
            }
            Transform Group(string name)
            {
                if(!groups.TryGetValue(name,out var t)){var go=new GameObject(name);go.transform.SetParent(generated,false);groups[name]=t=go.transform;}return t;
            }
            GameObject P(string group,string key,float x,float z,float y,float yaw=0,bool top=true,Vector3? scale=null)
            {
                var a=Assets[key];var q=Quaternion.Euler(0,yaw,0);var sc=scale??Vector3.one;
                var b=TransformBounds(a.Bounds,Matrix4x4.TRS(Vector3.zero,q,sc));var pivot=new Vector3(x-b.center.x,y-(top?b.max.y:b.min.y),z-b.center.z);b.center+=pivot;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,WorkScene);go.transform.SetParent(Group(group),true);
                go.transform.SetPositionAndRotation(pivot,q*a.Prefab.transform.localRotation);go.transform.localScale=Vector3.Scale(a.Prefab.transform.localScale,sc);Quiet(go);go.isStatic=true;
                if(key!="Sun"&&!go.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))throw new InvalidOperationException("Falta collider nativo: "+key);
                pieces.Add(new Piece{Key=key,Go=go,Bounds=b});return go;
            }
            void Floor(string group,float x,float z,float y)=>P(group,"Floor",x,z,y);
            void Walk(Vector3 a,Vector3 b,bool stair=false)=>walks.Add(new Walk{A=a,B=b,Stair=stair});
            void Shaft(float x,float z,float bottom,float top,string group)
            {
                float h=Assets["Post"].Bounds.size.y;
                for(float y=bottom;y<top-.02f;y+=h)P(group,"Post",x,z,y,0,false,new Vector3(1,Mathf.Min(h,top-y)/h,1));
            }
            bool StairBay(float x,float z,int level)=>level==1&&x==51&&z>=-9&&z<=9||level==2&&x==-9&&z>=15&&z<=33;
            void Ring(float min,float max,float y,int level)
            {
                for(float x=min+3;x<max;x+=6)for(float z=min+3;z<max;z+=6)
                {
                    if(x!=min+3&&x!=max-3&&z!=min+3&&z!=max-3)continue;
                    // Ring at Y-24 stays below the incoming stairs; omit only the Y-30 outgoing stair opening.
                    if(level==2&&StairBay(x,z,level))continue;
                    Floor("02 - Galerias Y"+y,x,z,y);
                }
            }
            void Flight(Vector3 start,Vector3 direction,int count,float drop)
            {
                var p=start;float yaw=direction.z>0?270:direction.z<0?90:direction.x<0?180:0;
                for(int i=0;i<count;i++)
                {
                    var next=p+direction*6+Vector3.down*drop;
                    P("04 - Escaleras de enlace","Stair",(p.x+next.x)/2,(p.z+next.z)/2,next.y,yaw,false,new Vector3(1,drop/4,1));
                    Walk(p,next,true);p=next;
                }
            }
            void Build()
            {
                for(float x=-21;x<60;x+=6)for(float z=-21;z<60;z+=6)Floor("01 - Piso profundo Y-38",x,z,-38);
                Ring(-18,54,-24,1);Ring(-12,48,-30,2);
                // Bridges cross in projection at (21,39), with a six metre level separation.
                for(float x=-9;x<=45;x+=6)Floor("03 - Puente este oeste Y-24",x,39,-24);
                for(float z=-3;z<=39;z+=6)Floor("03 - Puente norte sur Y-30",21,z,-30);
                Walk(new Vector3(-15,-24,39),new Vector3(51,-24,39));
                Walk(new Vector3(21,-30,-9),new Vector3(21,-30,45));
                Floor("04 - Descanso superior",51,-15,-16);
                Shaft(51,-15,-26,-18,"05 - Apoyo del descanso superior");
                Floor("04 - Descanso superior",45,-15,-16);
                Shaft(45,-15,-38,-18,"05 - Apoyo del descanso superior");
                Walk(new Vector3(57,-16,-15),new Vector3(45,-16,-15));Walk(new Vector3(45,-16,-15),new Vector3(45,-16,-12));
                Flight(new Vector3(45,-16,-12),Vector3.forward,4,2);
                Floor("04 - Descanso intermedio Y-24",45,15,-24);
                Walk(new Vector3(45,-24,12),new Vector3(45,-24,15));Walk(new Vector3(45,-24,15),new Vector3(51,-24,15));
                Walk(new Vector3(51,-24,15),new Vector3(51,-24,45));
                Floor("04 - Descanso intermedio Y-24",45,45,-24);
                Shaft(45,45,-32,-26,"05 - Apoyo del descanso intermedio");
                Walk(new Vector3(51,-24,45),new Vector3(45,-24,45));Walk(new Vector3(45,-24,45),new Vector3(42,-24,45));
                Flight(new Vector3(42,-24,45),Vector3.left,3,2);
                Walk(new Vector3(24,-30,45),new Vector3(-9,-30,45));Walk(new Vector3(-9,-30,45),new Vector3(-9,-30,36));
                Flight(new Vector3(-9,-30,36),Vector3.back,4,2);
                Walk(new Vector3(-9,-38,12),new Vector3(-9,-38,9));
                Walk(new Vector3(-9,-38,9),new Vector3(18,-38,9));
                foreach(var point in new[]{new Vector2(-15,-15),new Vector2(-15,51),new Vector2(51,51),new Vector2(51,-15),new Vector2(-15,15),new Vector2(-15,39),new Vector2(51,27),new Vector2(15,51),new Vector2(39,51),new Vector2(15,-15),new Vector2(39,-15)})
                    Shaft(point.x,point.y,-38,-26,"05 - Apoyos de galerias Y-24");
                foreach(var point in new[]{new Vector2(-9,-9),new Vector2(45,-9),new Vector2(45,45),new Vector2(-9,45),new Vector2(21,-3),new Vector2(21,39)})
                    Shaft(point.x,point.y,-38,-32,"05 - Apoyos de galerias Y-30");
                // Four transfer ribs frame the sculpture without crossing its envelope.
                foreach(float z in new[]{-20f,56f})
                {
                    P("06 - Arcos de transferencia provisionales","Arch",18,z,-8.024f,90,true,new Vector3(1,1,4));
                    foreach(float x in new[]{-2.175f,38.175f})Shaft(x,z,-38,-16.034f,"06 - Pilares de los arcos");
                }
                foreach(float x in new[]{-20f,56f})
                {
                    P("06 - Arcos de transferencia provisionales","Arch",x,18,-8.024f,0,true,new Vector3(1,1,4));
                    foreach(float z in new[]{-2.175f,38.175f})Shaft(x,z,-38,-16.034f,"06 - Pilares de los arcos");
                }
                // Excavation enclosure terminates at the underside of the retained upper gallery.
                for(float v=-21;v<60;v+=6)foreach(float side in new[]{-24f,60f})
                foreach(float bottom in new[]{-38f,-30f,-22f})
                {
                    float h=Mathf.Min(8,-18-bottom);
                    float inset=side<0?side+.4f:side-.4f;
                    P("07 - Contencion de la excavacion","Wall",inset,v,bottom,0,false,new Vector3(1,h/8,1));
                    P("07 - Contencion de la excavacion","Wall",v,inset,bottom,90,false,new Vector3(1,h/8,1));
                }
                CreateSun();AddRails();
            }
            void CreateSun()
            {
                var a=Assets["Sun"];var q=Quaternion.Euler(180,0,0);var b=TransformBounds(a.Bounds,Matrix4x4.Rotate(q));
                var pivot=new Vector3(18-b.center.x,-8.3f-b.max.y,18-b.center.z);b.center+=pivot;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,WorkScene);go.name="SolInvertido - corona original inmovil";go.transform.SetParent(Group("08 - El Sol Invertido"),true);
                go.transform.SetPositionAndRotation(pivot,q*a.Prefab.transform.localRotation);go.transform.localScale=a.Prefab.transform.localScale;Quiet(go);go.isStatic=true;
                foreach(var rb in go.GetComponentsInChildren<Rigidbody>()){rb.isKinematic=true;rb.useGravity=false;}
                sunBounds=b;pieces.Add(new Piece{Key="Sun",Go=go,Bounds=b});
                Shaft(18,18,-8.3f,-7.24f,"08 - Anclaje fijo a la cubierta");
                Debug.Log("SOL_INVERTIDO_CORONA: Renderer previsto="+b+"; escala original, rotacion fija180; sin animacion, VFX ni luces.");
            }
            void AddRails()
            {
                void Rail(string group,float x,float z,float y,float yaw=0)=>P(group,"Rail",x,z,y+1.25f,yaw,true);
                // Inner edges only; stair mouths and bridge junctions stay open.
                for(float v=-21;v<60;v+=6)
                {
                    if(v!= -15){Rail("09 - Pretiles superiores",-17.55f,v,-16);Rail("09 - Pretiles superiores",53.55f,v,-16);}
                    Rail("09 - Pretiles superiores",v,-17.55f,-16,90);Rail("09 - Pretiles superiores",v,53.55f,-16,90);
                }
                for(float x=-9;x<=45;x+=6)foreach(float z in new[]{36.45f,41.55f})Rail("09 - Pretiles de puentes",x,z,-24,90);
                for(float z=-3;z<=39;z+=6)foreach(float x in new[]{18.45f,23.55f})Rail("09 - Pretiles de puentes",x,z,-30);
            }
            void CreateViews()
            {
                var positions=new[]{new Vector3(57,-14.2f,15),new Vector3(39,-22.2f,39),new Vector3(21,-28.2f,-3),new Vector3(3,-36.2f,-6),new Vector3(84,2,-78)};
                var targets=new[]{new Vector3(18,-15,18),new Vector3(18,-16,18),new Vector3(18,-22,18),new Vector3(18,-25,18),new Vector3(18,-23,18)};
                for(int i=0;i<5;i++){var go=new GameObject("Vista "+(i+1));go.transform.SetParent(Group("10 - Vistas de revision"),false);go.transform.position=positions[i];go.transform.LookAt(targets[i]);var c=go.AddComponent<Camera>();c.enabled=false;c.nearClipPlane=.1f;c.farClipPlane=1200;c.fieldOfView=60;Views[i]=c;}
            }
            public void ShowSection(bool section)
            {
                if(ViewingReference)
                {
                    foreach(var go in referenceRoof)if(section)SceneVisibilityManager.instance.Hide(go,false);else SceneVisibilityManager.instance.Show(go,false);
                    foreach(var go in referenceWalls)if(section)SceneVisibilityManager.instance.Hide(go,true);else SceneVisibilityManager.instance.Show(go,true);
                    return;
                }
                foreach(var go in roofViews)if(go){if(section)SceneVisibilityManager.instance.Hide(go,false);else SceneVisibilityManager.instance.Show(go,false);}
                if(ViewingSecond){foreach(var go in secondWalls)if(section)SceneVisibilityManager.instance.Hide(go,true);else SceneVisibilityManager.instance.Show(go,true);return;}
                foreach(var p in pieces.Where(p=>p.Key=="Wall"))if(section&&(p.Bounds.center.x>59||p.Bounds.center.z< -23))SceneVisibilityManager.instance.Hide(p.Go,true);else SceneVisibilityManager.instance.Show(p.Go,true);
            }
            public void ValidateGeometry()
            {
                Physics.SyncTransforms();var all=context.GetComponentsInChildren<Collider>().Concat(generated.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy).ToArray();
                var lookup=all.Select(c=>(c,b:c.bounds)).Where(t=>Overlap(Outer,t.b)&&t.b.min.y< -7).ToArray();
                var allowed=new HashSet<Collider>(all);var capsuleHits=new Collider[256];var physics=WorkScene.GetPhysicsScene();
                walkSamples=0;minHeadroom=float.PositiveInfinity;
                foreach(var w in walks)
                {
                    var direction=w.B-w.A;var side=Vector3.Cross(Vector3.up,direction).normalized;int steps=Mathf.CeilToInt(direction.magnitude/.45f);
                    for(int i=0;i<=steps;i++)foreach(float lane in new[]{-1.2f,-.4f,.4f,1.2f})
                    {
                        var p=Vector3.Lerp(w.A,w.B,i/(float)steps)+side*lane;
                        var ray=new Ray(p+Vector3.up*1.15f,Vector3.down);var candidates=lookup.Where(t=>t.b.min.x<=p.x&&t.b.max.x>=p.x&&t.b.min.z<=p.z&&t.b.max.z>=p.z).ToArray();
                        var hits=new List<RaycastHit>();foreach(var t in candidates)if(t.c.Raycast(ray,out var hit,2.4f)&&hit.normal.y>.5f)hits.Add(hit);
                        if(hits.Count==0)throw new InvalidOperationException("Recorrido sin suelo: "+p);
                        var ground=hits.OrderBy(h=>Mathf.Abs(h.point.y-p.y)).First();if(Mathf.Abs(ground.point.y-p.y)>(w.Stair?.55f:.08f))throw new InvalidOperationException("Desnivel inesperado: "+p+" suelo="+ground.point);
                        var foot=ground.point;
                        int overlaps=physics.OverlapCapsule(foot+Vector3.up*.65f,foot+Vector3.up*1.75f,.3f,capsuleHits,~0,QueryTriggerInteraction.Ignore);
                        if(overlaps==capsuleHits.Length)throw new InvalidOperationException("Consulta de colisiones saturada.");
                        for(int j=0;j<overlaps;j++)if(allowed.Contains(capsuleHits[j]))throw new InvalidOperationException("Capsula bloqueada: "+foot+" / "+capsuleHits[j].name);
                        // Head/torso probes at capsule centre and four offsets; native stair convex slopes are retained.
                        foreach(var offset in new[]{Vector3.zero,Vector3.right*.3f,Vector3.left*.3f,Vector3.forward*.3f,Vector3.back*.3f})
                        {
                            var headRay=new Ray(foot+offset+Vector3.up*.65f,Vector3.up);
                            foreach(var t in lookup.Where(t=>t.b.min.x<=headRay.origin.x&&t.b.max.x>=headRay.origin.x&&t.b.min.z<=headRay.origin.z&&t.b.max.z>=headRay.origin.z))
                                if(t.c.Raycast(headRay,out var h,50))
                                {
                                    float clear=h.point.y-foot.y;minHeadroom=Mathf.Min(minHeadroom,clear);
                                    if(clear<2.1f)throw new InvalidOperationException("Altura libre insuficiente: "+foot+" / "+t.c.name+" altura="+clear);
                                }
                        }
                        walkSamples++;
                    }
                }
                var floors=pieces.Where(p=>p.Key=="Floor"&&Mathf.Abs(p.Bounds.max.y+38)<.01f).ToArray();if(floors.Length!=196)throw new InvalidOperationException("Piso profundo incompleto.");
                var duplicate=pieces.GroupBy(p=>p.Key+"|"+p.Go.transform.position.ToString("F3")+"|"+p.Go.transform.rotation.ToString("F3")+"|"+p.Go.transform.localScale.ToString("F3")).FirstOrDefault(g=>g.Count()>1);
                if(duplicate!=null)throw new InvalidOperationException("Piezas duplicadas: "+duplicate.Key);
                if(pieces.Count(p=>p.Key=="Stair")!=11)throw new InvalidOperationException("Escaleras incompletas.");
                foreach(var p in pieces.Where(p=>p.Key!="Sun"))if(!p.Go.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))throw new InvalidOperationException("Sin collider: "+p.Go.name);
                foreach(var p in pieces)if(!Inside(Outer,p.Bounds,.03f))throw new InvalidOperationException("Geometria fuera de la huella maxima84x84: "+p.Key+" "+p.Bounds);
                if(sunBounds.min.y< -23||sunBounds.max.y> -8)throw new InvalidOperationException("Corona fuera de su reserva vertical.");
                if(HasReference)ValidateStairBeamFrame();
                if(HasThird){ValidateThirdConnections();ValidateCornerMeshes();}
                int triangles=0;foreach(var f in generated.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh)for(int sub=0;sub<f.sharedMesh.subMeshCount;sub++)triangles+=(int)f.sharedMesh.GetIndexCount(sub)/3;
                Debug.Log("SOL_INVERTIDO_VALIDACION_OK: huella84x84, abertura72x72, profundidad22m, altura bajo vigas29.98m; niveles-16/-24/-30/-38; puente6m, escalera11 tramos 6m x2m, pendiente18.43 grados; piso196; desactivar144 pisos +"+removedColumns+" columnas solo copia; conservar52 pisos perimetrales; nuevos prefabs="+pieces.Count+"; renderers="+generated.GetComponentsInChildren<MeshRenderer>().Length+"; colliders="+generated.GetComponentsInChildren<Collider>().Length+"; triangulos="+triangles+"; muestras4carriles="+walkSamples+"; altura libre minima recorridos="+minHeadroom.ToString("F2")+". Desglose="+string.Join("; ",pieces.GroupBy(p=>p.Key).Select(g=>g.Key+":"+g.Count()))+". Arcos de transferencia son propuesta visual: escala longitudinal4, revision estructural artistica pendiente. Sin guardar ni integrar.");
            }
            void ValidateStairBeamFrame()
            {
                const string frame="V2 - Marco lateral del hueco de escalera";
                var members=pieces.Where(p=>p.Key=="Beam"&&p.Go.transform.parent.name==frame).ToArray();
                if(members.Length!=12)throw new InvalidOperationException("Marco de escalera incompleto: "+members.Length);
                // End headers sit at Z12/36; reserve the clear passage between their inner faces.
                var forbidden=new Bounds(new Vector3(-9,-32.39f,24),new Vector3(6,.78f,23f));
                foreach(var piece in pieces.Where(p=>p.Key=="Beam"&&Mathf.Abs(p.Bounds.max.y+32)<.03f))
                    if(piece.Bounds.Intersects(forbidden))throw new InvalidOperationException("Viga invade el hueco reservado: "+piece.Bounds);
                bool Touch(Bounds a,Bounds b){a.Expand(.04f);return a.Intersects(b);}
                var anchors=pieces.Where(p=>p.Key=="Beam"&&p.Go.transform.parent.name=="V2 - Vigas perimetrales de galerias"&&Mathf.Abs(p.Bounds.center.x+9)<.03f&&Mathf.Abs(p.Bounds.max.y+32)<.03f).ToArray();
                var reached=new HashSet<Piece>(members.Where(p=>anchors.Any(a=>Touch(p.Bounds,a.Bounds))));
                bool changed;do{changed=false;foreach(var p in members)if(!reached.Contains(p)&&reached.Any(q=>Touch(p.Bounds,q.Bounds))){reached.Add(p);changed=true;}}while(changed);
                if(reached.Count!=members.Length)throw new InvalidOperationException("Marco con vigas sin conexion a los extremos de la galeria.");
                if(!members.Any(p=>p.Bounds.min.z<12&&p.Bounds.max.z>12)||!members.Any(p=>p.Bounds.min.z<36&&p.Bounds.max.z>36))throw new InvalidOperationException("Falta encuentro del marco con un descanso.");
                Debug.Log("SOL_INVERTIDO_V2_MARCO_ESCALERA_OK: interrupcion X-9 Z12..36;12 modulos de viga forman dos laterales y dos cabeceros, conectados a ambos tramos conservados. Ancho transitable6m libre entre los cabeceros de los extremos. Encuentros comprobados por bounds; revision visual adicional requerida.");
            }
            protected override void OnCloseStage()
            {
                try{Original?.Verify();}finally{base.OnCloseStage();foreach(var mesh in meshes)if(mesh)Object.DestroyImmediate(mesh);}
            }
        }
    }
}
