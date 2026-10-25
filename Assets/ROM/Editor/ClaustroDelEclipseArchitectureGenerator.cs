using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ROM.EditorTools
{
    // Independent architecture pass; integration only through the explicit approved-stage menu.
    public static class ClaustroDelEclipseArchitectureGenerator
    {
        const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Root="Dungeon_Ciudad_del_Sol_Generated";
        const string Zone="05 - Claustro del Eclipse";
        const string StageName="ClaustroDelEclipse_Architecture";
        const string Menu="ROM/Dungeon Ciudad del Sol/Claustro del Eclipse/";
        const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string ST="Assets/Standard Assets/Sun_Temple/Prefabs/";
        const string EG="Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/";
        static readonly Vector3 Center=new Vector3(18,28,144);
        static readonly Dictionary<string,string> Paths=new Dictionary<string,string>
        {
            {"Base",ET+"Pits and Platforms/Platform_Base_01.prefab"},
            {"Stair",ET+"Staircases/Staircase_01.prefab"},
            {"LibraryColumn",ST+"Modules_Temple/Bld_Temple_Int_Column_A.prefab"},
            {"Pillar",EG+"SM_GranitePillar.prefab"},
            {"Wall",ET+"Buildings/Walls/1-Segment Long/Walls_2x1_01.prefab"},
            {"Arch",ET+"Bases/Thin/Entrance_Arch_01.prefab"},
            {"BrokenArch",ET+"Bases/Thin/Arch_03_Broken_01.prefab"},
            {"BrokenWall",ET+"Damaged/Broken_Wall_01.prefab"},
            {"BrokenColumn",ET+"Damaged/Column_Broken_01.prefab"},
            {"Trim",ET+"Railings and Decors/Bottom_Decor_01.prefab"},
            {"Rail",ET+"Railings and Decors/Railing_01.prefab"},
            {"Paving",ET+"Road and Plaza/Plaza_Floor_01.prefab"},
            {"Sun",ST+"Modules_Temple/Bld_Temple_Deco_Sun_A.prefab"},
            {"Altar",ET+"Props/Furniture/Stone_Altar_01.prefab"},
            {"Statue",EG+"SM_LadyStatue_Low.prefab"}
        };
        sealed class Asset { public GameObject Prefab; public Bounds Bounds; }
        sealed class Surface { public Bounds Bounds; public float Height; }
        sealed class Piece { public string Key; public Bounds Bounds; public GameObject Object; }
        sealed class Source
        {
            public Transform Root,Zone; public Scene Scene;
            public readonly List<Transform> Routes=new List<Transform>(),Landmarks=new List<Transform>();
            public readonly List<Surface> Ground=new List<Surface>();
            public readonly List<Bounds> Obstacles=new List<Bounds>();
        }
        sealed class Plan
        {
            public Transform Stage; public Source Source;
            public readonly List<Surface> Surfaces=new List<Surface>();
            public readonly List<Piece> Pieces=new List<Piece>();
            public readonly List<string> Skips=new List<string>();
            public readonly Dictionary<string,int> Counts=new Dictionary<string,int>();
        }
        static bool Ready=>!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating;

        [MenuItem(Menu+"1. Previsualizar complejo ceremonial (copia temporal) %#&F12")]
        public static void Preview()
        {
            if(!Ready) return;
            try
            {
                ClosePreview(); var source=Inspect(); var assets=LoadAssets();
                var preview=ScriptableObject.CreateInstance<CloisterPreview>(); preview.Source=source; preview.Assets=assets;
                StageUtility.GoToStage(preview,true);
            }
            catch(Exception e) { Debug.LogException(e); }
        }
        [MenuItem(Menu+"2. Validar accesos, escaleras y colliders")]
        public static void Validate()
        {
            try
            {
                var preview=StageUtility.GetCurrentStage() as CloisterPreview;
                if(!preview||preview.Plan==null) throw new InvalidOperationException("Abre primero la previsualizacion del Claustro.");
                ValidatePlan(preview.Plan);
            }
            catch(Exception e) { Debug.LogException(e); }
        }
        [MenuItem(Menu+"3. Cerrar previsualizacion sin guardar")]
        public static void ClosePreview()
        { if(Ready&&StageUtility.GetCurrentStage() is CloisterPreview) StageUtility.GoToMainStage(); }


        [MenuItem(Menu+"4. Integrar Claustro aprobado y guardar tras validar %#&F10")]
        public static void IntegrateApproved()
        {
            if(!Ready) return;
            int undo=-1; bool saved=false;
            try
            {
                var preview=StageUtility.GetCurrentStage() as CloisterPreview;
                string approved=preview && preview.Plan!=null?Layout(preview.Plan.Stage):null;
                ClosePreview();
                var source=Inspect();
                if(source.Scene.isDirty) throw new InvalidOperationException("La escena tiene cambios previos sin guardar; no se guardaran con esta etapa.");
                string[] stages={"01 - Entrada Portico Solar/PorticoSolar_Architecture","02 - Jardines Dorados/JardinesDorados_Architecture","03 - Plaza del Eclipse/PlazaDelEclipse_Architecture","04 - Archivos Solares/ArchivosSolares_Architecture"};
                foreach(var path in stages) if(!source.Root.Find(path)) throw new InvalidOperationException("Falta arquitectura aprobada: "+path);
                var matches=source.Zone.Cast<Transform>().Where(t=>t.name==StageName).ToArray();
                if(matches.Length>1) throw new InvalidOperationException("Claustro duplicado.");
                var existing=matches.FirstOrDefault();
                if(existing&&!existing.Find("__ROM_EclipseCloister_Architecture_v1")) throw new InvalidOperationException("Grupo ajeno; no se reemplazara.");
                var preservation=new Preservation(source.Scene,source.Zone,existing);
                var assets=LoadAssets();
                Undo.IncrementCurrentGroup(); undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Integrar Claustro aprobado");
                Plan plan;
                if(!existing) plan=Build(source,source.Zone,assets,true);
                else
                {
                    plan=new Plan{Stage=existing,Source=source};
                    foreach(Transform group in existing) foreach(Transform t in group)
                    {
                        if(!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
                        string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                        string key=Paths.Single(k=>k.Value==path).Key;
                        plan.Pieces.Add(new Piece{Key=key,Object=t.gameObject});
                    }
                }
                if(plan.Pieces.Count!=356||plan.Pieces.Count(p=>p.Key=="Stair")!=10) throw new InvalidOperationException("La composicion aprobada debe tener 356 prefabs y diez escaleras.");
                ValidatePlan(plan);
                if(approved!=null&&approved!=Layout(plan.Stage)) throw new InvalidOperationException("Distribucion distinta de la previsualizacion aprobada.");
                preservation.Verify();
                if(source.Scene.GetRootGameObjects().Any(g=>g.name=="Dungeon_Void_Atmosphere")) throw new InvalidOperationException("Vacio inesperado.");
                if(source.Scene.path!=Target) throw new InvalidOperationException("Escena destino incorrecta.");
                EditorSceneManager.MarkSceneDirty(source.Scene);
                if(!EditorSceneManager.SaveScene(source.Scene,Target,false)) throw new InvalidOperationException("Error al guardar.");
                saved=true; Undo.CollapseUndoOperations(undo);
                Debug.Log("CLAUSTRO_INTEGRADO_GUARDADO: 356 prefabs, diez escaleras, tres accesos libres. Cinco arquitecturas aprobadas presentes; objetos previos, seis zonas, nueve caminos y colliders intactos. Sin luces nuevas ni Vacio.");
            }
            catch(Exception e)
            {
                if(!saved&&undo>=0) Undo.RevertAllDownToGroup(undo);
                Debug.LogError("CLAUSTRO_INTEGRACION_DETENIDA: "+e.Message);
            }
        }
        static string Layout(Transform stage)
        {
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            var rows=new List<string>();
            foreach(Transform group in stage) foreach(Transform piece in group)
                if(PrefabUtility.IsAnyPrefabInstanceRoot(piece.gameObject))
                    rows.Add(group.name+"|"+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(piece.gameObject)+"|"+
                        piece.localPosition.ToString("F5",culture)+"|"+piece.localRotation.ToString("F5",culture)+"|"+piece.localScale.ToString("F5",culture));
            return Hash128.Compute(string.Join("\n",rows)).ToString();
        }

        sealed class Preservation
        {
            readonly List<Action> checks=new List<Action>();
            public Preservation(Scene scene,Transform parent,Transform existing)
            {
                var roots=scene.GetRootGameObjects().Select(g=>g.GetInstanceID()).ToArray();
                checks.Add(()=>{ if(!roots.SequenceEqual(scene.GetRootGameObjects().Select(g=>g.GetInstanceID()))) throw new InvalidOperationException("Cambio de raices ajeno a esta etapa."); });
                foreach(var root in scene.GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var original=t; var p=t.parent; var position=t.localPosition; var rotation=t.localRotation; var scale=t.localScale;
                    int sibling=t.GetSiblingIndex(); string name=t.name; bool active=t.gameObject.activeSelf;
                    int layer=t.gameObject.layer; string tag=t.tag; var flags=GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                    var children=t.Cast<Transform>().Select(v=>v.GetInstanceID()).ToArray();
                    var components=t.GetComponents<Component>().Select(v=>v?v.GetInstanceID():0).ToArray();
                    checks.Add(()=>
                    {
                        if(!original || original.parent!=p || original.localPosition!=position || original.localRotation!=rotation ||
                           original.localScale!=scale || original.GetSiblingIndex()!=sibling || original.gameObject.activeSelf!=active ||
                           original.gameObject.layer!=layer || original.tag!=tag || GameObjectUtility.GetStaticEditorFlags(original.gameObject)!=flags ||
                           (original.name!=name && !(original==existing && original.name==StageName)))
                            throw new InvalidOperationException("Objeto previo alterado: "+name);
                        var now=original.Cast<Transform>().Where(v=>!(original==parent && !existing && v.name==StageName)).Select(v=>v.GetInstanceID());
                        if(!children.SequenceEqual(now) || !components.SequenceEqual(original.GetComponents<Component>().Select(v=>v?v.GetInstanceID():0)))
                            throw new InvalidOperationException("Jerarquia/componentes previos alterados: "+name);
                    });
                    foreach(var component in t.GetComponents<Component>())
                    {
                        if(!component || component is Transform) continue;
                        var captured=component; string json=EditorJsonUtility.ToJson(component);
                        checks.Add(()=>{ if(!captured || EditorJsonUtility.ToJson(captured)!=json) throw new InvalidOperationException("Componente previo alterado: "+name); });
                    }
                }
            }
            public void Verify() { foreach(var check in checks) check(); }
        }

        static Source Inspect()
        {
            var scene=SceneManager.GetActiveScene();
            if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage()||scene.path!=Target)
                throw new InvalidOperationException("Activa Dungeon_Isla_del_Sol en el escenario principal.");
            var roots=scene.GetRootGameObjects().Where(g=>g.name==Root).ToArray();
            if(roots.Length!=1||!roots[0].transform.Find("__ROM_SolarStructure_12D9D67A_v1")) throw new InvalidOperationException("Base ausente o duplicada; no se reconstruira.");
            var root=roots[0].transform; var zone=root.Find(Zone);
            if(!zone||(zone.position-Center).sqrMagnitude>.0001f||(zone.lossyScale-Vector3.one).sqrMagnitude>.0001f||
                Quaternion.Angle(zone.rotation,Quaternion.identity)>.01f||root.position.sqrMagnitude>.0001f||
                (root.lossyScale-Vector3.one).sqrMagnitude>.0001f) throw new InvalidOperationException("Contrato de posicion/escala del Claustro alterado.");
            string[] routes={"01-02 -","02-03 -","03-04 -","03-05 -","04-05 -","03-06 -","05-07 -","06-07 -","01-06 -"};
            foreach(string prefix in routes) if(root.Cast<Transform>().Count(t=>t.name.StartsWith(prefix,StringComparison.Ordinal))!=1)
                throw new InvalidOperationException("Recorrido ausente o duplicado: "+prefix);
            var source=new Source{Root=root,Zone=zone,Scene=scene}; Physics.SyncTransforms();
            var floor=zone.Cast<Transform>().Single(t=>t.name.StartsWith("Suelo modular",StringComparison.Ordinal));
            foreach(var renderer in floor.GetComponentsInChildren<MeshRenderer>())
            {
                var b=renderer.bounds;
                if(renderer.enabled&&Mathf.Abs(b.size.x-6)<.05f&&Mathf.Abs(b.size.z-6)<.05f&&Mathf.Abs(b.max.y-Center.y)<.05f)
                    if(!source.Ground.Any(s=>(s.Bounds.center-b.center).sqrMagnitude<.001f)) source.Ground.Add(new Surface{Bounds=b,Height=0});
            }
            if(source.Ground.Count<100) throw new InvalidOperationException("No se reconocen las superficies originales del Claustro.");
            foreach(var collider in zone.GetComponentsInChildren<Collider>())
                if(collider.enabled&&!collider.isTrigger&&collider.bounds.max.y>Center.y+.3f) source.Obstacles.Add(collider.bounds);
            source.Routes.AddRange(root.Cast<Transform>().Where(t=>t.name.StartsWith("03-05 -",StringComparison.Ordinal)||t.name.StartsWith("04-05 -",StringComparison.Ordinal)||t.name.StartsWith("05-07 -",StringComparison.Ordinal)));
            var landmarks=root.Find("Arquitectura monumental - fuera de los pasos");
            if(landmarks) foreach(Transform landmark in landmarks)
            {
                Vector3 offset=landmark.position-Center;
                if(Mathf.Abs(offset.x)>54||offset.z< -48||offset.z>48||Mathf.Abs(offset.y)>.1f) continue;
                source.Landmarks.Add(landmark);
                foreach(var r in landmark.GetComponentsInChildren<MeshRenderer>())
                    if(r.enabled&&!r.name.EndsWith("_Shadow",StringComparison.OrdinalIgnoreCase)) source.Obstacles.Add(r.bounds);
            }
            return source;
        }
        static Bounds Reserve(float x,float z,float width,float depth,float bottom=.15f,float height=5.5f)
            =>new Bounds(Center+new Vector3(x,bottom+height/2,z),new Vector3(width,height,depth));
        static readonly Bounds[] MainClearance=
        {
            Reserve(0,-24,120,12), // Untouched lateral ports (-54,-24) and (+54,-24), 12m bridges.
            Reserve(36,-39,28,28), // Actual 24m south arrival at (+36,-48).
            Reserve(18,-30,64,12),
            Reserve(0,-17,26,34),  // Ceremonial approach, 26m clear.
            Reserve(0,-4,28,24)    // Broad ground-level ceremonial court, not an obstacle field.
        };
        static readonly Bounds[] StairClearance=
        {
            Reserve(-36,-14.65f,12,6.7f,0.15f,9.5f),Reserve(36,-14.65f,12,6.7f,0.15f,9.5f),
            Reserve(-30,3.15f,12,6.6f,4.15f,9.5f),Reserve(30,3.15f,12,6.6f,4.15f,9.5f),
            Reserve(-27,21,6,6,4.15f,9.5f),Reserve(27,21,6,6,4.15f,9.5f),
            Reserve(-27,12,6,12,8.15f,5.5f),Reserve(27,12,6,12,8.15f,5.5f),
            Reserve(-27,27,4,4,4.15f,5.5f),Reserve(27,27,4,4,4.15f,5.5f)
        };
        static bool Inside(Bounds b)
        {
            foreach(float x in new[]{b.min.x,b.max.x}) foreach(float z in new[]{b.min.z,b.max.z})
            { float nx=(x-Center.x)/54, nz=(z-Center.z+48)/96; if(nz<-.001f||nx*nx+nz*nz>1.003f) return false; }
            return true;
        }
        static Bounds Inset(Bounds b) { b.Expand(-.04f); return b; }
        static bool Supported(Plan plan,Bounds b,float level)
        {
            foreach(float x in new[]{b.min.x+.04f,b.center.x,b.max.x-.04f})
                foreach(float z in new[]{b.min.z+.04f,b.center.z,b.max.z-.04f})
                    if(!plan.Surfaces.Any(s=>Mathf.Abs(s.Height-level)<.035f&&x>=s.Bounds.min.x-.01f&&x<=s.Bounds.max.x+.01f&&z>=s.Bounds.min.z-.01f&&z<=s.Bounds.max.z+.01f)) return false;
            return true;
        }
        static bool OverlapXZ(Bounds a,Bounds b)=>a.min.x<b.max.x-.04f&&a.max.x>b.min.x+.04f&&a.min.z<b.max.z-.04f&&a.max.z>b.min.z+.04f;

        static Plan Build(Source source,Transform zone,Dictionary<string,Asset> assets,bool undo=false)
        {
            var root=new GameObject(StageName); root.transform.SetParent(zone,false);
            if(undo) Undo.RegisterCreatedObjectUndo(root,"Crear Claustro aprobado");
            new GameObject("__ROM_EclipseCloister_Architecture_v1").transform.SetParent(root.transform,false);
            var plan=new Plan{Stage=root.transform,Source=source}; plan.Surfaces.AddRange(source.Ground);
            var groups=new Dictionary<string,Transform>();
            var gold=AssetDatabase.LoadAssetAtPath<Material>(ST+"Content/Meshes/Main/Materials/Roof_Gold.mat");
            var stone=assets["Wall"].Prefab.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m);
            GameObject P(string group,string key,float x,float z,float y=0,float yaw=0,bool required=false,bool bridge=false)
            {
                var asset=assets[key]; var q=Quaternion.Euler(0,yaw,0);
                var b=TransformBounds(asset.Bounds,Matrix4x4.Rotate(q));
                var position=Center+new Vector3(x-b.center.x,y-b.min.y,z-b.center.z); b.center+=position;
                bool surface=key=="Base"||key=="Stair"||key=="Paving";
                string reason=!Inside(b)?"limite de plataforma":MainClearance.Any(r=>r.Intersects(Inset(b)))?"acceso original":null;
                if(reason==null&&source.Obstacles.Any(o=>o.Intersects(Inset(b)))) reason="estructura original";
                if(reason==null&&!surface&&StairClearance.Any(r=>r.Intersects(Inset(b)))) reason="escalera/pasillo";
                float support=key=="Paving"?0:y;
                if(reason==null&&!bridge&&!Supported(plan,b,support)) reason="sin apoyo completo";
                if(reason==null&&!surface&&!bridge&&key!="Sun"&&plan.Surfaces.Any(s=>s.Height>y+.1f&&s.Height<y+asset.Bounds.size.y&&OverlapXZ(b,s.Bounds))) reason="terraza elevada";
                if(reason==null&&!surface&&key!="Sun")
                    foreach(var previous in plan.Pieces.Where(p=>p.Key!="Base"&&p.Key!="Paving"&&p.Key!="Stair"))
                    {
                        if(!Inset(b).Intersects(Inset(previous.Bounds))) continue;
                        if(key=="Wall"&&previous.Key=="Wall") continue; // Intentional joined masonry courses.
                        var overlap=Vector3.Min(b.max,previous.Bounds.max)-Vector3.Max(b.min,previous.Bounds.min);
                        if(Mathf.Min(overlap.x,Mathf.Min(overlap.y,overlap.z))<.16f) continue;
                        reason="solape con "+previous.Key; break;
                    }
                if(reason!=null)
                {
                    if(required) throw new InvalidOperationException("Pieza necesaria: "+group+" / "+key+" ("+x+","+z+"): "+reason);
                    plan.Skips.Add(group+" / "+key+": "+reason); return null;
                }
                if(!groups.TryGetValue(group,out var parent))
                { var go=new GameObject(group); go.transform.SetParent(root.transform,false); groups[group]=parent=go.transform; }
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset.Prefab,zone.gameObject.scene);
                instance.transform.SetParent(parent,true); instance.transform.SetPositionAndRotation(position,q*asset.Prefab.transform.localRotation);
                instance.transform.localScale=asset.Prefab.transform.localScale; DisableImportedBehaviour(instance);
                if(key=="Wall"&&!instance.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))
                    foreach(var filter in instance.GetComponentsInChildren<MeshFilter>())
                        if(filter.sharedMesh&&filter.GetComponent<MeshRenderer>()&&filter.GetComponent<MeshRenderer>().enabled) filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
                if((key=="Sun"||key=="Trim")&&gold)
                    foreach(var r in instance.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterials=r.sharedMaterials.Select(m=>gold).ToArray();
                if(key=="LibraryColumn"&&stone)
                    foreach(var r in instance.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterials=r.sharedMaterials.Select(m=>stone).ToArray();
                Physics.SyncTransforms();
                if(instance.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger&&
                    (!Inside(c.bounds)||MainClearance.Any(r=>r.Intersects(Inset(c.bounds)))||(!surface&&StairClearance.Any(r=>r.Intersects(Inset(c.bounds)))))))
                {
                    string collisionDetails=string.Join("; ",instance.GetComponentsInChildren<Collider>().Select(c=>c.bounds.ToString()));
                    Object.DestroyImmediate(instance);
                    if(required) throw new InvalidOperationException("Collider invade circulacion: "+key+" ("+x+","+z+") / "+collisionDetails);
                    plan.Skips.Add(group+" / collider "+key); return null;
                }
                if((key=="Base"||key=="Stair")&&!instance.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger)) throw new InvalidOperationException("Superficie sin collider: "+key);
                plan.Pieces.Add(new Piece{Key=key,Bounds=b,Object=instance});
                plan.Counts[group]=plan.Counts.TryGetValue(group,out int count)?count+1:1;
                if(key=="Base") plan.Surfaces.Add(new Surface{Bounds=b,Height=y+2});
                if(key=="Wall") plan.Surfaces.Add(new Surface{Bounds=b,Height=y+asset.Bounds.size.y});
                return instance;
            }
            // Construct exclusively over real existing floor modules, forming a stepped semicircular ambulatory.
            foreach(var tile in source.Ground.OrderBy(s=>s.Bounds.center.x).ThenBy(s=>s.Bounds.center.z))
            {
                Vector3 p=tile.Bounds.center-Center; float outer=p.x*p.x/(44*44)+(p.z+12)*(p.z+12)/(46*46);
                float inner=p.x*p.x/(29*29)+(p.z+12)*(p.z+12)/(30*30);
                if(p.z< -9.01f||outer>1.03f||inner<1) continue;
                if(!P("01 - Galerias semicirculares +4","Base",p.x,p.z)) continue;
                P("01 - Galerias semicirculares +4","Base",p.x,p.z,2,0,true);
            }
            foreach(float sign in new[]{-1f,1f})
            {
                P("01 - Galerias semicirculares +4","Base",sign*27,27,0,0,true);
                P("01 - Galerias semicirculares +4","Base",sign*27,27,2,0,true);
                foreach(float x in new[]{27f,33f}) foreach(float z in new[]{9f,15f})
                    for(int level=0;level<2;level++) P("02 - Terrazas ceremoniales +8","Base",sign*x,z,4+level*2,0,true);
                foreach(float x in new[]{33f,39f}) P("03 - Escalinatas conectadas","Stair",sign*x,-18+assets["Stair"].Bounds.size.x/2+.22f,0,90,true);
                foreach(float x in new[]{27f,33f}) P("03 - Escalinatas conectadas","Stair",sign*x,assets["Stair"].Bounds.size.x/2,4,90,true);
                P("03 - Escalinatas conectadas","Stair",sign*27,24-assets["Stair"].Bounds.size.x/2,4,270,true);
            }
            // A genuine 32m ceremonial opening; native masonry lintels are supported by granite piers.
            float portalHeight=assets["Pillar"].Bounds.size.y;
            foreach(float x in new[]{-18f,18f}) P("04 - Portico monumental","Pillar",x,-9,0,0,true);
            foreach(float x in new[]{-15f,-9f,-3f,3f,9f,15f}) P("04 - Portico monumental","Base",x,-9,portalHeight,0,true,true);
            P("04 - Portico monumental","Arch",0,-9,portalHeight+2);
            P("04 - Portico monumental","Sun",0,-12,portalHeight+1,0,false,true);
            float alongX=assets["Wall"].Bounds.size.x>=assets["Wall"].Bounds.size.z?0:90;
            foreach(float sign in new[]{-1f,1f}) for(int level=0;level<Mathf.CeilToInt(14/assets["Wall"].Bounds.size.y);level++)
                P("04 - Portico monumental","Wall",sign*24.3f,-9,level*assets["Wall"].Bounds.size.y,alongX);

            // Curved temple enclosure, upper pavilions and inner colonnade share actual supported gallery surfaces.
            for(int i=0;i<=12;i++)
            {
                float a=i*Mathf.PI/12; float x=Mathf.Cos(a)*39,z=-12+Mathf.Sin(a)*40;
                float yaw=alongX+Mathf.Atan2(-40*Mathf.Cos(a),-39*Mathf.Sin(a))*Mathf.Rad2Deg;
                var footprint=TransformBounds(assets["Wall"].Bounds,Matrix4x4.Rotate(Quaternion.Euler(0,yaw,0)));
                footprint.center=Center+new Vector3(x,0,z);
                float level=Supported(plan,footprint,8)?8:4;
                for(int course=0;course<3;course++) P("05 - Templo semicircular y ruinas","Wall",x,z,level+course*assets["Wall"].Bounds.size.y,yaw);
            }
            var columns=new List<Vector3>();
            for(int i=0;i<18;i++)
            {
                float a=(5+i*10)*Mathf.Deg2Rad; float x=Mathf.Cos(a)*32,z=-12+Mathf.Sin(a)*31;
                if(P("06 - Columnata del claustro","LibraryColumn",x,z,4)) columns.Add(new Vector3(x,4,z));
            }
            for(int i=1;i<columns.Count;i++)
            {
                var a=columns[i-1]; var b=columns[i]; float distance=Vector3.Distance(a,b); if(distance>6.4f) continue;
                var middle=(a+b)/2; var tangent=b-a;
                float yaw=Mathf.Atan2(-tangent.z,tangent.x)*Mathf.Rad2Deg;
                P("06 - Columnata del claustro","Base",middle.x,middle.z,4+assets["LibraryColumn"].Bounds.size.y,yaw,false,true);
                P("06 - Columnata del claustro","Trim",middle.x,middle.z,6+assets["LibraryColumn"].Bounds.size.y,yaw,false,true);
            }
            foreach(float sign in new[]{-1f,1f})
            {
                foreach(float z in new[]{9f,15f}) P("07 - Altares y pabellones","Pillar",sign*33,z,8);
                P("07 - Altares y pabellones","Altar",sign*33,12,8,sign>0?90:270);
                P("08 - Ruinas asimetricas","BrokenColumn",sign*43,-36);
                P("08 - Ruinas asimetricas","BrokenArch",sign*42,-12);
                P("08 - Ruinas asimetricas","Statue",sign*43,-38);
            }
            P("07 - Altares y pabellones","Altar",0,21,4);
            P("08 - Ruinas asimetricas","BrokenWall",-42,-3);
            P("08 - Ruinas asimetricas","BrokenColumn",-18,-38);
            // Native railings guard exposed +4/+8 edges; protected stair mouths stay open.
            var walkable=plan.Surfaces.Where(s=>(Mathf.Abs(s.Height-4)<.01f||Mathf.Abs(s.Height-8)<.01f)&&
                Mathf.Abs(s.Bounds.size.x-6)<.01f&&Mathf.Abs(s.Bounds.size.z-6)<.01f).Where(s=>
                !plan.Pieces.Any(p=>p.Key=="Base"&&Mathf.Abs(p.Bounds.max.y-(Center.y+8))<.02f&&s.Height<8&&
                    p.Bounds.min.x<=s.Bounds.center.x&&p.Bounds.max.x>=s.Bounds.center.x&&p.Bounds.min.z<=s.Bounds.center.z&&p.Bounds.max.z>=s.Bounds.center.z)).ToArray();
            foreach(var cell in walkable) foreach(var normal in new[]{Vector3.left,Vector3.right,Vector3.back,Vector3.forward})
            {
                Vector3 next=cell.Bounds.center+normal*6;
                if(walkable.Any(other=>Mathf.Abs(other.Height-cell.Height)<.01f&&Mathf.Abs(other.Bounds.center.x-next.x)<.02f&&Mathf.Abs(other.Bounds.center.z-next.z)<.02f)) continue;
                float yaw=(assets["Rail"].Bounds.size.x>=assets["Rail"].Bounds.size.z?0:90)+(Mathf.Abs(normal.x)>.5f?90:0);
                var rb=TransformBounds(assets["Rail"].Bounds,Matrix4x4.Rotate(Quaternion.Euler(0,yaw,0)));
                float thickness=Mathf.Abs(normal.x)>.5f?rb.size.x:rb.size.z;
                var point=cell.Bounds.center-Center+normal*(3-thickness/2-.03f);
                P("10 - Bordes y barandas","Rail",point.x,point.z,cell.Height,yaw);
            }

            // Original walkable deck remains intact beneath a thin native ceremonial pavement.
            foreach(var tile in source.Ground)
            {
                if(plan.Pieces.Any(piece=>piece.Key=="Base"&&piece.Bounds.min.y<=Center.y+.1f&&OverlapXZ(piece.Bounds,tile.Bounds))) continue;
                var p=tile.Bounds.center-Center;
                P("09 - Pavimento del patio","Paving",p.x,p.z,.025f-assets["Paving"].Bounds.size.y);
            }
            foreach(string group in new[]{"01 - Galerias semicirculares +4","02 - Terrazas ceremoniales +8","03 - Escalinatas conectadas","04 - Portico monumental","06 - Columnata del claustro"})
                if(!plan.Counts.TryGetValue(group,out int count)||count<4) throw new InvalidOperationException("Composicion insuficiente: "+group);
            if(!plan.Pieces.Any(p=>p.Key=="Altar")) throw new InvalidOperationException("No se pudo colocar un altar apoyado y accesible.");
            Debug.Log("CLAUSTRO_COMPONENTES: "+string.Join(", ",plan.Pieces.GroupBy(p=>p.Key).Select(g=>g.Key+"="+g.Count())));
            ValidatePlan(plan);
            Debug.Log("CLAUSTRO_PREVIEW: "+plan.Pieces.Count+" prefabs nativos. Patio abierto, templo semicircular, galerias +4m, terrazas +8m, "+plan.Pieces.Count(p=>p.Key=="Stair")+" escaleras y altares. Omisiones seguras: "+plan.Skips.Count+". "+string.Join("; ",plan.Skips.Take(12)));
            return plan;
        }
        static void ValidatePlan(Plan plan)
        {
            Physics.SyncTransforms();
            foreach(var piece in plan.Pieces)
            {
                if(!piece.Object) throw new InvalidOperationException("Pieza perdida.");
                var t=piece.Object.transform; var original=PrefabUtility.GetCorrespondingObjectFromSource(piece.Object);
                if(!original||(t.localScale-original.transform.localScale).sqrMagnitude>.0001f) throw new InvalidOperationException("Escala original alterada: "+t.name);
                foreach(var child in t.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject)>0) throw new InvalidOperationException("Script faltante: "+child.name);
                foreach(var c in t.GetComponentsInChildren<Collider>())
                    if(c.enabled&&!c.isTrigger&&(!Inside(c.bounds)||MainClearance.Any(r=>r.Intersects(Inset(c.bounds)))||
                        (piece.Key!="Base"&&piece.Key!="Stair"&&piece.Key!="Paving"&&StairClearance.Any(r=>r.Intersects(Inset(c.bounds))))))
                        throw new InvalidOperationException("Collider bloquea un paso: "+c.name);
                foreach(var r in t.GetComponentsInChildren<MeshRenderer>())
                    if(r.sharedMaterials.Any(m=>!m||!m.shader)) throw new InvalidOperationException("Material faltante: "+r.name);
            }
            if(plan.Stage.GetComponentsInChildren<Light>().Any(l=>l.enabled)||plan.Stage.GetComponentsInChildren<ParticleSystem>().Any(p=>p.isPlaying)) throw new InvalidOperationException("No se permiten efectos ni luces en esta arquitectura.");
            Debug.Log("CLAUSTRO_VALIDACION_OK: plataforma D y apoyos reales conservados, tres accesos libres, escaleras con colliders, patio y pasillos protegidos; sin modificar escenas originales. Validacion geometrica, no NavMesh/gameplay.");
        }

        static Dictionary<string,Asset> LoadAssets()
        {
            var result=new Dictionary<string,Asset>();
            foreach(var path in Paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path.Value);
                if(!prefab) throw new InvalidOperationException("Falta prefab original: "+path.Value);
                bool found=false; Bounds visible=default;
                var normalize=Matrix4x4.TRS(Vector3.zero,prefab.transform.localRotation,prefab.transform.localScale)*prefab.transform.worldToLocalMatrix;
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer=filter.GetComponent<MeshRenderer>();
                    if(!renderer || !renderer.enabled || !filter.sharedMesh || renderer.name.EndsWith("_Shadow",StringComparison.OrdinalIgnoreCase)) continue;
                    if(filter.GetComponents<MonoBehaviour>().Any(c=>c && c.GetType().FullName=="SunTemple.DisableRenderer")) continue;
                    bool active=true;
                    for(var t=filter.transform;t;t=t.parent) { if(!t.gameObject.activeSelf) active=false; if(t==prefab.transform) break; }
                    if(!active) continue;
                    var bounds=TransformBounds(filter.sharedMesh.bounds,normalize*filter.transform.localToWorldMatrix);
                    if(found) visible.Encapsulate(bounds); else { visible=bounds; found=true; }
                }
                if(!found) throw new InvalidOperationException("Prefab sin malla visible: "+path.Value);
                result[path.Key]=new Asset{Prefab=prefab,Bounds=visible};
            }
            if((result["Base"].Bounds.size-new Vector3(6,2,6)).sqrMagnitude>.01f ||
               Mathf.Abs(result["Stair"].Bounds.size.y-4)>.01f || Mathf.Abs(result["Stair"].Bounds.size.z-6)>.01f ||
               result["Stair"].Bounds.size.x<6 || result["Stair"].Bounds.size.x>6.3f)
                throw new InvalidOperationException("Dimensiones nativas: Base="+result["Base"].Bounds+"; Stair="+result["Stair"].Bounds);
            Debug.Log("CLAUSTRO_MODULES: Wall="+result["Wall"].Bounds.size+"; LibraryColumn="+result["LibraryColumn"].Bounds.size);
            return result;
        }
        static Bounds TransformBounds(Bounds b,Matrix4x4 matrix)
        {
            var result=new Bounds(matrix.MultiplyPoint3x4(b.center),Vector3.zero);
            for(int i=0;i<8;i++) result.Encapsulate(matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,
                new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
            return result;
        }
        static void DisableImportedBehaviour(GameObject root)
        {
            // Legacy shadow-only helper meshes may have empty material slots in the source pack.
            // Repair only preview-instance slots and hide these helpers; source prefabs stay read-only.
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true);
            var fallback=renderers.SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m);
            foreach(var renderer in renderers)
            {
                if(renderer.name.EndsWith("_Shadow",StringComparison.OrdinalIgnoreCase)) renderer.enabled=false;
                if(fallback && renderer.sharedMaterials.Any(m=>!m)) renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m?m:fallback).ToArray();
                if(fallback && renderer.sharedMaterials.Length==0) renderer.sharedMaterials=new[]{fallback};
            }
            foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true)) if(script) script.enabled=false;
            foreach(var audio in root.GetComponentsInChildren<AudioSource>(true)) audio.enabled=false;
            foreach(var light in root.GetComponentsInChildren<Light>(true)) light.enabled=false;
            foreach(var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled=false;
            foreach(var particle in root.GetComponentsInChildren<ParticleSystem>(true))
            { var main=particle.main; main.playOnAwake=false; particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
            foreach(var body in root.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic=true; body.useGravity=false; }
        }


        sealed class CloisterPreview:PreviewSceneStage
        {
            public Source Source; public Dictionary<string,Asset> Assets; public Plan Plan;
            protected override GUIContent CreateHeaderContent()=>new GUIContent("Claustro del Eclipse - complejo ceremonial (temporal)");
            protected override bool OnOpenStage()
            {
                if(Source==null||!Source.Zone||Assets==null||!base.OnOpenStage()) return false;
                try
                {
                    Scene temp=scene; temp.name="TEMP_ClaustroDelEclipse_Architecture";
                    var holder=new GameObject("Contexto temporal"); SceneManager.MoveGameObjectToScene(holder,scene); holder.SetActive(false);
                    var root=new GameObject(Root); root.transform.SetParent(holder.transform,false);
                    var zone=Object.Instantiate(Source.Zone.gameObject,root.transform); zone.name=Zone;
                    zone.transform.SetPositionAndRotation(Source.Zone.position,Source.Zone.rotation);
                    var old=zone.transform.Find(StageName); if(old) Object.DestroyImmediate(old.gameObject);
                    foreach(var route in Source.Routes)
                    { var copy=Object.Instantiate(route.gameObject,root.transform); copy.name=route.name; copy.transform.SetPositionAndRotation(route.position,route.rotation); }
                    foreach(var landmark in Source.Landmarks)
                    { var copy=Object.Instantiate(landmark.gameObject,root.transform); copy.name=landmark.name; copy.transform.SetPositionAndRotation(landmark.position,landmark.rotation); }
                    DisableImportedBehaviour(root); holder.SetActive(true);
                    Plan=Build(Source,zone.transform,Assets);
                    var rig=new GameObject("Luz neutra de inspeccion - solo temporal"); SceneManager.MoveGameObjectToScene(rig,scene);
                    rig.transform.rotation=Quaternion.Euler(48,-32,0); var light=rig.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.1f; light.shadows=LightShadows.None;
                    EditorApplication.delayCall+=()=>
                    {
                        if(!this||StageUtility.GetCurrentStage()!=this) return;
                        var view=SceneView.lastActiveSceneView;
                        if(view) { view.sceneLighting=true; view.sceneViewState.showImageEffects=false; view.orthographic=false;
                            view.LookAtDirect(Center+Vector3.up*10,Quaternion.Euler(48,-25,0),90); view.Repaint(); }
                        Selection.activeGameObject=null;
                    };
                    Source=null; Assets=null;
                    Debug.Log("Previsualizacion independiente del Claustro lista. Copias de zona, torre y tres conexiones; no se integro ni guardo la escena original.");
                    return true;
                }
                catch(Exception e) { Debug.LogException(e); base.OnCloseStage(); return false; }
            }
        }
    }
}