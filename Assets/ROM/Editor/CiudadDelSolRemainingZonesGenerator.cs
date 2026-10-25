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
    // Manual additive pass. No initialization callbacks, scene saves or base generation.
    public static class CiudadDelSolRemainingZonesGenerator
    {
        const string Target = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Root = "Dungeon_Ciudad_del_Sol_Generated";
        const string Signature = "__ROM_RemainingArchitecture_v1";
        const string PorticoStage = "PorticoSolar_Architecture";
        const string Menu = "ROM/Dungeon Ciudad del Sol/Zonas restantes/";
        const string ET = "Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string ST = "Assets/Standard Assets/Sun_Temple/Prefabs/";
        const string EG = "Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/";
        // Deliberately single-zone while the entrance is under review. Never integrate from this pass.
        static IEnumerable<Zone> ActiveZones => Zones.Take(1);
        sealed class Zone
        {
            public string Name; public Vector3 Center; public Vector2 Size; public Vector2[] Ports;
            public Zone(string name, Vector3 center, Vector2 size, params Vector2[] ports)
            { Name=name; Center=center; Size=size; Ports=ports; }
        }
        static readonly Zone[] Zones =
        {
            new Zone("01 - Entrada Portico Solar",new Vector3(-126,0,-120),new Vector2(72,84),
                new Vector2(12,42),new Vector2(36,-12),new Vector2(0,-42)),
            new Zone("04 - Archivos Solares",new Vector3(-108,20,108),new Vector2(84,72),
                new Vector2(42,-24),new Vector2(42,12)),
            new Zone("05 - Claustro del Eclipse",new Vector3(18,28,144),new Vector2(108,96),
                new Vector2(36,-48),new Vector2(-54,-24),new Vector2(54,-24)),
            new Zone("06 - Santuario Profano",new Vector3(150,4,-78),new Vector2(96,108),
                new Vector2(-48,24),new Vector2(48,24),new Vector2(-48,-42)),
            new Zone("07 - El Sol Corrompido",new Vector3(180,36,156),new Vector2(120,120),
                new Vector2(-60,0),new Vector2(24,-48))
        };
        static readonly Dictionary<string,string> Paths = new Dictionary<string,string>
        {
            {"Column",ET+"Columns/Column_01.prefab"},
            {"BrokenColumn",ET+"Damaged/Column_Broken_01.prefab"},
            {"Wall",ET+"Damaged/Broken_Wall_01.prefab"},
            {"Arch",ET+"Bases/Arch_Wide_01.prefab"},
            {"BrokenArch",ET+"Bases/Thin/Arch_03_Broken_01.prefab"},
            {"Support",ET+"Bases/Base_Arch_Tall_01.prefab"},
            {"Base",ET+"Pits and Platforms/Platform_Base_01.prefab"},
            {"Stair",ET+"Staircases/Staircase_01.prefab"},
            {"Cornice",ET+"Railings and Decors/Bottom_Decor_Broken_01.prefab"},
            {"Tower",ST+"Buildings_Prefabbed/Bld_TempleTower_A.prefab"},
            {"Shelf",ST+"Props/Prop_BookCase_E.prefab"},
            {"Table",ST+"Props/Prop_Table_A.prefab"},
            {"Gate",EG+"SM_MainGate.prefab"},
            {"Statue",EG+"SM_LadyStatue_Low.prefab"},
            {"Altar",EG+"SM_Altar.prefab"},
            {"Sun",EG+"SM_SunDisk2.prefab"},
            {"Seal",ST+"Modules_Temple/Bld_Temple_Int_Circle_A.prefab"}
            ,{"Facade",ET+"Walls/Wall_Big_01.prefab"}
            ,{"EntranceArch",ET+"Bases/Thin/Entrance_Arch_01.prefab"}
            ,{"Paving",ET+"Road and Plaza/Plaza_Floor_01.prefab"}
            ,{"Trim",ET+"Railings and Decors/Bottom_Decor_01.prefab"}
        };
        sealed class Asset { public GameObject Prefab; public Bounds Bounds; }
        sealed class Context
        {
            public Zone Zone; public Transform Parent, Stage; public bool Undo;
            public Dictionary<string,Asset> Assets; public readonly List<string> Skips=new List<string>();
            public readonly List<Bounds> Occupied=new List<Bounds>();
            public readonly List<Bounds> Protected=new List<Bounds>();
            public int Count;
        }
        static readonly HashSet<int> Reviewed=new HashSet<int>();
        static bool Ready => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;

        [MenuItem(Menu+"1. Previsualizar solo Portico Solar %#&F6")]
        public static void Preview()
        {
            if (!Ready) return;
            try
            {
                if (StageUtility.GetCurrentStage() is RemainingPreview) StageUtility.GoToMainStage();
                var root=Inspect(SceneManager.GetActiveScene());
                var assets=LoadAssets();
                var stage=ScriptableObject.CreateInstance<RemainingPreview>();
                stage.Source=root; stage.Assets=assets;
                StageUtility.GoToStage(stage,true);
            }
            catch(Exception e) { Debug.LogException(e); }
        }

        [MenuItem(Menu+"2. Integrar Portico aprobado y guardar tras validar %#&F5")]
        public static void Integrate()
        {
            if (!Ready) return;
            int undo=-1; bool saved=false;
            try
            {
                ClosePreview();
                if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())
                    throw new InvalidOperationException("Sal del modo de edicion de prefabs antes de integrar.");
                var scene=SceneManager.GetSceneByPath(Target);
                if(!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("La escena original debe estar cargada; no se abrira otra escena.");
                SceneManager.SetActiveScene(scene);
                var root=Inspect(scene); var parent=root.Find(Zones[0].Name);
                if(!root.Find("02 - Jardines Dorados/JardinesDorados_Architecture") ||
                   !root.Find("03 - Plaza del Eclipse/PlazaDelEclipse_Architecture"))
                    throw new InvalidOperationException("Falta una de las arquitecturas aprobadas que deben conservarse.");
                var assets=LoadAssets();
                var existing=FindStage(parent);
                var before=new Preservation(scene,parent,existing);
                Undo.IncrementCurrentGroup(); undo=Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Integrar solo Portico Solar aprobado");
                if(existing)
                {
                    if(existing.name!=PorticoStage) { Undo.RecordObject(existing.gameObject,"Nombrar etapa aprobada"); existing.name=PorticoStage; }
                }
                else BuildPortico(root,Zones[0],assets,true);
                ValidateRoot(root);
                Inspect(scene); before.Verify();
                if(!parent.Find(PorticoStage)) throw new InvalidOperationException("Falta PorticoSolar_Architecture.");
                if(scene.path!=Target || StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())
                    throw new InvalidOperationException("Cambio el contexto antes de guardar.");
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,Target,false)) throw new InvalidOperationException("No se pudo guardar la escena original.");
                saved=true; Undo.CollapseUndoOperations(undo);
                Debug.Log("PORTICO INTEGRADO Y GUARDADO: PorticoSolar_Architecture. Accesos/colliders validados; todos los objetos previos, las siete zonas y los nueve recorridos conservados. Sin copiar la previsualizacion.",parent);
            }
            catch(Exception e)
            {
                if(!saved && undo>=0) Undo.RevertAllDownToGroup(undo);
                Debug.LogError("Integracion del Portico detenida; escena sin guardar: "+e.Message);
            }
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
                           (original.name!=name && !(original==existing && original.name==PorticoStage)))
                            throw new InvalidOperationException("Objeto previo alterado: "+name);
                        var now=original.Cast<Transform>().Where(v=>!(original==parent && !existing && v.name==PorticoStage)).Select(v=>v.GetInstanceID());
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

        [MenuItem(Menu+"3. Validar arquitectura y accesos")]
        public static void Validate()
        {
            try
            {
                var stage=StageUtility.GetCurrentStage() as RemainingPreview;
                var root=stage ? stage.Copy : Inspect(SceneManager.GetActiveScene());
                LoadAssets(); ValidateRoot(root);
                Debug.Log("Validacion correcta: contratos, prefabs, escalas, colliders y corredores reservados. Falta prueba jugable de cuatro jugadores.");
            }
            catch(Exception e) { Debug.LogException(e); }
        }

        [MenuItem(Menu+"4. Cerrar previsualizacion sin guardar")]
        public static void ClosePreview()
        {
            if(Ready && StageUtility.GetCurrentStage() is RemainingPreview) StageUtility.GoToMainStage();
        }

        static Transform Inspect(Scene scene)
        {
            if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage() || scene.path!=Target)
                throw new InvalidOperationException("Activa la escena original Dungeon_Isla_del_Sol en el escenario principal.");
            var matches=scene.GetRootGameObjects().Where(g=>g.name==Root).ToArray();
            if(matches.Length!=1 || !matches[0].transform.Find("__ROM_SolarStructure_12D9D67A_v1"))
                throw new InvalidOperationException("Se requiere la base existente unica y firmada; no se reconstruira.");
            var root=matches[0].transform;
            if(root.position.sqrMagnitude>.0001f || Quaternion.Angle(root.rotation,Quaternion.identity)>.01f ||
               (root.lossyScale-Vector3.one).sqrMagnitude>.0001f) throw new InvalidOperationException("Transformacion inesperada de la base.");
            foreach(var z in ActiveZones)
            {
                var parent=root.Find(z.Name);
                if(!parent || (parent.position-z.Center).sqrMagnitude>.0001f ||
                    Quaternion.Angle(parent.rotation,Quaternion.identity)>.01f ||
                    (parent.lossyScale-Vector3.one).sqrMagnitude>.0001f)
                    throw new InvalidOperationException("Contrato de zona alterado: "+z.Name);
                FindStage(parent);
            }
            string[] routes={"01-02 -","02-03 -","03-04 -","03-05 -","04-05 -","03-06 -","05-07 -","06-07 -","01-06 -"};
            foreach(string prefix in routes)
                if(root.Cast<Transform>().Count(t=>t.name.StartsWith(prefix,StringComparison.Ordinal))!=1)
                    throw new InvalidOperationException("Recorrido ausente o duplicado: "+prefix);
            return root;
        }

        static Transform FindStage(Transform parent)
        {
            var found=parent.Cast<Transform>().Where(t=>t.name=="Architecture" || t.name==PorticoStage).ToArray();
            if(found.Length>1 || (found.Length==1 && !found[0].Find(Signature)))
                throw new InvalidOperationException("Grupo Architecture ajeno o duplicado: "+parent.name);
            return found.Length==0 ? null : found[0];
        }
        static Dictionary<string,Asset> LoadAssets()
        {
            var result=new Dictionary<string,Asset>();
            foreach(var pair in Paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(pair.Value);
                if(!prefab) throw new InvalidOperationException("Falta recurso original: "+pair.Value);
                bool found=false; Bounds bounds=default;
                var normalize=Matrix4x4.TRS(Vector3.zero,prefab.transform.localRotation,prefab.transform.localScale)*prefab.transform.worldToLocalMatrix;
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r=filter.GetComponent<Renderer>();
                    if(!filter.sharedMesh || !r || !r.enabled) continue;
                    var b=TransformBounds(filter.sharedMesh.bounds,normalize*filter.transform.localToWorldMatrix);
                    if(!found) { bounds=b; found=true; } else bounds.Encapsulate(b);
                }
                if(!found) throw new InvalidOperationException("Sin malla: "+pair.Value);
                result.Add(pair.Key,new Asset{Prefab=prefab,Bounds=bounds});
            }
            if((result["Base"].Bounds.size-new Vector3(6,2,6)).sqrMagnitude>.01f)
                throw new InvalidOperationException("Cambio el modulo de suelo 6 x 2 x 6.");
            return result;
        }
        static Bounds TransformBounds(Bounds b,Matrix4x4 m)
        {
            var result=new Bounds(m.MultiplyPoint3x4(b.center),Vector3.zero);
            for(int i=0;i<8;i++) result.Encapsulate(m.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,
                new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
            return result;
        }
        static List<Bounds> Reservations(Zone z)
        {
            if(z.Name.StartsWith("01"))
            {
                // Actual northern 12m bridge, eastern 6m secondary route, and wide southern entrance.
                return new List<Bounds>
                {
                    Reserve(z,0,-26,18,40), Reserve(z,0,8,14,28),
                    Reserve(z,6,20,26,14), Reserve(z,12,33,14,26),
                    Reserve(z,18,-12,40,10)
                };
            }
            var result=new List<Bounds>();
            foreach(var port in z.Ports)
            {
                Vector2 inner=port.normalized*(z.Name.StartsWith("07")?25:13);
                // Rectangular envelopes also reserve generous turning space at dogleg arrivals.
                result.Add(new Bounds(z.Center+new Vector3((port.x+inner.x)/2,2.7f,(port.y+inner.y)/2),
                    new Vector3(Mathf.Abs(port.x-inner.x)+18,5.2f,Mathf.Abs(port.y-inner.y)+18)));
            }
            result.Add(new Bounds(z.Center+Vector3.up*2.7f,new Vector3(26,5.2f,26)));
            return result;
        }
        static Bounds Reserve(Zone z,float x,float zz,float width,float depth)
            => new Bounds(z.Center+new Vector3(x,3,zz),new Vector3(width,5.8f,depth));
        static bool Inside(Zone z,Bounds b)
        {
            var lo=b.min-z.Center; var hi=b.max-z.Center;
            if(lo.x < -z.Size.x/2 || hi.x>z.Size.x/2 || lo.z < -z.Size.y/2 || hi.z>z.Size.y/2) return false;
            if(!z.Name.StartsWith("07") && !z.Name.StartsWith("05")) return true;
            // Exact circle for arena; conservative ellipse for semicircular cloister footprint.
            foreach(float x in new[]{lo.x,hi.x}) foreach(float zz in new[]{lo.z,hi.z})
                if(x*x/(z.Size.x*z.Size.x/4)+zz*zz/(z.Size.y*z.Size.y/4)>.95f) return false;
            return true;
        }
        static void Build(Transform root,Zone zone,Dictionary<string,Asset> assets,bool undo)
        {
            if(zone.Name.StartsWith("01")) { BuildPortico(root,zone,assets); return; }
            var parent=root.Find(zone.Name); if(FindStage(parent)) return;
            var go=new GameObject("Architecture"); go.transform.SetParent(parent,false);
            if(undo) Undo.RegisterCreatedObjectUndo(go,"Crear etapa "+zone.Name);
            new GameObject(Signature).transform.SetParent(go.transform,false);
            var c=new Context{Zone=zone,Parent=parent,Stage=go.transform,Assets=assets,Undo=undo};
            c.Protected.AddRange(Reservations(zone));
            // Protect all pre-existing architecture in this zone, including base-built towers and galleries.
            Physics.SyncTransforms();
            foreach(var collider in parent.GetComponentsInChildren<Collider>())
                if(collider.enabled && !collider.isTrigger && collider.bounds.max.y>zone.Center.y+.25f)
                    c.Protected.Add(collider.bounds);
            Compose(c);
            if(c.Count<12) throw new InvalidOperationException("Composicion insuficiente en "+zone.Name+"; revisar envolventes. "+string.Join("; ",c.Skips.Take(5)));
            if(undo) Undo.RegisterFullObjectHierarchyUndo(go,"Registrar arquitectura completa");
            Debug.Log(zone.Name+": "+c.Count+" prefabs a escala original. "+c.Skips.Count+" piezas omitidas por contorno/acceso/solape. "+string.Join("; ",c.Skips.Take(6)));
        }
        // Place by rendered bottom-center, retaining the complete original prefab transform/scale.
        static GameObject Put(Context c,string key,float x,float z,float y=0,float yaw=0,bool surface=false)
        {
            var a=c.Assets[key]; var q=Quaternion.Euler(0,yaw,0);
            var rotated=TransformBounds(a.Bounds,Matrix4x4.Rotate(q));
            var desired=c.Zone.Center+new Vector3(x,y,z);
            var position=desired-new Vector3(rotated.center.x,rotated.min.y,rotated.center.z);
            var bounds=rotated; bounds.center+=position;
            var clearance=bounds; clearance.Expand(-.02f);
            bool collision=c.Protected.Any(b=>b.Intersects(clearance)) || c.Occupied.Any(b=>b.Intersects(clearance));
            if(!Inside(c.Zone,bounds) || collision)
            { c.Skips.Add(key+" ("+x+","+z+")"); return null; }
            var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,c.Stage.gameObject.scene);
            go.transform.SetParent(c.Stage,true); go.transform.SetPositionAndRotation(position,q*a.Prefab.transform.localRotation);
            go.transform.localScale=a.Prefab.transform.localScale;
            foreach(var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) if(behaviour) behaviour.enabled=false;
            foreach(var body in go.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic=true; body.useGravity=false; }
            foreach(var audio in go.GetComponentsInChildren<AudioSource>(true)) { audio.playOnAwake=false; audio.enabled=false; }
            foreach(var camera in go.GetComponentsInChildren<Camera>(true)) camera.enabled=false;
            foreach(var listener in go.GetComponentsInChildren<AudioListener>(true)) listener.enabled=false;
            Physics.SyncTransforms();
            // Renderer bounds alone may miss invisible collider overhangs; verify actual instances too.
            if(go.GetComponentsInChildren<Collider>().Any(v=>v.enabled && !v.isTrigger &&
                (!Inside(c.Zone,v.bounds) || c.Protected.Any(b=>b.Intersects(v.bounds)))))
            { Object.DestroyImmediate(go); c.Skips.Add(key+" collider fuera de contorno/acceso"); return null; }
            if(surface && !go.GetComponentsInChildren<Collider>().Any(v=>v.enabled&&!v.isTrigger))
            { Object.DestroyImmediate(go); throw new InvalidOperationException("Superficie sin collider: "+key); }
            c.Occupied.Add(bounds); c.Count++; return go;
        }
        static void Row(Context c,string key,float x0,float z0,float x1,float z1,int count,float yaw=0)
        { for(int i=0;i<count;i++) { float t=count==1?0:(float)i/(count-1); Put(c,key,Mathf.Lerp(x0,x1,t),Mathf.Lerp(z0,z1,t),0,yaw); } }
        static void Terrace(Context c,float x,float z,int nx,int nz)
        {
            // 6m modules, native 2m thickness; top +4m. Stair rises +4m towards local -X.
            int occupiedBefore=c.Occupied.Count;
            int countBefore=c.Count;
            var tiles=new List<GameObject>();
            for(int i=0;i<nx;i++) for(int j=0;j<nz;j++)
            { var tile=Put(c,"Base",x+(i-(nx-1)*.5f)*6,z+(j-(nz-1)*.5f)*6,2,0,true); if(tile) tiles.Add(tile); }
            // Two native 6m-wide stairs, high end touching the eastern terrace edge.
            var stair=c.Assets["Stair"].Bounds;
            float edge=x+nx*3;
            for(int lane=0;lane<2;lane++)
            { var step=Put(c,"Stair",edge+stair.size.x*.5f,z+(lane==0?-3:3),0,0,true); if(step) tiles.Add(step); }
            if(tiles.Count!=nx*nz+2)
            {
                foreach(var tile in tiles) Object.DestroyImmediate(tile);
                c.Occupied.RemoveRange(occupiedBefore,c.Occupied.Count-occupiedBefore); c.Count=countBefore;
                c.Skips.Add("Terraza completa omitida: suelo/escaleras inseguros");
            }
        }
        static void Compose(Context c)
        {
            switch(c.Zone.Name.Substring(0,2))
            {
                case "01":
                    // Processional axis stays clear; paired gatehouses and statuary frame it.
                    // Native gate as upper ceremonial portal, borne by two native monumental columns.
                    float pierX=(c.Assets["Gate"].Bounds.size.x-c.Assets["Column"].Bounds.size.x)*.5f;
                    if(pierX>=17)
                    {
                        var left=Put(c,"Column",-pierX,-29); var right=Put(c,"Column",pierX,-29);
                        if(left && right) Put(c,"Gate",0,-29,Mathf.Max(7,c.Assets["Column"].Bounds.size.y));
                        else c.Skips.Add("Portico superior omitido: apoyos incompatibles");
                    }
                    else c.Skips.Add("Porton nativo demasiado estrecho para el corredor reservado; entrada flanqueada abierta");
                    Put(c,"Tower",-24,-25); Put(c,"Tower",24,-25);
                    Row(c,"Statue",-23,-8,-23,22,3); Row(c,"Statue",23,-8,23,22,3,180);
                    Row(c,"Wall",-30,-10,-30,30,4,90); Row(c,"Wall",29,5,29,29,3,90);
                    Row(c,"Column",-25,-15,-25,30,5); Row(c,"BrokenColumn",25,0,25,30,4);
                    Terrace(c,-21,24,2,2); break;
                case "04":
                    // Two reading galleries with bookcases, transverse aisles and an open court.
                    foreach(float side in new[]{-29f,10f})
                    {
                        Row(c,"Column",side,-24,side,24,5);
                        Row(c,"Shelf",side-6,-22,side-6,22,6,side<0?90:270);
                        Row(c,"Arch",side,-18,side,18,3,90);
                    }
                    Row(c,"Wall",-35,28,20,28,5); Row(c,"BrokenColumn",-34,-28,12,-28,4);
                    Put(c,"Table",-21,0); Put(c,"Table",-21,15);
                    Terrace(c,-24,18,3,2); break;
                case "05":
                    // Horseshoe ambulatory with paired concentric colonnades and broken arcades.
                    for(int i=0;i<=12;i++)
                    {
                        float angle=i*Mathf.PI/12;
                        float x=Mathf.Cos(angle)*43, z=Mathf.Sin(angle)*34;
                        Put(c,i%4==1?"BrokenColumn":"Column",x,z,0,-i*15);
                        if(i%2==0) Put(c,"BrokenArch",Mathf.Cos(angle)*35,Mathf.Sin(angle)*26,0,-i*15);
                        if(i%3==0) Put(c,"Wall",Mathf.Cos(angle)*46,Mathf.Sin(angle)*37,0,90-i*15);
                    }
                    Terrace(c,-18,24,3,2); Put(c,"Altar",18,26);
                    Put(c,"Statue",-32,12); Put(c,"Statue",32,12,0,180); break;
                case "06":
                    // Asymmetric ruined nave, side sanctuary and ritual courts.
                    Row(c,"Column",-29,-30,-29,34,6); Row(c,"BrokenColumn",29,-30,29,34,6);
                    Row(c,"BrokenArch",-24,-24,-24,24,4,90);
                    Row(c,"Wall",-34,-40,30,-40,6);
                    Put(c,"Altar",18,-25); Put(c,"Sun",18,-29,5);
                    Put(c,"Statue",-18,-29); Put(c,"Wall",32,-14,0,90);
                    Terrace(c,21,-24,3,2); Violet(c,18,-25); Violet(c,-28,26); break;
                case "07":
                    // Open 50m central combat disc; broken outer crown and rear ceremonial throne.
                    Seal(c);
                    for(int i=0;i<20;i++)
                    {
                        float a=i*Mathf.PI*2/20;
                        Put(c,i%3==0?"BrokenColumn":"Column",Mathf.Cos(a)*48,Mathf.Sin(a)*48,0,-i*18);
                        if(i%2==0) Put(c,"BrokenArch",Mathf.Cos(a)*43,Mathf.Sin(a)*43,0,90-i*18);
                    }
                    Terrace(c,-9,36,4,2); Put(c,"Altar",16,34); Put(c,"Sun",0,39,7);
                    for(int i=0;i<5;i++) Violet(c,Mathf.Cos(i*1.2566f)*39,Mathf.Sin(i*1.2566f)*39);
                    break;
            }
            // Broken fascia and native buttresses below the deck emphasize suspended mass.
            for(int i=0;i<10;i++)
            {
                float a=(i+.25f)*Mathf.PI*2/10;
                float x=Mathf.Cos(a)*(c.Zone.Size.x*.39f), z=Mathf.Sin(a)*(c.Zone.Size.y*.38f);
                Put(c,"Support",x,z,-c.Assets["Support"].Bounds.size.y-.5f, i*36);
                Put(c,"Cornice",x,z,-c.Assets["Cornice"].Bounds.size.y-.1f,i*36);
            }
        }
        static void Seal(Context c)
        {
            // Original ceremonial inlay, no new physical obstacle and no changes to the base collider.
            var asset=c.Assets["Seal"];
            if(asset.Bounds.size.x>24 || asset.Bounds.size.z>24 || asset.Bounds.size.y>1)
            { c.Skips.Add("Medallon central incompatible con pavimento; simbolo solar posterior conservado"); return; }
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset.Prefab,c.Stage.gameObject.scene);
            go.transform.SetParent(c.Stage,true);
            go.transform.SetPositionAndRotation(c.Zone.Center+new Vector3(-asset.Bounds.center.x,.025f-asset.Bounds.min.y,-asset.Bounds.center.z),asset.Prefab.transform.localRotation);
            go.transform.localScale=asset.Prefab.transform.localScale;
            foreach(var collider in go.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
            foreach(var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) if(behaviour) behaviour.enabled=false;
            c.Count++;
        }
        // Dense entrance composition: architectural joints may touch/overlap deliberately.
        // The old whole-object occupied AABBs removed almost every connected facade piece.
        // Only footprint and real access reservations reject pieces in this single-zone pass.
        static void BuildPortico(Transform root,Zone zone,Dictionary<string,Asset> assets,bool undo=false)
        {
            var parent=root.Find(zone.Name);
            if(FindStage(parent)) throw new InvalidOperationException("La copia contiene una etapa anterior sin retirar.");
            var go=new GameObject(PorticoStage); go.transform.SetParent(parent,false);
            if(undo) Undo.RegisterCreatedObjectUndo(go,"Crear Portico Solar aprobado");
            new GameObject(Signature).transform.SetParent(go.transform,false);
            var c=new Context{Zone=zone,Parent=parent,Stage=go.transform,Assets=assets};
            c.Protected.AddRange(Reservations(zone));
            var groups=new Dictionary<string,Transform>();
            var counts=new Dictionary<string,int>();
            GameObject P(string group,string key,float x,float z,float y=0,float yaw=0)
            {
                Asset a=assets[key]; var rotation=Quaternion.Euler(0,yaw,0);
                Bounds b=TransformBounds(a.Bounds,Matrix4x4.Rotate(rotation));
                Vector3 position=zone.Center+new Vector3(x-b.center.x,y-b.min.y,z-b.center.z);
                b.center+=position;
                Bounds safe=b; safe.Expand(-.035f);
                if(!Inside(zone,b) || c.Protected.Any(r=>r.Intersects(safe)))
                { c.Skips.Add(group+" / "+key+" ("+x+","+z+")"); return null; }
                if(!groups.TryGetValue(group,out Transform groupRoot))
                { var child=new GameObject(group); child.transform.SetParent(go.transform,false); groups[group]=groupRoot=child.transform; }
                var item=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,go.scene);
                item.transform.SetParent(groupRoot,true);
                item.transform.SetPositionAndRotation(position,rotation*a.Prefab.transform.localRotation);
                item.transform.localScale=a.Prefab.transform.localScale;
                if(key=="Sun" || key=="Trim")
                {
                    var gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/Materials/Roof_Gold.mat");
                    if(gold) foreach(var renderer in item.GetComponentsInChildren<Renderer>())
                        renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>gold).ToArray();
                }
                foreach(var script in item.GetComponentsInChildren<MonoBehaviour>(true)) if(script) script.enabled=false;
                foreach(var body in item.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic=true; body.useGravity=false; }
                foreach(var audio in item.GetComponentsInChildren<AudioSource>(true)) audio.enabled=false;
                Physics.SyncTransforms();
                if(item.GetComponentsInChildren<Collider>().Any(v=>v.enabled && !v.isTrigger &&
                    c.Protected.Any(r=>r.Intersects(Inset(v.bounds)))))
                { Object.DestroyImmediate(item); c.Skips.Add(group+" / collider "+key); return null; }
                c.Count++; counts[group]=counts.TryGetValue(group,out int n)?n+1:1;
                return item;
            }
            float alongX(string key) => assets[key].Bounds.size.x>=assets[key].Bounds.size.z ? 0:90;
            Bounds rotated(string key,float yaw) => TransformBounds(assets[key].Bounds,Matrix4x4.Rotate(Quaternion.Euler(0,yaw,0)));
            // A complete ornamental pavement uses native modules, with top just above the existing deck.
            var paving=assets["Paving"].Bounds;
            for(float x=-36+paving.size.x/2;x<=36-paving.size.x/2+.01f;x+=paving.size.x)
                for(float z=-42+paving.size.z/2;z<=42-paving.size.z/2+.01f;z+=paving.size.z)
                    P("01 - Pavimento ceremonial","Paving",x,z,.025f-paving.size.y);

            // Paired structural piers leave an actual 18m clear opening. Header spans native 6m units.
            var col=assets["Column"].Bounds;
            int courses=Mathf.CeilToInt(12/col.size.y);
            float headerY=courses*col.size.y;
            float pierX=9+col.size.x/2;
            foreach(float sign in new[]{-1f,1f})
                for(int level=0;level<courses;level++) P("02 - Gran puerta transitable","Column",sign*pierX,0,level*col.size.y);
            for(int x=-6;x<=6;x+=6)
                P("02 - Gran puerta transitable","Base",x,0,headerY);
            P("02 - Gran puerta transitable","EntranceArch",0,0,headerY+2);
            P("02 - Gran puerta transitable","Sun",0,-2,headerY+4);

            // Continuous curtain wall joins the existing tower pair at (+/-18,0), not isolated landmarks.
            float wallYaw=alongX("Facade"); var wall=rotated("Facade",wallYaw);
            foreach(float sign in new[]{-1f,1f})
                for(float x=9+wall.size.x/2;x+wall.size.x/2<=35.5f;x+=wall.size.x-.08f)
                    for(float y=0;y<headerY-.1f;y+=wall.size.y)
                        P("03 - Fachada y murallas","Facade",sign*x,0,y,wallYaw);
            // Western enclosure intact; eastern enclosure deliberately broken around the secondary access.
            float sideYaw=wallYaw+90; var side=rotated("Facade",sideYaw);
            float edgeX=35-side.size.x/2;
            for(float z=-31+side.size.z/2;z+side.size.z/2<=35;z+=side.size.z-.08f)
            {
                P("03 - Fachada y murallas","Facade",-edgeX,z,0,sideYaw);
                if(z-side.size.z/2> -6 || z+side.size.z/2< -18)
                    P("03 - Fachada y murallas",z>20?"Wall":"Facade",edgeX,z,0,sideYaw);
            }

            // Two +4m terraces, each with a real 12m-wide native stair flight ascending northwards.
            foreach(float sign in new[]{-1f,1f})
            {
                string group=sign<0?"04 - Terraza occidental y escalinata":"05 - Terraza oriental y escalinata";
                for(int i=0;i<2;i++) for(int j=0;j<4;j++) for(int h=0;h<2;h++)
                    P(group,"Base",sign*(24+i*6),15+j*6,h*2);
                foreach(float lane in new[]{24f,30f}) P(group,"Stair",sign*lane,9,0,90);
                foreach(float z in new[]{18f,30f})
                    P(group,"Column",sign*32,z,4);
                P(group,"EntranceArch",sign*27,29,4,0);
                P(group,"Statue",sign*27,18,4,sign<0?90:270);
                P(group,"Sun",sign*27,34,8);
            }
            // Processional forecourt: statues, low plinths and lateral arcades; central approach stays open.
            foreach(float sign in new[]{-1f,1f})
            {
                for(int k=0;k<3;k++)
                {
                    float zz=-34+k*9;
                    P("06 - Procesion y esculturas","Base",sign*24,zz,0);
                    P("06 - Procesion y esculturas",k==1&&sign>0?"BrokenColumn":"Statue",sign*24,zz,2,sign<0?90:270);
                }
                P("07 - Arcadas y ruinas","Arch",sign*26,-25,0,90);
                P("07 - Arcadas y ruinas","BrokenArch",sign*28,37,0,90);
                P("07 - Arcadas y ruinas","Wall",sign*28,-37,0,wallYaw);
                P("07 - Arcadas y ruinas","BrokenColumn",sign*18,33);
                // Native stone cornice modules over the facade and buttresses below the suspended deck.
                for(int j=0;j<3;j++)
                {
                    P("08 - Cornisas y contrafuertes","Trim",sign*(15+j*6),-2,headerY+2,90);
                    P("08 - Cornisas y contrafuertes","Support",sign*29,-26+j*27,-assets["Support"].Bounds.size.y-.1f,90);
                }
            }
            Physics.SyncTransforms();
            string[] required={"01 - Pavimento ceremonial","02 - Gran puerta transitable","03 - Fachada y murallas",
                "04 - Terraza occidental y escalinata","05 - Terraza oriental y escalinata","06 - Procesion y esculturas"};
            foreach(string group in required)
                if(!counts.TryGetValue(group,out int n) || n<3) throw new InvalidOperationException("Composicion incompleta: "+group+". "+string.Join("; ",c.Skips.Take(8)));
            if(c.Count<85) throw new InvalidOperationException("Densidad insuficiente del Portico: "+c.Count);
            if(undo) Undo.RegisterFullObjectHierarchyUndo(go,"Registrar arquitectura completa del Portico");
            Debug.Log("Portico ampliado: "+c.Count+" prefabs, fachada conectada, vano 18m, dos terrazas +4m, escalinatas 12m y pavimento. "+c.Skips.Count+" omisiones: "+string.Join("; ",c.Skips.Take(15)));
        }
        static Bounds Inset(Bounds b) { b.Expand(-.035f); return b; }

        static void Violet(Context c,float x,float z)
        {
            var go=new GameObject("Corrupcion violeta - luz arquitectonica"); go.transform.SetParent(c.Stage,false);
            go.transform.position=c.Zone.Center+new Vector3(x,5,z);
            var light=go.AddComponent<Light>(); light.type=LightType.Point;
            light.color=new Color(.49f,.08f,1); light.intensity=3; light.range=13; light.shadows=LightShadows.None;
        }
        static void ValidateRoot(Transform root)
        {
            if(!root) throw new InvalidOperationException("Sin estructura para validar.");
            Physics.SyncTransforms(); int count=0;
            foreach(var z in ActiveZones)
            {
                var parent=root.Find(z.Name); var stage=FindStage(parent); if(!stage) continue; count++;
                if(stage.localPosition.sqrMagnitude>.0001f || (stage.localScale-Vector3.one).sqrMagnitude>.0001f ||
                    Quaternion.Angle(stage.localRotation,Quaternion.identity)>.01f) throw new InvalidOperationException("Transformacion de Architecture alterada.");
                var reservations=Reservations(z);
                foreach(var t in stage.GetComponentsInChildren<Transform>(true))
                {
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)
                        throw new InvalidOperationException("Script faltante en "+t.name);
                    if(PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                    {
                        var original=PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                        if(!original || (t.localScale-original.transform.localScale).sqrMagnitude>.0001f)
                            throw new InvalidOperationException("Escala de prefab alterada: "+t.name);
                    }
                }
                foreach(var renderer in stage.GetComponentsInChildren<Renderer>())
                    if(renderer.sharedMaterials.Any(m=>!m || !m.shader)) throw new InvalidOperationException("Material/shader faltante: "+renderer.name);
                foreach(var collider in stage.GetComponentsInChildren<Collider>())
                {
                    if(!collider.enabled || collider.isTrigger) continue;
                    if(collider is MeshCollider mesh && !mesh.sharedMesh) throw new InvalidOperationException("MeshCollider sin malla.");
                    if(reservations.Any(b=>b.Intersects(collider.bounds)))
                        throw new InvalidOperationException("Revisar collider en acceso: "+z.Name+" / "+collider.name);
                }
            }
            Debug.Log(count+" / 1 etapa presente: Portico Solar. Vano ceremonial 18m y conexiones existentes reservados; comprobacion conservadora de bounds, no NavMesh.");
        }

        sealed class RemainingPreview : PreviewSceneStage
        {
            public Transform Source, Copy; public Dictionary<string,Asset> Assets;
            protected override GUIContent CreateHeaderContent() => new GUIContent("Portico Solar - fachada monumental (temporal)");
            protected override bool OnOpenStage()
            {
                if(!Source || Assets==null || !base.OnOpenStage()) return false;
                try
                {
                    Scene previewScene=scene; previewScene.name="TEMP_CiudadDelSol_PorticoSolar";
                    var holder=new GameObject("Contexto temporal"); holder.SetActive(false);
                    SceneManager.MoveGameObjectToScene(holder,scene);
                    // Clone directly under an inactive preview-scene parent; imported scripts cannot run OnEnable.
                    var clone=new GameObject(Root); clone.transform.SetParent(holder.transform,false); Copy=clone.transform;
                    var sourceZone=Source.Find(Zones[0].Name);
                    var zoneCopy=Object.Instantiate(sourceZone.gameObject,Copy); zoneCopy.name=sourceZone.name;
                    zoneCopy.transform.SetPositionAndRotation(sourceZone.position,sourceZone.rotation);
                    // Only preview-owned copies may be removed; original hierarchy is never edited.
                    var old=FindStage(zoneCopy.transform); if(old) Object.DestroyImmediate(old.gameObject);
                    var landmarks=Source.Find("Arquitectura monumental - fuera de los pasos");
                    if(landmarks)
                    {
                        var context=new GameObject("Torres y arcos existentes (copias)"); context.transform.SetParent(Copy,false);
                        foreach(Transform landmark in landmarks)
                        {
                            Vector3 offset=landmark.position-Zones[0].Center;
                            if(Mathf.Abs(offset.x)>36 || Mathf.Abs(offset.z)>42 || Mathf.Abs(offset.y)>5) continue;
                            var item=Object.Instantiate(landmark.gameObject,context.transform);
                            item.transform.SetPositionAndRotation(landmark.position,landmark.rotation);
                        }
                    }
                    foreach(var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true)) if(behaviour) behaviour.enabled=false;
                    foreach(var audio in clone.GetComponentsInChildren<AudioSource>(true)) audio.enabled=false;
                    holder.SetActive(true);
                    Build(Copy,Zones[0],Assets,false);
                    ValidateRoot(Copy);
                    var rig=new GameObject("Iluminacion y camara de previsualizacion"); SceneManager.MoveGameObjectToScene(rig,scene);
                    var light=rig.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.6f;
                    rig.transform.rotation=Quaternion.Euler(48,-32,0);
                    var cameraGO=new GameObject("Camara de inspeccion"); cameraGO.transform.SetParent(rig.transform,false);
                    cameraGO.transform.position=Zones[0].Center+new Vector3(100,95,-125); cameraGO.transform.LookAt(Zones[0].Center+Vector3.up*9);
                    var camera=cameraGO.AddComponent<Camera>(); camera.farClipPlane=1800; camera.nearClipPlane=.3f;
                    camera.enabled=false; // Scene view is used interactively; no competing game camera.
                    Reviewed.Clear(); Reviewed.Add(0);
                    Selection.activeGameObject=clone;
                    if(SceneView.lastActiveSceneView)
                    {
                        var view=SceneView.lastActiveSceneView;
                        view.sceneLighting=true;
                        view.LookAt(Zones[0].Center+Vector3.up*9,Quaternion.Euler(36,-25,0),80,false);
                    }
                    // Unity restores a stage's previous view after OnOpenStage; frame after that restore.
                    EditorApplication.delayCall+=()=>
                    {
                        if(!this || StageUtility.GetCurrentStage()!=this || !Copy) return;
                        var view=SceneView.lastActiveSceneView;
                        if(view) { view.orthographic=false; view.LookAtDirect(Zones[0].Center+Vector3.up*9,Quaternion.Euler(36,-25,0),70); view.Repaint(); }
                        Selection.activeGameObject=null;
                    };
                    Source=null; Assets=null;
                    Debug.Log("Portico Solar: previsualizacion exclusiva lista. Las otras seis zonas no se copiaron ni generaron. Integracion deshabilitada y escena original sin guardar.");
                    return true;
                }
                catch(Exception e) { Debug.LogException(e); base.OnCloseStage(); return false; }
            }
        }
    }
}
