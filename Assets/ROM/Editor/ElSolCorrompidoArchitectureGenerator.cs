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
    // Independent architecture pass. Only the explicit approved-stage integration command may save the original.
    public static class ElSolCorrompidoArchitectureGenerator
    {
        const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Root="Dungeon_Ciudad_del_Sol_Generated";
        const string Zone="07 - El Sol Corrompido";
        const string StageName="ElSolCorrompido_Architecture";
        const string Menu="ROM/Dungeon Ciudad del Sol/El Sol Corrompido/";
        const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string ST="Assets/Standard Assets/Sun_Temple/Prefabs/";
        const string EG="Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/";
        static readonly Vector3 Center=new Vector3(180,36,156);
        static readonly Dictionary<string,string> Paths=new Dictionary<string,string>
        {
            {"Base",ET+"Pits and Platforms/Platform_Base_01.prefab"},
            {"Stair",ET+"Staircases/Staircase_01.prefab"},
            {"LibraryColumn",ST+"Modules_Temple/Bld_Temple_Int_Column_A.prefab"},
            {"GreatAltar",EG+"SM_Altar.prefab"},
            {"Disk",EG+"SM_SunDisk2.prefab"},
            {"WideArch",ET+"Bases/Arch_Wide_01.prefab"},
            {"Column",ET+"Columns/Column_01.prefab"},
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

        [MenuItem(Menu+"1. Previsualizar complejo ceremonial (copia temporal) %#&k")]
        public static void Preview()
        {
            if(!Ready) return;
            try
            {
                ClosePreview(); var source=Inspect(); var assets=LoadAssets();
                var preview=ScriptableObject.CreateInstance<SolarArenaPreview>(); preview.Source=source; preview.Assets=assets;
                StageUtility.GoToStage(preview,true);
            }
            catch(Exception e) { Debug.LogException(e); }
        }
        [MenuItem(Menu+"2. Validar accesos, escaleras y colliders")]
        public static void Validate()
        {
            try
            {
                var preview=StageUtility.GetCurrentStage() as SolarArenaPreview;
                if(!preview||preview.Plan==null) throw new InvalidOperationException("Abre primero la previsualizacion del El Sol Corrompido.");
                ValidatePlan(preview.Plan); ValidateWalks(preview.Plan,preview.Plan.Stage.gameObject.scene);
            }
            catch(Exception e) { Debug.LogException(e); }
        }
        [MenuItem(Menu+"3. Cerrar previsualizacion sin guardar")]
        public static void ClosePreview()
        { if(Ready&&StageUtility.GetCurrentStage() is SolarArenaPreview) StageUtility.GoToMainStage(); }


        [MenuItem(Menu+"4. Integrar El Sol Corrompido aprobado y guardar tras validar %#&l")]
        public static void IntegrateApproved()
        {
            if(!Ready) return;
            int undo=-1; bool saved=false;
            try
            {
                var preview=StageUtility.GetCurrentStage() as SolarArenaPreview;
                if(preview&&preview.Plan==null)
                {
                    Preview();
                    var rebuilt=StageUtility.GetCurrentStage() as SolarArenaPreview;
                    if(!rebuilt||rebuilt.Plan==null) throw new InvalidOperationException("No se pudo recuperar la previsualizacion aprobada.");
                    EditorApplication.delayCall+=IntegrateApproved;
                    return;
                }
                string approved=preview && preview.Plan!=null?Layout(preview.Plan.Stage):null;
                ClosePreview();
                var source=Inspect();
                if(source.Scene.isDirty) throw new InvalidOperationException("La escena tiene cambios previos sin guardar; no se guardaran con esta etapa.");
                string[] stages={"01 - Entrada Portico Solar/PorticoSolar_Architecture","02 - Jardines Dorados/JardinesDorados_Architecture","03 - Plaza del Eclipse/PlazaDelEclipse_Architecture","04 - Archivos Solares/ArchivosSolares_Architecture","05 - Claustro del Eclipse/ClaustroDelEclipse_Architecture","06 - Santuario Profano/SantuarioProfano_Architecture"};
                foreach(var path in stages) if(!source.Root.Find(path)) throw new InvalidOperationException("Falta arquitectura aprobada: "+path);
                var matches=source.Zone.Cast<Transform>().Where(t=>t.name==StageName).ToArray();
                if(matches.Length>1) throw new InvalidOperationException("El Sol Corrompido duplicado.");
                var existing=matches.FirstOrDefault();
                if(existing&&!existing.Find("__ROM_CorruptedSun_Architecture_v1")) throw new InvalidOperationException("Grupo ajeno; no se reemplazara.");
                var preservation=new Preservation(source.Scene,source.Zone,existing);
                var assets=LoadAssets();
                Undo.IncrementCurrentGroup(); undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Integrar El Sol Corrompido aprobado");
                Plan plan;
                if(!existing) plan=Build(source,source.Zone,assets,true);
                else
                {
                    plan=new Plan{Stage=existing,Source=source}; plan.Surfaces.AddRange(source.Ground);
                    foreach(Transform group in existing) foreach(Transform t in group)
                    {
                        if(!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
                        string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                        string key=Paths.Single(k=>k.Value==path).Key;
                        var q=t.rotation*Quaternion.Inverse(assets[key].Prefab.transform.localRotation);
                        var bounds=TransformBounds(assets[key].Bounds,Matrix4x4.TRS(t.position,q,Vector3.one));
                        plan.Pieces.Add(new Piece{Key=key,Object=t.gameObject,Bounds=bounds});
                        if(key=="Base"||key=="Wall") plan.Surfaces.Add(new Surface{Bounds=bounds,Height=bounds.max.y-Center.y});
                        if(key=="GreatAltar") { var top=bounds; top.Expand(new Vector3(-2,0,-2)); plan.Surfaces.Add(new Surface{Bounds=top,Height=bounds.max.y-Center.y}); }
                    }
                }
                if(plan.Pieces.Count!=237||plan.Pieces.Count(p=>p.Key=="Stair")!=11) throw new InvalidOperationException("La composicion aprobada debe tener 237 prefabs y once escaleras.");
                ValidatePlan(plan); ValidateWalks(plan,source.Scene);
                if(approved!=null&&approved!=Layout(plan.Stage)) throw new InvalidOperationException("Distribucion distinta de la previsualizacion aprobada.");
                preservation.Verify();
                if(source.Scene.GetRootGameObjects().Any(g=>g.name=="Dungeon_Void_Atmosphere")) throw new InvalidOperationException("Vacio inesperado.");
                if(source.Scene.path!=Target) throw new InvalidOperationException("Escena destino incorrecta.");
                EditorSceneManager.MarkSceneDirty(source.Scene);
                if(!EditorSceneManager.SaveScene(source.Scene,Target,false)) throw new InvalidOperationException("Error al guardar.");
                saved=true; Undo.CollapseUndoOperations(undo);
                Debug.Log("SOL_CORROMPIDO_INTEGRADO_GUARDADO: 237 prefabs, once escaleras, dos accesos libres. Siete arquitecturas aprobadas presentes; objetos previos, seis zonas, nueve caminos y colliders intactos. Sin luces nuevas ni Vacio.");
            }
            catch(Exception e)
            {
                if(!saved&&undo>=0) Undo.RevertAllDownToGroup(undo);
                Debug.LogError("SOL_CORROMPIDO_INTEGRACION_DETENIDA: "+e.Message);
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

        static void ValidateWalks(Plan plan,Scene scene)
        {
            Physics.SyncTransforms();
            var colliders=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger).ToArray();
            var hits=new Collider[64];
            void Walk(Vector3 a,Vector3 b,float width)
            {
                var d=b-a; d.y=0; var side=Vector3.Cross(Vector3.up,d.normalized);
                int n=Mathf.Max(1,Mathf.CeilToInt(d.magnitude/.5f));
                for(int lane=0;lane<4;lane++) for(int i=0;i<=n;i++)
                {
                    var p=Center+Vector3.Lerp(a,b,i/(float)n)+side*((lane-1.5f)*width/4);
                    float y=float.NegativeInfinity;
                    foreach(var c in colliders) if(c.Raycast(new Ray(p+Vector3.up*1.3f,Vector3.down),out var hit,1.9f)&&hit.normal.y>.5f) y=Mathf.Max(y,hit.point.y);
                    if(float.IsNegativeInfinity(y)) throw new InvalidOperationException("Sin apoyo de collider en "+p);
                    int count=scene.GetPhysicsScene().OverlapCapsule(new Vector3(p.x,y+.65f,p.z),new Vector3(p.x,y+1.65f,p.z),.3f,hits,~0,QueryTriggerInteraction.Ignore);
                    if(count>0) throw new InvalidOperationException("Paso bloqueado en "+p+" / "+hits[0].name);
                }
            }
            Walk(new Vector3(-59,0,0),Vector3.zero,8);
            Walk(new Vector3(24,0,-48),new Vector3(12,0,-48),8);
            Walk(new Vector3(12,0,-48),new Vector3(12,0,0),8);
            Walk(new Vector3(-24,0,-24),new Vector3(24,0,-24),4);
            Walk(new Vector3(-24,0,24),new Vector3(24,0,24),4);
            Walk(new Vector3(-27,4,29.55f),new Vector3(27,4,29.55f),3);
            foreach(float sign in new[]{-1f,1f})
            {
                Walk(new Vector3(sign*33,0,30),new Vector3(sign*27,4,30),4);
                foreach(float x in new[]{18f,24f}) Walk(new Vector3(sign*x,4,33),new Vector3(sign*x,8,39),4);
            }
            foreach(float x in new[]{39f,45f}) Walk(new Vector3(x,0,-30),new Vector3(x,4,-24),4);
            Walk(new Vector3(-45,0,12),new Vector3(-45,4,18),4);
            foreach(float x in new[]{-39f,-45f}) Walk(new Vector3(x,0,-12),new Vector3(x,4,-18),4);
            Debug.Log("SOL_CORROMPIDO_WALKS_OK: arena central despejada; dos accesos y once escaleras con cuatro carriles comprobados. No es una prueba de gameplay/NavMesh.");
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
                (root.lossyScale-Vector3.one).sqrMagnitude>.0001f) throw new InvalidOperationException("Contrato de posicion/escala del El Sol Corrompido alterado.");
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
            string[] approved={"01 - Entrada Portico Solar/PorticoSolar_Architecture","02 - Jardines Dorados/JardinesDorados_Architecture","03 - Plaza del Eclipse/PlazaDelEclipse_Architecture","04 - Archivos Solares/ArchivosSolares_Architecture","05 - Claustro del Eclipse/ClaustroDelEclipse_Architecture","06 - Santuario Profano/SantuarioProfano_Architecture"};
            foreach(var path in approved) if(!root.Find(path)) throw new InvalidOperationException("Falta arquitectura aprobada: "+path);
            if(source.Ground.Count<100) throw new InvalidOperationException("No se reconocen las superficies originales del El Sol Corrompido.");
            foreach(var collider in zone.GetComponentsInChildren<Collider>())
                if(collider.enabled&&!collider.isTrigger&&collider.bounds.max.y>Center.y+.3f) source.Obstacles.Add(collider.bounds);
            source.Routes.AddRange(root.Cast<Transform>().Where(t=>t.name.StartsWith("05-07 -",StringComparison.Ordinal)||t.name.StartsWith("06-07 -",StringComparison.Ordinal)));
            var landmarks=root.Find("Arquitectura monumental - fuera de los pasos");
            if(landmarks) foreach(Transform landmark in landmarks)
            {
                Vector3 offset=landmark.position-Center;
                if(Mathf.Abs(offset.x)>60||offset.z< -60||offset.z>60||Mathf.Abs(offset.y)>.1f) continue;
                source.Landmarks.Add(landmark);
                foreach(var r in landmark.GetComponentsInChildren<MeshRenderer>())
                    if(r.enabled&&!r.name.EndsWith("_Shadow",StringComparison.OrdinalIgnoreCase)) source.Obstacles.Add(r.bounds);
            }
            return source;
        }
        static Bounds Reserve(float x,float z,float width,float depth,float bottom=.15f,float height=5.5f)
            =>new Bounds(Center+new Vector3(x,bottom+height/2,z),new Vector3(width,height,depth));
        static readonly Bounds[] MainClearance={
            Reserve(0,0,52,52,.15f,7), // Future boss arena stays completely clear.
            Reserve(-48,0,30,16),    // Existing west arrival (-60,0).
            Reserve(24,-40,16,28),   // Existing southeast arrival (+24,-48).
            Reserve(12,-40,12,28),Reserve(18,-48,24,12) // Continue along bridge before turning around its original rail.
        };
        static readonly Bounds[] StairClearance={
            Reserve(-30,33,7,12,.15f,10),Reserve(30,33,7,12,.15f,10),
            Reserve(-21,36,12,7,4.15f,10),Reserve(21,36,12,7,4.15f,10),
            Reserve(0,29.55f,54,2.85f,4.15f), // Four 0.6m capsules between altar and front parapet.
            Reserve(42,-27,12,7,.15f,10),Reserve(-45,15,6,7,.15f,10),
            Reserve(-42,-15,12,7,.15f,10),
            Reserve(-45,21,4,18,4.15f),Reserve(45,0,4,48,4.15f)
        };
        static bool Inside(Bounds b)
        {
            foreach(float x in new[]{b.min.x,b.max.x}) foreach(float z in new[]{b.min.z,b.max.z})
                if(new Vector2(x-Center.x,z-Center.z).sqrMagnitude>3600.1f) return false;
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
            if(undo) Undo.RegisterCreatedObjectUndo(root,"Crear El Sol Corrompido");
            new GameObject("__ROM_CorruptedSun_Architecture_v1").transform.SetParent(root.transform,false);
            var plan=new Plan{Stage=root.transform,Source=source}; plan.Surfaces.AddRange(source.Ground);
            var groups=new Dictionary<string,Transform>();
            var gold=AssetDatabase.LoadAssetAtPath<Material>(ST+"Content/Meshes/Main/Materials/Roof_Gold.mat");
            var stone=assets["Wall"].Prefab.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m);
            GameObject P(string group,string key,float x,float z,float y=0,float yaw=0,bool required=false,bool bridge=false,bool embedded=false)
            {
                var asset=assets[key]; var q=Quaternion.Euler(0,yaw,0);
                var b=TransformBounds(asset.Bounds,Matrix4x4.Rotate(q));
                var position=Center+new Vector3(x-b.center.x,y-b.min.y,z-b.center.z); b.center+=position;
                bool surface=key=="Base"||key=="Stair"||key=="Paving";
                string reason=!Inside(b)?"limite de plataforma":MainClearance.Any(r=>r.Intersects(Inset(b)))?"acceso original":null;
                if(reason==null&&source.Obstacles.Any(o=>o.Intersects(Inset(b)))) reason="estructura original";
                if(reason==null&&!surface&&StairClearance.Any(r=>r.Intersects(Inset(b)))) reason="escalera/pasillo";
                float support=key=="Paving"?0:y;
                if(reason==null&&!bridge&&key!="Stair"&&!Supported(plan,b,support)) reason="sin apoyo completo";
                if(reason==null&&!surface&&!bridge&&!embedded&&key!="Sun"&&plan.Surfaces.Any(s=>s.Height>y+.1f&&s.Height<y+asset.Bounds.size.y&&OverlapXZ(b,s.Bounds))) reason="terraza elevada";
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
            void Terrace(string group,float x0,float x1,float z0,float z1,int height) {
                for(int y=0;y<height;y+=2) for(float x=x0;x<=x1;x+=6) for(float z=z0;z<=z1;z+=6)
                    P(group,"Base",x,z,y,0,true);
            }
            // North sanctuary: broad podium at +4, two flanking terraces at +8.
            Terrace("01 - Podio del santuario +4",-24,24,30,36,4);
            foreach(float sign in new[]{-1f,1f}) foreach(float x in new[]{18f,24f})
                for(int y=0;y<8;y+=2)
                    P(y<4?"01 - Podio del santuario +4":"02 - Terrazas ceremoniales +8","Base",sign*x,42,y,0,true);
            // Supported perimeter galleries follow the usable cells around the original towers.
            foreach(float x in new[]{-45f,-39f,39f,45f}) foreach(float z in new[]{-21f,-15f,15f,21f,27f})
            {
                if(x<0&&Mathf.Abs(z)==15) continue; // Leave room for full native stair runs beside the west port.
                for(int y=0;y<4;y+=2) P("03 - Galerias perimetrales +4","Base",x,z,y);
            }
            foreach(float x in new[]{39f,45f}) foreach(float z in new[]{-9f,-3f,3f,9f})
                for(int y=0;y<4;y+=2) P("03 - Galerias perimetrales +4","Base",x,z,y);
            // Native 4m stair flights; each landing is checked against actual colliders.
            foreach(float sign in new[]{-1f,1f})
            {
                P("04 - Escalinatas","Stair",sign*30,30,0,sign>0?0:180,true);
                foreach(float x in new[]{18f,24f}) P("04 - Escalinatas","Stair",sign*x,36,4,90,true);
            }
            foreach(float x in new[]{39f,45f}) P("04 - Escalinatas","Stair",x,-27,0,90,true);
            P("04 - Escalinatas","Stair",-45,15,0,90,true);
            foreach(float x in new[]{-39f,-45f}) P("04 - Escalinatas","Stair",x,-15,0,270,true);
            // Massive paired pavilions, each canopy carried directly by two granite piers.
            foreach(float sign in new[]{-1f,1f})
            {
                foreach(float x in new[]{16.9f,25.1f})
                    P("05 - Pabellones monumentales","Pillar",sign*x,43,8,0,true);
                float cap=8+assets["Pillar"].Bounds.size.y;
                foreach(float x in new[]{18f,24f})
                    P("05 - Pabellones monumentales","Base",sign*x,42,cap,0,true,true);
                P("05 - Pabellones monumentales","Arch",sign*21,42,cap+2,90,false,true);
                foreach(float z in new[]{-21f,-3f,21f})
                    P("06 - Columnatas y ruinas","Column",sign*51,z);
                P("06 - Columnatas y ruinas","BrokenArch",sign*42,-33,0,90);
                P("06 - Columnatas y ruinas","BrokenColumn",sign*33,-27);
                P("06 - Columnatas y ruinas","BrokenWall",sign*48,-9,0,90);
            }
            // Existing rear gate and both towers remain original, independent objects.
            foreach(float x in new[]{-6f,6f}) P("07 - Santuario solar destruido","LibraryColumn",x,38,4,0,true);
            float sanctuaryRoof=4+assets["LibraryColumn"].Bounds.size.y;
            foreach(float x in new[]{-6f,0f,6f})
                P("07 - Santuario solar destruido","Base",x,38,sanctuaryRoof,0,true,true);
            P("07 - Santuario solar destruido","Arch",0,38,sanctuaryRoof+2,90,false,true);
            var altar=P("08 - Altar y simbolo solar","GreatAltar",0,35,4,0,true);
            var altarTop=plan.Pieces.Last().Bounds; altarTop.Expand(new Vector3(-2,0,-2));
            plan.Surfaces.Add(new Surface{Bounds=altarTop,Height=4+assets["GreatAltar"].Bounds.size.y});
            P("08 - Altar y simbolo solar","Disk",0,35,4+assets["GreatAltar"].Bounds.size.y,45);
            foreach(float x in new[]{-24f,24f}) P("08 - Altar y simbolo solar","Altar",x,-18,4);
            foreach(float x in new[]{-24f,-12f,0f,12f})
            {
                P("09 - Ruinas meridionales","Column",x,-42);
                P("09 - Ruinas meridionales","BrokenColumn",x-3,-36,0,35);
            }
            foreach(float x in new[]{-18f,-6f,6f}) for(int course=0;course<2;course++)
                P("09 - Ruinas meridionales","Wall",x,-48,course*assets["Wall"].Bounds.size.y,90);
            P("09 - Ruinas meridionales","BrokenWall",-30,-39,0,35);
            P("09 - Ruinas meridionales","Statue",-18,-33,0,180);
            P("09 - Ruinas meridionales","Statue",36,33,0,200);
            // Native ornamental paving is flush, leaving the entire central arena unobstructed.
            foreach(var tile in source.Ground)
            {
                Vector3 p=tile.Bounds.center-Center;
                if(Mathf.Abs(p.x)>24||Mathf.Abs(p.z)>24) continue;
                P("10 - Pavimento de la arena","Paving",p.x,p.z,.025f-assets["Paving"].Bounds.size.y);
            }
            // Native railings follow only exposed, supported edges; stair clearance omits entry rails.
            var terraces=plan.Surfaces.Where(s=>(Mathf.Abs(s.Height-4)<.01f||Mathf.Abs(s.Height-8)<.01f)&&Mathf.Abs(s.Bounds.size.x-6)<.05f&&Mathf.Abs(s.Bounds.size.z-6)<.05f).ToArray();
            foreach(var tile in terraces)
            {
                if(terraces.Any(s=>s.Height>tile.Height+.1f&&OverlapXZ(s.Bounds,tile.Bounds))) continue;
                foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                {
                    var next=tile.Bounds.center+normal*6;
                    if(terraces.Any(s=>s.Height>=tile.Height-.01f&&Mathf.Abs(s.Bounds.center.x-next.x)<.05f&&Mathf.Abs(s.Bounds.center.z-next.z)<.05f)) continue;
                    var p=tile.Bounds.center+normal*(3-assets["Rail"].Bounds.size.x/2-.03f)-Center;
                    P("11 - Pretiles de galerias","Rail",p.x,p.z,tile.Height,Mathf.Abs(normal.z)>.5f?90:0);
                }
            }
            if(plan.Pieces.Count(p=>p.Key=="Stair")!=11) throw new InvalidOperationException("Faltan escaleras del anillo ceremonial.");
            ValidatePlan(plan);
            Debug.Log("SOL_CORROMPIDO_PREVIEW: "+plan.Pieces.Count+" prefabs; once escaleras. Grupos: "+string.Join("; ",plan.Counts.Select(k=>k.Key+"="+k.Value))+". Omisiones seguras: "+plan.Skips.Count);
            return plan;
        }        static void ValidatePlan(Plan plan)
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
            foreach(var roof in plan.Pieces.Where(p=>p.Key=="Base"&&p.Bounds.min.y>Center.y+10))
            {
                bool supported=plan.Pieces.Any(p=>(p.Key=="Pillar"||p.Key=="LibraryColumn")&&
                    Mathf.Abs(p.Bounds.max.y-roof.Bounds.min.y)<.05f&&
                    Vector2.Distance(new Vector2(p.Bounds.center.x,p.Bounds.center.z),new Vector2(roof.Bounds.center.x,roof.Bounds.center.z))<=6.05f);
                if(!supported) throw new InvalidOperationException("Cubierta sin apoyo estructural: "+roof.Object.name);
            }
            foreach(var arch in plan.Pieces.Where(p=>p.Key=="Arch"))
                if(!Supported(plan,arch.Bounds,arch.Bounds.min.y-Center.y)) throw new InvalidOperationException("Arco sin apoyo: "+arch.Object.name);
            if(plan.Stage.GetComponentsInChildren<Light>().Any(l=>l.enabled)||plan.Stage.GetComponentsInChildren<ParticleSystem>().Any(p=>p.isPlaying)) throw new InvalidOperationException("No se permiten efectos ni luces en esta arquitectura.");
            Debug.Log("SOL_CORROMPIDO_VALIDACION_OK: plataforma circular y apoyos reales conservados, dos accesos libres, escaleras con colliders, patio y pasillos protegidos; sin modificar escenas originales. Validacion geometrica, no NavMesh/gameplay.");
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
            Debug.Log("SOL_CORROMPIDO_MODULES: "+string.Join("; ",result.Select(k=>k.Key+"="+k.Value.Bounds.size)));
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


        sealed class SolarArenaPreview:PreviewSceneStage
        {
            public Source Source; public Dictionary<string,Asset> Assets; public Plan Plan;
            protected override GUIContent CreateHeaderContent()=>new GUIContent("El Sol Corrompido - complejo ceremonial (temporal)");
            protected override bool OnOpenStage()
            {
                if(Source==null||!Source.Zone||Assets==null||!base.OnOpenStage()) return false;
                try
                {
                    Scene temp=scene; temp.name="TEMP_ElSolCorrompido_Architecture";
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
                    ValidateWalks(Plan,scene);
                    EditorApplication.delayCall+=()=>
                    {
                        if(!this||StageUtility.GetCurrentStage()!=this) return;
                        var view=SceneView.lastActiveSceneView;
                        if(view) { view.sceneLighting=false; view.sceneViewState.showImageEffects=false; view.orthographic=false;
                            view.LookAtDirect(Center+Vector3.up*10,Quaternion.Euler(48,-25,0),90); view.Repaint(); }
                        Selection.activeGameObject=null;
                    };
                    Source=null; Assets=null;
                    Debug.Log("Previsualizacion independiente del El Sol Corrompido lista. Copias de zona, dos torres, monumento y dos conexiones; no se integro ni guardo la escena original.");
                    return true;
                }
                catch(Exception e) { Debug.LogException(e); base.OnCloseStage(); return false; }
            }
        }
    }
}



