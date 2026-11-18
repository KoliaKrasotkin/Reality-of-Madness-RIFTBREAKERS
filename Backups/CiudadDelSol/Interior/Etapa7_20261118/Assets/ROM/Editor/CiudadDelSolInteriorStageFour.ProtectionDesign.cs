using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolInteriorStageTwo
    {
        const string ProtectionOutput="Backups/CiudadDelSol/Interior/ProteccionesEtapa4_20261118";
        const string ProtectionPrefab="Assets/Standard Assets/Eternal Temple/Prefabs/Railings and Decors/Railing_Staircase_01.prefab";
        sealed class GuardFlight {public string id;public int index;public Vector3 start,dir;public Vector3 Right=>Vector3.Cross(Vector3.up,dir);}
        sealed class NavWitness {public Station station;public Vector3 nav;}
        [Serializable] sealed class ProtectionAttempt
        {
            public float thickness,offset,endPadding;public int navConflicts,unsafeSides;public bool routesClear,safetyEvaluated;
            public string reason;
            public Vector3 firstNavConflict;
            public string conflictGuard;
        }
        [Serializable] sealed class ProtectionSection
        {
            public string connection;public int flight,side;
            public Vector3 start,end;
            public float hazardFrom,hazardTo;
        }
        [Serializable] sealed class AvailableBand
        {
            public string connection;public int flight,side;public float along,physicalHalfWidth=3,navBoundary,agentEnvelope;
            public bool navPresent,boundaryFound;public Vector3 center,boundary;
        }
        [Serializable] sealed class ProtectionDesign
        {
            public string started,ended,sceneBefore,sceneAfter,error;
            public bool saved=false,playerMovement=false;
            public int navWitnesses,requiredGuards;
            public List<ProtectionAttempt> attempts=new List<ProtectionAttempt>();
            public List<Vector3> winningPositions=new List<Vector3>();
            public List<ProtectionSection> sections=new List<ProtectionSection>();
            public List<AvailableBand> bands=new List<AvailableBand>();
        }
        static GuardFlight[] ProtectionFlights()=>Enumerable.Range(0,4).Select(i=>new GuardFlight{id="VS01",index=i,start=V(45,-16-i*2,-12+i*6),dir=Vector3.forward})
            .Concat(Enumerable.Range(0,3).Select(i=>new GuardFlight{id="VS02",index=i,start=V(42-i*6,-24-i*2,45),dir=Vector3.left})).ToArray();

        public static void SearchStageFourProtectionsBatch()
        {
            if(!Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Diseño exclusivo batch/Edit Mode; no Play.");
            Directory.CreateDirectory(ProtectionOutput);
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Escena con cambios pendientes.");
            if(!UnityEngine.SceneManagement.SceneManager.GetSceneByPath(City).isLoaded)UnityEditor.SceneManagement.EditorSceneManager.OpenScene(City,UnityEditor.SceneManagement.OpenSceneMode.Single);
            SearchProtectionGeometry();
        }

        static void SearchProtectionGeometry()
        {
            var result=new ProtectionDesign{started=DateTime.UtcNow.ToString("o"),sceneBefore=Hash(City)};
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(City);var agent=NavMesh.GetSettingsByID(0);
            var cc=AssetDatabase.LoadAssetAtPath<GameObject>(Warrior).GetComponentInChildren<CharacterController>(true);
            int mask=0;for(int i=0;i<32;i++)if(!Physics.GetIgnoreLayerCollision((int)MultiFPS.GameLayers.character,i))mask|=1<<i;
            var cfg=new Config{scene=scene,physics=scene.GetPhysicsScene(),radius=cc.radius,height=cc.height,step=Mathf.Min(cc.stepOffset,agent.agentClimb),
                slope=Mathf.Min(cc.slopeLimit,agent.agentSlope),warriorSlope=cc.slopeLimit,mask=mask};
            cfg.stairBounds=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&c.name.StartsWith("Staircase_01",StringComparison.Ordinal)).Select(c=>{var b=c.bounds;b.Expand(new Vector3(cfg.radius*2,cfg.step*2,cfg.radius*2));return b;}).ToArray();
            var navCfg=new Config{scene=scene,physics=cfg.physics,radius=agent.agentRadius,height=Mathf.Max(agent.agentHeight,cc.height),step=cfg.step,slope=cfg.slope,warriorSlope=cfg.warriorSlope,mask=mask,stairBounds=cfg.stairBounds};
            var flights=ProtectionFlights();var required=new List<(GuardFlight f,int side,float lo,float hi)>();
            foreach(var f in flights)foreach(int side in new[]{-1,1})
            {
                var unsafeAt=Enumerable.Range(0,61).Select(i=>i*.1f).Where(d=>ProtectionUnsafeAt(f,side,cfg,d)).ToArray();
                if(unsafeAt.Length>0)required.Add((f,side,unsafeAt.Min(),unsafeAt.Max()));
            }
            result.requiredGuards=required.Count;
            foreach(var item in required)result.sections.Add(new ProtectionSection{connection=item.f.id,flight=item.f.index+1,side=item.side,start=item.f.start,end=item.f.start+item.f.dir*6+Vector3.down*2,hazardFrom=item.lo,hazardTo=item.hi});
            var bandFilter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
            foreach(var f in flights)foreach(float along in new[]{0f,.5f,3f,5f,5.5f,6f})foreach(int side in new[]{-1,1})
            {
                var expected=f.start+f.dir*along+Vector3.down*(along/3);
                var band=new AvailableBand{connection=f.id,flight=f.index+1,side=side,along=along};
                band.navPresent=NavMesh.SamplePosition(expected,out var center,.35f,bandFilter)&&Mathf.Abs(center.position.y-expected.y)<=.35f;
                if(band.navPresent)
                {
                    band.center=center.position;var dir=f.Right*side;
                    band.boundaryFound=NavMesh.Raycast(center.position,center.position+dir*6,out var edge,bandFilter);
                    if(band.boundaryFound){band.boundary=edge.position;band.navBoundary=Vector3.Dot(edge.position-center.position,dir);band.agentEnvelope=band.navBoundary+agent.agentRadius;}
                }
                result.bands.Add(band);
            }
            var witnesses=ProtectionWitnesses(flights,navCfg);result.navWitnesses=witnesses.Count;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ProtectionPrefab);
            if(!prefab||prefab.GetComponentsInChildren<Component>(true).Any(c=>!(c is Transform)&&!(c is MeshFilter)&&!(c is MeshRenderer)&&!(c is MeshCollider)))throw new InvalidOperationException("Prefabs de protección inesperados.");
            var originalVisual=prefab.GetComponent<MeshFilter>().sharedMesh;var originalCollider=prefab.GetComponent<MeshCollider>().sharedMesh;
            GameObject root=null;
            var meshes=new Dictionary<string,(Mesh visual,Mesh collision)>();
            try
            {
                foreach(float thickness in new[]{.30f,.20f,.12f,.08f,.04f})
                {
                    foreach(float padding in new[]{.32f,.16f,0f})
                    {
                    foreach(float offset in new[]{-.08f,-.04f,-.02f,0f,.02f,.04f,.08f,.15f})
                    {
                        var attempt=new ProtectionAttempt{thickness=thickness,offset=offset,endPadding=padding};result.attempts.Add(attempt);
                        root=new GameObject("ROM_Stage4_ProtectionDesign_NotSaved");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
                        foreach(var item in required)
                        {
                            var rail=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);rail.transform.SetParent(root.transform,true);
                            rail.name=item.f.id+"_"+(item.f.index+1)+"_"+item.side;
                            float from=Mathf.Max(0,item.lo-padding),to=Mathf.Min(6,item.hi+padding),length=Mathf.Max(.1f,to-from);
                            string key=thickness.ToString("R")+":"+length.ToString("R");
                            if(!meshes.TryGetValue(key,out var pair)){pair=(AdaptProtectionMesh(originalVisual,originalCollider,thickness,length),AdaptProtectionMesh(originalCollider,originalCollider,thickness,length));meshes.Add(key,pair);}
                            rail.GetComponent<MeshFilter>().sharedMesh=pair.visual;rail.GetComponent<MeshCollider>().sharedMesh=pair.collision;
                            var position=item.f.start+item.f.dir*from+Vector3.down*(from/3)+item.f.Right*(item.side>0?3+offset:-(3+offset+thickness));
                            rail.transform.SetPositionAndRotation(position,Quaternion.LookRotation(item.f.dir,Vector3.up));rail.transform.localScale=Vector3.one;
                        }
                        Physics.SyncTransforms();
                        foreach(var w in witnesses)
                        {
                            int count=cfg.physics.OverlapCapsule(w.station.foot+Vector3.up*navCfg.radius,w.station.foot+Vector3.up*(navCfg.height-navCfg.radius),navCfg.radius,Overlaps,mask,QueryTriggerInteraction.Ignore);
                            if(count==Overlaps.Length)throw new InvalidOperationException("Saturación de comprobación de huella navegable.");
                            var obstacle=Enumerable.Range(0,count).Select(i=>Overlaps[i]).FirstOrDefault(c=>c&&c.transform.IsChildOf(root.transform));
                            if(obstacle){attempt.navConflicts++;attempt.firstNavConflict=w.nav;attempt.conflictGuard=obstacle.name;break;}
                        }
                        if(attempt.navConflicts==0)
                        {
                            attempt.safetyEvaluated=true;
                            foreach(var f in flights)foreach(int side in new[]{-1,1})if(ProtectionSideUnsafe(f,side,cfg))attempt.unsafeSides++;
                            if(attempt.unsafeSides==0)
                            {
                                attempt.routesClear=ProtectionRoutesClear(cfg);
                                if(attempt.routesClear)
                                {
                                    result.winningPositions.AddRange(root.transform.Cast<Transform>().Select(t=>t.position));
                                    attempt.reason="Candidato de prevalidación: NavMesh libre, salidas laterales bloqueadas y rutas completas. Aún no guardado.";
                                }
                            }
                        }
                        UnityEngine.Object.DestroyImmediate(root);root=null;Physics.SyncTransforms();
                        File.WriteAllText(ProtectionOutput+"/Diseno_parcial.json",JsonUtility.ToJson(result,true));
                        if(result.winningPositions.Count>0)break;
                    }
                    if(result.winningPositions.Count>0)break;
                    }
                    if(result.winningPositions.Count>0)break;
                }
            }
            catch(Exception e){result.error=e.ToString();}
            finally
            {
                if(root)UnityEngine.Object.DestroyImmediate(root);
                foreach(var pair in meshes.Values){UnityEngine.Object.DestroyImmediate(pair.visual);UnityEngine.Object.DestroyImmediate(pair.collision);}
                Physics.SyncTransforms();result.ended=DateTime.UtcNow.ToString("o");result.sceneAfter=Hash(City);
                File.WriteAllText(ProtectionOutput+"/Diseno.json",JsonUtility.ToJson(result,true));
                if(result.sceneBefore!=result.sceneAfter)throw new InvalidOperationException("La prevalidación no debe guardar la escena.");
            }
        }

        static bool ProtectionSideUnsafe(GuardFlight f,int side,Config cfg)
        {
            return Enumerable.Range(0,61).Any(i=>ProtectionUnsafeAt(f,side,cfg,i*.1f));
        }
        static bool ProtectionUnsafeAt(GuardFlight f,int side,Config cfg,float along)
        {
                var expected=f.start+f.dir*along+Vector3.down*(along/3);var station=Probe(expected,cfg,true,cfg.step);
                if(!station.Clear)return true;
                var dir=f.Right*side;
                bool blocked=cfg.physics.CapsuleCast(station.foot+Vector3.up*cfg.radius,station.foot+Vector3.up*(cfg.height-cfg.radius),cfg.radius,dir,out var hit,3.5f,cfg.mask,QueryTriggerInteraction.Ignore);
                if(!blocked)
                {
                    // Open circulation on a continuous floor needs no railing.
                    if(Probe(station.floor+dir*3.5f,cfg,true,cfg.step).Clear)return false;
                    return true;
                }
                // Barrier must stop the actual capsule while its complete footprint is supported.
                var before=station.floor+dir*Mathf.Max(0,hit.distance-.001f);
                if(!Probe(before,cfg,true,cfg.step).Clear)return true;
            return false;
        }

        static List<NavWitness> ProtectionWitnesses(GuardFlight[] flights,Config cfg)
        {
            var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};var result=new List<NavWitness>();var seen=new System.Collections.Generic.HashSet<string>();
            foreach(var f in flights)for(int i=-2;i<=14;i++)for(int j=-20;j<=20;j++)foreach(float level in new[]{f.start.y-i/6f,-24,-30})
            {
                var q=f.start+f.dir*(i*.5f)+f.Right*(j*.2f);q.y=level;
                if(!NavMesh.SamplePosition(q,out var nav,.35f,filter)||Mathf.Abs(nav.position.y-q.y)>.35f)continue;
                string key=Mathf.RoundToInt(nav.position.x*1000)+":"+Mathf.RoundToInt(nav.position.y*1000)+":"+Mathf.RoundToInt(nav.position.z*1000);if(!seen.Add(key))continue;
                var station=Probe(nav.position,cfg,true,cfg.step);if(station.Clear)result.Add(new NavWitness{station=station,nav=nav.position});
            }
            return result;
        }

        static Mesh AdaptProtectionMesh(Mesh original,Mesh collider,float thickness,float length)
        {
            var cb=collider.bounds;var all=original.vertices;var refVertices=collider.vertices;
            float min=refVertices.Min(v=>v.y+v.z*2/3),max=refVertices.Max(v=>v.y+v.z*2/3);
            var copy=UnityEngine.Object.Instantiate(original);var scaleX=thickness/cb.size.x;var scaleZ=length/cb.size.z;var scaleY=1.5f/(max-min);
            var matrix=Matrix4x4.identity;matrix.m00=scaleX;matrix.m11=scaleY;matrix.m12=scaleY*2/3-scaleZ/3;matrix.m22=scaleZ;
            for(int i=0;i<all.Length;i++){var v=all[i];all[i]=new Vector3((v.x-cb.min.x)*scaleX,(v.y+v.z*2/3-min)*scaleY-(v.z-cb.min.z)*scaleZ/3-.10f,(v.z-cb.min.z)*scaleZ);}
            copy.vertices=all;var normals=copy.normals;var nmat=matrix.inverse.transpose;for(int i=0;i<normals.Length;i++)normals[i]=nmat.MultiplyVector(normals[i]).normalized;copy.normals=normals;
            copy.RecalculateBounds();copy.name="SolarGuard_2over6_"+thickness;return copy;
        }

        static bool ProtectionRoutesClear(Config cfg)
        {
            foreach(var p in new[]{StageFourNorthEntry(),StageFourPlans()[1]})
            {
                var guide=InspectTrace("Prevalidación de protección",p.points,p,cfg,true);
                var forward=Native(p.points[0],p.points[p.points.Length-1],p,cfg,"ida",guide.clear&&guide.missingNav==0);
                var reverse=Native(p.points[p.points.Length-1],p.points[0],p,cfg,"vuelta",guide.clear&&guide.missingNav==0);
                if(!guide.clear||guide.missingNav>0||!forward.complete||!reverse.complete||!forward.physical||!reverse.physical||forward.blockedRaycasts>0||reverse.blockedRaycasts>0||guide.widths.Any(w=>!w.fourStaticCapsulesClear||w.leftUnsupportedUnprotected||w.rightUnsupportedUnprotected))return false;
            }
            return true;
        }
    }
}
