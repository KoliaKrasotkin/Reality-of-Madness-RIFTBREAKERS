using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    /// <summary>
    /// Additive architectural pass over the existing zone 02. Never calls the structure generator.
    /// All execution is manual, all source assets are read-only, and scenes are never saved here.
    /// </summary>
    public static class JardinesDoradosArchitectureGenerator
    {
        const string Target = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string BaseRoot = "Dungeon_Ciudad_del_Sol_Generated";
        const string BaseSignature = "__ROM_SolarStructure_12D9D67A_v1";
        const string GardenName = "02 - Jardines Dorados";
        const string StageName = "JardinesDorados_Architecture";
        const string StageSignature = "__ROM_GoldenGardens_46F0C18E_v1";
        const string PreviewName = "TEMP_JardinesDorados_Architecture";
        const string PreviewSignature = "__ROM_GardensPreview_46F0C18E_v1";
        const string ViewTitle = "Jardines Dorados - Arquitectura";
        const string Menu = "ROM/Dungeon Ciudad del Sol/Jardines Dorados/";
        const string ET = "Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string ST = "Assets/Standard Assets/Sun_Temple/Prefabs/";
        const string EG = "Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/";
        const string GoldPath = "Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/Materials/Roof_Gold.mat";
        static readonly Vector3 Center = new Vector3(-102,4,-18);
        static readonly string[] RoutePrefixes = { "01-02 -", "02-03 -" };

        // Actual resources inspected in the three approved packs. No procedural stand-ins.
        const string ResourceNote = "No se encontro una fuente completa ni una palmera vegetal identificable. " +
            "El estanque usa bordes Curb_01 y el prefab original Dec_Puddle_A como lamina visual estatica; " +
            "no simula agua. Se usan arboles y arbustos originales; SM_PillarPalm es una columna, no vegetacion.";
        static readonly Dictionary<string,string> Paths = new Dictionary<string,string>
        {
            { "Base", ET + "Pits and Platforms/Platform_Base_01.prefab" },
            { "Stair", ET + "Staircases/Staircase_01.prefab" },
            { "StairRail", ET + "Railings and Decors/Railing_Staircase_01.prefab" },
            { "Rail", ET + "Railings and Decors/Railing_01.prefab" },
            { "Curb", ET + "Railings and Decors/Curb_01.prefab" },
            { "Fascia", ET + "Railings and Decors/Bottom_Decor_01.prefab" },
            { "BrokenFascia", ET + "Railings and Decors/Bottom_Decor_Broken_01.prefab" },
            { "Buttress", ET + "Bases/Base_Arch_Tall_01.prefab" },
            { "PlatformSupport", ET + "Bases/Base_Arch_01.prefab" },
            { "Arch", ET + "Bases/Arch_Wide_01.prefab" },
            { "BrokenArch", ET + "Bases/Thin/Arch_03_Broken_01.prefab" },
            { "BrokenWall", ET + "Damaged/Broken_Wall_01.prefab" },
            { "BrokenColumn", ET + "Damaged/Column_Broken_01.prefab" },
            { "Column", ET + "Columns/Column_01.prefab" },
            { "TreeA", ET + "Flora/Tree_01.prefab" },
            { "TreeB", ET + "Flora/Tree_02.prefab" },
            { "Shrub", ET + "Flora/Bush_01.prefab" },
            { "Ivy", ET + "Flora/Ivy_01.prefab" },
            { "TreeTall", ST + "Natural/Fol_TallTree_A.prefab" },
            { "Flowers", ST + "Natural/Fol_BushFlower_A.prefab" },
            { "FlowerGroup", ST + "Natural/Fol_BushFlower_D_Grouped.prefab" },
            { "Planter", ST + "Props/Prop_FlowerPot_D.prefab" },
            { "Bench", ST + "Props/Prop_StoneBench_A.prefab" },
            { "Water", ST + "Decals/Dec_Puddle_A.prefab" },
            { "Canopy", EG + "SM_Canopy_Small.prefab" },
            { "Statue", EG + "SM_LadyStatue_Low.prefab" }
        };
        static readonly HashSet<string> ColliderOptional = new HashSet<string>
            { "TreeTall", "Shrub", "Flowers", "FlowerGroup" };

        sealed class Prototype
        {
            public GameObject Prefab;
            public Bounds Render, Solid;
            public bool HasSolid;
        }

        sealed class Source
        {
            public Transform Root, Garden;
            public readonly List<Transform> Routes = new List<Transform>();
            public readonly List<Bounds> RouteProtection = new List<Bounds>();
            public readonly List<Bounds> Floor = new List<Bounds>();
            public readonly List<Bounds> ExistingProtection = new List<Bounds>();
        }

        sealed class Piece
        {
            public string Key, Group;
            public Vector3 Pivot;
            public float Yaw;
            public bool VisualOnly, Gold;
            public Bounds Render, Solid;
        }

        // Actual arrival: (-114,4,-66). Actual departure: (-54,4,12).
        // The existing western gallery and its stair are also kept clear.
        // Routes run around the pool; no main path crosses the ornamental water.
        static Bounds Corridor(float x0,float z0,float x1,float z1)
        {
            return new Bounds(Center+new Vector3((x0+x1)*.5f,2.5f,(z0+z1)*.5f),
                new Vector3(Mathf.Abs(x1-x0)+14,5,Mathf.Abs(z1-z0)+14));
        }
        static readonly Bounds[] Reservations =
        {
            Corridor(-12,-48,-12,-24), Corridor(-18,-24,24,-24),
            Corridor(24,-24,24,30), Corridor(24,30,48,30),
            Corridor(-18,-24,-18,6), Corridor(-18,21,24,21),
            // Ground-level landing in front of the NEW pavilion stair; the ramp begins south of it.
            new Bounds(Center+new Vector3(39,2.5f,17.3f),new Vector3(13,5,9.4f))
        };

        [MenuItem(Menu + "1. Previsualizar sobre una copia temporal")]
        public static void Preview()
        {
            if (!CanRun()) return;
            GameObject staging = null;
            try
            {
                Source source = InspectSource(WorkingScene());
                var assets = LoadAssets();
                var plan = new Plan(source,assets);
                plan.Compose();
                if (StageUtility.GetCurrentStage() is ArchitecturePreviewStage)
                {
                    var old=StageRoots(source.Garden).Select(t=>t.gameObject).ToArray();
                    staging=InstantiatePlan(source.Garden,assets,plan,source.Garden.gameObject.scene);
                    AddPreviewRig(staging.transform,source.Garden.gameObject.scene);
                    Commit(staging,old,"Regenerar previsualizacion de Jardines Dorados");
                    Selection.activeGameObject=staging;
                    staging=null;
                    FramePreview();
                }
                else
                {
                    var preview=ScriptableObject.CreateInstance<ArchitecturePreviewStage>();
                    preview.Configure(source,assets,plan);
                    StageUtility.GoToStage(preview,true);
                }
            }
            catch (Exception e)
            {
                if (staging) Object.DestroyImmediate(staging);
                Report(e);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem(Menu + "2. Generar o regenerar solo esta etapa")]
        public static void Generate()
        {
            if (!CanRun()) return;
            if (StageUtility.GetCurrentStage() != StageUtility.GetMainStage() || SceneManager.GetActiveScene().path != Target)
            {
                Debug.LogWarning("Activa Dungeon_Isla_del_Sol para generar. Usa la opcion 1 para previsualizar sin modificarla.");
                return;
            }
            GameObject staging = null;
            try
            {
                Source source = InspectSource(WorkingScene());
                var assets = LoadAssets();
                var plan = new Plan(source,assets);
                plan.Compose();
                var old = StageRoots(source.Garden).Select(t => t.gameObject).ToArray();
                staging = InstantiatePlan(source.Garden,assets,plan,source.Garden.gameObject.scene);
                Commit(staging,old,"Generar arquitectura de la Jardines Dorados");
                Selection.activeGameObject = staging;
                staging = null;
                EditorSceneManager.MarkSceneDirty(source.Garden.gameObject.scene);
                Debug.Log(plan.Description + " No se guardo la escena ni se modifico la estructura existente.");
            }
            catch (Exception e) { if (staging) Object.DestroyImmediate(staging); Report(e); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem(Menu + "3. Eliminar solo JardinesDorados_Architecture")]
        public static void Remove()
        {
            if (!CanRun()) return;
            try
            {
                Source source = InspectSource(WorkingScene(),false);
                var stages = StageRoots(source.Garden);
                if (stages.Count == 0) return;
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Eliminar arquitectura de Jardines Dorados");
                foreach (var stage in stages) Undo.DestroyObjectImmediate(stage.gameObject);
                Undo.CollapseUndoOperations(group);
                EditorSceneManager.MarkSceneDirty(source.Garden.gameObject.scene);
            }
            catch (Exception e) { Report(e); }
        }

        [MenuItem(Menu + "4. Validar arquitectura y accesos (solo lectura)")]
        public static void Validate()
        {
            try
            {
                var plan = new Plan(InspectSource(WorkingScene()),LoadAssets());
                plan.Compose();
                Debug.Log(plan.Description + " Validacion de assets y envolventes; no se crearon objetos.");
            }
            catch (Exception e) { Report(e); }
        }

        [MenuItem(Menu + "5. Cerrar previsualizacion sin guardar")]
        public static void ClosePreview()
        {
            if (!CanRun()) return;
            if (StageUtility.GetCurrentStage() is ArchitecturePreviewStage) StageUtility.GoToMainStage();
        }

        static Scene WorkingScene()
        {
            var stage=StageUtility.GetCurrentStage();
            if (stage is ArchitecturePreviewStage preview) return preview.scene;
            if (stage != StageUtility.GetMainStage())
                throw new InvalidOperationException("Sal del modo de edicion de prefabs antes de usar esta etapa.");
            return SceneManager.GetActiveScene();
        }

        // Unity's public Stage API isolates rendering and hierarchy without hiding, unloading,
        // rebuilding or changing any object in the user's main scenes.
        sealed class ArchitecturePreviewStage : PreviewSceneStage
        {
            Source source;
            Dictionary<string,Prototype> assets;
            Plan plan;

            public void Configure(Source source,Dictionary<string,Prototype> assets,Plan plan)
            { this.source=source; this.assets=assets; this.plan=plan; }

            protected override GUIContent CreateHeaderContent() => new GUIContent(ViewTitle);

            protected override bool OnOpenStage()
            {
                // No generation on domain reload: configuration only exists during a manual menu invocation.
                if (source==null || assets==null || plan==null || !source.Garden) return false;
                if (!base.OnOpenStage()) return false;
                GameObject staging=null;
                try
                {
                    Scene previewScene=scene;
                    previewScene.name=PreviewName;
                    staging=new GameObject(BaseRoot+"__PreviewBuilding");
                    SceneManager.MoveGameObjectToScene(staging,scene);
                    Marker(staging.transform,BaseSignature);
                    Marker(staging.transform,PreviewSignature);
                    Transform jardin=Copy(source.Garden,staging.transform);
                    foreach (var old in StageRoots(jardin)) Object.DestroyImmediate(old.gameObject);
                    foreach (var route in source.Routes) Copy(route,staging.transform);
                    var architecture=InstantiatePlan(jardin,assets,plan,scene);
                    architecture.name=StageName;
                    AddPreviewRig(architecture.transform,scene);
                    Commit(staging,Array.Empty<GameObject>(),"Previsualizar arquitectura de Jardines Dorados");
                    Selection.activeGameObject=architecture;
                    staging=null;
                    FramePreview();
                    Debug.Log(plan.Description+" La base y sus puentes son copias; la escena original sigue intacta.");
                    source=null; assets=null; plan=null;
                    return true;
                }
                catch (Exception e)
                {
                    if (staging) Object.DestroyImmediate(staging);
                    Report(e);
                    base.OnCloseStage();
                    return false;
                }
                finally { EditorUtility.ClearProgressBar(); }
            }

            protected override void OnCloseStage()
            {
                EditorUtility.ClearProgressBar();
                base.OnCloseStage();
            }
        }

        static bool CanRun() => !EditorApplication.isPlayingOrWillChangePlaymode &&
                                !EditorApplication.isCompiling && !EditorApplication.isUpdating;

        static void Report(Exception e)
        {
            if (e is OperationCanceledException) Debug.Log("Etapa cancelada; se conservo la arquitectura anterior.");
            else Debug.LogException(e);
        }

        static Source InspectSource(Scene scene,bool inspectGeometry=true)
        {
            if (!scene.IsValid() || (scene.path != Target && !(scene.path == "" &&
                (scene.name == "TEMP_Ciudad_del_Sol_Structure_Preview" || scene.name == PreviewName))))
                throw new InvalidOperationException("Activa la dungeon o su previsualizacion de estructura existente.");
            var roots = scene.GetRootGameObjects().Where(g=>g.name == BaseRoot).ToArray();
            if (roots.Length != 1 || !roots[0].transform.Find(BaseSignature))
                throw new InvalidOperationException("No se encontro una estructura base unica. Esta herramienta no la reconstruye.");
            Transform root = roots[0].transform;
            Transform jardin = root.Find(GardenName);
            if (!jardin || !jardin.gameObject.activeInHierarchy || Vector3.Distance(jardin.position,Center)>.01f ||
                Vector3.Distance(root.position,Vector3.zero)>.01f || Quaternion.Angle(root.rotation,Quaternion.identity)>.01f ||
                Vector3.Distance(root.lossyScale,Vector3.one)>.001f || Vector3.Distance(jardin.lossyScale,Vector3.one)>.001f ||
                Quaternion.Angle(jardin.rotation,Quaternion.identity)>.01f)
                throw new InvalidOperationException("El jardin no coincide con el contrato original: centro (-102,4,-18), escala 1 y sin rotacion.");
            StageRoots(jardin); // Reject unrelated objects with the reserved stage name before doing any work.
            var source = new Source { Root=root, Garden=jardin };
            if (!inspectGeometry) return source;
            foreach (string prefix in RoutePrefixes)
            {
                var matches = root.Cast<Transform>().Where(t=>t.name.StartsWith(prefix,StringComparison.Ordinal)).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("Falta o esta duplicado el recorrido " + prefix);
                source.Routes.Add(matches[0]);
                foreach (var c in matches[0].GetComponentsInChildren<Collider>())
                {
                    if (!c.enabled || c.isTrigger) continue;
                    Bounds b=c.bounds;
                    b.Expand(new Vector3(2,2,2));
                    source.RouteProtection.Add(b);
                }
            }
            Transform floor = jardin.Find("Suelo modular - superficie Y=4");
            Transform rim = jardin.Find("Perimetro y basamento");
            if (!floor || !rim) throw new InvalidOperationException("Cambio la jerarquia base de suelo/perimetro. Revisar antes de generar.");
            foreach (var parent in new[] {floor,rim})
                foreach (var c in parent.GetComponentsInChildren<BoxCollider>())
                    if (c.enabled && !c.isTrigger && Mathf.Abs(c.size.x-6)<.01f && Mathf.Abs(c.size.z-6)<.01f &&
                        c.bounds.max.y<=4.05f) source.Floor.Add(c.bounds);
            // Read every existing non-floor collider, excluding only an older copy of THIS pass.
            // In particular the +4m gallery remains original, including its 6m-wide stair.
            var owned=StageRoots(jardin);
            Transform oldGallery=jardin.Find("Galeria lateral elevada +4");
            if (!oldGallery) throw new InvalidOperationException("No se reconoce la galeria elevada original del jardin.");
            foreach (var c in jardin.GetComponentsInChildren<Collider>())
            {
                if (!c.enabled || c.isTrigger || owned.Any(t=>c.transform.IsChildOf(t))) continue;
                Bounds b=c.bounds;
                if (b.max.y<=4.05f) continue;
                b.Expand(new Vector3(.6f,.1f,.6f));
                source.ExistingProtection.Add(b);
                if (c.transform.IsChildOf(oldGallery))
                {
                    // Conservatively reserve body/headroom above the old raised floor and its ramp.
                    Vector3 min=b.min,max=b.max; max.y+=4.5f;
                    var walking=new Bounds(); walking.SetMinMax(min,max);
                    source.ExistingProtection.Add(walking);
                }
            }
            if (source.Floor.Count<240) throw new InvalidOperationException("No se reconoce el pavimento modular del jardin existente.");
            return source;
        }

        static List<Transform> StageRoots(Transform jardin)
        {
            var stages = jardin.Cast<Transform>().Where(t=>t.name == StageName).ToList();
            if (stages.Any(t=>!t.Find(StageSignature)))
                throw new InvalidOperationException("Hay un objeto JardinesDorados_Architecture ajeno a esta herramienta. No se modificara.");
            return stages;
        }

        static void Marker(Transform parent,string name)
        {
            var go=new GameObject(name);
            go.transform.SetParent(parent,false);
            go.hideFlags=HideFlags.HideInHierarchy;
        }

        static Transform Copy(Transform source,Transform parent)
        {
            var copy=Object.Instantiate(source.gameObject,parent);
            copy.name=source.name;
            copy.transform.SetPositionAndRotation(source.position,source.rotation);
            return copy.transform;
        }

        static void Commit(GameObject created,GameObject[] previous,string label)
        {
            Undo.IncrementCurrentGroup();
            int group=Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(label);
            created.name = created.transform.parent ? StageName : BaseRoot;
            Undo.RegisterCreatedObjectUndo(created,label);
            Undo.RegisterFullObjectHierarchyUndo(created,label);
            foreach (var old in previous) Undo.DestroyObjectImmediate(old);
            Undo.CollapseUndoOperations(group);
        }

        static Dictionary<string,Prototype> LoadAssets()
        {
            var result=new Dictionary<string,Prototype>();
            foreach (var pair in Paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(pair.Value);
                if (!prefab) throw new InvalidOperationException("Falta el prefab " + pair.Value);
                var prototype=new Prototype { Prefab=prefab };
                ReadBounds(prototype);
                result.Add(pair.Key,prototype);
            }
            if (!AssetDatabase.LoadAssetAtPath<Material>(GoldPath)) throw new InvalidOperationException("Falta el material original Roof_Gold.");
            var b=result["Base"].Render;
            if ((b.size-new Vector3(6,2,6)).sqrMagnitude>.01f || !result["Base"].HasSolid ||
                (result["Base"].Solid.size-b.size).sqrMagnitude>.01f)
                throw new InvalidOperationException("Cambio el modulo de suelo de 6 x 2 x 6 o su collider.");
            var stair=result["Stair"].Render;
            if (Mathf.Abs(stair.min.x)>.02f || Mathf.Abs(stair.max.y-4)>.02f || Mathf.Abs(stair.size.z-6)>.02f)
                throw new InvalidOperationException("Cambio el pivote de Staircase_01 o su ascenso hacia -X.");
            return result;
        }

        static void ReadBounds(Prototype asset)
        {
            Transform root=asset.Prefab.transform;
            Matrix4x4 normalized=Matrix4x4.TRS(Vector3.zero,root.localRotation,root.localScale)*root.worldToLocalMatrix;
            bool found=false,solid=false;
            Bounds visible=default,collision=default;
            foreach (var filter in asset.Prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var r=filter.GetComponent<MeshRenderer>();
                if (!r || !r.enabled || !filter.sharedMesh || !ActiveAsset(filter.transform,root)) continue;
                if (filter.GetComponents<MonoBehaviour>().Any(c=>c && c.GetType().FullName == "SunTemple.DisableRenderer")) continue;
                Encapsulate(ref visible,ref found,filter.sharedMesh.bounds,normalized*filter.transform.localToWorldMatrix);
            }
            foreach (var c in asset.Prefab.GetComponentsInChildren<Collider>(true))
            {
                if (!c.enabled || c.isTrigger || !ActiveAsset(c.transform,root)) continue;
                Bounds local;
                if (c is BoxCollider box) local=new Bounds(box.center,box.size);
                else if (c is MeshCollider mesh && mesh.sharedMesh) local=mesh.sharedMesh.bounds;
                else if (c is SphereCollider sphere) local=new Bounds(sphere.center,Vector3.one*sphere.radius*2);
                else if (c is CapsuleCollider capsule)
                {
                    Vector3 size=Vector3.one*capsule.radius*2;
                    size[capsule.direction]=Mathf.Max(size[capsule.direction],capsule.height);
                    local=new Bounds(capsule.center,size);
                }
                else throw new InvalidOperationException("Collider no medible: " + asset.Prefab.name);
                Encapsulate(ref collision,ref solid,local,normalized*c.transform.localToWorldMatrix);
            }
            if (!found) throw new InvalidOperationException("Prefab sin malla visible: " + asset.Prefab.name);
            asset.Render=visible; asset.Solid=collision; asset.HasSolid=solid;
        }

        static bool ActiveAsset(Transform t,Transform root)
        {
            for (;t;t=t.parent)
            {
                if (!t.gameObject.activeSelf) return false;
                if (t == root) break;
            }
            return true;
        }

        static void Encapsulate(ref Bounds result,ref bool found,Bounds local,Matrix4x4 matrix)
        {
            for (int x=-1;x<=1;x+=2) for (int y=-1;y<=1;y+=2) for (int z=-1;z<=1;z+=2)
            {
                Vector3 p=matrix.MultiplyPoint3x4(local.center+Vector3.Scale(local.extents,new Vector3(x,y,z)));
                if (!found) { result=new Bounds(p,Vector3.zero); found=true; }
                else result.Encapsulate(p);
            }
        }

        static Bounds WorldBounds(Bounds b,Vector3 pivot,float yaw)
        {
            bool found=false; Bounds result=default;
            Encapsulate(ref result,ref found,b,Matrix4x4.TRS(pivot,Quaternion.Euler(0,yaw,0),Vector3.one));
            return result;
        }

        sealed class Plan
        {
            readonly Source source;
            readonly Dictionary<string,Prototype> assets;
            public readonly List<Piece> Pieces=new List<Piece>();
            public int Omitted { get; private set; }
            public string Description => "Jardines Dorados: " + Pieces.Count + " prefabs, estanque ceremonial, " +
                "cuatro patios vegetales, galeria occidental conservada, pergola norte y pabellon elevado. " +
                Omitted + " piezas opcionales omitidas por colision/contorno. " + ResourceNote;

            public Plan(Source source,Dictionary<string,Prototype> assets) { this.source=source; this.assets=assets; }
            static Vector3 P(float x,float y,float z) => Center+new Vector3(x,y,z);

            bool Add(string key,Vector3 pivot,float yaw,string group,bool visual=false,bool gold=false,bool required=false)
            {
                Prototype a=assets[key];
                var p=new Piece { Key=key,Pivot=pivot,Yaw=yaw,Group=group,VisualOnly=visual,Gold=gold,
                    Render=WorldBounds(a.Render,pivot,yaw),Solid=a.HasSolid?WorldBounds(a.Solid,pivot,yaw):default };
                Bounds envelope=p.Render;
                if (!visual && a.HasSolid) envelope.Encapsulate(p.Solid);
                string failure=InsideFootprint(envelope)?null:"fuera de la superficie de 96 x 96";
                if (!visual && !a.HasSolid && !ColliderOptional.Contains(key)) failure="sin collider original";
                if (!visual && Protected(envelope)) failure="invade un acceso, un recorrido o la galeria existente";
                if (failure!=null)
                {
                    if (required) throw new InvalidOperationException(key+": "+failure+" en "+pivot);
                    Omitted++; return false;
                }
                Pieces.Add(p); return true;
            }

            bool Surface(string key,Vector3 anchor,float yaw,string group,bool top=false,bool visual=false,bool gold=false,bool required=false)
            {
                Bounds b=assets[key].Render;
                Vector3 offset=new Vector3(b.center.x,top?b.max.y:b.min.y,b.center.z);
                return Add(key,anchor-Quaternion.Euler(0,yaw,0)*offset,yaw,group,visual,gold,required);
            }

            bool Protected(Bounds b)
            {
                return source.RouteProtection.Any(r=>r.Intersects(b)) ||
                    source.ExistingProtection.Any(r=>r.Intersects(b)) || Reservations.Any(r=>r.Intersects(b));
            }

            static bool InsideFootprint(Bounds b)
            {
                return b.min.x>=Center.x-48.05f && b.max.x<=Center.x+48.05f &&
                    b.min.z>=Center.z-48.05f && b.max.z<=Center.z+48.05f;
            }

            float Underside(Vector3 p)
            {
                var boxes=source.Floor.Where(b=>p.x>=b.min.x-.01f && p.x<=b.max.x+.01f && p.z>=b.min.z-.01f && p.z<=b.max.z+.01f).ToArray();
                if (boxes.Length==0) throw new InvalidOperationException("Contrafuerte sin contacto con la base existente: "+p);
                return boxes.Min(b=>b.min.y);
            }

            public void Compose()
            {
                Pavement();
                Pool();
                GardenCourt(-36,-36,"TreeA");
                GardenCourt(-36,36,"TreeB");
                GardenCourt(39,-36,"TreeA");
                GardenCourt(9,-39,"TreeB");
                WestGallery();
                Pavilion(P(3,0,36),"05 - Pergola del patio norte");
                RaisedPavilion();
                SculptureAndRuins();
                Undercroft();
                if (!Pieces.Any(p=>p.Key=="Water") || Pieces.Count(p=>p.Key=="Canopy")!=2)
                    throw new InvalidOperationException("Composicion central/pabellones incompleta; no se aplicara la etapa.");
            }

            void Pavement()
            {
                const string group="01 - Senderos ceremoniales y juntas doradas";
                Vector2[][] paths=
                {
                    new[] {new Vector2(-12,-48),new Vector2(-12,-24),new Vector2(24,-24),new Vector2(24,30),new Vector2(48,30)},
                    new[] {new Vector2(-18,-24),new Vector2(-18,6)},
                    new[] {new Vector2(-18,21),new Vector2(24,21)}
                };
                int ordinal=0;
                foreach (var path in paths)
                    for (int j=0;j<path.Length-1;j++)
                    {
                        Vector2 delta=path[j+1]-path[j],direction=delta.normalized;
                        Vector2 side=new Vector2(direction.y,-direction.x);
                        float yaw=Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg;
                        int n=Mathf.CeilToInt(delta.magnitude/6);
                        for (int i=0;i<n;i++) foreach (float sign in new[] {-1f,1f})
                        {
                            Vector2 p=path[j]+direction*Mathf.Min((i+.5f)*6,delta.magnitude-3)+side*(sign*6);
                            Surface("Curb",P(p.x,.02f+(ordinal++%3)*.002f,p.y),yaw,group,true,true,true);
                        }
                    }
            }

            void Frame(Vector3 center,float width,float depth,string group,bool gold=false,bool inset=false,bool required=false)
            {
                // Native 6m curb pieces. All planned frames use multiples of six.
                for (int i=0;i<Mathf.RoundToInt(width/6);i++) foreach (float sign in new[] {-1f,1f})
                    Surface("Curb",center+new Vector3((i+.5f)*6-width*.5f,0,sign*depth*.5f),90,group,inset,inset,gold,required);
                for (int i=0;i<Mathf.RoundToInt(depth/6);i++) foreach (float sign in new[] {-1f,1f})
                    Surface("Curb",center+new Vector3(sign*width*.5f,0,(i+.5f)*6-depth*.5f),0,group,inset,inset,gold,required);
            }

            void Pool()
            {
                const string group="02 - Estanque ornamental somero - sin simulacion";
                // 3m east of center preserves the old western gallery's approach and the east promenade.
                Frame(P(3,0,0),24,24,group,true,false,true);
                Surface("Water",P(3,.035f,0),0,group,true,true,false,true);
                foreach (float z in new[] {-15.5f,10.5f})
                    Surface("Bench",P(3,0,z),z>0?180:0,group);
                foreach (float x in new[] {-7.5f,13.5f}) foreach (float z in new[] {-10f,10f})
                {
                    Surface("Planter",P(x,0,z),0,group);
                    Surface("Flowers",P(x,.42f,z),0,group);
                }
                // Water is a real original decal prefab with its original scale/rotation/material.
                // No pit excavation, water volume, generic primitive, or replacement floor is created.
            }

            void GardenCourt(float x,float z,string tree)
            {
                string group="03 - Patios vegetales/Patio "+x+","+z;
                Frame(P(x,0,z),12,12,group,false,false,true);
                Surface(tree,P(x,0,z),0,group,required:true);
                Surface("Shrub",P(x+2,0,z-2),0,group);
                Surface("FlowerGroup",P(x-3,0,z+3),25,group);
                Surface("FlowerGroup",P(x+3,0,z+2),-20,group);
                foreach (float dx in new[] {-4f,4f})
                {
                    Surface("Planter",P(x+dx,0,z-4),0,group);
                    Surface("Flowers",P(x+dx,.42f,z-4),0,group);
                }
                // Dense planting remains inside the framed courts, outside all protected paths.
            }

            void WestGallery()
            {
                const string group="04 - Arqueria junto a galeria occidental original";
                foreach (float z in new[] {-27f,-9f,9f,27f})
                {
                    bool placed=Surface(z==27?"BrokenArch":"Arch",P(-42,0,z),0,group);
                    if (placed && z!=27) Surface("Ivy",P(-46.1f,7,z),180,group,true,true);
                }
                foreach (float z in new[] {-36f,-18f,0f,18f,36f})
                    Surface(z==18?"BrokenColumn":"Column",P(-43,0,z),0,group);
                // Nothing is attached to, moved, disabled or replaced in the existing +4m gallery.
            }

            void Pavilion(Vector3 floor,string group)
            {
                foreach (float x in new[] {-5.2f,5.2f}) foreach (float z in new[] {-2.15f,2.15f})
                    Surface("Column",floor+new Vector3(x,0,z),0,group,required:true);
                Surface("Canopy",floor+Vector3.up*assets["Column"].Render.size.y,90,group,required:true);
                Frame(floor+Vector3.up*.018f,12,12,group,true,true);
            }

            void RaisedPavilion()
            {
                const string group="06 - Pabellon elevado oriental y escalinata de 12m";
                Vector3 floor=P(39,4,0);
                for (int x=0;x<2;x++) for (int z=0;z<2;z++)
                    Surface("Base",floor+new Vector3((x-.5f)*6,0,(z-.5f)*6),0,group,true,false,false,true);
                // Original stair ascends toward local -X. High end meets the north edge of the new deck.
                Stairs(P(39,0,12),Vector3.back,12,group);
                foreach (var normal in new[] {Vector3.right,Vector3.left,Vector3.back})
                {
                    Vector3 tangent=Vector3.Cross(Vector3.up,normal);
                    float yaw=Mathf.Atan2(tangent.x,tangent.z)*Mathf.Rad2Deg;
                    for (int i=0;i<2;i++) Surface("Rail",floor+normal*6.55f+tangent*((i-.5f)*6),yaw,group,required:true);
                }
                Surface("PlatformSupport",floor-Vector3.up*2,90,group,true,false,false,true);
                Pavilion(floor,group);
            }

            void Stairs(Vector3 low,Vector3 direction,float width,string group)
            {
                Vector3 side=Vector3.Cross(Vector3.up,direction),high=low+direction*6+Vector3.up*4;
                float yaw=Mathf.Atan2(direction.z,-direction.x)*Mathf.Rad2Deg;
                for (int i=0;i<Mathf.RoundToInt(width/6);i++)
                    Add("Stair",high-Vector3.up*4+side*((i+.5f)*6-width*.5f),yaw,group,required:true);
                float railYaw=Mathf.Atan2(-direction.x,-direction.z)*Mathf.Rad2Deg;
                float lift=1.5f-assets["StairRail"].Render.max.y;
                foreach (float sign in new[] {-1f,1f})
                    Add("StairRail",high+side*(sign*(width*.5f+.55f))+Vector3.up*lift,railYaw,group,required:true);
            }

            void SculptureAndRuins()
            {
                const string group="07 - Exedras, esculturas, jardineras y ruinas";
                foreach (float x in new[] {-12f,18f}) Surface("Statue",P(x,0,39),180,group);
                foreach (float x in new[] {-6f,0f,6f,12f}) Surface("Rail",P(x,-.9f,45),90,group);
                foreach (float z in new[] {-39f,-21f}) Surface("BrokenWall",P(45,0,z),0,group);
                foreach (float z in new[] {39f,45f}) Surface("TreeTall",P(36,0,z),0,group);
                foreach (var p in new[] {new Vector2(-18,36),new Vector2(18,36),new Vector2(36,-18),new Vector2(36,18)})
                {
                    Surface("Planter",P(p.x,0,p.y),0,group);
                    Surface("Flowers",P(p.x,.42f,p.y),0,group);
                }
            }

            void Undercroft()
            {
                const string group="08 - Cornisas y contrafuertes suspendidos";
                foreach (float x in new[] {-42f,-18f,6f,30f}) foreach (float z in new[] {-42f,42f})
                {
                    Vector3 p=P(x,0,z); p.y=Underside(p);
                    Surface("Buttress",p,90,group,true);
                }
                foreach (float z in new[] {-42f,-18f,6f,30f}) foreach (float x in new[] {-42f,42f})
                {
                    Vector3 p=P(x,0,z); p.y=Underside(p);
                    Surface("Buttress",p,0,group,true);
                }
                for (int i=0;i<15;i++) foreach (float sign in new[] {-1f,1f})
                {
                    float a=-42+i*6;
                    Vector3 north=P(a,0,sign*46),east=P(sign*46,0,a);
                    north.y=Underside(north)+.02f; east.y=Underside(east)+.02f;
                    Surface(i%7==2?"BrokenFascia":"Fascia",north,90,group,true,true);
                    Surface(i%7==4?"BrokenFascia":"Fascia",east,0,group,true,true);
                }
            }
        }

        static GameObject InstantiatePlan(Transform jardin,Dictionary<string,Prototype> assets,Plan plan,Scene scene)
        {
            var stage=new GameObject(StageName+"__Building");
            stage.transform.SetParent(jardin,false);
            try
            {
                var groups=new Dictionary<string,Transform>();
                Material gold=AssetDatabase.LoadAssetAtPath<Material>(GoldPath);
                for (int i=0;i<plan.Pieces.Count;i++)
                {
                    Piece p=plan.Pieces[i];
                    if (i%24==0 && EditorUtility.DisplayCancelableProgressBar("Arquitectura Jardines Dorados",
                        p.Group+" - "+i+"/"+plan.Pieces.Count,i/(float)plan.Pieces.Count)) throw new OperationCanceledException();
                    if (!groups.TryGetValue(p.Group,out var group))
                    {
                        Transform parent=stage.transform;
                        string groupPath="";
                        foreach (string label in p.Group.Split('/'))
                        {
                            groupPath=groupPath.Length==0?label:groupPath+"/"+label;
                            if (!groups.TryGetValue(groupPath,out var child))
                            {
                                var go=new GameObject(label); go.transform.SetParent(parent,false);
                                child=go.transform; groups.Add(groupPath,child);
                            }
                            parent=child;
                        }
                        group=parent;
                    }
                    Prototype asset=assets[p.Key];
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset.Prefab,scene);
                    instance.transform.SetParent(group,false);
                    instance.transform.localScale=asset.Prefab.transform.localScale;
                    instance.transform.SetPositionAndRotation(p.Pivot,Quaternion.Euler(0,p.Yaw,0)*asset.Prefab.transform.localRotation);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                    foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (!behaviour) continue;
                        if (behaviour.GetType().FullName=="SunTemple.DisableRenderer")
                        {
                            var helper=behaviour.GetComponent<Renderer>();
                            if (helper) { helper.enabled=false; PrefabUtility.RecordPrefabInstancePropertyModifications(helper); }
                        }
                        behaviour.enabled=false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    }
                    if (p.VisualOnly)
                        foreach (var c in instance.GetComponentsInChildren<Collider>(true))
                        { c.enabled=false; PrefabUtility.RecordPrefabInstancePropertyModifications(c); }
                    foreach (var r in instance.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (p.Gold) { r.sharedMaterials=Enumerable.Repeat(gold,r.sharedMaterials.Length).ToArray(); PrefabUtility.RecordPrefabInstancePropertyModifications(r); }
                        GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);
                    }
                }
                Marker(stage.transform,StageSignature);
                return stage;
            }
            catch { Object.DestroyImmediate(stage); throw; }
        }

        static void AddPreviewRig(Transform stage,Scene scene)
        {
            var rig=new GameObject("Solo preview - camara y luces"); rig.transform.SetParent(stage,false);
            var cameraObject=new GameObject("Camara de arquitectura"); cameraObject.transform.SetParent(rig.transform,false);
            var camera=cameraObject.AddComponent<Camera>();
            camera.orthographic=true; camera.orthographicSize=78; camera.depth=100;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.045f,.025f,.075f);
            camera.nearClipPlane=.3f; camera.farClipPlane=700;
            camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
            camera.transform.position=Center+new Vector3(90,95,-105);
            camera.transform.LookAt(Center+Vector3.up*9);
            foreach (var data in new[] {new Vector3(50,-35,1.15f),new Vector3(35,145,.55f)})
            {
                var go=new GameObject("Luz de inspeccion"); go.transform.SetParent(rig.transform,false);
                go.transform.rotation=Quaternion.Euler(data.x,data.y,0);
                var light=go.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=data.z; light.shadows=LightShadows.None;
            }
        }

        static void FramePreview()
        {
            var view=SceneView.lastActiveSceneView;
            if (view) view.Frame(new Bounds(Center+Vector3.up*9,new Vector3(112,56,112)),false);
        }
    }
}
