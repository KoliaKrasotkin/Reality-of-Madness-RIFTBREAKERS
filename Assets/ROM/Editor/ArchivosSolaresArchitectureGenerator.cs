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
    // Manual additive architectural pass. Integration only after explicit approval; never regenerates the base.
    public static class ArchivosSolaresArchitectureGenerator
    {
        const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Root="Dungeon_Ciudad_del_Sol_Generated";
        const string Zone="04 - Archivos Solares";
        const string StageName="ArchivosSolares_Architecture";
        const string Menu="ROM/Dungeon Ciudad del Sol/Archivos Solares/";
        const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string ST="Assets/Standard Assets/Sun_Temple/Prefabs/";
        static readonly Vector3 Center=new Vector3(-108,20,108);
        static readonly Dictionary<string,string> Paths=new Dictionary<string,string>
        {
            {"Base",ET+"Pits and Platforms/Platform_Base_01.prefab"},
            {"Stair",ET+"Staircases/Staircase_01.prefab"},
            {"Column",ET+"Columns/Column_01.prefab"},
            {"LibraryColumn",ST+"Modules_Temple/Bld_Temple_Int_Column_A.prefab"},
            {"BrokenColumn",ET+"Damaged/Column_Broken_01.prefab"},
            {"Wall",ET+"Buildings/Walls/1-Segment Long/Walls_2x1_01.prefab"},
            {"BrokenWall",ET+"Damaged/Broken_Wall_01.prefab"},
            {"Arch",ET+"Bases/Thin/Entrance_Arch_01.prefab"},
            {"BrokenArch",ET+"Bases/Thin/Arch_03_Broken_01.prefab"},
            {"Trim",ET+"Railings and Decors/Bottom_Decor_01.prefab"},
            {"Rail",ET+"Railings and Decors/Railing_01.prefab"},
            {"Paving",ET+"Road and Plaza/Plaza_Floor_01.prefab"},
            {"Shelf",ST+"Props/Prop_BookCase_E.prefab"},
            {"Table",ST+"Props/Prop_Table_A.prefab"},
            {"Bench",ST+"Props/Prop_StoneBench_A.prefab"},
            {"Sun",ST+"Modules_Temple/Bld_Temple_Deco_Sun_A.prefab"},
            {"Statue","Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/SM_LadyStatue_Low.prefab"}
        };
        sealed class Asset { public GameObject Prefab; public Bounds Bounds; }
        sealed class Source { public Transform Root,Zone; public Scene Scene; public List<Transform> Routes=new List<Transform>(); }
        sealed class BuildResult { public Transform Stage; public int Count; public List<string> Skips=new List<string>(); }
        static bool Ready=>!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating;

        [MenuItem(Menu+"4. Integrar Archivos aprobados y guardar tras validar %#&F11")]
        public static void IntegrateApproved()
        {
            if(!Ready) return;
            int undo=-1; bool saved=false;
            try
            {
                // Compare the approved live preview when available, but generate from original assets only.
                var preview=StageUtility.GetCurrentStage() as LibraryPreview;
                string approvedLayout=preview && preview.Architecture?Layout(preview.Architecture):null;
                ClosePreview();
                var scene=SceneManager.GetSceneByPath(Target);
                if(!scene.IsValid() || !scene.isLoaded || StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())
                    throw new InvalidOperationException("La original debe estar cargada en el escenario principal.");
                SceneManager.SetActiveScene(scene);
                var source=Inspect();
                string[] otherApproved={"01 - Entrada Portico Solar/PorticoSolar_Architecture",
                    "02 - Jardines Dorados/JardinesDorados_Architecture","03 - Plaza del Eclipse/PlazaDelEclipse_Architecture"};
                foreach(string path in otherApproved)
                    if(!source.Root.Find(path)) throw new InvalidOperationException("Falta la arquitectura aprobada "+path);
                var matches=source.Zone.Cast<Transform>().Where(t=>t.name==StageName).ToArray();
                if(matches.Length>1 || (matches.Length==1 && !matches[0].Find("__ROM_SolarArchives_Architecture_v1")))
                    throw new InvalidOperationException("Arquitectura de Archivos duplicada o ajena; no se reemplazara.");
                Transform existing=matches.FirstOrDefault();
                var before=new Preservation(scene,source.Zone,existing);
                Undo.IncrementCurrentGroup(); undo=Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Integrar solo Archivos Solares aprobados");
                Transform stage=existing;
                if(!stage)
                {
                    var built=Build(source.Zone,LoadAssets(),true);
                    if(built.Count!=327) throw new InvalidOperationException("La composicion cambio: "+built.Count+" prefabs, se esperaban 327. No se guardara.");
                    stage=built.Stage;
                }
                var pieces=stage.Cast<Transform>().SelectMany(g=>g.Cast<Transform>()).Where(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).ToArray();
                if(pieces.Length!=327) throw new InvalidOperationException("Se requieren exactamente 327 instancias nativas, hay "+pieces.Length);
                if(!existing) Undo.RegisterFullObjectHierarchyUndo(stage.gameObject,"Archivos Solares completos");
                ValidateStage(stage);
                if(approvedLayout!=null && Layout(stage)!=approvedLayout)
                    throw new InvalidOperationException("La distribucion no coincide con la previsualizacion aprobada.");
                Inspect(); before.Verify();
                foreach(string path in otherApproved)
                    if(!source.Root.Find(path)) throw new InvalidOperationException("Arquitectura aprobada perdida: "+path);
                if(scene.GetRootGameObjects().Any(g=>g.name=="Dungeon_Void_Atmosphere"))
                    throw new InvalidOperationException("Se detecto una atmosfera del Vacio inesperada; no se guardara.");
                if(scene.path!=Target || StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())
                    throw new InvalidOperationException("Cambio el contexto de edicion.");
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,Target,false)) throw new InvalidOperationException("No se pudo guardar la escena original.");
                saved=true; Undo.CollapseUndoOperations(undo);
                Debug.Log("ARCHIVOS INTEGRADOS Y ESCENA GUARDADA: 327 prefabs y distribucion aprobada conservados. Cuatro arquitecturas presentes: Portico Solar, Jardines Dorados, Plaza del Eclipse y Archivos Solares. Otras seis zonas, nueve caminos, objetos previos y colliders intactos; sin luces nuevas ni Vacio.",stage);
            }
            catch(Exception e)
            {
                if(!saved && undo>=0) Undo.RevertAllDownToGroup(undo);
                Debug.LogError("Integracion de Archivos detenida, no se guardo: "+e.Message);
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

        [MenuItem(Menu+"1. Previsualizar biblioteca-templo (copia temporal) %#&F1")]
        public static void Preview()
        {
            if(!Ready) return;
            try
            {
                ClosePreview();
                var source=Inspect(); var assets=LoadAssets();
                var stage=ScriptableObject.CreateInstance<LibraryPreview>(); stage.Source=source; stage.Assets=assets;
                StageUtility.GoToStage(stage,true);
            }
            catch(Exception e) { Debug.LogException(e); }
        }
        [MenuItem(Menu+"2. Validar previsualizacion y accesos")]
        public static void Validate()
        {
            try
            {
                var preview=StageUtility.GetCurrentStage() as LibraryPreview;
                if(!preview || !preview.Architecture) throw new InvalidOperationException("Abre primero la previsualizacion de Archivos Solares.");
                ValidateStage(preview.Architecture);
            }
            catch(Exception e) { Debug.LogException(e); }
        }
        [MenuItem(Menu+"3. Cerrar previsualizacion sin guardar")]
        public static void ClosePreview()
        { if(Ready && StageUtility.GetCurrentStage() is LibraryPreview) StageUtility.GoToMainStage(); }

        static Source Inspect()
        {
            var scene=SceneManager.GetActiveScene();
            if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage() || scene.path!=Target)
                throw new InvalidOperationException("Activa Dungeon_Isla_del_Sol en el escenario principal; esta herramienta no abre ni guarda escenas.");
            var roots=scene.GetRootGameObjects().Where(g=>g.name==Root).ToArray();
            if(roots.Length!=1 || !roots[0].transform.Find("__ROM_SolarStructure_12D9D67A_v1"))
                throw new InvalidOperationException("No se encontro la estructura base unica y firmada.");
            var root=roots[0].transform; var zone=root.Find(Zone);
            if(!zone || (zone.position-Center).sqrMagnitude>.0001f || (zone.lossyScale-Vector3.one).sqrMagnitude>.0001f ||
                Quaternion.Angle(zone.rotation,Quaternion.identity)>.01f || root.position.sqrMagnitude>.0001f ||
                (root.lossyScale-Vector3.one).sqrMagnitude>.0001f)
                throw new InvalidOperationException("Archivos no coincide con centro (-108,20,108), escala 1 y orientacion original.");
            if(!zone.Cast<Transform>().Any(t=>t.name.StartsWith("Galeria lateral elevada",StringComparison.Ordinal)))
                throw new InvalidOperationException("Falta la galeria occidental existente; no se reconstruira.");
            string[] routes={"01-02 -","02-03 -","03-04 -","03-05 -","04-05 -","03-06 -","05-07 -","06-07 -","01-06 -"};
            foreach(string prefix in routes)
                if(root.Cast<Transform>().Count(t=>t.name.StartsWith(prefix,StringComparison.Ordinal))!=1)
                    throw new InvalidOperationException("Recorrido ausente o duplicado: "+prefix);
            var source=new Source{Root=root,Zone=zone,Scene=scene};
            source.Routes.AddRange(root.Cast<Transform>().Where(t=>t.name.StartsWith("03-04 -",StringComparison.Ordinal)||t.name.StartsWith("04-05 -",StringComparison.Ordinal)));
            return source;
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
            Debug.Log("ARCHIVOS_MODULES: Wall="+result["Wall"].Bounds.size+"; LibraryColumn="+result["LibraryColumn"].Bounds.size);
            return result;
        }
        static Bounds TransformBounds(Bounds b,Matrix4x4 matrix)
        {
            var result=new Bounds(matrix.MultiplyPoint3x4(b.center),Vector3.zero);
            for(int i=0;i<8;i++) result.Encapsulate(matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,
                new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
            return result;
        }
        static Bounds Reserve(float x,float z,float width,float depth,float bottom=.15f,float height=5.5f)
            => new Bounds(Center+new Vector3(x,bottom+height/2,z),new Vector3(width,height,depth));
        static readonly Bounds[] Corridors=
        {
            Reserve(21,-24,56,14), // Arrival (-66,20,84), untouched 12m bridge.
            Reserve(21,12,56,14),  // Departure (-66,20,120), untouched 12m bridge.
            Reserve(24,-6,14,50),  // Eastern longitudinal corridor and turning space.
            Reserve(0,-12,14,38),  // Processional entrance into the central reading court.
            Reserve(-12,0,12,10),    // Existing western gallery's lower stair landing.
            Reserve(-24,0,12,36,4.15f), // Existing gallery's upper circulation.
            Reserve(-33,0,4,34)    // Clear ground-level western aisle.
        };
        static bool Inside(Bounds b)=>b.min.x>=Center.x-42-.02f && b.max.x<=Center.x+42+.02f &&
            b.min.z>=Center.z-36-.02f && b.max.z<=Center.z+36+.02f;
        static Bounds Inset(Bounds b) { b.Expand(-.04f); return b; }

        static BuildResult Build(Transform zone,Dictionary<string,Asset> assets,bool undo=false)
        {
            var go=new GameObject(StageName); go.transform.SetParent(zone,false);
            if(undo) Undo.RegisterCreatedObjectUndo(go,"Crear Archivos Solares aprobados");
            var result=new BuildResult{Stage=go.transform};
            new GameObject("__ROM_SolarArchives_Architecture_v1").transform.SetParent(go.transform,false);
            var groups=new Dictionary<string,Transform>(); var counts=new Dictionary<string,int>();
            var gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/Materials/Roof_Gold.mat");
            GameObject P(string group,string key,float x,float z,float y=0,float yaw=0,bool required=false)
            {
                var a=assets[key]; var q=Quaternion.Euler(0,yaw,0);
                var b=TransformBounds(a.Bounds,Matrix4x4.Rotate(q));
                var position=Center+new Vector3(x-b.center.x,y-b.min.y,z-b.center.z); b.center+=position;
                string reason=!Inside(b)?"contorno":Corridors.Any(r=>r.Intersects(Inset(b)))?"corredor":null;
                if(reason!=null)
                {
                    if(required) throw new InvalidOperationException("Pieza necesaria incompatible: "+group+" / "+key+" / "+reason);
                    result.Skips.Add(group+" / "+key+" ("+x+","+z+") / "+reason); return null;
                }
                if(!groups.TryGetValue(group,out var parent))
                { var child=new GameObject(group); child.transform.SetParent(go.transform,false); groups[group]=parent=child.transform; }
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,zone.gameObject.scene);
                instance.transform.SetParent(parent,true); instance.transform.SetPositionAndRotation(position,q*a.Prefab.transform.localRotation);
                instance.transform.localScale=a.Prefab.transform.localScale; DisableImportedBehaviour(instance);
                if(key=="Wall" && !instance.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))
                    foreach(var filter in instance.GetComponentsInChildren<MeshFilter>())
                        if(filter.sharedMesh && filter.GetComponent<MeshRenderer>() && filter.GetComponent<MeshRenderer>().enabled)
                            filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
                if(key=="LibraryColumn")
                {
                    var stone=assets["Wall"].Prefab.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m);
                    if(stone) foreach(var r in instance.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterials=r.sharedMaterials.Select(m=>stone).ToArray();
                }
                if((key=="Sun" || key=="Trim") && gold)
                    foreach(var r in instance.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterials=r.sharedMaterials.Select(m=>gold).ToArray();
                Physics.SyncTransforms();
                if(instance.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger&&
                    (!Inside(c.bounds)||Corridors.Any(r=>r.Intersects(Inset(c.bounds))))))
                {
                    Object.DestroyImmediate(instance);
                    if(required) throw new InvalidOperationException("Collider de pieza necesaria bloquea circulacion: "+key);
                    result.Skips.Add(group+" / "+key+" / collider"); return null;
                }
                if((key=="Base"||key=="Stair") && !instance.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))
                    throw new InvalidOperationException("Superficie nueva sin collider: "+key);
                counts[group]=counts.TryGetValue(group,out int n)?n+1:1; result.Count++; return instance;
            }
            float AlongX(string key)=>assets[key].Bounds.size.x>=assets[key].Bounds.size.z?0:90;
            void WallRun(string group,float x0,float x1,float z,int height)
            {
                float yaw=AlongX("Wall"); var b=TransformBounds(assets["Wall"].Bounds,Matrix4x4.Rotate(Quaternion.Euler(0,yaw,0)));
                for(float x=x0+b.size.x/2;x+b.size.x/2<=x1+.02f;x+=b.size.x-.06f)
                    for(int level=0;level<Mathf.CeilToInt(height/b.size.y);level++) P(group,"Wall",x,z,level*b.size.y,yaw);
            }
            // Pavement retains the exact original floor and colliders below its native thin ornament.
            var tile=assets["Paving"].Bounds;
            for(float x=-42+tile.size.x/2;x+tile.size.x/2<=42.01f;x+=tile.size.x)
                for(float z=-36+tile.size.z/2;z+tile.size.z/2<=36.01f;z+=tile.size.z)
                    P("01 - Patios y pavimento","Paving",x,z,.025f-tile.size.y);

            // Monumental triple-bay facade: central 18m opening plus the eastern longitudinal aisle.
            var col=assets["Column"].Bounds; int levels=Mathf.CeilToInt(12/col.size.y); float lintel=levels*col.size.y;
            foreach(float x in new[]{-12f,12f,35f}) for(int level=0;level<levels;level++)
                P("02 - Fachada monumental","Column",x,-9,level*col.size.y,0,true);
            foreach(float x in new[]{-6f,0f,6f,18f,24f,30f}) P("02 - Fachada monumental","Base",x,-9,lintel,0,true);
            P("02 - Fachada monumental","Arch",0,-9,lintel+2);
            P("02 - Fachada monumental","Sun",0,-10.5f,lintel+4);
            // Native wall modules turned in depth form solid gate pylons without narrowing either aisle.
            float pylonYaw=AlongX("Wall")+90;
            for(int level=0;level<Mathf.CeilToInt(12/assets["Wall"].Bounds.size.y);level++)
                foreach(float x in new[]{-12f,12f,35f})
                    P("02 - Fachada monumental","Wall",x,-10,level*assets["Wall"].Bounds.size.y,pylonYaw,true);
            WallRun("02 - Fachada monumental",-18,-9,-9,12);
            WallRun("02 - Fachada monumental",9,17,-9,12);
            for(int i=0;i<8;i++) P("02 - Fachada monumental","Trim",-15+i*6,-10.5f,lintel+2,90);

            // Two native +4m flights link the ground court to the new +8m stoa; original gallery stays untouched.
            P("03 - Escalinatas y descansos","Stair",-15,15-assets["Stair"].Bounds.size.x/2,0,90,true);
            P("03 - Escalinatas y descansos","Base",-15,18,0,0,true);
            P("03 - Escalinatas y descansos","Base",-15,18,2,0,true);
            P("03 - Escalinatas y descansos","Stair",-15,27-assets["Stair"].Bounds.size.x/2,4,90,true);
            for(int i=0;i<7;i++) for(int level=0;level<4;level++)
                P("04 - Galeria posterior elevada +8","Base",-15+i*6,30,level*2,0,true);
            foreach(float x in new[]{-15f,-3f,9f,21f})
                P("04 - Galeria posterior elevada +8","LibraryColumn",x,31.5f,8);
            float wallYaw=AlongX("Wall"); var wall=TransformBounds(assets["Wall"].Bounds,Matrix4x4.Rotate(Quaternion.Euler(0,wallYaw,0)));
            float back=36-wall.size.z/2-.1f;
            WallRun("05 - Salas y muros ornamentados",-18,36,back,24);
            P("05 - Salas y muros ornamentados","Sun",3,back-1,8+assets["LibraryColumn"].Bounds.size.y);
            for(int i=0;i<6;i++) P("04 - Galeria posterior elevada +8","Trim",-12+i*6,30,8+col.size.y,90);

            // Guard the new +8m gallery; leave its western stair opening completely clear.
            float railYaw=AlongX("Rail");
            var railBounds=TransformBounds(assets["Rail"].Bounds,Matrix4x4.Rotate(Quaternion.Euler(0,railYaw,0)));
            for(float x=-12+railBounds.size.x/2;x+railBounds.size.x/2<=24.01f;x+=railBounds.size.x)
                P("04 - Galeria posterior elevada +8","Rail",x,27.25f,8,railYaw);
            P("04 - Galeria posterior elevada +8","Rail",23.6f,30,8,railYaw+90);

            // Western temple wall encloses a real 6m-wide ground aisle beside the existing raised gallery.
            float sideYaw=wallYaw+90; var side=TransformBounds(assets["Wall"].Bounds,Matrix4x4.Rotate(Quaternion.Euler(0,sideYaw,0)));
            float edge=-42+side.size.x/2+.1f;
            for(float z=-30+side.size.z/2;z+side.size.z/2<=31;z+=side.size.z-.06f)
                for(int level=0;level<Mathf.CeilToInt(14/side.size.y);level++)
                    P("05 - Salas y muros ornamentados","Wall",edge,z,level*side.size.y,sideYaw);
            P("05 - Salas y muros ornamentados","Wall",-edge,-3,0,sideYaw);
            P("05 - Salas y muros ornamentados","BrokenWall",-edge,27,0,sideYaw);

            // Broken nave walls give the temple a recognizable enclosed volume, not isolated colonnades.
            foreach(float sign in new[]{-1f,1f})
            {
                float wallX=sign*(17-side.size.x/2-.1f);
                float end=sign<0?-5:5;
                for(float z=-15+side.size.z/2;z+side.size.z/2<=end+.01f;z+=side.size.z-.06f)
                    for(int level=0;level<Mathf.CeilToInt(12/side.size.y);level++)
                        P("05 - Salas y muros ornamentados","Wall",wallX,z,level*side.size.y,sideYaw);
            }

            // Bookcases give the complex a library identity; doors, stair landings and cross-aisles remain clear.
            foreach(float z in new[]{-15f,-8f,8f,15f}) P("06 - Archivos y salas de lectura","Shelf",-38,z,0,90);
            foreach(float z in new[]{-3f,3f,23f}) P("06 - Archivos y salas de lectura","Shelf",12,z,0,270);
            foreach(float x in new[]{-9f,3f,15f}) P("06 - Archivos y salas de lectura","Shelf",x,31,8,180);
            P("06 - Archivos y salas de lectura","Table",-11,-18);
            P("06 - Archivos y salas de lectura","Table",10,23);
            P("06 - Archivos y salas de lectura","Bench",-35,-22,0,90);
            P("06 - Archivos y salas de lectura","Bench",36,24,0,270);
            foreach(float z in new[]{-26f,26f}) P("07 - Arcadas y ruinas","Arch",-34,z,0,90);
            P("07 - Arcadas y ruinas","BrokenArch",30,-33);
            P("07 - Arcadas y ruinas","BrokenColumn",-30,-30);
            P("07 - Arcadas y ruinas","BrokenColumn",33,29);
            P("07 - Arcadas y ruinas","Statue",-12,-32);
            P("07 - Arcadas y ruinas","Statue",12,-32);
            foreach(string group in new[]{"02 - Fachada monumental","03 - Escalinatas y descansos","04 - Galeria posterior elevada +8","06 - Archivos y salas de lectura"})
                if(!counts.TryGetValue(group,out int n)||n<3) throw new InvalidOperationException("Composicion insuficiente: "+group);
            if(result.Count<100) throw new InvalidOperationException("Densidad arquitectonica insuficiente: "+result.Count);
            Debug.Log("ARCHIVOS_SOLARES_PREVIEW: "+result.Count+" prefabs nativos; entrada 18m, galeria occidental original +4m, stoa posterior +8m y dos vuelos de escalera. "+result.Skips.Count+" omisiones opcionales: "+string.Join("; ",result.Skips.Take(16)));
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
        static void ValidateStage(Transform stage)
        {
            if(!stage || stage.name!=StageName || (stage.position-Center).sqrMagnitude>.0001f)
                throw new InvalidOperationException("Etapa de archivos ausente o desplazada.");
            Physics.SyncTransforms();
            foreach(var t in stage.GetComponentsInChildren<Transform>(true))
            {
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new InvalidOperationException("Script faltante: "+t.name);
                if(PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                {
                    var asset=PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                    if(!asset || (t.localScale-asset.transform.localScale).sqrMagnitude>.0001f)
                        throw new InvalidOperationException("Escala nativa alterada: "+t.name);
                }
            }
            foreach(var collider in stage.GetComponentsInChildren<Collider>())
                if(collider.enabled&&!collider.isTrigger && (!Inside(collider.bounds)||Corridors.Any(r=>r.Intersects(Inset(collider.bounds)))))
                    throw new InvalidOperationException("Collider incompatible con un acceso: "+collider.name);
            foreach(var renderer in stage.GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterials.Any(m=>!m||!m.shader)) throw new InvalidOperationException("Material faltante: "+renderer.name);
            if(stage.GetComponentsInChildren<Light>().Any(l=>l.enabled) || stage.GetComponentsInChildren<ParticleSystem>().Any(p=>p.isPlaying))
                throw new InvalidOperationException("La arquitectura no debe agregar luces especiales ni efectos.");
            Debug.Log("ARCHIVOS_VALIDACION_OK: accesos este de 14m reservados, entrada 18m, desembarco occidental conservado, escalas originales y colliders presentes. No es una prueba de NavMesh/gameplay.");
        }
        sealed class LibraryPreview:PreviewSceneStage
        {
            public Source Source; public Dictionary<string,Asset> Assets; public Transform Architecture;
            protected override GUIContent CreateHeaderContent()=>new GUIContent("Archivos Solares - biblioteca-templo (temporal)");
            protected override bool OnOpenStage()
            {
                if(Source==null||!Source.Zone||Assets==null||!base.OnOpenStage()) return false;
                try
                {
                    Scene temp=scene; temp.name="TEMP_ArchivosSolares_Architecture";
                    var holder=new GameObject("Contexto temporal"); SceneManager.MoveGameObjectToScene(holder,scene); holder.SetActive(false);
                    var root=new GameObject(Root); root.transform.SetParent(holder.transform,false);
                    var zone=Object.Instantiate(Source.Zone.gameObject,root.transform); zone.name=Zone;
                    zone.transform.SetPositionAndRotation(Source.Zone.position,Source.Zone.rotation);
                    var old=zone.transform.Find(StageName); if(old) Object.DestroyImmediate(old.gameObject);
                    foreach(var route in Source.Routes)
                    { var copy=Object.Instantiate(route.gameObject,root.transform); copy.name=route.name; copy.transform.SetPositionAndRotation(route.position,route.rotation); }
                    var landmarks=Source.Root.Find("Arquitectura monumental - fuera de los pasos");
                    if(landmarks) foreach(Transform landmark in landmarks)
                    {
                        Vector3 offset=landmark.position-Center;
                        if(Mathf.Abs(offset.x)>42||Mathf.Abs(offset.z)>36||Mathf.Abs(offset.y)>1) continue;
                        var copy=Object.Instantiate(landmark.gameObject,root.transform); copy.name=landmark.name;
                        copy.transform.SetPositionAndRotation(landmark.position,landmark.rotation);
                    }
                    DisableImportedBehaviour(root); holder.SetActive(true);
                    Architecture=Build(zone.transform,Assets).Stage; ValidateStage(Architecture);
                    var rig=new GameObject("Luz neutra de inspeccion - solo previsualizacion"); SceneManager.MoveGameObjectToScene(rig,scene);
                    rig.transform.rotation=Quaternion.Euler(48,-32,0); var light=rig.AddComponent<Light>();
                    light.type=LightType.Directional; light.intensity=1.1f; light.shadows=LightShadows.None;
                    EditorApplication.delayCall+=()=>
                    {
                        if(!this||StageUtility.GetCurrentStage()!=this) return;
                        var view=SceneView.lastActiveSceneView;
                        if(view) { view.sceneLighting=true; view.sceneViewState.showImageEffects=false; view.orthographic=false;
                            view.LookAtDirect(Center+Vector3.up*10,Quaternion.Euler(48,-25,0),75); view.Repaint(); }
                        Selection.activeGameObject=null;
                    };
                    Source=null; Assets=null;
                    Debug.Log("Previsualizacion independiente de Archivos lista. Solo se copiaron esta zona, su torre y sus dos conexiones; no se integro ni guardo la escena original.");
                    return true;
                }
                catch(Exception e) { Debug.LogException(e); base.OnCloseStage(); return false; }
            }
        }
    }
}
