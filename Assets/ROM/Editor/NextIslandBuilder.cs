using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace ROM.Horde.EditorTools
{
    public static partial class FracturedForestBuilder
    {
        static readonly Vector3 SanctuaryCenter=new Vector3(-620,8,430);
        [MenuItem("ROM/Horda/Encontrar Santuario del Eclipse")]
        static void FocusSanctuary(){FocusZone("Bosque Fracturado - Santuario del Eclipse",SanctuaryCenter,260);}
        public static void NextIslandFromCommandLine()
        {
            try{BuildNextIsland();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        [MenuItem("ROM/Horda/Agregar Santuario y reparar suelo")]
        public static void BuildNextIsland()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);
            const string group="Bosque Fracturado - Santuario del Eclipse";
            foreach(var old in scene.GetRootGameObjects().Where(g=>g.name==group).ToArray())UnityEngine.Object.DestroyImmediate(old);
            root=new GameObject(group).transform;Random=new System.Random(71926);Converted.Clear();
            soil=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/ForestSoil.mat");rock=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/FracturedCliff.mat");stone=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/WeatheredRuins.mat");cyan=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/DimensionalCrystal.mat");
            var timber=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/BridgeTimber.mat");
            Satellite(SanctuaryCenter,220,"Santuario del Eclipse - isla de 440 metros");
            // Two independent access routes, with a wide central combat court.
            foreach(var origin in new[]{SatelliteCenters[0],SatelliteCenters[2]}){
                var direction=(SanctuaryCenter-origin).normalized;float radius=origin==SatelliteCenters[0]?RegionRadii[0]:RegionRadii[2];
                Bridge(origin+direction*(radius-24),SanctuaryCenter-direction*195,timber,"Puente al Santuario");
                var end=SanctuaryCenter-direction*195;
                var road=Primitive("Calzada del Santuario",PrimitiveType.Cube,(SanctuaryCenter+end)*.5f+Vector3.up*.04f,new Vector3(12,.08f,Vector3.Distance(end,SanctuaryCenter)),stone,false);road.transform.rotation=Quaternion.LookRotation(SanctuaryCenter-end);
            }
            var courtMaterial=Mat("EclipseCourt",new Color(.65f,.61f,.51f));
            courtMaterial.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Textures/T_FloorBricks_Low_lambert1_BaseColor.png"));courtMaterial.SetTextureScale("_BaseMap",new Vector2(16,16));EditorUtility.SetDirty(courtMaterial);
            var court=Primitive("Patio del Eclipse",PrimitiveType.Cylinder,SanctuaryCenter+Vector3.down*.04f,new Vector3(110,.12f,110),courtMaterial,false);
            court.AddComponent<MeshCollider>().sharedMesh=court.GetComponent<MeshFilter>().sharedMesh;
            // Landmark ensemble, not a copied demo city: processional court, broken
            // colonnades, two guardians, excavation pockets and wooded perimeter.
            var gate=Place(Egyptian+"SM_MainGateBrocken.prefab",SanctuaryCenter+new Vector3(0,0,65),24,0,"Portada fracturada");EnsureUrbanColliders(gate);
            for(int side=-1;side<=1;side+=2){
                var statue=Place(Egyptian+"SM_EgyptianStatue_Low_BIG.prefab",SanctuaryCenter+new Vector3(side*42,0,58),21,180,"Guardian del Eclipse");EnsureUrbanColliders(statue);
                for(int row=0;row<6;row++)EnsureUrbanColliders(Place(Egyptian+(row==3?"SM_PillarFlower2_Broken_1.prefab":"SM_PillarFlower2.prefab"),SanctuaryCenter+new Vector3(side*34,0,-60+row*20),row==3?7:14,0,"Columnata del Santuario"));
            }
            Place(Egyptian+"SM_Altar.prefab",SanctuaryCenter+new Vector3(0,0,90),5,180,"Altar del observatorio");
            Crystal(SanctuaryCenter+new Vector3(0,8,90),new Vector3(3,16,3));
            for(int i=0;i<90;i++){
                float a=Next(0,Mathf.PI*2),r=Next(115,202);var p=SanctuaryCenter+new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r);
                if(NearSanctuaryRoute(p))continue;
                Place(Temple+"Flora/Tree_0"+(i%2+1)+".prefab",p,Next(16,27),Next(0,360),"Arbol del Santuario");
                if(i%5==0)EnsureUrbanColliders(Place(Temple+"Damaged/Column_Broken_01.prefab",p+Vector3.right*6,Next(3,7),Next(0,360),"Ruina de exploracion"));
            }
            for(int i=0;i<3;i++){
                var p=SanctuaryCenter+new Vector3(i==0?-105:105,0,-65+i*68);
                for(int c=0;c<4;c++)Primitive("Cobertura de excavacion",PrimitiveType.Cube,p+new Vector3((c%2)*16-8,.8f,(c/2)*18-9),new Vector3(5,1.6f,1.5f),stone,true);
                Crystal(p+Vector3.up*3,new Vector3(2,6,2));
            }
            RepairGroundSurfaces();RepairRockCollisions();Physics.SyncTransforms();ClearBridgeRockIntersections();ClearSanctuaryApproaches();RepairVoidZone();Physics.SyncTransforms();
            var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();surface.BuildNavMesh();
            string navPath=Root+"/NavMesh-Eclipse.asset";var oldNav=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if(oldNav){EditorUtility.CopySerialized(surface.navMeshData,oldNav);surface.RemoveData();surface.navMeshData=oldNav;surface.AddData();EditorUtility.SetDirty(oldNav);}else AssetDatabase.CreateAsset(surface.navMeshData,navPath);
            EditorUtility.SetDirty(surface);
            DiagnoseEclipseRoutes();VerifyPath(SanctuaryCenter);foreach(var origin in new[]{SatelliteCenters[0],SatelliteCenters[2]})VerifyPath((origin+SanctuaryCenter)*.5f);
            ValidateGroundSamples();ValidateRockAndVoid();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Documentation/eclipse-island.txt","Santuario del Eclipse: "+SanctuaryCenter+"\nDiametro: 440 m. Dos puentes: ruinas occidentales y bosque norte.\nSuelo: mallas densas con normales hacia arriba, UV a escala constante y MeshCollider compartido.\n");
            Debug.Log("[Eclipse] New island, two bridges and ground repair saved.");
        }
        static bool NearSanctuaryRoute(Vector3 p)
        {
            foreach(var origin in new[]{SatelliteCenters[0],SatelliteCenters[2]}){
                var delta=origin-SanctuaryCenter;var v=p-SanctuaryCenter;float t=Mathf.Clamp01(Vector3.Dot(v,delta)/delta.sqrMagnitude);
                if(Vector3.Distance(v,delta*t)<24)return true;
            }return false;
        }
        static void ClearSanctuaryApproaches()
        {
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).ToArray()){
                if(!t)continue;string name=t.name.ToLowerInvariant();
                if(!name.Contains("arbol")&&!name.Contains("afloramiento")&&!name.Contains("ruina de exploracion"))continue;
                foreach(var origin in new[]{SatelliteCenters[0],SatelliteCenters[2]}){
                    Vector3 span=SanctuaryCenter-origin;float t0=Mathf.Clamp01(Vector3.Dot(t.position-origin,span)/span.sqrMagnitude);
                    if(Vector3.Distance(t.position,origin+span*t0)<16){UnityEngine.Object.DestroyImmediate(t.gameObject);break;}
                }
            }
        }
        static void RepairGroundSurfaces()
        {
            // Original islands were a 64-triangle fan; the recovered Sun surface also
            // overlapped its flat foundation. Retain island boundaries and walk levels.
            var groundTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Standard Assets/Eternal Temple/Textures/Soil_01_diffuse.png");
            var normal=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Standard Assets/Eternal Temple/Textures/Soil_01_normal.png");
            soil.SetTexture("_BaseMap",groundTexture);soil.SetColor("_BaseColor",new Color(.68f,.72f,.57f));soil.SetFloat("_Smoothness",.08f);soil.SetFloat("_Metallic",0);soil.SetTexture("_BumpMap",normal);soil.SetFloat("_BumpScale",.45f);soil.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(soil);
            rock.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HIVEMIND/PostApocalypticTown/URP/Art/Textures/LandscapeMaterial/T_Canyon_A.png"));
            rock.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HIVEMIND/PostApocalypticTown/URP/Art/Textures/LandscapeMaterial/T_Canyon_N.png"));rock.EnableKeyword("_NORMALMAP");rock.SetColor("_BaseColor",new Color(.52f,.56f,.59f));rock.SetTextureScale("_BaseMap",new Vector2(3,2));rock.SetTextureScale("_BumpMap",new Vector2(3,2));rock.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(rock);
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
                if(!filter.sharedMesh)continue;
                string file=AssetDatabase.GetAssetPath(filter.sharedMesh);
                if(file.StartsWith(Root+"/Meshes/")&&(file.Contains("Satellite_")||file.Contains("UrbanIsland")||file.Contains("ExtraIsland_"))){
                    float radius=filter.sharedMesh.bounds.extents.z/1.04f;
                    if(filter.transform.position==UrbanCenter)radius=180;
                    for(int i=0;i<3;i++)if(Vector3.Distance(filter.transform.position,SatelliteCenters[i])<1)radius=RegionRadii[i];
                    if(Vector3.Distance(filter.transform.position,SanctuaryCenter)<1)radius=220;
                    if(Vector3.Distance(filter.transform.position,SunCenter)<1){
                        // The recovered relief alone provides both rendering/collision.
                        filter.GetComponent<Renderer>().enabled=false;var flatCollider=filter.GetComponent<Collider>();if(flatCollider)flatCollider.enabled=false;continue;
                    }
                    var mesh=DenseGround(radius,filter.sharedMesh.name);
                    EditorUtility.CopySerialized(mesh,filter.sharedMesh);EditorUtility.SetDirty(filter.sharedMesh);UnityEngine.Object.DestroyImmediate(mesh);
                    if(filter.TryGetComponent<MeshCollider>(out var col)){col.sharedMesh=null;col.sharedMesh=filter.sharedMesh;}
                }
            }
            // Repair up-facing normals only on generated terrain; never edit vendor meshes.
            foreach(string path in new[]{Root+"/Meshes/ForestIsland.asset",SunRoot+"/SunTerrain.asset"}){
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh)continue;var normals=mesh.normals;
                if(path.Contains("SunTerrain")){
                    var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i].y=Mathf.Max(.04f,vertices[i].y);mesh.vertices=vertices;
                    mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                    foreach(var collider in UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.sharedMesh==mesh)){collider.sharedMesh=null;collider.sharedMesh=mesh;}
                }
                int down=normals.Count(n=>n.y<-.1f);if(down>normals.Length/2){var tris=mesh.triangles;for(int i=0;i<tris.Length;i+=3){int tmp=tris[i+1];tris[i+1]=tris[i+2];tris[i+2]=tmp;}mesh.triangles=tris;mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);}
            }
        }
        static Mesh DenseGround(float radius,string label)
        {
            const int rings=32,segments=128;var v=new List<Vector3>{Vector3.zero};var uv=new List<Vector2>{Vector2.zero};var tris=new List<int>();
            for(int r=1;r<=rings;r++)for(int s=0;s<segments;s++){
                float a=s*Mathf.PI*2/segments,rad=radius*r/rings*(1+.06f*Mathf.Sin(a*5));float x=Mathf.Sin(a)*rad,z=Mathf.Cos(a)*rad;
                // Flat traverse lanes remain aligned with existing props and bridges.
                v.Add(new Vector3(x,0,z));uv.Add(new Vector2(x,z)/8f);
            }
            for(int s=0;s<segments;s++)tris.AddRange(new[]{0,1+s,1+(s+1)%segments});
            for(int r=1;r<rings;r++)for(int s=0;s<segments;s++){int a=1+(r-1)*segments+s,b=1+(r-1)*segments+(s+1)%segments,c=a+segments,d=b+segments;tris.AddRange(new[]{a,c,b,b,c,d});}
            var m=new Mesh{name=label,vertices=v.ToArray(),uv=uv.ToArray(),triangles=tris.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        static void ValidateGroundSamples()
        {
            int count=0;foreach(var center in SatelliteCenters.Concat(new[]{UrbanCenter,SanctuaryCenter}))for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++){
                var p=center+new Vector3(x*18,35,z*18);if(!Physics.Raycast(p,Vector3.down,out var hit,65))throw new Exception("Ground collider missing: "+p);count++;
            }
            File.WriteAllText("Documentation/ground-validation.txt",count+" ground rays PASS; generated mesh normals and paired colliders repaired.\nSun native relief retained; flat overlapping foundation disabled.\n");
        }
        static void DiagnoseEclipseRoutes()
        {
            var report="";
            foreach(var origin in new[]{new Vector3(0,0,-15),SatelliteCenters[0],SatelliteCenters[2]})for(int i=0;i<=10;i++){
                var p=Vector3.Lerp(origin,SanctuaryCenter,i/10f);bool found=NavMesh.SamplePosition(p,out var hit,5,NavMesh.AllAreas);var nav=new NavMeshPath();bool from=NavMesh.SamplePosition(origin,out var start,5,NavMesh.AllAreas);
                if(from&&found)NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,nav);
                report+=$"From {origin} t={i/10f} query={p} found={found} sample={hit.position} status={nav.status} corners={string.Join(";",nav.corners.Select(c=>c.ToString()))}\n";
            }
            File.WriteAllText("Documentation/eclipse-route-debug.txt",report);
        }
    }
}
