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
 public static class UmbralDeLasProfundidadesGenerator
 {
  const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity", Dungeon="Dungeon_Ciudad_del_Sol_Generated", Route="06-07 -", Root="Umbral_De_Las_Profundidades_Preview";
  const string Menu="ROM/Dungeon Ciudad del Sol/Umbral de las Profundidades/";
  const string ET="Assets/Standard Assets/Eternal Temple/Prefabs/", EG="Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/";
  static readonly Dictionary<string,string> Paths=new Dictionary<string,string> {
   {"Base",ET+"Pits and Platforms/Platform_Base_01.prefab"}, {"Rail",ET+"Railings and Decors/Railing_01.prefab"},
   {"Column",ET+"Columns/Column_01.prefab"}, {"Stair",ET+"Staircases/Staircase_01.prefab"},
   {"StairRail",ET+"Railings and Decors/Railing_Staircase_01.prefab"}, {"Pillar",EG+"SM_GranitePillar.prefab"},
   {"Arch",ET+"Bases/Arch_Wide_01.prefab"}, {"BrokenArch",ET+"Bases/Thin/Arch_03_Broken_01.prefab"},
   {"Wall",ET+"Buildings/Walls/1-Segment Long/Walls_2x1_01.prefab"}, {"BrokenWall",ET+"Damaged/Broken_Wall_01.prefab"},
   {"BrokenColumn",ET+"Damaged/Column_Broken_01.prefab"}, {"Altar",EG+"SM_Altar.prefab"}, {"Disk",EG+"SM_SunDisk2.prefab"},
   {"Torch",ET+"Lanterns/Torch_01.prefab"},
   {"Flag","Assets/LeartesStudios/The_Rally_Point/Art/Prefabs/SM_Flag_01.prefab"},
   {"Crystal","Assets/Hovl Studio/HSFiles/Models/HOVLCrystal.fbx"}
  };
  sealed class Asset {public GameObject Prefab; public Bounds Bounds;}
  sealed class Piece {public string Key; public GameObject Object; public Bounds Bounds;}
  sealed class Source {
   public Scene Scene; public Transform Root,Route; public string FileHash; public bool Dirty;
   public readonly List<Action> Checks=new List<Action>();
   public void Verify(bool integrating=false) {foreach(var c in Checks)c(); if((!integrating&&Scene.isDirty!=Dirty)||HashScene()!=FileHash)throw new InvalidOperationException("La escena original fue alterada.");}
  }
  sealed class Plan {
   public Source Source; public Transform Root,Context; public readonly List<Piece> Pieces=new List<Piece>();
   public readonly List<Vector3[]> Walks=new List<Vector3[]>(); public readonly List<float> Widths=new List<float>();
   public readonly List<Material> Materials=new List<Material>();
  }
  static bool Ready=>!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode;
  static string HashScene(){using(var s=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(s.ComputeHash(System.IO.File.ReadAllBytes(Target)));}
  [MenuItem(Menu+"1. Previsualizar arena y conexion (copia temporal) %#&u")]
  public static void Preview(){if(!Ready)return;try{Close(); var s=ScriptableObject.CreateInstance<UmbralPreview>();s.Source=Inspect();s.Assets=LoadAssets();InspectSpace(s.Source);StageUtility.GoToStage(s,true);}catch(Exception e){Debug.LogException(e);}}
  [MenuItem(Menu+"2. Validar arena, puente y escalinata")]
  public static void Validate(){try{var s=StageUtility.GetCurrentStage() as UmbralPreview;if(!s||s.Plan==null)throw new InvalidOperationException("Abri primero la previsualizacion temporal.");ValidatePlan(s.Plan);}catch(Exception e){Debug.LogException(e);}}
  [MenuItem(Menu+"3. Cerrar previsualizacion sin guardar")]
  public static void Close(){if(Ready&&StageUtility.GetCurrentStage() is UmbralPreview)StageUtility.GoToMainStage();}
  [MenuItem(Menu+"4. Vista general de la arena")]
  public static void View(){if(!(StageUtility.GetCurrentStage() is UmbralPreview))return;var v=SceneView.lastActiveSceneView;if(v){v.sceneLighting=true;v.LookAtDirect(new Vector3(162,16,22),Quaternion.Euler(38,-30,0),62);v.Repaint();}}
  [MenuItem(Menu+"5. Vista lateral del puente y descenso")]
  public static void Side(){if(!(StageUtility.GetCurrentStage() is UmbralPreview))return;var v=SceneView.lastActiveSceneView;if(v){v.LookAtDirect(new Vector3(150,12,-15),Quaternion.Euler(12,95,0),52);v.Repaint();}}
        [MenuItem(Menu+"6. Vista del acceso bajo la arena")]
        public static void CryptView()
        {
            if(!(StageUtility.GetCurrentStage() is UmbralPreview))return;
            var view=SceneView.lastActiveSceneView;
            if(view){view.orthographic=false;view.LookAtDirect(new Vector3(162,7,23),Quaternion.Euler(0,0,0),7);view.Repaint();}
        }
        const string IntegratedRoot="Umbral_De_Las_Profundidades", Signature="__ROM_Umbral_639_Approved_v1";
        const string MaterialFolder="Assets/ROM/Generated/CiudadDelSol/UmbralProfundidades/Materials";
        [MenuItem(Menu+"7. Integrar version aprobada y guardar tras validar")]
        public static void IntegrateApproved()
        {
            if(!Ready)return;
            int undo=-1;bool saved=false;Source source=null;Plan plan=null;
            var createdAssets=new List<string>();
            try
            {
                var preview=StageUtility.GetCurrentStage() as UmbralPreview;
                string approved=preview&&preview.PreviewRoot?Layout(preview.PreviewRoot):null;
                Close();source=Inspect();
                if(source.Dirty)throw new InvalidOperationException("La original contiene cambios previos sin guardar. No se guardaran con esta integracion.");
                var allRoots=source.Scene.GetRootGameObjects();
                var existing=allRoots.Where(g=>g.name==IntegratedRoot).ToArray();
                if(existing.Length>0)
                {
                    if(existing.Length!=1||!existing[0].transform.Find(Signature))throw new InvalidOperationException("Umbral existente ambiguo; no se sobrescribira ni duplicara.");
                    if(existing[0].GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=639)throw new InvalidOperationException("La integracion existente no conserva sus 639 prefabs.");
                    Debug.Log("UMBRAL_YA_INTEGRADO: una unica estructura aprobada de 639 prefabs. No se duplico ni sobrescribio.",existing[0]);return;
                }
                if(allRoots.Count(g=>g.name=="Salida_Meridional_Generated")!=1)throw new InvalidOperationException("Salida Meridional ausente o duplicada.");
                if(allRoots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name==Root||t.name=="Contexto original - solo copias"||t.name.StartsWith("TEMP_CiudadDelSol",StringComparison.Ordinal)))
                    throw new InvalidOperationException("La original contiene objetos temporales inesperados.");
                var rootIDs=allRoots.Select(g=>g.GetInstanceID()).ToArray();
                string backup="Temp/ROM_Backups/Dungeon_Isla_del_Sol_before_Umbral_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));System.IO.File.Copy(Target,backup,false);
                var assets=LoadAssets();InspectSpace(source);
                Undo.IncrementCurrentGroup();undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Integrar Umbral de las Profundidades aprobado");
                plan=Build(source,source.Root,source.Scene,assets,true);
                if(plan.Pieces.Count!=639)throw new InvalidOperationException("La version aprobada debe contener exactamente 639 prefabs.");
                string generated=Layout(plan.Root);
                if(approved!=null&&generated!=approved)throw new InvalidOperationException("El resultado difiere de la ultima previsualizacion. Integracion cancelada. preview="+approved+" generado="+generated);
                if(plan.Pieces.Select(p=>p.Key+"|"+p.Object.transform.position.ToString("F5")+"|"+p.Object.transform.rotation.ToString("F5")).Distinct().Count()!=639)
                    throw new InvalidOperationException("Se detectaron prefabs duplicados.");
                ValidatePlan(plan,true);
                string[] names={"Umbral_Violeta.mat","Umbral_Estandartes_Violeta.mat","Umbral_Incrustaciones_Oro.mat"};
                if(plan.Materials.Count!=names.Length)throw new InvalidOperationException("Cantidad de materiales distinta de la aprobada.");
                EnsureFolder(MaterialFolder);
                for(int i=0;i<names.Length;i++)
                {
                    string path=MaterialFolder+"/"+names[i];
                    if(AssetDatabase.LoadMainAssetAtPath(path)||System.IO.File.Exists(path))throw new InvalidOperationException("Ya existe un material en "+path+"; no se sobrescribira.");
                    var m=plan.Materials[i];m.hideFlags=HideFlags.None;AssetDatabase.CreateAsset(m,path);createdAssets.Add(path);AssetDatabase.SaveAssetIfDirty(m);
                    if(!EditorUtility.IsPersistent(m))throw new InvalidOperationException("No se pudo persistir "+path);
                }
                if(Layout(plan.Root)!=generated)throw new InvalidOperationException("Guardar materiales altero la apariencia o distribucion.");
                foreach(var component in plan.Root.GetComponentsInChildren<Component>(true))
                    if(component&&PrefabUtility.IsPartOfPrefabInstance(component)&&PrefabUtility.GetCorrespondingObjectFromSource(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                plan.Root.name=IntegratedRoot;
                var sign=new GameObject(Signature);sign.transform.SetParent(plan.Root,false);sign.hideFlags=HideFlags.HideInHierarchy;
                if(!rootIDs.SequenceEqual(source.Scene.GetRootGameObjects().Where(g=>g!=plan.Root.gameObject).Select(g=>g.GetInstanceID())))throw new InvalidOperationException("Cambio inesperado en raices originales.");
                source.Verify(true);
                if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage()||source.Scene.path!=Target)throw new InvalidOperationException("Contexto de guardado incorrecto.");
                EditorSceneManager.MarkSceneDirty(source.Scene);
                if(!EditorSceneManager.SaveScene(source.Scene,Target,false))throw new InvalidOperationException("Fallo el guardado de la escena.");
                saved=true;Undo.CollapseUndoOperations(undo);
                if(source.Scene.isDirty||source.Scene.GetRootGameObjects().Count(g=>g.name==IntegratedRoot)!=1||plan.Pieces.Any(p=>!p.Object))throw new InvalidOperationException("Guardado realizado pero verificacion final incompleta.");
                Debug.Log("UMBRAL_INTEGRADO_GUARDADO: 639 prefabs, distribucion aprobada="+generated+", tres materiales URP persistentes, cuatro luces locales YA aprobadas; arena, puente, descenso y cripta con abertura libre. Siete zonas, nueve caminos y Salida Meridional intactos. Original guardada en "+Target+". Backup previo: "+backup,plan.Root);
                Selection.activeGameObject=plan.Root.gameObject;
            }
            catch(Exception e)
            {
                if(!saved)
                {
                    if(undo>=0)Undo.RevertAllDownToGroup(undo);
                    foreach(var path in createdAssets)AssetDatabase.DeleteAsset(path);
                    if(plan!=null)foreach(var m in plan.Materials)if(m&&!EditorUtility.IsPersistent(m))Object.DestroyImmediate(m);
                    if(source!=null)
                    {
                        try{source.Verify(true);}catch(Exception preservation){Debug.LogException(preservation);}
                    }
                }
                Debug.LogError("UMBRAL_INTEGRACION_DETENIDA: "+e.Message);Debug.LogException(e);
            }
        }
        static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            string parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);
            if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path))))throw new InvalidOperationException("No se pudo crear "+path);
        }
        static string Layout(Transform root)
        {
            var rows=new List<string>();var invariant=System.Globalization.CultureInfo.InvariantCulture;
            string V(Vector3 v)=>v.ToString("F5",invariant);
            string F(float f)=>f.ToString("R",invariant);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(t.name==Signature)continue;
                string path=t==root?"":AnimationUtility.CalculateTransformPath(t,root);
                var row=path+"|"+V(t.localPosition)+"|"+t.localRotation.ToString("F5",invariant)+"|"+V(t.localScale)+"|"+t.gameObject.activeSelf;
                if(PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))row+="|prefab:"+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                foreach(var r in t.GetComponents<Renderer>())
                {
                    row+="|renderer:"+r.enabled;
                    foreach(var m in r.sharedMaterials)
                    {
                        if(!m){row+="|null";continue;}
                        row+="|"+m.shader.name;
                        foreach(string name in new[]{"_BaseColor","_EmissionColor"})if(m.HasProperty(name))row+="|"+name+":"+m.GetColor(name).ToString("F5");
                        if(m.HasProperty("_BaseMap"))row+="|texture:"+AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap"));
                    }
                }
                foreach(var c in t.GetComponents<Collider>())
                {
                    row+="|collider:"+c.GetType().Name+"|"+c.enabled+"|"+c.isTrigger;
                    if(c is BoxCollider box)row+="|"+V(box.center)+"|"+V(box.size);
                    if(c is MeshCollider mesh)row+="|"+(mesh.sharedMesh?mesh.sharedMesh.name:"null")+"|"+mesh.convex;
                }
                foreach(var l in t.GetComponents<Light>())row+="|light:"+l.enabled+"|"+l.type+"|"+l.color.ToString("F5")+"|"+F(l.intensity)+"|"+F(l.range)+"|"+l.shadows;
                foreach(var line in t.GetComponents<LineRenderer>()){row+="|line:"+line.useWorldSpace+"|"+line.loop+"|"+F(line.startWidth);for(int i=0;i<line.positionCount;i++)row+="|"+V(line.GetPosition(i));}
                rows.Add(row);
            }
            rows.Sort(StringComparer.Ordinal);System.IO.File.WriteAllLines(root.gameObject.scene.path==Target?"Temp/ROM_Umbral_GeneratedLayout.txt":"Temp/ROM_Umbral_PreviewLayout.txt",rows);return Hash128.Compute(string.Join("\n",rows)).ToString();
        }
        static Source Inspect()
        {
            var scene = SceneManager.GetActiveScene();
            if(StageUtility.GetCurrentStage() != StageUtility.GetMainStage() || scene.path != Target)
                throw new InvalidOperationException("Activa Dungeon_Isla_del_Sol en el escenario principal.");
            var roots = scene.GetRootGameObjects().Where(g => g.name == Dungeon).ToArray();
            if(roots.Length != 1) throw new InvalidOperationException("Base ausente o duplicada. No se regenerara.");
            var root = roots[0].transform;
            if(root.position.sqrMagnitude > .0001f || Quaternion.Angle(root.rotation,Quaternion.identity) > .01f ||
                (root.lossyScale-Vector3.one).sqrMagnitude > .0001f)
                throw new InvalidOperationException("Transformacion de la base inesperada.");
            string[] routes = {"01-02 -","02-03 -","03-04 -","03-05 -","04-05 -","03-06 -","05-07 -","06-07 -","01-06 -"};
            foreach(var prefix in routes)
                if(root.Cast<Transform>().Count(t => t.name.StartsWith(prefix,StringComparison.Ordinal)) != 1)
                    throw new InvalidOperationException("Recorrido ausente o duplicado: " + prefix);
            string[] approved = {"01 - Entrada Portico Solar/PorticoSolar_Architecture","02 - Jardines Dorados/JardinesDorados_Architecture",
                "03 - Plaza del Eclipse/PlazaDelEclipse_Architecture","04 - Archivos Solares/ArchivosSolares_Architecture",
                "05 - Claustro del Eclipse/ClaustroDelEclipse_Architecture","06 - Santuario Profano/SantuarioProfano_Architecture",
                "07 - El Sol Corrompido/ElSolCorrompido_Architecture"};
            foreach(var path in approved) if(!root.Find(path)) throw new InvalidOperationException("Falta arquitectura aprobada: " + path);
            var source = new Source {Scene=scene, Root=root, Route=root.Cast<Transform>().Single(t=>t.name.StartsWith(Route,StringComparison.Ordinal)), FileHash=HashScene(), Dirty=scene.isDirty};
            if(!source.Route) throw new InvalidOperationException("No se encontro el puente inferior original.");
            // Compare every existing component, transform and child list after creating the temporary review.
            foreach(var originalRoot in scene.GetRootGameObjects())
                foreach(var t in originalRoot.GetComponentsInChildren<Transform>(true))
                {
                    var original = t; var parent=t.parent; var position=t.localPosition; var rotation=t.localRotation; var scale=t.localScale;
                    var children=t.Cast<Transform>().Select(c=>c.GetInstanceID()).ToArray();
                    var components=t.GetComponents<Component>().Select(c=>c?c.GetInstanceID():0).ToArray();
                    string name=t.name; bool active=t.gameObject.activeSelf; int sibling=t.GetSiblingIndex();
                    source.Checks.Add(() =>
                    {
                        if(!original || original.parent!=parent || original.localPosition!=position || original.localRotation!=rotation ||
                            original.localScale!=scale || original.name!=name || original.gameObject.activeSelf!=active || original.GetSiblingIndex()!=sibling ||
                            !children.SequenceEqual(original.Cast<Transform>().Select(c=>c.GetInstanceID())) ||
                            !components.SequenceEqual(original.GetComponents<Component>().Select(c=>c?c.GetInstanceID():0)))
                            throw new InvalidOperationException("Objeto original alterado: " + name);
                    });
                    foreach(var c in t.GetComponents<Component>())
                    {
                        if(!c || c is Transform) continue;
                        var component=c; string json=EditorJsonUtility.ToJson(c);
                        source.Checks.Add(() => { if(!component || EditorJsonUtility.ToJson(component)!=json) throw new InvalidOperationException("Componente original alterado: " + name); });
                    }
                }
            return source;
        }

        static Dictionary<string,Asset> LoadAssets()
        {
            var result = new Dictionary<string,Asset>();
            foreach(var path in Paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path.Value);
                if(!prefab) throw new InvalidOperationException("Falta prefab: " + path.Value);
                var normalize=Matrix4x4.TRS(Vector3.zero,prefab.transform.localRotation,prefab.transform.localScale)*prefab.transform.worldToLocalMatrix;
                Bounds bounds=default; bool found=false;
                foreach(var f in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r=f.GetComponent<MeshRenderer>();
                    if(!r || !r.enabled || !f.sharedMesh || r.name.EndsWith("_Shadow",StringComparison.OrdinalIgnoreCase)) continue;
                    bool active=true;
                    for(var t=f.transform;t;t=t.parent) { if(!t.gameObject.activeSelf) active=false; if(t==prefab.transform) break; }
                    if(!active) continue;
                    var b=TransformBounds(f.sharedMesh.bounds,normalize*f.transform.localToWorldMatrix);
                    if(found) bounds.Encapsulate(b); else { bounds=b; found=true; }
                }
                if(!found) throw new InvalidOperationException("Prefab sin malla: " + path.Value);
                result[path.Key]=new Asset{Prefab=prefab,Bounds=bounds};
            }
            if((result["Base"].Bounds.size-new Vector3(6,2,6)).sqrMagnitude>.01f)
                throw new InvalidOperationException("El modulo de piedra ya no mide 6 x 2 x 6.");
            return result;
        }
        static void InspectSpace(Source source)
        {
            Physics.SyncTransforms();
            var cavity=new Bounds(new Vector3(162,28,42),new Vector3(83.8f,20,83.8f));
            var blockers=source.Root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&c.bounds.Intersects(cavity)).ToArray();
            if(blockers.Length>0) throw new InvalidOperationException("La cavidad prevista esta ocupada: "+blockers[0].name+" "+blockers[0].bounds);
            var sanctuary=source.Root.Find("06 - Santuario Profano");
            var bounds=sanctuary.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>r.bounds).ToArray();
            var sum=bounds[0]; foreach(var b in bounds.Skip(1))sum.Encapsulate(b);
            Debug.Log("UMBRAL_INSPECCION: Santuario Renderer.bounds="+sum+"; arena desplazada +12m: X120..204 Z0..84 Y20; intrados Y16; descanso Y4, altura libre bruta 12m y libre bajo la cubierta de cripta 9.38m; cavidad libre de colliders. Camino oriental X222..234. El puente eleva su tablero a Y12 para superar la baranda original del borde norte sin retirarla.");
        }
        static Plan Build(Source source,Transform context,Scene scene,Dictionary<string,Asset> assets,bool undo=false)
        {
            var root=new GameObject(Root);SceneManager.MoveGameObjectToScene(root,scene);
            if(undo)Undo.RegisterCreatedObjectUndo(root,"Integrar Umbral aprobado");
            var plan=new Plan{Root=root.transform,Context=context,Source=source};
            var groups=new Dictionary<string,Transform>();var occupied=new HashSet<string>();
            float offset=12; // Rigid translation of the preserved arena; the bridge and new access use world coordinates.
            Transform Group(string name){if(!groups.TryGetValue(name,out var t)){var g=new GameObject(name);g.transform.SetParent(root.transform,false);groups[name]=t=g.transform;}return t;}
            GameObject P(string group,string key,float x,float z,float y,float yaw=0,bool top=false)
            {
                z+=offset;
                var asset=assets[key];var q=Quaternion.Euler(0,yaw,0);var b=TransformBounds(asset.Bounds,Matrix4x4.Rotate(q));
                var pivot=new Vector3(x-b.center.x,y-(top?b.max.y:b.min.y),z-b.center.z);b.center+=pivot;
                string id=key+"|"+pivot.ToString("F3")+"|"+yaw;
                if(!occupied.Add(id))throw new InvalidOperationException("Pieza duplicada: "+id);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(asset.Prefab,scene);go.transform.SetParent(Group(group),true);
                go.transform.SetPositionAndRotation(pivot,q*asset.Prefab.transform.localRotation);go.transform.localScale=asset.Prefab.transform.localScale;
                DisableImportedBehaviour(go);
                if(key!="Flag"&&key!="Crystal"&&!go.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))
                    foreach(var f in go.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled)f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
                plan.Pieces.Add(new Piece{Key=key,Object=go,Bounds=b});return go;
            }
            var floors=new HashSet<Vector3>();
            void Floor(string group,float x,float z,float y){if(floors.Add(new Vector3(x,y,z+offset)))P(group,"Base",x,z,y,0,true);}
            void Walk(Vector3 a,Vector3 b,float width){plan.Walks.Add(new[]{a+Vector3.forward*offset,b+Vector3.forward*offset});plan.Widths.Add(width);}
            void Rails(float x,float z,float y,float width){P("04 - Pretiles","Rail",x-width/2+.53f,z,y);P("04 - Pretiles","Rail",x+width/2-.53f,z,y);}
            void Flight(string group,float x,float lowZ,float lowY,int lanes,int direction=1)
            {
                float width=lanes*6;
                for(int lane=0;lane<lanes;lane++)P(group,"Stair",x+(lane+.5f)*6-width/2,lowZ+3*direction,lowY,direction>0?90:270);
                // Native sloping rails keep their original pivot and align with the original generator.
                foreach(float sign in new[]{-1f,1f})
                {
                    var a=assets["StairRail"];var go=(GameObject)PrefabUtility.InstantiatePrefab(a.Prefab,scene);
                    go.transform.SetParent(Group("04 - Pretiles inclinados"),true);
                    go.transform.SetPositionAndRotation(new Vector3(x+sign*(width/2+.55f),lowY+4+1.5f-a.Bounds.max.y,lowZ+6*direction+offset),Quaternion.Euler(0,direction>0?180:0,0)*a.Prefab.transform.localRotation);
                    go.transform.localScale=a.Prefab.transform.localScale;DisableImportedBehaviour(go);
                    var bb=TransformBounds(a.Bounds,Matrix4x4.TRS(go.transform.position,Quaternion.Euler(0,direction>0?180:0,0),Vector3.one));
                    plan.Pieces.Add(new Piece{Key="StairRail",Object=go,Bounds=bb});
                }
                Walk(new Vector3(x,lowY,lowZ-.15f*direction),new Vector3(x,lowY+4,lowZ+6.15f*direction),Mathf.Clamp(width-2,4,16));
            }
            // Octagonal native masonry, including a complete foundation course; no enlarged slabs.
            for(int ix=-7;ix<7;ix++)for(int iz=-7;iz<7;iz++)
            {
                float x=ix*6+3,z=iz*6+3;
                if(Mathf.Abs(x)+Mathf.Abs(z)+6>60.01f)continue;
                Floor("01 - Arena octagonal",162+x,30+z,20);Floor("02 - Espesor y contrafuertes",162+x,30+z,18);
                if((ix+7)%3==0&&(iz+7)%3==0)P("02 - Espesor y contrafuertes","Pillar",162+x==159&&(30+z==-9||30+z==9)?153:162+x,30+z,16,0,true);
            }
            offset=0;
            // One bridge with a 6m approach threaded between preserved forecourt ruins,
            // widening to a 12m stone span; four 4m flights reach the arena at Y20.
            Flight("03 - Puente del Santuario",129,-42,4,1);Flight("03 - Puente del Santuario",129,-36,8,1);
            foreach(float x in new[]{129f,135f})
            {
                if(x==129)foreach(float y in new[]{6f,8f})Floor("02 - Apoyos sobre patio existente",x,-33,y);
                foreach(float z in new[]{-27f,-21f})Floor("03 - Puente del Santuario",x,z,12);
                foreach(float y in new[]{6f,8f,10f})Floor("02 - Apoyos sobre patio existente",x,-27,y);
            }
            Rails(132,-27,12,12);Rails(132,-21,12,12);
            Flight("03 - Puente del Santuario",132,-18,12,2);Flight("03 - Puente del Santuario",132,-12,16,2);
            foreach(float x in new[]{129f,135f})foreach(float z in new[]{-3f,3f,9f,15f,21f})Floor("03 - Puente del Santuario",x,z,20);
            foreach(float z in new[]{-3f,3f,9f,15f})Rails(132,z,20,12);
            foreach(float x in new[]{129f,135f})P("02 - Espesor y contrafuertes","Pillar",x,9,18,0,true);
            foreach(float x in new[]{129f,135f})P("02 - Espesor y contrafuertes","Column",x,-21,10,0,true);
            Walk(new Vector3(129,4,-46),new Vector3(129,4,-42.2f),4);
            Walk(new Vector3(129,12,-29.8f),new Vector3(132,12,-27),4);
            Walk(new Vector3(132,12,-27),new Vector3(132,12,-18.2f),7);
            Walk(new Vector3(132,20,-5.8f),new Vector3(132,20,27),7);
            Walk(new Vector3(132,20,27),new Vector3(162,20,27),7);
            // One CENTRAL inward descent. A level west gallery reaches its outward-facing head.
            // The final flight (Z0..6) and the lower vestibule lie BELOW the preserved arena.
            foreach(float x in new[]{147f,153f,159f,165f,171f})
            {
                Floor("05 - Atrio superior del descenso",x,-21,20);Floor("05 - Atrio superior del descenso",x,-21,18);
                P("05 - Apoyos del atrio","Pillar",x,-21,16,0,true);
                P("04 - Pretiles del atrio","Rail",x,-23.47f,20,90);
            }
            foreach(float z in new[]{-15f,-9f,-3f})
            {
                Floor("05 - Galeria horizontal de acceso",147,z,20);Floor("05 - Galeria horizontal de acceso",147,z,18);
                P("05 - Apoyos del atrio","Pillar",147,z,16,0,true);Rails(147,z,20,6);
            }
            Rails(159,-21,20,30);
            Walk(new Vector3(147,20,3),new Vector3(147,20,-20.5f),4);
            Walk(new Vector3(147,20,-21),new Vector3(162,20,-21),4);
            Walk(new Vector3(162,20,-21),new Vector3(162,20,-18.2f),16);
            Walk(new Vector3(147,20,3),new Vector3(147,20,6),4);
            Walk(new Vector3(147,20,6),new Vector3(162,20,6),4);
            for(int i=0;i<4;i++)
            {
                float lowZ=-12+i*6,lowY=16-i*4;
                Flight("05 - Unica escalinata central hacia la cripta",162,lowZ,lowY,4,-1);
                foreach(float x in new[]{153f,159f,165f,171f})Floor("05 - Basamento de la escalinata",x,lowZ-3,lowY);
                foreach(float x in new[]{153f,162f,171f})P("05 - Apoyos del descenso","Pillar",x,lowZ-3,lowY-2,0,true);
            }
            // Small architectural vestibule only: no chambers, portal or gameplay.
            foreach(float x in new[]{153f,159f,165f,171f})foreach(float z in new[]{9f,15f,21f,27f})
            {
                Floor("06 - Descanso bajo la arena",x,z,4);
                if(z<=21)Floor("06 - Cubierta de la cripta",x,z,15.38f);
            }
            foreach(float x in new[]{150f,174f})foreach(float z in new[]{19.5f})foreach(float y in new[]{4f,8.69f})
                P("06 - Muros de la cripta","Wall",x,z,y);
            P("06 - Arco monumental bajo la arena","Arch",162,22.5f,4,90);
            // Preserve a true 12m central exit: protective end rails only at its flanks.
            foreach(float x in new[]{153f,171f})P("06 - Protecciones laterales del umbral","Rail",x,29.47f,4,90);
            foreach(float z in new[]{9f,15f,27f})Rails(162,z,4,24);
            Walk(new Vector3(162,20,-18),new Vector3(162,4,6),16);
            Walk(new Vector3(162,4,6),new Vector3(162,4,29.5f),4);
            // Lateral exploration of the flat landing, safely before the recessed doorway.
            Walk(new Vector3(156,4,12),new Vector3(168,4,12),4);
            // Follow exposed native floor edges, leaving only the two intended mouths open.
            foreach(var f in floors.Where(v=>Mathf.Abs(v.y-20)<.01f&&v.z>=3).ToArray())
            foreach(var d in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
            {
                if(floors.Contains(f+d*6))continue;
                var edge=f+d*3;
                if(edge.z<.1f&&edge.x>144&&edge.x<150)continue;
                // Bridge lateral protection was already placed outside the octagon.
                if(f.z<18&&f.x<140)continue;
                P("04 - Borde octagonal protegido","Rail",edge.x-d.x*.53f,edge.z-d.z*.53f,20,d.x==0?90:0);
            }
            offset=12;
            // Rear solar dais, separated from the fighting circle by a wide ambulatory.
            foreach(float x in new[]{153f,159f,165f,171f})foreach(float z in new[]{57f,63f})
            {Floor("07 - Santuario solar posterior",x,z,22);Floor("07 - Santuario solar posterior",x,z,24);}
            Flight("07 - Escalinata del altar",162,48,20,2);
            P("07 - Santuario solar posterior","Altar",162,61,24);
            P("07 - Santuario solar posterior","Disk",162,61,24+assets["Altar"].Bounds.size.y,45);
            foreach(float x in new[]{153f,171f})P("07 - Fachada solar","Pillar",x,63,24);
            P("07 - Fachada solar","Arch",162,61.5f,24,90);
            // Perimeter pillars and damaged side galleries: deliberately asymmetric ruins.
            var pillars=new[]{new Vector2(126,18),new Vector2(126,36),new Vector2(138,54),new Vector2(150,66),new Vector2(174,66),new Vector2(186,54),new Vector2(198,36),new Vector2(192,18),new Vector2(180,0),new Vector2(150,0)};
            for(int i=0;i<pillars.Length;i++)
            {
                var p=pillars[i];P("08 - Columnata monumental",i==3?"BrokenColumn":"Pillar",p.x,p.y,20,i*29);
                if(i==1||i==6)P("08 - Arcos deteriorados","BrokenArch",p.x+(i==1?3:-3),p.y+9,20,90);
            }
            foreach(var p in new[]{new Vector2(126,24),new Vector2(126,42),new Vector2(198,24),new Vector2(198,42),new Vector2(144,60),new Vector2(180,60)})
                P("09 - Muros y ruinas","BrokenWall",p.x,p.y,20,p.x==126||p.x==198?0:90);
            foreach(float x in new[]{150f,174f})foreach(float y in new[]{20f,24.69f})P("09 - Fachada posterior derruida","Wall",x,68,y,90);
            foreach(float z in new[]{24f,42f}){P("08 - Galeria occidental","Arch",127,z,20);P("08 - Galeria oriental","Arch",197,z,20);}
            foreach(var p in new[]{new Vector2(138,48),new Vector2(186,48),new Vector2(186,12),new Vector2(144,6),new Vector2(192,42)})
                P("09 - Muros y ruinas","BrokenColumn",p.x,p.y,20,37);
            var violet=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Umbral - violeta temporal",hideFlags=HideFlags.HideAndDontSave};
            violet.SetColor("_BaseColor",new Color(.29f,.025f,.55f));violet.EnableKeyword("_EMISSION");violet.SetColor("_EmissionColor",new Color(.45f,.015f,.85f)*1.7f);plan.Materials.Add(violet);
            var cloth=new Material(violet){name="Umbral - estandarte violeta temporal"};cloth.SetColor("_EmissionColor",Color.black);plan.Materials.Add(cloth);
            void Paint(GameObject go,Material material){foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();}
            // Real static flag mesh, hung from native rear pillars; no animated/gameplay behaviours.
            foreach(var f in new[]{new Vector3(138,28,51.95f),new Vector3(186,28,51.95f),new Vector3(153,32,60.95f),new Vector3(171,32,60.95f)})
            {var flag=P("10 - Estandartes violetas","Flag",f.x,f.z,f.y,90);Paint(flag,cloth);}
            foreach(var p in new[]{new Vector2(144,-3),new Vector2(180,-3),new Vector2(126,36),new Vector2(198,36),new Vector2(144,54),new Vector2(180,54)})
            {
                P("11 - Antorchas y cristales","Column",p.x,p.y,20);
                P("11 - Antorchas y cristales","Torch",p.x,p.y,26.83f);
                var crystal=P("11 - Antorchas y cristales","Crystal",p.x,p.y,28);Paint(crystal,violet);
            }
            foreach(var p in new[]{new Vector2(144,-3),new Vector2(180,-3),new Vector2(144,54),new Vector2(180,54)})
            {
                var g=new GameObject("Resplandor local violeta - sin sombras");g.transform.SetParent(Group("11 - Antorchas y cristales"),false);g.transform.position=new Vector3(p.x,28,p.y+offset);
                var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.58f,.06f,1);l.intensity=2;l.range=9;l.shadows=LightShadows.None;
            }
            // Flush ornamental solar inlay: line renderers have no colliders and share original gold.
            var gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Sun_Temple/Content/Meshes/Main/Materials/Roof_Gold.mat");
            if(!gold)throw new InvalidOperationException("Falta material solar original.");
            var inlay=new Material(gold){name="Umbral - oro de incrustacion temporal",hideFlags=HideFlags.HideAndDontSave};
            inlay.shader=Shader.Find("Universal Render Pipeline/Unlit");inlay.SetTexture("_BaseMap",null);inlay.SetColor("_BaseColor",new Color(1,.64f,.08f));inlay.SetFloat("_Cull",0);plan.Materials.Add(inlay);
            void Inlay(string name,Vector3[] points,bool loop,float width)
            {
                var g=new GameObject(name);g.transform.SetParent(Group("12 - Pavimento solar dorado"),false);var line=g.AddComponent<LineRenderer>();
                line.sharedMaterial=inlay;line.useWorldSpace=true;line.loop=loop;line.positionCount=points.Length;line.SetPositions(points.Select(p=>p+Vector3.forward*offset).ToArray());line.startWidth=line.endWidth=width;
                line.alignment=LineAlignment.TransformZ;g.transform.rotation=Quaternion.Euler(-90,0,0);line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            foreach(float radius in new[]{5f,19f,22f,24f})Inlay("Anillo solar "+radius,Enumerable.Range(0,128).Select(i=>new Vector3(162+Mathf.Cos(i*Mathf.PI/64)*radius,20.09f,30+Mathf.Sin(i*Mathf.PI/64)*radius)).ToArray(),true,.3f);
            for(int i=0;i<16;i++){float a=i*Mathf.PI/8;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Inlay("Rayo solar "+i,new[]{new Vector3(162,20.095f,30)+d*6,new Vector3(162,20.095f,30)+d*11,new Vector3(162,20.095f,30)+Quaternion.Euler(0,5,0)*d*7},false,.3f);}
            Walk(new Vector3(144,20,30),new Vector3(180,20,30),16);Walk(new Vector3(162,20,6),new Vector3(162,20,47.8f),12);
            Physics.SyncTransforms();return plan;
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
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true);
            var fallback=renderers.SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m);
            foreach(var r in renderers)
            {
                if(r.name.EndsWith("_Shadow",StringComparison.OrdinalIgnoreCase)) r.enabled=false;
                if(fallback&&r.sharedMaterials.Any(m=>!m)) r.sharedMaterials=r.sharedMaterials.Select(m=>m?m:fallback).ToArray();
                if(fallback&&r.sharedMaterials.Length==0) r.sharedMaterials=new[]{fallback};
            }
            foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true)) if(b) b.enabled=false;
            foreach(var a in root.GetComponentsInChildren<AudioSource>(true)) a.enabled=false;
            foreach(var l in root.GetComponentsInChildren<Light>(true)) l.enabled=false;
            foreach(var c in root.GetComponentsInChildren<Camera>(true)) c.enabled=false;
            foreach(var p in root.GetComponentsInChildren<ParticleSystem>(true)) { var m=p.main; m.playOnAwake=false; p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
            foreach(var b in root.GetComponentsInChildren<Rigidbody>(true)) { b.isKinematic=true; b.useGravity=false; }
        }

        static void ValidatePlan(Plan plan,bool integrating=false)
        {
            Physics.SyncTransforms();
            var all=plan.Context.GetComponentsInChildren<Collider>().Concat(plan.Root.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger).ToArray();
            var physics=plan.Root.gameObject.scene.GetPhysicsScene();var hits=new Collider[128];
            float Surface(float x,float z,float expected)
            {
                float y=float.NegativeInfinity;foreach(var c in all)
                    if(c.bounds.max.y>=expected-1.5f&&c.Raycast(new Ray(new Vector3(x,expected+1.2f,z),Vector3.down),out var hit,2.6f)&&hit.normal.y>.35f)y=Mathf.Max(y,hit.point.y);
                if(float.IsNegativeInfinity(y)||Mathf.Abs(y-expected)>1.2f)throw new InvalidOperationException("Sin suelo continuo a altura esperada: "+new Vector3(x,expected,z)+" suelo="+y);
                return y;
            }
            for(int path=0;path<plan.Walks.Count;path++)
            {
                var a=plan.Walks[path][0];var b=plan.Walks[path][1];var d=b-a;d.y=0;var side=Vector3.Cross(Vector3.up,d.normalized);
                int n=Mathf.Max(1,Mathf.CeilToInt(d.magnitude/.25f));
                for(int lane=0;lane<4;lane++)for(int i=0;i<=n;i++)
                {
                    var p=Vector3.Lerp(a,b,i/(float)n)+side*((lane-1.5f)*plan.Widths[path]/4);
                    float y=Surface(p.x,p.z,p.y);int count=physics.OverlapCapsule(new Vector3(p.x,y+.75f,p.z),new Vector3(p.x,y+1.7f,p.z),.3f,hits,~0,QueryTriggerInteraction.Ignore);
                    if(count>0)throw new InvalidOperationException("Paso bloqueado ruta "+path+" en "+p+" / "+hits[0].name);
                }
            }
            foreach(var p in plan.Pieces)
            {
                var prefab=PrefabUtility.GetCorrespondingObjectFromSource(p.Object);
                if(!prefab||(p.Object.transform.localScale-prefab.transform.localScale).sqrMagnitude>.0001f)throw new InvalidOperationException("Escala o prefab alterados: "+p.Key);
                foreach(var t in p.Object.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Script faltante: "+t.name);
                foreach(var r in p.Object.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterials.Length==0||r.sharedMaterials.Any(m=>!m||!m.shader))throw new InvalidOperationException("Material faltante: "+r.name);
                if(p.Key!="Flag"&&p.Key!="Crystal"&&!p.Object.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))throw new InvalidOperationException("Collider faltante: "+p.Key);
                // Solids must not invade the open fighting circle, above its walking surface.
                var delta=p.Bounds.center-new Vector3(162,p.Bounds.center.y,42);
                if(p.Key!="Base"&&p.Bounds.max.y>20.3f&&delta.magnitude-p.Bounds.extents.magnitude<17&&p.Key!="StairRail")
                    if(p.Bounds.min.x<179&&p.Bounds.max.x>145&&p.Bounds.min.z<59&&p.Bounds.max.z>25)
                        throw new InvalidOperationException("Arquitectura dentro de la arena libre: "+p.Key);
            }
            // Exact queries for all masonry BoxColliders, excluding their touching faces.
            var originalColliders=new HashSet<Collider>(plan.Context.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger));
            foreach(var box in plan.Root.GetComponentsInChildren<BoxCollider>().Where(c=>c.enabled&&!c.isTrigger))
            {
                var scale=box.transform.lossyScale;var half=Vector3.Scale(box.size*.5f,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)))-Vector3.one*.025f;
                if(half.x<=0||half.y<=0||half.z<=0)continue;
                int count=physics.OverlapBox(box.transform.TransformPoint(box.center),half,hits,box.transform.rotation,~0,QueryTriggerInteraction.Ignore);
                if(count==hits.Length)throw new InvalidOperationException("Consulta espacial saturada.");
                for(int i=0;i<count;i++)if(originalColliders.Contains(hits[i]))throw new InvalidOperationException("Mamposteria nueva invade collider original: "+box.name+" / "+hits[i].name+" "+box.bounds);
            }
            foreach(var created in plan.Root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))
            foreach(var original in originalColliders)
            {
                if(!created.bounds.Intersects(original.bounds))continue;
                bool supported=created is BoxCollider||created is SphereCollider||created is CapsuleCollider||(created is MeshCollider mesh&&mesh.convex)||original is BoxCollider||(original is MeshCollider other&&other.convex);
                if(supported&&Physics.ComputePenetration(created,created.transform.position,created.transform.rotation,original,original.transform.position,original.transform.rotation,out var direction,out float depth)&&depth>.04f)
                    throw new InvalidOperationException("Interseccion con original: "+created.name+" / "+original.name+" profundidad="+depth+" posicion="+created.bounds.center);
            }
            if(plan.Root.GetComponentsInChildren<Light>().Count(l=>l.enabled)!=4)throw new InvalidOperationException("Presupuesto de luces distinto de cuatro.");
            if(plan.Root.GetComponentsInChildren<UnityEngine.Rendering.Volume>().Length>0)throw new InvalidOperationException("No se permiten cambios globales de postprocesado.");
            // Check the visible doorway meshes too, rather than trusting a simplified prefab collider.
            var visualChecks=new List<GameObject>();
            try
            {
                var visualColliders=new List<MeshCollider>();
                foreach(var piece in plan.Pieces.Where(p=>p.Object.transform.parent.name.StartsWith("06 -",StringComparison.Ordinal)&&(p.Key=="Arch"||p.Key=="Wall")))
                foreach(var filter in piece.Object.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer=filter.GetComponent<MeshRenderer>();if(!filter.sharedMesh||!renderer||!renderer.enabled)continue;
                    var check=new GameObject("__Umbral_mesh_clearance_check"){hideFlags=HideFlags.HideAndDontSave};
                    SceneManager.MoveGameObjectToScene(check,plan.Root.gameObject.scene);visualChecks.Add(check);
                    check.transform.SetPositionAndRotation(filter.transform.position,filter.transform.rotation);check.transform.localScale=filter.transform.lossyScale;
                    var collider=check.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;visualColliders.Add(collider);
                }
                Physics.SyncTransforms();
                foreach(var collider in visualColliders)foreach(float x in new[]{160.5f,161.5f,162.5f,163.5f})foreach(float height in new[]{1f,1.5f,1.85f})
                {
                    if(collider.Raycast(new Ray(new Vector3(x,4+height,6.2f),Vector3.forward),out var hit,23.3f)||
                       collider.Raycast(new Ray(new Vector3(x,4+height,29.5f),Vector3.back),out hit,23.3f))
                        throw new InvalidOperationException("La malla visible obstruye la abertura: "+collider.sharedMesh.name+" en "+hit.point);
                }
            }
            finally{foreach(var check in visualChecks)if(check)Object.DestroyImmediate(check);}
            plan.Source.Verify(integrating);
            Debug.Log("UMBRAL_VALIDACION_OK: "+plan.Pieces.Count+" prefabs; arena 84x84 en (162,20,42), desplazada +12m, centro libre de 34m; puente 12m (aproximacion de 6m entre ruinas) con cuatro vuelos ascendentes desde Y4, cruce de baranda Y12; UNICA escalinata central de 24m, cuatro vuelos hacia dentro: Y20 a Y4; ultimo tramo bajo arena, descanso libre Z6..18, arco retranqueado Z22.5, umbral abierto hasta Z30; altura libre de cripta 9.38m. Cuatro carriles, colliders y referencias comprobados. Original sin cambios. Validacion geometrica; no prueba de gameplay/NavMesh.",plan.Root);
        }
        sealed class UmbralPreview:PreviewSceneStage
        {
            public Source Source;public Dictionary<string,Asset> Assets;public Plan Plan;
            public Transform PreviewRoot=>scene.GetRootGameObjects().SingleOrDefault(g=>g.name==Root)?.transform;
            protected override GUIContent CreateHeaderContent()=>new GUIContent("Umbral de las Profundidades - SOLO PREVISUALIZACION");
            protected override bool OnOpenStage()
            {
                if(Source==null||Assets==null||!base.OnOpenStage())return false;
                try
                {
                    Scene temporary=scene;temporary.name="TEMP_CiudadDelSol_Umbral";
                    var context=new GameObject("Contexto original - solo copias");SceneManager.MoveGameObjectToScene(context,scene);context.SetActive(false);
                    var copy=Object.Instantiate(Source.Root.gameObject,context.transform);copy.name=Source.Root.name;
                    copy.transform.SetPositionAndRotation(Source.Root.position,Source.Root.rotation);copy.transform.localScale=Source.Root.lossyScale;
                    foreach(var r in Source.Scene.GetRootGameObjects().Where(g=>g.name=="Salida_Meridional_Generated"))Object.Instantiate(r,context.transform);
                    DisableImportedBehaviour(context);context.SetActive(true);
                    foreach(var a in Assets)Debug.Log("UMBRAL_ASSET "+a.Key+" "+a.Value.Bounds.size);
                    Plan=Build(Source,context.transform,scene,Assets);
                    var light=new GameObject("Luz de inspeccion - solo preview");SceneManager.MoveGameObjectToScene(light,scene);light.transform.rotation=Quaternion.Euler(55,-30,0);var l=light.AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.3f;l.shadows=LightShadows.None;
                    EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this){View();Selection.activeGameObject=null;}};
                    ValidatePlan(Plan);
                    Debug.Log("UMBRAL_PREVIEW_LISTA: solo copia temporal. No hay menu de integracion ni guardado automatico. Original conservada.");return true;
                }
                catch(Exception e){Debug.LogException(e);return true;}
            }
            protected override void OnCloseStage()
            {
                if(Source!=null)Source.Verify();var materials=Plan==null?new List<Material>():Plan.Materials.ToList();base.OnCloseStage();
                foreach(var m in materials)if(m)Object.DestroyImmediate(m);
            }
        }
    }
}
