using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolInteriorStageTwo
    {
        const string Work4="Backups/CiudadDelSol/Interior/ConstruccionEtapa4_20261118";
        const string FlatRail="Assets/Standard Assets/Eternal Temple/Prefabs/Railings and Decors/Railing_01.prefab";
        const string BuildRoot4="CiudadDelSol_Etapa4_Protecciones_VS01_VS02";
        [Serializable] sealed class Build4
        {
            public string started,ended,error,sceneBefore,sceneAfter,navBefore,navAfter,assetFolder;
            public bool saved,rolledBack,originalObjectsUnchanged,outsideNavPreserved,regressionsPassed,confirmedReload;
            public float radius,climb,voxel;
            public int sources,outsideWitnesses,exteriorPaths;
            public List<Change4> changes=new List<Change4>();public List<Rail4> rails=new List<Rail4>();
            public List<Platform4> platforms=new List<Platform4>();
        }
        [Serializable] sealed class Platform4{public string prefab;public Vector3 center,size;public float top;}
        [Serializable] sealed class Change4{public string hierarchy,id;public Vector3 oldPosition,newPosition,oldScale,newScale;}
        [Serializable] sealed class Rail4{public string name,prefab;public Vector3 start,end,position;}
        sealed class Module4{public Vector3 a,b,normal;public bool stair;public Bounds bounds;}
        [Serializable] sealed class Exterior4{public Vector3 a,b;public bool complete;}
        [Serializable] sealed class Proof4{public string digest;public List<Vector3> outside;public List<Exterior4> exterior;}
        [Serializable] sealed class CoverageFailure4{public Vector3 expected,nearest;public bool projected;public float distance;}
        [Serializable] sealed class Ref4{public ExteriorRef4 exterior;}
        [Serializable] sealed class ExteriorRef4{public GroupRef4[] groups;}
        [Serializable] sealed class GroupRef4{public string name;public Vector3 pivot;}
        static readonly Bounds Patch4=new Bounds(V(42,-23,17),V(52,20,74));

        public static void ConstructStageFourBatch()
        {
            if(!Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Construcción acotada exclusiva batch/Edit Mode.");
            Directory.CreateDirectory(Work4);
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("No se sustituye una escena con cambios pendientes.");
            if(!SceneManager.GetSceneByPath(City).isLoaded)EditorSceneManager.OpenScene(City,OpenSceneMode.Single);
            var scene=SceneManager.GetSceneByPath(City);
            if(scene.GetRootGameObjects().Any(g=>g.name==BuildRoot4))throw new InvalidOperationException("La intervención ya existe; no se duplica.");
            var result=new Build4{started=DateTime.UtcNow.ToString("o"),sceneBefore=Hash(City),navBefore=Hash(Nav)};
            string backupScene=Work4+"/Assets/Scenes/Dungeon_Isla_del_Sol.unity",backupNav=Work4+"/"+Nav;
            if(!File.Exists(backupScene)||Hash(backupScene)!=result.sceneBefore||!File.Exists(backupNav)||Hash(backupNav)!=result.navBefore)
                throw new InvalidOperationException("Falta respaldo completo coincidente; no se construye.");
            var surface=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NavMeshSurface>(true)).Single(s=>s.name=="NavMesh_Exterior_CiudadDelSol");
            var originalData=surface.navMeshData;var originalCopy=UnityEngine.Object.Instantiate(originalData);NavMeshData candidate=null;
            var originals=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var allowed=new HashSet<int>();var before=Fingerprint4(originals,allowed);GameObject root=null;
            var meshes=new Dictionary<string,(Mesh visual,Mesh collision)>();bool diskWritten=false;
            try
            {
                var settings=surface.GetBuildSettings();result.radius=settings.agentRadius;result.climb=settings.agentClimb;result.voxel=settings.voxelSize;
                if(settings.agentTypeID!=0||Mathf.Abs(settings.agentRadius-.57f)>.0001f||Mathf.Abs(settings.agentClimb-.36f)>.0001f)throw new InvalidOperationException("Agente original incompatible.");
                var baseline=NavMesh.CalculateTriangulation();var outside=OutsideWitnesses4(baseline);result.outsideWitnesses=outside.Count;
                var exterior=ExteriorPaths4(baseline);result.exteriorPaths=exterior.Count;
                File.WriteAllText(Work4+"/Fase.txt","Baseline capturada. Construyendo geometría acotada.");
                var cfg=ConstructionConfig4(scene);
                var modules=new List<Module4>();var modifiedFloors=new Dictionary<Collider,(bool x,bool z,Vector3 center)>();
                foreach(var f in ProtectionFlights())
                {
                    var mid=f.start+f.dir*3+Vector3.down;
                    var stair=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&c.name.StartsWith("Staircase_01",StringComparison.Ordinal)&&c.bounds.Contains(mid)).OrderBy(c=>Vector3.Distance(c.bounds.center,mid)).First();
                    Resize4(stair,f.Right,8,stair.bounds.center,result,allowed);
                    modules.Add(new Module4{a=f.start,b=f.start+f.dir*6+Vector3.down*2,normal=f.Right,stair=true,bounds=stair.bounds});
                }
                // Existing upper approach: preserve the route axes, widen only its narrow floor modules.
                var upper=new[]{V(63,-16,12),V(63,-16,-15),V(45,-16,-15)};
                for(int i=1;i<upper.Length;i++)
                {
                    var a=upper[i-1];var b=upper[i];var normal=Vector3.Cross(Vector3.up,(b-a).normalized);
                    for(float d=0;d<=Vector3.Distance(a,b);d+=.5f)
                    {
                        var p=Vector3.Lerp(a,b,d/Vector3.Distance(a,b));
                        if(Ground(p,.03f,cfg,out var hit)&&hit.collider.name.StartsWith("Platform_Base_01",StringComparison.Ordinal))AddFloor4(modifiedFloors,hit.collider,Mathf.Abs(normal.x)>.5f,Mathf.Abs(normal.z)>.5f,hit.collider.bounds.center);
                    }
                }
                foreach(var target in new[]{V(45,-24,15),V(45,-24,45)})
                {
                    var floor=FloorAt4(scene,target);AddFloor4(modifiedFloors,floor,true,true,floor.bounds.center);
                }
                foreach(float x in new[]{21f,27f}){var floor=FloorAt4(scene,V(x,-30,45));AddFloor4(modifiedFloors,floor,false,true,floor.bounds.center);}
                foreach(float z in new[]{15f,21f,27f,33f,39f,45f}){var floor=FloorAt4(scene,V(51,-24,z));AddFloor4(modifiedFloors,floor,true,false,floor.bounds.center+Vector3.right);}
                root=new GameObject(BuildRoot4);SceneManager.MoveGameObjectToScene(root,scene);
                AddFloorBands4(root,modifiedFloors,modules,result);
                Physics.SyncTransforms();cfg=ConstructionConfig4(scene);
                // Gather all boundary intervals before placing anything; internal joins remain open.
                var edges=new List<(Vector3 a,Vector3 b,Vector3 n)>();
                foreach(var m in modules)
                {
                    if(m.stair)
                    {
                        edges.Add((m.a-m.normal*4,m.b-m.normal*4,-m.normal));edges.Add((m.a+m.normal*4,m.b+m.normal*4,m.normal));
                        var d=(m.b-m.a);d.y=0;d.Normalize();edges.Add((m.a-m.normal*4,m.a+m.normal*4,-d));edges.Add((m.b-m.normal*4,m.b+m.normal*4,d));
                    }
                    else
                    {
                        var b=m.bounds;float y=b.max.y;
                        edges.Add((V(b.min.x,y,b.min.z),V(b.max.x,y,b.min.z),Vector3.back));edges.Add((V(b.min.x,y,b.max.z),V(b.max.x,y,b.max.z),Vector3.forward));
                        edges.Add((V(b.min.x,y,b.min.z),V(b.min.x,y,b.max.z),Vector3.left));edges.Add((V(b.max.x,y,b.min.z),V(b.max.x,y,b.max.z),Vector3.right));
                    }
                }
                var spans=new List<(Vector3 a,Vector3 b,Vector3 n)>();
                foreach(var edge in edges)
                {
                    float length=new Vector2(edge.b.x-edge.a.x,edge.b.z-edge.a.z).magnitude;int count=Mathf.CeilToInt(length/.25f);int begin=-1;
                    for(int i=0;i<=count;i++)
                    {
                        bool need=false;
                        if(i<count)
                        {
                            var p=Vector3.Lerp(edge.a,edge.b,(i+.5f)/count);var inside=Probe(p-edge.n*1.1f,cfg,true,cfg.step);
                            if(inside.Clear)
                            {
                                var q=p+edge.n*.5f;q.y=inside.floor.y;
                                bool adjacent=Probe(q,cfg,true,cfg.step).Clear;
                                bool blocked=cfg.physics.CapsuleCast(inside.foot+Vector3.up*cfg.radius,inside.foot+Vector3.up*(cfg.height-cfg.radius),cfg.radius,edge.n,out _,2,cfg.mask,QueryTriggerInteraction.Ignore);
                                need=!adjacent&&!blocked&&!OpenFlightCrossing4(p,edge.n,cfg);
                            }
                        }
                        if(need&&begin<0)begin=i;
                        if(!need&&begin>=0)
                        {
                            float from=Mathf.Max(0,(float)begin/count-.15f/length),to=Mathf.Min(1,(float)i/count+.15f/length);
                            for(float t=from;t<to-.00001f;){float next=Mathf.Min(to,t+6/length);spans.Add((Vector3.Lerp(edge.a,edge.b,t),Vector3.Lerp(edge.a,edge.b,next),edge.n));t=next;}
                            begin=-1;
                        }
                    }
                }
                foreach(var edge in spans)BuildRail4(root,edge.a,edge.b,edge.n,meshes,result);
                Physics.SyncTransforms();result.originalObjectsUnchanged=Same4(Fingerprint4(originals,allowed),before.Where(p=>!allowed.Contains(p.Key)).ToDictionary(p=>p.Key,p=>p.Value));
                if(!result.originalObjectsUnchanged)throw new InvalidOperationException("Cambió un objeto original no autorizado.");
                File.WriteAllText(Work4+"/Fase.txt","Geometría preparada. Actualizando una copia del NavMesh manual.");
                var collect=typeof(NavMeshSurface).GetMethod("CollectSources",BindingFlags.Instance|BindingFlags.NonPublic);
                var sources=(List<NavMeshBuildSource>)collect.Invoke(surface,null);result.sources=sources.Count;
                candidate=UnityEngine.Object.Instantiate(originalData);
                if(!NavMeshBuilder.UpdateNavMeshData(candidate,settings,sources,originalData.sourceBounds))throw new InvalidOperationException("Falló la actualización de la candidata.");
                Swap4(surface,candidate);
                if(!Coverage4(outside)||!CompareExterior4(exterior))throw new InvalidOperationException("La candidata afecta navegación fuera del alcance o rutas exteriores.");
                var candidateOutside=OutsideWitnesses4(NavMesh.CalculateTriangulation());Swap4(surface,originalCopy);
                if(!Coverage4(candidateOutside))throw new InvalidOperationException("La candidata introduce navegación nueva fuera del alcance.");
                Swap4(surface,candidate);result.outsideNavPreserved=true;
                var proof=new Proof4{digest=TriangleDigest4(),outside=outside,exterior=exterior};
                File.WriteAllText(Work4+"/Navegacion_referencia.json",JsonUtility.ToJson(proof,true));
                File.WriteAllText(Work4+"/Fase.txt","Candidata registrada. Prevalidación física y regresiones sin guardar.");
                if(!ConstructionReception4(Work4+"/Prevalidacion",true))throw new InvalidOperationException("La geometría/candidata no supera la prevalidación. No se guarda.");
                result.regressionsPassed=true;
                string gen="Assets/ROM/Generated/InteriorEtapa4_"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");Directory.CreateDirectory(gen);AssetDatabase.Refresh();result.assetFolder=gen;
                int index=0;foreach(var pair in meshes.Values)foreach(var mesh in new[]{pair.visual,pair.collision})AssetDatabase.CreateAsset(mesh,gen+"/Baranda_"+(index++)+".asset");
                diskWritten=true;
                surface.RemoveData();EditorUtility.CopySerialized(candidate,originalData);EditorUtility.SetDirty(originalData);surface.navMeshData=originalData;surface.AddData();
                if(TriangleDigest4()!=proof.digest)throw new InvalidOperationException("CopySerialized no conservó la candidata validada.");
                AssetDatabase.SaveAssetIfDirty(originalData);
                if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("No se guardó la escena.");
                if(!ConstructionReception4(Work4+"/Recepcion",false)||!Coverage4(outside)||!CompareExterior4(exterior))throw new InvalidOperationException("Fallo tras persistencia: se restaura el respaldo.");
                result.saved=true;
            }
            catch(Exception e)
            {
                result.error=e.ToString();
                if(diskWritten)
                {
                    surface.RemoveData();EditorUtility.CopySerialized(originalCopy,originalData);EditorUtility.SetDirty(originalData);AssetDatabase.SaveAssetIfDirty(originalData);
                    File.Copy(backupScene,City,true);File.Copy(backupNav,Nav,true);AssetDatabase.ImportAsset(Nav,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                    originalData=AssetDatabase.LoadAssetAtPath<NavMeshData>(Nav);surface.navMeshData=originalData;surface.AddData();result.rolledBack=true;
                }
            }
            finally
            {
                if(!result.saved)
                {
                    surface.RemoveData();surface.navMeshData=originalData;surface.AddData();
                    if(root)UnityEngine.Object.DestroyImmediate(root);
                    foreach(var pair in meshes.Values)foreach(var mesh in new[]{pair.visual,pair.collision})if(mesh&&!AssetDatabase.Contains(mesh))UnityEngine.Object.DestroyImmediate(mesh);
                }
                if(candidate)UnityEngine.Object.DestroyImmediate(candidate);if(originalCopy)UnityEngine.Object.DestroyImmediate(originalCopy);
                result.ended=DateTime.UtcNow.ToString("o");result.sceneAfter=Hash(City);result.navAfter=Hash(Nav);
                File.WriteAllText(Work4+"/Construccion.json",JsonUtility.ToJson(result,true));
            }
        }

        static Config ConstructionConfig4(Scene scene)
        {
            var cc=AssetDatabase.LoadAssetAtPath<GameObject>(Warrior).GetComponentInChildren<CharacterController>(true);var a=NavMesh.GetSettingsByID(0);int mask=0;
            for(int i=0;i<32;i++)if(!Physics.GetIgnoreLayerCollision((int)MultiFPS.GameLayers.character,i))mask|=1<<i;
            var cfg=new Config{scene=scene,physics=scene.GetPhysicsScene(),radius=cc.radius,height=cc.height,step=Mathf.Min(cc.stepOffset,a.agentClimb),slope=Mathf.Min(cc.slopeLimit,a.agentSlope),warriorSlope=cc.slopeLimit,mask=mask};
            cfg.stairBounds=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&c.name.StartsWith("Staircase_01",StringComparison.Ordinal)).Select(c=>{var b=c.bounds;b.Expand(new Vector3(cc.radius*2,cfg.step*2,cc.radius*2));return b;}).ToArray();return cfg;
        }
        static Collider FloorAt4(Scene scene,Vector3 p)=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Single(c=>c.enabled&&c.gameObject.activeInHierarchy&&c.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)&&Mathf.Abs(c.bounds.max.y-p.y)<.03f&&new Vector2(c.bounds.center.x-p.x,c.bounds.center.z-p.z).magnitude<.1f);
        static void AddFloor4(Dictionary<Collider,(bool x,bool z,Vector3 center)> floors,Collider c,bool x,bool z,Vector3 center){if(floors.TryGetValue(c,out var old))floors[c]=(old.x||x,old.z||z,center);else floors.Add(c,(x,z,center));}
        static void AddFloorBands4(GameObject root,Dictionary<Collider,(bool x,bool z,Vector3 center)> desired,List<Module4> modules,Build4 report)
        {
            var existing=root.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&c.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)).Select(c=>c.bounds).ToArray();
            var cells=new HashSet<(int y,int x,int z)>();
            foreach(var pair in desired)
            {
                var old=pair.Key.bounds;modules.Add(new Module4{bounds=old});float y=old.max.y;
                var center=pair.Value.center;float sx=pair.Value.x?8:old.size.x,sz=pair.Value.z?8:old.size.z;
                for(int x=Mathf.RoundToInt(center.x-sx*.5f);x<Mathf.RoundToInt(center.x+sx*.5f);x++)
                for(int z=Mathf.RoundToInt(center.z-sz*.5f);z<Mathf.RoundToInt(center.z+sz*.5f);z++)
                {
                    var q=V(x+.5f,y,z+.5f);
                    // Widen the sides of the landing, never extend its horizontal
                    // top over a descending flight: that creates an abrupt lip.
                    if(ProtectionFlights().Any(f=>
                    {
                        var delta=q-f.start;float along=Vector3.Dot(delta,f.dir);
                        return along>0&&along<6&&Mathf.Abs(Vector3.Dot(delta,f.Right))<4&&y<=f.start.y+.03f&&y>f.start.y-along/3+.03f;
                    }))continue;
                    if(existing.Any(b=>Mathf.Abs(b.max.y-y)<.03f&&q.x>b.min.x&&q.x<b.max.x&&q.z>b.min.z&&q.z<b.max.z))continue;
                    cells.Add((Mathf.RoundToInt(y*1000),x,z));
                }
            }
            const string asset="Assets/Standard Assets/Eternal Temple/Prefabs/Pits and Platforms/Platform_Base_01.prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset);
            while(cells.Count>0)
            {
                var first=cells.OrderBy(p=>p.y).ThenBy(p=>p.z).ThenBy(p=>p.x).First();int w=1,d=1;
                while(w<6&&cells.Contains((first.y,first.x+w,first.z)))w++;
                while(d<6&&Enumerable.Range(0,w).All(x=>cells.Contains((first.y,first.x+x,first.z+d))))d++;
                for(int x=0;x<w;x++)for(int z=0;z<d;z++)cells.Remove((first.y,first.x+x,first.z+z));
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.scene);go.transform.SetParent(root.transform,true);go.name="Ampliacion_piedra_"+(report.platforms.Count+1);
                go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);go.transform.localScale=Vector3.one;
                var collider=go.GetComponent<Collider>();Physics.SyncTransforms();var b=collider.bounds;go.transform.localScale=V(w/b.size.x,1,d/b.size.z);Physics.SyncTransforms();b=collider.bounds;
                float top=first.y/1000f;var target=V(first.x+w*.5f,top,first.z+d*.5f);go.transform.position=V(target.x-b.center.x,top-b.max.y,target.z-b.center.z);Physics.SyncTransforms();
                modules.Add(new Module4{bounds=collider.bounds});report.platforms.Add(new Platform4{prefab=asset,center=collider.bounds.center,size=collider.bounds.size,top=top});
            }
        }
        static void Resize4(Collider c,Vector3 axis,float size,Vector3 center,Build4 result,HashSet<int> allowed)
        {
            var t=c.transform;var change=new Change4{hierarchy=Hierarchy(t),id=GlobalObjectId.GetGlobalObjectIdSlow(c).ToString(),oldPosition=t.position,oldScale=t.localScale};
            int index=Enumerable.Range(0,3).OrderByDescending(i=>Mathf.Abs(Vector3.Dot(t.TransformDirection(i==0?Vector3.right:i==1?Vector3.up:Vector3.forward),axis))).First();
            float extent=Mathf.Abs(axis.x)*c.bounds.size.x+Mathf.Abs(axis.z)*c.bounds.size.z;var scale=t.localScale;scale[index]*=size/extent;t.localScale=scale;
            Physics.SyncTransforms();var b=c.bounds;t.position+=V(center.x-b.center.x,0,center.z-b.center.z);Physics.SyncTransforms();
            allowed.Add(t.GetInstanceID());change.newPosition=t.position;change.newScale=t.localScale;result.changes.Add(change);
        }
        static Dictionary<int,string> Fingerprint4(Transform[] originals,HashSet<int> allowed)=>originals.Where(t=>!allowed.Contains(t.GetInstanceID())).ToDictionary(t=>t.GetInstanceID(),t=>EditorJsonUtility.ToJson(t)+"|"+t.gameObject.activeSelf+"|"+t.gameObject.layer);
        static bool Same4(Dictionary<int,string> a,Dictionary<int,string> b)=>a.Count==b.Count&&a.All(p=>b.TryGetValue(p.Key,out var s)&&s==p.Value);
        static void Swap4(NavMeshSurface s,NavMeshData d){s.RemoveData();s.navMeshData=d;s.AddData();}
        static List<Vector3> OutsideWitnesses4(NavMeshTriangulation tri)
        {
            var result=new List<Vector3>();for(int i=0;i<tri.indices.Length;i+=3)
            {var a=tri.vertices[tri.indices[i]];var b=tri.vertices[tri.indices[i+1]];var c=tri.vertices[tri.indices[i+2]];var bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Encapsulate(c);if(bounds.Intersects(Patch4))continue;
                foreach(var p in new[]{a,b,c,(a+b+c)/3,(a+b)/2,(b+c)/2,(a+c)/2})if(NavMesh.SamplePosition(p,out var hit,.35f,NavMesh.AllAreas))result.Add(hit.position);
            }return result;
        }
        static bool Coverage4(List<Vector3> points)
        {
            var f=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};var failures=new List<CoverageFailure4>();
            foreach(var p in points)if(!NavMesh.SamplePosition(p,out var hit,.001f,f)||Vector3.Distance(p,hit.position)>.001f)
            {
                var found=NavMesh.SamplePosition(p,out var near,.35f,f);failures.Add(new CoverageFailure4{expected=p,projected=found,nearest=found?near.position:Vector3.zero,distance=found?Vector3.Distance(p,near.position):-1});if(failures.Count==20)break;
            }
            if(failures.Count>0)File.WriteAllText(Work4+"/Fallo_cobertura.txt",string.Join("\n",failures.Select(x=>JsonUtility.ToJson(x))));return failures.Count==0;
        }
        static string TriangleDigest4()
        {
            var tri=NavMesh.CalculateTriangulation();var lines=new List<string>();
            for(int i=0;i<tri.indices.Length;i+=3)lines.Add(string.Join("|",new[]{tri.vertices[tri.indices[i]],tri.vertices[tri.indices[i+1]],tri.vertices[tri.indices[i+2]]}.Select(p=>p.x.ToString("R")+","+p.y.ToString("R")+","+p.z.ToString("R")).OrderBy(s=>s))+":"+tri.areas[i/3]);
            lines.Sort(StringComparer.Ordinal);using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",lines))));
        }
        public static void VerifyConstructedStageFourBatch()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Verificación exclusiva de batch.");
            var result=JsonUtility.FromJson<Build4>(File.ReadAllText(Work4+"/Construccion.json"));
            if(!result.saved||Hash(City)!=result.sceneAfter||Hash(Nav)!=result.navAfter)throw new InvalidOperationException("No hay intervención guardada coincidente.");
            if(!SceneManager.GetSceneByPath(City).isLoaded)EditorSceneManager.OpenScene(City,OpenSceneMode.Single);
            var proof=JsonUtility.FromJson<Proof4>(File.ReadAllText(Work4+"/Navegacion_referencia.json"));
            result.confirmedReload=TriangleDigest4()==proof.digest&&Coverage4(proof.outside)&&CompareExterior4(proof.exterior)&&ConstructionReception4(Work4+"/Recepcion_recarga",false);
            if(!result.confirmedReload)result.error="La recarga no acredita la intervención; revisar pruebas antes de declarar recepción.";
            File.WriteAllText(Work4+"/Construccion.json",JsonUtility.ToJson(result,true));
        }
        static List<Exterior4> ExteriorPaths4(NavMeshTriangulation tri)
        {
            var reference=JsonUtility.FromJson<Ref4>(File.ReadAllText("Docs/CiudadDelSol/Interior/Plan_20261117/Referencia_exterior.json"));
            var anchors=new List<Vector3>();foreach(var g in reference.exterior.groups.Take(7))
            {var candidates=tri.vertices.Where(p=>p.y>=-3&&Mathf.Abs(p.y-g.pivot.y)<=.35f).ToArray();if(candidates.Length==0)throw new InvalidOperationException("Sector exterior sin ancla: "+g.name);anchors.Add(candidates.OrderBy(p=>new Vector2(p.x-g.pivot.x,p.z-g.pivot.z).sqrMagnitude).First());}
            var result=new List<Exterior4>();foreach(var a in anchors)foreach(var b in anchors)if(a!=b){var p=new NavMeshPath();result.Add(new Exterior4{a=a,b=b,complete=NavMesh.CalculatePath(a,b,NavMesh.AllAreas,p)&&p.status==NavMeshPathStatus.PathComplete});}return result;
        }
        static bool CompareExterior4(List<Exterior4> paths)=>paths.All(p=>!p.complete||(NavMesh.CalculatePath(p.a,p.b,NavMesh.AllAreas,new NavMeshPath())&&Complete4(p.a,p.b)));
        static bool Complete4(Vector3 a,Vector3 b){var p=new NavMeshPath();return NavMesh.CalculatePath(a,b,NavMesh.AllAreas,p)&&p.status==NavMeshPathStatus.PathComplete;}

        static void BuildRail4(GameObject root,Vector3 a,Vector3 b,Vector3 normal,Dictionary<string,(Mesh visual,Mesh collision)> meshes,Build4 report)
        {
            var d=b-a;float len=new Vector2(d.x,d.z).magnitude;if(len<.05f)return;
            bool stair=Mathf.Abs(d.y)>.01f;string path=stair?ProtectionPrefab:FlatRail;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var visual=prefab.GetComponent<MeshFilter>().sharedMesh;
            var originalMeshCollider=prefab.GetComponent<MeshCollider>();var collider=originalMeshCollider?originalMeshCollider.sharedMesh:BoxMesh4(prefab.GetComponent<BoxCollider>());
            string key=path+":"+len.ToString("R")+":"+d.y.ToString("R");
            if(!meshes.TryGetValue(key,out var pair)){pair=(RailMesh4(visual,collider,len,d.y,stair),RailMesh4(collider,collider,len,d.y,stair));meshes.Add(key,pair);}
            if(!originalMeshCollider)UnityEngine.Object.DestroyImmediate(collider);
            d.y=0;var right=Vector3.Cross(Vector3.up,d.normalized);var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.scene);go.transform.SetParent(root.transform,true);go.name="Proteccion_"+(report.rails.Count+1);
            go.transform.SetPositionAndRotation(a-normal*.45f-right*.5f,Quaternion.LookRotation(d.normalized));go.transform.localScale=Vector3.one;
            go.GetComponent<MeshFilter>().sharedMesh=pair.visual;
            var mc=go.GetComponent<MeshCollider>();if(!mc){var box=go.GetComponent<BoxCollider>();var material=box.sharedMaterial;UnityEngine.Object.DestroyImmediate(box);mc=go.AddComponent<MeshCollider>();mc.sharedMaterial=material;}
            mc.sharedMesh=pair.collision;
            report.rails.Add(new Rail4{name=go.name,prefab=path,start=a,end=b,position=go.transform.position});
        }
        static Mesh RailMesh4(Mesh original,Mesh collider,float length,float delta,bool stair)
        {
            var cb=collider.bounds;float slope=stair?2f/3:0;float lo=collider.vertices.Min(v=>v.y+slope*v.z),hi=collider.vertices.Max(v=>v.y+slope*v.z);
            var mesh=UnityEngine.Object.Instantiate(original);var vtx=mesh.vertices;float sx=1/cb.size.x,sz=length/cb.size.z,sy=1.5f/(hi-lo);
            for(int i=0;i<vtx.Length;i++){var v=vtx[i];vtx[i]=V((v.x-cb.min.x)*sx,(v.y+slope*v.z-lo)*sy+(v.z-cb.min.z)/cb.size.z*delta-.1f,(v.z-cb.min.z)*sz);}
            mesh.vertices=vtx;var matrix=Matrix4x4.identity;matrix.m00=sx;matrix.m11=sy;matrix.m12=sy*slope+delta/cb.size.z;matrix.m22=sz;var ns=mesh.normals;for(int i=0;i<ns.Length;i++)ns[i]=matrix.inverse.transpose.MultiplyVector(ns[i]).normalized;mesh.normals=ns;if(mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0))mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
        static Mesh BoxMesh4(BoxCollider box)
        {
            if(!box)throw new InvalidOperationException("Baranda sin collider soportado.");
            var c=box.center;var e=box.size*.5f;var m=new Mesh();
            m.vertices=new[]{c+V(-e.x,-e.y,-e.z),c+V(e.x,-e.y,-e.z),c+V(e.x,e.y,-e.z),c+V(-e.x,e.y,-e.z),c+V(-e.x,-e.y,e.z),c+V(e.x,-e.y,e.z),c+V(e.x,e.y,e.z),c+V(-e.x,e.y,e.z)};
            m.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        // A widened landing can overlap the first tread. Its transverse perimeter is
        // an entrance, not an exposed side. Only omit a panel where an actual flight
        // lies across it and the existing physical step remains within Warrior rules.
        static bool OpenFlightCrossing4(Vector3 p,Vector3 normal,Config cfg)
        {
            foreach(var flight in ProtectionFlights())
            {
                if(Mathf.Abs(Vector3.Dot(normal,flight.dir))<.99f)continue;
                var delta=p-flight.start;float along=Vector3.Dot(delta,flight.dir);
                if(along<0||along>6||Mathf.Abs(Vector3.Dot(delta,flight.Right))>4)continue;
                var a=p-normal*.05f;var b=p+normal*.05f;
                if(Ground(a,cfg.step,cfg,out var ga)&&Ground(b,cfg.step,cfg,out var gb)&&Mathf.Abs(ga.point.y-gb.point.y)<=cfg.step)return true;
            }
            return false;
        }
        static bool Raw4(Report r,int n)=>r!=null&&string.IsNullOrEmpty(r.error)&&r.connections.Count==n&&r.negativeControlsRejectObstaclesAndOtherLevels&&r.connections.All(c=>c.guide.clear&&c.guide.missingNav==0&&c.forward.complete&&c.reverse.complete&&c.forward.physical&&c.reverse.physical&&c.forward.blockedRaycasts==0&&c.reverse.blockedRaycasts==0&&c.geometryFourLanes&&c.edgeProtectionClear);
        static bool ConstructionReception4(string folder,bool preview)
        {
            var r1=InspectSelected(new[]{P("A03-R21","A03","R21",V(156,-16,99),V(174,-16,99),V(174,-16,117),V(120,-16,117),V(117,-16,123))},folder+"/Etapa1","Regresión Etapa 1",preview);
            var r2=InspectSelected(Plans(),folder+"/Etapa2","Regresión Etapa 2",preview);
            var old=JsonUtility.FromJson<Report>(File.ReadAllText(StageThreeOutput+"/Recepcion.json"));var r3=InspectSelected(old.connections.Select(c=>c.plan).ToArray(),folder+"/Etapa3","Regresión Etapa 3",preview);
            var r4=InspectSelected(new[]{StageFourNorthEntry(),StageFourPlans()[1]},folder+"/Etapa4","Etapa 4 protegida",preview);
            var cfg=ConstructionConfig4(SceneManager.GetSceneByPath(City));
            bool protectedStairs=ProtectionFlights().All(f=>!ProtectionSideUnsafe(f,-1,cfg)&&!ProtectionSideUnsafe(f,1,cfg));
            var unsafeSamples=new List<string>();
            foreach(var f in ProtectionFlights())foreach(int side in new[]{-1,1})for(int i=0;i<=60;i++)
                if(ProtectionUnsafeAt(f,side,cfg,i*.1f))
                {
                    var expected=f.start+f.dir*(i*.1f)+Vector3.down*(i/30f);var station=Probe(expected,cfg,true,cfg.step);var dir=f.Right*side;
                    bool blocked=cfg.physics.CapsuleCast(station.foot+Vector3.up*cfg.radius,station.foot+Vector3.up*(cfg.height-cfg.radius),cfg.radius,dir,out var hit,3.5f,cfg.mask,QueryTriggerInteraction.Ignore);
                    var edge=Probe(station.floor+dir*(blocked?Mathf.Max(0,hit.distance-.001f):3.5f),cfg,true,cfg.step);
                    unsafeSamples.Add(f.id+"/"+(f.index+1)+" side="+side+" along="+(i*.1f).ToString("F2")+" point="+expected.ToString("F3")+" center="+JsonUtility.ToJson(station)+" blocked="+blocked+" hit="+(hit.collider?Hierarchy(hit.collider.transform):"none")+" distance="+hit.distance.ToString("R")+" before="+JsonUtility.ToJson(edge));
                }
            File.WriteAllLines(folder+"/Bordes_pendientes.txt",unsafeSamples);
            File.WriteAllText(folder+"/Seguridad_escaleras.txt","Pruebas de cápsula y apoyo cada 0,10 m en siete módulos; ambos flancos: "+protectedStairs);
            return Raw4(r1,1)&&Raw4(r2,6)&&Raw4(r3,3)&&Raw4(r4,2)&&protectedStairs;
        }
    }
}
