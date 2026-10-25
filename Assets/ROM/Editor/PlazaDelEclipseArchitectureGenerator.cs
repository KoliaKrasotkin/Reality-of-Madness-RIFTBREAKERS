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
    /// Additive architectural pass over the existing zone 03. Never calls the structure generator.
    /// All execution is manual, all source assets are read-only, and scenes are never saved here.
    /// </summary>
    public static class PlazaDelEclipseArchitectureGenerator
    {
        const string Target = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string BaseRoot = "Dungeon_Ciudad_del_Sol_Generated";
        const string BaseSignature = "__ROM_SolarStructure_12D9D67A_v1";
        const string PlazaName = "03 - Plaza del Eclipse";
        const string StageName = "PlazaDelEclipse_Architecture";
        const string StageSignature = "__ROM_EclipseArchitecture_719A50BC_v1";
        const string PreviewName = "TEMP_PlazaDelEclipse_Architecture";
        const string PreviewSignature = "__ROM_EclipsePreview_719A50BC_v1";
        const string ViewTitle = "Plaza del Eclipse - Arquitectura";
        const string Menu = "ROM/Dungeon Ciudad del Sol/Plaza del Eclipse/";
        const string ET = "Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string ST = "Assets/Standard Assets/Sun_Temple/Prefabs/";
        const string EG = "Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/";
        const string GoldPath = "Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/Materials/Roof_Gold.mat";
        static readonly Vector3 Center = new Vector3(18,12,12);
        static readonly string[] RoutePrefixes = { "02-03 -", "03-04 -", "03-05 -", "03-06 -" };

        static readonly Dictionary<string,string> Paths = new Dictionary<string,string>
        {
            { "Base", ET + "Pits and Platforms/Platform_Base_01.prefab" },
            { "Corner", ET + "Bases/Base_Rounded_01.prefab" },
            { "Stair", ET + "Staircases/Staircase_01.prefab" },
            { "StairRail", ET + "Railings and Decors/Railing_Staircase_01.prefab" },
            { "Rail", ET + "Railings and Decors/Railing_01.prefab" },
            { "Curb", ET + "Railings and Decors/Curb_01.prefab" },
            { "Fascia", ET + "Railings and Decors/Bottom_Decor_01.prefab" },
            { "BrokenFascia", ET + "Railings and Decors/Bottom_Decor_Broken_01.prefab" },
            { "Buttress", ET + "Bases/Base_Arch_Tall_01.prefab" },
            { "BalconySupport", ET + "Bases/Base_Arch_01.prefab" },
            { "Arch", ET + "Bases/Arch_Wide_01.prefab" },
            { "BrokenArch", ET + "Bases/Thin/Arch_03_Broken_01.prefab" },
            { "Wall", ET + "Walls/Wall_Big_01.prefab" },
            { "BrokenWall", ET + "Damaged/Broken_Wall_01.prefab" },
            { "BrokenWallShort", ET + "Damaged/Broken_Wall_04.prefab" },
            { "BrokenColumn", ET + "Damaged/Column_Broken_01.prefab" },
            { "Column", ST + "Modules_Temple/Bld_Temple_Int_Column_A.prefab" },
            { "Medallion", ST + "Modules_Temple/Bld_Temple_Int_Circle_A.prefab" },
            { "SolarCrown", ST + "Modules_Temple/Bld_Temple_Deco_Sun_A.prefab" },
            { "SolarShaft", EG + "SM_GranitePillar.prefab" }
        };

        sealed class Prototype
        {
            public GameObject Prefab;
            public Bounds Render, Solid;
            public bool HasSolid;
        }

        sealed class Source
        {
            public Transform Root, Plaza;
            public readonly List<Transform> Routes = new List<Transform>();
            public readonly List<Bounds> RouteProtection = new List<Bounds>();
            public readonly List<Bounds> Floor = new List<Bounds>();
        }

        sealed class Piece
        {
            public string Key, Group;
            public Vector3 Pivot;
            public float Yaw;
            public bool VisualOnly, Gold;
            public Bounds Render, Solid;
        }

        // These are the four ACTUAL zone-03 ports in SunDungeonStructureGenerator v1.
        // The north port belongs to the 24m dogleg, not an imaginary straight central bridge.
        sealed class Approach
        {
            public readonly Vector2 Outer, Inner;
            public readonly float HalfWidth;
            public Approach(float x, float z, float width)
            {
                var direction = new Vector2(x,z).normalized;
                Outer = new Vector2(x,z) + direction*6;
                Inner = direction*28;
                HalfWidth = width*.5f+2;
            }
        }
        static readonly Approach[] Approaches =
        {
            new Approach(-60,0,12), new Approach(-54,30,12),
            new Approach(-30,48,24), new Approach(60,0,12)
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
                    var old=StageRoots(source.Plaza).Select(t=>t.gameObject).ToArray();
                    staging=InstantiatePlan(source.Plaza,assets,plan,source.Plaza.gameObject.scene);
                    AddPreviewRig(staging.transform,source.Plaza.gameObject.scene);
                    Commit(staging,old,"Regenerar previsualizacion de la plaza");
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
                var old = StageRoots(source.Plaza).Select(t => t.gameObject).ToArray();
                staging = InstantiatePlan(source.Plaza,assets,plan,source.Plaza.gameObject.scene);
                Commit(staging,old,"Generar arquitectura de la Plaza del Eclipse");
                Selection.activeGameObject = staging;
                staging = null;
                EditorSceneManager.MarkSceneDirty(source.Plaza.gameObject.scene);
                Debug.Log(plan.Description + " No se guardo la escena ni se modifico la estructura existente.");
            }
            catch (Exception e) { if (staging) Object.DestroyImmediate(staging); Report(e); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem(Menu + "3. Eliminar solo PlazaDelEclipse_Architecture")]
        public static void Remove()
        {
            if (!CanRun()) return;
            try
            {
                Source source = InspectSource(WorkingScene(),false);
                var stages = StageRoots(source.Plaza);
                if (stages.Count == 0) return;
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Eliminar arquitectura de la plaza");
                foreach (var stage in stages) Undo.DestroyObjectImmediate(stage.gameObject);
                Undo.CollapseUndoOperations(group);
                EditorSceneManager.MarkSceneDirty(source.Plaza.gameObject.scene);
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
                if (source==null || assets==null || plan==null || !source.Plaza) return false;
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
                    Transform plaza=Copy(source.Plaza,staging.transform);
                    foreach (var old in StageRoots(plaza)) Object.DestroyImmediate(old.gameObject);
                    foreach (var route in source.Routes) Copy(route,staging.transform);
                    var architecture=InstantiatePlan(plaza,assets,plan,scene);
                    architecture.name=StageName;
                    AddPreviewRig(architecture.transform,scene);
                    Commit(staging,Array.Empty<GameObject>(),"Previsualizar arquitectura de la plaza");
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
            Transform plaza = root.Find(PlazaName);
            if (!plaza || !plaza.gameObject.activeInHierarchy || Vector3.Distance(plaza.position,Center)>.01f ||
                Vector3.Distance(root.position,Vector3.zero)>.01f || Quaternion.Angle(root.rotation,Quaternion.identity)>.01f ||
                Vector3.Distance(root.lossyScale,Vector3.one)>.001f || Vector3.Distance(plaza.lossyScale,Vector3.one)>.001f ||
                Quaternion.Angle(plaza.rotation,Quaternion.identity)>.01f)
                throw new InvalidOperationException("La plaza no coincide con el contrato original: centro (18,12,12), escala 1 y sin rotacion.");
            StageRoots(plaza); // Reject unrelated objects with the reserved stage name before doing any work.
            var source = new Source { Root=root, Plaza=plaza };
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
            Transform floor = plaza.Find("Suelo modular - superficie Y=12");
            Transform rim = plaza.Find("Perimetro y basamento");
            if (!floor || !rim) throw new InvalidOperationException("Cambio la jerarquia base de suelo/perimetro. Revisar antes de generar.");
            foreach (var parent in new[] {floor,rim})
                foreach (var c in parent.GetComponentsInChildren<BoxCollider>())
                    if (c.enabled && !c.isTrigger && Mathf.Abs(c.size.x-6)<.01f && Mathf.Abs(c.size.z-6)<.01f &&
                        c.bounds.max.y<=12.05f) source.Floor.Add(c.bounds);
            if (source.Floor.Count<300) throw new InvalidOperationException("No se reconoce el pavimento modular de la plaza existente.");
            return source;
        }

        static List<Transform> StageRoots(Transform plaza)
        {
            var stages = plaza.Cast<Transform>().Where(t=>t.name == StageName).ToList();
            if (stages.Any(t=>!t.Find(StageSignature)))
                throw new InvalidOperationException("Hay un objeto PlazaDelEclipse_Architecture ajeno a esta herramienta. No se modificara.");
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
            var corner=result["Corner"].Render;
            if ((corner.size-new Vector3(6,4,6)).sqrMagnitude>.01f || Mathf.Abs(corner.min.x)>.02f || Mathf.Abs(corner.max.z)>.02f)
                throw new InvalidOperationException("Cambio el cuarto de circulo Base_Rounded_01.");
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
            public string Description => "Plaza: " + Pieces.Count + " piezas a escala original, podio +4, " +
                "tres anillos, dos balcones y cuatro accesos protegidos. " + Omitted + " piezas perimetrales omitidas por seguridad.";

            public Plan(Source source,Dictionary<string,Prototype> assets) { this.source=source; this.assets=assets; }
            static Vector3 P(float x,float y,float z) => Center + new Vector3(x,y,z);
            static Vector3 Radial(float radius,float degrees,float y)
            {
                float a=degrees*Mathf.Deg2Rad;
                return Center+new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius);
            }
            static float NormalYaw(float degrees) => degrees-90;

            bool Add(string key,Vector3 pivot,float yaw,string group,bool visual=false,bool gold=false,bool required=false)
            {
                Prototype a=assets[key];
                var p=new Piece { Key=key, Pivot=pivot, Yaw=yaw, Group=group, VisualOnly=visual, Gold=gold,
                    Render=WorldBounds(a.Render,pivot,yaw), Solid=a.HasSolid?WorldBounds(a.Solid,pivot,yaw):default };
                Bounds envelope=p.Render;
                if (!visual && a.HasSolid) envelope.Encapsulate(p.Solid);
                string failure=InsideFootprint(envelope)?null:"fuera del contorno de 120 x 120";
                if (!visual && !a.HasSolid) failure="sin collider solido original";
                // Some imported column colliders are narrower than their stone bases.
                // Protect circulation against BOTH visible architecture and physical collision.
                if (!visual && a.HasSolid && Protected(envelope)) failure="invade circulacion o un recorrido existente";
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

            bool Protected(Bounds box)
            {
                foreach (var b in source.RouteProtection) if (b.Intersects(box)) return true;
                if (box.max.y<11.9f || box.min.y>16.5f) return false;
                foreach (var entry in Approaches) if (IntersectsApproach(box,entry)) return true;
                // Keep an uninterrupted 12m-wide circulation ring outside the central stairs.
                Vector2 nearest=new Vector2(Mathf.Clamp(Center.x,box.min.x,box.max.x)-Center.x,
                    Mathf.Clamp(Center.z,box.min.z,box.max.z)-Center.z);
                float farthest=0;
                foreach (float x in new[] {box.min.x,box.max.x}) foreach (float z in new[] {box.min.z,box.max.z})
                    farthest=Mathf.Max(farthest,Vector2.Distance(new Vector2(x,z),new Vector2(Center.x,Center.z)));
                return nearest.magnitude<36 && farthest>24;
            }

            static bool InsideFootprint(Bounds b)
            {
                foreach (float x in new[] {b.min.x,b.max.x}) foreach (float z in new[] {b.min.z,b.max.z})
                    if (Vector2.Distance(new Vector2(x,z),new Vector2(Center.x,Center.z))>60.05f) return false;
                return true;
            }

            static bool IntersectsApproach(Bounds b,Approach entry)
            {
                Vector2 axis=(entry.Inner-entry.Outer).normalized, side=new Vector2(-axis.y,axis.x);
                Vector2 center=(entry.Inner+entry.Outer)*.5f;
                Vector2 box=new Vector2(b.center.x-Center.x,b.center.z-Center.z);
                Vector2 ext=new Vector2(b.extents.x,b.extents.z);
                float halfLength=Vector2.Distance(entry.Inner,entry.Outer)*.5f;
                Vector2 delta=box-center;
                // Separating-axis test: axis-aligned collider envelope against the oriented avenue.
                if (Mathf.Abs(delta.x)>ext.x+Mathf.Abs(axis.x)*halfLength+Mathf.Abs(side.x)*entry.HalfWidth) return false;
                if (Mathf.Abs(delta.y)>ext.y+Mathf.Abs(axis.y)*halfLength+Mathf.Abs(side.y)*entry.HalfWidth) return false;
                if (Mathf.Abs(Vector2.Dot(delta,axis))>halfLength+Mathf.Abs(axis.x)*ext.x+Mathf.Abs(axis.y)*ext.y) return false;
                if (Mathf.Abs(Vector2.Dot(delta,side))>entry.HalfWidth+Mathf.Abs(side.x)*ext.x+Mathf.Abs(side.y)*ext.y) return false;
                return true;
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
                Monument();
                ColonnadesAndRuins();
                Balcony(-33);
                Balcony(33);
                Undercroft();
                if (Pieces.Count(p=>p.Group=="02 - Monumento solar y podio")<30)
                    throw new InvalidOperationException("La composicion central quedo incompleta.");
            }

            void Pavement()
            {
                const string group="01 - Pavimento ceremonial e incrustaciones";
                Surface("Medallion",P(0,.018f,0),0,group,true,true,false,true);
                // Embed native curb meshes: only 2-3cm of the original gold top is exposed.
                // Their colliders are disabled on these instances; the existing floor remains the collision surface.
                foreach (float radius in new[] {23f,35f,47f})
                    foreach (float offset in new[] {-.42f,.42f})
                    {
                        float r=radius+offset;
                        int count=Mathf.RoundToInt(2*Mathf.PI*r/assets["Curb"].Render.size.z);
                        for (int i=0;i<count;i++)
                        {
                            float angle=i*360f/count;
                            Surface("Curb",Radial(r,angle,.027f+(i%2)*.004f),angle+90,group,true,true,true,true);
                        }
                    }
                // Four radial ornamental axes make the concentric bands read as a solar diagram.
                for (int axis=0;axis<4;axis++)
                    for (int segment=0;segment<4;segment++)
                        Surface("Curb",Radial(23+segment*6,axis*90,.039f),axis*90,group,true,true,true,true);
            }

            void Monument()
            {
                const string group="02 - Monumento solar y podio";
                // A 24m rounded dais: a modular cross plus four original 6m quarter-circle corners.
                // The 12m-wide flat sides let native stairs meet the full landing without intersecting a curved solid.
                for (int x=0;x<4;x++) for (int z=0;z<4;z++)
                {
                    if ((x==0 || x==3) && (z==0 || z==3)) continue;
                    foreach (float top in new[] {2f,4f})
                        Surface("Base",P((x+.5f)*6-12,top,(z+.5f)*6-12),0,group,true,false,false,true);
                }
                Add("Corner",P(6,0,-6),0,group,required:true);
                Add("Corner",P(-6,0,-6),90,group,required:true);
                Add("Corner",P(-6,0,6),180,group,required:true);
                Add("Corner",P(6,0,6),270,group,required:true);
                foreach (var outward in new[] {Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                    Stairs(Center+outward*18,-outward,12,group);
                Surface("SolarShaft",P(0,4,0),0,group,required:true);
                float crownBottom=4+assets["SolarShaft"].Render.size.y-1;
                Surface("SolarCrown",P(0,crownBottom,0),0,group,false,true,false,true);
            }

            void Stairs(Vector3 low,Vector3 direction,float width,string group)
            {
                Vector3 side=Vector3.Cross(Vector3.up,direction), high=low+direction*6+Vector3.up*4;
                float yaw=Mathf.Atan2(direction.z,-direction.x)*Mathf.Rad2Deg;
                for (int i=0;i<Mathf.RoundToInt(width/6);i++)
                    Add("Stair",high-Vector3.up*4+side*((i+.5f)*6-width*.5f),yaw,group,required:true);
                float railYaw=Mathf.Atan2(-direction.x,-direction.z)*Mathf.Rad2Deg;
                float lift=1.5f-assets["StairRail"].Render.max.y;
                foreach (float sign in new[] {-1f,1f})
                    Add("StairRail",high+side*(sign*(width*.5f+.55f))+Vector3.up*lift,railYaw,group,required:true);
            }

            void ColonnadesAndRuins()
            {
                // Formal northern/eastern rhythm, fragmentary southern/western enclosure.
                foreach (float a in new[] {0f,18f,36f,54f,72f,108f,126f,144f,162f,180f,198f,216f,234f,252f,288f,354f})
                    Surface(a==198 || a==252 || a==288 ? "BrokenColumn":"Column",Radial(46,a,0),NormalYaw(a),
                        "03 - Columnatas monumentales y vestigios");
                foreach (float a in new[] {22f,142f,202f})
                    Surface("Arch",Radial(51,a,0),NormalYaw(a),"04 - Recintos y arquerias antiguas");
                foreach (float a in new[] {160f,228f,350f})
                    Surface("BrokenArch",Radial(52,a,0),NormalYaw(a),"04 - Recintos y arquerias antiguas");
                foreach (float a in new[] {6f,14f,30f,38f,54f,62f,116f,124f,150f,174f,182f,210f,218f,242f,250f})
                {
                    string key=a==14 || a==124 || a==218 ? "Wall" : (a==62 || a==182 || a==250 ? "BrokenWallShort":"BrokenWall");
                    Surface(key,Radial(54,a,0),NormalYaw(a),"04 - Recintos y arquerias antiguas");
                }
            }

            void Balcony(float x)
            {
                string group=x<0 ? "05 - Balcon suroeste y escalinata":"06 - Balcon sureste y escalinata";
                Vector3 center=P(x,4,-37.5f);
                for (int ix=0;ix<2;ix++) for (int iz=0;iz<2;iz++)
                    Surface("Base",center+new Vector3((ix-.5f)*6,0,(iz-.5f)*6),0,group,true,false,false,true);
                Vector3 inward=x<0?Vector3.right:Vector3.left;
                Stairs(center+inward*12-Vector3.up*4,-inward,12,group);
                foreach (var normal in new[] {Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                {
                    if (normal==inward) continue;
                    Vector3 tangent=Vector3.Cross(Vector3.up,normal);
                    float yaw=Mathf.Atan2(tangent.x,tangent.z)*Mathf.Rad2Deg;
                    for (int i=0;i<2;i++) Surface("Rail",center+normal*6.55f+tangent*((i-.5f)*6),yaw,group,required:true);
                }
                Surface("BalconySupport",center-Vector3.up*2,90,group,true,false,false,true);
            }

            void Undercroft()
            {
                const string group="07 - Cornisas inferiores y contrafuertes suspendidos";
                foreach (float a in new[] {0f,30f,60f,120f,150f,180f,210f,240f})
                {
                    Vector3 p=Radial(51,a,0); p.y=Underside(p);
                    Surface("Buttress",p,NormalYaw(a),group,true);
                }
                int count=Mathf.RoundToInt(2*Mathf.PI*56/6);
                for (int i=0;i<count;i++)
                {
                    float angle=i*360f/count;
                    Vector3 p=Radial(56,angle,0); p.y=Underside(p)+.02f;
                    Surface(i%11==3 ? "BrokenFascia":"Fascia",p,NormalYaw(angle),group,true,true);
                }
            }
        }

        static GameObject InstantiatePlan(Transform plaza,Dictionary<string,Prototype> assets,Plan plan,Scene scene)
        {
            var stage=new GameObject(StageName+"__Building");
            stage.transform.SetParent(plaza,false);
            try
            {
                var groups=new Dictionary<string,Transform>();
                Material gold=AssetDatabase.LoadAssetAtPath<Material>(GoldPath);
                for (int i=0;i<plan.Pieces.Count;i++)
                {
                    Piece p=plan.Pieces[i];
                    if (i%24==0 && EditorUtility.DisplayCancelableProgressBar("Arquitectura Plaza del Eclipse",
                        p.Group+" - "+i+"/"+plan.Pieces.Count,i/(float)plan.Pieces.Count)) throw new OperationCanceledException();
                    if (!groups.TryGetValue(p.Group,out var group))
                    {
                        var go=new GameObject(p.Group); go.transform.SetParent(stage.transform,false);
                        group=go.transform; groups.Add(p.Group,group);
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
            camera.orthographic=true; camera.orthographicSize=86; camera.depth=100;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.045f,.025f,.075f);
            camera.nearClipPlane=.3f; camera.farClipPlane=700;
            camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
            camera.transform.position=Center+new Vector3(105,115,-135);
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
            if (view) view.Frame(new Bounds(Center+Vector3.up*9,new Vector3(130,64,130)),false);
        }
    }
}
