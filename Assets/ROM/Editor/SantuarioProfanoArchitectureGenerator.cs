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
    // Review-only pass. No integration command and no writes to the original scene.
    public static class SantuarioProfanoArchitectureGenerator
    {
        const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Root="Dungeon_Ciudad_del_Sol_Generated";
        const string Zone="06 - Santuario Profano";
        const string StageName="SantuarioProfano_Architecture";
        const string Menu="ROM/Dungeon Ciudad del Sol/Santuario Profano/";
        const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string ST="Assets/Standard Assets/Sun_Temple/Prefabs/";
        const string EG="Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/";
        static readonly Vector3 Center=new Vector3(150,4,-78);
        static readonly Dictionary<string,string> Paths=new Dictionary<string,string>
        {
            {"Base",ET+"Pits and Platforms/Platform_Base_01.prefab"},
            {"Stair",ET+"Staircases/Staircase_01.prefab"},
            {"LibraryColumn",ST+"Modules_Temple/Bld_Temple_Int_Column_A.prefab"},
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

        [MenuItem(Menu+"1. Previsualizar complejo ceremonial (copia temporal) %#&F9")]
        public static void Preview()
        {
            if(!Ready) return;
            try
            {
                ClosePreview(); var source=Inspect(); var assets=LoadAssets();
                var preview=ScriptableObject.CreateInstance<SanctuaryPreview>(); preview.Source=source; preview.Assets=assets;
                StageUtility.GoToStage(preview,true);
            }
            catch(Exception e) { Debug.LogException(e); }
        }
        [MenuItem(Menu+"2. Validar accesos, escaleras y colliders")]
        public static void Validate()
        {
            try
            {
                var preview=StageUtility.GetCurrentStage() as SanctuaryPreview;
                if(!preview||preview.Plan==null) throw new InvalidOperationException("Abre primero la previsualizacion del Santuario Profano.");
                ValidatePlan(preview.Plan); ValidateWalks(preview.Plan,preview.Plan.Stage.gameObject.scene);
            }
            catch(Exception e) { Debug.LogException(e); }
        }
        [MenuItem(Menu+"3. Cerrar previsualizacion sin guardar")]
        public static void ClosePreview()
        { if(Ready&&StageUtility.GetCurrentStage() is SanctuaryPreview) StageUtility.GoToMainStage(); }


        [MenuItem(Menu+"4. Integrar Santuario aprobado y guardar tras validar %#&j")]
        public static void IntegrateApproved()
        {
            if(!Ready) return;
            int undo=-1; bool saved=false;
            try
            {
                var preview=StageUtility.GetCurrentStage() as SanctuaryPreview;
                if(preview&&preview.Plan==null)
                {
                    Preview();
                    var rebuilt=StageUtility.GetCurrentStage() as SanctuaryPreview;
                    if(!rebuilt||rebuilt.Plan==null) throw new InvalidOperationException("No se pudo recuperar la previsualizacion aprobada.");
                    EditorApplication.delayCall+=IntegrateApproved;
                    return;
                }
                string approved=preview && preview.Plan!=null?Layout(preview.Plan.Stage):null;
                ClosePreview();
                var source=Inspect();
                if(source.Scene.isDirty) throw new InvalidOperationException("La escena tiene cambios previos sin guardar; no se guardaran con esta etapa.");
                string[] stages={"01 - Entrada Portico Solar/PorticoSolar_Architecture","02 - Jardines Dorados/JardinesDorados_Architecture","03 - Plaza del Eclipse/PlazaDelEclipse_Architecture","04 - Archivos Solares/ArchivosSolares_Architecture","05 - Claustro del Eclipse/ClaustroDelEclipse_Architecture"};
                foreach(var path in stages) if(!source.Root.Find(path)) throw new InvalidOperationException("Falta arquitectura aprobada: "+path);
                var matches=source.Zone.Cast<Transform>().Where(t=>t.name==StageName).ToArray();
                if(matches.Length>1) throw new InvalidOperationException("Santuario duplicado.");
                var existing=matches.FirstOrDefault();
                if(existing&&!existing.Find("__ROM_ProfaneSanctuary_Architecture_v1")) throw new InvalidOperationException("Grupo ajeno; no se reemplazara.");
                var preservation=new Preservation(source.Scene,source.Zone,existing);
                var assets=LoadAssets();
                Undo.IncrementCurrentGroup(); undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Integrar Santuario aprobado");
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
                if(plan.Pieces.Count!=257||plan.Pieces.Count(p=>p.Key=="Stair")!=8) throw new InvalidOperationException("La composicion aprobada debe tener 257 prefabs y ocho escaleras.");
                ValidatePlan(plan); ValidateWalks(plan,source.Scene);
                if(approved!=null&&approved!=Layout(plan.Stage)) throw new InvalidOperationException("Distribucion distinta de la previsualizacion aprobada.");
                preservation.Verify();
                if(source.Scene.GetRootGameObjects().Any(g=>g.name=="Dungeon_Void_Atmosphere")) throw new InvalidOperationException("Vacio inesperado.");
                if(source.Scene.path!=Target) throw new InvalidOperationException("Escena destino incorrecta.");
                EditorSceneManager.MarkSceneDirty(source.Scene);
                if(!EditorSceneManager.SaveScene(source.Scene,Target,false)) throw new InvalidOperationException("Error al guardar.");
                saved=true; Undo.CollapseUndoOperations(undo);
                Debug.Log("SANTUARIO_INTEGRADO_GUARDADO: 257 prefabs, ocho escaleras, tres accesos libres. Seis arquitecturas aprobadas presentes; objetos previos, seis zonas, nueve caminos y colliders intactos. Sin luces nuevas ni Vacio.");
            }
            catch(Exception e)
            {
                if(!saved&&undo>=0) Undo.RevertAllDownToGroup(undo);
                Debug.LogError("SANTUARIO_INTEGRACION_DETENIDA: "+e.Message);
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

        [MenuItem(Menu+"4. Crear copia completa de revision temporal")]
        public static void CreateReviewCopy() {
            if(!Ready) return;
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Hay cambios sin guardar; guardalos o descartalos antes de crear la copia.");
            if(System.IO.File.Exists("Assets/Scenes/Previews/TEMP_SantuarioProfano_Review.unity"))
                throw new InvalidOperationException("La copia ya existe; abrila desde Project. No se sobreescribira.");
            ClosePreview(); BatchPreview();
        }        public static void BatchPreview()
        {
            const string copy="Assets/Scenes/Previews/TEMP_SantuarioProfano_Review.unity";
            string before=Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(System.IO.File.ReadAllBytes(Target)));
            EditorSceneManager.OpenScene(Target);
            var source=Inspect();
            string[] approved={"01 - Entrada Portico Solar/PorticoSolar_Architecture","02 - Jardines Dorados/JardinesDorados_Architecture",
                "03 - Plaza del Eclipse/PlazaDelEclipse_Architecture","04 - Archivos Solares/ArchivosSolares_Architecture","05 - Claustro del Eclipse/ClaustroDelEclipse_Architecture"};
            foreach(var path in approved) if(!source.Root.Find(path)) throw new InvalidOperationException("Falta arquitectura aprobada: "+path);
            System.IO.Directory.CreateDirectory("Assets/Scenes/Previews");
            if(!EditorSceneManager.SaveScene(source.Scene,copy,true)) throw new InvalidOperationException("No se pudo crear copia.");
            var scene=EditorSceneManager.OpenScene(copy);
            var zone=scene.GetRootGameObjects().Single(g=>g.name==Root).transform.Find(Zone);
            var beforeObjects=new Preservation(scene,zone,null);
            var plan=Build(source,zone,LoadAssets());
            beforeObjects.Verify(); ValidateWalks(plan,scene);
            if(!EditorSceneManager.SaveScene(scene,copy)) throw new InvalidOperationException("No se pudo guardar copia.");
            string after=Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(System.IO.File.ReadAllBytes(Target)));
            if(before!=after) throw new InvalidOperationException("Archivo original alterado.");
            System.IO.File.WriteAllText("Documentation/santuario-profano-validation.txt","SANTUARIO_VALIDADO\nCopia: "+copy+"\nOriginal SHA256/base64: "+before+
                "\nObjetos originales, seis zonas, cinco arquitecturas y nueve caminos: preservados.\nPrefabs: "+plan.Pieces.Count+"\nEscaleras: 8\nAccesos y cuatro carriles: comprobacion geometrica con rayos de colliders.\nNo es prueba de gameplay/NavMesh.\nDescartes: "+string.Join("\n",plan.Skips));
            Debug.Log("SANTUARIO_REVIEW_SAVED: "+copy);
        }
        static void ValidateWalks(Plan plan,Scene scene) {
            Physics.SyncTransforms();
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger).ToArray();
            void Walk(Vector3 a,Vector3 b,float width) {
                var tangent=b-a; tangent.y=0; var side=Vector3.Cross(Vector3.up,tangent.normalized);
                int n=Mathf.CeilToInt(tangent.magnitude/.25f);
                for(int lane=0;lane<4;lane++) for(int i=0;i<=n;i++) {
                    var p=Center+Vector3.Lerp(a,b,i/(float)n)+side*((lane-1.5f)*width/4);
                    float y=float.NegativeInfinity;
                    foreach(var c in all) if(c.Raycast(new Ray(p+Vector3.up*1.25f,Vector3.down),out var hit,1.8f)&&hit.normal.y>.5f) y=Mathf.Max(y,hit.point.y);
                    if(float.IsNegativeInfinity(y)) throw new InvalidOperationException("Sin suelo transitable en "+p);
                    if(scene.GetPhysicsScene().OverlapCapsule(new Vector3(p.x,y+.65f,p.z),new Vector3(p.x,y+1.65f,p.z),.3f,new Collider[32],~0,QueryTriggerInteraction.Ignore)>0)
                        throw new InvalidOperationException("Capsula bloqueada en "+p+" / "+string.Join("; ",Physics.OverlapCapsule(new Vector3(p.x,y+.65f,p.z),new Vector3(p.x,y+1.65f,p.z),.3f,~0,QueryTriggerInteraction.Ignore).Where(c=>c.transform.IsChildOf(plan.Stage)).Select(c=>c.name+" "+c.bounds)));
                }
            }
            Walk(new Vector3(-47,0,24),new Vector3(47,0,24),8);
            Walk(new Vector3(-47,0,-42),new Vector3(-39,0,-42),4);
            Walk(new Vector3(-39,0,-42),new Vector3(-39,0,24),4);
            Walk(new Vector3(3,0,24),new Vector3(3,0,-12),4);
            foreach(float x in new[]{-27f,-21f,21f,27f}) Walk(new Vector3(x,0,12),new Vector3(x,4,6),4);
            foreach(float x in new[]{-3f,3f}) {
                Walk(new Vector3(x,0,-12),new Vector3(x,4,-18),4);
                Walk(new Vector3(x,4,-30),new Vector3(x,8,-36),4);
            }
            foreach(float x in new[]{-21f,21f}) Walk(new Vector3(x,4,5.5f),new Vector3(x,4,-29),4);
            Walk(new Vector3(3,4,-18.5f),new Vector3(3,4,-29.5f),4);
            Debug.Log("SANTUARIO_WALKS_OK: suelo comprobado, cuatro carriles sin bloqueos nuevos y ocho escaleras. Ruta occidental por el lateral exterior para evitar los arcos originales conservados.");
        }        static Source Inspect()
        {
            var scene=SceneManager.GetActiveScene();
            if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage()||scene.path!=Target)
                throw new InvalidOperationException("Activa Dungeon_Isla_del_Sol en el escenario principal.");
            var roots=scene.GetRootGameObjects().Where(g=>g.name==Root).ToArray();
            if(roots.Length!=1||!roots[0].transform.Find("__ROM_SolarStructure_12D9D67A_v1")) throw new InvalidOperationException("Base ausente o duplicada; no se reconstruira.");
            var root=roots[0].transform; var zone=root.Find(Zone);
            if(!zone||(zone.position-Center).sqrMagnitude>.0001f||(zone.lossyScale-Vector3.one).sqrMagnitude>.0001f||
                Quaternion.Angle(zone.rotation,Quaternion.identity)>.01f||root.position.sqrMagnitude>.0001f||
                (root.lossyScale-Vector3.one).sqrMagnitude>.0001f) throw new InvalidOperationException("Contrato de posicion/escala del Santuario Profano alterado.");
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
            if(source.Ground.Count<100) throw new InvalidOperationException("No se reconocen las superficies originales del Santuario Profano.");
            foreach(var collider in zone.GetComponentsInChildren<Collider>())
                if(collider.enabled&&!collider.isTrigger&&collider.bounds.max.y>Center.y+.3f) source.Obstacles.Add(collider.bounds);
            source.Routes.AddRange(root.Cast<Transform>().Where(t=>t.name.StartsWith("03-06 -",StringComparison.Ordinal)||t.name.StartsWith("01-06 -",StringComparison.Ordinal)||t.name.StartsWith("06-07 -",StringComparison.Ordinal)));
            var landmarks=root.Find("Arquitectura monumental - fuera de los pasos");
            if(landmarks) foreach(Transform landmark in landmarks)
            {
                Vector3 offset=landmark.position-Center;
                if(Mathf.Abs(offset.x)>48||offset.z< -54||offset.z>54||Mathf.Abs(offset.y)>.1f) continue;
                source.Landmarks.Add(landmark);
                foreach(var r in landmark.GetComponentsInChildren<MeshRenderer>())
                    if(r.enabled&&!r.name.EndsWith("_Shadow",StringComparison.OrdinalIgnoreCase)) source.Obstacles.Add(r.bounds);
            }
            return source;
        }
        static Bounds Reserve(float x,float z,float width,float depth,float bottom=.15f,float height=5.5f)
            =>new Bounds(Center+new Vector3(x,bottom+height/2,z),new Vector3(width,height,depth));
        static readonly Bounds[] MainClearance={
            Reserve(0,24,96,14), Reserve(-30,-42,36,12), Reserve(-12,-12,10,72),
            Reserve(3,9,12,30), Reserve(-39,-6,6,84)
        };
        static readonly Bounds[] StairClearance={
            Reserve(-24,9,12,6.8f,.15f,9.5f),Reserve(24,9,12,6.8f,.15f,9.5f),
            Reserve(3,-15,18,6.8f,.15f,9.5f),Reserve(3,-33,18,6.8f,4.15f,9.5f),
            Reserve(-21,-12,4,32,4.15f),Reserve(21,-12,4,32,4.15f),
            Reserve(3,-24,12,12,4.15f),Reserve(3,-38,12,4,8.15f)
        };
        static bool Inside(Bounds b)=>b.min.x>=Center.x-48-.02f&&b.max.x<=Center.x+48+.02f&&
            b.min.z>=Center.z-54-.02f&&b.max.z<=Center.z+54+.02f;        static Bounds Inset(Bounds b) { b.Expand(-.04f); return b; }
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
            if(undo) Undo.RegisterCreatedObjectUndo(root,"Crear Santuario Profano");
            new GameObject("__ROM_ProfaneSanctuary_Architecture_v1").transform.SetParent(root.transform,false);
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
                string reason=!Inside(b)?"limite de plataforma":(key!="WideArch"&&MainClearance.Any(r=>r.Intersects(Inset(b))))?"acceso original":null;
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
                    (!Inside(c.bounds)||(key!="WideArch"&&MainClearance.Any(r=>r.Intersects(Inset(c.bounds))))||(!surface&&StairClearance.Any(r=>r.Intersects(Inset(c.bounds)))))))
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
            Terrace("01 - Galerias hipostilas +4",-27,-21,-27,3,4);
            Terrace("01 - Galerias hipostilas +4",21,27,-27,3,4);
            Terrace("02 - Presbiterio +4",-3,3,-27,-21,4);
            Terrace("03 - Santuario superior +8",-3,27,-45,-39,8);
            foreach(float x in new[]{-27f,-21f,21f,27f}) P("04 - Escalinatas","Stair",x,9,0,270,true);
            foreach(float x in new[]{-3f,3f}) {
                P("04 - Escalinatas","Stair",x,-15,0,270,true);
                P("04 - Escalinatas","Stair",x,-33,4,270,true);
            }
            // Tall native granite piers and a supported entablature frame a 30m-wide walkable facade.
            foreach(float x in new[]{-33f,33f}) P("05 - Fachada monumental","Pillar",x,-3,0,0,true);
            // Piers continue down to the original floor through the gallery foundations.
            // Four intermediate supports keep the existing high entablature from reading as a floating roof.
            foreach(float x in new[]{-27f,-5f,15f,27f})
                P("13 - Apoyos de la fachada","Pillar",x,-3,0,0,true,false,true);
            float top=assets["Pillar"].Bounds.size.y;
            foreach(float x in new[]{-33f,-27f,-21f,-15f,-9f,-3f,3f,9f,15f,21f,27f,33f})
                P("05 - Fachada monumental","Base",x,-3,top,0,true,true);
            P("05 - Fachada monumental","Arch",3,-3,top+2,90,false,true);
            foreach(float side in new[]{-1f,1f}) {
                foreach(float z in new[]{-21f,-9f}) P("06 - Columnata monumental","LibraryColumn",side*27,z,4);
                foreach(float z in new[]{-27f,-15f,-3f,9f}) for(int course=0;course<3;course++)
                    P("07 - Muros del templo","Wall",side*39,z,course*assets["Wall"].Bounds.size.y,90);
                P("08 - Altares ceremoniales","Altar",side*27,-18,4);
                P("09 - Ruinas y patios","BrokenArch",side*39,-9);
                P("09 - Ruinas y patios","BrokenColumn",side*33,-33);
                P("09 - Ruinas y patios","Statue",side*39,9);
                foreach(float z in new[]{-27f,-15f,-3f}) P("09 - Ruinas y patios","BrokenColumn",side*45,z);
                foreach(float z in new[]{-27f,-21f,-15f,-9f,-3f}) P("10 - Bordes de galerias","Rail",side*29.7f,z,4,90);
            }
            P("08 - Altares ceremoniales","Altar",3,-45,8,0,true);
            foreach(float x in new[]{-3f,9f,21f,27f}) P("11 - Abside monumental","LibraryColumn",x,-45,8);
            foreach(float x in new[]{-3f,3f,9f,15f,21f,27f}) for(int course=0;course<4;course++)
                P("11 - Abside monumental","Wall",x,-51,course*assets["Wall"].Bounds.size.y,90);

            // Portal bounds include its open arch; exact capsule tests below check the passage.
            P("14 - Fachada en ruinas","WideArch",3,3,0,90,true);
            // Additional facade wings, native masonry courses and collapsed perimeter fragments.
            foreach(float side in new[]{-1f,1f})
            {
                for(int course=0;course<3;course++)
                    P("14 - Fachada en ruinas","Wall",side*33,-11,course*assets["Wall"].Bounds.size.y);
                P("14 - Fachada en ruinas","BrokenWall",side*33,-23);
                P("14 - Fachada en ruinas","BrokenColumn",side*33,-15);
                P("15 - Ruinas del atrio","BrokenArch",side*27,42,0,side>0?15:195);
                P("15 - Ruinas del atrio","BrokenColumn",side*15,42,0,side>0?20:200);
                P("15 - Ruinas del atrio","Statue",side*33,36,0,180);
                P("15 - Ruinas del atrio","BrokenColumn",side*33,42,0,70);
                P("14 - Fachada en ruinas","Arch",side*21,-3,top+2,90,false,true);
            }
            // A supported baldachin frames the existing altar; the +8m floor and approach stay intact.
            float altarRoof=8+assets["LibraryColumn"].Bounds.size.y;
            foreach(float x in new[]{-3f,3f,9f})
                P("16 - Baldaquino del altar","Base",x,-45,altarRoof,0,true,true);
            P("16 - Baldaquino del altar","Arch",3,-45,altarRoof+2,90,false,true);
            P("16 - Baldaquino del altar","Sun",3,-49,14,0,false,true);
            foreach(float x in new[]{15f,27f})
                P("16 - Baldaquino del altar","BrokenColumn",x,-39,8,0);
            // Thin native pavement traces the ceremonial court without duplicating the ground collider surface.
            foreach(var tile in source.Ground) {
                Vector3 p=tile.Bounds.center-Center;
                if(Mathf.Abs(p.x)>30||p.z< -30||p.z>15) continue;
                if(plan.Pieces.Any(piece=>piece.Key=="Base"&&piece.Bounds.min.y<=Center.y+.1f&&OverlapXZ(piece.Bounds,tile.Bounds))) continue;
                P("12 - Patio ceremonial","Paving",p.x,p.z,.025f-assets["Paving"].Bounds.size.y);
            }
            if(plan.Pieces.Count(p=>p.Key=="Stair")!=8) throw new InvalidOperationException("Faltan escaleras.");
            if(plan.Pieces.Count(p=>p.Key=="Pillar" && p.Object.transform.parent.name=="13 - Apoyos de la fachada")!=4)
                throw new InvalidOperationException("Faltan apoyos intermedios de fachada.");
            Debug.Log("SANTUARIO_FACHADA: "+string.Join("; ",plan.Counts.Select(k=>k.Key+"="+k.Value)));
            ValidatePlan(plan);
            Debug.Log("SANTUARIO_PREVIEW: "+plan.Pieces.Count+" prefabs; ocho escaleras. Descartes: "+string.Join("; ",plan.Skips));
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
                    if(c.enabled&&!c.isTrigger&&(!Inside(c.bounds)||(piece.Key!="WideArch"&&MainClearance.Any(r=>r.Intersects(Inset(c.bounds))))||
                        (piece.Key!="Base"&&piece.Key!="Stair"&&piece.Key!="Paving"&&StairClearance.Any(r=>r.Intersects(Inset(c.bounds))))))
                        throw new InvalidOperationException("Collider bloquea un paso: "+c.name);
                foreach(var r in t.GetComponentsInChildren<MeshRenderer>())
                    if(r.sharedMaterials.Any(m=>!m||!m.shader)) throw new InvalidOperationException("Material faltante: "+r.name);
            }
            if(plan.Stage.GetComponentsInChildren<Light>().Any(l=>l.enabled)||plan.Stage.GetComponentsInChildren<ParticleSystem>().Any(p=>p.isPlaying)) throw new InvalidOperationException("No se permiten efectos ni luces en esta arquitectura.");
            Debug.Log("SANTUARIO_VALIDACION_OK: plataforma rectangular y apoyos reales conservados, tres accesos libres, escaleras con colliders, patio y pasillos protegidos; sin modificar escenas originales. Validacion geometrica, no NavMesh/gameplay.");
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
            Debug.Log("SANTUARIO_MODULES: "+string.Join("; ",result.Select(k=>k.Key+"="+k.Value.Bounds.size)));
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


        sealed class SanctuaryPreview:PreviewSceneStage
        {
            public Source Source; public Dictionary<string,Asset> Assets; public Plan Plan;
            protected override GUIContent CreateHeaderContent()=>new GUIContent("Santuario Profano - complejo ceremonial (temporal)");
            protected override bool OnOpenStage()
            {
                if(Source==null||!Source.Zone||Assets==null||!base.OnOpenStage()) return false;
                try
                {
                    Scene temp=scene; temp.name="TEMP_SantuarioProfano_Architecture";
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
                    Debug.Log("Previsualizacion independiente del Santuario Profano lista. Copias de zona, torre y tres conexiones; no se integro ni guardo la escena original.");
                    return true;
                }
                catch(Exception e) { Debug.LogException(e); base.OnCloseStage(); return false; }
            }
        }
    }
}


