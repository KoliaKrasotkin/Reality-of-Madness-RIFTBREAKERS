using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace ROM.Horde.EditorTools
{
    // Additive first art pass. Does not regenerate the gameplay scene or network identities.
    public static partial class FracturedForestBuilder
    {
        const string ScenePath = "Assets/ROM/Levels/map_rom_horde.unity";
        const string Root = "Assets/ROM/Levels/FracturedForest";
        const string Temple = "Assets/Standard Assets/Eternal Temple/Prefabs/";
        const string Egyptian = "Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/";
        const string Group = "Bosque Fracturado - Etapa 1";
        static System.Random Random = new System.Random(19026);
        static readonly Dictionary<Material, Material> Converted = new Dictionary<Material, Material>();
        static Material soil, path, rock, stone, cyan;
        static Transform root;

        [MenuItem("ROM/Horda/Aplicar Bosque Fracturado (etapa 1)")]
        public static void FromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build();
        }

        public static void FromCommandLine()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void BuildAndPreviewFromCommandLine()
        {
            try { Build(); PreviewFromCommandLine(); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        [MenuItem("ROM/Horda/Agregar islas y puentes (etapa 2)")]
        public static void IslandsFromMenu()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) AddIslands();
        }

        public static void IslandsFromCommandLine()
        {
            try { AddIslands(); PreviewFromCommandLine(); }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static readonly Vector3[] SatelliteCenters = { new Vector3(-310,8,0),new Vector3(310,8,0),new Vector3(0,8,450) };
        static readonly float[] RegionRadii = {155,170,185};
        static void AddIslands()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);


            const string group="Bosque Fracturado - Islas y puentes";
            foreach(var go in scene.GetRootGameObjects().Where(g=>g.name==group)) UnityEngine.Object.DestroyImmediate(go);
            root=new GameObject(group).transform;
            Random=new System.Random(29026); Converted.Clear();
            soil=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/ForestSoil.mat");
            stone=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/WeatheredRuins.mat");
            rock=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/FracturedCliff.mat");
            cyan=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/DimensionalCrystal.mat");
            var timber=Mat("BridgeTimber",new Color(.29f,.21f,.13f));
            for(int i=0;i<SatelliteCenters.Length;i++)
            {
                Vector3 center=SatelliteCenters[i];
                string label=new[]{"Isla de las ruinas","Isla del cristal","Isla del bosque antiguo"}[i];
                float radius=RegionRadii[i];
                Satellite(center,radius,label);
                Vector3 direction=new Vector3(center.x,0,center.z).normalized;
                Vector3 start=direction*(i==2?96:74);
                Vector3 shoulder=direction*(i==2?132:106)+Vector3.up*8;
                Vector3 end=center-direction*(radius-22);
                Bridge(start,shoulder,timber,"Rampa de acceso - "+label);
                Bridge(shoulder,end,timber,"Puente - "+label);
                RegionSectors(center,radius,i,label,timber);
                if(i==0)
                {
                    for(int c=0;c<6;c++)
                        Place(Temple+"Damaged/Column_Broken_01.prefab",center+new Vector3((c%3-1)*8,0,(c/3*2-1)*9),Next(5,9),Next(0,360),"Ruinas de la isla");
                }
                else if(i==1) Crystal(center+Vector3.up*5,new Vector3(4,10,4));
                else Place(Temple+"Bases/Base_Arch_Tall_Broken_01.prefab",center+Vector3.forward*12,12,0,"Santuario del bosque antiguo");
            }
            // Lateral crossings form exploration loops instead of three dead ends.
            for(int side=0;side<2;side++)
            {
                Vector3 direction=(SatelliteCenters[2]-SatelliteCenters[side]).normalized;
                Bridge(SatelliteCenters[side]+direction*(RegionRadii[side]-24),SatelliteCenters[2]-direction*(RegionRadii[2]-24),timber,"Paso entre regiones "+side);
            }
            // Clear full-width approaches through the original forest, including tree colliders.
            foreach(var tree in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Bosque - arbol" || t.name=="Sotobosque").ToArray())
                if((Mathf.Abs(tree.position.z)<19 && Mathf.Abs(tree.position.x)>58) || (Mathf.Abs(tree.position.x)<19 && tree.position.z>80))
                    UnityEngine.Object.DestroyImmediate(tree.gameObject);
            // The old north arch crosses the elevated deck at its crown: keep the
            // flanking pillars and move the arch onto the northern island instead.
            foreach(var gate in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Puerta del bosque" && t.position.z>80).ToArray())
                UnityEngine.Object.DestroyImmediate(gate.gameObject);
            // Remove floating set dressing that intersects the new traversal areas.
            foreach(var item in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Fragmento flotante distante").ToArray())
                if(SatelliteCenters.Any(c=>Vector2.Distance(new Vector2(c.x,c.z),new Vector2(item.position.x,item.position.z))<220))
                    UnityEngine.Object.DestroyImmediate(item.gameObject);
            var deathZone=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="DeathZone");
            if(deathZone && deathZone.TryGetComponent<BoxCollider>(out var deathCollider)) { deathCollider.size=new Vector3(1300,20,1200); deathZone.transform.position=new Vector3(0,-45,200); }
            var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            if(!surface) throw new InvalidOperationException("Falta NavMeshSurface.");
            surface.BuildNavMesh();
            string navPath=Root+"/NavMesh-Islands.asset";
            var old=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if(old) { EditorUtility.CopySerialized(surface.navMeshData,old); surface.RemoveData(); surface.navMeshData=old; surface.AddData(); EditorUtility.SetDirty(old); }
            else AssetDatabase.CreateAsset(surface.navMeshData,navPath);
            EditorUtility.SetDirty(surface);
            foreach(var center in SatelliteCenters) VerifyPath(center);
            for(int region=0;region<3;region++)
                foreach(var offset in SectorOffsets(RegionRadii[region])) VerifyPath(SatelliteCenters[region]+offset);
            for(int side=0;side<2;side++)
            {
                Vector3 direction=(SatelliteCenters[2]-SatelliteCenters[side]).normalized;
                Vector3 a=SatelliteCenters[side]+direction*(RegionRadii[side]-24),b=SatelliteCenters[2]-direction*(RegionRadii[2]-24);
                // The midpoint must lie on the crossing, proving that the bridge itself is traversable.
                VerifyPath((a+b)*.5f);
            }
            ValidateNavigation();
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[Forest] Destino ampliado: regiones de 310, 340 y 370 m de diametro; 14 destinos y cinco conexiones verificados.");
        }

        static Vector3[] SectorOffsets(float radius) => new[]{new Vector3(-radius*.42f,0,radius*.36f),new Vector3(radius*.42f,0,radius*.36f),new Vector3(0,0,-radius*.48f)};

        static bool RegionRoad(Vector3 offset,float radius)
        {
            // Wide cross routes join all bridge mouths and the three activity clearings.
            if(Mathf.Abs(offset.x)<13 || Mathf.Abs(offset.z)<13) return true;
            foreach(var end in SectorOffsets(radius))
            {
                float t=Mathf.Clamp01(Vector3.Dot(offset,end)/end.sqrMagnitude);
                if(Vector3.Distance(offset,end*t)<13 || Vector3.Distance(offset,end)<23) return true;
            }
            // North-side diagonal crossings remain clear of full tree colliders.
            return offset.z>0 && Mathf.Abs(Mathf.Abs(offset.x)-offset.z*.69f)<18;
        }

        static void RegionSectors(Vector3 center,float radius,int region,string label,Material timber)
        {
            var points=SectorOffsets(radius);
            var routeMat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/OldSandstonePath.mat");
            var ends=new List<Vector3>(points){Vector3.forward*(radius-10),Vector3.back*(radius-10),Vector3.right*(radius-10),Vector3.left*(radius-10)};
            for(int side=0;side<2;side++)
            {
                Vector3 dir=region==2 ? (SatelliteCenters[side]-center).normalized : (SatelliteCenters[2]-center).normalized;
                ends.Add(dir*(radius-15));
            }
            foreach(var end in ends)
            {
                var road=Primitive(label+" - sendero",PrimitiveType.Cube,center+end*.5f+Vector3.up*.025f,new Vector3(10,.035f,end.magnitude),routeMat,false);
                road.transform.rotation=Quaternion.LookRotation(end);
            }
            string[][] names={new[]{"Patio del santuario","Archivo derrumbado","Plaza de los guardianes"},new[]{"Cantera dimensional","Observatorio fracturado","Cuenca del cristal"},new[]{"Claro ancestral","Paso de las raices","Refugio del norte"}};
            for(int s=0;s<points.Length;s++)
            {
                Vector3 p=center+points[s];
                var area=new GameObject(names[region][s]); area.transform.SetParent(root,false); area.transform.position=p;
                for(int c=0;c<5;c++)
                {
                    float angle=(c*72+25)*Mathf.Deg2Rad;
                    Vector3 pos=p+new Vector3(Mathf.Sin(angle)*18,0,Mathf.Cos(angle)*18);
                    if(region==1) Crystal(pos+Vector3.up*Next(2,4),new Vector3(Next(1,3),Next(5,11),Next(1,3)));
                    else Place(Temple+"Damaged/Column_Broken_0"+(c%2+1)+".prefab",pos,Next(4,9),Next(0,360),names[region][s]+" - ruina");
                }
                // Low cover, with open access lanes through the center of each clearing.
                for(int c=0;c<4;c++)
                    Primitive(names[region][s]+" - cobertura",PrimitiveType.Cube,p+new Vector3(c%2==0?-10:10,.65f,c<2?-9:9),new Vector3(4,1.3f,1.2f),stone,true);
            }
            var used=new List<Vector3>();
            int wanted=region==2?165:110;
            for(int attempt=0;attempt<10000 && used.Count<wanted;attempt++)
            {
                float angle=Next(0,Mathf.PI*2),r=Mathf.Sqrt(Next(0,1))*(radius-12);
                var offset=new Vector3(Mathf.Sin(angle)*r,0,Mathf.Cos(angle)*r);
                if(offset.magnitude<28 || RegionRoad(offset,radius) || used.Any(p=>Vector3.Distance(p,offset)<12)) continue;
                Place(Temple+"Flora/Tree_0"+(used.Count%2+1)+".prefab",center+offset,Next(region==2?18:13,region==2?28:22),Next(0,360),label+" - arbol");
                used.Add(offset);
            }
            // Outcrops divide sightlines into smaller pockets without blocking the main routes.
            for(int i=0;i<22;i++)
            {
                float angle=Next(0,Mathf.PI*2),r=Next(30,radius-20);
                var offset=new Vector3(Mathf.Sin(angle)*r,0,Mathf.Cos(angle)*r);
                if(RegionRoad(offset,radius)) continue;
                var outcrop=Primitive(label+" - afloramiento",PrimitiveType.Sphere,center+offset+Vector3.up*2,new Vector3(Next(6,12),Next(4,9),Next(6,12)),rock,true);
                outcrop.transform.rotation=Quaternion.Euler(0,Next(0,360),0);
            }
        }

        static Mesh SatelliteSurface(Vector3 center,float radius,string label)
        {
            const int n=64;
            var v=new List<Vector3>{Vector3.zero}; var uv=new List<Vector2>{Vector2.zero}; var tris=new List<int>();
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n, r=radius*(1+.06f*Mathf.Sin(a*5));
                var p=new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r); v.Add(p); uv.Add(new Vector2(p.x,p.z)*.04f);
            }
            for(int i=0;i<n;i++) tris.AddRange(new[]{0,1+i,1+(i+1)%n});
            var mesh=new Mesh{name=label,vertices=v.ToArray(),triangles=tris.ToArray(),uv=uv.ToArray()}; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            int index=Array.IndexOf(SatelliteCenters,center);
            string key=index>=0?"Satellite_"+index:center==UrbanCenter?"UrbanIsland":"ExtraIsland_"+Mathf.RoundToInt(center.x)+"_"+Mathf.RoundToInt(center.z);
            string file=Root+"/Meshes/"+key+".asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(file);
            if(old) { EditorUtility.CopySerialized(mesh,old); mesh=old; EditorUtility.SetDirty(mesh); } else AssetDatabase.CreateAsset(mesh,file);
            return mesh;
        }

        static void Satellite(Vector3 center,float radius,string label)
        {
            var mesh=SatelliteSurface(center,radius,label);
            var go=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
            go.transform.SetParent(root,false); go.transform.position=center; go.isStatic=true;
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=soil; go.GetComponent<MeshCollider>().sharedMesh=mesh;
            Primitive(label+" - socle",PrimitiveType.Sphere,center+Vector3.down*30,new Vector3(radius*1.85f,56,radius*1.85f),rock,false);
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI*2/64;
                Primitive(label+" - acantilado",PrimitiveType.Sphere,center+new Vector3(Mathf.Sin(a)*(radius-8),-16,Mathf.Cos(a)*(radius-8)),new Vector3(Next(18,28),Next(34,52),Next(18,28)),rock,false);
            }
        }

        static void Bridge(Vector3 start,Vector3 end,Material timber,string label)
        {
            var delta=end-start; var rotation=Quaternion.LookRotation(delta); var right=rotation*Vector3.right;
            var deck=Primitive(label+" - suelo continuo",PrimitiveType.Cube,(start+end)*.5f-Vector3.up*.35f,new Vector3(8,.7f,delta.magnitude+1),stone,true);
            deck.transform.rotation=rotation;
            int sections=Mathf.CeilToInt(delta.magnitude/4);
            for(int i=0;i<=sections;i++)
            {
                Vector3 p=Vector3.Lerp(start,end,i/(float)sections);
                var plank=Primitive(label+" - travesano",PrimitiveType.Cube,p-Vector3.up*.04f,new Vector3(8.3f,.12f,.35f),timber,false); plank.transform.rotation=rotation;
                foreach(float sign in new[]{-1f,1f})
                    Primitive(label+" - poste",PrimitiveType.Cube,p+right*sign*3.8f+Vector3.up*.65f,new Vector3(.3f,1.4f,.3f),timber,true);
            }
            foreach(float sign in new[]{-1f,1f})
            {
                var rail=Primitive(label+" - baranda",PrimitiveType.Cube,(start+end)*.5f+right*sign*3.8f+Vector3.up*1.2f,new Vector3(.25f,.35f,delta.magnitude),timber,true); rail.transform.rotation=rotation;
            }
        }

        static void VerifyPath(Vector3 end)
        {
            var p=new NavMeshPath();
            if(!NavMesh.SamplePosition(end,out var target,3,NavMesh.AllAreas) || !NavMesh.SamplePosition(new Vector3(0,0,-15),out var start,5,NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(start.position,target.position,NavMesh.AllAreas,p) || p.status!=NavMeshPathStatus.PathComplete)
                throw new InvalidOperationException("Puente sin ruta navegable hacia "+end);
        }

        static void RepairRockCollisions()
        {
            int count=0;
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                string name=filter.name.ToLowerInvariant();
                if(!name.Contains("acantilado") && !name.Contains("afloramiento") && !name.Contains("fragmento flotante") && !name.Contains("socle")) continue;
                if(!filter.sharedMesh) continue;
                foreach(var old in filter.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(old);
                var collider=filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh=filter.sharedMesh; collider.convex=false; collider.isTrigger=false;
                filter.gameObject.layer=0; filter.gameObject.isStatic=true;
                count++;
            }
            Physics.SyncTransforms();
            Debug.Log("[Forest] Colision real aplicada a "+count+" mallas de roca.");
        }

        [MenuItem("ROM/Horda/Aplicar cascadas, colisiones y vacio")]
        public static void EnvironmentUpgradeFromMenu()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) UpgradeEnvironment();
        }

        public static void EnvironmentUpgradeFromCommandLine()
        {
            try { UpgradeEnvironment(); PreviewFromCommandLine(); }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void ProbeUrbanSourcesFromCommandLine()
        {
            try
            {
                var lines=new List<string>();
                foreach(string path in new[]{"Assets/HIVEMIND/PostApocalypticTown/URP/Scenes/LV_Junker_Town.unity","Assets/HIVEMIND/URP/DestructedBuildings/Scenes/L_Destructed_Building_Presentation.unity"})
                {
                    var source=EditorSceneManager.OpenScene(path);
                    lines.Add(path);
                    foreach(var go in source.GetRootGameObjects())
                    {
                        var renderers=go.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
                        if(renderers.Length==0) continue;
                        var b=renderers[0].bounds; foreach(var r in renderers) b.Encapsulate(r.bounds);
                        lines.Add(go.name+" | meshes="+renderers.Length+" | center="+b.center+" | size="+b.size);
                    }
                }
                File.WriteAllLines("Documentation/urban-source-bounds.txt",lines);
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void UpgradeEnvironment()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);

            UrbanIsland();
            Waterfalls();
            RepairRockCollisions();
            ClearBridgeRockIntersections();
            RepairVoidZone();
            var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            surface.BuildNavMesh();
            string file=Root+"/NavMesh-Environment.asset";
            var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(file);
            if(existing) { EditorUtility.CopySerialized(surface.navMeshData,existing); surface.RemoveData(); surface.navMeshData=existing; surface.AddData(); EditorUtility.SetDirty(existing); }
            else AssetDatabase.CreateAsset(surface.navMeshData,file);
            EditorUtility.SetDirty(surface);
            foreach(var center in SatelliteCenters) VerifyPath(center);
            for(int i=0;i<3;i++) foreach(var offset in SectorOffsets(RegionRadii[i])) VerifyPath(SatelliteCenters[i]+offset);
            VerifyPath(UrbanCenter);
            foreach(var offset in new[]{new Vector3(0,0,90),new Vector3(0,0,-90),new Vector3(100,0,0),new Vector3(-100,0,0)}) VerifyPath(UrbanCenter+offset);
            ValidateNavigation();
            ValidateRockAndVoid();
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[Forest] Cascadas, colisiones y DeathZone guardadas y verificadas.");
        }

        static void ClearBridgeRockIntersections()
        {
            var decks=UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name.Contains("suelo continuo")).ToArray();
            foreach(var collider in UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
            {
                string name=collider.name.ToLowerInvariant();
                if(!name.Contains("acantilado") && !name.Contains("fragmento flotante")) continue;
                bool overlaps=false;
                foreach(var deck in decks)
                {
                    float length=deck.size.z*deck.transform.lossyScale.z;
                    for(float d=-length*.5f;d<=length*.5f;d+=2)
                    {
                        Vector3 p=deck.transform.position+deck.transform.forward*d+Vector3.up*.45f;
                        if(collider.Raycast(new Ray(p+Vector3.up*2,Vector3.down),out var hit,2.4f)) { overlaps=true; break; }
                    }
                    if(overlaps) break;
                }
                // Remove the obstructing rock geometry as well as its collider.
                if(overlaps) UnityEngine.Object.DestroyImmediate(collider.gameObject);
            }
        }

        static readonly Vector3 UrbanCenter=new Vector3(100,8,-470);
        static void UrbanIsland()
        {
            const string group="Bosque Fracturado - Distrito urbano";
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach(var go in scene.GetRootGameObjects().Where(g=>g.name==group)) UnityEngine.Object.DestroyImmediate(go);
            root=new GameObject(group).transform;
            Random=new System.Random(39026); Converted.Clear();
            soil=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/ForestSoil.mat");
            rock=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/FracturedCliff.mat");
            stone=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/WeatheredRuins.mat");
            cyan=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/DimensionalCrystal.mat");
            const string town="Assets/HIVEMIND/PostApocalypticTown/URP/Art/Prefabs/";
            const string destroyed="Assets/HIVEMIND/URP/DestructedBuildings/Assets/Prefabs/";
            Satellite(UrbanCenter,180,"Distrito urbano - ciudad destruida");
            var asphalt=Mat("UrbanAsphalt",new Color(.13f,.14f,.14f));
            var concrete=Mat("UrbanConcrete",new Color(.38f,.38f,.34f));
            foreach(var axis in new[]{Vector3.forward,Vector3.right})
            {
                var road=Primitive("Avenida del distrito",PrimitiveType.Cube,UrbanCenter+Vector3.up*.025f,new Vector3(20,.04f,330),asphalt,false);
                road.transform.rotation=Quaternion.LookRotation(axis);
                foreach(float side in new[]{-1f,1f})
                {
                    Vector3 across=Vector3.Cross(Vector3.up,axis)*side*12;
                    var curb=Primitive("Acera",PrimitiveType.Cube,UrbanCenter+across+Vector3.up*.08f,new Vector3(3,.16f,305),concrete,true);
                    curb.transform.rotation=Quaternion.LookRotation(axis);
                }
            }
            string[] shops={"SM_Shop1_Merged.prefab","SM_Shop3_Merged.prefab","SM_Shop4_Merged.prefab","SM_Shop5_Closed_Merged.prefab","SM_Shop7_merged.prefab","SM_Shop8_Merged.prefab","SM_Shop9_Merged.prefab","SM_Garage_Merged.prefab"};
            for(int i=0;i<8;i++)
            {
                Vector3 p=UrbanCenter+new Vector3(i%2==0?-44:44,0,-105+(i/2)*66);
                var go=Place(town+shops[i],p,Next(11,17),i%2==0?90:-90,"Barrio comercial - "+i);
                FitUrbanFootprint(go,p,42); EnsureUrbanColliders(go);
            }
            // Open, damaged building shells in a second district, using the modular kit.
            for(int block=0;block<6;block++)
            {
                Vector3 origin=UrbanCenter+new Vector3(block%2==0?-105:105,0,-82+(block/2)*83);
                for(int floor=0;floor<2+(block%2);floor++)
                    for(int wall=0;wall<7;wall++)
                    {
                        Vector3 offset=wall<3?new Vector3((wall-1)*6,0,9):new Vector3(wall<5?-9:9,0,(wall%2==0?-3:3));
                        string prefab=wall%3==0?"SM_OrangeBrickWall_LineHalfDamaged_v1.prefab":"SM_ConcreateWall_LineHalfDamaged_v1.prefab";
                        var go=Place(destroyed+prefab,origin+offset+Vector3.up*(floor*4.5f),4.5f,wall<3?0:90,"Edificio destruido "+block+" - muro");
                        EnsureUrbanColliders(go);
                    }
                var debris=Place(destroyed+"SM_Debris_Pile_ConcreteBricks_3x3_v"+(block%3+1)+".prefab",origin+new Vector3(0,0,14),1.3f,Next(0,360),"Escombros del edificio");
                EnsureUrbanColliders(debris);
            }
            string[] props={"SM_Barrel.prefab","SM_Crate3.prefab","SM_Fence_Damaged.prefab","SM_Bin_2.prefab","SM_Water_Tank.prefab"};
            for(int i=0;i<45;i++)
            {
                Vector3 p=UrbanCenter+new Vector3((i%2==0?-1:1)*Next(19,30),0,Next(-140,140));
                if(Mathf.Abs(p.z-UrbanCenter.z)<22) continue;
                var go=Place(town+props[i%props.Length],p,i%5==4?4:Next(1,2.3f),Next(0,360),"Detalle urbano - "+i);
                EnsureUrbanColliders(go);
            }
            for(int i=0;i<4;i++)
            {
                var go=Place(town+"SM_PowerTower_Merged.prefab",UrbanCenter+new Vector3(i%2==0?-140:140,0,i<2?-70:70),30,0,"Torre del distrito");
                EnsureUrbanColliders(go);
            }
            var timber=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/BridgeTimber.mat");
            Vector3 direction=new Vector3(UrbanCenter.x,0,UrbanCenter.z).normalized;
            Vector3 start=direction*88,shoulder=direction*132+Vector3.up*8,end=UrbanCenter-direction*158;
            Bridge(start,shoulder,timber,"Acceso al distrito - rampa"); Bridge(shoulder,end,timber,"Puente al distrito urbano");
            foreach(var tree in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Bosque - arbol" || t.name=="Sotobosque").ToArray())
            {
                Vector3 p=new Vector3(tree.position.x,0,tree.position.z);
                float along=Vector3.Dot(p,direction);
                if(along>65 && along<170 && (p-direction*along).magnitude<21) UnityEngine.Object.DestroyImmediate(tree.gameObject);
            }
            Debug.Log("[Forest] Distrito urbano de 360 m agregado usando ambos paquetes URP.");
        }

        static void FitUrbanFootprint(GameObject go,Vector3 position,float maxSize)
        {
            var renderers=go.GetComponentsInChildren<MeshRenderer>();
            if(renderers.Length==0) return;
            var bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            float scale=Mathf.Min(1,maxSize/Mathf.Max(bounds.size.x,bounds.size.z));
            go.transform.localScale*=scale;
            bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            go.transform.position+=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        }

        static void EnsureUrbanColliders(GameObject go)
        {
            foreach(var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh || (filter.TryGetComponent<MeshRenderer>(out var renderer) && !renderer.enabled)) continue;
                if(!filter.GetComponent<Collider>()) filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
                filter.gameObject.layer=0; filter.gameObject.isStatic=true;
            }
        }

        static void ValidateRockAndVoid()
        {
            Physics.SyncTransforms();
            int checkedRocks=0;
            foreach(var collider in UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
            {
                string name=collider.name.ToLowerInvariant();
                if(!name.Contains("acantilado") && !name.Contains("afloramiento") && !name.Contains("fragmento flotante") && !name.Contains("socle")) continue;
                var bounds=collider.bounds;
                if(!collider.Raycast(new Ray(bounds.center+Vector3.up*(bounds.extents.y+5),Vector3.down),out var hit,bounds.size.y+10))
                    throw new InvalidOperationException("La roca no bloquea el raycast: "+collider.name);
                checkedRocks++;
            }
            var zone=GameObject.Find("DeathZone"); var box=zone.GetComponent<BoxCollider>(); var guard=zone.GetComponent<HordeVoidKillPlane>();
            if(!box.isTrigger || !guard || guard.KillHeight!=box.bounds.max.y)
                throw new InvalidOperationException("DeathZone y plano de muerte no coinciden.");
            foreach(var center in SatelliteCenters)
                if(!box.bounds.Contains(new Vector3(center.x,zone.transform.position.y,center.z))) throw new InvalidOperationException("DeathZone no cubre una region.");
            Debug.Log("[Forest] Raycasts fisicos correctos en "+checkedRocks+" rocas; cobertura de DeathZone verificada.");
        }

        static void RepairVoidZone()
        {
            var zone=GameObject.Find("DeathZone");
            if(!zone) throw new InvalidOperationException("Falta DeathZone original; no se pudo conservar su identidad de red.");
            zone.transform.position=new Vector3(0,-58,130);
            zone.transform.localScale=Vector3.one;
            var box=zone.GetComponent<BoxCollider>(); box.isTrigger=true; box.enabled=true;
            box.size=new Vector3(2300,40,2300);
            var body=zone.GetComponent<Rigidbody>(); if(!body) body=zone.AddComponent<Rigidbody>();
            body.isKinematic=true; body.useGravity=false;
            var fallback=zone.GetComponent<HordeVoidKillPlane>(); if(!fallback) fallback=zone.AddComponent<HordeVoidKillPlane>();
            fallback.KillHeight=-38;
            zone.layer=11;
            // Match the trigger against character controllers, hitboxes and items.
            box.includeLayers=(1<<6)|(1<<7)|(1<<8)|(1<<9)|(1<<10)|(1<<12);
            box.layerOverridePriority=10;
            EditorUtility.SetDirty(zone);
            Debug.Log("[Forest] DeathZone bajo todo el destino; plano de muerte servidor en Y=-38.");
        }

        static void Waterfalls()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            const string group="Bosque Fracturado - Cascadas";
            foreach(var go in scene.GetRootGameObjects().Where(g=>g.name==group)) UnityEngine.Object.DestroyImmediate(go);
            Transform oldRoot=root;
            root=new GameObject(group).transform;
            string texturePath=Root+"/Materials/WaterfallStreaks.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(!texture)
            {
                texture=new Texture2D(128,256,TextureFormat.RGBA32,false){name="WaterfallStreaks",wrapMode=TextureWrapMode.Repeat};
                var pixels=new Color[128*256];
                for(int y=0;y<256;y++) for(int x=0;x<128;x++)
                {
                    float stripe=Mathf.PerlinNoise(x*.14f,y*.014f);
                    float foam=Mathf.PerlinNoise(x*.04f+20,y*.07f);
                    float white=Mathf.Clamp01((stripe-.32f)*1.8f+foam*.2f);
                    pixels[y*128+x]=new Color(Mathf.Lerp(.1f,1,white),Mathf.Lerp(.45f,1,white),Mathf.Lerp(.65f,1,white),Mathf.Lerp(.35f,.9f,white));
                }
                texture.SetPixels(pixels); texture.Apply(); AssetDatabase.CreateAsset(texture,texturePath);
            }
            string file=Root+"/Materials/Waterfall.mat";
            var water=AssetDatabase.LoadAssetAtPath<Material>(file);
            if(!water) { water=new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(water,file); }
            water.SetTexture("_BaseMap",texture); water.SetColor("_BaseColor",Color.white);
            water.SetFloat("_Surface",1); water.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); water.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            water.SetFloat("_ZWrite",0); water.SetFloat("_Cull",0); water.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); water.renderQueue=3000;
            EditorUtility.SetDirty(water);
            var sites=new List<Vector3>{new Vector3(-78,0,-109),new Vector3(98,0,67)};
            for(int i=0;i<3;i++)
            {
                float r=RegionRadii[i];
                sites.Add(SatelliteCenters[i]+new Vector3(r*.72f,0,-r*.69f));
                sites.Add(SatelliteCenters[i]+new Vector3(-r*.78f,0,r*.63f));
            }
            sites.Add(UrbanCenter+new Vector3(130,0,-124));
            sites.Add(UrbanCenter+new Vector3(-140,0,-110));
            for(int i=0;i<sites.Count;i++)
            {
                Vector3 lip=sites[i];
                if(i<2) lip.y=Height(lip.x,lip.z);
                var normal=new Vector3(lip.x,0,lip.z).normalized;
                if(i>=2 && i<8) normal=(lip-SatelliteCenters[(i-2)/2]).normalized;
                if(i>=8) normal=(lip-UrbanCenter).normalized;
                var tangent=Vector3.Cross(Vector3.up,normal);
                float width=i%2==0?10:6;
                var mesh=new Mesh{name="WaterfallCurtain"};
                mesh.vertices=new[]{-tangent*width*.5f,tangent*width*.5f,-tangent*width*.64f+Vector3.down*78+normal*5,tangent*width*.64f+Vector3.down*78+normal*5};
                mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};
                mesh.triangles=new[]{0,1,2,2,1,3}; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                string meshPath=Root+"/Meshes/Waterfall_"+i+".asset";
                var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(old) { EditorUtility.CopySerialized(mesh,old); mesh=old; EditorUtility.SetDirty(mesh); } else AssetDatabase.CreateAsset(mesh,meshPath);
                var curtain=new GameObject("Cascada "+(i+1),typeof(MeshFilter),typeof(MeshRenderer),typeof(WaterfallFlow));
                curtain.transform.SetParent(root,false); curtain.transform.position=lip+normal*2+Vector3.up*.35f;
                curtain.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=curtain.GetComponent<MeshRenderer>(); renderer.sharedMaterial=water;
                renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
                curtain.GetComponent<WaterfallFlow>().Curtains=new[]{renderer};
                var stream=Primitive("Cauce de la cascada",PrimitiveType.Cube,lip-normal*13+Vector3.up*.09f,new Vector3(width,.1f,32),water,false);
                stream.transform.rotation=Quaternion.LookRotation(normal);
                var spray=new GameObject("Bruma de cascada",typeof(ParticleSystem)); spray.transform.SetParent(curtain.transform,false);
                spray.transform.localRotation=Quaternion.Euler(90,0,0);
                var particles=spray.GetComponent<ParticleSystem>(); var main=particles.main;
                main.startLifetime=3; main.startSpeed=2; main.startSize=1.1f; main.startColor=new Color(.7f,.9f,1,.25f); main.maxParticles=60; main.gravityModifier=.1f;
                var emission=particles.emission; emission.rateOverTime=14;
                var shape=particles.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.radius=width*.45f; shape.angle=16;
                spray.GetComponent<ParticleSystemRenderer>().sharedMaterial=water;
            }
            root=oldRoot;
            Debug.Log("[Forest] Diez cascadas con agua animada y bruma agregadas.");
        }

        public static void Build()
        {
            Random = new System.Random(19026);
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Meshes");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ScenePath);

            foreach (var go in scene.GetRootGameObjects().Where(g => g.name == Group))
                UnityEngine.Object.DestroyImmediate(go);
            Converted.Clear();
            root = new GameObject(Group).transform;
            soil = Mat("ForestSoil", new Color(.15f, .21f, .11f));
            soil.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Standard Assets/Eternal Temple/Textures/Soil_01_diffuse.png"));
            soil.SetColor("_BaseColor",new Color(.65f,.75f,.52f));
            path = Mat("OldSandstonePath", new Color(.40f, .33f, .22f));
            rock = Mat("FracturedCliff", new Color(.16f, .20f, .22f));
            stone = Mat("WeatheredRuins", new Color(.32f, .37f, .32f));
            cyan = Mat("DimensionalCrystal", new Color(.04f, .40f, .58f), true);

            Island();
            Paths();
            Landmarks();
            Forest();
            Fragments();
            Environment();
            foreach(var go in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
                if(go.name=="SkyRift" || go.name=="SkyRiftInner") go.gameObject.SetActive(false);
            // Existing arena stays readable, but its floor loses the metal appearance.
            foreach (var renderer in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)))
                if (renderer.gameObject.name == "Floor" && renderer.transform.parent && renderer.transform.parent.name == "Platform")
                    renderer.sharedMaterial = stone;

            var horde = UnityEngine.Object.FindFirstObjectByType<Horde>();
            if (horde)
            {
                var serialized = new SerializedObject(horde);
                serialized.FindProperty("_mapDisplayName").stringValue = "Bosque Fracturado";
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var surface = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            if (!surface) throw new InvalidOperationException("Falta NavMeshSurface del mapa Horda.");
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            string navPath = Root + "/NavMesh-Forest.asset";
            var previous = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if (previous)
            {
                EditorUtility.CopySerialized(surface.navMeshData, previous);
                surface.RemoveData();
                surface.navMeshData = previous;
                surface.AddData();
                EditorUtility.SetDirty(previous);
            }
            else AssetDatabase.CreateAsset(surface.navMeshData, navPath);
            EditorUtility.SetDirty(surface);
            ValidateNavigation();
            var representer=AssetDatabase.LoadMainAssetAtPath("Assets/ROM/Levels/representer_map_rom_horde.asset");
            if(representer)
            {
                var map=new SerializedObject(representer);
                map.FindProperty("Name").stringValue="Horda: Bosque Fracturado";
                map.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Forest] Etapa 1 guardada: isla de 230 x 300 m, bosque, ruinas y NavMesh.");
        }

        // Standalone visual QA: opens the saved scene without changing it.
        public static void PreviewFromCommandLine()
        {
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                var go = new GameObject("Forest preview camera", typeof(Camera));
                var camera = go.GetComponent<Camera>();
                bool islands=GameObject.Find("Bosque Fracturado - Islas y puentes");
                camera.transform.position = islands ? new Vector3(650,1000,-1100) : new Vector3(185, 235, -265);
                camera.transform.LookAt(new Vector3(0, 0, islands?200:5));
                camera.fieldOfView = 52;
                camera.farClipPlane = 3000;
                camera.nearClipPlane = 1;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.10f, .16f, .20f);
                var target = new RenderTexture(1600, 1200, 24);
                camera.targetTexture = target;
                // A design overview sits far above the playable atmosphere; reduce
                // fog for this capture only. No scene settings are saved here.
                if(islands) RenderSettings.fogDensity=.00025f;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(1600,1200,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1600,1200),0,0); image.Apply();
                Directory.CreateDirectory("Documentation");
                File.WriteAllBytes("Documentation/forest-stage1-overview.png",image.EncodeToPNG());
                if(islands)
                {
                    File.WriteAllBytes("Documentation/forest-destination-overview.png",image.EncodeToPNG());
                    RenderSettings.fogDensity=.0012f;
                    camera.nearClipPlane=.1f;
                    camera.transform.position=SatelliteCenters[0]+new Vector3(0,2,-100);
                    camera.transform.LookAt(SatelliteCenters[0]+new Vector3(0,4,15));
                    camera.Render(); RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,1600,1200),0,0); image.Apply();
                    File.WriteAllBytes("Documentation/forest-destination-ground.png",image.EncodeToPNG());
                    if(GameObject.Find("Bosque Fracturado - Distrito urbano"))
                    {
                        RenderSettings.fogDensity=.0007f;
                        camera.transform.position=UrbanCenter+new Vector3(200,220,-240);
                        camera.transform.LookAt(UrbanCenter+Vector3.up*3);
                        camera.Render(); RenderTexture.active=target;
                        image.ReadPixels(new Rect(0,0,1600,1200),0,0); image.Apply();
                        File.WriteAllBytes("Documentation/urban-island-overview.png",image.EncodeToPNG());
                        RenderSettings.fogDensity=.0012f;
                        camera.transform.position=UrbanCenter+new Vector3(0,2,-125);
                        camera.transform.LookAt(UrbanCenter+new Vector3(0,4,0));
                        camera.Render(); RenderTexture.active=target;
                        image.ReadPixels(new Rect(0,0,1600,1200),0,0); image.Apply();
                        File.WriteAllBytes("Documentation/urban-island-ground.png",image.EncodeToPNG());
                    }
                }
                camera.targetTexture=null; RenderTexture.active=null;
                UnityEngine.Object.DestroyImmediate(image); target.Release();
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(go);
                var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                Debug.Log("[Forest] Preview saved. Renderers: "+renderers.Length+"; trees: "+
                    UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Bosque - arbol"));
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static float Next(float min, float max) => min + (float)Random.NextDouble() * (max - min);
        static Vector3 At(float x, float z) => new Vector3(x, 0, z);
        static float Height(float x, float z)
        {
            float edge = Mathf.Sqrt(x*x/(115*115f) + z*z/(150*150f));
            return -.16f + Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.72f, 1f, edge)) *
                (3f + Mathf.PerlinNoise(x*.035f+100, z*.035f+100)*5);
        }

        static void Island()
        {
            const int segments = 128, rings = 28;
            var vertices = new List<Vector3> { new Vector3(0,-.16f,0) };
            var uv = new List<Vector2> { Vector2.zero };
            var triangles = new List<int>();
            for (int r=1; r<=rings; r++)
                for (int s=0; s<segments; s++)
                {
                    float a=s*Mathf.PI*2/segments;
                    float wobble = 1 + .035f*Mathf.Sin(a*7) + .025f*Mathf.Sin(a*11);
                    float x=Mathf.Sin(a)*115*r/rings*wobble, z=Mathf.Cos(a)*150*r/rings*wobble;
                    vertices.Add(new Vector3(x,Height(x,z),z)); uv.Add(new Vector2(x*.04f,z*.04f));
                }
            for (int s=0;s<segments;s++) { triangles.Add(0); triangles.Add(1+s); triangles.Add(1+(s+1)%segments); }
            for (int r=1;r<rings;r++) for (int s=0;s<segments;s++)
            {
                int a=1+(r-1)*segments+s,b=1+(r-1)*segments+(s+1)%segments,c=a+segments,d=b+segments;
                triangles.AddRange(new[]{a,c,b,b,c,d});
            }
            var mesh = new Mesh { name="ForestIsland", vertices=vertices.ToArray(), triangles=triangles.ToArray(), uv=uv.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string meshPath=Root+"/Meshes/ForestIsland.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(existing) { EditorUtility.CopySerialized(mesh,existing); mesh=existing; EditorUtility.SetDirty(mesh); }
            else AssetDatabase.CreateAsset(mesh,meshPath);
            var go=new GameObject("Isla principal - 230 x 300 metros",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
            go.transform.SetParent(root,false); go.isStatic=true;
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=soil;
            go.GetComponent<MeshCollider>().sharedMesh=mesh;
            for(int i=0;i<72;i++)
            {
                float a=i*Mathf.PI*2/72;
                var p=new Vector3(Mathf.Sin(a)*113,-12,Mathf.Cos(a)*148);
                var cliff=Primitive("Acantilado",PrimitiveType.Sphere,p,new Vector3(Next(12,22),Next(25,40),Next(12,22)),rock,false);
                cliff.transform.rotation=Quaternion.Euler(Next(-12,12),Next(0,360),Next(-15,15));
            }
        }

        static readonly Vector3[] Destinations = { new Vector3(0,0,-122),new Vector3(0,0,120),new Vector3(-72,0,22),new Vector3(72,0,28),new Vector3(60,0,-72) };
        static bool OnPath(Vector3 p, float width)
        {
            foreach(var end in Destinations)
            {
                float t=Mathf.Clamp01(Vector3.Dot(p,end)/end.sqrMagnitude);
                if(Vector3.Distance(p,end*t)<width) return true;
            }
            return false;
        }
        static void Paths()
        {
            foreach(var end in Destinations)
            {
                float length=end.magnitude;
                for(float d=33;d<length;d+=4)
                {
                    var p=end.normalized*d;
                    var tile=Primitive("Camino de tierra y losas",PrimitiveType.Cube,p+Vector3.down*.13f,new Vector3(9,.12f,4.2f),path,false);
                    tile.transform.rotation=Quaternion.LookRotation(end);
                }
            }
        }

        static void Landmarks()
        {
            // South entrance and northern continuation establish a long main axis.
            foreach(float z in new[]{-117f,113f})
            {
                Place(Temple+"Bases/Arch_Wide_01.prefab",At(0,z),14,0,"Puerta del bosque");
                foreach(float x in new[]{-11f,11f})
                    Place(Temple+"Columns/Column_01.prefab",At(x,z),11,0,"Pilon de acceso");
            }
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI*2/12;
                if(i%3==0) continue;
                Place(Temple+"Damaged/Column_Broken_01.prefab",At(-72+Mathf.Sin(a)*18,22+Mathf.Cos(a)*18),Next(5,9),Next(0,360),"Ruinas olvidadas");
            }
            Place(Temple+"Bases/Base_Arch_Tall_Broken_01.prefab",At(-72,38),13,90,"Santuario derrumbado");
            // Tilted monumental rings and crystal stand in for lost dimensional machinery.
            for(int i=0;i<7;i++)
            {
                float a=i*Mathf.PI*2/7;
                var column=Place(Temple+"Damaged/Column_Broken_02.prefab",At(72+Mathf.Sin(a)*16,28+Mathf.Cos(a)*16),Next(9,16),-i*360f/7,"Estructura VETIRENT caida");
                column.transform.Rotate(0,0,i%2==0?18:-14);
            }
            Crystal(new Vector3(72,7,28),new Vector3(3,12,3));
            for(int i=0;i<9;i++)
            {
                var p=At(60+Next(-12,12),-72+Next(-10,10));
                Place(Egyptian+"SM_Stone_01.prefab",p,Next(1,2.5f),Next(0,360),"Campamento - restos");
            }
            Place("Assets/Standard Assets/Sun_Temple/Prefabs/FX/FX_Bonfire_A.prefab",At(60,-72),2,0,"Hoguera del campamento");
            // Crystals frame the clearing without obstructing the eight existing portal approaches.
            for(int i=0;i<12;i++)
            {
                float a=(i*30+15)*Mathf.Deg2Rad;
                Crystal(new Vector3(Mathf.Sin(a)*37,1.7f,Mathf.Cos(a)*37),new Vector3(.65f,3.5f,.65f));
            }
        }

        static void Forest()
        {
            int count=0;
            var used=new List<Vector3>();
            for(int attempt=0;attempt<6000 && count<210;attempt++)
            {
                var p=At(Next(-108,108),Next(-140,140));
                float ellipse=p.x*p.x/(108*108f)+p.z*p.z/(140*140f);
                if(ellipse>1 || p.magnitude<55 || OnPath(p,10) || Destinations.Any(d=>Vector3.Distance(p,d)<23) || used.Any(q=>Vector3.Distance(p,q)<8)) continue;
                p.y=Height(p.x,p.z);
                Place(Temple+"Flora/Tree_0"+(count%2+1)+".prefab",p,Next(15,24),Next(0,360),"Bosque - arbol");
                used.Add(p); count++;
            }
            for(int i=0;i<170;i++)
            {
                var p=At(Next(-104,104),Next(-136,136));
                if(p.x*p.x/(104*104f)+p.z*p.z/(136*136f)>1 || p.magnitude<53 || OnPath(p,8) || Destinations.Any(d=>Vector3.Distance(p,d)<20)) continue;
                p.y=Height(p.x,p.z);
                Place(Temple+"Flora/Bush_0"+(i%3+1)+".prefab",p,Next(1.5f,3.2f),Next(0,360),"Sotobosque");
            }
        }

        static void Fragments()
        {
            for(int i=0;i<28;i++)
            {
                float a=Next(0,Mathf.PI*2), r=Next(170,225);
                var go=Place(Egyptian+"SM_Stone_0"+(i%6+1)+".prefab",new Vector3(Mathf.Sin(a)*r,Next(-25,55),Mathf.Cos(a)*r),Next(8,24),Next(0,360),"Fragmento flotante distante");
                foreach(var renderer in go.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>rock).ToArray();
                foreach(var c in go.GetComponentsInChildren<Collider>()) c.enabled=false;
            }
            Crystal(new Vector3(145,55,70),new Vector3(11,38,11));
        }

        static GameObject Place(string asset,Vector3 position,float height,float yaw,string label)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset);
            if(!prefab) throw new FileNotFoundException("Falta asset del bosque",asset);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);
            go.name=label; go.transform.rotation=Quaternion.Euler(0,yaw,0);
            // Legacy prefabs store their old demo-scene world position on the root.
            // Normalize it before calculating scaled bounds and grounding the instance.
            go.transform.position=Vector3.zero;
            var renderers=go.GetComponentsInChildren<Renderer>();
            var meshes=renderers.Where(r=>!(r is ParticleSystemRenderer)).ToArray();
            if(meshes.Length>0)
            {
                Bounds bounds=meshes[0].bounds;
                foreach(var r in meshes) bounds.Encapsulate(r.bounds);
                float scale=height/Mathf.Max(.01f,bounds.size.y);
                go.transform.localScale*=scale;
                var bottom=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                go.transform.position+=position-bottom*scale;
            }
            else go.transform.position=position;
            foreach(var r in meshes)
                r.sharedMaterials=r.sharedMaterials.Select(ConvertMaterial).ToArray();
            // Decorative legacy demo scripts must not create cameras or alter the scene.
            foreach(var c in go.GetComponentsInChildren<MonoBehaviour>()) if(c) c.enabled=false;
            foreach(var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic=true;
            return go;
        }

        static Material ConvertMaterial(Material source)
        {
            if(!source || source.shader.name.StartsWith("Universal Render Pipeline/")) return source;
            if(Converted.TryGetValue(source,out var cached)) return cached;
            string id=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string file=Root+"/Materials/Converted_"+id+".mat";
            var result=AssetDatabase.LoadAssetAtPath<Material>(file);
            if(!result) { result=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(result,file); }
            // Vendor Shader Graphs retain their URP maps as saved properties even
            // when an older graph no longer exposes the property to HasProperty.
            CopySavedTexture(source,result,"_BaseMap",new[]{"_BaseMap","_BaseColorMap","_MainTex","_Albedo_Map","_Base"});
            result.SetColor("_BaseColor",source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);
            CopySavedTexture(source,result,"_BumpMap",new[]{"_BumpMap","_NormalMap","_Normal_Map","_Normal"});
            if(result.GetTexture("_BumpMap")) result.EnableKeyword("_NORMALMAP");
            bool cutout=source.shader.name.IndexOf("cutout",StringComparison.OrdinalIgnoreCase)>=0 || (source.HasProperty("_Mode") && source.GetFloat("_Mode")==1) || source.IsKeywordEnabled("_ALPHATEST_ON") || source.shader.name.Contains("Tree") || source.shader.name.Contains("Nature");
            if(cutout) { result.SetFloat("_AlphaClip",1); result.SetFloat("_Cutoff",.4f); result.EnableKeyword("_ALPHATEST_ON"); result.renderQueue=2450; }
            result.SetFloat("_Cull",0); result.SetFloat("_Smoothness",.12f);
            EditorUtility.SetDirty(result); Converted[source]=result; return result;
        }

        static void CopySavedTexture(Material source,Material target,string destination,string[] candidates)
        {
            var properties=new SerializedObject(source).FindProperty("m_SavedProperties.m_TexEnvs");
            foreach(string name in candidates)
                for(int i=0;i<properties.arraySize;i++)
                {
                    var item=properties.GetArrayElementAtIndex(i);
                    if(item.FindPropertyRelative("first").stringValue!=name) continue;
                    var value=item.FindPropertyRelative("second");
                    var texture=value.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
                    if(!texture) continue;
                    target.SetTexture(destination,texture);
                    target.SetTextureScale(destination,value.FindPropertyRelative("m_Scale").vector2Value);
                    target.SetTextureOffset(destination,value.FindPropertyRelative("m_Offset").vector2Value);
                    return;
                }
        }

        static Material Mat(string name,Color color,bool emission=false)
        {
            string file=Root+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(file);
            if(!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,file); }
            m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",.1f);
            if(emission) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",color*3); }
            EditorUtility.SetDirty(m); return m;
        }
        static GameObject Primitive(string name,PrimitiveType type,Vector3 p,Vector3 scale,Material mat,bool collider)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(root,false);
            go.transform.position=p; go.transform.localScale=scale; go.isStatic=true;
            go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static void Crystal(Vector3 p,Vector3 scale)
        {
            string file=Root+"/Meshes/CrystalShard.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(file);
            if(!mesh)
            {
                mesh=new Mesh { name="CrystalShard" };
                // Separate vertices per face give the shard a faceted silhouette.
                Vector3[] points={new Vector3(0,.6f,0),new Vector3(0,-.5f,0),new Vector3(.5f,0,0),new Vector3(0,0,.5f),new Vector3(-.5f,0,0),new Vector3(0,0,-.5f)};
                int[] faces={0,3,2,0,4,3,0,5,4,0,2,5,1,2,3,1,3,4,1,4,5,1,5,2};
                mesh.vertices=faces.Select(i=>points[i]).ToArray(); mesh.triangles=Enumerable.Range(0,faces.Length).ToArray();
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh,file);
            }
            var go=new GameObject("Cristal dimensional",typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(root,false); go.transform.position=p; go.transform.localScale=scale;
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=cyan;
            go.isStatic=true;
            go.transform.rotation=Quaternion.Euler(12,35,18);
        }
        static void Environment()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.40f,.52f,.57f);
            RenderSettings.ambientEquatorColor=new Color(.23f,.32f,.26f);
            RenderSettings.ambientGroundColor=new Color(.10f,.14f,.12f);
            RenderSettings.ambientIntensity=1.3f;
            RenderSettings.fogColor=new Color(.22f,.34f,.38f); RenderSettings.fogDensity=.0012f;
            if(RenderSettings.sun) { RenderSettings.sun.color=new Color(1,.89f,.69f); RenderSettings.sun.intensity=1.6f; }
        }
        static void ValidateNavigation()
        {
            foreach(var end in Destinations)
            {
                var p=new NavMeshPath();
                if(!NavMesh.SamplePosition(end,out var target,12,NavMesh.AllAreas) ||
                   !NavMesh.SamplePosition(new Vector3(0,0,-15),out var start,5,NavMesh.AllAreas) ||
                   !NavMesh.CalculatePath(start.position,target.position,NavMesh.AllAreas,p) || p.status!=NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException("Ruta de navegacion incompleta hacia "+end);
            }
            Debug.Log("[Forest] Cinco rutas desde la arena verificadas en NavMesh.");
        }
    }
}
